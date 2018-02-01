from .resolver import register_resolvable_object


class Bar(object):
    def __str__(self):
        return 'Bar'


register_resolvable_object('bar', Bar)
