// Runs only in an isolated Unity QA product. Actual renders, never storefront footage.
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
public static class SkyPulseFinalPolishChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static string Output=>Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"final-polish");
    static readonly List<string> checks=new List<string>();
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    static void Check(bool pass,string text){if(!pass)throw new Exception(text);checks.Add(text);Debug.Log("FINAL_POLISH_CHECK: "+text);}
    static SkyPulseFinalPolishChecks(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006"||Application.productName!="SkyPulseFlapQA20261110")throw new Exception("Disposable batch QA product required");
        SkyPulseNativeGame.ReviewOriginalBirdArtwork=true;
        Directory.CreateDirectory(Output);Directory.CreateDirectory(Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"polish"));
        foreach(string stale in new[]{"FAILURE.txt","VALIDATION.json"})if(File.Exists(Path.Combine(Output,stale)))File.Delete(Path.Combine(Output,stale));
        PlayerPrefs.DeleteAll();PlayerPrefs.Save();SkyPulseBirdRosterPreview.Select("",false);
        EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");SessionState.SetBool("SkyPulse.FinalPolishQA",true);EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||!SessionState.GetBool("SkyPulse.FinalPolishQA",false))return;
        SessionState.SetBool("SkyPulse.FinalPolishQA",false);new GameObject("Isolated final polish QA").AddComponent<SkyPulseFinalPolishRunner>().Begin(Execute());
    }
    static IEnumerator Existing(string method,SkyPulseNativeGame g,Camera camera,RenderTexture target)
    {yield return (IEnumerator)typeof(SkyPulsePolishChecks).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{g,camera,target});}
    static IEnumerator Execute()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();g.enabled=false;
        Set(g,"hapticsEnabled",false);((AudioSource)Get(g,"audioSource")).mute=true;
        var camera=(Camera)Get(g,"flightCamera");var target=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);
        var canvas=((GameObject)Get(g,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        Call(g,"RefreshViewportDecor");Canvas.ForceUpdateCanvases();Call(g,"ApplySafeArea");Call(g,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();
        PreviewTruth(g);
        SkyPulseOneThumbChecks.Pure();SkyPulseReadabilityChecks.Pure();SkyPulsePickupDisplayChecks.Pure();SkyPulseBirdProgressionChecks.Pure();SkyPulseWorldLightingChecks.Pure();
        yield return SkyPulseEverydayControlsChecks.Live(g);
        yield return Existing("BirdChecks",g,camera,target);
        yield return Existing("MenuChecks",g,camera,target);
        yield return SkyPulseBirdFinishChecks.Live(g);
        SkyPulseBirdRosterPreview.Select("",false);
        yield return SkyPulseFirstPlayChecks.Live(g);
        yield return SkyPulseFailureClarityChecks.Live(g);
        yield return SkyPulseReadabilityChecks.Live(g);
        yield return SkyPulsePickupDisplayChecks.Live(g);
        yield return SkyPulseProgressionClarityChecks.Live(g);
        yield return SkyPulseFinalUiChecks.Live(g);
        yield return SkyPulseHomeProgressChecks.Live(g);
        yield return SkyPulseOneThumbChecks.Live(g);
        yield return SkyPulseModeFeedbackChecks.Live(g);
        Call(g,"ResetToMenu");Call(g,"UpdateFlightEnvironment",0f);SkyPulseWorldLightingChecks.Live((SkyPulseFlightEnvironment)Get(g,"flightEnvironment"));
        yield return SkyPulsePipeFinishChecks.Live(g);
        SkyPulseBirdRosterPreview.Select("neon_finch",true);yield return SkyPulseWorldStyleChecks.Capture(g);
        yield return Review(g,camera,target);
        SkyPulseBirdRosterPreview.Select("",false);Call(g,"ResetToMenu");camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
        File.WriteAllText(Path.Combine(Output,"VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",checks=checks.ToArray(),deviceTested=false,scope="Isolated Unity Editor: existing full bird/controls/UI/progression/gate/puzzle/pipe/world suites plus final lighting/UI changes and all eight exact free-plan outcomes. Rendered review candidate, owner visual acceptance and runtime solid promotion pending."},true));
        Debug.Log("SKYPULSE_FINAL_POLISH_PASS");
        if(Environment.GetEnvironmentVariable("SKYPULSE_INCLUDE_FIELD_TECH")=="1")yield return SkyPulseFieldTechChecks.Live(g,camera,target);
        if(Environment.GetEnvironmentVariable("SKYPULSE_INCLUDE_RUNTIME")=="1")
        {
            SkyPulseNativeGame.ReviewOriginalBirdArtwork=false;SkyPulseBirdRosterPreview.Select("",false);
            yield return (IEnumerator)typeof(SkyPulseRuntimeBirdChecks).GetMethod("Execute",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        }
        EditorApplication.Exit(0);
    }
    static void PreviewTruth(SkyPulseNativeGame game)
    {
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);
        var caption=typeof(SkyPulseNativeGame).GetMethod("BlackoutPreviewCaption",BindingFlags.Static|BindingFlags.NonPublic);
        var label=(Text)Get(game,"blackoutStatusText");int total=0,added=0,removed=0,sensor=0,complete=0;
        foreach(var puzzle in SkyPulseBlackoutCampaign.Stages)
        {
            var live=new BlackoutSession(puzzle);var preview=new BlackoutSession(puzzle);
            var solve=SkyPulseMissionChecks.Solve(puzzle);
            for(int step=0;step<=solve.Count;step++)
            {
                if(live.State!=MissionSessionState.Playing)break;
                foreach(int tile in Enumerable.Range(0,puzzle.Count).Where(puzzle.Rotatable))
                {
                    int trace=live.Trace,turns=live.Turns,cores=live.CoresPowered;var rotations=(int[])live.Rotations.Clone();
                    Check(live.PreviewRotation(tile,preview),"Free plan evaluates an available optic: "+puzzle.Id+" / "+step+" / "+tile);
                    string text=(string)caption.Invoke(null,new object[]{live,preview});
                    Check(text.Contains(preview.CoresPowered+" / "+puzzle.CoreCount+" CORES"),"Plan readout uses proposed powered cores");
                    Check(live.Trace==trace&&live.Turns==turns&&live.CoresPowered==cores&&live.Rotations.SequenceEqual(rotations),"Reading a proposed outcome never rotates or charges the live board");
                    if(preview.State==MissionSessionState.Failed)Check(text.StartsWith("TRACE WOULD RUN OUT"),"An exhausted proposed route explicitly warns of failure");
                    else Check(text.Contains(preview.Remaining+" TRACE LEFT"),"Plan readout reports exact remaining trace after the move");
                    if(preview.State==MissionSessionState.Playing)Check(text.Contains(preview.UplinkPowered?"UPLINK LIVE":"UPLINK DARK"),"Plan includes the proposed uplink state");
                    float width=label.cachedTextGenerator.GetPreferredWidth(text,label.GetGenerationSettings(label.rectTransform.rect.size));
                    Check(width<=label.rectTransform.rect.width+.5f,"Plan outcome fits one readable line: "+text);
                    total++;if(preview.CoresPowered>cores)added++;if(preview.CoresPowered<cores)removed++;if(preview.LastCost==3)sensor++;if(preview.State==MissionSessionState.Succeeded)complete++;
                }
                if(step<solve.Count)live.Rotate(solve[step]);
            }
        }
        // Authored solve prefixes avoid alarms. Reuse the existing alarm fixture
        // to verify a sensor hit and its disconnection, rather than assuming a
        // correct solution route must contain a penalised turn.
        var first=SkyPulseBlackoutCampaign.Stages[0];
        var alarmPuzzle=new BlackoutPuzzle("sensor","","",new[]{"##!#","S.M#","##C#","##ME"},first.Initial,8,2);
        var alarm=new BlackoutSession(alarmPuzzle);var proposed=new BlackoutSession(alarmPuzzle);
        alarm.PreviewRotation(14,proposed);string costly=(string)caption.Invoke(null,new object[]{alarm,proposed});
        Check(proposed.LastCost==3&&costly.Contains("5 TRACE LEFT")&&alarm.Trace==0,"Sensor preview exposes the actual three-trace result without charging");sensor++;
        alarm.PreviewRotation(6,proposed);string released=(string)caption.Invoke(null,new object[]{alarm,proposed});
        Check(proposed.LastCost==1&&released.Contains("7 TRACE LEFT")&&alarm.Trace==0,"Disconnecting a lit sensor previews its one-trace cost immediately");
        var poweredRoute=new BlackoutSession(first,proposed.Rotations);var lostPower=new BlackoutSession(first);
        poweredRoute.PreviewRotation(6,lostPower);string losing=(string)caption.Invoke(null,new object[]{poweredRoute,lostPower});
        Check(poweredRoute.CoresPowered>lostPower.CoresPowered&&losing.Contains("(-1)"),"A proposed turn that darkens a live core explicitly shows lost power");removed++;
        var nearlySpent=new BlackoutSession(first,null,7,7);var lastTurn=new BlackoutSession(first);nearlySpent.PreviewRotation(6,lastTurn);
        Check(((string)caption.Invoke(null,new object[]{nearlySpent,lastTurn})).StartsWith("TRACE WOULD RUN OUT")&&nearlySpent.Trace==7,"A final unsuccessful trace is warned before the live move");
        Check(total>40&&added>0&&removed>0&&sensor>0&&complete>0,"Plan coverage includes added/lost power, sensor costs and successful completions: "+total+" / "+added+" / "+removed+" / "+sensor+" / "+complete);
    }
    static void Present(SkyPulseNativeGame g,float dt)
    {
        Set(g,"ambientTime",(float)Get(g,"ambientTime")+dt);Call(g,"UpdateMenuBird",dt);Call(g,"UpdateProtocolPresentation",dt);Call(g,"UpdateCinematicPresentation",dt);Call(g,"UpdateHangarHeroPresentation");
        Canvas.ForceUpdateCanvases();Call(g,"ApplySafeArea");Call(g,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();SkyPulseBirdMenuPreview.Present();
        foreach(var b in UnityEngine.Object.FindObjectsByType<SkyPulseHeistBoard>())b.Render();foreach(var s in UnityEngine.Object.FindObjectsByType<SkyPulseDepthStage>())s.Render();
    }
    static IEnumerator Review(SkyPulseNativeGame g,Camera camera,RenderTexture target)
    {
        SkyPulseBirdRosterPreview.Select("",true);Set(g,"reduceMotionEnabled",false);
        foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay,SkyPulseMode.BlackoutProtocol})
        {
            Call(g,"SetHomeProtocol",mode);Call(g,"ResetToMenu");Present(g,1f);yield return null;Present(g,0f);camera.Render();Save(target,"home-"+mode+".png");
        }
        Call(g,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(g,"ResetToMenu");Call(g,"OpenHangar");
        Set(g,"hangarInformationFadeTime",1f);Present(g,1f);yield return null;Present(g,0f);camera.Render();Save(target,"hangar.png");
        Check(((CanvasGroup)Get(g,"hangarInformationCanvas")).alpha>.99f,"Hangar review includes completed name/traits/equip reveal");
        Call(g,"OpenUpgrades");Present(g,1f);yield return null;Present(g,0f);camera.Render();Save(target,"tech-tree.png");
        Call(g,"LaunchMission",SkyPulseMode.PulseRelay,0);var relay=(RelaySession)Get(g,"relaySession");
        Set(g,"birdY",relay.Mission.RelayY[0]);relay.Move(relay.Mission.RelayX(0)-3.8f);Call(g,"UpdateRelayPresentation");
        Present(g,1f);yield return null;Present(g,0f);camera.Render();Save(target,"relay-locked.png");
        relay.Collect(0);Call(g,"UpdateRelayPresentation");Present(g,0);camera.Render();Save(target,"relay-half-charged.png");
        relay.Collect(1);Call(g,"UpdateRelayPresentation");Present(g,.15f);camera.Render();Save(target,"relay-open.png");
        Call(g,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);Present(g,1f);yield return null;Present(g,0);camera.Render();Save(target,"heist.png");
        Call(g,"ToggleBlackoutPreview");Call(g,"PreviewBlackoutNode",6);Present(g,.2f);camera.Render();Save(target,"heist-plan.png");
        Call(g,"CancelBlackoutPreview");var session=(BlackoutSession)Get(g,"blackoutSession");foreach(int move in SkyPulseMissionChecks.Solve(session.Puzzle))Call(g,"TouchBlackoutNode",move);Present(g,.05f);camera.Render();Save(target,"heist-restored.png");
    }
    static void Save(RenderTexture target,string name)
    {
        var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;
    }
    [Serializable]sealed class Report{public string result,scope;public bool deviceTested;public string[] checks;}
    public static void Fail(Exception error){Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"FAILURE.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
}
public sealed class SkyPulseFinalPolishRunner:MonoBehaviour
{
    public void Begin(IEnumerator routine){StartCoroutine(Guard(routine));}
    IEnumerator Guard(IEnumerator routine)
    {
        while(true){bool more;object next;try{more=routine.MoveNext();next=more?routine.Current:null;}catch(Exception error){SkyPulseFinalPolishChecks.Fail(error);yield break;}if(!more)yield break;if(next is IEnumerator nested)yield return Guard(nested);else yield return next;}
    }
}
