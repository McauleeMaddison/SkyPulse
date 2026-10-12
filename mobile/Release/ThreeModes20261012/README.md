# Three-mode source update — 12 October 2026

Current branch: `codex/neon-visual-update`, fast-forwarded from its older state to the latest world/audio/build-7 work before implementation. Baseline source/release records were retained locally and committed as `026e382`. Earlier build 5 rollback tag/bundle and build 5/6/7 binaries remain preserved.

This is a changed Unity source candidate. It is **not** the uploaded build 7. App Store submission remains on hold; no App Store Connect change, upload, website publication or Apple message is authorized by this update.

## What is implemented

- Exactly three mode cards: Endless Route, Pulse Relay and Blackout Protocol, with collection, guide, privacy and Mission Control navigation.
- Endless delegates to the retained simulation. Its constants and 21 protected method bodies are compared to the baseline; integration hooks are normalized separately. The existing 15 birds, 120 unique paths, nine three-level Tech Tree nodes, world catalogue, GUIDs and save keys remain intact.
- Pulse Relay: First Circuit tutorial, Switchback, Highwire and Crossfeed; two to five ordered relays, separate gold charge, alignment/range-gated Pulse, optional high cells, energy shutters and an exit. First Circuit and Highwire offer a collectible Aegis. Clear failures, retry and best medals are saved separately. Medals depend on optional cells and protected hits; no new completion wallet bonus is granted.
- Blackout: Packet Recovery tutorial, Firewall Fork, Quiet Bypass and Dark Weave. Adjacent-node actions, required packets, firewalls, limited hacks and trace budgets. Hack = one charge and three trace without moving; move = one trace. Every puzzle has a budget-feasible, gold-capable route. Puzzle results never replace Endless best.
- Mission Control: four fixed UTC-day tasks; exactly five wallet crystals per claim and at most 20 per period; progress across attempts; claim states and local reset time; a journal recovers interrupted claims before wallet saves or subsequent purchases. Actual pickup events alone drive the pickup objective. Daily rewards bypass run totals and multipliers.
- Home adopts the last selected protocol: original cinematic flight, cyan/gold circuitry or black/green terminal graphics. Ambient circuit packets stop with Reduced Motion. This is a Home overlay, not a modification of the Endless world rendering.

## Open and play

1. In Unity Hub, add `/Users/mcauleemaddison/Desktop/SkyPulse/mobile` and use **Unity 6000.6.0f1**.
2. Open `Assets/Scenes/SkyPulse.unity`. There is no new scene or manual component wiring.
3. Press Play with a portrait Game view. Home → **CHOOSE MODE** opens the three cards. Each new mode → **SELECT MISSION** → **START MISSION**.
4. In Pulse Relay, tap to flap or use FLAP. Collect a gold cell, align with the next relay, and press PULSE when it lights up. Its activation window is 0.6–6 world units ahead and ±1.55 around the relay centre. Fly through the activated aperture and then the exit. Charge is not wallet currency.
5. In Blackout, tap adjacent tiles. ARM HACK enables an adjacent firewall action; opening it leaves the cursor where it is. Tap the open tile again to move. Read DATA/TRACE/HACKS before each action.
6. Return Home → **MISSION CONTROL** to inspect progress and claim completed tasks. New missions are always accessible to a starting player.

## Repeat local verification

From the repository root, these commands are safe for the real game save:

```sh
cd /Users/mcauleemaddison/Desktop/SkyPulse
python3 mobile/Tools/verify_endless_preservation.py
python3 mobile/Tools/verify_bird_registration.py
python3 mobile/Tools/generate_skypulse_audio.py --check mobile/Assets/Resources/SkyPulse/audio
python3 mobile/Tools/run_modes_qa.py
```

The last command creates a new disposable project and evidence folder. It sets a distinct company/product identity before launching Unity, copies the QA harnesses only into that copy's Editor folder, and resets only the QA save. Do not run two such tests concurrently. Do not put these harnesses in production Assets. Inspect its retained Unity log even when pass markers are present.

See `TEST_RESULTS.md`, `EVIDENCE_INDEX.md`, `DEVICE_CHECKLIST.md`, `PROVENANCE_ADDENDUM.md`, local `metadata.json`, `REVIEWER_WALKTHROUGH.txt` and `APPLE_RESPONSE_DRAFT.md`.

## Later device build

After reviewing the source, use **SkyPulse → Release → Configure iPhone and iPad Release**. Set a new unused iOS build number before exporting a changed candidate; the current stored number 7 belongs to historical build 7 and must not label the changed release. Choose **App Store Candidate** in a fresh output folder only when preparing the actual new candidate. Select the existing bundle ID and authorized signing team in Xcode, install it locally, and follow `DEVICE_CHECKLIST.md` on iPhone 17 Pro Max and iPad. Keep its commit/build/device/evidence record separate from build 7. Upload/submission requires the owner's later explicit authorization and compliance with the existing hold.

No new signed device archive, physical-device pass or Apple acceptance is claimed for this source update.
