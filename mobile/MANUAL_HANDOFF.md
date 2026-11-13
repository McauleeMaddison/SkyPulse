# SkyPulse manual development handoff

Repository: https://github.com/McauleeMaddison/SkyPulse — branch `main`.

Open **`mobile/` in Unity Hub** and the repository folder in VS Code. The shipping
scene is `mobile/Assets/Scenes/SkyPulse.unity`. Start with the
[editing map](EDITING_GUIDE.md) before changing code.

## Build 21 update — 13 November 2026

[Build 21](Release/DeviceBuild21/README.md) is installed and launched on the paired
iPhone. It carries the owner-requested Voltcrest
Hangar wing profile, three additional permanent upgrade families and two automatic
Endless pickups. See its QA record and refreshed weekend checklist. All 41 saved preference values survived installation and launch unchanged. Use its
[weekend checklist](Release/DeviceBuild21/DEVICE_CHECKLIST.md); manual acceptance remains pending.

## Historical installed Build 20 — 13 November 2026

[Build 20](Release/DeviceBuild20/README.md) is installed and launched with the latest visuals for the
owner's Saturday/Sunday manual test. Native Home, Hangar and flight now load the
combined fifteen-bird roster; the old Editor overlay yields to that presenter.
Accepted taps have a full physical upstroke/downstroke, roughly -27° to +28° in
30/60/120 Hz tests. No lift, spacing, collider, catalogue or economy changes.
The full regression and 261 native-bird assertions passed; device acceptance
is still pending. Check RELEASE_RECORD.json for actual installation evidence
and use the refreshed DEVICE_CHECKLIST.md rather than the old Build 18 checklist.
No public submission is authorized.

## Previous local candidate — 11 November 2026

The [current repair record](Development/EverydayPolish20261111/README.md) contains
the corrected distal feather fans, actual tap-edge strokes, stale reviewer/equip
repair, refined Hangar cradle, first-play guidance, failure/retry fixes and five
pooled 3D pickup housings. Its animated Chromaflux preview is an actual Unity render.
The owner project has been refreshed and the Studio is open on Chromaflux.
770 combined assertion lines and the separate 202-assertion pickup regression pass;
1,800 sampled wing poses report no wing/foot surface crossings. The audit is finite
and does not replace visual review or device tests. Solid birds remain Editor-only.
No phone build, installation, TestFlight upload or public submission occurred.

## Previous local candidate — 10 November 2026

Use the [current polish record](Development/PolishPass20261110/README.md) for
tap-driven solid wings, selected-bird crash identity, three shoulder corrections,
Editor Home/Hangar solids, world lighting and progression/mode feedback.
The previous continuous wing-clock experiment below is historical; gameplay
now follows accepted native tap strokes. Visual sign-off remains the next step.
No new device build, installation, TestFlight upload or review submission.

## Latest local repair — 10 November 2026

The [wing-flap repair](Development/FlightAnimation20261110/README.md) reproduces and
fixes tap-triggered starvation in the Editor-only solid bird preview. It retains
an essential stroke in Reduced Motion, holds pause poses and resets per run.
The source-specific actual-gameplay regression passed 993 checks. No saved art or
shipping flight forces changed. Build 19 and the owner visual-approval hold remain.

## Latest local work — 9 November 2026

Start with the [current polish record](Development/GameplayPolish20261109/README.md)
for new Endless pickups, exact rewards, changed files and source-specific checks.
The [bird editing map](Development/BirdRoster20261104/README.md) points to the
fifteen saved rigs and the twelve-world candidate. Finch is approved; other
roster/world appearance still needs owner review. Chromaflux and Virex received a
pair-only identity refinement after their shapes looked too similar. No runtime
3D roster rollout, iPhone build, installation or TestFlight upload occurred.

MacWebStudio attribution and current product information were saved in App Store
Connect; the branded privacy/support/player pages are published. See the
[public information record](Release/StoreListing20261109/README.md). The saved
version still selects historical build 15, while the last recorded installed
phone build is 19. Media and the matching final build require renewal before any
separately authorized submission. Public review remains on hold.

The older packages below are historical evidence for their own sources.

## Current local design candidate — 31 October 2026

The [DesignUpgrade20261031 package](Development/DesignUpgrade20261031/README.md)
records the current local previews, source manifests and QA scope. This source is
newer than installed iPhone build 19 and has not been installed or uploaded.
World/district and Relay presentation, Home labels, Hangar/Tech Tree finishes and
native tactile feedback work need acceptance on a newly signed device candidate.
Editor captures are not installed-phone footage; use each dated record for the
exact source and checks it covers.

Gameplay retains the original articulated bird artwork. The separate editable
Neon Finch workshop is an isolated art-review prototype. **The Finch appearance was subsequently approved in chat; the new 4 November
roster package records the additional fourteen models.** The latest
2 November revision refreshes existing sculpture buffers 001–125 and eight material
profiles: source-shaped round optics, helmet, swept wings, ivory shield and tail
exhaust. Bones, joints, prefab, scene, clips, bindings, lighting and all GUIDs stay
intact. The fixed source-pose comparison uses equal framing and scale; head/optic/
beak/shoulder bounds pass measured source tolerances. Pixel-identical appearance
and physical-device acceptance remain unproven. The additional roster art remains under review and
uninstalled. See the opening scene map in the editing guide.

Pipe collars use a real hollow mouth and separately editable structural parts;
see [the Unity pipe editing steps](EDITING_GUIDE.md#edit-the-actual-3d-pipes-in-unity).
Use [the guide's opening scene map](EDITING_GUIDE.md#open-the-right-scene) for the
worlds, distant atmosphere, Relay, pipes and Finch. Gameplay starts in the Editor
from `Assets/Scenes/SkyPulse.unity`; workshop Play remains model inspection.

The Editor stops Play before C# recompilation. Wait for compilation, then start
a fresh Play session. No owner save reset or App Store action is part of this work.

## Historical installed iPhone build 19

- Runtime source: `f6221f68314a864b898a7c7bbe9693bb528316e2`; Unity 6000.6.0f1.
- **SkyPulse 1.0.0 (19) is installed and launched on the paired iPhone.** It replaced the phone's Build 17 directly; TestFlight was not uploaded.
- The twelve-world/Relay model-detail pass and raised Blackout board are included.
- Full automated gameplay/world validation and signed native compilation passed. Physical visual/gameplay acceptance, sustained FPS, memory, thermals and retained-progress confirmation remain pending with the owner.
- [Current device checklist](Release/DeviceBuild19/DEVICE_CHECKLIST.md), [build/installation record](Release/DeviceBuild19/RELEASE_RECORD.json), [QA](Release/DeviceBuild19/QA_RECORD.json) and [release contract](Release/CURRENT_RELEASE.json) identify the actual candidate. Older release folders are historical.
- **Public App Review stays on hold.** The 9 November listing update saved copy only; no public submission or build replacement was made.

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

The installed-build metadata lives at the path in `Release/CURRENT_RELEASE.json`.
Its export/QA hashes describe build 21; newer source needs its own renewed checks.
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

Next visual acceptance: Finch is approved. Review the corrected Chrome Raven
against its original artwork before applying that approach to the other thirteen;
physical-device checks for the wider design candidate remain pending.

The subsequent local Finch 3D pass is documented in
[3D structure checks](Development/DesignUpgrade20261031/Checks/Finch-Structure.json)
and the [360° turntable](Development/DesignUpgrade20261031/Previews/Finch-3D/Turntable.mp4).
It retains the reference-facing geometry while closing the case and supporting
the optical, wing and foot assemblies. Acceptance remains pending. Prior reference
preview files are preserved; the structural proofs are in their own Finch-3D folder.
The installed app remains Build 19; no device install, upload or submission occurred.

The latest Finch optics/exhaust pass is in
[Finch-Eyes-Jet](Development/DesignUpgrade20261031/Previews/Finch-Eyes-Jet/Overview.png).
Eyes are smaller and supported by closed helmet pockets; a dedicated 1,102-vertex
jet replaces the static plume. [Actual GPU checks](Development/DesignUpgrade20261031/Checks/Finch-Eyes-Jet-GPU.json)
verify fixed-camera flow, deterministic repeat-time rendering and static Reduced
Motion. The existing eight-joint flight formulas, gameplay and other roster birds
are preserved. The art rig is the only changed runtime-source file relative to the
prior 40-group suite; its new motion passed focused tests. Visual review, device
profiling and gameplay integration remain pending. No new installed build.
