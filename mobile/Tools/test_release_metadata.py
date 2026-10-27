#!/usr/bin/env python3
"""Focused guards against stale or misleading release copy; no Unity save access."""
import copy
import json
import hashlib
import tempfile
from pathlib import Path
import unittest
from check_release_metadata import CURRENT, ROOT, validate_copy, validate_asset_inventory


class CurrentReleaseCopyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.contract = json.loads(CURRENT.read_text())
        cls.current = json.loads((ROOT / cls.contract['metadata']).read_text())

    def rejects(self, field, value):
        meta = copy.deepcopy(self.current)
        meta[field] = value
        with self.assertRaises(AssertionError):
            validate_copy(meta, self.contract, current=True)

    def test_current_candidate(self):
        validate_copy(self.current, self.contract, current=True)

    def test_stale_build9_copy(self):
        old = json.loads((ROOT / 'mobile/Release/PremiumTestFlight20261012/metadata.json').read_text())
        with self.assertRaises(AssertionError):
            validate_copy(old, self.contract, current=True)

    def test_retired_pulse_button(self):
        self.rejects('description', self.current['description'] + '\nPress the PULSE button to unlock gates.')

    def test_retired_blackout_copy(self):
        self.rejects('description', self.current['description'] + ' Tap connected neighbours and auto-breach.')

    def test_wrong_creator(self):
        self.rejects('copyright', '2026 Another Developer')

    def test_stale_training_unlock_benefit(self):
        for old,new in [('2% off future founder-bird unlocks', '5% off future founder-bird unlocks'), ('up to 20%', 'up to 50%')]:
            self.rejects('description', self.current['description'].replace(old,new))

    def test_missing_mode(self):
        self.rejects('description', self.current['description'].replace('BLACKOUT PROTOCOL', 'PUZZLES'))

    def test_repeated_or_empty_keywords(self):
        for value in ['flight,FLIGHT', 'flight,,relay', 'SkyPulse,relay']:
            self.rejects('keywords', value)

    def test_keyword_byte_limit(self):
        self.rejects('keywords', 'é' * 60)

    def test_unfinished_copy(self):
        self.rejects('promotionalText', 'Coming soon: placeholder content')

    def test_historical_copy_remains_readable(self):
        old = json.loads((ROOT / 'mobile/Release/PremiumTestFlight20261012/metadata.json').read_text())
        validate_copy(old, self.contract, current=False)



class AssetInventoryTests(unittest.TestCase):
    def test_recorded_editor_study_and_changed_content(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            name = 'Assets/Editor/local-study.asset'
            file = root / name
            file.parent.mkdir(parents=True)
            file.write_bytes(b'local study')
            exclusions = {name: hashlib.sha256(file.read_bytes()).hexdigest()}
            self.assertEqual(validate_asset_inventory({'Assets/game.asset'}, {'Assets/game.asset', name}, exclusions, root), {name})
            file.write_bytes(b'changed')
            with self.assertRaises(AssertionError):
                validate_asset_inventory(set(), {name}, exclusions, root)

    def test_runtime_exclusion_rejected(self):
        with self.assertRaises(AssertionError):
            validate_asset_inventory(set(), {'Assets/Resources/unexported.asset'}, {'Assets/Resources/unexported.asset': 'hash'}, Path('/unused'))

    def test_unknown_editor_asset_rejected(self):
        with self.assertRaises(AssertionError):
            validate_asset_inventory(set(), {'Assets/Editor/unknown.asset'}, {}, Path('/unused'))

    def test_missing_candidate_asset_rejected(self):
        with self.assertRaises(AssertionError):
            validate_asset_inventory({'Assets/game.asset'}, set(), {}, Path('/unused'))

if __name__ == '__main__':
    unittest.main()
