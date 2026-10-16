# Mobile gameplay polish — build 12

This release extends the tested build 11 implementation. Build 9 cannot be reused: it is already uploaded to Apple. The next unused build number was verified as 12 in App Store Connect on 16 October 2026. Public App Review remains on hold.

## Controls and gameplay

Pulse Relay uses one deliberate tap anywhere inside the playable camera area to flap. Interactive UI and pause/menu screens are excluded. Touch/mouse suppression and press-edge fences prevent duplicate flaps. There are no FLAP or PULSE gameplay buttons. Each pair of crystals belongs to one numbered gate; its first collected crystal opens it and spends one charge immediately. A spare improves the medal and cannot open another gate. Swept collection resolves before gate impact, including same-frame contact, independently of opening animation. Retry creates clean accounting.

Blackout uses immediate pointer-down taps on connected neighbours. A first closed-firewall tap spends one breach charge and three trace; another tap enters. Cores, cipher keys, phase switches, locks, threats and trace remain authoritative in the session model. Wrong adjacent actions cost resources and can fail the sector. Eight authored sectors increase in required route complexity. A successful intermediate clear saves once and activates the next board synchronously. A short confirmation remains above the already active board; held pointers, queued clicks and obsolete generation tokens cannot act on the next puzzle. The final sector uses the campaign result/replay flow.

## This pass's changes

- `SkyPulseNativeGame.Presentation.cs`: reused spectral crystal artwork gains moving facet/crown reflections and subtle colour depth; fixed approach-light objects guide each gate; portal unlock lighting and station parallax are refined. Relay Home now uses the same faceted sprite. Ambient guidance freezes on pause and is stationary under Reduced Motion. No per-frame object/material creation is added.
- `SkyPulseNativeGame.Blackout.cs`: the authoritative current node displays YOU immediately, including during rapid taps while the transport trail catches up. Labels are larger and scale within their existing generously sized hit areas. The 49 board controls, edge arrays and trail buffers remain pooled across sectors. Blackout Home retains only its sector map, guide and relevant progress; no Bird Hangar or Tech Tree.
- `SkyPulseBlackoutGraphic.cs`: Reduced Motion impact confirmation fades in place, with no expanding ring, particle travel or phase shockwave.
- `SkyPulseButtonFeedback.cs`, `SkyPulseNativeGame.Missions.cs`: cached mode/puzzle press feedback respects Reduced Motion. Mode-specific menu controls and shared layout widths are retained.
- `SkyPulseCircuitGraphic.cs`: removes the older gold diamond from Relay's Home hero in favour of the actual crystal sprite.
- `SkyPulseOneThumbChecks.cs`: adds immediate-position, stationary reduced-motion geometry, fixed target, eight-crystal presentation, pause and allocation regression cases to the existing input/gate/progression suite.
- PlayerSettings uses build 12. Bundle ID, team, product/company names, existing save keys, economy, authored puzzles and Endless handling/tuning remain unchanged from build 11.

## Validation and evidence

The isolated final Unity run compiled and exited 0 with all 14 required pass markers. It exercises real UI pointer-down handlers plus session/physics paths: touch/UI separation, ghost mouse rejection, held/stale input fences, final-crystal/gate contact, retry accounting, duplicate finalization, all eight sectors, final result, failure, pause/resume and saved/legacy progress. Flight/course simulation and animation timing are checked with 30/60/120 FPS steps. A normal-tap autopilot completes all four daily Relay circuits at each rate; this is automated reachability evidence, not a substitute for human playtesting.

Warmed Relay presentation and procedural Blackout mesh checks report zero managed allocations in their measured loops. They do not measure the entire frame, GPU time or device thermals. Nine metadata tests, all 90 bird frame registrations, 197 unique GUIDs, unchanged 145-image rights-ledger coverage, candidate identity/packaged privacy links and 378 source/configuration hashes pass. All 19 runtime scripts match the tested isolated project and exported source. The retained Editor log includes the previously observed Unity Search startup indexing exception; the required game checks all passed.

See [QA_RECORD.json](QA_RECORD.json), [RELEASE_RECORD.json](RELEASE_RECORD.json), [METADATA_ARCHIVE_CHECKS.json](METADATA_ARCHIVE_CHECKS.json), [BUILD_INFO.json](BUILD_INFO.json) and [SOURCE_MANIFEST.json](SOURCE_MANIFEST.json). The PNG previews are actual Unity QA renders with disposable/artificial progress, not physical-device screenshots or store-ready device evidence.

The native archive compiled and passed strict signature verification with normal macOS certificate-store access. Upload succeeded. Apple reports a missing vendor UnityRuntime dSYM for UUID `392D7A7F-6A4F-3A7A-8788-4089FA96FF38`; no matching file was found in the installed editor. SkyPulse and UnityFramework dSYMs and a durable ZIP of the archive are retained. Engine crash symbolication remains limited; this warning is not represented as fixed.

## Device checks still needed

Install/update the newly available build through TestFlight and test one-thumb comfort, short/fast taps, crystal clarity and sounds, pause/menu separation, gate passage, Blackout trace/charge failure, automatic sector progression, the final result, restart and retained progress after relaunch. Compare Reduced Motion and haptics settings. Check iPhone/iPad safe areas, sustained 60 FPS, heat and stutter on real hardware. Native compilation and editor checks do not certify device performance.

No new raster artwork, audio, external code sample, font or shader was introduced. Existing [artwork evidence and provider-rights limits](../IdentityRights20261013/RIGHTS_RECORD.md) remain applicable. No exclusive-rights or Apple similarity clearance claim is made. Fresh store media and Apple's comparison details remain outstanding before any separately authorized public submission.
