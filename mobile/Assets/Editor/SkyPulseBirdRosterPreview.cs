using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkyPulse.Mobile;

/// <summary>Editor-only inspection in the real game. Does not change equipped stats, rules or player preferences.</summary>
[InitializeOnLoad]
public static class SkyPulseBirdRosterPreview
{
    const string ActiveKey="SkyPulse.RosterPreview.Active",BirdKey="SkyPulse.RosterPreview.Bird";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const int Layer=26;
    static readonly string[] Sprites={"birdRenderer","birdFlapRenderer","birdRiseRenderer","birdParallaxRenderer","birdSafetyRenderer","birdDepthRenderer","birdEyeGlintRenderer"};
    static readonly Dictionary<SpriteRenderer,bool> originals=new Dictionary<SpriteRenderer,bool>();
    static GameObject holder;static SkyPulseNeonFinchPrototype rig;static string loaded;
    static float activeFlightTime,lastSample;static int sampledFrame=-1,observedRunSerial=int.MinValue;
    static float flightWingLift;static int wingFrame=-1;
    static string hitIdentity;static Sprite nativeHitSprite;static Vector3 nativeHitScale;static bool hitOverride;
    static bool clockWasPlaying;static SkyPulseNativeGame observedGame;
    static SkyPulseBirdRosterWorkshop.Design[] designs;
    public static SkyPulseNeonFinchPrototype CurrentRig=>rig;
    public static float CurrentFlightTime=>activeFlightTime;
    public static float TimeOverride=-1;
    public static bool Enabled=>SessionState.GetBool(ActiveKey,false);
    public static string SelectedId=>SessionState.GetString(BirdKey,"");
    static object Get(object value,string name)=>value?.GetType().GetField(name,Flags)?.GetValue(value);
    static SkyPulseBirdRosterPreview()
    {
        Camera.onPreCull+=BeforeCamera;
        EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.ExitingPlayMode||state==PlayModeStateChange.EnteredEditMode)Clear();};
        AssemblyReloadEvents.beforeAssemblyReload+=Clear;
    }
    public static void Select(string id,bool enabled)
    {
        string requested=id??"";bool changed=requested!=SelectedId||enabled!=Enabled;
        SessionState.SetString(BirdKey,requested);SessionState.SetBool(ActiveKey,enabled);
        // A manual selection clears any old still-capture override.
        TimeOverride=-1;
        if(!enabled||changed){Clear();SkyPulseBirdMenuPreview.Clear();}
    }
    /// <summary>One active-game clock tick per rendered frame; repeated camera renders never double it.</summary>
    public static float AdvanceFlightClock(int frame,float deltaTime,bool playing)
    {
        if(frame==sampledFrame){clockWasPlaying=playing;return activeFlightTime;}
        // The first frame establishes the epoch. A resumed frame also starts
        // from the held pose, so time spent paused never jumps the wings ahead.
        if(sampledFrame>=0&&playing&&clockWasPlaying&&float.IsFinite(deltaTime))
            activeFlightTime+=Mathf.Clamp(deltaTime,0f,.1f);
        sampledFrame=frame;clockWasPlaying=playing;return activeFlightTime;
    }
    public static void ResetFlightClock()
    {activeFlightTime=lastSample=flightWingLift=0f;sampledFrame=wingFrame=-1;clockWasPlaying=false;}
    public static void BeforeCamera(Camera camera)
    {
        if(!Enabled||!EditorApplication.isPlaying)return;
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();
        if(game==null||Get(game,"flightCamera") as Camera!=camera)return;
        UpdateNow(game);
    }
    public static void UpdateNow(SkyPulseNativeGame game,float forcedTime=-1)
    {
        if(!Enabled)return;
        if(forcedTime<0&&TimeOverride>=0)forcedTime=TimeOverride;
        string state=Get(game,"state").ToString();bool visible=(state=="Playing"||state=="Paused")&&(SkyPulseMode)Get(game,"activeMode")!=SkyPulseMode.BlackoutProtocol;
        if(!visible)
        {
            if(holder!=null)holder.SetActive(false);
            if(state=="Impact"||state=="GameOver")
            {
                // Native collision already chose its hit pose and disabled the
                // flight overlays. Do not restore the pre-flight depth layer.
                originals.Clear();ShowSelectedHit(game);
                foreach(string name in Sprites)if(name!="birdRenderer"&&Get(game,name) is SpriteRenderer overlay)overlay.enabled=false;
            }
            else{RestoreSprites();ReleaseHit(game,false);}
            ResetFlightClock();return;
        }
        ReleaseHit(game,false);
        var bird=(Transform)Get(game,"bird");if(bird==null)return;
        string id=SelectedId;if(string.IsNullOrEmpty(id))id=(string)Get(Get(game,"equippedSkin"),"Id");
        if(holder==null||loaded!=id||holder.transform.parent!=bird)
        {
            DisposeModel();ResetFlightClock();designs=designs??SkyPulseBirdRosterWorkshop.LoadDesigns();
            var design=designs.FirstOrDefault(d=>d.id==id);
            if(design==null&&id!="neon_finch")
            {string art=(string)Get(Get(game,"equippedSkin"),"ArtPath");design=designs.FirstOrDefault(d=>art!=null&&art.EndsWith(d.reference,StringComparison.Ordinal));}
            string path=design==null?SkyPulseBirdPrototypeWorkshop.PrefabPath:SkyPulseBirdRosterWorkshop.PrefabPath(design);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new InvalidOperationException("Create the saved roster model first: "+id);
            holder=new GameObject("Editor 3D bird visual review"){hideFlags=HideFlags.HideAndDontSave};holder.transform.SetParent(bird,false);holder.transform.localPosition=Vector3.zero;
            var model=UnityEngine.Object.Instantiate(prefab,holder.transform);model.name=id+" · live visual preview";rig=model.GetComponent<SkyPulseNeonFinchPrototype>();rig.animate=false;rig.turntable=false;rig.ResetPose();
            Bounds bounds=model.GetComponentsInChildren<Renderer>()[0].bounds;foreach(var r in model.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
            float scale=2.30f/Mathf.Max(.01f,bounds.size.x);holder.transform.localScale=Vector3.one*scale;holder.transform.localRotation=Quaternion.Euler(0,-9,0);
            foreach(var t in holder.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
            CreateLight(new Vector3(-2,4,-4),1.2f);CreateLight(new Vector3(2,1,3),.65f);
            loaded=id;
        }
        var serial=Get(game,"editorFlightRunSerial");int runSerial=serial is int value?value:0;
        if(observedGame!=game||observedRunSerial!=runSerial)
        {ResetFlightClock();observedGame=game;observedRunSerial=runSerial;}
        holder.SetActive(true);rig.reducedMotion=(bool)Get(game,"reduceMotionEnabled");
        // The native frame controller owns accepted taps and mid-stroke
        // reversals. Follow that state; the ambient clock never invents flaps.
        lastSample=forcedTime>=0?forcedTime:AdvanceFlightClock(Time.frameCount,Time.unscaledDeltaTime,state=="Playing");
        AdvanceWingLift(Time.frameCount,Time.unscaledDeltaTime,state=="Paused",NativeWingLift(game));
        if(forcedTime>=0)rig.SampleFlightPose(lastSample);else rig.SampleFlightPose(lastSample,flightWingLift);
        foreach(string name in Sprites)
        {var renderer=Get(game,name) as SpriteRenderer;if(renderer==null)continue;if(!originals.ContainsKey(renderer))originals.Add(renderer,renderer.enabled);renderer.enabled=false;}
    }
    public static float AdvanceWingLift(int frame,float deltaTime,bool paused,float target)
    {
        if(wingFrame==frame)return flightWingLift;
        if(!paused&&float.IsFinite(deltaTime)&&float.IsFinite(target))
            flightWingLift=Mathf.MoveTowards(flightWingLift,Mathf.Clamp01(target),Mathf.Clamp(deltaTime,0,.1f)*8f);
        wingFrame=frame;return flightWingLift;
    }
    public static float NativeWingLift(SkyPulseNativeGame game)
    {
        var frames=Get(game,"flapFrameBirdSprites") as Sprite[];
        if(frames==null||frames.Length<2||Get(game,"gameplayWingState").ToString()=="Settled")return 0;
        return 1f-Mathf.Clamp01((int)Get(game,"gameplayWingFrameIndex")/(float)(frames.Length-1));
    }
    static void ShowSelectedHit(SkyPulseNativeGame game)
    {
        string id=SelectedId;if(string.IsNullOrEmpty(id))return;
        var skin=game.GetType().GetMethod("FindBird",Flags).Invoke(game,new object[]{id});
        if(skin==null)return;
        var renderer=Get(game,"birdRenderer") as SpriteRenderer;var art=Get(game,"birdArt") as Transform;
        if(renderer==null||art==null)return;
        string path=Get(skin,"HitPath") as string;
        if(hitOverride&&hitIdentity==path)return;
        var pose=game.GetType().GetMethod("LoadPresentationPose",Flags).Invoke(game,new object[]{path}) as Sprite;
        if(pose==null)return;
        if(!hitOverride){nativeHitSprite=renderer.sprite;nativeHitScale=art.localScale;}
        renderer.sprite=pose;renderer.enabled=true;
        float width=(float)game.GetType().GetField("BirdDisplayWidth",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        art.localScale=Vector3.one*(width/Mathf.Max(.01f,pose.bounds.size.x));hitIdentity=path;hitOverride=true;
    }
    static void ReleaseHit(SkyPulseNativeGame game,bool restore)
    {
        if(!hitOverride)return;
        if(restore&&game!=null)
        {var renderer=Get(game,"birdRenderer") as SpriteRenderer;var art=Get(game,"birdArt") as Transform;if(renderer!=null)renderer.sprite=nativeHitSprite;if(art!=null)art.localScale=nativeHitScale;}
        hitOverride=false;hitIdentity=null;nativeHitSprite=null;
    }
    static void CreateLight(Vector3 at,float intensity)
    {var o=new GameObject("Preview-only bird studio light",typeof(Light));o.transform.SetParent(holder.transform,false);o.transform.localPosition=at;o.transform.LookAt(holder.transform.position);var light=o.GetComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<Layer;light.color=Color.white;light.intensity=intensity;light.shadows=LightShadows.None;}
    static void RestoreSprites(){foreach(var pair in originals)if(pair.Key!=null)pair.Key.enabled=pair.Value;originals.Clear();}
    static void DisposeModel()
    {RestoreSprites();if(holder!=null)UnityEngine.Object.DestroyImmediate(holder);holder=null;rig=null;loaded=null;}
    public static void Clear()
    {ReleaseHit(observedGame,true);DisposeModel();ResetFlightClock();observedGame=null;observedRunSerial=int.MinValue;TimeOverride=-1;}
}
