# Review preparation update — 29 September 2026

The 4.3(a) source/archive audit is complete: see [APPLE_4_3A_AUDIT.md](../../APPLE_4_3A_AUDIT.md). No imported game template or unrelated app identity was identified; Apple's comparison remains unresolved.

The user separately requested additional worlds. Current source now has a 12-world route and paged guide, with five supplied JPEG backgrounds. Isolated Unity compilation, existing gameplay regression, new world/save/guide checks and bird registration passed. This source is **not the archived/uploaded build 6**. No fresh device archive or App Store submission was made in this audit. PlayerSettings still records build 6; select an unused build number only after checking ASC before exporting the next candidate.

Use [WorldExpansion/RELEASE_NOTES.md](WorldExpansion/RELEASE_NOTES.md) for exact route, tests, candidate-specific draft copy and remaining device work. The later provenance remediation replaces five sound effects with source-generated audio; Unity import/decoding passed. See [Provenance/REMEDIATION_STATUS.md](Provenance/REMEDIATION_STATUS.md). App Store Connect access on 29 September reached a failed/expired sign-in, so the review thread was not re-read; the state below is a dated historical snapshot, not a current status guarantee. A direct recording attempt on 16 September did not start because automatic approval review hit a usage limit; no video is claimed from that attempt.

---

# Current review status — 16 September 2026

SkyPulse Arcade version 1.0 is now Prepare for Submission, with processed build 1.0.0 (6) selected and saved. The earlier 4.3(a) / Design: Spam rejection remains unresolved. Build 6 has not been submitted for review. See [Build6/AUDIT.md](Build6/AUDIT.md) for the actual rejection, source audit and preservation record.

## Completed

- Preserved the starting source with branch, commit, annotated rollback tag and verified Git bundle. Rechecked all 44 archived build 5 file hashes unchanged.
- Improved Home progression visibility, Tech Tree naming, world/power-up discovery and truthful cosmetic Hangar unlock information.
- Enabled native portrait iPhone and iPad support using the existing safe-area layout.
- Final Unity export, Release simulator build, gameplay/UI QA checks and signed device archive passed. Bird registration passed all 90 frames.
- Apple validation-only distribution passed, with a missing UnityRuntime.framework dSYM warning. This warning affects available debug symbols and remains unresolved.
- Final archive installed and launched on the physical iPhone 17 Pro Max. Owner confirmed iPhone and iPad simulator sizing/gameplay/features pass, including crystal pickup, Tech Tree, roster/Hangar, swipe, tap-to-fly and visible wing frames.
- Native iPad 11-inch Home, Hangar, Tech Tree/details, route guide and gameplay launch inspected. Genuine simulator captures saved separately from artificial-save editor QA captures.
- Revised App Store subtitle, promotional text, description, keywords and Review Notes saved. Sign-in required is off.

## Still required before submission

- Owner reports the physical iPhone and iPad feature tests pass. Preserve this attribution; automated checks separately cover save/restart logic.
- Capture and transfer native phone screenshots and a clean walkthrough/gameplay recording. Capture the required native iPad screenshot set.
- Replace old iPhone store screenshots and review attachment; complete the iPad screenshot sequence. Build 6 uploaded successfully at 12:33 BST, processed successfully, and is selected and saved.
- Recheck all final App Store fields, submit for review and send the prepared factual reply in the existing rejection thread.

The selected App Store binary is now build 6. One genuine 2048×2732 iPad Home screenshot is uploaded; existing iPhone screenshots and review attachment are historical. Do not identify those as build 6 evidence. Apple acceptance is not guaranteed by presentation changes.

## Exact candidate

- Device export: `mobile/Builds/iOS-device-universal-6-final`.
- Simulator export: `mobile/Builds/iOS-simulator-universal-6-final`.
- Gameplay source SHA256: `f80a9691940ac2b3954b408778e1200fafd50678984f6670b564d89b24c18507`.
- Signed archive: `/private/tmp/skypulse-build6-20260916/SkyPulse-final-1.0.0-6.xcarchive`.
- Durable ZIP: `artifacts/build-6-review-recovery/SkyPulse-final-1.0.0-6.xcarchive.zip`.
- Verification: `artifacts/build-6-review-recovery/final-verification.json`.

Earlier build 6 exports and archives are superseded. The final source has no C# compiler errors; the QA Editor log contains an unrelated Unity Search indexing exception. Xcode GUI launch failed with macOS incompatibility, although the Xcode CLI archive/validation/install workflow works.

Review contact phone and email were visually verified present on 16 September; accessibility/DOM text reads omit their values. Existing contact details were retained as requested.

See [the simulator crash investigation](Build6/SIMULATOR_CRASH.md): graphics-service replacement during the Xcode update interrupted the simulator; native Home rendering and saved progress were verified after restart.
