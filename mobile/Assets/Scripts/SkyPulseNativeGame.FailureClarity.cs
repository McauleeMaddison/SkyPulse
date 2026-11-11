using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        // Presentation only. Existing mode handlers own retries, rewards and saves.
        // Call once after CreateMissionInterface, and after each terminal mission report.
        readonly List<Button> failureActionBindings = new List<Button>(16);
        bool failureClarityInitialized;
        int failureNavigationFrame = -1;

        void InitializeFailureClarity()
        {
            if (failureClarityInitialized) return;
            failureClarityInitialized = true;
            var card = gameOverScreen.transform.Find("Game over card") as RectTransform;
            if (card != null)
            {
                card.sizeDelta = new Vector2(card.sizeDelta.x, 1460f);
                // Preserve the existing report and its rewards; make room beneath it
                // for three comfortably separated rows of the original actions.
                foreach (Transform child in card)
                    if (child is RectTransform rect && child.GetComponent<Button>() == null && child.GetComponent<SkyPulseScoreGlass>() == null)
                        rect.anchoredPosition += new Vector2(0f, 140f);
                foreach (var button in card.GetComponentsInChildren<Button>(true))
                {
                    var label = button.GetComponentInChildren<Text>();
                    var rect = (RectTransform)button.transform;
                    float y = label.text == "RETRY" ? -190f : label.text == "BIRD HANGAR" || label.text == "TECH TREE" ? -382f : -574f;
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
                    SetComfortHitTarget(button);
                    GuardFailureAction(button, gameOverScreen, FlightState.GameOver);
                }
                if (resultReasonText != null)
                {
                    resultReasonText.rectTransform.sizeDelta = new Vector2(720f, 38f);
                    resultReasonText.fontSize = 28;
                }
            }
            // These already have generous, separated hit areas. Keep their layout
            // and preference controls; only fence navigation from a retired pause.
            foreach (var button in pauseScreen.GetComponentsInChildren<Button>(true))
            {
                string text = button.GetComponentInChildren<Text>().text;
                if (text == "RESUME" || text == "RETURN TO MENU")
                    GuardFailureAction(button, pauseScreen, FlightState.Paused);
            }
        }

        void GuardFailureAction(Button button, GameObject screen, FlightState expected)
        {
            for (int i = failureActionBindings.Count - 1; i >= 0; i--)
                if (failureActionBindings[i] == null) failureActionBindings.RemoveAt(i);
            if (failureActionBindings.Contains(button)) return;
            failureActionBindings.Add(button);
            var original = button.onClick;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() =>
            {
                // A queued click from a hidden/previous report cannot restart a
                // new run, open another screen, or spend the same action twice.
                if (button == null || screen == null || !button.IsActive() || !button.IsInteractable() ||
                    !screen.activeInHierarchy || state != expected || failureNavigationFrame == Time.frameCount) return;
                failureNavigationFrame = Time.frameCount;
                FenceGameplayBoundary();
                original.Invoke();
            });
        }

        void RefreshEndlessFailureClarity()
        {
            if (activeMode != SkyPulseMode.EndlessRoute || resultReasonText == null ||
                (state != FlightState.Impact && state != FlightState.GameOver)) return;
            string copy = lastCrashReason == "GROUND CONTACT"
                ? "GROUND CONTACT · Tap sooner to climb"
                : "PIPE CONTACT · Aim through the opening";
            if (resultReasonText.text != copy) resultReasonText.text = copy;
        }

        string RelayFailureClarity(string reason)
        {
            if (relaySession == null) return reason;
            if (reason == "GROUND CONTACT") return "GROUND CONTACT\nTap earlier to stay above the flight deck.";
            int gate = Mathf.Clamp(relaySession.Passed, 0, relaySession.Mission.RelayCount - 1);
            if (reason.IndexOf("CRYSTAL", StringComparison.Ordinal) >= 0)
                return "GATE " + (gate + 1).ToString("00") + " LOCKED · " + relaySession.GateCrystals(gate) + " / 2 CRYSTALS\nCollect both matching crystals before the hoop.";
            if (reason == "MISSED EXIT") return "EXIT MISSED\nReach the green exit after the last gate.";
            if (reason == "UNPROTECTED ENERGY SHUTTER" || reason == "GATE IMPACT" || reason.StartsWith("MISSED RELAY", StringComparison.Ordinal))
            {
                bool exit = relaySession.Distance >= relaySession.Mission.ExitX - BirdHitboxHorizontalExtent() - .32f;
                return exit ? "EXIT RIM CONTACT\nKeep the bird inside the green opening."
                    : "GATE " + (gate + 1).ToString("00") + " RIM CONTACT\nKeep the bird inside the blue opening.";
            }
            return reason;
        }

        void ApplyMissionFailureClarity(bool won, string reason)
        {
            if (missionResultScreen == null || !missionResultScreen.activeInHierarchy || state != FlightState.GameOver) return;
            bool blackout = activeMode == SkyPulseMode.BlackoutProtocol;
            if (!won && reason.IndexOf("SAVE FAILED", StringComparison.Ordinal) < 0)
            {
                foreach (var label in missionResultScreen.GetComponentsInChildren<Text>())
                {
                    if (label.text == reason)
                    {
                        label.text = blackout
                            ? reason == "TRACE EXHAUSTED" && blackoutSession != null
                                ? "TRACE EXHAUSTED · " + blackoutSession.CoresPowered + " / " + blackoutSession.Puzzle.CoreCount + " CORES LIT"
                                : reason
                            : RelayFailureClarity(reason);
                        label.fontSize = blackout ? 30 : 28;
                    }
                    if (blackout && label.text == "REWIRE THE LIGHT · TRY ANOTHER ROUTE")
                    {
                        label.text = blackoutSession != null && blackoutSession.LastCost == 3
                            ? "SENSOR HIT · 3 TRACE · PREVIEW THE NEXT ROUTE FIRST"
                            : "PREVIEW IS FREE · LIGHT EVERY CORE AND THE UPLINK";
                        label.rectTransform.anchoredPosition = new Vector2(0f, -900f);
                    }
                }
            }
            // Buttons stay in creation order: next (when present), retry, map, home.
            // No completion modal is created: intermediate Blackout stages never call here.
            float y = blackout ? -340f : -90f;
            foreach (var button in missionResultScreen.GetComponentsInChildren<Button>())
            {
                var rect = (RectTransform)button.transform;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
                SetComfortHitTarget(button);
                GuardFailureAction(button, missionResultScreen, FlightState.GameOver);
                y -= 192f;
            }
        }
    }
}
