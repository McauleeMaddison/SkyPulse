# SkyPulse — 31 October design candidate

Current source review; **not a new installed build**. The iPhone release record remains Build 19 / `f6221f6`. Public App Review stays on hold.

## Start here

- [Revised Finch](Previews/Finch-04-three-quarter.png) and [moving Unity review](Previews/Neon-Finch-Mechanical-Preview.mp4). Smaller recessed mechanical optics; accepted body, feathers, wing/tail motion and rig preserved. This isolated art candidate is not the gameplay bird. **Wait for owner acceptance before converting the roster.**
- [Current Home layouts](Previews/Three-Homes.png), [all three modes](Previews/Three-Modes.png), [twelve worlds](Previews/Twelve-Worlds.png).
- [Hangar and Tech Tree](Previews/Visual-Preview.png), [Relay celestial materials](Previews/Relay-Planets.png).
- [Where to edit](../../EDITING_GUIDE.md). Unity is open in the Finch review scene; play the game in `Assets/Scenes/SkyPulse.unity`.

## What changed

- District silhouettes, supported terraces, solid facade structure, depth-specific window/LED intensity and deliberately spaced landmarks. Guarded refresh preserves owner edits, asset identities, pivots and prefab/scene transforms. Twenty-four authored runtime bakes now match their editable source.
- Layered volumetric lower clouds with lit crowns, retained world colour, richer Relay planet surfaces and a separately recorded original ocean albedo.
- Continuous scenery and lighting through Home, start, pause, retry and Reduced Motion transitions.
- Open 3D Hangar cradle, calmer materials and quintic transitions; sharper faceted holographic Tech Tree with interruptible focus.
- Home progress now sits inside its large Hangar/Tech Tree action cards, rather than floating beneath them. Four phone/tablet sizes and all modes checked; Relay and Blackout keep their own layouts.
- Preference-aware native iOS soft/impact feedback replaces the long generic vibration. Existing cooldown and settings retained.
- Only the first Neon Finch has a new 3D art-review rig. Its second revision changes 11 existing head meshes; no other model, material, scene, prefab or animation asset changes in that revision.

The established gameplay handling, pipe spacing/gaps/collision, gate charge rules, puzzle logic, saves, economy and original roster artwork remain protected by automated comparisons.

## Validation and limits

[QA summary](Checks/QA-Summary.json): full isolated Unity suite passed **40 groups**, with no C# compiler errors/warnings. Runtime source hashes still match that suite. Focused head revision checks passed byte/GUID preservation, repeat idempotency, 30/60/120 Hz deterministic pose sampling and zero warm pose allocations. Responsive Home captures cover four phone/tablet sizes. Native bridge compiled for arm64 iOS15 with warnings treated as errors; [copy checks](Checks/Player-Copy.json), [art evidence coverage](Checks/Artwork-Evidence.json) and [protected rules](Checks/Preservation.json) pass.

These are Editor/automated checks. Physical GPU/thermal/startup-memory measurements, iPhone/iPad visual acceptance, native plugin linking in a full export and haptic feel remain open. The 125-part Finch rig is an art-review prototype; merge/optimise draw calls before any gameplay integration. No device install, TestFlight upload, App Store metadata save or public review submission was performed in this pass. Preview video frame rates verify captured motion, not phone performance. Raw earlier failed-run files were excluded from this package.

## Project cleanup

The verified superseded 64-file VisualPolish20261030 preview batch is archived locally outside the active source tree, with byte hashes and recovery instructions in [Cleanup record](Checks/Cleanup-Record.json). Existing signed archives, dSYMs, installed-candidate evidence and editable owner studies remain intact. No gameplay code was proved unused, so none was deleted. Required Unity metadata is retained and hidden only in VS Code Explorer. Current previews are kept here, without raw frame-sequence duplication.
