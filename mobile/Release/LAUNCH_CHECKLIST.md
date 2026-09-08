# SkyPulse launch status — 7 September 2026

**Not ready for App Store submission.** The owner confirmed that the Apple account and public support/privacy details are not ready. Local code preparation cannot complete those requirements.

## Prepared locally

- Version 1.0.0, next candidate build 5. Build 4 remains the previously tested native export; it is not the updated source.
- Privacy screen supports public policy and support buttons. Set `privacyUrl` and `supportUrl` in `Assets/Resources/SkyPulsePublicLinks.json`; buttons appear only for valid HTTPS URLs. Values are deliberately empty until real pages exist.
- Unity menu **SkyPulse → Release → Export App Store Candidate** rejects missing links or signing team. This is a configuration check, not proof that pages are live or the account is enrolled. Ordinary device/Simulator exports remain available for testing.
- Export evidence now records the public-links configuration hash as well as gameplay/icon hashes.
- Store copy, privacy policy and support page drafts are in this directory.
- Installed Xcode 26.6 / iOS SDK 26.5 meet Apple's currently published minimum build requirement.

## Required before submission

1. Enroll in the Apple Developer Program, complete Apple's agreements and create an App Store Connect app for `com.mcauleemaddison.skypulse`. Select the actual team in Unity iOS Player Settings. Owner enrollment, payment and legal attestations remain with the owner.
2. Choose a public support email and website. Finalize and host the policy/support drafts, including contact information and support-email handling. Verify both URLs without login, then enter them in the JSON configuration and App Store Connect.
3. Export a fresh candidate after configuring the links. Check the two buttons on an iPhone. Produce a signed Release archive with Xcode, inspect its privacy report, validate it and upload it. Use an unused build number if 5 has already been uploaded. Preserve dSYMs.
4. Test the final signed build on a physical iPhone: launch, flight, pause/background/resume, saving after relaunch, both scrolling lists, purchases using earned crystals, audio/haptics, Reduced Motion, safe areas, sustained performance and heat. Resolve defects; use TestFlight for beta distribution.
5. Capture real screenshots from that final candidate in the sizes requested by App Store Connect. Existing simulated-balance QA renders are not final store screenshots.
6. Complete description/subtitle/keywords, categories, copyright, age-rating questionnaire, App Privacy, review contact, price, territories and export compliance. Verify rights to all included art/audio. Select the tested build and submit for App Review when approved for release.

## Verification boundaries

`BETA_VERIFICATION.json` describes build 4 only. Changes in build 5 invalidate its gameplay hash for the new candidate. No signed build, upload, physical-device pass or public website publication is claimed. See `LAUNCH_VERIFICATION.json` for this pass's checks.

Sources: [Apple submission requirements](https://developer.apple.com/app-store/submitting/), [App Review guidelines](https://developer.apple.com/app-store/review/guidelines/), [App Privacy](https://developer.apple.com/app-store/app-privacy-details/).
