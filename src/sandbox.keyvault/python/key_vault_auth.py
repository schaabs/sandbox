import adal
from adal import token_cache
from azure.common.credentials import BasicTokenAuthentication

KEY_VAULT_RESOURCE = 'https://vault.azure.net'
AZURE_MANAGEMENT_RESOURCE2 = 'https://management.core.windows.net/'

class KeyVaultAuth(object):
    def __init__(self, config, client_id):
        self._config = config
        self._cache = token_cache.TokenCache()
        self._client_id = client_id

        if self._config.token_cache:
            self._cache.deserialize(self._config.token_cache)

        self._context = adal.AuthenticationContext(self._config.authority_url, cache=self._cache)


    def get_keyvault_creds(self):
        return self._get_creds(KEY_VAULT_RESOURCE)

    def get_arm_creds(self):
        return self._get_creds(AZURE_MANAGEMENT_RESOURCE2)

    def _get_auth_token_from_code(self, resource):
        code = self._context.acquire_user_code(resource, self._client_id)

        print(code['message'])

        token = self._context.acquire_token_with_device_code(resource, code, self._client_id)

        return token

    def _get_creds(self, resource):
        token = None

        if not self._config.user_id:
            token = self._get_auth_token_from_code(resource)
        else:
            token = self._context.acquire_token(resource, self._config.user_id, self._client_id)

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
