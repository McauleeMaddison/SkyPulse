# Three-mode development preview — 23 October 2026

Fresh **actual Unity gameplay**, not concept art, a browser game or iPhone
footage. These silent H.264 clips are 750×1334 at 30 captured frames per second;
every encoded frame was decoded successfully. Capture rate is not device FPS.

- `Endless-Route.mp4`: 360 frames / 12 seconds, normal flight/collisions driven
  by QA taps. Shows authored wing poses, pipe LEDs and the revised skyline.
- `Pulse-Relay.mp4`: 420 frames / 14 seconds, normal automatic collection and
  gate passage. Includes rounded station hardware and the new low-profile deck.
- `Blackout-Protocol.mp4`: 270 frames / 9 seconds, actual mirror rotations that
  power a core, complete the uplink and immediately activate the next vault.
- `Three-Modes.mp4`: a synchronized nine-second excerpt of all three modes.
- `Three-Modes-Preview.png`: dated overview from the same Unity frame sequence.
- `Score-Cards.mp4` / `Score-Cards.png`: holographic results in all three modes,
  captured with actual mode UI handlers and isolated, artificial QA saves.
- `endless-result.png`, `relay-result.png`, `blackout-result.png`: full phone layouts.
- `How-to-Play-Blackout.png`: the updated Mission guide in a small-phone layout.

The gameplay render source is `98b6a13`; the result-card clips include the
subsequent `1bb749b` contrast correction. The complete local native candidate
contains both, including the updated How to Play. Exact hashes and checks are
recorded in `BUILD_AND_QA.json`, `SOURCE_MANIFEST.json` and `COPY_AUDIT.json`.

## What changed

**Endless:** true mesh skyline layers with rounded curtain-wall footprints,
recessed windows, swept crowns and continuous light reveals. Reflected color uses
each existing world image. Structures sit on the lower horizon with three rates
of parallax, rather than bobbing on exposed discs. All twelve world backdrops
and the existing flight camera remain; this is mesh scenery over a distant
authored sky, not an entirely mesh-only environment. The original pipe graphics,
tested width, gap, physics and collisions remain intact.

**Relay:** swept titanium ribs, inset conductors, volumetric recycler blades and
rounded maintenance craft replace box-like props. The station artwork supplies
their reflected color. One static 3D deck mesh holds bevelled panels, foundation,
continuous fascia and the fixed illuminated ground edge. The shader wraps panels
using actual course distance, with no per-frame object creation. The physical
ground remains at `GroundY = -8.45`; the existing gate aperture, red/blue laser
rules, paired random crystals and tap handling are unchanged.

**Blackout:** opening puzzles use larger optics and fixed projected touch targets.
An open precision frame and engraved possible routes anchor the console without
an opaque slab. The named restoration target, numbered cores, live uplink,
trace budget and Gold target make the existing authored challenge explicit. A
city ribbon responds to actual core power; completed districts read saved medals.
Planning cannot falsely restore a district. Restarts clear partial power, while
completed systems persist and the next vault remains immediately playable.
The eight puzzle solutions, budgets, rewards, save keys and economy are retained.

**Finished copy and help:** How to Play retains Flight, Tech Tree, Core, training,
breeding and the twelve-world/power-up map. Relay explains the illuminated ground
edge and automatic gate opening. Blackout now has Routing and Mission tabs for
splitters, shutters, Plan, city restoration, trace failure, Gold and progression.
No placeholder matches were found in 2,636 source/resource strings, 51 visible
screen states / 1,535 text values, or OCR across 145 shipping images. Functional
preview/selection labels remain. Source and visible-UI guards share one blocklist
under `Tools/`; they do not run in or add dependencies to the shipping player.

**Score/results finish:** a shared, rounded glass shader replaces flat card
faces, with fine spectral rims, a restrained moving reflection and crisp
lettering. Endless score/reward cards and result actions, Relay summaries and
Blackout summaries share the finish, retaining each mode's accent. HUD labels
use weaker emphasis. One static quad per surface and a shared lettering material
avoid animated mesh rebuilds, duplicated text and moving hit areas. Reduced
Motion removes the sweep; pause freezes the clock. Native result callbacks,
scores, reward calculation, save keys and economy are unchanged.

## Validation and limits

The graphics/card revision `98b6a13` passed 28 Unity groups, including all twelve worlds,
30/60/120 simulation-step transitions, automatic gates, same-frame collection,
held/duplicate input, retry, saves, once-only rewards and final-puzzle completion.
The seven-wide puzzle controls were checked for screen bounds, disjoint targets
and at least 44-point size on small phone, large phone and iPad layouts. Warmed
skyline, station, deck and optical animation allocated no managed memory.

The subsequent guide/copy checks passed with every guide topic fitting its text
area at all three layouts. Metadata-copy tests, artwork-ledger coverage and the
protected-rule / 238-GUID preservation check passed. OCR is not infallible and
does not replace visual review of stylized or low-contrast lettering.

The subsequent one-line Retry text-color correction (`1bb749b`) passed the
focused result checks and real centre raycasts for every result button at all
three sizes. Every label remains a single text mesh without an enabled shadow.
Animation timing passed 30/60/120-step tests; the warmed update allocated zero
managed bytes. A first QA run exposed a fixture leaving the world guide open;
that fixture now follows the guide's real Back behavior before continuing. A
fresh capture warms the camera before testing Unity's graphic-depth raycast cache.
These fixes are in test helpers, outside the shipping player.

Unity iOS export and the signed Xcode Release build are recorded separately from
Editor checks. The complete candidate is local build 18; the earlier installed
source `6692cd6` failed the owner's presentation acceptance. **The new revision
has not been installed on the phone.** Visual acceptance, speaker/headphone mix,
sustained frame rate, memory and thermals remain open. Native Unity-generated
deprecation/toolchain warnings and the pre-existing Editor Search indexing
exception remain recorded; this is not a warning-free or perfect-build claim.

No TestFlight upload, App Store Connect change, public review submission, website
publication, paid asset, main merge or capture-permission change was performed.
Apple's unidentified similarity concern remains on hold pending clarification.

## Editing map

- Skyline geometry/material: `Assets/Scripts/SkyPulseFlightEnvironment.cs`,
  `SkyPulseNativeGame.Environment.cs`, `Resources/SkyPulse/Shaders/FlightArchitecture.shader`.
- Station and floor: `SkyPulseRelayEnvironment.cs`, `SkyPulseRelayDeck.cs`,
  `SkyPulseNativeGame.Presentation.cs`, `Resources/SkyPulse/Shaders/RelayRunway.shader`.
- Optical console/objectives: `SkyPulseHeistBoard.cs`, `SkyPulseBlackoutGraphic.cs`,
  `SkyPulseNativeGame.Blackout.cs`.
- How to Play: `SkyPulseNativeGame.Guide.cs`.
- Results/HUD finish: `SkyPulseNativeGame.Results.cs`, `SkyPulseScoreGlass.cs`,
  `Resources/SkyPulse/Shaders/ScoreGlass.shader`.
- Checks: `Tools/SkyPulsePurposeChecks.cs`, `SkyPulseCopyChecks.cs`,
  `SkyPulseGuideWorldChecks.cs`, `SkyPulseScoreChecks.cs`, `SkyPulseBlackoutChecks.cs`,
  `SkyPulseCinematicChecks.cs`, `check_player_copy.py`, `run_dimensional_qa.py`.

Script names above are under `Assets/Scripts/`; shaders are under `Assets/`.
Development media and test helpers are outside the shipping Assets folder.
