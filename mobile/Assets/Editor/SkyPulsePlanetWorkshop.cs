using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SkyPulse.Mobile;

// Only legacy shared-planet material bindings are migrated. No model, transform,
// scene object, pipe, gameplay value or owner's unrelated material is rebuilt.
public static class SkyPulsePlanetWorkshop
{
    public const string Folder="Assets/Editor/SkyPulseBackgroundWorkshop/Pulse Relay World";
    public const string ScenePath=Folder+"/Scenes/Pulse Relay Orbital World.unity";
    public const string PrefabPath=Folder+"/Prefabs/relay.prefab";
    public const string OceanPath=Folder+"/Materials/relay-1.mat";
    public const string RockPath=Folder+"/Materials/relay-crater-moon.mat";
    public const string IcePath=Folder+"/Materials/relay-ice-moon.mat";
    const string VersionTag="SkyPulseCelestialFinish";
    [MenuItem("Tools/SkyPulse/Background Workshop/Refresh Relay planet materials")]
    public static void RefreshPlanetMaterials()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before updating the editable station.");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        Upgrade();
    }
    public static void UpgradeInQa()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Bulk migration requires the disposable QA product.");
        Upgrade();
    }
    static void Upgrade()
    {
        var ocean=AssetDatabase.LoadAssetAtPath<Material>(OceanPath);
        var shader=Resources.Load<Shader>("SkyPulse/Shaders/OrbitalPlanet");
        if(ocean==null||ocean.shader!=shader)throw new InvalidOperationException("The legacy Relay planet material is missing or uses a custom shader. It has been left unchanged.");
        UpdateLegacyOcean(ocean);
        var rock=MoonMaterial(ocean,RockPath,1);var ice=MoonMaterial(ocean,IcePath,2);
        int changed=0;
        var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            int edits=UpgradeRoot(prefab.transform,ocean,rock,ice);changed+=edits;
            if(edits>0)PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            var authoring=scene.GetRootGameObjects().Select(g=>g.GetComponent<SkyPulseBackgroundAuthoring>()).FirstOrDefault(a=>a!=null&&a.Relay);
            if(authoring==null)throw new InvalidOperationException("The editable Relay authoring root is missing.");
            int edits=UpgradeRoot(authoring.transform,ocean,rock,ice);changed+=edits;
            if(edits>0){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        AssetDatabase.SaveAssets();
        Debug.Log("SKYPULSE_PLANET_WORKSHOP_UPDATED: "+changed+" legacy moon bindings; meshes, transforms, object identities and custom material bindings preserved.");
    }
    static bool Near(Color a,Color b)=>(new Vector4(a.r-b.r,a.g-b.g,a.b-b.b,a.a-b.a)).sqrMagnitude<.000001f;
    static void UpdateLegacyOcean(Material original)
    {
        if(original.GetTag(VersionTag,false,"")=="1")return;
        var copy=new Material(original){name=original.name};
        try
        {
            bool legacyColours=Near(original.GetColor("_Deep"),new Color(.07f,.27f,.38f))&&Near(original.GetColor("_Light"),new Color(.39f,.58f,.62f))&&Near(original.GetColor("_Rim"),new Color(.19f,.51f,.64f));
            if(legacyColours)SkyPulseOrbitalWorld.ConfigureCelestialMaterial(copy,0);
            else
            {
                // Preserve a palette the owner already edited. Only new
                // geological-identity properties receive the ocean defaults.
                copy.SetFloat("_Kind",0);copy.SetFloat("_Seed",2.71f);copy.SetFloat("_Atmosphere",.024f);
                copy.SetVector("_LightDirection",new Vector4(-.66f,.58f,-.42f,0));
            }
            copy.SetOverrideTag(VersionTag,"1");
            if(!Application.isBatchMode)Undo.RecordObject(original,"Refresh Relay ocean material");
            EditorUtility.CopySerialized(copy,original);EditorUtility.SetDirty(original);
        }
        finally{UnityEngine.Object.DestroyImmediate(copy);}
    }
    static Material MoonMaterial(Material ocean,string path,int kind)
    {
        // Existing moon assets may already contain owner edits. Never replace
        // or reconfigure them during a repeated migration.
        var previous=AssetDatabase.LoadAssetAtPath<Material>(path);if(previous!=null)return previous;
        var value=new Material(ocean){name=Path.GetFileNameWithoutExtension(path)};
        SkyPulseOrbitalWorld.ConfigureCelestialMaterial(value,kind);value.SetOverrideTag(VersionTag,"1");AssetDatabase.CreateAsset(value,path);return value;
    }
    public static int UpgradeRoot(Transform root,Material legacyOcean,Material rock,Material ice)
    {
        int changed=0;
        foreach(var entry in new[]{("West distant moon",rock),("Lower distant moon",ice)})
        {
            var body=root.Find(entry.Item1);if(body==null)continue;var renderer=body.GetComponent<MeshRenderer>();
            if(renderer==null||renderer.sharedMaterials.Length!=1||renderer.sharedMaterial!=legacyOcean)continue;
            if(!Application.isBatchMode)Undo.RecordObject(renderer,"Assign distinct Relay moon material");
            renderer.sharedMaterial=entry.Item2;EditorUtility.SetDirty(renderer);changed++;
        }
        return changed;
    }
}
