#!/usr/bin/env python3
"""Read-only release metadata checks; not certification of originality or review approval."""
import argparse
import hashlib
import json
import plistlib
import re
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CURRENT = ROOT / 'mobile/Release/CURRENT_RELEASE.json'


def validate_copy(meta, contract, current=False):
    limits = {}
    for field, limit in [('name', 30), ('subtitle', 30), ('promotionalText', 170), ('description', 4000), ('keywords', 100)]:
        value = meta[field]
        length = len(value.encode('utf-8')) if field == 'keywords' else len(value)
        assert value.strip() and 0 < length <= limit, field + ' is empty or exceeds current Apple limit'
        assert not re.search(r'<[^>]+>', value), field + ' contains HTML'
        assert not re.search(r'\b(lorem ipsum|placeholder|coming soon|todo)\b', value, re.I), field + ' contains unfinished copy'
        limits[field] = {'length': length, 'limit': limit, 'unit': 'UTF-8 bytes' if field == 'keywords' else 'characters'}
    keywords = [word.strip().casefold() for word in meta['keywords'].split(',')]
    assert len(keywords) == len(set(keywords)) and all(len(word) > 2 for word in keywords), 'Empty or repeated keyword'
    assert all(word not in meta['name'].casefold().split() for word in keywords), 'Keyword duplicates app name'
    assert meta['name'] == contract['appName'], 'Wrong app identity'
    if current:
        assert meta['copyright'] == '2026 ' + contract['creator'], 'Wrong creator copyright notice'
        assert contract['creator'] in meta['description'], 'Creator missing from current copy'
        for mode in ['ENDLESS ROUTE', 'PULSE RELAY', 'BLACKOUT PROTOCOL']:
            assert mode in meta['description'], 'Current mode missing: ' + mode
        assert 'four fresh daily circuits' in meta['description'] and 'eight increasingly demanding' in meta['description'], 'Stale Relay/Blackout copy'
        assert 'Signal Heist' in meta['description'] and 'rotate mirrors' in meta['description'], 'Retired Blackout puzzle copy'
        assert not re.search(r'connected neighbours|auto-breach|cipher keys|phase switches', meta['description'], re.I), 'Retired tile-walking instructions'
        assert not re.search(r'(press|tap|use).{0,15}(PULSE button|FLAP button)', meta['description'], re.I), 'Retired gameplay button instructions'
    return limits


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--metadata', type=Path, help='Historical override; default follows CURRENT_RELEASE.json')
    parser.add_argument('--archive', type=Path)
    parser.add_argument('--require-build', type=str)
    parser.add_argument('--online', action='store_true')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    contract = json.loads(CURRENT.read_text())
    metadata_path = (args.metadata or ROOT / contract['metadata']).resolve()
    current = metadata_path == (ROOT / contract['metadata']).resolve()
    meta = json.loads(metadata_path.read_text())
    links = json.loads((ROOT / 'mobile/Assets/Resources/SkyPulsePublicLinks.json').read_text())
    settings = (ROOT / 'mobile/ProjectSettings/ProjectSettings.asset').read_text()
    report = {'metadataPath': str(metadata_path.relative_to(ROOT)) if metadata_path.is_relative_to(ROOT) else str(metadata_path),
              'currentReleaseContract': current, 'limits': validate_copy(meta, contract, current),
              'limitations': ['No global duplication/comparator test or rights certification.', 'Later source, policy and live listing changes require renewed checks.']}
    for field, suffix in [('privacyUrl', '/privacy'), ('supportUrl', '/support')]:
        expected = 'https://skypulse.macwebstudio.com' + suffix
        assert links[field] == meta[field] == expected, field + ' is inconsistent or not branded'
        if args.online:
            with urllib.request.urlopen(urllib.request.Request(expected, headers={'User-Agent': 'SkyPulseReleaseChecks/1.0'}), timeout=30) as response:
                html = response.read().decode('utf-8')
                assert response.status == 200 and response.url.startswith('https://skypulse.macwebstudio.com/')
            assert 'SkyPulse' in html and 'macwebstudio26@gmail.com' in html
            report[field] = {'url': expected, 'http': 200, 'contactPresent': True}
    assert meta['marketingUrl'] == 'https://skypulse.macwebstudio.com/'
    assert re.search(r'^\s+iPhone: ' + re.escape(contract['bundleId']) + r'\s*$', settings, re.M), 'Wrong iOS bundle identity'
    assert 'appleDeveloperTeamID: ' + contract['appleTeamId'] in settings
    # Company/product names are retained deliberately: Unity uses them in existing save paths.
    assert re.search(r'^  companyName: SkyPulse$', settings, re.M) and re.search(r'^  productName: SkyPulse$', settings, re.M)
    build = re.search(r'buildNumber:\s*\n(?:.*\n)*?\s+iPhone: (\d+)', settings).group(1)
    assert int(build) > 0
    if current:
        assert build == contract['build'], 'Current release contract must be updated for this build'
        qa = json.loads((ROOT / contract['qaRecord']).read_text())
        for path, expected_hash in qa['runtimeSourceSha256'].items():
            assert hashlib.sha256((ROOT / 'mobile' / path).read_bytes()).hexdigest() == expected_hash, 'Runtime changed since candidate QA: ' + path
        assert len(list((ROOT / 'mobile/Assets/Scripts').glob('*.cs'))) == len(qa['runtimeSourceSha256']), 'Runtime source added/removed since candidate QA'
        report['runtimeMatchesRecordedQA'] = len(qa['runtimeSourceSha256'])
        manifest = json.loads((ROOT / contract['sourceManifest']).read_text())['files']
        recorded_assets = {row['path'] for row in manifest if row['path'].startswith('Assets/')}
        actual_assets = {p.relative_to(ROOT / 'mobile').as_posix() for p in (ROOT / 'mobile/Assets').rglob('*') if p.is_file() and not p.name.startswith('.')}
        assert actual_assets == recorded_assets, 'Assets added/removed since exported candidate'
        for row in manifest:
            assert hashlib.sha256((ROOT / 'mobile' / row['path']).read_bytes()).hexdigest() == row['sha256'], 'Candidate source changed: ' + row['path']
        report['sourceManifestMatchesCandidate'] = len(manifest)
    if args.require_build:
        assert build == args.require_build
    guids = []
    for path in (ROOT / 'mobile/Assets').rglob('*.meta'):
        match = re.search(r'^guid: ([a-f0-9]{32})$', path.read_text(), re.M)
        assert match, path
        guids.append(match.group(1))
    assert len(guids) == len(set(guids)), 'Duplicate Unity GUID'
    assert not list((ROOT / 'mobile/Assets/Scripts').glob('*Checks.cs')), 'QA helper in runtime scripts'
    packages = json.loads((ROOT / 'mobile/Packages/manifest.json').read_text())['dependencies']
    assert not any(any(word in package for word in ['analytics', 'advertisement', 'purchasing']) for package in packages)
    report.update(build=build, uniqueUnityGuids=len(guids), canonicalRuntimeLinks=True, identityRetained=True, advertisingAnalyticsPurchasingPackages=False)
    if args.archive:
        app = args.archive / 'Products/Applications/SkyPulse.app'
        info = plistlib.loads((app / 'Info.plist').read_bytes())
        assert info['CFBundleIdentifier'] == contract['bundleId'] and info['CFBundleVersion'] == build and info['CFBundleShortVersionString'] == contract['version']
        assert info['UIDeviceFamily'] == [1, 2] and info['ITSAppUsesNonExemptEncryption'] is False
        privacy = plistlib.loads((app / 'PrivacyInfo.xcprivacy').read_bytes())
        assert not privacy.get('NSPrivacyTracking', False)
        assert any(api['NSPrivacyAccessedAPIType'] == 'NSPrivacyAccessedAPICategoryUserDefaults' and 'CA92.1' in api['NSPrivacyAccessedAPITypeReasons'] for api in privacy['NSPrivacyAccessedAPITypes'])
        contents = [p.read_bytes() for p in (app / 'Data').glob('*.assets')]
        for url in links.values():
            assert any(url.encode() in content for content in contents), 'Canonical URL absent from packaged assets: ' + url
        assert not any(b'skypulse-game.mcaulee19.chatgpt.site' in content for content in contents), 'Old hosted URL still packaged'
        report['archiveIdentityPrivacyAndPackagedLinks'] = True
    report['passed'] = True
    output = json.dumps(report, indent=2) + '\n'
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(output)
    print(output)


if __name__ == '__main__':
    main()
