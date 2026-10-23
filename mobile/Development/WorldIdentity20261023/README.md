# Twelve individual worlds, matching pipes and clearer player goals

Actual Unity Editor renders of the local development candidate, 23 October 2026.
These are rendered game frames, not concept artwork or physical iPhone footage.

## Previews

- [Twelve worlds and matching pipes](Twelve-Worlds-and-Pipes.png) and
  [animated gallery](Twelve-Worlds-and-Pipes.mp4): 20 frames, 10 captured FPS, two seconds.
- [Architecture gallery](Twelve-Architectures.png): the twelve real mesh families
  without the flight HUD or pipes obscuring them.
- [Three gameplay modes](Three-Modes.mp4): 270 frames, 30 captured FPS, nine seconds.
- [Three Homes](Three-Homes.mp4): 40 frames, 10 captured FPS, four seconds.

Capture FPS is not a measurement of iPhone performance. QA selects each world
directly, then pilots through its normal simulation and collision checks. These
short clips do not claim to demonstrate a natural 180-gate run.

| World and full-size preview | Physical architecture | Pipe structure and light |
|---|---|---|
| [Neon City](worlds/01-neon-city.png) | Floating mineral islands and stepped city spires | Original cyan/pink shaft, collar and flowing inlays |
| [Aurora Rise](worlds/02-aurora-rise.png) | Swept glass towers, ice terraces and arched bridges | Fluted silver body with sweeping aurora strips |
| [Solar Drift](worlds/03-solar-drift.png) | Suspended collector docks and radial machinery | Copper radiator bands and segmented solar energy |
| [Midnight Tide](worlds/04-midnight-tide.png) | Curved glass sails on grounded shafts | Blue seals and tidal light swells |
| [Velvet Dawn](worlds/05-velvet-dawn.png) | Rose observatories, domes and balconies | Rose-metal decorative inlays and breathing light |
| [Crystal Night](worlds/06-crystal-night.png) | Prismatic citadels and ice shelves | Faceted silver structure with crystalline glints |
| [Jade Horizon](worlds/07-jade-horizon.png) | Stacked garden sanctuaries and planted terraces | Jade collars, ring bands and flowing green light |
| [Violet Rain](worlds/08-violet-rain.png) | Cantilevered towers and linking sky bridges | Open vent frames, violet armour and falling light |
| [Eclipse](worlds/09-eclipse.png) | Leaning obelisks and crescent monuments | Obsidian crown, diamond inlays and crescent pulse |
| [Cobalt Storm](worlds/10-cobalt-storm.png) | Braced pylons and suspended generators | Zigzag conduits and scanning cobalt light |
| [Amber Skies](worlds/11-amber-skies.png) | Copper observatory domes and colonnades | Fluted copper structure and warm amber energy |
| [Polar Glow](worlds/12-polar-glow.png) | White arch habitats and glazed ice pods | Glazed silver body and polar light chevrons |

## What changed for players

The architecture is real volume rendered by a separate perspective camera.
Three depth layers move at different rates. The colourful sky artwork remains
on a distant atmosphere dome; the foreground structures are constructed meshes.
Later worlds have grounded facades, rather than twelve copies of floating rock.
Relay retains its distant orbital vista behind actual curved station hardware,
floor and concourses. Blackout uses glass optics, ceramic sockets and a live
holographic circuit over the existing city-restoration objectives.

Each world's pipe body and collar now have their own cached geometry, material
palette and LED rhythm. Neon keeps its original cyan/pink artwork. Departing
pipes retain their old design until the existing transition fades them out;
the incoming world's design appears after that fade. Pipe dimensions, collision
boxes, flight width, spacing, flap handling and scoring rules remain unchanged.

Endless's HUD now names the next world and counts down the gates remaining.
It follows the 15-gate milestones, resets on retry and handles the twelve-world
loop. The result card offers a short action based on ground contact or gate
impact. How to Play explains the new goal display. The presentation does not
add save keys, currencies, rewards or purchases.

Homes retain mode-specific identities: a violet city vista for Endless, a cyan
orbital dock for Relay and a mint optical command chamber for Blackout. Motion
respects Pause and Reduced Motion; decoration cannot intercept gameplay taps.

## Verification and release boundary

See [VALIDATION.json](VALIDATION.json) and [SOURCE_MANIFEST.json](SOURCE_MANIFEST.json)
for source revisions, test groups, local native compilation and media hashes.
The isolated QA suite covers automatic Relay unlocking and same-frame collection,
touch/UI separation, Blackout immediate progression and once-only rewards,
retries, saved progression, training/breeding, world transitions and guide text.

The native candidate remains local version 1.0.0, build 18. It has **not** been
installed on the phone, uploaded to TestFlight, merged to main or submitted for
public App Review. The existing phone installation is not this candidate.

Still required: owner visual acceptance, a full natural route walkthrough,
sustained device frame time, memory, battery/thermal checks, and listening on
the phone speaker and headphones. Existing Editor Search indexing and generated
native toolchain warnings are recorded; successful compilation is not a claim
of a bug-free game.

The metadata checks cover the local configuration and assets. They cannot
compare this game to Apple's private similarity database, resolve its allegation
or certify exclusive rights in generated artwork. No App Store metadata or
website was published during this pass. Public submission remains on hold.

## Highest-value next player checks

1. Judge each world's skyline, pipe silhouette and opening on the actual phone;
   confirm foreground detail never hides the bird or collision opening.
2. Play a natural run across world boundaries, including pause/resume and retry.
   Check that the target, scenery and pipes all tell the same progression story.
3. Play Relay with one thumb: both crystals must be reachable, locks must open
   immediately, and the next-circuit action must work without Mission Control.
4. In Blackout, use PLAN to inspect beam direction and trace cost, then restore
   the eight named city systems. Check immediate progression and final completion.
5. Compare a bird's previewed reward traits with actual Endless results; confirm
   training discounts and breeding previews are understandable before spending.
6. Run longer sessions on an older supported phone. Use measured frame time and
   memory to choose further optimisations before adding more effects or features.

These are acceptance checks for existing mechanics, not proposals to rebuild them.

## Editing map

- World mesh construction: `Assets/Scripts/SkyPulseWorldArchitecture.cs`.
- Perspective scene, atmosphere and pooling: `SkyPulseFlightEnvironment.cs`.
- Pipe mesh construction: `SkyPulseRouteArchitecture.cs`; binding and joins:
  `SkyPulseNativeGame.Depth.cs`.
- World and station integration: `SkyPulseNativeGame.Environment.cs`,
  `SkyPulseRelayEnvironment.cs` and `SkyPulseNativeGame.Presentation.cs`.
- Home finish and mechanisms: `SkyPulseNativeGame.Home.cs`, `SkyPulseDepthStage.cs`.
- Blackout depth: `SkyPulseHeistBoard.cs`, `SkyPulseBlackoutGraphic.cs`.
- Goal display and help: `SkyPulseNativeGame.Guide.cs`; collision guidance:
  `SkyPulseNativeGame.Results.cs`; one creation call in `SkyPulseNativeGame.cs`.
- Material finishes: `Assets/Resources/SkyPulse/Shaders/WorldAtmosphere.shader`,
  `FlightArchitecture.shader`, `EndlessRouteMetal.shader`, `HeistOptics.shader`
  and `ScoreGlass.shader`.
- Isolated Editor fixtures: `Tools/SkyPulseDimensionalChecks.cs`,
  `SkyPulseConstructedWorldChecks.cs`, `SkyPulseWorldStyleChecks.cs` and the
  existing focused regression fixtures. These are outside shipping Assets.

Script paths above are relative to `mobile/`; all production game rules stay in
their existing mode files. The source manifest lists the exact exported files.
