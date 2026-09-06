# SkyPulse beta readiness — 6 September 2026

Status: final candidate installed for tonight's Simulator visual testing. Both final native builds compile successfully. Not yet uploaded or approved for distribution.

## Completed

- Removed runtime flight trail and rear thrust, fixed square alpha flashes, redesigned pickup/perfect-pass/power-up effects and their cleanup/reduced-motion behaviour.
- Tuned gradual challenge across all three worlds and remix progression while preserving bird handling.
- Improved safe-area layout, full-screen dimmers, feedback placement, privacy wrapping and input guards; removed detached edge fragments during bird import.
- Final opaque 1024×1024 icon has two dimensional wings over a blue-violet cosmic sky. Inspected 256/180/120/60px previews and verified the installed Simulator Home Screen icon.
- Replaced missing font glyphs with crystal artwork in menu, HUD and hangar balances; all three verified in the final iOS Simulator build.
- Final automated regression PASS: 1,300 generated gates, milestone transitions, pause/resume, rewards/persistence, purchase guards and synthetic touch-scroll routing. All 90 registered flight frames verified. Earlier visual coverage includes 216 effects renders and 48 menu/unlock renders.
- Final iPhone Release compile PASS (unsigned): `Builds/iOS-device-final-2/Unity-iPhone.xcodeproj`.
- Final Simulator Release compile PASS and installed: `Builds/iOS-simulator-final-2/Unity-iPhone.xcodeproj`.
- Live Simulator checks include home, privacy, hangar, insufficient-balance modal, flight/HUD, result, retry, pause and Reduced Motion toggle. Existing progress was preserved when replacing the installed app.
- Source-plist layout fixed permanently in the export postprocessor. macOS protections and Unity compiler signature remained unchanged. Earlier usage-limit build rejection has cleared; the final builds now pass.

## Still needed before distribution

- Manual native swipe verification: the automation's drag arrives as a tap. Synthetic Unity drag tests pass; this does not prove native dragging works. Include this in tonight's visual testing.
- Resolve tonight's visual/gameplay feedback and any confirmed defects, then freeze a candidate. Neither automation nor screenshots guarantee perfect balance or player appeal.
- Confirm Apple membership/team, App Store Connect record and signing. Produce and validate a signed archive, upload and configure TestFlight.
- Supply public support contact and host the native privacy/support pages; add the public policy link in-app. Complete required beta metadata, compliance and review contact.
- The user selected Xcode Simulator for this pass. Physical-device performance, heat, battery use and haptics remain unverified.

Follow `TOMORROW_PLAN.md`. Machine-readable evidence and source/icon hashes: `BETA_VERIFICATION.json`. Test instructions: `../Tools/VISUAL_SMOKE.md` and `../Tools/EFFECTS_QA.md`. Local logs/images: `../../artifacts/effects-qa/`.

Export using SkyPulse > Release > Export Xcode Project for devices, or Export Xcode Simulator Project for Simulator. Each export requires an empty folder. The output is an Xcode source project, not an installable App Store package.
