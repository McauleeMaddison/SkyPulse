#!/usr/bin/env python3
"""Verify complete current image evidence coverage, not legal ownership or uniqueness."""
import argparse
import collections
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
IMAGE_EXTENSIONS = {'.png', '.jpg', '.jpeg', '.svg', '.webp', '.gif', '.tif', '.tiff', '.psd'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    contract = json.loads((ROOT / 'mobile/Release/CURRENT_RELEASE.json').read_text())
    ledger = json.loads((ROOT / contract['artworkLedger']).read_text())
    actual = {p.relative_to(ROOT).as_posix(): p for p in (ROOT / 'mobile/Assets').rglob('*') if p.is_file() and p.suffix.lower() in IMAGE_EXTENSIONS}
    recorded = {row['path']: row for row in ledger['assets']}
    assert len(recorded) == len(ledger['assets']), 'Repeated ledger path'
    assert actual.keys() == recorded.keys(), 'Image ledger coverage differs: ' + str(actual.keys() ^ recorded.keys())
    hashes = collections.defaultdict(list)
    for path, file in actual.items():
        row = recorded[path]
        digest = hashlib.sha256(file.read_bytes()).hexdigest()
        assert digest == row['sha256'] and file.stat().st_size == row['bytes'], 'Image changed since rights record: ' + path
        assert row['providerEvidence'] and row['useRightsBasis'] and (ROOT / row['evidenceNote']).is_file(), 'Missing source evidence: ' + path
        assert row['exclusiveRightsVerified'] is False, 'Unsupported per-image exclusivity assertion'
        hashes[digest].append(path)
    duplicates = sorted(sorted(paths) for paths in hashes.values() if len(paths) > 1)
    assert duplicates == sorted(sorted(paths) for paths in ledger['exactByteDuplicateGroups']), 'Duplicate report differs'
    assert ledger['imageCount'] == len(actual)
    assert ledger['exclusiveCopyrightCertified'] is False and ledger['appleSimilarityResolved'] is False
    report = {'passed': True, 'imagesCovered': len(actual), 'sha256AndSizesMatch': True,
              'exactByteDuplicateGroups': duplicates, 'ownerReferenceStatementRecorded': True,
              'exclusiveRightsCertified': False, 'appleSimilarityResolved': False,
              'limitations': ['Exact byte checks do not compare against other apps or certify copyright.', 'Owner attestations and published terms do not authenticate every original export.']}
    text = json.dumps(report, indent=2) + '\n'
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(text)
    print(text)


if __name__ == '__main__':
    main()
