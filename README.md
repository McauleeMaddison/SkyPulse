# SkyPulse

SkyPulse is a portrait-first Unity 6 one-tap cyberpunk flyer. It keeps the readable Flappy Bird rhythm—tap, judge momentum, clear a paired gate—but makes a successful run feel like a continuous flight through three transforming worlds.

The Unity project in [`mobile/`](/Users/user/Desktop/SkyPulse/mobile) is the shipping source of truth. The older web experiment is kept separately and is not the native game's implementation target.

## Game contract

The game has one fair endless route. Birds are cosmetic and permanent upgrades affect crystal collection only: no bird, item, or purchase changes flap physics, gate score, or a player's scoring potential.

- **Controls:** tap/click in the playfield, Space, or Up Arrow flaps. The first flap starts the run; inputs use a short duplicate-touch lockout. Escape, P, and the top-left pause control pause the run.
- **Camera:** the playable world remains a fixed 9:16 portrait side view. On desktop, it is centred while the active world decorates the side margins; gameplay geometry is never widened for landscape.
- **Flight:** shared gravity, flap impulse, terminal fall speed, collision ellipse, and bird dimensions. The top boundary clamps ascent; the lower hazard and gates are fatal unless Aegis is active.
- **Score:** each fully passed gate is exactly one point. Crystals are separate, persistent currency and bank the moment they are collected—even on a failed run.
- **Route:** Neon City runs from 0–14, Acid Foundry starts at score 15, and Orbital Bazaar at score 30. From score 45 onward the worlds rotate in harder remixes every 15 gates, capped at the intended speed and minimum opening.

## World pacing

| Score range | World | Gate behaviour | Opening / speed |
| --- | --- | --- | --- |
| 0–4 | Neon City tutorial | Three generous, static gates | 34% of height / 32% of width per second |
| 5–14 | Neon City | Standard static neon towers | 31% / 36% |
| 15–29 | Acid Foundry | 1.2 s chromatic tunnel, a recovery beat, then telegraphed vertical drift | 29% / 40% |
| 30–44 | Orbital Bazaar | Alternating antenna pylons and container towers, high/low openings | 27% / 44% |
| 45+ | Remix loop | Existing patterns combine; no new controls | minimum 25% / capped at 48% |

Every new gap is bounded against the previous one and generated inside the fixed flight envelope. Decorative art can overhang a gate body, but it may never create invisible collision inside the opening.

## Progression

### Hangar

All five birds use the same dimensions, hitbox, and physics. Their animation, trail colour, and flap accent differ only as presentation.

| Bird | Unlock |
| --- | ---: |
| Neon Finch | Available immediately |
| Chrome Raven | 250 crystals |
| Prism Hummingbird | 500 crystals |
| Koiwing Glider | 800 crystals |
| Verdant Kite | 1,200 crystals |

Each bird uses eight dedicated transparent frames: six flap positions, one impact pose, and one unlock pose. New birds should follow the same resource layout below, so a future addition stays data-driven.

```text
Assets/Resources/SkyPulse/characters/roster/
  <visual-id>-frame-01-v1.png              # raised wing
  …
  <visual-id>-frame-06-v1.png              # downstroke
  <visual-id>-frame-07-v1.png              # impact
  <visual-id>-frame-08-v1.png              # unlock
```

The shipped visual IDs are `volt`, `steel`, `prism`, `cinder`, and `verdant`.
Each new bird must add exactly the same eight-frame set and one new `Skin` entry;
the validation hook rejects a roster that drifts from the six-flap/one-hit/one-unlock contract.

### Crystal-only upgrades

| Track | Levels | Effect |
| --- | --- | --- |
| Crystal Resonator | 150 / 400 / 900 | Attracts crystals within 6% / 10% / 14% of playfield width |
| Salvage Codec | 200 / 500 / 1000 | Adds 10% / 20% / 30% of run crystals on the results screen |

The result screen separates the crystals picked up during the flight from the Salvage Codec bonus and shows the resulting persistent balance. Saved data includes balance, upgrade levels, unlocked/selected bird, best score, and farthest route reached.

## Power-ups

Power-ups are placed on reachable lines roughly every 8–12 gates, never in the first three gates or immediately before a world transition. Only one can be active at a time.

- **Aegis:** absorbs one obstacle impact, visibly shatters, safely neutralises dangerous downward velocity, and grants a short immunity beat.
- **Time Pulse:** runs the simulation at 70% for four seconds, preserving the same handling relationship between the bird and world.
- **Crystal Magnet:** attracts crystals inside 25% of the playfield width for six seconds; it does not pull power-ups.

## Visual direction

SkyPulse is illustrated 2.5D cyberpunk aviation: five scrolling depth planes, clean emissive edges, and a dark, low-detail flight corridor. The high-contrast layer always belongs to the bird, gate opening, crystals, and power-ups.

- **Neon City:** midnight navy, electric cyan, magenta, restrained amber signs.
- **Acid Foundry:** charcoal, toxic lime, hot orange, cyan coolant.
- **Orbital Bazaar:** deep violet, cobalt, holographic gold, white starlight.

Avoid pixel art, chibi proportions, photorealism, muddy bloom, dense opaque foreground objects, and decorative text in the flight corridor. New art should be transparent PNG, right-facing, consistently registered on its canvas, and imported through **SkyPulse → Optimise Mobile Art** so Android and iPhone use the established compressed texture budget.

## Open and play

1. Open Unity Hub and add [`mobile/`](/Users/user/Desktop/SkyPulse/mobile).
2. Open [`Assets/Scenes/SkyPulse.unity`](/Users/user/Desktop/SkyPulse/mobile/Assets/Scenes/SkyPulse.unity).
3. Press Play. The project forces portrait orientation and targets 60 fps. The canvas uses
   the device safe area and a fixed logical 9:16 playfield, so it fits iPhone 17 Pro Max
   safely while preserving the same gate geometry on smaller, taller, or wider screens.
4. Tap/click to start and flap. Use Escape/P or the pause control to pause.
5. In the editor, use **SkyPulse → Playtest Checklist** for the milestone-focused test pass. `F4` displays collision guides in editor/development builds.

## Suggested playtest pass

Run enough sessions to reach score 5, 15, and 30 repeatedly. Record whether the first three gates teach the beat; whether the Foundry tunnel leaves a safe recovery window; whether moving and alternating patterns read before they become dangerous; and whether a crystal arc ever asks for an impossible line. Change a single tuning value only after observing the same problem across multiple runs.

The most valuable next refinement after this pass is authored world audio stems. A restrained, cross-faded music layer for each world will make the score-15 and score-30 transitions land without adding visual clutter or changing the fair core loop.
