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
public static class SkyPulseBuild6Checks
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static object Get(object o, string n) => o.GetType().GetField(n, F).GetValue(o);
    static void Set(object o, string n, object v) => o.GetType().GetField(n, F).SetValue(o, v);
    static object Call(object o, string n, params object[] a) => o.GetType().GetMethod(n, F).Invoke(o, a);
    static void Check(bool c, string m) { if (!c) throw new Exception(m); Debug.Log("BUILD6_CHECK: " + m); }
    static SkyPulseBuild6Checks() { EditorApplication.update += Tick; }
    public static void Run()
    {
        if (!Application.productName.Contains("QA")) throw new Exception("Disposable QA product required");
        PlayerPrefs.DeleteAll(); PlayerPrefs.Save();
        SessionState.SetBool("Build6Checks", true); EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if (!SessionState.GetBool("Build6Checks", false) || !EditorApplication.isPlaying) return;
        SessionState.SetBool("Build6Checks", false);
        new GameObject("Build 6 QA runner").AddComponent<SkyPulseBuild6Runner>().Begin(Execute());
    }
    static Button FindButton(GameObject root, string label)
    {
        foreach (var b in root.GetComponentsInChildren<Button>())
            if (b.GetComponentInChildren<Text>().text == label) return b;
        throw new Exception("Missing button: " + label);
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
        Debug.Log("SKYPULSE_BUILD6_GAMEPLAY_PASS");
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
            foreach (var screen in new[] { "home", "guide", "hangar", "tech" })
            {
                guide.SetActive(false); Call(game, "ResetToMenu");
                if (screen == "guide") FindButton(home, "WORLDS & POWER-UPS").onClick.Invoke();
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
                if (screen == "home" || screen == "guide")
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
        File.WriteAllText(Path.Combine(folder, "PASS.txt"), "Build 6 UI, gameplay regression and editor captures passed. Artificial isolated QA save; not native device evidence.\n");
        Debug.Log("SKYPULSE_BUILD6_CHECKS_PASS"); EditorApplication.Exit(0);
    }
}
public sealed class SkyPulseBuild6Runner : MonoBehaviour
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
