// Copied into an isolated Editor project only. Never install in production Assets.
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulsePresentationChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("PRESENTATION_CHECK: "+message);}
    public static IEnumerator Captures(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006" || Application.productName!="SkyPulseQA20261006")throw new Exception("Disposable QA project required");
        game.enabled=false;Set(game,"reduceMotionEnabled",false);
        Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");
        var worlds=(Array)typeof(SkyPulseNativeGame).GetField("Worlds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);Call(game,"EquipWorld",worlds.GetValue(0));
        var camera=(Camera)Get(game,"flightCamera");var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        foreach(var size in new[]{new Vector2Int(750,1334),new Vector2Int(1320,2868),new Vector2Int(1640,2360)})
        {
            var widthTarget=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=widthTarget;
            Call(game,"StartFlight");float spacing=(float)Call(game,"RoutePipeSpacing");
            Check(Mathf.Abs(spacing-5.589f)<.0001f,"Owner-requested Endless gate spacing is 5.589 world units at "+size.x+"x"+size.y);
            var gates=(Array)Get(game,"pipePool");
            for(int i=1;i<gates.Length;i++)Check(Mathf.Abs((float)Get(gates.GetValue(i),"X")-(float)Get(gates.GetValue(i-1),"X")-spacing)<.0001f,"Endless spawned gate centres retain original spacing");
            Debug.Log("ENDLESS_SPACING_VIEW: "+size.x+"x"+size.y+" world="+spacing+" fraction="+(spacing/(float)Call(game,"GetViewportWidth")));
            camera.targetTexture=null;RenderTexture.ReleaseTemporary(widthTarget);
        }
        Call(game,"ResetToMenu");
        int captureWidth=750,captureHeight=1334;
        if(Environment.GetEnvironmentVariable("SKYPULSE_PHONE_PREVIEW")=="1"){captureWidth=1320;captureHeight=2868;}
        var rt=RenderTexture.GetTemporary(captureWidth,captureHeight,24);camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);Call(game,"RefreshViewportDecor");
        string folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"motion");Directory.CreateDirectory(folder);
        var scenes=Environment.GetEnvironmentVariable("SKYPULSE_PREVIEW_SCENES");
        foreach(string scene in string.IsNullOrEmpty(scenes)?new[]{"home-endless","home-relay","home-blackout","mode-dropdown","endless-flight","relay-flight","blackout-puzzle"}:scenes.Split(','))
        {
            string output=Path.Combine(folder,scene);Directory.CreateDirectory(output);
            if(scene.StartsWith("home")){Call(game,"SetHomeProtocol",scene=="home-relay"?SkyPulseMode.PulseRelay:scene=="home-blackout"?SkyPulseMode.BlackoutProtocol:SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");}
            if(scene.StartsWith("home"))Check(((SkyPulseDepthStage)Get(game,"homeDepth")).StageCamera.targetTexture!=null,"Home uses a real 3D camera and render target");
            if(scene=="mode-dropdown"){Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");}
            if(scene=="endless-flight")Call(game,"StartFlight");
            if(scene=="relay-flight")
            {
                Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);
                var relay=(RelaySession)Get(game,"relaySession");while(relay.Distance<6){Pilot(game);Call(game,"SimulateRelay",1f/120f);}Call(game,"UpdateRelayPresentation");
            }
            if(scene=="blackout-puzzle")Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);
            for(int frame=0;frame<40;frame++)
            {
                if(scene=="mode-dropdown")
                {
                    if(frame==5)Call(game,"OpenModeSelection");
                    if(frame==25)Call(game,"SelectHomeProtocol",SkyPulseMode.PulseRelay);
                }
                if(scene=="endless-flight")
                {
                    for(int step=0;step<12;step++)
                    {
                        float target=0,nearest=float.MaxValue;foreach(var gate in (Array)Get(game,"pipePool"))
                        {float x=(float)Get(gate,"X");if(!(bool)Get(gate,"Passed") && x>=-3f && x<nearest){nearest=x;target=(float)Get(gate,"GapCenter");}}
                        if((float)Get(game,"birdY")<target-.65f && (float)Get(game,"birdVelocity")<0)Call(game,"Flap");Call(game,"SimulateFlight",1f/120f);
                    }
                    Call(game,"UpdateGameplayWingState",.1f);Call(game,"UpdateBirdWingMotion");
                }
                if(scene=="relay-flight")
                {
                    for(int step=0;step<12;step++){Pilot(game);Call(game,"SimulateRelay",1f/120f);}
                    Call(game,"UpdateGameplayWingState",.1f);Call(game,"UpdateBirdWingMotion");Call(game,"UpdateRelayPresentation");
                }
                if(scene=="blackout-puzzle" && frame==18){var s=(BlackoutSession)Get(game,"blackoutSession");Call(game,"TouchBlackoutNode",SkyPulseMissionChecks.Solve(s.Puzzle)[0]);}
                Set(game,"ambientTime",(float)Get(game,"ambientTime")+.1f);Call(game,"UpdateMenuBird",.1f);Call(game,"UpdateProtocolPresentation",.1f);Call(game,"UpdateCinematicPresentation",.1f);
                yield return new WaitForSecondsRealtime(.1f);
                Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();foreach(var stage in UnityEngine.Object.FindObjectsByType<SkyPulseHeistBoard>(FindObjectsSortMode.None))stage.Render();foreach(var depth in UnityEngine.Object.FindObjectsByType<SkyPulseDepthStage>(FindObjectsSortMode.None))depth.Render();camera.Render();
                RenderTexture.active=rt;var texture=new Texture2D(captureWidth,captureHeight,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,captureWidth,captureHeight),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,frame.ToString("000")+".png"),texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);RenderTexture.active=null;
            }
        }
        Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);Call(game,"UpdateProtocolPresentation",.1f);
        foreach(Transform gate in (System.Collections.IEnumerable)Get(game,"relayShutters"))
        {
            var hardware=gate.Find("Volumetric relay assembly");Check(hardware!=null && hardware.GetComponent<SkyPulseRelayGate>()!=null,"Relay uses genuine hollow 3D mesh hardware");
            Check(gate.Find("Port collar")==null && gate.Find("Portal metal rim")==null,"Relay has no reused Endless collar or segmented block rim");
            Check(gate.GetComponentsInChildren<Collider2D>().Length==0,"Presentation does not add collision shapes");
        }
        var testRelay=(RelaySession)Get(game,"relaySession");Check(testRelay.Collect(0) && !testRelay.IsOpen(0) && testRelay.Collect(1) && testRelay.IsOpen(0),"Crystal logically opens shutter before artwork");Call(game,"UpdateRelayPresentation");
        var firstGate=((System.Collections.IList)Get(game,"relayShutters"))[0] as Transform;
        var shutter=firstGate.Find("Energy shutter").GetComponent<SpriteRenderer>();var previousShutterScale=shutter.transform.localScale;
        var glint=firstGate.Find("Portal emitter orbit").GetChild(0);var previousGlint=glint.localPosition;
        float time=(float)Get(game,"protocolVisualTime");Call(game,"PauseFlight");Call(game,"UpdateProtocolPresentation",2f);
        Call(game,"UpdateRelayPresentation");Check(shutter.transform.localScale==previousShutterScale,"Pause preserves shutter opening state");
        Check(glint.localPosition==previousGlint,"Pause freezes premium portal glints");
        Check((float)Get(game,"protocolVisualTime")==time,"Pause freezes relay presentation clock");
        Call(game,"ResumeFlight");Set(game,"reduceMotionEnabled",true);Call(game,"UpdateProtocolPresentation",2f);
        Call(game,"UpdateRelayPresentation");Check(shutter.color.a==0 && firstGate.Find("Activation window").GetComponent<SpriteRenderer>().color.a==0,"Reduced Motion clears activated shutter immediately");
        var reducedGlint=glint.localPosition;Call(game,"UpdateProtocolPresentation",1f);Call(game,"UpdateRelayPresentation");Check(glint.localPosition==reducedGlint,"Reduced Motion freezes premium portal glints");
        Check((float)Get(game,"protocolVisualTime")==time,"Reduced Motion freezes relay scenery and charge animation");
        Call(game,"ResetToMenu");Call(game,"OpenModeSelection");
        Check(((RectTransform)Get(game,"modeDropdownClip")).rect.height==688,"Reduced Motion opens complete comfort-sized dropdown immediately");
        Call(game,"CloseProtocolDropdown");Check(!((GameObject)Get(game,"modeSelectionScreen")).activeSelf,"Reduced Motion dismisses dropdown immediately");
        Set(game,"reduceMotionEnabled",false);Call(game,"ResetToMenu");camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);
        Debug.Log("SKYPULSE_PRESENTATION_CAPTURES_PASS");
    }
    static void Pilot(SkyPulseNativeGame game)
    {
        var relay=(RelaySession)Get(game,"relaySession");int gate=Math.Min(relay.Passed,relay.Mission.RelayCount-1);float target=SkyPulseMissionChecks.RelayTarget(game);
        if((float)Get(game,"birdY")<target-.65f && (float)Get(game,"birdVelocity")<0)Call(game,"Flap");
    }
}
