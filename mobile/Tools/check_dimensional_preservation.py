#!/usr/bin/env python3
"""Read-only comparison against the protected starting commit for the 3D branch."""
import hashlib,json,re,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
BASE='a343aef'
def old(path):return subprocess.check_output(['git','show',BASE+':'+path],cwd=ROOT)
def digest(data):return hashlib.sha256(data).hexdigest()
def body(source,name):
    pattern=r'^\s*(?:(?:private|public|internal|protected|static)\s+)*[\w<>\[\],]+\s+'+re.escape(name)+r'\([^\n]*\)\s*\{'
    found=re.search(pattern,source,re.M);assert found,'Missing method '+name
    start=source.index('{',found.start());depth=0
    for i in range(start,len(source)):
        if source[i]=='{':depth+=1
        elif source[i]=='}':depth-=1
        if depth==0:return source[found.start():i+1].strip()
    raise AssertionError('Unbalanced method '+name)
report={'baseCommit':BASE,'unchangedFiles':{},'unchangedMethods':{}}
files=['SkyPulseMissionModels.cs','SkyPulseDailyMissions.cs','SkyPulseBirdProgression.cs','SkyPulseInputFence.cs','SkyPulseRelayGate.cs','SkyPulsePrism.cs','SkyPulseNativeGame.Blackout.cs']
for name in files:
    path='mobile/Assets/Scripts/'+name;data=(ROOT/path).read_bytes();assert data==old(path),'Protected rules changed: '+name;report['unchangedFiles'][path]=digest(data)
for name in ['flap','score','crystal','crash','unlock']:
    path='mobile/Assets/Resources/SkyPulse/audio/'+name+'.wav';data=(ROOT/path).read_bytes();assert data==old(path),'Working cue replaced: '+name;report['unchangedFiles'][path]=digest(data)
for path in (ROOT/'mobile/Assets/Scenes').glob('*'):
    if path.is_file():
        rel=path.relative_to(ROOT).as_posix();assert path.read_bytes()==old(rel),'Scene changed';report['unchangedFiles'][rel]=digest(path.read_bytes())
for filename,names in {
 'SkyPulseNativeGame.cs':['CreateCamera','SimulateFlight','UpdateBird','Flap','CollidersOverlap','BirdCollidesWithPipe','RoutePipeSpacing','GetViewportWidth','UpdatePipes','ConfigurePipe','BeginWorldTransition','UpdateWorldTransition','CollectCrystalPickup','EndFlight','BankCollectedCrystals','SaveProgress'],
 'SkyPulseNativeGame.Missions.cs':['SimulateRelay','RelayPlaneContact','RelayCellContact','RelayUnlocked','FinalizeMission','ClaimDaily']
}.items():
    path='mobile/Assets/Scripts/'+filename;a=old(path).decode();b=(ROOT/path).read_text()
    for name in names:
        before,after=body(a,name),body(b,name);assert before==after,'Protected method changed: '+name;report['unchangedMethods'][name]=digest(after.encode())
path='mobile/Assets/Scripts/SkyPulseNativeGame.cs';a=old(path).decode();b=(ROOT/path).read_text()
assert body(a,'BeginFlight')==body(b,'BeginFlight').replace('            ResetFlightEnvironment();\n',''),'BeginFlight changed beyond presentation reset'
assert body(a,'LoadProgress')==body(b,'LoadProgress').replace('            if(audioSource!=null){audioSource.mute=!soundEnabled;if(!soundEnabled)StopGameSounds();}\n',''),'LoadProgress changed beyond applying saved audio preference'
guids={}
for path in (ROOT/'mobile/Assets').rglob('*.meta'):
    match=re.search(r'^guid: ([a-f0-9]{32})$',path.read_text(),re.M);assert match,'Invalid Unity metadata '+str(path)
    assert match[1] not in guids,'Duplicate Unity GUID';guids[match[1]]=path.relative_to(ROOT).as_posix()
report.update(uniqueUnityGuids=len(guids),presentationOnlyFlightHook=True,savedAudioPreferenceHook=True,passed=True)
print(json.dumps(report,indent=2))
