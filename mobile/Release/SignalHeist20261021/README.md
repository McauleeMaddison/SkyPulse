# SkyPulse 1.0.0 (17) — local Signal Heist and depth candidate

Source: `c1c014c3e2b61e355aebd5e96c48444c45583b0c` on `main`. Unity 6000.6.0f1. Public review stays on hold. This local candidate is not uploaded to TestFlight; build 15 remains the latest uploaded build.

## What changed

- **Blackout Protocol / Signal Heist:** eight optical puzzles replace the tile-walking game. Tap glass mirrors and splitters to route energy through every core to the uplink. Live relays open matching shutters. Every rotation spends trace; a lit sensor increases its cost. Extracting a vault saves once and immediately activates the next. Failed attempts restart; unfinished configurations resume. Prior achievements preserve access without granting medals for the new layouts.
- **Three-mode depth:** separate rendered 3D Home displays, smooth fixed-target button feedback, mechanical gate bodies in Endless, hollow crystal-unlocked hoops in Relay and a raised optical console in Blackout. The enclosing Home backplate is removed. Illustrated world/bird assets are retained; this is a depth composition, not a claim that all raster artwork became fully modeled characters.
- **Connected Tech Tree:** the expansive, asymmetric family tree remains one connected graph with 39 branches and 37 junctions. Thicker shaded glass conductors carry a narrow moving light vein; the core has one restrained orbit and nine optical family prisms move subtly. A full-bleed midnight atmosphere, softened field edges, calmer header controls and compact level labels replace competing decoration. Only next eligible prices appear on the map; tapping any family/level shows its exact effect and state in the refined inspector. All 36 family/level touch targets are generous and non-overlapping. Drag on both axes without a wheel or scrollbar. CORE returns smoothly and any fresh touch interrupts it. Existing prices, prerequisites, effects and save identifiers are unchanged; purchases retain both pan coordinates. Hangar restores its own chrome/viewport. Reduced Motion freezes decorative motion without blocking input.
- **Cleanup:** removed the retired Blackout walker, its renderer paths, old Home hero branches, unused legacy mode-progress class and disconnected upgrade-card connection renderer. The game still has one shipping implementation per mode. QA tools remain outside shipping Assets.

## Actual Unity previews

[Free-pan holographic tree, 15 seconds](holographic-tree-preview.mp4) · [Three-mode Home/gameplay walkthrough, 39 seconds](three-modes-3d-preview.mp4)

These are silent Editor development renders, not physical-device footage or App Store preview submissions. The tree video contains 450 unique frames at 30 FPS and visits both axes, the animated projector, upgrade inspection and smooth core return; the full walkthrough shows each Home and actual gameplay. No promotional mockup was substituted for gameplay.

## Verification

`QA_RECORD.json` hashes every runtime script and shader. All **19 required pass markers** were present in the final isolated run with exit 0. Checks include all eight puzzles and increasing solution costs, saved attempts/retries/final result, immediate progression with stale-input fencing, exactly-once rewards, Relay 0/2–2/2 and same-frame collection/collision at simulated 30/60/120 FPS, real two-axis drag events, connected tree topology, purchase guards, save/economy compatibility, safe-area layout, Reduced Motion and warmed allocation measurements.

`SOURCE_MANIFEST.json` has 421 entries. Each matches the fresh local iOS IL2CPP export at `/private/tmp/skypulse-17-finish-20261021/iOS`; `BUILD_INFO.json` identifies the exact commit. Earlier `/private/tmp/skypulse-17-20261021/iOS`, `/private/tmp/skypulse-17-tree-20261021/iOS`, `/private/tmp/skypulse-17-spectral-20261021/iOS` and `/private/tmp/skypulse-17-projector-20261021/iOS` exports are superseded. No signed build 17 archive or upload was performed. A Unity Editor Search startup exception is retained in the log as a vendor warning; intentional medal-save failure injection also logs an expected IOException.

Re-run from the repository root:

```sh
python3 mobile/Tools/run_modes_qa.py
python3 mobile/Tools/verify_endless_preservation.py
python3 mobile/Tools/verify_bird_registration.py
python3 mobile/Tools/check_artwork_ledger.py
python3 mobile/Tools/test_release_metadata.py
python3 mobile/Tools/check_release_metadata.py --require-build 17
```

## Required phone/iPad checks

Test all three modes on physical devices before calling this a release: optical board touch alignment and readability, a full eight-vault playthrough, pause/retry/resume, audio/haptics and Reduced Motion, Relay gates/ground and normal crystal reach, Tech Tree two-axis dragging and purchases, switching Hangar/Tree and retained saves. Profile sustained frame time, thermals, memory and render-target cost on an older supported iPhone and iPad. Editor simulation and zero allocations in measured loops do not establish sustained 60 FPS on devices.

## Store draft and artwork

The saved English (U.K.) description, promotional text, keywords and reviewer notes match this folder. Subtitle and Games → Action/Puzzle categories were saved. The draft is **Prepare for Submission**, still with build 15 selected and historical build 7 media. Game Center was unchecked. Before any later public submission, upload/select the validated candidate, replace media with fresh device evidence and obtain renewed owner authorization after Apple's response. Do not submit the current mixed draft.

No external artwork/model/font/audio was added. The existing 145-image rights ledger and owner statements are retained. Provider output permissions do not prove exclusive ownership or resolve Apple's unidentified private comparator.

## Source map

- `mobile/Assets/Scripts/SkyPulseBlackoutCampaign.cs`, `SkyPulseNativeGame.Blackout.cs`, `SkyPulseHeistBoard.cs` and `SkyPulseBlackoutGraphic.cs`: optical puzzle state, interactions, progression and 3D board presentation.
- `mobile/Assets/Scripts/SkyPulseNativeGame.TechTree.cs`, `SkyPulseTechTreeView.cs`, `SkyPulseTreePanInput.cs`, `SkyPulseTreeTouchFeedback.cs`, `SkyPulseNativeGame.cs` and the `HolographicTree.shader` / `TechTreeField.shader` resources: connected tree, two-axis controls, view alignment, level inspection and purchase integration. The retired `SkyPulseTechConnection.cs` is removed.
- `mobile/Assets/Scripts/SkyPulseDepthStage.cs`, `SkyPulseNativeGame.Depth.cs`, `SkyPulseNativeGame.Cinematic.cs`, `SkyPulseButtonFeedback.cs` and the optical/bird shaders: three-mode depth, Home controls and geometry. Existing flight collision logic remains authoritative.
- `mobile/Tools/SkyPulse*Checks.cs`, `run_modes_qa.py`, `verify_endless_preservation.py` and release-metadata checks: isolated regression coverage and validation.
- `mobile/Release/SignalHeist20261021/`, `CURRENT_RELEASE.json`, `REVIEW_STATUS.md` and root `README.md`: current metadata, evidence, previews and explicit review/device holds.

`CHANGED_FILES.txt` lists the changes since the last pushed build-15 records, including the earlier local cinematic work.
