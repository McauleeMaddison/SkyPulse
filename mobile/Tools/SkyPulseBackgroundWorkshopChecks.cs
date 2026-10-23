// Run only in the disposable QA project. Saves no changes to authoring scenes.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkyPulse.Mobile;

public static class SkyPulseBackgroundWorkshopChecks
{
    const string Output="Assets/Resources/SkyPulse/BackgroundEdits";
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);Debug.Log("WORKSHOP_CHECK: "+name);}
    public static void PrepareRelay()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        EditorSceneManager.OpenScene("Assets/Editor/Workshop Bootstrap.unity",OpenSceneMode.Single);
        typeof(SkyPulseBackgroundWorkshop).GetMethod("CreateRelay",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        Run();
    }
    public static void Run()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        if(Directory.Exists(Output))throw new Exception("Expected an empty QA override directory");
        if(string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))EditorSceneManager.OpenScene("Assets/Editor/Workshop Bootstrap.unity",OpenSceneMode.Single);
        for(int world=0;world<12;world++)for(int variant=0;variant<2;variant++)typeof(SkyPulseBackgroundWorkshop).GetMethod("WorldMaterial",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{world,variant});
        AssetDatabase.SaveAssets();
        var architecture=typeof(SkyPulseFlightEnvironment).Assembly.GetType("SkyPulse.Mobile.SkyPulseWorldArchitecture");
        var paths=Directory.GetFiles(SkyPulseBackgroundWorkshop.Folder+"/Scenes","*.unity");Check(paths.Length==25,"All 24 district arrangements and Relay have editable scenes");
        int structures=0;
        try
        {
            foreach(string path in paths)
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    var root=scene.GetRootGameObjects().Select(g=>g.GetComponent<SkyPulseBackgroundAuthoring>()).First(r=>r!=null);
                    if(!root.Relay&&(root.OriginalBeaconPoints==null||root.OriginalBeaconPoints.Length!=5))
                    {
                        var data=(Vector4[][])architecture.GetMethod("ExportBeaconLighting",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.World});root.OriginalBeaconPoints=data[0];root.BeaconColours=data[1];
                        EditorUtility.SetDirty(root);EditorSceneManager.SaveScene(scene);
                        string prefab=SkyPulseBackgroundWorkshop.Folder+"/Prefabs/world-"+root.World.ToString("00")+"-"+(root.Variant==0?"civic":"residential")+".prefab";PrefabUtility.SaveAsPrefabAsset(root.gameObject,prefab);
                    }
                    var filters=root.GetComponentsInChildren<MeshFilter>();structures+=filters.Length;
                    Check(filters.Length>=5&&filters.All(f=>f.sharedMesh!=null),"Separate selectable geometry: "+root.name+" / "+filters.Length);
                    SkyPulseBackgroundWorkshop.Bake(root);
                    if(root.Relay){CheckRelay(root);continue;}
                    string id="world-"+root.World.ToString("00")+"-"+(root.Variant==0?"civic":"residential");
                    var original=(Mesh)architecture.GetMethod("Build",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{root.World,root.Variant});
                    var baked=Resources.Load<Mesh>("SkyPulse/BackgroundEdits/"+id);
                    Check(baked!=null&&baked.vertexCount==original.vertexCount&&Vector3.Distance(baked.bounds.size,original.bounds.size)<.002f,"Bake preserves complete geometry: "+id);
                    if(root.World==0&&root.Variant==0)
                    {
                        var building=filters.First(f=>f.name.Contains("Building")).transform;var scale=building.localScale;float height=baked.bounds.max.y;
                        building.localScale=new Vector3(1,1.15f,1);SkyPulseBackgroundWorkshop.Bake(root);
                        Check(baked.bounds.max.y>height+.1f,"A resized building changes the saved gameplay mesh");building.localScale=scale;
                        var island=root.BeaconMount;var position=island.localPosition;var before=root.BeaconPoints()[0];island.localPosition+=Vector3.right*.4f;SkyPulseBackgroundWorkshop.Bake(root);
                        var lighting=Resources.Load<SkyPulseBackgroundLighting>("SkyPulse/BackgroundEdits/"+id+"-lighting");Check(Mathf.Abs(lighting.Points[0].x-before.x-.4f)<.002f,"Moving an island also moves its LED glow");island.localPosition=position;SkyPulseBackgroundWorkshop.Bake(root);
                        var loaded=(Mesh)typeof(SkyPulseFlightEnvironment).GetMethod("Architecture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{0,0});
                        Check(loaded!=baked&&loaded.vertexCount==baked.vertexCount,"Runtime loads an owned instance of the baked background");UnityEngine.Object.DestroyImmediate(loaded);
                    }
                    UnityEngine.Object.DestroyImmediate(original);
                }
                finally{EditorSceneManager.CloseScene(scene,true);}
            }
            Check(structures>=150,"Authoring scenes contain selectable structures: "+structures);
            Debug.Log("SKYPULSE_BACKGROUND_WORKSHOP_PASS");
        }
        finally{AssetDatabase.DeleteAsset(Output);AssetDatabase.Refresh();}
    }
    static void CheckRelay(SkyPulseBackgroundAuthoring root)
    {
        var rail=root.transform.Find("Parallax structural rib 0");var before=rail.localPosition;rail.localPosition+=Vector3.right*.75f;SkyPulseBackgroundWorkshop.Bake(root);
        var runner=new GameObject("Relay authoring verification");var source=runner.AddComponent<SkyPulseRelayEnvironment>();
        try
        {
            source.Build(18f*9/16,Resources.Load<Texture2D>("SkyPulse/backgrounds/pulse-relay-station-v1"));
            var actual=source.WorldCamera.transform.parent.Find(rail.name);
            Check(Mathf.Abs(actual.localPosition.x-before.x-.75f)<.002f,"Relay scene edits reach the live station");
            source.SetVisual(4,1);var first=actual.localPosition;source.SetVisual(4,1);Check((actual.localPosition-first).sqrMagnitude<.000001f,"Relay offsets do not accumulate between frames");
            rail.gameObject.SetActive(false);SkyPulseBackgroundWorkshop.Bake(root);
            var data=Resources.Load<SkyPulseRelayBackgroundEdits>("SkyPulse/BackgroundEdits/relay");Check(data.Parts.Any(p=>p.Name==rail.name&&p.Hidden),"Disabled station structures remain hidden in the bake");
        }
        finally
        {
            foreach(string field in new[]{"railMesh","turbineMesh","droneMesh","quad","material","backdrop","feed","image","scene"})
            {
                var owned=typeof(SkyPulseRelayEnvironment).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(source) as UnityEngine.Object;
                if(owned is Transform transform)UnityEngine.Object.DestroyImmediate(transform.gameObject);else if(owned!=null)UnityEngine.Object.DestroyImmediate(owned);
            }
            UnityEngine.Object.DestroyImmediate(runner);
        }
        rail.gameObject.SetActive(true);rail.localPosition=before;SkyPulseBackgroundWorkshop.Bake(root);
    }
}
