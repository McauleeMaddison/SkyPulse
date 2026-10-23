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
}
