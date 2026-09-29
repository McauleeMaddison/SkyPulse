# Provenance remediation status — 29 September 2026

## Completed fixes

- Recorded the owner's confirmation that they generated SkyPulse artwork in ARTA and own the code developed using VS Code. Preserved specific records of subsequent Codex image-generation work.
- Identified the matching ARTA App Store listing and checked its published iOS terms. Section VI permits commercial use of generated User Content under its conditions. See `ARTA_TERMS_RECORD.md`.
- Replaced all five undocumented-origin audio resources with sounds synthesized from retained source. Preserved the previous WAVs and metadata outside Unity Assets. The generator consumes no third-party samples, keeps all original durations/resource names/GUIDs, and does not change game logic. See `AUDIO_PROVENANCE.md` and `AUDIO_REPLACEMENT.json`.
- Updated the 167-file ledger, source-history narrative and factual Apple response draft. The current audio is now distinguished from the historical prototype/build 6 audio.

## Validation

- Generator reproduction check passes for all five WAV files.
- WAV format and frame counts exactly match the originals; all metadata bytes are unchanged. New signals have bounded peaks and begin/end at zero.
- Unity 6000.6.0f1 isolated import/scene/script validation passed, followed by resource loading and sample decoding of all five effects. Exit 0; markers `SKYPULSE_43A_STATIC_UNITY_PASS` and `SKYPULSE_AUTHORED_AUDIO_IMPORT_PASS`.
- Log: `artifacts/apple-43a-audit-20260929/authored-audio-unity.log`. The first attempts failed at sandbox IPC/licensing initialization; the successful retry retains an access-token warning and usbmuxd shutdown message. Those service warnings are not C# compiler failures and are not hidden.
- Prior twelve-world gameplay/save/guide tests remain applicable to unchanged game source. No new physical-device audio/listening test or release archive is claimed.

## Still needed for release

- Actual iPhone/iPad listening and the already listed expanded-world device/memory tests, followed by a newly numbered archive and matching store media. Existing build 5/6 archives remain unchanged.
- App Store Connect sign-in. The current access attempt reached `authResult=FAILED`; the current rejection thread could not be read. No reviewer message or submission has been sent during this remediation.
- A factual account/submission-history answer from the owner, if making a statement about that issue. We cannot infer it from code ownership or change it through a source edit. The clarification draft does not assert any unsupported account relationship or denial.

The old audio-origin gap no longer applies to the new source. Apple's broader 4.3(a) similarity finding remains open until Apple assesses the actual app/evidence. Neither a different sound file nor a licence record alone resolves a binary/metadata/concept comparison.
