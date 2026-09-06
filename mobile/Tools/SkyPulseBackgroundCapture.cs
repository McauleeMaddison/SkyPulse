using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SkyPulse.Mobile;
[InitializeOnLoad]
public static class SkyPulseBackgroundCapture
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static SkyPulseBackgroundCapture(){EditorApplication.update+=Tick;}
    public static void Run(){SessionState.SetBool("BackgroundCapture",true);EditorApplication.EnterPlaymode();}
    static void Tick()
    {
        if(!SessionState.GetBool("BackgroundCapture",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("BackgroundCapture",false);
        try{Capture();Debug.Log("SKYPULSE_BACKGROUND_PASS");EditorApplication.Exit(0);}
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Capture()
    {
        if(!Application.productName.Contains("QA"))throw new Exception("Requires isolated QA project");
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();
        if(game==null)game=new GameObject("Background fixture").AddComponent<SkyPulseNativeGame>();
        game.enabled=false;
        var folder=Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");Directory.CreateDirectory(folder);
        var worlds=(Array)typeof(SkyPulseNativeGame).GetField("Worlds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        var camera=(Camera)Get(game,"flightCamera");
        var rt=RenderTexture.GetTemporary(360,640,24);camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);
        Call(game,"RefreshViewportDecor");
        var backdrop=(SpriteRenderer)Get(game,"backgroundRenderer");
        var incoming=(SpriteRenderer)Get(game,"incomingBackground");
        var lights=(IList)Get(game,"ambientStars");
        for(int w=0;w<3;w++)
        {
            Call(game,"StartFlight");Set(game,"routeWorldIndex",w);Set(game,"routeWorld",worlds.GetValue(w));Call(game,"ApplyRouteWorldVisuals");
            Set(game,"birdY",0f);Call(game,"UpdateBird",0f);
            foreach(bool reduced in new[]{false,true})
            {
                Set(game,"reduceMotionEnabled",reduced);
                Vector3 first=Vector3.zero;
                for(int frame=0;frame<90;frame++)
                {
                    Set(game,"ambientTime",frame/15f);Call(game,"UpdateAmbientVisuals");
                    var position=((Transform)Get(lights[0],"Transform")).position;
                    if(frame==0)first=position;
                    if(reduced && position!=first)throw new Exception("Reduced Motion scenery moved");
                    if(!reduced && frame==89 && Vector3.Distance(position,first)<.5f)throw new Exception("Parallax imperceptible");
                    var bounds=backdrop.bounds;
                    if(bounds.min.y>-9f||bounds.max.y<9f||bounds.min.x>-camera.aspect*9f||bounds.max.x<camera.aspect*9f)throw new Exception("Backdrop exposes viewport edge");
                    if(!reduced || frame==0)
                    {
                        camera.Render();RenderTexture.active=rt;
                        var texture=new Texture2D(360,640,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,360,640),0,0);texture.Apply();
                        File.WriteAllBytes(Path.Combine(folder,$"world-{w}-{(reduced?"reduced":"motion")}-{frame:D3}.png"),texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                    }
                }
            }
        }
        incoming.enabled=true;Set(game,"reduceMotionEnabled",false);Call(game,"UpdateAmbientVisuals");
        if(incoming.transform.position!=backdrop.transform.position)throw new Exception("Dissolve misaligned");
        camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);
    }
}
