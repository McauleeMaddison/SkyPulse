# SkyPulse: where we left off

Checkpoint: first visual update, 9 September 2026.

**Current work is on `codex/neon-visual-update`.** The starting commit is `347f3f9` (`App Store review info`). The archived/uploaded build 5 is separate; this update has not been uploaded or submitted to Apple. You reported sending Apple the requested answers and iPhone recording.

## What this update contains

- Reflective dark-metal pipe shafts, cyan light strips, magenta power channels and open glowing collars. Their visible size fits the original collision boundaries.
- A translucent cyan/violet faceted crystal, matching wallet icons, and a brief ring with small sparkles when collected.
- New Neon City, Aurora Rise and Solar Drift skies with brighter edge-lit clouds, floating cities and a dark centre for readable flight. These are the three environments used by the main route.
- Reduced Motion stops the decorative crystal bob, rotation and glow pulse, freezes pipe light animation, and uses a short stationary collection acknowledgement.

Bird handling, route difficulty, gate dimensions, currency values, unlock prices and saved progress have not been changed by this pass. Your Hangar/Upgrades reference images are saved for the next interface pass; their extra currencies and example abilities have not been added.

## Open and play it yourself

1. Open **Unity Hub**. Select the project at `/Users/mcauleemaddison/Desktop/SkyPulse/mobile`. If it is not listed, use **Add project from disk** and choose that folder.
2. Open it with **Unity 6000.6.0f1**, the project's current version.
3. In Unity's Project panel, open **Assets → Scenes → SkyPulse**.
4. Wait for asset importing/compilation to finish, then press Unity's **Play** triangle.
5. Select the **Game** view in portrait. Tap the game's **Play** button; click in the flight area or press Space to flap. Use the game's top-left pause control to test Reduced Motion.
6. Stop Unity Play mode using the same triangle before making edits.

If the Game view still shows the old art, stop/restart Play mode and check that the project folder and branch match the ones above. Existing Xcode exports are build snapshots: they will continue showing the old art until a fresh Unity export is made.

## Review the actual rendered change

Open [the before/after viewer](/Users/mcauleemaddison/Desktop/SkyPulse/artifacts/neon-visual-qa/review.html), or view the [updated Neon City gameplay capture](/Users/mcauleemaddison/Desktop/SkyPulse/artifacts/neon-visual-qa/updated/world-0-normal.png).

These are actual Unity camera renders at 660×1434 using a deterministic QA scene. The bird, gate and pickup positions are deliberately fixed for comparison. They are development evidence, not footage or marketing screenshots from your iPhone.

## Where the editable work lives

- `Assets/Scripts/SkyPulseNativeGame.cs`: pipe rendering, crystal presentation/effects, and world artwork paths.
- `Assets/Editor/SkyPulseMobileArtSetup.cs`: texture import budgets. **SkyPulse → Optimise Mobile Art** retains 2048-pixel backgrounds, 1024-pixel pipes and 512-pixel pickups.
- `Assets/Resources/SkyPulse/art/pipes/`: new shaft and collar PNGs.
- `Assets/Resources/SkyPulse/art/powerups/generated/crystal-prism-neon-v4.png`: new crystal PNG.
- `Assets/Resources/SkyPulse/backgrounds/neon-flightdeck-v2.png` and `backgrounds/themes/{aurora-rise-v3,solar-drift-v3}.png`: new route backgrounds.
- `Release/NeonVisualUpdate/references/`: your three target images. The provenance notes in this folder describe the generated art and import settings.

The earlier artwork is retained under its existing filenames, making it easy to compare or revert the selected resource paths.

## Verified locally

The isolated QA copy uses its own company/product name so the tests do not touch your real saved game.

- Unity graphics-enabled compilation and eleven actual camera renders.
- Exact pipe collider comparison with the pre-update version, plus collision samples immediately inside and outside each gap edge.
- Crystal collection banks currency; effects expire and clear on restart, menu return and world transition.
- Reduced Motion keeps the new decorative crystal effects stationary.
- Existing 1,300-gate beta checks and visual-behaviour smoke checks: scoring, progression, pause/resume, saved rewards, purchase guards, world transitions and menu interaction.

Logs and captures are under `/Users/mcauleemaddison/Desktop/SkyPulse/artifacts/neon-visual-qa/`. `Tools/SkyPulseNeonVisualCapture.cs` is a QA helper and is intentionally outside shipping Assets.

## Next step

**Play this visual preview in Unity, then make a fresh iPhone development build for an on-device playtest.** Check small-screen readability, collection feedback, world transitions, smoothness and heat during a sustained run. This new visual update has not yet had that physical-device pass.

After that, the Hangar/Upgrades layout can be refined toward your other reference images. A future release still needs its own version/build number, archive and testing. Keep the currently submitted build 5 available while Apple finishes its review.

To resume with Codex, say: “Continue SkyPulse on codex/neon-visual-update. Read mobile/Release/NeonVisualUpdate/START_HERE.md. The next step is the iPhone visual playtest.”
