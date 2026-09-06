Unity visual-behaviour smoke checks
==================================

`SkyPulseVisualSmoke.cs` runs inside Unity Play mode in a disposable project copy.
Keep it outside the shipping Assets folder. Copy it and `SkyPulseBetaChecks.cs` into the test copy's
`Assets/Editor` directory, use a separate test company/product name in
ProjectSettings to isolate PlayerPrefs. The product name must contain `QA`; the harness clears that isolated test save. Run Unity with:

    -batchmode -nographics -projectPath <test-copy> -executeMethod SkyPulseVisualSmoke.Run -logFile <test-log>

Use a fresh Library directory for the copy; do not copy running editor process or
lock files. The harness enters Play mode, runs assertions, and exits with code 0
and `SKYPULSE_VISUAL_SMOKE_PASS` on success, or code 1 on failure.

Coverage: absence of flight trail and rear thrust renderers, 15/30/45/60-gate world changes, monotonic
dissolve and exact image handoff, recovery interval, restart during a transition,
hangar/tech tap-to-start, drag/swipe cancellation, purchase modal guard, and child
button click routing, touch-pointer dragging in both directions in both collection
screens, drag routing through cards, bottom-item reachability, and clearing fling
velocity when switching tabs. This headless check verifies behaviour, not rendered image
quality. Review the bird and backdrop blending in the Game view too.


Effects rendering pass
======================

`SkyPulseEffectsCapture.cs` complements the behaviour harness with real Unity
Camera.Render captures. Keep this helper outside shipping Assets. In a disposable
copy with a separate company/product name, copy it to Assets/Editor, open Unity
and choose **SkyPulse → Capture Effects QA**. It enters Play mode and invokes the
actual crystal collection, perfect-gate scoring, power-up collection and shield
consumption handlers. It writes 216 PNGs and PASS.txt under the project parent
folder's artifacts/effects-qa directory. PlayerPrefs are also backed up/restored,
but use an isolated project to avoid any effect on the player's saved state.

Coverage: three world backgrounds; normal and reduced motion; ordinary flight,
crystal, perfect pass, Aegis, Time Pulse and Crystal Magnet; initial flash,
expansion, fade and expiration; shield consumption; restart and menu cleanup;
absence of trail and rear thrust renderers. Captures render the gameplay camera;
inspect the full UI separately in the live Simulator/Game view.

Latest result (6 September 2026): PASS, Unity 6000.6.0f1, Metal.
See EFFECTS_QA.md for findings and scope.

Beta checks and screen captures (6 September 2026)
================================================

The smoke harness also invokes `SkyPulseBetaChecks.Run`: 1,300 generated gates,
route progression, unchanged handling, pause/resume, saved rewards, purchase guards,
15 bird pose sets and safe-area layout constraints. Both PASS markers were observed.

`SkyPulseBetaScreens.cs` produces 48 actual Unity renders at 540×960 and 660×1434,
including menus, privacy, purchase, gameplay feedback and all 15 unlock poses.
Copy it into an isolated QA project's Assets/Editor, then run its `Run` method with
graphics enabled. These are editor renders with simulated safe areas, not physical
iPhone validation or App Store marketing screenshots.

The earlier iPhone and Simulator native builds passed after correcting the source-plist layout. The final icon/counter rebuild is pending because automatic approval review reached its usage limit.
See ../Release/READINESS.md and SECURITY_REVIEW.md in that directory.

Final pass: the final source passed SKYPULSE_BETA_CHECKS_PASS and SKYPULSE_VISUAL_SMOKE_PASS in an isolated QA project. Both final native Release builds compiled and the final Simulator build was installed. Native manual dragging remains to be confirmed because computer-use drag injection behaved as a tap. See ../Release/BETA_VERIFICATION.json for precise scope.

Subsequent user verification: mouse scrolling works in both Bird Hangar and Tech Tree in the iOS Simulator; latest gameplay, behaviour and features were reported as good.
