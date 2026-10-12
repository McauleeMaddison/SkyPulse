# Current metadata audit — 12 October 2026

This records controllable checks and real corrections. It does not certify uniqueness against every App Store app or guarantee future review outcomes.

## Corrections

1. The runtime `SkyPulsePublicLinks.json` still contained the older hosted addresses. It now uses the verified branded HTTPS support/privacy pages at `skypulse.macwebstudio.com`. Unity GUIDs and save identifiers were preserved. Build 8's upload attempt was interrupted during completion after this mismatch was found; server status is recorded separately. Build 9 is the replacement candidate.
2. The withdrawn public listing's keywords duplicated “arcade” from the app name. The replacement keyword set removes this repetition and reflects flight, Relay, hacking, puzzles, offline play, birds, crystals, daily missions and the Tech Tree.
3. At the owner's explicit request, the English (U.K.) App Store Connect subtitle, promotional text, description, keywords and reviewer notes were rewritten for the three-mode candidate and saved. Reloaded field values match the retained local copy. No Add for Review/Submit action was used. The new description is consumer-facing; technical validation, historical evidence and the review hold are explained separately in reviewer notes.
4. Build 9 completed processing and is Testing in the existing one-person SkyPulse Internal group. Its What to Test notes were saved. No external testers or beta review submission were added.
5. Shared TestFlight metadata still described a three-world, single-mode prototype and incorrectly put the support page in its Privacy Policy URL field. The beta description and review notes now describe the three-mode candidate; the privacy URL now ends in `/privacy`. Marketing and feedback/contact details were retained. The beta licence agreement was not changed.

## Scope and findings

| Scope | Result |
| --- | --- |
| App identity | Live name SkyPulse Arcade; existing bundle ID, SKU and Apple ID retained; the app information title and subtitle fit the current limits |
| Creator/support | Public support and privacy pages identify Mcaulee Maddison and provide contact email; branded HTTPS pages load |
| Listing draft | Local name/subtitle/promo/description/keywords pass length and duplicate-keyword checks; no HTML, competitor names, unsupported purchases/login or global originality claims |
| Runtime resources | Canonical URLs align with the draft; 190 unique asset/folder GUIDs; no new art or identifiers renamed to alter a fingerprint |
| Dependencies/privacy | No advertising, analytics or purchasing package added; local progress/offline copy matches current implementation; exported privacy manifest retains UserDefaults reason CA92.1 and no tracking declaration |
| Signed binary | Build/version, bundle ID, iPhone/iPad family, encryption flag, source manifest and packaged URL bytes are verified on the final archive |
| Review evidence | Build 9 is selected and saved in the unsubmitted Prepare for Submission public draft; retained build 7 footage/screenshots are explicitly labelled historical in the new reviewer notes. Fresh candidate screenshots, walkthrough and physical-device sign-off are required before future public submission |
| Account/rights fields | Existing age rating, content-rights and non-trader declarations were read, not rewritten without new factual evidence; earlier provenance gaps remain dated |
| Review hold | Add for Review/Submit not used; no Apple message or website publication performed |

The three-mode listing is saved as an unsubmitted draft for the new candidate. Saving build 9 changed its label from Developer Rejected to Prepare for Submission; no review queue was entered. It is not released to the public. The public title/keyword fields are not evidence that Apple's undisclosed comparison has been cleared.

## Repeatable checks

```sh
python3 mobile/Tools/check_release_metadata.py --require-build 9 --online
python3 mobile/Tools/check_release_metadata.py --require-build 9 --archive /path/to/SkyPulse-1.0.0-9.xcarchive
```

The App Store exporter also refuses unbranded/mismatched URLs or a changed bundle ID, and no longer silently resets an existing version string to 1.0.0. For each future release, use a fresh confirmed build number, update accurate candidate copy and rerun the source/URL/archive checks. The script detects duplicate Unity GUIDs, duplicated keywords, app-name keyword repetition, current field limits, placeholder/unbranded URLs, identity mismatch, unexpected advertising/analytics/purchasing dependencies, privacy-manifest mismatch and old URL bytes in the packaged app. It cannot authenticate asset rights, detect all competing app similarities or predict new Apple rules. Inspect live App Store Connect fields, new screenshots and evidence before public submission.

Current limit references: [Apple app information](https://developer.apple.com/help/app-store-connect/reference/app-information/app-information/) and [platform version information](https://developer.apple.com/help/app-store-connect/reference/app-information/platform-version-information/). The latter specifies a 100-byte keyword field; treating it only as 100 characters could miss a future non-ASCII overflow.
