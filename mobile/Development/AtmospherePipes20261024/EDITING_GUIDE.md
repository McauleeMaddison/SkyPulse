# SkyPulse — pipes and distant atmosphere, 24 October 2026

Open the live project at `/Users/mcauleemaddison/Desktop/SkyPulse/mobile` in
Unity 6000.6.0f1. The Desktop workshop dated 23 October is a separate backup;
editing that copy does not change this project.

## Buildings, islands and bridges

The original 25 editing scenes remain in
`Assets/Editor/SkyPulseBackgroundWorkshop/Scenes`. Open a scene, expand its root
in the Hierarchy and select the named structure. Use Move (W), Rotate (E),
Scale (R), and F to frame the selection. Save, select the district root, then
press **Bake this background into gameplay**. Test in `Assets/Scenes/SkyPulse.unity`.

Each world has civic and residential arrangements. The Relay station has its
own scene. These are editing workspaces; they are not playable game scenes.
Moving a building does not require changing C# or configuring an input system.

## New distant skyline scenes

Open `Assets/Editor/SkyPulseBackgroundWorkshop/Distant Scenes` for twelve
additional world-specific skylines. Their buildings and supports are separate
objects, placed behind the original districts at runtime. Save and Bake the
root to produce `Assets/Resources/SkyPulse/BackgroundEdits/world-XX-horizon.asset`.

The atmosphere also contains two gently curved mist layers and nine small
colour-matched LED stars per world. These are pooled effects controlled by
`SkyPulseAtmosphereDepth.cs`; the sky stars are glowing transparent surfaces at
different depths, rather than sculpted landmark meshes. The enclosing sky
palette remains in `WorldAtmosphere.shader`.

## New reusable pipe assemblies

Open `Assets/Editor/SkyPulsePipeWorkshop/Scenes`. There is one scene and prefab
per world, containing separate **Barrel** and **Collar** mesh objects. Select
the root to edit Accent, Energy, Metal, Light Rate and Detail in the Inspector.
The material preview is static; run the actual game to see LED animation.

Save and press **Bake this pipe theme into gameplay**. The bake saves a shared
theme and two meshes under `Assets/Resources/SkyPulse/PipeEdits/world-XX*`.
Every pooled gate in that world reuses these resources. Baking fits the art to
the original bounds so flight width, spacing, gaps and colliders stay intact.

Keep the two named mesh objects. To change their shape, edit or replace their
readable Mesh assets with your preferred modelling tools; normal Scene tools
arrange objects and do not sculpt individual vertices. Preserve vertex colours
and normals, which encode the metal, seals and LED regions. Each part must
remain under 24,000 vertices. Extra child meshes are not included by this pipe
bake, so incorporate added geometry into the named Barrel or Collar mesh.

## Safe iteration

Save a scene copy before experimenting. Opening a scene does not apply it to
gameplay; Bake is the explicit step. Restart Play mode after baking. To return
one world to generated art, move that world's baked resources out of the
Resources folder while keeping the authoring scene and meshes as your backup.
The creation menus refuse to regenerate over existing editing scenes.

The workshop folders are Editor-only. The game ships cached combined meshes,
not these editing scenes. Heavy additions still require a real-device frame
time and visual check.

## Console and the Input Manager

The Console contains logs, warnings and errors; its counters are not a count of
broken scene objects. The current project uses Unity's legacy Input Manager for
gameplay taps, mouse/keyboard input and UI navigation. Unity recommends its newer
Input System for new work, but switching an existing game needs a separate
migration and control tests. Neither system is needed to arrange scenery in
Scene view. Do not change Active Input Handling merely to remove a notice.

The Blackout miniature is assigned at runtime and is now explicitly
nonserialized, removing its unnecessary scene-serialization warning.

## Preview scope

The new galleries are real Unity renders, and motion clips show scripted QA
piloting through the actual simulation. They are not new iPhone recordings or
App Store preview uploads. Relay's station ribs, turbines, deck and drones are
3D; its distant station vista remains artwork. This update does not submit the
app, install on the phone, or merge the development branch into main.
