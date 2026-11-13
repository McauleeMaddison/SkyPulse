# Editing SkyPulse

Open **`mobile/` in Unity Hub** and the repository in VS Code. Play
`Assets/Scenes/SkyPulse.unity`. The installed iPhone is still **Build 21**; the
[current local review](Development/Current/README.md) describes later source.
Public App Review is on hold. Keep the approved tap physics and wing motion fixed.

## Edit in Unity

| Work | Canonical scene/folder under `mobile/` |
| --- | --- |
| Complete game | `Assets/Scenes/SkyPulse.unity` |
| Civic/residential islands and buildings | `Assets/Editor/SkyPulseBackgroundWorkshop/Scenes` |
| Distant world atmosphere | `Assets/Editor/SkyPulseBackgroundWorkshop/Distant Scenes` |
| Relay structures, planets and skybridges | `Assets/Editor/SkyPulseBackgroundWorkshop/Pulse Relay World/Scenes/Pulse Relay Orbital World.unity` |
| Twelve pipe structures | `Assets/Editor/SkyPulsePipeWorkshop/Scenes` |
| Editable bird anatomy and source models | `Assets/Editor/SkyPulseBirdWorkshop` and `Assets/Editor/SkyPulseBirdRosterWorkshop` |

Workshop Play inspects a model. Return to `SkyPulse.unity` for gameplay. Keep saved
prefabs, material/mesh edits and `.meta` GUIDs. Do not regenerate a workshop over
manual changes. Generated `Resources/SkyPulse/Birds3D` assets are runtime outputs;
edit the canonical workshop model and use the existing validated bake workflow.
The historical word `Prototype` in a rig class name is retained for serialized
compatibility; it now also serves the accepted native roster.

## Edit in VS Code

All paths below are inside `Assets/Scripts/` unless stated otherwise.

| Change | File |
| --- | --- |
| Catalogue, lift, pipe collisions and run lifecycle | `SkyPulseNativeGame.cs` |
| Mode dispatch, Relay rules integration and wallet journal | `SkyPulseNativeGame.Missions.cs` |
| Existing four daily Relay courses | `SkyPulseDailyRelay.cs` |
| Blackout rules, eight authored story vaults and v3 save | `SkyPulseBlackoutCampaign.cs` |
| Four optional authored daily cases | `SkyPulseBlackoutCasebook.cs` |
| Blackout layout and touch bindings | `SkyPulseNativeGame.Blackout.cs` |
| Physical prisms, beams and fixed 49-module pool | `SkyPulseHeistBoard.cs` |
| Panoramic city arrangement and connected bridges | `SkyPulseRestorationDisplay.cs` |
| Eight reward names and saved lighting group binding | `SkyPulseWorldRestoration.cs` |
| Reward appearance | `Resources/SkyPulse/Shaders/FlightArchitecture.shader` |
| Home next-goal card, case launch and results | `SkyPulseNativeGame.Journey.cs` |
| Cosmetic mastery thresholds and daily case save | `SkyPulsePlayerJourney.cs` |
| Native equipment selection and flight pose dispatch | `SkyPulseNativeGame.SolidBirds.cs` |
| Approved wing stroke | `SkyPulseNeonFinchPrototype.cs` |
| Swipe Hangar layout | `SkyPulseNativeGame.Hangar.cs` |
| Native display lighting/cradle | `SkyPulseSolidBirdStudio.cs` |
| Bird traits, training, Core prices and breeding | `SkyPulseBirdProgression.cs`, `SkyPulseNativeGame.BirdProgression.cs` |
| Seven pickup rewards | `SkyPulseNativeGame.FieldTech.cs`, `SkyPulsePickupDisplay.cs` |
| How to Play | `SkyPulseNativeGame.Guide.cs` |

Files beginning `SkyPulseNativeGame.` are parts of one component. Do not attach
another game component for each partial file. VS Code nests them under the main
file. They remain at their established paths to preserve tooling and GUIDs.

## Validate

Read-only checks from the repository root:

```sh
python3 mobile/Tools/check_player_copy.py
python3 mobile/Tools/verify_bird_registration.py
```

Unity helpers in `Tools/` are copied to an isolated QA product before execution.
Some intentionally clear **that QA product's** preferences; never copy/run those
entry points against the owner product. `SkyPulseCatalogChecks` verifies twelve
real Tech families and negative controls. `SkyPulseRestorationChecks` and
`SkyPulseJourneyChecks` cover lighting, authored cases, progression, saves and UI.
The current review records which checks ran and what still needs device testing.

The release metadata checker follows the installed Build 21 contract. Later local
source intentionally differs from that immutable build; create a fresh numbered
release contract when an approved candidate is actually built and installed.
Local source checks cannot prove worldwide metadata uniqueness, exclusive AI art
rights, device performance or future Apple acceptance.

## Historical files and cleanup

Use `Development/Archive` for older previews. They are not the current design.
`artifacts/device-build21` retains the installed build and private save backups;
`artifacts/rollback-build20` retains a verified signed rollback; `artifacts/Historical`
retains older archives/logs and selected raw-frame evidence. The exact cleanup
allowlist and moved-folder map are in `Development/Current/CLEANUP_REPORT.json`.
Active source, original images, individual crash/unlock frames and editable Unity
structures were preserved. Do not publish private save exports.
