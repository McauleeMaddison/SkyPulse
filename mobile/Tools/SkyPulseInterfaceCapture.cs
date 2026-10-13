using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SkyPulse.Mobile;

// QA-only: copy into Assets/Editor of an isolated product whose name contains QA.
// Run with graphics enabled and SKYPULSE_QA_OUTPUT set. This fixture uses real
// shipping UI handlers/renderers but artificial saves and safe-area dimensions.
[InitializeOnLoad]
public static class SkyPulseInterfaceCapture
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly List<string> checks = new List<string>();
    static readonly StringBuilder audit = new StringBuilder("screen\tkind\tname\tx\ty\tw\th\tdetail\n");
    static object Get(object o, string n) => o.GetType().GetField(n, F).GetValue(o);
    static void Set(object o, string n, object v) => o.GetType().GetField(n, F).SetValue(o, v);
    static object Call(object o, string n, params object[] a)
    {
        foreach (var m in o.GetType().GetMethods(F))
        {
            if (m.Name != n || m.GetParameters().Length != a.Length) continue;
            return m.Invoke(o, a);
        }
        throw new MissingMethodException(n);
    }
    static Array Catalog(string n) => (Array)typeof(SkyPulseNativeGame).GetField(n, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
    static void Check(bool value, string message) { if (!value) throw new Exception(message); checks.Add("PASS " + message); }
    static SkyPulseInterfaceCapture() { EditorApplication.update += Tick; }
    public static void Run()
    {
        Check(Application.productName.Contains("QA"), "Isolated QA product name");
        PlayerPrefs.DeleteAll(); PlayerPrefs.Save();
        SessionState.SetBool("SkyPulseInterfaceCapture", true); EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if (!SessionState.GetBool("SkyPulseInterfaceCapture", false) || !EditorApplication.isPlaying) return;
        SessionState.SetBool("SkyPulseInterfaceCapture", false);
        new GameObject("Interface capture runner").AddComponent<SkyPulseInterfaceCaptureRunner>().Begin(Capture());
    }
    internal static void Failed(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    static IEnumerator Capture()
    {
        var folder = Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");
        Check(!string.IsNullOrEmpty(folder), "Output folder specified"); Directory.CreateDirectory(folder);
        var game = UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();
        if (game == null) game = new GameObject("Interface fixture").AddComponent<SkyPulseNativeGame>();
        game.enabled = false; Set(game, "hapticsEnabled", false);
        var source = (AudioSource)Get(game, "audioSource"); if (source != null) source.mute = true;
        var camera = (Camera)Get(game, "flightCamera");
        var canvas = ((GameObject)Get(game, "uiRoot")).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
        var skins = Catalog("Skins"); var upgrades = Catalog("Upgrades");
        var owned = (HashSet<string>)Get(game, "ownedSkinIds");
        var count = 0;
        foreach (var size in new[] { new Vector2Int(540, 960), new Vector2Int(660, 1434) })
        {
            var rt = RenderTexture.GetTemporary(size.x, size.y, 24); camera.targetTexture = rt; camera.rect = new Rect(0, 0, 1, 1);
            Call(game, "RefreshViewportDecor");
            foreach (var scenario in new[] { "home", "gameplay", "pause", "result", "hangar", "hangar-bottom", "upgrades", "upgrades-bottom", "purchase-bird", "purchase-upgrade", "purchase-insufficient", "unlock", "hangar-unlocked", "long-balance", "purchase-long-name-price" })
            {
                Call(game, "CloseUnlockReveal"); Call(game, "ClosePurchaseModal");
                ((GameObject)Get(game, "privacyScreen")).SetActive(false);
                ((IDictionary)Get(game, "upgradeLevels")).Clear(); owned.Clear();
                Set(game, "equippedSkin", skins.GetValue(0)); Set(game, "crystals", 1240); Set(game, "best", 124);
                Set(game, "ambientTime", 1.2f); Set(game, "reduceMotionEnabled", true); Call(game, "UpdateComfortCopy");
                Call(game, "ApplyEquippedVisuals"); Call(game, "ResetToMenu");
                yield return null;
                if (scenario == "gameplay" || scenario == "pause" || scenario == "result")
                {
                    Call(game, "StartFlight"); Set(game, "score", 27); ((Text)Get(game, "hudScoreText")).text = "27"; Set(game, "birdY", 0f); Set(game, "birdVelocity", 0f); Call(game, "UpdateBird", 0f);
                    if (scenario == "pause") Call(game, "PauseFlight");
                    if (scenario == "result")
                    {
                        Set(game, "runCrystalsCollected", 36); Set(game, "runFarthestWorldIndex", 1);
                        Call(game, "EndFlight"); Set(game, "state", Enum.Parse(Get(game, "state").GetType(), "GameOver")); Call(game, "RefreshScreens");
                    }
                }
                if (scenario.StartsWith("hangar") || scenario.StartsWith("purchase") || scenario == "unlock") Call(game, "OpenHangar");
                if (scenario.StartsWith("upgrades") || scenario == "purchase-upgrade" || scenario == "purchase-insufficient") Call(game, "OpenUpgrades");
                yield return null; Fit(game);
                if (scenario == "hangar" || scenario == "upgrades") CheckScrollRoutes(game, camera, scenario);
                if (scenario.EndsWith("-bottom"))
                {
                    var scroll = (ScrollRect)Get(game, "customizeScroll"); scroll.StopMovement(); scroll.verticalNormalizedPosition = 0f;
                    Canvas.ForceUpdateCanvases(); CheckLastButtonVisible(scroll, scenario);
                }
                if (scenario == "purchase-bird")
                {
                    // Click the actual second unowned card, then inspect its real modal.
                    var buttons = ((RectTransform)Get(game, "customizeContent")).GetComponentsInChildren<Button>();
                    Check(buttons.Length >= 2, "Hangar has clickable bird cards");
                    var cardPointer = Pointer(camera, buttons[1].transform as RectTransform);
                    var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(cardPointer, hits);
                    Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == buttons[1].gameObject,
                        "Graphic raycast reaches unowned card instead of decorative artwork");
                    ExecuteEvents.Execute(buttons[1].gameObject, cardPointer, ExecuteEvents.pointerClickHandler);
                    Check(((GameObject)Get(game, "purchaseModal")).activeSelf, "Unowned bird card opens purchase through click handler");
                }
                if (scenario == "purchase-upgrade" || scenario == "purchase-insufficient")
                {
                    Set(game, "crystals", scenario == "purchase-insufficient" ? 0 : 999999);
                    Call(game, "SelectUpgrade", upgrades.GetValue(0));
                    Check(((Button)Get(game, "purchaseConfirmButton")).interactable == (scenario != "purchase-insufficient"), "Purchase confirm matches available balance " + scenario);
                }
                if (scenario == "unlock")
                {
                    Set(game, "crystals", 999999); Call(game, "SelectSkin", skins.GetValue(1)); Call(game, "ConfirmPurchase"); Call(game, "UpdateUnlockReveal", .5f);
                    Check(((GameObject)Get(game, "unlockRevealModal")).activeSelf, "Bird purchase opens unlock reveal");
                    Check((string)Get(Get(game, "equippedSkin"), "Id") == (string)Get(skins.GetValue(1), "Id"), "Newly purchased bird equipped");
                }
                if (scenario == "hangar-unlocked")
                {
                    owned.Add((string)Get(skins.GetValue(1), "Id")); owned.Add((string)Get(skins.GetValue(2), "Id"));
                    Set(game, "equippedSkin", skins.GetValue(1)); Call(game, "ApplyEquippedVisuals"); Call(game, "RebuildCustomizeGrid");
                }
                if (scenario == "long-balance") { Set(game, "crystals", 9999999); Set(game, "best", 9999999); Call(game, "UpdateCrystalLabels"); Call(game, "UpdateModeCopy"); }
                if (scenario == "purchase-long-name-price")
                    Call(game, "OpenPurchaseModal", "CELESTIAL CHRONO PHOENIX", "THIS BIRD WILL BE EQUIPPED AFTER UNLOCKING", 9999999, Color.cyan, (Sprite)Call(game, "LoadSprite", Get(skins.GetValue(1), "ArtPath")));
                // A warm render allows legacy Unity Text to grow/rebuild its shared
                // font atlas before the evidence frame. Otherwise a rapid scenario
                // switch can capture temporarily missing or blurred static labels.
                yield return null; Fit(game); camera.Render();
                yield return null; Canvas.ForceUpdateCanvases();
                Save(game, camera, size, Path.Combine(folder, size.x + "-" + scenario + ".png"), size.x + "-" + scenario); count++;
            }
            camera.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
        }
        File.WriteAllText(Path.Combine(folder, "hierarchy-audit.tsv"), audit.ToString());
        File.WriteAllText(Path.Combine(folder, "PASS.txt"), count + " actual Unity Camera.Render screenshots at 540x960 and 660x1434.\n" + string.Join("\n", checks) + "\nThese are editor renders using artificial isolated saves, not physical iPhone screenshots. Text and button geometry is recorded in hierarchy-audit.tsv. Sub-44-pixel controls are flagged for review; pixels in a render do not establish physical points on a device.\n");
        Debug.Log("SKYPULSE_INTERFACE_CAPTURE_PASS " + count + " screenshots " + folder); EditorApplication.Exit(0);
    }
    static PointerEventData Pointer(Camera camera, RectTransform rect)
    {
        var center = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        return new PointerEventData(EventSystem.current) { pointerId = 0, button = PointerEventData.InputButton.Left, position = center, pressPosition = center };
    }
    static void CheckScrollRoutes(SkyPulseNativeGame game, Camera camera, string scenario)
    {
        var scroll = (ScrollRect)Get(game, "customizeScroll");
        Check(scroll.vertical && !scroll.horizontal && scroll.inertia, scenario + " vertical touch scrolling configured");
        Check(scroll.content.rect.height > scroll.viewport.rect.height, scenario + " has scroll range");
        foreach (var button in scroll.content.GetComponentsInChildren<Button>())
        {
            Check(ExecuteEvents.GetEventHandler<IDragHandler>(button.gameObject) == scroll.gameObject, scenario + " card forwards drag: " + button.name);
            Check(ExecuteEvents.GetEventHandler<IPointerClickHandler>(button.gameObject) == button.gameObject, scenario + " card owns click: " + button.name);
        }
        var touch = Pointer(camera, scroll.viewport); var before = scroll.content.anchoredPosition.y;
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.initializePotentialDrag); ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.beginDragHandler);
        touch.dragging = true; touch.position += Vector2.up * 120f; ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.dragHandler);
        Check(scroll.content.anchoredPosition.y > before + 1f, scenario + " swipe up scrolls");
        var after = scroll.content.anchoredPosition.y; touch.position -= Vector2.up * 80f; ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.dragHandler);
        Check(scroll.content.anchoredPosition.y < after - 1f, scenario + " swipe down scrolls");
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.endDragHandler);
        ((GameObject)Get(game, "customizeScreen")).GetComponent<SkyPulseRoundStartSurface>().OnPointerClick(touch);
        Check(Get(game, "state").ToString() == "Customize" && !((GameObject)Get(game, "purchaseModal")).activeSelf, scenario + " swipe neither launches flight nor purchase");
        scroll.StopMovement(); scroll.verticalNormalizedPosition = 1f; Canvas.ForceUpdateCanvases();
    }
    static void CheckLastButtonVisible(ScrollRect scroll, string scenario)
    {
        var buttons = scroll.content.GetComponentsInChildren<Button>(); Check(buttons.Length > 0, scenario + " buttons exist");
        var last = buttons[buttons.Length - 1].transform as RectTransform; var corners = new Vector3[4]; last.GetWorldCorners(corners);
        var lo = scroll.viewport.InverseTransformPoint(corners[0]); var hi = scroll.viewport.InverseTransformPoint(corners[2]);
        Check(lo.y >= scroll.viewport.rect.yMin - 1f && hi.y <= scroll.viewport.rect.yMax + 1f, scenario + " last action fully reachable at bottom");
    }
    static void Fit(SkyPulseNativeGame game)
    {
        Canvas.ForceUpdateCanvases(); var safe = (RectTransform)Get(game, "safeAreaRoot");
        safe.anchorMin = new Vector2(0, .035f); safe.anchorMax = new Vector2(1, .935f); safe.offsetMin = safe.offsetMax = Vector2.zero;
        Canvas.ForceUpdateCanvases(); Call(game, "FitInterfaceToSafeArea"); Canvas.ForceUpdateCanvases();
    }
    static void Save(SkyPulseNativeGame game, Camera camera, Vector2Int size, string path, string scenario)
    {
        Call(game, "UpdateMenuBird", .1f); Fit(game);
        foreach (var graphic in ((GameObject)Get(game, "uiRoot")).GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.GetType().Name != "SkyPulseUiGlyph") continue;
            Check(!graphic.raycastTarget, scenario + " decorative glyph cannot steal input: " + graphic.name);
            if (graphic.gameObject.activeInHierarchy && (bool)Get(graphic, "Animate"))
            {
                Call(graphic, "Update");
                Check(Mathf.Abs(graphic.canvasRenderer.GetAlpha() - 1f) < .0001f, scenario + " Reduced Motion glyph alpha stays fixed: " + graphic.name);
            }
        }
        camera.Render(); RenderTexture.active = camera.targetTexture;
        var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image); RenderTexture.active = null;
        var safe = (RectTransform)Get(game, "safeAreaRoot"); var content = (RectTransform)Get(game, "interfaceContentRoot");
        Check(content.rect.width * content.localScale.x <= safe.rect.width + .01f && content.rect.height * content.localScale.y <= safe.rect.height + .01f, scenario + " interface fits simulated safe area");
        foreach (var button in ((GameObject)Get(game, "uiRoot")).GetComponentsInChildren<Button>()) Audit(camera, scenario, "button", button.transform as RectTransform, button.interactable ? "enabled" : "disabled");
        foreach (var label in ((GameObject)Get(game, "uiRoot")).GetComponentsInChildren<Text>())
            Audit(camera, scenario, "text", label.rectTransform, "font=" + label.fontSize + ";bestFit=" + label.resizeTextForBestFit + ";preferred=" + label.preferredWidth.ToString("F1") + "x" + label.preferredHeight.ToString("F1") + ";" + label.text.Replace('\n', ' ').Replace('\t', ' '));
    }
    static void Audit(Camera camera, string screen, string kind, RectTransform rect, string detail)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners); var lo = RectTransformUtility.WorldToScreenPoint(camera, corners[0]); var hi = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        var w = hi.x - lo.x; var h = hi.y - lo.y;
        if (kind == "button" && (w < 44 || h < 44)) detail += ";REVIEW-under44-render-pixels";
        audit.AppendLine(screen + "\t" + kind + "\t" + rect.name + "\t" + lo.x.ToString("F1") + "\t" + lo.y.ToString("F1") + "\t" + w.ToString("F1") + "\t" + h.ToString("F1") + "\t" + detail);
    }
}
public sealed class SkyPulseInterfaceCaptureRunner : MonoBehaviour
{
    public void Begin(IEnumerator routine) { StartCoroutine(Guard(routine)); }
    IEnumerator Guard(IEnumerator routine)
    {
        while (true)
        {
            bool more; object next;
            try { more = routine.MoveNext(); next = more ? routine.Current : null; }
            catch (Exception e) { SkyPulseInterfaceCapture.Failed(e); yield break; }
            if (!more) yield break;
            yield return next;
        }
    }
}
