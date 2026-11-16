# SkyPulse — final local acceptance

Local source: 16 November 2026. Installed iPhone: **Build 21**, not this candidate.
Public App Review remains on hold. All manual items below are deliberately unchecked.

## Automated verification completed

Unity compilation, focused daily-save/retry/midnight checks and broader controls,
wing, world, UI, pickup and upgrade regressions passed in the isolated QA product.
See QA_REPORT.json for exact counts and source hashes. The Unity Editor-only
SearchDatabase indexing exception is still recorded; it was not hidden or patched.

## First: owner visual acceptance in Unity

Open `mobile/Assets/Scenes/SkyPulse.unity`, use a portrait Game view and press Play.

- [ ] Endless: selected 3D bird is correct; every tap visibly drives its full wing stroke.
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

## After visual approval: exact numbered device candidate

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
