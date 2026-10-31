// Standalone, disposable QA for the editable Neon Finch art-review prototype.
// Copy into Assets/Editor in the QA project; this never runs inside the game.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkyPulse.Mobile;

public static class SkyPulseBirdPrototypeChecks
{
    const string Folder="Assets/Editor/SkyPulseBirdWorkshop";
    const string PrefabPath=Folder+"/Prefabs/Neon Finch Rig.prefab";
    const string ScenePath=Folder+"/Scenes/Neon Finch Art Review.unity";
    static readonly List<string> assertions=new List<string>();
    [Serializable] sealed class Report
    {
        public string result,scene,prefab,geometry,scope;
        public int meshParts,vertices,triangles,bones;
        public long warmPoseAllocatedBytes;
        public Vector3 dimensions;
        public string[] checks,captures;
    }
    [Serializable] sealed class RevisionReport
    {
        public string result,scope;
        public int workshopFiles,headMeshes,changedHeadMeshes;
        public bool priorOpticsAlreadyCompact;
        public Vector3 leftLensDimensions,rightLensDimensions;
        public string[] changedAssets,checks;
    }
    struct Pose
    {
        public Vector3 position,scale;
        public Quaternion rotation;
        public Pose(Transform t){position=t.localPosition;scale=t.localScale;rotation=t.localRotation;}
    }
    static void Check(bool value,string message)
    {
        if(!value)throw new InvalidOperationException("NEON_FINCH_CHECK_FAIL: "+message);
        assertions.Add(message);Debug.Log("NEON_FINCH_CHECK: "+message);
    }
    static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
    static bool Finite(Vector3 p)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z);
    static bool Finite(Quaternion q)=>Finite(q.x)&&Finite(q.y)&&Finite(q.z)&&Finite(q.w);
    static Pose[] Poses(Transform[] bones)=>bones.Select(t=>new Pose(t)).ToArray();
    static float Difference(Pose a,Pose b)=>Vector3.Distance(a.position,b.position)+Quaternion.Angle(a.rotation,b.rotation)+Vector3.Distance(a.scale,b.scale);
    static bool Same(Pose[] a,Pose[] b)
    {
        if(a.Length!=b.Length)return false;
        for(int i=0;i<a.Length;i++)if(Vector3.Distance(a[i].position,b[i].position)>.00001f||Quaternion.Angle(a[i].rotation,b[i].rotation)>.04f||Vector3.Distance(a[i].scale,b[i].scale)>.00001f)return false;
        return true;
    }
    static Dictionary<string,string> Snapshot()
    {
        var result=new Dictionary<string,string>(StringComparer.Ordinal);
        var files=Directory.GetFiles(Folder,"*",SearchOption.AllDirectories).AsEnumerable();
        if(File.Exists(Folder+".meta"))files=files.Concat(new[]{Folder+".meta"});
        using(var sha=SHA256.Create())foreach(var path in files.OrderBy(p=>p,StringComparer.Ordinal))
            result.Add(path,BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))));
        return result;
    }
    static void RepeatPreservesAssets()
    {
        // Exercise an actual owner edit in the disposable copy, then restore the
        // exact original bytes before producing anything to be copied back.
        string materialPath=AssetDatabase.FindAssets("t:Material",new[]{Folder}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p,StringComparer.Ordinal).FirstOrDefault();
        Check(!string.IsNullOrEmpty(materialPath),"Editable material assets exist inside the isolated workshop");
        byte[] original=File.ReadAllBytes(materialPath);
        try
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            material.name+=" QA owner edit";EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
            var before=Snapshot();var guids=before.Keys.Where(p=>!p.EndsWith(".meta",StringComparison.Ordinal)).ToDictionary(p=>p,AssetDatabase.AssetPathToGUID);
            SkyPulseBirdPrototypeWorkshop.Generate();AssetDatabase.SaveAssets();
            var after=Snapshot();
            Check(before.Count==after.Count&&before.All(pair=>after.TryGetValue(pair.Key,out var hash)&&hash==pair.Value),"Repeated generation preserves every existing byte, including a deliberately edited material");
            Check(guids.All(pair=>AssetDatabase.AssetPathToGUID(pair.Key)==pair.Value),"Repeated generation preserves stable asset GUIDs");
        }
        finally
        {
            File.WriteAllBytes(materialPath,original);AssetDatabase.ImportAsset(materialPath,ImportAssetOptions.ForceUpdate);
        }
    }
    static bool IsHeadMesh(string path)
    {
        if(!path.StartsWith(Folder+"/Meshes/",StringComparison.Ordinal)||!path.EndsWith(".asset",StringComparison.Ordinal))return false;
        string file=Path.GetFileName(path);int number;
        return file.Length>4&&file[3]==' '&&int.TryParse(file.Substring(0,3),out number)&&number>=21&&number<=42;
    }
    static Mesh OpticalLens(int number)
    {
        string prefix=number.ToString("000")+" ";
        string path=Directory.GetFiles(Folder+"/Meshes",prefix+"*.asset").SingleOrDefault();
        return path==null?null:AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }
    static bool CompactLens(Mesh lens)=>lens!=null&&lens.bounds.size.x>.05f&&lens.bounds.size.y>.05f&&lens.bounds.size.x<=.46001f&&lens.bounds.size.y<=.42001f;
    public static void RunRevision()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Disposable batch QA product required before revising or capturing the prototype");
        assertions.Clear();
        string output=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT")??"/tmp","skypulse-neon-finch-review");
        Directory.CreateDirectory(output);
        try
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Head revision QA starts in Edit mode");
            EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity",OpenSceneMode.Single);
            if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)==null||!File.Exists(ScenePath))SkyPulseBirdPrototypeWorkshop.Generate();
            AssetDatabase.SaveAssets();
            var before=Snapshot();
            var headPaths=before.Keys.Where(IsHeadMesh).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            Check(headPaths.Length==22,"Existing head has exactly the expected 22 mesh assets numbered 021–042");
            var guids=before.Keys.Where(p=>!p.EndsWith(".meta",StringComparison.Ordinal)).ToDictionary(p=>p,AssetDatabase.AssetPathToGUID);
            bool wasCompact=CompactLens(OpticalLens(29))&&CompactLens(OpticalLens(36));
            SkyPulseBirdPrototypeWorkshop.ReviseOpticsInQa();AssetDatabase.SaveAssets();
            var revised=Snapshot();
            Check(before.Count==revised.Count&&before.Keys.All(revised.ContainsKey),"Head revision creates or removes no workshop asset or metadata file");
            var changed=before.Where(pair=>revised[pair.Key]!=pair.Value).Select(pair=>pair.Key).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            Check(changed.All(IsHeadMesh),"Only the existing 021–042 head mesh asset bytes may change");
            Check(before.Where(pair=>!IsHeadMesh(pair.Key)).All(pair=>revised[pair.Key]==pair.Value),"Prefab, scene, materials, animations, all non-head meshes and every meta file remain byte-for-byte unchanged");
            Check(guids.All(pair=>AssetDatabase.AssetPathToGUID(pair.Key)==pair.Value),"Every workshop asset GUID remains stable through the head revision");
            Check(headPaths.All(path=>guids[path]==AssetDatabase.AssetPathToGUID(path)),"All 22 existing head mesh identities are preserved");
            Check(changed.Length>0||wasCompact,"Head mesh geometry changed, or the existing optics were already the compact revision");
            var left=OpticalLens(29);var right=OpticalLens(36);
            Check(CompactLens(left)&&CompactLens(right),"Both optical lenses fit within 0.46 by 0.42 units; oversized lens geometry is removed");
            Check(left.vertices.All(Finite)&&right.vertices.All(Finite),"Revised optical lens geometry remains finite");
            SkyPulseBirdPrototypeWorkshop.ReviseOpticsInQa();AssetDatabase.SaveAssets();
            var repeated=Snapshot();
            Check(revised.Count==repeated.Count&&revised.All(pair=>repeated.TryGetValue(pair.Key,out var hash)&&hash==pair.Value),"Repeating the head revision is byte-identical and preserves owner edits elsewhere");
            Check(guids.All(pair=>AssetDatabase.AssetPathToGUID(pair.Key)==pair.Value),"Head revision repeat preserves every asset GUID");
            var report=new RevisionReport{result="PASS",scope="Only existing head mesh assets 021–042 may change; body, wings, tail, transforms, bones and animation assets are preserved",workshopFiles=before.Count,headMeshes=headPaths.Length,changedHeadMeshes=changed.Length,priorOpticsAlreadyCompact=wasCompact,leftLensDimensions=left.bounds.size,rightLensDimensions=right.bounds.size,changedAssets=changed,checks=assertions.ToArray()};
            File.WriteAllText(Path.Combine(output,"REVISION-PRESERVATION.json"),JsonUtility.ToJson(report,true));
            Debug.Log("SKYPULSE_NEON_FINCH_HEAD_REVISION_PASS: "+changed.Length+" head meshes changed; all non-head files and GUIDs preserved");
        }
        catch(Exception error)
        {
            File.WriteAllText(Path.Combine(output,"REVISION-FAILURE.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);return;
        }
        // Reuse the complete geometry, frame-rate, reduced-motion, allocation and
        // capture suite. Its existing output names and process exit are retained.
        Run();
    }
    public static void Run()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Disposable batch QA product required before generating or capturing the prototype");
        assertions.Clear();
        string output=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT")??"/tmp","skypulse-neon-finch-review");
        Directory.CreateDirectory(output);
        try
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Prototype QA starts in Edit mode");
            EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity",OpenSceneMode.Single);
            SkyPulseBirdPrototypeWorkshop.Generate();AssetDatabase.SaveAssets();RepeatPreservesAssets();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Check(prefab!=null,"Standalone editable rig prefab exists");
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var roots=scene.GetRootGameObjects();
            var all=roots.SelectMany(root=>root.GetComponentsInChildren<Component>(true)).Where(c=>c!=null).ToArray();
            Check(all.All(c=>!(c is Collider2D)&&!c.GetType().Name.Contains("NativeGame")),"Workshop contains no Collider2D and no NativeGame component");
            var rigs=all.OfType<SkyPulseNeonFinchPrototype>().ToArray();Check(rigs.Length==1,"Workshop contains one dedicated preview rig");
            var rig=rigs[0];
            Check(rig.GetComponentsInChildren<SpriteRenderer>(true).Length==0,"Bird model contains no sprite or image-plane substitute");
            var bones=new[]{rig.body,rig.head,rig.leftWing,rig.rightWing,rig.leftWingTip,rig.rightWingTip,rig.tail,rig.tailTip};
            Check(bones.All(b=>b!=null)&&bones.Distinct().Count()==bones.Length,"Eight distinct editable body, head, wing and tail bones are bound");
            Check(bones.All(b=>b==rig.transform||b.IsChildOf(rig.transform)),"All preview bones remain under the isolated bird root");
            Check(rig.leftWingTip.IsChildOf(rig.leftWing)&&rig.rightWingTip.IsChildOf(rig.rightWing)&&rig.tailTip.IsChildOf(rig.tail),"Wing and tail tips form articulated parent-child chains");
            var meshFilters=rig.GetComponentsInChildren<MeshFilter>(true);var meshes=meshFilters.Select(f=>f.sharedMesh).Where(m=>m!=null).Distinct().ToArray();
            int vertices=meshes.Sum(m=>m.vertexCount),triangles=meshes.Sum(m=>m.triangles.Length/3);
            Check(meshFilters.Length>25&&meshes.Length>25,"Mechanical shell and layered feathers use distinct authored mesh parts");
            Check(vertices>=15000&&vertices<=35000,"Authored geometry stays within the 15–35k prototype verification budget ("+vertices+" vertices)");
            Check(meshes.All(m=>m.vertexCount>0&&m.normals.Length==m.vertexCount&&m.triangles.Length>0),"All mesh parts have usable geometry and normals");
            Check(meshes.All(m=>m.vertices.All(Finite)),"All authored mesh coordinates are finite");
            var renderers=rig.GetComponentsInChildren<MeshRenderer>(true);Check(renderers.Length>25,"Rigid mesh parts are attached to the editable bone hierarchy");
            Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            Check(bounds.size.z>.7f,"Model has genuine front-to-back volume ("+bounds.size.z.ToString("F3")+" world units)");
            var materials=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
            Check(materials.Length>=5&&materials.All(m=>m.shader!=null),"Distinct editable metal, shell, ivory, optical and emissive finishes are assigned");
            foreach(var shader in materials.Select(m=>m.shader).Distinct())Check(ShaderUtil.GetShaderMessages(shader).All(m=>m.severity.ToString()!="Error"),"Material shader compiles: "+shader.name);
            bool wasTurntable=rig.turntable,wasAnimate=rig.animate,wasReduced=rig.reducedMotion;
            rig.turntable=false;rig.animate=true;rig.reducedMotion=false;rig.ResetPose();
            rig.SamplePose(1.231f);var deterministic=Poses(bones);rig.SamplePose(.079f);rig.SamplePose(1.231f);
            Check(Same(deterministic,Poses(bones)),"Pose sampling is deterministic and independent of prior sampled time");
            rig.SamplePose(2f);var endpoint=Poses(bones);
            foreach(int rate in new[]{30,60,120})
            {
                rig.ResetPose();for(int i=0;i<=rate*2;i++)rig.SamplePose(i/(float)rate);
                Check(Same(endpoint,Poses(bones)),"Absolute animation agrees at "+rate+" FPS");
            }
            rig.SamplePose(0);var start=Poses(bones);float wingMotion=0,tailMotion=0;
            for(int i=1;i<=120;i++)
            {
                rig.SamplePose(i/60f);var current=Poses(bones);
                for(int b=0;b<bones.Length;b++)CheckFinite(bones[b]);
                wingMotion=Mathf.Max(wingMotion,Difference(start[2],current[2])+Difference(start[3],current[3]));
                tailMotion=Mathf.Max(tailMotion,Difference(start[6],current[6])+Difference(start[7],current[7]));
            }
            Check(wingMotion>2f&&tailMotion>.05f,"Flight cycle articulates both wings and the two-joint tail");
            rig.reducedMotion=true;rig.SamplePose(0);var reducedStart=Poses(bones);float reducedSpan=0;
            for(int i=1;i<=120;i++)
            {
                rig.SamplePose(i/60f);var current=Poses(bones);
                for(int b=0;b<bones.Length;b++){CheckFinite(bones[b]);reducedSpan=Mathf.Max(reducedSpan,Difference(reducedStart[b],current[b]));}
            }
            Check(reducedSpan<Mathf.Max(1f,wingMotion*.35f),"Reduced motion keeps preview movement substantially below the full flight cycle");
            rig.reducedMotion=false;for(int i=0;i<32;i++)rig.SamplePose(i/60f);
            long allocationStart=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1200;i++)rig.SamplePose(i/120f);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-allocationStart;
            Check(allocated==0,"Warm pose sampling allocates zero managed bytes over 1,200 frames");
            string[] captures=Capture(rig,bounds,output);
            rig.ResetPose();rig.turntable=wasTurntable;rig.animate=wasAnimate;rig.reducedMotion=wasReduced;
            var report=new Report{result="PASS",scene=ScenePath,prefab=PrefabPath,geometry="Authored curved rigid mesh parts parented to editable articulated bones",scope="Art-review candidate only; no gameplay replacement or roster conversion",meshParts=meshFilters.Length,vertices=vertices,triangles=triangles,bones=bones.Length,dimensions=bounds.size,warmPoseAllocatedBytes=allocated,checks=assertions.ToArray(),captures=captures};
            File.WriteAllText(Path.Combine(output,"VALIDATION.json"),JsonUtility.ToJson(report,true));
            Debug.Log("SKYPULSE_NEON_FINCH_PROTOTYPE_PASS: "+vertices+" vertices, "+triangles+" triangles, "+meshFilters.Length+" mesh parts; captures in "+output);EditorApplication.Exit(0);
        }
        catch(Exception error)
        {
            File.WriteAllText(Path.Combine(output,"FAILURE.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);
        }
    }
    static void CheckFinite(Transform bone)
    {
        if(!Finite(bone.localPosition)||!Finite(bone.localRotation)||!Finite(bone.localScale))throw new InvalidOperationException("Animation produced a non-finite transform: "+bone.name);
    }
    static string[] Capture(SkyPulseNeonFinchPrototype rig,Bounds bounds,string output)
    {
        const int layer=29,width=1400,height=1000;
        var hierarchy=rig.GetComponentsInChildren<Transform>(true);var previousLayers=hierarchy.Select(t=>t.gameObject.layer).ToArray();
        foreach(var part in hierarchy)part.gameObject.layer=layer;
        var cameraObject=new GameObject("Neon Finch QA capture camera");var camera=cameraObject.AddComponent<Camera>();
        camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.018f,.031f,.057f,1);camera.cullingMask=1<<layer;
        camera.orthographic=true;camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.allowHDR=true;camera.allowMSAA=true;
        var texture=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){antiAliasing=4};var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;camera.targetTexture=texture;
        var paths=new List<string>();Vector3 center=bounds.center;
        try
        {
            rig.turntable=false;rig.animate=true;rig.reducedMotion=false;
            var views=new[]{new{file="01-side.png",direction=new Vector3(.03f,.08f,-1),time=0f},new{file="02-front.png",direction=new Vector3(1,.12f,-.03f),time=0f},new{file="03-rear.png",direction=new Vector3(-1,.12f,.05f),time=0f},new{file="04-three-quarter.png",direction=new Vector3(.6f,.24f,-1),time=0f},new{file="05-flight-a.png",direction=new Vector3(.27f,.16f,-1),time=.11f},new{file="06-flight-b.png",direction=new Vector3(.27f,.16f,-1),time=.25f},new{file="07-flight-c.png",direction=new Vector3(.27f,.16f,-1),time=.41f}};
            foreach(var view in views)
            {
                rig.SamplePose(view.time);camera.transform.position=center+view.direction.normalized*12;camera.transform.LookAt(center,Vector3.up);
                camera.orthographicSize=Mathf.Max(bounds.size.y*.72f,bounds.size.x*.47f,bounds.size.z*.72f);
                camera.Render();RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                var path=Path.Combine(output,view.file);File.WriteAllBytes(path,pixels.EncodeToPNG());paths.Add(path);
            }
            var motion=Path.Combine(output,"motion");Directory.CreateDirectory(motion);
            camera.transform.position=center+new Vector3(.6f,.24f,-1).normalized*12;camera.transform.LookAt(center,Vector3.up);
            for(int frame=0;frame<90;frame++)
            {
                rig.SamplePose(frame/30f);camera.Render();RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(motion,frame.ToString("000")+".png"),pixels.EncodeToPNG());
            }
            Check(paths.All(File.Exists),"Side, front, rear, three-quarter and three flight-phase review captures were written");
            return paths.ToArray();
        }
        finally
        {
            for(int i=0;i<hierarchy.Length;i++)hierarchy[i].gameObject.layer=previousLayers[i];
            RenderTexture.active=previous;camera.targetTexture=null;texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
