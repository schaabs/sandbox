import random
import string


def rand_str(min_len=None, max_len=None, length=None):

    if not length:
        min_len = min_len or min([8, max_len or 8])
        max_len = max_len or max([16, min_len or 16])
        length = random.randrange(min_len, max_len + 1)

    return ''.join(random.choice(string.printable) for _ in range(length))
