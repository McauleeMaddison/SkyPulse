# SkyPulse — Build 20 weekend checklist

Candidate: SkyPulse 1.0.0 (20) · physical iPhone 17 Pro Max.
Planned sessions: **Saturday 14 and Sunday 15 November 2026**.
Installation and signing evidence is in RELEASE_RECORD.json beside this file.
The initial phone build was 19; this checklist replaces the stale Build 18 label.

Editor automation passed, including all fifteen solid birds at 30/60/120 Hz,
full tap-stroke motion, gate/puzzle progression, UI and all twelve world/pipe
themes. These are **not** checkmarks for manual device acceptance. Every device
item below starts unchecked so your weekend results are clear.

**Saturday:** sections 1–5, 7 and 8: visual identity, wing strokes, all modes,
world/pipe joins, Hangar selection and comfort.
**Sunday:** sections 6, 9–12: progression, retained saves, interruptions,
longer-session performance, and release-information review.

For each issue record: mode, bird/world/vault, action, expected result, actual
result, whether retry repeats it, and a screenshot/recording when useful.
Preview locked birds in Hangar; gameplay checks can use owned birds. No QA
currency or unlocks have been added to the phone. Keep your real progress;
fresh-save/reset tests belong in a disposable QA save or separate test device.

**Public App Review remains on hold.** TestFlight and public store media remain
separate from this direct device candidate.

1. Project and compilation
- [ ] Open the mobile project using its recorded Unity version.
- [ ] Confirm the shipping SkyPulse scene opens correctly.
- [ ] Resolve game-related Console errors and investigate warnings.
- [ ] Confirm all artwork, materials, shaders and sounds load.
- [ ] Check a fresh launch works without an existing save.
- [ ] Check an existing save loads without losing progress.
2. Home screens and navigation
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
3. Endless Route gameplay
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
- [ ] All 12 worlds and their transitions work.
- [ ] All five pickups—Aegis, Time Pulse, Crystal Magnet, Crystal Echo and Route Lens—behave as described.
- [ ] Crystals, bonuses and result totals match the actual wallet increase.
- [ ] Game over, retry and Main Menu work without retaining old run effects.
4. Pulse Relay gameplay
- [ ] Tap-to-flap works throughout the playable area.
- [ ] No manual FLAP or PULSE gameplay buttons are required.
- [ ] Random crystal placement stays reachable through normal play.
- [ ] Gate positions and openings remain readable and achievable.
- [ ] Required crystals unlock their matching gate automatically.
- [ ] Collecting the final crystal and reaching the gate in the same frame succeeds.
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
5. Blackout Protocol / Signal Heist
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
6. Tech Tree and Core upgrades
- [ ] The connected tree pans smoothly in both directions.
- [ ] No unwanted scrollbar or mouse-wheel navigation remains.
- [ ] Branches connect cleanly and have consistent thickness and depth.
- [ ] Labels remain minimal, readable and holographic.
- [ ] Installed, available and locked levels are easy to distinguish.
- [ ] The next purchasable level has a clear touch target and exact price.
- [ ] Inspecting an upgrade does not purchase it.
- [ ] Purchases require the intended confirmation.
- [ ] Core levels have distinct colours and halo animations.
- [ ] Core discounts change the actual eligible prices correctly.
- [ ] Every purchased upgrade produces its stated gameplay benefit.
- [ ] Insufficient balances and maximum levels are handled clearly.
- [ ] Repeated taps cannot purchase the same level twice.
7. Bird Hangar, training and breeding
- [ ] Hangar swiping, selecting, unlocking and equipping work smoothly.
- [ ] Each bird has a clear identity and readable trait information.
- [ ] All fifteen founder birds have distinct solid 3D Hangar previews with centred rotation.
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
8. Graphics, animation and sound
- [ ] Lighting feels consistent across the bird, scenery, pipes and hoops.
- [ ] Materials have no missing textures, pink shaders or rectangular artefacts.
- [ ] Animation remains smooth during transitions and rapid input.
- [ ] Effects support gameplay without obscuring hazards or puzzle states.
- [ ] Avoid excessive bloom, screen shake and distracting particles.
- [ ] Reduced Motion removes unnecessary movement while preserving clear feedback.
- [ ] Sound effects play at appropriate levels without unpleasant repetition.
- [ ] Sound and haptic settings are respected and saved.
- [ ] Pause and backgrounding stop or suspend effects appropriately.
9. Saves and interruptions
- [ ] Scores, crystals, birds, upgrades, medals and settings save correctly.
- [ ] Retrying cannot duplicate rewards or deduct purchases again.
- [ ] Closing and reopening preserves completed transactions.
- [ ] Backgrounding during gameplay or a purchase resumes safely.
- [ ] Interrupted saves recover without corrupting the player’s progress.
- [ ] Updating from the previous build preserves existing saves.
- [ ] QA save-reset tools never run against the real player save.
10. Device performance and usability
- [ ] Test on a physical iPhone and iPad.
- [ ] Check the smallest supported portrait layout.
- [ ] Compare relevant behaviour at 30, 60 and 120 FPS.
- [ ] Measure sustained frame times during a longer play session.
- [ ] Check memory use does not keep growing after repeated retries and mode changes.
- [ ] Check heat, battery use and loading times.
- [ ] Test readability at low brightness and in daylight.
- [ ] Check fast tapping, accidental multitouch and app interruptions.
- [ ] Confirm no freezes, crashes or stuck screens during normal play.
11. Metadata, artwork and support
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
12. Release preparation — submission remains on hold
- [ ] Finish device testing on the exact candidate intended for release.
- [ ] Resolve failures and repeat the affected checks.
- [ ] Refresh QA records and source hashes after runtime changes.
- [ ] Create a fresh signed archive using an unused build number.
- [ ] Verify signing, privacy declarations and debugging symbols.
- [ ] Upload to TestFlight only when authorised.
- [ ] Verify the processed TestFlight build on the phone.
- [ ] Update App Store Connect copy and media to match that build.
- [ ] Review Apple’s reply and address its specific concerns.
- [ ] Obtain renewed approval before submitting for public App Review.
