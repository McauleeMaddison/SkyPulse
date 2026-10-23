#!/usr/bin/env python3
"""Compile/test/render the 3D upgrade in a disposable Unity product, not the owner save."""
import argparse, os, shutil, subprocess, tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--output',type=Path,required=True)
p.add_argument('--unity',type=Path,default=Path('/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity'))
a=p.parse_args();out=a.output.resolve();out.mkdir(parents=True,exist_ok=False)
qa=Path(tempfile.mkdtemp(prefix='skypulse-dimensional-qa-'))
for folder in ['Assets','Packages','ProjectSettings']:shutil.copytree(ROOT/'mobile'/folder,qa/folder)
settings=qa/'ProjectSettings/ProjectSettings.asset'
settings.write_text(settings.read_text().replace('companyName: SkyPulse','companyName: SkyPulseQA20261006').replace('productName: SkyPulse','productName: SkyPulseQA20261006'))
for source in (ROOT/'mobile/Tools').glob('*.cs'):shutil.copy2(source,qa/'Assets/Editor'/source.name)
shutil.copy2(ROOT/'mobile/Tools/player-copy-rules.json',qa/'Assets/Editor/player-copy-rules.json')
(out/'QA_PROJECT.txt').write_text(str(qa)+'\n')
env=os.environ.copy();env['SKYPULSE_QA_OUTPUT']=str(out/'captures');env['SKYPULSE_PHONE_PREVIEW']='1'
env['SKYPULSE_PREVIEW_SCENES']='home-relay,home-endless,home-blackout,relay-flight,endless-flight'
env['SKYPULSE_CAPTURE_ENDLESS']='1'
result=subprocess.run([str(a.unity),'-batchmode','-projectPath',str(qa),'-executeMethod','SkyPulseDimensionalChecks.Run','-logFile',str(out/'unity.log')],env=env)
log=(out/'unity.log').read_text(errors='replace')
markers=['SKYPULSE_SCORE_FINISH_PASS','SKYPULSE_COPY_UI_PASS','SKYPULSE_PURPOSE_LIVE_PASS','SKYPULSE_BLACKOUT_LIVE_PASS','SKYPULSE_HEIST_CAPTURE_PASS','SKYPULSE_PIPE_FINISH_PASS','SKYPULSE_TOUCH_COMFORT_PASS','SKYPULSE_DIMENSIONAL_AUDIO_PASS','SKYPULSE_FLIGHT_ENVIRONMENT_PASS','SKYPULSE_BIRD_PROGRESSION_PURE_PASS','SKYPULSE_DAILY_ROUTE_PURE_PASS','SKYPULSE_MISSION_PURE_PASS','SKYPULSE_ONE_THUMB_PURE_PASS','SKYPULSE_BLACKOUT_PURE_PASS','SKYPULSE_MISSION_INTEGRATION_PASS','SKYPULSE_ONE_THUMB_LIVE_PASS','SKYPULSE_DAILY_ROUTE_LIVE_PASS','SKYPULSE_BIRD_PROGRESSION_LIVE_PASS','SKYPULSE_GUIDE_WORLD_PASS','SKYPULSE_DEPTH_PASS','SKYPULSE_CINEMATIC_PASS','SKYPULSE_RELAY_MESH_PASS','SKYPULSE_PRESENTATION_CAPTURES_PASS','SKYPULSE_DIMENSIONAL_PASS']
if result.returncode or 'error CS' in log or any(m not in log for m in markers):raise SystemExit('QA failed; inspect '+str(out/'unity.log'))
print('PASS: '+str(qa)+'; evidence '+str(out))
