# Current metadata audit — 12 October 2026

This records controllable checks and real corrections. It does not certify uniqueness against every App Store app or guarantee future review outcomes.

## Corrections

1. The runtime `SkyPulsePublicLinks.json` still contained the older hosted addresses. It now uses the verified branded HTTPS support/privacy pages at `skypulse.macwebstudio.com`. Unity GUIDs and save identifiers were preserved. Build 8's upload attempt was interrupted during completion after this mismatch was found; server status is recorded separately. Build 9 is the replacement candidate.
2. The withdrawn public listing's keywords duplicated “arcade” from the app name. That keyword was removed and the draft saved without adding the app for review. The remaining copy still describes its selected historical build 7 accurately.
3. The local three-mode draft also removes the duplicated app-name keyword, checks keywords by UTF-8 bytes, and replaces its stale upload-status wording with build 9/private-beta status. Description, subtitle, promotional copy and tester notes describe the implemented features without claiming new physical-device testing.

## Scope and findings

| Scope | Result |
| --- | --- |
| App identity | Live name SkyPulse Arcade; existing bundle ID, SKU and Apple ID retained; the app information title and subtitle fit the current limits |
| Creator/support | Public support and privacy pages identify Mcaulee Maddison and provide contact email; branded HTTPS pages load |
| Listing draft | Local name/subtitle/promo/description/keywords pass length and duplicate-keyword checks; no HTML, competitor names, unsupported purchases/login or global originality claims |
| Runtime resources | Canonical URLs align with the draft; 190 unique asset/folder GUIDs; no new art or identifiers renamed to alter a fingerprint |
| Dependencies/privacy | No advertising, analytics or purchasing package added; local progress/offline copy matches current implementation; exported privacy manifest retains UserDefaults reason CA92.1 and no tracking declaration |
| Signed binary | Build/version, bundle ID, iPhone/iPad family, encryption flag, source manifest and packaged URL bytes are verified on the final archive |
| Review evidence | Current public draft remains Developer Rejected with selected build 7 and accurately labelled build 7 footage/screenshots; new public screenshots/walkthrough/device sign-off are required before selecting/submitting a later public candidate |
| Account/rights fields | Existing age rating, content-rights and non-trader declarations were read, not rewritten without new factual evidence; earlier provenance gaps remain dated |
| Review hold | Add for Review/Submit not used; no Apple message or website publication performed |

The local three-mode listing is prepared for the later public candidate. It is not falsely applied to the selected build 7 public draft. The public title/keyword fields are not evidence that Apple's undisclosed comparison has been cleared.

## Repeatable checks

```sh
python3 mobile/Tools/check_release_metadata.py --require-build 9 --online
python3 mobile/Tools/check_release_metadata.py --require-build 9 --archive /path/to/SkyPulse-1.0.0-9.xcarchive
```

The App Store exporter also refuses unbranded/mismatched URLs or a changed bundle ID, and no longer silently resets an existing version string to 1.0.0. For each future release, use a fresh confirmed build number, update accurate candidate copy and rerun the source/URL/archive checks. The script detects duplicate Unity GUIDs, duplicated keywords, app-name keyword repetition, current field limits, placeholder/unbranded URLs, identity mismatch, unexpected advertising/analytics/purchasing dependencies, privacy-manifest mismatch and old URL bytes in the packaged app. It cannot authenticate asset rights, detect all competing app similarities or predict new Apple rules. Inspect live App Store Connect fields, new screenshots and evidence before public submission.

Current limit references: [Apple app information](https://developer.apple.com/help/app-store-connect/reference/app-information/app-information/) and [platform version information](https://developer.apple.com/help/app-store-connect/reference/app-information/platform-version-information/). The latter specifies a 100-byte keyword field; treating it only as 100 characters could miss a future non-ASCII overflow.
