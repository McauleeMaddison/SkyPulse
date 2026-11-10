# SkyPulse Arcade

The [10 November wing-flap repair](mobile/Development/FlightAnimation20261110/README.md)
corrects the Editor 3D preview clock under repeated taps, pause/resume and retries.
Actual gameplay regression checks now cover the camera callback instead of relying
on forced-time art poses. New physical-device work remains on hold for visual approval.

The [9 November local polish](mobile/Development/GameplayPolish20261109/README.md)
adds Crystal Echo and Route Lens to Endless Route, with updated help and bounded
once-only rewards. It also gives Chromaflux and Virex clearer source-specific
shapes and checks that all fifteen Studio selections load one correct model.
The approved Finch is preserved; the other fourteen sculptures remain under
visual review. All twelve worlds have a newer local composition candidate in
[the world review guide](mobile/Development/BirdRoster20261104/Previews/World-Revision-2/README.md).
Owner appearance approval and a matching device candidate remain pending.

Open `mobile/` in Unity Hub and the **SkyPulse** repository in VS Code. Start with
[the editing guide](mobile/EDITING_GUIDE.md) for the actual game and model scenes,
code map and preservation rules. [The handoff](mobile/MANUAL_HANDOFF.md) records
the installed build and the work awaiting review.

**Installed on the paired iPhone: version 1.0.0, build 19**, from source
`f6221f68314a864b898a7c7bbe9693bb528316e2`. It was installed directly; build 17 was
last observed in TestFlight. Follow [the current release record](mobile/Release/CURRENT_RELEASE.json)
for its build, QA and device checklist. Owner device acceptance remains open.

**Newer local source: [31 October design candidate](mobile/Development/DesignUpgrade20261031/README.md).**
This includes world and Relay presentation, Home labels, Hangar and Tech Tree
finishes, and native tactile feedback work. It has not been installed or uploaded.
Its source manifests and dated checks apply to that local candidate, not the
installed build. The separate Neon Finch 3D workshop is an art-review prototype;
the Finch appearance has since been approved, while the remaining roster
sculptures need review. Gameplay continues to use the original articulated
bird artwork by default; the solid-model selector is Editor-only.

**Public App Review remains on hold** pending Apple's clarification, current
device/media validation and renewed owner authorization. Local compilation and
Editor previews do not establish phone performance or release acceptance.
[Review status](mobile/Release/REVIEW_STATUS.md).

## Play and edit

1. Open `mobile/Assets/Scenes/SkyPulse.unity`, select a portrait Game view and press Play.
2. Home starts on Endless Route. Tap the top-left mode badge to select Endless,
   Pulse Relay or Blackout, then press **PLAY**. Flight uses tap, Space or Up Arrow;
   pause uses the screen control, Escape or P. **HOW TO PLAY** covers the modes,
   progression and world map.
3. Use the [editing guide's scene map](mobile/EDITING_GUIDE.md#open-the-right-scene)
   for worlds, atmosphere, Relay, pipes and Finch. Workshop Play inspects models;
   gameplay starts from `SkyPulse.unity`.

Endless keeps fixed handling for all 15 catalogue birds. Each passed gate awards
one point. Its twelve worlds change every 15 gates: Neon City, Aurora Rise,
Solar Drift, Midnight Tide, Velvet Dawn, Crystal Night, Jade Horizon, Violet Rain,
Eclipse, Cobalt Storm, Amber Skies and Polar Glow; Neon City returns at score 180.
Crystals bank during the run. Nine upgrade families have three connected level
branches; temporary pickups provide Aegis, Time Pulse, Crystal Magnet, Crystal Echo and Route Lens.

Pulse Relay has four finite circuits refreshed each UTC day; crystals open the
matching gates automatically. Blackout has eight authored Signal Heist vaults,
immediate optical rotation and instant intermediate progression. Their medals
and completion data remain separate from the Endless best. Mission Control has
four UTC-day tasks worth five crystals each, with journaled claim recovery.
Training, Core and breeding rules and prices are documented in the editing guide.
Reduced Motion freezes ambient effects. Existing save identifiers and migration
paths must remain stable. There are no logins, ads, analytics or real-money purchases.

## Project folders

- `mobile/Assets/`: editable Unity scenes, scripts, workshop models and runtime resources.
- `mobile/Packages/` and `mobile/ProjectSettings/`: reproducible Unity configuration.
- `mobile/Tools/`: isolated QA tools, excluded from the normal player.
- `mobile/Development/DesignUpgrade20261031/`: current local previews and source-specific evidence.
- `mobile/Release/`: installed-build records, release drafts and historical provenance.
- `public-site/`: separately versioned privacy/support website, ignored by the game repository.
- `artifacts/`: ignored local captures and archives; preserve release archives, symbols and owner evidence.

Unity caches, logs and Xcode exports are generated outputs. Existing `.meta` files
retain asset identities even when hidden in VS Code Explorer.

## Check a change

Follow [Check an edit](mobile/EDITING_GUIDE.md#check-an-edit) and
[VISUAL_SMOKE.md](mobile/Tools/VISUAL_SMOKE.md). Run save-resetting QA helpers only
in their disposable QA project. Useful read-only checks include
`python3 mobile/Tools/check_player_copy.py` and
`python3 mobile/Tools/verify_bird_registration.py`.

The installed release contract still describes build 19. New source changes need
fresh source-specific QA and a signed candidate tested on physical devices before
updating that contract. Keep App Review submission on hold; local tests cannot
promise Apple acceptance. The earlier [4.3(a) audit](APPLE_4_3A_AUDIT.md) and
release folders apply only to their recorded sources and builds.
