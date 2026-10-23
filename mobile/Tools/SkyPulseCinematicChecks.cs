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
        var deck=(SkyPulseRelayDeck)Get(game,"relayDeck");var deckMesh=deck.GetComponent<MeshFilter>().sharedMesh;
        Check(deckMesh.vertexCount>100&&deckMesh.bounds.size.z>=3&&deck.GetComponentsInChildren<MeshRenderer>().Length==1,"Runway has a real bevelled panel volume in one pooled mesh draw");
        Check(deck.GetComponent<MeshRenderer>().sharedMaterial.shader.isSupported&&deck.GetComponentsInChildren<Collider>().Length==0&&deck.GetComponentsInChildren<Collider2D>().Length==0,"New floor material compiles without adding gameplay colliders");
        var deckMaterial=deck.GetComponent<MeshRenderer>().sharedMaterial;
        Check(deckMaterial.GetTexture("_Environment")!=null,"Deck uses the station artwork for coherent reflected lighting");
        for(int i=0;i<5;i++){environment.SetVisual(i,.1f*i);deck.SetVisual(i,.1f*i);}
        long environmentBefore=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<120;i++){environment.SetVisual(i/30f,i/60f);deck.SetVisual(i/30f,i/60f);}
        Check(GC.GetAllocatedBytesForCurrentThread()==environmentBefore,"Warmed station and shader-pooled deck animation allocate no managed memory");
        Check(Mathf.Abs(deck.transform.position.y+8.45f)<.0001f,"Panel animation never moves the authoritative ground guide height");
        float oldY=(float)Get(game,"birdY"),oldV=(float)Get(game,"birdVelocity"),oldD=session.Distance;
        var present=(Action<float>)Delegate.CreateDelegate(typeof(Action<float>),game,typeof(SkyPulseNativeGame).GetMethod("UpdateCinematicPresentation",F));
        for(int i=0;i<5;i++)present(1f/60);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<120;i++)present(1f/60);
        Check(GC.GetAllocatedBytesForCurrentThread()==before,"Warmed bird rig and pooled flight effects allocate no managed memory");
        Check(oldY==(float)Get(game,"birdY")&&oldV==(float)Get(game,"birdVelocity")&&oldD==session.Distance,"Presentation cannot move the player or advance the course");
        Call(game,"PauseFlight");float clock=(float)Get(game,"cinematicClock");present(1);Check((float)Get(game,"cinematicClock")==clock,"Pause freezes bird rig, engine ribbon and passage effects");Call(game,"ResumeFlight");
        Set(game,"reduceMotionEnabled",true);Call(game,"UpdateProtocolPresentation",0f);Call(game,"UpdateRelayPresentation");Check(lasers[0].color.a==0,"Reduced Motion clears unlocked beams immediately without a crossing effect");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);
        var network=(SkyPulseHeistBoard)Get(game,"blackoutBoard");var board=(BlackoutSession)Get(game,"blackoutSession");
        int trace=board.Trace;network.Reveal=1;
        for(int i=0;i<5;i++)network.Tick(1f/60);before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<120;i++)network.Tick(1f/60);
        long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Check(board.Trace==trace&&network.Circuit==board,"Live beam presentation never mutates the simulation");
        Check(allocated==0,"Warmed signal animation allocates no managed memory");
        foreach(int fps in new[]{30,60,120})
        {
            network.Reveal=1;for(int f=0;f<fps;f++)network.Tick(1f/fps);Check(network.Reveal==0,"Nonblocking vault reveal settles at "+fps+" FPS");
        }
        Call(game,"ResetToMenu");yield return null;Check(environment==null&&firstPrism==null,"Course cleanup releases owned scenery/crystal resources");
        Debug.Log("SKYPULSE_CINEMATIC_PASS");
    }
}
