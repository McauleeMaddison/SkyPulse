using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;
[InitializeOnLoad]
public static class SkyPulseBetaScreens
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static SkyPulseBetaScreens(){EditorApplication.update+=Tick;}
    public static void Run(){SessionState.SetBool("BetaScreens",true);EditorApplication.EnterPlaymode();}
    static void Tick()
    {
        if(!SessionState.GetBool("BetaScreens",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("BetaScreens",false);
        try{Capture();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Capture()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();
        if(game==null)game=new GameObject("Beta render fixture").AddComponent<SkyPulseNativeGame>();
        game.enabled=false;
        var initialSkins=(Array)typeof(SkyPulseNativeGame).GetField("Skins",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        Set(game,"equippedSkin",initialSkins.GetValue(0));Call(game,"ApplyEquippedVisuals");
        var folder=Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT")??"/private/tmp/skypulse-beta-screens";Directory.CreateDirectory(folder);
        var camera=(Camera)Get(game,"flightCamera");
        var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f;
        foreach(var size in new[]{new Vector2Int(540,960),new Vector2Int(660,1434)})
        {
            var rt=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);
            foreach(var scenario in new[]{"home","privacy","hangar","tech","purchase","pause","perfect","crystal","result"})
            {
                Call(game,"CloseUnlockReveal");Call(game,"ClosePurchaseModal");((GameObject)Get(game,"privacyScreen")).SetActive(false);
                Call(game,"ResetToMenu");
                if(scenario=="privacy")((GameObject)Get(game,"privacyScreen")).SetActive(true);
                if(scenario=="hangar")Call(game,"OpenHangar");
                if(scenario=="tech"||scenario=="purchase")Call(game,"OpenUpgrades");
                if(scenario=="purchase")
                {
                    var items=(Array)typeof(SkyPulseNativeGame).GetField("Upgrades",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
                    ((IDictionary)Get(game,"upgradeLevels")).Clear();Set(game,"crystals",9999);Call(game,"SelectUpgrade",items.GetValue(0));
                }
                if(scenario=="pause"||scenario=="perfect"||scenario=="crystal"||scenario=="result")
                {
                    Call(game,"StartFlight");
                    if(scenario=="pause")Call(game,"PauseFlight");
                    if(scenario=="perfect")Call(game,"ShowScoreBurst",1,true);
                    if(scenario=="crystal")Call(game,"ShowCrystalBurst",12,false);
                    if(scenario=="result")
                    {
                        Set(game,"score",27);Call(game,"EndFlight");Set(game,"state",Enum.Parse(Get(game,"state").GetType(),"GameOver"));Call(game,"RefreshScreens");
                    }
                }
                CaptureFrame(game,camera,size,Path.Combine(folder,$"{size.x}-{scenario}.png"));
            }
            var skins=(Array)typeof(SkyPulseNativeGame).GetField("Skins",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            foreach(var skin in skins)
            {
                Call(game,"ResetToMenu");Call(game,"ShowUnlockReveal",skin);Call(game,"UpdateUnlockReveal",.5f);
                CaptureFrame(game,camera,size,Path.Combine(folder,$"{size.x}-unlock-{Get(skin,"Id")}.png"));Call(game,"CloseUnlockReveal");
            }
            camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);
        }
        Debug.Log("SKYPULSE_BETA_SCREENS_PASS "+folder);
    }
    static void CaptureFrame(SkyPulseNativeGame game,Camera camera,Vector2Int size,string path)
    {
        Call(game,"UpdateMenuBird",.1f);
        Canvas.ForceUpdateCanvases();
        var safe=(RectTransform)Get(game,"safeAreaRoot");safe.anchorMin=new Vector2(0,.035f);safe.anchorMax=new Vector2(1,.935f);safe.offsetMin=safe.offsetMax=Vector2.zero;
        Canvas.ForceUpdateCanvases();Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();
        camera.Render();RenderTexture.active=camera.targetTexture;
        var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=null;
    }
}
