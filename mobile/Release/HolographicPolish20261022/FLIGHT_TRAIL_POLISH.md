# Moving flight preview and exhaust polish

The visible Endless shaft previously used the full collision-body height, which
extended through the collar. It now stops 0.04 world units inside the far shoulder.
The upper/lower opening, original collar silhouette, pipe widths, colliders,
spacing and flight forces are unchanged. All legacy collar glow layers are also
retired when the 3D surface is active.

The previous engine effect used constant-width, flat-coloured quads and a white
rectangular nozzle bar. Flight now uses a banked body-frame socket, a soft tapered
plume and a continuous coloured ribbon. Adjacent sections share their edge
positions, with bounded joins and a transparent pointed tip. Fixed 60 Hz history
samples account for their age within each rendered frame, including both samples
at 30 FPS. Geometry, colours and UV buffers are reused. Flap sparks are restrained.
Reduced Motion keeps a compact static plume and clears the history/particles.
The material's new feathering is opt-in, preserving Signal Heist's optical fibres.

## Evidence

- `endless-flight-preview.mp4`: **12 seconds, 360 decoded frames, 750×1334 at
  30 FPS**. This is an actual Unity Editor Endless flight simulation driven by
  ordinary buffered taps, not a stationary material inspection or a phone video.
  The QA pilot makes 19 taps, displays all six authored wing poses and earns six
  points through real collision-checked gate passes. Bird height changes by 6.51
  world units. No gate positions, collision shapes, flight forces, velocity,
  invulnerability or score are injected by the recording fixture.
- `endless-flight-frame.png` is the decoded middle frame. The middle frame and
  multiple original frames were visually inspected for joins and exhaust shape.
- Focused Unity tests check continuous ribbon joins, tip taper, opt-in soft
  material, reset, Reduced Motion and equivalent history at 30/60/120 FPS.
- Existing cap contour negative control and 216 scenery/depth samples pass.
  New bounds checks verify the shaft shoulder joint and original far ends;
  collar bounds continue matching the physical gap edge.
- Cinematic zero-allocation, pause, Relay charge/unlock, crystal mesh and Heist
  presentation checks pass. The live one-thumb suite also passes.
- The initial focused attempt exposed an upper/lower pipe selection error in the
  new test fixture. That fixture was corrected, then the complete focused run
  passed. No failing attempt is recorded as a successful test.

`QA_RECORD.json` identifies the current scope and earlier comprehensive results.
The fresh iOS IL2CPP export and manifest hashes are in `RELEASE_RECORD.json`.
These checks establish Editor behaviour and export consistency. Physical iPhone
and iPad readability, motion comfort, sustained frame times and thermals remain
to be checked. This local build 18 update is not uploaded; TestFlight remains
build 17 and public App Review remains on hold.

## Suggested next improvements

First reduce competition from the brightest background areas near the player,
then make the exact collision location clearer with a short, restrained impact
cue. Tune those changes on the phone before adding more effects. These are
proposals, not additional changes in this pass.
