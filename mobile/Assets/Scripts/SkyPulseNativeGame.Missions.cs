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
            homeProtocol=(SkyPulseMode)Mathf.Clamp(PlayerPrefs.GetInt("skypulse.home-protocol.v1",0),0,2);
            missionStore=new PlayerPrefsMissionStore();
            dailyMissions=new SkyPulseDailyMissions(missionStore,()=>DateTime.UtcNow);
            missionProgress=new SkyPulseModeProgress(missionStore);
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
            b.onClick.AddListener(()=>action());return b;
        }
        private GameObject MissionScreen(string name,bool blackout=false,bool backdrop=true)
        {
            var root=CreateScreen(interfaceContentRoot,name);
            if(backdrop)CreateFullPanel(root.transform,name+" backdrop",blackout?Color.black:Hex("#030b1c"));
            root.SetActive(false);return root;
        }
        private void CreateMissionInterface()
        {
            homeProtocolBackdrop=CreateFullPanel(homeScreen.transform,"Home protocol backdrop",Color.black).GetComponent<Image>();
            homeProtocolBackdrop.transform.SetAsFirstSibling();homeProtocolBackdrop.raycastTarget=false;
            var themeArt=new GameObject("Home protocol circuits",typeof(RectTransform),typeof(CanvasRenderer),typeof(SkyPulseCircuitGraphic));
            themeArt.transform.SetParent(homeProtocolBackdrop.transform,false);var themeRect=themeArt.GetComponent<RectTransform>();themeRect.sizeDelta=new Vector2(1000,1880);
            homeProtocolGraphic=themeArt.GetComponent<SkyPulseCircuitGraphic>();homeProtocolGraphic.raycastTarget=false;homeProtocolGraphic.Ambient=true;
            foreach(var text in homeScreen.GetComponentsInChildren<Text>())if(text.text=="BUILD YOUR FLOCK · POWER YOUR NEXT FLIGHT")homeProtocolCaption=text;
            modeSelectionScreen=MissionScreen("Mode selection");
            MissionText(modeSelectionScreen.transform,"SELECT YOUR PROTOCOL",855,50);
            MissionText(modeSelectionScreen.transform,"THREE WAYS TO PLAY · ONE SKYPULSE COLLECTION",785,23);
            var names=new[]{"ENDLESS ROUTE","PULSE RELAY","BLACKOUT PROTOCOL"};
            var details=new[]{"Tap to fly through the 12-world route.\nEarn crystals and build your Endless best.","Collect charge. Align. Pulse the numbered relays.\nRestore an authored circuit and reach its exit.","Tap through a terminal circuit. Recover data.\nSpend hacks carefully and stay below the trace limit."};
            var colours=new[]{Hex("#b17cff"),Hex("#ffc34d"),Hex("#65ff9d")};
            for(int i=0;i<3;i++)
            {
                int mode=i;float y=500-i*390;
                var card=CreateLuminousPanel(modeSelectionScreen.transform,names[i]+" card",new Vector2(0,y),new Vector2(950,350),Hex("#0a1629"),colours[i]);
                var art=new GameObject("Protocol artwork",typeof(RectTransform),typeof(CanvasRenderer),typeof(SkyPulseCircuitGraphic));
                art.transform.SetParent(card,false);var ar=art.GetComponent<RectTransform>();ar.sizeDelta=new Vector2(200,260);ar.anchoredPosition=new Vector2(-355,0);
                var circuit=art.GetComponent<SkyPulseCircuitGraphic>();circuit.Variant=i;circuit.color=colours[i];circuit.raycastTarget=false;
                CreateText(card,names[i],new Vector2(95,104),new Vector2(650,62),35,colours[i],TextAnchor.MiddleLeft,FontStyle.Bold);
                CreateText(card,details[i],new Vector2(95,18),new Vector2(650,104),27,Hex("#c6dded"),TextAnchor.MiddleLeft,FontStyle.Normal);
                MissionButton(card,i==0?"LAUNCH ROUTE":"SELECT MISSION",95,-100,610,()=>{if(mode==0)StartFlight();else OpenMissionSelection((SkyPulseMode)mode);},colours[i]);
            }
            MissionButton(modeSelectionScreen.transform,"MISSION CONTROL",0,-620,900,OpenMissionControl,Hex("#ffc34d"));
            MissionButton(modeSelectionScreen.transform,"BIRD HANGAR",-235,-760,430,()=>{CloseAuxiliaryScreens();OpenHangar();});
            MissionButton(modeSelectionScreen.transform,"TECH TREE",235,-760,430,()=>{CloseAuxiliaryScreens();OpenUpgrades();});
            MissionButton(modeSelectionScreen.transform,"HOME",-300,-905,270,ResetToMenu);
            MissionButton(modeSelectionScreen.transform,"WORLD GUIDE",0,-905,270,()=>{ResetToMenu();flightGuideScreen.SetActive(true);});
            MissionButton(modeSelectionScreen.transform,"PRIVACY",300,-905,270,()=>{ResetToMenu();privacyScreen.SetActive(true);});
            MissionButton(homeScreen.transform,"MISSION CONTROL",0,-950,720,OpenMissionControl,Hex("#ffc34d"));
            missionSelectionScreen=MissionScreen("Mission selection");
            missionControlScreen=MissionScreen("Mission Control");
            blackoutScreen=MissionScreen("Blackout terminal",true);
            relayHudScreen=MissionScreen("Pulse Relay HUD",false,false);
            missionResultScreen=MissionScreen("Protocol result");
        }
        private void ClearScreen(GameObject root)
        {
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
            foreach(var screen in new[]{modeSelectionScreen,missionSelectionScreen,missionControlScreen,missionResultScreen})if(screen!=null)screen.SetActive(false);
        }
        private void ClearMissionSession()
        {
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
            homeProtocolGraphic.color=homeProtocol==SkyPulseMode.BlackoutProtocol?new Color(.2f,1f,.4f,.15f):new Color(.1f,.9f,1f,.18f);
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
            if(modeSelectionScreen!=null)
            {
                var backdrop=modeSelectionScreen.transform.Find("Mode selection backdrop");if(backdrop!=null)backdrop.GetComponent<Image>().color=homeProtocol==SkyPulseMode.BlackoutProtocol?Color.black:Hex("#030b1c");
            }
        }
        private void PrepareEndlessMode() { SetHomeProtocol(SkyPulseMode.EndlessRoute);ResetToMenu(); }
        private void OpenModeSelection()
        {
            ResetToMenu();homeScreen.SetActive(false);modeSelectionScreen.SetActive(true);CheckDailyPeriod();
        }
        private void OpenMissionSelection(SkyPulseMode mode)
        {
            SetHomeProtocol(mode);
            ResetToMenu();homeScreen.SetActive(false);activeMode=mode;
            ClearScreen(missionSelectionScreen);missionSelectionScreen.SetActive(true);
            var colour=mode==SkyPulseMode.PulseRelay?Hex("#ffc34d"):Hex("#65ff9d");
            MissionText(missionSelectionScreen.transform,mode==SkyPulseMode.PulseRelay?"PULSE RELAY":"BLACKOUT PROTOCOL",870,50,colour);
            MissionText(missionSelectionScreen.transform,"TUTORIAL + THREE MISSIONS · ALL AVAILABLE",795,25);
            for(int i=0;i<4;i++)
            {
                int index=i;float y=555-i*340;
                var card=CreateLuminousPanel(missionSelectionScreen.transform,"Mission "+i,new Vector2(0,y),new Vector2(940,310),Hex("#09162a"),colour);
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
                state=FlightState.Playing;bird.gameObject.SetActive(false);blackoutSession=new BlackoutSession(BlackoutPuzzle.All[index]);
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
            missionWorld=new GameObject("Authored Pulse circuit").transform;
            HideOriginalWorld(true);
            CircuitSprite(missionWorld,"Circuit void",Vector2.zero,new Vector2(GetViewportWidth()+1,22),Hex("#02121b"),-30);
            for(int i=0;i<19;i++)CircuitSprite(missionWorld,"Quiet circuit rail "+i,new Vector2(0,-8.5f+i),new Vector2(GetViewportWidth()+1,.015f),Hex("#07343f"),-20);
            for(int i=0;i<relaySession.Mission.RelayCount;i++)
            {
                var shutter=new GameObject("Relay "+(i+1)).transform;shutter.SetParent(missionWorld,false);relayShutters.Add(shutter);
                CircuitSprite(shutter,"Mechanical spine",Vector2.zero,new Vector2(.65f,18),Hex("#194759"),2);
                CircuitSprite(shutter,"Energy shutter",Vector2.zero,new Vector2(.22f,18),Hex("#ffc34d"),3);
                float y=relaySession.Mission.RelayY[i];
                CircuitSprite(shutter,"Activation window",new Vector2(0,y),new Vector2(.8f,3.6f),Hex("#116575"),4);
                CircuitSprite(shutter,"Window top rail",new Vector2(0,y+1.83f),new Vector2(1.2f,.07f),Hex("#45eaff"),6);
                CircuitSprite(shutter,"Window bottom rail",new Vector2(0,y-1.83f),new Vector2(1.2f,.07f),Hex("#45eaff"),6);
                for(int n=0;n<5;n++)CircuitSprite(shutter,"Mechanical bus "+n,new Vector2(0,-6f+n*3f),new Vector2(1f,.10f),Hex("#41798b"),3);
                var labelObject=new GameObject("Relay number "+(i+1),typeof(TextMesh));labelObject.transform.SetParent(shutter,false);labelObject.transform.localPosition=new Vector3(-.9f,y+2.15f,0);
                var label=labelObject.GetComponent<TextMesh>();label.text=(i+1).ToString();label.font=uiFont;label.fontSize=36;label.characterSize=.18f;label.anchor=TextAnchor.MiddleCenter;label.color=Hex("#ffc34d");
                var labelRenderer=labelObject.GetComponent<MeshRenderer>();labelRenderer.sharedMaterial=uiFont.material;labelRenderer.sortingOrder=8;
                AddRelayCell(i*2,new Vector2(relaySession.Mission.RelayX(i)-8f,y));
                AddRelayCell(i*2+1,new Vector2(relaySession.Mission.RelayX(i)-5f,Mathf.Clamp(y+3.2f,-4.5f,5.2f)));
            }
            if(relaySession.Mission.HasShield)
            {
                relayShield=new GameObject("Aegis circuit pickup").transform;relayShield.SetParent(missionWorld,false);
                var icon=CreateRenderer("Aegis",LoadSprite(PowerUpArtworkPath(PowerUpKind.Aegis)),Color.white,8,relayShield);
                icon.transform.localScale=Vector3.one*(.9f/icon.sprite.bounds.size.x);
            }
            relayExit=new GameObject("Circuit finish line").transform;relayExit.SetParent(missionWorld,false);
            CircuitSprite(relayExit,"Exit beacon",new Vector2(0,relaySession.Mission.ExitY),new Vector2(.5f,3.8f),Hex("#65ff9d"),5);
            CircuitSprite(relayExit,"Exit port",new Vector2(0,relaySession.Mission.ExitY),new Vector2(.8f,18),new Color(.1f,.6f,.3f,.15f),2);
        }
        private void AddRelayCell(int id,Vector2 at)
        {
            var cell=new GameObject("Gold charge "+id).transform;cell.SetParent(missionWorld,false);
            CircuitSprite(cell,"Charge shell",Vector2.zero,new Vector2(.46f,.64f),Hex("#ffc34d"),7);
            CircuitSprite(cell,"Charge core",Vector2.zero,new Vector2(.18f,.4f),Color.white,8);
            relayCells.Add(cell);relayCellPositions.Add(at);
        }
        private void BuildRelayHud()
        {
            ClearScreen(relayHudScreen);relayHudScreen.SetActive(true);
            var top=CreateLuminousPanel(relayHudScreen.transform,"Relay instrument panel",new Vector2(0,795),new Vector2(970,320),Hex("#061626"),Hex("#45eaff"));
            MissionText(top,"PULSE RELAY · "+relaySession.Mission.Name,95,32,Hex("#ffc34d"));
            relayStatusText=MissionText(top,"",15,29);
            relayHintText=MissionText(top,"",-70,24,Hex("#9aefff"),890,68);
            MissionButton(relayHudScreen.transform,"PAUSE",375,540,230,PauseFlight);
            pulseButton=MissionButton(relayHudScreen.transform,"PULSE · 0",230,-780,460,PressPulse,Hex("#ffc34d"));
            MissionButton(relayHudScreen.transform,"FLAP",-240,-780,430,()=>{if(state==FlightState.Playing)BufferFlapInput();});
            MissionText(relayHudScreen.transform,"GOLD CELLS = CHARGE · ALIGN BEFORE PULSING",-925,23);
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
            relayStatusText.text=$"CHARGE  {relaySession.Charges}   ·   RELAYS  {relaySession.Passed}/{relaySession.Mission.RelayCount}";
            int next=relaySession.NextRelay;float ahead=next<relaySession.Mission.RelayCount?relaySession.Mission.RelayX(next)-relaySession.Distance:0;
            relayHintText.text=next==relaySession.Mission.RelayCount?"ALL ACTIVATED · REACH THE GREEN EXIT":$"NEXT: RELAY {next+1} · "+(ahead<=6f?"ALIGN & PRESS PULSE":"COLLECT A GOLD CELL");
            pulseButton.GetComponentInChildren<Text>().text="PULSE · "+relaySession.Charges;
            pulseButton.interactable=state==FlightState.Playing && next<relaySession.Mission.RelayCount && relaySession.Charges>0 && ahead>=.6f && ahead<=6f && Mathf.Abs(birdY-relaySession.Mission.RelayY[next])<=1.55f;
            for(int i=0;i<relayShutters.Count;i++)
            {
                relayShutters[i].localPosition=new Vector3(BirdX+relaySession.Mission.RelayX(i)-relaySession.Distance,0,0);
                var window=relayShutters[i].Find("Activation window").GetComponent<SpriteRenderer>();
                window.color=relaySession.IsOpen(i)?Hex("#02121b"):Hex("#116575");
                relayShutters[i].Find("Energy shutter").GetComponent<SpriteRenderer>().color=relaySession.IsOpen(i)?Hex("#45eaff"):Hex("#ffc34d");
            }
            for(int i=0;i<relayCells.Count;i++)relayCells[i].localPosition=new Vector3(BirdX+relayCellPositions[i].x-relaySession.Distance,relayCellPositions[i].y,0);
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
        private void BuildBlackoutBoard()
        {
            ClearScreen(blackoutScreen);blackoutScreen.SetActive(true);
            var puzzle=blackoutSession.Puzzle;var green=Hex("#65ff9d");
            for(int scan=-930;scan<940;scan+=90)
            {
                var rail=CreateImage(blackoutScreen.transform,"Static terminal scanline",new Vector2(0,scan),new Vector2(1020,1),new Color(.2f,1f,.4f,.035f));rail.sprite=whiteSprite;rail.raycastTarget=false;
            }
            MissionText(blackoutScreen.transform,"BLACKOUT PROTOCOL",850,48,green);
            MissionText(blackoutScreen.transform,puzzle.Name,770,30,green);
            blackoutStatusText=MissionText(blackoutScreen.transform,$"DATA {blackoutSession.Packets}/{blackoutSession.RequiredPackets}   TRACE {blackoutSession.Trace}/{puzzle.TraceLimit}   HACKS {blackoutSession.Charges}",675,28,green);
            float stride=Mathf.Min(130,900f/puzzle.Width);float startX=-(puzzle.Width-1)*stride*.5f;
            for(int row=0;row<puzzle.Height;row++)for(int col=0;col<puzzle.Width;col++)
            {
                int tile=row*puzzle.Width+col;char type=puzzle.Tile(tile);
                bool cursor=tile==blackoutSession.Cursor,opened=blackoutSession.Opened.Contains(tile);
                string symbol=cursor?"[YOU]":type=='P'?(blackoutSession.Recovered.Contains(tile)?"[OK]":"DATA"):type=='E'?"EXIT":type=='F'?(opened?"OPEN":"FIRE"):type=='#'?"####":"+";
                Color colour=cursor?Color.white:type=='F'&&!opened?Hex("#ffa958"):green;
                var node=CreatePanel(blackoutScreen.transform,"Terminal node "+tile,new Vector2(startX+col*stride,400-row*stride),new Vector2(stride-12,stride-12),type=='#'?Hex("#08100a"):Hex("#061b0e"));
                var face=node.GetComponent<Image>();face.sprite=whiteSprite;face.type=Image.Type.Simple;
                AddOutline(node.gameObject,blackoutSession.Adjacent(tile)?colour:new Color(colour.r,colour.g,colour.b,.30f),1.5f);
                var pixelText=CreateText(node,symbol,Vector2.zero,new Vector2(stride-20,40),22,colour,TextAnchor.MiddleCenter,FontStyle.Bold);pixelText.raycastTarget=false;
                if(type=='P' || cursor)for(int pixel=0;pixel<4;pixel++)
                {
                    var dot=CreateImage(node,"Data pixel "+pixel,new Vector2(-24+pixel*16,34),new Vector2(8,8),colour);dot.sprite=whiteSprite;dot.raycastTarget=false;
                }
                var button=node.gameObject.AddComponent<Button>();button.targetGraphic=face;
                button.interactable=type!='#' && blackoutSession.Adjacent(tile);
                button.onClick.AddListener(()=>BlackoutAction(tile));
            }
            blackoutHintText=MissionText(blackoutScreen.transform,hackArmed?"HACK ARMED · TAP AN ADJACENT FIREWALL":"TAP ADJACENT NODE · MOVE +1 TRACE · HACK +3 TRACE",-330,24,green);
            MissionText(blackoutScreen.transform,"DATA FIRST · EXIT LAST · CHARGES OPEN FIREWALLS",-415,23,green);
            var traceBack=CreatePanel(blackoutScreen.transform,"Trace meter",new Vector2(0,-505),new Vector2(880,28),Hex("#12321d"));
            CreatePanel(traceBack,"Trace level",new Vector2(-440+(880f*blackoutSession.Trace/puzzle.TraceLimit)*.5f,0),new Vector2(Mathf.Min(880,880f*blackoutSession.Trace/puzzle.TraceLimit),22),green);
            hackButton=MissionButton(blackoutScreen.transform,hackArmed?"CANCEL HACK":"ARM HACK",-235,-645,430,()=>{hackArmed=!hackArmed;BuildBlackoutBoard();},green);
            hackButton.interactable=blackoutSession.Charges>0;
            MissionButton(blackoutScreen.transform,"PAUSE",235,-645,430,PauseFlight,green);
            MissionText(blackoutScreen.transform,"Silver: finish with 2 spare trace. Gold: meet mission par "+puzzle.Par+".",-785,22,green);
            MissionButton(blackoutScreen.transform,"RESTART PUZZLE",0,-895,900,()=>LaunchMission(SkyPulseMode.BlackoutProtocol,selectedMission),green);
        }
        private void BlackoutAction(int tile)
        {
            if(activeMode!=SkyPulseMode.BlackoutProtocol || state!=FlightState.Playing || blackoutSession==null)return;
            if(!blackoutSession.Act(tile,hackArmed))return;
            hackArmed=false;Play(scoreSound);
            if(blackoutSession.State==MissionSessionState.Succeeded || blackoutSession.State==MissionSessionState.Failed)FinalizeMission();
            else BuildBlackoutBoard();
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
                try { missionProgress.Complete(activeMode,selectedMission,medal); }
                catch(Exception e) { Debug.LogWarning("SkyPulse medal save failed: "+e.GetType().Name);reason+=" · SAVE FAILED"; }
                if(activeMode==SkyPulseMode.BlackoutProtocol && selectedMission==0)RecordDailyEvent(DailyGameplayEvent.IntroBlackoutCompleted);
            }
            state=FlightState.GameOver;bird.gameObject.SetActive(false);if(birdBodyCollider!=null)birdBodyCollider.enabled=false;
            relayHudScreen.SetActive(false);blackoutScreen.SetActive(false);pauseScreen.SetActive(false);
            ClearScreen(missionResultScreen);missionResultScreen.SetActive(true);missionResultScreen.transform.SetAsLastSibling();
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
                int index=i;var card=CreateLuminousPanel(missionControlScreen.transform,"Daily task "+SkyPulseDailyMissions.Ids[i],new Vector2(0,535-i*350),new Vector2(940,310),Hex("#0b182a"),Hex("#45eaff"));
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
