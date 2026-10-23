// Actual Unity world/pipe previews. QA selects each route directly, then uses
// normal tap buffering and simulation. This does not change shipping progression.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseWorldStyleChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool good,string text){if(!good)throw new Exception(text);Debug.Log("WORLD_STYLE_CHECK: "+text);}
    public static IEnumerator Capture(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        game.enabled=false;Set(game,"hapticsEnabled",false);Set(game,"reduceMotionEnabled",false);
        var camera=(Camera)Get(game,"flightCamera");var old=camera.targetTexture;var rt=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=rt;
        var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var image=new Texture2D(750,1334,TextureFormat.RGB24,false);var worlds=(Array)typeof(SkyPulseNativeGame).GetField("Worlds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        string folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"world-styles");Directory.CreateDirectory(folder);
        for(int world=0;world<12;world++)
        {
            Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");Call(game,"StartFlight");
            Set(game,"score",world*15);((Text)Get(game,"hudScoreText")).text=(world*15).ToString();
            Set(game,"routeWorldIndex",world);Set(game,"routeWorld",worlds.GetValue(world));Call(game,"ApplyRouteWorldVisuals");Call(game,"ResetFlightEnvironment");
            float spacing=(float)Call(game,"RoutePipeSpacing");int slot=0;
            foreach(var pipe in (Array)Get(game,"pipePool"))Call(game,"ConfigurePipe",pipe,3.6f+(slot++)*spacing);
            Call(game,"UpdateCinematicPresentation",0f);Call(game,"RefreshViewportDecor");
            var bodies=(Mesh[])Get(game,"routeBodyThemes");var caps=(Mesh[])Get(game,"routeCapThemes");
            foreach(var mesh in new[]{bodies[world],caps[world]})Check(mesh.bounds.size.x<=1.001f&&mesh.bounds.size.x>=.97f&&Mathf.Abs(mesh.bounds.size.y-1)<.001f,"Pipe mesh stays within tested dimensions: "+world+" / "+mesh.name);
            var first=((IList)Get(game,"routeDepth"))[0];Check(((MeshFilter)Get(first,"BodyFilter")).sharedMesh==bodies[world]&&((MeshFilter)Get(first,"CapFilter")).sharedMesh==caps[world],"Visible shaft and collar bind to the current world mesh "+world);
            var material=(Material)Get(game,"routeMetal");Check(Mathf.Abs(material.GetFloat("_Theme")-world)<.001f&&material.shader.isSupported,"World-specific pipe material is active "+world);
            string frames=Path.Combine(folder,world.ToString("00"));Directory.CreateDirectory(frames);
            for(int frame=0;frame<40;frame++)
            {
                for(int step=0;step<12;step++)
                {
                    float nearest=float.MaxValue,target=0;
                    foreach(var pipe in (Array)Get(game,"pipePool")){float x=(float)Get(pipe,"X");if(!(bool)Get(pipe,"Passed")&&x>=-3.7f&&x<nearest){nearest=x;target=(float)Get(pipe,"GapCenter");}}
                    if((float)Get(game,"birdY")<target-.55f&&(float)Get(game,"birdVelocity")<0)Call(game,"BufferFlapInput");Call(game,"SimulateFlight",1f/120f);
                }
                Check(Get(game,"state").ToString()=="Playing","World preview follows collision-checked normal flight: "+world+" / "+frame);
                Set(game,"ambientTime",(float)Get(game,"ambientTime")+.1f);Call(game,"UpdateAmbientVisuals");Call(game,"UpdateGameplayWingState",.1f);Call(game,"UpdateBirdWingMotion");Call(game,"UpdateCinematicPresentation",.1f);
                Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,750,1334),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(frames,frame.ToString("000")+".png"),image.EncodeToPNG());if(frame==14)File.WriteAllBytes(Path.Combine(folder,"world-"+world.ToString("00")+".png"),image.EncodeToPNG());RenderTexture.active=active;yield return null;
            }
        }
        Call(game,"StartFlight");Call(game,"UpdateDepthPresentation",0f);Check(((Material)Get(game,"routeMetal")).GetFloat("_ThemeAmount")==0,"Retry into Neon City restores its original cyan/pink pipe art");
        Call(game,"BeginWorldTransition",1);Call(game,"UpdateDepthPresentation",0f);
        Check(((Material)Get(game,"routeMetal")).GetFloat("_Theme")==0,"Departing pipes retain their own design while fading");
        Call(game,"UpdateWorldTransition",.65f);Call(game,"UpdateDepthPresentation",0f);
        Check(((Material)Get(game,"routeMetal")).GetFloat("_Theme")==1,"Incoming world binds its new pipe geometry after departure fades");
        camera.targetTexture=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(image);Call(game,"ResetToMenu");Debug.Log("SKYPULSE_WORLD_STYLES_PASS");
    }
}
