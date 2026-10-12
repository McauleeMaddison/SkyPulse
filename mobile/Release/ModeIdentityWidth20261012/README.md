# Shared width and distinct mode views — 12 October 2026

Later on the same day, [RelayGateUplift20261012](../RelayGateUplift20261012/README.md) adds realistic Relay hardware and corrects preview framing. This record is retained as the preceding checkpoint.

This is still the existing **Unity 6000.6.0f1 mobile game for iPhone and iPad**. Open `mobile/Assets/Scenes/SkyPulse.unity`; new launches start on Endless Home. Tap the top-left badge, choose a free mode, then Play. Submission remains on hold; these source changes are newer than the retained build 7.

## Completed visual changes

- All protocols retain the original Endless camera viewport/orthographic width, 1080-unit fitted interface width and safe-area scaling. The opaque decorative side strips that made Relay appear narrower were removed. New backdrops cover the same full canvas; no mode-specific camera zoom or letterbox was added.
- Endless retains its existing sky/pipe gameplay and large animated bird Home presentation.
- Relay has a dedicated cinematic orbital station, floating illuminated portal relays in place of floor/ceiling pipe silhouettes, charge animation and a separate instrument deck. Its Home displays a smaller pilot bird beside an animated portal. The scene depicts depth in the artwork; the tested flight controller and objective coordinates remain unchanged.
- Blackout has a dedicated emerald core chamber and a substantially redesigned hardware-style puzzle board: raised pads, dimensional data/cursor cubes, illuminated connections, a command deck, clear counters, touch feedback and contextual trace warnings. Its Home replaces the bird with an animated top-down data-core display. Puzzle decisions remain turn-based.
- Ambient motion and portal/scenery effects respect Reduced Motion and pause. The three modes retain their separate session/results and the existing daily-mission reward rules.

## Verification

The full isolated Unity suite passes, including the original gameplay/save/economy regressions, all four Relay missions and four solvable Blackout puzzles, daily-claim recovery, legacy saves, roster/tech contracts, dropdown controls and Reduced Motion. Width assertions compare every new screen to the same Endless camera/interface across 750×1334, 1320×2868 and 1640×2360. Home checks confirm only Blackout hides the bird and both new protocols display their distinct hero art. Original source constants/critical methods remain preserved.

Evidence: `artifacts/mode-identity-width-20261012/final-qa/unity.log`, 63 still images and live animation frames. Additional three-mode comparisons are in `comparison-captures/`; all are actual isolated Editor renders, with assisted QA flight. The known Editor Search indexing exception and service diagnostics remain documented, not counted as gameplay failures. No new physical-device pass or signed archive is claimed.

Local previews: [three Home views](../../../artifacts/mode-identity-width-20261012/previews/three-home-views.gif), [three gameplay views](../../../artifacts/mode-identity-width-20261012/previews/three-gameplay-views.gif), [Blackout detail](../../../artifacts/mode-identity-width-20261012/previews/blackout-redesign.png).

Use `python3 mobile/Tools/run_modes_qa.py` for repeatable disposable QA. It uses a separate product/company identity and never resets the real player save; do not run two copies concurrently. QA helpers remain outside shipping Assets.

Before later release, use a new unused build number and repeat the [device checklist](../ThreeModes20261012/DEVICE_CHECKLIST.md), using the badge navigation above. Inspect all three Home views, mode switching, safe areas, pause/resume, Reduced Motion and new texture memory/frame pacing on iPhone and iPad. Existing build 7 footage/sign-offs are historical only. No upload, App Store Connect change, website publication or Apple message is authorized here.

New bitmap origins, full prompts and hashes are in [ASSET_GENERATION.json](ASSET_GENERATION.json); current [provenance](PROVENANCE_ADDENDUM.md), [file ledger](Provenance/FILE_LEDGER.csv) and [candidate record](RELEASE_RECORD.json) retain traceability. The updated [listing](metadata.json) is local/unsent and within the retained field limits.
