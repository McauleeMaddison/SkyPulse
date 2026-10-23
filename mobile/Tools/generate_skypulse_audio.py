#!/usr/bin/env python3
"""SkyPulse's 23 October 2026 sound palette, authored with Codex assistance.

Deterministic additive/FM oscillators, filtered noise and short early reflections.
No recordings, sample packs, external services or third-party audio libraries.
The existing resource names and cue durations remain compatible with the game.
"""
import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import struct
import wave

RATE = 48000
LEGACY_FRAMES = {'flap': 2205, 'score': 3969, 'crystal': 2866, 'crash': 7056, 'unlock': 6394}
FRAMES = {name: round(count / 22050 * RATE) for name, count in LEGACY_FRAMES.items()}
CUE_FRAMES = {**FRAMES, 'prism': 5520, 'sensor': 11760}
LEVELS = {'flap': .15, 'score': .20, 'crystal': .19, 'crash': .26, 'unlock': .22, 'prism': .16, 'sensor': .16}
TAU = 2 * math.pi


def smooth(value):
    value = min(1., max(0., value))
    return value * value * (3 - 2 * value)


def envelope(t, duration, decay=12., attack=.003):
    if t < 0 or t >= duration:
        return 0.
    return smooth(t / attack) * smooth((duration - t) / .016) * math.exp(-decay * t)


def bell(t, frequency, duration, decay=24., brightness=.32):
    if t < 0:
        return 0.
    # A soft glass attack relaxes into a rounded fundamental. The upper partial
    # decays faster, so repeated pickups don't become a piercing sine beep.
    modulation = brightness * math.exp(-t * 72) * math.sin(TAU * frequency * 2.01 * t)
    return (math.sin(TAU * frequency * t + modulation)
            + .19 * math.sin(TAU * frequency * 2.003 * t) * math.exp(-t * 40)
            + .065 * math.sin(TAU * frequency * 3.017 * t) * math.exp(-t * 63)) * envelope(t, duration, decay)


def make_clip(name):
    count = CUE_FRAMES[name]
    duration = (count - 1) / RATE
    values = []
    state = 0x534B5950 + list(CUE_FRAMES).index(name) * 7919
    air = low = 0.
    for i in range(count):
        t = i / RATE
        state = (1664525 * state + 1013904223) & 0xffffffff
        white = state / 2147483648. - 1.
        air += .44 * (white - air)
        low += .065 * (white - low)
        band = air - low
        if name == 'flap':
            # Airy wing/servo chuff, with a low body instead of a chiptune beep.
            phase = TAU * (185*t - 430*t*t)
            body = math.sin(phase + .18 * math.sin(TAU * 510*t)) * math.exp(-t*26)
            servo = .10 * math.sin(TAU*(1280*t - 4200*t*t)) * math.exp(-t*66)
            value = (body*.55 + band*.72*math.exp(-t*23) + servo) * envelope(t,duration,9, .002)
        elif name == 'score':
            value = .82*bell(t,659.255,duration,24,.20) + .67*bell(t-.055,987.767,duration-.055,28,.18)
        elif name == 'crystal':
            value = bell(t,1046.502,duration,30,.48) + .23*bell(t-.012,1567.982,duration-.012,39,.16)
            value += band*.11*envelope(t,duration,105,.0015)
        elif name == 'crash':
            # A damped metal/body impact with a soft tail, not a harsh burst.
            body = math.sin(TAU*(112*t-112*t*t))*.66
            metal = .19*math.sin(TAU*247*t)*math.exp(-t*23) + .11*math.sin(TAU*381*t)*math.exp(-t*32)
            value = (body + metal + low*.65 + band*.19*math.exp(-t*18)) * envelope(t,duration,14,.002)
        elif name == 'unlock':
            value = sum(gain*bell(t-start,f,duration-start,15,.25)
                        for start,f,gain in [(0,523.251,.62),(.056,659.255,.65),(.112,783.991,.72)])
            value += .11*math.sin(TAU*130.813*t)*envelope(t,duration,13,.008)
        elif name == 'prism':
            # Tactile optical detent: a tiny metal click over a rounded core.
            value = .65*bell(t,739.989,duration,42,.44)
            value += (.21*math.sin(TAU*329.628*t)+band*.24)*envelope(t,duration,48,.0015)
        else:
            # Quiet descending double warning, distinct from the impact cue.
            value = 0.
            for start,f in [(0,349.228),(.103,293.665)]:
                u=t-start
                if u>=0:
                    value += (math.sin(TAU*(f*u-70*u*u))+.10*math.sin(TAU*f*2*u))*envelope(u,duration-start,25,.004)
        values.append(math.tanh(value*.75)/.75)
    # Small, damped early reflections add material depth without a long reverb
    # tail, additional AudioSources or a runtime effect dependency.
    dry = values[:]
    wet = .035 if name in ('flap','crash','sensor') else .09
    for i in range(count):
        for seconds,gain in [(.011,wet),(.023,wet*.40)]:
            at=i-round(seconds*RATE)
            if at>=0: values[i] += dry[at]*gain
        values[i] *= smooth((count-1-i)/(RATE*.012))
    window = [math.sin(math.pi*i/(count-1))**2 for i in range(count)]
    dc = sum(values)/sum(window)
    values = [x-dc*w for x,w in zip(values,window)]
    peak = max(map(abs,values))
    pcm = [round(x/peak*LEVELS[name]*32767) for x in values]
    pcm[0]=pcm[-1]=0
    assert max(map(abs,pcm))<32767
    result=io.BytesIO()
    with wave.open(result,'wb') as wav:
        wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(RATE)
        wav.writeframes(struct.pack('<'+'h'*count,*pcm))
    return result.getvalue()


def generate(folder, names, write=False):
    folder=Path(folder)
    if write:folder.mkdir(parents=True,exist_ok=True)
    report={}
    for name in names:
        data=make_clip(name);path=folder/(name+'.wav')
        if write:path.write_bytes(data)
        elif path.read_bytes()!=data:raise SystemExit('Audio differs from generator: '+str(path))
        report[name]={'frames':CUE_FRAMES[name],'rate':RATE,'channels':1,'duration_seconds':CUE_FRAMES[name]/RATE,
                      'maximumPeak':LEVELS[name],'sha256':hashlib.sha256(data).hexdigest()}
    return report


def main():
    parser=argparse.ArgumentParser(description=__doc__);mode=parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--output',type=Path);mode.add_argument('--check',type=Path);args=parser.parse_args()
    print(json.dumps(generate(args.output or args.check,FRAMES,write=bool(args.output)),indent=2))


if __name__=='__main__':main()
