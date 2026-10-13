// Install only in a disposable QA project's Assets/Editor.
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
        public Dictionary<string,string> Strings=new Dictionary<string,string>();public Dictionary<string,int> Ints=new Dictionary<string,int>();public bool Fail;
        public string Get(string k,string d)=>Strings.TryGetValue(k,out var v)?v:d;
        public int GetInt(string k,int d)=>Ints.TryGetValue(k,out int v)?v:d;
        public void Set(string k,string v)=>Strings[k]=v;
        public void SetInt(string k,int v)=>Ints[k]=v;
        public void Flush(){if(Fail)throw new System.IO.IOException("Injected campaign persistence failure");}
    }
    public static void Pure()
    {
        int lastCost=-1,lastArea=0,lastCores=0,lastSlack=int.MaxValue;
        for(int i=0;i<8;i++)
        {
            var p=SkyPulseBlackoutCampaign.Stages[i];var path=SkyPulseMissionChecks.Solve(p);Check(path!=null,"Authored sector "+(i+1)+" is solvable");
            var s=new BlackoutSession(p);foreach(var a in path)Check(s.Act(a.Item1,a.Item2),"Solver action accepted by runtime session");
            Check(s.State==MissionSessionState.Succeeded && s.Medal==3,"Runtime sector solution succeeds with a gold medal");
            Check(s.Trace==SkyPulseBlackoutCampaign.MinimumTrace[i] && s.Trace>lastCost,"Sector minimum trace increases strictly: "+s.Trace);
            Check(p.Width*p.Height>=lastArea && s.RequiredPackets>=lastCores && p.TraceLimit-s.Trace<=lastSlack,"Network size/core count do not decrease, spare trace does not increase");
            Check(!p.HasPhaseRouting || s.SwitchesUsed>0,"Best phase-route solution actually uses a console");
            Check((s.CipherKeys&p.CipherKeyMask)==p.CipherKeyMask,"Best route recovers every authored cipher key");
            lastCost=s.Trace;lastArea=p.Width*p.Height;lastCores=s.RequiredPackets;lastSlack=p.TraceLimit-s.Trace;
        }
        var store=new Store();store.Set(SkyPulseModeProgress.BlackoutKey,"{\"version\":1,\"medals\":[3,2,0,0]}");store.SetInt(SkyPulseDailyMissions.WalletKey,321);
        const string oldCampaign="{\"version\":1,\"medals\":[3,2,0,0,0,0,0,0],\"bestTrace\":[7,12,0,0,0,0,0,0]}";
        store.Set("skypulse.blackout-campaign.v1",oldCampaign);
        var progress=new BlackoutCampaignProgress(store);Check(progress.CanPlay(0) && !progress.CanPlay(1),"Fresh campaign opens only sector one");
        progress.Complete(3,3,1);Check(progress.Unlocked==0,"Cannot skip locked sectors through completion calls");
        store.Fail=true;try{progress.Complete(0,3,7);}catch(System.IO.IOException){}
        Check(progress.Unlocked==0 && progress.Data.medals[0]==0,"Failed flush cannot expose an unsaved next-sector unlock");store.Fail=false;
        for(int i=0;i<8;i++){progress.Complete(i,3,SkyPulseBlackoutCampaign.MinimumTrace[i]);progress=new BlackoutCampaignProgress(store);Check(progress.CanPlay(Math.Min(7,i+1)),"Next sector unlock survives reload");}
        Check(store.Get(SkyPulseModeProgress.BlackoutKey,"")=="{\"version\":1,\"medals\":[3,2,0,0]}" && store.GetInt(SkyPulseDailyMissions.WalletKey,0)==321,"Build 9 medals and wallet remain unchanged");
        Check(store.Get("skypulse.blackout-campaign.v1","")==oldCampaign,"Earlier local campaign medals stay attached to their original layouts");
        var trap=new BlackoutSession(new BlackoutPuzzle("trap-test","","",new[]{"STPE"},4,1,4));
        Check(!trap.Act(1,true) && trap.Trace==0 && trap.Charges==1,"Cannot accidentally hack a security node");
        Check(trap.Act(1,false) && trap.Trace==2 && trap.Charges==1,"Security node costs exactly two trace");
        Check(trap.Act(2,false) && trap.Act(3,false) && trap.State==MissionSessionState.Succeeded && trap.Trace==4,"Security-node route succeeds at the exact budget boundary");
        var locked=new BlackoutSession(new BlackoutPuzzle("key-lock-test","","",new[]{"SLKPE"},20,2,20));
        Check(!locked.Act(1,false) && !locked.Act(1,true) && locked.Trace==0 && locked.Charges==2,"Missing key blocks movement and breach without spending trace or charges");
        var cipher=new BlackoutSession(new BlackoutPuzzle("two-key-test","","",new[]{"SKLklPE"},6,2,6));
        cipher.Pause(true);Check(!cipher.Act(1,false) && cipher.CipherKeys==0,"Pause cannot recover a key");cipher.Pause(false);
        Check(cipher.Act(1,false) && cipher.CipherKeys==1 && cipher.Powered(2) && !cipher.Powered(4),"Ice key unlocks only its own locks");
        Check(cipher.Act(2,false) && cipher.Act(3,false) && cipher.CipherKeys==3 && cipher.Act(4,false),"Violet key opens its distinct cipher channel");
        Check(cipher.Act(5,false) && cipher.Act(6,false) && cipher.State==MissionSessionState.Succeeded && cipher.Trace==6,"Keys and locks do not add hidden trace costs");
        var phase=new BlackoutSession(new BlackoutPuzzle("phase-test","","",new[]{"SCBP","..AE"},12,1,12));
        Check(phase.CircuitPhase==0 && phase.Powered(6) && !phase.Powered(2),"A new session starts on the mint circuit");
        Check(phase.Act(1,false) && phase.CircuitPhase==1 && phase.SwitchesUsed==1 && phase.Trace==1,"Entering the console toggles the circuit exactly once at normal move cost");
        Check(phase.Act(2,false) && !phase.Act(6,false) && !phase.Act(6,true) && phase.Trace==2 && phase.Cursor==2 && phase.Charges==1,"Inactive mint gate rejects move and breach without a turn");
        Check(phase.Act(1,false) && phase.CircuitPhase==0 && phase.SwitchesUsed==2 && !phase.Powered(2),"Returning to a console changes the route again");
        var wrongMove=new BlackoutSession(new BlackoutPuzzle("strict-move-test","","",new[]{"SBPE"},2,2,2,true));
        Check(wrongMove.Act(1,false) && wrongMove.LastActionFailed && wrongMove.Trace==1 && wrongMove.Cursor==0 && wrongMove.Charges==2,"Wrong movement spends exactly one trace without a free advance");
        wrongMove.Pause(true);Check(!wrongMove.Act(1,true) && wrongMove.Trace==1 && wrongMove.Charges==2,"Paused mistakes cannot burn resources");wrongMove.Pause(false);
        Check(wrongMove.Act(1,false) && wrongMove.Trace==2 && wrongMove.State==MissionSessionState.Failed,"The last trace causes failure immediately when extraction is unfinished");
        Check(!wrongMove.Act(1,true) && wrongMove.Trace==2 && wrongMove.Charges==2,"A failed run rejects any further resource spending");
        var wrongBreach=new BlackoutSession(new BlackoutPuzzle("strict-breach-test","","",new[]{"SLPE"},3,1,3,true));
        Check(wrongBreach.Act(1,true) && wrongBreach.Trace==3 && wrongBreach.Charges==0 && wrongBreach.Cursor==0 && wrongBreach.Opened.Count==0 && wrongBreach.State==MissionSessionState.Failed,"Failed breach spends one charge and three trace, leaves the lock closed and can lose the run");
        var emptyCharge=new BlackoutSession(new BlackoutPuzzle("strict-empty-charge-test","","",new[]{"SFPE"},3,0,3,true));
        Check(emptyCharge.Act(1,true) && emptyCharge.Trace==3 && emptyCharge.Charges==0 && emptyCharge.State==MissionSessionState.Failed,"An exhausted-charge attempt still spends trace and cannot create a negative charge balance");
        var boundary=new BlackoutSession(new BlackoutPuzzle("strict-exact-finish-test","","",new[]{"SKLPE"},4,1,4,true));
        for(int tile=1;tile<5;tile++)Check(boundary.Act(tile,false),"Exact-boundary route action accepted");
        Check(boundary.Trace==4 && boundary.State==MissionSessionState.Succeeded,"Successful extraction is allowed on the last available trace");
        Debug.Log("SKYPULSE_BLACKOUT_PURE_PASS");
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        game.enabled=false;Set(game,"reduceMotionEnabled",false);Call(game,"ResetToMenu");
        var store=new Store();Set(game,"blackoutCampaign",new BlackoutCampaignProgress(store));
        int wallet=(int)Get(game,"crystals"),best=(int)Get(game,"best");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,1);Check(Get(game,"blackoutSession")==null,"Locked sector cannot launch");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);
        var s=(BlackoutSession)Get(game,"blackoutSession");var controls=(List<Button>)Get(game,"blackoutNodeButtons");var ids=controls.Select(b=>b.GetEntityId()).ToArray();
        Call(game,"TouchBlackoutNode",1);Call(game,"TouchBlackoutNode",2);
        Check(s.Trace==2 && s.Cursor==2,"Deliberate successive moves respond during transport effects");
        Call(game,"PauseFlight");Call(game,"UpdateProtocolPresentation",0f);
        var network=(SkyPulseBlackoutGraphic)Get(game,"blackoutNetwork");float travel=network.Travel,phase=network.Phase;
        yield return new WaitForSecondsRealtime(.35f);Call(game,"UpdateProtocolPresentation",.35f);
        Check(network.Travel==travel && network.Phase==phase && s.Trace==2,"Pause freezes transport, ambient phase and trace");
        Call(game,"ResumeFlight");Call(game,"UpdateProtocolPresentation",.35f);Check(network.Travel==1,"Resume finishes transport with elapsed time");
        var campaign=(BlackoutCampaignProgress)Get(game,"blackoutCampaign");campaign.Complete(0,3,7);campaign.Complete(1,3,12);
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,2);s=(BlackoutSession)Get(game,"blackoutSession");
        Call(game,"TouchBlackoutNode",1);Call(game,"TouchBlackoutNode",2);
        Check(s.Trace==4 && s.Charges==0 && s.Cursor==1 && s.Opened.Contains(2),"One firewall tap breaches once for one charge and three trace without moving");
        Call(game,"TouchBlackoutNode",2);Check(s.Cursor==2 && s.Trace==5,"Second firewall tap enters the opened node");
        s=new BlackoutSession(new BlackoutPuzzle("live-resource-failure","RESOURCE LAB","",new[]{"SFPE"},3,0,3,true));Set(game,"blackoutSession",s);Call(game,"BuildBlackoutLab");
        Call(game,"TouchBlackoutNode",1);
        Check(Get(game,"state").ToString()=="GameOver" && s.State==MissionSessionState.Failed && s.Charges==0 && ((GameObject)Get(game,"missionResultScreen")).activeSelf,"An exhausted breach causes the real failure flow");
        var retry=((GameObject)Get(game,"missionResultScreen")).GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>().text.StartsWith("RETRY SECTOR"));retry.onClick.Invoke();
        s=(BlackoutSession)Get(game,"blackoutSession");Check(s.Trace==0 && s.CipherKeys==0 && s.Charges==s.Puzzle.HackCharges,"Retry resets trace, keys and charges");
        Call(game,"ResetToMenu");Set(game,"blackoutCampaign",new BlackoutCampaignProgress(new Store()));Set(game,"reduceMotionEnabled",true);
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);
        for(int sector=0;sector<8;sector++)
        {
            s=(BlackoutSession)Get(game,"blackoutSession");Check((int)Get(game,"selectedMission")==sector,"Next harder sector is already active");
            foreach(var a in SkyPulseMissionChecks.Solve(s.Puzzle))Call(game,"TouchBlackoutNode",a.Item1);
            Check(s.State==MissionSessionState.Succeeded,"Live one-thumb route completes sector "+sector);
            campaign=(BlackoutCampaignProgress)Get(game,"blackoutCampaign");string saved=JsonUtility.ToJson(campaign.Data);Call(game,"FinalizeMission");
            Check(JsonUtility.ToJson(campaign.Data)==saved && (int)Get(game,"crystals")==wallet && (int)Get(game,"best")==best,"Duplicate finalization cannot award or save again");
            Check(ids.SequenceEqual(controls.Select(b=>b.GetEntityId())) && controls.Count==49,"Sector progression reuses all 49 board controls");
            if(sector<7)Check(Get(game,"state").ToString()=="Playing" && !((GameObject)Get(game,"missionResultScreen")).activeSelf && ((BlackoutSession)Get(game,"blackoutSession")).Trace==0,"Intermediate clear needs no modal, button or animation delay");
        }
        Check(Get(game,"state").ToString()=="GameOver" && ((GameObject)Get(game,"missionResultScreen")).activeSelf,"Final sector uses the proper completion result");
        Check(((GameObject)Get(game,"missionResultScreen")).GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>().text=="REPLAY CAMPAIGN"),"Final result can replay the campaign");
        Call(game,"ResetToMenu");yield return null;Check(Get(game,"blackoutSession")==null && ((GameObject)Get(game,"homeScreen")).activeSelf,"Navigation clears the session without pending completion callbacks");
        Call(game,"OpenMissionSelection",SkyPulseMode.BlackoutProtocol);Call(game,"OpenMissionSelection",SkyPulseMode.PulseRelay);
        Check(!((GameObject)Get(game,"missionSelectionScreen")).GetComponentsInChildren<SkyPulseBlackoutGraphic>().Any(g=>g.Surface==BlackoutSurface.Backdrop && g.gameObject.activeInHierarchy),"Blackout backdrop cannot leak into Relay selection");
        Call(game,"ResetToMenu");Call(game,"InitializeMissionServices");Set(game,"reduceMotionEnabled",false);
        Debug.Log("SKYPULSE_BLACKOUT_LIVE_PASS");
    }
}
