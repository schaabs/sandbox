import random
import collections

SIZE = 6
Point = collections.namedtuple('Point', 'x, y')
mySequence = [(1,1),(4,1),(1,4),(4,4),(2,1),(4,2),(3,4),(1,3),(3,1),(4,3),(2,4),(1,2),(0,0),(5,0),(5,5),(0,5)]

class P(object):
    x = 0
    y = 0

    def __init__(self, x, y):
        self.x = x
        self.y = y
        self.left = P._create_point(x - 1, y)
        self.right = P._create_point(x + 1, y)
        self.up = P._create_point(x, y + 1)
        self.down = P._create_point(x, y - 1)

    @staticmethod
    def _create_point(x, y):
        return P(x,y) if P._is_valid(x, y) else None

    @staticmethod
    def _is_valid(x, y):
        return x >= 0 and x < SIZE and y >= 0 and y < SIZE



def create_mound(p):
    mound = [[0 for i in range(SIZE)] for j in range(SIZE)]

    gem = Point(random.randint(0, 5), random.randint(0, 5))

    mound[gem.x][gem.y] = 2

    if gem.x - 1 >= 0:
        mound[gem.x - 1][gem.y] = 1
    if gem.x + 1 < SIZE:
        mound[gem.x + 1][gem.y] = 1
    if gem.y - 1 >= 0:
        mound[gem.x][gem.y - 1] = 1
    if gem.y + 1 < SIZE:
        mound[gem.x][gem.y + 1] = 1

    return mound


def search_mound(mound, sequence):
    turns = 0
    for p in [Point(p[0], p[1]) for p in sequence]:
        turns += 1
        if mound[p.x][p.y] == 2:
            break
        elif mound[p.x][p.y] == 1:

        else:
            mound[p.x][p.y] = -1

def check_point(mound, p):
    turns = 0;
    if p.x - 1 >= 0:
        mound[gem.x - 1][gem.y] = 1
    if gem.x + 1 < SIZE:
        mound[gem.x + 1][gem.y] = 1
    if gem.y - 1 >= 0:
        mound[gem.x][gem.y - 1] = 1
    if gem.y + 1 < SIZE:
        mound[gem.x][gem.y + 1] = 1
