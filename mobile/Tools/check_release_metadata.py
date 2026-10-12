#!/usr/bin/env python3
"""Read-only release metadata checks; not certification of originality or future review approval."""
import argparse, hashlib, json, plistlib, re, urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--metadata', type=Path, default=ROOT/'mobile/Release/PremiumTestFlight20261012/metadata.json')
parser.add_argument('--archive', type=Path)
parser.add_argument('--require-build', type=str)
parser.add_argument('--online', action='store_true')
parser.add_argument('--output', type=Path)
args = parser.parse_args()
meta = json.loads(args.metadata.read_text())
links = json.loads((ROOT/'mobile/Assets/Resources/SkyPulsePublicLinks.json').read_text())
settings = (ROOT/'mobile/ProjectSettings/ProjectSettings.asset').read_text()
report = {'limits': {}, 'limitations': ['No global duplication/comparator test or rights certification.', 'Later source, policy and live listing changes require renewed checks.']}
for field, limit in [('name',30),('subtitle',30),('promotionalText',170),('description',4000),('keywords',100)]:
    value = meta[field]; length = len(value.encode('utf-8')) if field=='keywords' else len(value)
    assert 0 < length <= limit, field+' exceeds current Apple limit'
    assert not re.search(r'<[^>]+>',value), field+' contains HTML'
    report['limits'][field]={'length':length,'limit':limit,'unit':'UTF-8 bytes' if field=='keywords' else 'characters'}
keywords=[word.strip().casefold() for word in meta['keywords'].split(',')]
assert len(keywords)==len(set(keywords)) and all(len(word)>2 for word in keywords)
assert all(word not in meta['name'].casefold().split() for word in keywords), 'Keyword duplicates app name'
assert meta['name']=='SkyPulse Arcade'
for field, suffix in [('privacyUrl','/privacy'),('supportUrl','/support')]:
    expected='https://skypulse.macwebstudio.com'+suffix
    assert links[field]==meta[field]==expected, field+' is inconsistent or not branded'
    if args.online:
        with urllib.request.urlopen(urllib.request.Request(expected,headers={'User-Agent':'SkyPulseReleaseChecks/1.0'}),timeout=30) as response:
            html=response.read().decode('utf-8');assert response.status==200 and response.url.startswith('https://skypulse.macwebstudio.com/')
        assert 'SkyPulse' in html and 'macwebstudio26@gmail.com' in html
        report[field]={'url':expected,'http':200,'contactPresent':True}
assert 'com.mcauleemaddison.skypulse' in settings and 'appleDeveloperTeamID: TX54JLQ8VF' in settings
build=re.search(r'buildNumber:\s*\n(?:.*\n)*?\s+iPhone: (\d+)',settings).group(1)
assert int(build)>0
if args.require_build: assert build==args.require_build
guids=[]
for path in (ROOT/'mobile/Assets').rglob('*.meta'):
    match=re.search(r'^guid: ([a-f0-9]{32})$',path.read_text(),re.M);assert match,path
    guids.append(match.group(1))
assert len(guids)==len(set(guids)), 'Duplicate Unity GUID'
assert not list((ROOT/'mobile/Assets/Scripts').glob('*Checks.cs')), 'QA helper in runtime scripts'
packages=json.loads((ROOT/'mobile/Packages/manifest.json').read_text())['dependencies']
assert not any(any(word in package for word in ['analytics','advertisement','purchasing']) for package in packages)
report.update(build=build,uniqueUnityGuids=len(guids),canonicalRuntimeLinks=True,identityRetained=True,advertisingAnalyticsPurchasingPackages=False)
if args.archive:
    app=args.archive/'Products/Applications/SkyPulse.app';info=plistlib.loads((app/'Info.plist').read_bytes())
    assert info['CFBundleIdentifier']=='com.mcauleemaddison.skypulse' and info['CFBundleVersion']==build and info['CFBundleShortVersionString']=='1.0.0'
    assert info['UIDeviceFamily']==[1,2] and info['ITSAppUsesNonExemptEncryption'] is False
    privacy=plistlib.loads((app/'PrivacyInfo.xcprivacy').read_bytes());assert not privacy.get('NSPrivacyTracking',False)
    assert any(api['NSPrivacyAccessedAPIType']=='NSPrivacyAccessedAPICategoryUserDefaults' and 'CA92.1' in api['NSPrivacyAccessedAPITypeReasons'] for api in privacy['NSPrivacyAccessedAPITypes'])
    # Read the packaged serialized resource bytes, not only the source config.
    data=app/'Data';contents=[p.read_bytes() for p in data.glob('*.assets')]
    for url in links.values():assert any(url.encode() in content for content in contents),'Canonical URL absent from packaged assets: '+url
    assert not any(b'skypulse-game.mcaulee19.chatgpt.site' in content for content in contents),'Old hosted URL still packaged'
    report['archiveIdentityPrivacyAndPackagedLinks']=True
report['passed']=True
text=json.dumps(report,indent=2)+'\n'
if args.output:args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(text)
print(text)
