// Batch migration and preservation checks in a disposable copy, never the owner project.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkyPulse.Mobile;

public static class SkyPulsePlanetWorkshopChecks
{
    sealed class Node
    {
        public Vector3 Position,Scale;public Quaternion Rotation;public bool Active;public string Mesh;public string[] Materials;
    }
    static int assertions;
    static void Check(bool good,string text){assertions++;if(!good)throw new Exception(text);Debug.Log("PLANET_WORKSHOP_CHECK: "+text);}
    static string Ref(UnityEngine.Object value)=>value!=null?AssetDatabase.GetAssetPath(value)+":"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)):"null";
    static string Hierarchy(Transform t,Transform root){string path=t.name+"#"+t.GetSiblingIndex();while(t!=root){t=t.parent;path=t.name+"#"+t.GetSiblingIndex()+"/"+path;}return path;}
    static Dictionary<string,Node> Snapshot(string path)
    {
        var result=new Dictionary<string,Node>();GameObject prefab=null;UnityEngine.SceneManagement.Scene scene=default;
        Transform[] roots;
        if(path.EndsWith(".prefab")){prefab=PrefabUtility.LoadPrefabContents(path);roots=new[]{prefab.transform};}
        else{scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);roots=scene.GetRootGameObjects().Select(g=>g.transform).ToArray();}
        try
        {
            foreach(var root in roots)foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                var mesh=t.GetComponent<MeshFilter>();var renderer=t.GetComponent<MeshRenderer>();
                result.Add(Hierarchy(t,root),new Node{Position=t.localPosition,Scale=t.localScale,Rotation=t.localRotation,Active=t.gameObject.activeSelf,Mesh=Ref(mesh!=null?mesh.sharedMesh:null),Materials=renderer!=null?renderer.sharedMaterials.Select(m=>Ref(m)).ToArray():Array.Empty<string>()});
            }
        }
        finally{if(prefab!=null)PrefabUtility.UnloadPrefabContents(prefab);else EditorSceneManager.CloseScene(scene,true);}
        return result;
    }
    static string[] ObjectIds(string path)=>Regex.Matches(File.ReadAllText(path),@"^--- !u!\d+ &(-?\d+)",RegexOptions.Multiline).Cast<Match>().Select(m=>m.Groups[1].Value).OrderBy(s=>s).ToArray();
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    static Dictionary<string,string> Hashes(string folder)=>Directory.GetFiles(folder,"*",SearchOption.AllDirectories).ToDictionary(p=>p,p=>Hash(p));
    static void Compare(Dictionary<string,Node> before,Dictionary<string,Node> after,string label)
    {
        Check(before.Keys.OrderBy(s=>s).SequenceEqual(after.Keys.OrderBy(s=>s)),label+" hierarchy and sibling order remain unchanged");
        foreach(var pair in before)
        {
            var a=pair.Value;var b=after[pair.Key];string text=label+" "+pair.Key;
            Check(a.Position==b.Position&&a.Scale==b.Scale&&a.Rotation==b.Rotation&&a.Active==b.Active,text+" keeps transform and active state");
            Check(a.Mesh==b.Mesh,text+" keeps its model mesh and GUID");
            bool moon=pair.Key.Contains("/West distant moon#")||pair.Key.Contains("/Lower distant moon#");
            if(!moon)Check(a.Materials.SequenceEqual(b.Materials),text+" keeps unrelated material identities");
        }
    }
    public static void Run()
    {
        if(Application.companyName!="SkyPulseQA20261006"||!Application.isBatchMode)throw new Exception("Disposable batch QA only");
        assertions=0;string[] targets={SkyPulsePlanetWorkshop.ScenePath,SkyPulsePlanetWorkshop.PrefabPath};
        var originalGuids=AssetDatabase.FindAssets("",new[]{SkyPulsePlanetWorkshop.Folder}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>!AssetDatabase.IsValidFolder(p)).ToDictionary(p=>p,p=>AssetDatabase.AssetPathToGUID(p));
        var meshHashes=Directory.GetFiles(SkyPulsePlanetWorkshop.Folder+"/Meshes","*",SearchOption.AllDirectories).ToDictionary(p=>p,p=>Hash(p));
        var snapshots=targets.ToDictionary(p=>p,p=>Snapshot(p));var ids=targets.ToDictionary(p=>p,p=>ObjectIds(p));
        SkyPulsePlanetWorkshop.UpgradeInQa();
        foreach(string path in targets)
        {
            Compare(snapshots[path],Snapshot(path),Path.GetFileName(path));Check(ids[path].SequenceEqual(ObjectIds(path)),path+" serialized object IDs are unchanged");
        }
        foreach(var pair in originalGuids)Check(AssetDatabase.AssetPathToGUID(pair.Key)==pair.Value,"Existing asset GUID preserved: "+pair.Key);
        foreach(var pair in meshHashes)Check(Hash(pair.Key)==pair.Value,"Original model bytes preserved: "+pair.Key);
        var ocean=AssetDatabase.LoadAssetAtPath<Material>(SkyPulsePlanetWorkshop.OceanPath);var rock=AssetDatabase.LoadAssetAtPath<Material>(SkyPulsePlanetWorkshop.RockPath);var ice=AssetDatabase.LoadAssetAtPath<Material>(SkyPulsePlanetWorkshop.IcePath);
        Check(ocean.GetFloat("_Kind")==0&&rock.GetFloat("_Kind")==1&&ice.GetFloat("_Kind")==2,"Three editable material assets retain their geological identities");
        Check(ocean!=rock&&rock!=ice&&ice!=ocean,"Planet and moons have independent editable material assets");
        Check(new[]{ocean,rock,ice}.All(m=>m.name==Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(m))),"Main material names match their files");
        var fixture=new GameObject("Custom material preservation fixture");var custom=new Material(ocean){name="Owner custom moon"};var palette=new Color(.7f,.1f,.4f);custom.SetColor("_Deep",palette);
        try
        {
            var moon=new GameObject("West distant moon",typeof(MeshRenderer));moon.transform.SetParent(fixture.transform,false);moon.transform.localPosition=new Vector3(3,7,9);moon.GetComponent<MeshRenderer>().sharedMaterial=custom;
            int changes=SkyPulsePlanetWorkshop.UpgradeRoot(fixture.transform,ocean,rock,ice);
            Check(changes==0&&moon.GetComponent<MeshRenderer>().sharedMaterial==custom&&custom.GetColor("_Deep")==palette&&moon.transform.localPosition==new Vector3(3,7,9),"An owner's unrelated material and transform are never replaced");
        }
        finally{UnityEngine.Object.DestroyImmediate(fixture);UnityEngine.Object.DestroyImmediate(custom);}
        var hashes=Hashes(SkyPulsePlanetWorkshop.Folder);SkyPulsePlanetWorkshop.UpgradeInQa();
        var repeat=Hashes(SkyPulsePlanetWorkshop.Folder);Check(hashes.Count==repeat.Count&&hashes.All(p=>repeat[p.Key]==p.Value),"Repeated migration changes no scene, prefab, material, mesh or metadata bytes");
        string output=Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");
        if(!string.IsNullOrEmpty(output)){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"PLANET_WORKSHOP_VALIDATION.json"),"{\"passed\":true,\"assertions\":"+assertions+",\"source\":\"disposable Unity Editor batch\",\"device_tested\":false}");}
        Debug.Log("SKYPULSE_PLANET_WORKSHOP_PASS: "+assertions+" assertions");EditorApplication.Exit(0);
    }
}
