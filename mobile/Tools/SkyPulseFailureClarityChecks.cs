// Copy into Assets/Editor of a disposable QA project. Never exercise a player save.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseFailureClarityChecks
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static object Get(object o, string n) => o.GetType().GetField(n, F).GetValue(o);
    static void Set(object o, string n, object value) => o.GetType().GetField(n, F).SetValue(o, value);
    static object Call(object o, string n, params object[] args) => o.GetType().GetMethod(n, F).Invoke(o, args);
    static void Check(bool ok, string note) { if (!ok) throw new Exception(note); Debug.Log("FAILURE_CLARITY_CHECK: " + note); }
    static Button Action(GameObject root, string label) => root.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == label);
    static bool Has(GameObject root, string fragment) => root.GetComponentsInChildren<Text>().Any(t => t.text.Contains(fragment));
    static Rect Target(Button button, Camera camera)
    {
        var rect = (RectTransform)button.transform; var pad = button.GetComponent<Image>().raycastPadding;
        var min = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector3(rect.rect.xMin + pad.x, rect.rect.yMin + pad.y, 0)));
        var max = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector3(rect.rect.xMax - pad.z, rect.rect.yMax - pad.w, 0)));
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    static void Fit(SkyPulseNativeGame game, Vector2Int size)
    {
        Canvas.ForceUpdateCanvases(); Call(game, "ApplySafeArea");
        var safe = (RectTransform)Get(game, "safeAreaRoot");
        safe.anchorMin = size.x == 1320 ? new Vector2(0, 102f / 2868) : Vector2.zero;
        safe.anchorMax = size.x == 1320 ? new Vector2(1, 2692f / 2868) : Vector2.one;
        safe.offsetMin = safe.offsetMax = Vector2.zero;
        Canvas.ForceUpdateCanvases(); Call(game, "FitInterfaceToSafeArea"); Canvas.ForceUpdateCanvases();
    }
    static void Controls(GameObject root, Camera camera, float pixelRatio, Rect safe)
    {
        var buttons = root.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            var bounds = Target(buttons[i], camera);
            Check(bounds.width / pixelRatio >= 44f && bounds.height / pixelRatio >= 44f, "44 pt terminal action: " + buttons[i].name);
            Check(bounds.xMin >= safe.xMin - 1 && bounds.xMax <= safe.xMax + 1 && bounds.yMin >= safe.yMin - 1 && bounds.yMax <= safe.yMax + 1, "Terminal action remains inside safe area: " + buttons[i].name);
            for (int j = i + 1; j < buttons.Length; j++) Check(!bounds.Overlaps(Target(buttons[j], camera)), "Terminal actions do not overlap: " + buttons[i].name + " / " + buttons[j].name);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = bounds.center }, hits);
            Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == buttons[i].gameObject, "Centre raycast reaches terminal action: " + buttons[i].name);
        }
    }
    static void EndlessFailure(SkyPulseNativeGame game, string reason)
    {
        Call(game, "SetHomeProtocol", SkyPulseMode.EndlessRoute); Call(game, "StartFlight");
        Set(game, "lastCrashReason", reason); Call(game, "EndFlight");
        Set(game, "state", Enum.Parse(Get(game, "state").GetType(), "GameOver"));
        Call(game, "RefreshScreens"); Call(game, "UpdateScoreFinish", 0f);
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if (Application.companyName != "SkyPulseQA20261006" || !Application.productName.Contains("QA")) throw new Exception("Disposable QA identity required");
        game.enabled = false; Set(game, "hapticsEnabled", false);
        var camera = (Camera)Get(game, "flightCamera"); var canvas = ((GameObject)Get(game, "uiRoot")).GetComponent<Canvas>();
        var oldTarget = camera.targetTexture; var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        try
        {
            foreach (var size in new[] { new Vector2Int(640, 1136), new Vector2Int(1320, 2868), new Vector2Int(1640, 2360) })
            {
                var rt = RenderTexture.GetTemporary(size.x, size.y, 24); camera.targetTexture = rt;
                try
                {
                    float ratio = size.x == 1320 ? 3 : 2;
                    var safe = size.x == 1320 ? new Rect(0, 102, 1320, 2590) : new Rect(0, 0, size.x, size.y);
                    foreach (var mode in new[] { SkyPulseMode.EndlessRoute, SkyPulseMode.PulseRelay, SkyPulseMode.BlackoutProtocol })
                    {
                        if (mode == SkyPulseMode.EndlessRoute) EndlessFailure(game, "GROUND CONTACT");
                        else
                        {
                            Call(game, "LaunchMission", mode, 0);
                            if (mode == SkyPulseMode.PulseRelay)
                            {
                                var relay = (RelaySession)Get(game, "relaySession"); relay.Collect(0); relay.Fail("GATE 01 · BOTH CRYSTALS REQUIRED");
                            }
                            else
                            {
                                var puzzle = ((BlackoutSession)Get(game, "blackoutSession")).Puzzle;
                                Set(game, "blackoutSession", new BlackoutSession(puzzle, null, puzzle.TraceLimit));
                            }
                            Call(game, "FinalizeMission");
                        }
                        var root = (GameObject)Get(game, mode == SkyPulseMode.EndlessRoute ? "gameOverScreen" : "missionResultScreen");
                        Check(root.activeInHierarchy, "Actual terminal flow remains visible: " + mode);
                        Check(Has(root, mode == SkyPulseMode.EndlessRoute ? "Tap sooner to climb" : mode == SkyPulseMode.PulseRelay ? "1 / 2 CRYSTALS" : "TRACE EXHAUSTED"), "Failure explains the actual cause: " + mode);
                        Fit(game, size); yield return null; camera.Render(); Controls(root, camera, ratio, safe);
                        int wallet = (int)Get(game, "crystals"), children = root.transform.childCount;
                        Call(game, "FinalizeMission");
                        if (mode != SkyPulseMode.EndlessRoute) { Call(game, "ApplyMissionFailureClarity", false, mode == SkyPulseMode.PulseRelay ? "GATE 01 · BOTH CRYSTALS REQUIRED" : "TRACE EXHAUSTED"); Call(game, "ApplyMissionFailureClarity", false, "TRACE EXHAUSTED"); }
                        Check((int)Get(game, "crystals") == wallet && root.transform.childCount == children, "Repeated completion/presentation cannot duplicate rewards or controls: " + mode);
                        foreach (var label in root.GetComponentsInChildren<Text>())
                            if (label.text.Contains("Tap sooner") || label.text.Contains("Collect both") || label.text.StartsWith("TRACE EXHAUSTED"))
                                Check(label.preferredHeight <= label.rectTransform.rect.height + 2f, "Failure explanation fits: " + label.text);
                        var retry = Action(root, mode == SkyPulseMode.EndlessRoute ? "RETRY" : mode == SkyPulseMode.PulseRelay ? "RETRY MISSION" : "RETRY VAULT");
                        var menu = Action(root, "MAIN MENU");
                        yield return null; retry.onClick.Invoke();
                        object session = Get(game, mode == SkyPulseMode.BlackoutProtocol ? "blackoutSession" : "relaySession");
                        retry.onClick.Invoke(); menu.onClick.Invoke();
                        Check(Get(game, "state").ToString() == "Playing" && !root.activeSelf, "Retry immediately exposes playable mode and rejects queued menu: " + mode);
                        if (mode != SkyPulseMode.EndlessRoute) Check(ReferenceEquals(session, Get(game, mode == SkyPulseMode.BlackoutProtocol ? "blackoutSession" : "relaySession")), "Old retry callback cannot replace the new session: " + mode);
                        Call(game, "PauseFlight"); yield return null;
                        var pause = (GameObject)Get(game, "pauseScreen");
                        Action(pause, "RESUME").onClick.Invoke(); Action(pause, "RETURN TO MENU").onClick.Invoke();
                        Check(Get(game, "state").ToString() == "Playing", "Retired pause cannot navigate after Resume: " + mode);
                        if (mode == SkyPulseMode.EndlessRoute) EndlessFailure(game, "GROUND CONTACT");
                        else if (mode == SkyPulseMode.PulseRelay) Call(game, "FinishRelayFailure", "GROUND CONTACT");
                        else
                        {
                            var puzzle = ((BlackoutSession)Get(game, "blackoutSession")).Puzzle;
                            Set(game, "blackoutSession", new BlackoutSession(puzzle, null, puzzle.TraceLimit)); Call(game, "FinalizeMission");
                        }
                        yield return null;
                        var homeRoot = (GameObject)Get(game, mode == SkyPulseMode.EndlessRoute ? "gameOverScreen" : "missionResultScreen");
                        Action(homeRoot, "MAIN MENU").onClick.Invoke(); retry.onClick.Invoke();
                        Check(Get(game, "state").ToString() == "Menu" && ((GameObject)Get(game, "homeScreen")).activeSelf, "Main Menu works immediately and rejects a retired retry: " + mode);
                        Call(game, "ResetToMenu");
                    }
                }
                finally { camera.targetTexture = oldTarget; RenderTexture.ReleaseTemporary(rt); }
            }
            foreach (int fps in new[] { 30, 60, 120 })
            {
                EndlessFailure(game, "GATE IMPACT");
                for (int i = 0; i < fps; i++) Call(game, "UpdateScoreFinish", 1f / fps);
                Check(((Text)Get(game, "resultReasonText")).text == "PIPE CONTACT · Aim through the opening", "Pipe-contact advice is stable at " + fps + " FPS");
            }
            Call(game, "LaunchMission", SkyPulseMode.BlackoutProtocol, 0); Call(game, "RestartBlackoutVault");
            var completed = (BlackoutSession)Get(game, "blackoutSession");
            foreach (int tile in SkyPulseMissionChecks.Solve(completed.Puzzle)) Call(game, "TouchBlackoutNode", tile);
            Check(completed.State == MissionSessionState.Succeeded && Get(game, "state").ToString() == "Playing" && !((GameObject)Get(game, "missionResultScreen")).activeSelf, "Intermediate puzzle advances immediately without a failure/report overlay");
            int next = (int)Get(game, "selectedMission"); Call(game, "ApplyMissionFailureClarity", true, completed.Result);
            Check((int)Get(game, "selectedMission") == next && !((GameObject)Get(game, "missionResultScreen")).activeSelf, "Terminal polish never inserts an intermediate progression modal");
            Debug.Log("SKYPULSE_FAILURE_CLARITY_PASS");
        }
        finally
        {
            camera.targetTexture = oldTarget; canvas.renderMode = oldMode; canvas.worldCamera = oldCamera;
            Call(game, "ResetToMenu"); Call(game, "ApplySafeArea");
        }
    }
}
