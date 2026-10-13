# Build 11 validation

- Existing one-thumb Relay/Blackout, pause/resume, collision ordering, charge accounting, progression, rewards, saves, legacy migration, pooling/allocation and portrait layout suites retained.
- 366 deterministic daily decks: distinct names, adjacent-day refresh, bounded heights, gate spacing and exit position, valid progression for all four generated courses.
- Current-day medals saved once; same-day restart preserves routes/medals; UTC midnight resets daily progress and changes routes; clock rollback cannot reroll; failed refresh keeps the existing deck; cross-midnight completion does not mark a new day's course clear; wallet and legacy medals untouched.
- Actual result-button dispatch: next daily mission starts directly, all three Main Menu buttons restore the appropriate Home, final daily circuit has no fifth-mission button.
- Revised Endless gap/spacing still passes route bounds across 1,300 gates, capped smooth difficulty, score/world/save checks and original physics assertions.
- Real flight pilot completes all four current daily circuits at 30/60/120 render cadence over the existing 120 Hz simulation; same-frame pickup/collision checks retained.
- New result-screen renders and text-fit checks; existing 750×1334, 1320×2868 and 1640×2360 portraits and animated Home/gameplay previews retained.

Unity Editor Search's known startup indexing exception remains in the log; game compilation and guarded QA pass markers are separately checked.

## Physical checks still needed for build 11

On TestFlight verify the revised Endless cadence/openings, daily Relay selection and Next Mission, all result Main Menu buttons, actual thumb input/sound/haptics, Blackout instant progression, pause/retry/background behaviour and saved progress after relaunch. Actual overnight refresh and sustained device FPS are not inferred from the Editor simulations. Build 10 owner feedback requested these enhancements; no full build 10 or build 11 physical pass is claimed.
