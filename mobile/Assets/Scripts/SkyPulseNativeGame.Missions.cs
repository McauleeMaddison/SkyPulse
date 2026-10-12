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
        private SkyPulseModeProgress missionProgress;
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
        private readonly HashSet<int> usedRelayCells=new HashSet<int>();
        private Transform relayExit, relayShield;
        private bool relayShieldCollected, missionFinalized, hackArmed;
        private int selectedMission;
        private float missionUiTimer;
        private Text relayStatusText, relayHintText, blackoutStatusText, blackoutHintText, dailyResetText, dailyTotalText;
        private Button pulseButton, hackButton;
        private readonly List<Button> dailyClaimButtons=new List<Button>();
        private readonly List<Text> dailyProgressLabels=new List<Text>();
        private readonly List<RectTransform> dailyProgressBars=new List<RectTransform>();
        private readonly Dictionary<SpriteRenderer,bool> hiddenModeRenderers=new Dictionary<SpriteRenderer,bool>();
        private bool AuxiliaryScreenOpen => (modeSelectionScreen!=null && modeSelectionScreen.activeSelf) ||
            (missionSelectionScreen!=null && missionSelectionScreen.activeSelf) || (missionControlScreen!=null && missionControlScreen.activeSelf) ||
            (missionResultScreen!=null && missionResultScreen.activeSelf);

        private void InitializeMissionServices()
        {
            // A new launch opens on Endless; retain the older preference key for backwards compatibility.
            homeProtocol=SkyPulseMode.EndlessRoute;
            missionStore=new PlayerPrefsMissionStore();
            dailyMissions=new SkyPulseDailyMissions(missionStore,()=>DateTime.UtcNow);
            missionProgress=new SkyPulseModeProgress(missionStore);
            blackoutCampaign=new BlackoutCampaignProgress(missionStore);
        }
        private void CheckDailyPeriod()
        {
            if(dailyMissions==null)return;
            try { dailyMissions.EnsurePeriod(); crystals=missionStore.GetInt(SkyPulseDailyMissions.WalletKey,crystals); }
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
            if(dailyMissions==null || dailyMissions.Data.pending<0)return true;
            try { dailyMissions.RecoverPending();crystals=missionStore.GetInt(SkyPulseDailyMissions.WalletKey,crystals);return true; }
            catch(Exception e) { Debug.LogWarning("SkyPulse wallet save is waiting for recovery: "+e.GetType().Name);return false; }
        }
        private Text MissionText(Transform root,string label,float y,int size=28,Color? colour=null,float width=900f,float height=64f)
        {
            var text=CreateText(root,label,new Vector2(0,y),new Vector2(width,height),size,colour??Hex("#d8f6ff"),TextAnchor.MiddleCenter,FontStyle.Bold);
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
        }
        private Button MissionButton(Transform root,string label,float x,float y,float width,Action action,Color? colour=null,float height=100)
        {
            var b=CreateNeonButton(root,label,new Vector2(x,y),new Vector2(width,height),colour??Hex("#45eaff"));
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
            missionResultScreen=MissionScreen("Protocol result");
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
            foreach(var screen in new[]{modeSelectionScreen,missionSelectionScreen,missionControlScreen,missionResultScreen})if(screen!=null)screen.SetActive(false);
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
            missionFinalized=false;hackArmed=false;relayShieldCollected=false;
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
            if(homeProtocolCaption!=null)homeProtocolCaption.text=homeProtocol==SkyPulseMode.BlackoutProtocol?"BLACKOUT PROTOCOL · RECOVER THE DATA":homeProtocol==SkyPulseMode.PulseRelay?"PULSE RELAY · RESTORE THE CIRCUIT":"BUILD YOUR FLOCK · POWER YOUR NEXT FLIGHT";
            ApplyPremiumHomeTheme();
        }
        private void PrepareEndlessMode() { SetHomeProtocol(SkyPulseMode.EndlessRoute);ResetToMenu(); }
        private void OpenModeSelection()
        {
            ResetToMenu();ShowProtocolDropdown();CheckDailyPeriod();
        }
        private void OpenMissionSelection(SkyPulseMode mode)
        {
            if(mode==SkyPulseMode.BlackoutProtocol){OpenBlackoutSectors();return;}
            SetHomeProtocol(mode);
            ResetToMenu();homeScreen.SetActive(false);activeMode=mode;
            ClearScreen(missionSelectionScreen);missionSelectionScreen.SetActive(true);
            var colour=mode==SkyPulseMode.PulseRelay?Hex("#ffc34d"):Hex("#65ff9d");
            var architecture=missionSelectionScreen.GetComponentInChildren<SkyPulseCircuitGraphic>();architecture.Variant=(int)mode;architecture.CinematicUnderlay=true;var sceneImage=architecture.transform.parent.GetComponent<Image>();sceneImage.sprite=LoadSprite(mode==SkyPulseMode.PulseRelay?RelayStationPath:BlackoutCorePath);sceneImage.color=Color.white;architecture.SetVerticesDirty();
            MissionText(missionSelectionScreen.transform,mode==SkyPulseMode.PulseRelay?"PULSE RELAY":"BLACKOUT PROTOCOL",870,50,colour);
            MissionText(missionSelectionScreen.transform,"TUTORIAL + THREE MISSIONS · ALL AVAILABLE",795,25);
            for(int i=0;i<4;i++)
            {
                int index=i;float y=555-i*340;
                var card=PremiumPanel(missionSelectionScreen.transform,"Mission "+i,new Vector2(0,y),new Vector2(940,310),(int)mode,colour);
                string name=mode==SkyPulseMode.PulseRelay?RelayMission.All[i].Name:BlackoutPuzzle.All[i].Name;
                string briefing=mode==SkyPulseMode.PulseRelay?RelayMission.All[i].Briefing:BlackoutPuzzle.All[i].Briefing;
                int medal=(mode==SkyPulseMode.PulseRelay?missionProgress.Relay:missionProgress.Blackout).medals[i];
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
            if(mode==SkyPulseMode.BlackoutProtocol && !blackoutCampaign.CanPlay(index))return;
            SetHomeProtocol(mode);
            ResetToMenu();homeScreen.SetActive(false);selectedMission=index;activeMode=mode;missionFinalized=false;
            if(mode==SkyPulseMode.PulseRelay)
            {
                // Reuse setup, animation, input buffering and flight forces; only dispatch the course simulation.
                BeginFlight();foreach(var pair in pipePool)pair.Root.SetActive(false);
                foreach(var pickup in powerUpPool){pickup.Active=false;pickup.Root.SetActive(false);}
                foreach(var pickup in crystalPickupPool){pickup.Active=false;pickup.Root.SetActive(false);}
                relaySession=new RelaySession(RelayMission.All[index]);CreateRelayWorld();BuildRelayHud();
            }
            else
            {
                shieldImmunityTimer=0f;shieldHitStopTimer=0f;
                state=FlightState.Playing;bird.gameObject.SetActive(false);blackoutSession=new BlackoutSession(SkyPulseBlackoutCampaign.Stages[index]);
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
        private void BuildRelayHud()
        {
            ClearScreen(relayHudScreen);relayHudScreen.SetActive(true);
            var header=PremiumPanel(relayHudScreen.transform,"Relay flight deck",new Vector2(0,784),new Vector2(970,355),1,Hex("#45eaff"));
            MissionText(header,"PULSE RELAY",118,43,Hex("#ffd078"));
            MissionText(header,relaySession.Mission.Name,51,26,Hex("#bdebf0"));
            relayChargeText=ProtocolStat(header,"CHARGE", "0",-313,-52,286,1,Hex("#ffd078"));
            relayProgressText=ProtocolStat(header,"RELAYS", "0 / 0",0,-52,286,1,Hex("#45eaff"));
            relayStatusText=ProtocolStat(header,"NEXT RELAY", "01",313,-52,286,1,Hex("#45eaff"));
            relayHintText=MissionText(relayHudScreen.transform,"",535,24,Hex("#c4f4f5"),940,64);
            MissionButton(relayHudScreen.transform,"PAUSE",375,425,230,PauseFlight);
            PremiumPanel(relayHudScreen.transform,"Pilot control deck",new Vector2(0,-800),new Vector2(990,200),1,Hex("#45eaff"));
            pulseButton=MissionButton(relayHudScreen.transform,"PULSE · 0",230,-770,460,PressPulse,Hex("#ffd078"));
            MissionButton(relayHudScreen.transform,"FLAP",-240,-770,430,()=>{if(state==FlightState.Playing)BufferFlapInput();});
            MissionText(relayHudScreen.transform,"GOLD CELLS = CHARGE · ALIGN BEFORE PULSING",-860,21,Hex("#a3c7d8"));
            UpdateRelayPresentation();
        }
        private void PressPulse()
        {
            if(activeMode!=SkyPulseMode.PulseRelay || state!=FlightState.Playing || relaySession==null)return;
            if(relaySession.Pulse(birdY)) { RecordDailyEvent(DailyGameplayEvent.RelayActivated);Play(unlockSound); }
            UpdateRelayPresentation();
        }
        private void UpdateRelayPresentation()
        {
            if(relaySession==null)return;
            relayChargeText.text=relaySession.Charges.ToString();relayProgressText.text=$"{relaySession.Passed} / {relaySession.Mission.RelayCount}";relayStatusText.text=relaySession.NextRelay>=relaySession.Mission.RelayCount?"EXIT":(relaySession.NextRelay+1).ToString("00");
            int next=relaySession.NextRelay;float ahead=next<relaySession.Mission.RelayCount?relaySession.Mission.RelayX(next)-relaySession.Distance:0;
            relayHintText.text=next==relaySession.Mission.RelayCount?"ALL ACTIVATED · REACH THE GREEN EXIT":$"NEXT: RELAY {next+1} · "+(ahead<=6f?"ALIGN & PRESS PULSE":"COLLECT A GOLD CELL");
            pulseButton.GetComponentInChildren<Text>().text="PULSE · "+relaySession.Charges;
            pulseButton.interactable=state==FlightState.Playing && next<relaySession.Mission.RelayCount && relaySession.Charges>0 && ahead>=.6f && ahead<=6f && Mathf.Abs(birdY-relaySession.Mission.RelayY[next])<=1.55f;
            for(int i=0;i<relayShutters.Count;i++)
            {
                relayShutters[i].localPosition=new Vector3(BirdX+relaySession.Mission.RelayX(i)-relaySession.Distance,0,0);

            }
            for(int i=0;i<relayCells.Count;i++)relayCells[i].localPosition=new Vector3(BirdX+relayCellPositions[i].x-relaySession.Distance,relayCellPositions[i].y,0);
            AnimateRelayArtwork();
            if(relayShield!=null)relayShield.localPosition=new Vector3(BirdX+2f-relaySession.Distance,0,0);
            if(relayExit!=null)relayExit.localPosition=new Vector3(BirdX+relaySession.Mission.ExitX-relaySession.Distance,0,0);
        }
        private void SimulateRelay(float dt)
        {
            ConsumeBufferedFlap();UpdatePowerUpEffects(dt);
            if(shieldHitStopTimer>0){shieldHitStopTimer=Mathf.Max(0,shieldHitStopTimer-dt);return;}
            float step=slowFieldTimer>0?dt*.70f:dt;
            UpdateBird(step);if(state!=FlightState.Playing)return;
            relaySession.Advance(GetWorldWidth()*.345f*step,birdY);
            for(int i=0;i<relayCellPositions.Count;i++)
            {
                var at=relayCellPositions[i];float x=BirdX+at.x-relaySession.Distance;
                if(!usedRelayCells.Contains(i) && Mathf.Abs(x-BirdX)<.6f && Mathf.Abs(at.y-birdY)<.9f && relaySession.Collect(i))
                {usedRelayCells.Add(i);relayCells[i].gameObject.SetActive(false);Play(crystalSound);}
            }
            // Reuse one-collision Aegis behavior; no charge cells enter the crystal wallet.
            if(relaySession.Mission.HasShield && !relayShieldCollected && Mathf.Abs(2f-relaySession.Distance)<.65f && Mathf.Abs(birdY)<1f)
            { relayShieldCollected=true;shieldCharges=1;if(relayShield!=null)relayShield.gameObject.SetActive(false);UpdatePowerUpHud(); }
            for(int i=0;i<relayShutters.Count;i++)
            {
                float x=relaySession.Mission.RelayX(i)-relaySession.Distance;
                if(Mathf.Abs(x)<=BirdHitboxHorizontalExtent()+.32f && (!relaySession.IsOpen(i) || Mathf.Abs(birdY-relaySession.Mission.RelayY[i])+BirdHitboxVerticalExtent()>1.8f) && shieldImmunityTimer<=0)
                    relaySession.Hazard(UseShield());
            }
            if(relaySession.State==MissionSessionState.Succeeded || relaySession.State==MissionSessionState.Failed)FinalizeMission();
        }
        private bool UpdateMissionModes(float dt)
        {
            UpdateProtocolPresentation(dt);
            if(homeProtocolGraphic!=null)homeProtocolGraphic.ReducedMotion=reduceMotionEnabled;
            missionUiTimer+=dt;
            if(missionUiTimer>=1f){missionUiTimer=0;CheckDailyPeriod();if(missionControlScreen!=null && missionControlScreen.activeSelf)RefreshDailyCards();}
            if(AuxiliaryScreenOpen)return true;
            if(activeMode==SkyPulseMode.EndlessRoute)return false;
            foreach(var item in hiddenModeRenderers)if(item.Key!=null)item.Key.enabled=false;
            if(state==FlightState.Playing && (Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P))){PauseFlight();return true;}
            if(state==FlightState.Paused && (Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P))){ResumeFlight();return true;}
            if(state!=FlightState.Playing)return true;
            if(activeMode==SkyPulseMode.PulseRelay && relaySession!=null)
            {
                if(WasTapped() && !PointerOverUi())BufferFlapInput();
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
            bool paused=state==FlightState.Paused;
            if(relaySession!=null)relaySession.Pause(paused);
            if(blackoutSession!=null)blackoutSession.Pause(paused);
            pauseScreen.SetActive(paused);pauseScreen.transform.SetAsLastSibling();
            relayHudScreen.SetActive(activeMode==SkyPulseMode.PulseRelay && state==FlightState.Playing);
            blackoutScreen.SetActive(activeMode==SkyPulseMode.BlackoutProtocol && state==FlightState.Playing);
            return true;
        }
        private void BuildBlackoutBoard() { BuildBlackoutLab(); }
        private void BlackoutAction(int tile)
        {
            if(activeMode!=SkyPulseMode.BlackoutProtocol || state!=FlightState.Playing || blackoutSession==null)return;
            if(!blackoutSession.Act(tile,hackArmed))return;
            hackArmed=false;Play(scoreSound);
            if(blackoutSession.State==MissionSessionState.Succeeded || blackoutSession.State==MissionSessionState.Failed)FinalizeMission();
            else RefreshBlackoutLab();
        }
        private void FinishRelayFailure(string reason)
        {
            if(relaySession==null)return;relaySession.Fail(reason);FinalizeMission();
        }
        private static string MedalName(int medal)=>medal>=3?"GOLD":medal==2?"SILVER":"BRONZE";
        private void FinalizeMission()
        {
            if(missionFinalized)return;missionFinalized=true;
            bool won=activeMode==SkyPulseMode.PulseRelay?relaySession.State==MissionSessionState.Succeeded:blackoutSession.State==MissionSessionState.Succeeded;
            int medal=activeMode==SkyPulseMode.PulseRelay?relaySession.Medal:blackoutSession.Medal;
            string reason=activeMode==SkyPulseMode.PulseRelay?relaySession.Result:blackoutSession.Result;
            if(won)
            {
                try { if(activeMode==SkyPulseMode.BlackoutProtocol)blackoutCampaign.Complete(selectedMission,medal,blackoutSession.Trace);else missionProgress.Complete(activeMode,selectedMission,medal); }
                catch(Exception e) { Debug.LogWarning("SkyPulse medal save failed: "+e.GetType().Name);reason+=" · SAVE FAILED"; }
                if(activeMode==SkyPulseMode.BlackoutProtocol && selectedMission==0)RecordDailyEvent(DailyGameplayEvent.IntroBlackoutCompleted);
            }
            state=FlightState.GameOver;bird.gameObject.SetActive(false);if(birdBodyCollider!=null)birdBodyCollider.enabled=false;
            relayHudScreen.SetActive(false);blackoutScreen.SetActive(false);pauseScreen.SetActive(false);
            ClearScreen(missionResultScreen);missionResultScreen.SetActive(true);missionResultScreen.transform.SetAsLastSibling();
            if(activeMode==SkyPulseMode.BlackoutProtocol){BuildBlackoutResult(won,reason,medal);Play(won?unlockSound:crashSound);return;}
            var colour=won?Hex("#65ff9d"):Hex("#ffc34d");
            MissionText(missionResultScreen.transform,won?"PROTOCOL COMPLETE":"PROTOCOL INTERRUPTED",500,44,colour);
            MissionText(missionResultScreen.transform,reason,360,34,colour);
            MissionText(missionResultScreen.transform,won?"MEDAL · "+MedalName(medal):"RETRY THE MISSION TO RESTORE THIS CIRCUIT",230,30);
            MissionText(missionResultScreen.transform,"Mission completion saves medals. Daily tasks reward crystals.",90,24);
            var mode=activeMode;int mission=selectedMission;
            MissionButton(missionResultScreen.transform,"RETRY MISSION",0,-85,850,()=>LaunchMission(mode,mission),colour);
            MissionButton(missionResultScreen.transform,"MISSIONS",0,-235,850,()=>OpenMissionSelection(mode));
            MissionButton(missionResultScreen.transform,"MISSION CONTROL",0,-385,850,OpenMissionControl,Hex("#ffc34d"));
            MissionButton(missionResultScreen.transform,"MODE SELECTION",0,-535,850,OpenModeSelection);
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
