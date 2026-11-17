# SkyPulse 1.0.0 (22) — owner test candidate

Build 22 was installed over Build 21 and launched on the owner’s iPhone on
16 November 2026. It is also processed in TestFlight’s existing SkyPulse Internal
group. Current instructions and the saved App Store draft selecting Build 22 were
reloaded and verified on 17 November. **Public App Review remains on hold.**

App Store preparation: [readiness report](APP_STORE_READINESS.md) and [media plan](MEDIA_CAPTURE_PLAN.md).

Start with [DEVICE_CHECKLIST.md](DEVICE_CHECKLIST.md), then record observations in
[TEST_RESULTS.md](TEST_RESULTS.md). No owner visual/gameplay acceptance is inferred
from automated checks or the successful install. Do not uninstall or reset your save.

Runtime source is Git main `1fd3dfa62b93e35fc4d94439eea17ccfac0091ff`. The exported source
manifest matches all 2,559 recorded entries; the approved bird assets and wing
sampler are retained. All 44 preferences matched across installation. Daily state
refreshed on launch and the owner confirmed choosing Neon Finch.

- [Release/install/save evidence](RELEASE_RECORD.json)
- [Source and Editor QA](QA_RECORD.json), [full QA report](../../Development/Current/QA_REPORT.json)
- [Native verification and symbol warning](NATIVE_VERIFICATION.json)
- [Saved store draft and remaining media checks](APP_STORE_DRAFT_RECORD.json)
- [Source/archive/metadata and live HTTPS checks](METADATA_CHECKS.json)
- [Actual Unity previews](../../Development/Current/UnityAcceptance/README.md)

Signed app, Xcode export/archive, rollback builds and private save backups remain
in the ignored Desktop project folder `artifacts/device-build22/`. Private saves,
contact details and generated app binaries are not committed. Native temporary
logs from 16 November no longer persist; the release record states that limit.

Known follow-up: UnityRuntime.framework’s matching dSYM was not supplied by the
installed Unity toolchain. Apple accepted the upload with that symbol warning;
app and UnityFramework symbols match. Unity’s Editor-only SearchDatabase exception
also remains recorded. Neither is labelled as fixed.

This is ready for the owner to start acceptance testing, not a claim of universal
bug freedom, completed device profiling or Apple approval.
