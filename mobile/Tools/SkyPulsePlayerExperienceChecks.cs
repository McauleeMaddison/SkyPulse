// Disposable Editor QA only; not copied into the shipping player.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using SkyPulse.Mobile;

public static class SkyPulsePlayerExperienceChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("EXPERIENCE_CHECK: "+message);}
    sealed class Store:IMissionStore
    {
        readonly Dictionary<string,string> data=new Dictionary<string,string>();
        readonly Dictionary<string,int> ints=new Dictionary<string,int>();
        public string Get(string k,string d)=>data.TryGetValue(k,out var v)?v:d;
        public int GetInt(string k,int d)=>ints.TryGetValue(k,out var v)?v:d;
        public void Set(string k,string v)=>data[k]=v;
        public void SetInt(string k,int v)=>ints[k]=v;
        public void Flush(){}
    }
    static void Fit(SkyPulseNativeGame g)
    {
        Canvas.ForceUpdateCanvases();Call(g,"ApplySafeArea");Call(g,"FitInterfaceToSafeArea");Call(g,"FitBlackoutToSafeArea");Canvas.ForceUpdateCanvases();
    }
    static void Save(Camera camera,RenderTexture rt,string file)
    {
        foreach(var b in UnityEngine.Object.FindObjectsByType<SkyPulseHeistBoard>(FindObjectsSortMode.None))b.Render();
        foreach(var s in UnityEngine.Object.FindObjectsByType<SkyPulseDepthStage>(FindObjectsSortMode.None))s.Render();
        foreach(var t in UnityEngine.Object.FindObjectsByType<SkyPulseTechTreeView>(FindObjectsSortMode.None))t.Render();
        camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
        File.WriteAllBytes(file,image.EncodeToPNG());UnityEngine.Object.Destroy(image);RenderTexture.active=previous;
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        game.enabled=false;Set(game,"hapticsEnabled",false);Set(game,"reduceMotionEnabled",false);
        var camera=(Camera)Get(game,"flightCamera");var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        string folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"player-experience");Directory.CreateDirectory(folder);
        var rt=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=rt;Call(game,"RefreshViewportDecor");
        foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay,SkyPulseMode.BlackoutProtocol})
        {
            Call(game,"SetHomeProtocol",mode);Call(game,"ResetToMenu");Call(game,"UpdateCinematicPresentation",0f);Call(game,"UpdateProtocolPresentation",0f);yield return null;Fit(game);
            var home=(GameObject)Get(game,"homeScreen");
            Check(home.GetComponentsInChildren<SkyPulseCircuitGraphic>(true).Where(f=>f.Surface==SkyPulseSurface.Panel).All(f=>f.SoftGlass),"Every Home control uses the same satin surface: "+mode);
            var stage=(SkyPulseDepthStage)Get(game,"homeDepth");stage.Render();var target=stage.StageCamera.targetTexture;var previous=RenderTexture.active;RenderTexture.active=target;
            var alpha=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);alpha.ReadPixels(new Rect(0,0,target.width,target.height),0,0);alpha.Apply();
            Check(alpha.GetPixel(0,0).a<.01f&&alpha.GetPixel(target.width-1,target.height-1).a<.01f,"Home render target has transparent corners rather than an opaque rectangle: "+mode);
            UnityEngine.Object.Destroy(alpha);RenderTexture.active=previous;Save(camera,rt,Path.Combine(folder,"home-"+mode+".png"));
        }
        Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"StartFlight");Call(game,"UpdateCinematicPresentation",0f);
        var body=(SpriteRenderer)Get(game,"birdRenderer");var props=new MaterialPropertyBlock();body.GetPropertyBlock(props);var metal=(Material)Get(game,"routeMetal");
        Check(Vector4.Distance(props.GetColor("_LightTint"),metal.GetColor("_LightTint"))<.001f,"Bird and pipe metal read the identical world key colour");
        float y=(float)Get(game,"birdY"),velocity=(float)Get(game,"birdVelocity");Call(game,"UpdateCinematicPresentation",.2f);
        Check((float)Get(game,"birdY")==y&&(float)Get(game,"birdVelocity")==velocity,"Lighting cannot change flight handling");
        for(int frame=0;frame<90;frame++)
        {
            float nearest=float.MaxValue,target=0;
            foreach(var gate in (Array)Get(game,"pipePool")){float x=(float)Get(gate,"X");if(!(bool)Get(gate,"Passed")&&x>=-3&&x<nearest){nearest=x;target=(float)Get(gate,"GapCenter");}}
            if((float)Get(game,"birdY")<target-.65f&&(float)Get(game,"birdVelocity")<0)Call(game,"Flap");
            Call(game,"SimulateFlight",1f/60);Call(game,"UpdateGameplayWingState",1f/60);Call(game,"UpdateBirdWingMotion");Call(game,"UpdateCinematicPresentation",1f/60);
        }
        Call(game,"UpdateScoreBurst",10f);Fit(game);Save(camera,rt,Path.Combine(folder,"endless-pipe-lighting.png"));Call(game,"StartFlight");
        var pickup=((Array)Get(game,"crystalPickupPool")).GetValue(0);int wallet=(int)Get(game,"crystals");
        for(int i=0;i<100;i++)Call(game,"CollectCrystalPickup",pickup);
        int banked=(int)Get(game,"runCrystalsCollected"),bird=(int)Get(game,"runBirdPickupBonus"),tech=(int)Get(game,"runTechPickupBonus");
        Check(banked==100+bird+tech&&(int)Get(game,"crystals")==wallet+banked,"One hundred physical pickups reconcile with the banked bird/tech subtotals");
        Set(game,"score",10);Set(game,"perfectPasses",4);Set(game,"newBest",false);Call(game,"RefreshProgressionResultLabels");int paid=(int)Get(game,"crystals");
        Check(paid-wallet==banked+(int)Get(game,"runBirdResultBonus")+(int)Get(game,"runTechResultBonus"),"Displayed pickup, bird and tech rows sum to the once-only actual wallet credit");
        Call(game,"RefreshProgressionResultLabels");Check((int)Get(game,"crystals")==paid,"Repainting the reward report never credits again");
        Call(game,"EndFlight");Set(game,"impactFrameTimer",0f);Set(game,"impactTumbleTimer",0f);Call(game,"Update");Fit(game);Save(camera,rt,Path.Combine(folder,"bird-trait-rewards.png"));
        Call(game,"ResetToMenu");Call(game,"OpenUpgrades");yield return null;Fit(game);Save(camera,rt,Path.Combine(folder,"tech-next-levels.png"));
        var oldCampaign=Get(game,"blackoutCampaign");var campaign=new BlackoutCampaignProgress(new Store());Set(game,"blackoutCampaign",campaign);
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);Call(game,"RestartBlackoutVault");
        var session=(BlackoutSession)Get(game,"blackoutSession");string saveBefore=JsonUtility.ToJson(campaign.Data);
        Call(game,"ToggleBlackoutPreview");Call(game,"PreviewBlackoutNode",14);
        var commit=(Button)Get(game,"blackoutCommitButton");
        Check(session.Trace==0&&JsonUtility.ToJson(campaign.Data)==saveBefore,"Opening and changing a plan neither spends nor saves progress");
        Call(game,"RestartBlackoutVault");commit.onClick.Invoke();
        Check(((BlackoutSession)Get(game,"blackoutSession")).Trace==0,"A queued preview confirmation cannot spend after restart");
        string clip=Path.Combine(folder,"signal-planning");Directory.CreateDirectory(clip);
        for(int frame=0;frame<270;frame++)
        {
            if(frame==30){Call(game,"ToggleBlackoutPreview");Call(game,"PreviewBlackoutNode",14);}
            if(frame==105)Call(game,"CommitBlackoutPreview");
            if(frame==150){Call(game,"ToggleBlackoutPreview");Call(game,"PreviewBlackoutNode",6);}
            if(frame==225)Call(game,"CommitBlackoutPreview");
            Call(game,"UpdateProtocolPresentation",1f/30);yield return new WaitForSecondsRealtime(1f/30);Fit(game);
            if(frame==65)Save(camera,rt,Path.Combine(folder,"signal-preview.png"));
            if(frame==190)Save(camera,rt,Path.Combine(folder,"signal-restore-preview.png"));
            Save(camera,rt,Path.Combine(clip,frame.ToString("000")+".png"));
        }
        Check(campaign.Data.medals[0]>0&&(int)Get(game,"selectedMission")==1,"Planned extraction restores a saved system and activates the next vault immediately");
        Check(((Text)Get(game,"blackoutCompletionToast")).text.Contains("ONLINE")||((Text)Get(game,"blackoutCompletionToast")).text==SkyPulseBlackoutCampaign.Objective(1),"Completion connects to a persistent network objective");
        Call(game,"ResetToMenu");Set(game,"blackoutCampaign",oldCampaign);Call(game,"InitializeMissionServices");
        camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);Debug.Log("SKYPULSE_PLAYER_EXPERIENCE_PASS");
    }
}

[InitializeOnLoad]
public static class SkyPulsePlayerExperienceTargeted
{
    static SkyPulsePlayerExperienceTargeted(){EditorApplication.update+=Tick;}
    public static void Run(){if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");SessionState.SetBool("SkyPulseExperienceTargeted",true);EditorApplication.EnterPlaymode();}
    static void Tick()
    {
        if(!SessionState.GetBool("SkyPulseExperienceTargeted",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("SkyPulseExperienceTargeted",false);new GameObject("Focused visual QA").AddComponent<SkyPulseWorldExpansionRunner>().Begin(Execute());
    }
    static IEnumerator Execute()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();if(game==null)game=new GameObject("QA game").AddComponent<SkyPulseNativeGame>();game.enabled=false;
        yield return SkyPulseDepthChecks.Live(game);yield return SkyPulsePlayerExperienceChecks.Live(game);Debug.Log("SKYPULSE_EXPERIENCE_TARGETED_PASS");EditorApplication.Exit(0);
    }
}
