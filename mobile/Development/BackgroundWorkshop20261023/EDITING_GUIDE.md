# SkyPulse — visual background workshop

For the additional reusable pipe assemblies and distant skylines added on
24 October, see `../AtmospherePipes20261024/EDITING_GUIDE.md`. The original
district and Relay editing scenes described below remain available.

This workshop lets you edit the real 3D structures in Unity's Scene view. You do
not need to edit C# to move, rotate, resize, duplicate or remove a building,
island, bridge or Relay structure. Code still handles flight, animation,
parallax, transitions and mobile pooling.

## Start here

1. Open the SkyPulse Unity project with Unity 6000.6.0f1. Stop Play mode.
2. In the Project panel, open `Assets/Editor/SkyPulseBackgroundWorkshop/Scenes`.
3. Open **01 Neon City Civic.unity**. Expand its background root in the Hierarchy.
4. Select a building, cliff island, promenade, bridge or garden. Use Unity's Move,
   Rotate and Scale tools. Press **F** in the Scene view to frame your selection.
5. Save the scene. Select the background root and press **Bake this background
   into gameplay** in the Inspector. Restart Play mode in `Assets/Scenes/SkyPulse.unity`.

There are 24 district scenes: civic and residential arrangements for each of the
12 worlds. Edit both arrangements when changing a world's overall style. The
main game mixes them across its depth layers. There is also **25 Pulse Relay
Station.unity**, with actual station ribs, turbines and drones as separate meshes.

## What Bake does

- Endless: combines the edited objects into one mobile mesh per arrangement.
  Moved/resized islands carry their LED light-spill positions into the bake.
- Relay: saves edited geometry and offsets while retaining each existing
  structure's animation. Disabled/deleted original structures stay hidden.
  Duplicated structures with new names are supported as additional static scenery.
- Baked data goes in `Assets/Resources/SkyPulse/BackgroundEdits`. The game loads
  it on the next run. Without a bake, it uses the existing generated background.
- Authoring scenes, prefabs and source meshes are under `Editor`; they are not
  added to the app's scene list or shipped as hundreds of scene objects.

Keep Relay structures directly under the station root. Duplicate an existing
object as a starting point so its vertex colours/material response are preserved.
A district must remain under 60,000 vertices. Bake rejects an empty district or
one over this budget. Inspect the result in the actual portrait game afterward.

The separate distant Relay vista is still artwork. The selectable foreground
station ribs, turbine blades and maintenance drones are genuine mesh geometry.
The ground and gate gameplay remain in the game scene, not in the background-only
authoring scene. A background scene is an editing workspace, not a playable level.

## Shape editing versus arrangement

Objects can be arranged with normal Unity tools. Their surfaces are saved Mesh
assets; the Scene view does not itself provide a mesh-sculpting tool. For custom
vertex/shape modelling, edit/replace a selected Mesh using your preferred Unity
modelling tools, then Bake. Keep the existing shared material and vertex colours
when possible. Buildings contain their facade detail as a single building mesh,
rather than a separate GameObject for every window.

## Undo, safety and source

Save a copy of an authoring scene before experimenting. Ordinary Unity Undo works
while editing; baking saves a separate compiled resource. To restore generated
geometry, remove only that world's baked mesh and lighting assets from
`Assets/Resources/SkyPulse/BackgroundEdits`; remove `relay.asset` to restore the
original Relay layout. Keep your authoring scenes so your edits are not lost.

The original game scene, rules, flight width, collision geometry, saved progress
and economy are not edited by the workshop. Scenery is decorative, not collision
geometry. Test the final phone performance after making substantial additions.

The project copy supplied on the Desktop includes `Assets`, `Packages` and
`ProjectSettings`. Add its **Unity Project** folder in Unity Hub to open that saved
copy. It has no Library cache, signing credentials, Xcode export or App Store
submission. The ZIP is a second backup of the same editable project. The live
working project remains `/Users/mcauleemaddison/Desktop/SkyPulse/mobile`.

## Relevant files

- Scene/mesh generation and Bake: `Assets/Editor/SkyPulseBackgroundWorkshop.cs`.
- Authoring component: `Assets/Scripts/SkyPulseBackgroundAuthoring.cs` (Editor only).
- Endless district generator: `Assets/Scripts/SkyPulseWorldArchitecture.cs`.
- Endless rendering/pooling: `Assets/Scripts/SkyPulseFlightEnvironment.cs`.
- Relay geometry/animation: `Assets/Scripts/SkyPulseRelayEnvironment.cs`.
- Compiled lighting and Relay edit data: `SkyPulseBackgroundLighting.cs` and
  `SkyPulseRelayBackgroundEdits.cs`.

The workshop covers the twelve Endless world backgrounds and the Relay station.
Blackout's interactive Signal Heist board and the mode Homes remain separate game
systems; their complete source is included in the project backup.
