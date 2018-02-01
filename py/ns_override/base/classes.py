
class Base(object):
    def greeting(self, name):
        return 'hello i am {}'.format(name)


class Foo(Base):
    def print_greeting(self):
        print(self.greeting('foo'))


class Bar(Base):
    def print_greeting(self):
        print(self.greeting('bar'))
