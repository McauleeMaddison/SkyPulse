# Replacement audio with retained source — 29 September 2026

The five current sound effects were synthesized specifically for SkyPulse with the retained Python standard-library generator `mobile/Tools/generate_skypulse_audio.py`, authored with Codex in this session. It uses mathematical tones, envelopes and a deterministic noise sequence. It reads no input audio, recordings, sample packs or third-party sound libraries and calls no external service.

| Resource | Character | Duration |
|---|---|---|
| flap | Rising wing-servo pulse | 0.100 seconds |
| score | Two-note gate confirmation | 0.180 seconds |
| crystal | Bright short chime | 2866 / 22050 seconds |
| crash | Falling mechanical thud and filtered noise | 0.320 seconds |
| unlock | Ascending three-tone confirmation | 6394 / 22050 seconds |

All retain the prior clip's frame count, 22050 Hz rate, mono 16-bit PCM format, resource path and Unity metadata/GUID. No game code, trigger, score, progression or sound preference was changed. The sound design itself has changed and requires an on-device listening check.

The previous sounds remain byte-for-byte in `PreviousAudio/`, outside Unity Assets, together with their original metadata. They matched the earlier prototype but their creation records were not recovered in the inspected history. Replacement addresses that documentation gap; it is not a finding that the previous sounds infringed rights or caused Apple's rejection. Historical build 5 and build 6 retain their original audio.

`AUDIO_REPLACEMENT.json` records old/new WAV hashes, metadata hashes and the generator hash. Generate into a temporary directory with `python3 mobile/Tools/generate_skypulse_audio.py --output /private/tmp/skypulse-audio-check`. Verify current files using `python3 mobile/Tools/generate_skypulse_audio.py --check mobile/Assets/Resources/SkyPulse/audio`.

The generator verifies bounded peaks and zero-valued first/last samples. Its output is deterministic on the tested Python environment. Unity import/decode validation is recorded separately in `REMEDIATION_STATUS.md`; no new uploaded build is implied by this source change.
