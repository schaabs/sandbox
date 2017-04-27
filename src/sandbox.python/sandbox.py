class Foo(object):
    def __init__(self, message):
        self.message = message


class Bar(Foo):
    def __init__(self):
        self.ps = ' ttfn'

    def display_message(self):
        print('%s %s'%(self.message, self.ps))

if __name__ == '__main__':
    f = Foo("Hello World")
    f.__class__ = Bar
    f.display_message()
