# SkyPulse readiness

Current status (7 September 2026): **not ready for submission**. Build 5 source adds configurable policy/support links and an App Store export configuration check. The owner has not yet set up the Apple Developer account or public support/privacy details. See `LAUNCH_CHECKLIST.md` and `LAUNCH_VERIFICATION.json`. The build 4 native evidence below is historical and does not validate the changed source.

## Beta evidence — 6 September 2026

Status: final candidate installed for tonight's Simulator visual testing. Both final native builds compile successfully. Not yet uploaded or approved for distribution.

## Completed

- Removed runtime flight trail and rear thrust, fixed square alpha flashes, redesigned pickup/perfect-pass/power-up effects and their cleanup/reduced-motion behaviour.
- Tuned gradual challenge across all three worlds and remix progression while preserving bird handling.
- Improved safe-area layout, full-screen dimmers, feedback placement, privacy wrapping and input guards; removed detached edge fragments during bird import.
- Final opaque 1024×1024 icon has two dimensional wings over a blue-violet cosmic sky. Inspected 256/180/120/60px previews and verified the installed Simulator Home Screen icon.
- Replaced missing font glyphs with crystal artwork in menu, HUD and hangar balances; all three verified in the final iOS Simulator build.
- Final automated regression PASS: 1,300 generated gates, milestone transitions, pause/resume, rewards/persistence, purchase guards and synthetic touch-scroll routing. All 90 registered flight frames verified. Earlier visual coverage includes 216 effects renders and 48 menu/unlock renders.
- Final iPhone Release compile PASS (unsigned): `Builds/iOS-device-pacing-3/Unity-iPhone.xcodeproj`.
- Final Simulator Release compile PASS and installed: `Builds/iOS-simulator-pacing-3/Unity-iPhone.xcodeproj`.
- Live Simulator checks include home, privacy, hangar, insufficient-balance modal, flight/HUD, result, retry, pause and Reduced Motion toggle. Existing progress was preserved when replacing the installed app.
- Source-plist layout fixed permanently in the export postprocessor. macOS protections and Unity compiler signature remained unchanged. Earlier usage-limit build rejection has cleared; the final builds now pass.

## Still needed before distribution

- User playtest confirmed Bird Hangar and Tech Tree mouse scrolling in iOS Simulator works and reported the latest gameplay, behaviour and features as good. Automated gesture delivery remains a tool limitation, not an outstanding reported game defect.
- Resolve tonight's visual/gameplay feedback and any confirmed defects, then freeze a candidate. Neither automation nor screenshots guarantee perfect balance or player appeal.
- Confirm Apple membership/team, App Store Connect record and signing. Produce and validate a signed archive, upload and configure TestFlight.
- Supply public support contact and host the native privacy/support pages; add the public policy link in-app. Complete required beta metadata, compliance and review contact.
- The user selected Xcode Simulator for this pass. Physical-device performance, heat, battery use and haptics remain unverified.

Follow `TOMORROW_PLAN.md`. Machine-readable evidence and source/icon hashes: `BETA_VERIFICATION.json`. Test instructions: `../Tools/VISUAL_SMOKE.md` and `../Tools/EFFECTS_QA.md`. Local logs/images: `../../artifacts/effects-qa/`.

Export using SkyPulse > Release > Export Xcode Project for devices, or Export Xcode Simulator Project for Simulator. Each export requires an empty folder. The output is an Xcode source project, not an installable App Store package.

Build 3 tuning update: vertical openings are one percentage point of camera height narrower after the first three learning gates (about 3–4% relative narrowing). Gate speed rises from .44 to .45 at score 40, settles at .46 by 44, rises to .47 at 60 and settles at the existing .48 ceiling by 64. Horizontal spacing and bird handling are unchanged. The updated 1,300-gate regression passed; the new Simulator app was installed and its main menu inspected on iPhone 17.

Build 4 supersedes the opening tuning above: gates 0–14 now continuously accelerate from .345 to .395 and narrow from .32 to .282 of camera height, with varied heights from the first gate. There is no three-gate tutorial exemption. Score 40/60 increases, horizontal spacing and handling remain unchanged. Backgrounds use bounded camera drift and three moving depths of soft lights; Reduced Motion disables this scenery motion. The 1,300-gate regression and 273-frame background render/coverage pass succeeded.
