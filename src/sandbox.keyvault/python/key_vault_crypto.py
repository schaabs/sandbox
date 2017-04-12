import base64
import datetime
import sys
import argparse

from azure.keyvault.generated.models import KeyVaultErrorException

from python.key_vault_agent import KeyVaultAgent
from azure.keyvault.generated import KeyVaultClient

CLIENT_ID = '8fd4d3c4-efea-49aa-b1de-2c33c22da56e'


class KeyVaultCryptoAgent(KeyVaultAgent):
    def __init__(self, client_id):
        self._initialize(client_id)

    def encrypt(self, f_in, f_out, vault_name, key_name, key_version=None):
        vault = self.get_vault(vault_name)

        buff = f_in.read()
        buff = base64.encodebytes(buff)
        buff = buff.replace(b'\n', b'')

        try:
            buff = self.data_client.encrypt(vault.properties.vault_uri, key_name, key_version or '', 'RSA1_5', buff)
        except KeyVaultErrorException as e:
            print(str(e))

        buff = base64.decodebytes(buff)
        f_out.write(buff)


def _parse_args(argv):
    parser = argparse.ArgumentParser()

    parser.add_argument('action', choices=['encrypt', 'decrypt'], help='specifies whether to encrypt or decrypt the specified "in" file')
    parser.add_argument('infile', type=argparse.FileType('rb'), help='specifies the file on which to preform the crypto action')
    parser.add_argument('outfile', type=argparse.FileType('wb'), help='specifies the file in which to store the crypto action result')
    parser.add_argument('vault', help='the key to use for the crypto action')
    parser.add_argument('key', help='the key to use for the crypto action')

    return parser.parse_args(argv)


def main(argv):
    argv = ['', 'encrypt', 'd:\\temp\\crypto_encrypt_in.txt', 'd:\\temp\\crypto_encrypt_out.txt', 'sdschaab-replkv', 'repl-key1']
    args = _parse_args(argv[1:])

    crypto_agent = KeyVaultCryptoAgent(CLIENT_ID)

    if args.action == 'encrypt':
        crypto_agent.encrypt(args.infile, args.outfile, args.vault, args.key)


if __name__ == '__main__':
    main(sys.argv)
