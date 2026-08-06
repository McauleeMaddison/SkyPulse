# SkyPulse Mobile

This is the native, cross-platform foundation for SkyPulse. It uses **Unity 6 + C#** so the same game can later ship to Android and iPhone from one project.

## Current milestone

The first native flight loop is intentionally focused and lightweight:

- portrait mobile layout and instant tap-to-flap input;
- deterministic gravity, pipe generation, floor and pipe collision;
- an object pool for pipes and a compact trail with no per-frame allocations;
- 60 FPS target, capped simulation delta, and native audio hooks;
- a locally saved bird collection with clear crystal prices and an explicit confirm/cancel unlock dialog;
- three animated neon pickups: **Slow Field** cuts obstacle scroll speed for 5.5 seconds, **Pulse Shield** absorbs one impact, and **Crystal Cache** awards 12 crystals;
- liquid bird motion with cross-faded wing poses, flight stretch, breathing, shield/slow-field auras, and animated orbital pickup art;
- Nova, its flap frame, the Neon City world, and the core sound effects brought across as the first art bridge.

The live `web/` game remains the friend beta while this native project reaches feature parity. Do not add new gameplay to both long-term: build and prove the native loop first, then move menus, cosmetics, progress, Daily Flight, and sharing across in deliberate passes.

## Open and run on this Mac

1. Open Unity Hub.
2. Add this `mobile/` folder as a project.
3. Open `Assets/Scenes/SkyPulse.unity` and press Play.

The Unity editor installed here can run the Mac simulation. Its mobile export modules are not installed yet, so Android/iPhone packages are a separate toolchain step once the native loop is approved.

## Native build order

1. Validate tap feel, gravity, collision fairness, sound, and frame stability in the Unity simulator.
2. Move the premium Home, Game Over, Pause, Settings, and tutorial screens into native UI.
3. Import the complete bird, world, trail, pipe, and reward catalogue.
4. Add locally saved progress and Daily Flight.
5. Install Android/iOS export support and make device builds.
6. Test on current and older Android/iPhone hardware before TestFlight or Play testing.
