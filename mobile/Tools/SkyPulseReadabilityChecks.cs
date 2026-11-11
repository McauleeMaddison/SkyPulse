// Disposable Editor QA only; copy to Assets/Editor of an isolated QA product.
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseReadabilityChecks
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static object Get(object value, string name) => value.GetType().GetField(name, F).GetValue(value);
    static void Set(object value, string name, object data) => value.GetType().GetField(name, F).SetValue(value, data);
    static object Call(object value, string name, params object[] args) => value.GetType().GetMethods(F).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(value, args);
    static object PureCall(string name, params object[] args) => typeof(SkyPulseNativeGame).GetMethod(name, F).Invoke(null, args);
    static void Check(bool good, string note)
    {
        if (!good) throw new Exception(note);
        Debug.Log("READABILITY_CHECK: " + note);
    }
    public static void Pure()
    {
        var busy = new[] { false, true, true, true, true, true };
        var priorities = new byte[] { 0, 1, 1, 1, 1, 1 };
        var started = new double[] { 0, 6, 4, 5, 2, 3 };
        int Voice(byte requested) => (int)PureCall("SelectReadableEffectVoice", requested, busy, priorities, started);
        Check(Voice(1) == -1, "Saturated ordinary taps cannot use the reserved critical voice");
        Check(Voice(2) == 0 && Voice(3) == 0, "A free reserved voice accepts both unlock and crash");
        busy[0] = true; priorities[0] = 3;
        Check(Voice(2) == 4 && Voice(3) == 4, "A full pool replaces its oldest ordinary voice for either critical event");
        priorities[4] = 2;
        Check(Voice(3) == 5, "A newer ordinary voice is replaced before a critical cue");
        for (int i = 0; i < priorities.Length; i++) priorities[i] = 2;
        priorities[0] = 3;
        Check(Voice(3) == 4, "A crash remains audible even when every voice carries a critical cue");
        for (int i = 0; i < priorities.Length; i++) priorities[i] = byte.MaxValue;
        Check(Voice(2) == -1, "Missing audio sources are never selected");
        busy[2] = false; priorities[2] = 0;
        Check(Voice(1) == 2, "The existing ordinary pool reuses a free voice without growing");
        float flap = (float)PureCall("ReadableEffectGain", 0), crystal = (float)PureCall("ReadableEffectGain", 2);
        Check(flap < crystal && crystal < (float)PureCall("ReadableEffectGain", 4), "Flap, collection and unlock have distinct balanced mix roles");
        for (int cue = 0; cue < 7; cue++)
        {
            float gain = (float)PureCall("ReadableEffectGain", cue);
            Check(gain > 0 && gain <= 1, "Cue gains retain the existing fixed master headroom: " + cue);
        }
        Check((int)PureCall("ReadableTimerTenths", .001f) == 1 && (int)PureCall("ReadableTimerTenths", 0f) == 0, "A live sub-tenth effect never incorrectly reads zero seconds");
        Check((int)PureCall("ReadableTimerTenths", float.NaN) == 0 && (int)PureCall("ReadableTimerTenths", float.PositiveInfinity) == 0, "Invalid presentation times cannot overflow the timer label");
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Check((string)PureCall("ReadablePickupCopy", "ECHO", 58, 4) == "ECHO 5.8s · +4 LEFT", "Echo explains its exact remaining bonus budget and stable decimal time");
        }
        finally { CultureInfo.CurrentCulture = culture; }
        Check((string)PureCall("ReadablePickupCopy", "AEGIS READY", 0, 0) == "AEGIS READY", "A charged shield shows readiness instead of an invented timer");
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if (!Application.isBatchMode || Application.companyName != "SkyPulseQA20261006" || !Application.productName.Contains("QA"))
            throw new Exception("Disposable batch QA product required");
        string[] timers = { "crystalEchoTimer", "routeLensTimer", "shieldImmunityTimer", "magnetHaloTimer", "slowFieldTimer" };
        var savedTimers = timers.Select(n => (float)Get(game, n)).ToArray();
        int savedBudget = (int)Get(game, "crystalEchoBudget"), savedShield = (int)Get(game, "shieldCharges");
        bool motion = (bool)Get(game, "reduceMotionEnabled"), sound = (bool)Get(game, "soundEnabled"), haptics = (bool)Get(game, "hapticsEnabled");
        var control = (AudioSource)Get(game, "audioSource"); bool muted = control.mute; float volume = control.volume;
        try
        {
            foreach (string timer in timers) Set(game, timer, 0f);
            Set(game, "shieldCharges", 0); Set(game, "crystalEchoBudget", 0);
            Call(game, "UpdatePowerUpHud");
            var panel = (RectTransform)Get(game, "readablePickupPanel");
            Check(panel != null && !panel.gameObject.activeSelf, "Normal flight has no empty pickup panel");
            int children = panel.childCount;
            var labels = (Text[])Get(game, "readablePickupLabels");
            Set(game, "crystalEchoTimer", 5.8f); Set(game, "crystalEchoBudget", 4);
            Call(game, "UpdatePowerUpHud");
            Check(panel.gameObject.activeSelf && labels.Count(t => t.gameObject.activeSelf) == 1 && labels[0].text.Contains("+4 LEFT"), "One active pickup uses one compact row and shows its remaining reward");
            foreach (string timer in timers) Set(game, timer, 4.2f);
            Set(game, "shieldCharges", 1);
            Call(game, "UpdatePowerUpHud");
            Check(labels.Count(t => t.gameObject.activeSelf) == 6 && panel.rect.height <= 114.01f, "All six simultaneous states fit at most three rows in one panel");
            Check(labels.All(t => t.fontSize >= 30 && !t.resizeTextForBestFit), "Pickup text never shrinks back to tiny seventeen-unit print");
            Check(panel.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget), "Every part of the status panel is transparent to gameplay input");
            Check(!((Text)Get(game, "hudPowerUpText")).gameObject.activeSelf, "The former single status label cannot duplicate the new HUD");
            Canvas.ForceUpdateCanvases();
            var hudMode = (Text)Get(game, "hudModeText");
            var score = (RectTransform)((Text)Get(game, "hudScoreText")).transform.parent;
            var coach = ((Text)Get(game, "hudCoachText")).rectTransform;
            var goal = ((Text)Get(game, "routeGoalText")).rectTransform;
            Check(!ScreenRect(panel).Overlaps(ScreenRect(score)) && !ScreenRect(panel).Overlaps(ScreenRect(coach)) && !ScreenRect(panel).Overlaps(ScreenRect(goal)), "The maximum panel remains outside score, next-world goal and flight-coach bands");
            Check(!ScreenRect(hudMode.rectTransform).Overlaps(ScreenRect(score)), "World identification has a separate clear top-center slot");
            foreach (var label in labels)
            {
                float height = label.cachedTextGenerator.GetPreferredHeight(label.text, label.GetGenerationSettings(label.rectTransform.rect.size));
                Check(height <= label.rectTransform.rect.height + .5f, "Actual status copy fits one line without overflow: " + label.text);
            }
            // This tests the real lightweight HUD observer, not the existing
            // simulation's timer mathematics. It may never decrement gameplay.
            foreach (int fps in new[] { 30, 60, 120 })
            {
                float beforeTimer = (float)Get(game, "crystalEchoTimer");
                int beforeBudget = (int)Get(game, "crystalEchoBudget");
                for (int frame = 0; frame < fps; frame++) Call(game, "UpdatePowerUpHud");
                Check((float)Get(game, "crystalEchoTimer") == beforeTimer && (int)Get(game, "crystalEchoBudget") == beforeBudget, "Render-frequency refresh does not spend or accelerate pickups at " + fps + " Hz");
            }
            Set(game, "reduceMotionEnabled", true); Call(game, "UpdatePowerUpHud");
            Check(labels[0].text.Contains("+4 LEFT") && (bool)Get(game, "hapticsEnabled") == haptics, "Reduced Motion preserves readable states and the observer never changes haptic preferences");
            Set(game, "crystalEchoBudget", 3); Call(game, "UpdatePowerUpHud");
            Check(labels[0].text.Contains("+3 LEFT"), "Spending an Echo charge refreshes its budget even within the same timer tenth");
            var refresh = (Action)typeof(SkyPulseNativeGame).GetMethod("UpdateReadablePowerUpHud", F).CreateDelegate(typeof(Action), game);
            for (int n = 0; n < 12; n++) refresh();
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < 120; n++) refresh();
            Check(GC.GetAllocatedBytesForCurrentThread() == allocated && panel.childCount == children, "An unchanged warm HUD refresh allocates no managed memory or new objects");
            uint sequence = (uint)Get(game, "effectSequence");
            control.mute = true; Call(game, "Play", Get(game, "crashSound"));
            Check((uint)Get(game, "effectSequence") == sequence, "Critical rescue respects the existing source mute");
            control.mute = false; Set(game, "soundEnabled", false); Call(game, "Play", Get(game, "unlockSound"));
            Check((uint)Get(game, "effectSequence") == sequence, "Critical rescue respects the player's Sound Off setting");
            Set(game, "soundEnabled", true); control.volume = 0; Call(game, "Play", Get(game, "unlockSound"));
            Check((uint)Get(game, "effectSequence") == sequence, "Zero user volume neither starts a cue nor consumes a voice");
            Check((bool)Get(game, "hapticsEnabled") == haptics, "Audio routing never enables haptics as a replacement for muted sound");
            foreach (string timer in timers) Set(game, timer, 0f);
            Set(game, "shieldCharges", 0); Set(game, "crystalEchoBudget", 0); Call(game, "UpdatePowerUpHud");
            Check(!panel.gameObject.activeSelf, "Retry/reset values remove every expired status immediately");
            yield return null;
        }
        finally
        {
            for (int i = 0; i < timers.Length; i++) Set(game, timers[i], savedTimers[i]);
            Set(game, "shieldCharges", savedShield); Set(game, "crystalEchoBudget", savedBudget);
            Set(game, "reduceMotionEnabled", motion); Set(game, "soundEnabled", sound);
            control.mute = muted; control.volume = volume; Call(game, "UpdatePowerUpHud");
        }
        Debug.Log("SKYPULSE_READABILITY_PASS");
    }
    static Rect ScreenRect(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var canvas = rect.GetComponentInParent<Canvas>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]), max = min;
        for (int i = 1; i < corners.Length; i++)
        {
            var point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]); min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
}
