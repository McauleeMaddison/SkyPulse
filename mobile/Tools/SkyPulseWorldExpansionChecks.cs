using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

// Disposable QA project only. These tests intentionally reset its isolated save.
[InitializeOnLoad]
public static class SkyPulseWorldExpansionChecks
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static object Get(object o, string n) => o.GetType().GetField(n, F).GetValue(o);
    static void Set(object o, string n, object v) => o.GetType().GetField(n, F).SetValue(o, v);
    static object Call(object o, string n, params object[] a) => o.GetType().GetMethod(n, F).Invoke(o, a);
    static void Check(bool c, string m) { if (!c) throw new Exception(m); Debug.Log("WORLD_CHECK: " + m); }
    static SkyPulseWorldExpansionChecks() { EditorApplication.update += Tick; }
    public static void Run()
    {
        if (!Application.productName.Contains("QA")) throw new Exception("Disposable QA product required");
        PlayerPrefs.DeleteAll(); PlayerPrefs.Save();
        SessionState.SetBool("WorldExpansionChecks", true); EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if (!SessionState.GetBool("WorldExpansionChecks", false) || !EditorApplication.isPlaying) return;
        SessionState.SetBool("WorldExpansionChecks", false);
        new GameObject("Build 6 QA runner").AddComponent<SkyPulseWorldExpansionRunner>().Begin(Execute());
    }
    static Button FindButton(GameObject root, string label)
    {
        foreach (var b in root.GetComponentsInChildren<Button>())
            if (b.GetComponentInChildren<Text>().text == label) return b;
        throw new Exception("Missing button: " + label);
    }
    static void ValidateWorldExpansion(SkyPulseNativeGame game)
    {
        var type = typeof(SkyPulseNativeGame);
        var worlds = (Array)type.GetField("Worlds", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var lookup = type.GetMethod("WorldIndexForScore", BindingFlags.Static | BindingFlags.NonPublic);
        var expected = new[] { "neon_city", "aurora_rise", "solar_drift", "midnight_tide", "velvet_dawn", "crystal_night", "jade_horizon", "violet_rain", "eclipse", "cobalt_storm", "amber_skies", "polar_glow" };
        Check(worlds.Length == expected.Length, "All twelve route worlds present");
        for (int i = 0; i < expected.Length; i++)
        {
            var world = worlds.GetValue(i);
            Check((string)Get(world, "Id") == expected[i], "Stable world order: " + expected[i]);
            Check(Resources.Load<Texture2D>((string)Get(world, "BackgroundPath")) != null, "Real texture for " + expected[i]);
            Check((int)lookup.Invoke(null, new object[] { i * 15 }) == i, "World begins at its milestone: " + expected[i]);
            Check((int)lookup.Invoke(null, new object[] { i * 15 + 14 }) == i, "World lasts fifteen gates: " + expected[i]);
        }
        Check((int)lookup.Invoke(null, new object[] { 180 }) == 0, "Full route wraps at score 180");
        Check((int)lookup.Invoke(null, new object[] { 360 }) == 0, "Second full route wraps");
        Call(game, "StartFlight");
        for (int i = 1; i < worlds.Length; i++)
        {
            Set(game, "score", i * 15); Call(game, "BeginWorldTransition", i);
            Call(game, "UpdateWorldTransition", 2f); Call(game, "UpdateWorldTransition", 2f);
            Check((int)Get(game, "routeWorldIndex") == i, "Transition completes: " + expected[i]);
            Check(((Text)Get(game, "hudModeText")).text == (string)Get(worlds.GetValue(i), "Name"), "HUD world label agrees");
        }
        int crystals = (int)Get(game, "crystals");
        Call(game, "SaveProgress"); Set(game, "farthestWorldIndex", 0); Call(game, "LoadProgress");
        Check((int)Get(game, "farthestWorldIndex") == 11, "New furthest-world progress survives reload");
        Check((int)Get(game, "crystals") == crystals, "World progression preserves crystal balance");
        PlayerPrefs.SetInt("skypulse.native.farthest-world", 2); Call(game, "LoadProgress");
        Check((int)Get(game, "farthestWorldIndex") == 2, "Existing build 6 world progress remains valid");
        Call(game, "ResetToMenu");
        Check(((Text)Get(game, "menuRouteText")).text.Contains("MIDNIGHT TIDE"), "Existing player gets next newly available milestone");
        var home = (GameObject)Get(game, "homeScreen"); var guide = (GameObject)Get(game, "flightGuideScreen");
        FindButton(home, "WORLDS & POWER-UPS").onClick.Invoke();
        var prev = FindButton(guide, "PREVIOUS"); var next = FindButton(guide, "NEXT");
        Check(!prev.interactable, "Guide starts with Previous disabled");
        for (int page = 0; page < 4; page++)
        {
            var text = string.Join("|", Array.ConvertAll(guide.GetComponentsInChildren<Text>(), t => t.text));
            for (int slot = 0; slot < 3; slot++)
                Check(text.Contains((string)Get(worlds.GetValue(page * 3 + slot), "Name")), "Guide shows each reachable world");
            if (page < 3) { Check(next.interactable, "Next enabled before final page"); next.onClick.Invoke(); }
        }
        Check(!next.interactable, "Guide stops after the final three worlds");
        for (int i = 0; i < 3; i++) prev.onClick.Invoke();
        Check(!prev.interactable, "Guide returns to first page without underflow");
        FindButton(guide, "BACK TO HOME").onClick.Invoke();
    }
    static IEnumerator Execute()
    {
        var game = UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();
        if (game == null) game = new GameObject("QA game").AddComponent<SkyPulseNativeGame>();
        game.enabled = false;
        Set(game, "hapticsEnabled", false);
        ((AudioSource)Get(game, "audioSource")).mute = true;
        var home = (GameObject)Get(game, "homeScreen");
        var guide = (GameObject)Get(game, "flightGuideScreen");
        Check(!guide.activeSelf, "Guide starts closed");
        FindButton(home, "WORLDS & POWER-UPS").onClick.Invoke();
        Check(guide.activeSelf, "Home opens route and pickup guide");
        Call(game, "Update");
        Check(Get(game, "state").ToString() == "Menu", "Guide keeps menu state");
        FindButton(guide, "BACK TO HOME").onClick.Invoke();
        Check(!guide.activeSelf, "Guide closes to Home");
        Check(((Text)Get(game, "menuHangarProgressText")).text == "1 / 15 BIRDS OWNED", "Fresh save shows actual starting bird count");
        Check(((Text)Get(game, "menuTechProgressText")).text == "0 / 27 TECH LEVELS", "Fresh save shows tech capacity");
        FindButton(home, "TECH TREE").onClick.Invoke();
        Check(((Text)Get(game, "customizeTitle")).text == "TECH TREE", "Home Tech Tree opens correct collection");
        Call(game, "ResetToMenu");
        FindButton(home, "BIRD HANGAR").onClick.Invoke();
        for (int i = 0; i < 15; i++) Call(game, "ShowNextHangarBird");
        Check((int)Get(game, "hangarPageIndex") == 0, "Hangar navigation wraps all 15 birds");
        Call(game, "ResetToMenu");
        SkyPulseBetaChecks.Run(game);
        ValidateWorldExpansion(game);
        Debug.Log("SKYPULSE_WORLD_EXPANSION_GAMEPLAY_PASS");
        PlayerPrefs.DeleteAll(); PlayerPrefs.Save();
        game.enabled = false;
        Set(game, "crystals", 0);
        ((IDictionary)Get(game, "upgradeLevels")).Clear();
        ((HashSet<string>)Get(game, "ownedUpgradeIds")).Clear();
        ((HashSet<string>)Get(game, "ownedSkinIds")).Clear();
        Call(game, "LoadProgress"); Call(game, "ResetToMenu"); Call(game, "UpdateCrystalLabels");
        var folder = Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");
        Directory.CreateDirectory(folder);
        var camera = (Camera)Get(game, "flightCamera");
        var canvas = ((GameObject)Get(game, "uiRoot")).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
        foreach (var size in new[] { new Vector2Int(750, 1334), new Vector2Int(1320, 2868), new Vector2Int(1640, 2360) })
        {
            var rt = RenderTexture.GetTemporary(size.x, size.y, 24); camera.targetTexture = rt;
            camera.rect = new Rect(0, 0, 1, 1); Call(game, "RefreshViewportDecor");
            foreach (var screen in new[] { "home", "guide", "guide-page-2", "guide-page-3", "guide-page-4", "hangar", "tech", "world-5", "world-8", "world-9", "world-10", "world-11" })
            {
                guide.SetActive(false); Call(game, "ResetToMenu");
                if (screen.StartsWith("guide"))
                {
                    FindButton(home, "WORLDS & POWER-UPS").onClick.Invoke();
                    var previous = FindButton(guide, "PREVIOUS");
                    while (previous.interactable) previous.onClick.Invoke();
                    int page = screen == "guide" ? 1 : int.Parse(screen.Substring(11));
                    for (int step = 1; step < page; step++) FindButton(guide, "NEXT").onClick.Invoke();
                }
                if (screen.StartsWith("world-"))
                {
                    int index = int.Parse(screen.Substring(6));
                    Call(game, "StartFlight"); Set(game, "score", index * 15);
                    Call(game, "BeginWorldTransition", index);
                    Call(game, "UpdateWorldTransition", 2f); Call(game, "UpdateWorldTransition", 2f);
                    Call(game, "UpdateModeCopy");
                }
                if (screen == "hangar") Call(game, "OpenHangar");
                if (screen == "tech") Call(game, "OpenUpgrades");
                yield return null;
                Canvas.ForceUpdateCanvases();
                var safe = (RectTransform)Get(game, "safeAreaRoot");
                safe.anchorMin = new Vector2(0, .035f); safe.anchorMax = new Vector2(1, .935f); safe.offsetMin = safe.offsetMax = Vector2.zero;
                Canvas.ForceUpdateCanvases(); Call(game, "FitInterfaceToSafeArea");
                Call(game, "UpdateMenuBird", .1f); Canvas.ForceUpdateCanvases(); camera.Render();
                yield return null;
                Canvas.ForceUpdateCanvases(); camera.Render();
                if (screen == "home" || screen.StartsWith("guide"))
                    foreach (var label in (screen == "home" ? home : guide).GetComponentsInChildren<Text>())
                    {
                        if (!label.resizeTextForBestFit)
                            Check(label.preferredHeight <= label.rectTransform.rect.height + 2f, size.x + " " + screen + " text height fits: " + label.text);
                    }
                RenderTexture.active = rt;
                var capture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); capture.Apply();
                File.WriteAllBytes(Path.Combine(folder, size.x + "-" + screen + ".png"), capture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(capture); RenderTexture.active = null;
            }
            camera.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
        }
        File.WriteAllText(Path.Combine(folder, "PASS.txt"), "World expansion boundaries, guide, saves, gameplay regression and editor captures passed. Artificial isolated QA save; not native device evidence.\n");
        Debug.Log("SKYPULSE_WORLD_CHECKS_PASS"); EditorApplication.Exit(0);
    }
}
public sealed class SkyPulseWorldExpansionRunner : MonoBehaviour
{
    public void Begin(IEnumerator routine) { StartCoroutine(Guard(routine)); }
    IEnumerator Guard(IEnumerator routine)
    {
        while (true)
        {
            bool more; object next;
            try { more = routine.MoveNext(); next = more ? routine.Current : null; }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
            if (!more) yield break;
            yield return next;
        }
    }
}
