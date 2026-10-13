# Verification — Relay portal uplift

The full disposable Unity 6000.6.0f1 suite passes on the current source. Nine expected mode/gameplay/save/capture pass markers are present, with no C# compiler errors. The new hardware imports, opens correctly with the existing Relay model, adds no colliders and appears on Relay Home only.

The regression suite retains the 1,300-gate route check, pause/resume, rewards, power-ups, save compatibility, all authored Relay missions and Blackout solutions, daily-claim recovery, 15-bird/120-frame contracts and nine three-level Tech nodes. The original camera/interface-width checks pass across 750×1334, 1320×2868 and 1640×2360.

New focused checks confirm:

- Consecutive Endless gate centres remain 5.265 world units apart at every tested resolution.
- The new Relay texture replaces segmented rim geometry and reused pipe collars.
- Portal presentation introduces no physics colliders.
- Pause preserves the current shutter-opening scale.
- Reduced Motion clears the activated shutter and membrane immediately.
- The detailed portal Home image is visible only in Relay.

| Editor render | Original gate spacing | Share of visible width |
| --- | --- | --- |
| 750×1334 | 5.265 units | 52.03% |
| 1320×2868 | 5.265 units | 63.55% |
| 1640×2360 | 5.265 units | 42.09% |

This confirms the framing explanation for the earlier animation: the original logical spacing is constant while wider aspect ratios expose more scenery. `verify_endless_preservation.py` now compares 27 protected methods, all original constants and the stable catalogues against retained build-7 commit `026e382`, plus the seven normalized integration hooks. The current entire main runtime source is also unchanged from the preceding candidate.

Evidence remains local in `artifacts/relay-gate-uplift-20261012/`: full-suite log, 63 stills, seven motion scenes, taller phone comparisons, original-source comparison and static scene/resource/player audit. Source/assets/log hashes and exact counts are in `RELEASE_RECORD.json`. These are actual Editor renders using isolated saves and assisted flight, not device screenshots. The metal opening/transparent centre, Home composition and new gate alignment were visually inspected.

The known Editor Search indexing exception and service diagnostics, and expected injected medal-save IOException, remain in the logs. They are retained as diagnostics rather than hidden. No physical-device pass, new signed archive, upload or App Store submission is claimed. Test the next numbered candidate on iPhone and iPad before later release; the submission hold remains in place.
