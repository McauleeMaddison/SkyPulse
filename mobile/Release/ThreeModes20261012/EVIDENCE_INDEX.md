# Current candidate evidence index

Scope: the 12 October changed source on `codex/neon-visual-update`, based on preserved commit `026e382`. This index distinguishes new Editor evidence from historical binaries/device footage. App Store submission remains on hold.

## Source and reproducible records

- [Specification](SPECIFICATION.txt), [opening/testing/build guide](README.md), [save and lifecycle contracts](SAVE_AND_LIFECYCLE.md).
- Runtime: `mobile/Assets/Scripts/SkyPulseMissionModels.cs`, `SkyPulseDailyMissions.cs`, `SkyPulseNativeGame.Missions.cs`, `SkyPulseCircuitGraphic.cs` and the focused integration changes in `SkyPulseNativeGame.cs`.
- QA: `mobile/Tools/SkyPulseMissionChecks.cs`, `run_modes_qa.py` and `verify_endless_preservation.py`, alongside the retained beta/world/release-edge harnesses. These tools are outside shipping Assets.
- [Test results](TEST_RESULTS.md), [verification JSON](VERIFICATION.JSON), [protected source comparison](ENDLESS-PRESERVATION.JSON), [baseline preservation](BASELINE-PRESERVATION.JSON), [source candidate record](RELEASE_RECORD.json).
- [Current provenance addendum](PROVENANCE_ADDENDUM.md) and regenerated [file ledger](Provenance/FILE_LEDGER.csv), [inventory](Provenance/INVENTORY.json). Original dated ledgers remain in `mobile/Release/Provenance/`.

## Local QA media and logs

These files are deliberately kept in ignored local `artifacts/`, alongside preserved binaries. They are available on this Mac, not included in the shipping app or assumed to be available in a remote checkout.

- Baseline working-copy archive: `artifacts/modes-update-20261012/baseline-working-copy.tar.gz`; starting file hashes and Git status are adjacent.
- Final full-suite log: `artifacts/modes-update-20261012/reproducible-run/final-layout-unity.log`.
- Final scene/resource audit: `artifacts/modes-update-20261012/reproducible-run/static-final.log`.
- Retained original wrapper run: `artifacts/modes-update-20261012/reproducible-run/unity.log` and `qa-entrypoint.cs`; `QA_PROJECT.txt` records the disposable project path.
- Original 63 Editor images: `artifacts/modes-update-20261012/reproducible-run/captures/`. Each resolution includes Home, Hangar, Tech Tree, four guide pages, selected worlds, mode selection, both mission lists, Relay flight, Blackout gameplay, Mission Control and all three Home themes.
- [Two new modes preview](../../../artifacts/modes-update-20261012/previews/new-gameplay-preview.png), [three Home themes](../../../artifacts/modes-update-20261012/previews/home-themes-preview.png), [mode selection and Mission Control](../../../artifacts/modes-update-20261012/previews/mode-and-missions-preview.png).

The sheets arrange real Unity Editor renders; they are not generated concept mockups or physical-device/App Store screenshots. Their save/progress values are isolated QA data. Use fresh exact-candidate device media for a later submission.

## Drafts and historical evidence

- [Local listing draft](metadata.json), [field lengths](METADATA_LENGTHS.json), [reviewer walkthrough](REVIEWER_WALKTHROUGH.txt), [prepared unsent Apple reply](APPLE_RESPONSE_DRAFT.md), [changed-candidate device checklist](DEVICE_CHECKLIST.md).
- Owner's scoped account-history statement: `mobile/Release/Build7/OWNER_ACCOUNT_HISTORY.md`. The related support request/hand-off and case 102987914250 remain dated 6 October records. No new Apple reply was inspected during this implementation.
- Build 7 record, archive/evidence, device sign-offs and recording remain historical: `mobile/Release/Build7/RELEASE_RECORD.json`. They do not validate the newly implemented modes.
- Current hold: `mobile/Release/Build7/SUBMISSION_HOLD.md` and `mobile/Release/REVIEW_STATUS.md`. The App Store page, metadata, website and Apple correspondence were not changed by this source update.
- Build 5 rollback tag/bundle and build 5/6/7 archived binaries are preserved. See the dated release/audit records for their exact identities.
