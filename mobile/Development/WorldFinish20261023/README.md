# World atmosphere and pipe finish — 23 October 2026

This local Unity candidate refines the eleven Endless Route worlds after Neon
City. The owner-approved district/building meshes are unchanged. Neon City's sky
and pipes match the reference captures with zero changed pixels; its original
pipe fragment also remains byte-for-byte unchanged.

## Preview files

- [All twelve worlds with pipes](Worlds-and-Pipes.png)
- [Worlds in motion](Worlds-in-Motion.mp4): twenty normal gameplay frames at
  10 captured FPS, two seconds per world.
- [All pipe finishes](Pipe-Finishes.png)
- [Pipe lighting in motion](Pipe-Lighting-in-Motion.mp4): thirty actual material
  captures at 15 FPS, two seconds; the same clock drives runtime lighting.
- [Matched before/after inspection](Before-and-After.png)
- [All atmospheres](Atmospheres.png), isolated from the buildings for colour review.
- [Individual full-size previews](PREVIEW_INDEX.md)

These are actual Unity renders, not generated concepts. The neutral pipe camera
is an inspection view. World views use normal flight simulation/collisions; QA
selects each world directly, rather than representing a complete natural run.
Captured FPS is not a measurement of sustained iPhone performance.

## World direction

| World | Atmosphere | Pipe material and lighting |
|---|---|---|
| Neon City | Established blue/magenta sky and cloud sea | Original dark precision alloy, cyan/pink LEDs — reference preserved |
| Aurora Rise | Deep teal sky, mint clouds and a slow aurora | Pearl ceramic, mint/lavender diffusers, flowing light |
| Solar Drift | Navy/rose sky over warm amber clouds | Fluted graphite/copper collector housing, amber/orange energy packets |
| Midnight Tide | Ocean-blue sky and turquoise haze | Navy titanium, teal seals and tidal light waves |
| Velvet Dawn | Plum sky, rose cloud banks and warm horizon | Rose enamel, champagne trim and pink/gold breathing light |
| Crystal Night | Indigo sky with ice-blue/violet light | Faceted silver, recessed inlays and moving crystal highlights |
| Jade Horizon | Blue-green sky and soft emerald mist | Jade ceramic, champagne accents and flowing green channels |
| Violet Rain | Deep plum, violet light and fuchsia edge clouds | Rounded violet armour, cyan secondary inlays and descending light packets |
| Eclipse | Obsidian-indigo sky with a restrained warm horizon | Angular black-metal crown, gold/amethyst diffusers and slow pulses |
| Cobalt Storm | Midnight blue, luminous cobalt clouds and slow storm illumination | Cobalt induction housing, illuminated coil bands and travelling blue energy |
| Amber Skies | Navy/terracotta sky and soft amber cloud banks | Fluted bronze, warm amber/orange inlays and sweeping light |
| Polar Glow | Arctic blue, turquoise haze and lavender aurora | Pearl insulated shell, cyan/lavender light and a slow shifting wave |

The later pipe finishes retain machined surface detail from the existing artwork
while using their own housing profiles, anodisation/enamel and two-colour inlays.
The collars have a rolled metallic bezel, recessed dark well, luminous gasket
and a moving rim reflection. Forward diffusers remain readable on a small
portrait screen; duplicate overlapping rails on six-channel housings are avoided.

Skies retain the existing authored imagery on the atmosphere dome. The new colour
palettes, soft cloud fields, aurora and storm illumination are shader effects.
The occupied buildings/islands remain real perspective meshes. No new image
asset or purchased asset was introduced. This is stylised rendering, not a claim
of physically simulated volumetric clouds or photorealism.

World identities blend separately, including the return to Neon City. The new
palettes share their cloud-noise evaluation during a crossfade. Existing pooled
meshes/materials, capped background render target and time-based uniforms remain;
no per-frame object construction or new gameplay colliders were added.

## Verification and limits

`VALIDATION.json` records source hashes, reference comparison, targeted material
checks, the full existing Unity regression suite, image/video evidence and the
preservation/copy/artwork guards. Coverage includes all twelve worlds, visible
lighting motion, Reduced Motion, 30/60/120 Hz simulation agreement, palette
transition endpoints, pipe mesh bounds, seated Neon cap joins, pause/retry,
Relay automatic gates and Blackout progression. Both final shader rendering and
C# compilation completed without compile errors. The existing Editor Search
indexing exception and intentionally injected save-failure checks remain in the
full log; the regression suite completed successfully.

Gameplay input, flight width, gap/spacing, physics, saves, economy, Relay/Blackout
rules and the approved building source are unchanged. The warmed environment
update allocation check passed. Sustained device FPS, GPU cost, thermals and
owner acceptance still need a physical-phone check. No updated native app was
built or installed for this visual candidate; the previous signed build predates
this material pass. No TestFlight upload or App Review submission was made.

## Editing map

- Pipe housing profiles, colour pairs and recessed lighting geometry:
  `mobile/Assets/Scripts/SkyPulseRouteArchitecture.cs`.
- Metal/enamel shading, rolled collars, diffuser and light animation:
  `mobile/Assets/Resources/SkyPulse/Shaders/EndlessRouteMetal.shader`.
- World palettes, cloud colour, aurora and sky transitions:
  `mobile/Assets/Resources/SkyPulse/Shaders/WorldAtmosphere.shader`.
- Incoming/outgoing world identities on the existing background material:
  `mobile/Assets/Scripts/SkyPulseFlightEnvironment.cs`.
- Editor-only reference, animation, Reduced Motion and timing checks:
  `mobile/Tools/SkyPulseWorldFinishChecks.cs` (outside shipping Assets).
