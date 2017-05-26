from azure.keyvault import KeyVaultClient, KeyVaultAuthentication
from azure.mgmt.keyvault import KeyVaultManagementClient
from key_vault_auth import KeyVaultAuth
from key_vault_config import KeyVaultConfig


class KeyVaultAgent(object):

    def __init__(self, client_id):
        self._initialize(client_id)

    def _initialize(self, client_id, config=None):
        self.config = config or KeyVaultConfig()
        self.config.from_disk()

        self.auth = KeyVaultAuth(self.config, client_id)
        self.mgmt_client = KeyVaultManagementClient(self.auth.get_arm_creds(), self.config.subscription_id)
        self.data_client = KeyVaultClient(KeyVaultAuthentication(self.auth.get_keyvault_creds))

    _attribute_map = {
        'config': {'key': 'config', 'type': 'KeyVaultConfig'},
        'auth': {'key': 'auth', 'type': 'KeyVaultAuth'},
        'mgmt_client': {'key': 'mgmt_client', 'type': 'KeyVaultManagementClient'},
        'data_client': {'key': 'data_client', 'type': 'KeyVaultClient'}
    }

    def get_vault(self, vault_name):
        return self.mgmt_client.vaults.get(self.config.resource_group, vault_name)

    def create_vault(self):
        pass
