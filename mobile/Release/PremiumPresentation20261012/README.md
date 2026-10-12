# Premium presentation and badge navigation — 12 October 2026

This update follows the owner's visual feedback and later badge-dropdown instruction. It supersedes the earlier full-screen mode chooser and retained-mode startup behavior. A new launch shows Home in Endless Route. Selection remains active during the current session; the existing Home preference key is retained for backwards compatibility.

Open `/Users/mcauleemaddison/Desktop/SkyPulse/mobile` in Unity **6000.6.0f1**, open `Assets/Scenes/SkyPulse.unity` and press Play. Tap the **top-left mode badge**, select one of the three free modes, then press **PLAY**. Endless starts flight; Relay and Blackout open their four mission/puzzle choices. The dropdown grows from the badge in 0.2 seconds, dismisses on an outside tap, and changes modes without starting gameplay. Reduced Motion makes its expansion/dismissal immediate.

## Visual changes

- Pulse Relay uses the existing cinematic backgrounds and prepared textured pipe/collar artwork from Endless. Its four missions use Neon City, Aurora Rise, Solar Drift and Midnight Tide scenery. Gold shutter beams, cyan-lit ports, numbered address plates, soft charge illumination, separate instruments and a machined control deck distinguish the circuit mode.
- A successful Pulse opens the shutter immediately for gameplay. A decorative 0.28-second beam contraction, port lighting, gently drifting scenery and charge movement communicate that action. The visual animation does not alter objective positions or flight forces.
- Home's Relay theme layers the original scenery with animated circuit routes and orbit lighting. Blackout uses green terminal architecture, routed packets, a crisp data lattice and a restrained scanline treatment. Both keep the original animated mechanical bird and shared collection controls.
- Blackout gameplay has connected-node artwork, distinct packet/firewall/exit/cursor glyphs, machined telemetry panels and gentle ambient packet/cursor motion. Node actions, budgets and authored solutions are unchanged.
- Mission lists, result controls and daily mission cards share the new bevelled gradient surfaces and illuminated action treatment. Home now has a smaller Play action instead of a large mode chooser.

## Verification and evidence

Run these from the repository root:

```sh
python3 mobile/Tools/verify_endless_preservation.py
python3 mobile/Tools/verify_bird_registration.py
python3 mobile/Tools/run_modes_qa.py
```

The final command creates a disposable copy with a separate company/product identity, runs the retained regression suites and captures 63 stills plus 200 animation frames. Test scripts remain outside production Assets. Do not run two QA copies concurrently. Never run save-resetting harnesses against the real player save.

See [test results](TEST_RESULTS.md), [provenance addendum](PROVENANCE_ADDENDUM.md), [candidate record](RELEASE_RECORD.json), local [metadata](metadata.json) and [reviewer walkthrough](REVIEWER_WALKTHROUGH.txt).

Local animated previews (actual Editor renders, isolated save):

- [Relay and Blackout gameplay](../../../artifacts/premium-presentation-20261012/previews/gameplay-motion.gif)
- [Badge dropdown](../../../artifacts/premium-presentation-20261012/previews/badge-dropdown.gif)
- [Home themes](../../../artifacts/premium-presentation-20261012/previews/home-motion.gif)

Native-size stills and frame sequences are in `artifacts/premium-presentation-20261012/final-qa/captures/`. The GIFs arrange actual captured frames; flight is an assisted QA replay, not physical-device footage. Older captures/archives are preserved separately.

## Later physical-device checks and release

Use the earlier [changed-candidate checklist](../ThreeModes20261012/DEVICE_CHECKLIST.md), with this navigation amendment: **new launch → Endless Home → top-left badge → select mode → Play**. Check dropdown expansion/dismissal, top-left safe areas, each selected mode's Play destination, and return Home. Verify readable controls and particle/charge/shutter motion with Reduced Motion both on and off. Inspect Relay texture memory/frame pacing on iPhone and iPad; mobile performance is not proven by Editor captures.

Use a **new unused build number** when later exporting this changed candidate. Existing build 7 footage/sign-offs cover historical build 7 only. No new signed binary, physical-device sign-off, App Store Connect change, website publication or Apple message was made. The owner's submission hold remains in force.
