// Disposable Editor QA only. Observe presentation from the real mode sessions;
// this helper never belongs in Assets/Scripts or a shipping player.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseModeFeedbackChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);Debug.Log("MODE_FEEDBACK_CHECK: "+text);}
    sealed class Store:IMissionStore
    {
        readonly Dictionary<string,string> values=new Dictionary<string,string>();
        public string Get(string key,string fallback)=>values.TryGetValue(key,out var value)?value:fallback;
        public int GetInt(string key,int fallback)=>fallback;
        public void Set(string key,string value)=>values[key]=value;
        public void SetInt(string key,int value){}
        public void Flush(){}
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable batch QA required");
        game.enabled=false;Set(game,"hapticsEnabled",false);Set(game,"reduceMotionEnabled",false);
        Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);yield return null;
        var relay=(RelaySession)Get(game,"relaySession");var hardware=(List<SkyPulseRelayGate>)Get(game,"relayPortalHardware");var gate=hardware[0];
        var labels=(List<TextMesh>)Get(game,"relayGateLabels");var lasers=(List<SpriteRenderer>)Get(game,"relayLasers");
        int children=gate.transform.childCount;var passage=gate.PassageSurface;
        Check(passage!=null&&!passage.enabled,"Each Relay gate starts with one inactive pooled passage wave");
        Check(gate.GetComponentsInChildren<Collider>(true).Length==0&&gate.GetComponentsInChildren<Collider2D>(true).Length==0,"Passage graphics add no competing colliders");
        var mesh=passage.GetComponent<MeshFilter>().sharedMesh;
        Check(mesh.vertexCount==384&&mesh.bounds.size.z>3&&passage.sharedMaterial.shader.isSupported,"Passage wave is a bounded real-depth mesh with a supported existing shader");
        Check(gate.ChargeFraction==0&&!relay.IsOpen(0),"Uncharged visual still corresponds to a logically closed gate");
        relay.Collect(0);Call(game,"UpdateRelayPresentation");
        Check(gate.ChargeFraction==.5f&&!relay.IsOpen(0)&&labels[0].text.Contains("1/2"),"First assigned crystal shows a partial circuit and clear 1/2 label");
        relay.Collect(1);Call(game,"UpdateRelayPresentation");
        Check(gate.ChargeFraction==1&&relay.IsOpen(0)&&labels[0].text.Contains("OPEN")&&lasers[0].color.b>lasers[0].color.r,"Second assigned crystal opens logically before the visual release and turns its laser blue");
        Check(!passage.enabled,"Unlocking alone cannot falsely trigger a passed-gate wave");
        relay.Move(relay.Mission.RelayX(0)+.9f);relay.ResolveCourse(relay.Mission.RelayY[0],false);Call(game,"UpdateRelayPresentation");
        Check(relay.Passed==1&&passage.enabled&&labels[0].text.Contains("CLEARED"),"An actual resolved passage triggers one wave and marks the gate cleared");
        foreach(int fps in new[]{30,60,120})
        {
            gate.SetPassage(false,0,false);gate.SetPassage(true,1,false);
            for(int frame=1;frame<=fps/5;frame++)gate.SetPassage(true,1+frame/(float)fps,false);
            var scale=passage.transform.localScale;float progress=gate.PassageProgress;
            Check(passage.enabled&&scale.x>1&&scale.x<1.18f,"Passage expands smoothly inside its bounded presentation interval at "+fps+" Hz");
            gate.SetPassage(true,1+(fps/5)/(float)fps,false);
            Check(gate.PassageProgress==progress&&passage.transform.localScale==scale,"Holding presentation time freezes the passage during pause at "+fps+" Hz");
            gate.SetPassage(true,2,false);gate.SetPassage(true,3,false);
            Check(!passage.enabled&&gate.PassageProgress==1,"Expired passage cannot be retriggered by repeated presentation callbacks at "+fps+" Hz");
        }
        gate.SetPassage(false,0,false);gate.SetPassage(true,0,true);gate.SetPassage(true,.1f,false);
        Check(!passage.enabled,"Reduced Motion consumes the event without a delayed replay after the setting changes");
        gate.SetPassage(false,0,false);gate.SetPassage(true,4,false);Check(passage.enabled,"A fresh gate state can start a new passage wave");
        Action<bool,float,bool> present=gate.SetPassage;for(int i=0;i<10;i++)present(true,4.1f,false);
        long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<180;i++)present(true,4.1f,false);
        Check(GC.GetAllocatedBytesForCurrentThread()==before&&gate.transform.childCount==children&&gate.PassageSurface==passage,"Warm passage presentation reuses its mesh/material/property block with zero managed allocations");
        var original=(BlackoutCampaignProgress)Get(game,"blackoutCampaign");var campaign=new BlackoutCampaignProgress(new Store());
        try
        {
            Set(game,"blackoutCampaign",campaign);Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);yield return null;
            var plan=(Button)Get(game,"blackoutPlanButton");
            Check(plan.GetComponentInChildren<Text>().text=="PLAN","Blackout plan control matches its in-game guide wording");
            var session=(BlackoutSession)Get(game,"blackoutSession");int trace=session.Trace;
            Call(game,"ToggleBlackoutPreview");Call(game,"PreviewBlackoutNode",6);
            Check(plan.GetComponentInChildren<Text>().text=="CLOSE"&&session.Trace==trace,"Planning is visibly distinct and inspecting a route spends no trace");
            Call(game,"CancelBlackoutPreview");
            var nodes=(List<Text>)Get(game,"blackoutNodeLabels");bool readable=false;
            for(int i=0;i<session.Puzzle.Count;i++)if(session.Puzzle.Tile(i)=='C'&&!session.Powered[i])readable|=nodes[i].color.maxColorComponent>.8f;
            Check(readable,"Required unpowered cores retain readable target labels");
            foreach(int tile in SkyPulseMissionChecks.Solve(session.Puzzle))Call(game,"TouchBlackoutNode",tile);
            var next=(BlackoutSession)Get(game,"blackoutSession");var toast=(Text)Get(game,"blackoutCompletionToast");
            Check(next!=session&&next.State==MissionSessionState.Playing&&(int)Get(game,"selectedMission")==1,"City restoration immediately leaves the next puzzle playable");
            Check(toast.text.Contains(SkyPulseBlackoutCampaign.Systems[0].ToUpperInvariant()+" ONLINE")&&toast.text.Contains("1 / 8"),"Completion feedback names the restored system and the actual persistent campaign total");
            Check(campaign.Data.medals.Count(m=>m>0)==1&&!((GameObject)Get(game,"missionResultScreen")).activeSelf,"The local feedback does not create another restoration or an intermediate modal");
            Call(game,"UpdateBlackoutActionAnimation",2f);
            Check(!toast.text.Contains("ONLINE · 1 / 8")&&next.Trace==0,"Toast expiry restores quiet campaign status without spending the next puzzle's trace");
        }
        finally{Set(game,"blackoutCampaign",original);Call(game,"ResetToMenu");}
        Debug.Log("SKYPULSE_MODE_FEEDBACK_PASS");
    }
}
