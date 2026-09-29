# SkyPulse Apple Guideline 4.3(a) Audit

Audit date: 29 September 2026. Baseline: `edaf03da16a0c2f44f8ce67c953e3005283fbfc7`. Unity project: `mobile/` (not repository root).

## Executive Summary

**No identifiable imported game template, unrelated application identity, or accidentally included demo scene was found in the inspected Unity project. This does not establish uniqueness against Apple's private comparison corpus or resolve the rejection.** The user's newly supplied rejection specifically mentions similarity to apps submitted by a terminated developer account. Local source inspection cannot verify or disprove that account relationship, identify Apple's comparison app, or prove ownership of all inputs.

**MEDIUM — potential concept/presentation similarity remains:** the baseline Home/game loop still uses tap-to-fly bird-and-pipe obstacle play (`CreateHomeScreen`, `UpdatePipes`). This familiar concept may matter to review, but no comparison app or causal evidence was provided. The existing Hangar, earned-crystal Tech Tree, persistent collection, route and tactical pickups are the verifiable differentiators; additional art alone is not proof of a distinct binary or independent provenance. No gameplay redesign was made to speculate about Apple's detection.

The initial audit was read-only. It covered all 161 non-meta files in `mobile/Assets` (139 PNGs, five WAVs, three JSONs, 12 C# files, one scene and one build profile), manifests/lockfile, ProjectSettings, release scripts, relevant generated final-build Xcode/IL2CPP files and the preserved build 6 archive. Asset GUIDs, exact duplicate hashes, resource keys, source references and archived scene/resource strings were checked. Build artifacts, package caches and the separate website were distinguished from player source. No external source-code plagiarism comparison was performed.

Technical concern levels measure evidence/release concerns, not probability of Apple approval:

1. **HIGH — unresolved Apple account-related similarity allegation; provenance remediation in progress.** No local smoking gun explains the terminated-account similarity statement. Retain development history and establish origins/rights for artwork, audio, references and any original code contributions before claiming independent authorship. Several art-generation notes exist; they are not a complete rights ledger. **Follow-up:** a [167-file evidence ledger and development-history packet](mobile/Release/Provenance/START_HERE.md) now records current hashes, Git path history, existing generation notes and the exact match of all five sounds to the earlier SkyPulse prototype. The owner now confirms SkyPulse artwork was generated in ARTA and asserts ownership of code developed using VS Code. Specific repository notes also record later built-in-tool image generation; those records remain intact. ARTA’s published commercial-use terms have now been checked. Five audio resources have been replaced with effects generated from retained source, preserving original files outside Assets. Original artwork generation/reference records and account history remain incomplete; see [remediation status](mobile/Release/Provenance/REMEDIATION_STATUS.md). Supplied Pups portfolio cards provide creative background rather than direct SkyPulse generation records. This narrows the documentation gap without resolving Apple’s allegation.
2. **MEDIUM — release evidence can describe the wrong version.** Historical metadata names Acid Foundry/Orbital Bazaar and top-left pause. Build 6 actually uses Neon City/Aurora Rise/Solar Drift and top-right pause. September 16 screenshots/notes and uploaded build state are historical, not a current ASC verification. The subsequently requested world expansion will require a newly built/tested candidate and matching evidence.
3. **MEDIUM — extra world content shipped but was not normally reachable.** Nine world definitions/backgrounds existed, but `WorldIndexForScore` cycled only three. Six extra background names occur in archived `Data/resources.assets`. They have production code and saved-world references, so they were not safe demo-deletion candidates. The user subsequently explicitly requested the supplied world images be added alongside existing worlds; see Changes Applied.
4. **LOW — legacy SkyPulse code and identifiers.** Old bird aliases, older world-name comments, unused-looking Hangar UI helpers and internal Beta/QA names exist. They are project history, not evidence of a different developer's app. Preserve save migrations and legitimate test tools.
5. **LOW — normal Unity binary/framework overlap.** Unity frameworks, generated Xcode test target, TextMeshPro/uGUI and engine strings are present. No third-party game-framework package was identified. Do not rename engine identifiers or remove Unity components to alter a binary fingerprint.

Apple's current public [4.3 guidance](https://developer.apple.com/app-store/review/guidelines/#spam) addresses duplicate app submissions and indistinguishable offerings. Its public wording does not disclose how Apple selected the comparison in this rejection. This report treats the supplied rejection as case-specific evidence, not a finding that any particular file caused it.

## Current Git State

At entry:

- Branch: `codex/build-6-review-recovery-20260916`, tracking the corresponding origin branch; no ahead/behind count in status.
- Latest commit: `edaf03da16a0c2f44f8ce67c953e3005283fbfc7`, “Record device sign-off, build 6 upload and simulator service crash,” 16 September 2026.
- Modified/staged files: none. Untracked non-ignored files: none. Ignored build artifacts exist and were retained.
- No reset, clean, checkout-overwrite or stash was used; there were no user changes requiring preservation.
- After completing initial inspection, created `codex/4-3a-audit-worlds-20260929` for this audit and the user's later world-addition request.
- Existing rollback points retained: `skypulse-build-5-preserved-20260916`, `skypulse-build-6-candidate-20260916`; `artifacts/skypulse-build-5-preserved-20260916.bundle`; preserved build 5 archive under `artifacts/release-2026-09-08/`; final build 6 ZIP under `artifacts/build-6-review-recovery/`.
- Initial gameplay SHA256: `f80a9691940ac2b3954b408778e1200fafd50678984f6670b564d89b24c18507`, matching the final build 6 export's evidence.
- `git log` shows incremental native-game development from `7fb3e4b` (“Add Unity mobile game foundation,” 5 August 2026) through later gameplay, progression and visual changes. This supports traceability, not independent proof of authorship.

## Unity Packages

Sources: `mobile/Packages/manifest.json`, `mobile/Packages/packages-lock.json`. All are **A: normal Unity packages**. **B: no non-Unity dependency found. C: no game-template/starter/tutorial package found.** No scoped registry, Git dependency or local third-party package is declared. Registry package resolves to `https://packages.unity.com`.

| Dependency | Version | Scope/source | Use / action |
|---|---|---|---|
| `com.unity.2d.sprite` | 1.0.0 | Direct / builtin | Sprite authoring/import tooling; editor tests stay in package cache — KEEP (LOW) |
| `com.unity.device-simulator.devices` | 1.0.1 | Direct / registry | Unity editor device definitions; no player assembly — KEEP (LOW) |
| `com.unity.ugui` | 2.6.0 | Direct / builtin | Production Canvas, Text, Button, ScrollRect, EventSystem; includes TextMeshPro — KEEP (LOW) |
| `com.unity.modules.adaptiveperformance` | 1.0.0 | Direct / builtin | Unity engine/dependency module; direct gameplay need not established — KEEP (LOW) |
| `com.unity.modules.animation` | 1.0.0 | Transitive / builtin | Unity engine/dependency module; direct gameplay need not established — KEEP (LOW) |
| `com.unity.modules.audio` | 1.0.0 | Direct / builtin | AudioSource / five production sound effects — KEEP (LOW) |
| `com.unity.modules.hierarchycore` | 1.0.0 | Transitive / builtin | Unity engine/dependency module; direct gameplay need not established — KEEP (LOW) |
| `com.unity.modules.imageconversion` | 1.0.0 | Direct / builtin | Texture support / editor capture workflows — KEEP (LOW) |
| `com.unity.modules.imgui` | 1.0.0 | Direct / builtin | uGUI/editor dependency — KEEP (LOW) |
| `com.unity.modules.jsonserialize` | 1.0.0 | Direct / builtin | JsonUtility for public links and bird frame data — KEEP (LOW) |
| `com.unity.modules.physics` | 1.0.0 | Direct / builtin | uGUI dependency; do not delete based on lack of custom 3D gameplay — KEEP (LOW) |
| `com.unity.modules.physics2d` | 1.0.0 | Direct / builtin | Collider2D / BoxCollider2D collision checks — KEEP (LOW) |
| `com.unity.modules.physicscore2d` | 1.0.0 | Direct / builtin | Physics2D dependency — KEEP (LOW) |
| `com.unity.modules.subsystems` | 1.0.0 | Transitive / builtin | Unity engine/dependency module; direct gameplay need not established — KEEP (LOW) |
| `com.unity.modules.tetgen` | 1.0.0 | Direct / builtin | Unity engine/dependency module; direct gameplay need not established — KEEP (LOW) |
| `com.unity.modules.timelinefoundation` | 1.0.0 | Direct / builtin | Unity engine/dependency module; direct gameplay need not established — KEEP (LOW) |
| `com.unity.modules.ui` | 1.0.0 | Transitive / builtin | uGUI dependency — KEEP (LOW) |
| `com.unity.modules.uielements` | 1.0.0 | Transitive / builtin | Unity engine/dependency module; direct gameplay need not established — KEEP (LOW) |
| `com.unity.modules.vectorgraphics` | 1.0.0 | Direct / builtin | Unity engine/dependency module; direct gameplay need not established — KEEP (LOW) |

There are 14 direct and 19 total resolved entries. `com.unity.ugui@23caec89ae27/package.json` confirms its TMP content. Tests and documentation in `mobile/Library/PackageCache/com.unity.2d.sprite@947350d67f82`, `com.unity.ugui@23caec89ae27`, and `com.unity.device-simulator.devices@2af643ceec84` are normal package material. They are not imported demo games. Archived `Data/ScriptingAssemblies.json` lists engine assemblies, Unity UI/TMP and `Assembly-CSharp`; no test assembly was listed. Presence in that list does not prove every engine method survives native linking. No package removals recommended to address 4.3(a).

## Third-Party Assets

Identifiable vendors are Unity and operating-system font providers. Other art/audio origins are only partially documented; “unknown” does not mean stolen or third-party.

| Item / exact location | Purpose / used? | Demo content / shipped? | Action / concern |
|---|---|---|---|
| Unity packages listed above; `mobile/Library/PackageCache/` | Engine/UI/import/editor tooling; production engine/UI used | Package tests/docs exist; no test assembly or demo scene found in archived player | KEEP — LOW; retain normal licences/attribution |
| OS `Avenir Next`, `Arial`; Unity `LegacyRuntime.ttf`, requested in `SkyPulseNativeGame.CreateInterface` | Runtime font with fallbacks; no separately imported font file under Assets | No demo font collection under Assets | KEEP — LOW |
| `mobile/Assets/Resources/SkyPulse/characters/roster/` (120 PNGs) | 15 birds × six flight frames plus hit/unlock poses; production referenced | No demo folders; player resources | VERIFY LICENCE — MEDIUM: complete provenance for source/reference inputs; keep all frames |
| `mobile/Assets/Branding/SkyPulseAppIcon.png` | App icon, all configured icon slots | No demo; archived app has icon catalogue | KEEP / VERIFY LICENCE — MEDIUM: generation note in `mobile/Release/ICON.md`; reference bird rights still need records |
| `mobile/Assets/Resources/SkyPulse/backgrounds/` (nine PNGs at baseline) | Three route worlds, six legacy/catalogue worlds | No third-party demo identified; extra names found in archived resources | INVESTIGATE / VERIFY LICENCE — MEDIUM; three active backgrounds documented in `mobile/Release/NeonVisualUpdate/BACKGROUND_ART_PROVENANCE.md` |
| `mobile/Assets/Resources/SkyPulse/art/pipes/` | Replacement pipe art used in gameplay | No demos; production paths | KEEP / VERIFY LICENCE — LOW/MEDIUM; generation record in `PIPE_ART_PROVENANCE.md`, incomplete original prompt retention |
| `mobile/Assets/Resources/PipeBody.png`, `PipeCap.png`, `PipeGlow.png` | Runtime-loaded original pipe art/fallback; cap dimensions preserve collider sizing | Production resources, not safe unused-demo candidates | KEEP / VERIFY LICENCE — MEDIUM provenance gap; deleting cap can affect collisions |
| `mobile/Assets/Resources/SkyPulse/art/powerups/generated/` | Crystal and three tactical pickups | Four production textures, no demos | KEEP / VERIFY LICENCE — MEDIUM; crystal documented in `CRYSTAL_ART_PROVENANCE.md`, complete pickup records absent |
| `mobile/Assets/Resources/SkyPulse/audio/{flap,score,crash,crystal,unlock}.wav` | Five explicitly loaded and used sound effects | No samples/demos identified; production resources | KEEP / VERIFY LICENCE — MEDIUM: no complete source/licence ledger found |
| `public-site/package.json`, `public-site/app/` | Separate React/Vinext/Sites support website and its dependencies | Generic website scaffold name `sites-project`; outside Unity Assets, not shipped in iOS player | KEEP — LOW binary relevance; support wording needs separate publishing review |

No identifiable purchased Asset Store game/template or third-party C# gameplay framework was found. Generated artwork notes are development evidence, not legal certification of rights in every supplied reference. New user-supplied worlds are recorded separately in `mobile/Release/WorldExpansion/ART_PROVENANCE.md`.

## Demo / Sample / Template Content

**LOW — no matching demo/sample/tutorial/starter/test/showcase/documentation paths found inside baseline `mobile/Assets`.** One scene, no prefabs/models/custom shader files, no embedded native plugins and no additional project asmdefs were found there. Exact SHA256 grouping of all 161 non-meta assets found **zero byte-identical duplicate groups**. This is not a perceptual similarity comparison: animation frames and themed art can be visually related without identical hashes.

`mobile/Tools/SkyPulseBetaChecks.cs`, `SkyPulseBuild6Checks.cs`, `SkyPulseBetaScreens.cs`, and other capture helpers are project-owned QA tools outside Assets. Unity does not compile these into the normal player. Several intentionally seed/reset QA saves; never copy them into production Assets or run them against a real save. `SkyPulseBuild6Checks.Run` explicitly requires a QA product name. `artifacts/.../qa` and generated `mobile/Builds/` exports are separate ignored outputs, not player input.

Generated `Unity-iPhone Tests` in the Xcode export is Unity's native test target, not a game demo. No `.xctest` or demo/test paths were found in the final archive inventory. Do not delete cache tests or generated targets to manipulate similarity.

## Old or Unexpected Identifiers

| Concern | Exact location / finding | Classification / reasoning |
|---|---|---|
| LOW | `SkyPulseNativeGame.cs`: `web beta` in summary comment; editor `SkyPulseReleaseBuild.cs` default destination `Builds/iOS-beta-` | KEEP; comments/export folder are not player-facing Beta branding |
| LOW | `SkyPulseNativeGame.LoadProgress`: comment says starting bird “Nova”; `ConfigurePipe`: comments refer to Foundry/Bazaar | CLEAN UP comments only; historical SkyPulse vocabulary, not proof of another app |
| LOW | `MigrateRosterSkinId`: `volt`, `steel`, `prism`, `cinder`, `verdant`; `newbird01`–`newbird10` current IDs | KEEP; aliases protect existing ownership and IDs persist in saves; broad rename is unsafe |
| LOW | `BirdHangarProfile` has Speed/Maneuverability/Stability and descriptions | INVESTIGATE MANUALLY; these are not separate live flight stats. Shared `EndlessTuning` governs every bird. Do not promise bird-specific handling in review evidence |
| LOW | `Assets/Settings/Build Profiles/New iOS Profile.asset` | KEEP; generic editor profile name, global scene list inherited, no player identity override |
| LOW | Empty `templatePackageId` / `templateDefaultScene` and standard `APPLICATION:Default` WebGL setting | KEEP; normal Unity defaults, not a game-template identifier |
| LOW | `SkyPulsePublicLinks.IsPublicHttpsUrl`: `example.com`, `.invalid`, `.local` | KEEP; these are rejected URL patterns, not configured public endpoints |
| MEDIUM | `mobile/Release/APP_STORE_COPY.md`, `APP_REVIEW_RESPONSE.md`, `SUPPORT_PAGE_DRAFT.md`, `NeonVisualUpdate/START_HERE.md` | Historical world/control copy can be pasted accidentally. Use candidate-specific documents; keep history but mark supersession in release planning |
| LOW | `public-site/package.json`: `sites-project`; framework/template terminology in website dependencies | Separate site scaffold, not Unity binary residue; no need to rename generic dependencies |

All seven production C# files use `SkyPulse.Mobile`; five editor files use `SkyPulse.Mobile.Editor`. No unrelated game/developer namespace was found. Unity/Apple names and standard library/service names are expected. Those historical comments were corrected during this work, while actual compatibility aliases were preserved. No evidence found that “Nova”, “Foundry” or “Bazaar” identifies a separate published product; do not present that inference as fact.

## PlayerSettings and Bundle Configuration

`mobile/ProjectSettings/ProjectSettings.asset` and `mobile/Assets/Editor/SkyPulseReleaseBuild.cs` agree on SkyPulse company/product and app identifier `com.mcauleemaddison.skypulse`. Version `1.0.0`, iOS build `6` at baseline. Team `TX54JLQ8VF`; automatic signing; no manually specified provisioning profile. iPhoneAndiPad, portrait, minimum iOS 15.0, IL2CPP. `stripEngineCode: 1`; no deliberate custom stripping-level override. Target SDK enum is Unity's platform setting, not another application ID.

The icon GUID resolves to `Assets/Branding/SkyPulseAppIcon.png`. Unity splash/logo are enabled with no custom splash logos; **LOW — normal engine branding, not evidence of a template**. Do not alter splash/licensing/signing to hide engine origin. `UnityConnectSettings.asset` has Analytics/Ads/Purchasing/Crash Reporting disabled; engine URLs remain defaults, not another developer's service integration.

Display name “SkyPulse”, main title “SKYPULSE / ARCADE”, and historical ASC name “SkyPulse Arcade” consistently identify the product. No unrelated application identifier in authored player settings was found.

## Apple / iOS Configuration

Examined `mobile/Assets/Editor/SkyPulseIosPostprocess.cs`, `SkyPulseReleaseBuild.cs`, and final device export `mobile/Builds/iOS-device-universal-6-final/` including `SkyPulse-Info.plist`, `PrivacyInfo.xcprivacy`, `Unity-iPhone.xcodeproj/project.pbxproj`, runtime initializers, assembly list and IL2CPP output.

- Postprocessor explicitly sets display name SkyPulse, non-exempt-encryption false and privacy declarations including UserDefaults reason `CA92.1`; renames the source plist for the export's macOS folder handling. It preserves Unity manifest entries.
- No authored StoreKit configuration, entitlements file, associated-domain integration or custom URL scheme found. `iOSURLSchemes` and `macOSURLSchemes` are empty. No ad/IAP SDK in manifests.
- Final archive app plist independently confirms `com.mcauleemaddison.skypulse`, SkyPulse, 1.0.0 (6), device families `[1,2]`, portrait, arm64/Metal and minimum 15.0.
- **Additional bundle identifiers ARE present:** `com.unity3d.framework` for UnityFramework; `com.unity.UnityRuntime` for UnityRuntime; generated test target uses `com.unity3d.${PRODUCT_NAME:rfc1034identifier}`; dSYM metadata uses `com.apple.xcode.dsym...`. These belong to engine/test/debug artifacts, not a second game. KEEP.
- Generated PBX capability `com.apple.GameControllers.appletvos` is enabled; no corresponding custom authored capability integration was found. **LOW — INVESTIGATE only if it affects future export validation; do not reset signing or edit generated exports speculatively.**
- Archive inventory contains UnityFramework and UnityRuntime, not an unknown vendor SDK. AppleDouble `._` entries exist in the archive ZIP; these are backup metadata, not a demo game. The ZIP is a preserved archive, not the file uploaded as an IPA.
- Entitlements boundary: a `codesign -d --entitlements` read on the executable extracted alone produced an invalid-blob warning, so this audit does **not** assert verified effective distribution entitlements. Recheck the full newly exported distribution app at release time. Historical September 16 Apple validation/upload succeeded; that is not a fresh validation of future changes.

No Bundle ID, Team ID, developer account, signing credential or provisioning setting was changed by the audit.

## Build Scenes

Exact configured list (`mobile/ProjectSettings/EditorBuildSettings.asset`):

| Enabled | Scene | GUID |
|---|---|---|
| Yes | `Assets/Scenes/SkyPulse.unity` | `5a5fa0c29c5a4ced948ef3d8f0b3e002` |

`SkyPulseReleaseBuild.Scene` and `BuildPipeline.BuildPlayer` use the same single scene. `New iOS Profile.asset` has `m_OverrideGlobalSceneList: 0`; no private scene override. This is the only `.unity` asset in the project. Archived `Data/globalgamemanagers` contains `Assets/Scenes/SkyPulse.unity`, and archive has only `Data/level0` as a scene payload.

The scene itself has no authored gameplay component dependency: runtime initialization in `SkyPulseBootstrap.StartSkyPulse` creates `SkyPulseNativeGame`. Thus a scene-only GUID scan is insufficient for asset usage; dynamic Resources/catalogue references were checked separately. **No demo or sample scene was found in the actual archived build.**

## Resources / StreamingAssets / Addressables

Baseline `Assets/Resources` contains 146 non-meta assets, 52,263,917 source bytes (not installed size). Every resource key appears literally in runtime source, including data/catalogue entries; a textual reference alone does not prove normal player reachability. No StreamingAssets or Addressables configuration/package was found.

Unity includes Resources assets even without scene references; see [Unity resource-loading documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/LoadingResourcesatRuntime.html). The archive contains `resources.assets`, resource data and the following six legacy world names: `midnight-tide-v2`, `velvet-dawn-v2`, `crystal-night-v2`, `jade-horizon-v2`, `violet-rain-v2`, `eclipse-v2`. Names in serialized resources support inclusion; this was not a full object-level extraction of every texture.

Those six backgrounds are referenced by `Worlds`, old collection branches and `LoadProgress` saved-world lookup. **MEDIUM — extra shipped content; INVESTIGATE, not REMOVE IF UNUSED.** User's subsequent request now makes the existing world catalogue intended route content. Original pipe resources remain loaded and preserve geometry/fallbacks. `bird-pose-bounds.json` and `bird-frame-registration.json` are runtime data, not development leftovers.

After the requested expansion, the six legacy worlds are reachable, and the two explicitly replaced original PNGs are preserved outside Resources. Current resource count is 149. No blindly removable third-party Resources sample was found. Exact byte duplicate scan returned zero groups across baseline Assets. Historical build/QA copies elsewhere in the repository are intentional evidence, not duplicates packaged by this Unity project.

## Suspicious or Unused Code

| Code / exact file | Classification | Evidence / concern |
|---|---|---|
| `CreateHangarStatRow`, `CreateHangarRatingRow`, `CreateBirdHangarCard`, `CreateBirdHangarDetailPanel`, `SetBirdHangarContentHeight`, `AllowsPowerUps` in `SkyPulseNativeGame.cs` | REMOVE IF UNUSED (recommendation only) | LOW; each had only its declaration in authored C# scan. Several Hangar helpers also exist in final IL2CPP source. This proves generated code presence, not final linker retention. Preserve pending complete reflection/UI-path validation; not third-party samples |
| `BirdHangarProfile` rating fields | INVESTIGATE MANUALLY | LOW; obsolete-looking presentation data, not actual per-bird flight tuning; user explicitly asked to preserve bird systems |
| `CosmeticCategory.Worlds/Pipes`, `EquipWorld`, `EquipPipe`, `CreateCosmeticCard` | INVESTIGATE MANUALLY | LOW; current UI exposes Hangar/Tech, but old catalogue/save paths remain. Do not mass-delete |
| `MigrateRosterSkinId`, upgrade aliases in `LoadProgress`, `ownedUpgradeIds` | KEEP | Required compatibility with existing saves; deleting because names look old risks progress loss |
| `OnApplicationFocus`, `OnApplicationPause`, `Awake`, `Update`, bootstrap initializer | KEEP | Unity lifecycle/reflection entry points; lack of ordinary C# callers is expected |
| `CreateEmergencyBirdSprite`, fallback pipe/art loaders | KEEP | Missing-art recovery and geometry dependencies; not template leftovers |
| `UpdateDevelopmentQualityControls`, collision diagnostics | KEEP | Project-owned diagnostics; check compilation guards, not broad removal |
| `Assets/Editor/SkyPulsePlaytestChecklist.cs`, import/art tools and postprocessor | KEEP | Editor tooling; isolated Unity player-assembly inspection lists only seven runtime scripts |
| `Tools/*Checks.cs`, `Tools/*Capture.cs`, `Tools/*Screens.cs` | KEEP | Legitimate QA outside Assets; no production player inclusion |

There is one substantial custom runtime implementation, not two competing imported gameplay frameworks. Generic class/type names and shared Unity boilerplate are not grounds for renaming.

## SkyPulse Original Systems

“Original systems” here means SkyPulse-specific implementation found in this repository; it is not a legal originality certification. Main implementation is `mobile/Assets/Scripts/SkyPulseNativeGame.cs` (`SkyPulse.Mobile.SkyPulseNativeGame`).

| System | Actual symbols / supporting files |
|---|---|
| Bootstrap | `SkyPulseBootstrap.cs`: `StartSkyPulse`; runtime creates native game |
| Bird roster / selection / purchase | `Skins` (15 entries), `SelectSkin`, `EquipSkin`, `ConfirmPurchase`, `IsSkinOwned`; `Resources/SkyPulse/characters/roster/` |
| Bird Hangar / swipe / reveal | `BuildBirdHangarPage`, `ShowPreviousHangarBird`, `ShowNextHangarBird`, `OnHangarBeginDrag/Drag/EndDrag`, `AnimateHangarSwipe`, `ShowUnlockReveal` |
| Bird visual motion / stats | `LoadFlapFrameSequence`, `UpdateGameplayWingState`, `LoadRegisteredFlapFrame`, `LoadPresentationPose`; frame-registration and pose-bounds JSON. Rarity/price/ownership differ; actual flight physics shared. No claim of performance-stat progression |
| Tech Tree | `Upgrades`, `BuildTechTree`, `CreateTechMapNode`, `InspectTechNode`, `IsUpgradePrerequisiteMet`, `GetUpgradeLevel`; `SkyPulseTechConnection.cs` draws connections |
| Upgrade IDs / persistent effects | Collection: `crystal_resonator`, `prism_conduit`, `gravity_well`; Recovery: `salvage_codec`, `recovery_cache`, `archive_engine`; Mastery: `precision_harvester`, `streak_capacitor`, `apex_matrix`. Nine × three levels, preceding-node level 2 gates later nodes |
| Saves / migrations | `LoadProgress`, `SaveProgress`, `MigrateRosterSkinId`, `UpgradeAliasLevel`; `skypulse.native.*` PlayerPrefs, owned birds/levels/crystals/best/world/preferences |
| Crystal economy | `ConfigureGateCrystals`, `ConfigureCrystalPickup`, `CollectCrystalPickup`, `BankCollectedCrystals`, `ApplyResultTechBonuses`; pickup value/reach and result reward helpers |
| Tactical power-ups | `PowerUpKind`, `ConfigureRoutePowerUp`, `CollectPowerUp`, `UseShield`, `UpdatePowerUpEffects`; Aegis, Time Pulse (4s), Crystal Magnet (6s) |
| Worlds / progression | `Worlds`, `WorldIndexForScore`, `BeginWorldTransition`, `UpdateWorldTransition`, `RecordFarthestWorld`, `RouteWorldName`, `CreateFlightGuideScreen` |
| Flight physics | `FlightTuning`, `EndlessTuning`, `SimulateFlight`, `BufferFlapInput`, `ConsumeBufferedFlap`, `ActiveGravity`, `ActiveFlapVelocity` |
| Obstacles / collision | `CreatePipePair`, `ConfigurePipe`, `LayoutPipePair`, `UpdateRouteGateMotion`, `BirdCollidesWithPipe`, `CollidersOverlap`, bird/pipe collider layout |
| Scoring / difficulty | `UpdatePipes`, `RouteSpeedFraction`, `RouteGapFraction`, `RouteMaximumCenterStep`, `RouteDriftFraction`, perfect-pass rewards |
| Main UI / safe areas | `CreateInterface`, `CreateHomeScreen`, `ApplySafeArea`, `FitInterfaceToSafeArea`, `RefreshScreens`; `SkyPulseUiGlyph.cs`, `SkyPulseButtonFeedback.cs`, `SkyPulseRoundStartSurface.cs` |
| Game Over / Retry / pause | `EndFlight`, `UpdateImpactTumble`, `CreateGameOverScreen`, `RestartFlight`, `PauseFlight`, `ResumeFlight` |
| Privacy/support links | `SkyPulsePublicLinks.cs`, `Resources/SkyPulsePublicLinks.json`, `CreatePrivacyScreen` |

## Changes Applied

**Subsequent provenance remediation:** five WAVs replaced by deterministic source-generated effects, old WAVs/metadata preserved in `mobile/Release/Provenance/PreviousAudio/`, ARTA published commercial-use terms checked, and source ledger/owner statement/reviewer draft added. Unity import and decoding passed for all five effects. See [remediation status](mobile/Release/Provenance/REMEDIATION_STATUS.md). This is an additional source change requiring a new candidate; historical build 6 is unchanged.


The initial audit found no third-party demo deletion justified. After the baseline inspection and successful isolated compile, applied only the following audit cleanup and separately user-authorized world changes:

- Created this report, including all package dependencies, file-specific findings and a factual Apple reply draft.
- Corrected historical Nova/Foundry/Bazaar comments in `mobile/Assets/Scripts/SkyPulseNativeGame.cs`; no aliases/save IDs changed.
- Marked `mobile/Release/APP_STORE_COPY.md` and `APP_REVIEW_RESPONSE.md` as historical to prevent accidental reuse. Added a dated update to `mobile/Release/REVIEW_STATUS.md` and updated `README.md` to distinguish current source from the preserved build 6.
- Implemented the explicitly requested world addition in `SkyPulseNativeGame.cs`: all nine existing world IDs retain order, three new IDs appended (Cobalt Storm, Amber Skies, Polar Glow), supplied art used for the existing Crystal Night and Eclipse. Twelve worlds are now reachable at successive 15-gate milestones; full route wraps at 180. Home/HUD/results/save bounds and the four-page guide agree.
- Added five byte-identical supplied JPEGs and five unique GUID `.meta` files under `mobile/Assets/Resources/SkyPulse/backgrounds/themes/`: `amber-skies-user-20260929`, `cobalt-storm-user-20260929`, `crystal-night-user-20260929`, `eclipse-user-20260929`, `polar-glow-user-20260929` (all `.jpeg`).
- After verifying no remaining production path/GUID references, moved `crystal-night-v2.png`, `eclipse-v2.png` and their `.meta` files to `mobile/Release/WorldExpansion/PreviousArtwork/`. **No artwork bytes were deleted**; exact hashes match Git baseline. They no longer enter future players through Resources. This does not alter the archived/uploaded build 6.
- Added `mobile/Release/WorldExpansion/ART_PROVENANCE.md` and `RELEASE_NOTES.md` with hashes, route, test details, remaining device work and candidate-specific draft metadata.
- Added `mobile/Tools/SkyPulseAuditValidation.cs` and `SkyPulseWorldExpansionChecks.cs`, outside shipping Assets. Logs/captures are ignored local artifacts under `artifacts/apple-43a-audit-20260929/`.

No packages/scenes were removed. No App Store edit, submission or message was sent during this audit. No build number was guessed: PlayerSettings still says 6, and the new source is explicitly **not** the existing uploaded build 6. A newly numbered, tested candidate is required before publishing expanded-world copy.

## Items Deliberately Left Unchanged

Bundle ID, Team ID, signing/provisioning, build 5/archive, uploaded build 6, branch rollback points, existing save keys, upgrade IDs, bird ownership/migration, flight tuning, collider geometry, score/difficulty curves, pickup timing, crystal economy and legitimate QA tools. No engine framework renaming, fabricated identifiers, mass asset deduplication or deletion of licensed material. No new developer account or duplicate app was created. Existing world save values remain valid; only the accepted farthest-world upper bound expands with the requested route. Uncertain dead UI helpers remain recommendations.

## Manual Checks Required

Initial baseline validation on 29 September:

- Disposable copy `/private/tmp/skypulse-43a-audit-20260929`, Unity **6000.6.0f1**, `-batchmode -nographics -quit -executeMethod SkyPulseAuditValidation.Run`.
- Exit 0, `SKYPULSE_43A_STATIC_UNITY_PASS`; no `error CS` compiler messages. One enabled scene, zero missing scene MonoBehaviours, all 161 asset imports load, 146 Resources assets. Player assembly inputs contain exactly seven authored runtime scripts, no editor/QA helper.
- Asset/ProjectSettings serialized GUID scan resolved all non-builtin GUIDs against Assets/package metadata; no duplicate asset GUIDs, absent `.meta`, orphan `.meta`, or byte-identical asset groups. This does not prove all runtime paths or fileID subobjects correct.
- No play mode or save mutation in this initial test. Log includes engine shutdown/curl/debugger warnings; do not describe the entire Editor log as warning-free. Original gameplay source hash matched build 6.

Post-change checks also passed:

- `SkyPulseWorldExpansionChecks.Run` in the isolated QA copy exited 0 with `SKYPULSE_WORLD_EXPANSION_GAMEPLAY_PASS` and `SKYPULSE_WORLD_CHECKS_PASS`. It runs the existing 1,300-gate/economy/save/purchase/animation checks and additional 12-world boundary/resource/transition/HUD/wrap/old-save/new-save/guide-navigation checks.
- Produced 36 Editor QA renders at 750×1334, 1320×2868 and 1640×2360. Guide/Home text-fit assertions passed. Representative iPhone/iPad guide and supplied-world images were visually inspected; these artificial-state images are not store/device evidence.
- After moving the superseded PNGs, re-ran compilation/import/scene validation: exit 0 and `SKYPULSE_43A_STATIC_UNITY_PASS`; 149 Resources assets, zero missing imported assets/scene scripts. Current Assets contains 164 non-meta, non-hidden files (plus an ignored Finder `.DS_Store`); all non-builtin serialized GUIDs resolve.
- `python3 mobile/Tools/verify_bird_registration.py` passed all 90 frames. Eighteen critical method bodies are byte-identical to baseline, including flight parameters/simulation, collider checks/layout, difficulty curves, power-up durations, save writes, purchases, roster migration, Hangar and Tech Tree builders.
- World QA log contains a Unity Editor Search indexing `ArgumentOutOfRangeException` and licensing/service warnings. They are retained in the log and are not presented as successful zero-error Editor testing. No C# compiler error was found.
- `git diff --check` passed. Five supplied image copies and both archived PNGs/metadata were hash-verified. All 44 build 5 archive files and the build 6 ZIP hash remain unchanged. Final evidence: `artifacts/apple-43a-audit-20260929/verification.json`.

Required hands-on checks for a new world candidate (not claimed complete by static audit):

1. In Unity 6000.6.0f1 open `mobile/Assets/Scenes/SkyPulse.unity`; confirm no Console compiler/missing-reference errors.
2. On isolated test saves, verify fresh and existing saves: bird ownership/equipped bird, all nine upgrade levels/prerequisites, crystals, best score, preferences and farthest world. Never run destructive QA on the real player save.
3. On iPhone and iPad, inspect Home, every guide page/scroll extent, Hangar swipe, Tech Tree details and purchase confirmations. Check safe areas and long world names.
4. Play tap flight, collide with pipe/cap/ground, score and retry; pause/background/resume. Compare handling and score-based speed/gaps with the preserved build.
5. Verify every world boundary and route wrap, backdrop fill at both aspect ratios, transition smoothness, no missing textures, readable crystals/obstacles/HUD, and Reduced Motion.
6. Verify Aegis one-hit protection, Time Pulse 4 seconds, Magnet 6 seconds; collected crystals and end-of-run bonuses persist once, including after restart.
7. Profile texture memory and transition frame times on actual supported hardware after expanding backgrounds.
8. Export a fresh candidate to a fresh folder, use a new unused build number, archive/validate distribution signing and effective entitlements; preserve all previous archives. Capture evidence from that exact candidate.

## App Store Review Evidence

Suggested factual source-backed text; adapt build number and device evidence only after candidate verification:

> SkyPulse includes a 15-bird Hangar with selection, earned-crystal unlocks, swipe navigation and persistent ownership, implemented in `SkyPulseNativeGame.cs` through `Skins`, `BuildBirdHangarPage`, `SelectSkin` and `SaveProgress`. Its Tech Tree uses nine named nodes across Collection, Recovery and Mastery, each with three levels and prerequisite checks, implemented through `BuildTechTree`, `InspectTechNode`, `IsUpgradePrerequisiteMet` and the reward helpers. `SkyPulseTechConnection.cs` and `SkyPulseUiGlyph.cs` provide its connected-tree and mechanical-flight interface visuals. The same native game class implements buffered tap flight, collider-based obstacle checks, score-based difficulty, crystals, three tactical power-ups, world transitions and retry/persistent progression. Home exposes Bird Hangar, Tech Tree and Worlds & Power-ups directly. All birds share flight handling; unlocks are cosmetic and use earned crystals.
>
> We inspected the project's dependencies, build scenes, product identifiers and preserved build archive. We found only Unity package dependencies, the SkyPulse scene and the expected SkyPulse app identifier, with normal Unity framework identifiers. Please identify any specific binary component, metadata element or concept associated with the reported terminated-account similarity so we can address the actual concern with supporting development/provenance evidence.

Do not say Apple approved this audit, the rejection is resolved, no third-party code exists, all art was hand-drawn, or no prior-account association exists unless independently established. Do not send this reply automatically as part of this audit; prepare current candidate evidence first.

## Recommended Next Steps

1. Resolve provenance/account-comparison uncertainty with dated source history and an asset-origin ledger; ask Apple for actionable specifics without inventing an explanation.
2. The requested world expansion and isolated checks are complete. Perform the listed physical-device/late-route/memory checks; keep findings about baseline build 6 distinct from the new source.
3. Recheck the current ASC rejection/build selection and media; the September 16 snapshot is stale. Replace any historical evidence with the exact newly tested candidate.
4. Correct support/marketing copy through its separate site publishing workflow, and retain honest cosmetic-bird/shared-physics language. Keep candidate world names/counts aligned.
5. Only after actual device verification, upload a new numbered candidate and submit a concise, accurate reviewer walkthrough. This audit alone cannot guarantee approval.
