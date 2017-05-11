from ..base import Foo as FooBase

class Foo(FooBase):
    def print_greeting(self):
        print(self.greeting('foo and improved'))

