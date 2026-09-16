# Current review status — 16 September 2026

SkyPulse Arcade remains rejected under 4.3(a) / Design: Spam. Build 6 has not been submitted. See [Build6/AUDIT.md](Build6/AUDIT.md) for the actual rejection, source audit and preservation record.

## Completed

- Preserved the starting source with branch, commit, annotated rollback tag and verified Git bundle. Rechecked all 44 archived build 5 file hashes unchanged.
- Improved Home progression visibility, Tech Tree naming, world/power-up discovery and truthful cosmetic Hangar unlock information.
- Enabled native portrait iPhone and iPad support using the existing safe-area layout.
- Final Unity export, Release simulator build, gameplay/UI QA checks and signed device archive passed. Bird registration passed all 90 frames.
- Apple validation-only distribution passed, with a missing UnityRuntime.framework dSYM warning. This warning affects available debug symbols and remains unresolved.
- Final archive installed and launched on the physical iPhone 17 Pro Max. Hands-on test results are pending.
- Native iPad 11-inch Home, Hangar, Tech Tree/details, route guide and gameplay launch inspected. Genuine simulator captures saved separately from artificial-save editor QA captures.
- Revised App Store subtitle, promotional text, description, keywords and Review Notes saved. Sign-in required is off.

## Still required before submission

- Finish physical iPhone and iPad run/restart/save checks; record actual results and any fixes.
- Capture and transfer native phone screenshots and a clean walkthrough/gameplay recording. Capture the required native iPad screenshot set.
- Replace old store screenshots and review attachment; upload final build 6, wait for processing and attach it instead of build 5.
- Recheck all final App Store fields, submit for review and send the prepared factual reply in the existing rejection thread.

The selected App Store binary is still build 5 and existing screenshots/attachment are historical. Do not identify them as build 6 evidence. Apple acceptance is not guaranteed by presentation changes.

## Exact candidate

- Device export: `mobile/Builds/iOS-device-universal-6-final`.
- Simulator export: `mobile/Builds/iOS-simulator-universal-6-final`.
- Gameplay source SHA256: `f80a9691940ac2b3954b408778e1200fafd50678984f6670b564d89b24c18507`.
- Signed archive: `/private/tmp/skypulse-build6-20260916/SkyPulse-final-1.0.0-6.xcarchive`.
- Durable ZIP: `artifacts/build-6-review-recovery/SkyPulse-final-1.0.0-6.xcarchive.zip`.
- Verification: `artifacts/build-6-review-recovery/final-verification.json`.

Earlier build 6 exports and archives are superseded. The final source has no C# compiler errors; the QA Editor log contains an unrelated Unity Search indexing exception. Xcode GUI launch failed with macOS incompatibility, although the Xcode CLI archive/validation/install workflow works.
