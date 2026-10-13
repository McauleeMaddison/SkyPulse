# Local validation — 12 October 2026

Unity 6000.6.0f1 compiled and ran the local source in disposable QA projects. No shipping-device export, signed archive or upload was produced for this revamp.

The final full run passes eleven markers: mission pure, Blackout pure, 1,300-gate beta regression, release edges, legacy saves, mission integration, world-expansion gameplay, Blackout live UI, mission captures, presentation captures and world checks. Captures include 750 × 1334, 1320 × 2868 and 1640 × 2360 viewports. The preserved original Endless methods/constants/catalogues pass the existing protection check; `SkyPulseNativeGame.cs` is unchanged from the uploaded build 9 source.

Focused checks cover:

- Shortest winning paths for all eight authored sectors, replayed through the actual session model. Minimum costs rise strictly; core count/area do not decrease and spare trace does not increase.
- Locked-stage rejection, sequential unlocks after reload, best-medal persistence, and a deliberately failed store flush that cannot expose a new unlock.
- Preservation of the old four-puzzle medal JSON and wallet; a security visit costs exactly two trace and cannot accidentally spend a hack charge.
- Actual touch movement and duplicate-input blocking, firewall selection with no spending, one-charge/three-trace breach, and subsequent movement through the opened node.
- Pause freezing ambient phase, transport and trace; Reduced Motion completing the touch loop; extraction, Next Sector, clean restart/navigation, no duplicate finalization or wallet/high-score change.
- No Blackout backdrop leaking into Relay selection; shared tested viewport widths and text-height fits.

The initial full run's capture assertion expected Blackout to use the old shared hero component. The test was updated to verify the new dedicated core and the retained Relay portal separately. The QA coroutine guard now reports nested capture failures instead of leaving an idle Editor process. The initial failed log is retained.

Unity's existing Search indexing exception and service diagnostics, plus deliberately injected persistence failures, remain in the logs and are distinct from runtime C# compilation errors. The final run returns success with all expected markers. No fresh physical-device pass, measured phone frame-rate result or claim of universal bug freedom is made.

Remaining device work applies to a separately numbered future candidate: touch target comfort, sustained animation/frame pacing, pause/background/resume, Reduced Motion, all eight stages, retained progression and the unchanged other modes. Build 9 on TestFlight still contains its previous Blackout presentation and four puzzles.
