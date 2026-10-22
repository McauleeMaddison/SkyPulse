# Pipe joins, LED motion and rounded scenery — 22 October 2026

Development follow-up on `codex/dimensional-flight-20261022`. Main, TestFlight,
App Store Connect and the public review submission remain unchanged.

## Changes

- **Connected caps.** The old fixed 0.04-world-unit overlap stopped short of
  parts of the collar artwork's curved edge. Shafts now seat 28% of the collar's
  height inside its far shoulder. The overlap stays behind the solid wall,
  clear of the open lip. Top and bottom use the same symmetric rule. Existing
  artwork, cap extents, hitboxes, flight width, spacing and gap sizes are retained.
- **Visible LED motion.** The previous additive effect saturated already-bright
  artwork on an LDR display. The diffuser now varies its actual luminance, with
  packets travelling along the cyan/pink column strips and around the collar's
  elliptical rim. Light stays on between packets. The existing elapsed-time clock
  freezes on pause and becomes static with Reduced Motion.
- **Rounded architecture.** Pointed pyramids and spires were removed. The twelve
  shared mesh signatures use rounded towers, layered terraces, light bands and
  orbital machinery. Brighter brushed surfaces and restrained horizon placement
  replace the large unlit, faceted forms. City towers appear on the lower horizon;
  only symmetrical orbital hardware frames the top. The authored world backdrops
  remain. The existing two pooled banks, three depth layers and original flight
  camera are reused; no gameplay colliders or extra flight cameras were added.
- **Clean retry feedback.** Starting or retrying a flight immediately clears the
  previous score/crystal/world-entry message. Run rules and rewards are unchanged.

## Files to edit

- `Assets/Scripts/SkyPulseNativeGame.Depth.cs`: visible shaft/collar seating.
- `Assets/Resources/SkyPulse/Shaders/EndlessRouteMetal.shader`: travelling LEDs.
- `Assets/Scripts/SkyPulseFlightEnvironment.cs`: rounded mesh profiles, placement
  and pooled parallax; `FlightArchitecture.shader`: surface light and haze.
- `Assets/Scripts/SkyPulseNativeGame.cs`: presentation reset in `BeginFlight`.
- `Tools/SkyPulsePipeFinishChecks.cs`: render tests and real-flight preview driver.
  `SkyPulseDepthChecks.cs`: geometry, light clock and retry-feedback regression.
  `check_dimensional_preservation.py`: explicit presentation-only reset allowlist.

## Validation and evidence

The final Unity run exited 0 with 23 pass groups. Automated results and exact
source hashes are in `QA_RECORD.json`.
The new render test compares the former join with the seated join in the same
camera: the negative control shows 13 top / 12 bottom background-hole samples;
the repaired joins show zero across the sampled shaft width. LED phases produce
over 15,000 changed pixels per test view with no geometry movement. This is a
specific render regression, not a proof of every possible display condition.

Existing tests retain all twelve world loads/transitions at 30/60/120 simulation
steps, original collisions and economy, automatic Relay gates, instant once-only
Blackout progression, pointer/UI separation, rewards, saves and bird progression.
Warmed scenery updates allocate no managed memory. The protection check keeps
235 unique Unity GUIDs and the original scenes, rule files and five audio cues.

`endless-repaired-flight.mp4` is **actual Unity-rendered gameplay**, 360 frames /
12 seconds / 30 FPS / 750×1334. The QA pilot uses normal tap buffering and the
original flight/collision loop. It shows six authored wing poses and six scored
passes. It is not an iPhone recording or App Store media. Preview frames and the
render-test images are kept beside this file.

The earlier local Build 18 launched on the paired iPhone, where the user reported
these presentation issues. A successful install/launch does not constitute device
acceptance. Physical checks for the corrected candidate, speaker/headphone sound,
sustained frame rate, memory and thermal behaviour remain necessary. The earlier
app-only performance trace timed out waiting for the phone; no FPS result is
claimed. The pre-existing Unity Editor Search indexing exception is recorded
separately from runtime checks.

No Apple upload, review submission, public release, main merge, website change,
new paid asset or capture-permission change is part of this follow-up. Apple's
unidentified similarity allegation remains unresolved pending its clarification.
