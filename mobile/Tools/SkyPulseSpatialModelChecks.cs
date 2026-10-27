using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkyPulse.Mobile;

// Refresh shared model buffers in an isolated project, retaining the existing
// scene layouts, prefab layouts, persistent asset IDs and artist-facing labels.
public static class SkyPulseSpatialModelChecks
{
    const BindingFlags F=BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    const string Folder=SkyPulseBackgroundWorkshop.Folder;
    static void Check(bool ok,string note){if(!ok)throw new Exception(note);Debug.Log("SPATIAL_MODEL_CHECK: "+note);}
    public static void RefreshAssets()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");
        var files=Directory.GetFiles(Folder+"/Scenes","*.unity").Concat(Directory.GetFiles(Folder+"/Prefabs","*.prefab")).ToDictionary(p=>p,File.ReadAllBytes);
        EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity",OpenSceneMode.Single);
        var create=typeof(SkyPulseBackgroundWorkshop).GetMethod("CreateDistrict",F);var architecture=typeof(SkyPulseNativeGame).Assembly.GetType("SkyPulse.Mobile.SkyPulseWorldArchitecture");
        try
        {
            for(int world=0;world<12;world++)for(int variant=0;variant<2;variant++)
            {
                string path=Folder+"/Prefabs/world-"+world.ToString("00")+"-"+(variant==0?"civic":"residential")+".prefab";
                var before=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshFilter>();
                var pivots=before.ToDictionary(f=>AssetDatabase.GetAssetPath(f.sharedMesh),f=>f.transform.localPosition);int vertices=before.Sum(f=>f.sharedMesh.vertexCount);
                create.Invoke(null,new object[]{world,variant});
                var after=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshFilter>();
                Check(after.Length==before.Length,"Selectable structure count is preserved: "+world+" / "+variant);
                foreach(var part in after)
                {
                    string meshPath=AssetDatabase.GetAssetPath(part.sharedMesh);Check(pivots.ContainsKey(meshPath),"Existing mesh identity retained: "+meshPath);
                    var delta=part.transform.localPosition-pivots[meshPath];
                    if(delta.sqrMagnitude>.0000000001f){var points=part.sharedMesh.vertices;for(int i=0;i<points.Length;i++)points[i]+=delta;part.sharedMesh.vertices=points;part.sharedMesh.RecalculateBounds();EditorUtility.SetDirty(part.sharedMesh);}
                    Check(part.sharedMesh.vertexCount<60000,"Editable structure remains within mobile mesh budget: "+meshPath);
                }
                Check(after.Sum(f=>f.sharedMesh.vertexCount)>vertices,"District receives additional modelled facade detail: "+world+" / "+variant);
                AssetDatabase.SaveAssets();
            }
        }
        finally
        {
            foreach(var entry in files)File.WriteAllBytes(entry.Key,entry.Value);
            AssetDatabase.Refresh();
        }
        foreach(var entry in files)Check(File.ReadAllBytes(entry.Key).SequenceEqual(entry.Value),"Scene/prefab bytes preserved: "+entry.Key);
        foreach(string path in Directory.GetFiles(Folder+"/Scenes","*.unity").Where(p=>!Path.GetFileName(p).StartsWith("25 ")))
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);Mesh original=null;
            try
            {
                var root=scene.GetRootGameObjects().Select(g=>g.GetComponent<SkyPulseBackgroundAuthoring>()).First(r=>r!=null);
                original=(Mesh)architecture.GetMethod("Build",F).Invoke(null,new object[]{root.World,root.Variant});var filters=root.GetComponentsInChildren<MeshFilter>();
                Check(original.vertexCount<60000,"Complete runtime district remains within mobile mesh budget: "+root.World+" / "+root.Variant);
                Check(filters.Sum(f=>f.sharedMesh.vertexCount)==original.vertexCount,"Editable district retains the full generated model: "+root.World+" / "+root.Variant);
                var bounds=new Bounds();bool first=true;
                foreach(var filter in filters)
                {
                    var local=filter.sharedMesh.bounds;var centre=filter.transform.localPosition+local.center;
                    if(first){bounds=new Bounds(centre,local.size);first=false;}else bounds.Encapsulate(new Bounds(centre,local.size));
                }
                Check(Vector3.Distance(bounds.center,original.bounds.center)<.003f&&Vector3.Distance(bounds.size,original.bounds.size)<.003f,"Original scene pivots still place detail correctly: "+root.World+" / "+root.Variant);
            }
            finally{if(original!=null)UnityEngine.Object.DestroyImmediate(original);EditorSceneManager.CloseScene(scene,true);}
        }
        RefreshRelay();AssetDatabase.SaveAssets();Debug.Log("SKYPULSE_SPATIAL_MODELS_PASS");
    }
    static void RefreshRelay()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Pulse Relay World/Prefabs/relay.prefab");
        var fixture=new GameObject("Relay model refresh source");var env=fixture.AddComponent<SkyPulseRelayEnvironment>();
        try
        {
            env.Build(18f*9/16,Resources.Load<Texture2D>("SkyPulse/backgrounds/pulse-relay-station-v1"));
            var source=env.WorldCamera.transform.parent.GetComponentsInChildren<MeshFilter>().ToDictionary(f=>f.name,f=>f.sharedMesh);var copied=new HashSet<Mesh>();
            foreach(var part in prefab.GetComponentsInChildren<MeshFilter>())
            {
                var destination=part.sharedMesh;if(!copied.Add(destination))continue;
                Check(source.ContainsKey(part.name),"Relay editable model retains its scene identity: "+part.name);
                Check(source[part.name].vertexCount<60000,"Relay detail stays within mesh budget: "+part.name);
                var clone=UnityEngine.Object.Instantiate(source[part.name]);clone.name=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(destination));EditorUtility.CopySerialized(clone,destination);UnityEngine.Object.DestroyImmediate(clone);EditorUtility.SetDirty(destination);
            }
        }
        finally{env.ReleaseAuthoringResources();UnityEngine.Object.DestroyImmediate(fixture);}
    }
}
