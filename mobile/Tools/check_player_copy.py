#!/usr/bin/env python3
"""Read-only placeholder guard for shipping source strings and resource JSON.
Functional previews, empty dynamic labels, saved IDs and developer comments are
not unfinished player copy. This guard complements visible-screen checks/OCR.
"""
import json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
RULES=ROOT/'mobile/Tools/player-copy-rules.json'

def literals(source):
    i=0;line=1
    while i<len(source):
        if source.startswith('//',i):
            end=source.find('\n',i);i=len(source) if end<0 else end;continue
        if source.startswith('/*',i):
            end=source.find('*/',i+2);end=len(source) if end<0 else end+2
            line+=source.count('\n',i,end);i=end;continue
        if source[i]=="'":
            i+=1
            while i<len(source):
                if source[i]=='\\':i+=2;continue
                if source[i]=="'":i+=1;break
                i+=1
            continue
        if source[i]=='"':
            start=line;verbatim=i>0 and source[i-1]=='@';i+=1;parts=[]
            while i<len(source):
                if source[i]=='"':
                    if verbatim and source.startswith('""',i):parts.append('"');i+=2;continue
                    i+=1;break
                if not verbatim and source[i]=='\\' and i+1<len(source):
                    parts.append({'n':'\n','r':'\r','t':'\t'}.get(source[i+1],source[i+1]));i+=2;continue
                line+=source[i]=='\n';parts.append(source[i]);i+=1
            yield start,''.join(parts)
            continue
        line+=source[i]=='\n';i+=1

def strings(value):
    if isinstance(value,str):yield value
    elif isinstance(value,dict):
        for v in value.values():yield from strings(v)
    elif isinstance(value,list):
        for v in value:yield from strings(v)

def main():
    pattern=re.compile(json.loads(RULES.read_text())['pattern'],re.I)
    for unfinished in ('COMING SOON','Placeholder title','Lorem ipsum','TBD','[Your name]'):
        assert pattern.search(unfinished),'Guard missed negative control '+unfinished
    for valid in ('Preview your offspring','Plan a rotation','Temporary power-ups','Select a prism'):
        assert not pattern.search(valid),'Guard rejected functional copy '+valid
    paths=sorted((ROOT/'mobile/Assets/Scripts').glob('*.cs'));findings=[];count=0
    for path in paths:
        for line,value in literals(path.read_text()):
            count+=1
            if pattern.search(value):findings.append({'path':str(path.relative_to(ROOT)),'line':line,'text':value})
    resources=sorted((ROOT/'mobile/Assets/Resources').rglob('*.json'))
    for path in resources:
        for value in strings(json.loads(path.read_text())):
            count+=1
            if pattern.search(value):findings.append({'path':str(path.relative_to(ROOT)),'text':value})
    print(json.dumps({'passed':not findings,'sourceFiles':len(paths),'resourceJsonFiles':len(resources),'stringsChecked':count,'negativeControlsPassed':5,'legitimateCopyControlsPassed':4,'findings':findings,'limitations':['Does not certify typography, image text, every possible dynamic state or future edits.']},indent=2))
    raise SystemExit(bool(findings))
if __name__=='__main__':main()
