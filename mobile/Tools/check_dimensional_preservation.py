#!/usr/bin/env python3
"""Read-only comparison against the protected starting commit for the 3D branch."""
import hashlib,json,re,subprocess
from pathlib import Path
from generate_skypulse_audio import CUE_FRAMES, make_clip
ROOT=Path(__file__).resolve().parents[2]
BASE='a343aef'
AUDIO_IDENTITY_BASE='ad47d69' # Includes the two later Signal Heist cues.
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
files=['SkyPulseMissionModels.cs','SkyPulseDailyMissions.cs','SkyPulseBirdProgression.cs','SkyPulseInputFence.cs','SkyPulseRelayGate.cs','SkyPulsePrism.cs','SkyPulseBlackoutCampaign.cs']
for name in files:
    path='mobile/Assets/Scripts/'+name;data=(ROOT/path).read_bytes();assert data==old(path),'Protected rules changed: '+name;report['unchangedFiles'][path]=digest(data)
# The owner explicitly requested a new sound palette on 23 October. Keep the
# resource names/GUIDs and require reproducible authored output, rather than
# treating an old waveform as an immutable gameplay rule.
report['authoredAudio']={}
for name in CUE_FRAMES:
    path='mobile/Assets/Resources/SkyPulse/audio/'+name+'.wav';data=(ROOT/path).read_bytes()
    assert data==make_clip(name),'Unreproducible cue: '+name
    meta=path+'.meta';original_meta=subprocess.check_output(['git','show',AUDIO_IDENTITY_BASE+':'+meta],cwd=ROOT)
    assert (ROOT/meta).read_bytes()==original_meta,'Audio identity/import settings changed: '+name
    report['authoredAudio'][path]={'sha256':digest(data),'metaSha256':digest((ROOT/meta).read_bytes()),'matchesGenerator':True}
for path in (ROOT/'mobile/Assets/Scenes').glob('*'):
    if path.is_file():
        rel=path.relative_to(ROOT).as_posix();assert path.read_bytes()==old(rel),'Scene changed';report['unchangedFiles'][rel]=digest(path.read_bytes())
for filename,names in {
 'SkyPulseNativeGame.cs':['CreateCamera','SimulateFlight','UpdateBird','Flap','CollidersOverlap','BirdCollidesWithPipe','RoutePipeSpacing','GetViewportWidth','UpdatePipes','ConfigurePipe','BeginWorldTransition','UpdateWorldTransition','CollectCrystalPickup','EndFlight','BankCollectedCrystals','SaveProgress'],
 'SkyPulseNativeGame.Missions.cs':['SimulateRelay','RelayPlaneContact','RelayCellContact','RelayUnlocked','FinalizeMission','ClaimDaily'],
 # The owner requested Blackout objective/presentation changes on 23 October.
 # Protect each input, save and progression entry point as well as the complete
 # campaign simulator above, while allowing its existing UI to be redesigned.
 'SkyPulseNativeGame.Blackout.cs':['TouchBlackoutNode','ActivateNextBlackoutPuzzle','RestartBlackoutVault','SaveBlackoutAttempt','ToggleBlackoutPreview','CancelBlackoutPreview','PreviewBlackoutNode','CommitBlackoutPreview','ResetBlackoutEffects']
}.items():
    path='mobile/Assets/Scripts/'+filename;a=old(path).decode();b=(ROOT/path).read_text()
    for name in names:
        before,after=body(a,name),body(b,name);assert before==after,'Protected method changed: '+name;report['unchangedMethods'][name]=digest(after.encode())
path='mobile/Assets/Scripts/SkyPulseNativeGame.cs';a=old(path).decode();b=(ROOT/path).read_text()
begin=body(b,'BeginFlight').replace('            ResetFlightEnvironment();\n','').replace('            scoreBurstTimer = 0f;\n            if (scoreBurstText != null) scoreBurstText.gameObject.SetActive(false);\n','')
assert body(a,'BeginFlight')==begin,'BeginFlight changed beyond scenery and stale-feedback presentation resets'
assert body(a,'LoadProgress')==body(b,'LoadProgress').replace('            if(audioSource!=null){audioSource.mute=!soundEnabled;if(!soundEnabled)StopGameSounds();}\n',''),'LoadProgress changed beyond applying saved audio preference'
guids={}
for path in (ROOT/'mobile/Assets').rglob('*.meta'):
    match=re.search(r'^guid: ([a-f0-9]{32})$',path.read_text(),re.M);assert match,'Invalid Unity metadata '+str(path)
    assert match[1] not in guids,'Duplicate Unity GUID';guids[match[1]]=path.relative_to(ROOT).as_posix()
report.update(uniqueUnityGuids=len(guids),presentationOnlyFlightHook=True,newRunFeedbackReset=True,savedAudioPreferenceHook=True,passed=True)
print(json.dumps(report,indent=2))
