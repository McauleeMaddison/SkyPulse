using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

// Editor-only harness copied into a separate QA identity by run_modes_qa.py.
public static class SkyPulseDailyRouteChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static void Check(bool ok,string note){if(!ok)throw new Exception(note);Debug.Log("DAILY_ROUTE_CHECK: "+note);}
    sealed class Store:IMissionStore
    {
        public readonly Dictionary<string,string> Text=new Dictionary<string,string>();
        public readonly Dictionary<string,int> Ints=new Dictionary<string,int>();
        public int Flushes;public bool Fail;
        public string Get(string k,string d)=>Text.TryGetValue(k,out var v)?v:d;
        public int GetInt(string k,int d)=>Ints.TryGetValue(k,out var v)?v:d;
        public void Set(string k,string v)=>Text[k]=v;
        public void SetInt(string k,int v)=>Ints[k]=v;
        public void Flush(){if(Fail)throw new System.IO.IOException("Injected daily save failure");Flushes++;}
    }
    static string Fingerprint(RelayMission[] missions)=>string.Join("|",missions.Select(m=>m.Id+":"+m.Spacing.ToString("R")+":"+m.ExitY.ToString("R")+":"+string.Join(",",m.RelayY.Select(y=>y.ToString("R")))));
    public static void Pure()
    {
        var now=new DateTime(2026,10,13,23,59,59,DateTimeKind.Utc);var store=new Store();store.SetInt(SkyPulseDailyMissions.WalletKey,777);store.Set(SkyPulseModeProgress.RelayKey,"legacy medals");
        var day=SkyPulseDailyMissions.Day(now);var daily=new SkyPulseDailyRelay(store,()=>now);string layout=Fingerprint(daily.Missions);
        daily.Complete(day,0,3);int flush=store.Flushes;daily.Complete(day,0,3);
        Check(store.Flushes==flush&&daily.Data.medals[0]==3,"Repeated daily completion saves once");
        daily=new SkyPulseDailyRelay(store,()=>now);Check(Fingerprint(daily.Missions)==layout&&daily.Data.medals[0]==3,"Same-day restart preserves routes and medals");
        now=now.AddSeconds(1);Check(daily.EnsurePeriod()&&daily.Data.day==day+1&&daily.Data.medals.All(m=>m==0)&&Fingerprint(daily.Missions)!=layout,"UTC midnight creates a fresh four-circuit deck and daily progress");
        daily.Complete(day,1,2);Check(daily.Data.medals.All(m=>m==0)&&store.Get(SkyPulseDailyRelay.LastRunKey,"").Contains(day.ToString()),"Cross-midnight run is recorded without clearing another day's circuit");
        now=now.AddDays(-2);Check(!daily.EnsurePeriod()&&daily.Data.day==day+1,"Clock rollback cannot reroll or reopen old daily progress");
        now=now.AddDays(3);store.Fail=true;bool failed=false;try{daily.EnsurePeriod();}catch(System.IO.IOException){failed=true;}
        Check(failed&&daily.Data.day==day+1,"Failed refresh persistence keeps the current playable deck");store.Fail=false;daily.EnsurePeriod();
        Check(store.GetInt(SkyPulseDailyMissions.WalletKey,0)==777&&store.Get(SkyPulseModeProgress.RelayKey,"")=="legacy medals","Daily medals never debit wallet or rewrite legacy circuit medals");
        string previous="";
        for(int d=0;d<366;d++)
        {
            var deck=SkyPulseDailyRelay.Generate(day+d);string fingerprint=Fingerprint(deck);
            Check(deck.Select(m=>m.Name.Substring(5)).Distinct().Count()==4,"Daily circuit names are distinct");
            Check(deck.Length==4&&fingerprint!=previous&&fingerprint==Fingerprint(SkyPulseDailyRelay.Generate(day+d)),"Daily deck changes deterministically: "+d);previous=fingerprint;
            for(int i=0;i<4;i++)
            {
                var m=deck[i];Check(m.RelayCount==i+2&&m.RelayY.All(y=>Math.Abs(y)<=1.65001f)&&m.Spacing>=18&&m.Spacing<=24&&Math.Abs(m.ExitY)<=.70001f,"Generated course respects tested thumb reach and cadence");
                for(int g=1;g<m.RelayCount;g++)Check(Math.Abs(m.RelayY[g]-m.RelayY[g-1])<=1.90001f,"Generated approach never jumps beyond reach bound");
                var s=new RelaySession(m);for(int g=0;g<m.RelayCount;g++){s.Collect(g*2);s.Advance(m.RelayX(g)+1-s.Distance,m.RelayY[g]);}s.Advance(m.ExitX+1-s.Distance,m.ExitY);Check(s.State==MissionSessionState.Succeeded,"Every generated deck has valid gate/exit progression");
            }
        }
        Debug.Log("SKYPULSE_DAILY_ROUTE_PURE_PASS");
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        game.enabled=false;Call(game,"InitializeMissionServices");Call(game,"ResetToMenu");
        var daily=(SkyPulseDailyRelay)Get(game,"dailyRelay");long day=daily.Data.day;
        Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);var relay=(RelaySession)Get(game,"relaySession");
        // Complete through the real session/course dispatch; the actual flap
        // pilot and pickup/collision paths remain covered at 30/60/120 FPS.
        for(int i=0;i<relay.Mission.RelayCount;i++)relay.Collect(i*2);relay.Advance(relay.Mission.ExitX+1,relay.Mission.ExitY);Call(game,"FinalizeMission");
        var result=(GameObject)Get(game,"missionResultScreen");yield return null;Capture(game,"relay-next-result");
        var next=result.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="NEXT MISSION");next.onClick.Invoke();
        Check(Get(game,"state").ToString()=="Playing"&&(int)Get(game,"selectedMission")==1&&((RelaySession)Get(game,"relaySession")).Mission.Day==day,"Next Mission immediately starts the next daily circuit without Mission Control");
        Call(game,"FinishRelayFailure","QA INTERRUPT");result=(GameObject)Get(game,"missionResultScreen");result.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="MAIN MENU").onClick.Invoke();
        Check(Get(game,"state").ToString()=="Menu"&&Get(game,"homeProtocol").ToString()=="PulseRelay","Relay result Main Menu restores its selected Home");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);var s=(BlackoutSession)Get(game,"blackoutSession");s.Act(1,false);
        Set(game,"blackoutSession",new BlackoutSession(new BlackoutPuzzle("qa-fail","QA","",new[]{"SFPE"},3,0,3,true)));Call(game,"BuildBlackoutLab");Call(game,"TouchBlackoutNode",1);result=(GameObject)Get(game,"missionResultScreen");
        yield return null;Capture(game,"blackout-result");
        result.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="MAIN MENU").onClick.Invoke();Check(Get(game,"state").ToString()=="Menu"&&Get(game,"homeProtocol").ToString()=="BlackoutProtocol","Blackout result Main Menu restores its selected Home");
        Call(game,"StartFlight");Set(game,"state",Enum.Parse(Get(game,"state").GetType(),"GameOver"));Call(game,"RefreshScreens");var endless=(GameObject)Get(game,"gameOverScreen");yield return null;Capture(game,"endless-result");
        endless.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="MAIN MENU").onClick.Invoke();
        Check(Get(game,"state").ToString()=="Menu"&&Get(game,"activeMode").ToString()=="EndlessRoute","Endless result has a direct Main Menu action");
        Call(game,"LaunchMission",SkyPulseMode.PulseRelay,3);relay=(RelaySession)Get(game,"relaySession");for(int i=0;i<relay.Mission.RelayCount;i++)relay.Collect(i*2);relay.Advance(relay.Mission.ExitX+1,relay.Mission.ExitY);Call(game,"FinalizeMission");
        Check(!((GameObject)Get(game,"missionResultScreen")).GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>().text=="NEXT MISSION"),"Final daily circuit does not offer an invalid fifth mission");
        Call(game,"ResetToMenu");yield return null;
        Debug.Log("SKYPULSE_DAILY_ROUTE_LIVE_PASS");
    }
    static void Capture(SkyPulseNativeGame game,string name)
    {
        var camera=(Camera)Get(game,"flightCamera");var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();
        var rt=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        Call(game,"RefreshViewportDecor");Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();camera.Render();
        var active=(GameObject)Get(game,name=="endless-result"?"gameOverScreen":"missionResultScreen");
        foreach(var t in active.GetComponentsInChildren<Text>())if(!t.resizeTextForBestFit)Check(t.preferredHeight<=t.rectTransform.rect.height+2,"Result text fits: "+t.text);
        var old=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(750,1334,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,750,1334),0,0);texture.Apply();
        var folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"results");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);RenderTexture.active=old;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);
    }

}
