// Real shader/pixel checks in the disposable QA product, not phone validation.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SkyPulse.Mobile;

public static class SkyPulsePipeFinishChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);Debug.Log("PIPE_FINISH_CHECK: "+s);}
    static MeshRenderer Copy(Transform parent,MeshRenderer source)
    {
        var o=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);o.layer=29;
        o.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
        var r=o.GetComponent<MeshRenderer>();r.sharedMaterial=source.sharedMaterial;
        var properties=new MaterialPropertyBlock();source.GetPropertyBlock(properties);r.SetPropertyBlock(properties);return r;
    }
    static Vector3 Rgb(Color c)=>new Vector3(c.r,c.g,c.b);
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");Call(game,"StartFlight");Call(game,"UpdateDepthPresentation",0f);
        var stage=new GameObject("Isolated pipe junction QA");stage.transform.position=new Vector3(21000,21000,0);
        var probe=GameObject.CreatePrimitive(PrimitiveType.Quad);probe.transform.SetParent(stage.transform,false);probe.layer=29;probe.transform.localPosition=new Vector3(0,0,4);probe.transform.localScale=new Vector3(30,30,1);
        var background=new Color(.16f,.47f,.56f);var backing=new Material(Shader.Find("Unlit/Color")){color=background,renderQueue=2010};probe.GetComponent<MeshRenderer>().sharedMaterial=backing;
        var cameraObject=new GameObject("Pipe join camera",typeof(Camera));cameraObject.transform.SetParent(stage.transform,false);cameraObject.transform.localPosition=new Vector3(0,0,-6);
        var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1.5f;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=background;
        var target=new RenderTexture(960,640,24);target.Create();camera.targetTexture=target;var pixels=new Texture2D(960,640,TextureFormat.RGB24,false);var prior=RenderTexture.active;
        var shaft=(Material)Get(game,"routeMetal");var collar=(Material)Get(game,"routeCapMetal");float clock=shaft.GetFloat("_Clock");
        string folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"pipe-finish");Directory.CreateDirectory(folder);
        Action read=()=>{camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,960,640),0,0);pixels.Apply();};
        try
        {
            foreach(string side in new[]{"Top","Bottom"})
            {
                var pair=((Array)Get(game,"pipePool")).GetValue(0);var surface=Get(pair,side);
                var body=(BoxCollider2D)Get(surface,"BodyCollider");var cap=(BoxCollider2D)Get(surface,"CapCollider");var source=((IEnumerable)Get(game,"routeDepth")).Cast<object>().First(v=>ReferenceEquals(Get(v,"Surface"),surface));
                var bodyRenderer=Copy(stage.transform,(MeshRenderer)Get(source,"BodyRenderer"));var capRenderer=Copy(stage.transform,(MeshRenderer)Get(source,"CapRenderer"));
                var bodyPosition=body.transform.localPosition+new Vector3(body.offset.x,body.offset.y,0);var capPosition=cap.transform.localPosition+new Vector3(cap.offset.x,cap.offset.y,0);
                float direction=bodyPosition.y>capPosition.y?1:-1;float far=bodyPosition.y+direction*body.size.y*.5f-capPosition.y;
                var physicalBody=body.size;var physicalCap=cap.size;
                capRenderer.transform.localScale=new Vector3(cap.size.x,cap.size.y,cap.size.x);
                Action<float> seat=near=>{bodyRenderer.transform.localPosition=new Vector3(0,(far+near)*.5f,0);bodyRenderer.transform.localScale=new Vector3(body.size.x,Mathf.Abs(far-near),body.size.x);};
                Func<int> gaps=()=>{
                    int holes=0;
                    for(int x=0;x<31;x++)for(int y=0;y<61;y++)
                    {
                        var point=stage.transform.TransformPoint(new Vector3(Mathf.Lerp(-.43f,.43f,x/30f)*body.size.x,direction*cap.size.y*Mathf.Lerp(.1f,.7f,y/60f),0));
                        var at=camera.WorldToScreenPoint(point);var colour=pixels.GetPixel((int)at.x,(int)at.y);
                        if(Vector3.Distance(Rgb(colour),Rgb(background))<.035f)holes++;
                    }
                    return holes;
                };
                seat(direction*(cap.size.y*.5f-.04f));read();int before=gaps();File.WriteAllBytes(Path.Combine(folder,side+"-old-join.png"),pixels.EncodeToPNG());
                seat(direction*cap.size.y*.22f);read();int after=gaps();File.WriteAllBytes(Path.Combine(folder,side+"-seated-join.png"),pixels.EncodeToPNG());
                Check(before>10,"Negative control reproduces daylight at the old "+side+" join: "+before);
                Check(after==0,"Seated "+side+" collar has no background holes across the shaft: "+after);
                Check(body.size==physicalBody&&cap.size==physicalCap,"Visual join does not alter "+side+" hitboxes");
                shaft.SetFloat("_Clock",0);collar.SetFloat("_Clock",0);read();var still=pixels.GetPixels();int changed=0;
                foreach(float phase in new[]{.25f,.5f,.75f})
                {
                    shaft.SetFloat("_Clock",phase);collar.SetFloat("_Clock",phase);read();var now=pixels.GetPixels();
                    int count=0;for(int i=0;i<now.Length;i++)if(Vector3.Distance(Rgb(now[i]),Rgb(still[i]))>.10f)count++;changed=Mathf.Max(changed,count);
                    File.WriteAllBytes(Path.Combine(folder,side+"-lights-"+phase.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png"),pixels.EncodeToPNG());
                }
                Check(changed>100,"Travelling "+side+" LED highlights produce visible pixel change without geometry motion: "+changed);
                UnityEngine.Object.Destroy(bodyRenderer.gameObject);UnityEngine.Object.Destroy(capRenderer.gameObject);yield return null;
            }
        }
        finally
        {
            shaft.SetFloat("_Clock",clock);collar.SetFloat("_Clock",clock);RenderTexture.active=prior;camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(backing);UnityEngine.Object.Destroy(stage);
        }
        Call(game,"ResetToMenu");Debug.Log("SKYPULSE_PIPE_FINISH_PASS");
    }
}

[InitializeOnLoad]
public static class SkyPulsePipeFinishTargeted
{
    static SkyPulsePipeFinishTargeted(){EditorApplication.update+=Tick;}
    public static void Capture(){SessionState.SetBool("SkyPulsePipeMovieOnly",true);Run();}
    public static void Run(){if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");PlayerPrefs.DeleteAll();PlayerPrefs.Save();SessionState.SetBool("SkyPulsePipeFinish",true);EditorApplication.EnterPlaymode();}
    static void Tick()
    {
        if(!SessionState.GetBool("SkyPulsePipeFinish",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("SkyPulsePipeFinish",false);new GameObject("Pipe presentation QA").AddComponent<SkyPulseWorldExpansionRunner>().Begin(Execute());
    }
    static IEnumerator Execute()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();if(game==null)game=new GameObject("QA game").AddComponent<SkyPulseNativeGame>();game.enabled=false;
        bool movieOnly=SessionState.GetBool("SkyPulsePipeMovieOnly",false);SessionState.SetBool("SkyPulsePipeMovieOnly",false);
        if(!movieOnly)
        {
            yield return SkyPulsePipeFinishChecks.Live(game);
            yield return (IEnumerator)typeof(SkyPulseDimensionalChecks).GetMethod("Environment",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{game});
        }
        yield return SkyPulseFlightPolishChecks.Capture(game);
        Debug.Log("SKYPULSE_PIPE_FINISH_TARGETED_PASS");EditorApplication.Exit(0);
    }
}
