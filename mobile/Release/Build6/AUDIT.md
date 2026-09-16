# Build 6 review recovery — 16 September 2026

## Rollback verified before app changes

- Original clean checkout: `codex/neon-visual-update`, `258f1db65b23e330a163e7d9a07539c0fb047cfb`.
- Working branch: `codex/build-6-review-recovery-20260916`.
- Preservation commit: `4a704ed6c08fcc972d4aa8c1207da0c85b4720ef` (empty commit; identical source tree).
- Annotated tag: `skypulse-build-5-preserved-20260916`.
- Verified complete Git bundle: `artifacts/skypulse-build-5-preserved-20260916.bundle`.
- Existing archive preserved: `artifacts/release-2026-09-08/SkyPulse-1.0.0-5.xcarchive`. All 44 file hashes recorded in `artifacts/build-6-review-recovery/build5-preservation.json`.
- Existing `mobile/Builds/iOS-store-5` and `iOS-simulator-store-5` are not export destinations for this work.
- Today's source still had build number 5, but its gameplay SHA256 differs from the September 7 build 5 export. The preserved source is the exact starting checkout; the archived binary is separately preserved. Do not treat them as identical.

To inspect/restore the preserved source without discarding current work, create a separate checkout:

```
git worktree add ../SkyPulse-preserved-20260916 skypulse-build-5-preserved-20260916
```

The original branch is unchanged. The bundle is a local independent Git-history backup, not an off-machine backup or a binary archive.

## Live App Store Connect audit

Read directly from the signed-in record on 16 September 2026. No listing changes had been saved at the time of this snapshot.

- App: **SkyPulse Arcade**, Apple ID `6809440803`.
- Bundle ID: `com.mcauleemaddison.skypulse` (matches Unity and preserved export).
- Locale: English (U.K.). Subtitle: **Flap. Flow. Fly**.
- Store version: **1.0**. Selected binary: **1.0.0 (5)**. Status: Rejected.
- Submission: `d5ba215d-daa2-4d6b-9441-3ae6e1507bc6`.
- Apple's September 15 message: **Guideline 4.3(a), Design — Spam**. Reviewed on **iPad Air 11-inch (M3)**. Apple describes similarity in binary, metadata and/or concept, but does not identify the specific category or another app.
- Owner clarification reply already sent September 15 at 4:42 PM. Do not send a duplicate clarification or claim Apple answered it.
- Three submitted screenshots: `IMG_0724.png`, `IMG_0726.png`, `IMG_0725.png`, from 6.9-inch display, reused for 6.5-inch. Visible thumbnails cover Home, Tech Tree and Hangar. No App Preview is present. They are not build 6 evidence.
- Description starts “Tap to flap. Find your flow.” It names Neon City, Acid Foundry and Orbital Bazaar, 15 birds, crystals, upgrade tree and three power-ups.
- Review Notes explain Play, pause, Bird Hangar, Upgrades, earned crystals and Privacy, but not the three tech branches, prerequisites, world score milestones or detailed navigation.
- **Sign-in required is checked**, while Notes say no account is required. This is a metadata inconsistency to correct.
- Existing review attachment: `ScreenRecording_09-10-2026 2-18-36 pm_1.mov` (UI filename contains a narrow space). Not build 6 evidence.
- Categories: Games / Action / Casual. Age rating: 4+ globally, with regional exceptions shown by Apple.
- Copyright: 2026 Mcaulee Maddison. Automatic release after approval selected.
- Support and privacy pages were opened in the browser and loaded with game-specific content and the public support email. URLs are configured in `SkyPulsePublicLinks.json` and match the listing support URL.

Live record: https://appstoreconnect.apple.com/apps/6809440803/distribution/ios/version/inflight
Review thread: https://appstoreconnect.apple.com/apps/6809440803/distribution/reviewsubmissions/details/d5ba215d-daa2-4d6b-9441-3ae6e1507bc6
Apple policy: https://developer.apple.com/app-store/review/guidelines/#spam

## Source and asset findings

- Unity 6000.6.0f1. Manifest/lockfile contain Unity modules, Sprite, uGUI and Unity device simulator definitions; no third-party game-template package was found. This cannot establish uniqueness against Apple's private app corpus.
- Shipping scene is `Assets/Scenes/SkyPulse.unity`. Runtime scripts are under `Assets/Scripts`.
- No player-facing Beta/Flappy/template string was found in runtime source. “web beta” occurs only in a source comment; an editor export default folder contains “iOS-beta”. These are not visible app branding. Unity template ID/default-scene fields are empty.
- Current route selects only the first three worlds: Neon City (start), Aurora Rise (score 15), Solar Drift (score 30); it rotates every 15 gates afterward. Additional catalog entries are not reachable route worlds and must not be advertised as such.
- 15 cosmetic birds share handling. Nine Tech Tree nodes have three levels each across Collection, Recovery and Mastery; prerequisite chains require level 2 above.
- Aegis absorbs one collision; Time Pulse lasts 4 seconds; Crystal Magnet lasts 6 seconds. First pickup gate is selected from route-score range 8–12, so discovery requires surviving the opening section.
- Background/crystal/pipe provenance notes record generated artwork. They do not establish rights for every bird/audio/reference source. Do not claim all art is hand-drawn or that an exhaustive rights audit is complete.
- Earlier local store copy and review drafts are historical. The existing `APP_REVIEW_RESPONSE.md` concerns Guideline 2.1, not this rejection.

## Implemented presentation changes

- Home shows actual owned birds, purchased tech levels and the next world milestone.
- Main/result/navigation labels consistently say Tech Tree.
- Home has a Worlds & Power-ups guide showing the three real route backgrounds, score thresholds, pickup art/effects and earned-crystal progression.
- No new purchases, currency grants, review-only bypasses, physics changes or save-key changes.
- iOS build number changed from 5 to 6 in working source only.

## Verification boundaries

Unity compilation and isolated QA checks passed. The existing 1,300-gate/economy/persistence regression ran successfully, as did the added navigation/progression checks. All 90 bird frames pass registration checks. Editor captures at 750×1334, 1320×2868 and 1640×2360 are local QA evidence with artificial saves, not App Store or physical-device screenshots.

The final candidate compiles with no C# compiler errors and its gameplay/UI checks pass. The QA Editor log also contains a Unity Search indexing ArgumentOutOfRangeException, separate from the app checks; this is not a completely error-free Editor log. Native iPad Home, Hangar, Tech Tree/details, guide and gameplay launch have been inspected. The owner subsequently authorized the physical iPhone test; the exact final archive was installed and launched, with hands-on results still pending. Upload, processing, build selection, physical-phone screenshot/video evidence and submission are not complete.

## Owner steering: native iPad support

The owner explicitly selected native iPhone and iPad support during this session. Release configuration now targets iPhoneAndiPad. Portrait gameplay and safe-area fitting are retained. The earlier iPhone-only build 6 exports are superseded and must not be uploaded. Those initial universal exports were also superseded by the final exports below. Native iPad screenshots are now required alongside iPhone evidence.

## Additional presentation correction

Native Hangar inspection exposed performance bars that differed by bird despite shared physics. Build 6 now replaces those with shared-handling disclosure, actual crystal balance and remaining unlock cost. The pause button in the current source is top-right; old review notes and the support website refer to top-left. Final Review Notes are corrected; the separately hosted support page still needs its own wording update. Final exports have the suffix `universal-6-final`; earlier build 6 exports/archives are superseded.

## Final candidate verification

- Final device source SHA256: `f80a9691940ac2b3954b408778e1200fafd50678984f6670b564d89b24c18507`.
- Release simulator build and signed device archive succeeded. Device-family metadata is `[1,2]`, version `1.0.0`, build `6`, portrait/full-screen.
- Apple validation-only export succeeded with a warning: UnityRuntime.framework dSYM is missing for UUID `08E1664C-D28D-3927-82AF-CC0AC420CBD9`. The installed Unity iOS module does not contain that dSYM. Preserve the warning; do not fabricate symbols or claim clean symbol upload.
- Signed archive: `/private/tmp/skypulse-build6-20260916/SkyPulse-final-1.0.0-6.xcarchive`. Build outside Desktop/iCloud to avoid FinderInfo xattrs invalidating code signing.
- Durable archive and included dSYMs: `artifacts/build-6-review-recovery/SkyPulse-final-1.0.0-6.xcarchive.zip`; ZIP integrity checked. SHA256 `ea0ae0bc4a9efae63c902ec684ca78a42b2074fa7bedbb9b1e438d3108ec12bf`.
- `final-verification.json` records matching source hash, archive metadata and all 44 preserved build 5 file hashes rechecked unchanged.
- Native simulator captures are under `final-native-ipad/`; these are not physical-phone captures. Editor captures remain QA-only.
- Revised listing text and Review Notes saved live. Build 5 remains selected; old screenshots remain until replacement evidence is ready. No new review-thread reply or submission has been sent.
