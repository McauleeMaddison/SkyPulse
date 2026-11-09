# Editable 3D bird roster — current local status

The approved Neon Finch is preserved. The other fourteen palettes were approved;
their sculptures remain under owner visual review. Chrome Raven received positive
feedback, and the rest still need an appearance decision in context. The latest
Chromaflux/Virex source-identity correction and its verification are in the
[9 November polish record](../GameplayPolish20261109/README.md).

## Open the correct Unity scene

Gameplay starts from `Assets/Scenes/SkyPulse.unity`.
Use **Tools → SkyPulse → Bird Workshop → 3D roster in actual gameplay** for the
Editor-only visual selector, or **3D bird studio** for orbit/animation inspection.
Neither changes equipped stats, unlocks, progress or the installed phone build.
The Studio selector loads one saved model and disposes the previous instance.

Open `Assets/Editor/SkyPulseBirdWorkshop/Roster/Roster Gallery.unity` to inspect all
fifteen birds as editable scene objects. Each of the fourteen additional birds
has one named folder with its `Name Rig.prefab`, `Geometry.asset` mesh container,
`Finishes.asset` material container and `Name Review.unity` scene. Expand the
prefab to edit individual armour, eyes, feathers and connectors. Keep `.meta`
files and rig joint identities. Source-specific authoring is in
`SkyPulseBirdRosterWorkshop.cs`, `SkyPulseBirdRosterRaven.cs` and
`SkyPulseBirdRosterDesigns.json`. Saved artist edits are guarded.

The approved Finch is in `Assets/Editor/SkyPulseBirdWorkshop/Prefabs/Neon Finch
Rig.prefab`; its workshop and original artwork were not regenerated.

## Worlds and release hold

All twelve latest world compositions, individual views and source-specific checks
are in the [world revision guide](Previews/World-Revision-2/README.md). Their
appearance is not yet approved. No more phone builds/device checks should run
until the owner is satisfied with the local visuals.

The phone remains on Build 19. Runtime 3D Hangar integration remains deferred in
[Pending-Hangar-Integration.patch](Pending-Hangar-Integration.patch); it is not an
active player script. Review all art before applying/baking/retesting a rollout.
Public App Review remains on hold. Newer local features and art are not evidence
of what is in the old selected App Store build.

## Historical evidence

Older images in `Previews/Birds`, `Previews/Raven-Revision`, `Roster-3D.png` and
`Roster-Flight.mp4` describe earlier sculptures, not the current canonical models.
Their dated checks remain under `Checks`; do not present them as fresh captures
or visual approval. The latest pair/identity tests are linked above rather than
adding another set of live bird assets.
