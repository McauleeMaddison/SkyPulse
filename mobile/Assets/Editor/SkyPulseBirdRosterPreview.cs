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
    static readonly string[] Sprites={"birdRenderer","birdFlapRenderer","birdRiseRenderer","birdParallaxRenderer","birdSafetyRenderer","birdDepthRenderer"};
    static readonly Dictionary<SpriteRenderer,bool> originals=new Dictionary<SpriteRenderer,bool>();
    static GameObject holder;static SkyPulseNeonFinchPrototype rig;static string loaded;static float lastSample,flapStart,lastWing;
    static SkyPulseBirdRosterWorkshop.Design[] designs;
    public static SkyPulseNeonFinchPrototype CurrentRig=>rig;
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
    {SessionState.SetString(BirdKey,id??"");SessionState.SetBool(ActiveKey,enabled);if(!enabled)Clear();}
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
        if(!visible){if(holder!=null)holder.SetActive(false);RestoreSprites();return;}
        var bird=(Transform)Get(game,"bird");if(bird==null)return;
        string id=SelectedId;if(string.IsNullOrEmpty(id))id=(string)Get(Get(game,"equippedSkin"),"Id");
        if(holder==null||loaded!=id)
        {
            Clear();designs=designs??SkyPulseBirdRosterWorkshop.LoadDesigns();
            var design=designs.FirstOrDefault(d=>d.id==id);
            if(design==null&&id!="neon_finch")
            {string art=(string)Get(Get(game,"equippedSkin"),"ArtPath");design=designs.FirstOrDefault(d=>art!=null&&art.EndsWith(d.reference,StringComparison.Ordinal));}
            string path=design==null?SkyPulseBirdPrototypeWorkshop.PrefabPath:SkyPulseBirdRosterWorkshop.PrefabPath(design);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new InvalidOperationException("Create the saved roster model first: "+id);
            holder=new GameObject("Editor 3D bird visual review"){hideFlags=HideFlags.HideAndDontSave};holder.transform.SetParent(bird,false);holder.transform.localPosition=new Vector3(0,0,-3);
            var model=UnityEngine.Object.Instantiate(prefab,holder.transform);model.name=id+" · live visual preview";rig=model.GetComponent<SkyPulseNeonFinchPrototype>();rig.animate=false;rig.turntable=false;rig.ResetPose();
            Bounds bounds=model.GetComponentsInChildren<Renderer>()[0].bounds;foreach(var r in model.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
            float scale=2.30f/Mathf.Max(.01f,bounds.size.x);holder.transform.localScale=Vector3.one*scale;holder.transform.localRotation=Quaternion.Euler(0,-9,0);
            foreach(var t in holder.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
            CreateLight(new Vector3(-2,4,-4),1.2f);CreateLight(new Vector3(2,1,3),.65f);
            loaded=id;flapStart=Time.unscaledTime;lastWing=0;
        }
        holder.SetActive(true);rig.reducedMotion=(bool)Get(game,"reduceMotionEnabled");
        float wing=(float)Get(game,"wingTimer");if(wing<lastWing)flapStart=Time.unscaledTime;lastWing=wing;
        if(forcedTime>=0)lastSample=forcedTime;else if(state!="Paused")lastSample=Time.unscaledTime-flapStart;
        rig.SamplePose(lastSample);
        foreach(string name in Sprites)
        {var renderer=Get(game,name) as SpriteRenderer;if(renderer==null)continue;if(!originals.ContainsKey(renderer))originals.Add(renderer,renderer.enabled);renderer.enabled=false;}
    }
    static void CreateLight(Vector3 at,float intensity)
    {var o=new GameObject("Preview-only bird studio light",typeof(Light));o.transform.SetParent(holder.transform,false);o.transform.localPosition=at;o.transform.LookAt(holder.transform.position);var light=o.GetComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<Layer;light.color=Color.white;light.intensity=intensity;light.shadows=LightShadows.None;}
    static void RestoreSprites(){foreach(var pair in originals)if(pair.Key!=null)pair.Key.enabled=pair.Value;originals.Clear();}
    public static void Clear(){RestoreSprites();if(holder!=null)UnityEngine.Object.DestroyImmediate(holder);holder=null;rig=null;loaded=null;}
}
