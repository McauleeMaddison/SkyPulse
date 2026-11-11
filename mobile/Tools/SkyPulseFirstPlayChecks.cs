// Optional first-play teaching QA, run only inside the disposable Unity product.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseFirstPlayChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    const string Enabled="skypulse.first-play.tips.v1",Endless="skypulse.first-play.endless.v1",Relay="skypulse.first-play.relay.v1",Heist="skypulse.first-play.heist.v1";
    static object Get(object o,string name)=>o.GetType().GetField(name,F).GetValue(o);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,F).SetValue(o,value);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethods(F).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(o,args);
    static void Check(bool pass,string note){if(!pass)throw new Exception(note);Debug.Log("FIRST_PLAY_CHECK: "+note);}
    sealed class Store:IMissionStore
    {
        readonly Dictionary<string,int> numbers=new Dictionary<string,int>();readonly Dictionary<string,string> text=new Dictionary<string,string>();
        public int Writes,Flushes;
        public string Get(string k,string d)=>text.TryGetValue(k,out var v)?v:d;
        public int GetInt(string k,int d)=>numbers.TryGetValue(k,out int v)?v:d;
        public void Set(string k,string v){text[k]=v;Writes++;}public void SetInt(string k,int v){numbers[k]=v;Writes++;}public void Flush(){Flushes++;}
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable first-play QA required");
        game.enabled=false;Call(game,"ResetToMenu");var oldStore=Get(game,"missionStore");int originalWallet=(int)Get(game,"crystals");
        var store=new Store();store.SetInt(SkyPulseDailyMissions.WalletKey,originalWallet);store.Writes=0;Set(game,"missionStore",store);Set(game,"hapticsEnabled",false);
        try
        {
            Call(game,"StartFlight");Call(game,"UpdateFirstPlayGuidance",0f);
            var coach=(Text)Get(game,"hudCoachText");Check(coach.gameObject.activeSelf&&coach.text.Contains("TAP TO FLAP")&&!coach.raycastTarget,"First real Endless run exposes a readable, touch-transparent tap instruction");
            Check(store.GetInt(Endless,0)==0&&store.Writes==0,"Showing instructions cannot mark a mode learned or write player progress");
            float before=(float)Get(game,"firstPlaySeconds");Call(game,"PauseFlight");Call(game,"UpdateFirstPlayGuidance",5f);
            Check((float)Get(game,"firstPlaySeconds")==before&&!coach.gameObject.activeSelf,"Pause hides the coach and preserves its reading time");Call(game,"ResumeFlight");
            Call(game,"Flap");Call(game,"UpdateFirstPlayGuidance",1f/60);
            Check(coach.text.Contains("OPENING")&&store.GetInt(Endless,0)==0,"An accepted flap advances teaching without pretending the gate was cleared");
            Set(game,"score",1);Call(game,"UpdateFirstPlayGuidance",1f/60);int writes=store.Writes;
            Check(store.GetInt(Endless,0)==1&&!coach.gameObject.activeSelf,"Actual flap plus authoritative passed-gate count completes and dismisses Endless teaching");
            for(int i=0;i<20;i++)Call(game,"UpdateFirstPlayGuidance",1f/60);
            Check(store.Writes==writes,"Repeated completion observations cannot award or write teaching completion twice");
            Call(game,"RestartFlight");Call(game,"UpdateFirstPlayGuidance",1f/60);Check(!coach.gameObject.activeSelf,"Learned Endless tips stay hidden on retry");
            Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);Call(game,"UpdateFirstPlayGuidance",0f);
            var hint=(Text)Get(game,"relayHintText");Check(hint.text.Contains("TAP THE FLIGHT AREA")&&!hint.raycastTarget,"Relay begins with the existing transparent header hint, not an extra gameplay modal");
            Call(game,"Flap");Call(game,"UpdateFirstPlayGuidance",0f);Check(hint.text.Contains("BOTH CRYSTALS"),"Relay teaches its required matching pair after the first accepted flap");
            var relay=(RelaySession)Get(game,"relaySession");relay.Collect(0);Call(game,"UpdateFirstPlayGuidance",0f);
            Check(hint.text.Contains("ONE MORE")&&!relay.IsOpen(0),"The first assigned crystal produces correct remaining-charge guidance");
            relay.Collect(1);Call(game,"UpdateFirstPlayGuidance",0f);Check(hint.text.Contains("FLY THROUGH")&&relay.IsOpen(0),"A logically open gate immediately changes the instruction to passage");
            relay.Move(relay.Mission.RelayX(0)+.9f);relay.ResolveCourse(relay.Mission.RelayY[0],false);Call(game,"UpdateFirstPlayGuidance",0f);
            Check(store.GetInt(Relay,0)==1&&store.GetInt(Heist,0)==0,"A real first-gate passage completes only the Relay teaching key");
            Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);Call(game,"UpdateFirstPlayGuidance",0f);
            var briefing=(Text)Get(game,"blackoutBriefing");var session=(BlackoutSession)Get(game,"blackoutSession");
            Check(briefing.text.Contains("silver prism")&&!briefing.raycastTarget,"Blackout teaching sits in the existing objective briefing without covering the board");
            Call(game,"ToggleBlackoutPreview");int tile=Enumerable.Range(0,session.Puzzle.Count).First(session.Puzzle.Rotatable);
            Call(game,"PreviewBlackoutNode",tile);Call(game,"UpdateFirstPlayGuidance",0f);
            Check(!(bool)Get(game,"firstPlayTurned")&&session.Turns==0&&store.GetInt(Heist,0)==0,"PLAN and speculative rotations cannot teach a move, spend live trace or complete guidance");Call(game,"CancelBlackoutPreview");
            foreach(int move in SkyPulseMissionChecks.Solve(session.Puzzle))Call(game,"TouchBlackoutNode",move);
            Check(store.GetInt(Heist,0)==1&&(int)Get(game,"selectedMission")==1&&Get(game,"blackoutSession")!=session,"Success is recorded before immediate next-puzzle activation; no tutorial NEXT modal is introduced");
            Call(game,"UpdateFirstPlayGuidance",0f);Check(!(bool)Get(game,"firstPlayActive"),"Held callbacks from the cleared puzzle cannot re-arm tips on its successor");
            Call(game,"SetFirstPlayTipsEnabled",false);Check(store.GetInt(Enabled,1)==0,"Player can opt out without altering any learned-mode key");
            Check(store.GetInt(Endless,0)==1&&store.GetInt(Relay,0)==1&&store.GetInt(Heist,0)==1,"Opting out preserves all demonstrated completions");
            Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");Call(game,"OpenModeGuide");yield return null;Canvas.ForceUpdateCanvases();
            var guide=(GameObject)Get(game,"modeGuideScreen");var replay=guide.transform.Find("Guide · play with tips").GetComponent<Button>();var toggle=guide.transform.Find("Guide · optional play tips").GetComponent<Button>();
            Check(replay!=null&&toggle!=null&&((RectTransform)replay.transform).rect.height>=176&&((RectTransform)toggle.transform).rect.height>=176,"Guide owns generous explicit replay and optional-tip controls");
            int wallet=(int)Get(game,"crystals");replay.onClick.Invoke();Call(game,"UpdateFirstPlayGuidance",0f);
            Check(Get(game,"state").ToString()=="Playing"&&coach.gameObject.activeSelf&&(int)Get(game,"crystals")==wallet,"PLAY WITH TIPS starts an explicitly requested normal run without buying anything");
            Check(store.GetInt(Endless,0)==1,"Replaying teaching does not erase a previously earned completion");
            int replayWrites=store.Writes;Set(game,"score",3);replay.onClick.Invoke();toggle.onClick.Invoke();
            Check((int)Get(game,"score")==3&&store.Writes==replayWrites,"A queued guide replay callback cannot restart the new run or rewrite preferences");
            Set(game,"score",0);
            for(int frame=0;frame<300;frame++)Call(game,"UpdateFirstPlayGuidance",.1f);
            Check(!coach.gameObject.activeSelf&&store.GetInt(Endless,0)==1,"Quiet time budget dismisses tips without changing earned progress");
        }
        finally
        {
            Call(game,"ResetToMenu");Set(game,"missionStore",oldStore);Set(game,"crystals",originalWallet);Call(game,"ReadFirstPlayPreferences");
        }
        Debug.Log("SKYPULSE_FIRST_PLAY_PASS");
    }
}
