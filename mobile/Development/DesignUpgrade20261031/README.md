# SkyPulse — 31 October design candidate

Current source review; **not a new installed build**. The iPhone release record remains Build 19 / `f6221f6`. Public App Review stays on hold.

## Start here

- [Current Finch finish](Previews/Finch-Metal-Finish.png), [material close-up](Previews/Finch-Metal-Detail.png) and [moving Unity review](Previews/Finch-Metal-Finish-Motion.mp4). **Latest user-selected focus: metal finish and detail.** Satin cobalt/cyan armour, brushed titanium/champagne fittings, finer machining and restrained highlights. The accepted compact face, every geometry buffer, rig, wing/tail motion and lighting are unchanged. **Wait for owner acceptance before converting the roster or integrating this model into gameplay.**
- [Current Home layouts](Previews/Three-Homes.png), [all three modes](Previews/Three-Modes.png), [twelve worlds](Previews/Twelve-Worlds.png).
- [Hangar and Tech Tree](Previews/Visual-Preview.png), [Relay celestial materials](Previews/Relay-Planets.png).
- [Where to edit](../../EDITING_GUIDE.md). Open the Finch review scene listed in the guide; play the game in `Assets/Scenes/SkyPulse.unity`.

## What changed

- District silhouettes, supported terraces, solid facade structure, depth-specific window/LED intensity and deliberately spaced landmarks. Guarded refresh preserves owner edits, asset identities, pivots and prefab/scene transforms. Twenty-four authored runtime bakes now match their editable source.
- Layered volumetric lower clouds with lit crowns, retained world colour, richer Relay planet surfaces and a separately recorded original ocean albedo.
- Continuous scenery and lighting through Home, start, pause, retry and Reduced Motion transitions.
- Open 3D Hangar cradle, calmer materials and quintic transitions; sharper faceted holographic Tech Tree with interruptible focus.
- Home progress now sits inside its large Hangar/Tech Tree action cards, rather than floating beneath them. Four phone/tablet sizes and all modes checked; Relay and Blackout keep their own layouts.
- Preference-aware native iOS soft/impact feedback replaces the long generic vibration. Existing cooldown and settings retained.
- Only the first Neon Finch has a new 3D art-review rig. This revision reshapes the existing body/head mesh buffers 001–042 and applies a constrained PBR finish to eight existing materials. The wing/tail buffers 043 onward, rig, prefab, animation and all original asset GUIDs remain unchanged. A separate lighting check proves the review camera, hierarchy, rig and renderer bindings are preserved while lighting and a new studio reflection asset are applied. See [sculpture preservation](Checks/Finch-Preservation.json) and [lighting preservation](Checks/Finch-Lighting.json).

The established gameplay handling, pipe spacing/gaps/collision, gate charge rules, puzzle logic, saves, economy and original roster artwork remain protected by automated comparisons.

The compact rounded face is retained from the previous correction (about 38% shorter exposed beak projection). This latest pass changes only the existing workshop shader and eight material profiles; all geometry, scene, rig, clips, lighting and metadata are byte-preserved. New `_BrushStrength` and `_SatinStrength` controls expose the surface finish directly in Unity. No duplicate shader or alternate bird implementation was added.

## Validation and limits

[QA summary](Checks/QA-Summary.json): full isolated Unity suite passed **40 groups**, with no C# compiler errors/warnings. Runtime source hashes still match that suite. The latest focused sculpture/material/lighting checks passed byte/GUID preservation, nonzero-normal regression checks, a material-scope mutation negative control, repeat idempotency, 30/60/120 Hz deterministic pose sampling and zero warm pose allocations. Responsive Home captures cover four phone/tablet sizes. Native bridge compiled for arm64 iOS15 with warnings treated as errors; [copy checks](Checks/Player-Copy.json), [art evidence coverage](Checks/Artwork-Evidence.json) and [protected rules](Checks/Preservation.json) pass.

These are Editor/automated checks. Physical GPU/thermal/startup-memory measurements, iPhone/iPad visual acceptance, native plugin linking in a full export and haptic feel remain open. The 125-part Finch rig is an art-review prototype; merge/optimise draw calls before any gameplay integration. No device install, TestFlight upload, App Store metadata save or public review submission was performed in this pass. Preview video frame rates verify captured motion, not phone performance. Raw earlier failed-run files were excluded from this package.

## Project cleanup

The verified superseded 64-file VisualPolish20261030 preview batch is archived locally outside the active source tree, with byte hashes and recovery instructions in [Cleanup record](Checks/Cleanup-Record.json). Existing signed archives, dSYMs, installed-candidate evidence and editable owner studies remain intact. No gameplay code was proved unused, so none was deleted. Three verified-unused private prototype builders were also removed. Required Unity metadata is retained and hidden only in VS Code Explorer. Current previews are kept here, without raw frame-sequence duplication.
