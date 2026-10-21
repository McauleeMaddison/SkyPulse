# Player experience priorities after build 17 testing

These are proposed next changes, not features included in the uploaded build. Start with feedback from the current iPhone TestFlight build; retain the tested flight width, forces, collisions, save compatibility, automatic Relay gates and immediate Blackout progression.

1. **Make progression benefits perceptible.** The current result groups bird and Tech Tree bonuses under the Tech reward total. Add a concise bird/tech reward breakdown and a restrained trait activation cue. Show Core savings in the purchase flow and a useful comparison of equipped bird versus previewed offspring. Validate every displayed subtotal against the once-only wallet credit; decoration must not award extra currency or overwhelm flight.
2. **Give the whole scene one lighting language.** Match the bird shader, metal highlights, crystals and foreground hardware to each world's key light and palette. Add restrained contact light/shadow and softer distant atmosphere. Keep the flight corridor, hoop aperture and crystal identity clear. Validate contrast on the smallest supported phone, including low brightness and Reduced Motion.
3. **Make Signal Heist decisions easier to anticipate.** Prototype a non-spending rotation/beam preview and a clear cost cue when a lit sensor makes a move cost three trace. Teach the first mirror, relay and sensor through short interactive encounters, then let players act freely. Preserve one deliberate tap per move, the trace budget, immediate puzzle transitions and saved attempts.
4. **Improve the first session and failure feedback.** Teach each mode's first meaningful action in play. Show the specific cause of failure and, where useful, the failed gate/beam location, with immediate Retry and Home still available. Use a short, distinct visual/audio/haptic vocabulary for collection, unlock, breach, failure and breeding; settings must remain respected. Do not add mandatory celebration waits or ambiguous gestures.
5. **Prioritise sustained smoothness and legibility.** Measure frame times, GPU load, memory and thermals during longer real iPhone/iPad sessions, including repeated Home/Hangar/Core/puzzle transitions. Tune distant detail and render-target resolution against measurements; preserve responsive input and critical cues. Review real point-size text and touch comfort. A measured phone pass is required before claiming sustained 60 FPS.

Design basis: observations of the current Unity code and rendered build, plus Apple's motion/game accessibility guidance and Unity's graphics profiling guidance. These choices should be evaluated with player behaviour, not implemented as a blind increase in visual effects.

- https://developer.apple.com/design/human-interface-guidelines/motion
- https://developer.apple.com/design/human-interface-guidelines/designing-for-games
- https://docs.unity.com/en-us/engine/6000.3/manual/analysis/graphics-performance-profiling

Public App Review remains on hold. These proposals do not resolve Apple's unidentified similarity comparison and do not authorise a submission.
