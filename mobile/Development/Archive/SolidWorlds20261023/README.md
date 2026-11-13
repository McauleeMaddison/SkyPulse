# Solid architecture and integrated pipe designs — 23 October 2026

The building models in this package remain current and owner-approved. Updated
sky colours and pipe finishes are in [WorldFinish20261023](../WorldFinish20261023/PREVIEW_INDEX.md).
The world/pipe images below preserve the earlier material pass for comparison.
The earlier all-glass district and
simple monument previews are superseded. Images and movies here come from Unity,
not generated concept art. This candidate has not replaced the phone build.

## Preview files

- `PREVIEW_INDEX.md`: the complete labelled index, with full-size images for all
  twelve worlds and both building arrangements in each world (24 model views).
- `Models-Civic.png` / `Models-Residential.png`: the two complete model galleries.
  Every district is fitted inside the neutral inspection camera, including its
  roofs and terrain. This is not the gameplay camera.
- `All-Models-in-Motion.mp4`: all twelve civic models rotating together, 90
  captured frames at 30 FPS (three seconds).
- `models/`: full-size 1000 × 1200 model inspections, two per world.
- `architecture/`: full-size background views for each world, without HUD/pipes.
- `Worlds-and-Pipes.png` / `Worlds-in-Motion.mp4`: twelve gameplay views, including
  each world's pipe housing and lighting. The movie is twenty captured frames at
  10 FPS: two seconds per tile after the normal district entry reveal.
- `Architecture.png`: the twelve built districts without the flight HUD or pipes.
- `Structure-Detail.png` / `Structure-Inspection.mp4`: close views of the actual
  Neon City and Velvet Dawn models and materials against a neutral inspection
  background. This is model inspection, not a claim about the gameplay camera.
- `Flight.mp4`: twelve seconds of normal Endless simulation, 360 frames at 30
  captured FPS, using the normal tap buffer and collision checks.
- `Home.png`: Endless Home with its new live district background.
- `worlds/`: each of the twelve full-size gameplay images.

Capture frame rate is not measured iPhone performance. QA selects each milestone
world directly for its short vignette; the gallery is not a natural 180-gate run.
`PREVIEW_VALIDATION.json` records the additional complete-model capture, image
counts, framing checks and video decoding. The shipping scripts and shaders are
unchanged by this preview-only addition.

## The design change

Buildings now have solid structural walls with recessed window bays and dark
reveals. Glass is confined to windows and enclosed sky bridges. Structural piers
continue between floors; roof caps, plant, railings, promenades and skyways are
modelled geometry. Window frames surround actual inset panes, rather than a
full-height transparent-looking shell.

The districts have distinct plans:

| World | Buildings and setting | Pipe family |
|---|---|---|
| [Neon City](worlds/01-neon-city.png) | Stepped structural skyscrapers, inhabited rock islands and skyways | Original cyan/pink precision alloy, with refined reflections |
| [Aurora Rise](worlds/02-aurora-rise.png) | Swept ceramic towers and suspended skyport bridges on ice | Ceramic housings and flowing mint channels |
| [Solar Drift](worlds/03-solar-drift.png) | Solid research towers, panel wings and engineered docking decks | Collector housings with segmented amber energy |
| [Midnight Tide](worlds/04-midnight-tide.png) | Solid sail-shaped towers, ocean basin and waterfront arcade | Titanium seals and blue tidal light |
| [Velvet Dawn](worlds/05-velvet-dawn.png) | Rose-stone keep, turrets, gatehouse, parapets and gardens | Rose alloy housings with soft pink light |
| [Crystal Night](worlds/06-crystal-night.png) | Bevelled structural citadels and recessed ice-blue glazing | Faceted silver shells with crystalline pulses |
| [Jade Horizon](worlds/07-jade-horizon.png) | Planted solid terraces, framed pavilions and garden paths | Bioceramic collars and green flowing channels |
| [Violet Rain](worlds/08-violet-rain.png) | Braced arcology towers with enclosed connecting skyways | Rounded armour and descending violet light |
| [Eclipse](worlds/09-eclipse.png) | Obsidian fortress, gatehouse and crescent structure | Precision black-metal crown with restrained gold light |
| [Cobalt Storm](worlds/10-cobalt-storm.png) | Structural energy towers, generator rings and service bridges | Induction housings and cobalt pulses |
| [Amber Skies](worlds/11-amber-skies.png) | Sandstone citadel, turret and copper observatory | Fluted bronze housing and warm amber energy |
| [Polar Glow](worlds/12-polar-glow.png) | Insulated solid habitats, arch ribs and small glazed bays | Polar shells and cool flowing channels |

Each world has a civic and residential mesh arrangement. The same fixed renderer
pool uses both, with separate physical depth layers and course-distance parallax.
Skylines no longer repeat one identical monument at every position. Islands have
broader cliff formations and smoother surface normals; the old pointed undersides
and repeated grey moon were removed. The unused moon shader and its Unity meta
file were also removed.

Pipe changes replace the old flat plates with continuous barrel profiles,
surface-following channels, seals and machined collars. Eleven world finishes
now shade independently of the original pipe illustration. Neon's familiar
cyan/pink source art remains. Corrected small-vector normals restore reflective
lighting; depth-separated and filtered light channels avoid flickering black
recesses. Lighting rhythms are specific to each world. No gate shape animates
away from its collision box, and the original flight width and handling remain.

The rich original sky imagery is retained on the distant atmosphere dome. These
are constructed foreground districts with real depth, rather than a claim that
every distant sky pixel is new geometry. Materials use the project's existing
mobile shaders; this is a stylised game art pass, not a photorealism claim.

## Verification

`VALIDATION.json` records the source revisions, complete Unity check groups,
source/asset guards, local iOS export/build/signature results and preview hashes.
`SOURCE_MANIFEST.json` lists the exact files in the native export.

Coverage includes original camera and flight dimensions, cap seating/contours,
LED motion, all twelve world signatures, pause/Reduced Motion, 30/60/120 Hz
simulation checks, automatic Relay gates, same-frame crystal collection,
Blackout progression, duplicate input/rewards, retries, saves, training/breeding,
guide text and touch targets. New checks reject collapsed normals and lazy shader
compile errors. All 24 district meshes stay below the 60,000-vertex budget; the
full set's estimated vertex/index payload is about 36 MiB. Runtime transforms and
uniforms reuse the existing pools, with warmed allocation checks.

Still unverified: owner visual acceptance on the physical phone, a full natural
twelve-world run, sustained FPS, peak memory, battery/thermals and audio listening.
Existing Editor Search indexing and generated native toolchain warnings remain
recorded. Passing these checks does not establish a perfect or bug-free game.

No changes were made to save keys, scoring, economy, flight controls, gate spacing
or the tested playfield width. Relay and Blackout retain their established rules.
No main merge, phone installation, TestFlight upload, public submission, live
metadata change or website publication was made. The App Review hold remains.

## Editing map

- District plans, solid facades, windows, castles and islands:
  `mobile/Assets/Scripts/SkyPulseWorldArchitecture.cs`.
- World camera, two district arrangements, pooling and parallax:
  `mobile/Assets/Scripts/SkyPulseFlightEnvironment.cs`.
- Pipe profiles, recesses and material palettes:
  `mobile/Assets/Scripts/SkyPulseRouteArchitecture.cs`.
- Wall, stone, glass, atmosphere and pipe lighting:
  `mobile/Assets/Resources/SkyPulse/Shaders/FlightArchitecture.shader` and
  `EndlessRouteMetal.shader`.
- Editor-only inspection and checks: `mobile/Tools/SkyPulseDistrictDetailChecks.cs`,
  `SkyPulseDimensionalChecks.cs`, `SkyPulseWorldStyleChecks.cs` and
  `SkyPulseConstructedWorldChecks.cs`. These fixtures are not in shipping Assets.
