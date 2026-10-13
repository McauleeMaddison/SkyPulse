# Relay hardware uplift — 12 October 2026

The existing Unity iPhone/iPad game now uses detailed metallic portal hardware for Pulse Relay's numbered gates, exit and Home hero. A new transparent sprite gives the portals brushed metal, physical depth, gold fittings and cyan emitters. The old segmented line rim, reused Endless pipe collars, rectangular aperture wash and address plate were removed. The elliptical energy membrane and narrow shutter fade/retract when Pulse activates a gate; the hardware brightens as the circuit opens. Pause freezes the transition, and Reduced Motion shows the open state immediately.

The new visual opening is aligned with the existing 3.6-unit activation window. Relay mission coordinates, alignment tolerance, charge/order rules, objective checks, pickups and flight forces are unchanged. The sprite is visual artwork and adds no collider. Original artwork/GUIDs are retained.

## Original Endless spacing

Endless gameplay has not been edited in this uplift. Its entire `SkyPulseNativeGame.cs` file is byte-identical to the preceding candidate. The preserved build-7 source uses `CameraHeight = 18`, logical playfield aspect `9/16` and `PipeSpacingFraction = 0.52`: **18 × 9/16 × 0.52 = 5.265 world units between consecutive gate centres**. The camera, spacing function, spawn path, physics and pipe art are compared against commit `026e382`.

The earlier animated comparison rendered at 750×1334, close to 9:16. The owner's taller iPhone screenshots use 1320×2868. The unchanged 5.265-unit spacing therefore occupies about **52.0%** of the earlier preview width and **63.6%** of the taller iPhone width. A wider frame displays more of the world and makes the same gates appear relatively closer together. New phone previews use 1320×2868, without modifying the production camera or spacing.

## Open and validate

Open `mobile/Assets/Scenes/SkyPulse.unity` in Unity 6000.6.0f1. Play opens Endless Home. Tap the top-left badge, choose Pulse Relay, press Play, then choose a mission. Align with a numbered aperture, collect gold charge and press Pulse. Verify the metal portal, clear opening, shrinking shutter and brightening rim. The exit uses matching hardware with green lighting. Return to Endless and check the original pipe art and handling.

Repeat isolated QA with:

```sh
python3 mobile/Tools/run_modes_qa.py
python3 mobile/Tools/verify_endless_preservation.py
```

Optional taller animation captures: set `SKYPULSE_PHONE_PREVIEW=1` before running QA. `SKYPULSE_PREVIEW_SCENES=home-relay,endless-flight,relay-flight` restricts animation capture scenes; the full gameplay suite still runs. QA uses a disposable product identity and never resets the real player's save. Do not run two QA copies at once.

See [test results](TEST_RESULTS.md), [candidate record](RELEASE_RECORD.json), [artwork prompt and saved path](ASSET_GENERATION.json), [provenance addendum](PROVENANCE_ADDENDUM.md) and [file ledger](Provenance/FILE_LEDGER.csv). This guide extends the [three-mode identity update](../ModeIdentityWidth20261012/README.md). Previews are actual Editor renders using isolated saves and assisted QA flight.

Local previews: [animated phone comparison](../../../artifacts/relay-gate-uplift-20261012/previews/relay-gate-phone-preview.gif), [Relay gate detail](../../../artifacts/relay-gate-uplift-20261012/previews/1320-relay-flight.png) and [Relay Home](../../../artifacts/relay-gate-uplift-20261012/previews/1320-home-relay.png). These use the taller phone framing discussed above.

The new signed binary and physical iPhone/iPad checks remain pending. Use the existing [device checklist](../ThreeModes20261012/DEVICE_CHECKLIST.md), including portal readability, aperture alignment, pause/resume, Reduced Motion and texture memory/frame pacing. Build 7 sign-offs are historical. App Store submission remains on hold; no upload, website publication or Apple message was performed.
