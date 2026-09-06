"""Measure pose alpha bounds without changing artwork; requires Pillow."""
from pathlib import Path
import hashlib, json, re
from PIL import Image
mobile=Path(__file__).resolve().parents[1]
resources=mobile/'Assets/Resources'
source=(mobile/'Assets/Scripts/SkyPulseNativeGame.cs').read_text()
paths=sorted(set(re.findall(r'"(SkyPulse/characters/roster/[^"\n]+-frame-0[78](?:-v1)?)"',source)))
assert len(paths)==30
frames=[]
for path in paths:
 p=resources/(path+'.png');im=Image.open(p).convert('RGBA')
 # Keep all nonzero alpha including faint effects; remove empty canvas only.
 bounds=im.getchannel('A').getbbox();assert bounds,p
 x0,y0,x1,y1=bounds
 x0=max(0,x0-2);y0=max(0,y0-2);x1=min(im.width,x1+2);y1=min(im.height,y1+2)
 frames.append(dict(path=path,x=x0,y=im.height-y1,width=x1-x0,height=y1-y0,sourceWidth=im.width,sourceHeight=im.height,sourceSha256=hashlib.sha256(p.read_bytes()).hexdigest()))
out=resources/'SkyPulse/characters/bird-pose-bounds.json'
out.write_text(json.dumps(dict(frames=frames),indent=2)+'\n')
print('Measured 30 hit/unlock poses; every nontransparent pixel retained.')
