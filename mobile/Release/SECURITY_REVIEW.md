# Security verification — 6 September 2026

Status: targeted checks completed; not a malware-free certification.

## Observed evidence

- The unsigned iPhone Release build fails when macOS terminates Unity il2cpp with signal 9. The system security log explicitly records a Gatekeeper rejection at 17:40:51 local time. The user confirmed the warning appeared during the assistant's work. macOS rejection process IDs 26829 and 28009 exactly match the terminated compiler processes in the first and retry build logs, confirming the build triggered these rejections.
- With access to macOS security services, `codesign --verify --verbose=4` reports both the installed and exported ARM64 il2cpp compiler valid on disk and satisfying its designated requirement. Signer: Developer ID Application: Unity Technologies SF, team 9QW8UQUTAA, Apple certificate chain; timestamp 29 August 2026.
- Both compiler files have SHA-256 `3f952cbfa1b49b26384ca6c21032d97704029558a51cbf12908dc97addcc2f08`. This establishes the export matches the installed compiler, not that every dependency is malware-free.
- Earlier restricted-shell signature checks falsely reported invalid signatures; the unrestricted verification supersedes those results.
- `spctl` describes the compiler as valid code but not an app. A command-line compiler is not a complete application bundle; this is not a malware detection.
- Targeted native game source searches found no network clients, URL launches, subprocess launches, dynamic code downloads or native imports. Saving uses Unity PlayerPrefs. This is a focused review, not exhaustive static/dynamic malware analysis.
- Assets contains no DLL, dylib, shared-library, static-library, framework, bundle or shell-script plug-in files. Package lock entries use Unity built-in modules and Unity's package registry. No ads, analytics, account or real-money purchase package was identified.
- Generated export includes app, UnityFramework and UnityRuntime privacy manifests. Final privacy report and signed-build validation remain pending.

No Gatekeeper setting was disabled, quarantine/provenance removed, or compiler signature replaced. Do not use a security bypass as evidence that the game is clean. Resolve the exact Apple warning through the supported macOS/Unity workflow, then build and validate a signed iPhone release.

Apple distinguishes an inability to verify software from a detected-malware alert: https://support.apple.com/en-ie/102445

Evidence log: ../../artifacts/effects-qa/skypulse-beta-xcode-retry.log

## Resolution of the macOS build rejection

Renaming the exported source plist from Info.plist to SkyPulse-Info.plist and updating the main Xcode target's INFOPLIST_FILE allowed the original, unmodified signed compiler to run. Both iPhone Release and Simulator Release builds then succeeded. This supports the diagnosis that macOS was assessing the source export as an app bundle. The export postprocessor now applies that standard source-file layout automatically. No security settings, compiler signatures or provenance attributes were changed.

These successful builds predate the final two-wing icon and crystal-counter fix. Their subsequent Unity export succeeded; automatic approval review rejected the native rebuild due to its usage limit. That rejection is separate from macOS Gatekeeper and must not be bypassed.

## Final verification completed

After the user requested completion, approval review allowed the final builds. Both final two-wing-icon/counter-fix builds now compile successfully, and the final Simulator app is installed. The earlier usage-limit rejection is historical. See BETA_VERIFICATION.json and TOMORROW_PLAN.md for current status. This does not constitute an absolute malware-free certification or App Store approval.
