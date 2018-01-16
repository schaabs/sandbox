from azure.keyvault import KeyVaultClient, KeyVaultAuthentication
from azure.keyvault.custom.http_message_security import generate_pop_key
from azure.keyvault.custom.key_vault_authentication import AccessToken
import requests
import json
import os
import keyring
import time
import codecs
import logging
import sys

try:
    import urllib.parse as urlutil
except:
    import urllib as urlutil


def _get_client_id():
    return os.getenv('AZURE_CLIENT_ID', '22222222-2222-2222-2222-222222222222')


def _get_client_secret():
    return os.getenv('AZURE_CLIENT_SECRET', 'zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz')


def _get_vault_url():
    return os.getenv('AZURE_VAULT_URL', 'https://myvault.vault.azure.net')


_token_cache = {}

def _random_name(prefix=None):
    prefix = prefix if prefix else ''
    # append a random short integer onto the prefix
    return prefix + '-' + str(int(codecs.encode(os.urandom(2), 'hex'), 16))

def _token_expired(token):
    expires_on = float(token.get('expires_on', '0'))
    return expires_on <= time.time()


def _authenticate(server, resource, scope, scheme):

    # use the authority, resource, scope and scheme as a key to the token cache
    token_cache_key = '_'.join([server, resource, scope, scheme]).lower()

    # check if we have a cached token response from AAD
    token = _token_cache.get(token_cache_key, None)

    # if no cached response exists or the cached token is expired for the auth request
    # request a token from the specified authority (server)
    if (not token) or (_token_expired(token)):
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
        data = urlutil.urlencode(body)
        response = requests.request('POST', server + '/oauth2/token', data=data)

        # raise error for non success status codes
        response.raise_for_status()
        token = response.json()

        # if a pop key was used to create the token add it to the cached token so it can be
        # used to sign messages in this session
        if pop_key:
            token['pop_key'] = pop_key

        # cache the response
        _token_cache[token_cache_key] = token

    # return a new AccessToken with the scheme, AAD access_token, and POP key
    return AccessToken(scheme, token['access_token'], token.get('pop_key', None))


if __name__ == "__main__":
    # create the key vault client with our PoP enabled AAD callback
    client = KeyVaultClient(KeyVaultAuthentication(_authenticate))

    logging.basicConfig(level=logging.DEBUG)
    # create a key to perform server side crypto operations
    key_name = _random_name('key')
    client.create_key(_get_vault_url(), key_name, 'RSA')
    print('Created key vault key: ' + key_name)

    # create a random AES key to wrap
    to_wrap = os.urandom(16)
    print('\noriginal value:')
    print(to_wrap)

    # wrap the key
    wrapped = client.wrap_key(vault_base_url=_get_vault_url(),
                              key_name=key_name,
                              key_version='',
                              algorithm='RSA-OAEP',
                              value=to_wrap)
    print('\nwrap_key result:')
    print(wrapped.result)

    unwrapped = client.unwrap_key(vault_base_url=_get_vault_url(),
                                 key_name=key_name,
                                 key_version='',
                                 algorithm='RSA-OAEP',
                                 value=wrapped.result)
    print('\nunwrap result:')
    print(unwrapped.result)

    # delete the key
    client.delete_key(_get_vault_url(), key_name)

