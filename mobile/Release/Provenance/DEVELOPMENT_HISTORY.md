# Retained development history

Examined from local Git on 29 September 2026, baseline `edaf03da16a0c2f44f8ce67c953e3005283fbfc7`. These are repository records, not independent proof of authorship or a comparison against Apple's private app corpus.

| Git author date | Commit | Observed record |
|---|---|---|
| 2026-08-04 | `7e35646` | Initial Python/Kivy SkyPulse prototype: `main.py`, `settings.py`, project README and artwork. README describes crystals, shop, unlocks and local progression. Its claim that art is original is a historical assertion, not independent verification. |
| 2026-08-04 | `edcf770` | Prototype sound effects added alongside game-feel and cosmetic changes. |
| 2026-08-05 | `7fb3e4b` | Unity foundation adds `SkyPulseBootstrap.cs`, a 339-line `SkyPulseNativeGame.cs`, the SkyPulse scene, packages and selected prototype media. |
| 2026-08-06 | `80ba914` | Native power-ups and bird purchase confirmation. |
| 2026-08-10 | `42fe8c7` | Further flight/visual changes and crystal/unlock sound imports. |
| 2026-08-30 | `2ac8972` | Five robotic birds, eight PNG poses per bird, plus roster implementation changes. |
| 2026-09-02 | `a4f0469` | Eight PNG poses for each of ten additional birds. |
| 2026-09-02 | `ef82a89` | Native code integrates the additional birds. |
| 2026-09-02 | `4ae0adc` | Tech Tree polish. |
| 2026-09-06 | `be7e839` | Bird animation, trails, world transitions and mobile navigation changes. |
| 2026-09-09 | `262dc5a` | Neon pipe, crystal and background update with retained generation notes. |
| 2026-09-16 | `edaf03d` | Device sign-off, historical build 6 upload and simulator service crash records. |

The sequence supports a history of iterative work on SkyPulse. It cannot establish the source of files before their first commit or rule out shared input material.

## Exact audio continuity

Before remediation, all five baseline WAV files were byte-for-byte equal to the same-named files at `edcf770:assets/audio/`. Those baseline bytes are now preserved in `PreviousAudio/`; old/new SHA-256 values are recorded in `AUDIO_REPLACEMENT.json`. The current candidate instead uses the retained generator described in `AUDIO_PROVENANCE.md`. This links the Unity audio to the earlier SkyPulse prototype, but does not establish how the sounds were originally made. The inspected prototype code loads local WAV files; no audio generator was established from that code.

## Existing art records

- `mobile/Release/NeonVisualUpdate/BACKGROUND_ART_PROVENANCE.md`: three backgrounds, image-generation output identifiers and retained prompts/reference path.
- `mobile/Release/NeonVisualUpdate/PIPE_ART_PROVENANCE.md`: generated pipe textures; exact prompts explicitly not retained.
- `mobile/Release/NeonVisualUpdate/CRYSTAL_ART_PROVENANCE.md`: generated crystal; exact prompt explicitly not retained.
- `mobile/Release/ICON.md`: generated icon and underlying bird/previous-icon references.
- `mobile/Release/WorldExpansion/ART_PROVENANCE.md`: five user-supplied JPEGs and exact copied-file hashes.

These notes provide useful links in the evidence chain. The origins of the referenced inputs still need to be recorded. Current references are included in the ledger separately from shipping Assets.

## Reproduction

Run `git show --stat <commit>` and `git show <commit>:<path>` to inspect the records. Run `python3 mobile/Tools/build_provenance_ledger.py` to regenerate file identities and prototype audio comparisons. No author email addresses or credentials are needed in the reviewer packet.
