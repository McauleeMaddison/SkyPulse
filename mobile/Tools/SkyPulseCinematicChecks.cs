using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using SkyPulse.Mobile;

public static class SkyPulseCinematicChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);Debug.Log("CINEMATIC_CHECK: "+s);}
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        game.enabled=false;Set(game,"reduceMotionEnabled",false);Call(game,"LaunchMission",SkyPulseMode.PulseRelay,2);Call(game,"UpdateCinematicPresentation",0f);
        var session=(RelaySession)Get(game,"relaySession");var gates=(List<SkyPulseRelayGate>)Get(game,"relayPortalHardware");var lasers=(List<SpriteRenderer>)Get(game,"relayLasers");
        Call(game,"UpdateRelayPresentation");Check(lasers.Count==gates.Count*3&&lasers[0].color.r>.9f&&lasers[0].color.b<.3f,"Locked gate has three visible red laser beams across its aperture");
        session.Collect(0);Call(game,"UpdateRelayPresentation");Check(gates[0].ChargeFraction==.5f&&!session.IsOpen(0),"First crystal charges half the ring while the gate remains locked");
        session.Collect(1);Call(game,"UpdateRelayPresentation");Check(gates[0].ChargeFraction==1&&session.IsOpen(0)&&lasers[0].color.b>.9f,"Final crystal switches lasers blue and unlocks synchronously");
        var prisms=(List<SkyPulsePrism>)Get(game,"relayPrisms");var firstPrism=prisms[0];Check(prisms.Count==8,"All eight circuit crystals use real faceted mesh volumes");
        Check(firstPrism.Shell.GetComponent<MeshFilter>().sharedMesh.bounds.size.z>.3f&&firstPrism.Shell.sharedMaterial.shader.isSupported,"Crystal has optical depth and supported shader");
        var bird=(SpriteRenderer)Get(game,"birdRenderer");Check(bird.sprite.vertices.Length>100&&bird.sharedMaterial.shader.isSupported,"Registered bird pose has a flexible articulated mesh and supported lighting shader");
        var environment=(SkyPulseRelayEnvironment)Get(game,"relayEnvironment");Check(environment!=null&&environment.GetComponentsInChildren<MeshRenderer>().Length==10,"Layered station contains shared-mesh machinery and maintenance drones");
        float oldY=(float)Get(game,"birdY"),oldV=(float)Get(game,"birdVelocity"),oldD=session.Distance;
        var present=(Action<float>)Delegate.CreateDelegate(typeof(Action<float>),game,typeof(SkyPulseNativeGame).GetMethod("UpdateCinematicPresentation",F));
        for(int i=0;i<5;i++)present(1f/60);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<120;i++)present(1f/60);
        Check(GC.GetAllocatedBytesForCurrentThread()==before,"Warmed bird rig and pooled flight effects allocate no managed memory");
        Check(oldY==(float)Get(game,"birdY")&&oldV==(float)Get(game,"birdVelocity")&&oldD==session.Distance,"Presentation cannot move the player or advance the course");
        Call(game,"PauseFlight");float clock=(float)Get(game,"cinematicClock");present(1);Check((float)Get(game,"cinematicClock")==clock,"Pause freezes bird rig, engine ribbon and passage effects");Call(game,"ResumeFlight");
        Set(game,"reduceMotionEnabled",true);Call(game,"UpdateProtocolPresentation",0f);Call(game,"UpdateRelayPresentation");Check(lasers[0].color.a==0,"Reduced Motion clears unlocked beams immediately without a crossing effect");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);
        var network=(SkyPulseBlackoutGraphic)Get(game,"blackoutNetwork");network.BeginSectorTransition();
        var board=(BlackoutSession)Get(game,"blackoutSession");int cursor=board.Cursor,trace=board.Trace;
        for(int i=0;i<20;i++)network.EmitSignal(Vector2.zero,new Vector2(i,4),1);
        Check(board.Cursor==cursor&&board.Trace==trace,"Pooled security signals and sector transition have no gameplay side effects");
        for(int i=0;i<5;i++)network.TickEffects(1f/60);before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<120;i++){network.EmitSignal(Vector2.zero,Vector2.one,1);network.TickEffects(1f/60);}
        Check(GC.GetAllocatedBytesForCurrentThread()==before,"Rapid pooled puzzle signals allocate no managed memory");
        foreach(int fps in new[]{30,60,120})
        {
            network.BeginSectorTransition();for(int f=0;f<fps;f++)network.TickEffects(1f/fps);Check(network.SectorReveal==0,"Nonblocking sector transition settles consistently at "+fps+" FPS");
        }
        Call(game,"ResetToMenu");yield return null;Check(environment==null&&firstPrism==null,"Course cleanup releases owned scenery/crystal resources");
        Debug.Log("SKYPULSE_CINEMATIC_PASS");
    }
}
