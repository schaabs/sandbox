
_dict = {}


def register_resolvable_object(name, type):
    _dict[name] = type


def resolve_to_instance(name):
    return _dict[name]()
