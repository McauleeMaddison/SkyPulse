# SkyPulse — 31 October design candidate

Current source review; **not a new installed build**. The iPhone release record remains Build 19 / `f6221f6`. Public App Review stays on hold.

## Start here

Latest optics/exhaust pass (2 November): [overview](Previews/Finch-Eyes-Jet/Overview.png),
[eye close-up](Previews/Finch-Eyes-Jet/Eyes.png), [flight](Previews/Finch-Eyes-Jet/Flight.mp4),
[blue jet close-up](Previews/Finch-Eyes-Jet/Jet.mp4) and [turntable](Previews/Finch-Eyes-Jet/Turntable.mp4).
Smaller curved optical lenses sit inside continuous helmet pockets with fitted slim
bezels. One seven-volume blue jet replaces the static exhaust lances; downstream
flow, a bright core and moving tips use absolute time and respect Reduced Motion.
[Focused checks](Checks/Finch-Eyes-Jet-Validation.json), [actual GPU proof](Checks/Finch-Eyes-Jet-GPU.json)
and [change preservation](Checks/Finch-Eyes-Jet-Preservation.json) pass. This is art
revision v10, with visual acceptance pending. Other birds remain on hold.

Preceding structural pass (2 November): [3D views](Previews/Finch-3D/Structure.png),
[360° turntable](Previews/Finch-3D/Turntable.mp4), [flight](Previews/Finch-3D/Flight.mp4),
[reference view](Previews/Finch-3D/Reference.png) and [wireframe](Previews/Finch-3D/Wireframe.png).
The closed helmet, supported optical caps, connected foot mounts and deeper wing fan
use real geometry. [Unity structural checks](Checks/Finch-Structure.json) cover 72 closed
solid parts and 9 support overlaps; existing wing X/Y and optical fronts are retained.
**Visual acceptance remains pending.** This is art revision v9, not an App Store
build. The older reference previews and records below are preserved as the preceding
v8 appearance review; they do not describe the latest structural geometry.

- [Finch reference reconstruction](Previews/Finch-Source-Match.png), [fixed-scale source pose](Previews/Finch-Reference-Model.png), [3D view](Previews/Finch-Reference-ThreeQuarter.png), [close-up](Previews/Finch-Reference-Detail.png) and [animated review](Previews/Finch-Source-Motion.mp4). The original Volt artwork now drives the helmet, round optic/gold crescent, swept wings, ivory belly, shoulder and tail/exhaust. **Owner visual acceptance remains pending. Do not convert other birds or integrate this model into gameplay until accepted.**
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
- Only the first Neon Finch has an isolated editable 3D art-review rig. Source-traced mesh buffers 001–125 and eight material profiles replace the previous interpretation. The prefab, scene, all bones/transforms, wing/tail animation, material bindings, original reference card, lighting and every asset GUID remain intact. See [sculpture preservation](Checks/Finch-Preservation.json) and [lighting preservation](Checks/Finch-Lighting.json).

The established gameplay handling, pipe spacing/gaps/collision, gate charge rules, puzzle logic, saves, economy and original roster artwork remain protected by automated comparisons.

The source image is the design blueprint. Seven main feather bodies and their cyan sections use explicit closed cubic outlines with real thickness and bevels, replacing the slender centre-line approximations. Head, optic, beak and shoulder bounds are checked against independently measured native pixels, with 2–3px tolerances. The fixed 1024×768 Unity capture uses the same frame as the original 512×384 image at twice the resolution. The comparison crops the same coordinates from both; birds are not independently fitted or repainted. It is genuine articulated geometry with depth and concealed mounts, not an image-plane substitute. Real 3D lighting and underspecified hidden surfaces mean a pixel-identical reproduction is not established. Superseded narrow-optic/plate helpers and procedural shader scribes have been removed.

## Validation and limits

[QA summary](Checks/QA-Summary.json): full isolated Unity suite passed **40 groups**, with no C# compiler errors/warnings. Gameplay runtime source hashes still match that suite; the isolated art-review rig changed for the jet and passed the new focused Unity checks. The latest focused sculpture/material/lighting checks passed byte/GUID preservation, nonzero-normal regression checks, a material-scope mutation negative control, repeat idempotency, all 33 wing/tail blade coverage masks, 30/60/120 Hz deterministic pose sampling and zero warm pose allocations. Responsive Home captures cover four phone/tablet sizes. Native bridge compiled for arm64 iOS15 with warnings treated as errors; [copy checks](Checks/Player-Copy.json), [art evidence coverage](Checks/Artwork-Evidence.json) and [protected rules](Checks/Preservation.json) pass.

These are Editor/automated checks. Physical GPU/thermal/startup-memory measurements, iPhone/iPad visual acceptance, native plugin linking in a full export and haptic feel remain open. The Finch rig now has 125 sculpted renderers plus one jet renderer and is an art-review prototype; merge/optimise draw calls before any gameplay integration. No device install, TestFlight upload, App Store metadata save or public review submission was performed in this pass. Preview video frame rates verify captured motion, not phone performance. Raw earlier failed-run files were excluded from this package.

## Project cleanup

The verified superseded 64-file VisualPolish20261030 preview batch is archived locally outside the active source tree, with byte hashes and recovery instructions in [Cleanup record](Checks/Cleanup-Record.json). Existing signed archives, dSYMs, installed-candidate evidence and editable owner studies remain intact. No gameplay code was proved unused, so none was deleted. Three verified-unused private prototype builders were also removed. Required Unity metadata is retained and hidden only in VS Code Explorer. Current previews are kept here, without raw frame-sequence duplication.
