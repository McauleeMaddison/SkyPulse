using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using SkyPulse.Mobile;

// Authoring only: uses the production generator's editor capture boundaries.
// The game still renders one combined mesh per district, not hundreds of parts.
public static class SkyPulseBackgroundWorkshop
{
    public const string Folder="Assets/Editor/SkyPulseBackgroundWorkshop";
    const string Output="Assets/Resources/SkyPulse/BackgroundEdits";
    static readonly Type Architecture=typeof(SkyPulseFlightEnvironment).Assembly.GetType("SkyPulse.Mobile.SkyPulseWorldArchitecture");
    static readonly FieldInfo Capture=Architecture.GetField("EditorPartCapture",BindingFlags.Static|BindingFlags.NonPublic);
    static readonly string[] Worlds={"Neon City","Aurora Rise","Solar Drift","Midnight Tide","Velvet Dawn","Crystal Night","Jade Horizon","Violet Rain","Eclipse","Cobalt Storm","Amber Skies","Polar Glow"};
    static int depth,startTriangle;
    static object builder;
    static GameObject district;
    static Material districtMaterial;
    static string assetPrefix;
    static readonly List<Vector2Int> ranges=new List<Vector2Int>();
    static int piece;
    static T Field<T>(object source,string name)=>(T)source.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(source);
    static void DirectoryFor(string path){Directory.CreateDirectory(path);AssetDatabase.Refresh();}
    static T Save<T>(T value,string path) where T:UnityEngine.Object
    {
        var existing=AssetDatabase.LoadAssetAtPath<T>(path);
        if(existing!=null){EditorUtility.CopySerialized(value,existing);UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(value,path);return value;
    }
    static Array Definitions()=>(Array)typeof(SkyPulseNativeGame).GetField("Worlds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
    static T Definition<T>(object value,string name)=>(T)value.GetType().GetField(name).GetValue(value);
    static Material WorldMaterial(int world,int variant)
    {
        var definition=Definitions().GetValue(world);
        var mat=new Material(Resources.Load<Shader>("SkyPulse/Shaders/FlightArchitecture")){name=Worlds[world]+" architecture"};
        mat.SetFloat("_Architecture",1);mat.SetFloat("_Design",world);mat.SetFloat("_Clock",2);
        mat.SetColor("_Tint",Definition<Color>(definition,"Accent"));mat.SetColor("_Haze",Definition<Color>(definition,"Floor"));
        var texture=Resources.Load<Texture2D>(Definition<string>(definition,"BackgroundPath"));if(texture!=null)mat.SetTexture("_Environment",texture);
        Architecture.GetMethod("ConfigureBeaconLighting",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{mat,world});
        return Save(mat,Folder+"/Materials/world-"+world.ToString("00")+"-"+variant+".mat");
    }
    [MenuItem("Tools/SkyPulse/Background Workshop/Create editable backgrounds")]
    public static void Create()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before creating authoring scenes.");
        // Never overwrite an artist's existing scene edits on a second click.
        if(Directory.Exists(Folder+"/Scenes")&&Directory.GetFiles(Folder+"/Scenes","*.unity").Length>0)throw new InvalidOperationException("The workshop already exists. Open its scenes; do not regenerate over saved edits.");
        DirectoryFor(Folder+"/Scenes");DirectoryFor(Folder+"/Meshes");DirectoryFor(Folder+"/Materials");DirectoryFor(Folder+"/Prefabs");
        var settings=EditorSettings.serializationMode;
        try
        {
            // Mesh buffers are binary; scenes/prefabs remain ordinary editable
            // Unity assets. This affects only newly authored assets in this call.
            EditorSettings.serializationMode=SerializationMode.ForceBinary;
            if(string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))
            {
                if(!Application.isBatchMode)throw new InvalidOperationException("Save your current Unity scene first, then create the workshop. Your open scene will be kept intact.");
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Editor/Workshop Bootstrap.unity");
            }
            for(int world=0;world<12;world++)
            for(int variant=0;variant<2;variant++)CreateDistrict(world,variant);
            CreateRelay();AssetDatabase.SaveAssets();
        }
        finally{Capture.SetValue(null,null);EditorSettings.serializationMode=settings;}
        Debug.Log("SKYPULSE_BACKGROUND_WORKSHOP_CREATED: 24 editable districts and one Relay scene. Gameplay assets are not changed until Bake.");
    }
    static void CreateDistrict(int world,int variant)
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        string id="world-"+world.ToString("00")+"-"+(variant==0?"civic":"residential");
        district=new GameObject(Worlds[world]+(variant==0?" · Civic":" · Residential"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(district,scene);
        var authoring=district.AddComponent<SkyPulseBackgroundAuthoring>();authoring.World=world;authoring.Variant=variant;
        districtMaterial=WorldMaterial(world,variant);assetPrefix=Folder+"/Meshes/"+id;piece=0;depth=0;ranges.Clear();builder=null;
        Capture.SetValue(null,(Action<string,object,bool>)CapturePart);
        Mesh original=null;
        try
        {
            original=(Mesh)Architecture.GetMethod("Build",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{world,variant});
            if(builder!=null)
            {
                var indices=Field<List<int>>(builder,"t");int from=0;
                foreach(var range in ranges)
                {
                    if(range.x>from)AddPart("Architectural details",builder,from,range.x);
                    from=range.y;
                }
                if(from<indices.Count)AddPart("Architectural details",builder,from,indices.Count);
            }
            int authoredVertices=district.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.vertexCount);
            if(authoredVertices!=original.vertexCount)throw new Exception("Authoring split lost geometry: "+id+" / "+authoredVertices+" / "+original.vertexCount);
            var beaconData=(Vector4[][])Architecture.GetMethod("ExportBeaconLighting",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{world});
            authoring.OriginalBeaconPoints=beaconData[0];authoring.BeaconColours=beaconData[1];
            var mount=district.GetComponentsInChildren<MeshFilter>().FirstOrDefault(f=>f.name.Contains("Cliff island")||f.name.Contains("Station foundation")||f.name.Contains("Harbour foundation"));
            if(mount!=null){authoring.BeaconMount=mount.transform;authoring.OriginalMountPosition=mount.transform.localPosition;authoring.OriginalMountRotation=mount.transform.localRotation;authoring.OriginalMountScale=mount.transform.localScale;}
            PrefabUtility.SaveAsPrefabAsset(district,Folder+"/Prefabs/"+id+".prefab");
            Camera(scene,new Vector3(7.5f,6.3f,-17),new Vector3(0,1,0));
            EditorSceneManager.SaveScene(scene,Folder+"/Scenes/"+(world+1).ToString("00")+" "+Worlds[world]+(variant==0?" Civic":" Residential")+".unity");
            Debug.Log("WORKSHOP_DISTRICT: "+id+" / "+district.transform.childCount+" selectable structures / "+authoredVertices+" vertices");
        }
        finally{Capture.SetValue(null,null);if(original!=null)UnityEngine.Object.DestroyImmediate(original);EditorSceneManager.CloseScene(scene,true);}
    }
    static void CapturePart(string name,object source,bool begin)
    {
        builder=source;
        if(begin)
        {
            if(depth++==0){startTriangle=Field<List<int>>(source,"t").Count;}
        }
        else if(--depth==0)
        {
            int end=Field<List<int>>(source,"t").Count;
            AddPart(name,source,startTriangle,end);ranges.Add(new Vector2Int(startTriangle,end));
        }
    }
    static void AddPart(string name,object source,int first,int last)
    {
        if(last<=first)return;
        var vertices=Field<List<Vector3>>(source,"v");var normals=Field<List<Vector3>>(source,"n");var colours=Field<List<Color>>(source,"c");var triangles=Field<List<int>>(source,"t");
        var map=new Dictionary<int,int>();var v=new List<Vector3>();var n=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
        for(int i=first;i<last;i++)
        {
            int old=triangles[i];
            if(!map.TryGetValue(old,out int index)){index=v.Count;map.Add(old,index);v.Add(vertices[old]);n.Add(normals[old]);c.Add(colours[old]);}
            t.Add(index);
        }
        var bounds=new Bounds(v[0],Vector3.zero);foreach(var position in v)bounds.Encapsulate(position);
        var pivot=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);for(int i=0;i<v.Count;i++)v[i]-=pivot;
        var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateBounds();
        string label=(++piece).ToString("00")+" "+name;
        mesh=Save(mesh,assetPrefix+"-"+piece.ToString("00")+".asset");
        var part=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(district.transform,false);part.transform.localPosition=pivot;
        part.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=part.GetComponent<MeshRenderer>();renderer.sharedMaterial=districtMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }
    static void Camera(UnityEngine.SceneManagement.Scene scene,Vector3 position,Vector3 target)
    {
        var item=new GameObject("Authoring preview camera",typeof(Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(item,scene);
        item.transform.position=position;item.transform.LookAt(target);var camera=item.GetComponent<Camera>();camera.fieldOfView=38;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.039f,.064f);
    }
    static void CreateRelay()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var runner=new GameObject("Relay export source");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(runner,scene);
        var source=runner.AddComponent<SkyPulseRelayEnvironment>();var image=Resources.Load<Texture2D>("SkyPulse/backgrounds/pulse-relay-station-v1");source.Build(18f*9/16,image);
        var root=new GameObject("Pulse Relay · Station structures");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);var authoring=root.AddComponent<SkyPulseBackgroundAuthoring>();authoring.Relay=true;
        var originals=source.WorldCamera.transform.parent.GetComponentsInChildren<MeshRenderer>();var meshes=new Dictionary<Mesh,Mesh>();var materials=new Dictionary<Material,Material>();
        foreach(var renderer in originals)
        {
            var filter=renderer.GetComponent<MeshFilter>();if(filter==null)continue;
            var item=new GameObject(renderer.name,typeof(MeshFilter),typeof(MeshRenderer));item.transform.SetParent(root.transform,false);
            item.transform.localPosition=renderer.transform.localPosition;item.transform.localRotation=renderer.transform.localRotation;item.transform.localScale=renderer.transform.localScale;
            if(!meshes.TryGetValue(filter.sharedMesh,out var mesh)){mesh=Save(UnityEngine.Object.Instantiate(filter.sharedMesh),Folder+"/Meshes/relay-"+meshes.Count+".asset");meshes.Add(filter.sharedMesh,mesh);}
            if(!materials.TryGetValue(renderer.sharedMaterial,out var mat)){mat=Save(UnityEngine.Object.Instantiate(renderer.sharedMaterial),Folder+"/Materials/relay-"+materials.Count+".mat");materials.Add(renderer.sharedMaterial,mat);}
            item.GetComponent<MeshFilter>().sharedMesh=mesh;item.GetComponent<MeshRenderer>().sharedMaterial=mat;
        }
        // The distant artwork stays a separate reference layer. The station's
        // selectable metal ribs, motors and drones are actual mesh objects.
        var editable=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name!="Distant station artwork").Select(f=>f.transform).ToArray();
        authoring.OriginalNames=editable.Select(t=>t.name).ToArray();authoring.OriginalPositions=editable.Select(t=>t.localPosition).ToArray();authoring.OriginalRotations=editable.Select(t=>t.localRotation).ToArray();authoring.OriginalScales=editable.Select(t=>t.localScale).ToArray();
        // Release the temporary runtime owner's resources safely in Edit mode.
        foreach(string field in new[]{"railMesh","turbineMesh","droneMesh","quad","material","backdrop","feed","image","scene"})
        {
            var owned=typeof(SkyPulseRelayEnvironment).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(source) as UnityEngine.Object;
            if(owned is Transform transform)UnityEngine.Object.DestroyImmediate(transform.gameObject);else if(owned!=null)UnityEngine.Object.DestroyImmediate(owned);
        }
        UnityEngine.Object.DestroyImmediate(runner);PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Prefabs/relay.prefab");Camera(scene,new Vector3(0,0,-18),Vector3.zero);
        EditorSceneManager.SaveScene(scene,Folder+"/Scenes/25 Pulse Relay Station.unity");EditorSceneManager.CloseScene(scene,true);
    }
    [MenuItem("Tools/SkyPulse/Background Workshop/Create distant skyline scenes")]
    public static void CreateHorizons()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring scenery.");
        string folder=Folder+"/Distant Scenes";
        if(Directory.Exists(folder)&&Directory.GetFiles(folder,"*.unity").Length>0)throw new InvalidOperationException("Distant scenes already exist. Open them rather than replacing edits.");
        DirectoryFor(folder);
        if(string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Save the current scene before creating distant scenes.");
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Editor/Workshop Bootstrap.unity");
        }
        for(int world=0;world<12;world++)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);Mesh original=null;
            try
            {
                district=new GameObject(Worlds[world]+" · Distant skyline");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(district,scene);
                var root=district.AddComponent<SkyPulseBackgroundAuthoring>();root.Horizon=true;root.World=world;
                var source=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/world-"+world.ToString("00")+"-0.mat");
                districtMaterial=UnityEngine.Object.Instantiate(source);districtMaterial.name=Worlds[world]+" distant atmosphere";districtMaterial.SetFloat("_Backdrop",1);districtMaterial.SetFloat("_BeaconGlow",0);
                districtMaterial=Save(districtMaterial,Folder+"/Materials/world-"+world.ToString("00")+"-horizon.mat");
                assetPrefix=Folder+"/Meshes/world-"+world.ToString("00")+"-horizon";piece=0;depth=0;ranges.Clear();builder=null;
                Capture.SetValue(null,(Action<string,object,bool>)CapturePart);original=(Mesh)Architecture.GetMethod("BuildHorizon",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{world});
                var indices=Field<List<int>>(builder,"t");int from=0;
                foreach(var range in ranges){if(range.x>from)AddPart("Distant support",builder,from,range.x);from=range.y;}
                if(from<indices.Count)AddPart("Distant support",builder,from,indices.Count);
                PrefabUtility.SaveAsPrefabAsset(district,Folder+"/Prefabs/world-"+world.ToString("00")+"-horizon.prefab");Camera(scene,new Vector3(16,7,-20),new Vector3(0,2,0));
                EditorSceneManager.SaveScene(scene,folder+"/"+(world+1).ToString("00")+" "+Worlds[world]+" Atmosphere.unity");
            }
            finally{Capture.SetValue(null,null);if(original!=null)UnityEngine.Object.DestroyImmediate(original);EditorSceneManager.CloseScene(scene,true);}
        }
        AssetDatabase.SaveAssets();Debug.Log("SKYPULSE_HORIZON_WORKSHOP_CREATED: twelve separate editable distant skylines; existing district scenes preserved.");
    }
    [MenuItem("Tools/SkyPulse/Background Workshop/Bake selected background")]
    public static void BakeSelected()
    {
        var root=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<SkyPulseBackgroundAuthoring>():null;
        if(root==null)throw new InvalidOperationException("Select a background root or one of its structures in an authoring scene.");Bake(root);
    }
    public static void Bake(SkyPulseBackgroundAuthoring root)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before baking scenery.");
        DirectoryFor(Output);
        if(root.Relay)BakeRelay(root);
        else
        {
            var parts=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.gameObject.activeInHierarchy&&f.sharedMesh!=null).ToArray();
            if(parts.Length==0)throw new InvalidOperationException("A district must contain at least one mesh.");
            var mesh=new Mesh{name=Worlds[root.World]+(root.Variant==0?" / civic district":" / residential district")};
            mesh.CombineMeshes(parts.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
            if(mesh.vertexCount>=60000){UnityEngine.Object.DestroyImmediate(mesh);throw new InvalidOperationException("This district exceeds the mobile budget of 60,000 vertices. Simplify it before baking.");}
            string id="world-"+root.World.ToString("00")+"-"+(root.Horizon?"horizon":root.Variant==0?"civic":"residential");
            Save(mesh,Output+"/"+id+".asset");
            if(!root.Horizon){var lighting=ScriptableObject.CreateInstance<SkyPulseBackgroundLighting>();lighting.Points=(Vector4[])root.BeaconPoints().Clone();lighting.Colours=(Vector4[])root.BeaconColours.Clone();Save(lighting,Output+"/"+id+"-lighting.asset");}
        }
        AssetDatabase.SaveAssets();Debug.Log("WORKSHOP_BAKED: "+root.name+". Restart Play mode to load the saved background.");
    }
    static void BakeRelay(SkyPulseBackgroundAuthoring root)
    {
        var layout=ScriptableObject.CreateInstance<SkyPulseRelayBackgroundEdits>();var parts=new List<SkyPulseRelayBackgroundEdits.Part>();
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.gameObject.activeInHierarchy&&f.sharedMesh!=null&&f.name!="Distant station artwork"))
        {
            int index=Array.IndexOf(root.OriginalNames,filter.name);var t=filter.transform;
            if(t.parent!=root.transform)throw new InvalidOperationException("Keep Relay structures directly under the station root so their animation offsets remain clear.");
            var mesh=Save(UnityEngine.Object.Instantiate(filter.sharedMesh),Output+"/relay-part-"+parts.Count.ToString("00")+".asset");
            parts.Add(new SkyPulseRelayBackgroundEdits.Part{Name=filter.name,Mesh=mesh,AdditionalStructure=index<0,
                PositionOffset=index<0?t.localPosition:t.localPosition-root.OriginalPositions[index],
                RotationOffset=index<0?t.localRotation:t.localRotation*Quaternion.Inverse(root.OriginalRotations[index]),
                ScaleMultiplier=index<0?t.localScale:new Vector3(t.localScale.x/root.OriginalScales[index].x,t.localScale.y/root.OriginalScales[index].y,t.localScale.z/root.OriginalScales[index].z)});
        }
        foreach(string original in root.OriginalNames)if(!parts.Any(p=>p.Name==original))parts.Add(new SkyPulseRelayBackgroundEdits.Part{Name=original,Hidden=true});
        layout.Parts=parts.ToArray();Save(layout,Output+"/relay.asset");
    }
}
