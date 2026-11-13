# Twelve worlds — current local visual candidate

**Owner visual approval is pending.** Start with the [gameplay sheet](Twelve-Worlds-Gameplay.png),
[Home sheet](Twelve-Worlds-Homes.png), [Relay comparison](Pulse-Relay-Home-Gameplay.png),
[Neon City motion](Neon-City-Motion.mp4) and [Aurora Rise motion](Aurora-Rise-Motion.mp4).
These are actual Unity Editor renders of all twelve existing worlds.

Each world now has three complete districts: a civic landmark, a lower residential
counterpoint and one quiet distant marker. The foreground tower clones are gone.
Each world starts from its authored composition at its actual entry distance;
its outgoing bank keeps the same phase. Original upper-sky artwork surrounds
clear central air, with sparse coloured cloud banks at the edges. Curved sail
and round facades now receive their recessed windows.

## Inspect every world

| World | Gameplay | Home |
| --- | --- | --- |
| 01 · Neon City | [Full frame](world-00-flight.png) | [Full frame](world-00-home.png) |
| 02 · Aurora Rise | [Full frame](world-01-flight.png) | [Full frame](world-01-home.png) |
| 03 · Solar Drift | [Full frame](world-02-flight.png) | [Full frame](world-02-home.png) |
| 04 · Midnight Tide | [Full frame](world-03-flight.png) | [Full frame](world-03-home.png) |
| 05 · Velvet Dawn | [Full frame](world-04-flight.png) | [Full frame](world-04-home.png) |
| 06 · Crystal Night | [Full frame](world-05-flight.png) | [Full frame](world-05-home.png) |
| 07 · Jade Horizon | [Full frame](world-06-flight.png) | [Full frame](world-06-home.png) |
| 08 · Violet Rain | [Full frame](world-07-flight.png) | [Full frame](world-07-home.png) |
| 09 · Eclipse | [Full frame](world-08-flight.png) | [Full frame](world-08-home.png) |
| 10 · Cobalt Storm | [Full frame](world-09-flight.png) | [Full frame](world-09-home.png) |
| 11 · Amber Skies | [Full frame](world-10-flight.png) | [Full frame](world-10-home.png) |
| 12 · Polar Glow | [Full frame](world-11-flight.png) | [Full frame](world-11-home.png) |

All 26 full Home/gameplay captures are preserved. Additional views show the
[normal unobstructed world feed](world-00-unobstructed.png),
[separate architecture inspection](world-00-architecture-inspection.png),
[shadows off](world-00-shadow-off.png) / [on](world-00-shadow-on.png), and
[35%](world-crossfade-35.png) / [65%](world-crossfade-65.png) crossfades.
The architecture close-up changes only the inspection camera. Sheets arrange
full frames without repainting them. Each motion clip contains 90 real
collision-checked frames over three seconds at 30 fps; encoding rate is not a
device performance measurement. Raw motion frames remain in disposable QA output.
World fixtures select identities directly at nonzero travel; this is not one
uninterrupted scored run.

## Editable source and local checks

Original editable objects, scenes, prefabs, part pivots and existing GUIDs remain
under `Assets/Editor/SkyPulseBackgroundWorkshop`. Civic/residential bakes remain
in `Assets/Resources/SkyPulse/BackgroundEdits`. Flight controls, camera field of
view, pipe collisions, saves and economy remain unchanged; see the
[gameplay preservation record](../../Checks/World-Revision-2/Gameplay-Preservation.json).
The approved Finch and original bird artwork were preserved. Other roster
sculptures are not approved by this world preview package.

The [render report](../../Checks/World-Revision-2/Atmosphere-Render.json) passed
805 assertions, including all twelve nonzero world arrivals, exact bank handoff
registration, 30/60/120 Hz presentation agreement, Reduced Motion pixel identity
and real shadow comparisons. The [district report](../../Checks/World-Revision-2/District-Geometry.json)
passed 1,666 assertions across 24 meshes, maximum 57,412 vertices. The
[promotion audit](../../Checks/World-Revision-2/Promotion-Audit.json) verified 221
unique final asset paths and 1,527 protected files. A later 43-path facade
refinement updates that same set; it is not another 43 unique assets.
[Player-copy checks](../../Checks/World-Revision-2/Player-Copy.json) also passed.
These technical checks do not establish visual approval.

Clouds share one Editor-baked 64³ RGBA texture (1 MiB). Sparse edge volumes use
20 bounded primary samples, at most 80 volume reads per cloud pixel, with
empty-region skips. District shadows use an isolated city light; the manual
city render temporarily extends then restores shadow distance. **Mobile frame
time, memory pressure, battery and thermal performance remain unverified.**

Unity Editor's internal SearchDatabase indexing raised an
`ArgumentOutOfRangeException` during QA startup. World assertions completed;
this is **not a clean-Console certification**. The
[actual Editor log](../../Checks/World-Revision-2/Atmosphere-Editor.log) is preserved.

**No iPhone build, Xcode export, signing, installation or device checks will run
until the owner is happy with these previews. Installed iPhone Build 19 remains
unchanged. Public App Review remains on hold.** No upload or submission occurred.
