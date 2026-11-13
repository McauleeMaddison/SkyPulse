# Constructed Endless world and three Home identities — 23 October 2026

**Historical preview, superseded.** The colourful twelve-world candidate, with
individual architecture and matching pipes, is in
[WorldIdentity20261023](../WorldIdentity20261023/README.md). This folder preserves
the earlier visual experiment; use the new gallery when reviewing the game.

These are actual Unity renders of the local development revision, using isolated
QA saves. They are not concept images or physical iPhone footage.

- `Endless-3D-World.mp4`: 12 seconds / 360 decoded frames at 30 captured FPS.
  Normal flight and QA taps show the live city, wings, trail and original gates.
- `Three-Homes.mp4`: four seconds / 40 decoded frames at 10 captured FPS, showing
  the three phone-aspect Homes together. Capture FPS is not device performance.
- `home-endless.png`, `home-relay.png`, `home-blackout.png`: individual full layouts.
- `Twelve-Worlds.png`: actual renders of all twelve constructed-world palettes.

## What is now genuinely 3D

Endless Home and flight no longer display the flat city illustration. A dedicated
perspective camera renders a constructed scene: two pooled banks of eighteen
city-district instances, a mesh atmosphere dome and a lit spherical moon. Towers
have rounded or stepped profiles, enclosed skyways, recessed glazing, small lit
windows, curtain-wall mullions and reflected highlights. Foundations extend below
the view, rather than ending as floating shapes. Three layers occupy separate
physical depths and move at different rates using actual course distance.

The live camera feed is composited behind the original orthographic flight
camera. Its render target is 720 pixels wide, changes height with the viewport
and is capped at 1920 pixels, with 2× MSAA and 16-bit depth. Meshes/materials are
constructed once; viewport changes alone resize that target. The scenery owns no
colliders. Its camera renders only its isolated layer and cannot change flight
input, the bird hitbox, gate spacing or the tested playfield width. Pause keeps
the existing feed still; Reduced Motion freezes travel and decorative motion.

The original world images remain used by the route guide and material reflection
references, so they are retained. Relay and Blackout keep their existing authored
distant vistas; this update does not claim their whole environments are mesh-only.

## Home identities and interaction

The three Homes now use a quieter SkyPulse brand, prominent mode titles,
holographic glass controls and crisp lettering. Endless uses its live skyline
and an open observation rail; Relay has an orbital launch cradle and two rotating
charge cores; Blackout has a three-part optical iris around its restored-system
core. The same existing actions and saved progression remain available.

Every visible Home button was checked for actual centre raycasts, safe bounds,
at least 44-point targets and disjoint hit areas on small phone, large phone and
iPad fixtures. Endless/Blackout use a larger guide action and a shared footer
row; Relay retains its circuit/bird/guide row. Home decorations cannot take taps.

## Verification and limits

The full graphics revision `cc90b9b` passed 28 Unity regression groups, including
all twelve worlds and transition steps at 30/60/120 Hz, the original camera and
pipe geometry, Relay automatic gates and same-frame final-crystal collection,
Blackout solutions/progression, duplicate and held input, retry, saved progress,
once-only rewards, guide text, copy checks, touch targets and warmed scenery
allocation checks.

The final shipping revision `33bc4ac` replaces a runtime-only built-in shader
lookup with an explicitly packaged Resources shader. A rendered 512×896 pixel
comparison found zero changed pixels. Final Home/preview checks also passed.
Native source hashes, export, signing and media evidence are in `VALIDATION.json`
and `SOURCE_MANIFEST.json`. The full gameplay clips were captured from the full
regression revision; the subsequent shader-packaging change preserves its pixels.

The copy guard found no placeholder matches in 2,675 source/resource strings or
51 visible UI states / 1,555 text values. All 242 Unity GUIDs are unique; the
existing 145-image artwork ledger still matches. These checks do not resolve
Apple's private similarity allegation or guarantee future metadata compliance.

The candidate is local build 18. **It has not been installed on the iPhone or
uploaded to TestFlight.** Owner visual acceptance, sustained device FPS, memory,
thermals and the final speaker/headphone mix remain unverified. The pre-existing
Editor Search indexing exception and generated native toolchain warnings remain
recorded. Passing compilation is not a claim of a perfect or bug-free game.

No main merge, public review submission, App Store metadata edit, website
publication, paid asset purchase or device-capture permission change was made.
The App Review hold remains in effect.

## Editing map

Paths below are relative to `mobile/Assets/`:

- World construction, camera, pooling and lifecycle: `Scripts/SkyPulseFlightEnvironment.cs`.
- World visibility and original-flight integration: `Scripts/SkyPulseNativeGame.Environment.cs`.
- Tower finish: `Resources/SkyPulse/Shaders/FlightArchitecture.shader`.
- Atmosphere, moon and packed live feed: `WorldAtmosphere.shader`, `WorldMoon.shader`,
  `WorldComposite.shader` in the same shader folder.
- Home titles, finish and footer: `Scripts/SkyPulseNativeGame.Home.cs`.
- Home 3D mechanisms: `Scripts/SkyPulseDepthStage.cs`.
- Shared glass control geometry/material: `Scripts/SkyPulseCircuitGraphic.cs`,
  `Scripts/SkyPulseNativeGame.Results.cs`.
- Presentation hook: `Scripts/SkyPulseNativeGame.Presentation.cs`.

The editor-only checks are `mobile/Tools/SkyPulseConstructedWorldChecks.cs`,
`SkyPulseDimensionalChecks.cs` and `SkyPulseTouchComfortChecks.cs`. They are not
included in the shipping player. Local logs and signed archives are ignored under
`artifacts/constructed-world-20261023/`.
