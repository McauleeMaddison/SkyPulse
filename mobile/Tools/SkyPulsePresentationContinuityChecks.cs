using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using SkyPulse.Mobile;

// A visual camera handoff must not alter the tested flight or economy.
public static class SkyPulsePresentationContinuityChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);Debug.Log("PRESENTATION_CONTINUITY_CHECK: "+why);}
    static Vector3[] Positions(SkyPulseFlightEnvironment env)
    {
        var slots=(Transform[,])Get(env,"scenery");var p=new Vector3[12];for(int i=0;i<12;i++)p[i]=slots[1,i].localPosition;return p;
    }
    static void Same(Vector3[] prior,SkyPulseFlightEnvironment env,string reason)
    {
        var next=Positions(env);for(int i=0;i<prior.Length;i++)Check((prior[i]-next[i]).sqrMagnitude<.000001f,reason+" / "+i);
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");
        Set(game,"reduceMotionEnabled",false);Set(game,"hapticsEnabled",false);
        Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"StartFlight");Call(game,"ResetToMenu");
        var env=(SkyPulseFlightEnvironment)Get(game,"flightEnvironment");env.ResetView();
        Set(game,"sceneryHomeTravel",37.5f);Set(game,"sceneryWasHome",true);Call(game,"UpdateCinematicPresentation",0f);
        var atHome=Positions(env);int wallet=(int)Get(game,"crystals");
        Check(Mathf.Abs(env.Travel-37.5f)<.0001f,"Fixture is a genuinely drifted Home rather than a zero-distance false positive");
        Call(game,"StartFlight");Call(game,"UpdateCinematicPresentation",0f);Same(atHome,env,"Home-to-flight keeps every district in place");
        Check((float)Get(game,"sceneryTravel")==0&&(int)Get(game,"score")==0,"A seamless visual handoff still starts a fresh zero-score run");
        for(int i=0;i<8;i++){Call(game,"SimulateFlight",1f/60);Call(game,"UpdateCinematicPresentation",1f/60);}
        Check(env.Travel>37.5f,"Normal pipe travel advances the retained skyline");
        var paused=Positions(env);Call(game,"PauseFlight");Call(game,"UpdateCinematicPresentation",1f);Same(paused,env,"Pause preserves the live composition");Call(game,"ResumeFlight");
        Call(game,"EndFlight");Call(game,"UpdateCinematicPresentation",0f);var crashed=Positions(env);
        Call(game,"RestartFlight");Call(game,"UpdateCinematicPresentation",0f);Same(crashed,env,"Retry resets the run without snapping the district");
        Check((float)Get(game,"sceneryTravel")==0&&(int)Get(game,"score")==0,"Retry keeps logical distance and score reset");
        var running=Positions(env);Call(game,"ResetToMenu");Call(game,"UpdateCinematicPresentation",0f);Same(running,env,"Returning Home keeps its visible world pose");
        // Reproduce the combined setting/handoff states, not just each action
        // in isolation: the held view has a nonzero input-distance offset.
        Call(game,"StartFlight");Call(game,"UpdateCinematicPresentation",0f);
        Set(game,"reduceMotionEnabled",true);Call(game,"UpdateCinematicPresentation",0f);
        for(int i=0;i<8;i++){Call(game,"SimulateFlight",1f/60);Call(game,"UpdateCinematicPresentation",1f/60);}
        Set(game,"reduceMotionEnabled",false);Call(game,"UpdateCinematicPresentation",0f);
        Check(Mathf.Abs((float)Get(env,"distanceOffset"))>.001f,"Fixture really resumes from a held view with a nonzero offset");
        var afterMotion=Positions(env);Call(game,"ResetToMenu");Call(game,"UpdateCinematicPresentation",0f);Same(afterMotion,env,"Reduced Motion resume then Home does not apply the view offset twice");
        Set(game,"reduceMotionEnabled",true);Call(game,"UpdateCinematicPresentation",0f);var held=Positions(env);float heldClock=env.PresentationClock;
        Call(game,"StartFlight");Call(game,"UpdateCinematicPresentation",0f);Same(held,env,"Starting a run with Reduced Motion enabled preserves the held view");
        Check(env.PresentationClock==heldClock,"Reduced Motion restart retains its existing light phase");
        Call(game,"EndFlight");Call(game,"UpdateCinematicPresentation",0f);held=Positions(env);Call(game,"RestartFlight");Call(game,"UpdateCinematicPresentation",0f);Same(held,env,"Retry with Reduced Motion enabled preserves the held view");
        Set(game,"reduceMotionEnabled",false);Call(game,"UpdateCinematicPresentation",0f);Call(game,"ResetToMenu");
        Check(wallet==(int)Get(game,"crystals"),"Camera handoffs award and spend no crystals");
        Debug.Log("SKYPULSE_PRESENTATION_CONTINUITY_PASS");yield return null;
    }
}
