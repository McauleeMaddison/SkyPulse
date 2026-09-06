# Visual verification scope — 6 September 2026

Candidate: 1.0.0 (3), gameplay/icon hashes in BETA_VERIFICATION.json. The latest source is installed in the Xcode iOS Simulator. This is evidence of the listed checks, not a guarantee of universal player satisfaction.

| Area | Evidence / result |
| --- | --- |
| App icon | Final two-wing cosmic design, opaque 1024px; 256/180/120/60px inspection; final Simulator Home Screen checked. |
| Bird trail and square flashes | Runtime trail/rear thrust removed; alpha correction and effects lifecycle checked in Unity rendering suite. |
| Pickups / perfect pass / powers | 216 Unity renders across three worlds and normal/reduced motion; expiration and cleanup included. |
| Menus / modals / unlocks | 48 Unity renders at two portrait sizes; final Simulator home/privacy/hangar/purchase/result/pause inspected. |
| Counters | Crystal sprite visible in final native menu, HUD and hangar counters. |
| Controls | Final Simulator launch, retry, pause and Reduced Motion toggle checked. Preference restored after test. |
| Course progression | Final automated regression includes 1,300 generated gates and world milestones; subjective difficulty still needs tonight's playtest. |
| Bird frames | 90 flight-frame registrations match source art; 15 unlock poses reviewed. |
| Swipe gestures | Synthetic Unity tests pass. User manually confirmed mouse scrolling works in both Bird Hangar and Tech Tree in iOS Simulator. |
| Long-session performance | Not certified by these captures or short Simulator checks. |

Tonight: report any overlap, clipped artwork, square/white flash, unclear pickup, missed input, unwanted action while dragging or unfair difficulty change with its score and screenshot. Resolve confirmed defects before signing the release candidate. See TOMORROW_PLAN.md for the remaining launch work.

User playtest feedback: latest gameplay, behaviour and features reported as good. Keep the beta feature set stable; only change confirmed defects or clear readability problems before submission.
