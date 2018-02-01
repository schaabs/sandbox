from abc import ABCMeta, abstractmethod

class A(object):
    _val = ['a']

    @classmethod
    def val(cls):
        return cls._val

    def func(self, **kwargs):
        arg = kwargs.get('myarg', 'booyah')
        print(arg)
        kwargs['myarg'] = 'booyah'

    def __str__(self):
        return self._val

class B(A):
    _val = []
    _val.append('b')
    A.val().append('c')

    def func(self, **kwargs):
        super(B, self).func(**kwargs)

        print(kwargs.get('myarg', 'doh'))

def strapon(data, tag):
    tag.append('strapped')
    print(data)
    return data

if __name__ == "__main__":
    print(A.val())
    print(B.val())
    print(isinstance(B(), A))
    print(B().func(myarg='snizzle'))
