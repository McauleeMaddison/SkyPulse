# Simulator graphics-service interruption — 16 September 2026

Report: `artifacts/build-6-review-recovery/simulator-crash-123422.ips` (copied from the macOS DiagnosticReports entry).

- Process: SkyPulse, running in iPad Air 11-inch M4 simulator `A795FC83-001F-421E-9D62-6981A55602AB`.
- Capture time: 12:33:56.9731 BST.
- Exception: EXC_CRASH / SIGABRT.
- Termination namespace: METAL, code 102.
- Recorded reason: connection to SimMetalHost XPC service PID 5420 was lost, XPC_ERROR_CONNECTION_INTERRUPTED.
- Triggered thread: UnityGfxDeviceWorker. Stack passes through MTLSimulator_encountered_XPC_error and Metal buffer allocation. No managed gameplay exception is identified by this report.

At 12:33:56.520, the screenshot command reported a CoreSimulator version change: framework 1171.7 did not match existing service 1171.6, so it removed the stale service. The crash followed approximately 0.45 seconds later. This strongly supports graphics-service replacement as the cause of this particular abort. It does not establish that every possible app crash is excluded.

The iPad 13-inch simulator also shut down during service replacement and was booted again with its installed app/save retained. Post-restart verification is recorded separately. No game source change is justified by this service-disconnection report alone. The physical iPhone test passed according to the owner; the device build does not use SimMetalHost.

Post-restart native capture confirms Home renders on the affected 11-inch device, with 51 crystals and best score 21 preserved. Capture: `final-native-ipad/after-service-restart.png`. Xcode is now 27.0 (27A266a); its replacement UI is Device Hub at `Contents/Applications/DeviceHub.app`. This is an installed-tool change, not a change to the existing archived build 6. Device Hub UI automation currently times out, while simctl launch and native screenshot commands work.
