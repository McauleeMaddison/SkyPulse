# Neon districts, detailed islands and sound palette — 23 October 2026

Local visual/audio candidate following the owner's requested additions. The
established district plans, skyline forms, castles, islands and bridge locations
are retained, with more facade relief and selective occupied-window lighting.

## Previews

- [Cliff LEDs](Cliff-Star-Details.png)
- [Building detail](Building-Details.png) / [rotating inspection](Building-Inspection.mp4)
- [All twelve civic models](Models-Civic.png)
- [All twelve residential models](Models-Residential.png)
- [Current gameplay in all twelve worlds](Worlds-and-Pipes.png)
- [Worlds in motion](Worlds-in-Motion.mp4)
- [Additional pipe housing finish](Pipe-Finishes.png)
- [Pulse Relay gameplay](Pulse-Relay.mp4) / [still](Pulse-Relay.png) / [background layer](Pulse-Relay-Background.png)
- [New sound cues](Audio-Showcase.wav) / [cue order](AUDIO_SHOWCASE.md)
- [All individual full-size previews](PREVIEW_INDEX.md)

These are actual Unity renders. The rotating inspection uses 90 frames at 30
captured FPS; the gameplay montage uses 20 frames at 10 captured FPS, two seconds
per tile after the world entry reveal. Capture FPS does not measure device FPS.
The inspection camera fits each model, rather than representing the play camera.

## Visual changes

- Selected recessed panes have stable occupied-room lighting. Most glazing stays
  dark/reflective; primary and secondary colours are drawn directly from each
  world's existing pipe LED palette. Gentle intensity drift uses the existing
  presentation clock and freezes with pause/Reduced Motion.
- Window reveals are deeper. Raised facade ribs follow the existing tower shape;
  short front ledges add relief without putting a large ring around every floor.
- Recessed entrances have projecting jambs, a canopy, a light strip and steps.
  Roof parapets, screened plant, small masts and structural bridge portals give
  the buildings detail at different scales. Castle turrets have base buttresses.
- Pavilion light is concentrated at the roof perimeter instead of filling the
  entire roof plate. The buildings retain opaque walls and recessed glazing.
- Broken cliff ledges add physical strata. Small five-point neon star beacons
  have layered metal sidewalls, reflective bevels, a contrasting luminous gasket,
  a dark recess, faceted cores and a small diamond centre. Five varied-size stars
  span each rocky cliff front; four sit across dock foundations. They use each
  world’s pipe colour pair and are decorative, not pickups. The light also spills
  onto the actual foundation surface, with a gentle shared-clock pulse. Cached
  mesh-space beacon positions keep the illumination attached in 3D. No billboard
  aura, bloom pass or real-time light is needed.

All additions stay in the existing cached district meshes. No extra per-window
objects, real-time lights, gameplay colliders, image assets, textures or material
instances were added. The same 25 perspective-scene renderers and two material
banks are used. All 24 district meshes remain below the 60,000-vertex budget.
The measured maximum is 57,264 vertices; the full district vertex/index payload
is estimated at 44,696,424 bytes (about 42.6 MiB). This increased geometry cost
still needs sustained phone profiling.

The previous atmosphere and pipe-finish pass is retained. The eleven themed
housings gain small recessed end joints and machined service fasteners, filtered
for small-screen readability. Their existing individual profiles, colour pairs,
animated LED channels and collar finishes remain intact. Neon City’s sky and
original pipe appearance are unchanged; its buildings now include the requested
neon-window, facade and glowing cliff-star improvements.

## Sound changes

All seven cues were recreated with a shared deterministic synthesis palette:
soft wing/servo air, glassy crystal pickups, a short gate confirmation, a warm
unlock, tactile prism detents, a restrained sensor warning and a damped impact.
Filtered noise, additive/FM partials and small early reflections are baked into
the files. No recordings, sample packs, music or external services were used.

The clips are mono 48 kHz PCM with the same resource names and cue durations
(within one output sample of the prior durations). Audio GUIDs/import settings,
the existing six-voice pool, mixer gain, rate limit, mute and pause behaviour
remain intact. Nothing new is synthesized or allocated per tap at runtime.
`AUDIO_RECORD.json` records old/new hashes, generator hashes, PCM metrics and
Unity-decoded peaks. The source cue showcase is not an iPhone audio recording.

Reproduce/verify with:

```sh
python3 mobile/Tools/generate_skypulse_audio.py --check mobile/Assets/Resources/SkyPulse/audio
python3 mobile/Tools/generate_heist_audio.py --check mobile/Assets/Resources/SkyPulse/audio
```

## Verification and limits

`VALIDATION.json` records the full Unity regression groups, all 24 selective
window-light comparisons and all 24 star-light on/off comparisons, mesh limits, media validation and source guards.
The window comparison renders each model with illumination on/off and verifies
that only a small portion of the model changes. Pause and Reduced Motion now
explicitly check the architectural lighting clock as well as scenery movement.
The new clips decode in Unity and pass mix-headroom, mute, pause, focus-loss,
rapid-input and voice-pool checks. Existing C#/shader compilation and the normal
flight, Relay, Blackout, economy/save, guide, breeding and input checks passed.

Source PCM checks verify mono/rate/length, zero endpoints, negligible DC and
headroom. These and Unity's playback checks do not replace listening on the
phone speaker and headphones. Visual acceptance, a full natural twelve-world
run, sustained device FPS, memory, battery and thermals also remain pending.

This is a local development candidate. No updated native build was installed,
no TestFlight upload or public App Review submission was made, and main has not
been merged. The previous phone build predates these changes.

## Visual editing in Unity

[Background Workshop guide](../BackgroundWorkshop20261023/EDITING_GUIDE.md).
There are 25 saved scenes with 227 selectable structures. Their Bake workflow
preserves mobile pooling, loads optional compiled edits on the next run, carries
island glow positions with moved structures and retains Relay animation offsets.
The authoring integration passed geometry, resize, light-attachment and runtime
loading checks. The complete gameplay regression suite was rerun afterward.
Relay's distant vista remains artwork behind genuine perspective mesh hardware;
the preview does not claim that every distant feature is geometry.

## Editing map

- District geometry, window selection, entrances, roofs, cliff strata and stars:
  `mobile/Assets/Scripts/SkyPulseWorldArchitecture.cs`.
- Occupied-window material response and emission:
  `mobile/Assets/Resources/SkyPulse/Shaders/FlightArchitecture.shader`; cached
  lighting is bound on world changes by `SkyPulseFlightEnvironment.cs`.
- Subtle additional housing finish:
  `mobile/Assets/Resources/SkyPulse/Shaders/EndlessRouteMetal.shader`.
- Shared audio synthesis: `mobile/Tools/generate_skypulse_audio.py`;
  Heist cue CLI: `mobile/Tools/generate_heist_audio.py`.
- Editor-only model/window tests: `mobile/Tools/SkyPulseDistrictDetailChecks.cs`;
  lifecycle/gameplay/audio checks: `mobile/Tools/SkyPulseDimensionalChecks.cs`.
