from ..algorithm import Algorithm, SignatureAlgorithm
from ..transform import SignatureTransform
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.asymmetric import ec


class _EcdsaSignatureTransform(SignatureTransform):
    def __init__(self, key, hash_algo):
        self._key = key
        self._hash_algo = hash_algo

    def sign(self, data):
        return self._key.sign(data, ec.ECDSA(self._hash_algo))

    def verify(self, signature, data):
        return self._key.verify(signature, data, ec.ECDSA(self._hash_algo))

    def dispose(self):
        self._key = None
        self._hash_algo = None


class _Ecdsa(SignatureAlgorithm):
    _hash_algo=None

    def create_signature_transform(self, key):
        return _EcdsaSignatureTransform(key, self._hash_algo)


class Ecdsa256(_Ecdsa):
    _name = 'ECDSA256'
    _hash_algo=hashes.SHA256()


class Es256(_Ecdsa):
    _name = 'ES256'
    _hash_algo=hashes.SHA256()


class Es384(_Ecdsa):
    _name = 'ES384'
    _hash_algo=hashes.SHA384()


class Es512(_Ecdsa):
    _name = 'ES512'
    _hash_algo=hashes.SHA512()


Ecdsa256.register()
Es256.register()
Es384.register()
Es512.register()
