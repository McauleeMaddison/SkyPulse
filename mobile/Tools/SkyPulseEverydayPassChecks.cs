// Entry point for a disposable Unity clone, never for the owner's player save.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulseEverydayPassChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    static readonly List<string> checks=new List<string>();
    static string Output=>Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"everyday");
    static void Check(bool good,string note){if(!good)throw new Exception(note);checks.Add(note);Debug.Log("EVERYDAY_PASS_CHECK: "+note);}
    static SkyPulseEverydayPassChecks(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006"||Application.productName!="SkyPulseFlapQA20261110")throw new Exception("Disposable batch QA required");
        Directory.CreateDirectory(Output);Directory.CreateDirectory(Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"polish"));foreach(string file in new[]{"FAILURE.txt","VALIDATION.json"})if(File.Exists(Path.Combine(Output,file)))File.Delete(Path.Combine(Output,file));
        PlayerPrefs.DeleteAll();PlayerPrefs.Save();SkyPulseBirdRosterPreview.Select("",false);EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");SessionState.SetBool("SkyPulse.EverydayQA.Pending",true);EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool("SkyPulse.EverydayQA.Pending",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("SkyPulse.EverydayQA.Pending",false);new GameObject("Isolated everyday QA").AddComponent<SkyPulseEverydayPassRunner>().Begin(Execute());
    }
    static IEnumerator Execute()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();game.enabled=false;Set(game,"hapticsEnabled",false);((AudioSource)Get(game,"audioSource")).mute=true;
        var camera=(Camera)Get(game,"flightCamera");var target=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);
        var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        Call(game,"RefreshViewportDecor");Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();
        SkyPulseOneThumbChecks.Pure();SkyPulseReadabilityChecks.Pure();SkyPulsePickupDisplayChecks.Pure();
        yield return SkyPulseEverydayControlsChecks.Live(game);
        yield return Selection(game,camera,target);
        yield return (IEnumerator)typeof(SkyPulsePolishChecks).GetMethod("MenuChecks",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{game,camera,target});
        SkyPulseBirdRosterPreview.Select("",false);
        yield return SkyPulseFirstPlayChecks.Live(game);
        yield return SkyPulseFailureClarityChecks.Live(game);
        yield return SkyPulseReadabilityChecks.Live(game);
        yield return SkyPulsePickupDisplayChecks.Live(game);
        yield return SkyPulseOneThumbChecks.Live(game);
        yield return SkyPulseProgressionClarityChecks.Live(game);
        yield return SkyPulseModeFeedbackChecks.Live(game);
        Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");SkyPulseBirdRosterPreview.Select("",true);
        yield return null;Canvas.ForceUpdateCanvases();SkyPulseBirdMenuPreview.Present();camera.Render();Save(target,"home.png");
        Call(game,"OpenHangar");yield return null;Canvas.ForceUpdateCanvases();SkyPulseBirdMenuPreview.Present();camera.Render();Save(target,"hangar.png");
        Call(game,"ResetToMenu");Call(game,"StartFlight");Call(game,"UpdateCinematicPresentation",0f);Call(game,"UpdateFirstPlayGuidance",0f);yield return null;Canvas.ForceUpdateCanvases();camera.Render();Save(target,"endless-first-play.png");
        Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);Call(game,"UpdateCinematicPresentation",0f);Call(game,"UpdateFirstPlayGuidance",0f);yield return null;Canvas.ForceUpdateCanvases();camera.Render();Save(target,"relay-first-play.png");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);Call(game,"UpdateCinematicPresentation",0f);Call(game,"UpdateFirstPlayGuidance",0f);yield return null;Canvas.ForceUpdateCanvases();((SkyPulseHeistBoard)Get(game,"blackoutBoard")).Render();camera.Render();Save(target,"heist-first-play.png");
        SkyPulseBirdRosterPreview.Select("",false);Call(game,"ResetToMenu");camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
        File.WriteAllText(Path.Combine(Output,"VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",deviceTested=false,checks=checks.ToArray(),scope="Disposable Unity Editor real input/selection, optional first-play learning, result controls, retry, readable HUD/audio, all five actual pooled pickup housings, gate/puzzle/progression regression. Mobile performance and owner visual acceptance pending."},true));
        Debug.Log("SKYPULSE_EVERYDAY_PASS");EditorApplication.Exit(0);
    }
    static IEnumerator Selection(SkyPulseNativeGame game,Camera camera,RenderTexture target)
    {
        Call(game,"ResetToMenu");var owned=(HashSet<string>)Get(game,"ownedSkinIds");var saved=new HashSet<string>(owned);var originalSkin=Get(game,"equippedSkin");var originalTrail=Get(game,"equippedTrail");int wallet=(int)Get(game,"crystals");
        try
        {
            foreach(string id in new[]{"neon_finch","chrome_raven","verdant_kite","newbird04"})owned.Add(id);
            typeof(SkyPulseBirdRosterPreview).GetField("lastEquippedId",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
            SkyPulseBirdRosterPreview.Select("verdant_kite",true);Call(game,"EquipSkin",Call(game,"FindBird","chrome_raven"));
            Check(SkyPulseBirdRosterPreview.SelectedId==""&&Get(Get(game,"equippedSkin"),"Id").ToString()=="chrome_raven","First real equip clears a stale green reviewer override even without an observed initial equip");
            foreach(string id in new[]{"newbird04","neon_finch","chrome_raven"})
            {
                SkyPulseBirdRosterPreview.Select("verdant_kite",true);Call(game,"EquipSkin",Call(game,"FindBird",id));Call(game,"ResetToMenu");
                yield return null;Canvas.ForceUpdateCanvases();SkyPulseBirdMenuPreview.Present();
                var hero=(RectTransform)Get(game,"menuHeroTransform");var view=hero.GetComponentsInChildren<RawImage>().Single(r=>r.name.StartsWith("Editor solid bird"));
                string display=id=="newbird04"?"Chromaflux":id=="neon_finch"?"Neon Finch":"Chrome Raven";
                Check(SkyPulseBirdRosterPreview.SelectedId==""&&view.name.Contains(display),"Home solid follows the actual Hangar equip: "+id);
                Call(game,"StartFlight");camera.Render();Check(SkyPulseBirdRosterPreview.CurrentRig!=null&&SkyPulseBirdRosterPreview.CurrentRig.name.StartsWith(id),"Actual flight loads the same selected identity: "+id);
                Call(game,"EndFlight");SkyPulseBirdRosterPreview.UpdateNow(game);var skin=Get(game,"equippedSkin");var hit=(Sprite)Call(game,"LoadPresentationPose",Get(skin,"HitPath"));
                Check(((SpriteRenderer)Get(game,"birdRenderer")).sprite==hit,"Equipped identity keeps its own hit image: "+id);
            }
            Call(game,"ResetToMenu");Call(game,"OpenHangar");Set(game,"hangarPageIndex",0);Call(game,"RebuildCustomizeGrid");yield return null;Canvas.ForceUpdateCanvases();
            SkyPulseBirdRosterPreview.Select("verdant_kite",true);
            var hangar=(GameObject)Get(game,"customizeScreen");var equip=hangar.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>()?.text=="EQUIP BIRD");equip.onClick.Invoke();
            Check(Get(Get(game,"equippedSkin"),"Id").ToString()=="neon_finch"&&SkyPulseBirdRosterPreview.SelectedId=="","Actual Hangar EQUIP callback owns the selected identity and clears art overrides");
            Check((int)Get(game,"crystals")==wallet,"Equipping owned birds during preview never charges the wallet");
        }
        finally{SkyPulseBirdRosterPreview.Select("",false);owned.Clear();foreach(string id in saved)owned.Add(id);Set(game,"equippedSkin",originalSkin);Set(game,"equippedTrail",originalTrail);Call(game,"ApplyEquippedVisuals");Call(game,"ResetToMenu");}
    }
    static void Save(RenderTexture target,string name)
    {
        var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;
    }
    [Serializable]sealed class Report{public string result,scope;public bool deviceTested;public string[] checks;}
    public static void Fail(Exception error){Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"FAILURE.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
}
public sealed class SkyPulseEverydayPassRunner:MonoBehaviour
{
    public void Begin(IEnumerator routine){StartCoroutine(Guard(routine));}
    IEnumerator Guard(IEnumerator routine)
    {
        while(true){bool more;object next;try{more=routine.MoveNext();next=more?routine.Current:null;}catch(Exception error){SkyPulseEverydayPassChecks.Fail(error);yield break;}if(!more)yield break;if(next is IEnumerator nested)yield return Guard(nested);else yield return next;}
    }
}
