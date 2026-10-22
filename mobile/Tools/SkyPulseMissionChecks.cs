using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using SkyPulse.Mobile;

// Editor-only harness copied into a disposable project. Never touches the real player save.
public static class SkyPulseMissionChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("MISSION_CHECK: "+message);}
    sealed class Store : IMissionStore
    {
        public Dictionary<string,string> Working=new Dictionary<string,string>(), Durable=new Dictionary<string,string>();
        public int FlushCount,FailAt=-1;public bool After;
        public string Get(string key,string fallback)=>Working.TryGetValue(key,out var v)?v:fallback;
        public void Set(string key,string value)=>Working[key]=value;
        public int GetInt(string key,int fallback)=>int.TryParse(Get(key,""),out var v)?v:fallback;
        public void SetInt(string key,int value)=>Set(key,value.ToString());
        public void Flush(){FlushCount++;if(FlushCount==FailAt && !After)throw new IOException("Injected pre-flush interruption");Durable=new Dictionary<string,string>(Working);if(FlushCount==FailAt && After)throw new IOException("Injected post-flush interruption");}
        public Store Restart()=>new Store{Working=new Dictionary<string,string>(Durable),Durable=new Dictionary<string,string>(Durable)};
    }
    static DateTime date=new DateTime(2026,10,12,12,0,0,DateTimeKind.Utc);
    public static void PureChecks()
    {
        var store=new Store();store.SetInt(SkyPulseDailyMissions.WalletKey,100);store.Flush();
        var daily=new SkyPulseDailyMissions(store,()=>date);
        daily.Progress(DailyGameplayEvent.EndlessGate,4);daily.Progress(DailyGameplayEvent.EndlessGate,6);
        Check(daily.Data.progress[0]==10,"Daily progress aggregates across attempts");
        for(int i=0;i<4;i++){daily.Progress((DailyGameplayEvent)i,100);Check(daily.Claim(i),"Completed task can be claimed");Check(!daily.Claim(i),"Double-click claim is rejected");}
        Check(store.GetInt(SkyPulseDailyMissions.WalletKey,0)==120 && daily.ClaimedTotal==20,"Exactly five each and maximum twenty per period");
        Check(daily.Data.progress[3]==10,"Rewards do not recursively emit collection events");
        date=date.AddDays(-1);daily.EnsurePeriod();Check(daily.ClaimedTotal==20 && !daily.Claim(0),"Backward clock cannot reopen claimed day");
        date=date.AddDays(2);daily.EnsurePeriod();Check(daily.ClaimedTotal==0 && daily.Data.progress.All(p=>p==0),"Forward UTC rollover resets tasks once");
        var reopened=new SkyPulseDailyMissions(store.Restart(),()=>date);Check(reopened.Data.day==daily.Data.day,"Reopening does not reroll day or tasks");
        for(int phase=1;phase<=5;phase++)foreach(bool after in new[]{false,true})
        {
            var s=new Store();s.SetInt(SkyPulseDailyMissions.WalletKey,70);s.Flush();var d=new SkyPulseDailyMissions(s,()=>date);d.Progress(DailyGameplayEvent.EndlessGate,10);
            s.FlushCount=0;s.FailAt=phase;s.After=after;
            try{d.Claim(0);}catch(IOException){}
            var restart=s.Restart();var recovered=new SkyPulseDailyMissions(restart,()=>date);
            if(!recovered.Data.claimed[0])recovered.Claim(0);
            Check(restart.GetInt(SkyPulseDailyMissions.WalletKey,0)==75 && recovered.Data.claimed[0] && !recovered.Claim(0),"Interrupted claim recovers once: phase "+phase+" after="+after);
        }
        foreach(var mission in RelayMission.All)
        {
            var relay=new RelaySession(mission);Check(!relay.IsOpen(0),"Gate begins closed");
            Check(relay.Collect(0) && !relay.IsOpen(0) && relay.Charges==1,"First matching crystal is insufficient");
            Check(relay.Collect(1) && relay.IsOpen(0) && relay.Charges==0 && relay.NextRelay==1,"Second matching crystal opens immediately and spends exactly two charges");
            Check(!relay.Collect(0),"Charge cell cannot collect twice");
            relay.Advance(mission.RelayX(0)+1,mission.RelayY[0]);
            for(int i=1;i<mission.RelayCount;i++){Check(relay.Collect(i*2) && !relay.IsOpen(i) && relay.Collect(i*2+1) && relay.IsOpen(i),"Next gate opens only from its own complete pair");relay.Advance(mission.RelayX(i)+1-relay.Distance,mission.RelayY[i]);}
            relay.Advance(mission.ExitX+1-relay.Distance,mission.ExitY);Check(relay.State==MissionSessionState.Succeeded,"Every relay mission has a complete objective route");int cells=relay.Cells;relay.Collect(999);relay.Advance(20,0);Check(relay.Cells==cells,"Finalized relay ignores repeated events");
        }
        var missed=new RelaySession(RelayMission.All[0]);missed.Advance(14,0);Check(missed.State==MissionSessionState.Failed,"Missing required relay produces failure");
        var hit=new RelaySession(RelayMission.All[0]);hit.Hazard(false);Check(hit.State==MissionSessionState.Failed,"Unprotected hazard fails");
        Debug.Log("SKYPULSE_MISSION_PURE_PASS");
    }
    public static float RelayTarget(SkyPulseNativeGame game)
    {
        var s=(RelaySession)Get(game,"relaySession");var used=(HashSet<int>)Get(game,"usedRelayCells");var positions=(List<Vector2>)Get(game,"relayCellPositions");
        for(int i=s.Passed*2;i<Math.Min(positions.Count,s.Passed*2+2);i++)if(!used.Contains(i) && positions[i].x>=s.Distance-1.0f)return positions[i].y;
        return s.Passed<s.Mission.RelayCount?s.Mission.RelayY[s.Passed]:s.Mission.ExitY;
    }
    static readonly Dictionary<BlackoutPuzzle,List<int>> solutions=new Dictionary<BlackoutPuzzle,List<int>>();
    // QA-only exhaustive orientation search. No solver or answer table ships.
    public static List<int> Solve(BlackoutPuzzle puzzle)
    {
        if(solutions.TryGetValue(puzzle,out var cached))return cached;
        var tiles=Enumerable.Range(0,puzzle.Count).Where(puzzle.Rotatable).ToArray();
        int combinations=1;foreach(int t in tiles)combinations*=puzzle.Orientations(t);
        int best=int.MaxValue;int[] goal=null;var probe=new BlackoutSession(puzzle);
        for(int state=0;state<combinations;state++)
        {
            int code=state,cost=0;
            foreach(int t in tiles){int n=puzzle.Orientations(t);int r=code%n;code/=n;probe.Rotations[t]=r;cost+=(r-puzzle.Initial[t]+n)%n;}
            if(cost>=best)continue;probe.Recalculate();
            if(probe.CoresPowered==puzzle.CoreCount&&probe.UplinkPowered){best=cost;goal=(int[])probe.Rotations.Clone();}
        }
        if(goal==null)return null;
        var initial=new BlackoutSession(puzzle);int entry=-1;
        for(int i=0;i<initial.BeamCount;i++){int tile=initial.Beams[i].To;if(puzzle.Rotatable(tile)){entry=tile;break;}}
        var path=new List<int>();
        // Configure downstream optics with the entry diverted, then connect the
        // source last. This also tests that the runtime never latches dark relays.
        foreach(int t in tiles.Where(t=>t!=entry).Concat(new[]{entry}))
        {
            if(t<0)continue;int n=puzzle.Orientations(t),turns=(goal[t]-puzzle.Initial[t]+n)%n;
            for(int i=0;i<turns;i++)path.Add(t);
        }
        solutions[puzzle]=path;return path;
    }
    public static void IntegrationChecks(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006" || Application.productName!="SkyPulseQA20261006")throw new Exception("Disposable QA project required");
        game.enabled=false;Set(game,"hapticsEnabled",false);((AudioSource)Get(game,"audioSource")).mute=true;
        int originalBest=(int)Get(game,"best"), wallet=(int)Get(game,"crystals");
        for(int mission=0;mission<RelayMission.All.Length;mission++)
        {
            Call(game,"LaunchMission",SkyPulseMode.PulseRelay,mission);
            var relay=(RelaySession)Get(game,"relaySession");
            for(int step=0;step<20000 && relay.State==MissionSessionState.Playing;step++)
            {
                int gate=Math.Min(relay.Passed,relay.Mission.RelayCount-1);
                float target=RelayTarget(game);
                float y=(float)Get(game,"birdY"),v=(float)Get(game,"birdVelocity");
                if(y<target-.65f && v<0)Call(game,"Flap");
                Call(game,"SimulateRelay",1f/120f);
            }
            Check(relay.State==MissionSessionState.Succeeded,"Real flight forces and authored course can complete relay mission "+mission+": "+relay.Result);
            Call(game,"FinalizeMission");Check((int)Get(game,"best")==originalBest && (int)Get(game,"crystals")==wallet,"Relay result does not overwrite Endless score or award wallet bonus");
        }
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);
        float birdY=(float)Get(game,"birdY");var blackout=(BlackoutSession)Get(game,"blackoutSession");
        for(int i=0;i<50;i++)Call(game,"Update");Check((float)Get(game,"birdY")==birdY && blackout.Trace==0,"Puzzle never simulates flight or processes background taps");
        Call(game,"OnApplicationPause",true);Check(blackout.State==MissionSessionState.Paused,"Focus-loss pause freezes puzzle");
        Call(game,"OnApplicationPause",false);Check(blackout.State==MissionSessionState.Paused,"Returning from background waits for resume");Call(game,"ResumeFlight");
        foreach(var action in Solve(blackout.Puzzle)){Call(game,"TouchBlackoutNode",action);}
        Call(game,"FinalizeMission");Check((int)Get(game,"best")==originalBest && (int)Get(game,"crystals")==wallet,"Puzzle result does not change Endless score or wallet");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);
        var medalFault=new Store { FailAt=3 };
        Set(game,"blackoutCampaign",new BlackoutCampaignProgress(medalFault));
        blackout=(BlackoutSession)Get(game,"blackoutSession");
        foreach(var action in Solve(blackout.Puzzle)){Call(game,"TouchBlackoutNode",action);}
        Check(Get(game,"state").ToString()=="GameOver" && ((GameObject)Get(game,"missionResultScreen")).activeSelf && ((GameObject)Get(game,"missionResultScreen")).GetComponentsInChildren<Text>().Any(t=>t.text.Contains("SAVE FAILED")),"Medal-save interruption still shows a completed result and a truthful save failure");
        Call(game,"InitializeMissionServices");
        Call(game,"OpenMissionControl");var daily=(SkyPulseDailyMissions)Get(game,"dailyMissions");
        int run=(int)Get(game,"runCrystalsCollected"), bonus=(int)Get(game,"runCrystalBonus");int collected=daily.Data.progress[3];
        daily.Progress(DailyGameplayEvent.EndlessGate,10);Call(game,"ClaimDaily",0);Call(game,"ClaimDaily",0);
        Check((int)Get(game,"crystals")==wallet+5 && (int)Get(game,"runCrystalsCollected")==run && (int)Get(game,"runCrystalBonus")==bonus && daily.Data.progress[3]==collected,"Daily UI claim grants five once without run totals, bonus or collection progress");
        Call(game,"OpenUpgrades");
        var faultStore=new Store();faultStore.SetInt(SkyPulseDailyMissions.WalletKey,1000);faultStore.Flush();
        var interrupted=new SkyPulseDailyMissions(faultStore,()=>date);interrupted.Progress(DailyGameplayEvent.EndlessGate,10);
        Set(game,"missionStore",faultStore);Set(game,"dailyMissions",interrupted);Set(game,"crystals",1000);
        faultStore.FlushCount=0;faultStore.FailAt=4;
        try{Call(game,"CreditWallet",5,WalletRewardSource.DailyMission,0);}catch(TargetInvocationException e){if(!(e.InnerException is IOException))throw;}
        faultStore.FailAt=-1;
        ((IDictionary)Get(game,"upgradeLevels")).Clear();((HashSet<string>)Get(game,"ownedUpgradeIds")).Clear();
        var upgrade=((Array)typeof(SkyPulseNativeGame).GetField("Upgrades",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)).GetValue(0);
        Call(game,"SelectUpgrade",upgrade);Call(game,"ConfirmPurchase");
        Check(interrupted.Data.pending<0 && (int)Get(game,"crystals")==855 && (int)Call(game,"GetUpgradeLevel",Get(upgrade,"Id"))==1,"Pending daily journal settles before a subsequent upgrade debit");
        Call(game,"InitializeMissionServices");
        Call(game,"StartFlight");Check(Get(game,"activeMode").ToString()=="EndlessRoute" && Get(game,"relaySession")==null && Get(game,"blackoutSession")==null && Get(game,"missionWorld")==null,"Switching to Endless clears prior session and course");
        Call(game,"ResetToMenu");
        var skins=(Array)typeof(SkyPulseNativeGame).GetField("Skins",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);var paths=new HashSet<string>();
        foreach(var skin in skins){foreach(string path in (string[])Get(skin,"FlapFramePaths"))Check(paths.Add(path),"Unique bird flight resource");Check(paths.Add((string)Get(skin,"HitPath")),"Unique bird hit pose");Check(paths.Add((string)Get(skin,"UnlockPath")),"Unique bird unlock pose");}
        Check(skins.Length==15 && paths.Count==120,"Exactly fifteen birds and 120 unique frame paths");
        var tech=(Array)typeof(SkyPulseNativeGame).GetField("Upgrades",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        var ids=new HashSet<string>();foreach(var node in tech)Check(ids.Add((string)Get(node,"Id")) && (int)node.GetType().GetProperty("MaxLevel").GetValue(node)==3,"Stable unique three-level Tech Tree node");
        Check(tech.Length==9,"Exactly nine Tech Tree nodes");
        foreach(var node in tech){string prerequisite=(string)Get(node,"PrerequisiteId");Check(string.IsNullOrEmpty(prerequisite)||ids.Contains(prerequisite),"Tech prerequisite resolves to a stable node");}
        ((HashSet<string>)Get(game,"ownedSkinIds")).Clear();((IDictionary)Get(game,"upgradeLevels")).Clear();((HashSet<string>)Get(game,"ownedUpgradeIds")).Clear();
        foreach(var node in tech)PlayerPrefs.DeleteKey("skypulse.native.upgrade."+Get(node,"Id"));
        PlayerPrefs.SetInt("skypulse.native.best",88);PlayerPrefs.SetInt("skypulse.native.crystals",321);PlayerPrefs.SetInt("skypulse.native.farthest-world",8);
        PlayerPrefs.SetString("skypulse.native.skin","newbird10");PlayerPrefs.SetString("skypulse.native.owned-skins","volt,newbird10");
        PlayerPrefs.SetInt("skypulse.native.cyber-roster-v2",0);PlayerPrefs.SetString("skypulse.native.owned-upgrades","crystal_resonator_2,salvage_codec_3");PlayerPrefs.Save();
        Call(game,"LoadProgress");Call(game,"SaveProgress");
        Check((int)Get(game,"best")==88 && (int)Get(game,"crystals")==321 && (int)Get(game,"farthestWorldIndex")==8,"Legacy score, crystals and world progress survive new services and save");
        Check(((HashSet<string>)Get(game,"ownedSkinIds")).Contains("neon_finch") && ((HashSet<string>)Get(game,"ownedSkinIds")).Contains("newbird10") && (string)Get(Get(game,"equippedSkin"),"Id")=="newbird10","Legacy bird alias migrates without losing equipped newbird10");
        Check((int)Call(game,"GetUpgradeLevel","crystal_resonator")==2 && (int)Call(game,"GetUpgradeLevel","salvage_codec")==3,"Legacy level aliases retain their upgrade effects");
        Call(game,"ClosePurchaseModal");Call(game,"SelectUpgrade",tech.GetValue(2));Call(game,"ConfirmPurchase");
        Check((int)Get(game,"crystals")==321 && (int)Call(game,"GetUpgradeLevel",Get(tech.GetValue(2),"Id"))==0,"Locked prerequisite cannot spend wallet crystals");
        Call(game,"SetHomeProtocol",SkyPulseMode.BlackoutProtocol);Call(game,"ResetToMenu");Call(game,"InitializeMissionServices");Call(game,"ApplyHomeProtocolTheme");
        Check(Get(game,"homeProtocol").ToString()=="EndlessRoute" && ((Text)Get(game,"difficultyText")).text=="ENDLESS ROUTE","New launch defaults to Endless while retaining player progression");
        Call(game,"SetHomeProtocol",SkyPulseMode.BlackoutProtocol);Call(game,"ApplyHomeProtocolTheme");
        Call(game,"OpenModeSelection");Call(game,"UpdateProtocolPresentation",.25f);
        Check(((GameObject)Get(game,"homeScreen")).activeSelf && ((RectTransform)Get(game,"modeDropdownClip")).rect.height==688f,"Comfort-sized mode list expands beneath badge while Home remains visible");
        Call(game,"SelectHomeProtocol",SkyPulseMode.PulseRelay);
        Check(!((GameObject)Get(game,"modeSelectionScreen")).activeSelf && Get(game,"homeProtocol").ToString()=="PulseRelay" && Get(game,"state").ToString()=="Menu","Selecting dropdown item changes Home without launching gameplay");
        Call(game,"LaunchHomeProtocol");Check(Get(game,"state").ToString()=="Playing" && Get(game,"activeMode").ToString()=="PulseRelay","Home Play launches the next Relay circuit directly");
        Call(game,"ResetToMenu");Call(game,"SetHomeProtocol",SkyPulseMode.BlackoutProtocol);
        Set(game,"reduceMotionEnabled",true);Call(game,"UpdateMissionModes",.02f);
        var circuit=(SkyPulseCircuitGraphic)Get(game,"homeProtocolGraphic");Set(circuit,"phase",.37f);Call(circuit,"Update");
        Check(circuit.ReducedMotion && (float)Get(circuit,"phase")==.37f,"Reduced Motion freezes Home circuit animation");
        Set(game,"reduceMotionEnabled",false);
        Debug.Log("SKYPULSE_MISSION_LEGACY_PASS");
        Debug.Log("SKYPULSE_MISSION_INTEGRATION_PASS");
    }
    public static IEnumerator Captures(SkyPulseNativeGame game)
    {
        Call(game,"InitializeMissionServices");
        var captureCampaign=(BlackoutCampaignProgress)Get(game,"blackoutCampaign");
        for(int i=0;i<8;i++)captureCampaign.Complete(i,3,SkyPulseBlackoutCampaign.MinimumTrace[i]);
        string folder=Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");var camera=(Camera)Get(game,"flightCamera");
        var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        foreach(var size in new[]{new Vector2Int(750,1334),new Vector2Int(1320,2868),new Vector2Int(1640,2360)})
        {
            var rt=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);Call(game,"RefreshViewportDecor");
            Rect expectedViewport=camera.rect;float expectedCameraWidth=2f*camera.orthographicSize*camera.aspect;float expectedContentWidth=((RectTransform)Get(game,"interfaceContentRoot")).rect.width;
            foreach(string screen in new[]{"mode-selection","relay-selection","relay-flight","blackout-selection","blackout-puzzle","mission-control","home-endless","home-relay","home-blackout"})
            {
                if(screen.StartsWith("home-"))
                {
                    Call(game,"SetHomeProtocol",screen=="home-relay"?SkyPulseMode.PulseRelay:screen=="home-blackout"?SkyPulseMode.BlackoutProtocol:SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");
                    Check(((Image)Get(game,"homeProtocolBackdrop")).gameObject.activeSelf==(screen!="home-endless"),"Home theme follows selected protocol");
                    Check(((RectTransform)Get(game,"menuHeroTransform")).gameObject.activeSelf==(screen!="home-blackout"),"Blackout uses data core instead of bird hero");
                    Check(((SkyPulseDepthStage)Get(game,"homeDepth")).Mode==(screen=="home-relay"?SkyPulseMode.PulseRelay:screen=="home-blackout"?SkyPulseMode.BlackoutProtocol:SkyPulseMode.EndlessRoute),"Each Home uses its own real 3D stage");
                    if(screen=="home-blackout")
                    {
                        var protocol=(Text)Get(game,"difficultyText");
                        Check(protocol.preferredWidth<=protocol.rectTransform.rect.width+2f,"Blackout Home protocol label fits its badge");
                    }
                }
                if(screen=="mode-selection"){Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"OpenModeSelection");Call(game,"UpdateProtocolPresentation",.25f);}
                if(screen=="relay-selection")Call(game,"OpenMissionSelection",SkyPulseMode.PulseRelay);
                if(screen=="relay-flight")
                {
                    Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);
                    var relay=(RelaySession)Get(game,"relaySession");
                    while(relay.Distance<8f && relay.State==MissionSessionState.Playing)
                    {
                        if((float)Get(game,"birdY")<-.65f && (float)Get(game,"birdVelocity")<0)Call(game,"Flap");
                        Call(game,"SimulateRelay",1f/120f);
                    }
                    Call(game,"UpdateGameplayWingState",.08f);Call(game,"UpdateBirdWingMotion");Call(game,"UpdateRelayPresentation");
                }
                if(screen=="blackout-selection")Call(game,"OpenMissionSelection",SkyPulseMode.BlackoutProtocol);
                if(screen=="blackout-puzzle")
                {
                    Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,1);
                    var puzzle=(BlackoutSession)Get(game,"blackoutSession");
                    foreach(var action in Solve(puzzle.Puzzle).Take(2)){Call(game,"TouchBlackoutNode",action);}
                }
                if(screen=="mission-control")Call(game,"OpenMissionControl");
                yield return null;Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();foreach(var stage in UnityEngine.Object.FindObjectsByType<SkyPulseHeistBoard>(FindObjectsSortMode.None))stage.Render();foreach(var depth in UnityEngine.Object.FindObjectsByType<SkyPulseDepthStage>(FindObjectsSortMode.None))depth.Render();camera.Render();
                Check(camera.rect==expectedViewport && Mathf.Abs(2f*camera.orthographicSize*camera.aspect-expectedCameraWidth)<.001f,"Shared Endless camera width: "+screen+" at "+size.x);
                Check(((RectTransform)Get(game,"interfaceContentRoot")).rect.width==expectedContentWidth,"Shared Endless interface width: "+screen);
                if(screen=="home-relay" || screen=="home-blackout")
                {
                    var backdrop=((Image)Get(game,"homeProtocolBackdrop")).rectTransform;var content=(RectTransform)Get(game,"interfaceContentRoot");
                    Check(Mathf.Abs(backdrop.rect.width*content.localScale.x-((RectTransform)canvas.transform).rect.width)<2f,"Home backdrop fills same canvas width as Endless");
                }
                var screens=new[]{"modeSelectionScreen","missionSelectionScreen","blackoutScreen","missionControlScreen","relayHudScreen"};
                foreach(string field in screens)
                {
                    var panel=(GameObject)Get(game,field);if(!panel.activeSelf)continue;
                    foreach(var text in panel.GetComponentsInChildren<Text>())
                        if(!text.resizeTextForBestFit)Check(text.preferredHeight<=text.rectTransform.rect.height+2f,"Text fits "+screen+": "+text.text);
                }
                RenderTexture.active=rt;var texture=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,size.x,size.y),0,0);texture.Apply();
                File.WriteAllBytes(Path.Combine(folder,size.x+"-"+screen+".png"),texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);RenderTexture.active=null;
            }
            camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);
        }
        Call(game,"ResetToMenu");Debug.Log("SKYPULSE_MISSION_CAPTURES_PASS");
    }
}
