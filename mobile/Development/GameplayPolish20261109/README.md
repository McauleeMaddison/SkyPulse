# Local gameplay polish — 9 November 2026

This candidate adds two Endless Route pickups and improves the source-specific
appearance of Chromaflux and Virex. Open `Assets/Scenes/SkyPulse.unity` to play.
The new birds are still an Editor-only visual review; their appearance has not
been approved for a phone rollout. The approved Neon Finch is unchanged.

## Controls and new rewards

All pickups activate on contact; no additional gameplay buttons are needed.

| Pickup | Effect |
| --- | --- |
| Crystal Echo | Six seconds; +1 for each collected Endless crystal, capped at six extra per pickup. Refreshing does not stack the duration or budget. |
| Route Lens | Eight seconds of subtle brackets around the next two pipe openings. It does not steer, move gates or change their collision shapes. |

Both pause with the run and clear on retry/Home or a mode change. Echo finds are
banked immediately and listed separately in results. They do not increase the
base used by daily tasks or percentage Tech/bird rewards. Relay gate charge and
Blackout trace are unchanged. Aegis, Time Pulse and Crystal Magnet still work.
The guide now has Survival/Discovery groups with enlarged touch targets. Reduced
Motion also stops pickup bobbing, spinning and orbiting accents.

## Bird identity correction

The screenshots exposed visual similarity, not a wrong dropdown binding or
multiple active birds. The original references themselves share pink/gold.
Chromaflux now has a broad white layered fan, longer swept crown and curved beak;
Virex has narrower pink flight blades, gold edges, compact crown and straighter
beak. Only their existing mesh/prefab files were replaced. Original artwork,
colours, Finch, twelve other sculptures, scenes and existing GUIDs were preserved.
No arbitrary recolouring or duplicate roster asset folders were added.

## Editing map

- `Assets/Scripts/SkyPulseNativeGame.PickupBoosts.cs`: durations, bounded Echo accounting, cached emblems and pooled Lens guides.
- `SkyPulseNativeGame.cs`: existing pickup pool, contact guards, HUD, guide and results wiring.
- `.BirdProgression.cs`: excludes Echo from inherited percentage-reward inputs.
- `.Cinematic.cs`: Lens presentation follows the final simulated pipe pose in LateUpdate.
- `.Depth.cs`: disposes the two generated emblem textures when the game is destroyed.
- `Assets/Editor/SkyPulseBirdRosterRaven.cs`: guarded source-specific sculpture authoring. Artist edits are refused rather than overwritten.
- `Assets/Editor/SkyPulseBirdWorkshop/Roster/Chromaflux` and `Virex`: editable canonical prefab and mesh containers. No new runtime bird export is installed.

## Verification and what remains

See [pickup checks](Checks/Pickup-Validation.json), [bird identities](Checks/Bird-Identity.json),
[strict structure/optics checks](Checks/Bird-Structure.json),
[protected asset promotion](Checks/Bird-Promotion.json) and
[gameplay preservation](Checks/Gameplay-Preservation.json).
The pickup checks exercise actual Unity contact handlers, production fixed-step
simulation under 30/60/120 Hz render cadences, once-only rewards, expiry,
pause/focus/retry, mode separation, icon coverage and small phone/iPad guide
layouts. Lens presentation allocates zero managed bytes over 300 warmed ticks.
The Studio checks switch all fifteen models and confirm one instance at a time.
These are Editor correctness checks, not measured physical-device frame rates.
A copied QA Library produced a Unity SearchDatabase startup exception; this is
not a certification that every Editor Console message is absent.

The owner still needs to approve current world/bird appearance. Do not run new
iPhone/Xcode/signing/archive/install/TestFlight work until that approval. The
phone remains on Build 19. Public App Review remains on hold pending Apple’s
clarification and renewed authorization. Neither the new pickups nor these art
changes are claimed to be in the historical selected App Store build 15.

The [public guide](https://skypulse.macwebstudio.com/guide) explains the pickups
as current development features and distinguishes older installed builds. The
saved App Store listing remains the separately recorded draft; refresh its build,
matching media and final feature copy before any later authorized submission.
