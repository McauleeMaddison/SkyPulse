// Model inspection of the same mesh/material used by flight. No generated artwork.
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulseDistrictDetailChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static SkyPulseDistrictDetailChecks(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Isolated model review required");
        SessionState.SetBool("DistrictDetailQa",true);EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool("DistrictDetailQa",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("DistrictDetailQa",false);new GameObject("District inspection runner").AddComponent<SkyPulseWorldExpansionRunner>().Begin(Capture());
    }
    static IEnumerator Capture()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();if(game==null)game=new GameObject("QA game").AddComponent<SkyPulseNativeGame>();game.enabled=false;
        typeof(SkyPulseNativeGame).GetField("hapticsEnabled",F).SetValue(game,false);Call(game,"StartFlight");Call(game,"UpdateCinematicPresentation",1f);
        var env=(SkyPulseFlightEnvironment)Get(game,"flightEnvironment");var camera=env.WorldCamera;var material=new Material(((Material[])Get(env,"surfaces"))[1]);
        foreach(var renderer in env.WorldScene.GetComponentsInChildren<MeshRenderer>(true))renderer.enabled=false;
        var proxy=new GameObject("Actual district inspection",typeof(MeshFilter),typeof(MeshRenderer));proxy.layer=30;proxy.transform.SetParent(env.WorldScene,false);proxy.GetComponent<MeshRenderer>().sharedMaterial=material;
        camera.transform.localPosition=new Vector3(7.5f,6.3f,-14.8f);camera.transform.LookAt(env.WorldScene.TransformPoint(new Vector3(0,1.50f,0)));camera.fieldOfView=33;camera.aspect=5f/6;
        camera.backgroundColor=new Color(.025f,.039f,.064f);var rt=new RenderTexture(1000,1200,24){antiAliasing=2};rt.Create();camera.targetTexture=rt;
        var image=new Texture2D(1000,1200,TextureFormat.RGB24,false);var worlds=(Array)typeof(SkyPulseNativeGame).GetField("Worlds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        string folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"district-detail");Directory.CreateDirectory(folder);
        // Include both arrangements actually used by flight, rather than only
        // the five examples from the first visual review. Inspection cameras
        // never change the game's camera or the owner's save.
        for(int variant=0;variant<2;variant++)
        for(int world=0;world<worlds.Length;world++)
        {
            var definition=worlds.GetValue(world);var type=definition.GetType();
            material.SetColor("_Tint",(Color)type.GetField("Accent").GetValue(definition));material.SetColor("_Haze",(Color)type.GetField("Floor").GetValue(definition));
            material.SetFloat("_Design",world);material.SetFloat("_Visibility",1);material.SetFloat("_Clock",2);
            typeof(SkyPulseFlightEnvironment).Assembly.GetType("SkyPulse.Mobile.SkyPulseWorldArchitecture").GetMethod("ConfigureBeaconLighting",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{material,world});
            var sprite=(Sprite)Call(game,"LoadSprite",(string)type.GetField("BackgroundPath").GetValue(definition));material.SetTexture("_Environment",sprite.texture);
            var mesh=((Mesh[])Get(env,variant==0?"silhouettes":"residential"))[world];
            if(mesh==null||mesh.vertexCount==0)throw new Exception("Missing district model: "+world+" / "+variant);
            proxy.GetComponent<MeshFilter>().sharedMesh=mesh;
            FrameModel(camera,env.WorldScene,mesh.bounds);
            int frames=variant==0?90:1;
            string arrangement=variant==0?"civic":"residential";
            string variantFolder=Path.Combine(folder,arrangement);Directory.CreateDirectory(variantFolder);
            string motion=Path.Combine(variantFolder,world.ToString("00"));Directory.CreateDirectory(motion);
            for(int frame=0;frame<frames;frame++)
            {
                proxy.transform.localRotation=Quaternion.Euler(0,-22+frame*.48f,0);material.SetFloat("_Clock",2+frame/30f);camera.Render();
                var prior=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1000,1200),0,0);image.Apply();RenderTexture.active=prior;
                if(frame==0)
                {
                    var lit=image.GetPixels32();material.SetFloat("_WindowGlow",0);camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1000,1200),0,0);image.Apply();RenderTexture.active=prior;
                    var dark=image.GetPixels32();int changed=0,modelPixels=0;var background=lit[0];
                    for(int pixel=0;pixel<lit.Length;pixel++)
                    {
                        if(Math.Abs(lit[pixel].r-background.r)+Math.Abs(lit[pixel].g-background.g)+Math.Abs(lit[pixel].b-background.b)>8)modelPixels++;
                        if(Math.Abs(lit[pixel].r-dark[pixel].r)+Math.Abs(lit[pixel].g-dark[pixel].g)+Math.Abs(lit[pixel].b-dark[pixel].b)>15)changed++;
                    }
                    if(changed<60||changed>modelPixels*.30f)throw new Exception("Window lighting must be visible and selective: "+world+" / "+variant+" / "+changed+" of "+modelPixels);
                    Debug.Log("DISTRICT_WINDOW_CHECK: "+world+" / "+variant+" / "+changed+" illuminated pixels of "+modelPixels);
                    material.SetFloat("_WindowGlow",1);camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1000,1200),0,0);image.Apply();RenderTexture.active=prior;
                    var glowing=image.GetPixels32();material.SetFloat("_BeaconGlow",0);camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1000,1200),0,0);image.Apply();RenderTexture.active=prior;
                    var noSpill=image.GetPixels32();int glowPixels=0;
                    for(int pixel=0;pixel<glowing.Length;pixel++)if(Math.Abs(glowing[pixel].r-noSpill[pixel].r)+Math.Abs(glowing[pixel].g-noSpill[pixel].g)+Math.Abs(glowing[pixel].b-noSpill[pixel].b)>15)glowPixels++;
                    if(glowPixels<30)throw new Exception("Star LEDs must visibly illuminate the foundation: "+world+" / "+variant+" / "+glowPixels);
                    Debug.Log("DISTRICT_BEACON_CHECK: "+world+" / "+variant+" / "+glowPixels+" illuminated foundation pixels");
                    material.SetFloat("_BeaconGlow",1);camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1000,1200),0,0);image.Apply();RenderTexture.active=prior;
                }
                var png=image.EncodeToPNG();File.WriteAllBytes(Path.Combine(motion,frame.ToString("000")+".png"),png);if(frame==0)File.WriteAllBytes(Path.Combine(variantFolder,"world-"+world.ToString("00")+".png"),png);yield return null;
            }
            Debug.Log("DISTRICT_PREVIEW_CAPTURE: "+world.ToString("00")+" / "+arrangement+" / "+mesh.vertexCount+" vertices / "+frames+" frames");
        }
        foreach(var message in ShaderUtil.GetShaderMessages(material.shader))if(message.severity.ToString()=="Error")throw new Exception(message.message);
        camera.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(proxy);
        Debug.Log("SKYPULSE_DISTRICT_DETAIL_PASS");EditorApplication.Exit(0);
    }
    static void FrameModel(Camera camera,Transform scene,Bounds bounds)
    {
        // Fit the entire rotating model, including the tallest roofs and island
        // underside. This changes only the neutral inspection camera.
        var target=scene.TransformPoint(bounds.center);
        var direction=new Vector3(7.5f,4.8f,-14.8f).normalized;
        camera.transform.position=target+direction;camera.transform.LookAt(target);
        float vertical=Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad)*.87f;
        float horizontal=vertical*camera.aspect,distance=1;
        for(int angle=0;angle<=8;angle++)
        for(int corner=0;corner<8;corner++)
        {
            var local=bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
            var point=scene.TransformPoint(Quaternion.Euler(0,Mathf.Lerp(-22,22,angle/8f),0)*local)-target;
            float depth=Vector3.Dot(point,camera.transform.forward);
            distance=Mathf.Max(distance,Mathf.Abs(Vector3.Dot(point,camera.transform.up))/vertical-depth,Mathf.Abs(Vector3.Dot(point,camera.transform.right))/horizontal-depth);
        }
        camera.transform.position=target+direction*distance;
    }
}
