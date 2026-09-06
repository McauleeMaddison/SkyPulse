Unity visual-behaviour smoke checks
==================================

`SkyPulseVisualSmoke.cs` runs inside Unity Play mode in a disposable project copy.
Keep it outside the shipping Assets folder. Copy it into the test copy's
`Assets/Editor` directory, use a separate test company/product name in
ProjectSettings to isolate PlayerPrefs, and run Unity with:

    -batchmode -nographics -projectPath <test-copy> -executeMethod SkyPulseVisualSmoke.Run -logFile <test-log>

Use a fresh Library directory for the copy; do not copy running editor process or
lock files. The harness enters Play mode, runs assertions, and exits with code 0
and `SKYPULSE_VISUAL_SMOKE_PASS` on success, or code 1 on failure.

Coverage: sustained horizontal trail length, preserved historical flight path,
reduced-motion particles, clean resets, 15/30/45/60-gate world changes, monotonic
dissolve and exact image handoff, recovery interval, restart during a transition,
hangar/tech tap-to-start, drag/swipe cancellation, purchase modal guard, and child
button click routing, touch-pointer dragging in both directions in both collection
screens, drag routing through cards, bottom-item reachability, and clearing fling
velocity when switching tabs. This headless check verifies behaviour, not rendered image
quality. Review the actual trail glow and backdrop blending in the Game view too.
