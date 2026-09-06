# SkyPulse beta readiness — 6 September 2026

Status: beta polish saved; NOT yet ready to upload. The earlier iPhone and Simulator Release builds compiled successfully after the source-plist layout fix. The latest two-wing icon and crystal-counter correction exported successfully, but their final native rebuild was blocked by automatic approval review because its usage limit was reached. No signed archive or TestFlight upload has been produced.

## Completed

- Removed runtime bird trail and rear thrust; fixed square alpha flashes; redesigned crystal, perfect-pass and three active power-up effects, with reduced-motion handling and cleanup.
- Tuned the complete three-world route and remix progression. First gates teach, later gates gradually tighten and accelerate; handling remains unchanged.
- Improved safe-area fitting, full-screen dimmers, feedback placement, privacy wrapping and menu input guards. Removed disconnected edge fragments from imported bird sprites without altering source PNGs.
- Replaced the icon with an opaque 1024×1024 two-wing mechanical bird over a vivid blue-violet cosmic background. Checked 256/180/120/60-pixel previews. See ICON.md for asset and generation prompts.
- Unity behaviour checks PASS, including 1,300 generated gates, transitions, pause/resume, rewards/persistence, purchase guards and touch-scroll routing. All 90 registered flight frames verified.
- 216 effects renders and 48 full-screen/menu/unlock renders produced in Unity. These establish editor behaviour and appearance, not physical-device performance.
- Fresh Unity 6000.6.0f1 export succeeded: `Builds/iOS-beta-1.0.0-2/Unity-iPhone.xcodeproj`, version 1.0.0 (2), bundle `com.mcauleemaddison.skypulse`. Xcode 26.6 and iOS SDK 26.5 are installed.
- The latest export is `Builds/iOS-simulator-final-2/Unity-iPhone.xcodeproj`; its build-info hashes match the final icon and crystal-counter source. Earlier compiled exports contain the previous one-wing icon and previous counter implementation.
- Installed the earlier Simulator Release build, verified its updated Home Screen icon, and inspected home, privacy, hangar and insufficient-balance modal. A simulated drag opened a card rather than scrolling; investigate whether the gesture delivery or native routing caused it. Do not count native touch scrolling as passed.

## Blocking release checks

1. Finish compiling and installing the latest Simulator export when approval review is available again. Validate the two-wing icon on the Home Screen and crystal artwork in all three counters, then complete live gameplay, pause/retry, scrolling and effects checks. Re-export a fresh DeviceSDK candidate with these final assets before signing.
2. The user selected Xcode iOS Simulator testing for this pass. Physical-device heat, battery use, haptics and performance remain unverified; do not represent Simulator results as hardware evidence.
3. Confirm Apple Developer membership/signing team, create/confirm App Store Connect record, archive and validate a signed Release build. Only a development signing identity was observed; distribution setup has not been validated.
4. Supply public support contact and host the native privacy/support pages. PRIVACY_POLICY_DRAFT.md needs owner details and publication. Complete privacy answers, age rating, availability and review contact.
5. Capture final device screenshots and distribute through TestFlight for beta testing. Beta feedback is still needed to judge difficulty and retention; technical checks cannot guarantee that players find the balance ideal.

## Reproduce / inspect

Test instructions: ../Tools/VISUAL_SMOKE.md and ../Tools/EFFECTS_QA.md.
Evidence: ../../artifacts/effects-qa/ (smoke, screen, export and native failure logs; image captures).
Tester notes: BETA_TEST_NOTES.md.

Export with SkyPulse > Release > Export Xcode Project. Default is a new timestamped Builds/iOS-beta-* folder; SKYPULSE_IOS_OUTPUT can select another empty destination. Open Unity-iPhone.xcodeproj in Xcode; the export folder is source/build inputs, not an installable iPhone app.

Apple beta distribution guidance: https://developer.apple.com/app-store/review/guidelines/#beta-testing
