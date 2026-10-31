# SkyPulse manual development handoff

Repository: https://github.com/McauleeMaddison/SkyPulse — branch `main`.

Open **`mobile/` in Unity Hub** and the repository folder in VS Code. The shipping
scene is `mobile/Assets/Scenes/SkyPulse.unity`. Start with the
[editing map](EDITING_GUIDE.md) before changing code.

## Local visual repairs after Build 19 — 30–31 October 2026

The source includes the [world/Home/Hangar/Tech Tree and bird-shape pass](Development/VisualPolish20261030/README.md), followed by visual regression repairs. **These changes are newer than the installed iPhone Build 19**; a new signed device candidate and owner acceptance are still required. Do not describe Editor previews as installed-phone footage. Refer to the dated QA record for the exact source that each capture and check covers.

The original articulated bird artwork is restored; the rejected sculpted replacement has been removed. Pipe collars use a real hollow mouth and separately editable structural parts; see [the Unity pipe editing steps](EDITING_GUIDE.md#edit-the-actual-3d-pipes-in-unity). The game starts in the Editor only from `Assets/Scenes/SkyPulse.unity`. Workshop Play remains an inspection session, preventing its static pipe and camera from appearing in actual gameplay.

The Editor safely stops Play before C# recompilation. Wait for compilation, then start a fresh Play session. No owner save reset or App Store action is part of this pass.

## Current state — local Build 19

- Runtime source: `f6221f68314a864b898a7c7bbe9693bb528316e2`; Unity 6000.6.0f1.
- **SkyPulse 1.0.0 (19) is installed and launched on the paired iPhone.** It replaced the phone's Build 17 directly; TestFlight was not uploaded.
- The twelve-world/Relay model-detail pass and raised Blackout board are included.
- Full automated gameplay/world validation and signed native compilation passed. Physical visual/gameplay acceptance, sustained FPS, memory, thermals and retained-progress confirmation remain pending with the owner.
- [Current device checklist](Release/DeviceBuild19/DEVICE_CHECKLIST.md), [build/installation record](Release/DeviceBuild19/RELEASE_RECORD.json), [QA](Release/DeviceBuild19/QA_RECORD.json) and [release contract](Release/CURRENT_RELEASE.json) identify the actual candidate. Older release folders are historical.
- **Public App Review stays on hold.** No App Store Connect copy/media/build selection or submission was changed in this session.

## Preserve while editing

The `SkyPulseNativeGame.*.cs` files extend one partial game class. Do not add a
second game component or a parallel implementation. Keep the existing script
GUID, `.meta` files, catalogue IDs, company/bundle identity and save keys.

Keep the tested flight width, handling, pipe spacing and collision dimensions.
The visible shaft seats inside its modelled collar; physical collision shapes
intentionally overlap. Relay crystals open their matching gate automatically before collision
resolution. Blackout's intermediate puzzles activate immediately without a
Continue button. Preserve those behaviours, input/UI separation, economy,
once-only rewards and saved progress when changing graphics.

Core progression, bird training and breeding already have gameplay benefits.
Training contributes 2% off future founder-bird unlocks per completed level,
capped at 20%. Breeding creates a permanent offspring with previewed inherited
traits and retains its parents. Read the authoritative pricing and transaction
functions in the editing guide before changing costs or rewards.

The current prepared metadata lives at the path in `CURRENT_RELEASE.json`.
Changing code makes its recorded export/QA hashes stale until renewed checks.
Local metadata checks cannot certify global uniqueness or settle Apple's
unidentified similarity allegation. Keep original art/generation evidence;
generated artwork is not evidence of exclusive rights by itself.

## Copy this prompt to continue with the repository

```text
Help me continue SkyPulse Arcade, my existing Unity mobile game, manually in
Unity and VS Code. Work from the actual supplied SkyPulse main repository.
If you cannot read it, ask for the relevant files before assessing or rewriting
features. Do not infer current code from screenshots or old release folders.

First read README.md, mobile/MANUAL_HANDOFF.md, mobile/EDITING_GUIDE.md and
mobile/Release/CURRENT_RELEASE.json. Follow the current contract to its QA and
release records. Inspect the gameplay, UI, effects and shaders, including every
SkyPulseNativeGame partial. Identify the Unity version from ProjectSettings.

Write a full, prioritised game checklist in these categories:
1. Project setup, compilation and reproducible builds.
2. All three mode Homes, menus, safe areas and one-thumb input.
3. Endless Route: wing animation, movement, pipes/LEDs/joins, trail, collisions,
   score, worlds, pickups, pause, results and retry.
4. Pulse Relay: daily circuits, reachable crystals, automatic gate unlocking,
   same-frame pickup/collision, 3D hoop depth, results and next mission.
5. Blackout Protocol / Signal Heist: readable 3D board, meaningful objectives,
   rotation and beam/cost previews, trace/failure/retry, instant progression,
   stale-input protection and final-vault completion.
6. Tech Tree, Core levels, bird unlocking/training/breeding, exact costs,
   discounts, inherited traits and once-only transactions.
7. Saves, update compatibility, interrupted writes and retained settings.
8. Materials, lighting, animation, audio, haptics and Reduced Motion.
9. 30/60/120 FPS behaviour, allocation checks, device frame times, memory,
   thermals, launch/background/resume and iPhone/iPad layouts.
10. Artwork evidence, app identity, privacy/support links, current accurate
    metadata/screenshots/video, archive/symbols and release readiness.

For each checklist item give: priority, actual implementation file/method,
current evidence, a concrete manual test and a pass condition. Distinguish
implemented code, automated verification, physical-device verification,
unverified behaviour, proposed improvements and external blockers. Leave
untested items unchecked. Do not propose rebuilding features that already work.

Then choose the three highest-value improvements. For the first one, explain
the player benefit and give precise edits, with the filename and method to
replace, so I can apply them manually. Make one small change at a time and
explain how to test it before moving on. Preserve the existing controls,
collision geometry, economy and save compatibility unless I approve a change.
Respect reduced-motion/haptic settings and keep mobile effects allocation-light.

Do not upload, publish or submit to App Review. Do not promise exclusive AI-art
rights, global metadata uniqueness, a bug-free game or Apple acceptance.
```

## First manual checks after an edit

1. Save in VS Code, allow Unity to compile, and resolve Console errors.
2. Play the affected mode in portrait; check motion, readability and tap targets.
3. Check pause/resume, failure/retry, Reduced Motion and reopening saved progress.
4. Check affected prices/rewards with insufficient and exact crystal balances;
   confirm repeated taps cannot spend or reward twice.
5. Run the applicable isolated checks described in `EDITING_GUIDE.md`. QA reset
   helpers belong in the disposable project, never in the real player save.
6. Test the new candidate on physical devices before treating it as release-ready.

Suggested next visual work: improve bird/background separation and give the exact
collision location a restrained impact cue. These remain proposals.
