# Unity visual acceptance — 16 November 2026

**Editor checks passed; owner approval and device checks remain pending.**
The main Unity game is open for the owner. Installed iPhone Build 21 is unchanged.

- [All twelve fuller worlds](Twelve-Worlds.png) — each theme's own authored civic/residential models, eight district placements and three joining skyways.
- [Three gameplay views](Three-Modes.png) — actual native bird, automatic Relay gate and immediate Blackout progression.
- [Homes](Homes.png) — Relay's final card faces and labels now match their full touch areas.
- [Hangar, Tech Tree, training and breeding](Hangar-and-Upgrades.png).
- [Current guides](Guide-Samples.png), with individual pages under Guides.
- [Fifteen birds' tap phases](Bird-Tap-Phases.png) — stills at 0/80/180/320 ms, using fixed-step controller updates. Not a device-FPS video.
- [Machine-readable report](REPORT.json) — source hashes, current checks and precise limits.

Worlds, Guides and Screens hold full-resolution views. These use simulated QA
progress and are not App Store screenshots. No real player currency or unlocks
were granted. The opaque-looking pipe bore is the actual hollow cap interior;
the retained art/materials and collision dimensions were not replaced.

The connected row shares one parallax offset so its bridges do not pull apart.
Supporting upper and lower districts provide depth. The existing two renderer
banks are reused; the prototype does not spawn scenery each frame. The actual
building structures remain in the canonical editable Unity district workshops;
placement/camera/parallax are in SkyPulseFlightEnvironment.

The known SearchDatabase editor exception remains; game compilation reports no
C# warnings/errors. Audio was muted for automated capture. Phone/iPad frame pacing,
memory, heat, audio and haptic feel still require the approved numbered device build.
