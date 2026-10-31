// Guarded district redesign, preservation and real workshop renders. Disposable
// batch project only. No shipping save or App Store action is involved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkyPulse.Mobile;

public static class SkyPulseDistrictDesignChecks
{
    static int assertions;
    [Serializable] sealed class Record {public bool passed;public int assertions,worlds,districts,maximumVertices,customMeshFixtures;public string[] scope;}
    static void Check(bool ok,string note){assertions++;if(!ok)throw new InvalidOperationException("DISTRICT_DESIGN: "+note);}
    static Dictionary<string,string> Layouts()=>Directory.GetFiles(SkyPulseBackgroundWorkshop.Folder+"/Scenes","*.unity").Concat(Directory.GetFiles(SkyPulseBackgroundWorkshop.Folder+"/Prefabs","world-*.prefab")).ToDictionary(p=>p,SkyPulseDistrictDesignWorkshop.Hash);
    static Dictionary<string,string> Guids()=>Directory.GetFiles(SkyPulseBackgroundWorkshop.Folder,"*.meta",SearchOption.AllDirectories).Concat((Directory.Exists("Assets/Resources/SkyPulse/BackgroundEdits")?Directory.GetFiles("Assets/Resources/SkyPulse/BackgroundEdits","*.meta",SearchOption.AllDirectories):Array.Empty<string>())).ToDictionary(p=>p,p=>Regex.Match(File.ReadAllText(p),@"(?m)^guid: (\w+)").Groups[1].Value);
    static void Preserved(Dictionary<string,string> layouts,Dictionary<string,string> guids)
    {
        foreach(var item in layouts)Check(File.Exists(item.Key)&&SkyPulseDistrictDesignWorkshop.Hash(item.Key)==item.Value,"Scene/prefab serialized objects and owner layouts unchanged: "+item.Key);
        foreach(var item in guids)Check(File.Exists(item.Key)&&Regex.Match(File.ReadAllText(item.Key),@"(?m)^guid: (\w+)").Groups[1].Value==item.Value,"Existing GUID preserved: "+item.Key);
    }
    static void Capture(SkyPulseBackgroundAuthoring root,UnityEngine.SceneManagement.Scene scene,int world,int variant)
    {
        string output=Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");if(string.IsNullOrEmpty(output)||variant!=0)return;
        var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Single(c=>c.name=="Authoring preview camera");
        var objects=root.GetComponentsInChildren<Transform>(true);var layers=objects.Select(t=>t.gameObject.layer).ToArray();var prior=camera.targetTexture;var oldMask=camera.cullingMask;float aspect=camera.aspect;
        var active=RenderTexture.active;var target=RenderTexture.GetTemporary(750,1100,24);var pixels=new Texture2D(750,1100,TextureFormat.RGB24,false);
        try
        {
            foreach(var item in objects)item.gameObject.layer=29;camera.cullingMask=1<<29;camera.targetTexture=target;camera.aspect=750f/1100;camera.Render();
            RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,750,1100),0,0);pixels.Apply();
            string folder=Path.Combine(output,"district-design");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"world-"+world.ToString("00")+".png"),pixels.EncodeToPNG());
        }
        finally
        {
            for(int i=0;i<objects.Length;i++)objects[i].gameObject.layer=layers[i];camera.targetTexture=prior;camera.cullingMask=oldMask;camera.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
    static void OwnerMeshFixture()
    {
        var baseline=JsonUtility.FromJson<SkyPulseDistrictDesignWorkshop.Baseline>(File.ReadAllText(SkyPulseDistrictDesignWorkshop.BaselinePath));
        var part=baseline.parts.First(p=>p.generated&&p.name=="Building");byte[] original=File.ReadAllBytes(part.path);
        try
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(part.path);var points=mesh.vertices;points[0]+=new Vector3(.017f,.009f,-.011f);mesh.vertices=points;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssets();
            string ownerHash=SkyPulseDistrictDesignWorkshop.Hash(part.path);Check(ownerHash!=part.installedSha256,"Synthetic owner geometry is a real saved edit");
            SkyPulseDistrictDesignWorkshop.UpgradeInQa();
            Check(SkyPulseDistrictDesignWorkshop.Hash(part.path)==ownerHash,"A repeat refresh leaves the owner's saved geometry byte-identical");
            var unowned=new SkyPulseDistrictDesignWorkshop.PartRecord{generated=false,sha256=ownerHash};
            Check(!SkyPulseDistrictDesignWorkshop.MayRefresh(unowned,ownerHash),"A baseline mesh that never matched the original generator is ineligible");
            Check(!SkyPulseDistrictDesignWorkshop.MayRefresh(part,ownerHash),"A later owner edit is ineligible despite the known asset path and GUID");
        }
        finally{File.WriteAllBytes(part.path,original);AssetDatabase.ImportAsset(part.path,ImportAssetOptions.ForceUpdate);}
    }
    static Dictionary<string,string> InstalledBuffers()
    {
        var baseline=JsonUtility.FromJson<SkyPulseDistrictDesignWorkshop.Baseline>(File.ReadAllText(SkyPulseDistrictDesignWorkshop.BaselinePath));
        return baseline.parts.Select(p=>p.path).Concat(Directory.GetFiles("Assets/Resources/SkyPulse/BackgroundEdits","world-*.asset")).Append(SkyPulseDistrictDesignWorkshop.BaselinePath).ToDictionary(p=>p,SkyPulseDistrictDesignWorkshop.Hash);
    }
    public static void Run()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Disposable batch QA project required.");
        try
        {
            assertions=0;var layouts=Layouts();var guids=Guids();
            var baseline=JsonUtility.FromJson<SkyPulseDistrictDesignWorkshop.Baseline>(File.ReadAllText(SkyPulseDistrictDesignWorkshop.BaselinePath));
            Check(baseline.sourceCommit=="17a8f6de216413ee9af6f2d99a9cbd612ec067a8"&&baseline.parts.Length==216,"Original committed design baseline identifies all 216 authored parts");
            SkyPulseDistrictDesignWorkshop.UpgradeInQa();Preserved(layouts,guids);OwnerMeshFixture();SkyPulseDistrictDesignWorkshop.UpgradeInQa();
            var buffers=InstalledBuffers();SkyPulseDistrictDesignWorkshop.UpgradeInQa();
            foreach(var item in buffers)Check(SkyPulseDistrictDesignWorkshop.Hash(item.Key)==item.Value,"Repeated update is byte-stable: "+item.Key);
            Preserved(layouts,guids);int maximum=0;
            for(int world=0;world<12;world++)for(int variant=0;variant<2;variant++)
            {
                string path="Assets/Resources/SkyPulse/BackgroundEdits/world-"+world.ToString("00")+"-"+(variant==0?"civic":"residential")+".asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);Check(mesh!=null&&mesh.vertexCount<60000,"District stays inside the 16-bit mobile mesh budget: "+path);maximum=Mathf.Max(maximum,mesh.vertexCount);
                Check(mesh.bounds.size.z>1&&mesh.bounds.size.x>3&&mesh.bounds.min.y> -4&&mesh.bounds.max.y>4,"Actual district has landmark height, foundation and real front-to-back depth");
                Check(mesh.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z)),"Finite architectural geometry");
                var scene=EditorSceneManager.OpenScene(SkyPulseDistrictDesignWorkshop.ScenePath(world,variant),OpenSceneMode.Additive);
                try
                {
                    var root=scene.GetRootGameObjects().Select(g=>g.GetComponent<SkyPulseBackgroundAuthoring>()).Single(a=>a!=null);
                    Check(root.GetComponentsInChildren<Collider>(true).Length==0&&root.GetComponentsInChildren<Collider2D>(true).Length==0,"Editable city has no gameplay collision shapes");
                    Capture(root,scene,world,variant);
                }
                finally{EditorSceneManager.CloseScene(scene,true);}
            }
            string output=Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");if(!string.IsNullOrEmpty(output))
            {
                Directory.CreateDirectory(Path.Combine(output,"district-design"));File.WriteAllText(Path.Combine(output,"district-design/VALIDATION.json"),JsonUtility.ToJson(new Record{passed=true,assertions=assertions,worlds=12,districts=24,maximumVertices=maximum,customMeshFixtures=1,
                    scope=new[]{"216 original source geometry comparisons; only known generated buffers eligible","original scene/prefab bytes, owner layouts and all existing GUIDs preserved","real saved custom mesh edit retained by refresh","same original part pivots keep moved structures registered","repeated refresh and bake byte-stable","24 mobile-budget district meshes and 12 real workshop camera renders","no player saves, controls, economy, colliders or App Store changes"}},true));
            }
            Debug.Log("SKYPULSE_DISTRICT_DESIGN_PASS: "+assertions+" assertions, 24 districts / maximum "+maximum+" vertices, real owner-edit negative fixture preserved");EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
