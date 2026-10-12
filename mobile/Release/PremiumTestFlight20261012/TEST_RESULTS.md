# Premium candidate validation — 12 October 2026

The full isolated Unity 6000.6.0f1 suite passes all nine expected markers, including the 1,300-gate Endless route regression, authored Relay missions/Blackout puzzle solutions, daily reward recovery, legacy saves, roster/tech contracts, safe-area widths and motion checks. The static audit finds one enabled SkyPulse scene, 152 Resources assets and twelve runtime scripts; QA helpers are excluded from player compilation. No C# compiler errors occurred.

New checks confirm that premium portal glints freeze during pause and Reduced Motion. The protected original-source check passes 27 method comparisons, constants/catalogues and seven normalized integration hooks against build-7 source `026e382`. The main Endless runtime and mission/daily rules remain byte-identical to the preceding candidate. Existing artwork/GUIDs remain unchanged.

The full suite rendered 63 stills and 280 animated frames. The taller phone Home comparison was inspected. Captures use isolated saves and assisted flight. Physical-device validation of this candidate begins with the requested TestFlight build; prior build 7 footage/sign-offs are historical.

Unity's DeviceSDK export succeeds. The source/asset/meta hash manifest matches every current candidate asset, including all twelve runtime scripts. A source revision and manifest hash are recorded in `SkyPulse-build-info.json`. Archive/signature/processing/group results and log hashes are recorded in `RELEASE_RECORD.json` once verified.

The initial Desktop-stage archive failed code signing because Finder metadata was attached to app/framework/storyboard folders. Signing staging was moved to temporary storage; the initial failure log is retained alongside the corrected archive log. Only Finder/resource metadata handling was changed; game code and security settings were not altered to work around that error.

Known Editor Search indexing/service diagnostics and the deliberately injected medal-save IOException remain in the QA logs. They are retained rather than concealed. Public App Store submission remains on hold, and no fresh device pass or Apple approval is claimed.

Final replacement candidate: build 9. It changes the runtime links to the branded domain and adds metadata/export guards; all twelve runtime scripts remain identical to the passed visual candidate. The metadata preflight and final build 9 archive are checked separately. Build 8 attempt logs are retained.

Build 9's archive succeeds and its signature passes strict/deep verification. Its compiled presentation markers are present. The metadata preflight verifies bundle/build/device families/privacy declarations and the canonical branded URL bytes in the actual packaged app, with no older hosted URLs. All 364 exported manifest entries match the source snapshot. Field limits and four intentionally invalid metadata fixtures were checked separately.

Xcode Organizer upload completed with the missing UnityRuntime.framework dSYM warning retained in the distribution evidence. Apple processed build 9; the existing owner's SkyPulse Internal group shows **Testing**, with tester notes saved. The App Store description, promotional text, keywords and reviewer notes match the local copy after reload, and the new subtitle is saved. Build 9 is selected in the unsubmitted public draft; its label changed to Prepare for Submission when saved, and no review was requested. Shared beta description/notes and its corrected privacy URL were also saved. These results establish delivery and automated validation, not a new device pass.
