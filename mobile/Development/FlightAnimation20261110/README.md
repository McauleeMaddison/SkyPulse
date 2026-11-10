# Flight animation repair — 10 November 2026

The owner reported that taps still lifted the bird in Unity but its wings stayed
still. The default Editor 3D preview restarted its 1.65-second pose clock at every
tap. Earlier forced-time art captures bypassed that live timing path.

## Reproduction and result

The historical source was reopened in a separate QA product and observed through
real buffered taps, the normal Update/LateUpdate loop and Camera.onPreCull. Fifteen
accepted taps advanced all six native artwork poses, while the solid model was
trapped in a 13.27-degree partial stroke with no downstroke.

The corrected preview uses one continuous active-flight clock per rendered frame.
It pauses without including stopped time on resume and resets on retries or model
changes. Repeated camera passes cannot advance the clock twice. In Reduced Motion,
a restrained essential wing stroke remains visible while ambient body/head/tail
movement and jet shimmer stay still. Studio SamplePose retains its original
behaviour; no geometry, finishes, artwork, prefab bindings or player setting changed.

Actual corrected gameplay completed a 55.99-degree stroke under sixteen rapid taps
in both Endless Route and Pulse Relay. The near-wing landmark moved about 27 pixels
relative to the body at a 750-pixel-wide render; Reduced Motion Finch retained a
33.59-degree stroke. These are correctness/visibility measurements, not device FPS.

## Files and checks

- `Assets/Editor/SkyPulseBirdRosterPreview.cs`: live flight clock and camera callback.
- `Assets/Scripts/SkyPulseNeonFinchPrototype.cs`: separate gameplay pose sampler.
- `Assets/Scripts/SkyPulseNativeGame.cs`: Editor-only run identity; shipping physics and input unchanged.
- `Tools/SkyPulseFlapChecks.cs`: baseline reproduction and focused regression coverage.

[Baseline](Checks/Baseline.json), [993 passing checks](Checks/Flap-Validation.json)
and [preservation](Checks/Preservation.json) record the exact source hashes.
Coverage includes original six-frame art, rapid input, both flight modes, Reduced
Motion, pause/resume/retry, repeated camera passes and UI exclusion. C# compilation
had zero errors or warnings. A copied QA Library still logged the known unrelated
Unity Editor Search startup exception; these checks do not certify an empty Console.

The course fixture moved obstacles away or precharged Relay gates and granted
safety immunity only inside the disposable QA product to isolate animation.
Its crops are diagnostic test evidence, not a fair-play run, storefront preview
or recording of the installed iPhone build. No forced pose time was used in the
live regression cases. Phone appearance/performance awaits owner approval; Build
19 and the public App Review hold remain unchanged.

## Ten proposed priorities

1. Reliability: keep input, collisions, pause/retry and save recovery covered by real gameplay regressions.
2. Art direction: unify material response, light direction and contrast across characters, worlds and interfaces.
3. World composition: give each world a clear landmark, purposeful island spacing and layered atmosphere that preserves the flight lane.
4. Bird expression: retain reference-specific silhouettes and readable wing, tail and thrust motion at real gameplay size.
5. Relay spectacle: refine the existing crystal-charge, physical unlock and passage sequence without delaying collision clearance.
6. Blackout purpose: make each existing city-system restoration obvious through board/network feedback and clear next objectives.
7. Hangar presentation: consistent rotating model framing, lighting, swipe response and concise trait information.
8. Upgrade value: clearer before/after numbers for training, breeding and Core savings, with unmistakable NEXT purchases.
9. Feedback: readable pickup timers and distinct, balanced collection/unlock sounds and optional haptics.
10. Mobile finishing: after visual approval, profile a fresh matching candidate on iPhone/iPad for sustained frame times, memory and heat, then refresh release media.

These are recommended next priorities, not ten features claimed as implemented.
The current code already provides progression benefits, city-restoration goals and
Relay's automatic unlock; improve their clarity rather than duplicating their rules.
