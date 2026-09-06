# SkyPulse release readiness — 6 September 2026

Status: NOT approved for public submission. Target test device: iPhone 17 Pro Max.

## Completed evidence

- Unity 6000.6.0f1 iOS Build Support installed.
- Xcode 26.6 installed; iOS 26.5 platform download completed.
- Initial Unity iOS export succeeded at Builds/iOS-release-1.
- Game scripts compile with warnings treated as errors.
- Release editor scripts compile with warnings treated as errors.
- All 90 flight registration entries match source hashes and importer settings.
- Thirty hit/unlock alpha bounds measured without changing source PNGs.
- Updated interface scaling fits the complete 1080×1920 layout on tall phones.

The initial Xcode export predates the latest canvas, privacy and pose-bounds changes.
Do not archive or submit that export. Export into a fresh folder after testing the
final source. The unsigned Xcode Release compilation passed after installing the iOS platform.
This validates the initial export only; it does not validate signing, device
installation, the latest source changes or runtime behaviour.

## Blocking verification

Automated Unity Play-mode checks were explicitly rejected by automatic approval
review because the earlier usage-limit restriction remained active. Do not bypass
that restriction by launching the same tests through another route. A new approval
or a user-run test session is required. Compilation is not evidence of gameplay,
visual quality, mobile performance or readiness for App Review.

Before release, verify:

1. Home, hangar, tech map, locked/available/maxed nodes, purchase modal, pause,
   privacy, result and all 15 unlock reveals on the phone and a small portrait view.
   Check text wrapping, touch sizes, safe area, feather clipping and tap routing.
2. All 15 birds: single/repeated flaps, collision pose, recovery and restart. Watch
   rigid body/face position across frames. Inspect source feather tips touching
   the canvas; alpha-bounds fitting cannot reconstruct clipped source artwork.
3. Repeated rounds across 15/30/45/60 gates: trails, transition pacing, empty gaps,
   collision fairness, rewards, pause/background/resume and relaunch persistence.
4. Actual touch scrolling in both directions, every node reachable, no accidental
   round start or purchase after dragging, position retained after buying a level.
5. Economy: level/prerequisite enforcement, insufficient balance, maxed upgrades,
   one-time result rewards and meaningful reward pacing after stronger upgrades.
6. At least 15 minutes on iPhone 17 Pro Max: stable frame pacing, memory, heat,
   sound, haptics, Reduced Motion and offline play. Then verify a signed Release build.

## Monday / Apple account prerequisites

- Confirm paid Apple Developer Program membership and select the correct signing team.
- Supply a public support email and host a native-app privacy policy and support page.
- Create App Store Connect record for com.mcauleemaddison.skypulse (version 1.0.0).
- Complete age rating, pricing, availability, privacy answers, copyright and review contact.
- Capture screenshots from the verified final build. No generated art as gameplay evidence.
- Archive and validate with Xcode, distribute to TestFlight, then submit after on-device checks pass.

## Build commands

Unity: SkyPulse > Release > Configure iPhone Release, then
SkyPulse > Release > Export Xcode Project. Default output: Builds/iOS.
The export requires an empty output folder. Set SKYPULSE_IOS_OUTPUT to choose another.
Open its Unity-iPhone.xcodeproj in Xcode. Do not create a new Xcode app or clone the
Unity repository into Xcode as a substitute for export.

The project uses earned in-game crystals, with no real-money IAP, advertising,
analytics or account package identified in the current native implementation.
Review generated privacy manifests and Xcode's privacy report before submission.
The browser privacy page describes a separate browser build and is not the native
App Store policy.
