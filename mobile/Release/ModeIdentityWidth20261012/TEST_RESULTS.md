# Verification — 12 October 2026

The changed candidate passes isolated Unity 6000.6.0f1 compilation, gameplay regressions and visual checks. These results validate the current source and imported assets in the Editor, not a newly signed physical-device binary.

| Check | Result |
| --- | --- |
| Full mode/daily/save suite | All nine expected pass markers present |
| Original Endless preservation | 21 protected method bodies and original constants unchanged; seven integration hooks checked against their original behavior |
| Gameplay regressions | 1,300 route gates, pause/resume, power-up timing, persistence, purchase guards and one-time rewards pass |
| New modes | Relay charge/order/exit/failure checks and all authored Blackout puzzle solutions pass |
| Daily economy | Five per claim, maximum twenty per day, interrupted-claim recovery and no recursive collection progress pass |
| Legacy contracts | 15 birds, 120 distinct frame paths, nine three-level Tech nodes and legacy saves pass |
| Equal mode width | 27 camera-width and 27 interface-width assertions pass across 750×1334, 1320×2868 and 1640×2360 |
| Home identity | Alternate backdrops fill the original canvas; Relay pilot/portal and Blackout core views are present; only Blackout hides the Home bird |
| Motion/accessibility | Pause freezes Relay presentation; Reduced Motion freezes scenery/charge animation and opens/dismisses the complete badge dropdown immediately |
| Static import/player audit | One enabled SkyPulse scene, 151 Resources assets, 12 runtime scripts, no missing assets/scripts, no QA helper in player compilation |
| Source and asset preservation | Exact QA source/PNG/meta hashes match this candidate; 188 unique GUIDs; original artwork, GUIDs, two app archives and retained support-site ZIP hashes preserved |
| Visual inspection | 63 stills inspected as applicable; 200 full-suite animation frames plus 280 expanded comparison frames rendered; Home/gameplay comparison sheets inspected |

No C# compiler errors occurred. The logs retain the known Unity Editor Search indexing exception and service diagnostics, plus the deliberately injected medal-save IOException. The corrected static audit passes. These diagnostics are not hidden or described as a physical-device crash test.

Evidence is local under `artifacts/mode-identity-width-20261012/`: `final-qa/unity.log`, `static-unity.log`, `comparison-unity.log`, `endless-preservation.json`, `final-verification.json` and `previews/`. Log/source hashes are retained in `RELEASE_RECORD.json`. Captures use isolated QA saves and assisted flight, not the owner's retained save.

The final QA copy contains all current runtime bytes. The later comparison helper adds Endless Home/flight to the already tested five motion scenes without changing runtime code. The repeatable QA runner now captures seven scenes. No new signed archive, upload or renewed device sign-off was produced here. Repeat the physical iPhone/iPad checklist on the next numbered candidate, including frame pacing and texture memory. Submission remains on hold.
