# SkyPulse — App Store readiness, 18 November 2026

**Decision: HOLD public review. Build 22 is ready for owner acceptance testing.**
Candidate: 1.0.0 (22), bundle `com.mcauleemaddison.skypulse`, runtime source
`1fd3dfa`. Later commits in main update release tools and records; they do not silently
change this binary’s gameplay. App Store Connect remains Prepare for Submission.

## Verified evidence

| Area | Current evidence | Remaining work |
| --- | --- | --- |
| Version and build | 1.0.0 (22) installed/launched; processed in SkyPulse Internal; selected in saved draft, reloaded 17 November | Complete the owner's tests on this exact candidate |
| Listing | Current English (U.K.) description, promo, keywords, branded URLs and MacWebStudio credit saved/reloaded | Recheck after any subsequent gameplay change; other localizations not audited this pass |
| Metadata validation | All field limits pass; 18 focused guards pass; 71 runtime scripts and all 2,559 manifest entries match | No claim of global uniqueness or Apple approval |
| App information | Name/subtitle/categories and privacy URLs verified 16 November | Saved rating/questionnaire and rights declaration observed 18 November; owner content/rights review and applicable account compliance remain open |
| Signing/privacy archive | Correct identity, iPhone/iPad families and packaged links; required UserDefaults privacy reason; strict signatures pass | Vendor UnityRuntime dSYM warning remains; app and UnityFramework dSYMs match |
| Saved progress | All 44 preferences identical across install; after-launch changes limited to daily rollover and owner-confirmed bird selection | Owner confirms in-game state during checklist |
| Public pages | All four branded pages published for Build 22 and returned HTTPS 200 with expected content | Current guide covers seven pickups, 36 Tech levels, mastery, daily heists and restoration. Published Home checked at 319 px with no horizontal overflow; remaining browser/device layouts need acceptance |
| Source artwork evidence | All 147 current images match the existing evidence ledger, with no uncovered or changed image | Original generation exports/ARTA receipt not independently inspected; no exclusive-rights certificate |
| Review status | Draft saved; no Add for Review/Submit action | Owner hold stays active; Apple's earlier private similarity/account comparison remains unidentified |

## Automated polish recheck — 18 November

All three current isolated Unity runs passed: final polish/field Tech/native birds,
full visual/layout/world transitions, and finish/save/progression acceptance fixtures.
There were zero C# compiler errors/warnings and no reported text overflow in the
captured screens. The existing Editor SearchDatabase exception remains recorded.
These results do not replace owner testing or measure physical-device performance.
See [POLISH_REPORT.json](POLISH_REPORT.json) for exact scope and retained logs.

Three missing, referenced sky textures were restored from their exact Build 22
Git blobs. All 2,559 candidate file hashes now match; no runtime redesign or new
phone build was introduced. Strict signatures and retained native binary hashes
were rechecked after removing Finder-added bundle attributes. Build 22 remains
installed on the phone; the game was not launched for the owner.

The artwork checker also found an obsolete link to a note moved into Development/Archive;
that link is corrected and all 147 recorded image hashes/sizes pass. Current 3D
models still use raster material/backdrop artwork and individual crash/unlock frames.
Their internal origin records remain accurate. Marketing fields and public pages
contain no AI-tool attribution. Optional attribution has been removed from the
reviewer-note draft, replacing it with current Unity 3D implementation
wording. The official browser runtime was subsequently restored, and the revised
notes plus paid-download wording were saved and compared after reload on 18 November.
Historical save evidence is retained in Git history.

## Complete before considering submission

1. Work through [DEVICE_CHECKLIST.md](DEVICE_CHECKLIST.md) and record
   [TEST_RESULTS.md](TEST_RESULTS.md). Resolve reproducible failures; a runtime
   change requires a new numbered build and affected retests. Include actual
   iPhone/iPad model, OS, session length and measured performance evidence.
2. The owner reports saving **macwebstudio26@gmail.com** in App Review Information
   on 17 November. No further correction is requested. The browser is restored,
   but masks the Email value; independent value verification remains unavailable.
   The owner’s save is recorded separately from agent-observed fields.
3. The saved **age-rating questionnaire** was inspected on 18 November (calculated
   4+, no override), alongside the rights declaration and standard EULA. Complete
   owner content/rights review and applicable account/compliance checks. Paid Apps Agreement, banking and listed tax forms are now verified Active.
   A low age rating is not inferred from the artwork. Accessibility features are
   not declared supported until their published evaluation criteria are met.
4. Capture approved current media using [MEDIA_CAPTURE_PLAN.md](MEDIA_CAPTURE_PLAN.md).
   Existing store screenshots/preview and build 7 review walkthrough are historical.
   Keep their backups before replacing them. Editor QA images are not phone evidence.
5. **China mainland is excluded and independently verified on 18 November.**
   It was still selected when checked, so the existing owner instruction was applied.
   All 174 other country selections were preserved. Do not change other territories
   without a new instruction.
   No AI/security declaration has been made. Check other selected-country
   requirements separately. Vietnam game
   licensing and EU trader status also require review if those regions are selected.
   Do not enter invented registration numbers or infer legal status from a free price.
6. Review Apple's reply about the earlier similarity/account concern alongside
   development history and the artwork ledger. Existing review notes explain the
   three modes, walkthrough and provenance limits; no new message is being sent.
7. Obtain renewed explicit owner permission for **public App Review** only after
   the above evidence is complete. A TestFlight upload is not that permission.

## Facts prepared for live forms — not submitted answers

- One-time paid download; all three modes included. No player account, chat, user
  content feed, advertisements or in-app purchases.
- Earned crystals fund birds, training, breeding, Core and twelve Tech families.
  Random flight pickups are not a paid loot-box feature.
- Eight Signal Heist vaults, optional daily cases and local city-light restoration.
- The shipped game uses local saves, a score-copy action and external branded
  support/privacy links. No AI-generation service was found in runtime source.
  AI-assisted asset creation is retained in the provenance record. This observation
  does not determine the answer to a jurisdiction-specific legal declaration.
- Apple supplies the standard EULA unless a custom agreement is configured;
  no new contract is accepted during this readiness pass.

These facts help review the actual forms; they do not replace the age questionnaire,
rights review, country documentation or the owner's device results.

## Paid release and restored browser — 18 November

The owner approved **£0.79** after the live UK picker showed no exact £0.80 option.
The price was saved/reloaded with Apple comparable international prices. Current
listing and reviewer text now say one purchase includes all three offline modes.
Public-site version 9 is published with matching planned launch pricing and a
privacy explanation of Apple’s payment processing. The game binary and crystal
economy are unchanged; the 18 metadata guard tests pass.

**Paid Apps Agreement, bank account and listed tax forms: Active.** After the
owner's submissions and a further page refresh on 18 November, Apple now shows
these items as Active. The incomplete-tax and banking-processing notices have
cleared. No further action is displayed for these items. Earlier pending-status
observations are retained as historical evidence in the draft record.
No agreement was accepted, financial information entered or legal status selected
by the agent; no private financial values were retained. Current DSA non-trader
status and Vietnam licensing applicability also need owner review for the paid
release. Existing Mac/Vision Pro availability remains unchanged and unverified.
See [APP_STORE_DRAFT_RECORD.json](APP_STORE_DRAFT_RECORD.json) and its screenshots.

Browser editing is restored through the already-enabled official Browser plugin.
No Safari security setting or broad permission change was needed. Real draft
Save/reload comparisons verified the connection. No Add for Review action occurred.

## Known diagnostics

Unity's Editor-only SearchDatabase exception remains recorded. Apple's successful
upload included a nonfatal missing UnityRuntime.framework dSYM warning. Neither
is described as fixed. See [NATIVE_VERIFICATION.json](NATIVE_VERIFICATION.json).

## Primary references checked 17 November

- [Apple review completeness and accurate metadata](https://developer.apple.com/app-store/review/guidelines/)
- [Information for a complete review](https://developer.apple.com/help/app-review/before-submitting-for-review/complete-review)
- [App information, required fields and regional requirements](https://developer.apple.com/help/app-store-connect/reference/app-information/app-information/)
- [Set an app age rating](https://developer.apple.com/help/app-store-connect/manage-app-information/set-an-app-age-rating)
- [China mainland declarations](https://developer.apple.com/help/app-store-connect/manage-compliance-information/view-china-mainland-compliance-information)
- [Screenshot specifications](https://developer.apple.com/help/app-store-connect/reference/app-information/screenshot-specifications)
- [App preview specifications](https://developer.apple.com/help/app-store-connect/reference/app-information/app-preview-specifications/)

No readiness percentage or acceptance probability is assigned. The missing owner
acceptance, current media and live form evidence prevent a submission-ready verdict.
