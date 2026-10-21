// Editor-only heist regressions. Copied into a disposable QA project; never ships.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseBlackoutChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static void Check(bool ok,string note){if(!ok)throw new Exception(note);Debug.Log("BLACKOUT_CHECK: "+note);}
    sealed class Store:IMissionStore
    {
        public readonly Dictionary<string,string> Strings=new Dictionary<string,string>();public readonly Dictionary<string,int> Ints=new Dictionary<string,int>();public bool Fail;
        public string Get(string k,string d)=>Strings.TryGetValue(k,out var v)?v:d;
        public int GetInt(string k,int d)=>Ints.TryGetValue(k,out int v)?v:d;
        public void Set(string k,string v)=>Strings[k]=v;
        public void SetInt(string k,int v)=>Ints[k]=v;
        public void Flush(){if(Fail)throw new System.IO.IOException("Injected campaign persistence failure");}
    }
    public static void Pure()
    {
        int previous=0;
        foreach(var p in SkyPulseBlackoutCampaign.Stages)
        {
            var path=SkyPulseMissionChecks.Solve(p);Check(path!=null,"Exhaustive orientation search finds a solution: "+p.Name);
            var s=new BlackoutSession(p);Check(s.State==MissionSessionState.Playing,"Vault never starts completed");
            Check(!s.Rotate(-1)&&!s.Rotate(p.Count)&&!s.Rotate(p.Start)&&s.Trace==0,"Only rotatable optics consume a turn");
            s.Pause(true);Check(!s.Rotate(path[0]),"Paused prism rejects input");s.Pause(false);
            foreach(int tile in path)Check(s.Rotate(tile),"Solution rotation accepted");
            Check(s.State==MissionSessionState.Succeeded&&s.Medal==3&&s.CoresPowered==p.CoreCount&&s.UplinkPowered,"All live cores and uplink are required for gold extraction");
            Check(s.Trace==p.Par&&p.Par>previous,"Minimum rotation/trace cost increases strictly: "+p.Par);previous=p.Par;
            int trace=s.Trace;Check(!s.Rotate(path[0])&&s.Trace==trace,"Completed session ignores duplicate input");
            var disconnected=new BlackoutSession(p);Array.Copy(s.Rotations,disconnected.Rotations,p.Count);disconnected.Recalculate();
            if(p.Rows.Any(row=>row.Contains('G')))
            {
                Check((disconnected.RelayMask&1)!=0,"Powered relay opens matching shutter in the same calculation");
                int entry=disconnected.Beams.Take(disconnected.BeamCount).Select(b=>b.To).First(p.Rotatable);
                disconnected.Rotations[entry]=(disconnected.Rotations[entry]+1)%p.Orientations(entry);disconnected.Recalculate();
                Check(disconnected.RelayMask==0,"Disconnected relays do not leave shutters latched open");
            }
        }
        var first=SkyPulseBlackoutCampaign.Stages[0];
        var exact=new BlackoutSession(new BlackoutPuzzle("exact","","",first.Rows,first.Initial,2,2));foreach(int t in SkyPulseMissionChecks.Solve(first))exact.Rotate(t);
        Check(exact.State==MissionSessionState.Succeeded&&exact.Trace==2,"Extraction succeeds on the exact last trace");
        var failed=new BlackoutSession(first);while(failed.State==MissionSessionState.Playing)failed.Rotate(6);
        Check(failed.State==MissionSessionState.Failed&&failed.Trace==8&&!failed.Rotate(14),"Unfinished optics exhaust the budget and cannot act after failure");
        var alarm=new BlackoutSession(new BlackoutPuzzle("sensor","","",new[]{"##!#","S.M#","##C#","##ME"},first.Initial,8,2));
        Check(alarm.Alarms==1&&alarm.Rotate(14)&&alarm.LastCost==3&&alarm.Trace==3,"A lit sensor charges one turn plus two trace");
        Check(alarm.Rotate(6)&&alarm.LastCost==1&&alarm.State==MissionSessionState.Succeeded,"Disconnecting a sensor immediately restores normal cost");
        var loop=new BlackoutSession(new BlackoutPuzzle("loop","","",new[]{"S.YM","C.M.","##MM"},new[]{0,0,0,1,0,0,0,0,0,0,1,0},20,4));
        int count=loop.BeamCount;for(int i=0;i<100;i++)loop.Recalculate();Check(count==loop.BeamCount&&count<200,"A cyclic split beam terminates deterministically within fixed buffers");
        var store=new Store();const string legacy="{\"version\":2,\"medals\":[3,3,3,3,3,3,3,3]}";store.Set("skypulse.blackout-campaign.v2",legacy);store.SetInt(SkyPulseDailyMissions.WalletKey,321);
        var migrated=new BlackoutCampaignProgress(store);Check(migrated.CanPlay(7)&&migrated.Data.medals.All(m=>m==0),"Legacy achievements retain access without falsely completing new puzzles");
        var progress=new BlackoutCampaignProgress(new Store());Check(progress.CanPlay(0)&&!progress.CanPlay(1),"Fresh player starts with vault one");
        progress.Complete(3,3,1);Check(progress.Unlocked==0,"Locked vault cannot be skipped by a completion call");
        progress=new BlackoutCampaignProgress(store);var attempt=progress.Start(0,true);attempt.Rotate(14);progress.SaveAttempt(0,attempt);
        var resumed=new BlackoutCampaignProgress(store).Start(0);Check(resumed.Trace==1&&resumed.Rotations.SequenceEqual(attempt.Rotations),"Unfinished prism configuration and trace survive relaunch");
        while(attempt.State==MissionSessionState.Playing)attempt.Rotate(14);progress.SaveAttempt(0,attempt);
        Check(new BlackoutCampaignProgress(store).Start(0).Trace==0,"Reopening an exhausted attempt starts a playable retry");
        store.Fail=true;try{progress.Complete(0,3,2);}catch(System.IO.IOException){}Check(progress.Data.medals[0]==0,"Failed flush cannot publish an unlock in memory");store.Fail=false;
        for(int i=0;i<8;i++){progress.Complete(i,3,SkyPulseBlackoutCampaign.MinimumTrace[i]);progress=new BlackoutCampaignProgress(store);Check(progress.CanPlay(Math.Min(7,i+1)),"New vault completion survives reload");}
        Check(store.Get("skypulse.blackout-campaign.v2","")==legacy&&store.GetInt(SkyPulseDailyMissions.WalletKey,0)==321,"Legacy save and crystal economy remain untouched");
        var calculate=(Action)Delegate.CreateDelegate(typeof(Action),loop,typeof(BlackoutSession).GetMethod("Recalculate"));for(int i=0;i<3;i++)calculate();long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<500;i++)calculate();
        Check(GC.GetAllocatedBytesForCurrentThread()==before,"Warmed beam calculation uses fixed buffers without managed allocations");
        Debug.Log("SKYPULSE_BLACKOUT_PURE_PASS");
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        game.enabled=false;Set(game,"reduceMotionEnabled",false);Call(game,"ResetToMenu");Set(game,"blackoutCampaign",new BlackoutCampaignProgress(new Store()));
        int wallet=(int)Get(game,"crystals"),best=(int)Get(game,"best");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,1);Check(Get(game,"blackoutSession")==null,"Locked vault cannot launch");Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);
        var s=(BlackoutSession)Get(game,"blackoutSession");var controls=(List<Button>)Get(game,"blackoutNodeButtons");var ids=controls.Select(b=>b.GetEntityId()).ToArray();
        Call(game,"TouchBlackoutNode",14);Check(s.Trace==1&&s.Rotations[14]==1,"Prism responds immediately without waiting for animation");
        Call(game,"PauseFlight");Call(game,"UpdateProtocolPresentation",0f);var network=(SkyPulseHeistBoard)Get(game,"blackoutBoard");float phase=network.Phase;network.Reveal=.5f;
        yield return new WaitForSecondsRealtime(.35f);Call(game,"TouchBlackoutNode",6);Check(network.Phase==phase&&network.Reveal==.5f&&s.Trace==1,"Pause freezes animation and rejects puzzle taps");
        Call(game,"ResumeFlight");Call(game,"RestartBlackoutVault");s=(BlackoutSession)Get(game,"blackoutSession");Check(s.Trace==0&&controls[6].interactable,"Restart clears attempt and restores touch interaction");
        while(s.State==MissionSessionState.Playing)Call(game,"TouchBlackoutNode",6);
        Check(Get(game,"state").ToString()=="GameOver"&&((GameObject)Get(game,"missionResultScreen")).activeSelf,"Trace exhaustion uses proper failure result");
        ((GameObject)Get(game,"missionResultScreen")).GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="RETRY VAULT").onClick.Invoke();
        Check(((BlackoutSession)Get(game,"blackoutSession")).Trace==0&&controls[6].interactable,"Failure retry is immediately playable");
        Call(game,"ResetToMenu");Set(game,"blackoutCampaign",new BlackoutCampaignProgress(new Store()));Set(game,"reduceMotionEnabled",true);Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);
        for(int vault=0;vault<8;vault++)
        {
            s=(BlackoutSession)Get(game,"blackoutSession");Check((int)Get(game,"selectedMission")==vault,"Next harder vault is already active");
            foreach(int t in SkyPulseMissionChecks.Solve(s.Puzzle))Call(game,"TouchBlackoutNode",t);
            Check(s.State==MissionSessionState.Succeeded,"Live routing completes vault "+vault);var campaign=(BlackoutCampaignProgress)Get(game,"blackoutCampaign");string saved=JsonUtility.ToJson(campaign.Data);Call(game,"FinalizeMission");
            Check(JsonUtility.ToJson(campaign.Data)==saved&&(int)Get(game,"crystals")==wallet&&(int)Get(game,"best")==best,"Duplicate completion cannot award or save twice");
            Check(controls.Count==49&&ids.SequenceEqual(controls.Select(b=>b.GetEntityId())),"Progression reuses every pooled optical socket");
            if(vault<7)Check(Get(game,"state").ToString()=="Playing"&&!((GameObject)Get(game,"missionResultScreen")).activeSelf&&((BlackoutSession)Get(game,"blackoutSession")).Trace==0,"Intermediate extraction has no modal, countdown or NEXT tap");
        }
        Check(Get(game,"state").ToString()=="GameOver"&&((GameObject)Get(game,"missionResultScreen")).GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>().text=="REPLAY HEIST"),"Final vault opens the proper final completion flow");
        Call(game,"ResetToMenu");yield return null;Check(Get(game,"blackoutSession")==null&&((GameObject)Get(game,"homeScreen")).activeSelf,"Navigation clears the live circuit and any boundary input");
        Call(game,"OpenMissionSelection",SkyPulseMode.BlackoutProtocol);Call(game,"OpenMissionSelection",SkyPulseMode.PulseRelay);
        Check(!((GameObject)Get(game,"missionSelectionScreen")).GetComponentsInChildren<SkyPulseBlackoutGraphic>().Any(g=>g.Surface==BlackoutSurface.Backdrop&&g.gameObject.activeInHierarchy),"Heist chamber cannot leak into Relay selection");
        Call(game,"ResetToMenu");Call(game,"InitializeMissionServices");Set(game,"reduceMotionEnabled",false);Debug.Log("SKYPULSE_BLACKOUT_LIVE_PASS");
    }
    public static IEnumerator Captures(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        game.enabled=false;Call(game,"ResetToMenu");Set(game,"reduceMotionEnabled",false);var campaign=new BlackoutCampaignProgress(new Store());
        for(int i=0;i<8;i++)campaign.Complete(i,3,SkyPulseBlackoutCampaign.MinimumTrace[i]);Set(game,"blackoutCampaign",campaign);
        var camera=(Camera)Get(game,"flightCamera");var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        string folder=System.IO.Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"signal-heist");System.IO.Directory.CreateDirectory(folder);
        var rt=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=rt;Call(game,"RefreshViewportDecor");
        for(int vault=0;vault<8;vault++)
        {
            Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,vault);Call(game,"RestartBlackoutVault");var session=(BlackoutSession)Get(game,"blackoutSession");var path=SkyPulseMissionChecks.Solve(session.Puzzle);
            // Configure a real reachable mid-puzzle position; stop before extraction.
            var previewPath=path.Skip(path.Count-1).Concat(path.Take(path.Count-1)).ToArray();
            for(int i=0;i<Math.Max(0,previewPath.Length-2)&&session.State==MissionSessionState.Playing;i++)Call(game,"TouchBlackoutNode",previewPath[i]);
            Call(game,"UpdateProtocolPresentation",0f);yield return new WaitForSecondsRealtime(.2f);
            Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Call(game,"FitBlackoutToSafeArea");Canvas.ForceUpdateCanvases();Save(camera,rt,System.IO.Path.Combine(folder,"vault-"+(vault+1).ToString("00")+".png"));
        }
        // Two complete, genuine examples with immediate progression. Rotate the
        // entry first so the viewer sees the beam changing during each turn.
        foreach(int vault in new[]{0,2})
        {
            Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,vault);Call(game,"RestartBlackoutVault");var session=(BlackoutSession)Get(game,"blackoutSession");var path=SkyPulseMissionChecks.Solve(session.Puzzle);
            int entry=path[path.Count-1];var sequence=path.Where(t=>t==entry).Concat(path.Where(t=>t!=entry)).ToArray();
            string clip=System.IO.Path.Combine(folder,vault==0?"first-light":"split-signal");System.IO.Directory.CreateDirectory(clip);int turn=0;
            for(int frame=0;frame<90;frame++)
            {
                if(frame>=15&&(frame-15)%10==0&&turn<sequence.Length)Call(game,"TouchBlackoutNode",sequence[turn++]);
                Call(game,"UpdateProtocolPresentation",.1f);yield return new WaitForSecondsRealtime(.1f);
                Canvas.ForceUpdateCanvases();Call(game,"FitBlackoutToSafeArea");Save(camera,rt,System.IO.Path.Combine(clip,frame.ToString("000")+".png"));
            }
            Check(session.State==MissionSessionState.Succeeded,"Preview uses real prism turns and successful extraction");
        }
        // Screen points (pixels / device pixel ratio) remain at least 44pt on
        // the narrowest supported portrait phone. This is layout, not device FPS.
        camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);
        foreach(var size in new[]{new Vector2Int(640,1136),new Vector2Int(1320,2868),new Vector2Int(1640,2360)})
        {
            rt=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=rt;Call(game,"RefreshViewportDecor");Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,7);Call(game,"RestartBlackoutVault");
            yield return null;Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Call(game,"FitBlackoutToSafeArea");Canvas.ForceUpdateCanvases();
            foreach(var button in ((GameObject)Get(game,"blackoutScreen")).GetComponentsInChildren<Button>())
            {
                if(!button.interactable)continue;var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                var a=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);float ratio=size.x==640?2:size.x==1320?3:2;
                Check(Mathf.Min(b.x-a.x,b.y-a.y)/ratio>=44f,"Active puzzle touch target is >=44 points at "+size.x+": "+button.name+" size="+(b-a)+" ratio="+ratio+" safe="+((RectTransform)Get(game,"safeAreaRoot")).rect+" scale="+button.transform.lossyScale);
            }
            Save(camera,rt,System.IO.Path.Combine(folder,size.x+"-seven-wide.png"));camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);
        }
        Call(game,"InitializeMissionServices");Call(game,"ResetToMenu");Debug.Log("SKYPULSE_HEIST_CAPTURE_PASS");
    }
    static void Save(Camera camera,RenderTexture rt,string file)
    {
        foreach(var board in UnityEngine.Object.FindObjectsByType<SkyPulseHeistBoard>(FindObjectsSortMode.None))board.Render();
        camera.Render();RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();System.IO.File.WriteAllBytes(file,image.EncodeToPNG());UnityEngine.Object.Destroy(image);RenderTexture.active=null;
    }

}
