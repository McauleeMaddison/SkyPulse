# Guide, world transitions and public information

The current local build 18 candidate adds a mode-specific How to Play entry on
all three Homes. Endless has readable Flight, Tech Tree, Core, Bird Training and
Breeding topics, plus the existing complete Worlds & Power-ups map. Selected
subjects have an explicit highlight. Relay explains automatic gates and daily
circuits; its bird page distinguishes cosmetic Relay equipment from Endless
reward traits. Signal Heist explains routing, PLAN, trace and instant progression.
Breeding's Core level 2 requirement is explicit. Browsing never spends crystals.

All twelve authored world textures load distinctly. The existing 15-gate world
milestones and 180/360 wrap points pass. The newer opaque 3D pipe renderers had
not joined the old sprite departure fade; they now use depth-safe coverage until
the departing pool is hidden. Shared bird/pipe lighting blends between world
colours. Flight physics, pipe dimensions, spacing, saves and economy are retained.

## Current verification

- 36 transition sequences: all twelve boundaries at 30, 60 and 120 FPS, including
  the return to Neon City. Background alpha remains monotonic over an opaque old
  image, and completion commits the exact fully blended sprite.
- Departure visibility resets for the new pipe pool. Recovery, pause/retry and
  saved world discovery pass. A rendered-pixel test checks partial coverage and
  absence of colour/depth writes at zero visibility on the actual collar mesh.
- All guide pages fit 640×1136, 1320×2868 and 1640×2360 layouts. Home entry,
  topic selection, map reset and no-spend navigation pass.
- Existing contour, cinematic zero-allocation/pause/Relay/Heist and one-thumb
  integration checks pass. QA runtime hashes match the repository.
- Exact-source iOS IL2CPP export: 441 matching source entries. No signed archive
  or upload was created. Physical iPhone/iPad play, sustained FPS and motion
  comfort remain pending; Editor timing checks do not certify device smoothness.

## Published website

The existing public Site keeps its branded address and public audience:
https://skypulse.macwebstudio.com. Home/support/privacy now describe the current
modes and progression, and `/guide` covers controls, Core, training, breeding and
all twelve world milestones. Privacy wording includes locally saved lineage,
Core/Tech levels, medals and daily data, with an updated effective date.

The production build, TypeScript and local route/asset checks pass. Ordinary
browser visits verified the published support, privacy and guide content.
Automated Python/web requests received Cloudflare bot rejection; those were not
counted as successful public HTTP tests. The temporary local-preview connection
was lost during rebuilding; the published branded guide is the retained output.
`WEBSITE_UPDATE.json` identifies Site version 5 and the separate GitHub backup.

## App Store Connect — draft only

English (U.K.) description, promotion, keywords and reviewer notes were saved and
verified after reopening. They cover current modes, visual presentation, automatic
gates, PLAN, Core savings, training, breeding and the new guide. App name,
subtitle, bundle identity and categories were checked. The branded privacy URL
and existing Data Not Collected declaration were verified.

The draft remains **Prepare for Submission**. It still selects historical build
15 and older media, including the build 7 review attachment. Local build 18 is
not uploaded; build 17 remains the last known TestFlight build. A current tested
candidate and matching screenshots/video must replace those before any later
submission. App Review remains on hold pending Apple's clarification and renewed
owner authorization. No Add for Review/Submit action was taken.

Metadata checks validate current identity, copy limits and source consistency.
They do not certify global uniqueness, exclusive artwork rights or clearance of
Apple's private similarity comparison.
