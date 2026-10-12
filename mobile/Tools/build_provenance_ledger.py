#!/usr/bin/env python3
"""Inventory evidence, without inferring copyright or authorship from Git."""
import argparse
import csv
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output', type=Path, default=ROOT / 'mobile/Release/Provenance')
parser.add_argument('--date', default='2026-09-29')
args = parser.parse_args()
OUT = args.output if args.output.is_absolute() else ROOT / args.output

def git(*args):
    result = subprocess.run(['git', *args], cwd=ROOT, capture_output=True, check=True)
    return result.stdout

def evidence_for(path):
    if path.endswith('pulse-relay-gate-v2.png'):
        return 'Generated with the built-in image_gen tool on 12 October 2026, without reference inputs; transparent portal hardware used only by Relay; exact prompt/output/hash retained; no exclusivity certification', 'mobile/Release/RelayGateUplift20261012/ASSET_GENERATION.json'
    if any(path.endswith(name) for name in ['pulse-relay-station-v1.png','blackout-core-v1.png']):
        return 'Generated in this session with the built-in image_gen tool on 12 October 2026, without reference images; exact prompts/output records/hash retained; no independent certification of exclusivity', 'mobile/Release/ModeIdentityWidth20261012/ASSET_GENERATION.json'
    if path.endswith('SkyPulseNativeGame.Presentation.cs') or path.endswith('SkyPulseCircuitGraphic.cs'):
        return 'Codex-assisted premium mode presentation on 12 October 2026; procedural lighting/material details, retained generated portal artwork and pause/Reduced Motion support; protected Endless source unchanged', 'mobile/Release/PremiumTestFlight20261012/PROVENANCE_ADDENDUM.md'
    if path.endswith('SkyPulseNativeGame.Missions.cs'):
        return 'Codex-assisted presentation on 12 October 2026: procedural geometry, distinct portal/data-core views, badge navigation and motion; new generated backdrops and retained artwork separately documented; no external shader/font inputs', 'mobile/Release/ModeIdentityWidth20261012/PROVENANCE_ADDENDUM.md'
    if path.endswith('SkyPulseNativeGame.cs'):
        return 'Existing gameplay implementation retained; Codex-assisted protocol, UI, lifecycle and explicit-wallet hooks added on 12 October 2026 against baseline 026e382; protected flight methods/catalogues unchanged', 'mobile/Release/ThreeModes20261012/PROVENANCE_ADDENDUM.md'
    if any(path.endswith(name) for name in ['SkyPulseMissionModels.cs','SkyPulseDailyMissions.cs','SkyPulseNativeGame.Missions.cs','SkyPulseCircuitGraphic.cs']):
        return 'SkyPulse three-mode implementation authored with Codex assistance on 12 October 2026 from the owner request; procedural circuit/grid/pixel geometry retained in code; no new image, sample or font inputs', 'mobile/Release/ThreeModes20261012/PROVENANCE_ADDENDUM.md'
    if 'characters/roster/' in path:
        return 'Bird artwork: owner confirms ARTA-generated SkyPulse artwork; original generation records and reference origins not independently verified', ''
    if '/audio/' in path:
        return 'Synthesized for SkyPulse with retained standard-library generator; no input samples', 'mobile/Release/Provenance/AUDIO_PROVENANCE.md'
    if '-user-20260929.jpeg' in path:
        return 'User-supplied world image; owner confirms ARTA generation; original records/reference origins not independently verified', 'mobile/Release/WorldExpansion/ART_PROVENANCE.md'
    if '/Branding/' in path:
        return 'Repository note records AI generation; underlying bird/reference origin pending', 'mobile/Release/ICON.md'
    if any(name in path for name in ['neon-flightdeck-v2','aurora-rise-v3','solar-drift-v3']):
        return 'Repository note records AI generation; supplied reference origin pending', 'mobile/Release/NeonVisualUpdate/BACKGROUND_ART_PROVENANCE.md'
    if '/art/pipes/pipe-' in path:
        return 'Repository note records AI generation; exact prompts not retained; reference origin pending', 'mobile/Release/NeonVisualUpdate/PIPE_ART_PROVENANCE.md'
    if 'crystal-prism-neon-v4' in path:
        return 'Repository note records AI generation; exact prompt not retained; reference origin pending', 'mobile/Release/NeonVisualUpdate/CRYSTAL_ART_PROVENANCE.md'
    if '/references/' in path:
        return 'Supplied visual reference; original source and permission pending', ''
    if Path(path).suffix in ['.png', '.jpeg', '.wav']:
        return 'Owner declares ARTA artwork origin; specific source records and reference origins not independently verified', ''
    return 'Project implementation/configuration history; owner asserts code ownership and development in VS Code; individual contributions not independently verified', ''

paths = sorted(p for p in (ROOT / 'mobile/Assets').rglob('*')
               if p.is_file() and not p.name.startswith('.') and p.suffix != '.meta')
paths += sorted((ROOT / 'mobile/Release/NeonVisualUpdate/references').glob('*.png'))
rows = []
for p in paths:
    relative = p.relative_to(ROOT).as_posix()
    data = p.read_bytes()
    history = git('log', '--follow', '--diff-filter=A', '--format=%H|%aI|%s', '--', relative).decode().splitlines()
    first = history[-1].split('|', 2) if history else ['', '', 'Not committed at inventory time']
    try:
        same_head = git('show', 'HEAD:' + relative) == data
    except subprocess.CalledProcessError:
        same_head = False
    state, evidence = evidence_for(relative)
    prototype_path = ''
    prototype_match = ''
    if p.suffix == '.wav':
        prototype_path = 'assets/audio/' + p.name
        try:
            prototype_match = str(git('show', 'edcf770:' + prototype_path) == data).lower()
        except subprocess.CalledProcessError:
            prototype_match = 'missing'
    rows.append(dict(path=relative, bytes=len(data), sha256=hashlib.sha256(data).hexdigest(),
        first_path_commit=first[0], first_path_author_date=first[1], first_path_subject=first[2],
        same_bytes_as_HEAD=str(same_head).lower(), evidence_status=state, evidence_note=evidence,
        prototype_path=prototype_path, matches_prototype_edcf770=prototype_match,
        rights_status=('Retained synthesis source; no third-party audio inputs' if p.suffix == '.wav' else 'Not independently verified')))
OUT.mkdir(parents=True, exist_ok=True)
with (OUT / 'FILE_LEDGER.csv').open('w', newline='') as f:
    writer = csv.DictWriter(f, fieldnames=list(rows[0]), lineterminator="\n")
    writer.writeheader()
    writer.writerows(rows)
metadata = dict(inventory_date=args.date, baseline_head=git('rev-parse','HEAD').decode().strip(),
    branch=git('branch','--show-current').decode().strip(), files=len(rows),
    scope='All non-hidden, non-meta files under mobile/Assets plus three retained visual references',
    limitations=['First-path commit and author date are repository claims, not independently verified creation dates.',
                'First-path commit does not necessarily contain the current bytes; see same_bytes_as_HEAD and SHA256.',
                'This ledger does not certify authorship, licence compliance, uniqueness or Apple account relationships.'],
    audio_prototype_matches=sum(r['matches_prototype_edcf770']=='true' for r in rows),
    audio_replacement_record='mobile/Release/Provenance/AUDIO_REPLACEMENT.json')
(OUT / 'INVENTORY.json').write_text(json.dumps(metadata, indent=2)+'\n')
print(json.dumps(metadata, indent=2))
