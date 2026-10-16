# Relay free-flight polish — build 15

Pulse Relay keeps free tap-to-flap flight. Crystals are randomly placed ahead of each numbered gate; there is no imposed climb/dive pattern or visible guide path. Daily seed data makes a day's random placement stable across retries/relaunches, with fresh circuits each UTC day. Gate heights are random across a broader usable view (centres from -4.8 to +3.0 world units), with an approach reach limit between neighbours. There is no forced direction pattern. The compact HUD is anchored to the actual safe top edge, separate from centred menu layout; crystal/gate counts, hint and pause are consolidated inside it. Both matching crystals are now required: 0/2 and 1/2 remain locked, 2/2 unlocks immediately and spends the assigned pair once. A shield cannot bypass this prerequisite. Other gates' crystals cannot unlock it. The gate label and flight hint show the requirement.

The gameplay hoops and green exit use hollow 3D mesh hardware with a recessed bore, lighter metal finish, curved outer housings, raised collars and inset moving light. Rear surfaces draw behind the pilot and near lips draw in front, so the bird passes inside. The 3.7-unit aperture is smaller than build 13's 4.5-unit opening; the visible bore and collision test agree. The flight camera/width and bird forces are preserved. A scrolling runway has its bright collision edge at the existing `GroundY = -8.45`; an unprotected fall still fails. Reduced Motion and pause freeze the relevant animation.

The bird's authored flap controller already worked in actual gameplay. The rejected initial preview held the bird level. The current GIF and 14-second MP4 run the real Flap, physics, pickup, collision, wing-state and gate-progression paths. They demonstrate several wing poses and successive gates. They are Unity development renders using disposable progress and an automated tap pilot, **not device footage or a store App Preview**. Home retains its cinematic portal artwork; this replacement targets gameplay hardware.

## Changed implementation

- `Assets/Scripts/SkyPulseRelayGate.cs` and `Assets/Resources/SkyPulse/Shaders/RelayTitanium.shader`: hollow meshes, near/far layering, bevel/trim/housing geometry, metal reflections and embedded emitters. Created at course setup; meshes/materials are released with their gate.
- `Assets/Scripts/SkyPulseRelayDeck.cs` and `Assets/Resources/SkyPulse/Shaders/RelayRunway.shader`: visible runway/slab and scrolling panels, with its front edge at the actual ground height.
- `Assets/Scripts/SkyPulseMissionModels.cs`: random legacy-mission pickup positions, required matching pair, counts and smaller Relay aperture. Existing save/wallet keys and reward amounts are untouched.
- `Assets/Scripts/SkyPulseDailyRelay.cs`: separately seeded random crystal positions within reachable approach windows; broader random gate-height targets with a reachable neighbour limit; existing spacing retained. No Unity global random-state consumption.
- `Assets/Scripts/SkyPulseNativeGame.Presentation.cs`: mode-only mesh/deck integration, random pickups, no forced guide lines, cached 0/2–2/2 labels, immediate energy-window clearance and animation clock.
- `Assets/Scripts/SkyPulseNativeGame.Missions.cs`, `.Controls.cs`: prerequisite-aware collision, safe-top compact HUD, cached charge hint and accurate two-crystal instructions.
- `ProjectSettings/ProjectSettings.asset`: build 15. Matching Unity `.meta` files are included.
- `Tools/SkyPulseRelayGateChecks.cs`, `SkyPulseOneThumbChecks.cs`, `SkyPulseDailyRouteChecks.cs`, `SkyPulseMissionChecks.cs`, `SkyPulsePresentationChecks.cs` and `run_modes_qa.py`: isolated regressions, real-flapping preview and four-render pilot-occlusion proof.

## Validation and limits

The final free-flight/random-crystal Unity run exited 0 with all 15 required markers. The separate lock supplement also exited 0. Cases cover 0/2 and 1/2 failure even under shield immunity, final-crystal/same-frame impact success, exactly-once accounting, out-of-order pairs, retries, pause, Reduced Motion, UI/touch fences, real pilot-pixel occlusion, visible-ground failure, 30/60/120 FPS course reachability, all Blackout progression/failure/idempotency cases and legacy saves/economy. A year of generated daily layouts is checked for determinism, freshness and reachable approach positions. Automated tap flight is not a human comfort test.

All 21 runtime scripts and both shaders match the passed QA project. Exported source/configuration is recorded in `SOURCE_MANIFEST.json`. Nine metadata regressions, 90 bird frame registrations, existing artwork hashes and protected Endless methods pass. Measured warmed gate/presentation loops allocate no managed memory. The retained log includes the known Unity Search startup indexing exception; game markers and process exit passed. Full-frame GPU time, sustained FPS/heat, clipping, small-screen readability and actual iPhone/iPad comfort still require device testing. See `QA_RECORD.json`, `BUILD_INFO.json`, `METADATA_CHECKS.json` and `RELEASE_RECORD.json` for exact delivery evidence.

No new raster art, audio, font, purchased model or external code sample was introduced. Mesh/shader source with AI coding assistance is preserved in Git. Existing ARTA/OpenAI evidence and its limits still apply; no exclusive rights or Apple comparator clearance is claimed.

Public App Review stays on hold pending Apple's clarification and renewed owner authorization. `APP_PREVIEW_BRIEF.md` prepares the requested product-page video; fresh build 15 device footage remains necessary. Old build 7 media is historical.

The fresh native archive passed strict signing checks and upload succeeded. Apple reports the recurring vendor UnityRuntime dSYM warning (UUID `392D7A7F-6A4F-3A7A-8788-4089FA96FF38`). App/UnityFramework symbols and a durable archive ZIP are retained; engine-level crash symbolication is limited. This is not claimed fixed.

Build 15 is processed and **Testing in SkyPulse Internal**, with one internal invitation. Its What to Test text was saved and matched after reload. See testflight-build15-testing.png and testflight-notes-saved.png. Physical iPhone/iPad gameplay, sustained performance and current marketing footage remain pending.

The public draft now selects build 15; its description and reviewer notes matched after separate navigation, and its status remains Prepare for Submission. No Add for Review/Submit action was taken. An existing unsupported Game Center flag was found. Its disable control remained loading, so removal is not verified and must be rechecked before public review. This does not block internal TestFlight. See public-draft-build15-saved.png and RELEASE_RECORD.json.
