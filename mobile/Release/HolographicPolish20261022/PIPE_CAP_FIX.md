# Pipe-cap contour correction

The original collar PNG contains transparent corners around its elliptical rim.
The 3D material previously replaced those texels with opaque dark metal, which
made a rectangular black patch visible on the cap.

The collar now clips fully transparent source pixels before writing colour or
depth. Alpha-to-coverage gives the contour smoother multisampled edges without
mixing transparent pixels into a black fringe. The shaft stays opaque. Original
artwork, mesh dimensions, collider sizes, gate spacing, flight behaviour and
animated cyan/pink LED strips are retained.

## Verification

- An isolated camera renders the actual shared collar mesh/material over a
  contrasting scenery plane drawn **after** it. This checks both visible colour
  and unwanted depth occlusion.
- A negative control switches contour handling off and reproduces the old patch.
- Both upper/lower orientations, three LED phases, four corners and nine samples
  per corner pass: **216 corner/depth comparisons**, plus six solid-centre checks
  that prevent a falsely passing invisible cap.
- Existing collider bounds, 30/60/120 FPS animation clocks, Reduced Motion, all
  three Homes and player-experience checks pass in the disposable Unity project.
- The exact-source iOS IL2CPP export passes. `SOURCE_MANIFEST.json` matches all
  439 source entries. Metadata remains unchanged and its consistency checks pass.
- The refreshed four-second Unity LED preview is fully decoded (120 frames,
  750×1334 at 30 FPS); its middle frame and the upper/lower isolated renders were
  visually inspected.

`QA_RECORD.json` distinguishes this focused fix from the earlier comprehensive
checks. Source changes are limited to `SkyPulseNativeGame.Depth.cs` and
`EndlessRouteMetal.shader`; no new external asset or save-format change.

This remains a local build 18 candidate. Physical device testing is pending;
TestFlight remains build 17 and public App Review remains on hold.
