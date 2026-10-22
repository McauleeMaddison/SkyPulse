// Disposable Editor checks and an actual flight-loop recording, not a device run.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseFlightPolishChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);Debug.Log("FLIGHT_POLISH_CHECK: "+s);}
    public static void Trail()
    {
        var obj=new GameObject("Disposable spectral trail QA");var fx=obj.AddComponent<SkyPulseFlightEffects>();fx.Build();
        var tint=new Color(.05f,.8f,1);Vector2[] reference=null;
        foreach(int fps in new[]{30,60,120})
        {
            fx.ResetPresentation();fx.Tick(0,Vector2.zero,Vector2.right,tint,false);
            for(int f=1;f<=fps;f++)fx.Tick(1f/fps,new Vector2(0,Mathf.Sin(2f*f/fps)),Vector2.right,tint,false);
            var path=(Vector2[])Get(fx,"path");int count=(int)Get(fx,"pathCount");
            Check(count>=24&&count<=25,"Time-based history stays bounded at "+fps+" FPS");
            var points=new Vector2[24];Array.Copy(path,points,24);
            if(reference==null)reference=points;
            else for(int i=0;i<24;i++)Check(Vector2.Distance(reference[i],points[i])<.009f,"Comparable ribbon path across frame rates: "+fps+" / "+i);
            var v=(Vector3[])Get(fx,"vertices");var c=(Color[])Get(fx,"colours");var uv=(Vector2[])Get(fx,"uv");
            for(int i=1;i<count-1;i++)
            {
                Check((v[i*4]-v[(i-1)*4+3]).sqrMagnitude<.000001f&&(v[i*4+1]-v[(i-1)*4+2]).sqrMagnitude<.000001f,"Ribbon shares exact joins without overlapping block segments");
                Check(uv[i*4].x>=uv[(i-1)*4].x,"UV falloff is continuous along the ribbon");
            }
            int tip=(count-2)*4+2;
            Check((v[tip]-v[tip+1]).sqrMagnitude<.000001f&&c[tip].a==0,"Ribbon ends in a transparent point");
            Check(((Material)Get(fx,"material")).GetFloat("_Soft")==1,"Flight material feathers both sides and ends");
        }
        var shared=new Material(Resources.Load<Shader>("SkyPulse/Shaders/FlightLight"));Check(shared.GetFloat("_Soft")==0,"Heist fibres keep their existing material behaviour");UnityEngine.Object.Destroy(shared);
        fx.ResetPresentation();fx.Tick(0,new Vector2(2,1),Vector2.right,tint,true);
        var still=(Vector3[])((Vector3[])Get(fx,"vertices")).Clone();
        fx.Flap(new Vector2(2,1),tint,true);fx.Tick(.12f,new Vector2(2,1),Vector2.right,tint,true);
        Check((int)Get(fx,"pathCount")==0&&(int)Get(fx,"used")==80,"Reduced Motion has only the compact soft plume");
        var now=(Vector3[])Get(fx,"vertices");for(int i=0;i<80;i++)Check(still[i]==now[i],"Reduced Motion plume does not pulse or animate");
        fx.ResetPresentation();fx.Tick(0,new Vector2(8,6),Vector2.right,tint,false);
        Check((int)Get(fx,"pathCount")==1&&((Vector2[])Get(fx,"path"))[0]==new Vector2(8,6),"Retry discards all preceding flight samples");
        UnityEngine.Object.Destroy(obj);Debug.Log("SKYPULSE_TRAIL_GEOMETRY_PASS");
    }
    public static IEnumerator Capture(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        game.enabled=false;Set(game,"hapticsEnabled",false);Set(game,"reduceMotionEnabled",false);
        Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");Call(game,"StartFlight");
        var camera=(Camera)Get(game,"flightCamera");var before=camera.targetTexture;
        var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var rt=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=rt;
        Call(game,"RefreshViewportDecor");Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        string folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"flight-gameplay");Directory.CreateDirectory(folder);
        var poses=new HashSet<Sprite>();float lo=100,hi=-100;int taps=0;
        var bird=(SpriteRenderer)Get(game,"birdRenderer");
        const float dt=1f/30;
        int previousCapture=Time.captureFramerate;Time.captureFramerate=30;
        try
        {
            for(int frame=0;frame<360;frame++)
            {
                float nearest=float.MaxValue,target=0;
                foreach(var gate in (Array)Get(game,"pipePool"))
                {
                    float x=(float)Get(gate,"X");
                    if(!(bool)Get(gate,"Passed")&&x>=-3.7f&&x<nearest){nearest=x;target=(float)Get(gate,"GapCenter");}
                }
                float y=(float)Get(game,"birdY"),speed=(float)Get(game,"birdVelocity");
                // QA controls the normal tap buffer; no position, gate, velocity,
                // collision, score or invulnerability changes are injected.
                if(y<target-.55f&&speed<0){Call(game,"BufferFlapInput");taps++;}
                for(int step=0;step<4;step++)Call(game,"SimulateFlight",1f/120);
                if(Get(game,"state").ToString()!="Playing")throw new Exception("QA pilot crashed at frame "+frame+": "+Get(game,"lastCrashReason"));
                Set(game,"ambientTime",(float)Get(game,"ambientTime")+dt);Call(game,"UpdateAmbientVisuals");
                Call(game,"UpdateScoreBurst",dt);Call(game,"UpdateFlightFeedback",dt);Call(game,"UpdateCrystalPickupBursts",dt);
                Call(game,"UpdateGameplayWingState",dt);Call(game,"UpdateBirdWingMotion");Call(game,"UpdateFlightCoach");Call(game,"UpdateCinematicPresentation",dt);
                poses.Add(bird.sprite);y=(float)Get(game,"birdY");lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);
                camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(folder,frame.ToString("000")+".png"),image.EncodeToPNG());RenderTexture.active=active;
                yield return null;
            }
            Check(poses.Count>=5&&hi-lo>.8f&&taps>=5,"Recording contains visible authored wing strokes and real climb/fall under normal taps");
            Check((int)Get(game,"score")>=2,"Recording shows real moving gates and successful collision-checked passes");
            File.WriteAllText(Path.Combine(folder,"CAPTURE.txt"),"Actual Endless simulation, 360 frames / 12 seconds / 30 FPS / 750x1334. Editor QA tap driver; unchanged physics/collisions. Taps="+taps+", wing poses="+poses.Count+", score="+Get(game,"score")+", y range="+(hi-lo)+". Not device evidence.\n");
        }
        finally{Time.captureFramerate=previousCapture;camera.targetTexture=before;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(image);}
        Call(game,"ResetToMenu");Debug.Log("SKYPULSE_FLIGHT_GAMEPLAY_CAPTURE_PASS");
    }
}
