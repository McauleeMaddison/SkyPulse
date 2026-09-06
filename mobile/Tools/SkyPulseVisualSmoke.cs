using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulseVisualSmoke
{
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Get(object obj, string name) => obj.GetType().GetField(name, Flags).GetValue(obj);
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Flags).Invoke(obj, args);
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static SkyPulseVisualSmoke()
    {
        if (SessionState.GetBool("SkyPulseVisualSmoke", false))
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode) EditorApplication.delayCall += Execute;
            };
    }
    public static void Run()
    {
        SessionState.SetBool("SkyPulseVisualSmoke", true);
        EditorApplication.EnterPlaymode();
    }
    static void CheckTouchScrolling(SkyPulseNativeGame game, string openMethod)
    {
        Call(game, openMethod);
        Canvas.ForceUpdateCanvases();
        var scroll = (ScrollRect)Get(game, "customizeScroll");
        Check(scroll.vertical && !scroll.horizontal && scroll.inertia, "Mobile scroll configuration invalid");
        Check(scroll.content.rect.height > scroll.viewport.rect.height, "List has no scroll range");
        Check(scroll.viewport.GetComponent<Image>().raycastTarget, "Empty viewport cannot receive a swipe");
        foreach (var button in scroll.content.GetComponentsInChildren<Button>())
            Check(ExecuteEvents.GetEventHandler<IDragHandler>(button.gameObject) == scroll.gameObject,
                "Card intercepts touch dragging: " + button.name);
        var center = RectTransformUtility.WorldToScreenPoint(null, scroll.viewport.TransformPoint(scroll.viewport.rect.center));
        var touch = new PointerEventData(EventSystem.current) { pointerId = 0,
            button = PointerEventData.InputButton.Left, position = center, pressPosition = center };
        var before = scroll.content.anchoredPosition.y;
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.initializePotentialDrag);
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.beginDragHandler);
        touch.dragging = true; touch.position += Vector2.up * 120f;
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.dragHandler);
        Check(scroll.content.anchoredPosition.y > before + 1f, "Upward touch swipe does not scroll " + openMethod);
        var afterUp = scroll.content.anchoredPosition.y;
        touch.position -= Vector2.up * 80f;
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.dragHandler);
        Check(scroll.content.anchoredPosition.y < afterUp - 1f, "Downward touch swipe does not scroll " + openMethod);
        ExecuteEvents.Execute(scroll.gameObject, touch, ExecuteEvents.endDragHandler);
        ((GameObject)Get(game, "customizeScreen")).GetComponent<SkyPulseRoundStartSurface>().OnPointerClick(touch);
        Check(Get(game, "state").ToString() == "Customize", "Touch swipe starts a run");
        Check(!((GameObject)Get(game, "purchaseModal")).activeSelf, "Touch swipe opens a purchase");
        scroll.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
        var last = (RectTransform)scroll.content.GetChild(scroll.content.childCount - 1);
        var corners = new Vector3[4]; last.GetWorldCorners(corners);
        var bottom = scroll.viewport.InverseTransformPoint(corners[0]).y;
        Check(bottom >= scroll.viewport.rect.yMin - 1f, "Last item is clipped at bottom of " + openMethod);
        scroll.velocity = new Vector2(0f, 900f);
        Call(game, openMethod == "OpenHangar" ? "OpenUpgrades" : "OpenHangar");
        Check(scroll.velocity.sqrMagnitude == 0f, "Tab inherits previous touch fling");
    }
    public static void Execute()
    {
        try
        {
            SessionState.SetBool("SkyPulseVisualSmoke", false);
            var game = UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();
            if (game == null) game = new GameObject("Visual smoke test").AddComponent<SkyPulseNativeGame>();
            Call(game, "StartFlight");
            var bird = (Transform)Get(game, "bird");
            var core = (LineRenderer)Get(game, "trailCore");
            for (var i = 0; i < 120; i++) Call(game, "UpdateTrail", 1f / 120f);
            Check(core.positionCount == 64, "Trail history did not fill");
            Check(core.GetPosition(0).x - core.GetPosition(63).x > 1f, "Trail collapses on level flight");
            for (var i = 1; i < 64; i++) Check(core.GetPosition(i).x < core.GetPosition(i - 1).x, "Trail doubles back during level flight");
            var tail = core.GetPosition(63);
            bird.position += Vector3.up * .2f;
            Call(game, "UpdateTrail", 1f / 120f);
            Check(Mathf.Abs(core.GetPosition(63).y - tail.y) < .001f, "New flap teleports old trail history");
            Set(game, "reduceMotionEnabled", true);
            Call(game, "UpdateTrail", 1f / 120f);
            foreach (var spark in (SpriteRenderer[])Get(game, "trailSparks")) Check(!spark.enabled, "Reduced motion spark still active");
            Set(game, "reduceMotionEnabled", false);
            Call(game, "ClearTrail");
            Check(core.positionCount == 0, "Restart retains trail geometry");
            foreach (var spark in (SpriteRenderer[])Get(game, "trailSparks")) Check(!spark.enabled, "Restart retains sparks");

            var backdrop = (SpriteRenderer)Get(game, "backgroundRenderer");
            var incoming = (SpriteRenderer)Get(game, "incomingBackground");
            foreach (var milestone in new[] {15, 30, 45, 60})
            {
                Set(game, "score", milestone);
                var next = (milestone / 15) % 3;
                var old = backdrop.sprite;
                Call(game, "BeginWorldTransition", next);
                Check(backdrop.sprite == old && incoming.enabled && incoming.color.a == 0f, "Backdrop swaps at start");
                float previous = 0f;
                for (var step = 0; step < 108; step++)
                {
                    Call(game, "UpdateWorldTransition", 1f / 120f);
                    Check(incoming.color.a >= previous, "Dissolve reverses");
                    previous = incoming.color.a;
                    Check(backdrop.sprite == old, "Outgoing backdrop removed mid-blend");
                }
                Check(Mathf.Abs(incoming.color.a - .5f) < .01f, "Dissolve midpoint is not balanced");
                for (var step = 0; step < 110; step++) Call(game, "UpdateWorldTransition", 1f / 120f);
                Check(!incoming.enabled && backdrop.sprite == incoming.sprite, "Dissolve completion changes the image");
                Check((float)Get(game, "worldRecoveryTimer") > .8f, "Recovery beat lost");
                Check(!(bool)Get(game, "departingObjectsVisible"), "Departing renderers not restored");
            }
            Set(game, "score", 75);
            Call(game, "BeginWorldTransition", 2);
            Call(game, "UpdateWorldTransition", .1f);
            Call(game, "StartFlight");
            Check(!incoming.enabled && (float)Get(game, "worldTransitionTimer") == 0f, "Retry retains transition overlay");
            Call(game, "OpenCustomize");
            var custom = (GameObject)Get(game, "customizeScreen");
            var surface = custom.GetComponent<SkyPulseRoundStartSurface>();
            Check(surface != null, "Hangar has no tap-to-start surface");
            var evt = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = new Vector2(200, 200), pressPosition = new Vector2(200, 200) };
            evt.dragging = true;
            surface.OnPointerClick(evt);
            Check(Get(game, "state").ToString() == "Customize", "Scroll starts a run");
            evt.dragging = false; evt.position += Vector2.up * 100;
            surface.OnPointerClick(evt);
            Check(Get(game, "state").ToString() == "Customize", "Swipe starts a run");
            evt.position = evt.pressPosition;
            var modal = (GameObject)Get(game, "purchaseModal");
            modal.SetActive(true); surface.OnPointerClick(evt);
            Check(Get(game, "state").ToString() == "Customize", "Purchase modal launches a run");
            modal.SetActive(false);
            foreach (var button in custom.GetComponentsInChildren<Button>())
                Check(ExecuteEvents.GetEventHandler<IPointerClickHandler>(button.gameObject) == button.gameObject,
                    "Background steals button clicks");
            surface.OnPointerClick(evt);
            Check(Get(game, "state").ToString() == "Playing", "Hangar tap fails to launch");
            Call(game, "OpenUpgrades");
            surface.OnPointerClick(evt);
            Check(Get(game, "state").ToString() == "Playing", "Tech tree tap fails to launch");
            CheckTouchScrolling(game, "OpenHangar");
            CheckTouchScrolling(game, "OpenUpgrades");
            Debug.Log("SKYPULSE_VISUAL_SMOKE_PASS: trail continuity, reduced motion, resets, milestones 15/30/45/60, dissolve endpoints, recovery, retry during transition, hangar/tech taps, drag and modal guards, button click routing, touch scrolling in both directions, last-item access, tab inertia reset.");
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
