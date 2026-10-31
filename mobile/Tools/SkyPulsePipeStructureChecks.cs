// Destructive migration QA is restricted to the disposable Unity product.
// It never touches player saves. Run after copying Assets into the QA project:
// Unity -batchmode -projectPath <qa> -executeMethod SkyPulsePipeStructureChecks.Run
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkyPulse.Mobile;

public static class SkyPulsePipeStructureChecks
{
    const BindingFlags Static=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static readonly Type Factory=typeof(SkyPulseNativeGame).Assembly.GetType("SkyPulse.Mobile.SkyPulseRouteArchitecture");
    static int assertions;
    [Serializable] sealed class Record {public string result;public int worlds,assemblies,editableParts,bakedParts,assertions;public string[] scope;}
    sealed class Node
    {
        public string path;public Vector3 position,scale;public Quaternion rotation;public bool active;
        public string mesh;public string[] materials;
    }
    sealed class Assembly
    {
        public int world;public Bounds barrel,collar;public Color accent,energy,metal;public float rate,detail;
        public List<Node> nodes;
    }
    static void Check(bool ok,string message)
    {
        if(!ok)throw new InvalidOperationException("PIPE_STRUCTURE: "+message);
        assertions++;
    }
    static string Id(UnityEngine.Object value)
    {
        if(value==null)return "null";
        string guid;long local;return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out guid,out local)?guid+":"+local:value.name;
    }
    static string Relative(Transform root,Transform item)=>item==root?"":AnimationUtility.CalculateTransformPath(item,root);
    static Assembly Snapshot(SkyPulsePipeAuthoring root)
    {
        return new Assembly{world=root.World,barrel=root.BarrelBounds,collar=root.CollarBounds,accent=root.Accent,energy=root.Energy,metal=root.Metal,rate=root.LightRate,detail=root.Detail,
            nodes=root.GetComponentsInChildren<Transform>(true).Select(t=>{
                var filter=t.GetComponent<MeshFilter>();var renderer=t.GetComponent<MeshRenderer>();
                return new Node{path=Relative(root.transform,t),position=t.localPosition,rotation=t.localRotation,scale=t.localScale,active=t.gameObject.activeSelf,
                    mesh=filter!=null?Id(filter.sharedMesh):null,materials=renderer!=null?renderer.sharedMaterials.Select(Id).ToArray():null};
            }).ToList()};
    }
    static void Preserved(Assembly before,SkyPulsePipeAuthoring root,string context)
    {
        Check(before.world==root.World&&before.barrel==root.BarrelBounds&&before.collar==root.CollarBounds,context+" world and collision-fit bounds survive migration");
        Check(before.accent==root.Accent&&before.energy==root.Energy&&before.metal==root.Metal&&before.rate==root.LightRate&&before.detail==root.Detail,context+" finish settings survive migration");
        foreach(var old in before.nodes)
        {
            var t=string.IsNullOrEmpty(old.path)?root.transform:root.transform.Find(old.path);Check(t!=null,context+" keeps "+old.path);
            Check(t.localPosition==old.position&&t.localRotation==old.rotation&&t.localScale==old.scale&&t.gameObject.activeSelf==old.active,context+" keeps local layout "+old.path);
            var filter=t.GetComponent<MeshFilter>();var renderer=t.GetComponent<MeshRenderer>();
            Check(old.mesh==(filter!=null?Id(filter.sharedMesh):null),context+" keeps mesh identity "+old.path);
            Check((old.materials==null&&renderer==null)||(renderer!=null&&old.materials!=null&&old.materials.SequenceEqual(renderer.sharedMaterials.Select(Id))),context+" keeps material identities "+old.path);
        }
    }
    static SkyPulsePipeAuthoring Authoring(UnityEngine.SceneManagement.Scene scene)
    {
        return scene.GetRootGameObjects().Select(g=>g.GetComponent<SkyPulsePipeAuthoring>()).Single(a=>a!=null);
    }
    static void Details(SkyPulsePipeAuthoring root,string context)
    {
        int count=0;
        for(int i=0;i<4;i++)
        {
            var parent=root.transform.Find(i<2?"Collar":"Barrel");var group=parent.Find(SkyPulsePipeWorkshop.StructureVersion);
            Check(group!=null,context+" separate detail group exists");
            var detail=group.Find(SkyPulsePipeWorkshop.DetailNames[i]);Check(detail!=null,context+" editable "+SkyPulsePipeWorkshop.DetailNames[i]);
            var filter=detail.GetComponent<MeshFilter>();var renderer=detail.GetComponent<MeshRenderer>();
            Check(filter!=null&&filter.sharedMesh!=null&&filter.sharedMesh.isReadable,context+" editable readable mesh");
            Check(renderer!=null&&renderer.sharedMaterial==parent.GetComponent<MeshRenderer>().sharedMaterial,context+" inherits pipe finish");
            var b=filter.sharedMesh.bounds;
            if(i==1)
            {
                var vertices=filter.sharedMesh.vertices;float depth=root.World==3?.9f:1;
                Check(vertices.Any(v=>Mathf.Abs(v.y+.245f)<.0001f),context+" recessed bore has a physical lower floor");
                Check(vertices.Count(v=>Mathf.Abs(v.y-.30f-.4f*v.z/depth)<.0002f)>100,context+" mouth follows an inclined circular plane instead of a front-facing ellipse");
                Check(b.size.z>.80f&&b.size.x>.89f,context+" bore has true circular front-to-back volume");
            }
            Check(b.min.x>=-.5002f&&b.max.x<=.5002f&&b.min.y>=-.5002f&&b.max.y<=.5002f&&b.min.z>=-.5002f&&b.max.z<=.5002f,context+" hardware stays within original housing");
            Check(group.GetComponentsInChildren<Transform>(true).Count(t=>t.name==SkyPulsePipeWorkshop.DetailNames[i])==1,context+" no duplicated named detail");
            count++;
        }
        Check(count==4,context+" has four editable parts");
    }
    static Dictionary<string,string> AssetGuids()
    {
        return new[]{SkyPulsePipeWorkshop.Folder,SkyPulsePipeWorkshop.Output}.Where(Directory.Exists)
            .SelectMany(folder=>Directory.GetFiles(folder,"*.meta",SearchOption.AllDirectories))
            .ToDictionary(p=>p,p=>Regex.Match(File.ReadAllText(p),@"(?m)^guid: (\w+)").Groups[1].Value);
    }
    static void Capture(SkyPulsePipeAuthoring root,UnityEngine.SceneManagement.Scene scene)
    {
        string output=Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");if(string.IsNullOrEmpty(output))return;
        var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Single(c=>c.name=="Pipe inspection camera");
        var transforms=root.GetComponentsInChildren<Transform>(true);var layers=transforms.Select(t=>t.gameObject.layer).ToArray();
        var oldTarget=camera.targetTexture;float oldAspect=camera.aspect;int oldMask=camera.cullingMask;var oldClear=camera.clearFlags;var oldActive=RenderTexture.active;
        var target=new RenderTexture(600,1000,24);var pixels=new Texture2D(600,1000,TextureFormat.RGB24,false);
        try
        {
            foreach(var t in transforms)t.gameObject.layer=29;
            target.Create();camera.targetTexture=target;camera.aspect=.6f;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.Render();
            RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,600,1000),0,0);pixels.Apply();
            string folder=Path.Combine(output,"pipe-structures");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"world-"+root.World.ToString("00")+".png"),pixels.EncodeToPNG());
        }
        finally
        {
            for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];
            camera.targetTexture=oldTarget;camera.aspect=oldAspect;camera.cullingMask=oldMask;camera.clearFlags=oldClear;RenderTexture.active=oldActive;
            target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
    static HashSet<string> SerializedIds(string path)=>new HashSet<string>(Regex.Matches(File.ReadAllText(path),@"(?m)^--- !u!\d+ &(-?\d+)").Cast<Match>().Select(m=>m.Groups[1].Value));
    static void Fit(Bounds actual,Bounds expected,string context)
    {
        Check((actual.center-expected.center).sqrMagnitude<.00000001f&&(actual.size-expected.size).sqrMagnitude<.00000001f,context+" exact original fit bounds");
    }
    static Mesh CallMesh(string method,params object[] args)=>(Mesh)Factory.GetMethod(method,Static).Invoke(null,args);
    static void RuntimeMeshPolicy(int world,SkyPulseNativeGame game)
    {
        var method=typeof(SkyPulseNativeGame).GetMethod("RouteThemeMesh",BindingFlags.Instance|BindingFlags.NonPublic);
        for(int part=0;part<2;part++)
        {
            var source=(Mesh)typeof(SkyPulsePipeWorkshop).GetMethod("Readable",Static).Invoke(null,new object[]{world,part==1});
            Mesh built=null,hardware0=null,hardware1=null,copy=null;
            try
            {
                hardware0=CallMesh("Hardware",world,part==1?0:2);hardware1=CallMesh("Hardware",world,part==1?1:3);
                built=(Mesh)method.Invoke(game,new object[]{null,world,part==1});
                Check(built.vertexCount==source.vertexCount+hardware0.vertexCount+hardware1.vertexCount,"generated fallback contains each hardware part once");
                copy=(Mesh)method.Invoke(game,new object[]{source,world,part==1});
                Check(copy!=source&&copy.vertexCount==source.vertexCount,"authored mesh copied without adding unrequested hardware");
                Check(copy.vertices.SequenceEqual(source.vertices),"authored mesh vertex layout remains authoritative");
                Fit(copy.bounds,source.bounds,"authored runtime copy");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);if(built!=null)UnityEngine.Object.DestroyImmediate(built);if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);
                if(hardware0!=null)UnityEngine.Object.DestroyImmediate(hardware0);if(hardware1!=null)UnityEngine.Object.DestroyImmediate(hardware1);
            }
        }
    }
    static void AuthoredEdits(SkyPulsePipeAuthoring source,SkyPulseNativeGame game)
    {
        // Synthetic edits live only on a throwaway clone: migration must not
        // reset a user's moved part, child, material, or disabled hardware.
        var clone=UnityEngine.Object.Instantiate(source.gameObject);clone.name="Transient authored-layout regression";
        try
        {
            var root=clone.GetComponent<SkyPulsePipeAuthoring>();var collar=root.transform.Find("Collar");
            collar.localPosition+=new Vector3(.013f,.021f,-.017f);
            var custom=new GameObject("Owner detail retained");custom.transform.SetParent(collar,false);custom.transform.localPosition=new Vector3(.07f,.03f,-.09f);
            var bezel=collar.Find(SkyPulsePipeWorkshop.StructureVersion+"/"+SkyPulsePipeWorkshop.DetailNames[0]);bezel.localScale=new Vector3(.97f,.96f,.99f);bezel.gameObject.SetActive(false);
            var before=Snapshot(root);Check(SkyPulsePipeWorkshop.AddStructuralDetails(root)==0,"repeat migration adds no duplicate parts after owner edits");
            Preserved(before,root,"owner edit fixture");
            SkyPulsePipeWorkshop.Bake(root);
            var theme=AssetDatabase.LoadAssetAtPath<SkyPulsePipeTheme>(SkyPulsePipeWorkshop.Output+"/world-"+root.World.ToString("00")+".asset");
            var mesh=theme.Collar;int expected=collar.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.vertexCount);
            Check(mesh.vertexCount==expected,"disabled owner hardware is omitted from the bake");
            var runtime=(Mesh)typeof(SkyPulseNativeGame).GetMethod("RouteThemeMesh",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{mesh,root.World,true});
            try{Check(runtime.vertexCount==expected&&runtime.vertices.SequenceEqual(mesh.vertices),"runtime preserves disabled parts and authored edits without readdition");}
            finally{UnityEngine.Object.DestroyImmediate(runtime);}
        }
        finally{UnityEngine.Object.DestroyImmediate(clone);}
    }
    public static void CaptureAll()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Disposable QA required");
        for(int world=0;world<12;world++)
        {
            var scene=EditorSceneManager.OpenScene(SkyPulsePipeWorkshop.ScenePath(world),OpenSceneMode.Additive);
            try{Capture(Authoring(scene),scene);}finally{EditorSceneManager.CloseScene(scene,true);}
        }
        Debug.Log("SKYPULSE_PIPE_FRESH_CAPTURE_PASS");EditorApplication.Exit(0);
    }
    public static void Run()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Disposable batch QA product required; this migrates and bakes QA assets.");
        try
        {
            assertions=0;
            var gameObject=new GameObject("Editor-only runtime mesh policy fixture");gameObject.SetActive(false);var game=gameObject.AddComponent<SkyPulseNativeGame>();
            var guids=AssetGuids();var originalIds=new Dictionary<string,HashSet<string>>();var sceneBefore=new Dictionary<int,Assembly>();var prefabBefore=new Dictionary<int,Assembly>();
            for(int world=0;world<12;world++)
            {
                string prefabPath=SkyPulsePipeWorkshop.PrefabPath(world),scenePath=SkyPulsePipeWorkshop.ScenePath(world);
                originalIds[prefabPath]=SerializedIds(prefabPath);originalIds[scenePath]=SerializedIds(scenePath);
                var prefab=PrefabUtility.LoadPrefabContents(prefabPath);try{prefabBefore[world]=Snapshot(prefab.GetComponent<SkyPulsePipeAuthoring>());}finally{PrefabUtility.UnloadPrefabContents(prefab);}
                var scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);try{sceneBefore[world]=Snapshot(Authoring(scene));}finally{EditorSceneManager.CloseScene(scene,true);}
            }
            SkyPulsePipeWorkshop.RefreshPhysicalStructureMeshes();
            SkyPulsePipeWorkshop.UpgradeReusableAssemblies();SkyPulsePipeWorkshop.UpgradeReusableAssemblies();
            for(int world=0;world<12;world++)
            {
                string context="world "+world;
                var prefab=PrefabUtility.LoadPrefabContents(SkyPulsePipeWorkshop.PrefabPath(world));
                try
                {
                    var root=prefab.GetComponent<SkyPulsePipeAuthoring>();Preserved(prefabBefore[world],root,context+" prefab");Details(root,context+" prefab");Check(SkyPulsePipeWorkshop.AddStructuralDetails(root)==0,context+" prefab migration idempotent");
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
                var scene=EditorSceneManager.OpenScene(SkyPulsePipeWorkshop.ScenePath(world),OpenSceneMode.Additive);
                try
                {
                    var root=Authoring(scene);Preserved(sceneBefore[world],root,context+" scene");Details(root,context+" scene");Check(SkyPulsePipeWorkshop.AddStructuralDetails(root)==0,context+" scene migration idempotent");
                    AuthoredEdits(root,game);SkyPulsePipeWorkshop.Bake(root);
                    string id=SkyPulsePipeWorkshop.Output+"/world-"+world.ToString("00");var theme=AssetDatabase.LoadAssetAtPath<SkyPulsePipeTheme>(id+".asset");
                    Check(theme!=null&&theme.Barrel!=null&&theme.Collar!=null,context+" baked runtime references exist");
                    for(int part=0;part<2;part++)
                    {
                        string partName=part==0?"Barrel":"Collar";var mesh=part==0?theme.Barrel:theme.Collar;var expected=part==0?root.BarrelBounds:root.CollarBounds;
                        int authoredVertices=root.transform.Find(partName).GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.vertexCount);
                        Check(mesh.isReadable&&mesh.vertexCount==authoredVertices&&mesh.vertexCount<24000,context+" "+partName+" bakes every active editable vertex once");
                        Check(mesh.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z)),context+" "+partName+" finite vertex coordinates");Fit(mesh.bounds,expected,context+" "+partName);
                        var runtime=(Mesh)typeof(SkyPulseNativeGame).GetMethod("RouteThemeMesh",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{mesh,world,part==1});
                        try{Check(runtime.vertexCount==mesh.vertexCount&&runtime.vertices.SequenceEqual(mesh.vertices),context+" authored bake is copied without extra hardware");}
                        finally{UnityEngine.Object.DestroyImmediate(runtime);}
                    }
                    Check(theme.Accent==root.Accent&&theme.Energy==root.Energy&&theme.Metal==root.Metal&&Mathf.Approximately(theme.LightRate,root.LightRate)&&Mathf.Approximately(theme.Detail,root.Detail),context+" runtime finish matches authoring controls");
                    Capture(root,scene);
                }
                finally{EditorSceneManager.CloseScene(scene,true);}
                RuntimeMeshPolicy(world,game);
            }
            UnityEngine.Object.DestroyImmediate(gameObject);AssetDatabase.SaveAssets();
            foreach(var old in guids)Check(File.Exists(old.Key)&&Regex.Match(File.ReadAllText(old.Key),@"(?m)^guid: (\w+)").Groups[1].Value==old.Value,"existing asset GUID preserved: "+old.Key);
            foreach(var old in originalIds)Check(old.Value.IsSubsetOf(SerializedIds(old.Key)),"existing serialized objects retained: "+old.Key);
            var output=Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");if(!string.IsNullOrEmpty(output))
            {
                string folder=Path.Combine(output,"pipe-structures");Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder,"VALIDATION.json"),JsonUtility.ToJson(new Record{result="PASS",worlds=12,assemblies=24,editableParts=96,bakedParts=24,assertions=assertions,
                    scope=new[]{"existing GUIDs and serialized IDs retained","base layout, meshes, materials and finish controls retained","idempotent migration and owner edit fixture","all active meshes baked once inside original fit bounds","generated fallback hardware once; authored bakes remain authoritative","Unity Editor QA; physical device visuals/performance pending"}},true));
            }
            Debug.Log("SKYPULSE_PIPE_STRUCTURE_PASS: 12 worlds, 24 reusable assemblies, 96 editable parts, 24 runtime meshes; "+assertions+" assertions");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
