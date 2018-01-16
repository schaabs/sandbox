from .resolver import register_resolvable_object


class Foo(object):
    def __str__(self):
        return 'Foo'


register_resolvable_object('foo', Foo)
