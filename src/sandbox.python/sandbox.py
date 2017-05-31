import inspect


def build_vault_impl(vault_name, secret):
    print('creating vault: %s with secret %s'%(vault_name, secret))

def build_vault(f):
    vault_name = f.__name__

    def wrapper(self):
        build_vault_impl(vault_name, self.secret)
        f(self, vault_name)
    return wrapper


class Foo(object):
    def __init__(self, secret):
        self.secret = secret

    @build_vault
    def test1(self, vault_name=None):
        print('executing test1 with vault %s'%vault_name);

class Bar(Foo):
    @build_vault
    def test2(self, vault_name=None):
        print('executing test2 with vault %s'%vault_name);

if __name__ == '__main__':
    f = Foo('password')
    b = Bar('password')
    f.test1()
    b.test2()
