#!/usr/bin/env python3
"""Generate or verify Signal Heist's optical detent and sensor warning.
Shares SkyPulse's current deterministic synthesis and mix palette.
No recordings, packs, services or runtime effect dependency.
"""
import argparse
import json
from pathlib import Path
from generate_skypulse_audio import generate, make_clip


def clip(name):
    return make_clip(name)


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);mode=parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--output',type=Path);mode.add_argument('--check',type=Path);args=parser.parse_args()
    print(json.dumps(generate(args.output or args.check,['prism','sensor'],write=bool(args.output)),indent=2))
