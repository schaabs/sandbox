import datetime
import sys
import argparse

def _parse_args():
    parser = argparse.ArgumentParser()

    parser.add_argument('--in', )

def main(argv):
    start_time = datetime.datetime.now();

    config = _parse_args(argv[1:])

    _init_output(config)

    cmdProc = _create_command_processor(config)

    cmdProc.Process(config)

    Output.Message('total elapsed time %s' % (datetime.datetime.now() - start_time))


if __name__ == '__main__':
    main(sys.argv)
