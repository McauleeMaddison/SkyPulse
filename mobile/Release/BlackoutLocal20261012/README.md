# Local Blackout Protocol revamp — 12 October 2026

Historical record: this describes the first local presentation experiment before the later cipher/phase update and the owner's authorization to commit/push the completed changes. See [the newer development record](../BlackoutComplexityLocal20261012/README.md). The source/QA hashes below retain their original capture state.

This is an **uncommitted local experiment newer than TestFlight 1.0.0 (9)**. The owner asked for a more premium, living puzzle game and a harder puzzle after each clear. Nothing in this update was uploaded, pushed to GitHub, submitted for review or applied to App Store Connect. Build 9, its store copy, archived source manifests and signed binaries remain historical, unchanged records.

## What changed

- A suspended, animated core and dark chamber give Blackout its own Home view. Home displays campaign clears separately from the Endless best.
- A new sector map previews eight authored layouts. Only sector one starts available; a saved clear unlocks the next. Replays retain the best medal and trace result.
- The puzzle is a floating, machined lattice with moving signals, illuminated connections, hovering cores, security nodes and an animated uplink. The cursor travels between nodes; collection, breach and security actions have distinct bursts. Turns rebuild no node controls.
- A closed adjacent firewall is selected without spending anything. The Breach action then spends one charge and three trace without moving. The opened node can be entered normally.
- Later security nodes cost two trace per visit. They are shown in red and have a different effect from core recovery and firewall breaches.
- Extraction presents the earned medal, trace result, campaign progress and Next Sector. A failed progress flush cannot unlock the next sector in memory.
- Pause/background handling freezes transport and ambient motion. Reduced Motion makes transport/effects settle immediately. Navigation cancels pending effects; a result cannot finalize twice or award a wallet bonus.

## Difficulty progression

The authored shortest winning route requires more trace at every successive stage. Core counts and board areas never decrease; spare trace on the best route never increases. This establishes concrete progression, rather than relying only on the stage number. Human difficulty and touch performance still need playtesting.

| Sector | Name | Minimum trace | Budget | Cores | Grid |
| --- | --- | ---: | ---: | ---: | --- |
| 01 | Packet Recovery | 7 | 13 | 2 | 5 × 4 |
| 02 | Ghost Channel | 12 | 18 | 2 | 5 × 5 |
| 03 | Firewall Fork | 21 | 26 | 3 | 6 × 5 |
| 04 | Split Horizon | 22 | 27 | 3 | 6 × 6 |
| 05 | Watchtower | 25 | 29 | 3 | 7 × 6 |
| 06 | Cipher Vault | 29 | 32 | 4 | 7 × 7 |
| 07 | Dark Weave | 30 | 33 | 5 | 7 × 7 |
| 08 | Core Extraction | 33 | 35 | 5 | 7 × 7 |

## Compatibility and release boundary

The original four `BlackoutPuzzle.All` definitions and `skypulse.blackout.v1` medals are retained. The new campaign uses separate IDs and `skypulse.blackout-campaign.v1` data; old medals are not reassigned to different puzzles. Endless source, physics, gate spacing, art, bird paths, Tech Tree, Relay definitions, wallet and daily reward amounts remain unchanged. The original daily Packet Recovery task still counts a legitimate first-sector clear.

The local PlayerSettings build number remains 9 as a retained configuration; **do not export/upload this changed source as build 9**. Any future device candidate needs a fresh unused number, device testing, matching metadata/evidence and explicit authorization. Public review remains on hold awaiting Apple's clarification.

## Open and test locally

1. Open `mobile/` in Unity **6000.6.0f1**, with `Assets/Scenes/SkyPulse.unity`.
2. Press Play, select Blackout Protocol from the top-left badge, then press Play.
3. Complete sector one and use Next Sector. Check movement, core collection, selecting/breaching firewalls, security costs, restart and sector locks.
4. Check pause/background/resume and Reduced Motion. Close/reopen and check saved unlocks and best medals. Check that Endless and Relay still work.

Do not run the QA tools in the production Unity project: they reset a **disposable QA identity**. Use `python3 mobile/Tools/run_modes_qa.py --output artifacts/your-new-local-qa-folder` for an isolated run.

See [validation](TEST_RESULTS.md), [provenance](PROVENANCE.md) and the local artifacts at `artifacts/blackout-local-20261012/`. Previews are actual Unity Editor captures with isolated saves; advanced stages are unlocked artificially for preview capture. They are not native-device footage.

The completed animated preview is `artifacts/blackout-local-20261012/previews/blackout-local-animated.gif`, showing Home, live Sector 03 movement/breach and extraction together. The still overview shows Sector 06. `LOCAL_RECORD.json` records the exact fifteen runtime script hashes matching the successful QA run, eleven pass groups, and verification that all twenty-eight build 9 evidence hashes remain unchanged. Preview source captures and a reproducible composition script are retained alongside the GIF.
