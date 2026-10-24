// Editor-only focused checks and authoring generation in a disposable product.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulseAtmospherePipeChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string name)=>o.GetType().GetField(name,F).GetValue(o);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,F).Invoke(o,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("ATMOSPHERE_PIPE_CHECK: "+message);}
    static SkyPulseAtmospherePipeChecks(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");
        const string output="Assets/Resources/SkyPulse/PipeEdits";
        if(Directory.Exists(output))throw new Exception("The disposable fixture must start without pipe overrides.");
        var scene=EditorSceneManager.OpenScene(SkyPulsePipeWorkshop.Folder+"/Scenes/01 Neon City Pipes.unity",OpenSceneMode.Additive);
        try
        {
            var root=scene.GetRootGameObjects().Select(g=>g.GetComponent<SkyPulsePipeAuthoring>()).First(r=>r!=null);
            root.LightRate=1.25f;SkyPulsePipeWorkshop.Bake(root);
        }
        finally{EditorSceneManager.CloseScene(scene,true);}
        SessionState.SetBool("AtmospherePipeQa",true);EditorApplication.EnterPlaymode();
    }
    public static void CreateAuthoring()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");
        SkyPulsePipeWorkshop.Create();SkyPulseBackgroundWorkshop.CreateHorizons();
        ValidateAuthoring();
    }
    public static void ValidateAndRun()
    {
        ValidateAuthoring();Run();
    }
    static void ValidateAuthoring()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");
        foreach(var path in Directory.GetFiles(SkyPulsePipeWorkshop.Folder+"/Scenes","*.unity"))
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var root=scene.GetRootGameObjects().Select(g=>g.GetComponent<SkyPulsePipeAuthoring>()).First(r=>r!=null);SkyPulsePipeWorkshop.Bake(root);
                var data=Resources.Load<SkyPulsePipeTheme>("SkyPulse/PipeEdits/world-"+root.World.ToString("00"));
                Check(data!=null&&data.Barrel!=null&&data.Collar!=null,"Both reusable pipe parts bake: "+root.World);
                Check(Vector3.Distance(data.Barrel.bounds.size,root.BarrelBounds.size)<.001f&&Vector3.Distance(data.Collar.bounds.size,root.CollarBounds.size)<.001f,"Bake retains tested pipe dimensions: "+root.World);
                var original=root.LightRate;root.LightRate=1.25f;SkyPulsePipeWorkshop.Bake(root);Check(Mathf.Abs(data.LightRate-1.25f)<.001f,"LED-rate edits survive saving: "+root.World);root.LightRate=original;
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
        AssetDatabase.DeleteAsset("Assets/Resources/SkyPulse/PipeEdits");
        foreach(var path in Directory.GetFiles(SkyPulseBackgroundWorkshop.Folder+"/Distant Scenes","*.unity"))
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var root=scene.GetRootGameObjects().Select(g=>g.GetComponent<SkyPulseBackgroundAuthoring>()).First(r=>r!=null);SkyPulseBackgroundWorkshop.Bake(root);
                Check(Resources.Load<Mesh>("SkyPulse/BackgroundEdits/world-"+root.World.ToString("00")+"-horizon")!=null,"Distant skyline bakes: "+root.World);
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
        AssetDatabase.DeleteAsset("Assets/Resources/SkyPulse/BackgroundEdits");AssetDatabase.SaveAssets();Debug.Log("SKYPULSE_ATMOSPHERE_PIPE_AUTHORING_PASS");
    }
    static void Tick()
    {
        if(!SessionState.GetBool("AtmospherePipeQa",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("AtmospherePipeQa",false);new GameObject("Atmosphere and pipe checks").AddComponent<SkyPulseWorldExpansionRunner>().Begin(Live());
    }
    static IEnumerator Live()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();if(game==null)game=new GameObject("QA game").AddComponent<SkyPulseNativeGame>();game.enabled=false;Call(game,"StartFlight");Call(game,"UpdateCinematicPresentation",1f);
        var env=(SkyPulseFlightEnvironment)Get(game,"flightEnvironment");var air=(SkyPulseAtmosphereDepth)Get(env,"atmosphereDepth");var horizons=(Mesh[])Get(air,"horizons");var stars=(Mesh[])Get(air,"stars");var layers=(Transform[,])Get(air,"layers");var materials=(Material[,])Get(air,"materials");
        Check(horizons.Length==12&&horizons.All(m=>m!=null&&m.vertexCount<12000),"Twelve bounded low-detail distant skylines");Check(stars.All(m=>m.vertexCount==36),"Nine LED sky stars use one tiny cached mesh per world");
        for(int world=0;world<12;world++)
        {
            air.Present(1,world,4,2,0,Color.cyan,Color.black);air.Present(1,world,4,2,1,Color.cyan,Color.black);
            Check(layers[1,0].GetComponent<MeshFilter>().sharedMesh==horizons[world]&&layers[1,3].GetComponent<MeshFilter>().sharedMesh==stars[world],"Hidden incoming world binds its own geometry: "+world);
        }
        foreach(int fps in new[]{30,60,120})
        {
            for(int frame=0;frame<=fps;frame++)env.Present(0,frame*4f/fps,frame/(float)fps,1,Color.cyan,Color.black,false);
            var position=layers[1,0].localPosition;env.Present(0,4,1,1,Color.cyan,Color.black,false);Check((position-layers[1,0].localPosition).sqrMagnitude<.000001f,"Atmospheric parallax agrees at "+fps+" Hz");
        }
        env.Present(0,30,8,1,Color.cyan,Color.black,true);var frozen=layers[1,0].localPosition;env.Present(0,70,13,1,Color.cyan,Color.black,true);
        Check(frozen==layers[1,0].localPosition&&materials[1,2].GetFloat("_Clock")==0,"Reduced Motion freezes atmospheric travel and LED shimmer");
        env.Present(0,2,1,1,Color.cyan,Color.black,false);for(int i=0;i<30;i++)env.Present(0,i,1,1,Color.cyan,Color.black,false);
        long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<200;i++)env.Present(0,i,1,1,Color.cyan,Color.black,false);Check(GC.GetAllocatedBytesForCurrentThread()==before,"Steady scenery updates allocate no managed memory");
        var bodies=(Mesh[])Get(game,"routeBodyThemes");var cached=(Mesh[])bodies.Clone();Call(game,"RestartFlight");Call(game,"UpdateDepthPresentation",0f);Check(bodies.SequenceEqual(cached),"Retry reuses the twelve cached pipe assemblies");
        var authored=(SkyPulsePipeTheme[])Get(game,"authoredPipeThemes");
        Check(authored[0]!=null&&bodies[0]!=authored[0].Barrel&&Vector3.Distance(bodies[0].bounds.size,authored[0].Barrel.bounds.size)<.001f,"Runtime owns a fitted copy of the authored pipe mesh");
        Check(Mathf.Abs(((Material)Get(game,"routeMetal")).GetFloat("_LightRate")-1.25f)<.001f,"Baked LED speed reaches the actual gameplay material");
        Call(game,"BeginWorldTransition",1);Call(game,"UpdateWorldTransition",.65f);Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);Call(game,"UpdateCinematicPresentation",0f);
        Check(!((SpriteRenderer)Get(game,"backgroundRenderer")).enabled&&!((SpriteRenderer)Get(game,"incomingBackground")).enabled&&!((SpriteRenderer)Get(game,"backgroundVeil")).enabled,"Switching from an unfinished Endless dissolve cannot overlay Relay's station");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);Call(game,"UpdateCinematicPresentation",0f);
        Check(!((SpriteRenderer)Get(game,"incomingBackground")).enabled,"Blackout also excludes the previous world's dissolve");
        var first=((System.Collections.IList)Get(game,"routeDepth"))[0];Check(((MeshFilter)Get(first,"BodyFilter")).sharedMesh==bodies[0],"Every gate binds a shared cached pipe mesh");
        foreach(string shader in new[]{"AtmosphereStars","AtmosphereVeil","OrbitalStructure","FlightArchitecture","EndlessRouteMetal"})
        {
            var source=Resources.Load<Shader>("SkyPulse/Shaders/"+shader);Check(source!=null&&source.isSupported,"Supported mobile shader: "+shader);
            foreach(var message in ShaderUtil.GetShaderMessages(source))if(message.severity.ToString()=="Error")throw new Exception(message.message);
        }
        Debug.Log("ATMOSPHERE_GEOMETRY_BUDGET: "+horizons.Sum(m=>m.vertexCount)+" distant vertices; "+stars.Sum(m=>m.vertexCount)+" star vertices; eight pooled renderers");
        var holder=new GameObject("Atmosphere lifecycle fixture");var owned=new GameObject("Owned atmosphere").AddComponent<SkyPulseAtmosphereDepth>();owned.Build(holder.transform);
        var ownedMeshes=((Mesh[])Get(owned,"horizons")).Concat((Mesh[])Get(owned,"stars")).Append((Mesh)Get(owned,"cloud")).ToArray();var ownedMaterials=((Material[,])Get(owned,"materials")).Cast<Material>().ToArray();
        UnityEngine.Object.Destroy(holder);yield return null;yield return null;
        Check(ownedMeshes.All(m=>m==null)&&ownedMaterials.All(m=>m==null),"Atmosphere destruction releases all owned meshes and materials");
        authored[0]=null;game.GetType().GetField("routeDepthTheme",F).SetValue(game,-1);Call(game,"UpdateDepthPresentation",0f);
        AssetDatabase.DeleteAsset("Assets/Resources/SkyPulse/PipeEdits");AssetDatabase.SaveAssets();Debug.Log("SKYPULSE_ATMOSPHERE_PIPE_LIVE_PASS");
        if(System.Environment.GetEnvironmentVariable("SKYPULSE_ATMOSPHERE_PREVIEWS")=="1")
        {
            yield return SkyPulseWorldStyleChecks.Capture(game);
            yield return SkyPulsePresentationChecks.Captures(game);
            Debug.Log("SKYPULSE_ATMOSPHERE_PREVIEWS_PASS");
        }
        EditorApplication.Exit(0);
    }
}
