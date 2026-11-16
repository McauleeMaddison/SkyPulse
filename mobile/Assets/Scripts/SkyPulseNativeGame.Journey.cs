using System;
using UnityEngine;
using UnityEngine.UI;
namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        SkyPulsePlayerJourney playerJourney;bool blackoutDailyActive;long blackoutDailyDay;
        Button journeyButton;Text journeyTitle,journeyDetail;Action journeyAction;
        Text resultJourneyText;
        string journeyRunBirdId;
        int journeyRunStartRank;

        // Capture at launch, not on each result refresh: reopening a report must
        // keep its earned tier while replaying starts a fresh comparison.
        void BeginJourneyRun()
        {
            journeyRunBirdId = equippedSkin?.Id;
            journeyRunStartRank = playerJourney == null || journeyRunBirdId == null
                ? 0 : playerJourney.BirdRank(journeyRunBirdId);
        }

        string JourneyRunSummary()
        {
            if (playerJourney == null || equippedSkin == null) return "";
            string id = equippedSkin.Id;
            int rank = playerJourney.BirdRank(id);
            if (id == journeyRunBirdId && rank > journeyRunStartRank)
                return SkyPulsePlayerJourney.RankName(rank) + " CRADLE UNLOCKED";
            if (rank >= 3) return "AURORA CRADLE EARNED";
            bool relay = activeMode == SkyPulseMode.PulseRelay;
            int target = relay ? (rank == 0 ? 2 : rank == 1 ? 4 : 5)
                : (rank == 0 ? 5 : rank == 1 ? 15 : 30);
            int bestResult = relay ? playerJourney.RelayBest(id) : playerJourney.FlightBest(id);
            return SkyPulsePlayerJourney.RankName(rank + 1) + " CRADLE · BEST " +
                bestResult + " / " + target + " GATES";
        }

        void LaunchNextRelayMission(long completedDay, int completedMission)
        {
            // Refresh before choosing the index. A result may remain open across
            // midnight; its old course must not skip today's first circuit.
            CheckDailyPeriod();
            SetHomeProtocol(SkyPulseMode.PulseRelay);
            int next = completedDay == dailyRelay.Data.day
                ? Mathf.Min(completedMission + 1, dailyRelay.Missions.Length - 1)
                : HomeNextMission();
            LaunchMission(SkyPulseMode.PulseRelay, next);
        }

        // Retry the completed extraction itself after a failed persistence call.
        // Keep the solved board, trace and date; never ask the player to solve it
        // again or silently start a different daily design at midnight.
        void RetryDailyBlackoutSave()
        {
            if (!blackoutDailyActive || activeMode != SkyPulseMode.BlackoutProtocol ||
                state != FlightState.GameOver || blackoutSession == null ||
                blackoutSession.State != MissionSessionState.Succeeded) return;
            missionFinalized = false;
            FinalizeMission();
        }

        void ReplayDailyBlackout()
        {
            blackoutSession = playerJourney.StartCase(true);
            blackoutDailyDay = playerJourney.Data.day;
            selectedMission = SkyPulseBlackoutCasebook.Index(blackoutDailyDay);
            missionFinalized = false;
            state = FlightState.Playing;
            missionResultScreen.SetActive(false);
            BuildBlackoutLab();
            RefreshScreens();
        }

        void ApplyJourneyHome()
        {
            if(playerJourney==null||homeScreen==null)return;playerJourney.EnsureDay();
            if(journeyButton==null)
            {
                journeyButton=MissionButton(homeScreen.transform,"",0,-650,940,()=>journeyAction?.Invoke(),Hex("#8cdcff"),148);
                var old=journeyButton.GetComponentInChildren<Text>();old.gameObject.SetActive(false);
                journeyTitle=CreateText(journeyButton.transform,"",new Vector2(0,28),new Vector2(870,46),32,Hex("#dff7ff"),TextAnchor.MiddleCenter,FontStyle.Bold);journeyTitle.raycastTarget=false;
                journeyDetail=CreateText(journeyButton.transform,"",new Vector2(0,-27),new Vector2(870,46),28,Hex("#aac9da"),TextAnchor.MiddleCenter,FontStyle.Normal);journeyDetail.raycastTarget=false;
            }
            HomeActionSurface(journeyButton,ProtocolAccent(homeProtocol));
            SetHomeRect(modeHomeGuide,new Vector2(0,-870),new Vector2(300,144));homeTasks.anchoredPosition=new Vector2(-340,-870);homeTasks.sizeDelta=new Vector2(300,144);homePrivacy.anchoredPosition=new Vector2(340,-870);homePrivacy.sizeDelta=new Vector2(280,144);
            foreach(var button in new[]{modeHomeGuide,homeTasks.GetComponent<Button>(),homePrivacy.GetComponent<Button>(),journeyButton}){SetComfortHitTarget(button);var face=button.targetGraphic as SkyPulseCircuitGraphic;if(face!=null){face.rectTransform.sizeDelta=((RectTransform)button.transform).sizeDelta;face.SetVerticesDirty();}}
            menuRouteText.gameObject.SetActive(false);
            if(homeProtocol==SkyPulseMode.BlackoutProtocol)
            {
                if(blackoutCampaign.RestoredMask!=255){int next=HomeNextMission();journeyTitle.text="NEXT · "+SkyPulseWorldRestoration.Benefit(next).ToUpperInvariant();journeyDetail.text="Clear vault "+(next+1).ToString("00")+" · saved lighting in all 12 worlds";journeyAction=()=>LaunchMission(SkyPulseMode.BlackoutProtocol,next);}
                else {homeLaunchButton.GetComponentInChildren<Text>().text="DAILY HEIST";journeyTitle.text="DAILY HEIST · "+SkyPulseBlackoutCasebook.All[SkyPulseBlackoutCasebook.Index(playerJourney.Data.day)].Name.ToUpperInvariant();journeyDetail.text="Gold cases "+GoldCaseCount()+" / 4 · unlock the Archivist cradle";journeyAction=LaunchDailyBlackout;}
            }
            else if(homeProtocol==SkyPulseMode.PulseRelay)
            {
                SetHomeRect(modeHomeMap,new Vector2(-240,-440),new Vector2(448,156));homeHangar.anchoredPosition=new Vector2(240,-440);homeHangar.sizeDelta=new Vector2(448,156);
                // Final Relay layout owns both the visible face and its lettering,
                // not just a larger invisible hit area over the former small card.
                HomeActionLabel(homeHangarTitle,Vector2.zero,new Vector2(416,116),40,Hex("#eefbff"),FontStyle.Bold);
                HomeActionLabel(modeHomeMap.GetComponentInChildren<Text>(),Vector2.zero,new Vector2(416,116),40,Hex("#eefbff"),FontStyle.Bold);
                HomeActionSurface(modeHomeMap,ProtocolAccent(homeProtocol));
                HomeActionSurface(homeHangar.GetComponent<Button>(),ProtocolAccent(homeProtocol));
                int next=HomeNextMission();journeyTitle.text="TODAY · "+dailyRelay.Missions[next].Name;journeyDetail.text="Clear this circuit · build your bird's cradle mastery";journeyAction=LaunchHomeProtocol;
            }
            else
            {
                int rank=playerJourney.BirdRank(equippedSkin.Id),target=rank==0?5:rank==1?15:30;
                journeyTitle.text=rank<3?"NEXT · "+SkyPulsePlayerJourney.RankName(rank+1)+" DISPLAY CRADLE":"AURORA MASTERY · "+equippedSkin.Name.ToUpperInvariant();
                journeyDetail.text=rank<3?"Best "+playerJourney.FlightBest(equippedSkin.Id)+" / "+target+" gates · cosmetic Hangar lighting":"Explore today's circuits or claim daily crystal tasks";journeyAction=rank<3?(Action)StartFlight:OpenMissionControl;
            }
            // A ready, existing five-crystal task takes priority; no new payout.
            for(int i=0;i<4;i++)if(!dailyMissions.Data.claimed[i]&&dailyMissions.Data.progress[i]>=SkyPulseDailyMissions.Targets[i]){int ready=i;journeyTitle.text="DAILY REWARD READY · 5 CRYSTALS";journeyDetail.text="Tap to claim your completed task";journeyAction=()=>ClaimJourneyDaily(ready);break;}
        }
        void ClaimJourneyDaily(int index)
        {
            try{CreditWallet(5,WalletRewardSource.DailyMission,index);UpdateCrystalLabels();Play(crystalSound);ApplyJourneyHome();}
            catch(Exception e){Debug.LogWarning("SkyPulse daily claim interrupted: "+e.GetType().Name);journeyDetail.text="Save interrupted · reopen Mission Control to recover";}
        }
        int GoldCaseCount(){int n=0;for(int i=0;i<4;i++)if((playerJourney.Data.caseGoldMask&(1<<i))!=0)n++;return n;}
        void RecordJourneyFlight(){if(playerJourney==null||equippedSkin==null)return;try{playerJourney.RecordFlight(equippedSkin.Id,score);}catch(Exception e){Debug.LogWarning("SkyPulse mastery save interrupted: "+e.GetType().Name);}}
        void LaunchDailyBlackout()
        {
            ResetToMenu();SetHomeProtocol(SkyPulseMode.BlackoutProtocol);homeScreen.SetActive(false);playerJourney.EnsureDay();blackoutDailyDay=playerJourney.Data.day;blackoutDailyActive=true;selectedMission=SkyPulseBlackoutCasebook.Index(blackoutDailyDay);activeMode=SkyPulseMode.BlackoutProtocol;blackoutSession=playerJourney.StartCase();missionFinalized=false;state=FlightState.Playing;bird.gameObject.SetActive(false);HideOriginalWorld(false);BuildBlackoutBoard();RefreshScreens();
        }
        void BuildDailyBlackoutResult(bool won,string reason,int medal)
        {
            bool saveFailed = won && reason.IndexOf("SAVE FAILED", StringComparison.Ordinal) >= 0;
            BlackoutBackdrop(missionResultScreen.transform);
            MissionText(missionResultScreen.transform, saveFailed ? "EXTRACTION UNSAVED" : won ? "DAILY HEIST COMPLETE" : "SIGNAL LOST", 835, 46, saveFailed ? Hex("#ff8992") : BlackoutIce);
            MissionText(missionResultScreen.transform, blackoutSession.Puzzle.Name, 750, 30, BlackoutMint);
            var report = BlackoutPanel(missionResultScreen.transform, "Daily case report", new Vector2(0,360), new Vector2(930,450));
            MissionText(report, saveFailed ? "PROGRESS SAVE INTERRUPTED" : won ? MedalName(medal) + " EXTRACTION" : reason, 140, 36, BlackoutIce, 860, 85);
            MissionText(report, "TRACE " + blackoutSession.Trace + " / " + blackoutSession.Puzzle.TraceLimit, 30, 30, BlackoutIce);
            MissionText(report, "ARCHIVE · " + GoldCaseCount() + " / 4 GOLD CASES", -65, 30, BlackoutMint);
            MissionText(report, saveFailed ? "Your solved route is retained. Tap Retry Save." : "Four gold case designs unlock the Archivist cradle.", -148, 25, BlackoutIce, 860, 75);
            BlackoutButton(missionResultScreen.transform, saveFailed ? "RETRY SAVE" : "REPLAY DAILY CASE", 0, -80, 900, saveFailed ? (Action)RetryDailyBlackoutSave : ReplayDailyBlackout, 160);
            BlackoutButton(missionResultScreen.transform,"VAULT MAP",0,-250,900,OpenBlackoutVaults,160);BlackoutButton(missionResultScreen.transform,"MAIN MENU",0,-420,900,ResetToMenu,160);
        }
    }
}
