# Distinct mode artwork and code origins

Baseline source/evidence commit: `4cf4201`. The owner requested equal widths, clearly different gameplay/player presentations, three distinct premium Home screens, and a major Blackout visual redesign. Codex implemented the presentation changes in this session.

Two new background PNGs were generated with the **built-in image_gen tool**, without external/reference-image inputs. Their exact prompts, original saved outputs and shipping hashes are retained in `ASSET_GENERATION.json`. They are copied into the project and used by actual Unity Home/gameplay views; original generator outputs remain preserved. No CLI/API-key fallback or downloaded asset pack was used. These newly generated assets are distinct from the older owner-declared ARTA artwork and documented earlier image-generation records.

The portal, data-core hero, raised tile pads, cube glyphs, circuit lighting and motion remain authored procedural Unity code in `SkyPulseNativeGame.Presentation.cs` / `SkyPulseCircuitGraphic.cs`. Supporting screen/layout hooks are in `SkyPulseNativeGame.Missions.cs`. New `.meta` files have new GUIDs; existing artwork paths/GUIDs were retained. The original flight, puzzle and daily-rule model files remain unchanged by this visual update.

The new dated ledger records current file hashes and Git history; repository author dates are not independently authenticated creation dates. A prompt/generation record does not certify exclusivity, ownership rights or global uniqueness. Earlier missing ARTA exports/reference origins/permissions and Apple's unidentified comparator remain unresolved and are not silently converted into complete provenance.

All previews are actual isolated Editor renders. They do not show a newly signed physical-device build, and the owner’s build 7 tests/footage do not validate this candidate. Original archived builds are retained. App Store submission remains withdrawn; no Apple message or website publication occurred.
