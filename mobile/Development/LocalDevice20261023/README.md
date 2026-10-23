# Local iPhone repair candidate — 23 October 2026

Source `6692cd61aecbddde3b34fd65031186ef2b8cd358` on
`codex/dimensional-flight-20261022` was exported, signed, installed over the
existing SkyPulse app and launched on the paired iPhone 17 Pro Max. This is
version 1.0.0, local build 18. No uninstall, TestFlight upload, App Store Connect
change, public review submission or main merge was performed.

**This candidate failed presentation acceptance:** the owner reported unpolished
Endless buildings and Blackout puzzles that felt random and lacked a compelling
objective, then supplied a 33.34-second iPhone recording. Installation and launch
are confirmed, but do not establish visual or gameplay acceptance. Do not use
this candidate as approved store media or a submission candidate yet.

## Included repairs

- Symmetric top/bottom pipe shaft seating behind the curved collar shoulder.
- Travelling cyan/pink LED light packets along shafts and around collar rims.
- Rounded horizon architecture replacing the rejected pyramid/spire scenery.
- Clearing the previous run's score, crystal or world-entry feedback on retry.
- The existing Relay hoops, automatic crystals/gates and Signal Heist puzzle
  implementation are retained. The retired October 12 prototype is not used.

See [the repair report](../PipeFinish20261022/README.md) for render comparisons,
actual Unity gameplay footage and the 23 automated check groups. Current runtime,
shader and audio hashes still match that completed QA run; the unchanged suite
was not rerun solely for this native rebuild.

## Native validation

- Unity 6000.6.0f1 iOS export exited 0; Xcode Release build succeeded.
- All 453 source-manifest entries matched the working tree byte for byte.
- The installed bundle uses the existing bundle ID, build 18, iOS 15 minimum
  and iPhone/iPad device families. Deep/strict code-sign verification passed.
- Device install and launch commands both returned success on iOS 27.2.
- Eleven metadata-copy tests passed; artwork-ledger coverage matched 145 images
  with no exact-byte duplicate groups. Protected rules, scenes and 235 unique
  Unity GUIDs passed the preservation check.

The native build still emits warnings from Unity-generated code/toolchain
integration, including libc++ platform availability, deprecated native APIs and
a script phase without declared outputs. A successful build is not a claim that
the native log has no warnings. Logs are saved under
`artifacts/local-device-20261023/` (ignored); compact results and binary hashes
are in `BUILD_RECORD.json`. `SOURCE_MANIFEST.json` identifies the exported files.

## Remaining checks

The newer scenery/objective work on this branch follows these reports and is
separate from the signed candidate recorded here. After validating that follow-up,
repeat the relevant phone checks:
top/bottom joins, LED motion, scenery, Relay automatic gates, Signal Heist input
and progression, pause/retry and retained progress after reopening. Speaker and
headphone balance, sustained frame rate, memory and thermals remain unmeasured.
The phone uses the same network transport as the previous unsuccessful profiler
attempt, so that attempt was not repeated without a connection change.

The saved Relay/Signal Heist reference media under `PhoneReference20261022/`
depicts the earlier installed source `f71eb7d` in Unity, not a new iPhone recording.
This candidate needs its own approved device media before store preparation.
Copy and artwork checks do not certify exclusive rights or resolve Apple's
unidentified similarity allegation. Public review remains on hold.
