# SkyPulse — current local review

13 November 2026. **Local Unity source candidate; iPhone Build 21 is unchanged.**
Public App Review remains on hold. These previews are actual Unity renders,
with no AI repainting. Open [the gallery](index.html).

## Implemented

- **Blackout city overlook:** five detailed authored districts and two skyways
  joined to the actual transformed landing decks. The wide city window is above
  the puzzle, outside every interactive optic target. A quieter surround keeps
  the live routes readable. The tiny miniature among the board pieces is gone.
- **Eight distinct permanent rewards:** dock approach lights; cargo district
  windows; transit markers; relay beacons; power-grid window banks; skybridge
  lights; cliff shield markers; rooftop uplinks. Existing medals persist each
  lighting group in all twelve Endless worlds. These add to the original world
  lighting. Free PLAN previews do not save/award restoration; actual partial
  core power previews the active system in the city window.
- **Next-goal cards on all Homes:** a clear next mastery target, daily Relay
  circuit or named restoration. Existing completed daily tasks can be claimed
  through the card, using the existing five-crystal journal transaction. The
  four existing refreshed Relay courses remain the same system, not a duplicate.
- **Earned cradle lighting:** each bird reaches Ion/Prism/Aurora with a best
  Endless score of 5/15/30 gates or a successful Relay circuit of 2/4/5 gates.
  Mastery is per catalogue bird, including offspring; it changes the display
  cradle, not the approved model, wing profile, flight stats or currency prices.
- **Optional daily heists:** four genuinely authored layouts use the same
  mirrors/splitters/relays/shutters/sensors and trace rules. Their verified Gold
  minima are 4, 4, 4 and 7 trace. One design rotates per monotonic UTC day, with
  no entry fee, timer or streak penalty. Saved attempts resume; restart across
  midnight binds the right date/case. Gold on all four designs unlocks shared
  Archivist cradle lighting. These cases cannot complete a story vault or replace
  its saved attempt. The new local `skypulse.player-journey.v1` record stores
  cosmetic mastery and optional attempts; it is not a second spendable economy.
- **Immediate puzzle picture:** binding a new puzzle also publishes its rendered
  board immediately, preventing the previous case's texture appearing under new
  labels. No extra NEXT/CONTINUE or celebration wait is introduced.
- **Reported Console error:** the stale nine-node check now validates twelve
  upgrade families (36 levels). Duplicate IDs, malformed costs and dependencies
  still fail validation. The unused slow-HUD field and retired flat-deck renderer/
  helpers were removed. How to Play and the editing guide reflect the new features.

## Review

- [Blackout gameplay and daily case](Blackout-Current.png)
- [Three Homes and next goals](Home-Goals.png)
- [Earned Hangar cradle finishes](Cradle-Rewards.png)
- [Eight city restoration purposes](City-Progression.png)
- [Motion preview](Blackout-Motion.gif) — fixed-step Editor rendering, not measured device FPS
- [Full verification and source hashes](QA_REPORT.json)
- [Cleanup allowlist and moved-folder lookup](CLEANUP_REPORT.json)

## Verification and limits

279 city/restoration checks and 86 journey assertions passed, plus the existing
Blackout, raised-optic, restoration-purpose and touch suites. The broader pass
completed 3,424 final-polish, 188 Field Tech and 261 native-bird assertions. Coverage
includes exhaustive authored-case solutions/budgets, same-frame progression,
preview/commit, restart, final completion, duplicate events, saved state, midnight
retry, unchanged wallet values for mastery, exactly-once daily claims, actual
44-point safe/raycastable targets on small/notched phones and iPad, simulated
30/60/120 Hz rotors and warm allocation checks. Every isolated reward also changes
actual rendered world pixels differently from the unearned baseline.

The approved wing method, physical rig, bird assets, Relay gate/deck/rules,
bird/economy model and app identity settings remain unchanged. Read the source
hash report for precise boundaries. Copy guard and all 90 original bird-frame
registrations passed. Required reference, fallback, hit and unlock artwork remains.

**Remaining editor diagnostic:** Unity 6000.6.0f1's `SearchDatabase.EnumerateAll`
throws an editor-only `ArgumentOutOfRangeException` during batch startup even after
regenerating the isolated search cache. It is recorded separately; it was not
suppressed or patched in the engine. The final broad run has no C# compiler errors
or warnings and no recurrence of the reported Tech-family error. This is not a
claim that every Unity engine diagnostic has been resolved.

Owner visual approval and subsequent iPhone/iPad acceptance/profiling remain.
No new build was installed or uploaded, and nothing was submitted to public review.
The installed release contract continues to describe immutable Build 21.

## Clean navigation

Use `mobile/EDITING_GUIDE.md` and the root README. VS Code groups the main game
partials. Older studies are under `Development/Archive`; their historical hash
reports retain original paths, with the moved-folder lookup in the cleanup record.
The cleanup removed approximately 34.67 GiB of
reproducible caches and raw frame sequences. Current Build 21, the verified signed
Build 20 rollback, save backups, videos/reports, source artwork and editable Unity
workshops are preserved. `Library`/`Temp` were retained while Unity was active.
