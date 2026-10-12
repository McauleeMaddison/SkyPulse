# Current provenance addendum — 12 October 2026

The 29 September audit and 6 October build-7 evidence remain historical. They did not include Pulse Relay, Blackout Protocol or Mission Control. This addendum describes only the changed source candidate.

## New contributions and actual origins

The owner supplied the retained `SPECIFICATION.txt` on 12 October. Codex implemented the new code in this session against the latest local source. The owner previously declared ownership and VS Code development; that declaration does not independently certify all contributions or rights.

- `SkyPulseMissionModels.cs`: authored relay layouts and objective sessions; authored terminal grids and turn-based puzzle rules. No external template, data set or generator is used.
- `SkyPulseDailyMissions.cs`: UTC periods, local versioned state, gameplay-event progress and reward journal; PlayerPrefs adapter and separate medals. No network, account or server-backed anti-cheat.
- `SkyPulseNativeGame.Missions.cs`: Unity integration, protocol navigation, relay rendering, grid UI, Home themes and wallet-source dispatch. Geometry is authored from rectangles, rails, numbering, static scanlines and existing UI primitives. Flight reuses the existing equipped bird and existing Aegis artwork. Mission Control reuses the existing crystal artwork.
- `SkyPulseCircuitGraphic.cs`: retained code draws vector/pixel circuit graphics and restrained Home packet movement. No bitmap generation, downloaded imagery, external shader or new font input was used. Text uses the project's existing OS-font/fallback setup.
- QA/verification tools remain in `mobile/Tools`, outside shipping Assets. The temporary Editor copies are not production input.

Contribution traceability: retained baseline commit `026e382`, preserved working-copy tar, subsequent Git diff/commits, implementation specification, verification logs, protected-body comparison and a new dated file ledger. Git author dates are repository metadata, not independently authenticated creation dates.

The new ledger lives under this addendum's `Provenance/` directory so the earlier ledger/date is not rewritten. It records current paths, sizes, SHA256 values and available Git path-introduction history. Procedural visuals are evidenced by retained implementation, not fabricated image-generation prompts.

## Existing records verified and preserved

All five WAVs match the retained `generate_skypulse_audio.py` output. The previous sound files remain outside Assets; their old origin gap is not silently converted into a claim about historical binaries. Original artwork paths and existing `.meta`/GUID bytes, plus the recorded archived ZIP hashes, are unchanged.

Owner-declared ARTA artwork remains distinct from specifically documented later image-generation work in NeonVisualUpdate/ICON/world expansion records. The owner’s account-history statement and the Apple support request, case 102987914250, remain correctly attributed. This update makes no finding that Apple’s terminated-account comparison was identified or disproved.

## Genuine missing evidence

Original ARTA exports/history and exact prompts are not complete for the bird frames, some worlds and power-up imagery. Origins/permissions for some supplied reference images and early icon/bird inputs remain unverified. Later notes contain partial generation information, not a complete rights ledger. No missing dates, receipts, licences, prompts or permissions were invented.

Apple has not supplied an actionable component/asset/metadata/concept comparator in the records read for this update. No new Apple reply was inspected, and no external comparison corpus was searched. Normal Unity frameworks, package identifiers, bundle ID and signing configuration were retained; the shipping scene remains SkyPulse.unity.

Device evidence gap: the retained build-7 iPhone recording and owner sign-offs cover build 7 only. New images are isolated Editor QA renders. A new numbered binary and physical iPhone/iPad verification remain required before submission.
