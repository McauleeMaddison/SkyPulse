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
        foreach(int world in new[]{0,4,7,8,10})
        {
            var definition=worlds.GetValue(world);var type=definition.GetType();
            material.SetColor("_Tint",(Color)type.GetField("Accent").GetValue(definition));material.SetColor("_Haze",(Color)type.GetField("Floor").GetValue(definition));
            material.SetFloat("_Design",world);material.SetFloat("_Visibility",1);material.SetFloat("_Clock",2);
            var sprite=(Sprite)Call(game,"LoadSprite",(string)type.GetField("BackgroundPath").GetValue(definition));material.SetTexture("_Environment",sprite.texture);
            proxy.GetComponent<MeshFilter>().sharedMesh=((Mesh[])Get(env,"silhouettes"))[world];
            int frames=world==0||world==4?90:1;
            string motion=Path.Combine(folder,world.ToString("00"));Directory.CreateDirectory(motion);
            for(int frame=0;frame<frames;frame++)
            {
                proxy.transform.localRotation=Quaternion.Euler(0,-22+frame*.48f,0);material.SetFloat("_Clock",2+frame/30f);camera.Render();
                var prior=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1000,1200),0,0);image.Apply();RenderTexture.active=prior;
                File.WriteAllBytes(Path.Combine(motion,frame.ToString("000")+".png"),image.EncodeToPNG());if(frame==0)File.WriteAllBytes(Path.Combine(folder,"world-"+world.ToString("00")+".png"),image.EncodeToPNG());yield return null;
            }
        }
        foreach(var message in ShaderUtil.GetShaderMessages(material.shader))if(message.severity.ToString()=="Error")throw new Exception(message.message);
        camera.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(proxy);
        Debug.Log("SKYPULSE_DISTRICT_DETAIL_PASS");EditorApplication.Exit(0);
    }
}
