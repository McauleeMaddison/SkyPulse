// Disposable Editor checks for the actual input edge, sprite and fixed-step boundary.
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseEverydayControlsChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    static void Check(bool good,string note){if(!good)throw new Exception(note);Debug.Log("EVERYDAY_INPUT_CHECK: "+note);}
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        game.enabled=false;Set(game,"hapticsEnabled",false);var camera=(Camera)Get(game,"flightCamera");
        var skins=(Array)typeof(SkyPulseNativeGame).GetField("Skins",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        int frame=Time.frameCount+50;float now=Time.unscaledTime+2;
        foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay})foreach(var skin in skins)
        {
            Call(game,"ResetToMenu");Set(game,"equippedSkin",skin);
            if(mode==SkyPulseMode.EndlessRoute)Call(game,"StartFlight");else Call(game,"LaunchMission",mode,0);
            var controller=(SkyPulseInputFence)Get(game,"gameplayInput");controller.ReleaseAll();
            string method=mode==SkyPulseMode.EndlessRoute?"SubmitEndlessPress":"SubmitRelayPress";
            var frames=(Sprite[])Get(game,"flapFrameBirdSprites");var renderer=(SpriteRenderer)Get(game,"birdRenderer");var settled=renderer.sprite;
            Check((int)Get(game,"activeFlapFrameIndex")==frames.Length-1,"Launch keeps the bird settled until an input: "+mode+" / "+Get(skin,"Id"));
            Check((bool)Call(game,method,100,frame++,now,true,camera.pixelRect.center),"Fresh play-area finger is accepted: "+mode+" / "+Get(skin,"Id"));
            Check((float)Get(game,"birdVelocity")>0&&(float)Get(game,"wingTimer")==0,"Accepted edge applies the original impulse before simulation: "+mode);
            Check(renderer.sprite==frames[frames.Length-2]&&renderer.sprite!=settled&&SkyPulseBirdRosterPreview.NativeWingLift(game)>0,"Adjacent native drawing and solid-wing target change on the input edge: "+mode+" / "+Get(skin,"Id"));
            Check(!(bool)Call(game,method,100,frame++,now+.01f,true,camera.pixelRect.center),"Held contact cannot generate another impulse");
            Check(!(bool)Call(game,method,-1,frame++,now+.02f,false,camera.pixelRect.center),"Simulated mouse cannot duplicate the touch");
            Call(game,"ReleaseGameplayPointer",100);Set(game,"birdVelocity",-2f);
            Check((bool)Call(game,method,100,frame++,now+.03f,true,camera.pixelRect.center)&&(float)Get(game,"birdVelocity")>0,"A separately released tap stays responsive inside the old 70ms polling lockout");
            Set(game,"birdVelocity",-1f);Call(game,"ConsumeBufferedFlap");
            Check((float)Get(game,"birdVelocity")==-1f&&(float)Get(game,"bufferedFlapUntil")<0,"Fixed-step input cannot replay an already applied edge");
            now+=1;
        }
        Call(game,"ResetToMenu");Call(game,"StartFlight");yield return null;Canvas.ForceUpdateCanvases();
        var hud=(GameObject)Get(game,"hudScreen");var pause=hud.GetComponentsInChildren<Button>().Single();
        var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();Camera uiCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        var at=RectTransformUtility.WorldToScreenPoint(uiCamera,pause.transform.position);float velocity=(float)Get(game,"birdVelocity");
        Check(!(bool)Call(game,"SubmitEndlessPress",201,frame++,now,true,at)&&(float)Get(game,"birdVelocity")==velocity,"Endless pause hit target cannot also flap");
        Check(!(bool)Call(game,"SubmitEndlessPress",202,frame++,now,true,new Vector2(-5,-5)),"Off-viewport press cannot flap");
        Call(game,"PauseFlight");Check(!(bool)Call(game,"SubmitEndlessPress",203,frame++,now,true,camera.pixelRect.center),"Paused Endless ignores gameplay input");
        Call(game,"ResumeFlight");
        Check(!(bool)Call(game,"SubmitEndlessPress",204,Time.frameCount,now,true,camera.pixelRect.center),"Resume-frame queued input is fenced");
        Call(game,"ResetToMenu");
        Debug.Log("SKYPULSE_EVERYDAY_INPUT_PASS");
    }
}
