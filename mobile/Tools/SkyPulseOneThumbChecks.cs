// Disposable Editor QA only; production input/renderer paths are exercised directly.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseOneThumbChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static void Check(bool ok,string note){if(!ok)throw new Exception(note);Debug.Log("ONE_THUMB_CHECK: "+note);}
    public static void Pure()
    {
        var f=new SkyPulseInputFence();Check(f.Press(4,1,1,true),"Fresh finger down accepted");
        Check(!f.Press(4,2,1.1f,true),"Held finger cannot repeat");
        Check(!f.Press(-1,2,1.12f,false),"Simulated mouse cannot duplicate a touch");
        f.Release(4);Check(f.Press(4,3,1.2f,true),"Next deliberate finger down accepted");
        f.Release(4);f.Boundary(4);Check(!f.Press(4,4,1.3f,true),"Completion-frame queued press fenced");
        Check(f.Press(4,5,1.4f,true),"Next frame fresh press accepted");
        f.Release(4);Check(!f.Press(7,5,1.4f,true),"Two event sources in one frame cannot double-act");
        f.ReleaseAll();Check(f.Press(-1,8,2,false),"Real mouse works after touch suppression window");
        var r=new RelaySession(RelayMission.All[2]);Check(r.Collect(4)&&!r.IsOpen(2)&&r.Charges==1,"One crystal cannot open an assigned gate");
        Check(r.Collect(5)&&r.IsOpen(2)&&!r.IsOpen(0)&&!r.IsOpen(1)&&r.Charges==0&&r.UnlockedGates==1,"A complete pair opens only its assigned gate, even out of order");
        Check(!r.Collect(5)&&r.Charges==0&&r.Cells==2,"Duplicate crystal cannot spend or award again");
        r.Pause(true);Check(!r.Collect(0)&&r.Distance==0,"Paused relay rejects pickups");r.Pause(false);
        Check(r.Collect(0)&&!r.IsOpen(0)&&r.Collect(1)&&r.Charges==0&&r.IsOpen(0),"Next gate spends exactly its own two collected charges");
        r=new RelaySession(RelayMission.All[2]);Check(r.Charges==0&&r.Cells==0&&!r.IsOpen(2),"Retry creates clean gate accounting");
        Debug.Log("SKYPULSE_ONE_THUMB_PURE_PASS");
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        game.enabled=false;Call(game,"InitializeMissionServices");Set(game,"hapticsEnabled",false);
        var camera=(Camera)Get(game,"flightCamera");
        foreach(int fps in new[]{30,60,120})
        {
            Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);var r=(RelaySession)Get(game,"relaySession");
            var cells=(List<Vector2>)Get(game,"relayCellPositions");cells[0]=new Vector2(12,r.Mission.RelayY[0]);r.Collect(1);r.Move(11.97f);
            Set(game,"birdY",r.Mission.RelayY[0]);Set(game,"birdVelocity",0f);Set(game,"shieldCharges",0);Set(game,"shieldImmunityTimer",0f);
            Call(game,"SimulateRelay",1f/fps);
            Check(r.IsOpen(0)&&r.Cells==2&&r.HazardHits==0&&r.State==MissionSessionState.Playing,"Same-frame crystal + gate succeeds at "+fps+" FPS before opening animation");
            Check(((List<float>)Get(game,"relayOpeningTimes"))[0]==(float)Get(game,"protocolVisualTime"),"Logical opening is immediate at animation start");
            foreach(int collected in new[]{0,1})
            {
                Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);r=(RelaySession)Get(game,"relaySession");cells=(List<Vector2>)Get(game,"relayCellPositions");
                for(int n=0;n<cells.Count;n++)cells[n]=new Vector2(-100,0);
                if(collected==1)r.Collect(0);r.Move(11.97f);Set(game,"birdY",r.Mission.RelayY[0]);Set(game,"birdVelocity",0f);Set(game,"shieldCharges",1);Set(game,"shieldImmunityTimer",1f);
                Call(game,"SimulateRelay",1f/fps);
                Check(r.State==MissionSessionState.Failed&&!r.IsOpen(0)&&r.Cells==collected,"A gate with "+collected+"/2 crystals cannot be passed, even under shield immunity, at "+fps+" FPS");
            }
            for(int mission=0;mission<4;mission++)
            {
                Call(game,"LaunchMission",SkyPulseMode.PulseRelay,mission);r=(RelaySession)Get(game,"relaySession");float accumulator=0;
                for(int frame=0;frame<12000&&r.State==MissionSessionState.Playing;frame++)
                {
                    int gate=Math.Min(r.Passed,r.Mission.RelayCount-1);float target=SkyPulseMissionChecks.RelayTarget(game);
                    if((float)Get(game,"birdY")<target-.65f&&(float)Get(game,"birdVelocity")<0)Call(game,"Flap");
                    accumulator+=1f/fps;
                    while(accumulator>=1f/120f&&r.State==MissionSessionState.Playing){Call(game,"SimulateRelay",1f/120f);accumulator-=1f/120f;}
                }
                Check(r.State==MissionSessionState.Succeeded&&r.UnlockedGates==r.Mission.RelayCount&&r.HazardHits==0,"Normal tap pilot reaches automatic crystals and all gates: "+mission+" at "+fps+" FPS");
            }
        }
        Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);yield return null;Canvas.ForceUpdateCanvases();
        var hud=(GameObject)Get(game,"relayHudScreen");Check(!hud.GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>().text=="FLAP"||b.GetComponentInChildren<Text>().text=="PULSE"),"Relay has no manual-action buttons");
        var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();Camera uiCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        var pause=hud.GetComponentsInChildren<Button>().Single();var pauseAt=RectTransformUtility.WorldToScreenPoint(uiCamera,pause.transform.position);
        Check((bool)Call(game,"InteractiveUiAt",pauseAt),"Actual pause button raycast excludes gameplay");
        Check(!(bool)Call(game,"SubmitRelayPress",19,Time.frameCount+2,20f,true,pauseAt),"Pause-area finger cannot flap");
        var instrument=((Text)Get(game,"relayChargeText")).rectTransform;var statAt=RectTransformUtility.WorldToScreenPoint(uiCamera,instrument.position);
        Check(!(bool)Call(game,"InteractiveUiAt",statAt),"Decorative crystal HUD does not block flight input");
        Vector2 centre=camera.pixelRect.center;float v=(float)Get(game,"birdVelocity");
        Check((bool)Call(game,"SubmitRelayPress",19,Time.frameCount+3,21f,true,centre)&&(float)Get(game,"birdVelocity")>v,"Playable-area fresh tap applies flap immediately");
        Check(!(bool)Call(game,"SubmitRelayPress",-1,Time.frameCount+4,21.1f,false,centre),"Simulated mouse cannot flap again");
        Call(game,"PauseFlight");Check(!(bool)Call(game,"SubmitRelayPress",20,Time.frameCount+5,22f,true,centre),"Paused gameplay ignores touches");Call(game,"ResumeFlight");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);yield return null;
        var s=(BlackoutSession)Get(game,"blackoutSession");var targets=(List<SkyPulsePressTarget>)Get(game,"blackoutPressTargets");
        int prism=SkyPulseMissionChecks.Solve(s.Puzzle)[0];
        var pointer=new PointerEventData(EventSystem.current){pointerId=38,button=PointerEventData.InputButton.Left};
        targets[prism].OnPointerDown(pointer);Check(s.Trace==1&&s.Rotations[prism]!=s.Puzzle.Initial[prism],"Real pointer-down rotates the optic immediately");
        Check(((SkyPulseHeistBoard)Get(game,"blackoutBoard")).Circuit.Rotations[prism]==s.Rotations[prism],"Visual rotation target updates before animation finishes");
        yield return null;targets[prism].OnPointerDown(pointer);Check(s.Trace==1,"Held pointer cannot rotate twice");targets[prism].OnPointerUp(pointer);
        yield return null;targets[prism].OnPointerDown(pointer);Check(s.Trace==2,"Fresh released/repressed pointer responds during the effect");targets[prism].OnPointerUp(pointer);
        int old=(int)Get(game,"blackoutGeneration");Call(game,"RestartBlackoutVault");s=(BlackoutSession)Get(game,"blackoutSession");
        foreach(var a in SkyPulseMissionChecks.Solve(s.Puzzle))Call(game,"TouchBlackoutNode",a);
        var next=(BlackoutSession)Get(game,"blackoutSession");Check(s.State==MissionSessionState.Succeeded&&next!=s&&next.Trace==0,"Next vault activates in the completion call");
        int nextPrism=SkyPulseMissionChecks.Solve(next.Puzzle)[0];
        Call(game,"HandleBlackoutPress",nextPrism,old,40);Check(next.Trace==0,"Stale previous-vault generation cannot act");
        Call(game,"HandleBlackoutPress",nextPrism,(int)Get(game,"blackoutGeneration"),40);Check(next.Trace==0,"Queued completion-frame press cannot enter next vault");
        Check(targets.All(t=>t.GetComponent<Button>().onClick.GetPersistentEventCount()==0),"Rotations are not queued in click-on-release callbacks");
        foreach(int fps in new[]{30,60,120})
        {
            Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);Call(game,"RestartBlackoutVault");s=(BlackoutSession)Get(game,"blackoutSession");Call(game,"TouchBlackoutNode",SkyPulseMissionChecks.Solve(s.Puzzle)[0]);
            var optic=(SkyPulseHeistBoard)Get(game,"blackoutBoard");
            for(int frame=0;frame<fps;frame++)optic.Tick(1f/fps);
            Check(Mathf.Abs(Mathf.DeltaAngle(optic.VisualAngle(prism),s.Rotations[prism]*90))<.01f&&s.Trace==1,"Rotation settles without extra turns at "+fps+" FPS: angle="+optic.VisualAngle(prism)+" target="+s.Rotations[prism]+" trace="+s.Trace+" paused="+optic.MotionPaused);
        }
        Set(game,"reduceMotionEnabled",true);Call(game,"UpdateProtocolPresentation",0f);
        Check(targets[prism].transform.localScale==Vector3.one,"3D feedback leaves puzzle touch targets fixed");
        var network=(SkyPulseHeistBoard)Get(game,"blackoutBoard");network.ReducedMotion=true;network.Reveal=0;
        network.Tick(0);var points=(Vector3[])Get(network,"beamVertices");var positions=(Vector3[])points.Clone();network.Tick(.4f);
        Check(positions.SequenceEqual(points),"Reduced Motion keeps real 3D beam geometry stationary");
        Set(game,"reduceMotionEnabled",false);Call(game,"LaunchMission",SkyPulseMode.PulseRelay,2);
        var crystalHighlights=(List<SpriteRenderer>)Get(game,"relayCrystalHighlights");
        Check(crystalHighlights.Count==8,"Eight random crystals retain premium highlights without an imposed guide path");
        var animateRelay=(Action)Delegate.CreateDelegate(typeof(Action),game,typeof(SkyPulseNativeGame).GetMethod("AnimateRelayArtwork",F));
        for(int i=0;i<3;i++)animateRelay();long relayBefore=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<120;i++)animateRelay();
        Check(GC.GetAllocatedBytesForCurrentThread()==relayBefore,"Warmed Relay crystal and gate animation creates no managed allocations");
        var spark=((List<SpriteRenderer>)Get(game,"relayCrystalGlints"))[0];Call(game,"PauseFlight");Vector3 sparkBefore=spark.transform.localPosition;
        Call(game,"UpdateProtocolPresentation",1f);Call(game,"UpdateRelayPresentation");Check(spark.transform.localPosition==sparkBefore,"Pause freezes Relay crystal animation");Call(game,"ResumeFlight");
        Set(game,"reduceMotionEnabled",true);Call(game,"UpdateProtocolPresentation",0f);Call(game,"UpdateRelayPresentation");sparkBefore=spark.transform.localPosition;
        Set(game,"protocolVisualTime",99f);Call(game,"UpdateRelayPresentation");Check(spark.transform.localPosition==sparkBefore,"Reduced Motion keeps Relay crystal glints stationary while gates remain usable");
        foreach(var mode in new[]{SkyPulseMode.BlackoutProtocol,SkyPulseMode.PulseRelay,SkyPulseMode.EndlessRoute,SkyPulseMode.BlackoutProtocol,SkyPulseMode.EndlessRoute})
        {
            Call(game,"SetHomeProtocol",mode);Call(game,"ResetToMenu");var home=(GameObject)Get(game,"homeScreen");
            Check(home.transform.Find("Button · TECH TREE").gameObject.activeSelf==(mode==SkyPulseMode.EndlessRoute),"Tech Tree appears only in relevant Home");
            Check(home.transform.Find("Button · BIRD HANGAR").gameObject.activeSelf==(mode!=SkyPulseMode.BlackoutProtocol),"Blackout Home has no Bird Hangar");
            Check(home.transform.Find("Button · WORLDS & POWER-UPS").gameObject.activeSelf==(mode==SkyPulseMode.EndlessRoute),"World guide stays specific to Endless");
        }
        // Warm mesh buffers, then measure the real procedural mesh method through
        // a cached delegate (no reflection invocation allocation in the interval).
        using(var vh=new VertexHelper())foreach(var g in ((GameObject)Get(game,"uiRoot")).GetComponentsInChildren<SkyPulseBlackoutGraphic>(true))
        {
            var draw=(Action<VertexHelper>)Delegate.CreateDelegate(typeof(Action<VertexHelper>),g,typeof(SkyPulseBlackoutGraphic).GetMethod("OnPopulateMesh",F,null,new[]{typeof(VertexHelper)},null));
            for(int i=0;i<3;i++)draw(vh);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10;i++)draw(vh);
            Check(GC.GetAllocatedBytesForCurrentThread()==before,"Warmed "+g.Surface+" geometry has no managed array allocations");
        }
        Set(game,"reduceMotionEnabled",false);Call(game,"InitializeMissionServices");Call(game,"ResetToMenu");
        Debug.Log("SKYPULSE_ONE_THUMB_LIVE_PASS");
    }
}
