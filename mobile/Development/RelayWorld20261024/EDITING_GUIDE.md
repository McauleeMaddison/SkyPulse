# Pulse Relay — editable orbital world

Open the live SkyPulse project:
`/Users/mcauleemaddison/Desktop/SkyPulse/mobile` (Unity 6000.6.0f1).
The older Desktop workshop is a separate project copy.

## Open the world

In the Project panel, open:
`Assets/Editor/SkyPulseBackgroundWorkshop/Pulse Relay World/Scenes/Pulse Relay Orbital World.unity`.

Expand **Pulse Relay · Station structures** in the Hierarchy. The suspended
launch hub, two transit towers, four paired concourse levels, bridges, orbital ribs,
arrival apron, service spires, moons and atmosphere are separate objects.
Use W/E/R to move, rotate or scale a selection; F frames it in Scene view.
The scene has an inspection camera. A portrait 9:16 Game view is useful for
comparing its composition with the original illustration; Scene view lets you
orbit freely around the actual geometry.

The original illustration guides the arrangement and materials. This is a
fully modelled reconstruction, with differences in fine detail and lighting;
it is not a pixel-identical reproduction. There is no panoramic station quad
behind the models. The illustration remains available as a reflection and
reference asset. The reusable alloy texture is a material on the geometry,
not a camera-projected background image.

## Save and apply an edit

1. Arrange the objects and save the scene.
2. Select the station root, then press **Bake this background into gameplay**.
3. Open `Assets/Scenes/SkyPulse.unity`, restart Play mode and choose Pulse Relay.

The bake writes meshes, materials and layout offsets to
`Assets/Resources/SkyPulse/BackgroundEdits/relay*`. The game loads the saved
result on the next run. It keeps the existing machine animations and applies
your offsets without accumulating them on each frame. Disabled objects remain
hidden; duplicated objects with unique names become additional static scenery.
Keep structures directly under the root so their offsets remain unambiguous.

Save a scene copy before experimenting. Opening the authoring scene alone does
not apply it to gameplay. Creation menus refuse to regenerate over saved scenes.
The previous 25 station/district editing scenes remain in their original folder;
this dedicated scene is the current Relay-world editing workspace.

## Shapes and materials

Meshes are readable Editor assets under this folder's **Meshes** directory.
Moving a GameObject arranges it. Changing individual vertices requires your
preferred Unity modelling tools or replacing its Mesh. Preserve normals and
vertex colours: the shared finish uses them for metal, glazing and illumination.

Several objects reuse a Mesh or Material. Duplicate that asset and assign it to
one object when you want a unique variant. Material changes are included in Bake.
The hardware materials expose **Surface detail** and **Surface scale** for the
alloy texture. The atmosphere exposes Deep space, Horizon haze and Warm station
glow. The planet exposes cloud-band and rim colours. Scene previews use a static
clock; native gameplay supplies the live animation clock.

The illustrated alloy texture is in
`Assets/Resources/SkyPulse/materials/orbital-alloy-v1.png`. It uses mipmaps,
mirrored tiling, trilinear filtering and an ASTC 6×6 iPhone override. The full
generation prompt and source record are in `MATERIAL_PROVENANCE.md`.

## Gameplay boundaries and validation

These structures have no gameplay colliders. The decorative arrival apron is
part of the background; the actual fixed ground guide and its collision rule
remain in the playable game. Gates, crystals, bird controls, course width,
progression, wallet and saved progress are separate from this workshop.

You do not need to configure Input Manager to edit scenery. It handles the
game's existing controls; Unity's Scene and Inspector tools handle editing.

The environment uses cached meshes and materials, and supports pause and Reduced
Motion. Deterministic 30/60/120 update checks are not device performance results.
Large manual additions still need a real iPhone frame-time, heat and visual check.
No App Review submission or new phone installation is part of this local work.

## Runtime source

- `Assets/Scripts/SkyPulseOrbitalGeometry.cs`: shared model construction.
- `Assets/Scripts/SkyPulseOrbitalWorld.cs`: the layout and depth animation.
- `Assets/Scripts/SkyPulseRelayEnvironment.cs`: perspective camera, machines and bakes.
- `Assets/Editor/SkyPulseBackgroundWorkshop.cs`: scene export and Bake.

There is one active Relay-world implementation. The rejected camera-mapping
experiment is not included in these runtime files.

## Bridge connections

The four transit bridges connect the west transfer tower to the east tower
at the upper, middle, lower and arrival decks. The overhead gantry joins the
launch hub to the upper east deck. Each landing has a physical shoe, lit glazed
entry and under-deck braces. Their default endpoints are generated from the
actual platform positions rather than independently placed bridge lengths.

The connected station shares one motion offset at runtime. Manual edits are
intentionally free: if you move a tower or a deck in Unity, move or resize its
connecting bridges too, then inspect both ends before baking. The bake does
not automatically redesign connections after an artist changes their layout.

## Asset import warnings

Use **Tools → SkyPulse → Background Workshop → Repair workshop asset names**
if an older workshop asset reports that its main-object name differs from its
filename. It changes asset root names, preserves geometry, scene labels and
GUIDs, and leaves local numbered copies alone. New exports already match names.
