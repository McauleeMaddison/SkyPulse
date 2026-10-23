// Editor-only material/capture checks in the disposable QA product. Not shipped.
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulseWorldFinishChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("WORLD_FINISH_CHECK: "+message);}
    static SkyPulseWorldFinishChecks(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Isolated QA required");
        PlayerPrefs.DeleteAll();PlayerPrefs.Save();SessionState.SetBool("WorldFinishQa",true);EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool("WorldFinishQa",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("WorldFinishQa",false);new GameObject("World finish inspection").AddComponent<SkyPulseWorldExpansionRunner>().Begin(Capture());
    }
    static MeshRenderer Copy(Transform parent,MeshRenderer source,Vector3 position,Vector3 scale)
    {
        var o=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer));o.layer=29;o.transform.SetParent(parent,false);o.transform.localPosition=position;o.transform.localScale=scale;
        o.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
        var renderer=o.GetComponent<MeshRenderer>();renderer.sharedMaterial=source.sharedMaterial;
        var block=new MaterialPropertyBlock();source.GetPropertyBlock(block);renderer.SetPropertyBlock(block);return renderer;
    }
    static Color32[] Read(Camera camera,RenderTexture rt,Texture2D image,string path)
    {
        camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=old;
        if(path!=null)File.WriteAllBytes(path,image.EncodeToPNG());return image.GetPixels32();
    }
    static int Changed(Color32[] a,Color32[] b,int tolerance)
    {
        int count=0;for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>tolerance)count++;return count;
    }
    static void CompareReference(string reference,string relative,Color32[] pixels,bool unchanged)
    {
        if(string.IsNullOrEmpty(reference))return;
        var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(Path.Combine(reference,relative)));
        int count=Changed(pixels,texture.GetPixels32(),3);UnityEngine.Object.Destroy(texture);
        Check(unchanged?count<20:count>1000,(unchanged?"Neon reference preserved: ":"New finish changes visible pixels: ")+relative+" / "+count);
    }
    static IEnumerator Capture()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();if(game==null)game=new GameObject("QA game").AddComponent<SkyPulseNativeGame>();game.enabled=false;
        Set(game,"hapticsEnabled",false);Set(game,"reduceMotionEnabled",false);Call(game,"StartFlight");Call(game,"UpdateCinematicPresentation",1f);
        bool referenceOnly=Environment.GetEnvironmentVariable("SKYPULSE_FINISH_REFERENCE")=="1";
        string reference=Environment.GetEnvironmentVariable("SKYPULSE_FINISH_BASELINE");string output=Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");Directory.CreateDirectory(output);
        var worlds=(Array)typeof(SkyPulseNativeGame).GetField("Worlds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        var env=(SkyPulseFlightEnvironment)Get(game,"flightEnvironment");var sky=env.WorldCamera;env.SetViewport(18f*512/896);
        foreach(var r in env.WorldScene.GetComponentsInChildren<MeshRenderer>(true))r.enabled=r.name=="3D atmospheric sky dome";
        var oldSky=sky.targetTexture;var target=new RenderTexture(512,896,24){antiAliasing=2};target.Create();sky.targetTexture=target;
        var image=new Texture2D(512,896,TextureFormat.RGB24,false);
        var stage=new GameObject("Pipe finish inspection");stage.transform.position=new Vector3(21000,21000,0);
        var cameraObject=new GameObject("Pipe finish camera",typeof(Camera));cameraObject.transform.SetParent(stage.transform,false);cameraObject.transform.localPosition=new Vector3(0,0,-8);
        var pipeCamera=cameraObject.GetComponent<Camera>();pipeCamera.enabled=false;pipeCamera.cullingMask=1<<29;pipeCamera.orthographic=true;pipeCamera.orthographicSize=3.6f;pipeCamera.aspect=512f/896;pipeCamera.clearFlags=CameraClearFlags.SolidColor;pipeCamera.backgroundColor=new Color(.025f,.039f,.064f);pipeCamera.targetTexture=target;
        for(int world=0;world<12;world++)
        {
            var definition=worlds.GetValue(world);Set(game,"routeWorldIndex",world);Set(game,"routeWorld",definition);Call(game,"ApplyRouteWorldVisuals");Call(game,"UpdateDepthPresentation",0f);
            env.ResetView();env.SetReflection(world,((Sprite)Call(game,"LoadSprite",(string)Get(definition,"BackgroundPath"))).texture);
            env.Present(world,0,0,1,(Color)Get(definition,"Accent"),(Color)Get(definition,"Floor"),false);
            string name=world.ToString("00");var skyPixels=Read(sky,target,image,Path.Combine(output,"sky-"+name+".png"));CompareReference(reference,"sky-"+name+".png",skyPixels,world==0);
            var copies=new GameObject("World pipe pair");copies.transform.SetParent(stage.transform,false);
            int side=0;
            foreach(var item in (System.Collections.IEnumerable)Get(game,"routeDepth"))
            {
                if(side==2)break;var surface=Get(item,"Surface");var body=(BoxCollider2D)Get(surface,"BodyCollider");var cap=(BoxCollider2D)Get(surface,"CapCollider");
                bool upper=side==0;float direction=upper?1:-1,x=upper?.95f:-.95f,y=upper?.60f:-.60f;
                var capRenderer=(MeshRenderer)Get(item,"CapRenderer");var bodyRenderer=(MeshRenderer)Get(item,"BodyRenderer");
                Copy(copies.transform,capRenderer,new Vector3(x,y,0),new Vector3(cap.size.x,cap.size.y,cap.size.x));
                float near=y+direction*cap.size.y*.22f,far=y+direction*5;
                Copy(copies.transform,bodyRenderer,new Vector3(x,(near+far)*.5f,0),new Vector3(body.size.x,Mathf.Abs(far-near),body.size.x));side++;
            }
            var shaft=(Material)Get(game,"routeMetal");var collar=(Material)Get(game,"routeCapMetal");shaft.SetFloat("_Clock",0);collar.SetFloat("_Clock",0);
            var first=Read(pipeCamera,target,image,Path.Combine(output,"pipe-"+name+".png"));CompareReference(reference,"pipe-"+name+".png",first,world==0);
            if(!referenceOnly)
            {
                string motion=Path.Combine(output,"pipes",name);Directory.CreateDirectory(motion);int changed=0;
                for(int frame=0;frame<30;frame++)
                {
                    float time=frame/15f;shaft.SetFloat("_Clock",time);collar.SetFloat("_Clock",time);
                    var pixels=Read(pipeCamera,target,image,Path.Combine(motion,frame.ToString("000")+".png"));changed=Mathf.Max(changed,Changed(first,pixels,20));yield return null;
                }
                Check(changed>100,"Animated pipe lighting is visible in world "+world+" / "+changed+" pixels");
                env.Present(world,0,8,1,(Color)Get(definition,"Accent"),(Color)Get(definition,"Floor"),false);
                var moved=Read(sky,target,image,null);Check(Changed(skyPixels,moved,3)>100,"Atmosphere animates gently in world "+world);
                env.Present(world,0,8,1,(Color)Get(definition,"Accent"),(Color)Get(definition,"Floor"),true);var reduced=Read(sky,target,image,null);
                env.Present(world,20,19,1,(Color)Get(definition,"Accent"),(Color)Get(definition,"Floor"),true);Check(Changed(reduced,Read(sky,target,image,null),0)==0,"Reduced Motion freezes the complete atmosphere "+world);
                Color32[] timed=null;
                foreach(int fps in new[]{30,60,120})
                {
                    for(int frame=0;frame<=fps;frame++)env.Present(world,0,frame/(float)fps,1,(Color)Get(definition,"Accent"),(Color)Get(definition,"Floor"),false);
                    var pixels=Read(sky,target,image,null);
                    if(timed!=null)Check(Changed(timed,pixels,0)==0,"Atmosphere agrees at one second / "+fps+" Hz / world "+world);
                    timed=pixels;
                }
            }
            UnityEngine.Object.Destroy(copies);yield return null;
        }
        if(!referenceOnly)
        {
            foreach(var pair in new[]{new[]{0,1},new[]{5,6},new[]{11,0}})
            {
                var from=worlds.GetValue(pair[0]);var to=worlds.GetValue(pair[1]);env.ResetView();
                env.Present(pair[0],0,2,1,(Color)Get(from,"Accent"),(Color)Get(from,"Floor"),false);var before=Read(sky,target,image,null);
                env.Present(pair[1],0,2,0,(Color)Get(to,"Accent"),(Color)Get(to,"Floor"),false);Check(Changed(before,Read(sky,target,image,null),0)==0,"Sky transition begins at outgoing palette "+pair[0]+" to "+pair[1]);
                env.Present(pair[1],0,2,.99999f,(Color)Get(to,"Accent"),(Color)Get(to,"Floor"),false);var near=Read(sky,target,image,null);
                env.Present(pair[1],0,2,1,(Color)Get(to,"Accent"),(Color)Get(to,"Floor"),false);Check(Changed(near,Read(sky,target,image,null),3)<20,"Sky transition has no final palette snap "+pair[0]+" to "+pair[1]);
            }
        }
        foreach(string name in new[]{"WorldAtmosphere","EndlessRouteMetal"})foreach(var message in ShaderUtil.GetShaderMessages(Resources.Load<Shader>("SkyPulse/Shaders/"+name)))if(message.severity.ToString()=="Error")throw new Exception(message.message);
        sky.targetTexture=oldSky;pipeCamera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(stage);
        Debug.Log("SKYPULSE_WORLD_FINISH_PASS");EditorApplication.Exit(0);
    }
}
