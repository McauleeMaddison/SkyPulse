#!/usr/bin/env python3
"""Author two small Signal Heist cues; preserve the five existing flight effects.
Deterministic oscillators/filtered noise only. No recordings, packs or services.
"""
import argparse, hashlib, json, math, struct, wave, io
from pathlib import Path
RATE=48000
def clip(name):
    duration=.115 if name=='prism' else .245
    values=[]; state=0x5349474E; filtered=0
    for i in range(round(duration*RATE)):
        t=i/RATE; state=(1664525*state+1013904223)&0xffffffff
        filtered+=.12*((state/2147483648-1)-filtered)
        release=min(1,(duration-t)/.018); attack=min(1,t/.003)
        if name=='prism':
            value=(math.sin(2*math.pi*740*t)*math.exp(-t*35)+.21*math.sin(2*math.pi*1817*t)*math.exp(-t*57)+filtered*.55)*math.exp(-t*8)
        else:
            # Restrained double sensor warning, distinct from a failed flight.
            value=0
            for start in [0,.105]:
                u=t-start
                if u>=0:value+=(math.sin(2*math.pi*(370*u-120*u*u))+.15*math.sin(2*math.pi*740*u))*min(1,u/.004)*math.exp(-u*24)
        values.append(value*attack*max(0,release))
    peak=max(map(abs,values)); values=[round(x/peak*.18*32767) for x in values]; values[0]=values[-1]=0
    output=io.BytesIO()
    with wave.open(output,'wb') as wav:
        wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(RATE);wav.writeframes(struct.pack('<'+'h'*len(values),*values))
    return output.getvalue()
if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);g=p.add_mutually_exclusive_group(required=True);g.add_argument('--output',type=Path);g.add_argument('--check',type=Path);a=p.parse_args();folder=a.output or a.check
    folder.mkdir(parents=True,exist_ok=True);report={}
    for name in ['prism','sensor']:
        data=clip(name);path=folder/(name+'.wav')
        if a.output:path.write_bytes(data)
        else:assert path.read_bytes()==data,'Cue differs: '+name
        report[name]={'sha256':hashlib.sha256(data).hexdigest(),'rate':RATE,'channels':1,'maximumPeak':.18}
    print(json.dumps(report,indent=2))
