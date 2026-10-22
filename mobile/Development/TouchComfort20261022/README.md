# Small-screen touch comfort — 22 October 2026

Runtime source: `f71eb7d8014fd6e7386811b9d60a88ad1ab6bddc` on
`codex/dimensional-flight-20261022`. This follows the dimensional-flight upgrade.
Main and the App Store submission remain unchanged.

## Implemented

- Endless and Relay pause icons retain their small visible face, with a larger
  invisible tap area. Both fit inside the safe region; Relay's pause sits to the
  right of the readable gate counters.
- The Home mode badge has the same generous touch area. Its dropdown has three
  taller, separated choices with larger names/descriptions and no extra footer.
- The shared Pause screen gives Resume, Reduced Motion, Haptics, Sound and Return
  to Menu separate, generous rows. All three modes use it.
- No new images, assets, economy changes or gameplay rules. Gate collisions,
  flight width/spacing/forces, crystal accounting, puzzle progression and player
  save formats retain the protected implementation.

## Verified

- Unity batch run exit 0; **21 pass groups**, including the new actual-Canvas
  raycast checks. See `QA_RECORD.json` for exact source hashes and markers.
- Small 640×1136 @2× phone, notched 1320×2868 @3× phone and 1640×2360 @2× tablet
  layouts. Tested badge, mode choices, flight pause and shared Pause actions are
  at least 44 pt in each profile (about 49 pt on the smallest profile).
- Expanded pause areas reach the actual Button outside the visible icon. Relay
  refuses to flap there; the open flight area still accepts gameplay input.
  Mode choices and Pause rows have disjoint hit areas and remain within safe
  bounds. Labels fit. Actual Resume actions work in all three modes.
- Existing regression groups cover same-frame Relay collection/unlock/impact,
  touch/mouse deduplication, instant/once-only puzzle progression, retries,
  rewards/save recovery, Core/training/breeding and all twelve world transitions
  at 30/60/120 FPS. Those frame-step checks are not physical-device FPS results.
- Protection comparison passes, including 235 unique Unity GUIDs, unchanged
  gameplay methods, scenes and original five audio assets. Both generated Heist
  cues reproduce their recorded hashes. Eleven metadata-copy tests pass.
- Fresh local iOS export succeeds; all **453 source-manifest entries** match the
  repository. Version 1.0.0 / local build 18. No Apple upload or submission.

Two historical test assertions still expected the previous 496-unit menu height;
they now require the new full 688-unit layout, including Reduced Motion. The final
run has no C# compilation failures or new runtime exceptions. The previously
observed Unity Editor Search indexing `ArgumentOutOfRangeException` remains in
the Editor log and is not being reported as a gameplay pass.

## Preview and device acceptance

`pause-touch-comfort.png` and `mode-list-touch-comfort.png` are actual Unity
renders, **not iPhone recordings or current App Store media**.

Device gameplay and sustained performance are still pending. Check the local
candidate on the iPhone: all three mode Homes, tap/flap, automatic gates, missed
crystals/ground failure, Blackout moves/planning/progression, Pause preferences,
resume/retry/menu and saved progress after closing/reopening. Listen on the
speaker and headphones. A successful install/launch alone does not verify these.

Keep public submission on hold. Apple's private similarity concern cannot be
proven cleared by local metadata checks. Replace historical App Store screenshots
and recordings with genuine final-candidate device media before any future
submission.

## Editing

`SkyPulseNativeGame.Controls.cs` owns `SetComfortHitTarget`: this changes Unity's
raycast padding, not the visual rectangle. Apply it only to isolated controls;
dense rows need explicit layout spacing to avoid ambiguous touches.
`SkyPulseNativeGame.Presentation.cs` owns the mode-list dimensions;
`SkyPulseNativeGame.cs` owns shared Pause layout;
`SkyPulseNativeGame.Missions.cs` owns Relay's top-right pause position.

Run `mobile/Tools/run_dimensional_qa.py` with a new output folder to reproduce the
full suite in a separate QA product. `SkyPulseTouchComfortChecks.cs` is Editor-only
test tooling outside production Assets. The owner's save is never cleared.
