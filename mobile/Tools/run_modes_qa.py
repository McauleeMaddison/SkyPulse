#!/usr/bin/env python3
"""Build an isolated QA copy, run regressions and render previews. Never resets the real player save."""
import argparse,datetime,os,shutil,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--unity',type=Path,default=Path('/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity'))
parser.add_argument('--output',type=Path)
a=parser.parse_args()
if not a.unity.is_file():raise SystemExit('Unity executable not found; pass --unity PATH')
stamp=datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%d-%H%M%S')
out=(a.output or ROOT/'artifacts'/('modes-qa-'+stamp)).resolve();out.mkdir(parents=True,exist_ok=False)
qa=Path(tempfile.mkdtemp(prefix='skypulse-modes-qa-'))
for folder in ['Assets','Packages','ProjectSettings']:shutil.copytree(ROOT/'mobile'/folder,qa/folder)
p=qa/'ProjectSettings/ProjectSettings.asset';s=p.read_text().replace('companyName: SkyPulse','companyName: SkyPulseQA20261006').replace('productName: SkyPulse','productName: SkyPulseQA20261006');p.write_text(s)
for name in ['SkyPulseBetaChecks.cs','SkyPulseWorldExpansionChecks.cs','SkyPulseReleaseEdgeChecks.cs','SkyPulseMissionChecks.cs','SkyPulsePresentationChecks.cs','SkyPulseBlackoutChecks.cs','SkyPulseOneThumbChecks.cs','SkyPulseDailyRouteChecks.cs','SkyPulseRelayGateChecks.cs','SkyPulseCinematicChecks.cs','SkyPulseDepthChecks.cs','SkyPulseTechTreeChecks.cs','SkyPulseBirdProgressionChecks.cs']:
    shutil.copy2(ROOT/'mobile/Tools'/name,qa/'Assets/Editor'/name)
p=qa/'Assets/Editor/SkyPulseWorldExpansionChecks.cs'
s=p.read_text().replace('        var game = UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();','        SkyPulseBirdProgressionChecks.Pure();\n        SkyPulseMissionChecks.PureChecks();\n        SkyPulseBlackoutChecks.Pure();\n        SkyPulseOneThumbChecks.Pure();\n        SkyPulseDailyRouteChecks.Pure();\n        var game = UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();',1)
s=s.replace('        ValidateWorldExpansion(game);','        ValidateWorldExpansion(game);\n        SkyPulseReleaseEdgeChecks.Run(game);\n        SkyPulseMissionChecks.IntegrationChecks(game);',1)
s=s.replace('        File.WriteAllText(Path.Combine(folder, "PASS.txt")','        yield return SkyPulseDepthChecks.Live(game);\n        yield return SkyPulseTechTreeChecks.Live(game);\n        yield return SkyPulseBirdProgressionChecks.Live(game);\n        yield return SkyPulseBlackoutChecks.Live(game);\n        yield return SkyPulseOneThumbChecks.Live(game);\n        yield return SkyPulseDailyRouteChecks.Live(game);\n        yield return SkyPulseMissionChecks.Captures(game);\n        yield return SkyPulseCinematicChecks.Live(game);\n        yield return SkyPulseRelayGateChecks.Live(game);\n        yield return SkyPulseBlackoutChecks.Captures(game);\n        yield return SkyPulsePresentationChecks.Captures(game);\n        File.WriteAllText(Path.Combine(folder, "PASS.txt")',1);p.write_text(s)
shutil.copy2(p,out/'qa-entrypoint.cs')
(out/'QA_PROJECT.txt').write_text(str(qa)+'\n')
env=os.environ.copy();env['SKYPULSE_QA_OUTPUT']=str(out/'captures')
result=subprocess.run([str(a.unity),'-batchmode','-projectPath',str(qa),'-executeMethod','SkyPulseWorldExpansionChecks.Run','-logFile',str(out/'unity.log')],env=env)
log=(out/'unity.log').read_text(errors='replace')
markers=['SKYPULSE_BIRD_PROGRESSION_PURE_PASS','SKYPULSE_BIRD_PROGRESSION_LIVE_PASS','SKYPULSE_TECH_TREE_PASS','SKYPULSE_DEPTH_PASS','SKYPULSE_HEIST_CAPTURE_PASS','SKYPULSE_CINEMATIC_PASS','SKYPULSE_RELAY_MESH_PASS','SKYPULSE_DAILY_ROUTE_PURE_PASS','SKYPULSE_DAILY_ROUTE_LIVE_PASS','SKYPULSE_ONE_THUMB_PURE_PASS','SKYPULSE_ONE_THUMB_LIVE_PASS','SKYPULSE_BLACKOUT_PURE_PASS','SKYPULSE_BLACKOUT_LIVE_PASS','SKYPULSE_BETA_CHECKS_PASS','SKYPULSE_RELEASE_EDGE_PASS','SKYPULSE_MISSION_PURE_PASS','SKYPULSE_MISSION_INTEGRATION_PASS','SKYPULSE_MISSION_LEGACY_PASS','SKYPULSE_MISSION_CAPTURES_PASS','SKYPULSE_PRESENTATION_CAPTURES_PASS','SKYPULSE_WORLD_CHECKS_PASS']
if result.returncode or any(m not in log for m in markers):raise SystemExit('QA did not pass; inspect '+str(out/'unity.log'))
print('PASS; isolated project retained at '+str(qa)+'; evidence '+str(out))
