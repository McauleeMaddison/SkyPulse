# SkyPulse — Build 22 owner acceptance checklist

Candidate: **SkyPulse 1.0.0 (22)** · Unity 6000.6.0f1 · runtime source `1fd3dfa`.
Prepared 17 November 2026. Installed and launched on iPhone 17 Pro Max;
also processed in the existing TestFlight group. [Release evidence](RELEASE_RECORD.json).
**Public App Review remains on hold until you finish testing and approve submission.**

The game is ready to begin this checklist. Passing compilation, QA and installation
is not a substitute for your visual/gameplay acceptance. No runtime changes are
being slipped into this candidate while you review it. Use a new numbered build
and repeat affected checks if a bug requires a runtime fix.

Record PASS / FAIL / NOT TESTED with a reason in [TEST_RESULTS.md](TEST_RESULTS.md).
Leave unchecked anything you cannot reach or purchase yet. Use owned birds for
flight tests and locked-bird previews for appearance. Keep your real save; do not
uninstall, clear storage, change the clock, add QA currency or force unlocks.

## Start here — first ten minutes

- [ ] Note version/build, phone model, iOS version, brightness and sound/Reduced Motion settings.
- [ ] Endless: play one run with isolated taps, then quick taps. Each tap gives lift and a full wing stroke; the selected bird stays correct.
- [ ] Endless: check top/bottom pipe joins, LEDs, trail, collision explanation, pause/resume, Retry and Main Menu.
- [ ] Relay: collect the numbered crystals, see the matching hoop turn red-to-blue and pass through; test pause/retry and Next Mission after a clear.
- [ ] Blackout: inspect the named restoration goal, try a free PLAN preview, make turns and complete a reachable puzzle. The next puzzle is immediately playable.
- [ ] Hangar: swipe, equip a different owned bird, start a run, return Home and reopen the app. The equipped identity and progress persist.

If this smoke check fails, record the reproduction and stop that affected flow;
continue independent visual checks. There is no need to repeat a known failure dozens of times.

## Suggested sessions

Session A: Homes, birds, worlds/pipes and all three gameplay modes (sections 2–5, 7–8).
Session B: upgrades, saves, interruption and longer-session/device checks (sections 6, 9–10).
Release checks (11–12) stay separate until the game passes. Unavailable iPad,
locked purchases or unreached worlds remain NOT TESTED; they do not become passes.

## Already verified separately

- Exported source: all 2,559 entries match the Desktop project; 71 runtime scripts match QA.
- Automated Editor compilation and focused controls, gates, progression, birds, touch and world suites passed.
- All twelve Editor worlds/transitions were checked at simulated 30/60/120 Hz.
- Signed app/archive identities and strict signatures verified; app/UnityFramework symbols match.
- All 44 preference values were identical before/after installation. Later changes were daily rollover and your confirmed Neon Finch selection.
- Build 22 processed in TestFlight; current listing copy and selected build saved/reloaded. No public review submitted.

Open diagnostics: Editor-only Unity SearchDatabase exception; missing UnityRuntime
framework dSYM warning (upload succeeded). See [native evidence](NATIVE_VERIFICATION.json).
Those diagnostics are not marked fixed. Actual device FPS, heat, memory, audio,
haptics, all visual approvals and current store media are still to be assessed.

## 1. Project and compilation

- [ ] Open the mobile project using its recorded Unity version.
- [ ] Confirm the shipping SkyPulse scene opens correctly.
- [ ] Resolve game-related Console errors and investigate warnings.
- [ ] Confirm all artwork, materials, shaders and sounds load.
- [ ] QA only: test first-run behaviour in a disposable save or separate test device; never erase the owner’s progress.
- [ ] Confirm the installed Build 22 opens your existing save with expected scores, wallet, birds and upgrades.
## 2. Home screens and navigation

- [ ] Endless Route is selected on a fresh launch.
- [ ] Tapping the top-left mode badge opens the mode list.
- [ ] All three modes open the correct Home screen and gameplay.
- [ ] Each Home has its own clear visual identity and smooth animation.
- [ ] Remove unintended black squares, overlapping layers and broken transparency.
- [ ] Remove placeholder, temporary, repeated and misleading text.
- [ ] Keep labels readable and buttons comfortably usable with one thumb.
- [ ] Blackout does not show irrelevant Bird Hangar or Tech Tree controls.
- [ ] Check every Back, Close, Pause and Main Menu button.
- [ ] Check layouts around notches, Dynamic Island and screen edges.
## 3. Endless Route gameplay

- [ ] Each deliberate tap produces one responsive flap.
- [ ] Tapping interactive UI never causes an unintended flap.
- [ ] Every owned bird displays a full upstroke and downstroke on taps, then settles to glide.
- [ ] Test isolated taps and rapid repeated taps; pause holds the pose and retry resets it.
- [ ] No flat duplicate appears over the solid 3D bird; each bird keeps its own crash artwork.
- [ ] Preserve the tested screen width, flight handling and pipe spacing.
- [ ] Pipe shafts meet their collars cleanly, without visible overlap.
- [ ] Pipe caps have no black patches, flickering or broken edges.
- [ ] Cyan and pink LED strips animate cleanly.
- [ ] The trail attaches naturally to the bird and follows its banking.
- [ ] The trail has soft edges and a tapered end, with no white rectangle.
- [ ] Bird, obstacles and collectibles remain readable against bright scenery.
- [ ] Pipe and ground collisions match what the player sees.
- [ ] Passing a gate awards the correct score exactly once.
- [ ] All naturally reached worlds and transitions look correct. Record each in the world table; leave unreached worlds untested on device. All 12 have separate Editor render/transition evidence.
- [ ] All seven pickups—Aegis, Time Pulse, Crystal Magnet, Crystal Echo, Route Lens, Supply Cache and Gate Link—behave as described.
- [ ] Supply Cache banks 5 crystals once; Supply Decoder levels raise that to 7, 9 and 11.
- [ ] Gate Link pays 2 crystals for each of the next 3 clean passes; Link Reactor raises that to 3, 4 and 5 per gate. Shield-skipped gates do not count.
- [ ] The Link HUD shows remaining gates; retry clears it and already banked crystals remain.
- [ ] Pulse Matrix adds 1, 2 or 3 seconds to Time Pulse, Magnet, Echo and Lens, while Echo keeps its 6-crystal reward cap.
- [ ] Crystals, bonuses and result totals match the actual wallet increase.
- [ ] Game over, retry and Main Menu work without retaining old run effects.
## 4. Pulse Relay gameplay

- [ ] Tap-to-flap works throughout the playable area.
- [ ] No manual FLAP or PULSE gameplay buttons are required.
- [ ] Random crystal placement stays reachable through normal play.
- [ ] Gate positions and openings remain readable and achievable.
- [ ] Required crystals unlock their matching gate automatically.
- [ ] Near-gate final-crystal collections allow immediate passage. Exact same-frame ordering has automated coverage; report any phone collision that disagrees.
- [ ] Charge is spent once and cannot accidentally unlock another gate.
- [ ] Locked gates show red lasers; unlocked gates show blue confirmation.
- [ ] Opening animations never delay collision clearance.
- [ ] The bird visibly passes through the hoop’s opening with correct depth.
- [ ] Crystals look faceted and dimensional, with readable collection feedback.
- [ ] Gate lighting, particles and sound give clear confirmation.
- [ ] Missing required charge prevents passage consistently.
- [ ] Retry resets crystals, charge, gates and mission progress correctly.
- [ ] Completing a mission offers the next mission directly.
- [ ] Daily circuits refresh at the documented UTC reset without losing saved rewards.
- [ ] Completed daily rewards cannot be claimed twice.
## 5. Blackout Protocol / Signal Heist

- [ ] Each puzzle explains its objective clearly and briefly.
- [ ] Every required control is comfortably reachable with one thumb.
- [ ] Prism rotation responds immediately and predictably.
- [ ] Energy beams, receivers, threats and powered states remain readable.
- [ ] The 3D board feels coherent rather than visually crowded.
- [ ] Beam and cost previews match the move that will actually happen.
- [ ] Previewing a move does not spend resources or alter saved progress.
- [ ] Trace and failure costs match the displayed rules.
- [ ] Resource limits allow failure and meaningful restart decisions.
- [ ] Each later puzzle adds meaningful difficulty.
- [ ] All eight vaults remain solvable within their intended budgets.
- [ ] Completion records rewards and progress exactly once.
- [ ] Intermediate puzzles advance immediately without a Continue button.
- [ ] Completion feedback does not block the next puzzle.
- [ ] Held touches and queued taps cannot accidentally affect the next puzzle.
- [ ] Completing the final vault opens the proper completion flow.
- [ ] Pause, failure, restart and returning to Home work correctly.
- [ ] The city overlook stays above the puzzle and never obscures an interactive optic.
- [ ] Each new vault medal names a different lighting network and visibly restores it in the flight worlds; replay does not award it twice.
- [ ] Story vault progress and the optional daily case remain separate after reopen/restart.
- [ ] Today’s daily case resumes correctly. Test subsequent authored cases on later UTC days; do not change the phone clock or seed the real save.
- [ ] An earned Gold Archivist finish appears only after all four authored daily designs have Gold; leave this untested until legitimately earned.
## 6. Tech Tree and Core upgrades

- [ ] The connected tree pans smoothly in both directions.
- [ ] No unwanted scrollbar or mouse-wheel navigation remains.
- [ ] Branches connect cleanly and have consistent thickness and depth.
- [ ] Labels remain minimal, readable and holographic.
- [ ] Installed, available and locked levels are easy to distinguish.
- [ ] The next purchasable level has a clear touch target and exact price.
- [ ] All 12 families / 36 levels are reachable by dragging, including the three new fourth-tier families.
- [ ] New-family prerequisites require level 2 in Gravity Well, Archive Engine or Apex Matrix.
- [ ] New upgrade quotes include Core discounts and survive closing/reopening. Inspect locked items without spending; defer purchases you cannot yet afford.
- [ ] Inspecting an upgrade does not purchase it.
- [ ] Purchases require the intended confirmation.
- [ ] Core levels have distinct colours and halo animations.
- [ ] Core discounts change the actual eligible prices correctly.
- [ ] Every purchased upgrade produces its stated gameplay benefit.
- [ ] Insufficient balances and maximum levels are handled clearly.
- [ ] Repeated taps cannot purchase the same level twice.
## 7. Bird Hangar, training and breeding

- [ ] Hangar swiping, selecting, unlocking and equipping work smoothly.
- [ ] Each bird has a clear identity and readable trait information.
- [ ] All fifteen founder birds have distinct solid 3D Hangar previews with centred rotation.
- [ ] Every rotating Hangar bird uses Voltcrest’s display wing cadence, sweep and outward feather-fan pose; no wing/foot clipping is visible.
- [ ] Gameplay still responds to taps rather than the continuous Hangar animation.
- [ ] Equip an owned bird, start a run, then retry and reopen: the selected identity stays correct.
- [ ] Training previews the exact cost and benefit before purchase.
- [ ] Trained birds receive the advertised reward benefits.
- [ ] Each completed training level contributes 2% founder-bird unlock savings, capped at 20%.
- [ ] Displayed bird prices match the actual deducted amount.
- [ ] Breeding requires two eligible owned parents.
- [ ] Inherited traits and the final breeding cost are previewed accurately.
- [ ] Breeding spends once, retains both parents and creates the promised offspring.
- [ ] The offspring appears in the Hangar and can be equipped.
- [ ] Duplicate owned combinations are handled correctly.
- [ ] Training, offspring, ownership and equipment survive reopening the app.
- [ ] Per-bird Ion/Prism/Aurora cradle tiers match Endless bests of 5/15/30 gates or successful Relay lengths of 2/4/5 gates.
- [ ] Results show a tier only when newly earned; replay shows the next target. Mastery does not change handling or wallet prices.
## 8. Graphics, animation and sound

- [ ] Lighting feels consistent across the bird, scenery, pipes and hoops.
- [ ] Materials have no missing textures, pink shaders or rectangular artefacts.
- [ ] Animation remains smooth during transitions and rapid input.
- [ ] Effects support gameplay without obscuring hazards or puzzle states.
- [ ] Avoid excessive bloom, screen shake and distracting particles.
- [ ] Reduced Motion removes unnecessary movement while preserving clear feedback.
- [ ] Sound effects play at appropriate levels without unpleasant repetition.
- [ ] Sound and haptic settings are respected and saved.
- [ ] Pause and backgrounding stop or suspend effects appropriately.
## 9. Saves and interruptions

- [ ] Scores, crystals, birds, upgrades, medals and settings save correctly.
- [ ] Retrying cannot duplicate rewards or deduct purchases again.
- [ ] Closing and reopening preserves completed transactions.
- [ ] Backgrounding during gameplay or a purchase resumes safely.
- [ ] Ordinary background/reopen saves retain progress. Injected write/flush failures belong only in disposable QA, where Retry Save recovery already passed.
- [ ] Owner confirms retained progress in-game after the Build 21 → 22 update. The private backup comparison already found all 44 preferences identical across installation.
- [ ] QA save-reset tools never run against the real player save.
## 10. Device performance and usability

- [ ] Test on a physical iPhone and iPad.
- [ ] Check the smallest supported portrait layout.
- [ ] QA/device profiling: compare behaviour at supported 30/60/120 FPS settings. Fixed-step Editor simulations already passed; they do not establish actual phone FPS.
- [ ] Measure sustained frame times during a longer play session.
- [ ] Check memory use does not keep growing after repeated retries and mode changes.
- [ ] Check heat, battery use and loading times.
- [ ] Test readability at low brightness and in daylight.
- [ ] Check fast tapping, accidental multitouch and app interruptions.
- [ ] Confirm no freezes, crashes or stuck screens during normal play.
## 11. Metadata, artwork and support

- [ ] App name, description and keywords accurately describe the current build.
- [ ] Remove stale instructions and claims about unfinished features.
- [ ] Screenshots and preview footage match the release candidate.
- [ ] Keep creator details, bundle identity and support contact consistent.
- [ ] Support and privacy pages load over HTTPS and work on mobile.
- [ ] Privacy wording matches the app’s actual data handling.
- [ ] Retain original artwork exports, generation history and purchase receipts.
- [ ] Investigate third-party logos, references or assets with unclear permission.
- [ ] Do not claim exclusive AI-art rights or global metadata uniqueness without evidence.
- [ ] Keep Apple’s unidentified similarity allegation recorded as unresolved until clarified.
## 12. Release preparation — submission remains on hold

- [ ] Finish device testing on the exact candidate intended for release.
- [ ] Resolve failures and repeat the affected checks.
- [ ] Refresh QA records and source hashes after runtime changes.
- [ ] Confirm the accepted candidate still matches the retained signed Build 22 archive; any runtime fix needs a new unused build number.
- [ ] Review the retained signing/privacy evidence and open UnityRuntime dSYM warning in NATIVE_VERIFICATION.json. Do not mark that warning resolved.
- [ ] Confirm TestFlight shows 1.0.0 (22). Its upload and processing are already verified; no additional upload is needed to start this checklist.
- [ ] Verify the processed TestFlight build on the phone.
- [ ] Confirm saved App Store copy and selected Build 22 remain current; replace historical screenshots/preview and build 7 walkthrough with accepted current device media.
- [ ] Review Apple’s reply and address its specific concerns.
- [ ] Verify the confirmed App Review contact macwebstudio26@gmail.com is saved live, resolve applicable country/compliance notices, then obtain renewed owner approval before public review. Do not press Add for Review during acceptance.

## World-by-world visual record

At each naturally reached world, check inhabited islands/bridges, lit windows,
sky/cloud colour, themed pipe/collar joins and a clear flight path. Compare the
[actual Unity renders](../../Development/Current/UnityAcceptance/Twelve-Worlds.png)
if useful. Editor images do not establish phone acceptance.

| World | Editor evidence | Phone appearance | Phone transition | Issue ID |
| --- | --- | --- | --- | --- |
| 01 Neon City | Captured / automated | Not tested | Not tested | — |
| 02 Aurora Rise | Captured / automated | Not tested | Not tested | — |
| 03 Solar Drift | Captured / automated | Not tested | Not tested | — |
| 04 Midnight Tide | Captured / automated | Not tested | Not tested | — |
| 05 Velvet Dawn | Captured / automated | Not tested | Not tested | — |
| 06 Crystal Night | Captured / automated | Not tested | Not tested | — |
| 07 Jade Horizon | Captured / automated | Not tested | Not tested | — |
| 08 Violet Rain | Captured / automated | Not tested | Not tested | — |
| 09 Eclipse | Captured / automated | Not tested | Not tested | — |
| 10 Cobalt Storm | Captured / automated | Not tested | Not tested | — |
| 11 Amber Skies | Captured / automated | Not tested | Not tested | — |
| 12 Polar Glow | Captured / automated | Not tested | Not tested | — |

## Finish and return

Return the checked list, issue log and useful recordings. We will fix reproducible
failures, repeat the affected checks, verify performance and fresh store media,
then make an evidence-based App Review readiness assessment. Apple’s earlier
unidentified similarity/account concern also needs specific clarification. No
checklist or metadata audit can guarantee Apple acceptance.
