# SkyPulse — local visual repairs, 31 October 2026

This source is newer than the installed iPhone **Build 19**. The repair pass has
not been installed, uploaded to TestFlight or submitted to public App Review.
Build 19 and its release records remain unchanged. **Public review stays on hold.**

## Repair scope

- **Birds:** restored the original articulated artwork and removed the rejected
  sculpted replacement. Measured flap silhouettes retain the connected feathers
  and exhaust while excluding detached sheet fragments; original PNGs, bird IDs
  and gameplay dimensions remain intact. Endless Home's stray blue rail is gone.
- **Editor isolation:** workshop scenes remain model inspections. Starting Play
  there no longer adds the game, pooled obstacles or gameplay cameras. Open
  `Assets/Scenes/SkyPulse.unity` to play; this prevents an inspection pipe from
  appearing alongside real obstacles or in Relay.
- **Pipes:** all twelve themes have a genuinely hollow, inclined mouth, inner
  bore and rolled lip. Separate bezels, bores, support ribs and light guides are
  editable Unity objects. Runtime uses the authored bake, including hidden parts,
  fitted to the existing collision dimensions. Moving LEDs respect Reduced Motion.
- **Endless worlds:** seven composed districts per bank replace scattered tower
  placement across all twelve worlds. Layered depth, quieter distant detail and
  world-coloured sparse clouds retain each world's identity and flight corridor.
  Existing milestone timing, handling and obstacle spacing remain unchanged.
- **Relay:** cobalt/violet atmosphere, warm alloy structure and controlled cyan
  lighting separate the connected towers, bridges and deck. The ocean planet,
  crater moon and glacial moon have distinct materials. Gate/crystal rules and
  immediate automatic unlocking remain in the existing gameplay path.
- **Blackout:** smoked optical prisms, dark mounts, restrained beams and clearer
  circuit feedback replace the opaque toy-like finish. Rapid taps retain immediate
  response and forward rotation. Completed systems show truthful replay/refinement
  goals using saved trace and medals. Completion, rewards and puzzle progression
  stay in the existing campaign logic.
- **Hangar and Tech Tree:** original animated birds remain in the 3D docking
  cradle. Connected holographic branches, concise levels and CORE/NEXT locators
  retain direct panning and confirmation before spending.

## Actual previews

- [All twelve worlds and their pipes](Twelve-Worlds.png), with full-size captures
  in `Worlds`.
- [Three gameplay modes](Three-Modes.png), [three Homes](Three-Homes.png) and
  [visual overview](Visual-Preview.png).
- [Twelve editable pipe assemblies](Pipe-Assemblies.png), with individual workshop
  renders in `Pipe-Assemblies`; [Relay celestial materials](Relay-Planets.png).
- Silent motion: [Endless flight](Endless-Flight.mp4),
  [Relay flight](Relay-Flight.mp4), [Blackout Home](Blackout-Home-Motion.mp4),
  [First Light puzzle](Blackout-First-Light.mp4),
  [Split Signal puzzle](Blackout-Split-Signal.mp4),
  [Hangar](Hangar-Motion.mp4) and [Tech Tree](Tech-Tree-Motion.mp4).

These are actual Unity Editor camera captures using disposable QA saves, not
phone recordings or App Store preview submissions. Galleries contain the source
frames without stretching or repainting. Flight/Home clips are sampled at 10 FPS
in simulation time; Hangar, tree and puzzle clips use 30 FPS capture sequences.
Encoding or capture rate does not establish device performance.

## Evidence and remaining acceptance

`QA_RECORD.json` identifies the exact source, completed automated checks and their
scope. Source manifests distinguish the full regression run from any subsequent
focused visual checks. Earlier preview batches and single-change warning records
are superseded; do not combine old evidence with the current source.

The maintained preservation check covers protected gameplay, input, save and
economy methods. Editor compilation stops active SkyPulse Play before domain
reload; wait for compilation and start a fresh session. No owner save reset is
part of this pass.

Physical iPhone visual acceptance, one-thumb comfort, sustained frame times,
memory and thermals remain pending on a newly built candidate. Check all three
Homes, all twelve world transitions, original bird flap poses, pipe joins/LEDs,
Relay charge/passages, Blackout rapid taps/progression, pause/retry, Reduced
Motion and saved progress. These local checks cannot guarantee Apple acceptance.

## Edit the models

Open a named scene under `Assets/Editor/SkyPulsePipeWorkshop/Scenes`. Expand
`Barrel` or `Collar` → `Structural details · v1`, edit the individual objects,
save, then use **Bake this pipe theme into gameplay** on the assembly root.
Keep the physical bezel and bore aligned with the inclined mouth. Workshop Play
is inspection only; return to `Assets/Scenes/SkyPulse.unity` for gameplay.
See [the editing guide](../../EDITING_GUIDE.md) for world and pipe workflows.
