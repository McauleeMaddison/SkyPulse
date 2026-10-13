# SkyPulse Arcade

> **Current candidate: build 10.** Relay has automatic numbered crystal gates and tap-only flight; Blackout has eight increasingly demanding sectors, automatic firewall breach and instant saved progression. Mode-specific Home controls and pooled effects are documented in [the current development record](mobile/Release/OneThumbPolish20261013/README.md). TestFlight upload status is recorded there after publication. Build 9 remains historical.

> **App Review hold:** the public version remains Prepare for Submission/unsubmitted. Build 9 and its updated metadata are saved in the draft and available for private TestFlight testing. Await Apple’s clarification and renewed owner authorization before resubmitting. See [current status](mobile/Release/REVIEW_STATUS.md).

SkyPulse is an offline, portrait Unity arcade game for iPhone and iPad. The current source candidate is `mobile/`; open it with Unity **6000.6.0f1**. The 12 October updates add three modes, daily missions, shared tested widths, distinct animated Home/player views and a compact mode dropdown. Build 9 is already in the existing SkyPulse Internal group. Build 10 is the current update candidate; see [its release guide](mobile/Release/OneThumbPolish20261013/README.md) for validation and publication status.

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
3. Home starts on Endless Route. Tap its top-left badge to expand the three free modes, select one, then press **PLAY**. Flight uses tap/Space/Up Arrow; Relay crystals automatically open matching gates. Blackout uses immediate adjacent-node taps and automatic firewall breaches. Use the pause control or Escape/P to pause.
4. Open Bird Hangar to unlock cosmetic birds, or Tech Tree to spend earned crystals.

Endless Route retains fixed handling for all 15 birds. Each passed gate awards one point. Neon City starts the run, Aurora Rise begins at score 15 and Solar Drift at 30; the route continues through Midnight Tide (45), Velvet Dawn (60), Crystal Night (75), Jade Horizon (90), Violet Rain (105), Eclipse (120), Cobalt Storm (135), Amber Skies (150) and Polar Glow (165), returning to Neon City at 180. Worlds change every 15 gates with the existing capped difficulty. The Worlds & Power-ups guide previews all 12 across four pages. Crystals bank during the run. Nine upgrade nodes affect collection/rewards, while three temporary pickups provide Aegis, Time Pulse and Crystal Magnet. No login, ads, analytics or real-money purchases are used.

Pulse Relay adds a tutorial and three finite circuits; Blackout Protocol has eight authored cipher/phase sectors with increasing difficulty and instant progression. Their separate medals and completion data never replace the Endless best. Mission Control tracks four UTC-day tasks worth five crystals each, up to 20 per period, with journaled claim recovery. Home changes its cinematic/circuit/terminal theme with the selected mode during the session; new launches begin on Endless. Relay shares the original cinematic/metal artwork, and Blackout uses animated obsidian lattice geometry and a suspended data core. Reduced Motion freezes ambient effects.

Saved scores, crystals, owned birds, upgrade levels and preferences retain their existing keys and migration paths. New mode, daily and Home-protocol data uses separate versioned keys.

## Verify changes

Run `python3 mobile/Tools/verify_bird_registration.py` to validate all 90 flight frames. For gameplay, persistence, purchases, scrolling and safe-area checks, follow `mobile/Tools/VISUAL_SMOKE.md`; run those harnesses only in an isolated QA project, because they reset the QA save. Use **SkyPulse → Playtest Checklist** for manual testing.

The 29 September audit and world expansion are documented in [APPLE_4_3A_AUDIT.md](APPLE_4_3A_AUDIT.md) and [WorldExpansion/RELEASE_NOTES.md](mobile/Release/WorldExpansion/RELEASE_NOTES.md). They require a fresh candidate and device verification; the preserved build 6 does not contain this expansion.

## Prepare a release

Configure the iPhone and iPad release through **SkyPulse → Release**. Set an unused iOS build number and signing team, check the configured privacy/support pages, then export **App Store Candidate** into a fresh folder. Build and test that exported candidate on a physical iPhone before archiving and submitting it through Xcode/App Store Connect. Local checks do not establish App Review acceptance.
