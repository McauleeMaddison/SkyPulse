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
for name in ['SkyPulseBetaChecks.cs','SkyPulseWorldExpansionChecks.cs','SkyPulseReleaseEdgeChecks.cs','SkyPulseMissionChecks.cs']:
    shutil.copy2(ROOT/'mobile/Tools'/name,qa/'Assets/Editor'/name)
p=qa/'Assets/Editor/SkyPulseWorldExpansionChecks.cs'
s=p.read_text().replace('        var game = UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();','        SkyPulseMissionChecks.PureChecks();\n        var game = UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();',1)
s=s.replace('        ValidateWorldExpansion(game);','        ValidateWorldExpansion(game);\n        SkyPulseReleaseEdgeChecks.Run(game);\n        SkyPulseMissionChecks.IntegrationChecks(game);',1)
s=s.replace('        File.WriteAllText(Path.Combine(folder, "PASS.txt")','        yield return SkyPulseMissionChecks.Captures(game);\n        File.WriteAllText(Path.Combine(folder, "PASS.txt")',1);p.write_text(s)
shutil.copy2(p,out/'qa-entrypoint.cs')
(out/'QA_PROJECT.txt').write_text(str(qa)+'\n')
env=os.environ.copy();env['SKYPULSE_QA_OUTPUT']=str(out/'captures')
result=subprocess.run([str(a.unity),'-batchmode','-projectPath',str(qa),'-executeMethod','SkyPulseWorldExpansionChecks.Run','-logFile',str(out/'unity.log')],env=env)
log=(out/'unity.log').read_text(errors='replace')
markers=['SKYPULSE_BETA_CHECKS_PASS','SKYPULSE_RELEASE_EDGE_PASS','SKYPULSE_MISSION_PURE_PASS','SKYPULSE_MISSION_INTEGRATION_PASS','SKYPULSE_MISSION_LEGACY_PASS','SKYPULSE_MISSION_CAPTURES_PASS','SKYPULSE_WORLD_CHECKS_PASS']
if result.returncode or any(m not in log for m in markers):raise SystemExit('QA did not pass; inspect '+str(out/'unity.log'))
print('PASS; isolated project retained at '+str(qa)+'; evidence '+str(out))
