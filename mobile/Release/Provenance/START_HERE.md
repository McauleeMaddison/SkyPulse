# SkyPulse provenance evidence — 29 September 2026

Status: **development traceability documented; owner confirms ARTA artwork and code ownership. The next candidate’s audio origin is now documented through a retained generator. Account history and supporting artwork records remain open.** No finding of copied material or terminated-account affiliation is made. This packet is an evidence inventory, not a legal certificate or assurance of App Store approval.

See [REMEDIATION_STATUS.md](REMEDIATION_STATUS.md) for completed fixes and remaining release work.

## What is now documented

- [FILE_LEDGER.csv](FILE_LEDGER.csv) identifies 167 files: all 164 current non-hidden, non-meta Unity Assets files and three retained visual references. Every row has its SHA-256, size, first recorded path introduction where available, current-vs-HEAD comparison, evidence note and unresolved rights status.
- [DEVELOPMENT_HISTORY.md](DEVELOPMENT_HISTORY.md) records reproducible Git milestones and the exact-byte link between the five preserved baseline sounds and the earlier SkyPulse prototype.
- [OWNER_STATEMENT.md](OWNER_STATEMENT.md) separates the owner's statements from missing facts. Do not silently turn a partial statement into an assertion about every asset.
- [APPLE_RESPONSE_DRAFT.md](APPLE_RESPONSE_DRAFT.md) offers a restrained, source-backed request for clarification. It is a draft and has not been sent.
- [INVENTORY.json](INVENTORY.json) records the baseline, scope and limitations. Regenerate the inventory with `python3 mobile/Tools/build_provenance_ledger.py` from the repository root after asset changes.

The ledger's first-path dates are Git author dates, which are self-reported and editable. They are evidence of the retained repository narrative, not independently authenticated dates. Renames are followed by Git heuristics. A file may have changed since its first introduction; its current SHA-256 is the identity of the audited bytes. Uncommitted new images deliberately have no invented historical commit.

## Remaining evidence, in practical order

| Area | Evidence already available | Fact or record still needed |
|---|---|---|
| Project origin and contributions | Owner confirms code ownership and VS Code development; Python prototype, Unity foundation, incremental changes | Retain contribution records and relevant dependency licences; editor choice is not proof of every contribution |
| Bird artwork: 120 frames | Owner confirms SkyPulse artwork generated in ARTA; import commits and frame tuning | Retain original generation exports/history and identify reference-image sources |
| Other worlds and power-up art | Owner declares ARTA origin; current hashes, path history and some specific generation notes | Preserve specific later built-in-tool generation records alongside the owner declaration |
| New five world images | User-provided JPEGs copied byte-for-byte; owner confirms ARTA generation | Retain original generation records and input/reference sources |
| September visual update and icon | Existing AI-generation notes, some output IDs and prompts | Origin/permission for reference images and previous bird/icon inputs |
| Five sound effects | Replaced with effects synthesized from retained source; old WAVs preserved outside Assets | Unity import validation and on-device listening check; see AUDIO_PROVENANCE.md |
| Apple account allegation | Rejection wording supplied by owner | Owner's account/submission history and clarification from Apple about actionable similarity details |

For AI work, retain whatever genuinely exists: original output files, conversation/export or generation IDs, dates, prompts, tool/provider and any reference images with their sources. Do not reconstruct missing prompts and present them as originals. ARTA’s published iOS terms and commercial-use provision have now been checked; see [ARTA_TERMS_RECORD.md](ARTA_TERMS_RECORD.md). AI generation alone does not establish exclusivity or resolve similarity concerns.

For purchased/downloaded work, retain the source URL, receipt and applicable licence. For commissioned work, retain the agreement and delivery record. Evidence may be retained privately; do not publish account credentials, payment details, private conversations or a whole repository unnecessarily.

## Release boundary

This remediation replaces five audio resources and retains the generator, originals and evidence outside Unity Assets. Game logic and preserved build 5/6 archives are unchanged. The current twelve-world source remains different from the historical uploaded build 6. New device testing, a newly numbered archive and matching media are still required for that changed source. This packet does not assert the new worlds exist in build 6.
