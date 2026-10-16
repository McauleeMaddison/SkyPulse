# Pulse Relay volumetric gate rebuild — build 13

The owner's 16 October iPhone recording demonstrated the real problem: all of the bird's sprite layers drew over one flat gate image. Gameplay gates and the exit now use actual hollow mesh assemblies. Far metal surfaces render behind the pilot; the camera-facing lip renders in front. The pilot enters the open bore and emerges beyond the near collar. This uses real three-dimensional geometry inside the existing orthographic, sprite-based flight game; it does not convert the whole game or its bird artwork to a fully 3D character/camera system.

The assembly has bevelled titanium, a recessed bore, raised collars, fasteners, inset emitters, moving light sweeps and an unlock impulse. Open gates clear their energy membrane immediately. The hoop's vertical bore retains the tested 4.5-unit collision opening; no mesh colliders, flight forces, course coordinates, charge rules, economy or save keys changed. Normal tap-to-flap, assigned automatic crystal gates, same-frame unlock precedence and retries are retained. Blackout and Endless gameplay remain unchanged from build 12. Home retains its existing cinematic portal artwork; this replacement targets gameplay gates and the green exit.

## Files changed

- `Assets/Scripts/SkyPulseRelayGate.cs`: builds far/near mesh surfaces once per course, animates shader values without allocating, and releases native meshes/materials when the gate is destroyed.
- `Assets/Resources/SkyPulse/Shaders/RelayTitanium.shader`: included mobile shader with normal-based metal lighting, reflections and animated emitters. Uses existing camera/sorting, without an additional render camera or realtime shadow maps.
- `Assets/Scripts/SkyPulseNativeGame.Presentation.cs`: uses the mesh assemblies in Relay gameplay and the exit; raises gate labels clear of the new shell, clears unlocked energy windows immediately and drives the existing pause/Reduced Motion clock.
- Matching Unity `.meta` files and `ProjectSettings/ProjectSettings.asset` (build 13).
- `Tools/SkyPulseRelayGateChecks.cs`, `Tools/SkyPulsePresentationChecks.cs`, `Tools/run_modes_qa.py`: isolated geometry/rendering and lifecycle checks integrated into the existing gameplay suite.

## Validation

Final isolated Unity QA exited 0 with all 15 required markers. It verifies real rendered near-lip occlusion of the pilot, genuine mesh depth, shader support, the retained aperture, no added colliders, destruction on menu/reset and zero managed allocation in warmed gate animation. Existing suites pass same-frame crystal/gate contact, UI/touch separation, retry accounting, course reachability and timing at 30/60/120 FPS, Blackout progression/failure/idempotency, saves and all mode-specific Homes. Pause and Reduced Motion checks pass. The normal-tap course pilot is automated reachability evidence, not a human comfort test.

All 20 runtime script hashes and the shader match the tested QA project. All 383 exported source/configuration entries match the candidate. Nine metadata regressions, 90 bird frame registrations, 200 unique Unity GUIDs and all 145 unchanged artwork-ledger hashes pass. The retained log contains the known Unity Editor Search indexing exception; the required game markers and process exit passed. Native Metal shader export succeeded. See `QA_RECORD.json`, `BUILD_INFO.json`, `SOURCE_MANIFEST.json`, `METADATA_CHECKS.json` and `RELEASE_RECORD.json` for the exact candidate and delivery state.

The PNG/GIF are actual Unity development renders with a controlled passage and disposable progress. They are not physical-device captures or an App Store App Preview. Full-frame device FPS/thermals, clipping, touch comfort, iPhone/iPad appearance and retained real-player progress require testing through TestFlight. No build 13 device pass is claimed.

## Publication and provenance

Public App Review remains on hold pending Apple's comparator clarification and renewed owner authorization. Earlier build 7 store media remains historical. Prepared metadata mentions the genuine 3D Relay hoops and keeps the existing creator, bundle ID, branded links and accurate three-mode features. App Preview preparation is tracked in `APP_PREVIEW_BRIEF.md`; fresh footage of this build is still required.

No new raster image, sound, font, purchased model or external code sample was added. The mesh and shader are authored source with AI coding assistance, preserved in Git. Existing ARTA/OpenAI artwork evidence and its limits remain in `../IdentityRights20261013/`. This work does not establish exclusive rights or resolve Apple's private comparison.
