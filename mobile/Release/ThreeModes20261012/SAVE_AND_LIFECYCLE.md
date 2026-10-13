# Save and lifecycle contracts

Legacy wallet, score, birds, upgrades, preferences and farthest-world keys retain their names and migration paths. New data uses `skypulse.daily.v1` (+ backup), `skypulse.relay.v1`, `skypulse.blackout.v1` and `skypulse.home-protocol.v1`. The four entries in each mode's medal array have stable catalogue order; appending the fourth entry also preserves three-entry development saves. Future reordering requires a migration.

The UTC period is a monotonic saved day number. A forward day starts the same four stable task IDs; reopening cannot reroll them. A clock rollback retains the greatest saved day and its claims. Launch, focus changes, progress and claims check rollover. This is an offline clock policy, not a server-enforced economy.

A claim marks its task and stores an absolute post-credit wallet target in the daily journal before wallet mutation. Pending intent is replayed, the wallet is flushed, and then the journal is cleared. Completed state is also backed up. Credits use an explicit reward source. Daily claims never use BankCollectedCrystals, increment run pickup totals, alter high scores or invoke result multipliers. Pending transactions recover before an ordinary wallet save or an upgrade/bird debit, preventing an absolute recovery target from undoing a later purchase.

PlayerPrefs/filesystem failures are logged and claims/debits fail closed while a journal remains pending. The system cannot guarantee recovery from loss of every local save copy, app deletion or deliberate device tampering.

Endless uses its original FlightState and simulation. The protocol dispatcher returns before flight input/simulation for puzzles and auxiliary screens. Relay sessions reuse buffered flaps, gravity, animation, bounds and Aegis effects, while separate course geometry/objectives are simulated. Puzzles use adjacent-node callbacks only. New session states freeze on pause; return from background requires explicit resume. Switching clears previous sessions/world objects/tactical effects, and finalized results are guarded against repetition. Returning Home saves no new completion reward.

Daily events originate at an actual scored Endless gate, a successfully activated relay, a finalized introductory puzzle success or actual crystal-pickup collection. Shield skips, UI navigation, reward claims, result redraws and retries do not emit those events by themselves.
