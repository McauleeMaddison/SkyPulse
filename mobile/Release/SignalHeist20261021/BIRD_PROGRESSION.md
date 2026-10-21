# Core, training and inherited bird traits

This local build-17 expansion gives the existing 15 founder birds distinct reward combinations and adds saved offspring to the same Hangar carousel. It is not uploaded or physically device-tested. Public App Review remains on hold.

## Core evolution

Tap the central crystal in Tech Tree. The fixed header CORE control still returns the map to its origin.

| Core | Colour / halo | Price to reach | Bird training cap | Lineage bays | Inherited grade cap | Upgrade savings |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| 1 · Ion | Cyan, slow tilted orbit | Free baseline | 2 | Breeding locked | 1 | 0% |
| 2 · Jade | Green, precessing orbit | 400 | 3 | 8 | 1 | 5% |
| 3 · Prism | Violet, counter-orbiting pair | 900 | 4 | 12 | 2 | 10% |
| 4 · Solar | Amber, gently breathing paired halos | 1,800 | 5 | 16 | 2 | 15% |
| 5 · Nova | Rose, faster opposing/precessing light guides | 3,200 | 5 | 20 | 3 | 20% |

Existing Collection, Recovery and Mastery upgrades keep their base catalogue prices, effects and prerequisites. The current Core reduces their payable crystal costs, bird training and breeding costs by 0/5/10/15/20%. Payable costs are calculated once from the base price and rounded up to whole crystals; a 150-crystal upgrade costs 150/143/135/128/120 across Ion through Nova. Core evolution and founder unlock costs keep their stated prices. Prior purchases are retained without refunds or repricing. Core levels do not lock existing Tech Tree purchases. Reduced Motion uses a steady halo pose. The upgrade preview uses its own isolated render stage so it cannot accidentally render the main tree.

## Bird training and real effects

Each founder has a different pair of traits. Hangar shows current traits; TRAIN BIRD shows exact current and next effects. Training levels 2–5 have base costs of 120 / 280 / 600 / 1,100 earned crystals, subject to the Core cap and discount. The first training upgrade costs 120 at Ion, 114 at Jade, 108 at Prism, 102 at Solar and 96 at Nova. The preview, affordability check and journaled debit use the same calculation. Existing players retain their birds and begin at training level 1.

Both traits strengthen with training. Primary strength is `gene grade + 1 + training level`; secondary strength is `gene grade + training level - 1`.

| Trait | Actual Endless Route reward |
| --- | --- |
| Collector | +2% physical pickup crystals per strength point, accumulated fractionally within the run |
| Salvager | +2% pre-result run crystals per strength point at run end |
| Precision | Strength-point crystals for each completed group of four perfect gates |
| Voyager | Strength-point crystals for each completed group of ten gates |
| Recovery | Twice the strength in crystals once a run reaches at least five gates |

Result rewards use the existing once-only result flag. Retry resets fractional collection carry. Bird choice does not change gravity, flap lift, collisions, gate score, route dimensions, Relay crystal requirements or Blackout puzzles. Bird traits grant Endless currency; other modes retain their existing reward rules.

## Breeding

At Core level 2, choose two different owned parents in Hangar → BREED. The parent picker uses a normal touch list and returns to the same preview with Back. The complete preview is deterministic: no random roll or waiting timer.

The child inherits the first parent's appearance and primary trait. Its complementary trait comes from the second parent's secondary trait, or that parent's primary trait if the first would duplicate the child's primary. A parent trained to at least level 3 passes one additional gene grade, within the current Core's grade cap. Every offspring begins at training level 1. The base price is `300 + 200 × (sum of inherited grades - 2)`, from 300 to 1,100 crystals, then the current Core discount applies. A grade-one pair costs 285 at Jade or 240 at Nova. The exact traits, reward conditions and discounted cost are shown before confirming.

Both parents remain owned. The child receives a persistent lineage ID, parent links and generation, enters the real carousel and can be equipped/trained. Its six authored flight frames and registration come from the first parent's founder silhouette; presentation accents and trail derive from the inherited traits. This does not generate a new illustrated character asset.

Breeding combines reward traits to suit a player’s approach. Higher inherited grades give stronger collection or milestone rewards than grade-one birds at the same training level; parent training and Core evolution therefore improve future offspring potential.

An already-owned combination of appearance, traits and grades cannot charge again. Generations stop at 8; Core 5 has 20 offspring bays. Founder ownership is retained separately.

## Persistence and verification

The new versioned `skypulse.birds.v1` record and completed backup retain Core rank, training, offspring, parents and a monotonic revision. Purchases journal the resulting progression and absolute wallet target before applying the debit. Interrupted writes replay that target before other wallet mutations; stale revisions and old UI-panel callbacks cannot charge again. Existing native ownership, upgrade and wallet identifiers remain compatible. Core rank determines discounts on reload without a new save key. Tech Tree confirmations retain their quoted Core, level and payable price; a changed quote refreshes the panel without spending and requires fresh confirmation. Offspring are reconstructed before the equipped bird is resolved on load.

Automated checks cover all founder combinations, actual training/result rewards, fixed-price inheritance, retained parents, duplicate/stale confirmations, save/restart, interrupted debits, insufficient funds, maximum Core/training, picker Back, modal input separation, non-overlapping targets and each halo at simulated 30/60/120 FPS with Reduced Motion. The broader flight, Relay and Blackout regressions also run.

Still required: physical iPhone/iPad controls, readability, save/reopen, a longer progression playthrough to assess reward balance, and sustained CPU/GPU/memory/thermal profiling. Editor checks are not a physical-device pass.

## Presentation refinement

Core evolution, training and breeding now share bevelled satin-glass surfaces, quieter secondary controls and an accent-coloured purchase action. Core evolution has a larger live optical preview, a five-rank colour track and compact comparisons for training, lineage bays and gene grade. Training displays both current and next reward values in separate readable rows, with direct access to the Core when capped.

Training and lineage use a single isolated 3D studio per open panel. Registered wing frames and the existing mechanical-bird shader animate above lathed titanium pedestals with recessed light rims. The birds remain authored illustrated characters; the pedestals and connecting display are geometry. Paired moving signals flow from the parent cards towards the offspring without crossing the parent names. The parent picker shows portraits, levels and existing traits. Both parents and the exact inherited combination remain visible before purchase.

Decorative motion never moves a touch rectangle or delays an action. Panel emphasis fades in over 0.18 seconds while controls remain available. Reduced Motion makes this immediate and freezes the bird, pedestal and signal motion. Animated display poses match at simulated 30/60/120 FPS; the measured warmed studio loop allocates no managed memory. Display cameras are disabled immediately when replacing a panel and resources are destroyed on the following frame. Mesh-framing checks and actual Unity-rendered image inspection cover the pedestals; the separate 90-frame registration check covers the authored wing frames. These are Editor checks, not device frame-rate certification.

The presentation refinement itself changed no game rules. The subsequent owner-requested Core-value update adds the discounts above while preserving trait calculations, inheritance rules, save keys, flight forces and score. Shipping presentation lives in `SkyPulseNativeGame.BirdProgression.cs`, `SkyPulseProgressionStage.cs` and `SkyPulseProgressionSurface.cs`, with the presentation update called from `SkyPulseNativeGame.Depth.cs`. Focused checks remain outside shipping Assets in `Tools/SkyPulseBirdProgressionChecks.cs`.

The revised description and reviewer notes are prepared locally. The earlier saved App Store Connect copy predates this expansion and must be updated before a later separately authorized review submission.
