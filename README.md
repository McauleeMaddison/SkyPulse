# SkyPulse Arcade

> **Current local candidate: build 18.** Open 3D Homes replace dark support slabs; pipe and bird lighting share the world palette. Tech Tree levels have clear Installed/Next/Locked labels, Core/training/breeding have visible progression benefits, and Signal Heist has eight saved restoration objectives with non-spending beam/cost planning. The latest pass cleans up pipe shoulder joins and replaces the rectangular exhaust with a soft tapered flight trail. [Current release guide and moving Unity preview](mobile/Release/HolographicPolish20261022/README.md). Final-source scoped Unity checks and the exact-commit iOS IL2CPP export passed. Physical testing remains pending. Build 17 remains available in internal TestFlight; build 18 is not uploaded.

For manual work in Unity and VS Code, start with the [handoff guide and checklist prompt](mobile/MANUAL_HANDOFF.md) and [editing map](mobile/EDITING_GUIDE.md).

> **App Review hold:** public submission remains on hold pending Apple's clarification, current device/media validation and renewed owner authorization. Candidate copy is prepared locally; the public draft is not a matching build 18 submission. [Review status](mobile/Release/REVIEW_STATUS.md).

## Project layout

- `mobile/Assets/Scripts/`: game loop, UI glyphs, button feedback, tap handling, bootstrap and public links.
- `mobile/Assets/Resources/`: runtime artwork, seven authored sound effects, bird registration data and privacy/support URLs.
- `mobile/Assets/Editor/`: art import tools, playtest checklist and iOS release/export setup.
- `mobile/Assets/Scenes/`: the shipping `SkyPulse.unity` scene.
- `mobile/Packages/` and `mobile/ProjectSettings/`: reproducible Unity configuration.
- `mobile/Tools/`: isolated QA harnesses and artwork checks; excluded from the shipping player.
- `mobile/Release/`: App Review drafts, release evidence and historical design/provenance notes. Start with `REVIEW_STATUS.md` for the current review status; older verification applies only to its recorded build.
- `public-site/`: separately versioned privacy/support website, ignored by this game's Git repository.
- `artifacts/`: ignored local test captures and signed archives. Preserve archives and dSYMs for release diagnosis.

The retired browser prototype and its root-level assets have been removed. Runtime assets belong inside the Unity project. Generated `Library`, `Builds`, `Logs`, `Temp` and `UserSettings` folders are ignored; Xcode exports are snapshots, not editable game source.

## Play

1. Add `mobile/` in Unity Hub and open `Assets/Scenes/SkyPulse.unity`.
2. Press Play and use a portrait Game view.
3. Home starts on Endless Route. Tap its top-left badge to expand the three free modes, select one, then press **PLAY**. Flight uses tap/Space/Up Arrow; Relay crystals automatically open matching gates. Blackout uses immediate mirror/splitter rotation taps to power every core and the uplink. Use the pause control or Escape/P to pause. Open **HOW TO PLAY** for mode controls and, in Endless, Tech Tree, Core, training, breeding and the Worlds & Power-ups map.
4. Open Bird Hangar to unlock cosmetic birds, or Tech Tree to explore the connected hologram by dragging in any direction and spend earned crystals.

Endless Route retains fixed handling for all 15 birds. Each passed gate awards one point. Neon City starts the run, Aurora Rise begins at score 15 and Solar Drift at 30; the route continues through Midnight Tide (45), Velvet Dawn (60), Crystal Night (75), Jade Horizon (90), Violet Rain (105), Eclipse (120), Cobalt Storm (135), Amber Skies (150) and Polar Glow (165), returning to Neon City at 180. Worlds change every 15 gates with the existing capped difficulty. The Worlds & Power-ups guide previews all 12 across four pages. Crystals bank during the run. Nine upgrade families, each with three connected level branches, affect collection/rewards, while three temporary pickups provide Aegis, Time Pulse and Crystal Magnet. No login, ads, analytics or real-money purchases are used.

Pulse Relay offers four finite circuits refreshed each UTC day; Blackout Protocol has eight authored Signal Heist optical vaults with increasing trace costs and instant progression. Their separate medals and completion data never replace the Endless best. Mission Control tracks four UTC-day tasks worth five crystals each, up to 20 per period, with journaled claim recovery. Home changes its cinematic/circuit/terminal theme with the selected mode during the session; new launches begin on Endless. Relay uses actual 3D mesh hoops during flight, keeps its cinematic Home artwork, and Blackout uses a raised 3D optical console and a suspended data core. The old flat tile-walking implementation and disconnected upgrade-card renderer have been removed. Reduced Motion freezes ambient effects.

Saved scores, crystals, owned birds, upgrade levels and preferences retain their existing keys and migration paths. New mode, daily and Home-protocol data uses separate versioned keys.

## Verify changes

Run `python3 mobile/Tools/check_release_metadata.py --online --require-build 18` for the current listing, identity, links and exact candidate source checks, `python3 mobile/Tools/test_release_metadata.py` for stale-copy guards, and `python3 mobile/Tools/check_artwork_ledger.py` for complete current image/hash coverage. Update `CURRENT_RELEASE.json` and matching evidence when a future candidate changes; historical metadata must be selected explicitly with `--metadata`. These checks do not certify exclusive rights or Apple's private comparison.

Run `python3 mobile/Tools/verify_bird_registration.py` to validate all 90 flight frames. For gameplay, persistence, purchases, scrolling and safe-area checks, follow `mobile/Tools/VISUAL_SMOKE.md`; run those harnesses only in an isolated QA project, because they reset the QA save. Use **SkyPulse → Playtest Checklist** for manual testing.

The 29 September audit and world expansion are documented in [APPLE_4_3A_AUDIT.md](APPLE_4_3A_AUDIT.md) and [WorldExpansion/RELEASE_NOTES.md](mobile/Release/WorldExpansion/RELEASE_NOTES.md). They require a fresh candidate and device verification; the preserved build 6 does not contain this expansion.

## Prepare a release

Configure the iPhone and iPad release through **SkyPulse → Release**. Set an unused iOS build number and signing team, check the configured privacy/support pages, then export **App Store Candidate** into a fresh folder. Build and test that exported candidate on a physical iPhone before archiving and submitting it through Xcode/App Store Connect. Local checks do not establish App Review acceptance.
