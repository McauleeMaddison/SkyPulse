# SkyPulse build 18 — holographic UI, LED pipes and purposeful Hangar progression

Local iPhone/iPad candidate. **Not uploaded or physically device-tested. Build 17 remains the last uploaded TestFlight build. Public App Review remains on hold.** See `RELEASE_RECORD.json` for the final compiled source and export evidence.

## Latest pipe-cap repair

Transparent collar corners now preserve the scenery behind them, removing the opaque black patch while keeping the 3D geometry and LED animation. See [the repair checks](PIPE_CAP_FIX.md). The four-second pipe preview and source records below have been refreshed.

## Player changes

- The connected Tech Tree keeps free two-axis dragging and all 37 fixed, disjoint touch targets. Each level is explicit: installed, next with its exact price, or locked. The available level has an illuminated plus marker and a small “View upgrade” cue. Inspection still requires a separate purchase confirmation.
- Tree, Core/training and lineage lettering use a shared holographic shader per view, a restrained spectral gradient and light sweep. The lettering and hit targets stay stationary. Reduced Motion keeps a static finish. Animation uses elapsed time; warmed tree/effects checks verify no managed allocations.
- All three Homes omit repeated marketing/instruction strips. Relevant mode actions, progress, privacy and the field guides remain. Signal Heist keeps a single restoration objective.
- Endless uses its original cyan/magenta shaft and collar textures on the existing collider-matched 3D metal geometry. Only the coloured LED inlays emit and carry travelling bright packets. Reduced Motion keeps the rails illuminated without moving packets. Flight width, force, collision sizes and gate spacing are unchanged.
- Each completed bird-training level now adds **2% off future founder-bird unlocks, capped at 20%**. Existing trained saves receive this benefit automatically: it is derived from their stored genome levels. Prices round up once to a whole crystal. Training already improves the two displayed Endless reward traits; the panel now also previews the exact next-bird price change.
- Hangar prices and confirmations use the same discounted quote. A changed training quote refreshes before it can charge. Insufficient balances, repeated confirmation and already-owned birds are guarded. Core discounts for Tech, training and breeding remain separate; their base prices and existing reward rules are retained.
- Breeding creates a permanent equippable offspring with the exact previewed inherited traits. It keeps both parents, blocks duplicate owned combinations, adds the child to the carousel and now uses the existing bird-unlock reveal to make the result clear. It produces a lineage bird, not a free additional founder unlock. Trained offspring also contribute to Hangar savings.

## Actual Unity previews

These are silent Unity Editor development renders with QA-owned demonstration progress. They are not phone recordings or App Store preview uploads.

- `tech-tree-preview.mp4`: eight seconds / 240 frames / 30 FPS, connected-tree inspection and return to Core.
- `endless-led-preview.mp4`: four seconds / 120 frames / 30 FPS, stationary inspection of the real pipe shader’s animated strips. The bird/camera are deliberately held for material inspection; this clip is not a flight-control test.
- `tech-collection-focus.png`, `tech-upgrade-inspector.png`
- `bird-training.png`, `bird-lineage.png`
- `home-EndlessRoute.png`, `home-PulseRelay.png`, `home-BlackoutProtocol.png`

Both videos were fully decoded after encoding. Their frame counts and hashes are recorded. Moving targets and transactions were checked separately from the media fixtures.

## Editing in VS Code

Start with [the editing guide](../../EDITING_GUIDE.md). `SkyPulseNativeGame.cs` has an editing map, foldable regions and notes around catalogue prices, stable IDs, tuning and saves. The main file fell from 9,014 to about 6,560 lines. Eighteen methods moved unchanged to `.Hangar.cs` and `.PipeVisuals.cs`; six unused private helpers and four unused layout constants were removed. This is still one partial MonoBehaviour, with its original script GUID and saved identities. `REFACTOR_CHECKS.json` retains the exact moved-body hashes.

## Verification and limits

`QA_RECORD.json` distinguishes the 22-group complete run after the gameplay/economy changes from the scoped rerun after source organisation. The latest two-file material correction additionally passes the focused cap-contour/depth and player-experience suite. Checks cover real purchases, quote refreshes, insufficient/exact balances, Core savings, inherited offspring, journal recovery, reloads, touch separation, Relay auto-unlock, immediate puzzle progression, original flight behaviour, UI bounds, shaders, Reduced Motion and 30/60/120 FPS animation clocks. A rendered-pixel check catches invisible pipe faces even when a shader compiles successfully.

The final iOS IL2CPP export is recorded in `BUILD_INFO.json` and `SOURCE_MANIFEST.json`; every source entry is compared with the repository. This task does not create a signed archive, upload a build, update the live App Store Connect listing or submit App Review. The earlier build 17 symbol-packaging warning remains documented in `../SignalHeist20261021/SYMBOLS_NOTE.md`; a local export does not establish that the archive warning is removed.

Physical phone/tablet checks remain: legibility in daylight and at low brightness, animated rail intensity, one-thumb navigation, training/unlock/breeding flows, update/save retention, sustained frame times, memory and thermals. Editor checks do not certify physical 60 FPS or App Review acceptance.

The logs retain a Unity Editor Search startup `ArgumentOutOfRangeException`; the affected stack is Editor indexing, outside the game. Persistence tests intentionally inject save errors to verify recovery. No shipping gameplay exception was observed in the successful runs.

No new external image, model, font or audio asset was added. Existing art is reused; the new material/lettering code is procedural. The provenance ledger and Apple’s unidentified private similarity comparison remain separate from these checks. Metadata validation checks the current prepared copy, identity, source hashes and local duplication/formatting; it cannot certify global exclusivity or future policy compliance.

## Next improvements

Prioritise physical playtesting of this candidate, a short interactive first Signal Heist encounter, and clearer failure-location feedback. Measure readability and effect density on the phone before adding more particles or lighting. Consider a distinct visual accent for inherited trait grades, provided it stays readable and never changes the tested flight hitbox.
