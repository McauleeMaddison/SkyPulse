// Editor-only regressions for real city-restoration feedback and enlarged optics.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;
using UnityEditor;

public static class SkyPulsePurposeChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static void Check(bool ok,string note){if(!ok)throw new Exception(note);Debug.Log("PURPOSE_CHECK: "+note);}
    sealed class Store:IMissionStore
    {
        readonly Dictionary<string,string> data=new Dictionary<string,string>();
        public string Get(string k,string d)=>data.TryGetValue(k,out var v)?v:d;
        public int GetInt(string k,int d)=>d;
        public void Set(string k,string v)=>data[k]=v;
        public void SetInt(string k,int v){}
        public void Flush(){}
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        Call(game,"ResetToMenu");var store=new Store();var campaign=new BlackoutCampaignProgress(store);Set(game,"blackoutCampaign",campaign);
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);yield return null;
        var board=(SkyPulseHeistBoard)Get(game,"blackoutBoard");var network=(SkyPulseBlackoutGraphic)Get(game,"blackoutNetwork");
        var controls=(List<Button>)Get(game,"blackoutNodeButtons");
        Check(board.OpticScale>1.25f&&((RectTransform)controls[6].transform).rect.width>=190,"Opening vault has larger optics and generous touch targets");
        Check(network.RestoredMask==0&&network.LivePower==0,"New city network is dark until actual cores are powered");
        Call(game,"ToggleBlackoutPreview");Call(game,"PreviewBlackoutNode",6);
        Check(network.LivePower==0&&network.RestoredMask==0,"Speculative planning cannot falsely light the city or save a restoration");Call(game,"CancelBlackoutPreview");
        Call(game,"TouchBlackoutNode",6);
        Check(Mathf.Abs(network.LivePower-.5f)<.001f&&network.RestoredMask==0,"Actual core power illuminates the active district without marking an unfinished vault complete");
        Call(game,"RestartBlackoutVault");
        Check(network.LivePower==0&&network.RestoredMask==0,"Restart removes partial power without granting city completion");
        for(int vault=0;vault<8;vault++)
        {
            var session=(BlackoutSession)Get(game,"blackoutSession");
            Check(((Text)Get(game,"blackoutObjectiveTitle")).text.Contains(SkyPulseBlackoutCampaign.Systems[vault].ToUpperInvariant()),"Active objective identifies the system being restored "+vault);
            Check(((Text)Get(game,"blackoutBriefing")).text==session.Puzzle.Briefing,"Each vault exposes its specific optical challenge "+vault);
            Check(controls.All(b=>b.transform.localScale==Vector3.one),"Visual scale cannot move or animate UI hit targets");
            foreach(int tile in SkyPulseMissionChecks.Solve(session.Puzzle))Call(game,"TouchBlackoutNode",tile);
            int mask=(1<<(vault+1))-1;
            Check(campaign.Data.medals.Take(vault+1).All(m=>m>0),"Restoration represents persisted successful solves "+vault);
            if(vault<7)Check(network.RestoredMask==mask&&network.ActiveVault==vault+1&&network.LivePower==0,"Completed district lights while the next unsolved district is already playable "+vault);
        }
        var reloaded=new BlackoutCampaignProgress(store);Check(reloaded.Data.medals.All(m=>m>0),"All eight restored systems survive campaign reload");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,7);Check(Mathf.Abs(board.OpticScale-1)<.001f&&network.RestoredMask==255,"Seven-wide challenge retains readable spacing and saved city state");
        Call(game,"InitializeMissionServices");Call(game,"ResetToMenu");Debug.Log("SKYPULSE_PURPOSE_LIVE_PASS");
    }
}

[InitializeOnLoad]
public static class SkyPulsePurposeCapture
{
    static SkyPulsePurposeCapture(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        SessionState.SetBool("SkyPulsePurposeCapture",true);EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool("SkyPulsePurposeCapture",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("SkyPulsePurposeCapture",false);new GameObject("Purpose preview QA").AddComponent<SkyPulseWorldExpansionRunner>().Begin(Capture());
    }
    static IEnumerator Capture()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();if(game==null)game=new GameObject("QA game").AddComponent<SkyPulseNativeGame>();game.enabled=false;
        if(Environment.GetEnvironmentVariable("SKYPULSE_COPY_GUIDE_ONLY")=="1")
        {
            yield return SkyPulseGuideWorldChecks.Live(game);yield return SkyPulseCopyChecks.Live(game);
            Debug.Log("SKYPULSE_COPY_GUIDE_PASS");EditorApplication.Exit(0);yield break;
        }
        if(Environment.GetEnvironmentVariable("SKYPULSE_PREVIEW_RELAY_ONLY")=="1")
        {
            yield return SkyPulseCinematicChecks.Live(game);yield return SkyPulseRelayGateChecks.Live(game);
            Debug.Log("SKYPULSE_RELAY_FINISH_PASS");EditorApplication.Exit(0);yield break;
        }
        yield return SkyPulseBlackoutChecks.Captures(game);Debug.Log("SKYPULSE_PURPOSE_CAPTURE_PASS");EditorApplication.Exit(0);
    }
}
