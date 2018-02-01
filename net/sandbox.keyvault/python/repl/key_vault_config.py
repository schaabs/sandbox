import os
import json

CONFIG_PATH = 'keyvault.cfg'


class KeyVaultConfig(dict):
    def __init__(self):

        self.authority_url = ''
        self.subscription_id = ''
        self.tenant_id = ''
        self.token_cache = ''
        self.user_id = ''
        self.resource_group = ''
        self.user_oid = ''
        self.location = ''

    def __getattr__(self, item):
        return self[item]

    def __setattr__(self, key, value):
        self[key] = value

    def from_disk(self):
        if os.path.isfile(CONFIG_PATH):
            with open(CONFIG_PATH, 'r') as configFile:
                try:
                    deserialized = json.load(configFile)
                except json.JSONDecodeError:
                    print('error loading config file')
                    return
                for key, value in deserialized.items():
                    if value:
                        self[key] = value

    def to_disk(self):
        with open(CONFIG_PATH, 'w') as configFile:
            json.dump(self, configFile, sort_keys=True, indent=4, separators=(',', ': '))
