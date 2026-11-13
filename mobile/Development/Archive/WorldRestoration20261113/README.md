# SkyPulse — selected world restoration and Titanium Skyport

13 November 2026. **Local source candidate after installed Build 21.**
Owner selected Blackout A (World Restoration) and Relay A (Titanium Skyport).
These are real Unity Editor captures of the implemented source, with no AI repainting.
The iPhone installation and public App Review submission have not changed.

Open [the review gallery](index.html), [selected designs](Selected-Designs.png),
[restoration sequence](Blackout-Restoration.png), or [motion preview](Blackout-Motion.gif).
The GIF is a stepped Editor rendering of real rotations/beam flow, not a device FPS recording.

## What is implemented

- Blackout's fixed 49-module renderer now has deeper cut-glass prisms, separate
  metal edge frames, stepped gimbal bearings, calibration contacts, vented
  carriers and broad optical reflections. Its 28-degree physical desk and
  independently projected hit targets remain fixed. Mirrors ease forward during
  rapid taps; beam packets and pooled charge ripples use existing timing.
- The wide flat projection plane, unused channel mesh and old 2D restoration
  ribbon/glyph renderer were removed. An open metal chassis leaves the chamber
  visible. The island overview and board share no interactive overlay.
- A live 3D miniature uses the same authored Neon civic mesh/lighting as Endless.
  Actual cores/uplink power previews the current system's lighting. PLAN does not
  power it or award progress. Restart clears partial power. A completion gives a
  restrained light scan while the next puzzle is already interactive. Pause and
  Reduced Motion are respected.
- The existing eight saved medals determine permanent extra lighting in **all
  twelve Endless worlds**. Original lighting stays unchanged at zero progress.
  Dock beacons/Relay station/City shield add cliff-beacon light; Cargo link/Power
  exchange/SkyPulse uplink add occupied-window light; Transit grid/Skyway network
  add roof/path LED light. These are additive visual rewards; they do not gate
  flight access, alter collisions or create another currency/save system.
- Relay A keeps both detailed towers and their physically connected middle,
  lower and arrival decks. Competing near/overhead hardware, two service spires
  and the second drone are hidden. The planet is less dominant, glow is reduced,
  and a shader-level titanium finish supports readable and non-readable authored
  meshes without runtime mesh duplication. Existing floor/gates/charge/crystals
  and automatic gate passage keep their original rules/dimensions.
- How to Play now explains the island and saved flight-world lighting.

## Verification

See [QA_REPORT.json](QA_REPORT.json) for checks, protected file hashes and source hashes.
The isolated QA product passed 147 new checks; existing Blackout, physical optic,
restoration-purpose and responsive touch checks; plus 3,424 final-polish, 188
Field Tech and 261 native-bird assertions. It covers all eight puzzle solutions,
preview/commit costs, pause/retry, saved progress, stale input/duplicate completion,
instant intermediate and final completion, 30/60/120 Hz simulated rotor timing,
negative-control inactive-optic contrast, warm allocation checks, all twelve light
bindings, and actual centre raycasts/44-point targets at small-phone, notched-phone
and iPad sizes. Rendered before/after Neon lights also differ at an identical clock.

Bird meshes, approved Voltcrest-style wing code, flight physics, pipe spacing,
Relay rules/collisions, Field Tech economy and app identity settings are byte-for-byte
unchanged from the selected-design baseline. The existing v3 campaign save remains
its single source of truth. No new save key or currency was introduced.

The source checker passed with no unfinished player copy; all 90 original frames
across 15 birds retain registration, and asset GUIDs are unique within this project.
These checks do not establish exclusive artwork rights, global metadata uniqueness
or Apple review approval.

## Honest assessment and remaining work

This connects Blackout to SkyPulse much better, but it is not proof of a finished
premium game. The main remaining gameplay limitation is the eight-puzzle campaign's
breadth. After owner visual review, test which boards feel satisfying, then add
carefully authored route variations and optional mastery targets. Avoid filling the
screen with effects to compensate for weak choices.

Next: owner review of the actual stills/motion; refine any specific objections;
then build a new numbered candidate and test it on iPhone/iPad, including sustained
frame times/memory/heat. Keep the already-approved wing motion, tap controls and
flight dimensions frozen. Public screenshots/product copy should only describe a
matching tested release. Public review remains on hold.

The earlier twelve-world density studies remain alternatives; no unselected
Endless density layout was promoted in this implementation.

## Files

Runtime: `SkyPulseHeistBoard`, `SkyPulseRestorationDisplay`, `SkyPulseWorldRestoration`,
Blackout campaign/UI/graphic, Flight environment/native hook, Relay environment/
orbital composition, guide, presentation glows; `HeistOptics`, `FlightArchitecture`,
`OrbitalStructure` shaders. QA: `SkyPulseRestorationChecks`, adjusted existing
HeistDepth/Purpose checks. All are inside `mobile/Assets` or `mobile/Tools`.
