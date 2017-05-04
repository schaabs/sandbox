class Foo(object):
    def __init__(self):
        pass

    def greet(self, person, adj, **opargs):
        print('Hello %s, %s' % (person, adj))

    def greet(self, person, **opargs):
        print('Hello %s' % person)

if __name__ == '__main__':
    f = Foo()
    f.greet('Double D')
    f.greet('Double D', 'asshole')
