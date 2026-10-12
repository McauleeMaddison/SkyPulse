# Premium presentation and TestFlight candidate — 12 October 2026

The owner authorized additional visual polish and a fresh TestFlight upload for testing on their iPhone. This authorizes the private beta build; the public App Store submission hold remains in force.

Candidate: **SkyPulse 1.0.0 (9)**, Unity 6000.6.0f1, existing bundle ID `com.mcauleemaddison.skypulse` and signing team. **Uploaded, processed and Testing in SkyPulse Internal on 12 October 2026 at 22:20 BST.** The existing tester is the owner; no external group or new tester was added. Install/update through TestFlight for fresh physical-device validation.

App Store Connect's lists contained builds 5–7 before preparing build 8. A later audit found old hosted URLs in the runtime config; build 8's upload attempt was interrupted during completion. Build 9 corrects the runtime links to the verified branded support/privacy pages. The later visible build list contains 5, 6, 7 and 9, with no build 8 listed; this is a dated observation, not a claim that cancelling a client always reverses an upload.

At the owner's request the English (U.K.) description, promotional text, keywords, subtitle and review notes were saved for the three-mode candidate. Build 9 is selected in the **unsubmitted** public draft. After saving the replacement build, its label changed from Developer Rejected to **Prepare for Submission**; Add for Review/Submit was not used. Old build 7 screenshots/video remain historical and are explicitly identified in reviewer notes. Fresh device sign-off and matching public evidence are still required before any later submission.

## Visual changes

- Three Home screens gain subtle mode-specific edge lighting: cyan/violet flight streaks, gold/cyan Relay telemetry and emerald data lights.
- Home, mission, puzzle and action surfaces have richer smoked-glass gradients, inset metal highlights and restrained material detailing.
- Relay keeps the new detailed portal hardware, adds moving emitter glints and removes the prominent overlaid grid to let the orbital artwork read more clearly.
- Blackout's Home data core is now dimensional, with floating data/cursor cubes and softly illuminated adjacent puzzle tiles.
- Effects respect pause and Reduced Motion. No new bitmap, font, shader, package or external network service was added in this polish.

Endless's flight code, original gates, 5.265-unit spacing, hitboxes, physics, gaps and reward behavior remain unchanged. The two mission-rule files and daily economy remain unchanged. Existing assets, GUIDs, archives and build 7 evidence are retained; older device passes do not validate this candidate.

## Testing

The isolated suite passes mode isolation, authored Relay missions and Blackout solutions, daily recovery, legacy saves, roster/tech contracts, original Endless preservation and phone/iPad width checks. The new emitter glints freeze during pause and Reduced Motion. Source/log/archive hashes and upload status belong in `RELEASE_RECORD.json`.

After TestFlight offers build 9, install/update SkyPulse on the iPhone. Tap the top-left mode badge to inspect all three Home screens. Play Endless, complete First Circuit with Pulse, complete the introductory Blackout puzzle, then claim a completed daily task. Check pause/background/resume, retry, sound, Reduced Motion, readable controls, frame pacing and retained progress after closing/reopening. Repeat on iPad before later public release.

The build exporter retains a complete source/asset/configuration hash manifest alongside the Xcode project. All 364 manifest entries match the final source snapshot; all twelve runtime scripts match the passed QA project. The signed archive ZIP, available dSYMs, signed IPA, distribution logs and verification remain under `artifacts/testflight-9-20261012/`. QA runs only with a disposable product identity. No real player save is reset.

Upload completed with a missing vendor UnityRuntime.framework dSYM warning (UUID `3C32AE36-550D-3FCA-A8C4-69FC56FF5B23`). Apple processed the build and made it available for internal testing. That framework's symbolication remains limited; no matching vendor dSYM was fabricated. The earlier CLI upload encountered a credential-cache error even after Xcode sign-in; the signed-in Xcode Organizer upload succeeded.

Use [tester notes](TESTFLIGHT_NOTES.txt) and the [device checklist](../ThreeModes20261012/DEVICE_CHECKLIST.md). The [provenance addendum](PROVENANCE_ADDENDUM.md) and dated ledger retain the earlier ARTA/generated-art distinctions and unresolved evidence gaps. Apple approval and global uniqueness are not claimed.
