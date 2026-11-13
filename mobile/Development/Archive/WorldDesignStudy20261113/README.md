# SkyPulse — worlds and mode design options

13 November 2026. **Review study only; installed Build 21 and shipping runtime are unchanged.**
All images are actual Unity Editor renders. No AI repainting or invented engine screenshots.
The source, renderer and limits are recorded in [RENDER_RECORD.json](RENDER_RECORD.json).

Open [the full review gallery](index.html), [twelve worlds](Twelve-Worlds.png),
[worlds with gameplay](Twelve-Worlds-Gameplay.png), [Endless density options](Endless-Options.png),
[Relay options](Relay-Options.png), and [Blackout concepts](Blackout-Options.png).

## Recommended world direction

**A — Layered districts.** A main landmark, smaller residential/support quarters,
near terraces and distant occupied islands. The study has five prominent plus four
smaller distant district elements, with two physically connected terrace bridges.
It uses each world's own saved architectural parts, sky, lighting palette and pipes.

**B — Airy archipelago.** Five more separated districts, a larger role for open sky,
and a future focus on cloud strata, small craft and moving environment details.
The preview shows the architectural spacing; extra cloud/traffic work is proposed.

**C — Metropolitan.** Eleven district elements for the strongest occupied-city feel.
This is the busiest option and would need the most careful reduction of distant
geometry and effects on older phones.

My recommendation is A, followed by hand-composed per-world grouping and purposeful
small details: landing bays, roofs, terraces, lit interior clusters, connected paths
and a little distant traffic. Keep the bird/crystal/opening corridor legible. These
are reusable module studies, not eleven brand-new building kits.

## Every world at full size

| World | Background | Gameplay context |
| --- | --- | --- |
| 01 · Neon City | [World](Previews/world-00-background.png) | [Game view](Previews/world-00-gameplay.png) |
| 02 · Aurora Rise | [World](Previews/world-01-background.png) | [Game view](Previews/world-01-gameplay.png) |
| 03 · Solar Drift | [World](Previews/world-02-background.png) | [Game view](Previews/world-02-gameplay.png) |
| 04 · Midnight Tide | [World](Previews/world-03-background.png) | [Game view](Previews/world-03-gameplay.png) |
| 05 · Velvet Dawn | [World](Previews/world-04-background.png) | [Game view](Previews/world-04-gameplay.png) |
| 06 · Crystal Night | [World](Previews/world-05-background.png) | [Game view](Previews/world-05-gameplay.png) |
| 07 · Jade Horizon | [World](Previews/world-06-background.png) | [Game view](Previews/world-06-gameplay.png) |
| 08 · Violet Rain | [World](Previews/world-07-background.png) | [Game view](Previews/world-07-gameplay.png) |
| 09 · Eclipse | [World](Previews/world-08-background.png) | [Game view](Previews/world-08-gameplay.png) |
| 10 · Cobalt Storm | [World](Previews/world-09-background.png) | [Game view](Previews/world-09-gameplay.png) |
| 11 · Amber Skies | [World](Previews/world-10-background.png) | [Game view](Previews/world-10-gameplay.png) |
| 12 · Polar Glow | [World](Previews/world-11-background.png) | [Game view](Previews/world-11-gameplay.png) |

## Pulse Relay

- **A — Titanium skyport (recommended):** retain detailed towers and connected lower
  decks; remove competing overhead rings and near launch hardware, soften broad
  glow, reduce planet dominance and use one restrained service craft.
- **B — Pearl terminal:** lighter ceramic/alloy structures and a clean cyan palette,
  with the same physical depth. A brighter alternative to Endless's night city.
- **C — Open flight corridor:** a strong eastern tower, fewer cross-screen structures
  and more open sightlines around crystals and hoops. The simplest spatial layout.

The render study keeps the real hoop, floor, bird and crystal rules. Production
follow-up would refine floor materials and architectural LODs, and coordinate crystal
charging, red/blue lock feedback and passage effects. Automatic gate collision
clearance must stay immediate. These mechanics already exist and should be refined.

## Blackout: improve purpose, interaction and identity

The current implementation already has eight authored optical puzzles; mirrors,
splitters, relay shutters and sensors; trace budgets and Gold targets; free PLAN
previews; saved attempts; and immediate intermediate progression. Rebuilding those
working systems is unnecessary. It lacks a strong world-specific visual payoff and
has limited campaign breadth. Planning currently needs a mode/selection/confirmation
flow; a direct preview gesture could make inspection more comfortable.

| Direction | What the player does | New payoff and identity | Scope |
| --- | --- | --- | --- |
| **A — World Restoration (recommended)** | Route energy through authored, readable optics to restore a named district system. Keep current rules first. | A 3D SkyPulse island responds to powered cores; success brings beacons, transit and windows online. Milestone cosmetic badges or extra landmark lighting connect it to the flight worlds without blocking flight access. | Lowest-risk fit: renderer, feedback and campaign presentation first; expand authored puzzles after playtesting. |
| **B — Circuit Forge** | A proposed sequential charging variant: calibrate a few clear power stages for a mechanical flight module. | Bird/module showcase, permanent cosmetic milestones and Hangar display rewards, with no flight advantage. | Larger rules and reward change. The preview uses the existing board; sequential charging and cosmetic awards are not implemented. |
| **C — Relay Control** | Solve short station-control circuits to reactivate a visible gate or station subsystem. | A 3D hoop changes from locked to powered as the circuit is completed; optional daily logic challenges. | Medium/larger scope: gate/system objective binding and a solvable authored/daily content pipeline. The preview is a goal/layout concept, not a working new gate puzzle. |

For A I would implement, in order:

1. **Readability:** blue/silver interactive optics, distinct powered states, a quiet
   background, smaller mounts and fewer competing projection effects. Keep threats
   red and the destination distinct; colour must have a shape/label cue too.
2. **Direct planning:** optional press-and-hold ghost preview, release to cancel,
   quick tap to rotate. Preserve immediate feedback, generous hit targets, trace
   costs and generation fencing between puzzles. The existing PLAN button remains
   an accessible alternative. This gesture is proposed, not implemented here.
3. **Visible restoration:** connect actual live core/relay state to a small district
   diorama. A completed system stays visibly restored in the campaign. Its animation
   runs while the next board is already available, respecting Reduced Motion.
4. **More purposeful content:** short authored cases with multiple useful routes,
   clear problems and optional mastery targets. Expand beyond eight boards. Test
   every new solution/budget; do not add time pressure by default.
5. **Lasting motivation:** clear completion milestones and restrained cosmetic
   rewards. Keep one economy and avoid making puzzle completion mandatory for flight.

The three Blackout images are visual concepts on the same existing puzzle engine.
Their new goal showcases, layout, rewards and gestures require production
implementation and testing after a direction is chosen. They are not phone builds.

## Promotion plan after visual choice

Author named district groups and positions in editable Unity scenes/prefabs;
retain the existing source objects and GUIDs. Bake combined/LOD runtime output
separately. Then verify world transitions, Reduced Motion, touch mapping, puzzle
progression, memory and sustained phone performance. Preserve the approved birds,
tap controls, pipe dimensions, saves, current economy and auto-unlocking gates.
The source helper is [PreviewDriver.cs.txt](PreviewDriver.cs.txt); it refuses to run
outside the disposable QA product. No shipping source or public submission changed.
