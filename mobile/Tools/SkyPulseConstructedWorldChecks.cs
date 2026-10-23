// Isolated Editor capture of the built world, not generated concept art.
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulseConstructedWorldChecks
{
    static SkyPulseConstructedWorldChecks(){EditorApplication.update+=Tick;}
    public static void Run(){if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Isolated QA required");PlayerPrefs.DeleteAll();PlayerPrefs.Save();SessionState.SetBool("ConstructedQa",true);EditorApplication.EnterPlaymode();}
    static void Tick()
    {
        if(!SessionState.GetBool("ConstructedQa",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("ConstructedQa",false);new GameObject("Constructed world preview").AddComponent<SkyPulseWorldExpansionRunner>().Begin(Capture());
    }
    static IEnumerator Capture()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();if(game==null)game=new GameObject("QA game").AddComponent<SkyPulseNativeGame>();
        game.enabled=false;typeof(SkyPulseNativeGame).GetField("hapticsEnabled",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,false);
        CompositeCheck(game);
        yield return SkyPulsePresentationChecks.Captures(game);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay,SkyPulseMode.BlackoutProtocol})
        {
            typeof(SkyPulseNativeGame).GetMethod("SetHomeProtocol",flags).Invoke(game,new object[]{mode});typeof(SkyPulseNativeGame).GetMethod("ResetToMenu",flags).Invoke(game,null);
            var home=(GameObject)typeof(SkyPulseNativeGame).GetField("homeScreen",flags).GetValue(game);
            var label=(Text)typeof(SkyPulseNativeGame).GetField("homeIdentity",flags).GetValue(game);
            if(label.text!=(mode==SkyPulseMode.EndlessRoute?"ENDLESS ROUTE":mode==SkyPulseMode.PulseRelay?"PULSE RELAY":"BLACKOUT PROTOCOL"))throw new Exception("Wrong Home identity");
            foreach(var face in home.GetComponentsInChildren<SkyPulseCircuitGraphic>())if(face.Surface==SkyPulseSurface.Panel&&(!face.ProjectedGlass||!face.material.shader.isSupported))throw new Exception("Home does not use supported projected glass");
        }
        Debug.Log("SKYPULSE_CONSTRUCTED_PREVIEW_PASS");EditorApplication.Exit(0);
    }
    static void CompositeCheck(SkyPulseNativeGame game)
    {
        var f=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(SkyPulseNativeGame).GetMethod("StartFlight",f).Invoke(game,null);
        var camera=(Camera)typeof(SkyPulseNativeGame).GetField("flightCamera",f).GetValue(game);var prior=camera.targetTexture;
        var rt=RenderTexture.GetTemporary(512,896,24);camera.targetTexture=rt;
        typeof(SkyPulseNativeGame).GetMethod("UpdateCinematicPresentation",f).Invoke(game,new object[]{.5f});
        var env=(SkyPulseFlightEnvironment)typeof(SkyPulseNativeGame).GetField("flightEnvironment",f).GetValue(game);
        var material=(Material)typeof(SkyPulseFlightEnvironment).GetField("compositeMaterial",f).GetValue(env);var packed=material.shader;
        if(!packed.isSupported||packed!=Resources.Load<Shader>("SkyPulse/Shaders/WorldComposite"))throw new Exception("Missing packed world shader");
        var ui=(GameObject)typeof(SkyPulseNativeGame).GetField("uiRoot",f).GetValue(game);ui.SetActive(false);
        var image=new Texture2D(512,896,TextureFormat.RGB24,false);var active=RenderTexture.active;
        material.shader=Shader.Find("Unlit/Texture");camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,512,896),0,0);image.Apply();var reference=image.GetPixels32();
        material.shader=packed;camera.Render();image.ReadPixels(new Rect(0,0,512,896),0,0);image.Apply();var result=image.GetPixels32();int differences=0;
        for(int i=0;i<reference.Length;i++)if(Math.Abs(reference[i].r-result[i].r)+Math.Abs(reference[i].g-result[i].g)+Math.Abs(reference[i].b-result[i].b)>3)differences++;
        if(differences>20)throw new Exception("Packed composite changed rendered world pixels: "+differences);
        camera.targetTexture=prior;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);ui.SetActive(true);
        typeof(SkyPulseNativeGame).GetMethod("ResetToMenu",f).Invoke(game,null);Debug.Log("SKYPULSE_WORLD_COMPOSITE_PASS: "+differences+" changed pixels");
    }
}
