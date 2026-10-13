# SkyPulse flight-deck interface update

Working branch: `codex/neon-visual-update`. Starting point for this pass: `676c8b0` (`Fix Bird Hangar profile lookup`).

## Why this pass exists

The Hangar's unlock/equipped badges worked, but the screen still looked like a collection of generic boxes. This pass gives the interface a visual language based on the mechanical birds: swept wings, illuminated docking rings, cut-corner controls and connected upgrade circuits. The bird artwork remains the centre of attention.

## What to look for

- Home: SkyPulse Arcade title, hero bird in its flight dock, clear primary Play control and personal best.
- Hangar: larger birds, individual docking bays, readable names and separate equipped/owned/locked states.
- Upgrades: connected progression paths using the game's existing upgrades, levels and crystal prices.
- Flight: score and pause controls kept clear of the flight corridor.
- Purchase and unlock: matching acquisition dock and a more distinctive arrival reveal.

The wing marks and circuit shapes are generated directly by Unity's UI mesh. They need no additional downloaded art or full-screen post-processing. Their optional breathing light respects Reduced Motion.

## Open the current work

1. In Unity Hub, open `/Users/mcauleemaddison/Desktop/SkyPulse/mobile` using Unity **6000.6.0f1**.
2. Open **Assets → Scenes → SkyPulse** and wait for compilation.
3. Press Unity's **Play** triangle and select a portrait Game view.
4. Open **Bird Hangar**. Check the equipped bird, an owned bird, and a locked bird. Swipe down through the complete roster.
5. Open **Upgrades**. Scroll through the branches and inspect an upgrade. Check its existing price, next effect and prerequisite.
6. Start a run, use Pause, change Reduced Motion, resume, and check the results after an impact.
7. Stop Unity Play mode with the same triangle when finished.

Do not assume an existing Xcode export includes these changes: Unity exports are snapshots. The next physical-phone test requires a fresh Unity development export.

## Editable files

- `Assets/Scripts/SkyPulseNativeGame.cs`: screen composition, labels, selection state and shared controls.
- `Assets/Scripts/SkyPulseUiGlyph.cs`: wing insignia, docking rings, circuit hexagons and horizon marks.
- `Tools/SkyPulseInterfaceCapture.cs`: isolated editor capture/interaction harness, outside shipping Assets.

The previous pipe/crystal/background work and provenance remain documented in `Release/NeonVisualUpdate/`.

## Verification and continuation

Implementation and editor verification are in progress. Final results will be recorded here before this pass is handed back.

This is local development work for a future update. The previously uploaded App Store build 5 is a separate archive. This interface pass has not been exported to a physical iPhone or submitted to Apple.
