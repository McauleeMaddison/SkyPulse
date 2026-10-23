// Editor-only checks; copied into an isolated product by run_dimensional_qa.py.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulseDimensionalChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object value)=>o.GetType().GetField(n,F).SetValue(o,value);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);Debug.Log("DIMENSIONAL_CHECK: "+s);}
    static SkyPulseDimensionalChecks(){EditorApplication.update+=Tick;}
    public static void Run(){if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Isolated QA only");PlayerPrefs.DeleteAll();PlayerPrefs.Save();SessionState.SetBool("DimensionalQa",true);EditorApplication.EnterPlaymode();}
    static void Tick()
    {
        if(!SessionState.GetBool("DimensionalQa",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("DimensionalQa",false);
        new GameObject("Dimensional QA").AddComponent<SkyPulseWorldExpansionRunner>().Begin(Execute());
    }
    static IEnumerator Execute()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Isolated QA only");
        SkyPulseBirdProgressionChecks.Pure();SkyPulseDailyRouteChecks.Pure();SkyPulseMissionChecks.PureChecks();SkyPulseOneThumbChecks.Pure();SkyPulseBlackoutChecks.Pure();
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();if(game==null)game=new GameObject("QA game").AddComponent<SkyPulseNativeGame>();game.enabled=false;
        Set(game,"hapticsEnabled",false);
        yield return Audio(game);
        yield return SkyPulseTouchComfortChecks.Live(game);
        yield return Environment(game);
        yield return SkyPulsePipeFinishChecks.Live(game);
        SkyPulseMissionChecks.IntegrationChecks(game);
        yield return SkyPulseOneThumbChecks.Live(game);
        yield return SkyPulseBlackoutChecks.Live(game);
        yield return SkyPulsePurposeChecks.Live(game);
        yield return SkyPulseDailyRouteChecks.Live(game);
        var founders=(Array)typeof(SkyPulseNativeGame).GetField("Skins",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        Set(game,"equippedSkin",founders.GetValue(0));Call(game,"ApplyEquippedVisuals");
        yield return SkyPulseBirdProgressionChecks.Live(game);
        yield return SkyPulseGuideWorldChecks.Live(game);
        yield return SkyPulseCopyChecks.Live(game);
        yield return SkyPulseDepthChecks.Live(game);
        yield return SkyPulseCinematicChecks.Live(game);
        yield return SkyPulseRelayGateChecks.Live(game);
        yield return SkyPulsePresentationChecks.Captures(game);
        yield return SkyPulseBlackoutChecks.Captures(game);
        if(System.Environment.GetEnvironmentVariable("SKYPULSE_CAPTURE_ENDLESS")=="1")yield return SkyPulseFlightPolishChecks.Capture(game);
        Debug.Log("SKYPULSE_DIMENSIONAL_PASS");EditorApplication.Exit(0);
    }
    static IEnumerator Audio(SkyPulseNativeGame game)
    {
        Call(game,"StopGameSounds");Set(game,"soundEnabled",true);var master=(AudioSource)Get(game,"audioSource");master.mute=false;
        var voices=(AudioSource[])Get(game,"effectVoices");Check(voices.Length==6&&voices.All(v=>v!=null),"All six voices are preallocated");
        string[] names={"flapSound","scoreSound","crystalSound","crashSound","unlockSound","prismSound","sensorSound"};float maxPeak=0;
        foreach(string name in names)
        {
            var clip=(AudioClip)Get(game,name);Check(clip!=null,"Audio reference resolves: "+name);clip.LoadAudioData();
            var data=new float[clip.samples*clip.channels];Check(clip.GetData(data,0),"Unity decodes "+name);
            float peak=0;foreach(float sample in data){if(float.IsNaN(sample)||float.IsInfinity(sample))throw new Exception("Invalid decoded sample");peak=Mathf.Max(peak,Mathf.Abs(sample));}
            Check(peak>.01f&&peak<.32f,"Cue has mix headroom: "+name+" "+peak);maxPeak=Mathf.Max(maxPeak,peak);
        }
        Check(maxPeak*6*.45f+.25f*.28f<.95f,"Worst-case seven-source sum retains headroom");
        var play=(Action<AudioClip>)Delegate.CreateDelegate(typeof(Action<AudioClip>),game,typeof(SkyPulseNativeGame).GetMethod("Play",F));
        int count=game.GetComponents<AudioSource>().Length;
        foreach(string name in names)for(int i=0;i<100;i++)play((AudioClip)Get(game,name));
        Check(game.GetComponents<AudioSource>().Length==count&&voices.Count(v=>v.isPlaying)<=6,"Rapid feedback cannot create sources or unbounded overlaps");
        Call(game,"ToggleSound");Check(!((bool)Get(game,"soundEnabled"))&&PlayerPrefs.GetInt("skypulse.native.sound",1)==0&&voices.All(v=>!v.isPlaying),"Sound OFF saves and stops all effects");
        play((AudioClip)Get(game,"crashSound"));Check(voices.All(v=>!v.isPlaying),"Muted impacts stay silent");
        Set(game,"soundEnabled",true);Call(game,"LoadProgress");Check(!(bool)Get(game,"soundEnabled")&&master.mute,"Saved sound preference reloads correctly");Call(game,"ToggleSound");
        Call(game,"StartFlight");play((AudioClip)Get(game,"flapSound"));Call(game,"PauseFlight");Check(voices.All(v=>!v.isPlaying),"Pause immediately stops audio tails");Call(game,"ResumeFlight");
        play((AudioClip)Get(game,"unlockSound"));Call(game,"OnApplicationFocus",false);Check(voices.All(v=>!v.isPlaying),"Focus loss stops audio");Call(game,"ResetToMenu");
        var fallback=typeof(SkyPulseNativeGame).GetMethod("LoadAudioCue",BindingFlags.Static|BindingFlags.NonPublic);
        var score=(AudioClip)Get(game,"scoreSound");Check(fallback.Invoke(null,new object[]{"missing-test-cue",score})==score,"Absent optional sound safely falls back to an existing clip");
        Debug.Log("SKYPULSE_DIMENSIONAL_AUDIO_PASS");yield return null;
    }
    static IEnumerator Environment(SkyPulseNativeGame game)
    {
        Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"StartFlight");Call(game,"UpdateCinematicPresentation",1f);
        var env=(SkyPulseFlightEnvironment)Get(game,"flightEnvironment");var camera=(Camera)Get(game,"flightCamera");
        Check(env!=null&&env.GetComponentsInChildren<MeshRenderer>(true).Length==36,"Active flight uses two pooled 18-piece real mesh banks");
        Check(camera.orthographic&&camera.transform.position==new Vector3(0,0,-10)&&camera.nearClipPlane<1&&camera.farClipPlane>30,"Original orthographic camera contains the architecture depth range");
        Check(env.GetComponentsInChildren<Collider>(true).Length==0&&env.GetComponentsInChildren<Collider2D>(true).Length==0&&env.GetComponentsInChildren<Camera>(true).Length==0,"Scenery owns no colliders or extra camera");
        foreach(var r in env.GetComponentsInChildren<MeshRenderer>(true))Check(r.sortingOrder<0&&r.sharedMaterial.shader.isSupported,"All scenery stays behind gameplay with supported shader");
        var meshes=(Mesh[])Get(env,"silhouettes");Check(meshes.Length==12&&new HashSet<Mesh>(meshes).Count==12&&meshes.All(m=>m.bounds.size.z>.2f),"Twelve unique mesh signatures have genuine volume");
        foreach(int fps in new[]{30,60,120})
        {
            env.ResetView();for(int frame=0;frame<=fps;frame++)env.Present(0,frame*4f/fps,frame/(float)fps,1,Color.cyan,Color.black,false);
            var transforms=(Transform[,])Get(env,"scenery");var expected=transforms[1,7].localPosition;
            env.Present(0,4,1,1,Color.cyan,Color.black,false);Check((expected-transforms[1,7].localPosition).sqrMagnitude<.000001f,"Absolute-distance parallax agrees at "+fps+" FPS");
        }
        var items=(Transform[,])Get(env,"scenery");env.Present(0,20,4,1,Color.cyan,Color.black,true);var reduced=items[1,7].localPosition;env.Present(0,30,8,1,Color.cyan,Color.black,true);Check(reduced==items[1,7].localPosition,"Reduced Motion freezes scenery travel and idle drift");
        for(int world=0;world<12;world++)
        {
            env.Present(world,10,1,0,Color.cyan,Color.black,false);env.Present(world,10,1,.5f,Color.cyan,Color.black,false);
            var mats=(Material[])Get(env,"surfaces");Check(Mathf.Abs(mats[1].GetFloat("_Visibility")-.5f)<.001f||world==0,"Incoming geometry follows world dissolve");
            env.Present(world,10,1,1,Color.cyan,Color.black,false);Check(env.CurrentWorld==world,"World signature committed "+world);
        }
        env.Present(0,10,1,1,Color.cyan,Color.black,false);for(int i=0;i<3;i++)env.Present(0,10,1,1,Color.cyan,Color.black,false);
        long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<120;i++)env.Present(0,i/30f,i/60f,1,Color.cyan,Color.black,false);Check(GC.GetAllocatedBytesForCurrentThread()==before,"Warmed architecture update has zero managed allocations");
        yield return RenderEnvironment(game,env,camera);
        Call(game,"PauseFlight");var frozen=items[1,7].localPosition;Call(game,"UpdateCinematicPresentation",1f);Check(frozen==items[1,7].localPosition,"Pause freezes flight scenery");Call(game,"ResumeFlight");
        Call(game,"ResetToMenu");Call(game,"StartFlight");Call(game,"UpdateCinematicPresentation",0f);Check(env.Travel==0,"Retry resets scenery distance");
        Call(game,"SetHomeProtocol",SkyPulseMode.PulseRelay);Call(game,"ResetToMenu");
        var home=(GameObject)Get(game,"homeScreen");var map=(Button)Get(game,"modeHomeMap");var guide=(Button)Get(game,"modeHomeGuide");var hangar=(RectTransform)Get(game,"homeHangar");
        Check(((RectTransform)map.transform).anchoredPosition.y==hangar.anchoredPosition.y&&((RectTransform)guide.transform).anchoredPosition.y==hangar.anchoredPosition.y,"Relay secondary actions occupy one calm row");
        Check(!((Text)Get(game,"menuHangarProgressText")).gameObject.activeSelf&&!((Text)Get(game,"menuCrystalText")).transform.parent.gameObject.activeSelf,"Relay removes duplicate collection/wallet clutter");
        Check(home.transform.Find("Button · MISSION CONTROL").gameObject.activeSelf&&home.transform.Find("Button · PRIVACY").gameObject.activeSelf,"Mission Control and privacy remain accessible");
        yield return null;SkyPulsePlayerExperienceChecks.CaptureUi(game,"relay-home-refined");
        Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");Check(hangar.anchoredPosition==new Vector2(-187,-355)&&((RectTransform)guide.transform).sizeDelta==new Vector2(660,96),"Switching restores original Endless layout");
        var cleanup=new GameObject("Scenery lifecycle fixture");var owned=cleanup.AddComponent<SkyPulseFlightEnvironment>();owned.Build(12);
        var ownedMeshes=(Mesh[])Get(owned,"silhouettes");var ownedMaterials=(Material[])Get(owned,"surfaces");
        UnityEngine.Object.Destroy(cleanup);yield return null;yield return null;
        Check(ownedMeshes.All(m=>m==null)&&ownedMaterials.All(m=>m==null),"Owned environment meshes and materials are released on destroy");
        Debug.Log("SKYPULSE_FLIGHT_ENVIRONMENT_PASS");
    }
    static IEnumerator RenderEnvironment(SkyPulseNativeGame game,SkyPulseFlightEnvironment env,Camera camera)
    {
        var prior=camera.targetTexture;var active=RenderTexture.active;
        var target=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=target;
        var image=new Texture2D(750,1334,TextureFormat.RGB24,false);
        var worlds=(Array)typeof(SkyPulseNativeGame).GetField("Worlds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        for(int world=0;world<12;world++)
        {
            Set(game,"routeWorld",worlds.GetValue(world));Set(game,"routeWorldIndex",world);
            var accent=(Color)Get(worlds.GetValue(world),"Accent");var haze=(Color)Get(worlds.GetValue(world),"Floor");
            env.Present(world,4,2,1,accent,haze,false);yield return null;
            env.gameObject.SetActive(false);camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,750,1334),0,0);image.Apply();var without=image.GetPixels32();
            env.gameObject.SetActive(true);camera.Render();image.ReadPixels(new Rect(0,0,750,1334),0,0);image.Apply();var with=image.GetPixels32();
            int differences=0;for(int i=0;i<with.Length;i++)if(Math.Abs(with[i].r-without[i].r)+Math.Abs(with[i].g-without[i].g)+Math.Abs(with[i].b-without[i].b)>8)differences++;
            Check(differences>250,"Real architecture contributes visible camera pixels in world "+world+": "+differences);
            var folder=System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"architecture");System.IO.Directory.CreateDirectory(folder);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,"world-"+world.ToString("00")+".png"),image.EncodeToPNG());
        }
        camera.targetTexture=prior;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.Destroy(image);
    }

}
