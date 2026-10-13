# Validation record

Unity 6000.6.0f1, isolated Editor QA. The final suite includes pure models, actual game dispatch, PointerEventData, UI raycasts and rendered screens.

- Same-frame crystal collection and swept gate impact at 30/60/120 FPS: matching gate opens before impact, with no shield or animation delay.
- All four Relay circuits completed by a simple flap pilot at 30/60/120 render cadence over the existing 120 Hz simulation; no gate hazard hits.
- Assigned gate identity, spare/duplicate cells, invalid IDs, paused collection, reset accounting and completion events.
- Fresh vs held press, touch/simulated mouse suppression, completion-frame fence, actual interactive pause raycast vs decorative HUD, immediate flap, pause input exclusion.
- All eight live Blackout solutions: increasing difficulty, exactly-once persistence, zero intermediate results/Next controls/wait, proper final result/replay, stable 49-control pool.
- Actual pointer-down response, deliberate follow-up during movement, held pointer rejection, stale generation rejection, queued completion-frame rejection.
- Auto-breach costs, exhaustion/failure, retry resets, keys/phase mechanics, save interruption, wallet and legacy progress preservation.
- Transport animation at 30/60/120 FPS, paused ambient/transport freeze, Reduced Motion immediate movement.
- Warmed procedural Blackout meshes measured through cached delegates with VertexHelper buffers: zero managed allocations in the measured mesh loops. Frame/render performance on hardware is not inferred from this check.
- Portrait captures at 750×1334, 1320×2868 and 1640×2360: shared Endless camera/interface width, text fit, mode-specific controls, guides and animated previews.
- Existing beta, release-edge, world, legacy migration, daily journal/reward, presentation and Blackout model suites retained.

The Unity Editor Search indexer reports an unrelated startup indexing exception. It is retained in the log; game scripts compile and the guarded QA runner independently requires every PASS marker and exit code zero.

## Physical device checks still required

These results do not constitute physical-device gameplay or a 60 FPS hardware certification. On TestFlight build 10 verify real thumb input/UI exclusion, crystal-to-gate timing, sound/haptics settings, all Relay circuits, all Blackout sectors/final flow, background/resume, retry, saved medals/collection after relaunch, and readability on iPhone/iPad. Earlier build 7 owner checks and build 9 installations do not validate build 10.
