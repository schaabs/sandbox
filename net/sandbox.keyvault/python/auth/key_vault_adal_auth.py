from msrest.authentication import Authentication
from adal import AuthenticationContext

_KEY_VAULT_RESOURCE = 'https://vault.azure.net'
_TOKEN_ENTRY_TOKEN_TYPE = 'tokenType'
_ACCESS_TOKEN = 'accessToken'

class KeyVaultUserPasswordAuth(Authentication):

    def __init__(self, context, user, password, client_id):
        self.auth = self._get_token
        self.context = context
        self.user = user
        self.password = password
        self.client_id = client_id

    def _get_token(self):
        token_entry = self.context.acquire_token_with_username_password(_KEY_VAULT_RESOURCE, self.user, self.password, self.client_id)
        return token_entry[_TOKEN_ENTRY_TOKEN_TYPE], token_entry[_ACCESS_TOKEN]