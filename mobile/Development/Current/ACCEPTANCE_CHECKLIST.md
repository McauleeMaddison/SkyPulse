# SkyPulse — final local acceptance

Local source: 16 November 2026. New candidate: **Build 22**, requested for iPhone visual testing; consult its release record for installation status.
Public App Review remains on hold. All manual items below are deliberately unchecked.

## Automated verification completed

Unity compilation, focused daily-save/retry/midnight checks and broader controls,
wing, world, UI, pickup and upgrade regressions passed in the isolated QA product.
See QA_REPORT.json for exact counts and source hashes. The Unity Editor-only
SearchDatabase indexing exception is still recorded; it was not hidden or patched.

## Editor review completed — owner sign-off stays separate

- [x] Native 3D presenter and selected identity captured for all 15 birds; approved rig files unchanged.
- [x] All 12 worlds captured with fuller connected city groups and theme-specific pipes.
- [x] Six district placements and three connected skyways per world use the existing bounded renderer pool; sampled parallax preserves landing attachments.
- [x] Relay collection hides the collected crystals, opens the matching gate and accepts actual passage through the opening.
- [x] Blackout live/Plan/instant-next views captured with the city above the board.
- [x] Home, guide and touch suites passed; the Relay narrow-face/label mismatch was corrected.
- [x] All 12 world transitions checked at simulated 30/60/120 Hz, including pause/retry.
- [x] Current controls, bird, pickup and upgrade suites passed; prior save/result checks retain their recorded source boundary.
- [x] Current C# compilation has no warnings/errors; unused legacy HUD field removed.

These checkmarks are Editor evidence only, not an assertion that the owner has
approved the art or played every flow. [Preview sheets and report](UnityAcceptance/README.md).
The known Unity Editor search-index exception is still recorded. Audio/haptics,
real-device performance and every owner item below remain unapproved.

## First: owner visual acceptance in Unity

Use **SkyPulse → Play Main Game** in Unity (it opens `Assets/Scenes/SkyPulse.unity` and preserves your save). Use the portrait Game/Simulator view. **SkyPulse → Playtest Checklist** opens the matching review summary.

- [ ] Endless: selected 3D bird is correct; every tap visibly drives its full wing stroke.
- [ ] Endless: approve all twelve inhabited island foundations, visible window lighting, building details and sky; no unwanted isolated island remains.
- [ ] Endless: pipes/collars have no duplicates, black patches or gaps; all 12 worlds remain readable.
- [ ] Relay: crystals open only their matching gate immediately; the bird passes through the actual hoop opening.
- [ ] Blackout: the city window remains separate from the board; the named restoration and live beam are clear.
- [ ] Intermediate puzzles are playable immediately; final puzzle and daily case show the proper result.
- [ ] Newly earned bird cradles are clear in Endless/Relay results; replay shows the next target without another unlock.
- [ ] Hangar swiping/equipping, Tech Tree panning, training and breeding remain comfortable and readable.
- [ ] Results, Home and guide labels have no clipping or overlaps on small phone, notched phone and iPad views.
- [ ] Pause, retry, Main Menu, Reduced Motion, sound and haptic settings behave as expected.

Do not deliberately damage the owner's storage to test save failure. That scenario
was exercised with injected write/flush failures in disposable QA; the screenshot
shows the retained solved route and Retry Save action.

## Build 22 device acceptance — requested by the owner

- [ ] Create a fresh signed numbered build with source hashes and debugging symbols.
- [ ] Install while preserving the existing save; verify scores, wallet, birds, upgrades and medals.
- [ ] Repeat all three modes and the checklist above on iPhone and iPad.
- [ ] Check today's circuit/case behavior over a natural UTC rollover and app reopen.
- [ ] Measure sustained frame pacing, loading, memory and heat on actual devices.
- [ ] Capture fresh screenshots/recording from that exact accepted device build.
- [ ] Update App Store Connect copy and public guide to match the eventual release candidate.

App Store submission is a separate later decision. Do not submit while the owner's
hold remains active. Editor checks and preview approval do not certify device
performance, universal bug freedom, exclusive AI-art rights or Apple acceptance.
