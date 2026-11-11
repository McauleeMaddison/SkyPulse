# SkyPulse local wing and everyday-play repair — 11 November 2026

Chromaflux and the ten newer reference rigs now carry their long feather fans
outboard of their feet. Accepted flight taps visibly begin a wing stroke, and
an actual Hangar equip clears a stale Editor model override. The open owner Unity
project has been refreshed and left on **3D Bird Review → Studio → Chromaflux**.

[Animated Chromaflux wing cycle](Previews/Chromaflux-Wing-Cycle.gif) ·
[Hangar](Previews/Hangar.png) · [Home](Previews/Home.png) ·
[validation](Checks/Validation.json)

## What changed

- **Wings:** the reference artwork for Solara through Ionwing depicts a downstroke.
  Their distal feather fans were still resting downward after the earlier shoulder
  correction. Only their two outer-wing rest rotations and matching bind rotations
  changed. Each prefab retains its object IDs, geometry, materials, scene and GUID.
  Finch and the first four roster prefabs retain their saved geometry/rest pose.
  The shared tap-flight stroke direction now agrees with the Studio stroke.
- **Controls and identity:** each accepted Endless input edge immediately applies
  the existing lift and starts its visible stroke. Touch/mouse duplication, UI
  taps and held input across screen changes remain fenced. Equipping a bird in
  Hangar returns the temporary 3D reviewer to the actual equipped identity; Home,
  flight and the bird-specific crash image follow that selection.
- **Hangar:** the existing rotating 3D review display has restrained machined radial
  inlays and inset moving LED arcs. Swipe selection remains; Reduced Motion holds
  those decorative arcs still. This solid-bird display is still an Editor review.
- **First play:** optional, nonblocking guidance uses existing gameplay labels for
  each mode. It learns from accepted actions and success, pauses with gameplay,
  and can be replayed or disabled from How to Play. Three additive learned keys
  and a tips preference preserve old saves and do not affect rewards.
- **Failure and restart:** results identify ground, pipe, uncharged gate, rim or
  trace failure and provide larger separate controls. Stale result callbacks are
  guarded. Blackout restart now closes its old result screen before making the
  board playable; intermediate puzzles still progress immediately.
- **Pickups:** all five existing types have distinct pooled 3D housings: shield,
  horseshoe, gyro, paired crystals and aperture. No new reward type or price was
  introduced. A quiet, larger HUD shows remaining duration or Echo bonus budget.
  Existing sounds receive clearer relative levels and priority in the fixed voice
  pool, respecting mute. Shared meshes/materials are reused during play.

## What was checked

Unity 6000.6.0f1 compiled the candidate in a disposable clone with separate player
preferences. The final combined run logged **770 assertion lines** covering real
Equip-button callbacks, selection/hit identity, 30/60/120 Hz input/presentation,
UI input separation, first-play learning, result target spacing and retries,
small/notched phone and iPad layouts, readable HUD/audio policy, pooled pickup
objects, progression prices and same-frame Relay unlocking/instant Blackout
progression. The separate pickup regression passed **202 assertions** including
rewards, saves, retries and existing effect behaviour.

The geometric audit sampled 120 Studio/flight/tap/Reduced Motion poses per bird:
**347 contact poses before, zero after, across 1,800 sampled poses**. It tests
noncoplanar triangle-surface intersection; it is not proof of every possible
instant, coplanar overlap, complete containment or owner appearance acceptance.
The GIF is an actual Unity mesh render of the corrected saved Chromaflux prefab.

All changed C# sources match the tested clone. Existing textures, geometry,
materials, scenes and `.meta` files are unchanged; the four new scripts have unique
GUIDs. Original artwork registration and player-copy checks pass. See the source
hashes and bounded assertion transcripts in [Checks](Checks/Validation.json).
The isolated Editor logs also contain a SearchDatabase indexing exception; this
record does not claim an empty Unity Console.

## Review next

In the normal **SkyPulse** scene, enable **Tools → SkyPulse → Bird Workshop →
3D roster in actual gameplay**, keep **Use equipped bird**, and start a fresh Play
session. Review Chromaflux from front and side, then equip owned birds in Hangar
and test repeated taps, pause, collision and retry in both flight modes. Compare
[the three mode hints](Previews/Endless-First-Play.png) and the five pickup previews.
The Studio animates independently; gameplay flaps are driven by accepted taps.

The 3D roster remains an **Editor-only visual review**, awaiting owner sign-off
before runtime promotion. No iPhone candidate, archive, installation or TestFlight
upload was made. Sustained device frame pacing, memory, heat, touch comfort and
sound balance still require a matching approved device candidate. Installed build
19 and App Store release records are unchanged. **Public App Review stays on hold.**
