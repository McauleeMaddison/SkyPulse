# SkyPulse Mobile

This is the native, cross-platform foundation for SkyPulse. It uses **Unity 6 + C#** so the same game can later ship to Android and iPhone from one project.

## Current milestone

The native flight loop is now tuned for a deliberate feel-and-performance pass:

- portrait mobile layout with buffered touch input and a fixed 120 Hz flight simulation, so the same route is stable at 30, 60, and 120 FPS;
- central, mode-specific tuning for flap, gravity, collision tolerance, gap shrink, speed ramp, and power-up frequency;
- Classic and Daily Route fair flights with fixed physics and no gameplay upgrades; Adventure retains expressive power-ups and Flight Tech upgrades;
- a daily seeded obstacle sequence, gradual gate-to-gate route changes, perfect-pass feedback, and a development-only F3 collision overlay;
- pooled pipes, pickups, and trails with no per-frame allocations, plus a 60 FPS target and capped catch-up time;
- a locally saved bird collection with clear crystal prices and an explicit confirm/cancel unlock dialog;
- seven animated neon pickups: **Slow Field** slows obstacle scroll, **Pulse Shield** absorbs an impact, **Crystal Cache** awards currency, **Sky Surge** boosts lift, **Score Prism** increases gate rewards, **Magnet Halo** pulls in pickups, and **Phase Shift** lets the bird pass through pipes briefly;
- a 12-item, locally saved **Flight Tech** upgrade collection: Thrust Plumes, Featherweight, Air Brakes, Rescue Feather, Time Weaver, Shield Cell, Cache Cores, Magnet Array, Phase Stabilizer, Prism Resonator, Comet Trail, and Starheart;
- Adventure world profiles ranging from Easy through Apex, while Classic and Daily keep world selection cosmetic for fair scores;
- dimensional bird motion with three authored full-body poses, breathing, layered body depth, flight stretch, trails, auras, and impact/perfect-pass blooms;
- the premium three-pose **Aetherwing** flight rig for the entire bird collection: tucked glide, raised lift, and power downstroke with authored visor detail instead of a generic mascot eye; each theme keeps its own restrained material tint and trail;
- broad, layered neon plumbing gates for every pipe theme: deep metal body, recessed cylindrical reflection, side rails, a wide coupling collar, a readable energy seam, and collision bounds matched to that outer collar;
- seven new dimensional, transparent power-up artworks with pulsing depth and orbital motion;
- native crystal/unlock effects alongside Nova, Neon City, and the core sound bridge.

On a device, high-value moments also get a single restrained haptic pulse: perfect passes, power-up pickups, shield/rescue saves, and impacts. Normal flaps remain haptic-free so the game never turns into continuous vibration.

The live `web/` game remains the friend beta while this native project reaches feature parity. Do not add new gameplay to both long-term: build and prove the native loop first, then move menus, cosmetics, progress, Daily Flight, and sharing across in deliberate passes.

## Open and run on this Mac

1. Open Unity Hub.
2. Add this `mobile/` folder as a project.
3. Open `Assets/Scenes/SkyPulse.unity` and press Play.

For a quick visual-performance pass after importing new art, run **SkyPulse → Optimise Mobile Art** in the Unity menu. It sets transparent, clamp, non-mipmapped texture imports and ASTC 6×6 platform compression, with a 512 px budget for gameplay art, 1024 px for birds, and source-preserving 2048 px for cinematic backgrounds.

This project already has the iOS module installed. Unity remains the only source of truth for gameplay, art and UI; Xcode is the Apple build/signing/profiling hand-off that Unity generates.

## Test on a physical phone

You do not install Unity on the phone. Unity runs on the Mac, creates an app build, and installs that app onto the connected phone.

### Android (fastest first device test)

1. In **Unity Hub → Installs**, find Unity `6000.0.47f1`, choose **Add modules**, and install **Android Build Support**, including **Android SDK & NDK Tools** and **OpenJDK**.
2. On the Android phone, enable **Developer options** and **USB debugging**, then connect it to the Mac using a data-capable USB cable and accept the phone’s trust/debugging prompt.
3. In Unity, open **File → Build Profiles**, add/switch to **Android**, select the phone under **Run Device**, then use **Build And Run**.
4. For performance work, tick **Development Build** and **Autoconnect Profiler** for one test build only; turn them off for normal play tests.

### iPhone: Unity → Xcode → device

Use both tools, but for different jobs:

| Tool | Owns |
| --- | --- |
| **Unity** | the SkyPulse scene, C# gameplay, physics, art, UI, iOS settings and the generated Xcode project |
| **Xcode** | Apple signing, installing to an iPhone, Instruments profiling, Archive and TestFlight upload |

1. In **Unity Hub**, keep this project on Unity `6000.0.47f1` with **iOS Build Support** installed.
2. In Unity, open **File → Build Profiles**, select the **iOS** profile and make it active. For on-device testing tick **Development Build**; use **Autoconnect Profiler** only for a profiling pass.
3. In **Edit → Project Settings → Player → iOS → Other Settings**, set the unique Bundle Identifier (currently `com.mcauleemaddison.skypulse`), a release version such as `0.1.0`, and increment the build number for every distributable build.
4. Choose **Build** and select a folder outside `Assets/`, for example `mobile/Builds/iOS/SkyPulse`. Unity writes `Unity-iPhone.xcodeproj` there. Do not edit Unity-generated C++ or project files as permanent game changes: rebuild them from Unity instead.
5. Open `Unity-iPhone.xcodeproj` in Xcode. Select the **Unity-iPhone** target, then **Signing & Capabilities**. Select your Apple team and let Xcode manage signing.
6. Connect, unlock and trust the iPhone; enable **Developer Mode** on the phone if iOS requests it. Select that iPhone in Xcode's destination menu, then press **Run** (▶). The first install can take several minutes because Unity's IL2CPP code is compiled by Xcode.
7. For TestFlight, choose **Any iOS Device**, then **Product → Archive → Distribute App → App Store Connect → Upload**. This requires an active Apple Developer Program membership.

If the iPhone runs a newer iOS beta than the Xcode/macOS combination supports, Xcode cannot deploy to it. You can still build the generated project from Unity, test in the editor and use the web beta; a Mac capable of the Xcode release matched to that iOS beta is required for direct device installation.

Record each first device session: device model, OS version, average FPS, first-run score, accidental taps, unclear deaths, and any visual hitch. That is the evidence needed to make final release calls rather than guessing from the editor.

## Native build order

1. Validate tap feel, gravity, collision fairness, sound, and frame stability in the Unity simulator.
2. Move the premium Home, Game Over, Pause, Settings, and tutorial screens into native UI.
3. Import the complete bird, world, trail, pipe, and reward catalogue.
4. Add locally saved progress and Daily Flight.
5. Install Android/iOS export support and make device builds.
6. Test on current and older Android/iPhone hardware before TestFlight or Play testing.
