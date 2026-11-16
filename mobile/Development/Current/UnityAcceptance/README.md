# Unity visual acceptance — 16 November 2026

**Calmer revision: Editor checks passed; owner approval and device checks remain pending.**
The main Unity game is open for the owner. Installed iPhone Build 21 is unchanged.

- [All twelve fuller worlds](Twelve-Worlds.png) — each theme's own authored civic/residential models, six district placements and three joining skyways.
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
Two restrained distant districts provide depth. The existing two renderer
banks are reused; the prototype does not spawn scenery each frame. The actual
building structures remain in the canonical editable Unity district workshops;
placement/camera/parallax are in SkyPulseFlightEnvironment.

The known SearchDatabase editor exception remains; game compilation reports no
C# warnings/errors. Audio was muted for automated capture. Phone/iPad frame pacing,
memory, heat, audio and haptic feel still require the approved numbered device build.

The owner found the previous fuller arrangement too chaotic. This revision removes
the isolated lower quarter and high central district, lowers the two distant
landmarks, and refines window/beacon readability. Jade Horizon no longer shows the
painted bare rocks at the sides of its sky illustration. The connected main city
and its themed structures remain. Review this revision before approving a phone build.

Current evidence is in `calm-final.log.gz` and `calm-regression.log.gz`.
`city-focused.log.gz` is the earlier save/result evidence at `7b3edc3`; it was not
rerun for this scenery-only refinement. Runtime/shader source hashes identify the
exact current candidate, and manual acceptance remains unchecked.
