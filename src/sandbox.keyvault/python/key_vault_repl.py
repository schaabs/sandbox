#!/usr/local/bin/python
from azure.common.credentials import ServicePrincipalCredentials
from azure.common.credentials import UserPassCredentials
from azure.common.credentials import BasicTokenAuthentication
from azure.keyvault import KeyVaultClient
from azure.mgmt.keyvault import KeyVaultManagementClient
from adal import token_cache
import adal
import json
import os

CLIENT_ID = '8fd4d3c4-efea-49aa-b1de-2c33c22da56e' # Azure cli
KEY_VAULT_RESOURCE = 'https://vault.azure.net'
AZURE_MANAGEMENT_RESOURCE2 = 'https://management.core.windows.net/'

def _json_format(obj):
    return json.dumps(obj, sort_keys=True, indent=4, separators=(',', ': '))

class KV_Config(dict):
    def __init__(self):

        self.authority_url = ''
        self.subscription_id = ''
        self.tenant_id = ''
        self.token_cache = ''
        self.user_id = ''
        self.resource_group = ''
        self.user_oid = ''

    def __getattr__(self, item):
        return self[item]

    def __setattr__(self, key, value):
        self[key] = value

    def from_disk(self):
        if os.path.isfile('kvconfig.json'):
            with open('kvconfig.json', 'r') as configFile:
                try:
                    dict = json.load(configFile)
                except json.JSONDecodeError:
                    print('error loading config file')
                    return
                for key, value in dict.items():
                    if value:
                        self[key] = value

    def to_disk(self):
        with open('kvconfig.json', 'w') as configFile:
            json.dump(self, configFile, sort_keys=True, indent=4, separators=(',', ': '))

class KV_Auth(object):
    def __init__(self, config):
        self._config = config

        self._cache = token_cache.TokenCache()

        if self._config.token_cache:
            self._cache.deserialize(self._config.token_cache)

        self._context = adal.AuthenticationContext(self._config.authority_url, cache=self._cache)


    def get_keyvault_creds(self):
        return self._get_creds(KEY_VAULT_RESOURCE)

    def get_arm_creds(self):
        return self._get_creds(AZURE_MANAGEMENT_RESOURCE2)

    def _get_auth_token_from_code(self, resource):
        code = self._context.acquire_user_code(resource, CLIENT_ID)

        print(code['message'])

        token = self._context.acquire_token_with_device_code(resource, code, CLIENT_ID)

        return token

    def _get_creds(self, resource):
        token = None

        if not self._config.user_id:
            token = self._get_auth_token_from_code(resource)
        else:
            token = self._context.acquire_token(resource, self._config.user_id, CLIENT_ID)

            if not token:
                token = self._get_auth_token_from_code(resource)

        self._config.token_cache = self._cache.serialize()

        if token:
            if not self._config.user_id:
                self._config.user_id = token['userId']

            if not self._config.user_oid:
                self._config.user_oid = token['oid']

            token['access_token'] = token['accessToken']

            return BasicTokenAuthentication(token)

        return None


class KV_Repl(object):
    _repl_break_commands = set(('quit', 'q', 'back', 'b'))

    def __init__(self, config):
        self._auth = KV_Auth(config)
        self._config = config
        self._mgmt_client = KeyVaultManagementClient(self._auth.get_arm_creds(), config.subscription_id)
        self._data_client = KeyVaultClient(self._auth.get_keyvault_creds())

    def start(self):
        self._vault_index();

    def _vault_index(self):
        selection = None

        exit_loop = False

        while not exit_loop:
            print('\nAvailable Vaults:\n')

            vaults = self._get_vault_list()

            for idx, vault in enumerate(vaults):
                print('%d. %s' % (idx, vault.name))

            print('\n#:select vault (a)dd (d)elete (q)uit\n')

            selection = input('> ')


            if selection == 'a' or selection == 'add':
                continue
            else:
                try:
                    i = int(selection)

                except ValueError:
                    print('invalid input')
                    continue

                selection = self._vault_details(vaults[i])

            if selection == 'q' or selection == 'quit':
                exit_loop = True
                continue

    def _add_vault(self):
        name = input('\nenter vault name:')

    def _vault_details(self, vault):
        selection = None

        exit_loop = False

        while not exit_loop:
            vault_info = self._mgmt_client.vaults.get(self._config.resource_group, vault.name)

            print('\nName:\t%s' % vault_info.name)

            print('Uri:\t%s' % vault_info.properties.vault_uri)

            print('Id:\t%s' % vault_info.id)

            print('\n(s)ecrets (k)eys (c)ertificates (b)ack (q)uit\n')

            selection = input('> ')

            if selection == 's' or selection == 'secrets':
                selection = self._secret_index(vault_info)
            elif selection == 'k' or selection == 'keys':
                print('\nnot yet implemented\n')
                continue
            elif selection == 'c' or selection == 'certificates':
                print('\nnot yet implemented\n')
                continue
            else:
                print('invalid input')
                continue

            if selection == 'b' or selection == 'back':
                selection = None
                exit_loop = True
                continue
            elif selection == 'q' or selection == 'quit':
                exit_loop = True
                continue

        return selection

    def _secret_index(self, vault_info):
        selection = None
        exit_loop = False

        while not exit_loop:
            print('\n%s Secrets:\n' % vault_info.name)

            secrets = [secret for secret in self._data_client.get_secrets(vault_info.properties.vault_uri)]

            for idx, s in enumerate(secrets):
                print('%d. %s' % (idx, KV_Repl._get_secret_name_from_url(s.id)))

            print('\n#:show secret value (a)dd (d)elete (b)ack (q)uit\n')

            selection = input('> ')

            if selection == 'a' or selection == 'add':
                self._add_secret(vault_info)
                continue
            elif selection == 'd' or selection == 'delete':
                print('\nnot yet implemented\n')
                continue
            elif selection == 'b' or selection == 'back' or selection == 'q' or selection == 'quit':
                exit_loop = True
                continue
            else:
                try:
                    i = int(selection)

                except ValueError:
                    i = -1

                if i < 0 and i >= len(secrets):
                    print('invalid input')
                    continue

                print('%s = %s' % (KV_Repl._get_secret_name_from_url(secrets[i].id), self._data_client.get_secret(secrets[i].id).value))

        return selection

    def _add_secret(self, vault_info):
        secret_name = input('\nSecret Name: ')
        secret_value = input('Secret Value: ')

        self._data_client.set_secret(vault_info.properties.vault_uri, secret_name, secret_value)

        print('\nSecret %s added to vault %s' % (secret_name, vault_info.name))

    @staticmethod
    def _get_secret_name_from_url(url):
        split = url.split('/')
        return split[len(split) - 1]

    def _get_vault_list(self):
        vault_list = [vault for vault in self._mgmt_client.vaults.list()]

        return vault_list

config = KV_Config()

config.from_disk()

repl = KV_Repl(config)

repl.start()

config.to_disk()

print('success!')



