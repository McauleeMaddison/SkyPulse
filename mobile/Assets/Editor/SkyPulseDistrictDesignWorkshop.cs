using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkyPulse.Mobile;

// Guarded design refresh. Existing scene objects, transforms and mesh GUIDs are
// never recreated. A changed artist mesh is authoritative and is left alone.
public static class SkyPulseDistrictDesignWorkshop
{
    public const string BaselinePath=SkyPulseBackgroundWorkshop.Folder+"/district-design-baseline-v1.json";
    const string MeshFolder=SkyPulseBackgroundWorkshop.Folder+"/Meshes/";
    static readonly Type Factory=typeof(SkyPulseNativeGame).Assembly.GetType("SkyPulse.Mobile.SkyPulseWorldArchitecture");
    const BindingFlags Static=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
    static readonly FieldInfo CaptureField=Factory.GetField("EditorPartCapture",Static);
    [Serializable] public sealed class PartRecord { public string path,name,sha256,installedSha256;public bool generated;public Vector3 pivot; }
    [Serializable] public sealed class Baseline { public int version=1,designVersion=1;public string sourceCommit="17a8f6de216413ee9af6f2d99a9cbd612ec067a8";public PartRecord[] parts; }
    sealed class Part { public string name;public Mesh mesh;public Vector3 pivot; }
    static int captureDepth,start,piece;static object builder;
    static readonly List<Vector2Int> ranges=new List<Vector2Int>();static readonly List<Part> captured=new List<Part>();
    static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);
    public static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    static void Guard(){if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Use the disposable batch QA project for bulk district design updates.");}
    static void Capture(string name,object source,bool begin)
    {
        builder=source;
        if(begin){if(captureDepth++==0)start=Field<List<int>>(source,"t").Count;}
        else if(--captureDepth==0){int end=Field<List<int>>(source,"t").Count;Add(name,source,start,end);ranges.Add(new Vector2Int(start,end));}
    }
    static void Add(string name,object source,int first,int last)
    {
        if(first>=last)return;
        var oldV=Field<List<Vector3>>(source,"v");var oldN=Field<List<Vector3>>(source,"n");var oldC=Field<List<Color>>(source,"c");var oldT=Field<List<int>>(source,"t");
        var map=new Dictionary<int,int>();var v=new List<Vector3>();var n=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
        for(int i=first;i<last;i++){int old=oldT[i];if(!map.TryGetValue(old,out int index)){index=v.Count;map.Add(old,index);v.Add(oldV[old]);n.Add(oldN[old]);c.Add(oldC[old]);}t.Add(index);}
        var bounds=new Bounds(v[0],Vector3.zero);foreach(var at in v)bounds.Encapsulate(at);
        var pivot=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        var mesh=new Mesh{name=(++piece).ToString("00")+" "+name};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateBounds();
        captured.Add(new Part{name=name,mesh=mesh,pivot=pivot});
    }
    static Part[] Generate(int world,int variant)
    {
        piece=0;captureDepth=0;builder=null;ranges.Clear();captured.Clear();Mesh complete=null;
        CaptureField.SetValue(null,(Action<string,object,bool>)Capture);
        try
        {
            complete=(Mesh)Factory.GetMethod("Build",Static).Invoke(null,new object[]{world,variant});
            if(builder!=null){int from=0;var indices=Field<List<int>>(builder,"t");foreach(var range in ranges){if(range.x>from)Add("Architectural details",builder,from,range.x);from=range.y;}if(from<indices.Count)Add("Architectural details",builder,from,indices.Count);}
            return captured.ToArray();
        }
        finally{CaptureField.SetValue(null,null);if(complete!=null)UnityEngine.Object.DestroyImmediate(complete);}
    }
    static string Prefix(int world,int variant)=>MeshFolder+"world-"+world.ToString("00")+"-"+(variant==0?"civic":"residential");
    public static void CaptureBaseline()
    {
        Guard();if(File.Exists(BaselinePath))throw new InvalidOperationException("The original design baseline already exists; it must not be replaced with a later design.");
        var records=new List<PartRecord>();
        for(int world=0;world<12;world++)for(int variant=0;variant<2;variant++)
        {
            var parts=Generate(world,variant);
            try
            {
                for(int i=0;i<parts.Length;i++)
                {
                    string path=Prefix(world,variant)+"-"+(i+1).ToString("00")+".asset";
                    if(!File.Exists(path)||AssetDatabase.LoadAssetAtPath<Mesh>(path)==null)throw new InvalidOperationException("Missing existing authored mesh "+path);
                    var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);var raw=parts[i].mesh.vertices;for(int v=0;v<raw.Length;v++)raw[v]-=parts[i].pivot;
                    bool generated=existing.vertices.SequenceEqual(raw)&&existing.normals.SequenceEqual(parts[i].mesh.normals)&&existing.colors.SequenceEqual(parts[i].mesh.colors)&&existing.triangles.SequenceEqual(parts[i].mesh.triangles);
                    records.Add(new PartRecord{path=path,name=parts[i].name,sha256=Hash(path),pivot=parts[i].pivot,generated=generated});
                }
            }
            finally{foreach(var part in parts)UnityEngine.Object.DestroyImmediate(part.mesh);}
        }
        File.WriteAllText(BaselinePath,JsonUtility.ToJson(new Baseline{parts=records.ToArray()},true));AssetDatabase.Refresh();
        Debug.Log("SKYPULSE_DISTRICT_BASELINE_PASS: "+records.Count+" exact original mesh hashes and source pivots recorded / "+records.Count(r=>r.generated)+" match the generator; no mesh or layout edited");EditorApplication.Exit(0);
    }
    public static bool MayRefresh(PartRecord part,string currentHash)
        => part.generated&&(currentHash==part.sha256||(!string.IsNullOrEmpty(part.installedSha256)&&currentHash==part.installedSha256));
    [MenuItem("Tools/SkyPulse/Background Workshop/Refine original district structures")]
    public static void RefreshKnownStructures()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before refreshing the editable districts.");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        Upgrade();
    }
    public static void UpgradeInQa(){Guard();Upgrade();}
    public static string ScenePath(int world,int variant)=>SkyPulseBackgroundWorkshop.Folder+"/Scenes/"+(world+1).ToString("00")+" "+Worlds[world]+(variant==0?" Civic":" Residential")+".unity";
    static readonly string[] Worlds={"Neon City","Aurora Rise","Solar Drift","Midnight Tide","Velvet Dawn","Crystal Night","Jade Horizon","Violet Rain","Eclipse","Cobalt Storm","Amber Skies","Polar Glow"};
    static void Upgrade()
    {
        if(!File.Exists(BaselinePath))throw new InvalidOperationException("The guarded original-mesh baseline is required. No district has been changed.");
        var baseline=JsonUtility.FromJson<Baseline>(File.ReadAllText(BaselinePath));if(baseline.version!=1)throw new InvalidOperationException("Unsupported district baseline.");
        var index=baseline.parts.ToDictionary(p=>p.path);var replaced=new HashSet<string>();int changed=0,custom=0,unchanged=0;
        for(int world=0;world<12;world++)for(int variant=0;variant<2;variant++)
        {
            var parts=Generate(world,variant);
            try
            {
                for(int i=0;i<parts.Length;i++)
                {
                    string path=Prefix(world,variant)+"-"+(i+1).ToString("00")+".asset";
                    if(!index.TryGetValue(path,out var record)||record.name!=parts[i].name||!File.Exists(path)){custom++;continue;}
                    string current=Hash(path);
                    if(!MayRefresh(record,current)){custom++;continue;}
                    if(baseline.designVersion==2&&record.installedSha256==current){unchanged++;continue;}
                    var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing==null){custom++;continue;}
                    var replacement=parts[i].mesh;replacement.name=Path.GetFileNameWithoutExtension(path);
                    var vertices=replacement.vertices;for(int v=0;v<vertices.Length;v++)vertices[v]-=record.pivot;
                    replacement.vertices=vertices;replacement.RecalculateBounds();
                    if(!Application.isBatchMode)Undo.RecordObject(existing,"Refine original district structure");
                    EditorUtility.CopySerialized(replacement,existing);EditorUtility.SetDirty(existing);
                    // A mesh copied in the Editor can retain the previous GPU
                    // vertex buffer until fresh upload; keep same-process previews honest.
                    existing.vertices=existing.vertices;existing.normals=existing.normals;existing.colors=existing.colors;
                    for(int sub=0;sub<existing.subMeshCount;sub++)existing.SetIndices(existing.GetIndices(sub),existing.GetTopology(sub),sub,false);
                    existing.UploadMeshData(false);replaced.Add(path);changed++;
                }
            }
            finally{foreach(var part in parts)UnityEngine.Object.DestroyImmediate(part.mesh);}
        }
        AssetDatabase.SaveAssets();
        foreach(var part in baseline.parts)if(replaced.Contains(part.path))part.installedSha256=Hash(part.path);
        baseline.designVersion=2;
        File.WriteAllText(BaselinePath,JsonUtility.ToJson(baseline,true));AssetDatabase.Refresh();
        for(int world=0;world<12;world++)for(int variant=0;variant<2;variant++)
        {
            string path=ScenePath(world,variant);var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var root=scene.GetRootGameObjects().Select(g=>g.GetComponent<SkyPulseBackgroundAuthoring>()).Single(a=>a!=null);
                SkyPulseBackgroundWorkshop.Bake(root);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        AssetDatabase.SaveAssets();
        Debug.Log("SKYPULSE_DISTRICT_DESIGN_UPDATED: "+changed+" original generated parts refined; "+custom+" custom/missing parts retained; "+unchanged+" already installed parts retained. Existing scene/prefab layouts and mesh GUIDs preserved.");
    }
}
