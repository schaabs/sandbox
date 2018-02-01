import random
import string
import time


def rand_str(min_len=None, max_len=None, length=None):

    if not length:
        min_len = min_len or min([8, max_len or 8])
        max_len = max_len or max([16, min_len or 16])
        length = random.randrange(min_len, max_len + 1)

    return ''.join(random.choice(string.printable) for _ in range(length))

def poll(func, args=(), retry_wait=None, max_retries=None, retry_on_exception=True):
    retry_count = 0
    ret = None
    while not ret and (not max_retries or retry_count < max_retries):
        try:
            ret = func(*args[0:])
        except Exception as e:
            if not retry_on_exception:
                raise e

        if not ret and retry_wait:
            time.sleep(retry_wait)

    return ret
