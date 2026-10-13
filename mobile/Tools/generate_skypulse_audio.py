#!/usr/bin/env python3
"""SkyPulse effects, authored with Codex on 2026-09-29.

Uses only mathematical oscillators and deterministic noise; no samples,
recordings, sound packs or external services. Python standard library only.
Run with --output DIR to generate, or --check DIR to verify existing output.
"""
import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import struct
import wave

RATE = 22050
# Keep the exact lengths of the previous resource clips.
FRAMES = {'flap': 2205, 'score': 3969, 'crystal': 2866, 'crash': 7056, 'unlock': 6394}
LEVELS = {'flap': .18, 'score': .25, 'crystal': .23, 'crash': .30, 'unlock': .26}


def envelope(t, duration, decay=8):
    attack = min(1.0, t / .004)
    release = min(1.0, max(0.0, (duration - t) / .018))
    return attack * release * math.exp(-decay * t)


def make_clip(name):
    count = FRAMES[name]
    duration = (count - 1) / RATE
    samples = []
    noise_state = 0x534B5950
    noise_smooth = 0.0
    for i in range(count):
        t = i / RATE
        if name == 'flap':
            # Compact rising wing-servo pulse, with a subdued second partial.
            phase = 2 * math.pi * (310*t + 1100*t*t)
            value = (math.sin(phase) + .18*math.sin(2*phase)) * envelope(t, duration, 20)
        elif name == 'score':
            # Clear two-note gate confirmation with an overlapping release.
            value = 0.0
            for start, frequency in [(0, 660), (.065, 990)]:
                u = t-start
                if u >= 0:
                    value += math.sin(2*math.pi*frequency*u) * envelope(u, duration-start, 20)
        elif name == 'crystal':
            # Short bright, slightly inharmonic crystal chime.
            value = (math.sin(2*math.pi*1320*t) + .32*math.sin(2*math.pi*2093*t)) * envelope(t, duration, 26)
        elif name == 'crash':
            # Falling mechanical thud with deterministic filtered noise.
            noise_state = (1664525*noise_state + 1013904223) & 0xffffffff
            noise = noise_state / 2147483648.0 - 1.0
            noise_smooth += .24*(noise-noise_smooth)
            phase = 2*math.pi*(160*t - 180*t*t)
            value = (.72*math.sin(phase) + .55*noise_smooth) * envelope(t, duration, 13)
        else:
            # Ascending three-tone unlock cue; no sampled melody.
            value = 0.0
            for start, frequency in [(0, 523.25), (.070, 659.25), (.140, 783.99)]:
                u = t-start
                if u >= 0:
                    value += (math.sin(2*math.pi*frequency*u) + .12*math.sin(4*math.pi*frequency*u)) * envelope(u, duration-start, 13)
        samples.append(value)
    peak = max(abs(value) for value in samples)
    pcm = [round(value * LEVELS[name] / peak * 32767) for value in samples]
    assert pcm[0] == pcm[-1] == 0
    assert max(abs(value) for value in pcm) < 32767
    buffer = io.BytesIO()
    with wave.open(buffer, 'wb') as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(struct.pack('<' + 'h'*count, *pcm))
    return buffer.getvalue()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--output', type=Path)
    mode.add_argument('--check', type=Path)
    args = parser.parse_args()
    folder = args.output or args.check
    if args.output:
        folder.mkdir(parents=True, exist_ok=True)
    manifest = {}
    for name, frames in FRAMES.items():
        data = make_clip(name)
        target = folder / (name + '.wav')
        if args.output:
            target.write_bytes(data)
        elif target.read_bytes() != data:
            raise SystemExit('Audio differs from generator: ' + str(target))
        manifest[name] = {'frames': frames, 'rate': RATE, 'channels': 1,
                          'duration_seconds': frames / RATE,
                          'sha256': hashlib.sha256(data).hexdigest()}
    print(json.dumps(manifest, indent=2))

if __name__ == '__main__':
    main()
