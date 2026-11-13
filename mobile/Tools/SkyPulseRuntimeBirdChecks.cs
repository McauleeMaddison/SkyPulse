// Disposable QA only: the exact native presenter and combined assets used on iPhone.
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
public static class SkyPulseRuntimeBirdChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static readonly List<string> checks=new List<string>();
    static string Output=>Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"runtime-birds");
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add(text);Debug.Log("RUNTIME_BIRD_CHECK: "+text);}
    static SkyPulseRuntimeBirdChecks(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        Directory.CreateDirectory(Output);SkyPulseNativeGame.ReviewOriginalBirdArtwork=false;SkyPulseBirdRosterPreview.Select("",false);
        PlayerPrefs.DeleteAll();PlayerPrefs.Save();EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");SessionState.SetBool("SkyPulse.RuntimeBirdQA",true);EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||!SessionState.GetBool("SkyPulse.RuntimeBirdQA",false))return;
        SessionState.SetBool("SkyPulse.RuntimeBirdQA",false);new GameObject("Runtime bird QA").AddComponent<SkyPulseRuntimeBirdRunner>().Begin(Execute());
    }
    static IEnumerator Execute()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();g.enabled=false;Set(g,"hapticsEnabled",false);Set(g,"reduceMotionEnabled",false);((AudioSource)Get(g,"audioSource")).mute=true;
        var camera=(Camera)Get(g,"flightCamera");var target=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);
        var canvas=((GameObject)Get(g,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        Call(g,"RefreshViewportDecor");Canvas.ForceUpdateCanvases();Call(g,"ApplySafeArea");Call(g,"FitInterfaceToSafeArea");
        var skins=(Array)typeof(SkyPulseNativeGame).GetField("Skins",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        foreach(var skin in skins)
        {
            string id=(string)Get(skin,"Id");((HashSet<string>)Get(g,"ownedSkinIds")).Add(id);Call(g,"EquipSkin",skin);
            foreach(int fps in new[]{30,60,120})
            {
                Call(g,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(g,"ResetToMenu");Call(g,"StartFlight");Call(g,"UpdateSolidBirds",0f);
                var rig=(SkyPulseNeonFinchPrototype)Get(g,"solidRig");Check(rig!=null&&g.RuntimeSolidBirdsActive&&(string)Get(g,"solidPilotId")==id,"Native runtime loads the selected combined bird: "+id+" / "+fps);
                var bind=rig.leftWing.localRotation;Call(g,"Flap");float first=Quaternion.Angle(bind,rig.leftWing.localRotation);
                Check((float)Get(g,"birdVelocity")>0&&first>=5f,"One accepted press immediately raises the bird and opens the physical wing: "+id+" / "+fps);
                float low=0,high=0;
                for(int frame=0;frame<fps;frame++)
                {
                    Call(g,"UpdateGameplayWingState",1f/fps);Call(g,"UpdateSolidBirds",1f/fps);
                    float angle=Mathf.DeltaAngle(0,(Quaternion.Inverse(bind)*rig.leftWing.localRotation).eulerAngles.x);low=Mathf.Min(low,angle);high=Mathf.Max(high,angle);
                }
                Check(high>24&&low< -20&&high-low>46,"A complete tap stroke has clear upstroke and downstroke, then glide: "+id+" / "+fps+" / "+low.ToString("F2")+".."+high.ToString("F2"));
                Check(Mathf.Abs((float)Get(g,"solidWingStroke"))<.001f,"No tap settles the physical wing instead of inventing more flaps: "+id+" / "+fps);
                Call(g,"Flap");Call(g,"UpdateGameplayWingState",.08f);Call(g,"UpdateSolidBirds",.08f);Call(g,"PauseFlight");var held=rig.leftWing.localRotation;float clock=(float)Get(g,"solidFlightTime");
                Call(g,"UpdateSolidBirds",.1f);Check(rig.leftWing.localRotation==held&&(float)Get(g,"solidFlightTime")==clock,"Pause preserves the exact wing pose and clock: "+id+" / "+fps);Call(g,"ResumeFlight");
            }
            Set(g,"reduceMotionEnabled",true);Call(g,"RestartFlight");Call(g,"UpdateSolidBirds",0f);var reduced=(SkyPulseNeonFinchPrototype)Get(g,"solidRig");var neutral=reduced.leftWing.localRotation;Call(g,"Flap");float peak=0;
            for(int frame=0;frame<60;frame++){Call(g,"UpdateGameplayWingState",1f/60);Call(g,"UpdateSolidBirds",1f/60);peak=Mathf.Max(peak,Quaternion.Angle(neutral,reduced.leftWing.localRotation));}
            Check(peak>14&&peak<25,"Reduced Motion preserves a restrained but readable essential stroke: "+id);Set(g,"reduceMotionEnabled",false);
            Call(g,"EndFlight");Call(g,"UpdateSolidBirds",0f);var hit=(Sprite)Call(g,"LoadPresentationPose",Get(skin,"HitPath"));
            Check(!((GameObject)Get(g,"solidPilot")).activeSelf&&((SpriteRenderer)Get(g,"birdRenderer")).sprite==hit&&((SpriteRenderer)Get(g,"birdRenderer")).enabled,"Impact shows this bird's own hit artwork with no duplicate solid: "+id);
            yield return null;
        }
        Call(g,"EquipSkin",skins.GetValue(0));Call(g,"ResetToMenu");Call(g,"UpdateCinematicPresentation",.1f);Call(g,"UpdateSolidBirds",.1f);Canvas.ForceUpdateCanvases();camera.Render();Save(target,"home.png");
        Call(g,"OpenHangar");Set(g,"hangarInformationFadeTime",1f);Call(g,"UpdateHangarHeroPresentation");Call(g,"UpdateSolidBirds",.1f);yield return null;Canvas.ForceUpdateCanvases();camera.Render();Save(target,"hangar.png");
        var views=(System.Collections.IDictionary)Get(g,"solidMenuViews");Check(views.Count==3,"The native Hangar has exactly three solid carousel views");
        Check(!((SkyPulseProgressionStage)Get(g,"hangarPreviewStage")).enabled,"The old articulated portrait stage cannot animate/render beneath the native solid");
        for(int i=0;i<20;i++){Set(g,"hangarPageIndex",(i+1)%15);Call(g,"RebuildCustomizeGrid");Call(g,"UpdateSolidBirds",.1f);yield return null;}
        Check(views.Count==3,"Repeated carousel pages replace retired views without exhausting the studio pool");
        Call(g,"ResetToMenu");Call(g,"LaunchMission",SkyPulseMode.PulseRelay,0);Call(g,"UpdateSolidBirds",0f);Call(g,"Flap");
        Check((SkyPulseNeonFinchPrototype)Get(g,"solidRig")!=null&&((GameObject)Get(g,"solidPilot")).activeSelf,"Relay uses the same selected physical pilot and accepted tap stroke");
        Call(g,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);Call(g,"UpdateSolidBirds",0f);Check(!((GameObject)Get(g,"solidPilot")).activeSelf&&views.Count==0,"Blackout cannot retain flight birds or hidden Hangar targets");
        var audit=SkyPulseWingClearanceChecks.InspectAll(0,false);Check(audit.birds.All(b=>b.contactPoses==0),"Full signed strokes and Reduced Motion have no sampled wing/foot surface crossings across all15 canonical rigs");
        File.WriteAllText(Path.Combine(Output,"VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",checks=checks.ToArray(),deviceAcceptance=false},true));
        camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);Debug.Log("SKYPULSE_RUNTIME_BIRDS_PASS");EditorApplication.Exit(0);
    }
    static void Save(RenderTexture target,string path)
    {var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,path),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;}
    [Serializable]sealed class Report{public string result;public string[] checks;public bool deviceAcceptance;}
    public static void Fail(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"FAILURE.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
}
public sealed class SkyPulseRuntimeBirdRunner:MonoBehaviour
{
    public void Begin(IEnumerator r){StartCoroutine(Guard(r));}
    IEnumerator Guard(IEnumerator r){while(true){bool more;object next;try{more=r.MoveNext();next=more?r.Current:null;}catch(Exception e){SkyPulseRuntimeBirdChecks.Fail(e);yield break;}if(!more)yield break;if(next is IEnumerator nested)yield return Guard(nested);else yield return next;}}
}
