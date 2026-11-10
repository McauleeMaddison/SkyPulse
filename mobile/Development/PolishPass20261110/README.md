# Local gameplay and presentation pass — 10 November 2026

This candidate follows the owner's request for tap-driven solid wings, higher shoulder mounts, each bird's own crash pose and a more useful 3D Hangar. It supersedes the continuous exhibition-style flight motion in the earlier [flight-clock experiment](../FlightAnimation20261110/README.md). No phone build or public review submission was made.

## Behaviour and presentation

- The Editor flight preview follows the native accepted-tap upstroke/downstroke state. A new tap during the downstroke reverses from the current pose; no input settles to glide. Smoothing advances only once per rendered frame, holds during pause and resets on retry. Studio/Hangar exhibition motion stays separate. Reduced Motion retains restrained essential flight movement.
- Native crashes resolve the currently equipped bird's own hit path instead of trusting a prior cached pose. The Editor selector also displays its selected bird's own authored crash image without changing equipped stats, unlocking birds or altering saves. Old flight depth/glint layers cannot reappear over the crash.
- Solara, Virex and Borealis have raised shoulders. Both rotating assemblies and all four body-side fittings move together; bind positions are recaptured. Meshes, materials, joint IDs and prefab GUIDs stay intact. The saved correction refuses changed source prefabs; original YAML block order is retained on promotion.
- Home and the three Hangar bays can display the saved solid models in the Editor. The featured Hangar bird sits above an open bevelled titanium cradle with an inset light ring. All birds share a centred 12°/second showcase rotation and 1.65-second wing cycle. Aspect-correct transparent surfaces replace the previous artwork and studio, preserving swipe, neighbour buttons, unlock UI, training and breeding actions. Camera framing covers a full turn and stroke without resizing each frame.
- Twelve-world architecture lighting now takes restrained key/fill tones from each place. Neon keeps its original colours; geometry, sky density, island positions and pipe dimensions are unchanged.
- Relay has a pooled annular passage ripple and clear partial/open/cleared labels, observing the existing gate logic without delaying passage. Blackout's PLAN/CLOSE labels, unpowered targets, route contrast and completion toast make the existing city-restoration objective clearer; its next puzzle remains immediately playable.
- Tech Tree locates affordable upgrades first and explains exact shortfalls. Training, Core savings and breeding show actual next costs and reward effects without changing prices, transactions or the economy.

## Actual checks

[Gameplay checks](Checks/Gameplay.json) contain 615 focused assertions: all fifteen tap/crash identities, no stale impact overlays, pause/retry, live smoothing sampled at 30/60/120 Hz, Home/Hangar replacement, per-view aspect and full-turn/stroke framing. Separate suites passed real same-frame Relay charge/gate collision, one-thumb input fencing, immediate/once-only Blackout progression, exact progression pricing/copy fit, pooled feedback allocation and all twelve world/pipe themes with collision-checked simulation and transition checks.

[Pickup regression](Checks/Pickups.json) passed 202 assertions for bounded rewards, physical contact, duplicate callbacks, pause/retry/mode isolation, warmed pooling and guide fit. [Tested source hashes](Checks/Source-Validation.json) match every changed C# and shader file. [Asset preservation](Checks/Preservation.json) and [shoulder promotion](Checks/Shoulder-Promotion.json) record unchanged source art, Finch, geometry, finishes, scenes and GUID identity.

Final Unity C# compilation passed. The copied disposable Library still logs the known Unity Editor Search startup indexing exception; this is not an empty-Console certification. Tests used a separate QA product/save. 30/60/120 Hz cases verify logic and smoothing, not measured phone performance. Bird diagnostics isolate animation and are not storefront footage.

## Review in Unity

Open `mobile/Assets/Scenes/SkyPulse.unity`. Choose **Tools → SkyPulse → Bird Workshop → 3D roster in actual gameplay**, keep **Show 3D model** enabled, and press Play. **Use equipped bird** follows the real equipped identity; an explicit dropdown model overrides appearance only. Home, Hangar and both flight modes use the canonical solids for this Editor review. Switch to Hangar and swipe normally. Check tap strokes, mounting, crash art, centre framing and the cradle in motion. Stop the preview to restore native artwork and release its temporary cameras/surfaces.

The remaining roster/world appearance still needs owner sign-off. The installed iPhone stays on the previously recorded Build 19; the shipping runtime roster remains original artwork until the approved solids are deliberately promoted. Public review stays on hold. After visual approval, make one matching device candidate and test sustained performance, touch feel, saves and interruptions before replacing release media.
