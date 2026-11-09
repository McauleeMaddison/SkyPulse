# SkyPulse — current local visual review

The approved Neon Finch remains unchanged. The other fourteen **colour palettes**
were approved; their first-pass shapes and surface detail were rejected. The owner
also rejected the first twelve-world preview set. Those older sheets below are
historical review records, not accepted release artwork.

## Current review candidates

- **All twelve worlds — current local candidate, approval pending:**
  [preview guide and individual worlds](Previews/World-Revision-2/README.md),
  [gameplay sheet](Previews/World-Revision-2/Twelve-Worlds-Gameplay.png),
  [Home sheet](Previews/World-Revision-2/Twelve-Worlds-Homes.png),
  [Neon motion](Previews/World-Revision-2/Neon-City-Motion.mp4),
  [Aurora motion](Previews/World-Revision-2/Aurora-Rise-Motion.mp4).
- **Chrome Raven:** [original/reference comparison](Previews/Raven-Revision/Reference-Comparison.png),
  [three-quarter model](Previews/Raven-Revision/ThreeQuarter.png),
  [reverse](Previews/Raven-Revision/Reverse.png), [flap video](Previews/Raven-Revision/Flight.mp4).

The current twelve-world candidate uses three complete staggered districts,
world-entry anchoring, curved facade windows and clear central air with sparse
coloured edge clouds. Its [current evidence and device hold](Previews/World-Revision-2/README.md)
apply to these previews. The lower Neon/Raven verification records describe the
earlier focused package. **No world appearance is approved, and no iPhone/Xcode,
signing, installation or device checks will run until the owner likes the previews.**

Raven has a sectioned fuselage, individually traced closed bevelled plates,
connected armour, fitted triangular optics, machined wing channels and seated
keel lights. Hidden surfaces are authored interpretations of a single-view image.
Its appearance still needs the owner's review before this approach is applied to
the other thirteen unapproved birds. No pixel-identical match is claimed.

## Verification and device hold

The final QA project used the same restored runtime gameplay source as the owner
project. Raven passed 543 focused checks: 39,818 owned vertices, finite closed
manifold meshes, seated optics, preserved joint bindings, deterministic sampling
at 30/60/120 Hz, Reduced Motion and zero warm pose allocations. The city meshes
stay below 31,347 vertices each; 1,168 authoring/runtime files were checked, other
worlds/scenes/prefabs/lighting/metas retained, and repeated authoring was stable.
The final GPU report includes 377 assertions, all twelve worlds/Relay inspection,
90 collision-checked gameplay frames and the separate city inspection camera.
Compilation finished with zero C# errors/warnings. These results establish local
technical checks, not visual approval or physical-device frame rate/thermal limits.

- [Raven evidence](Checks/Raven-Revision.json)
- [City geometry](Checks/Neon-City-Geometry.json) and [actual render/motion checks](Checks/Neon-City-Render.json)
- [Protected gameplay](Checks/Current-Gameplay-Preservation.json) and [guarded asset promotion](Checks/Final-Candidate-Promotion.json)
- [Video decoding](Checks/Current-Media.json) and [appearance status](Checks/Acceptance-Scope.json)

**iPhone: Build 19, unchanged. Public App Review: on hold.** No install, upload,
submission, website update or App Store metadata change occurred. The original
runtime Hangar/training/unlock files were restored while art is unapproved.
Prepared three-bay 3D Hangar code is saved as one non-executable
[deferred integration patch](Pending-Hangar-Integration.patch), outside Assets. The patch uses zero context; after approval, validate it with
`git apply --check --unidiff-zero` before applying/retesting.
Its [earlier prototype checks](Checks/Deferred-Hangar-Prototype.json) do not certify
the revised artwork or a release. Do not apply/bake/install until art is approved;
then rebuild from the final assets and repeat device checks.

## Edit in Unity

Play from `Assets/Scenes/SkyPulse.unity`. The model workshop is a separate scene.
Raven: `Assets/Editor/SkyPulseBirdWorkshop/Roster/Chrome Raven/Chrome Raven Review.unity`.
Finch: `Assets/Editor/SkyPulseBirdWorkshop/Scenes/Neon Finch Art Review.unity`.
Neon City: `Assets/Editor/SkyPulseBackgroundWorkshop/Scenes/01 Neon City Civic.unity`
and the Residential scene. All existing source scene/prefab identities were kept.

The Raven revision only replaces known generated first-pass assets and preserves
saved revised/owner-edited assets on repeat. The city QA refresh only targets
known-generated Neon City buffers. Keep `.meta` files. Use the normal main game
and Editor-only **3D roster in actual gameplay** selector for flight inspection;
that selector changes appearance, not equipped stats, controls or saves.

## Historical first roster review

The approved Finch is preserved. The other fourteen catalogue birds now have
individual solid 3D bodies, helmets, fitted optics, beaks, crests, curved feathers,
mechanical connectors and source-specific materials. They reuse the Finch's
working eight-joint flight rig. Hidden surfaces are authored interpretations of
single-view references; pixel-identical reproduction is not claimed.

## Preview first

- [All fourteen models](Previews/Roster-3D.png) · [animated gallery](Previews/Roster-Flight.mp4).
- [Actual gameplay video](Previews/Gameplay-Preview.mp4): Chrome Raven, Prism Hummingbird and Ionwing, three seconds each. Normal game simulation and an automated pilot are used.
- [All birds in Endless Route](Previews/Endless-Gameplay.png) · [all birds in Pulse Relay](Previews/Relay-Gameplay.png).
- Full portrait captures are in `Previews/Gameplay`; individual close-ups, reverse views and flight videos are in `Previews/Birds`.

These show the actual Unity game scene with the new Editor visual preview active.
The equipped bird's stats and original collider remain in use. This is a local
art-review candidate; **the iPhone still has Build 19**. Do not use these candidate
images as evidence of the currently installed or uploaded build.

## Edit in Unity

Open `Assets/Editor/SkyPulseBirdWorkshop/Roster/Roster Gallery.unity` to inspect
all fifteen birds, including the preserved Finch. Use Scene view to orbit freely.
Each of the other fourteen birds has its own named folder containing only:

- `Name Rig.prefab`: eight-joint hierarchy with separately selectable, named armour,
  feathers, eye layers, connectors, talons and tail objects.
- `Geometry.asset`: named mesh sub-assets in one container.
- `Finishes.asset`: individual material sub-assets in one container.
- `Name Review.unity`: dedicated model-inspection scene.

Open a prefab in Prefab Mode to move, rotate or scale individual parts. Expand the
material container to edit enamel, metal, iris, glow or glass. Keep `.meta` files.
**Create remaining reference roster** is a no-op for saved prefabs and preserves
manual owner edits. Native-pixel design measurements live in
`Assets/Editor/SkyPulseBirdRosterDesigns.json`; the shared authoring code is
`SkyPulseBirdRosterWorkshop.cs`. Existing approved Finch assets were not regenerated.

For a live gameplay inspection, open **Tools → SkyPulse → Bird Workshop → 3D roster
in actual gameplay**, open the normal `Assets/Scenes/SkyPulse.unity` scene and
enter Play. Enable **Show 3D model** and pick a bird, or use the equipped bird.
Then start Endless Route or Pulse Relay normally. The selector changes the Editor
visual only. It does not unlock birds, change equipment, write player preferences
or enter Blackout. Stop the preview to restore the original presentation.

The owner’s Unity window was returned to the actual `Assets/Scenes/SkyPulse.unity`
scene and checked through native screen access. Its model workshop is separate
from the playable game; no new phone installation has occurred.

## Validation and release limits

Focused Unity 6000.6 Metal checks passed for all fourteen models: retained nonempty
mesh buffers, finite normals, closed/manifold edges, physical eye support and
outward layer order, supported shaders, eight joints, deterministic 30/60/120 Hz
sampling, Reduced Motion, zero warm pose allocations and repeat-authoring byte
preservation. The models have 27,144–33,218 owned vertices each, plus the existing
shared jet where used. This is a prototype geometry bound, not device profiling.

The actual game booted and rendered all fourteen models in both flight modes
(28 portrait captures). Tests check unchanged bird Y/velocity and hitbox transforms
when selecting a visual, reuse of the active rig, and restoration when stopped.
Three gameplay clips and fourteen individual flight clips decode with distinct
samples. [Studio evidence](Checks/Studio.json), [gameplay evidence](Checks/Gameplay.json),
[preservation](Checks/Preservation.json) and [video verification](Checks/Media.json)
are included. C# compilation finished with zero errors/warnings. Unity's disposable
batch project also logged a SearchDatabase startup exception; it did not prevent
these game/geometry checks and is not a clean-Console certification.

New roster visual acceptance, physical iPhone/iPad appearance, sustained GPU/thermal
profiling, and gameplay integration in an exported build remain pending. The editable rigs retain individually selectable renderers. A separate
Editor baker combines by joint/material for runtime display; its initial
optimized outputs remain isolated in QA and are not approved release assets. Public App Review remains on hold. No upload,
website/App Store copy update, device installation or review submission occurred.

## Source and individual previews

| Bird | Original artwork | Unity model |
|---|---|---|
| Chrome Raven | [Reference](../../Assets/Resources/SkyPulse/characters/roster/steel-frame-04-v1.png) | [3D](<Previews/Birds/Chrome Raven/ThreeQuarter.png>) · [Flight](<Previews/Birds/Chrome Raven/Flight.mp4>) |
| Prism Hummingbird | [Reference](../../Assets/Resources/SkyPulse/characters/roster/prism-frame-04-v1.png) | [3D](<Previews/Birds/Prism Hummingbird/ThreeQuarter.png>) · [Flight](<Previews/Birds/Prism Hummingbird/Flight.mp4>) |
| Koiwing Glider | [Reference](../../Assets/Resources/SkyPulse/characters/roster/cinder-frame-04-v1.png) | [3D](<Previews/Birds/Koiwing Glider/ThreeQuarter.png>) · [Flight](<Previews/Birds/Koiwing Glider/Flight.mp4>) |
| Verdant Kite | [Reference](../../Assets/Resources/SkyPulse/characters/roster/verdant-frame-04-v1.png) | [3D](<Previews/Birds/Verdant Kite/ThreeQuarter.png>) · [Flight](<Previews/Birds/Verdant Kite/Flight.mp4>) |
| Solara | [Reference](../../Assets/Resources/SkyPulse/characters/roster/newbird01-frame-04.png) | [3D](<Previews/Birds/Solara/ThreeQuarter.png>) · [Flight](<Previews/Birds/Solara/Flight.mp4>) |
| Astra | [Reference](../../Assets/Resources/SkyPulse/characters/roster/newbird02-frame-04.png) | [3D](<Previews/Birds/Astra/ThreeQuarter.png>) · [Flight](<Previews/Birds/Astra/Flight.mp4>) |
| Voltcrest | [Reference](../../Assets/Resources/SkyPulse/characters/roster/newbird03-frame-04.png) | [3D](<Previews/Birds/Voltcrest/ThreeQuarter.png>) · [Flight](<Previews/Birds/Voltcrest/Flight.mp4>) |
| Chromaflux | [Reference](../../Assets/Resources/SkyPulse/characters/roster/newbird04-frame-04.png) | [3D](<Previews/Birds/Chromaflux/ThreeQuarter.png>) · [Flight](<Previews/Birds/Chromaflux/Flight.mp4>) |
| Virex | [Reference](../../Assets/Resources/SkyPulse/characters/roster/newbird05-frame-04.png) | [3D](<Previews/Birds/Virex/ThreeQuarter.png>) · [Flight](<Previews/Birds/Virex/Flight.mp4>) |
| Borealis | [Reference](../../Assets/Resources/SkyPulse/characters/roster/newbird06-frame-04.png) | [3D](<Previews/Birds/Borealis/ThreeQuarter.png>) · [Flight](<Previews/Birds/Borealis/Flight.mp4>) |
| Roselight | [Reference](../../Assets/Resources/SkyPulse/characters/roster/newbird07-frame-04.png) | [3D](<Previews/Birds/Roselight/ThreeQuarter.png>) · [Flight](<Previews/Birds/Roselight/Flight.mp4>) |
| Pyrestorm | [Reference](../../Assets/Resources/SkyPulse/characters/roster/newbird08-frame-04.png) | [3D](<Previews/Birds/Pyrestorm/ThreeQuarter.png>) · [Flight](<Previews/Birds/Pyrestorm/Flight.mp4>) |
| Nightveil | [Reference](../../Assets/Resources/SkyPulse/characters/roster/newbird09-frame-04.png) | [3D](<Previews/Birds/Nightveil/ThreeQuarter.png>) · [Flight](<Previews/Birds/Nightveil/Flight.mp4>) |
| Ionwing | [Reference](../../Assets/Resources/SkyPulse/characters/roster/newbird10-frame-04.png) | [3D](<Previews/Birds/Ionwing/ThreeQuarter.png>) · [Flight](<Previews/Birds/Ionwing/Flight.mp4>) |
