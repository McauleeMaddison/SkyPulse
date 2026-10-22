# Current installed-candidate reference — 22 October 2026

These previews use **local Build 18, source `f71eb7d8014fd6e7386811b9d60a88ad1ab6bddc`**,
the source recorded for the successful local iPhone installation earlier today.
They use isolated QA progress and are actual Unity renders, not phone recordings.

- `Current-Build-18-Relay-and-Signal-Heist.png`: dated two-mode comparison.
- `Pulse-Relay-Build-18.png` and `.mp4`: 3D hoop passage and automatic crystal gates;
  real flap/physics loop, 420 frames / 14 seconds / 30 FPS.
- `Signal-Heist-Build-18.png` and `.mp4`: real prism rotations, beams and immediate
  next-vault progression; 270 frames / 9 seconds / 30 FPS.
- `SOURCE.json`: source-manifest comparison, media hashes and capture checks.

452 entries match the installed candidate's export manifest. Only QA product
settings differ, to protect the owner's save. The capture run exited 0 with
presentation, Relay mesh/occlusion, Heist capture and phone-reference pass markers.
Both videos were decoded completely and have the recorded frame counts.

## Stale-preview correction

The panel shown in the user's screenshot displayed
`artifacts/modes-update-20261012/previews/new-gameplay-preview.png`, a **12 October
prototype** with retired FLAP/PULSE buttons and the old hacking grid. It was not a
live Unity Game view or the current implementation. The initial response wrongly
treated that old image as a new runtime regression before inspecting its path.

The original image is retained at
`artifacts/modes-update-20261012/previews/archive/new-gameplay-preview-ARCHIVED-2026-10-12.png`.
Its old familiar path now points to a copy of the current, dated comparison, so
reopening it does not show the obsolete controls. These artifact paths are local;
the fresh reference media in this directory are versioned on the upgrade branch.

Relay and Blackout gameplay source files match the installed candidate. No old
controls or old hacking-grid gameplay were reintroduced. New pipe/scenery and
retry-feedback repairs remain branch-only. No further phone installation, main
merge, Apple upload or review submission was performed after the reported mismatch.

The Codex panel-open request was queued. The images and videos also remain usable
directly from this directory without relying on the older preview tab refreshing.
