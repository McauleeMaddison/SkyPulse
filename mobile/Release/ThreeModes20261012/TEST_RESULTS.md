# Changed-source verification — 12 October 2026

Result: **available automated checks passed** on Unity 6000.6.0f1. This verifies the changed source in an isolated Editor project; it is not a signed device release, a physical-device sign-off or an App Review decision.

## Isolation and repeatability

`mobile/Tools/run_modes_qa.py` copies Assets, Packages and ProjectSettings into a disposable project, sets both company and product to `SkyPulseQA20261006`, and copies the QA helpers into that copy's Editor folder. The integration harness checks that identity before testing. Only this QA save is reset. The owner's real game save was not reset. Run one QA copy at a time.

The wrapper completed successfully. After adding medal-save failure coverage and correcting the long Home protocol badge, the same complete suite ran again against refreshed source. Final gameplay log: `artifacts/modes-update-20261012/reproducible-run/final-layout-unity.log`. The original wrapper log and intermediate iterations are retained separately. Static scene/resource audit: `static-final.log` in the same directory.

## Passed checks

| Area | Evidence and coverage |
| --- | --- |
| Unity compilation/imports | No C# compiler errors. One enabled SkyPulse scene, no missing imported assets or scene scripts, 149 Resources assets. Player assembly has 11 runtime scripts; QA helpers are excluded. |
| Retained Endless | Existing beta checks cover 1,300 gates, route bounds, handling, pause/resume, one-time rewards, purchases, persistence, bird poses and safe-area fit. World expansion and release-edge checks also pass. |
| Source preservation | 21 protected method bodies and all original constants/catalogues match baseline `026e382`; seven integration methods match after normalizing their explicit dispatch/progress/wallet hooks. This is a source comparison, not an assertion that all possible runtime behavior is proven. |
| Pulse Relay | Every one of the four authored courses completes with the original flight forces in the real game integration. Separate model checks cover charge requirement/spending, duplicate cells, proximity/alignment, relay order, missed relay, hazard failure, exit completion and rejection of post-result events. Results do not credit a completion bonus or alter Endless best. |
| Blackout | A minimum-trace search finds a budget-feasible, gold-capable route for each of the four authored puzzles; those legal actions are executed against the session model. Checks cover adjacency, walls, firewall charge/trace costs, trace overflow, required packets, pause and rejection of repeated result input. Integration checks confirm no flight simulation, focus-loss pause and independent results. |
| Daily rewards | Progress accumulates across attempts; double claims fail; each task credits exactly five, at most 20 per UTC period. Forward rollover, backward clocks and restart are tested. Ten interrupted-claim cases cover failure before/after each of five flush stages and recovery to one credit. |
| Wallet isolation | Daily credits do not advance pickup tasks, run-crystal totals, bonuses or high scores. An interrupted claim recovers before an immediate upgrade debit. Actual pickup progress is emitted only at the flight pickup event. |
| Legacy saves/collection | Legacy bird and upgrade aliases retain ownership, equipped newbird10, levels, score, wallet and world progress. Full collection reload, purchase prerequisites and guards pass. Exactly 15 birds, 120 unique frame paths and nine unique three-level nodes with valid prerequisites are asserted. |
| Home and layouts | Protocol choice survives service reload. Reduced Motion freezes ambient circuit phase. 63 Editor renders cover 750×1334, 1320×2868 and 1640×2360. New-screen text fit and the full Blackout badge pass; representative phone/tablet captures and preview sheets were visually inspected. |
| Failure presentation | An intentionally injected medal-save IOException still opens the completed result screen and displays SAVE FAILED. |
| Artwork/audio/archives | All 90 flight frames pass registration. Five WAVs match the retained audio generator exactly. All baseline artwork and existing GUID bytes, plus the three recorded build-5/6/7 archive ZIP hashes, remain unchanged. Only the old NativeGame script changes among baseline Assets; four additional scripts/metas are new. No duplicate GUIDs among 185 current Asset metadata records. |
| Draft listing | Name 15/30, subtitle 24/30, promotional text 160/170, description 2410/4000 and keywords 87/100. Draft describes implemented modes, cosmetic birds, environments, local saves and daily rewards. |

Pass markers: `SKYPULSE_MISSION_PURE_PASS`, `SKYPULSE_BETA_CHECKS_PASS`, `SKYPULSE_RELEASE_EDGE_PASS`, `SKYPULSE_MISSION_LEGACY_PASS`, `SKYPULSE_MISSION_INTEGRATION_PASS`, `SKYPULSE_WORLD_EXPANSION_GAMEPLAY_PASS`, `SKYPULSE_MISSION_CAPTURES_PASS`, `SKYPULSE_WORLD_CHECKS_PASS`, plus `SKYPULSE_43A_STATIC_UNITY_PASS` in the static audit.

## Retained diagnostics and limits

The gameplay log contains a Unity Editor Search indexing `ArgumentOutOfRangeException`, with its stack in `UnityEditor.Search.SearchDatabase`. Licence/service/USB diagnostics are also retained. The injected medal-save warning is expected test evidence. Do not describe the entire Editor log as warning-free.

The owner's previous iPhone 17 Pro Max testing/recording and iPad simulator sign-offs apply to **build 7 only**. The changed candidate still needs a new unused build number, a fresh signed device build, the iPhone/iPad checks in `DEVICE_CHECKLIST.md`, profiling and fresh candidate-specific recordings. No new binary was created or uploaded here.

Apple's unidentified terminated-account similarity comparison remains unresolved. New gameplay and accurate local records address controllable differentiation/evidence work; they do not identify or disprove Apple's comparator. Some original art/reference provenance is still missing. Submission remains withdrawn pending Apple's answers and the owner's explicit authorization.
