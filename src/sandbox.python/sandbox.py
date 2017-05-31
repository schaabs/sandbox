import unittest


def build_vault_impl(vault_name, secret):
    print('creating vault: %s with secret %s'%(vault_name, secret))


def build_vault(f):
    test_friendly_name = f.__name__.replace('_', '-')
    vault_name = test_friendly_name + '-vault'
    def wrapper(self):
        build_vault_impl(vault_name, self.secret)
        f(self, vault_name)
    return wrapper


class Foo(unittest.TestCase):
    def setUp(self):
        self.secret = 'password1'

    @build_vault
    def test_1(self, vault_name=None):
        print('executing test1 with vault %s'%vault_name)


class Bar(unittest.TestCase):
    def setUp(self):
        self.secret = 'password2'

    @build_vault
    def test_2(self, vault_name=None):
        print('executing test2 with vault %s'%vault_name)

if __name__ == '__main__':
    unittest.main()
