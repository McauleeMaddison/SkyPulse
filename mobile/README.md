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
- dimensional bird motion with cross-faded wing poses, breathing, layered body depth, flight stretch, trails, auras, and impact/perfect-pass blooms;
- animated cobalt-neon pipe gateways and seven new dimensional, transparent power-up artworks with pulsing depth and orbital motion;
- native crystal/unlock effects alongside Nova, Neon City, and the core sound bridge.

The live `web/` game remains the friend beta while this native project reaches feature parity. Do not add new gameplay to both long-term: build and prove the native loop first, then move menus, cosmetics, progress, Daily Flight, and sharing across in deliberate passes.

## Open and run on this Mac

1. Open Unity Hub.
2. Add this `mobile/` folder as a project.
3. Open `Assets/Scenes/SkyPulse.unity` and press Play.

For a quick visual-performance pass after importing new art, run **SkyPulse → Optimise Mobile Art** in the Unity menu. It sets transparent, clamp, non-mipmapped texture imports and ASTC 6×6 platform compression, with a 512 px budget for gameplay art and 1024 px for birds/backgrounds.

The Unity editor installed here can run the Mac simulation. Its mobile export modules are not installed yet, so Android/iPhone packages are a separate toolchain step once the native loop is approved.

## Native build order

1. Validate tap feel, gravity, collision fairness, sound, and frame stability in the Unity simulator.
2. Move the premium Home, Game Over, Pause, Settings, and tutorial screens into native UI.
3. Import the complete bird, world, trail, pipe, and reward catalogue.
4. Add locally saved progress and Daily Flight.
5. Install Android/iOS export support and make device builds.
6. Test on current and older Android/iPhone hardware before TestFlight or Play testing.
