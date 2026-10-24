# SkyPulse — reusable pipes and atmosphere

Local Unity work dated 24 October 2026. Open the live project at
`/Users/mcauleemaddison/Desktop/SkyPulse/mobile`.

- [Twelve-world gallery](Twelve-Worlds.png)
- [Twelve-world motion preview](Twelve-Worlds-Motion.mp4)
- [Pulse Relay gameplay](Pulse-Relay.png) and [motion](Pulse-Relay-Motion.mp4)
- [Pulse Relay Home](Pulse-Relay-Home.png)
- [Editing guide](EDITING_GUIDE.md)
- [Validation evidence](VALIDATION.json)

There are twelve reusable pipe scenes/prefabs and twelve additional distant
skyline scenes. The original 24 district arrangements and Relay editing scene
remain intact. Pipes retain their tested bounds and pooled shared meshes.
Atmosphere gains curved mist layers, small world-coloured LED stars and a
restrained distant city layer. Relay hardware gains recessed optical lanes and
a dedicated metal finish. Unfinished Endless dissolves no longer overlay other
modes. The Blackout miniature no longer produces a serialization warning.

The automated runs passed 29 regression groups and 79 focused authoring/runtime
checks. Motion videos contain real Unity QA frames at 10 captured frames/second;
this is not a device frame-rate measurement or a new iPhone recording. Device
performance, heat and comfort remain to be checked. Compressed logs record the
Unity Editor QuickSearch startup exception seen in the disposable test copy;
gameplay assertions and compilation completed successfully.

This is a development-branch checkpoint, with no public App Review submission,
phone installation or main-branch merge. Relay's distant illustration is still
present at this checkpoint; replacing it with a fully constructed station is
the following requested task.
