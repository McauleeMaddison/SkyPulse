using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkyPulse.Mobile;

/// <summary>Detects real duplicated roster bindings and stale Studio selection, independently of palette similarity.</summary>
public static class SkyPulseRosterIdentityChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    [Serializable] sealed class Bird {public string id,name,reference,prefab,geometrySha256,materialSha256,normalizedShapeSha256;public int meshParts,vertices;}
    [Serializable] sealed class Report {public string result,scope;public Bird[] birds;public string[] checks;public bool deviceTested;}
    static readonly List<string> checks=new List<string>();
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException("ROSTER_IDENTITY_FAIL: "+message);checks.Add(message);}
    static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    static string HashFile(string path)=>Hash(File.ReadAllBytes(path));
    static object Get(object value,string name)=>value.GetType().GetField(name,Private).GetValue(value);
    static void Set(object value,string name,object item)=>value.GetType().GetField(name,Private).SetValue(value,item);
    static void Call(object value,string name)=>value.GetType().GetMethod(name,Private).Invoke(value,null);
    static Dictionary<string,string> Snapshot()=>Directory.GetFiles(SkyPulseBirdPrototypeWorkshop.Folder,"*",SearchOption.AllDirectories).Where(path=>!path.EndsWith(".DS_Store",StringComparison.Ordinal)).ToDictionary(path=>path,HashFile);
    public static void RunPairRefinement()
    {
        if(Application.companyName!="SkyPulseQA20261006"||!Application.isBatchMode)throw new InvalidOperationException("Disposable batch QA product required");
        string output=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"roster-pair-v6");Directory.CreateDirectory(output);checks.Clear();
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");var before=Snapshot();
            var targets=SkyPulseBirdRosterWorkshop.LoadDesigns().Where(d=>d.id=="newbird04"||d.id=="newbird05").ToArray();
            var allowed=new HashSet<string>(targets.SelectMany(d=>new[]{SkyPulseBirdRosterWorkshop.BirdFolder(d)+"/Geometry.asset",SkyPulseBirdRosterWorkshop.PrefabPath(d)}));
            SkyPulseBirdRosterWorkshop.RefineMagentaSourceIdentities();var after=Snapshot();
            Check(before.Count==after.Count&&before.Where(pair=>!allowed.Contains(pair.Key)).All(pair=>after.TryGetValue(pair.Key,out string hash)&&hash==pair.Value),"Pair correction changes only two geometry assets and two prefab files; Finch, twelve other birds, palettes, scene bindings and all metadata are byte-identical");
            var flags=BindingFlags.Static|BindingFlags.NonPublic;
            typeof(SkyPulseRosterChecks).GetField("outputOverride",flags).SetValue(null,output);
            typeof(SkyPulseRosterChecks).GetField("checks",flags).GetValue(null).GetType().GetMethod("Clear").Invoke(typeof(SkyPulseRosterChecks).GetField("checks",flags).GetValue(null),null);
            foreach(var d in targets)
            {
                AssetDatabase.ImportAsset(SkyPulseBirdRosterWorkshop.BirdFolder(d)+"/Geometry.asset",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(SkyPulseBirdRosterWorkshop.PrefabPath(d),ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                // Reuse the exact existing closed-volume, normal, optical-seat,
                // shared motion and zero-allocation checks, without loosening.
                typeof(SkyPulseRosterChecks).GetMethod("Inspect",flags).Invoke(null,new object[]{d,true});
            }
            var strictChecks=(List<string>)typeof(SkyPulseRosterChecks).GetField("checks",flags).GetValue(null);checks.AddRange(strictChecks);
            EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");SkyPulseBirdRosterWorkshop.RefineMagentaSourceIdentities();var repeat=Snapshot();
            Check(after.Count==repeat.Count&&after.All(pair=>repeat.TryGetValue(pair.Key,out string hash)&&hash==pair.Value),"Repeated pair authoring is read-only and creates no duplicate files or model parts");
            File.WriteAllText(Path.Combine(output,"VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",scope="Guarded correction of only two saved source-specific studies, independent disk import and existing strict mesh/optics/motion checks. Actual Unity captures are local appearance candidates; owner approval pending.",birds=new Bird[0],checks=checks.ToArray(),deviceTested=false},true));
            Debug.Log("SKYPULSE_ROSTER_PAIR_V6_PASS: "+checks.Count+" strict checks; "+output);EditorApplication.Exit(0);
        }
        catch(Exception error){File.WriteAllText(Path.Combine(output,"FAILURE.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
    }
    public static void Run()
    {
        if(Application.companyName!="SkyPulseQA20261006"||!Application.isBatchMode)throw new InvalidOperationException("Disposable batch QA product required");
        string output=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"roster-identity");Directory.CreateDirectory(output);checks.Clear();
        SkyPulseBirdRosterPreviewWindow window=null;
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");var before=Snapshot();var designs=SkyPulseBirdRosterWorkshop.LoadDesigns();
            Check(designs.Length==14&&designs.Select(d=>d.id).Distinct().Count()==14,"Fourteen non-Finch designs have unique stable roster identifiers");
            Check(designs.Select(d=>d.name).Distinct().Count()==14&&designs.Select(SkyPulseBirdRosterWorkshop.PrefabPath).Distinct().Count()==14,"All roster names and canonical prefab paths are distinct");
            Check(designs.Select(d=>d.reference).Distinct().Count()==14&&designs.Select(d=>d.sourceSha256).Distinct().Count()==14,"Every roster design points to its own unique original reference image");
            var birds=new List<Bird>();
            foreach(var d in designs)
            {
                string source="Assets/Resources/SkyPulse/characters/roster/"+d.reference+".png";
                Check(HashFile(source)==d.sourceSha256,"Original reference bytes match authored identity: "+d.id);
                string folder=SkyPulseBirdRosterWorkshop.BirdFolder(d),path=SkyPulseBirdRosterWorkshop.PrefabPath(d);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Check(prefab!=null&&prefab.name==d.name+" Rig","Canonical prefab has the matching roster name: "+d.id);
                var filters=prefab.GetComponentsInChildren<MeshFilter>(true);
                Check(filters.Length>0&&filters.All(f=>f.sharedMesh!=null&&AssetDatabase.GetAssetPath(f.sharedMesh)==folder+"/Geometry.asset"||f.sharedMesh!=null&&AssetDatabase.GetAssetPath(f.sharedMesh)==SkyPulseBirdJetWorkshop.MeshPath),"All mesh parts resolve the selected bird's own geometry, with shared exhaust only where intended: "+d.id);
                var materials=prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).ToArray();
                Check(materials.Length>0&&materials.All(m=>m!=null&&AssetDatabase.GetAssetPath(m)==folder+"/Finishes.asset"),"All finish slots resolve the selected bird's own palette: "+d.id);
                var points=filters.Where(f=>AssetDatabase.GetAssetPath(f.sharedMesh)==folder+"/Geometry.asset").SelectMany(f=>f.sharedMesh.vertices.Select(v=>prefab.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
                var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);float scale=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
                var shape=points.Select(p=>(p-bounds.center)/scale).Select(p=>new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000))).OrderBy(p=>p.x).ThenBy(p=>p.y).ThenBy(p=>p.z);
                string normalized=string.Join(";",shape.Select(p=>p.x+","+p.y+","+p.z));
                birds.Add(new Bird{id=d.id,name=d.name,reference=d.reference,prefab=path,geometrySha256=HashFile(folder+"/Geometry.asset"),materialSha256=HashFile(folder+"/Finishes.asset"),normalizedShapeSha256=Hash(Encoding.UTF8.GetBytes(normalized)),meshParts=filters.Length,vertices=points.Length});
            }
            Check(birds.Select(b=>b.geometrySha256).Distinct().Count()==14,"No two birds have byte-identical geometry assets");
            Check(birds.Select(b=>b.normalizedShapeSha256).Distinct().Count()==14,"No two birds have identical combined geometry after translation and scale normalization");
            Check(birds.Select(b=>b.materialSha256).Distinct().Count()==14,"No two birds have identical finish assets");
            window=ScriptableObject.CreateInstance<SkyPulseBirdRosterPreviewWindow>();Set(window,"tab",0);
            var names=(string[])Get(window,"names");var ids=(string[])Get(window,"ids");var paths=(string[])Get(window,"paths");
            Check(names.Length==15&&ids.Distinct().Count()==15&&paths.Distinct().Count()==15,"Studio has fifteen unique ID/name/path entries including approved Finch");
            foreach(string id in ids.Concat(new[]{"newbird04","newbird05","newbird04","chrome_raven","newbird05"}))
            {
                Call(window,"DisposePreview");Set(window,"studioIndex",Array.IndexOf(ids,id));Call(window,"CreatePreview");
                int index=Array.IndexOf(ids,id);var model=(GameObject)Get(window,"model");
                var canonical=AssetDatabase.LoadAssetAtPath<GameObject>(paths[index]);
                var expectedMeshes=new HashSet<Mesh>(canonical.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh));
                var expectedFinishes=new HashSet<Material>(canonical.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials));
                Check(model!=null&&(string)Get(window,"previewError")==null,"Studio creates the selected model without errors: "+id);
                Check(model.name==names[index]+" · unsaved studio preview","Studio model identity matches the dropdown selection: "+id);
                Check(model.GetComponentsInChildren<MeshFilter>(true).All(f=>expectedMeshes.Contains(f.sharedMesh)),"Studio switches to the selected bird's real saved mesh: "+id);
                Check(model.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).All(m=>expectedFinishes.Contains(m)),"Studio switches to the selected bird's real saved finishes: "+id);
                Check(model.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SkyPulseNeonFinchPrototype>(true)).Count()==1,"Exactly one bird rig exists in the current Studio preview scene: "+id);
                Call(window,"DisposePreview");Check(model==null,"Switching selection destroys the previous preview instance: "+id);
            }
            UnityEngine.Object.DestroyImmediate(window);window=null;var after=Snapshot();
            Check(before.Count==after.Count&&before.All(pair=>after.TryGetValue(pair.Key,out string hash)&&hash==pair.Value),"Identity validation leaves Finch, all roster assets, scenes and metadata byte-identical");
            File.WriteAllText(Path.Combine(output,"VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",scope="Saved-reference/prefab/geometry identity and real Studio instance switching. Similar source palettes are allowed; visual design approval remains the owner's decision. No phone build or save access.",birds=birds.ToArray(),checks=checks.ToArray(),deviceTested=false},true));
            Debug.Log("SKYPULSE_ROSTER_IDENTITY_PASS: "+checks.Count+" checks; selected models map to individual source geometry and finishes; "+output);EditorApplication.Exit(0);
        }
        catch(Exception error){if(window!=null)UnityEngine.Object.DestroyImmediate(window);File.WriteAllText(Path.Combine(output,"FAILURE.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
