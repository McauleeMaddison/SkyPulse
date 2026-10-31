# Editing SkyPulse in VS Code

Open the **SkyPulse** repository in VS Code and `mobile/` in Unity Hub.
This is the main editing guide. The [current local design candidate](Development/DesignUpgrade20261031/README.md)
is newer than installed iPhone build 19 (`f6221f6`) and has not been installed or
uploaded. Use its source-specific records when reviewing previews. Physical
phone acceptance remains pending; public App Review remains on hold.

## Open the right scene

Paths below are relative to `mobile/`. Open an existing scene rather than
regenerating a workshop over saved edits.

| Work | Scene or folder |
|---|---|
| Play the game | `Assets/Scenes/SkyPulse.unity` |
| Twelve worlds, civic and residential models | `Assets/Editor/SkyPulseBackgroundWorkshop/Scenes/01 Neon City Civic.unity` and the other named Civic/Residential scenes |
| Distant atmosphere for each world | `Assets/Editor/SkyPulseBackgroundWorkshop/Distant Scenes/01 Neon City Atmosphere.unity` and the other named atmosphere scenes |
| Relay station, planet and moons | `Assets/Editor/SkyPulseBackgroundWorkshop/Pulse Relay World/Scenes/Pulse Relay Orbital World.unity` |
| Twelve editable pipe themes | `Assets/Editor/SkyPulsePipeWorkshop/Scenes/01 Neon City Pipes.unity` and the other named pipe scenes |
| First Neon Finch art review | `Assets/Editor/SkyPulseBirdWorkshop/Scenes/Neon Finch Art Review.unity` |
| Gameplay and UI code | `Assets/Scripts/` |
| Shared shaders and runtime resources | `Assets/Resources/SkyPulse/` |

Workshop Play is model inspection. Return to `Assets/Scenes/SkyPulse.unity` to
test gameplay. Preserve the existing scenes, prefabs, materials, animation and
`.meta` identities. Numbered mesh copies are local studies, not cleanup targets.

The Finch remains an isolated prototype. Current acceptance is **the head revision
only**, with inset mechanical optics; keep its body, wings, tail, joints and
animation intact. Stop further Finch or roster work until that head is reviewed.
The prototype is not integrated into gameplay, which retains the original bird art.

## Where to make a code change

Start with `mobile/Assets/Scripts/SkyPulseNativeGame.cs`. All
`SkyPulseNativeGame.*.cs` files extend **one partial game class**. Keep the existing
`SkyPulseNativeGame.cs` component on the game scene; each partial supplies methods
and fields for that same component. Follow calls across the partial files before
changing behaviour.

The `#region` sections appear in VS Code’s Outline and can be folded. On Mac,
use **⌘P** to open a file, **⌘⇧O** to jump to a method, **F12** to follow a call,
and **⇧F12** to find its references.

| Change | File / search term |
|---|---|
| Bird names, original art and catalogue prices | `SkyPulseNativeGame.cs` → `Skins` |
| Tech names, three prices and effect descriptions | `SkyPulseNativeGame.cs` → `Upgrades` |
| Flight gravity and flap force | `SkyPulseNativeGame.cs` → `EndlessTuning` |
| Original flight width, pipe spacing and openings | `SkyPulseNativeGame.cs` → `PortraitPlayfieldAspect`, `PipeSpacingFraction`, `RouteGapFraction` |
| Hangar layout, profile text and swipe animation | `SkyPulseNativeGame.Hangar.cs` → `BuildBirdHangarPage` |
| Training, Core and breeding screens | `SkyPulseNativeGame.BirdProgression.cs` → `OpenBirdTraining`, `OpenCoreProgression`, `OpenBreeding` |
| Real training/breeding rules and costs | `SkyPulseBirdProgression.cs` |
| Tree labels and clickable upgrade targets | `SkyPulseNativeGame.TechTree.cs` → `BuildTechTree` |
| Connected 3D tree branches, Core and halo movement | `SkyPulseTechTreeView.cs` |
| Glossy text finish | `Resources/SkyPulse/Shaders/HolographicText.shader` |
| Holographic score/result cards and labels | `SkyPulseNativeGame.Results.cs`, `SkyPulseScoreGlass.cs`, `Resources/SkyPulse/Shaders/ScoreGlass.shader` |
| Original 3D pipe material and light packets | `SkyPulseNativeGame.Depth.cs`, `Resources/SkyPulse/Shaders/EndlessRouteMetal.shader` |
| Original pipe artwork placement / collider anchors | `SkyPulseNativeGame.PipeVisuals.cs` |
| Tapered exhaust, soft trail and passage sparks | `SkyPulseFlightEffects.cs`, `SkyPulseNativeGame.Cinematic.cs` |
| Pooled 3D scenery during Endless flight | `SkyPulseFlightEnvironment.cs`, `SkyPulseNativeGame.Environment.cs`, `Resources/SkyPulse/Shaders/FlightArchitecture.shader` |
| Endless's live perspective world, sky dome and low clouds | `SkyPulseFlightEnvironment.cs` → `Build`, `SetViewport`, `Architecture`; `WorldAtmosphere.shader`, `WorldComposite.shader` under `Resources/SkyPulse/Shaders/` |
| Mode titles, glass Home controls and footer spacing | `SkyPulseNativeGame.Home.cs` → `ApplyHomeIdentity` |
| Relay station ribs, turbines and drones | `SkyPulseRelayEnvironment.cs` |
| Relay's fixed ground guide and pooled 3D deck panels | `SkyPulseRelayDeck.cs`, `Resources/SkyPulse/Shaders/RelayRunway.shader` |
| Six audio voices, optional cue fallbacks and saved sound switch | `SkyPulseNativeGame.Audio.cs` |
| Native tactile cues and saved haptic switch | `SkyPulseTactileFeedback.cs`, `SkyPulseNativeGame.Controls.cs`, `Assets/Plugins/iOS/SkyPulseTactileFeedback.mm` |
| How to Play topics and route-map entry | `SkyPulseNativeGame.Guide.cs` |
| Three Home layouts and mode input | `SkyPulseNativeGame.Controls.cs`, `.Presentation.cs` |
| Relay gate rules | `SkyPulseMissionModels.cs` → `RelaySession` |
| Signal Heist puzzle rules / presentation | `SkyPulseBlackoutCampaign.cs`, `SkyPulseNativeGame.Blackout.cs`, `SkyPulseHeistBoard.cs` |
| Blackout's city-restoration ribbon and objective telemetry | `SkyPulseBlackoutGraphic.cs` → `Network`, `SkyPulseNativeGame.Blackout.cs` → `BlackoutNetworkCaption` |

Code paths in this table are relative to `mobile/Assets/Scripts`; shader paths
start at `mobile/Assets`. The native plugin path starts at `mobile/`.

## Prices and progression

- A catalogue bird’s `Price` is its **base price**, not necessarily its current
  payable price. Every completed training level adds 2% off future founder-bird
  unlocks, capped at 20%. `BirdUnlockPrice()` supplies the Hangar and purchase
  dialog with the same whole-crystal price. Do not bypass it in a new button.
- Core savings apply separately to Tech upgrades, training and breeding.
  `UpgradeCrystalPrice()`, `TrainingCost()` and `BreedingCost()` calculate those
  prices. Core evolution keeps its own stated price.
- Breeding previews the exact inherited traits, spends once, keeps both parents,
  adds a permanent offspring and equips it. `Preview()` does not spend crystals;
  `Breed()` performs the guarded transaction. Identical owned combinations are
  blocked. Training offspring also contributes to Hangar unlock savings.
- Tech `LevelEffects` are descriptions. The actual reward/collection functions
  must agree with them. Changing the description alone does not change gameplay.

## Visual edits

For pipe LEDs, `packet` in `EndlessRouteMetal.shader` controls the moving bright
section; `_Clock * .48` controls its speed. Cyan/pink masks keep the effect on
the original coloured strips. Reduced Motion freezes the clock and keeps the
lights on. Change the finish without changing pipe collision dimensions.
The collar now has a modelled inclined circular mouth, annular shoulder and
inner bore. Its mesh defines the opening and silhouette; it is not a hole painted
onto a flat cap. The shared shader still uses `_Cutout` to distinguish collars from shafts,
while structural meshes bypass the old artwork
alpha cutout. `_ArtFlip` mirrors the collar for upper pipes; keep that
orientation handling when editing the shader.

The twelve Endless worlds use individually authored civic/residential district
meshes from `Resources/SkyPulse/BackgroundEdits`, viewed by a dedicated perspective
scenery camera behind the unchanged flight camera. Edit the workshop scenes under
`Assets/Editor/SkyPulseBackgroundWorkshop`; bake only the world you intend to
change. `SkyPulseFlightEnvironment` reuses two district banks with three depth
layers. Keep absolute-distance parallax and the collider-free background.

`WorldAtmosphere.shader` grades the authored upper sky and evaluates a bounded
cloud volume below the islands. `SkyPulseFlightEnvironment.BuildCloudNoise`
creates its shared 32-cubed density texture once. World transitions reuse it;
Reduced Motion freezes the cloud clock. Physical iPhone GPU/thermal checks remain
required after changing the volume sampling budget. Relay has its own editable
station structure and materials; it does not reuse Endless's floating districts.

`SkyPulseRelayDeck` draws all bevelled runway panels in one static mesh. Its
shader wraps panel centers using `_Distance`; the front light remains fixed at
the existing `GroundY`. Do not move that light or add collision shapes for visual
polish. Reduced Motion supplies zero distance/clock through the existing path.

Blackout's enlarged opening optics use projected, fixed touch targets. The
city ribbon reads existing saved medals, while partial illumination reads the
live cores and uplink. Planning must never write city completion. Budget,
solutions, saves and immediate progression remain in the existing campaign code.

For the Tech Tree, change text/cues in `BuildTechTree`, and geometry in
`SkyPulseTechTreeView`. Keep fixed touch targets, direct two-axis dragging and
the confirm-before-spending flow. The letters use one shared material, rather
than overlapping text layers; their light sweep does not move the controls.

## Keep existing players’ progress safe

Keep catalogue `Id` values, `PlayerPrefs` keys, the app/company identity and
existing `.meta` GUIDs stable. Unity uses the original script GUID to retain its
scene component. Rename visible labels instead of saved identifiers. Add a
migration deliberately if the save format ever changes.

## Check an edit

1. Save in VS Code, let Unity compile, and resolve Console errors.
2. Play the affected screen/mode. Check pause, retry and Reduced Motion.
3. For prices, test insufficient and exact balances, repeated confirmation,
   and reopening saved progress.
4. Run `python3 mobile/Tools/run_dimensional_qa.py --output artifacts/my-change-qa`
   from the repository root for the isolated regression suite. Use a fresh output
   directory. It creates its own project/save identity; do not copy its save-reset
   helpers into the shipping project.
5. Validate new builds on your phone. Editor renderings do not establish device
   frame rate, thermals, battery use or touch comfort.

Release evidence lives in `mobile/Release/CURRENT_RELEASE.json`. Runtime changes
make its recorded QA/export stale until checked again. App Review submission
remains on hold; editing or compiling locally does not submit anything.

## Bird exhaust and pipe joins

`SkyPulseNativeGame.Cinematic.cs` places the exhaust socket in the bird's body
frame, so it follows the bank. `SkyPulseFlightEffects.cs` samples that socket at
60 Hz and joins a tapered ribbon with shared edge positions. Its fixed buffers
avoid per-frame allocations. Plume length/width are presentation values only.
`FlightLight.shader` feathers this material; `_Soft` defaults to zero so the
existing Signal Heist fibres keep their finish. Reduced Motion removes the
history/sparks and leaves a compact, static, feathered plume.

In `SkyPulseNativeGame.Depth.cs`, the visible shaft seats inside the collar at
22% of its height toward the far shoulder. The hollow collar and shaft meet as
separate 3D meshes. Collision shapes still overlap for reliable gameplay; do not
trim those colliders when adjusting the visible join. The original cyan/pink
artwork and animated LED material remain in use.

## How to Play and world crossfades

`SkyPulseNativeGame.Guide.cs` owns the mode-specific guide and Endless topics for
Tech, Core, training and breeding. The world map is reached from its Worlds &
Power-ups control. Keep help copy aligned with the actual cost/reward functions.

`EndlessRouteMetal.shader` uses `_Visibility` for depth-safe departure coverage.
`UpdateDepthPresentation` derives it from the existing world-transition timer,
matching the departing sprite pool. `SceneLightTint` blends the shared bird/pipe
light across the same transition. Do not change the original flight geometry or
world milestone rules when adjusting these presentation values.

## Small-screen touch targets

`SetComfortHitTarget` in `SkyPulseNativeGame.Controls.cs` gives isolated HUD pause
icons and the Home mode badge an invisible 176-unit touch area without enlarging
their visible artwork. It uses the existing Unity UI raycaster, so Relay excludes
the complete target from tap-to-flap. Do not apply this indiscriminately to dense
controls: expanded targets must not overlap. The mode list and shared Pause rows
instead reserve explicit layout space. Their actual raycasts and safe-area bounds
are covered by `Tools/SkyPulseTouchComfortChecks.cs` in the disposable QA product.

## Bird silhouettes and safe Editor iteration

SkyPulseBirdRig.PrepareSprite queues the measured flap mesh. FlushPending runs
from LateUpdate before rendering; never invoke its native geometry operation
from Awake or an Editor callback. Measured coverage lives in
Resources/SkyPulse/characters/bird-silhouette-coverage.json and is reproducible
with Tools/bake_bird_shapes.swift. Original art, registration and gameplay
collision dimensions stay intact. Simple bird UI images use the same mesh.
The original articulated bird artwork is the gameplay appearance. The separate
Neon Finch workshop is for art review only; its current head-only acceptance
hold is described in the opening scene map.

Assets/Editor/SkyPulsePlaySessionGuard.cs ends Editor Play before compilation.
Wait and press Play again; existing progress loads through ordinary startup.
Tools/SkyPulseReloadChecks.cs verifies a real compile, fresh update loop and
reopening Hangar/Tech Tree in the isolated QA product.

## Edit the actual 3D pipes in Unity

Open `Assets/Editor/SkyPulsePipeWorkshop/Scenes/01 Neon City Pipes.unity`, or any
of the other eleven named world scenes. Prefabs are beside them in `Prefabs`.
Expand the world assembly and then:

- `Collar / Structural details · v1`: **Rolled cap bezel** and **Recessed cap bore**.
- `Barrel / Structural details · v1`: **Barrel support ribs** and **Inset light guides**.

These are separate readable mesh objects. Select them with Unity's Move, Rotate
and Scale tools; hide an object to omit it. Select the assembly root to adjust
its two light colours, metal finish, LED speed and detail. The cap mouth and
inner wall are real mesh surfaces: keep the bezel and bore aligned with the
inclined collar opening, and check the complete assembly from the gameplay view.

Save the scene, then select its assembly root and use **Bake this pipe theme into
gameplay** in the Inspector. This updates only that world's reusable meshes and
finish in `Assets/Resources/SkyPulse/PipeEdits`. Runtime uses that bake exactly;
it does not restore hidden details. Baking fits the visible assembly back to the
original collision dimensions, so it preserves flight width, gaps and spacing.
The gameplay collider remains authoritative: decorative reshaping must not imply
a new passable area.

Workshop scenes are for inspecting and editing models. Pressing Play there does
not start SkyPulse: the Editor bootstrap excludes workshop scenes so their static
pipes and inspection cameras cannot leak into a run. Open
`Assets/Scenes/SkyPulse.unity`, then press Play to test actual gameplay.

Existing scenes are never regenerated by the Create menu. The Add missing details
button adds only absent named children; it preserves existing objects and edits.
