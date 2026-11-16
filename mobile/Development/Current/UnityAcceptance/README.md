# Detailed world review — 16 November 2026

Editor checks passed. Owner requested fresh Build 22 for iPhone testing; device
installation and visual acceptance are recorded separately. Public review stays on hold.

- [Twelve actual gameplay worlds](Twelve-Worlds.png), with full-size views in Worlds.
- [Civic district close-ups](Districts/Civic) and [residential close-ups](Districts/Residential).
- [Three modes](Three-Modes.png), [Homes](Homes.png), [Hangar and upgrades](Hangar-and-Upgrades.png).
- [Guides](Guide-Samples.png) and [all 15 birds' tap phases](Bird-Tap-Phases.png).
- [Exact source/asset hashes, check counts and limits](REPORT.json).

The calmer four-connected-plus-two-distant city remains. The foundations now have
real cliff openings, recessed utility rooms, supported gallery ledges, service
bays and matte mineral seams. Window occupancy and brightness were refined after
the owner found the previous lights unreadable at gameplay scale. Building
entrances and existing roof details are more defined. Cloud rims use the world's
colour and directional light; density samples and calm motion settings are retained.

All 24 district variants remain in the existing editable Unity workshops, with
original scene/prefab layouts, pivots and GUIDs. Bulk refresh preserves saved
custom geometry. Maximum combined mesh size is 58,630 vertices. The two runtime
banks and existing parallax/skyway layout remain; no gameplay colliders were added.

The current logs are window-final, window-regression, window-design and
window-inspection (.log.gz). Mesh/owner-edit preservation passed 1,666 assertions;
window and beacon on/off GPU checks passed for all 24 variants. Inspection views
are stills. QA progress is simulated and owner saves were not reset or seeded.
Earlier save/result evidence remains in city-focused.log.gz at 7b3edc3.

Game C# compilation has no reported warnings/errors. The known Unity Editor
SearchDatabase exception remains recorded. Device FPS, memory, heat, sound,
haptic comfort and final visual approval require the new iPhone/iPad candidate.
