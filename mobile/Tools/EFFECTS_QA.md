# Beta effects visual QA — 6 September 2026

Result: the requested effects pass the Unity editor rendering review.

## Corrections

- Removed all flight trail and rear thrust renderers, construction, update and reset paths.
- Fixed the shared soft glow texture: SmoothStep previously interpolated between 0 and the radius, leaving alpha in the square texture corners. Distance is now normalised before interpolation, producing transparent edges.
- Replaced the flat event flash with a restrained halo and fine expanding ring behind the bird, with immediate initialisation, event-specific duration and complete fade-out.
- Aegis uses a fine hexagonal shield with corner anchors. Time Pulse uses three clock arcs and timing marks. Crystal Magnet uses opposed field lines. Reduced motion stops field rotation and breathing.
- Restart now clears active power field renderers immediately.

## Evidence

Final run: `artifacts/effects-qa/20260906-165525/PASS.txt` (paths relative to repository root).

216 actual 540 × 960 Unity Camera.Render images cover three worlds × two motion settings × six scenarios × six lifecycle samples. The harness invokes real collection and scoring handlers, checks for removed trail/thrust objects, and asserts flash expiration, timed power expiration, shield consumption, and restart/menu cleanup.

Reviewed close-up contact sheets show no rectangular flash boundaries, no white trail strip, readable bird artwork, distinct power field silhouettes, and clean expired states. The live Unity iOS Simulator view was also inspected during ordinary flight.

Review images: `artifacts/effects-qa/normal-overview.png`, `reduced-overview.png`, `world-0-sequence.png`, `world-1-sequence.png`, `world-2-sequence.png`, and `expiration-overview.png`.

## Scope

This pass covers the bird trail and the requested event/power-up effects. It is not a full-game release certification. The exported iOS build has not been installed or reviewed on a physical iPhone in this pass; App Store submission was not performed.
