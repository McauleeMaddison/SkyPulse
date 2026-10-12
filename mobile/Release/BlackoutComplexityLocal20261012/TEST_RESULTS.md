# Local validation

Unity 6000.6.0f1 compiled and ran the updated fifteen runtime scripts in an isolated QA project. The full run returned exit zero with all eleven expected pass groups, covering mission models, Blackout models/live UI, 1,300-gate beta regression, release edges, old saves, integration, world gameplay, mission/presentation captures and world checks.

Focused checks verify all eight authored solutions against the live session model; strictly rising minimum trace; nondecreasing board/core counts; and nonincreasing spare trace. New cases exercise matching and mismatched keys, key/lock movement costs, console toggling and return visits, inactive-gate penalties, failed breach charge/trace costs, immediate trace exhaustion, legal extraction at the exact boundary, failure-screen/retry behavior, pause, duplicate touches, cached controls, and live phase/key telemetry. The live fixture never awards campaign progress.

Progression checks preserve both build 9's original medal JSON and the previous local v1 campaign. The new v2 campaign starts at sector one, rejects locked launches/skipped completions, survives reloads and does not expose an in-memory unlock after an injected flush failure. The established wallet/high-score, result finalization, navigation, background, pause and Reduced Motion checks also pass.

The original Endless protection check passes all twenty-seven protected methods and seven normalized integration hooks. No package, project setting, artwork, audio, old asset GUID or original Endless source file changed during this iteration.

The first pass used forgiving blocked-move behavior. The owner then requested resource penalties; the final QA reruns the full suite with that change. Both runs are retained. The successful log retains Unity's existing Editor Search indexing exception and service diagnostics. These are separate from game-code compilation failures and deliberately injected persistence faults. The QA source identity, log hashes, preview records and historical build 9 evidence checks are recorded in `LOCAL_RECORD.json`.

A capture-helper assumption about the first console on the solver's route was corrected without changing gameplay. Visual inspection then caught a transient error label overlapping the bottom row on seven-row boards. Feedback now uses the status rail and hides its idle caption during the effect; the full suite was rerun after this correction. Earlier logs and captures remain available separately.

The visual capture pass exercises advanced key/phase layouts at 750 × 1334, 1320 × 2868 and 1640 × 2360, including text-height checks. Physical phone touch comfort, sustained frame pacing, all eight-sector playthroughs and perceived difficulty remain unverified for this new local source. No native candidate was exported or uploaded.
