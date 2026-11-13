# Dimensional flight upgrade — 22 October 2026

Follow-up: [small-screen touch comfort and local device candidate](../TouchComfort20261022/README.md).
The source-specific evidence below remains a record of the earlier runtime commit.

For the current installed candidate's Relay and Signal Heist, use the
[dated Build 18 reference](../PhoneReference20261022/README.md). The 12 October
prototype preview with FLAP/PULSE controls is historical and has been archived.

Development branch: `codex/dimensional-flight-20261022`.
Runtime implementation: `8d0dce7bc31c87da7c9600fdd0f544d1c0436d1e`.
Protected starting/main commit: `a343aef23503603e85d0018e663d0d3ebf4b616a`.

This is a local development candidate, retaining build number 18. It is not a
new TestFlight release or App Review submission. Main and the canonical release
records were left intact. Earlier phone build 18 was installed from the preceding
source before this new brief; it does **not** contain this branch's final scenery
and audio changes. Do not treat it or old recordings as evidence of this source.

## What was already working

The existing orthographic flight, tap input, original LED pipes, 3D hoop geometry,
front/rear bird occlusion, numbered crystals and automatic unlock/collision order
were retained. Signal Heist, the four-task daily economy, founder catalogue,
training, breeding, Core evolution, Tech Tree, saves and five flight sounds remain.
The source comparison and Unity tests verify these specific claims.

## Missing pieces and changes

Endless had dimensional pipes and home stages, but no shared system for real
in-flight architecture across all twelve world themes. It now has twelve authored
procedural mesh signatures, distributed through three depth layers in two reusable
18-piece banks. Each world has its own silhouette, layout seed, light and fog
palette. Scale, yaw and spacing vary between instances. Bevelled spires, suspended
forms and curved structures complement the existing distant world artwork.

The scenery uses the **existing flight camera**, at z = -10, orthographic, with
its existing clipping range and culling mask. Mesh instances occupy roughly
z = 6–14, within that range, and sorting orders -27/-22/-17, ahead of the distant
backdrop and behind gameplay. Actual pixel comparisons confirmed geometry is
visible in all twelve themes. No additional camera, RenderTexture, Rigidbody or
collider is introduced for active scenery. The home-stage cameras remain separate.

Parallax observes the actual first pipe's travel; recycled anchors cannot teleport
the scenery. Thus slow fields, hit-stop and pauses retain their existing timing.
Two preallocated banks use the existing world blend. A short scenery entrance
fade blocks no input. Reduced Motion freezes drift, travel and animated light.
Owned meshes and materials are explicitly released. Warmed scenery updates create
no measured managed allocations in the Editor checks.

Relay keeps its original hollow titanium hoops and existing 3D machinery system.
That system now uses shared curved structural ribs at the view's edges, and its
far turbines wrap within their scenery span. The first render review caught ribs
crossing the route too prominently; the final version frames the edges instead.
Aperture, bird hitboxes, charge rules and passage timing were not edited. Existing
charge halves, red-to-blue lasers, lock retraction, pooled passage ripple, authored
bird flaps, trail and completion/failure effects remain responsive.

Relay Home has one launch action, one row for Circuit Map / Your Bird / How to
Play, and a quiet Mission Control / Privacy footer. Collection counts, the wallet,
duplicate pilot text, large deck borders and the 2D radar overlay were removed
from this Home. Darkened station art and thin translucent holographic controls
improve contrast. Switching modes restores the previous Endless layout. The flight
HUD is shallower and omits its redundant circuit-name line; charge, opened gates,
passed gates, the next objective and pause remain visible at the safe-area top.

The existing six-channel sound pool and saved sound switch are retained. Two
missing clips, `prism.wav` and `sensor.wav`, now provide a short optical detent and
restrained double warning for Heist. They use deterministic mathematical synthesis
with no recordings, sample packs or external service. Missing optional clips fall
back to score/crash. All audio channels, including the Relay passage channel, are
prepared at startup. Passage playback is bounded, and loading the saved sound
preference updates the master mute immediately. Pause, restart, mute and focus loss
stop tails. The five working effects are byte-for-byte unchanged.

## Files

| File under `mobile/` | Purpose |
|---|---|
| `Assets/Scripts/SkyPulseFlightEnvironment.cs` (new) | Twelve signatures, pooled banks, parallax, blending and cleanup |
| `Assets/Scripts/SkyPulseNativeGame.Environment.cs` (new) | Observe authoritative flight and connect scenery to the existing coordinator |
| `Assets/Resources/SkyPulse/Shaders/FlightArchitecture.shader` (new) | Mobile single-pass shaded metal, inset light, atmospheric depth and coverage fade |
| `Assets/Scripts/SkyPulseNativeGame.cs` | Presentation-only run reset and applying the already saved audio preference |
| `Assets/Scripts/SkyPulseNativeGame.Cinematic.cs` | Integrate the active scenery; reuse the prewarmed passage audio channel |
| `Assets/Scripts/SkyPulseRelayEnvironment.cs` | Curved edge structures and recycler wrapping |
| `Assets/Scripts/SkyPulseNativeGame.Controls.cs` | Calm Relay Home; restore other mode layouts |
| `Assets/Scripts/SkyPulseNativeGame.Missions.cs` | Relay Home contrast and compact HUD |
| `Assets/Scripts/SkyPulseCircuitGraphic.cs` | Opt-in holographic control finish |
| `Assets/Scripts/SkyPulseNativeGame.Audio.cs` | Optional cue fallback, prewarm passage audio |
| `Assets/Resources/SkyPulse/audio/prism.wav`, `sensor.wav` (new) | Dedicated Heist cues, with new Unity import metadata |
| `Tools/SkyPulseDimensionalChecks.cs` (new) | Runtime, render, audio, cleanup and layout checks plus existing regressions |
| `Tools/run_dimensional_qa.py` (new) | Reproduce the check suite in a new disposable Unity product |
| `Tools/check_dimensional_preservation.py` (new) | Compare protected rules, scene, five sounds and Unity GUIDs against a343aef |
| `Tools/generate_heist_audio.py` (new) | Reproduce/verify the two new sounds |
| `EDITING_GUIDE.md` | Add the new editing locations |

The original `SkyPulseNativeGame` remains the single scene coordinator. Its partial
files are not separate game components and should not be attached individually.
No scene or working asset was deleted.

## Executed verification

The exact-source iOS IL2CPP export passed with all **453** source entries matching.
Xcode then completed an **unsigned Release build for generic iOS** with exit 0.
Neither step signed, installed or uploaded this final branch.

See `QA_RECORD.json` for hashes, pass markers, exact source and native build results.
The final Unity process exited 0 with all required markers. Checks include:

- Real rendered geometry in all 12 worlds; 36 original transition sequences at
  30/60/120 FPS; deterministic parallax, no new colliders/camera, depth ordering,
  reduced motion, pause, retry and owned-resource destruction.
- Original camera, flight physics, input, pipe spacing, hitboxes, collision and
  hoop rules preserved by method/file comparisons. The existing real-flap pilot
  completed circuits and exercised crystal collection, same-frame automatic
  unlock and front/rear hoop occlusion.
- One-thumb input/UI separation, held/queued touch fences, duplicate completion,
  immediate and final puzzle progression, failure/retry and saved configuration.
- Four daily tasks, five-crystal rewards, duplicate claim prevention, write-fault
  recovery, daily circuit refresh and end-of-run Main Menu/Next Mission.
- Actual Core, training and breeding prices, inheritance, unlock savings and
  persistence. The pre-existing owner-requested 2% training saving (cap 20%) and
  Core discounts were **not** introduced or altered in this branch.
- All seven clips imported and decoded with finite samples and measured headroom;
  bounded source count under rapid requests, mute persistence, pause/focus loss,
  fallback and restart. Existing five WAVs also match their retained generator.
- Guide text/navigation layouts at 640×1136, 1320×2868 and 1640×2360; all three
  Homes and gameplay captured from Unity. No C# compilation errors in the pass.

Two initial fixture failures were corrected: the reused QA product's old puzzle
attempt needed clearing, and the legacy-skin migration check intentionally left
bird 10 equipped before a test expecting Neon Finch. No economy fix was needed.
The final log also contains the previously observed Unity **Editor Search indexing**
`ArgumentOutOfRangeException`; this is recorded separately from gameplay pass
markers, not described as a runtime failure or an entirely error-free Editor.

## Preview provenance and remaining checks

`relay-home.png`, `relay-flight.png`, `endless-depth.png` and `relay-motion.mp4`
are actual Unity renders of the recorded source, **not iPhone footage**. The movie
contains 420 frames / 14 seconds / 30 FPS at 750×1334, and was fully decode-checked.
It uses the existing test pilot, original physics and collision-checked gates.
`audio-check.wav` is an audition sequence, not a gameplay recording: flap, score,
crystal, crash, unlock, prism, sensor, with pauses between cues at the gameplay gain.

Physical iPhone/iPad gameplay, speaker/headphone listening, motion comfort,
sustained GPU performance, memory/thermal behaviour and background/reopen checks
remain unverified for this final branch. No measured 60 FPS claim is made. The
local native export/build is compilation evidence, not a signed/device-tested
release. No final branch build was installed, uploaded or submitted.

App Store Connect still has the earlier saved draft text, historical build 15 and
build 7 media; this branch did not alter or submit them. Those selected media must
be replaced with genuine final-candidate device evidence before any future
submission. Existing videos were not renamed to pretend they are current.

Unity `.meta` files retain GUIDs and import settings; deleting them can break asset
references. Release JSON files hold listing copy and build/check records. Neither
file type is proof of Apple's alleged similarity. Local checks verify identity,
copy limits and unique Unity GUIDs, not global exclusivity or Apple's private
comparator. The branded support/privacy/guide wording remains accurate to the
preserved progression; no website was republished in this branch.

Automatic approval review rejected enabling the Mac-wide capture-device setting.
It was not changed. Use the iPhone's built-in recorder for later device footage.

## Open and test manually

1. In VS Code, verify the branch is `codex/dimensional-flight-20261022`. It is
   already checked out in this workspace. Keep main unchanged.
2. Open `/Users/mcauleemaddison/Desktop/SkyPulse/mobile` in Unity **6000.6.0f1**.
3. Open `Assets/Scenes/SkyPulse.unity`. Set Game view to portrait 9:16, then Play.
4. In Endless, check flaps, original pipe joins/trail, layered scenery and world
   changes every 15 gates. Check pause/retry and Reduced Motion.
5. Use the top-left mode badge for Relay. Check the cleaner Home, its three
   secondary actions, two crystals per numbered gate, red-to-blue unlock and
   actual hoop passage. Check missed crystals and ground failure too.
6. In Blackout, compare prism, core-power, sensor and result sounds. In Pause,
   toggle SOUND off, resume/retry and reopen Play to verify persistence. Confirm
   Mission Control still grants each eligible five-crystal reward only once.
7. Listen at comfortable device volume and profile a later authorized device build
   before treating these previews as final release evidence.

Reproduce automated checks from the repository root:

```sh
python3 mobile/Tools/run_dimensional_qa.py --output /private/tmp/skypulse-dimensional-check-new
python3 mobile/Tools/check_dimensional_preservation.py
python3 mobile/Tools/generate_heist_audio.py --check mobile/Assets/Resources/SkyPulse/audio
python3 mobile/Tools/generate_skypulse_audio.py --check mobile/Assets/Resources/SkyPulse/audio
```

The output folder for the runner must be new. It copies the project, uses a separate
QA product name/save and leaves the owner's open Unity project untouched.
