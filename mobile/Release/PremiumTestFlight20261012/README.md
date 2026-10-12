# Premium presentation and TestFlight candidate — 12 October 2026

The owner authorized additional visual polish and a fresh TestFlight upload for testing on their iPhone. This authorizes the private beta build; the public App Store submission hold remains in force.

Candidate: **SkyPulse 1.0.0 (8)**, Unity 6000.6.0f1, existing bundle ID `com.mcauleemaddison.skypulse` and signing team. App Store Connect's live build and upload lists contained builds 5–7 before preparing build 8.

## Visual changes

- Three Home screens gain subtle mode-specific edge lighting: cyan/violet flight streaks, gold/cyan Relay telemetry and emerald data lights.
- Home, mission, puzzle and action surfaces have richer smoked-glass gradients, inset metal highlights and restrained material detailing.
- Relay keeps the new detailed portal hardware, adds moving emitter glints and removes the prominent overlaid grid to let the orbital artwork read more clearly.
- Blackout's Home data core is now dimensional, with floating data/cursor cubes and softly illuminated adjacent puzzle tiles.
- Effects respect pause and Reduced Motion. No new bitmap, font, shader, package or external network service was added in this polish.

Endless's flight code, original gates, 5.265-unit spacing, hitboxes, physics, gaps and reward behavior remain unchanged. The two mission-rule files and daily economy remain unchanged. Existing assets, GUIDs, archives and build 7 evidence are retained; older device passes do not validate this candidate.

## Testing

The isolated suite passes mode isolation, authored Relay missions and Blackout solutions, daily recovery, legacy saves, roster/tech contracts, original Endless preservation and phone/iPad width checks. The new emitter glints freeze during pause and Reduced Motion. Source/log/archive hashes and upload status belong in `RELEASE_RECORD.json`.

After TestFlight offers build 8, install/update SkyPulse on the iPhone. Tap the top-left mode badge to inspect all three Home screens. Play Endless, complete First Circuit with Pulse, complete the introductory Blackout puzzle, then claim a completed daily task. Check pause/background/resume, retry, sound, Reduced Motion, readable controls, frame pacing and retained progress after closing/reopening. Repeat on iPad before later public release.

The build exporter retains a complete source/asset/configuration hash manifest alongside the Xcode project. The signed archive and dSYMs remain under `artifacts/testflight-8-20261012/`. QA runs only with a disposable product identity. No real player save is reset.

Use [tester notes](TESTFLIGHT_NOTES.txt) and the [device checklist](../ThreeModes20261012/DEVICE_CHECKLIST.md). The [provenance addendum](PROVENANCE_ADDENDUM.md) and dated ledger retain the earlier ARTA/generated-art distinctions and unresolved evidence gaps. Apple approval and global uniqueness are not claimed.
