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
public static class SkyPulseOrbitalWorldChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static void Check(bool good,string text){if(!good)throw new Exception(text);Debug.Log("ORBITAL_WORLD_CHECK: "+text);}
    static SkyPulseOrbitalWorldChecks(){EditorApplication.update+=Tick;}
    public static void CreateAndRun()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");
        ConfigureSurface();
        SkyPulseBackgroundWorkshop.CreateOrbitalWorld();Run();
    }
    public static void RepairAndRun()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");
        SkyPulseBackgroundWorkshop.RepairWorkshopAssetNames();Run();
    }
    static void ConfigureSurface()
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/SkyPulse/materials/orbital-alloy-v1.png");
        importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=true;importer.sRGBTexture=true;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Mirror;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=2;importer.textureCompression=TextureImporterCompression.CompressedHQ;
        var platform=importer.GetPlatformTextureSettings("iPhone");platform.overridden=true;platform.maxTextureSize=2048;platform.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(platform);importer.SaveAndReimport();
    }
    public static void Run()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");
        ValidateAuthoring();SessionState.SetBool("OrbitalWorldQa",true);EditorApplication.EnterPlaymode();
    }
    static void ValidateAuthoring()
    {
        var scene=EditorSceneManager.OpenScene(SkyPulseBackgroundWorkshop.Folder+"/Pulse Relay World/Scenes/Pulse Relay Orbital World.unity",OpenSceneMode.Additive);
        var root=scene.GetRootGameObjects().Select(g=>g.GetComponent<SkyPulseBackgroundAuthoring>()).First(r=>r!=null);var tower=root.transform.Find("East transit tower");var mat=tower.GetComponent<MeshRenderer>().sharedMaterial;var tint=mat.GetColor("_Tint");
        GameObject fixture=null;
        try
        {
            Check(scene.GetRootGameObjects().Length==2&&scene.GetRootGameObjects().Count(g=>g.GetComponent<Camera>()!=null)==1,"Authoring scene contains only its editable root and one camera");
            Check(root.GetComponentsInChildren<MeshFilter>().All(f=>AssetDatabase.Contains(f.sharedMesh))&&root.GetComponentsInChildren<MeshRenderer>().All(r=>AssetDatabase.Contains(r.sharedMaterial)),"All authoring meshes and materials are persistent assets rather than embedded runtime copies");
            Check(root.GetComponentsInChildren<MeshFilter>().Select(f=>(UnityEngine.Object)f.sharedMesh).Concat(root.GetComponentsInChildren<MeshRenderer>().Select(r=>(UnityEngine.Object)r.sharedMaterial)).Distinct().All(asset=>asset.name==Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(asset))),"Every exported mesh and material main-object name matches its filename");
            Check(root.transform.childCount==33&&root.transform.Find("Distant station artwork")==null,"All 33 world objects are individually editable without a panorama quad");
            CheckConnections(root.transform,"Saved authoring scene");
            Check(root.transform.Find("East transit tower").GetComponent<MeshFilter>().sharedMesh.isReadable,"Habitat mesh stays readable for modelling tools in the Editor");
            tower.localPosition+=Vector3.right*.65f;mat.SetColor("_Tint",Color.cyan);root.transform.Find("Distant blue planet").gameObject.SetActive(false);
            var extra=UnityEngine.Object.Instantiate(root.transform.Find("East lower concourse").gameObject,root.transform);extra.name="Observation platform";extra.transform.localPosition=new Vector3(0,-10,40);
            SkyPulseBackgroundWorkshop.Bake(root);
            fixture=new GameObject("Baked station verification");var runtime=fixture.AddComponent<SkyPulseRelayEnvironment>();runtime.Build(18f*9/16,Resources.Load<Texture2D>("SkyPulse/backgrounds/pulse-relay-station-v1"));
            var world=(SkyPulseOrbitalWorld)Get(runtime,"orbitalWorld");runtime.SetVisual(0,2);
            Check(Mathf.Abs(world.Find(tower.name).localPosition.x-tower.localPosition.x)<.002f,"Authored tower movement reaches the running station");
            var first=world.Find(tower.name).localPosition;for(int i=0;i<10;i++)runtime.SetVisual(0,2);Check(first==world.Find(tower.name).localPosition,"Fixed structure offsets never accumulate");
            var actual=world.Find(tower.name).GetComponent<MeshRenderer>().sharedMaterial;Check(actual.GetColor("_Tint")==Color.cyan&&actual.GetFloat("_Clock")==2,"Baked material changes retain live animation");
            Check(!world.Find("Distant blue planet").gameObject.activeSelf,"A disabled planet remains disabled after baking");
            Check(runtime.WorldCamera.transform.parent.Find("Observation platform")!=null,"Additional authored structure loads into the world");
        }
        finally
        {
            if(fixture!=null){fixture.GetComponent<SkyPulseRelayEnvironment>().ReleaseAuthoringResources();UnityEngine.Object.DestroyImmediate(fixture);}mat.SetColor("_Tint",tint);EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();EditorSceneManager.CloseScene(scene,true);
            AssetDatabase.DeleteAsset("Assets/Resources/SkyPulse/BackgroundEdits");
        }
        Debug.Log("SKYPULSE_ORBITAL_AUTHORING_PASS");
    }
    static void Tick()
    {
        if(!SessionState.GetBool("OrbitalWorldQa",false)||!EditorApplication.isPlaying)return;SessionState.SetBool("OrbitalWorldQa",false);new GameObject("Orbital QA").AddComponent<SkyPulseWorldExpansionRunner>().Begin(Live());
    }
    static IEnumerator Live()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();if(game==null)game=new GameObject("QA game").AddComponent<SkyPulseNativeGame>();game.enabled=false;Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);Call(game,"UpdateCinematicPresentation",0f);
        var env=(SkyPulseRelayEnvironment)Get(game,"relayEnvironment");var world=(SkyPulseOrbitalWorld)Get(env,"orbitalWorld");var renderers=env.WorldCamera.transform.parent.GetComponentsInChildren<MeshRenderer>();
        Check(renderers.Length==33&&world.StructureCount==29,"Bounded station uses 33 pooled surfaces with 29 authored world objects");
        Check(renderers.All(r=>r.name!="Distant station artwork"&&r.sharedMaterial.shader.isSupported),"Every scene shader is supported and there is no panorama plane");
        Check(renderers.Select(r=>r.GetComponent<MeshFilter>().sharedMesh).Distinct().Count()==11,"Station objects share eleven cached model meshes");
        foreach(var mesh in renderers.Select(r=>r.GetComponent<MeshFilter>().sharedMesh).Distinct())Check(mesh.vertexCount<60000,"16-bit mesh budget: "+mesh.name+" / "+mesh.vertexCount);
        var terminal=world.Find("Suspended launch hub").GetComponent<MeshFilter>().sharedMesh;
        Check(terminal.bounds.min.y> -1.1f&&terminal.name.Contains("observation terminal"),"Observation terminal has a cabin and compact underside without the old long spindle");
        var stars=world.Find("Orbital LED star field").GetComponent<MeshFilter>().sharedMesh;
        Check(stars.vertexCount==28*4&&stars.uv.Length==stars.vertexCount,"Twenty-eight background LED lenses share one textured-coordinate mesh");
        Check(world.Find("Angled arrival apron").GetComponent<MeshFilter>().sharedMesh.name.Contains("service hatches"),"Arrival deck includes framed edges, service hatches and structural supports");
        Check(env.WorldCamera.transform.parent.GetComponentsInChildren<Collider>(true).Length==0&&env.WorldCamera.transform.parent.GetComponentsInChildren<Collider2D>(true).Length==0,"New world cannot alter flight collision");
        foreach(int fps in new[]{30,60,120}){for(int i=0;i<=fps;i++)env.SetVisual(i*4f/fps,i/(float)fps);var pose=world.Find("Distant blue planet").localRotation;var position=world.Find("East transit tower").localPosition;env.SetVisual(4,1);Check(Quaternion.Angle(pose,world.Find("Distant blue planet").localRotation)<.001f&&Vector3.Distance(position,world.Find("East transit tower").localPosition)<.0001f,"Planet and depth parallax are independent of frame rate at "+fps);}
        foreach(float distance in new[]{0f,65f,170f,300f}){env.SetVisual(distance,3);CheckConnections(world.transform,"Runtime distance "+distance);}
        for(int i=0;i<10;i++)env.SetVisual(4,1);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)env.SetVisual(4,i/60f);Check(GC.GetAllocatedBytesForCurrentThread()==before,"Warmed 3D station presentation allocates no managed memory");
        foreach(string name in new[]{"OrbitalAtmosphere","OrbitalPlanet","OrbitalStructure","AtmosphereStars"})foreach(var message in ShaderUtil.GetShaderMessages(Resources.Load<Shader>("SkyPulse/Shaders/"+name)))if(message.severity.ToString()=="Error")throw new Exception(message.message);
        Debug.Log("ORBITAL_WORLD_BUDGET: "+renderers.Select(r=>r.GetComponent<MeshFilter>().sharedMesh).Distinct().Sum(m=>m.vertexCount)+" unique vertices / "+renderers.Length+" surfaces");
        env.SetViewport(18f*941/1672);env.SetVisual(0,0);env.WorldCamera.Render();
        var prior=RenderTexture.active;RenderTexture.active=env.WorldImage;var preview=new Texture2D(env.WorldImage.width,env.WorldImage.height,TextureFormat.RGB24,false);preview.ReadPixels(new Rect(0,0,preview.width,preview.height),0,0);preview.Apply();
        File.WriteAllBytes(Path.Combine(System.Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"world-only.png"),preview.EncodeToPNG());UnityEngine.Object.Destroy(preview);RenderTexture.active=prior;env.SetViewport((float)Call(game,"GetViewportWidth"));
        CaptureDetail(env,world.Find("East middle concourse").position,new Vector3(-15,5,-18),new Vector3(-3,0,0),"bridge-detail.png");
        CaptureDetail(env,world.Find("Suspended launch hub").position,new Vector3(6,1.3f,-9),Vector3.zero,"terminal-detail.png");
        CaptureDetail(env,world.Find("Angled arrival apron").position,new Vector3(7,10,-10),new Vector3(0,0,7),"arrival-detail.png");
        Call(game,"PauseFlight");float clock=(float)Get(game,"protocolVisualTime");Call(game,"UpdateProtocolPresentation",1f);Check((float)Get(game,"protocolVisualTime")==clock,"Pause freezes the station presentation clock");Call(game,"ResumeFlight");
        game.GetType().GetField("reduceMotionEnabled",F).SetValue(game,true);Call(game,"UpdateProtocolPresentation",1f);Call(game,"UpdateRelayPresentation");Check(world.Find("Distant blue planet").localRotation==Quaternion.Euler(0,0,-18),"Reduced Motion freezes the new planet rotation");game.GetType().GetField("reduceMotionEnabled",F).SetValue(game,false);
        var ownedMeshes=renderers.Select(r=>r.GetComponent<MeshFilter>().sharedMesh).Distinct().ToArray();var ownedMaterials=renderers.Select(r=>r.sharedMaterial).Distinct().ToArray();
        Debug.Log("SKYPULSE_ORBITAL_WORLD_PASS");yield return SkyPulsePresentationChecks.Captures(game);Call(game,"ResetToMenu");yield return null;yield return null;
        Check(ownedMeshes.All(m=>m==null)&&ownedMaterials.All(m=>m==null),"Leaving a course releases all owned station meshes and materials");
        Debug.Log("SKYPULSE_ORBITAL_CAPTURE_PASS");EditorApplication.Exit(0);
    }
    static void CheckConnections(Transform root,string label)
    {
        foreach(string level in new[]{"upper","middle","lower","arrival"})
        {
            var route=root.Find(char.ToUpperInvariant(level[0])+level.Substring(1)+" transit bridge");
            var start=route.TransformPoint(Vector3.left*7);var end=route.TransformPoint(Vector3.right*7);
            var west=root.Find("West "+level+" concourse").InverseTransformPoint(start);var east=root.Find("East "+level+" concourse").InverseTransformPoint(end);
            Check(OnDeck(west)&&OnDeck(east),label+": "+level+" bridge overlaps both real concourse decks");
        }
        var gantry=root.Find("Overhead optical gantry");var hub=root.Find("Suspended launch hub");
        var exit=hub.InverseTransformPoint(gantry.TransformPoint(Vector3.left*7));
        var entry=root.Find("East upper concourse").InverseTransformPoint(gantry.TransformPoint(Vector3.right*7));
        Check(Mathf.Abs(exit.y)<.01f&&new Vector2(exit.x,exit.z).magnitude>1.3f&&new Vector2(exit.x,exit.z).magnitude<3.8f&&OnDeck(entry),label+": launch gantry connects the hub deck to the upper concourse");
    }
    static bool OnDeck(Vector3 point){float radius=new Vector2(point.x,point.z).magnitude;return Mathf.Abs(point.y)<.01f&&radius>5.30f&&radius<6.20f;}
    static void CaptureDetail(SkyPulseRelayEnvironment env,Vector3 anchor,Vector3 offset,Vector3 lookOffset,string filename)
    {
        var camera=env.WorldCamera;var position=camera.transform.position;var rotation=camera.transform.rotation;float aspect=camera.aspect;var output=camera.targetTexture;
        var image=new RenderTexture(1400,900,16,RenderTextureFormat.ARGB32){antiAliasing=2};image.Create();camera.targetTexture=image;camera.aspect=1400f/900;
        camera.transform.position=anchor+offset;camera.transform.LookAt(anchor+lookOffset);camera.Render();
        var prior=RenderTexture.active;RenderTexture.active=image;var detail=new Texture2D(1400,900,TextureFormat.RGB24,false);detail.ReadPixels(new Rect(0,0,1400,900),0,0);detail.Apply();File.WriteAllBytes(Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),filename),detail.EncodeToPNG());
        RenderTexture.active=prior;camera.targetTexture=output;camera.aspect=aspect;camera.transform.SetPositionAndRotation(position,rotation);image.Release();UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(detail);
    }
}
