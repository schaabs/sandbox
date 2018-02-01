from abc import ABCMeta, abstractmethod
from six import with_metaclass


_alg_registry = {}


class Algorithm(object):
    _name = None

    @classmethod
    def name(cls):
        return cls._name

    @classmethod
    def register(cls):
        _alg_registry[cls._name] = cls

    @staticmethod
    def resolve(name):
        return _alg_registry[name]()


class EncryptionAlgorithm(with_metaclass(ABCMeta, Algorithm)):

    @abstractmethod
    def create_encryptor(self, key):
        raise NotImplementedError()

    @abstractmethod
    def create_decryptor(self, key):
        raise NotImplementedError()


class SymmetricEncryptionAlgorithm(EncryptionAlgorithm):

    @abstractmethod
    def create_encryptor(self, key, iv):
        raise NotImplementedError()

    @abstractmethod
    def create_decryptor(self, key, iv):
        raise NotImplementedError()


class AuthenticatedSymmetricEncryptionAlgorithm(EncryptionAlgorithm):

    @abstractmethod
    def create_encryptor(self, key, iv, auth_data, auth_tag):
        raise NotImplementedError()

    @abstractmethod
    def create_decryptor(self, key, iv, auth_data, auth_tag):
        raise NotImplementedError()


class SignatureAlgorithm(Algorithm):

    @abstractmethod
    def create_signature_transform(self, key):
        raise NotImplementedError()
