# Build 22 — owner results and issues

Candidate: SkyPulse 1.0.0 (22), runtime `1fd3dfa`.
Prepared 17 November 2026. **Owner gameplay testing has not started.**
The owner clarified this explicitly; earlier quick-choice replies are not completed
gameplay tests. Use the Notes checklist at the owner’s pace, with no timed prompts.
Technical install/source/save evidence is in RELEASE_RECORD.json; do not copy it
into the owner PASS column without checking the actual game.

## Agent preflight — 17 November 2026

These checks are separate from the owner results below.

- Build 22 is still installed and its process was observed through Apple’s device service.
- Device reports iPhone 17 Pro Max / iOS 27.3 beta. Stable-OS and physical iPad testing remain pending.
- The existing 186 source/metadata hashes and 1,168 world asset hashes still match QA. The full 2,559-entry Build 22 manifest is retained.
- Existing control, wing, gate, progression, world and save/regression log evidence was verified; it was not relabelled as new phone gameplay testing.
- QA-CAPTURE-001 was reproduced and fixed in the review tool. All fifteen birds pass 150 focused complete-frame checks, with 60 updated captures and zero C# compiler errors/warnings.
- Game Performance Overview is available with per-frame metrics. The first trace never started: Instruments listed the iPhone offline and timed out (exit 13). CoreDevice still found a compatible developer image and reported passcode required. No measured FPS/memory/performance pass is recorded. The owner chose manual testing without profiling; no further trace attempt is pending.
- Feature freeze is active. No installed runtime or real save was altered.

## Resolved QA issue: QA-CAPTURE-001

Severity: minor evidence defect. Screen: the 0 ms bird tap-preview image.
The capture bypassed the last presentation pass after Flap, so its first picture
could show the legacy sprite over the native 3D pilot. Complete-frame rendering
through the real LateUpdate hides it. The QA helper now completes that boundary
and checks all fallback renderers and selected identity before each image.

Status: fixed in the review tool and focused checks passed. No confirmed shipping
runtime defect or new phone build is claimed. Evidence:
[focused report](../../Development/Current/UnityAcceptance/BIRD_TAP_CAPTURE.json),
[corrected sheet](../../Development/Current/UnityAcceptance/Bird-Tap-Phases.png).

## Agent recheck — 18 November 2026

Three isolated Unity suites passed with zero C# compiler errors/warnings. They
include native bird strokes, touch/UI separation, matching automatic Relay gates,
puzzle progression/finalization, save interruption recovery, and all twelve world
transitions at simulated 30/60/120 Hz. No text-overflow findings were reported.
Three missing sky textures were restored byte-for-byte to the Build 22 candidate.
Source integrity, strict signatures, metadata guards and all four public links pass.
The missing archived evidence-note link was corrected. Build 22 is still installed;
the app was not launched, the owner save was not changed and no performance trace
was recorded. Owner result rows below stay **NOT TESTED** until supplied.
Full evidence: [POLISH_REPORT.json](POLISH_REPORT.json).

## Session record

Date/time:
Device / iOS:
Installed version / build:
Brightness / sound / haptics / Reduced Motion:
Session duration:

## Results

Use PASS, FAIL or NOT TESTED with a reason. Link an issue ID for every failure.

| Area | Result | Notes / issue ID |
| --- | --- | --- |
| First ten-minute smoke check | NOT TESTED | |
| Homes and navigation | NOT TESTED | |
| Bird tap strokes / identity / crash artwork | NOT TESTED | |
| Twelve worlds and pipe joins | NOT TESTED | Use world table in checklist |
| Pickups / result wallet totals | NOT TESTED | |
| Relay crystals / gates / Next Mission | NOT TESTED | |
| Blackout story / Plan / instant progression | NOT TESTED | |
| City restoration / optional daily case | NOT TESTED | |
| Hangar / training / breeding | NOT TESTED | Mark unaffordable purchases separately |
| Tech / Core costs and benefits | NOT TESTED | |
| Saves / pause / background / reopen | NOT TESTED | |
| Small phone / iPad layout | NOT TESTED | |
| Longer session / frame pacing / memory / heat | NOT TESTED | Actual measurements required |
| Audio / haptics / Reduced Motion | NOT TESTED | |
| Current support/privacy/guide and store media | NOT TESTED | Fresh accepted device media pending |

## Issue template — duplicate for each issue

ID:
Severity: blocker / major / minor / visual preference
Mode / screen:
Bird / world / circuit / vault:
Steps (what you tapped):
Expected:
Actual:
Repeats after retry / reopen:
Screenshot or video filename:
Status: open / fixed in build __ / retested

## Release gates still open

- Owner checklist and visual sign-off.
- Reproducible defects resolved and affected checks repeated on the matching candidate.
- Real iPhone/iPad usability and measured performance evidence.
- Current approved device screenshots/preview and review walkthrough.
- Owner reports saving macwebstudio26@gmail.com; independently recheck it when browser access returns. Current public-site publication evidence is recorded separately.
- Owner reports saving China mainland as excluded. Independently verify that selection; review other applicable App Store compliance notices without guessing declarations.
- Apple’s unidentified similarity/account concern clarified and addressed.
- Renewed explicit owner authorization to submit for App Review.

No public review submission is authorised by filling out this file.
