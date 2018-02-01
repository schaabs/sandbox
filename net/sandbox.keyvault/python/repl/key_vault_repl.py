#!/usr/local/bin/python
from azure.mgmt.keyvault.models import Sku
from azure.mgmt.keyvault.models import VaultCreateOrUpdateParameters, VaultProperties, SkuName, AccessPolicyEntry, \
    Permissions, KeyPermissions, SecretPermissions, CertificatePermissions
from azure.keyvault import KeyVaultClient, KeyVaultAuthentication
from azure.keyvault.models import JsonWebKeyType
from azure.mgmt.keyvault import KeyVaultManagementClient
import json
import os
import sys

from key_vault_config import KeyVaultConfig
from key_vault_auth import KeyVaultAuth

CLIENT_ID = '8fd4d3c4-efea-49aa-b1de-2c33c22da56e' # Azure cli
CLIENT_OID = '8694d835-b4e2-419a-a315-b13c854166e2'
CLIENT_TENANT_ID = 'a7fc734e-9961-43ce-b4de-21b8b38403ba'


def _json_format(obj):
    return json.dumps(obj, sort_keys=True, indent=4, separators=(',', ': '))


class KV_Repl(object):

    _repl_break_commands = set(('back', 'b'))

    _repl_quit_commands = set(('quit', 'q'))

    def __init__(self, config):
        self._auth = KeyVaultAuth(config, CLIENT_ID)
        self._config = config
        self._mgmt_client = KeyVaultManagementClient(self._auth.get_arm_creds(), config.subscription_id)
        self._data_client = KeyVaultClient(self._auth.get_keyvault_creds())
        self._selected_vault = None
        self._current_index = None

    def start(self):
        try:
            self._vault_index_loop();

        except SystemExit:
            print('\nuser exited\n')

    def _continue_repl(self, display_action, break_commands=()):
        display_action()

        self._selection = input('> ').lower()

        if self._selection in break_commands:
            return None

        elif self._selection in KV_Repl._repl_quit_commands:
            sys.exit()

        try:
            self._selection = int(self._selection)
        except ValueError:
            pass

        return self._selection


    def _display_vault_index(self):

        print('\nAvailable Vaults:\n')

        self._current_index = self._get_vault_list()

        for idx, vault in enumerate(self._current_index):
            print('%d. %s' % (idx, vault.name))

        print('\n#:select | (a)dd | (d)elete | (q)uit')

    def _vault_index_loop(self):
        while self._continue_repl(self._display_vault_index) is not None:
            vaults = self._current_index

            if isinstance(self._selection, int):
                i = self._selection

                if i >= 0 and i < len(vaults):
                    self._selected_vault = self._mgmt_client.vaults.get(self._config.resource_group, vaults[i].name)
                    self._vault_detail_loop()
                else:
                    print('invalid vault index')

            elif self._selection == 'a' or self._selection == 'add':
                self._add_vault()

            else:
                print('invalid input')

    def _add_vault(self):
        name = input('\nenter vault name:')

        all_perms = Permissions()
        all_perms.keys = [KeyPermissions.all]
        all_perms.secrets = [SecretPermissions.all]
        all_perms.certificates = [CertificatePermissions.all]

        user_policy = AccessPolicyEntry(self._config.tenant_id, self._config.user_oid, all_perms)

        app_policy = AccessPolicyEntry(CLIENT_TENANT_ID, CLIENT_OID, all_perms)

        access_policies = [user_policy, app_policy]

        properties = VaultProperties(self._config.tenant_id, Sku(name='standard'), access_policies)

        properties.enabled_for_deployment = True
        properties.enabled_for_disk_encryption = True
        properties.enabled_for_template_deployment = True

        vault = VaultCreateOrUpdateParameters(self._config.location, properties)

        self._mgmt_client.vaults.create_or_update(self._config.resource_group, name, vault)

        print('vault %s created\n' % name)

    def _display_selected_vault_detail(self):
        print('\nName:\t%s' % self._selected_vault.name)
        print('Uri:\t%s' % self._selected_vault.properties.vault_uri)
        print('Id:\t%s' % self._selected_vault.id)

        print('\n(s)ecrets | (k)eys | (c)ertificates | (e)ncrypt | (d)ecrypt | (b)ack | (q)uit\n')

    def _vault_detail_loop(self):

        while self._continue_repl(self._display_selected_vault_detail, break_commands=KV_Repl._repl_break_commands) is not None:

            if self._selection == 's' or self._selection == 'secrets':
                self._secret_index_loop()

            elif self._selection == 'k' or self._selection == 'keys':
                self._key_index_loop()

            elif self._selection == 'c' or self._selection == 'certificates':
                print('\nnot yet implemented\n')

            elif self._selection == 'e' or self._selection == 'encrypt':
                self._encrypt_file()
            else:
                print('invalid input')

    def _encrypt_file(self):
        while True:
            inpath = input('input file: ')

            if os.path.isfile(inpath):
                break
            else:
                print('error: file not found')

        while True:
            outpath = input('output file: ')

    @staticmethod
    def _prompt_for_file_path(prompt, verify_exists):
        inpath = input(prompt)

    def _display_secret_index(self):
        self._current_index = []

        secret_iter = self._data_client.get_secrets(self._selected_vault.properties.vault_uri)

        if secret_iter is not None:
            try:
                self._current_index = [secret for secret in secret_iter]
            except TypeError:
                pass

        print('\n%s Secrets:\n' % self._selected_vault.name)

        for idx, s in enumerate(self._current_index):
            print('%d. %s' % (idx, KV_Repl._get_name_from_url(s.id)))

        print('\n#:show secret value (a)dd (d)elete (b)ack (q)uit\n')

    def _secret_index_loop(self):

        while self._continue_repl(self._display_secret_index, break_commands=KV_Repl._repl_break_commands) is not None:

            secrets = self._current_index

            if isinstance(self._selection, int):
                i = self._selection

                if i >= 0 and i < len(secrets):
                    print('\n%s = %s\n' % (KV_Repl._get_secret_name_from_url(secrets[i].id), self._data_client.get_secret(secrets[i].id).value))
                else:
                    print('invalid secret index')

            elif self._selection == 'a' or self._selection == 'add':
                self._add_secret()

            elif self._selection == 'd' or self._selection == 'delete':
                print('\nnot yet implemented\n')

    def _add_secret(self):
        secret_name = input('\nSecret Name: ')
        secret_value = input('Secret Value: ')
        self._data_client.set_secret(self._selected_vault.properties.vault_uri, secret_name, secret_value)
        print('\nSecret %s added to vault %s' % (secret_name, self._selected_vault.name))

    def _display_key_index(self):
        self._current_index = []

        key_iter = self._data_client.get_keys(self._selected_vault.properties.vault_uri)

        if key_iter is not None:
            try:
                self._current_index = [secret for secret in key_iter]
            except TypeError:
                print('warning: caught TypeError')
                pass

        print('\n%s Keys:\n' % self._selected_vault.name)

        for idx, k in enumerate(self._current_index):
            print('%d. %s' % (idx, KV_Repl._get_name_from_url(k.kid)))

        print('\n#:get key | (a)dd | (i)mport | (d)elete | (b)ack | (q)uit\n')

    def _key_index_loop(self):

        while self._continue_repl(self._display_key_index, break_commands=KV_Repl._repl_break_commands) is not None:

            keys = self._current_index

            if isinstance(self._selection, int):
                i = self._selection

                if i >= 0 and i < len(keys):
                    print('\n%s = %s\n' % (KV_Repl._get_secret_name_from_url(keys[i].id), self._data_client.get_secret(keys[i].id).value))
                else:
                    print('invalid key index')

            elif self._selection == 'a' or self._selection == 'add':
                self._add_key()

            elif self._selection == 'd' or self._selection == 'delete':
                print('\nnot yet implemented\n')

    def _add_key(self):
        key_name = input('\nKey Name: ')

        self._data_client.create_key(self._selected_vault.properties.vault_uri, key_name, kty=JsonWebKeyType.rsa.value)
        print('\nSecret %s added to vault %s' % (key_name, self._selected_vault.name))

    @staticmethod
    def _get_name_from_url(url):
        split = url.split('/')
        return split[len(split) - 1]

    def _get_vault_list(self):
        vault_list = [vault for vault in self._mgmt_client.vaults.list()]
        return vault_list

config = KeyVaultConfig()

config.from_disk()

repl = KV_Repl(config)

repl.start()

config.to_disk()




