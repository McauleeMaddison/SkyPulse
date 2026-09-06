# SkyPulse release plan — Monday 7 September 2026

Target: a signed beta uploaded to App Store Connect and available to the chosen TestFlight testers, subject to Apple's processing/review. This is different from publishing a public App Store listing. Apple directs beta distribution through TestFlight: [guidelines](https://developer.apple.com/app-store/review/guidelines/#beta-testing).

## Tonight: record visual feedback on the installed build

Use SkyPulse 1.0.0 (2) in the open Xcode iOS Simulator. The final icon has two wings and a blue-violet cosmic background. Existing scores and crystals were preserved during installation.

- Swipe the Bird Hangar and Tech lists up/down, reach the final entries, and confirm dragging never opens a card or starts a run. Automated Unity event checks pass, but Simulator drag injection behaves like a tap; manual verification is still needed.
- Check home, privacy, purchase/cancel, pause, result and retry. Look for clipping, tiny text, misplaced controls and background flashes.
- Play through gates 1–15 and into later worlds. Note the score and action for any unfair gate, sudden jump in difficulty, missed flap, visible trail or square effect.
- Watch crystal/perfect-pass feedback and all three power-ups, including expiry. Test Reduced Motion as well.
- Save a screenshot plus score, selected bird and action for each issue. Do not erase progress merely to repeat a test.

## Tomorrow, in order

1. **Fix tonight's confirmed issues and freeze the candidate.** Reproduce each report; resolve the native swipe question first. Repeat only the affected checks plus a short launch/flight/pause/retry regression. Include a small iPhone Simulator viewport. Keep controls consistent while adjusting course difficulty only where testing justifies it.

2. **Complete owner/account details.** Confirm paid Apple Developer membership, the correct team and App Store Connect access. Create or verify the SkyPulse record for `com.mcauleemaddison.skypulse`. You must personally complete login/2FA, paid enrollment or any binding agreements if required. Confirm the beta audience (internal team or external testers). [Apple distribution requirements](https://developer.apple.com/documentation/xcode/distributing-your-app-for-beta-testing-and-releases).

3. **Publish support and privacy information.** Supply the public support email and approved website/domain. Finish `PRIVACY_POLICY_DRAFT.md`, host the support/privacy pages, and add the public policy link to the in-game privacy screen. Verify both URLs. Answer App Privacy from the native implementation and included SDKs; review the generated privacy report. Apple requires accessible policy links in-app and in metadata, and contact information through support. [App Review guidelines](https://developer.apple.com/app-store/review/guidelines/).

4. **Prepare the signed iPhone build.** Use `Builds/iOS-device-final-2/Unity-iPhone.xcodeproj` only if source/icon hashes still match `BETA_VERIFICATION.json`; otherwise export to a new empty folder. Choose the correct signing team and a generic iOS device destination, archive in Release, and validate in Xcode Organizer. A Simulator app cannot be uploaded. Increase the build number if App Store Connect has already received build 2. Check the final icon, bundle ID, portrait orientation, encryption declaration and privacy manifests. Preserve symbols for crash reports.

5. **Upload and configure TestFlight.** Upload the validated archive. After processing, complete export compliance and beta information: description, feedback email, review contact and what to test (adapt `BETA_TEST_NOTES.md`). Add the chosen tester group. Internal testers require App Store Connect access; an external beta can require Beta App Review. Apple controls processing/review times, so availability tomorrow cannot be guaranteed. [TestFlight overview](https://developer.apple.com/help/app-store-connect/test-a-beta-version/testflight-overview), [external testing](https://developer.apple.com/help/app-store-connect/test-a-beta-version/invite-external-testers).

6. **Verify the delivered build and record the handoff.** Install from TestFlight, check startup/saving and run a short gameplay pass. Physical-device performance, heat, battery use and haptics remain unverified by tonight's Simulator work. Commit and push final changes, record commit/build number and the test results, and confirm tester access.

## Public App Store release, if that is also wanted

After beta feedback is resolved, finish the public product page (description, subtitle, categories, age rating, pricing/availability, copyright, support/privacy URLs and accurate screenshots from the final game), supply review details, select the approved candidate and submit for App Review. Uploading to TestFlight alone does not publish the public listing. Release timing depends on Apple's review and your release choice.

## Current handoff

Both final unsigned iPhone Release and Simulator Release builds compiled. The Simulator installation, final icon, three crystal counters, launch, retry, pause and Reduced Motion control were visually checked. The final automated regression passed, including 1,300 generated gates and 90 registered flight frames. See `BETA_VERIFICATION.json` for precise scope. No signed archive, App Store Connect upload, public support/privacy publication or physical-device performance certification has been completed.
