# Editing SkyPulse in VS Code

Open the **SkyPulse** folder. Start with `mobile/Assets/Scripts/SkyPulseNativeGame.cs`.
Its `#region` sections appear in VS Code’s Outline and can be folded. On Mac,
use **⌘P** to open a file, **⌘⇧O** to jump to a method, **F12** to follow a call,
and **⇧F12** to find its references.

## Where to make a change

All `SkyPulseNativeGame.*.cs` files extend **one game class**. They are not
separate games or duplicate components. Keep the original `SkyPulseNativeGame.cs`
component on the scene; do not attach each partial file separately.

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
| Original 3D pipe material and light packets | `SkyPulseNativeGame.Depth.cs`, `Resources/SkyPulse/Shaders/EndlessRouteMetal.shader` |
| Original pipe artwork placement / collider anchors | `SkyPulseNativeGame.PipeVisuals.cs` |
| Tapered exhaust, soft trail and passage sparks | `SkyPulseFlightEffects.cs`, `SkyPulseNativeGame.Cinematic.cs` |
| Pooled 3D scenery during Endless flight | `SkyPulseFlightEnvironment.cs`, `SkyPulseNativeGame.Environment.cs`, `Resources/SkyPulse/Shaders/FlightArchitecture.shader` |
| Relay station ribs, turbines and drones | `SkyPulseRelayEnvironment.cs` |
| Six audio voices, optional cue fallbacks and saved sound switch | `SkyPulseNativeGame.Audio.cs` |
| How to Play topics and route-map entry | `SkyPulseNativeGame.Guide.cs` |
| Three Home layouts and mode input | `SkyPulseNativeGame.Controls.cs`, `.Presentation.cs` |
| Relay gate rules | `SkyPulseMissionModels.cs` → `RelaySession` |
| Signal Heist puzzle rules / presentation | `SkyPulseBlackoutCampaign.cs`, `SkyPulseNativeGame.Blackout.cs`, `SkyPulseHeistBoard.cs` |

Paths in the table are relative to `mobile/Assets/Scripts`, except the shaders,
which are relative to `mobile/Assets`.

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
The collar material sets `_Cutout` to 1 so transparent artwork corners write
neither dark pixels nor depth. Keep that contour handling when editing its shader;
the solid shaft deliberately leaves `_Cutout` at 0.

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
4. Run `python3 mobile/Tools/run_modes_qa.py --output artifacts/my-change-qa`
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

In `SkyPulseNativeGame.Depth.cs`, the visible shaft ends 0.04 world units inside
the collar's far shoulder. Collision shapes still overlap for reliable gameplay;
do not trim those colliders when adjusting the visible join. The original collar
cutout and cyan/pink LED material remain in use.

## How to Play and world crossfades

`SkyPulseNativeGame.Guide.cs` owns the mode-specific guide and Endless topics for
Tech, Core, training and breeding. The world map is reached from its Worlds &
Power-ups control. Keep help copy aligned with the actual cost/reward functions.

`EndlessRouteMetal.shader` uses `_Visibility` for depth-safe departure coverage.
`UpdateDepthPresentation` derives it from the existing world-transition timer,
matching the departing sprite pool. `SceneLightTint` blends the shared bird/pipe
light across the same transition. Do not change the original flight geometry or
world milestone rules when adjusting these presentation values.
