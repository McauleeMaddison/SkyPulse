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
        var rt=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);Call(game,"RefreshViewportDecor");
        string folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"motion");Directory.CreateDirectory(folder);
        foreach(string scene in new[]{"home-relay","home-blackout","mode-dropdown","relay-flight","blackout-puzzle"})
        {
            string output=Path.Combine(folder,scene);Directory.CreateDirectory(output);
            if(scene.StartsWith("home")){Call(game,"SetHomeProtocol",scene=="home-relay"?SkyPulseMode.PulseRelay:SkyPulseMode.BlackoutProtocol);Call(game,"ResetToMenu");}
            if(scene=="mode-dropdown"){Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");}
            if(scene=="relay-flight")
            {
                Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);
                var relay=(RelaySession)Get(game,"relaySession");while(relay.Distance<6){Pilot(game);Call(game,"SimulateRelay",1f/120f);}Call(game,"UpdateRelayPresentation");
            }
            if(scene=="blackout-puzzle")Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,1);
            for(int frame=0;frame<40;frame++)
            {
                if(scene=="mode-dropdown")
                {
                    if(frame==5)Call(game,"OpenModeSelection");
                    if(frame==25)Call(game,"SelectHomeProtocol",SkyPulseMode.PulseRelay);
                }
                if(scene=="relay-flight")
                {
                    for(int step=0;step<12;step++){Pilot(game);if(frame>=7)Call(game,"PressPulse");Call(game,"SimulateRelay",1f/120f);}
                    Call(game,"UpdateGameplayWingState",.1f);Call(game,"UpdateBirdWingMotion");Call(game,"UpdateRelayPresentation");
                }
                if(scene=="blackout-puzzle" && frame==18){Set(game,"hackArmed",false);Call(game,"BlackoutAction",1);}
                Set(game,"ambientTime",(float)Get(game,"ambientTime")+.1f);Call(game,"UpdateMenuBird",.1f);Call(game,"UpdateProtocolPresentation",.1f);
                yield return new WaitForSecondsRealtime(.1f);
                Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();camera.Render();
                RenderTexture.active=rt;var texture=new Texture2D(750,1334,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,750,1334),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,frame.ToString("000")+".png"),texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);RenderTexture.active=null;
            }
        }
        Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);Call(game,"UpdateProtocolPresentation",.1f);
        float time=(float)Get(game,"protocolVisualTime");Call(game,"PauseFlight");Call(game,"UpdateProtocolPresentation",2f);
        Check((float)Get(game,"protocolVisualTime")==time,"Pause freezes relay presentation clock");
        Call(game,"ResumeFlight");Set(game,"reduceMotionEnabled",true);Call(game,"UpdateProtocolPresentation",2f);
        Check((float)Get(game,"protocolVisualTime")==time,"Reduced Motion freezes relay scenery and charge animation");
        Call(game,"ResetToMenu");Call(game,"OpenModeSelection");
        Check(((RectTransform)Get(game,"modeDropdownClip")).rect.height==496,"Reduced Motion opens complete dropdown immediately");
        Call(game,"CloseProtocolDropdown");Check(!((GameObject)Get(game,"modeSelectionScreen")).activeSelf,"Reduced Motion dismisses dropdown immediately");
        Set(game,"reduceMotionEnabled",false);Call(game,"ResetToMenu");camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);
        Debug.Log("SKYPULSE_PRESENTATION_CAPTURES_PASS");
    }
    static void Pilot(SkyPulseNativeGame game)
    {
        var relay=(RelaySession)Get(game,"relaySession");int gate=Math.Min(relay.Passed,relay.Mission.RelayCount-1);float target=relay.Passed==relay.Mission.RelayCount?relay.Mission.ExitY:relay.Mission.RelayY[gate];
        if((float)Get(game,"birdY")<target-.65f && (float)Get(game,"birdVelocity")<0)Call(game,"Flap");
    }
}
