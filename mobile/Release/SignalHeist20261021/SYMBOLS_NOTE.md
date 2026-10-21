# Build 17 symbol-upload warning

Apple accepted build 17 and processed it for SkyPulse Internal. The upload still reports a missing dSYM for `UnityRuntime.framework`, UUID `392D7A7F-6A4F-3A7A-8788-4089FA96FF38`. This warning has not been removed.

The archive log establishes the packaging cause:

- Unity 6000.6.0f1 supplies `UnityRuntime.framework/UnityRuntime` as a static `ar` archive, approximately 244 MB. It is linked into `UnityFramework` by the native link command.
- The generated Xcode project also includes this static framework in the app's Embed Frameworks phase.
- During that copy, Xcode logs `Injecting stub binary into codeless framework`, compiles `/dev/null` and creates a small dynamic `UnityRuntime` stub. The archived stub is approximately 50 KB, has the UUID Apple reports and has no symbols reported by `nm`.
- The archived `UnityFramework` dependency list does not require a dynamic `UnityRuntime`. The missing symbol warning therefore identifies the synthesized empty wrapper, rather than an absent game or engine executable.

Matching retained dSYMs:

| Binary | Matching dSYM UUID |
| --- | --- |
| SkyPulse | A9971A29-179B-3917-9CD1-1CA35CE3B91D |
| UnityFramework | 15B2C6C2-0131-3FD3-9CB7-2CD56CF49E0D |

Evidence is retained in `artifacts/testflight-17-20261021/xcode-archive-17.log` (the static-framework copy and stub creation at lines 4142–4148), `xcode-upload-17.log`, and the durable `.xcarchive.zip`. `dwarfdump --uuid`, `file`, `nm` and `otool -L` were used to inspect the real binaries. No symbols were fabricated or UUIDs renamed.

The earlier provisional description of a missing vendor-engine symbol bundle was too broad: inspection of the archive log identified Xcode's empty stub as the specific object named in this warning. This observation does not prove every possible engine crash can be fully symbolicated, and does not certify device stability.

Build 17's uploaded binary remains unchanged. For a future native packaging cleanup, first verify any framework resources/privacy manifests, then test removal of the unnecessary static-framework embedding in an isolated export. Re-archive, inspect dependencies and retained resources, verify signatures, and exercise on a real device before changing the release pipeline. Do not suppress the warning with a fabricated dSYM.

Apple explains how matching dSYMs provide readable crash information in [Building your app to include debugging information](https://developer.apple.com/documentation/xcode/building-your-app-to-include-debugging-information) and [Locating a missing debug symbol file](https://developer.apple.com/documentation/xcode/locating-a-missing-debug-symbol-file).
