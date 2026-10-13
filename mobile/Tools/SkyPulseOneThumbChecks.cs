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
        var r=new RelaySession(RelayMission.All[2]);Check(r.Collect(4)&&r.IsOpen(2)&&!r.IsOpen(0)&&!r.IsOpen(1)&&r.Charges==0,"A crystal opens only its assigned gate, even out of order");
        Check(r.Collect(5)&&r.Charges==1&&r.UnlockedGates==1&&r.LastUnlockedGate==-1,"Spare cell keeps one charge without opening another gate");
        Check(!r.Collect(5)&&r.Charges==1&&r.Cells==2,"Duplicate crystal cannot spend or award again");
        r.Pause(true);Check(!r.Collect(0)&&r.Distance==0,"Paused relay rejects pickups");r.Pause(false);
        Check(r.Collect(0)&&r.Charges==1&&r.IsOpen(0),"Next gate spends exactly its collected charge");
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
            var cells=(List<Vector2>)Get(game,"relayCellPositions");cells[0]=new Vector2(12,0);r.Move(11.97f);
            Set(game,"birdY",0f);Set(game,"birdVelocity",0f);Set(game,"shieldCharges",0);Set(game,"shieldImmunityTimer",0f);
            Call(game,"SimulateRelay",1f/fps);
            Check(r.IsOpen(0)&&r.Cells==1&&r.HazardHits==0&&r.State==MissionSessionState.Playing,"Same-frame crystal + gate succeeds at "+fps+" FPS before opening animation");
            Check(((List<float>)Get(game,"relayOpeningTimes"))[0]==(float)Get(game,"protocolVisualTime"),"Logical opening is immediate at animation start");
            for(int mission=0;mission<4;mission++)
            {
                Call(game,"LaunchMission",SkyPulseMode.PulseRelay,mission);r=(RelaySession)Get(game,"relaySession");float accumulator=0;
                for(int frame=0;frame<12000&&r.State==MissionSessionState.Playing;frame++)
                {
                    int gate=Math.Min(r.Passed,r.Mission.RelayCount-1);float target=r.Passed==r.Mission.RelayCount?r.Mission.ExitY:r.Mission.RelayY[gate];
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
        var pointer=new PointerEventData(EventSystem.current){pointerId=38,button=PointerEventData.InputButton.Left};
        targets[1].OnPointerDown(pointer);Check(s.Cursor==1&&s.Trace==1,"Real pointer-down moves the puzzle immediately");
        yield return null;targets[2].OnPointerDown(pointer);Check(s.Cursor==1&&s.Trace==1,"Held pointer cannot hit the next node");targets[1].OnPointerUp(pointer);
        yield return null;targets[2].OnPointerDown(pointer);Check(s.Cursor==2&&s.Trace==2,"Fresh released/repressed pointer advances during the effect");targets[2].OnPointerUp(pointer);
        int old=(int)Get(game,"blackoutGeneration");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);s=(BlackoutSession)Get(game,"blackoutSession");
        foreach(var a in SkyPulseMissionChecks.Solve(s.Puzzle))Call(game,"TouchBlackoutNode",a.Item1);
        var next=(BlackoutSession)Get(game,"blackoutSession");Check(s.State==MissionSessionState.Succeeded&&next!=s&&next.Trace==0,"Next puzzle activates in the same completion call");
        Call(game,"HandleBlackoutPress",1,old,40);Check(next.Trace==0,"Stale previous-puzzle generation cannot act");
        Call(game,"HandleBlackoutPress",1,(int)Get(game,"blackoutGeneration"),40);Check(next.Trace==0,"Queued completion-frame press cannot enter next puzzle");
        Check(targets.All(t=>t.GetComponent<Button>().onClick.GetPersistentEventCount()==0),"Movement is not queued in click-on-release callbacks");
        foreach(int fps in new[]{30,60,120})
        {
            Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);Call(game,"TouchBlackoutNode",1);
            for(int frame=0;frame<fps;frame++)Call(game,"UpdateProtocolPresentation",1f/fps);
            Check(((SkyPulseBlackoutGraphic)Get(game,"blackoutNetwork")).Travel==1,"Smooth transport completes at "+fps+" FPS");
        }
        Set(game,"reduceMotionEnabled",true);Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);Call(game,"TouchBlackoutNode",1);
        Check(((SkyPulseBlackoutGraphic)Get(game,"blackoutNetwork")).Travel==1,"Reduced Motion moves instantly without an interaction delay");
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
