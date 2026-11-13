# SkyPulse Arcade

A Unity mobile game developed by **MacWebStudio** — [macwebstudio.com](https://macwebstudio.com).
Open `mobile/` in Unity Hub and this repository in VS Code.

## Start here

- [Editing guide](mobile/EDITING_GUIDE.md) — scenes, scripts and where to change each feature.
- [Current visual review](mobile/Development/Current/README.md) — local source candidate, previews and verification.
- [Installed Build 21](mobile/Release/DeviceBuild21/README.md) — the existing iPhone installation.
- [Device checklist](mobile/Release/DeviceBuild21/DEVICE_CHECKLIST.md) — use after approving the new local visuals.
- [Exact installed-release record](mobile/Release/CURRENT_RELEASE.json) — signing/runtime evidence for Build 21.

The current local source adds the city restoration overlook, eight distinct saved
lighting rewards, next-goal cards, cosmetic bird mastery and four optional authored
daily heists. It also corrects the twelve-family Tech validator. This source has
not replaced the iPhone installation. **Public App Review remains on hold.**

## Folder map

| Folder | Purpose |
| --- | --- |
| `mobile/Assets` | Game source, canonical editable models, scenes and runtime resources |
| `mobile/Packages`, `mobile/ProjectSettings` | Unity dependencies and project configuration |
| `mobile/Tools` | Isolated QA helpers, build/metadata checks; not shipping gameplay |
| `mobile/Development/Current` | Latest review, screenshots, source hashes and cleanup record |
| `mobile/Development/ArtSources` | Retained artwork provenance |
| `mobile/Development/Archive` | Historical design studies and earlier review media |
| `mobile/Release` | Versioned release records, provenance and device checklists |
| `artifacts` | Ignored local output: current Build 21, signed Build 20 rollback and historical evidence |
| `public-site` | Separately versioned support/privacy website checkout |

Unity regenerates `Library`, `Temp`, `Logs` and local build folders. VS Code hides
these generated folders and groups the main game partial files. `.meta` files are
asset identifiers and must stay paired with their assets; they are not App Store
listing metadata. Do not delete artwork simply because a reference is loaded
through a resource path rather than a scene GUID.

The approved bird models and wing sampler, flight dimensions, currency and current
save keys remain part of the existing project. New mastery is cosmetic and stored
locally; it does not sell or spend a second currency. See [NOTICE.md](NOTICE.md)
and the [historical App Review audit](APPLE_4_3A_AUDIT.md) for provenance/review context.
