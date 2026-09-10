# SkyPulse Arcade

SkyPulse is an offline, portrait Unity arcade game for iPhone. The shipping source is `mobile/`; open it with Unity **6000.6.0f1**.

## Project layout

- `mobile/Assets/Scripts/`: game loop, UI glyphs, button feedback, tap handling, bootstrap and public links.
- `mobile/Assets/Resources/`: runtime artwork, five sound effects, bird registration data and privacy/support URLs.
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
3. Tap to flap; Space/Up Arrow also work in the editor. Use the pause control or Escape/P to pause.
4. Open Bird Hangar to unlock cosmetic birds, or Upgrades to spend earned crystals.

There is one endless route with fixed handling for all 15 birds. Each passed gate awards one point. Neon City starts the run, Acid Foundry begins at score 15 and Orbital Bazaar at 30; the worlds then rotate every 15 gates with capped difficulty. Crystals bank during the run. Nine upgrade nodes affect collection/rewards, while three temporary pickups provide Aegis, Time Pulse and Crystal Magnet. No login, ads, analytics or real-money purchases are used.

Saved scores, crystals, owned birds, upgrade levels and preferences retain their existing keys and migration paths.

## Verify changes

Run `python3 mobile/Tools/verify_bird_registration.py` to validate all 90 flight frames. For gameplay, persistence, purchases, scrolling and safe-area checks, follow `mobile/Tools/VISUAL_SMOKE.md`; run those harnesses only in an isolated QA project, because they reset the QA save. Use **SkyPulse → Playtest Checklist** for manual testing.

## Prepare a release

Configure the iPhone release through **SkyPulse → Release**. Set an unused iOS build number and signing team, check the configured privacy/support pages, then export **App Store Candidate** into a fresh folder. Build and test that exported candidate on a physical iPhone before archiving and submitting it through Xcode/App Store Connect. Local checks do not establish App Review acceptance.
