# Pulse Relay — modelled station reconstruction

The original station illustration now guides a real perspective environment:
suspended launch hub, two detailed transit towers, four paired concourse levels, connected skybridges,
two orbital ribs, an angled arrival apron, service spires, a planet and moons.
The panorama quad and superseded decorative ring implementation are removed.
The world is shared by Relay Home and gameplay.

This is a reconstruction for visual review. The comparison shows remaining
differences in geometry, material detail and lighting. The user selected fully
modelled objects that can be moved and viewed from other angles. The rejected
camera-projection experiment is not part of the runtime implementation.

- [Original versus 3D comparison](Reference-Comparison.png)
- [World render](World.png) and [bridge landing detail](Bridge-Detail.png)
- [Gameplay](Gameplay.png) and [motion](Gameplay-Motion.mp4)
- [Home](Home.png)
- [Unity editing guide](EDITING_GUIDE.md)
- [Validation](VALIDATION.json)
- [Generated alloy material provenance and exact prompt](MATERIAL_PROVENANCE.md)

Open the live project at `/Users/mcauleemaddison/Desktop/SkyPulse/mobile`.
The dedicated workshop is
`Assets/Editor/SkyPulseBackgroundWorkshop/Pulse Relay World`.
It contains a clean editing scene, prefab, persistent mesh assets and materials.
There are no embedded temporary runtime objects in the editing scene.

The new geometry factory and layout live in `SkyPulseOrbitalGeometry.cs` and
`SkyPulseOrbitalWorld.cs`. `SkyPulseRelayEnvironment.cs` owns the camera,
machines and authored layout. `SkyPulseBackgroundWorkshop.cs` exports and bakes
the scene. `SkyPulseRelayBackgroundEdits.cs` now supports optional material
overrides while retaining older layout fields. The shared crystal builder and
protected game rules remain byte-for-byte unchanged.

Automated checks cover scene integrity, persistent assets, movement/material
bakes, hidden and added structures, nonaccumulating offsets, deterministic
30/60/120 updates, pause, Reduced Motion, zero warmed managed allocations and
resource cleanup. The complete gameplay suite also passed. The clean authoring
export was checked after the final Editor-only cleanup correction.

The video is a silent 10-fps Unity QA capture. Real iPhone frame time, heat and
visual acceptance still require device testing. No App Review submission,
TestFlight upload or phone installation was performed. The user requested that
this work and the preceding world improvements be committed and pushed to GitHub main.

The alloy image was generated using the built-in imagegen tool and saved into
`Assets/Resources/SkyPulse/materials/orbital-alloy-v1.png`. Its exact prompt,
reference and source record are linked above and included in the active image
ledger. It is a reusable material texture on real meshes.

## Connected skybridge correction

All four cross-station bridges now derive both ends from matching east/west
landing decks. The launch gantry connects the suspended hub to the upper east
concourse. Glazed entrance frames, inset lights, solid undersides, docking
shoes and cantilever braces make the junctions visible. The station uses one
shared motion offset so animation cannot separate these connections.

The saved scene and runtime at four travel distances verify all five routes.
There are 33 editable/rendered objects, eleven shared model meshes and 191,436
unique vertices. These are geometry budgets, not a measured phone frame rate.

## Terminal, sky and deck finish

The pointed spindle was replaced with a low observation cabin, framed canopy,
compact service pods and a broad engineered underside. Towers now include
external ribs, recessed ventilation and glazed service bays. Twenty-eight
softly animated LED lenses share one background mesh. The sky has layered dust
wisps; planets add oceans, coastlines, clouds, twilight and rim illumination.
The arrival deck has inset guidance lights, framed perimeter rails, service
hatches, ventilation grilles and visible cantilever supports.

[Detail gallery](Station-Details.png) · [Terminal](Terminal-Detail.png) ·
[Arrival deck](Arrival-Detail.png). These are actual Unity renders.

## Console repair

The 15 reported live warnings concerned main-object names that differed from
their Unity asset filenames. The export helper now saves matching names. The
repair corrected 18 Relay assets, then 181 older workshop assets (157 meshes
and 24 materials). Existing geometry and references were compared against Git:
only names and default-zero material serialization changed. Scene object labels
and GUIDs are preserved. The live Console showed one normal repair log, zero
warnings and zero errors without manually clearing it. Repair tools remain
available under Tools → SkyPulse → Background Workshop.
