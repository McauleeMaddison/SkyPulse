# Build 11 identity, metadata and artwork audit

SkyPulse Arcade is Mcaulee Maddison's Unity mobile project. This pass makes the release record, listing copy and artwork evidence specific to the current three-mode candidate. It does not alter gameplay or the already uploaded binary.

## Completed

- Added [CURRENT_RELEASE.json](../CURRENT_RELEASE.json) as the authoritative pointer to build 11 and its exact QA/source evidence. The release checker previously defaulted to build 9; it now follows this contract and rejects mismatched build, creator, mode copy, source hashes, identifiers or runtime links.
- Rewrote [metadata.json](metadata.json) around the creator, named route worlds, daily automatic Relay gates, eight Blackout cipher/phase sectors and current Home/result controls. Subtitle, promotional text, description, keywords and review notes were saved in English (U.K.) in App Store Connect. Review remains on hold.
- Replaced build 9's selection in the editable public draft with build 11 at the owner's request. Previously uploaded binaries remain Apple history; build 11 is the selected candidate and is also Testing in SkyPulse Internal.
- Recorded all 145 Unity source images with SHA-256, original evidence links and provider/use-rights basis in [ARTWORK_LEDGER.json](ARTWORK_LEDGER.json). No exact byte-duplicate image group was found. The owner's reference and lifetime-purchase statements are recorded accurately. See [RIGHTS_RECORD.md](RIGHTS_RECORD.md) and the repository [NOTICE.md](../../../NOTICE.md).
- Retained bundle/team identity, company/product names, save compatibility, economy and all 378 exported source/configuration files unchanged. Company/product names affect Unity save locations; a creator-credit change must not move existing player progress.

## Validation

Nine focused metadata tests pass, including rejection of build 9 copy, retired PULSE-button instructions, wrong creator, missing mode, repeated/empty keywords, UTF-8 keyword overflow and placeholder text. [Metadata/archive checks](METADATA_ARCHIVE_CHECKS.json) validate 197 unique Unity GUIDs, the iPhone/iPad archive identity, privacy manifest and actual packaged branded URLs. [Online checks](METADATA_ONLINE_CHECKS.json) verify both public pages return HTTP 200 with the support contact. [Artwork checks](ARTWORK_CHECKS.json) verify full ledger coverage and exact file hashes.

The 19 current runtime scripts and all 378 manifest entries match build 11. The retained Unity log's SHA-256 and all 14 required QA markers were checked against [QA_RECORD.json](../DailyRoutes20261013/QA_RECORD.json). Gameplay tests were not rerun for this documentation/tool-only change. A representative 40-image contact sheet was visually inspected for branding/reference concerns; this is a visual check, not a third-party similarity database search.

## Remaining release boundaries

Fresh physical build 11 gameplay and sustained performance checks are pending. Store screenshots and the retained review recording are older build 7 evidence and need current replacements before submission. The known vendor UnityRuntime dSYM gap limits engine crash symbolication. These facts remain in [the signed release evidence](../DailyRoutes20261013/RELEASE_RECORD.json).

Provider terms support output ownership/use subject to their conditions, but cannot establish that every AI output is exclusive to one person. The ARTA lifetime receipt and original generation exports have not been independently inspected. Apple's private comparison and terminated-account reference remain unidentified. The public version is **Prepare for Submission**, and no Add for Review/Submit action or Apple message is authorized during this pass.

See [APP_STORE_DRAFT_RECORD.json](APP_STORE_DRAFT_RECORD.json) for verified saved fields and candidate selection. Historical release records remain evidence; they are not the current default.
