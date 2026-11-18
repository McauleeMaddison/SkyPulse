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
| Metadata validation | All field limits pass; 16 focused guards pass; 71 runtime scripts and all 2,559 manifest entries match | No claim of global uniqueness or Apple approval |
| App information | Name/subtitle/categories and privacy URLs verified 16 November | Live age questionnaire, content-rights declaration, price, territories and account compliance still need verification |
| Signing/privacy archive | Correct identity, iPhone/iPad families and packaged links; required UserDefaults privacy reason; strict signatures pass | Vendor UnityRuntime dSYM warning remains; app and UnityFramework dSYMs match |
| Saved progress | All 44 preferences identical across install; after-launch changes limited to daily rollover and owner-confirmed bird selection | Owner confirms in-game state during checklist |
| Public pages | All four branded pages published for Build 22 and returned HTTPS 200 with expected content | Current guide covers seven pickups, 36 Tech levels, mastery, daily heists and restoration; browser layout QA remains unverified |
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
**local reviewer-note draft**, replacing it with current Unity 3D implementation
wording. This revision is **not yet saved to App Store Connect**; browser editing
controls are unavailable. The 17 November live-save record is retained separately.

## Complete before considering submission

1. Work through [DEVICE_CHECKLIST.md](DEVICE_CHECKLIST.md) and record
   [TEST_RESULTS.md](TEST_RESULTS.md). Resolve reproducible failures; a runtime
   change requires a new numbered build and affected retests. Include actual
   iPhone/iPad model, OS, session length and measured performance evidence.
2. The owner reports saving **macwebstudio26@gmail.com** in App Review Information
   on 17 November. No further correction is requested. Independently reload and
   verify it when browser control returns; the owner’s save is recorded separately
   from the earlier agent-observed draft.
3. Verify the live **age-rating questionnaire**, content-rights declaration,
   free price, selected territories and applicable account/compliance details.
   A low age rating is not inferred from the artwork. Accessibility features are
   not declared supported until their published evaluation criteria are met.
4. Capture approved current media using [MEDIA_CAPTURE_PLAN.md](MEDIA_CAPTURE_PLAN.md).
   Existing store screenshots/preview and build 7 review walkthrough are historical.
   Keep their backups before replacing them. Editor QA images are not phone evidence.
5. The owner reports **excluding China mainland and saving availability** for
   the initial release on 17 November. Independently verify that saved selection
   before submission; do not change other territories without a new instruction.
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

- No player account, chat, user content feed, advertisements or real-money purchases.
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
