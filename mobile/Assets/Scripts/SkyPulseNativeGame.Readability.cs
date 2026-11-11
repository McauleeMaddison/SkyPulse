using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        // Presentation only: all timers, Echo rewards, controls and save data stay
        // owned by the existing gameplay code. Build these six text slots once;
        // at most three quiet rows appear in one panel above the flight corridor.
        const float PickupStatusTop = 752f;
        const float PickupStatusRow = 34f;
        const float PickupStatusWidth = 900f;
        RectTransform readablePickupPanel;
        readonly Text[] readablePickupLabels = new Text[6];
        readonly int[] readablePickupCodes = new int[6];
        readonly int[] readablePickupTenths = new int[6];
        readonly int[] readablePickupBudgets = new int[6];
        int readablePickupCount, readablePickupVisibleCount = -1;
        static readonly Color PickupInk = new Color(.95f, .98f, 1f);
        static readonly Color EchoInk = new Color(.81f, .73f, 1f);
        static readonly Color LensInk = new Color(.56f, .94f, 1f);
        static readonly Color AegisInk = new Color(.58f, 1f, .78f);
        static readonly Color TimeInk = new Color(.79f, .66f, 1f);

        // Hook: call once at the end of CreateHud, after hudCoachText exists.
        void InitializePickupReadability(Transform parent)
        {
            if (readablePickupPanel != null) return;
            readablePickupPanel = CreateLuminousPanel(parent, "Active flight effects", Vector2.zero,
                new Vector2(PickupStatusWidth, PickupStatusRow + 12f),
                new Color(.009f, .025f, .052f, .86f), new Color(.40f, .77f, 1f, .27f));
            readablePickupPanel.pivot = new Vector2(.5f, 1f);
            readablePickupPanel.anchoredPosition = new Vector2(0f, PickupStatusTop);
            readablePickupPanel.GetComponent<Image>().raycastTarget = false;
            for (int i = 0; i < readablePickupLabels.Length; i++)
            {
                var label = CreateText(readablePickupPanel, string.Empty, Vector2.zero,
                    new Vector2(424f, PickupStatusRow), 30, PickupInk, TextAnchor.MiddleLeft, FontStyle.Bold);
                label.name = "Flight effect status " + (i + 1);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, 1f);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.raycastTarget = false;
                label.gameObject.SetActive(false);
                readablePickupLabels[i] = label;
                readablePickupCodes[i] = readablePickupTenths[i] = readablePickupBudgets[i] = -1;
            }
            // The world label has a dedicated clear gap between score and pause.
            // The pickup panel can then use the old small-print status band.
            if (hudModeText != null)
            {
                hudModeText.rectTransform.anchoredPosition = new Vector2(0f, 946f);
                hudModeText.rectTransform.sizeDelta = new Vector2(360f, 40f);
                hudModeText.fontSize = 28;
                hudModeText.alignment = TextAnchor.MiddleCenter;
                hudModeText.horizontalOverflow = HorizontalWrapMode.Wrap;
                hudModeText.verticalOverflow = VerticalWrapMode.Truncate;
                hudModeText.raycastTarget = false;
            }
            if (routeGoalText != null)
            {
                routeGoalText.rectTransform.anchoredPosition = new Vector2(0f, 856f);
                routeGoalText.rectTransform.sizeDelta = new Vector2(400f, 36f);
                routeGoalText.fontSize = 24;
                routeGoalText.alignment = TextAnchor.MiddleCenter;
                routeGoalText.horizontalOverflow = HorizontalWrapMode.Wrap;
                routeGoalText.verticalOverflow = VerticalWrapMode.Truncate;
            }
            if (hudPowerUpText != null) hudPowerUpText.gameObject.SetActive(false);
            readablePickupPanel.gameObject.SetActive(false);
        }

        // Hook: the existing UpdatePowerUpHud delegates here. It is already
        // called on collection, countdown, Aegis use and every reset/retry.
        void UpdateReadablePowerUpHud()
        {
            if (readablePickupPanel == null) return;
            if (hudPowerUpText != null && hudPowerUpText.gameObject.activeSelf)
                hudPowerUpText.gameObject.SetActive(false);
            readablePickupCount = 0;
            if (crystalEchoTimer > 0f && crystalEchoBudget > 0)
                AddReadablePickup(4, crystalEchoTimer, crystalEchoBudget, "ECHO", EchoInk);
            if (routeLensTimer > 0f)
                AddReadablePickup(5, routeLensTimer, 0, "LENS", LensInk);
            if (shieldCharges > 0)
                AddReadablePickup(3, 0f, 0, "AEGIS READY", AegisInk);
            if (shieldImmunityTimer > 0f)
                AddReadablePickup(2, shieldImmunityTimer, 0, "RECOVERY", AegisInk);
            if (magnetHaloTimer > 0f)
                AddReadablePickup(1, magnetHaloTimer, 0, "MAGNET", LensInk);
            if (slowFieldTimer > 0f)
                AddReadablePickup(0, slowFieldTimer, 0, "TIME PULSE", TimeInk);
            if (readablePickupVisibleCount == readablePickupCount) return;
            readablePickupVisibleCount = readablePickupCount;
            int rows = (readablePickupCount + 1) / 2;
            for (int i = 0; i < readablePickupLabels.Length; i++)
            {
                bool show = i < readablePickupCount;
                var label = readablePickupLabels[i];
                if (label.gameObject.activeSelf != show) label.gameObject.SetActive(show);
                if (!show) continue;
                bool alone = readablePickupCount == 1;
                label.rectTransform.anchoredPosition = new Vector2(alone ? 0f : (i % 2 == 0 ? -220f : 220f), -6f - PickupStatusRow * (i / 2 + .5f));
                label.rectTransform.sizeDelta = new Vector2(alone ? PickupStatusWidth - 32f : 424f, PickupStatusRow);
            }
            readablePickupPanel.sizeDelta = new Vector2(PickupStatusWidth, rows * PickupStatusRow + 12f);
            bool active = readablePickupCount > 0;
            if (readablePickupPanel.gameObject.activeSelf != active) readablePickupPanel.gameObject.SetActive(active);
        }

        void AddReadablePickup(int code, float seconds, int budget, string label, Color colour)
        {
            int slot = readablePickupCount++;
            int tenths = ReadableTimerTenths(seconds);
            var text = readablePickupLabels[slot];
            if (readablePickupCodes[slot] != code || readablePickupTenths[slot] != tenths || readablePickupBudgets[slot] != budget)
            {
                text.text = ReadablePickupCopy(label, tenths, budget);
                readablePickupCodes[slot] = code;
                readablePickupTenths[slot] = tenths;
                readablePickupBudgets[slot] = budget;
            }
            text.color = colour;
        }
        static int ReadableTimerTenths(float seconds)
        {
            return float.IsFinite(seconds) && seconds > 0f ? Mathf.CeilToInt(Mathf.Min(seconds, 600f) * 10f) : 0;
        }
        static string ReadablePickupCopy(string label, int tenths, int budget)
        {
            string timer = tenths > 0 ? " " + (tenths * .1f).ToString("0.0", CultureInfo.InvariantCulture) + "s" : string.Empty;
            return budget > 0 ? label + timer + " · +" + budget.ToString(CultureInfo.InvariantCulture) + " LEFT" : label + timer;
        }

        // The existing six voices and warmed clips remain the entire SFX pool.
        // Critical cues may replace the oldest least-important voice; ordinary
        // taps never steal the reserved voice or cut off a critical event.
        readonly bool[] readableVoiceBusy = new bool[EffectVoiceCount];
        readonly byte[] readableVoicePriorities = new byte[EffectVoiceCount];
        readonly double[] readableVoiceStarted = new double[EffectVoiceCount];
        const byte OrdinaryEffect = 1, UnlockEffect = 2, ImpactEffect = 3;

        // Hook: the existing Play(AudioClip) delegates here.
        void PlayReadableEffect(AudioClip clip)
        {
            if (clip == null || audioSource == null || audioSource.mute || !soundEnabled || audioSource.volume <= 0f) return;
            byte priority = clip == crashSound ? ImpactEffect : clip == unlockSound ? UnlockEffect : OrdinaryEffect;
            int cue = -1;
            for (int i = 0; i < effectClips.Length; i++) if (effectClips[i] == clip) { cue = i; break; }
            double now = AudioSettings.dspTime;
            // Ordinary event bursts are coalesced. A critical unlock or crash is
            // always audible, including a full pool and back-to-back events.
            if (priority == OrdinaryEffect && cue >= 0 && now < nextEffectAt[cue]) return;
            for (int i = 0; i < effectVoices.Length; i++)
            {
                var voice = effectVoices[i];
                readableVoiceBusy[i] = voice == null || voice.isPlaying;
                if (voice == null) readableVoicePriorities[i] = byte.MaxValue;
            }
            int selected = SelectReadableEffectVoice(priority, readableVoiceBusy, readableVoicePriorities, readableVoiceStarted);
            if (selected < 0) return;
            var chosen = effectVoices[selected];
            if (chosen.isPlaying) chosen.Stop();
            // Flaps sit beneath crystal/score detail; unlock and impact stay
            // foremost without increasing the existing fixed master headroom.
            float variation = (effectSequence++ % 3 - 1f) * .012f;
            chosen.pitch = clip == flapSound || clip == prismSound ? 1f + variation : 1f;
            chosen.volume = EffectGain * Mathf.Clamp01(audioSource.volume) * ReadableEffectGain(cue);
            chosen.priority = priority == ImpactEffect ? 32 : priority == UnlockEffect ? 48 : 128;
            chosen.mute = false;
            chosen.panStereo = 0f;
            chosen.clip = clip;
            readableVoicePriorities[selected] = priority;
            readableVoiceStarted[selected] = now;
            chosen.Play();
            if (cue >= 0) nextEffectAt[cue] = now + .035;
        }
        static int SelectReadableEffectVoice(byte requested, bool[] busy, byte[] priorities, double[] started)
        {
            int first = requested > OrdinaryEffect ? 0 : 1;
            for (int i = first; i < busy.Length; i++) if (!busy[i]) return i;
            if (requested == OrdinaryEffect) return -1;
            int best = -1;
            for (int i = 0; i < busy.Length; i++)
            {
                if (priorities[i] == byte.MaxValue) continue;
                if (best < 0 || priorities[i] < priorities[best] || priorities[i] == priorities[best] && started[i] < started[best]) best = i;
            }
            return best;
        }
        static float ReadableEffectGain(int cue)
        {
            switch (cue)
            {
                case 0: return .56f; // flap
                case 1: return .78f; // score
                case 2: return .84f; // crystal
                case 3: return 1f;   // impact
                case 4: return 1f;   // unlock
                case 5: return .72f; // prism turn
                case 6: return .88f; // security warning
                default: return .84f;
            }
        }
    }
}
