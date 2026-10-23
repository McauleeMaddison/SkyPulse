using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        Action resetWorldGuidePage;
        int modeGuidePage;
        Transform modeGuideBody;
        readonly Button[] modeGuideTabs = new Button[5];
        readonly string[] endlessGuideTopics = { "FLIGHT", "TECH TREE", "SKYPULSE CORE", "BIRD TRAINING", "BREEDING" };

        Text routeGoalText;
        int routeGoalScore = -1;
        void CreateRouteGoal(Transform parent)
        {
            routeGoalText = CreateText(parent, "", new Vector2(-226, 680), new Vector2(480, 32), 22, Hex("#c4dbe8"), TextAnchor.MiddleLeft, FontStyle.Normal);
            routeGoalText.gameObject.name = "Next world goal";
            routeGoalText.raycastTarget = false;
        }
        void UpdateRouteGoalFeedback()
        {
            // Read the existing score only. No separate missions, rewards or save
            // keys; the label follows retries and every twelve-world route lap.
            if (routeGoalText == null) return;
            bool visible = activeMode == SkyPulseMode.EndlessRoute;
            if (routeGoalText.gameObject.activeSelf != visible) routeGoalText.gameObject.SetActive(visible);
            if (!visible || routeGoalScore == score) return;
            routeGoalScore = score;
            int gates = Mathf.Max(0, score);
            routeGoalText.text = $"{Worlds[((gates / 15) + 1) % Worlds.Length].Name} · {15 - gates % 15} TO GO";
        }

        void OpenWorldGuide()
        {
            if (modeGuideScreen != null) modeGuideScreen.SetActive(false);
            resetWorldGuidePage?.Invoke();
            flightGuideScreen.SetActive(true);
            flightGuideScreen.transform.SetAsLastSibling();
        }
        void OpenModeGuide()
        {
            ClearScreen(modeGuideScreen);
            modeGuideScreen.SetActive(true); modeGuideScreen.transform.SetAsLastSibling(); modeGuidePage = 0;
            var accent = ProtocolAccent(homeProtocol);
            bool endless = homeProtocol == SkyPulseMode.EndlessRoute;
            bool relay = homeProtocol == SkyPulseMode.PulseRelay;
            MissionText(modeGuideScreen.transform, "HOW TO PLAY", 865, 46, accent);
            MissionText(modeGuideScreen.transform, endless ? "ENDLESS ROUTE" : relay ? "PULSE RELAY" : "BLACKOUT PROTOCOL", 792, 27, Hex("#bdcfdd"), 870, 60);
            int topics = endless ? 5 : 2;
            for (int i = 0; i < modeGuideTabs.Length; i++) modeGuideTabs[i] = null;
            if (topics > 1)
            {
                for (int i = 0; i < topics; i++)
                {
                    int page = i;
                    string label = endless ? endlessGuideTopics[i] : relay ? (i == 0 ? "FLIGHT" : "YOUR BIRDS") : (i == 0 ? "ROUTING" : "YOUR MISSION");
                    float x = endless ? (i % 3 - 1) * 310 : i == 0 ? -235 : 235;
                    modeGuideTabs[i] = MissionButton(modeGuideScreen.transform, label, x, 642 - (endless ? i / 3 * 170 : 0), endless ? 292 : 450, () => ShowModeGuidePage(page), accent, 148);
                    var marker = CreateImage(modeGuideTabs[i].transform, "Current topic", new Vector2(0, -52), new Vector2(104, 3), accent);
                    marker.raycastTarget = false;
                }
                if (endless) MissionButton(modeGuideScreen.transform, "WORLDS &\nPOWER-UPS", 310, 472, 292, OpenWorldGuide, accent, 148);
            }
            var field = PremiumPanel(modeGuideScreen.transform, "Field guide reading area", new Vector2(0, endless ? -80 : 50), new Vector2(940, 860), (int)homeProtocol, accent);
            var content = new GameObject("Guide topic content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(field, false); content.sizeDelta = new Vector2(940, 860); modeGuideBody = content;
            MissionText(modeGuideScreen.transform, "Crystals are earned in play. No real-money purchases.", -620, 25, Hex("#a9c3d0"), 870, 70);
            MissionButton(modeGuideScreen.transform, "BACK", 0, -815, 850, () => modeGuideScreen.SetActive(false), accent, 148);
            ShowModeGuidePage(0);
        }
        void ShowModeGuidePage(int page)
        {
            bool endless = homeProtocol == SkyPulseMode.EndlessRoute, relay = homeProtocol == SkyPulseMode.PulseRelay;
            modeGuidePage = Mathf.Clamp(page, 0, endless ? 4 : 1);
            foreach (Transform child in modeGuideBody) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            for (int i = 0; i < modeGuideTabs.Length; i++)
            {
                var tab = modeGuideTabs[i]; if (tab == null) continue;
                tab.interactable = i != modeGuidePage;
                tab.transform.Find("Current topic").gameObject.SetActive(i == modeGuidePage);
                tab.GetComponentInChildren<Text>().color = i == modeGuidePage ? ProtocolAccent(homeProtocol) : Hex("#effcff");
                // The selected tab remains readable; it does not spend or launch.
                var colours = tab.colors; colours.disabledColor = new Color(.72f, .94f, 1f, 1); tab.colors = colours;
            }
            if (!endless && !relay)
            {
                if (modeGuidePage == 0)
                {
                    GuideStep(0, "ROUTE LIVE ENERGY", "Tap a silver mirror to turn it. Power every numbered core and the amber uplink together. All targets must stay live to restore this city system.");
                    GuideStep(1, "SPLIT AND UNLOCK", "Splitters send light forward and left. Match their input to the arriving beam. Power RELAY A or B to open its matching shutter; keep that relay connected.");
                    GuideStep(2, "PLAN BEFORE YOU SPEND", "Open PLAN and tap an optic to inspect its next beam and exact trace cost. Confirm ROTATE to spend once. Closing Plan leaves the live circuit unchanged.");
                }
                else
                {
                    GuideStep(0, "RESTORE EIGHT CITY SYSTEMS", "Each vault restores a named city system. The skyline ribbon responds to live core power; a cleared district stays online in your saved campaign.");
                    GuideStep(1, "PROTECT YOUR TRACE", "Each turn costs 1 trace, or 3 while a red sensor is powered. Light every core and the uplink before the budget runs out, or restart the vault.");
                    GuideStep(2, "CHASE GOLD, KEEP MOVING", "Meet the displayed Gold trace target for the top medal. Cleared vaults save once and activate the next puzzle immediately; the eighth completes the network.");
                }
            }
            else if (relay)
            {
                if (modeGuidePage == 0)
                {
                    GuideStep(0, "ONE TAP, ONE FLAP", "Tap anywhere in the flight area to flap. Pause and menu controls never flap. Stay above the illuminated deck edge and fly through each hoop opening.");
                    GuideStep(1, "CRYSTALS OPEN GATES", "Collect both randomly placed crystals for the matching numbered gate. At 2/2 its red lasers turn blue and the lock opens immediately. Fly through the clear opening.");
                    GuideStep(2, "CLEAR TODAY'S CIRCUITS", "Fly through the open hoops and the green exit. Choose the next circuit from the result screen. Four new circuits arrive each UTC day; today's medals reset at refresh.");
                }
                else
                {
                    GuideStep(0, "FLY YOUR EQUIPPED BIRD", "Open Bird Hangar, choose an owned bird and equip it. Every bird has the same flight handling. Bird reward traits and Tech Tree bonuses apply to Endless, not Relay.");
                    GuideStep(1, "TRAIN FOR ENDLESS REWARDS", "Train an owned bird in the Hangar to strengthen its two reward traits. Completed training also lowers future founder-bird unlock prices, up to 20% across your flock.");
                    GuideStep(2, "GROW YOUR LINEAGE", "Choose two different owned parents in Breed. Preview the inherited traits and crystal cost before confirming. Both parents stay; the new offspring joins your Hangar.");
                }
            }
            else switch (modeGuidePage)
            {
                case 1:
                    GuideStep(0, "EXPLORE THE CONNECTED TREE", "Open Tech Tree from Home. Drag in any direction to follow its Collection, Recovery and Mastery branches. Installed, NEXT and locked levels show your path.");
                    GuideStep(1, "INSPECT, THEN UPGRADE", "Tap an available level to inspect its exact crystal cost and effect, then confirm to buy. Reach level two in the preceding upgrade to open the next family.");
                    GuideStep(2, "BUILD YOUR ENDLESS REWARDS", "There are nine upgrade families with three levels each. Their collection and reward benefits apply to Endless Route. Core savings are already included in the shown price.");
                    break;
                case 2:
                    GuideStep(0, "EVOLVE THE CENTRAL CORE", "Tap the central SkyPulse Core in Tech Tree, then inspect the next level. Its colour and halo change as it evolves from ION through JADE, PRISM and SOLAR to NOVA.");
                    GuideStep(1, "SPEND LESS ON UPGRADES", "Core levels 2 to 5 give 5%, 10%, 15% and 20% savings on Tech, bird training and breeding. The purchase panel shows the final crystal price. Core evolution has its own cost.");
                    GuideStep(2, "MAKE ROOM TO GROW", "A higher Core raises the training limit, up to bird level 5, and opens more lineage bays. Core 3 and Core 5 unlock stronger inherited trait grades for breeding.");
                    break;
                case 3:
                    GuideStep(0, "TRAIN AN OWNED BIRD", "Open Bird Hangar, choose your bird and tap Train. Inspect its next level, two reward traits and crystal cost. Evolve the Core when you reach its training limit.");
                    GuideStep(1, "EARN WITH DISTINCT TRAITS", "Collector draws extra value from pickups; Salvager adds crystal salvage. Precision rewards perfect gates, Voyager rewards distance and Recovery rewards surviving five gates.");
                    GuideStep(2, "LOWER FUTURE UNLOCK COSTS", "Each completed training level adds 2% off future founder-bird unlocks, capped at 20% across the flock. Traits improve Endless rewards; flight handling stays the same.");
                    break;
                case 4:
                    GuideStep(0, "CHOOSE TWO OWNED PARENTS", "At Core level 2, open Breed in Bird Hangar and choose two different owned birds. You keep both parents. Parent order chooses the appearance and which reward traits are inherited.");
                    GuideStep(1, "PREVIEW YOUR OFFSPRING", "Check its appearance, inherited traits and exact crystal cost before confirming. Parents trained to level 3 or above can pass stronger grades when the Core allows them.");
                    GuideStep(2, "ADD A LASTING FLOCK MEMBER", "Confirm once to unlock and equip the offspring. It stays in your Hangar and can be trained. Duplicate owned combinations are blocked; evolve the Core for more lineage bays.");
                    break;
                default:
                    GuideStep(0, "TAP TO TAKE FLIGHT", "Tap PLAY, then tap the flight area to flap through each pipe opening. Avoid the pipes and ground. Use the pause button to stop, resume or return to Home.");
                    GuideStep(1, "DISCOVER TWELVE WORLDS", "The flight HUD shows your next world and gates remaining. Every run begins in Neon City; a new world arrives every 15 gates. After Polar Glow, Neon City returns at score 180. Your best is saved.");
                    GuideStep(2, "TURN FLIGHTS INTO PROGRESS", "Collect crystals and power-ups in flight. Spend earned crystals on birds, training, breeding, Core evolution and Tech. Open the topics above for their exact benefits.");
                    break;
            }
        }
        void GuideStep(int index, string title, string detail)
        {
            var accent = ProtocolAccent(homeProtocol);
            var card = PremiumPanel(modeGuideBody, "Guide step " + index, new Vector2(0, 278 - index * 278), new Vector2(880, 244), (int)homeProtocol, accent);
            MissionText(card, title, 76, 28, accent, 808, 60);
            var text = MissionText(card, detail, -26, 27, Hex("#d2def0"), 792, 146);
            text.fontStyle = FontStyle.Normal;
        }
    }
}
