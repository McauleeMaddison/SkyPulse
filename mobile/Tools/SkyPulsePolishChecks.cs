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
public static class SkyPulsePolishChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static readonly List<string> checks=new List<string>();
    static string Output=>Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"polish");
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    static void Check(bool good,string note){if(!good)throw new Exception(note);checks.Add(note);Debug.Log("POLISH_CHECK: "+note);}
    static SkyPulsePolishChecks(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006"||Application.productName!="SkyPulseFlapQA20261110")throw new Exception("Disposable QA identity required");
        Directory.CreateDirectory(Output);PlayerPrefs.DeleteAll();PlayerPrefs.Save();
        EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");SkyPulseBirdRosterWorkshop.FitSavedShoulders();
        SessionState.SetBool("SkyPulse.PolishQA.Pending",true);EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool("SkyPulse.PolishQA.Pending",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("SkyPulse.PolishQA.Pending",false);new GameObject("Isolated polish QA").AddComponent<SkyPulsePolishRunner>().Begin(Execute());
    }
    static IEnumerator Execute()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();game.enabled=false;
        Set(game,"hapticsEnabled",false);((AudioSource)Get(game,"audioSource")).mute=true;
        var camera=(Camera)Get(game,"flightCamera");var target=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);
        var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        Call(game,"RefreshViewportDecor");Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();
        SkyPulseOneThumbChecks.Pure();SkyPulseMissionChecks.PureChecks();SkyPulseBirdProgressionChecks.Pure();SkyPulseWorldLightingChecks.Pure();
        yield return BirdChecks(game,camera,target);
        yield return MenuChecks(game,camera,target);
        SkyPulseBirdRosterPreview.Select("",false);
        yield return SkyPulseProgressionClarityChecks.Live(game);
        yield return SkyPulseModeFeedbackChecks.Live(game);
        yield return SkyPulseOneThumbChecks.Live(game);
        Call(game,"ResetToMenu");Call(game,"UpdateFlightEnvironment",0f);
        var environment=(SkyPulseFlightEnvironment)Get(game,"flightEnvironment");SkyPulseWorldLightingChecks.Live(environment);
        SkyPulseBirdRosterPreview.Select("neon_finch",true);
        yield return SkyPulseWorldStyleChecks.Capture(game);
        SkyPulseBirdRosterPreview.Select("",false);
        Call(game,"ResetToMenu");
        camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
        File.WriteAllText(Path.Combine(Output,"VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",deviceTested=false,checks=checks.ToArray(),scope="Isolated Unity Editor: tap-driven solid birds, fifteen identity-specific crash frames, shoulders and Home/Hangar surfaces; actual gate and puzzle/progression checks. Device performance and owner appearance approval pending."},true));
        Debug.Log("SKYPULSE_POLISH_PASS");EditorApplication.Exit(0);
    }
    static void Start(SkyPulseNativeGame game,SkyPulseMode mode,string id)
    {
        SkyPulseBirdRosterPreview.Select("",false);game.enabled=false;Call(game,"ResetToMenu");Set(game,"equippedSkin",Call(game,"FindBird",id));
        Set(game,"reduceMotionEnabled",false);
        if(mode==SkyPulseMode.EndlessRoute)Call(game,"StartFlight");else Call(game,"LaunchMission",mode,0);
        Set(game,"shieldImmunityTimer",100f);SkyPulseBirdRosterPreview.Select(id,true);
    }
    static IEnumerator BirdChecks(SkyPulseNativeGame game,Camera camera,RenderTexture target)
    {
        var skins=(Array)typeof(SkyPulseNativeGame).GetField("Skins",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        var designs=SkyPulseBirdRosterWorkshop.LoadDesigns();
        foreach(var d in designs.Where(d=>SkyPulseBirdRosterWorkshop.ShoulderLift(d)>0))
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(SkyPulseBirdRosterWorkshop.PrefabPath(d));var rig=prefab.GetComponent<SkyPulseNeonFinchPrototype>();
            float expected=(d.body.Average(p=>p.y)-d.wingRoot.y)*.01f+SkyPulseBirdRosterWorkshop.ShoulderLift(d);
            Check(Mathf.Abs(rig.leftWing.position.y-expected)<.0001f&&Mathf.Abs(rig.leftWing.position.y-rig.rightWing.position.y)<.0001f,"Both corrected shoulders share the measured raised mounting: "+d.id);
        }
        int count=0;
        foreach(var skin in skins)
        {
            string id=(string)Get(skin,"Id");Start(game,SkyPulseMode.EndlessRoute,id);
            // Deliberately poison the cached prior hit; current identity must win.
            Set(game,"hitBirdSprite",Call(game,"LoadPresentationPose",(string)Get(skins.GetValue(0),"HitPath")));
            Call(game,"ShowHitFrame");var expected=(Sprite)Call(game,"LoadPresentationPose",(string)Get(skin,"HitPath"));
            Check(((SpriteRenderer)Get(game,"birdRenderer")).sprite==expected,"Native crash resolves the current bird's hit image: "+id);
            Call(game,"SetBirdArtwork");camera.Render();var rig=SkyPulseBirdRosterPreview.CurrentRig;Check(rig!=null,"Actual camera loads one solid rig: "+id);
            var neutral=rig.leftWing.localRotation;Call(game,"Flap");
            for(int step=0;step<12;step++){Call(game,"UpdateGameplayWingState",1f/60);yield return null;camera.Render();}
            Check(Quaternion.Angle(neutral,rig.leftWing.localRotation)>5,"Accepted tap lifts the actual solid wing: "+id);
            for(int step=0;step<36;step++){Call(game,"UpdateGameplayWingState",1f/60);yield return null;camera.Render();}
            Check(SkyPulseBirdRosterPreview.NativeWingLift(game)==0&&Quaternion.Angle(neutral,rig.leftWing.localRotation)<.1f,"Without another tap the solid wing settles to glide: "+id);
            if(id=="chrome_raven"||id=="newbird01"||id=="newbird05")Save(target,"flight-"+id+".png");
            // A selected review bird can differ from the equipped/stat bird.
            Set(game,"equippedSkin",skins.GetValue(0));Call(game,"SetBirdArtwork");Call(game,"EndFlight");SkyPulseBirdRosterPreview.UpdateNow(game);
            Check(((SpriteRenderer)Get(game,"birdRenderer")).sprite==expected&&Get(Get(game,"equippedSkin"),"Id").ToString()=="neon_finch","Editor impact preserves chosen artwork without equipping or unlocking it: "+id);
            foreach(string overlay in new[]{"birdFlapRenderer","birdRiseRenderer","birdParallaxRenderer","birdDepthRenderer","birdSafetyRenderer","birdEyeGlintRenderer"})
                Check(!((SpriteRenderer)Get(game,overlay)).enabled,"Crash does not restore a stale secondary layer: "+id+" / "+overlay);
            SkyPulseBirdRosterPreview.Select("",false);count++;
        }
        Check(count==15,"All fifteen roster identities exercise tap motion and crash artwork");
        foreach(int fps in new[]{30,60,120})foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay})
        {
            Start(game,mode,"chrome_raven");camera.Render();var rig=SkyPulseBirdRosterPreview.CurrentRig;rig.ResetPose();var bind=rig.leftWing.localRotation;float peak=0;
            SkyPulseBirdRosterPreview.ResetFlightClock();Call(game,"Flap");
            for(int step=0;step<fps;step++)
            {
                Call(game,"UpdateGameplayWingState",1f/fps);float lift=SkyPulseBirdRosterPreview.AdvanceWingLift(step,1f/fps,false,SkyPulseBirdRosterPreview.NativeWingLift(game));
                Check(SkyPulseBirdRosterPreview.AdvanceWingLift(step,1f/fps,false,1)==lift,"Repeated camera sampling cannot double-advance the wing: "+mode+" / "+fps+" / "+step);
                rig.SampleFlightPose(step/(float)fps,lift);peak=Mathf.Max(peak,Quaternion.Angle(bind,rig.leftWing.localRotation));
            }
            Check(peak>25&&Quaternion.Angle(bind,rig.leftWing.localRotation)<.01f,"One native tap produces a complete raised-to-glide stroke: "+mode+" / "+fps+" Hz");
        }
        Start(game,SkyPulseMode.EndlessRoute,"chrome_raven");Call(game,"Flap");
        for(int step=0;step<8;step++){Call(game,"UpdateGameplayWingState",1f/60);yield return null;camera.Render();}
        var current=SkyPulseBirdRosterPreview.CurrentRig;var held=current.leftWing.localRotation;Call(game,"PauseFlight");
        for(int n=0;n<5;n++){yield return null;camera.Render();Check(Quaternion.Angle(held,current.leftWing.localRotation)<.001f,"Pause holds the tap-driven wing");}
        Call(game,"ResumeFlight");Set(game,"reduceMotionEnabled",true);Call(game,"Flap");
        for(int n=0;n<8;n++){Call(game,"UpdateGameplayWingState",1f/60);yield return null;camera.Render();}
        Check(current.reducedMotion,"Actual rendered preview honours Reduced Motion");
        Call(game,"RestartFlight");camera.Render();Check(SkyPulseBirdRosterPreview.NativeWingLift(game)==0,"Retry resets the input stroke without a phantom flap");
    }
    static IEnumerator MenuChecks(SkyPulseNativeGame game,Camera camera,RenderTexture target)
    {
        Call(game,"ResetToMenu");Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"RefreshScreens");SkyPulseBirdRosterPreview.Select("chrome_raven",true);
        yield return null;Canvas.ForceUpdateCanvases();SkyPulseBirdMenuPreview.Present();camera.Render();
        Check(SkyPulseBirdMenuPreview.ActiveViews==1,"Home displays a single solid bird surface");
        var home=(RectTransform)Get(game,"menuHeroTransform");var raw=home.GetComponentsInChildren<RawImage>().Single(r=>r.name.StartsWith("Editor solid bird"));
        Check(raw.texture!=null&&!raw.raycastTarget&&!((Image)Get(game,"menuBirdImage")).enabled,"Home replaces its old bird artwork without blocking touch input");
        Save(target,"home-3d.png");
        Call(game,"OpenHangar");yield return null;Canvas.ForceUpdateCanvases();SkyPulseBirdMenuPreview.Present();camera.Render();
        Check(SkyPulseBirdMenuPreview.ActiveViews==3,"Hangar retains three actual roster bays with solid birds");
        var root=(RectTransform)Get(game,"hangarCurrentBirdRoot");Check(root.GetComponentsInChildren<RawImage>().Any(r=>r.name.StartsWith("Editor solid bird")&&!r.raycastTarget),"Featured solid bird remains inside the native swipe carousel");
        var legacy=(SkyPulseProgressionStage)Get(game,"hangarPreviewStage");
        Check(!legacy.enabled&&!legacy.GetComponent<RawImage>().enabled,"The previous flat-art studio and its cradle are disabled behind the solid hero");
        InspectMenuViews();Save(target,"hangar-3d.png");
        Call(game,"ShowNextHangarBird");yield return null;Canvas.ForceUpdateCanvases();SkyPulseBirdMenuPreview.Present();camera.Render();
        Check(SkyPulseBirdMenuPreview.ActiveViews==3,"Swiping selection disposes old view surfaces instead of duplicating bays");
        SkyPulseBirdRosterPreview.Select("",false);Check(SkyPulseBirdMenuPreview.ActiveViews==0,"Stopping visual review releases its menu cameras, render targets and overlays");
    }
    static void InspectMenuViews()
    {
        var table=(System.Collections.IDictionary)typeof(SkyPulseBirdMenuPreview).GetField("views",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        foreach(System.Collections.DictionaryEntry entry in table)
        {
            var view=entry.Value;var surface=(RawImage)Get(view,"surface");var texture=(RenderTexture)Get(view,"texture");
            var utility=(PreviewRenderUtility)Get(view,"preview");var rig=(SkyPulseNeonFinchPrototype)Get(view,"rig");var turn=(Transform)Get(view,"turntable");
            Check(Mathf.Abs(texture.width/(float)texture.height-surface.rectTransform.rect.width/surface.rectTransform.rect.height)<.006f,"Each carousel texture preserves its actual UI aspect ratio");
            bool fits=true;float min=.5f,max=.5f;
            for(int yaw=0;yaw<12;yaw++)for(int pose=0;pose<8;pose++)
            {
                turn.localRotation=Quaternion.Euler(0,yaw*30,0);rig.SamplePose((yaw+pose/8f)*rig.flapPeriod);
                foreach(var renderer in rig.GetComponentsInChildren<Renderer>())
                {
                    var b=renderer.localBounds;
                    for(int corner=0;corner<8;corner++)
                    {
                        var point=utility.camera.WorldToViewportPoint(renderer.localToWorldMatrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));
                        min=Mathf.Min(min,point.x,point.y);max=Mathf.Max(max,point.x,point.y);fits&=point.z>0&&point.x>.001f&&point.x<.999f&&point.y>.001f&&point.y<.999f;
                    }
                }
            }
            Check(fits,"Solid carousel bird stays framed across a complete turn and wing cycle; viewport="+min+".."+max);
            view.GetType().GetMethod("Render",F).Invoke(view,new object[]{0f,false});
        }
    }
    static void Save(RenderTexture target,string file)
    {
        var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,file),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;
    }
    [Serializable] sealed class Report{public string result,scope;public bool deviceTested;public string[] checks;}
    public static void Fail(Exception error){Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"FAILURE.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
}
public sealed class SkyPulsePolishRunner:MonoBehaviour
{
    public void Begin(IEnumerator routine){StartCoroutine(Guard(routine));}
    IEnumerator Guard(IEnumerator routine)
    {
        while(true)
        {
            bool more;object next;try{more=routine.MoveNext();next=more?routine.Current:null;}catch(Exception error){SkyPulsePolishChecks.Fail(error);yield break;}
            if(!more)yield break;if(next is IEnumerator nested)yield return Guard(nested);else yield return next;
        }
    }
}
