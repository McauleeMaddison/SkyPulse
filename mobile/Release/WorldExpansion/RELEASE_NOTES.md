# World expansion — candidate preparation, 29 September 2026

Requested by the owner during the 4.3(a) audit. This is an intentional feature addition, not an assertion that more backgrounds resolve Apple's similarity allegation. No App Store metadata was published or review reply sent during this audit.

## Route

| First score | World | Artwork |
|---|---|---|
| 0 | Neon City | Existing |
| 15 | Aurora Rise | Existing |
| 30 | Solar Drift | Existing |
| 45 | Midnight Tide | Existing catalogue, now reachable in route |
| 60 | Velvet Dawn | Existing catalogue, now reachable in route |
| 75 | Crystal Night | Supplied JPEG; same world ID |
| 90 | Jade Horizon | Existing catalogue, now reachable in route |
| 105 | Violet Rain | Existing catalogue, now reachable in route |
| 120 | Eclipse | Supplied JPEG; same world ID |
| 135 | Cobalt Storm | Supplied JPEG; new world |
| 150 | Amber Skies | Supplied JPEG; new world |
| 165 | Polar Glow | Supplied JPEG; new world |
| 180 | Neon City again | Full route repeats |

Guide retains three readable previews per page with Previous/Next controls. All four pages are available from Home without unlocking or reaching a high score. Actual gameplay entry still requires the listed score; no reviewer-only shortcut was added. New worlds use existing pipe/trail styles and existing score-based flight tuning. World catalogue order/old IDs are preserved, and the three new IDs are appended.

The farthest-world save uses the same key with its valid maximum expanded from 2 to 11. Existing values 0–2 remain valid. Save write format, roster migration, owned birds, upgrade IDs, economy, collider geometry, gravity/flap parameters, speed/gap curves and power-up durations remain unchanged. Twelve backgrounds are warmed/cached before flight to avoid first-use load stalls; profile actual device memory/startup cost before release.

Two superseded PNGs and their `.meta` files moved from Resources to `PreviousArtwork/`, retaining exact bytes and GUIDs. Production references now point at the supplied JPEGs; no serialized GUID reference to the old images was found. The archived build 6 remains untouched and includes its original backgrounds. See [ART_PROVENANCE.md](ART_PROVENANCE.md).

## Verification performed

- Unity 6000.6.0f1 isolated project `/private/tmp/skypulse-43a-audit-20260929`; product `SkyPulseWorldAuditQA20260929` isolates PlayerPrefs. Never copy/run the gameplay harness in the user's production project.
- `Tools/SkyPulseWorldExpansionChecks.cs` runs the existing `SkyPulseBetaChecks` (1,300 generated gates plus economy/persistence/purchase/animation checks), then checks all 12 world entries, resource loads, boundaries, transition/HUD agreement, score-180/360 wrapping, expanded save reload, legacy value 2, Home next-world copy and all guide pages.
- Exit 0; `SKYPULSE_WORLD_EXPANSION_GAMEPLAY_PASS` and `SKYPULSE_WORLD_CHECKS_PASS` in `artifacts/apple-43a-audit-20260929/world-expansion-unity.log`.
- 36 Editor QA captures at 750×1334, 1320×2868 and 1640×2360 cover Home, guide pages, Hangar, Tech Tree and the five supplied worlds. These use artificial test state and are **not App Store screenshots or actual phone playtest evidence**.
- Guide/Home text-height assertions passed; selected phone/iPad guide and background renders were visually inspected. Captures are in `artifacts/apple-43a-audit-20260929/captures/`.
- Final separate import/reference validation after removing superseded files from the disposable Assets: exit 0, `SKYPULSE_43A_STATIC_UNITY_PASS`, 149 resource assets, one configured scene, no missing scene scripts/imported assets. Source metadata GUID scan resolves all referenced non-builtin GUIDs.
- All 90 bird frame registration/source checks passed. Eighteen critical method bodies compared byte-for-byte to baseline, including save writes, purchases, Hangar/Tech Tree, flight/collision/difficulty and power-up duration handling.
- Editor log contains a Unity Search indexing `ArgumentOutOfRangeException`, licensing-token refresh warning and shutdown/device-service warnings. They do not negate the successful game assertions, but this is not a completely clean Editor log.

Reproduction: create a disposable copy of `mobile/Assets`, `Packages`, `ProjectSettings`; give it a unique product name containing `QA`; copy `Tools/SkyPulseBetaChecks.cs` and `Tools/SkyPulseWorldExpansionChecks.cs` into its `Assets/Editor`. Set `SKYPULSE_QA_OUTPUT` to an output directory. Launch Unity 6000.6.0f1 with `-batchmode -projectPath <copy> -executeMethod SkyPulseWorldExpansionChecks.Run -logFile <log>` (no `-quit`; harness exits after play-mode checks). For a read-only compile/import/scene check, copy `Tools/SkyPulseAuditValidation.cs` into the disposable Editor directory and use `-batchmode -nographics -quit -executeMethod SkyPulseAuditValidation.Run`.

## Required before release

No new Xcode export/archive, physical-device run or ASC validation was performed for these changes. PlayerSettings remains build 6; choose a verified unused number for the new candidate. Follow the audit's eight manual checks, especially every late world boundary, full-route wrap, old-save upgrade compatibility, texture memory and safe-area guide navigation on iPhone/iPad. Use a fresh export destination and preserve previous archives.

## Draft App Store copy — only for the newly tested world candidate

Name: SkyPulse Arcade

Subtitle: Birds, Crystals & Tech Tree

Promotional text: Build your flock of 15 mechanical birds. Collect crystals, develop three Tech Tree branches and chase your next milestone through a 12-world flight route.

Description:

Build your flock. Develop your Tech Tree. Explore your next world.

SkyPulse Arcade combines tap-to-fly runs with a persistent collection of 15 mechanical birds and crystal-powered progression. Browse your Bird Hangar, unlock cosmetic designs with crystals earned in play and choose the bird for your next run. Every bird shares the same handling.

Develop Collection, Recovery and Mastery through nine Tech Tree nodes with three levels each. Inspect each effect and prerequisite before spending your earned crystals.

Begin in Neon City and reach a new environment every 15 gates. The route continues through Aurora Rise, Solar Drift, Midnight Tide, Velvet Dawn, Crystal Night, Jade Horizon, Violet Rain, Eclipse, Cobalt Storm, Amber Skies and Polar Glow, then repeats. Open Worlds & Power-ups on Home to preview every environment and its score milestone.

Collect Aegis for one-collision protection, Time Pulse for four seconds of slower flight or Crystal Magnet to attract crystals for six seconds. Earn crystals during flight and build your saved collection between attempts.

Play offline in portrait on iPhone and iPad. No account, advertising or real-money purchases. Scores, crystals, birds, upgrade levels and preferences save locally on your device. Reduced Motion and haptic controls are available.

Reviewer walkthrough addition (append to candidate notes, do not mislabel as build 6):

“From Home open Worlds & Power-ups and use Next/Previous to inspect all four pages of the 12-world route. Every world is visible in the guide without a purchase. In gameplay, score milestones are 0, 15, 30, 45, 60, 75, 90, 105, 120, 135, 150 and 165; the route repeats from Neon City at 180. The submitted candidate must include this expansion before these notes are used.”

The root audit provides the factual custom-system response to Apple's 4.3(a) concern. Do not claim the extra worlds establish independent ownership, that Apple approved the audit, or that all images were hand-drawn. Retain source/licence records for supplied art and reference inputs.
