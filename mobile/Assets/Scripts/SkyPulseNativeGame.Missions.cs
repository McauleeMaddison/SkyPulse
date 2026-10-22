using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        private SkyPulseMode activeMode=SkyPulseMode.EndlessRoute;
        private RelaySession relaySession;
        private BlackoutSession blackoutSession;
        private SkyPulseDailyMissions dailyMissions;
        private SkyPulseDailyRelay dailyRelay;
        private IMissionStore missionStore;
        private SkyPulseMode homeProtocol;
        private Image homeProtocolBackdrop;
        private SkyPulseCircuitGraphic homeProtocolGraphic;
        private Text homeProtocolCaption;
        private GameObject modeSelectionScreen, missionSelectionScreen, missionControlScreen, relayHudScreen, blackoutScreen, missionResultScreen;
        private Transform missionWorld;
        private readonly List<Transform> relayShutters=new List<Transform>();
        private readonly List<Transform> relayCells=new List<Transform>();
        private readonly List<Vector2> relayCellPositions=new List<Vector2>();
        private readonly HashSet<int> usedRelayCells=new HashSet<int>(10);
        private Transform relayExit, relayShield;
        private bool relayShieldCollected, missionFinalized;
        private int selectedMission;
        private float missionUiTimer;
        private RectTransform relayHudDock;
        private Text relayStatusText, relayHintText, blackoutStatusText, dailyResetText, dailyTotalText;
        private readonly List<Button> dailyClaimButtons=new List<Button>();
        private readonly List<Text> dailyProgressLabels=new List<Text>();
        private readonly List<RectTransform> dailyProgressBars=new List<RectTransform>();
        private readonly Dictionary<SpriteRenderer,bool> hiddenModeRenderers=new Dictionary<SpriteRenderer,bool>();
        private bool AuxiliaryScreenOpen => (modeSelectionScreen!=null && modeSelectionScreen.activeSelf) ||
            (missionSelectionScreen!=null && missionSelectionScreen.activeSelf) || (missionControlScreen!=null && missionControlScreen.activeSelf) ||
            (missionResultScreen!=null && missionResultScreen.activeSelf) || (modeGuideScreen!=null && modeGuideScreen.activeSelf);

        private void InitializeMissionServices()
        {
            // A new launch opens on Endless; retain the older preference key for backwards compatibility.
            homeProtocol=SkyPulseMode.EndlessRoute;
            missionStore=new PlayerPrefsMissionStore();
            dailyMissions=new SkyPulseDailyMissions(missionStore,()=>DateTime.UtcNow);
            dailyRelay=new SkyPulseDailyRelay(missionStore,()=>DateTime.UtcNow);
            blackoutCampaign=new BlackoutCampaignProgress(missionStore);
        }
        private void CheckDailyPeriod()
        {
            if(dailyMissions==null || !RecoverWalletTransaction())return;
            try {
                dailyMissions.EnsurePeriod();crystals=missionStore.GetInt(SkyPulseDailyMissions.WalletKey,crystals);
                if(dailyRelay!=null && dailyRelay.EnsurePeriod())
                {
                    if(missionSelectionScreen!=null && missionSelectionScreen.activeSelf && activeMode==SkyPulseMode.PulseRelay)OpenMissionSelection(SkyPulseMode.PulseRelay);
                    else if(homeScreen!=null && homeScreen.activeSelf && homeProtocol==SkyPulseMode.PulseRelay)ApplyHomeProtocolTheme();
                }
            }
            catch(Exception e) { Debug.LogWarning("SkyPulse mission save will retry: "+e.GetType().Name); }
        }
        private void RecordDailyEvent(DailyGameplayEvent kind,int amount=1)
        {
            if(dailyMissions==null)return;
            try { dailyMissions.Progress(kind,amount); }
            catch(Exception e) { Debug.LogWarning("SkyPulse mission progress save failed: "+e.GetType().Name); }
        }
        private void CreditWallet(int amount,WalletRewardSource source,int dailyIndex=-1)
        {
            if(!RecoverWalletTransaction())throw new InvalidOperationException("Wallet recovery is pending");
            if(dailyMissions!=null)dailyMissions.EnsurePeriod();
            if(source==WalletRewardSource.DailyMission)
            {
                if(amount!=5)throw new InvalidOperationException("Daily rewards are exactly five crystals");
                try { dailyMissions.Claim(dailyIndex); }
                finally { crystals=missionStore.GetInt(SkyPulseDailyMissions.WalletKey,crystals); }
                return;
            }
            crystals=missionStore.GetInt(SkyPulseDailyMissions.WalletKey,crystals);
            crystals+=amount;
            if(source==WalletRewardSource.FlightPickup)runCrystalsCollected+=amount;
        }
        private bool RecoverWalletTransaction()
        {
            try
            {
                if(dailyMissions!=null&&dailyMissions.Data.pending>=0){dailyMissions.RecoverPending();crystals=missionStore.GetInt(SkyPulseDailyMissions.WalletKey,crystals);}
                if(birdProgression!=null&&birdProgression.Data.pending){birdProgression.RecoverPending();RebuildBirdRoster();crystals=missionStore.GetInt(SkyPulseDailyMissions.WalletKey,crystals);}
                return true;
            }
            catch(Exception e) {Debug.LogWarning("SkyPulse wallet save is waiting for recovery: "+e.GetType().Name);return false;}
        }
        private Text MissionText(Transform root,string label,float y,int size=28,Color? colour=null,float width=900f,float height=64f)
        {
            var text=CreateText(root,label,new Vector2(0,y),new Vector2(width,height),size,colour??Hex("#d8f6ff"),TextAnchor.MiddleCenter,FontStyle.Bold);
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
        }
        private Button MissionButton(Transform root,string label,float x,float y,float width,Action action,Color? colour=null,float height=100)
        {
            var b=CreateNeonButton(root,label,new Vector2(x,y),new Vector2(width,height),colour??Hex("#45eaff"));
            var feedback=b.GetComponent<SkyPulseButtonFeedback>();
            if(feedback!=null){feedback.ReducedMotion=reduceMotionEnabled;protocolButtonFeedback.Add(feedback);}
            var t=b.GetComponentInChildren<Text>();t.resizeTextForBestFit=true;t.resizeTextMinSize=20;t.resizeTextMaxSize=28;
            PolishButton(b,colour.HasValue && colour.Value.g>.85f && colour.Value.r<.6f?2:1,colour??Hex("#45eaff"),label=="PLAY" || label.StartsWith("PULSE") || label.StartsWith("START"));
            b.onClick.AddListener(()=>action());return b;
        }
        private GameObject MissionScreen(string name,bool blackout=false,bool backdrop=true)
        {
            var root=CreateScreen(interfaceContentRoot,name);
            if(backdrop){var backing=CreateFullPanel(root.transform,name+" backdrop",blackout?Color.black:Hex("#030b1c"));ProtocolBackdrop(backing,blackout?2:0);}
            root.SetActive(false);return root;
        }
        private void CreateMissionInterface()
        {
            homeProtocolBackdrop=CreateFullPanel(homeScreen.transform,"Home protocol backdrop",Color.black).GetComponent<Image>();
            homeProtocolBackdrop.transform.SetAsFirstSibling();homeProtocolBackdrop.raycastTarget=false;
            homeProtocolGraphic=ProtocolBackdrop(homeProtocolBackdrop.rectTransform,1);
            foreach(var text in homeScreen.GetComponentsInChildren<Text>())if(text.text=="BUILD YOUR FLOCK · POWER YOUR NEXT FLIGHT")homeProtocolCaption=text;
            CreateProtocolDropdown();SetupHomePresentation();
            MissionButton(homeScreen.transform,"MISSION CONTROL",0,-950,720,OpenMissionControl,Hex("#ffc34d"));
            missionSelectionScreen=MissionScreen("Mission selection");
            missionControlScreen=MissionScreen("Mission Control");
            blackoutScreen=MissionScreen("Blackout terminal",true);
            relayHudScreen=MissionScreen("Pulse Relay HUD",false,false);
            var relayRoot=(RectTransform)relayHudScreen.transform;relayRoot.SetParent(safeAreaRoot,false);
            relayRoot.anchorMin=Vector2.zero;relayRoot.anchorMax=Vector2.one;relayRoot.offsetMin=relayRoot.offsetMax=Vector2.zero;
            missionResultScreen=MissionScreen("Protocol result");
            SetupModeHomeControls();
        }
        private void ClearScreen(GameObject root)
        {
            RemoveScreenPresentation(root);
            PrepareBlackoutScreenClear(root);
            // Screens are regenerated only on navigation/action, never every frame.
            foreach(Transform child in root.transform)
            {
                var rect=child as RectTransform;
                if(rect!=null && interfaceBackdrops.Contains(rect))continue;
                child.gameObject.SetActive(false);Destroy(child.gameObject);
            }
        }
        private void CloseAuxiliaryScreens()
        {
            modeDropdownOpen=false;
            foreach(var screen in new[]{modeSelectionScreen,missionSelectionScreen,missionControlScreen,missionResultScreen,modeGuideScreen})if(screen!=null)screen.SetActive(false);
        }
        private void ClearMissionSession()
        {
            ResetBlackoutEffects();
            ClearProtocolWorldArt();
            if(activeMode!=SkyPulseMode.EndlessRoute){shieldImmunityTimer=0f;shieldHitStopTimer=0f;}
            if(missionWorld!=null){missionWorld.gameObject.SetActive(false);Destroy(missionWorld.gameObject);missionWorld=null;}
            foreach(var item in hiddenModeRenderers)if(item.Key!=null)item.Key.enabled=item.Value;
            hiddenModeRenderers.Clear();
            relayShutters.Clear();relayCells.Clear();relayCellPositions.Clear();usedRelayCells.Clear();relayExit=null;relayShield=null;
            if(relayHudScreen!=null)relayHudScreen.SetActive(false);
            if(blackoutScreen!=null)blackoutScreen.SetActive(false);
            CloseAuxiliaryScreens();relaySession=null;blackoutSession=null;activeMode=SkyPulseMode.EndlessRoute;
            missionFinalized=false;relayShieldCollected=false;
        }
        private void SetHomeProtocol(SkyPulseMode mode)
        {
            homeProtocol=mode;PlayerPrefs.SetInt("skypulse.home-protocol.v1",(int)mode);PlayerPrefs.Save();ApplyHomeProtocolTheme();
        }
        private void ApplyHomeProtocolTheme()
        {
            if(homeProtocolBackdrop==null)return;
            bool alternate=homeProtocol!=SkyPulseMode.EndlessRoute;homeProtocolBackdrop.gameObject.SetActive(alternate);
            homeProtocolBackdrop.color=homeProtocol==SkyPulseMode.BlackoutProtocol?Color.black:Hex("#02131c");
            homeProtocolGraphic.Variant=homeProtocol==SkyPulseMode.BlackoutProtocol?2:1;
            homeProtocolGraphic.color=Color.white;
            homeProtocolGraphic.CinematicUnderlay=homeProtocol!=SkyPulseMode.EndlessRoute;
            homeProtocolGraphic.enabled=homeProtocol!=SkyPulseMode.BlackoutProtocol;
            homeProtocolBackdrop.sprite=homeProtocol==SkyPulseMode.PulseRelay?LoadSprite(RelayStationPath):LoadSprite(BlackoutCorePath);homeProtocolBackdrop.color=Color.white;
            homeProtocolGraphic.ReducedMotion=reduceMotionEnabled;homeProtocolGraphic.SetVerticesDirty();
            difficultyText.text=homeProtocol==SkyPulseMode.BlackoutProtocol?"BLACKOUT PROTOCOL":homeProtocol==SkyPulseMode.PulseRelay?"PULSE RELAY":"ENDLESS ROUTE";
            bool terminal=homeProtocol==SkyPulseMode.BlackoutProtocol;
            var protocolBadge=(RectTransform)difficultyText.transform.parent;
            protocolBadge.sizeDelta=new Vector2(terminal?310f:200f,68f);
            protocolBadge.anchoredPosition=new Vector2(terminal?-300f:-355f,950f);
            difficultyText.rectTransform.sizeDelta=new Vector2(terminal?290f:180f,48f);
            difficultyText.horizontalOverflow=terminal?HorizontalWrapMode.Wrap:HorizontalWrapMode.Overflow;
            difficultyText.verticalOverflow=terminal?VerticalWrapMode.Truncate:VerticalWrapMode.Overflow;
            menuTitleText.color=homeProtocol==SkyPulseMode.BlackoutProtocol?Hex("#65ff9d"):homeProtocol==SkyPulseMode.PulseRelay?Hex("#ffc34d"):Hex("#d8fbff");
            if(homeProtocolCaption!=null)homeProtocolCaption.text=homeProtocol==SkyPulseMode.BlackoutProtocol?"BLACKOUT PROTOCOL · SIGNAL HEIST":homeProtocol==SkyPulseMode.PulseRelay?"PULSE RELAY · RESTORE THE CIRCUIT":"BUILD YOUR FLOCK · POWER YOUR NEXT FLIGHT";
            ApplyPremiumHomeTheme();
        }
        private void PrepareEndlessMode() { SetHomeProtocol(SkyPulseMode.EndlessRoute);ResetToMenu(); }
        private void OpenModeSelection()
        {
            ResetToMenu();ShowProtocolDropdown();CheckDailyPeriod();
        }
        private void OpenMissionSelection(SkyPulseMode mode)
        {
            if(mode==SkyPulseMode.BlackoutProtocol){OpenBlackoutVaults();return;}
            CheckDailyPeriod();SetHomeProtocol(mode);
            ResetToMenu();homeScreen.SetActive(false);activeMode=mode;
            ClearScreen(missionSelectionScreen);missionSelectionScreen.SetActive(true);
            var colour=mode==SkyPulseMode.PulseRelay?Hex("#ffc34d"):Hex("#65ff9d");
            var architecture=missionSelectionScreen.GetComponentInChildren<SkyPulseCircuitGraphic>();architecture.Variant=(int)mode;architecture.CinematicUnderlay=true;var sceneImage=architecture.transform.parent.GetComponent<Image>();sceneImage.sprite=LoadSprite(mode==SkyPulseMode.PulseRelay?RelayStationPath:BlackoutCorePath);sceneImage.color=Color.white;architecture.SetVerticesDirty();
            MissionText(missionSelectionScreen.transform,mode==SkyPulseMode.PulseRelay?"PULSE RELAY":"BLACKOUT PROTOCOL",870,50,colour);
            MissionText(missionSelectionScreen.transform,"FOUR DAILY CIRCUITS · REFRESH "+dailyRelay.ResetUtc.ToLocalTime().ToString("HH:mm")+" LOCAL",795,25);
            for(int i=0;i<4;i++)
            {
                int index=i;float y=555-i*340;
                var card=PremiumPanel(missionSelectionScreen.transform,"Mission "+i,new Vector2(0,y),new Vector2(940,310),(int)mode,colour);
                string name=dailyRelay.Missions[i].Name;
                string briefing=dailyRelay.Missions[i].Briefing;
                int medal=dailyRelay.Data.medals[i];
                MissionText(card,name,117,30,colour,850,48);
                var brief=CreateText(card,briefing,new Vector2(0,25),new Vector2(850,114),24,Hex("#c6dded"),TextAnchor.MiddleCenter,FontStyle.Normal);
                brief.horizontalOverflow=HorizontalWrapMode.Wrap;brief.verticalOverflow=VerticalWrapMode.Truncate;
                MissionText(card,medal==0?"NOT COMPLETED":"BEST MEDAL · "+MedalName(medal),-53,21,null,850,28);
                MissionButton(card,"START MISSION",0,-113,780,()=>LaunchMission(mode,index),colour,80);
            }
            MissionButton(missionSelectionScreen.transform,"MODE SELECTION",-235,-860,430,OpenModeSelection);
            MissionButton(missionSelectionScreen.transform,"MISSION CONTROL",235,-860,430,OpenMissionControl,Hex("#ffc34d"));
        }
        private void LaunchMission(SkyPulseMode mode,int index)
        {
            if(!RecoverWalletTransaction())return;
            CheckDailyPeriod();
            if(mode==SkyPulseMode.PulseRelay && (index<0 || index>=dailyRelay.Missions.Length))return;
            if(mode==SkyPulseMode.BlackoutProtocol && !blackoutCampaign.CanPlay(index))return;
            FenceGameplayBoundary();
            SetHomeProtocol(mode);
            ResetToMenu();homeScreen.SetActive(false);selectedMission=index;activeMode=mode;missionFinalized=false;
            if(mode==SkyPulseMode.PulseRelay)
            {
                // Reuse setup, animation, input buffering and flight forces; only dispatch the course simulation.
                BeginFlight();foreach(var pair in pipePool)pair.Root.SetActive(false);
                foreach(var pickup in powerUpPool){pickup.Active=false;pickup.Root.SetActive(false);}
                foreach(var pickup in crystalPickupPool){pickup.Active=false;pickup.Root.SetActive(false);}
                relaySession=new RelaySession(dailyRelay.Missions[index]);CreateRelayWorld();BuildRelayHud();
            }
            else
            {
                shieldImmunityTimer=0f;shieldHitStopTimer=0f;
                state=FlightState.Playing;bird.gameObject.SetActive(false);blackoutSession=blackoutCampaign.Start(index);
                HideOriginalWorld(false);BuildBlackoutBoard();
            }
            RefreshScreens();
        }
        private void HideOriginalWorld(bool keepBird)
        {
            foreach(var renderer in FindObjectsByType<SpriteRenderer>())
            {
                if(missionWorld!=null && renderer.transform.IsChildOf(missionWorld))continue;
                if(keepBird && renderer.transform.IsChildOf(bird))continue;
                if(!hiddenModeRenderers.ContainsKey(renderer))hiddenModeRenderers.Add(renderer,renderer.enabled);
                renderer.enabled=false;
            }
        }
        private SpriteRenderer CircuitSprite(Transform parent,string name,Vector2 position,Vector2 size,Color colour,int order=4)
        {
            var r=CreateRenderer(name,whiteSprite,colour,order,parent);r.transform.localPosition=position;r.transform.localScale=new Vector3(size.x/whiteSprite.bounds.size.x,size.y/whiteSprite.bounds.size.y,1);return r;
        }
        private void CreateRelayWorld()
        {
            BuildPremiumRelayWorld();
        }
        private void AddRelayCell(int id,Vector2 at)
        {
            BuildPremiumRelayCell(id,at);
        }
        private int relayDisplayedCells=-1,relayDisplayedPassed=-1,relayDisplayedOpen=-1;
        private float relayUnlockFeedback;
        private string relayUnlockCaption="",relayFlightCaption="";
        private void BuildRelayHud()
        {
            ClearScreen(relayHudScreen);relayHudScreen.SetActive(true);
            var header=PremiumPanel(relayHudScreen.transform,"Relay flight deck",Vector2.zero,new Vector2(970,240),1,Hex("#45eaff"));
            relayHudDock=header;header.anchorMin=header.anchorMax=new Vector2(.5f,1);header.pivot=new Vector2(.5f,1);header.anchoredPosition=new Vector2(0,-12);
            var title=MissionText(header,"PULSE RELAY",80,30,Hex("#ffd078"),650,42);title.rectTransform.anchoredPosition=new Vector2(-110,80);
            var mission=MissionText(header,relaySession.Mission.Name,43,20,Hex("#bdebf0"),700,32);mission.rectTransform.anchoredPosition=new Vector2(-75,43);
            relayChargeText=RelayTopStat(header,"CRYSTALS",-310,Hex("#77eaff"));
            relayProgressText=RelayTopStat(header,"GATES OPEN",0,Hex("#45eaff"));
            relayStatusText=RelayTopStat(header,"PASSED",310,Hex("#65ffad"));
            MissionButton(header,"Ⅱ",410,58,100,PauseFlight,Hex("#8eeaff"),100);
            relayHintText=MissionText(header,"TWO MATCHING CRYSTALS OPEN EACH GATE",-91,21,Hex("#c4f4f5"),900,34);
            FitRelayHudToSafeArea();

            relayDisplayedCells=relayDisplayedPassed=relayDisplayedOpen=-1;relayUnlockFeedback=0;
            UpdateRelayPresentation();
        }
        private Text RelayTopStat(Transform header,string label,float x,Color colour)
        {
            var caption=MissionText(header,label,2,22,colour,280,30);caption.rectTransform.anchoredPosition=new Vector2(x,2);
            var value=MissionText(header,"0",-35,35,Hex("#e4fff5"),280,46);value.rectTransform.anchoredPosition=new Vector2(x,-35);return value;
        }
        private void FitRelayHudToSafeArea()
        {
            if(relayHudDock==null||interfaceContentRoot==null)return;
            float scale=interfaceContentRoot.localScale.x;relayHudDock.localScale=Vector3.one*scale;
            relayHudDock.anchoredPosition=new Vector2(0,-12*scale);
        }
        private void RelayUnlocked(int gate)
        {
            RecordDailyEvent(DailyGameplayEvent.RelayActivated);Play(unlockSound);PulseHaptic(.12f);
            relayUnlockCaption="GATE "+(gate+1).ToString("00")+" OPEN · FLY THROUGH";relayUnlockFeedback=.8f;
            relayOpeningTimes[gate]=protocolVisualTime;
        }
        private void UpdateRelayPresentation()
        {
            if(relaySession==null)return;
            bool captionChanged=relayDisplayedCells!=relaySession.Cells||relayDisplayedPassed!=relaySession.Passed;
            if(relayDisplayedCells!=relaySession.Cells){relayDisplayedCells=relaySession.Cells;relayChargeText.text=relayDisplayedCells+" / "+(relaySession.Mission.RelayCount*2);}
            if(relayDisplayedOpen!=relaySession.UnlockedGates){relayDisplayedOpen=relaySession.UnlockedGates;relayProgressText.text=relayDisplayedOpen+" / "+relaySession.Mission.RelayCount;}
            if(relayDisplayedPassed!=relaySession.Passed){relayDisplayedPassed=relaySession.Passed;relayStatusText.text=relayDisplayedPassed+" / "+relaySession.Mission.RelayCount;}
            if(captionChanged)relayFlightCaption=relaySession.Passed==relaySession.Mission.RelayCount?"REACH THE GREEN EXIT":"GATE "+(relaySession.Passed+1).ToString("00")+" · "+relaySession.GateCrystals(relaySession.Passed)+" / 2 CRYSTALS";
            string hint=relayUnlockFeedback>0?relayUnlockCaption:relayFlightCaption;
            if(relayHintText.gameObject.activeSelf!=(hint.Length>0))relayHintText.gameObject.SetActive(hint.Length>0);
            if(relayHintText.text!=hint)relayHintText.text=hint;
            for(int i=0;i<relayShutters.Count;i++)relayShutters[i].localPosition=new Vector3(BirdX+relaySession.Mission.RelayX(i)-relaySession.Distance,0,0);
            for(int i=0;i<relayCells.Count;i++)if(relayCells[i].gameObject.activeSelf)relayCells[i].localPosition=new Vector3(BirdX+relayCellPositions[i].x-relaySession.Distance,relayCellPositions[i].y,0);
            AnimateRelayArtwork();
            if(relayShield!=null)relayShield.localPosition=new Vector3(BirdX+2f-relaySession.Distance,0,0);
            if(relayExit!=null)relayExit.localPosition=new Vector3(BirdX+relaySession.Mission.ExitX-relaySession.Distance,0,0);
        }
        private static bool RelayCellContact(Vector2 from,Vector2 to,Vector2 cell)
        {
            // Swept generous pickup ellipse: never miss a crystal between two
            // simulation samples, and do not make a thumb chase its tiny glint.
            from=new Vector2((from.x-cell.x)/1.05f,(from.y-cell.y)/1.4f);
            to=new Vector2((to.x-cell.x)/1.05f,(to.y-cell.y)/1.4f);
            var delta=to-from;float t=delta.sqrMagnitude>.000001f?Mathf.Clamp01(-Vector2.Dot(from,delta)/delta.sqrMagnitude):0;
            return (from+delta*t).sqrMagnitude<=1;
        }
        private bool RelayPlaneContact(float plane,float previousDistance,float previousY,out float impactY)
        {
            float current=relaySession.Distance,radius=BirdHitboxHorizontalExtent()+.32f;
            bool contact=previousDistance<=plane+radius && current>=plane-radius;
            float t=current>previousDistance?Mathf.Clamp01((plane-previousDistance)/(current-previousDistance)):1;
            impactY=Mathf.Lerp(previousY,birdY,t)+BirdHitboxWorldOffset().y;return contact;
        }
        private void SimulateRelay(float dt)
        {
            if(state!=FlightState.Playing || relaySession==null || relaySession.State!=MissionSessionState.Playing)return;
            ConsumeBufferedFlap();UpdatePowerUpEffects(dt);
            if(shieldHitStopTimer>0){shieldHitStopTimer=Mathf.Max(0,shieldHitStopTimer-dt);return;}
            float step=slowFieldTimer>0?dt*.70f:dt,previousY=birdY,previousDistance=relaySession.Distance;
            UpdateBird(step);if(state!=FlightState.Playing)return;
            relaySession.Move(GetWorldWidth()*.345f*step);
            var from=new Vector2(previousDistance,previousY);var to=new Vector2(relaySession.Distance,birdY);
            // Collection and logical unlocking precede every gate impact and
            // course/pass resolution, including a crystal at the gate this step.
            for(int i=0;i<relayCellPositions.Count;i++)
            {
                if(usedRelayCells.Contains(i) || !RelayCellContact(from,to,relayCellPositions[i]) || !relaySession.Collect(i))continue;
                usedRelayCells.Add(i);ShowCrystalPickupBurst(new Vector3(BirdX+relayCellPositions[i].x-relaySession.Distance,relayCellPositions[i].y,0));
                relayCells[i].gameObject.SetActive(false);
                if(relaySession.LastUnlockedGate>=0)RelayUnlocked(relaySession.LastUnlockedGate);else {Play(crystalSound);PulseHaptic(.10f);}
            }
            if(relaySession.Mission.HasShield && !relayShieldCollected && RelayCellContact(from,to,new Vector2(2,0)))
            {relayShieldCollected=true;shieldCharges=1;if(relayShield!=null)relayShield.gameObject.SetActive(false);UpdatePowerUpHud();}
            for(int i=0;i<relayShutters.Count && relaySession.State==MissionSessionState.Playing;i++)
            {
                float impact;
                if(RelayPlaneContact(relaySession.Mission.RelayX(i),previousDistance,previousY,out impact))
                {
                    if(!relaySession.IsOpen(i))relaySession.Fail("GATE "+(i+1).ToString("00")+" · BOTH CRYSTALS REQUIRED");
                    else if(Mathf.Abs(impact-relaySession.Mission.RelayY[i])+BirdHitboxVerticalExtent()>RelaySession.ApertureHalfHeight && shieldImmunityTimer<=0)relaySession.Hazard(UseShield());
                }
            }
            float exitY;
            if(relaySession.State==MissionSessionState.Playing && RelayPlaneContact(relaySession.Mission.ExitX,previousDistance,previousY,out exitY) && Mathf.Abs(exitY-relaySession.Mission.ExitY)+BirdHitboxVerticalExtent()>RelaySession.ApertureHalfHeight && shieldImmunityTimer<=0)relaySession.Hazard(UseShield());
            relaySession.ResolveCourse(birdY,false);
            if(relaySession.State==MissionSessionState.Succeeded || relaySession.State==MissionSessionState.Failed)FinalizeMission();
        }
        private bool UpdateMissionModes(float dt)
        {
            UpdateGameplayPointerReleases();
            UpdateProtocolPresentation(dt);
            if(homeProtocolGraphic!=null)homeProtocolGraphic.ReducedMotion=reduceMotionEnabled;
            missionUiTimer+=dt;
            if(missionUiTimer>=1f){missionUiTimer=0;CheckDailyPeriod();if(missionControlScreen!=null && missionControlScreen.activeSelf)RefreshDailyCards();}
            if(modeGuideScreen!=null && modeGuideScreen.activeSelf && Input.GetKeyDown(KeyCode.Escape)) {modeGuideScreen.SetActive(false);return true;}
            if(AuxiliaryScreenOpen)return true;
            if(activeMode==SkyPulseMode.EndlessRoute)return false;
            foreach(var item in hiddenModeRenderers)if(item.Key!=null)item.Key.enabled=false;
            if(state==FlightState.Playing && (Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P))){PauseFlight();return true;}
            if(state==FlightState.Paused && (Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P))){ResumeFlight();return true;}
            if(state!=FlightState.Playing)return true;
            if(activeMode==SkyPulseMode.PulseRelay && relaySession!=null)
            {
                PollRelayGameplayPress();
                simulationAccumulator=Mathf.Min(simulationAccumulator+dt,MaximumSimulationCatchup);
                while(simulationAccumulator>=SimulationStep && state==FlightState.Playing){SimulateRelay(SimulationStep);simulationAccumulator-=SimulationStep;}
                if(state==FlightState.Playing){UpdateGameplayWingState(dt);UpdateBirdWingMotion();UpdateRelayPresentation();}
            }
            return true;
        }
        private bool RefreshMissionScreens()
        {
            if(activeMode==SkyPulseMode.EndlessRoute || (relaySession==null && blackoutSession==null))return false;
            homeScreen.SetActive(false);hudScreen.SetActive(false);gameOverScreen.SetActive(false);customizeScreen.SetActive(false);
            FenceGameplayBoundary();
            bool paused=state==FlightState.Paused;
            if(relaySession!=null)relaySession.Pause(paused);
            if(blackoutSession!=null)blackoutSession.Pause(paused);
            pauseScreen.SetActive(paused);pauseScreen.transform.SetAsLastSibling();
            relayHudScreen.SetActive(activeMode==SkyPulseMode.PulseRelay && state==FlightState.Playing);
            blackoutScreen.SetActive(activeMode==SkyPulseMode.BlackoutProtocol && state==FlightState.Playing);
            return true;
        }
        private void BuildBlackoutBoard() { BuildBlackoutLab(); }
        private void FinishRelayFailure(string reason)
        {
            if(relaySession==null)return;relaySession.Fail(reason);FinalizeMission();
        }
        private static string MedalName(int medal)=>medal>=3?"GOLD":medal==2?"SILVER":"BRONZE";
        private void FinalizeMission()
        {
            if(activeMode==SkyPulseMode.EndlessRoute)return;
            var outcome=activeMode==SkyPulseMode.PulseRelay?(relaySession==null?MissionSessionState.Selection:relaySession.State):(blackoutSession==null?MissionSessionState.Selection:blackoutSession.State);
            if(outcome!=MissionSessionState.Succeeded && outcome!=MissionSessionState.Failed)return;
            if(missionFinalized)return;missionFinalized=true;
            bool won=activeMode==SkyPulseMode.PulseRelay?relaySession.State==MissionSessionState.Succeeded:blackoutSession.State==MissionSessionState.Succeeded;
            int medal=activeMode==SkyPulseMode.PulseRelay?relaySession.Medal:blackoutSession.Medal;
            string reason=activeMode==SkyPulseMode.PulseRelay?relaySession.Result:blackoutSession.Result;
            if(won)
            {
                bool saved=true;
                try { if(activeMode==SkyPulseMode.BlackoutProtocol)blackoutCampaign.Complete(selectedMission,medal,blackoutSession.Trace);else dailyRelay.Complete(relaySession.Mission.Day,selectedMission,medal); }
                catch(Exception e) { saved=false;Debug.LogWarning("SkyPulse medal save failed: "+e.GetType().Name);reason+=" · SAVE FAILED"; }
                if(saved && activeMode==SkyPulseMode.BlackoutProtocol && selectedMission==0)RecordDailyEvent(DailyGameplayEvent.IntroBlackoutCompleted);
                if(saved && activeMode==SkyPulseMode.BlackoutProtocol && selectedMission<SkyPulseBlackoutCampaign.Stages.Length-1)
                {
                    int completed=selectedMission;ActivateNextBlackoutPuzzle(completed);Play(unlockSound);PulseHaptic(.16f);return;
                }
            }
            FenceGameplayBoundary();
            state=FlightState.GameOver;bird.gameObject.SetActive(false);if(birdBodyCollider!=null)birdBodyCollider.enabled=false;
            relayHudScreen.SetActive(false);blackoutScreen.SetActive(false);pauseScreen.SetActive(false);
            ClearScreen(missionResultScreen);missionResultScreen.SetActive(true);missionResultScreen.transform.SetAsLastSibling();
            if(activeMode==SkyPulseMode.BlackoutProtocol){BuildBlackoutResult(won,reason,medal);Play(won?unlockSound:crashSound);return;}
            var colour=won?Hex("#65ff9d"):Hex("#ffc34d");
            var architecture=missionResultScreen.GetComponentInChildren<SkyPulseCircuitGraphic>();architecture.Variant=1;architecture.CinematicUnderlay=true;var backing=architecture.transform.parent.GetComponent<Image>();backing.sprite=LoadSprite(RelayStationPath);backing.color=Color.white;
            MissionText(missionResultScreen.transform,"PULSE RELAY",840,48,Hex("#ffd078"));
            var report=PremiumPanel(missionResultScreen.transform,"Circuit report",new Vector2(0,365),new Vector2(930,650),1,colour);
            MissionText(report,won?"CIRCUIT COMPLETE":"FLIGHT INTERRUPTED",240,40,colour);
            MissionText(report,relaySession.Mission.Name,150,30);
            MissionText(report,won?MedalName(medal)+" MEDAL":reason,42,36,colour,860,100);
            MissionText(report,"CRYSTALS  "+relaySession.Cells+" / "+(relaySession.Mission.RelayCount*2),-75,28,Hex("#aeefff"));
            MissionText(report,"GATES  "+relaySession.Passed+" / "+relaySession.Mission.RelayCount,-145,28,Hex("#94ffe1"));
            if(reason.Contains("SAVE FAILED"))MissionText(report,"MEDAL SAVE FAILED · RETRY",-240,26,Hex("#ff8595"));
            var mode=activeMode;int mission=selectedMission;long day=relaySession.Mission.Day;
            if(won && mission<3 && !reason.Contains("SAVE FAILED"))MissionButton(missionResultScreen.transform,"NEXT MISSION",0,-90,850,()=>LaunchMission(mode,day==dailyRelay.Data.day?mission+1:HomeNextMission()),colour,112);
            MissionButton(missionResultScreen.transform,"RETRY MISSION",0,won&&mission<3?-240:-90,850,()=>LaunchMission(mode,mission),colour,112);
            MissionButton(missionResultScreen.transform,"DAILY CIRCUITS",0,-405,850,()=>OpenMissionSelection(mode));
            MissionButton(missionResultScreen.transform,"MAIN MENU",0,-550,850,ResetToMenu);
            Play(won?unlockSound:crashSound);
        }
        private void OpenMissionControl()
        {
            ResetToMenu();homeScreen.SetActive(false);CheckDailyPeriod();ClearScreen(missionControlScreen);missionControlScreen.SetActive(true);
            dailyClaimButtons.Clear();dailyProgressLabels.Clear();dailyProgressBars.Clear();
            MissionText(missionControlScreen.transform,"MISSION CONTROL",880,50,Hex("#ffc34d"));
            dailyResetText=MissionText(missionControlScreen.transform,"",780,23);
            for(int i=0;i<4;i++)
            {
                int index=i;var card=PremiumPanel(missionControlScreen.transform,"Daily task "+SkyPulseDailyMissions.Ids[i],new Vector2(0,535-i*350),new Vector2(940,310),i==2?2:1,i==2?Hex("#65ffad"):Hex("#45eaff"));
                MissionText(card,SkyPulseDailyMissions.Modes[i],106,22,Hex("#45eaff"));
                MissionText(card,SkyPulseDailyMissions.Objectives[i],48,31);
                var back=CreatePanel(card,"Progress rail",new Vector2(-195,-30),new Vector2(430,18),Hex("#142d46"));
                var bar=CreatePanel(back,"Mission progress",Vector2.zero,new Vector2(0,12),Hex("#45eaff"));dailyProgressBars.Add(bar);
                dailyProgressLabels.Add(CreateText(card,"",new Vector2(-180,-91),new Vector2(470,50),23,Hex("#a1d6ed"),TextAnchor.MiddleCenter,FontStyle.Bold));
                var rewardIcon=CreateImage(card,"Crystal reward icon",new Vector2(415,100),new Vector2(38,58),Color.white);rewardIcon.sprite=LoadSprite(CrystalArtworkPath);rewardIcon.preserveAspect=true;rewardIcon.raycastTarget=false;
                dailyClaimButtons.Add(MissionButton(card,"5 CRYSTALS",255,-64,340,()=>ClaimDaily(index),Hex("#ffc34d")));
            }
            dailyTotalText=MissionText(missionControlScreen.transform,"",-710,26,Hex("#ffc34d"));
            MissionButton(missionControlScreen.transform,"MODE SELECTION",-235,-860,430,OpenModeSelection);
            MissionButton(missionControlScreen.transform,"HOME",235,-860,430,ResetToMenu);
            RefreshDailyCards();
        }
        private void RefreshDailyCards()
        {
            if(dailyMissions==null || dailyResetText==null)return;
            var remaining=dailyMissions.ResetUtc-DateTime.UtcNow;if(remaining<TimeSpan.Zero)remaining=TimeSpan.Zero;
            dailyResetText.text=$"RESET {dailyMissions.ResetUtc.ToLocalTime():ddd HH:mm} LOCAL · {Math.Floor(remaining.TotalHours):00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
            var data=dailyMissions.Data;
            for(int i=0;i<dailyClaimButtons.Count;i++)
            {
                bool ready=data.progress[i]>=SkyPulseDailyMissions.Targets[i];
                dailyProgressLabels[i].text=$"{data.progress[i]} / {SkyPulseDailyMissions.Targets[i]} · "+(data.claimed[i]?"CLAIMED":ready?"READY TO CLAIM":"IN PROGRESS");
                dailyClaimButtons[i].interactable=ready && !data.claimed[i];
                dailyClaimButtons[i].GetComponentInChildren<Text>().text=data.claimed[i]?"CLAIMED":ready?"CLAIM 5 CRYSTALS":"REWARD · 5";
                float width=430f*data.progress[i]/SkyPulseDailyMissions.Targets[i];dailyProgressBars[i].sizeDelta=new Vector2(width,12);dailyProgressBars[i].anchoredPosition=new Vector2(-215+width*.5f,0);
            }
            dailyTotalText.text=$"CLAIMED TODAY · {dailyMissions.ClaimedTotal} / 20 CRYSTALS   ·   WALLET {crystals}";
            if(dailyMissions.ClockBehind)dailyTotalText.text="CLOCK BEHIND SAVED DAY · CLAIMS RETAINED · "+dailyMissions.ClaimedTotal+" / 20";
        }
        private void ClaimDaily(int index)
        {
            if(missionControlScreen==null || !missionControlScreen.activeSelf)return;
            dailyClaimButtons[index].interactable=false;
            try { CreditWallet(5,WalletRewardSource.DailyMission,index);UpdateCrystalLabels();Play(crystalSound);RefreshDailyCards(); }
            catch(Exception e){Debug.LogWarning("SkyPulse reward journal retained for recovery: "+e.GetType().Name);dailyTotalText.text="SAVE INTERRUPTED · REOPEN TO RECOVER YOUR CLAIM";}
        }
    }
}
