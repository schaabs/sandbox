import os
from Crypto.PublicKey import RSA
from haikunator import Haikunator

from azure.mgmt.keyvault import KeyVaultManagementClient
from azure.keyvault.generated import KeyVaultClient
from msrestazure.azure_active_directory import ServicePrincipalCredentials

from azure.mgmt.keyvault.models import AccessPolicyEntry, VaultProperties, Sku, KeyPermissions, SecretPermissions, \
    CertificatePermissions, Permissions, VaultCreateOrUpdateParameters


class KeyVaultSampleConfig(object):
    """
    Configuration settings for use in KeyVault sample code.  Users wishing to run this sample can either set these
    values as environment values or simply update the hard-coded values below
        
    :ivar subscription_id: Azure subscription id for the user intending to run the sample
    :vartype subscription_id: str
    
    :ivar client_id: Azure Active Directory Application Client ID to run the sample
    :vartype client_id: str
    
    :ivar client_oid: Azure Active Directory Application Client Object ID to run the sample
    :vartype client_oid: str
    
    :ivar tenant_id: Azure Active Directory tenant id of the user intending to run the sample
    :vartype tenant_id: str
    
    :ivar client_secret: Azure Active Directory Application Client Secret to run the sample
    :vartype client_secret: str
    :ivar location: Azure regional location on which to execute the sample 
    :vartype location: str
    :ivar group_name: Azure resource group on which to execute the sample 
    :vartype group_name: str
    """
    def __init__(self):
        # get credential information from the environment or replace the dummy values with your client credentials
        self.subscription_id = os.getenv('AZURE_SUBSCRIPTION_ID', '11111111-1111-1111-1111-111111111111')
        self.client_id = os.getenv('AZURE_CLIENT_ID', '22222222-2222-2222-2222-222222222222')
        self.client_oid = os.getenv('AZURE_CLIENT_0ID', '33333333-3333-3333-3333-333333333333')
        self.tenant_id = os.getenv('AZURE_TENANT_ID', '44444444-4444-4444-4444-444444444444')
        self.client_secret = os.getenv('AZURE_CLIENT_SECRET', 'zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz=')
        self.location = os.getenv('AZURE_LOCATION', 'westus')
        self.group_name = os.getenv('AZURE_RESOURCE_GROUP', 'azure-sample-group')


class KeyVaultSample(object):
    DEFAULT_LOCATION = 'westus'
    GROUP_NAME = 'azure-sample-group'

    def __init__(self):
        self.config = KeyVaultSampleConfig()
        self.credentials = None
        self.keyvault_data_client = None
        self.keyvault_mgmt_client = None

    def setup_sample(self):
        self.credentials = ServicePrincipalCredentials(self.config.client_id, self.config.client_secret)
        self.keyvault_mgmt_client = KeyVaultManagementClient(self.credentials, self.config.subscription_id)
        self.keyvault_data_client = KeyVaultClient(self.credentials)

    def create_vault(self):
        self.keyvault_mgmt_client.vaults.create_or_update()
        vault_name = KeyVaultSample.get_unique_name()

        permissions = Permissions()
        permissions.keys = [KeyPermissions.all]
        permissions.secrets = [SecretPermissions.all]
        permissions.certificates = [CertificatePermissions.all]

        policy = AccessPolicyEntry(self.config.tenant_id, self.config.client_oid, permissions)

        properties = VaultProperties(self.config.tenant_id, Sku(name='standard'), policies=[policy])

        parameters = VaultCreateOrUpdateParameters(self.config.location, properties)
        parameters.properties.enabled_for_deployment = True
        parameters.properties.enabled_for_disk_encryption = True
        parameters.properties.enabled_for_template_deployment = True

        vault = self.keyvault_mgmt_client.vaults.create_or_update(self.config.group_namne, vault_name, parameters)

        return vault

    @staticmethod
    def get_unique_name():
        return Haikunator().haikunate(delimiter='-')


class BackupRestoreSample(KeyVaultSample):
    def __init__(self):
        super(KeyVaultSampleBase)

    def run_sample(self):
        self.setup_sample()

    def backup_restore_secret_sample(self):
        """
        Creates a key vault containing a secret, then uses backup_secret and restore_secret to 
        import the secret to another key vault 
        :return: None
        """
        # create a key vault
        first_vault = self.create_vault()

        # add a secret to the vault
        secret_name = KeyVaultSample.get_unique_name()
        secret_value = 'this is a secret value to be migrated from one vault to another'

        secret = self.keyvault_data_client.set_secret(first_vault.properties.vault_uri, secret_name, secret_value)

        print(secret)

        # backup the secret
        backup = self.keyvault_data_client.backup_secret(first_vault.properties.vault_uri, secret_name)

        print(backup)

        # create a second vault
        second_vault = self.create_vault()

        # restore the secret to the new vault
        self.keyvault_data_client.restore_secret(second_vault.properties.vault_uri, backup.value)

        # get the secret from the new vault
        restored_secret = self.keyvault_data_client.get_secret(second_vault.properties.vault_uri, secret_name)

        print(restored_secret)

    def backup_restore_key_sample(self):
        """
        Creates a key vault containing a key, then uses backup_key and restore_key to 
        import the key with matching versions to another key vault 
        :return: None
        """
        # create a key vault
        first_vault = self.create_vault()

        # create a key in the vault
        key_name = KeyVaultSample.get_unique_name()

        key = self.keyvault_data_client.create_key(first_vault.properties.vault_uri, key_name, 'RSA')

        print(key)

        self.keyvault_data_client.import_key(first_vault.properties.vault_uri, key_name, )

        # backup the key
        backup = self.keyvault_data_client.backup_key(first_vault.properties.vault_uri, key_name)

        print(backup)

        # create a second vault
        second_vault = self.create_vault()

        # restore the key to the new vault
        self.keyvault_data_client.restore_key(second_vault.properties.vault_uri, backup.value)

        # get the secret from the new vault
        restored_key = self.keyvault_data_client.get_key(second_vault.properties.vault_uri, key_name,)

        print(restored_key)


