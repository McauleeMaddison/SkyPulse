using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkyPulse.Mobile;

public static class SkyPulsePipeWorkshop
{
    public const string Folder="Assets/Editor/SkyPulsePipeWorkshop";
    const string Output="Assets/Resources/SkyPulse/PipeEdits";
    static readonly Type Factory=typeof(SkyPulseNativeGame).Assembly.GetType("SkyPulse.Mobile.SkyPulseRouteArchitecture");
    static readonly string[] Names={"Neon City","Aurora Rise","Solar Drift","Midnight Tide","Velvet Dawn","Crystal Night","Jade Horizon","Violet Rain","Eclipse","Cobalt Storm","Amber Skies","Polar Glow"};
    static object captured;
    static void Capture(object builder){captured=builder;}
    static T Field<T>(string name)=>(T)captured.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(captured);
    static Color Colour(string name,int world)=>(Color)Factory.GetMethod(name).Invoke(null,new object[]{world});
    static Mesh Readable(int world,bool cap)
    {
        var hook=Factory.GetField("EditorMeshCapture",BindingFlags.Static|BindingFlags.NonPublic);Mesh generated=null;hook.SetValue(null,(Action<object>)Capture);
        try
        {
            generated=(Mesh)Factory.GetMethod("Build").Invoke(null,new object[]{world,cap});
            var mesh=new Mesh{name=generated.name};mesh.SetVertices(Field<List<Vector3>>("v"));mesh.SetNormals(Field<List<Vector3>>("n"));mesh.SetColors(Field<List<Color>>("c"));mesh.SetTriangles(Field<List<int>>("t"),0);mesh.RecalculateBounds();return mesh;
        }
        finally{hook.SetValue(null,null);captured=null;if(generated!=null)UnityEngine.Object.DestroyImmediate(generated);}
    }
    [MenuItem("Tools/SkyPulse/Pipe Workshop/Create reusable pipe scenes")]
    public static void Create()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        if(Directory.Exists(Folder+"/Scenes")&&Directory.GetFiles(Folder+"/Scenes","*.unity").Length>0)throw new InvalidOperationException("Pipe scenes already exist; open them instead of overwriting edits.");
        foreach(string part in new[]{"Scenes","Meshes","Materials","Prefabs"})Directory.CreateDirectory(Folder+"/"+part);AssetDatabase.Refresh();
        if(string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Save your current scene before creating the pipe workshop.");
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Folder+"/Workshop session.unity");
        }
        for(int world=0;world<12;world++)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                var root=new GameObject(Names[world]+" · Reusable pipe assembly");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
                var authoring=root.AddComponent<SkyPulsePipeAuthoring>();authoring.World=world;authoring.Accent=Colour("Accent",world);authoring.Energy=Colour("Secondary",world);authoring.Metal=Colour("Metal",world);
                for(int part=0;part<2;part++)
                {
                    bool cap=part==1;string id="world-"+world.ToString("00")+(cap?"-collar":"-barrel");
                    var mesh=Readable(world,cap);AssetDatabase.CreateAsset(mesh,Folder+"/Meshes/"+id+".asset");if(cap)authoring.CollarBounds=mesh.bounds;else authoring.BarrelBounds=mesh.bounds;
                    var item=new GameObject(cap?"Collar":"Barrel",typeof(MeshFilter),typeof(MeshRenderer));item.transform.SetParent(root.transform,false);item.GetComponent<MeshFilter>().sharedMesh=mesh;
                    item.transform.localPosition=cap?Vector3.zero:new Vector3(0,-1.83f,0);item.transform.localScale=cap?new Vector3(1.08f,.62f,1.08f):new Vector3(1,3.3f,1);
                    var material=new Material(Resources.Load<Shader>("SkyPulse/Shaders/EndlessRouteMetal")){name=Names[world]+(cap?" collar":" barrel")};
                    material.SetFloat("_Theme",world);material.SetFloat("_ThemeAmount",world==0?0:1);material.SetFloat("_Cutout",cap?1:0);material.SetFloat("_Clock",2);
                    var texture=Resources.Load<Texture2D>(cap?"SkyPulse/art/pipes/pipe-collar-neon-v1":"SkyPulse/art/pipes/pipe-shaft-neon-v1");material.mainTexture=texture;
                    material.SetVector("_ArtRect",cap?new Vector4(225f/1934,91f/813,1484f/1934,608f/813):new Vector4(266f/1024,0,493f/1024,1));
                    material.SetColor("_ThemeAccent",authoring.Accent);material.SetColor("_ThemeEnergy",authoring.Energy);material.SetColor("_MetalTone",authoring.Metal);
                    AssetDatabase.CreateAsset(material,Folder+"/Materials/"+id+".mat");item.GetComponent<MeshRenderer>().sharedMaterial=material;
                }
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Prefabs/"+(world+1).ToString("00")+" "+Names[world]+".prefab");
                var camera=new GameObject("Pipe inspection camera",typeof(Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera,scene);camera.transform.position=new Vector3(2.5f,.7f,-6.5f);camera.transform.LookAt(new Vector3(0,-1.4f,0));camera.GetComponent<Camera>().fieldOfView=34;camera.GetComponent<Camera>().backgroundColor=new Color(.025f,.039f,.064f);
                EditorSceneManager.SaveScene(scene,Folder+"/Scenes/"+(world+1).ToString("00")+" "+Names[world]+" Pipes.unity");
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
        AssetDatabase.SaveAssets();Debug.Log("SKYPULSE_PIPE_WORKSHOP_CREATED: twelve reusable pipe assemblies; gameplay is changed only by Bake.");
    }
    static Mesh BakePart(SkyPulsePipeAuthoring root,string name,Bounds expected)
    {
        var part=root.transform.Find(name);if(part==null)throw new InvalidOperationException("Keep the named Barrel and Collar objects.");var filter=part.GetComponent<MeshFilter>();
        if(filter==null||filter.sharedMesh==null||!filter.sharedMesh.isReadable)throw new InvalidOperationException(name+" requires a readable mesh for baking.");
        var mesh=new Mesh{name=Names[root.World]+" / authored "+name};mesh.CombineMeshes(new[]{new CombineInstance{mesh=filter.sharedMesh,transform=root.transform.worldToLocalMatrix*part.localToWorldMatrix}},true,true);
        var bounds=mesh.bounds;if(bounds.size.x<.01f||bounds.size.y<.01f||bounds.size.z<.01f||mesh.vertexCount>24000){UnityEngine.Object.DestroyImmediate(mesh);throw new InvalidOperationException("Keep a complete pipe under 24,000 vertices per part.");}
        var fit=new Vector3(expected.size.x/bounds.size.x,expected.size.y/bounds.size.y,expected.size.z/bounds.size.z);var vertices=mesh.vertices;var normals=mesh.normals;
        for(int i=0;i<vertices.Length;i++){vertices[i]=Vector3.Scale(vertices[i]-bounds.center,fit)+expected.center;normals[i]=new Vector3(normals[i].x/fit.x,normals[i].y/fit.y,normals[i].z/fit.z).normalized;}
        mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();return mesh;
    }
    static T Save<T>(T value,string path) where T:UnityEngine.Object
    {
        var previous=AssetDatabase.LoadAssetAtPath<T>(path);if(previous==null){AssetDatabase.CreateAsset(value,path);return value;}
        EditorUtility.CopySerialized(value,previous);UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(previous);return previous;
    }
    public static void Bake(SkyPulsePipeAuthoring root)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
        var barrel=BakePart(root,"Barrel",root.BarrelBounds);Mesh collar=null;
        try
        {
            collar=BakePart(root,"Collar",root.CollarBounds);Directory.CreateDirectory(Output);AssetDatabase.Refresh();string id=Output+"/world-"+root.World.ToString("00");
            var data=ScriptableObject.CreateInstance<SkyPulsePipeTheme>();data.Barrel=Save(barrel,id+"-barrel.asset");barrel=null;data.Collar=Save(collar,id+"-collar.asset");collar=null;
            data.Accent=root.Accent;data.Energy=root.Energy;data.Metal=root.Metal;data.LightRate=Mathf.Clamp(root.LightRate,.4f,1.6f);data.Detail=Mathf.Clamp(root.Detail,0,1.5f);Save(data,id+".asset");AssetDatabase.SaveAssets();
            Debug.Log("SKYPULSE_PIPE_BAKED: "+Names[root.World]+" / fitted to original collision dimensions");
        }
        finally{if(barrel!=null)UnityEngine.Object.DestroyImmediate(barrel);if(collar!=null)UnityEngine.Object.DestroyImmediate(collar);}
    }
}

[CustomEditor(typeof(SkyPulsePipeAuthoring))]
public sealed class SkyPulsePipeAuthoringInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();EditorGUILayout.HelpBox("Edit the separate Barrel and Collar objects or the finish above. Bake fits the art to the original dimensions; flight gaps and colliders stay unchanged.",MessageType.Info);
        if(GUILayout.Button("Bake this pipe theme into gameplay",GUILayout.Height(34)))SkyPulsePipeWorkshop.Bake((SkyPulsePipeAuthoring)target);
    }
}
