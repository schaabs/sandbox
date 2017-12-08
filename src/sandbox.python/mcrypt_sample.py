from azure.keyvault import KeyVaultClient, KeyVaultAuthentication
from azure.keyvault.custom.http_message_security import generate_pop_key
from azure.keyvault.custom.key_vault_authentication import AccessToken
import requests
import json
import os
try:
    import urllib.parse as urlutil
except:
    import urllib as urlutil

_token_cache = { }
_url_encode_func = None

def _get_client_id():
    return os.getenv('AZURE_CLIENT_ID', '22222222-2222-2222-2222-222222222222')


def _get_client_secret():
    return os.getenv('AZURE_CLIENT_SECRET', 'zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz')

def _get_vault_url():
	return  os.getenv('AZURE_VAULT_URL', 'https://myvault.vault.azure.net')

def _authenticate(server, resource, scope, scheme):
    token_cache_key = server + resource + scope + scheme
    access_token = _token_cache.get(token_cache_key, None)

    if not access_token:
        # if the scheme is pop we need to generate a proof of possession key
        pop_key = generate_pop_key() if scheme.lower() == 'pop' else None

        # create the body for the AAD auth request
        body = {
            'resource': resource,
            'response_type': 'token',
            'grant_type': 'client_credentials',
            'client_id': _get_client_id(),
            'client_secret': _get_client_secret()
        }
        if pop_key:
            body['pop_jwk'] = json.dumps(pop_key.to_jwk().serialize())

        # url encode the body and post the requst
        data =urlutil.urlencode(body)
        response = requests.request('POST', server + '/oauth2/token', data=data)

        # raise error for non success status codes
        response.raise_for_status()
        token_dict = response.json()

        # create the access token
        access_token = AccessToken(scheme, token_dict['access_token'], pop_key)
        _token_cache[token_cache_key] = access_token
    return access_token


if __name__ == "__main__":
    # create the key vault client with our PoP enabled AAD callback
    client = KeyVaultClient(KeyVaultAuthentication(_authenticate))

    # create a random AES key to wrap
    to_wrap = os.urandom(16)

    print('\noriginal value:')
    print(to_wrap)
	
    # wrap the key
    wrapped = client.wrap_key(vault_base_url=_get_vault_url(),
                              key_name='key1',
                              key_version='',
                              algorithm='RSA-OAEP',
                              value=to_wrap)

    print('\nwrap result:')
    print(wrapped.result)
	
    unwrapped = client.unwrap_key(vault_base_url=_get_vault_url(),
                                 key_name='key1',
                                 key_version='',
                                 algorithm='RSA-OAEP',
                                 value=wrapped.result)
	
    print('\nunwrap result:')
    print(unwrapped.result)
	