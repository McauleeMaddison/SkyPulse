// Disposable Editor QA and real-game visual captures. Never resets a real player profile.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulseRosterChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static readonly List<string> checks=new List<string>();
    static readonly List<BirdReport> birds=new List<BirdReport>();
    const string Revision5Body="01 Sculpted continuous fuselage and flight bank v5";
    static readonly float[] PhaseTimes={0,.4125f,.825f,1.2375f,1.65f};
    static readonly float[] TurntableTimes={0,1,5,15,30};
    static string outputOverride;
    static string Output=>outputOverride??Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"roster");
    [Serializable] sealed class BirdReport{public string id,name,reference,sourceSha256;public int meshParts,vertices,triangles,openEdges,nonManifoldEdges;public float depth;public long warmAllocatedBytes;public string[] views;}
    [Serializable] sealed class Report{public string result,scope;public BirdReport[] birds;public string[] checks;public bool deviceTested;}
    [Serializable] sealed class Stamp{public string path,sha256;}
    [Serializable] sealed class RevisionChange{public string id,geometryBefore,geometryAfter,prefabBefore,prefabAfter;public bool alreadyRevision5;}
    [Serializable] sealed class GenerationReport{public string result,revision,scope;public Stamp[] generatorSources,protectedInputs;public RevisionChange[] birds;public string[] checks;public bool deviceTested;}
    [Serializable] sealed class MotionSample{public float time;public Quaternion rootDelta;public Vector3 rootPosition;public Quaternion[] jointDeltas;}
    [Serializable] sealed class MotionBird{public string id,sourcePrefabSha256;public float flapPeriod,flapDegrees,bankDegrees,turntableDegreesPerSecond;public MotionSample[] phases,turntable;}
    [Serializable] sealed class MotionReport{public string result,scope,baselineId;public MotionBird[] birds;public string[] checks;public bool deviceTested;}
    sealed class Pose
    {
        public Quaternion root;public Vector3 rootPosition;public Quaternion[] rotations;public Vector3[] positions;
        public Pose(SkyPulseNeonFinchPrototype rig,Transform[] joints)
        {root=rig.transform.localRotation;rootPosition=rig.transform.localPosition;rotations=joints.Select(t=>t.localRotation).ToArray();positions=joints.Select(t=>t.localPosition).ToArray();}
    }
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static void Check(bool ok,string text){if(!ok)throw new Exception("ROSTER_CHECK_FAIL: "+text);checks.Add(text);}
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    static Dictionary<string,string> Snapshot()=>Directory.GetFiles(SkyPulseBirdRosterWorkshop.Folder,"*",SearchOption.AllDirectories).Where(p=>!p.EndsWith(".DS_Store")).ToDictionary(p=>p,Hash);
    static SkyPulseRosterChecks(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(Application.companyName!="SkyPulseQA20261006"||!Application.isBatchMode)throw new Exception("Disposable batch QA product required");
        Directory.CreateDirectory(Output);checks.Clear();birds.Clear();
        try
        {
            var before=Snapshot();SkyPulseBirdRosterWorkshop.Generate();var after=Snapshot();
            Check(before.Count==after.Count&&before.All(p=>after[p.Key]==p.Value),"Repeat authoring preserves every roster asset byte and creates no duplicate");
            foreach(var d in SkyPulseBirdRosterWorkshop.LoadDesigns())Inspect(d);
            File.WriteAllText(Path.Combine(Output,"STUDIO-VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",scope="Fourteen source-measured articulated solid-mesh studies, Editor only; owner visual/device review pending.",birds=birds.ToArray(),checks=checks.ToArray(),deviceTested=false},true));
            Debug.Log("SKYPULSE_ROSTER_STUDIO_PASS: "+birds.Count+" individually measured true3D birds, closed meshes, fitted optics and deterministic flight");
            EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");PlayerPrefs.DeleteAll();PlayerPrefs.Save();SessionState.SetBool("Roster.GameQA",true);EditorApplication.EnterPlaymode();
        }
        catch(Exception e){Fail(e);}
    }
    // Explicit migration followed by disk readback; never enters gameplay or resets a save.
    public static void RunRevision5Review()
    {
        if(Application.companyName!="SkyPulseQA20261006"||!Application.isBatchMode)throw new Exception("Disposable batch QA product required");
        outputOverride=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"roster-v5");
        Directory.CreateDirectory(Output);checks.Clear();birds.Clear();SessionState.SetBool("Roster.GameQA",false);
        foreach(string previous in new[]{"REVISION5-GENERATION.json","STUDIO-VALIDATION.json","MOTION-VALIDATION.json","FAILURE.txt"})
        {string path=Path.Combine(Output,previous);if(File.Exists(path))File.Delete(path);}
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");
            var designs=SkyPulseBirdRosterWorkshop.LoadDesigns();
            Check(designs.Length==14&&designs.Select(d=>d.id).Distinct().Count()==14&&designs.All(d=>d.id!="neon_finch"),"Revision 4 migration targets exactly fourteen distinct non-Finch birds");
            var sources=new[]{"Assets/Editor/SkyPulseBirdRosterWorkshop.cs","Assets/Editor/SkyPulseBirdRosterRaven.cs"}.ToDictionary(path=>path,Hash);
            var protectedBefore=ProtectedSnapshot();var before=Snapshot();
            var changes=designs.Select(d=>new RevisionChange{id=d.id,alreadyRevision5=HasRevision5Marker(d),geometryBefore=Hash(SkyPulseBirdRosterWorkshop.BirdFolder(d)+"/Geometry.asset"),prefabBefore=Hash(SkyPulseBirdRosterWorkshop.PrefabPath(d))}).ToArray();
            SkyPulseBirdRosterWorkshop.ReviseRemainingRosterSculptures();
            var after=Snapshot();var allowed=new HashSet<string>(designs.SelectMany(d=>new[]{SkyPulseBirdRosterWorkshop.BirdFolder(d)+"/Geometry.asset",SkyPulseBirdRosterWorkshop.PrefabPath(d)}),StringComparer.Ordinal);
            Check(before.Count==after.Count&&before.Where(p=>!allowed.Contains(p.Key)).All(p=>after.ContainsKey(p.Key)&&after[p.Key]==p.Value),"Revision 4 changes only fourteen saved geometry/prefab files; palettes, scene bindings and every existing meta remain byte-identical");
            Check(SameSnapshot(protectedBefore,ProtectedSnapshot()),"Revision 4 preserves every approved Finch workshop file and original roster artwork byte-for-byte");
            // The generator has returned. Force a disk import before loading separate
            // prefab contents and saved review scenes for all subsequent checks.
            foreach(var d in designs)
            {
                AssetDatabase.ImportAsset(SkyPulseBirdRosterWorkshop.BirdFolder(d)+"/Geometry.asset",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(SkyPulseBirdRosterWorkshop.PrefabPath(d),ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                ReadBackRevision5(d);
                var change=changes.Single(c=>c.id==d.id);change.geometryAfter=Hash(SkyPulseBirdRosterWorkshop.BirdFolder(d)+"/Geometry.asset");change.prefabAfter=Hash(SkyPulseBirdRosterWorkshop.PrefabPath(d));
                Check(change.alreadyRevision5?change.geometryBefore==change.geometryAfter&&change.prefabBefore==change.prefabAfter:change.geometryBefore!=change.geometryAfter&&change.prefabBefore!=change.prefabAfter,"Saved Revision 4 geometry and prefab are "+(change.alreadyRevision5?"preserved on repeat: ":"newly generated before readback: ")+d.id);
            }
            var first=Snapshot();SkyPulseBirdRosterWorkshop.ReviseRemainingRosterSculptures();var second=Snapshot();
            Check(SameSnapshot(first,second),"Repeated Revision 4 migration preserves every revised roster asset byte-for-byte and creates no duplicate");
            Check(SameSnapshot(protectedBefore,ProtectedSnapshot()),"Repeated Revision 4 migration preserves Finch and original artwork byte-for-byte");
            Check(sources.All(p=>Hash(p.Key)==p.Value),"Generator source bytes remain frozen through migration and saved-asset readback");
            File.WriteAllText(Path.Combine(Output,"REVISION5-GENERATION.json"),JsonUtility.ToJson(new GenerationReport{result="PASS",revision="4",scope="Explicit guarded authoring migration and independent saved-prefab readback; source hashes identify the generator used. Appearance approval remains pending.",generatorSources=Stamps(sources),protectedInputs=Stamps(protectedBefore),birds=changes,checks=checks.ToArray(),deviceTested=false},true));
            foreach(var d in designs)Inspect(d,true);
            File.WriteAllText(Path.Combine(Output,"STUDIO-VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",scope="Fourteen saved Revision 4 articulated solid-mesh candidates reopened after explicit generation. Strict geometry/optics checks and actual phase/turntable renders; owner appearance approval pending. No Play mode, save reset, build or device install.",birds=birds.ToArray(),checks=checks.ToArray(),deviceTested=false},true));
            var motionBirds=InspectRosterMotion(designs);
            Check(SameSnapshot(second,Snapshot())&&SameSnapshot(protectedBefore,ProtectedSnapshot()),"Readback, structural inspection, rendering and all fifteen motion checks leave saved candidates, Finch and artwork byte-identical");
            File.WriteAllText(Path.Combine(Output,"MOTION-VALIDATION.json"),JsonUtility.ToJson(new MotionReport{result="PASS",scope="Actual saved Unity rigs sampled at shared absolute times with turntable enabled, including a full thirty-second revolution at 30/60/120 Hz. Sampling frequency is a correctness check, not measured device FPS. Appearance approval pending.",baselineId="neon_finch",birds=motionBirds,checks=checks.ToArray(),deviceTested=false},true));
            Debug.Log("SKYPULSE_ROSTER_REVISION5_PASS: fourteen saved sculptures; fifteen Finch-matched motion rigs; appearance approval pending; "+Output);EditorApplication.Exit(0);
        }
        catch(Exception e){Fail(e);}
    }
    static Stamp[] Stamps(Dictionary<string,string> snapshot)=>snapshot.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>new Stamp{path=p.Key,sha256=p.Value}).ToArray();
    static bool SameSnapshot(Dictionary<string,string> first,Dictionary<string,string> second)=>first.Count==second.Count&&first.All(p=>second.TryGetValue(p.Key,out var hash)&&hash==p.Value);
    static Dictionary<string,string> ProtectedSnapshot()
    {
        string roster=SkyPulseBirdRosterWorkshop.Folder+"/";string artwork="Assets/Resources/SkyPulse/characters/roster";
        var paths=Directory.GetFiles(SkyPulseBirdPrototypeWorkshop.Folder,"*",SearchOption.AllDirectories).Where(p=>!p.StartsWith(roster,StringComparison.Ordinal))
            .Concat(Directory.GetFiles(artwork,"*",SearchOption.AllDirectories));
        if(File.Exists(artwork+".meta"))paths=paths.Concat(new[]{artwork+".meta"});
        return paths.Where(p=>Path.GetFileName(p)!=".DS_Store").OrderBy(p=>p,StringComparer.Ordinal).ToDictionary(p=>p,Hash);
    }
    static bool HasRevision5Marker(SkyPulseBirdRosterWorkshop.Design d)
    {var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(SkyPulseBirdRosterWorkshop.PrefabPath(d));var rig=prefab!=null?prefab.GetComponent<SkyPulseNeonFinchPrototype>():null;return rig!=null&&rig.body!=null&&rig.body.name==Revision5Body;}
    static void ReadBackRevision5(SkyPulseBirdRosterWorkshop.Design d)
    {
        var root=PrefabUtility.LoadPrefabContents(SkyPulseBirdRosterWorkshop.PrefabPath(d));
        try
        {
            var rig=root.GetComponent<SkyPulseNeonFinchPrototype>();
            Check(rig!=null&&rig.body!=null&&rig.body.name==Revision5Body,"Independently reopened saved prefab has the exact Revision 4 body marker: "+d.id);
            string geometry=SkyPulseBirdRosterWorkshop.BirdFolder(d)+"/Geometry.asset";
            var owned=root.GetComponentsInChildren<MeshFilter>(true).Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh)).Where(path=>path!=SkyPulseBirdJetWorkshop.MeshPath).ToArray();
            Check(owned.Length>0&&owned.All(path=>path==geometry),"Saved Revision 4 prefab resolves its own canonical geometry without staging references: "+d.id);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static Transform[] MotionJoints(SkyPulseNeonFinchPrototype rig)=>new[]{rig.body,rig.head,rig.leftWing,rig.rightWing,rig.leftWingTip,rig.rightWingTip,rig.tail,rig.tailTip};
    static bool SamePose(Pose first,Pose second)=>Quaternion.Angle(first.root,second.root)<.001f&&(first.rootPosition-second.rootPosition).sqrMagnitude<1e-12f&&first.rotations.Select((r,i)=>Quaternion.Angle(r,second.rotations[i])<.001f&&(first.positions[i]-second.positions[i]).sqrMagnitude<1e-12f).All(ok=>ok);
    static bool Finite(Transform t)=>float.IsFinite(t.localPosition.x)&&float.IsFinite(t.localPosition.y)&&float.IsFinite(t.localPosition.z)&&float.IsFinite(t.localRotation.x)&&float.IsFinite(t.localRotation.y)&&float.IsFinite(t.localRotation.z)&&float.IsFinite(t.localRotation.w);
    static MotionSample SampleMotion(SkyPulseNeonFinchPrototype rig,Transform[] joints,Pose bind,float time)
    {
        rig.SamplePose(time);
        Check(Finite(rig.transform)&&joints.All(Finite),"Finite root and eight-joint pose at "+time+" seconds: "+rig.name);
        return new MotionSample{time=time,rootDelta=Quaternion.Inverse(bind.root)*rig.transform.localRotation,rootPosition=rig.transform.localPosition,jointDeltas=joints.Select((t,i)=>Quaternion.Inverse(bind.rotations[i])*t.localRotation).ToArray()};
    }
    static MotionBird[] InspectRosterMotion(SkyPulseBirdRosterWorkshop.Design[] designs)
    {
        var sources=new[]{new KeyValuePair<string,string>("neon_finch",SkyPulseBirdPrototypeWorkshop.PrefabPath)}.Concat(designs.Select(d=>new KeyValuePair<string,string>(d.id,SkyPulseBirdRosterWorkshop.PrefabPath(d)))).ToArray();
        var reports=new List<MotionBird>();MotionBird baseline=null;
        foreach(var source in sources)
        {
            var root=PrefabUtility.LoadPrefabContents(source.Value);
            try
            {
                var rig=root.GetComponent<SkyPulseNeonFinchPrototype>();Check(rig!=null,"Saved motion source has its rig: "+source.Key);
                var joints=MotionJoints(rig);Check(joints.All(t=>t!=null)&&joints.Distinct().Count()==8,"Saved motion source has eight distinct joints: "+source.Key);
                if(baseline==null)Check(rig.flapPeriod==1.65f&&rig.flapDegrees==28&&rig.bankDegrees==5&&rig.turntableDegreesPerSecond==12,"Approved Finch establishes the 1.65-second, 28-degree flap, 5-degree bank and 12-degree/second turntable baseline");
                else Check(rig.flapPeriod==baseline.flapPeriod&&rig.flapDegrees==baseline.flapDegrees&&rig.bankDegrees==baseline.bankDegrees&&rig.turntableDegreesPerSecond==baseline.turntableDegreesPerSecond,"Saved flap, bank and turntable settings exactly match approved Finch: "+source.Key);
                rig.animate=false;rig.turntable=true;rig.reducedMotion=false;rig.ResetPose();var bind=new Pose(rig,joints);
                var report=new MotionBird{id=source.Key,sourcePrefabSha256=Hash(source.Value),flapPeriod=rig.flapPeriod,flapDegrees=rig.flapDegrees,bankDegrees=rig.bankDegrees,turntableDegreesPerSecond=rig.turntableDegreesPerSecond};
                report.phases=PhaseTimes.Select(time=>SampleMotion(rig,joints,bind,time)).ToArray();
                Check(Quaternion.Angle(report.phases[0].jointDeltas[2],report.phases[1].jointDeltas[2])>20&&Quaternion.Angle(report.phases[1].jointDeltas[2],report.phases[3].jointDeltas[2])>50&&Quaternion.Angle(report.phases[0].jointDeltas[2],report.phases[4].jointDeltas[2])<.001f,"Quarter-cycle phases prove a moving 28-degree wing stroke and 1.65-second return: "+source.Key);
                Check(Quaternion.Angle(report.phases[1].jointDeltas[3],report.phases[3].jointDeltas[3])>50&&Quaternion.Angle(report.phases[0].jointDeltas[4],report.phases[1].jointDeltas[4])>1&&Quaternion.Angle(report.phases[0].jointDeltas[6],report.phases[1].jointDeltas[6])>.05f&&Quaternion.Angle(report.phases[0].jointDeltas[7],report.phases[1].jointDeltas[7])>.05f,"Both wings, distal fan and two-joint tail articulate through the shared flight phases: "+source.Key);
                if(baseline!=null)Check(report.phases.Select((sample,p)=>sample.jointDeltas.Select((rotation,j)=>Quaternion.Angle(rotation,baseline.phases[p].jointDeltas[j])<.001f).All(ok=>ok)).All(ok=>ok),"All eight sampled joint motions match approved Finch at every shared absolute phase: "+source.Key);
                report.turntable=TurntableTimes.Select(time=>SampleMotion(rig,joints,bind,time)).ToArray();
                Check(report.turntable.All(sample=>Quaternion.Angle(sample.rootDelta,Quaternion.Euler(0,sample.time*12,0))<.001f&&(sample.rootPosition-bind.rootPosition).sqrMagnitude<1e-12f),"Actual turntable root yaw is 0/12/60/180/360 degrees at 0/1/5/15/30 seconds without translation: "+source.Key);
                rig.SamplePose(30);var endpoint=new Pose(rig,joints);
                foreach(int fps in new[]{30,60,120})
                {
                    rig.ResetPose();for(int frame=0;frame<=fps*30;frame++)rig.SamplePose(frame/(float)fps);
                    Check(SamePose(endpoint,new Pose(rig,joints)),"Thirty-second absolute root/joint endpoint agrees at "+fps+" Hz: "+source.Key);
                }
                rig.SamplePose(1.2375f);var deterministic=new Pose(rig,joints);rig.SamplePose(5);rig.SamplePose(1.2375f);
                Check(SamePose(deterministic,new Pose(rig,joints)),"Root and all joints sample independently of previous absolute time: "+source.Key);
                rig.reducedMotion=true;rig.SamplePose(0);var reduced=new Pose(rig,joints);
                Check(SamePose(bind,reduced),"Reduced Motion returns the turntable root and every joint to the captured bind pose: "+source.Key);
                foreach(float time in PhaseTimes.Concat(TurntableTimes)){rig.SamplePose(time);Check(SamePose(reduced,new Pose(rig,joints)),"Reduced Motion holds root translation/rotation and all joints steady at "+time+" seconds: "+source.Key);}
                reports.Add(report);if(baseline==null)baseline=report;
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        Check(reports.Count==15&&reports.Select(r=>r.id).Distinct().Count()==15,"All fifteen saved rigs pass the same approved-Finch motion and turntable checks");
        return reports.ToArray();
    }
    // A focused local sculpture review; never enters gameplay or resets a player save.
    public static void RunRavenReview()
    {
        if(Application.companyName!="SkyPulseQA20261006"||!Application.isBatchMode)throw new Exception("Disposable batch QA product required");
        outputOverride=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"raven-v2");
        Directory.CreateDirectory(Output);checks.Clear();birds.Clear();
        foreach(string previous in new[]{"RAVEN-VALIDATION.json","FAILURE.txt"})
        {string path=Path.Combine(Output,previous);if(File.Exists(path))File.Delete(path);}
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");
            var before=Snapshot();
            typeof(SkyPulseBirdRosterWorkshop).GetMethod("ReviseChromeRaven").Invoke(null,null);
            var after=Snapshot();string raven=SkyPulseBirdRosterWorkshop.Folder+"/Chrome Raven/";
            Check(before.Where(p=>!p.Key.StartsWith(raven,StringComparison.Ordinal)).All(p=>after.ContainsKey(p.Key)&&after[p.Key]==p.Value),"Approved Finch and all other saved roster assets remain byte-identical");
            var design=SkyPulseBirdRosterWorkshop.LoadDesigns().Single(d=>d.id=="chrome_raven");
            Inspect(design);
            var first=Snapshot();
            typeof(SkyPulseBirdRosterWorkshop).GetMethod("ReviseChromeRaven").Invoke(null,null);
            var second=Snapshot();
            Check(first.Count==second.Count&&first.All(p=>second[p.Key]==p.Value),"Repeated Raven revision preserves the revised assets byte-for-byte");
            File.WriteAllText(Path.Combine(Output,"RAVEN-VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",scope="Chrome Raven structural revision only, actual Unity Editor model renders and articulated motion; geometry and appearance await owner review. No device install, save reset or release.",birds=birds.ToArray(),checks=checks.ToArray(),deviceTested=false},true));
            Debug.Log("SKYPULSE_ROSTER_RAVEN_PASS: closed bevelled reference geometry, seated optics, deterministic articulated flight; owner appearance approval pending");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Fail(e);}
    }
    public static void CheckPreviewWindow()
    {
        if(Application.companyName!="SkyPulseQA20261006"||!Application.isBatchMode)throw new Exception("Disposable QA product required");
        var window=ScriptableObject.CreateInstance<SkyPulseBirdRosterPreviewWindow>();
        try{var script=MonoScript.FromScriptableObject(window);Check(script!=null&&script.GetClass()==typeof(SkyPulseBirdRosterPreviewWindow),"Gameplay preview window has a valid matching Unity script identity");}
        finally{UnityEngine.Object.DestroyImmediate(window);}
        File.WriteAllText(Path.Combine(Output,"WINDOW-VALIDATION.json"),"{\"result\":\"PASS\",\"scope\":\"Preview EditorWindow instantiates and resolves its Unity script identity; native UI interaction remains unverified\"}");
        Debug.Log("SKYPULSE_ROSTER_WINDOW_PASS: preview window has a valid Unity script identity");EditorApplication.Exit(0);
    }
    static void Inspect(SkyPulseBirdRosterWorkshop.Design d,bool requireRevision5=false)
    {
        Check(Hash("Assets/Resources/SkyPulse/characters/roster/"+d.reference+".png")==d.sourceSha256,"Original source evidence unchanged: "+d.id);
        EditorSceneManager.OpenScene(SkyPulseBirdRosterWorkshop.ScenePath(d));var rig=UnityEngine.Object.FindAnyObjectByType<SkyPulseNeonFinchPrototype>();
        Check(rig!=null&&rig.GetComponentsInChildren<SpriteRenderer>().Length==0&&rig.GetComponentsInChildren<Collider2D>().Length==0,"Solid inspection rig without image-plane bird or gameplay collider: "+d.id);
        if(requireRevision5)Check(rig.body!=null&&rig.body.name==Revision5Body,"Reopened saved review scene resolves the exact Revision 4 sculpture: "+d.id);
        rig.animate=false;rig.turntable=false;rig.reducedMotion=false;rig.ResetPose();
        var filters=rig.GetComponentsInChildren<MeshFilter>();var renderers=rig.GetComponentsInChildren<MeshRenderer>();
        var own=filters.Where(f=>AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(SkyPulseBirdRosterWorkshop.BirdFolder(d)+"/",StringComparison.Ordinal)).Select(f=>f.sharedMesh).Distinct().ToArray();
        int vertices=own.Sum(m=>m.vertexCount),triangles=own.Sum(m=>m.triangles.Length/3);int open=0,nonManifold=0;
        foreach(var mesh in own)
        {
            Check(mesh.vertexCount>=8&&mesh.triangles.Length>=12,"Saved mesh retains authored vertices and triangles: "+d.id+" "+mesh.name);
            Check(mesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z))&&mesh.normals.Length==mesh.vertexCount&&mesh.normals.All(n=>n.sqrMagnitude>.2f),"Finite lit mesh normals: "+d.id+" "+mesh.name);
            var edge=Edges(mesh);open+=edge.Item1;nonManifold+=edge.Item2;
            Check(edge.Item1==0&&edge.Item2==0,"Closed manifold volume: "+d.id+" "+mesh.name);
        }
        Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        Check(bounds.size.z>.4f&&vertices<40000,"Real front-to-back depth and bounded prototype geometry: "+d.id+" v="+vertices+" depth="+bounds.size.z);
        foreach(var shader in renderers.SelectMany(r=>r.sharedMaterials).Select(m=>m.shader).Distinct())Check(shader.isSupported&&ShaderUtil.GetShaderMessages(shader).All(m=>m.severity.ToString()!="Error"),"Supported compiled material: "+shader.name);
        var bones=new[]{rig.body,rig.head,rig.leftWing,rig.rightWing,rig.leftWingTip,rig.rightWingTip,rig.tail,rig.tailTip};
        Check(bones.All(t=>t!=null)&&bones.Distinct().Count()==8,"Eight editable articulated joints bound: "+d.id);
        Vector2 origin=Vector2.zero;foreach(var p in d.body)origin+=p.V;origin/=d.body.Length;
        var point=new Vector2((d.eye[0]-origin.x)*.01f,(origin.y-d.eye[1])*.01f);
        var helmet=filters.Single(f=>f.name=="Closed reference helmet"||f.name=="Continuous fitted helmet");
        foreach(int side in new[]{-1,1})
        {
            var glass=filters.Single(f=>f.name=="Seated dark optical glass "+side);var iris=filters.Single(f=>f.name=="Reference luminous iris "+side);var core=filters.Single(f=>f.name=="Optical core "+side||f.name=="Seated optical emitter "+side);
            var probe=core.transform.TransformPoint(core.sharedMesh.bounds.center);point=new Vector2(probe.x,probe.y);
            Check(filters.Where(f=>f==helmet||f.name.StartsWith("Helmet fitted panel ",StringComparison.Ordinal)||f.name=="Connected recessed optical housing "+side).Any(f=>Overlap(f,glass,point)),"Glass is physically seated in helmet assembly: "+d.id+" "+side);
            Check(Overlap(glass,iris,point)&&Overlap(iris,core,point),"Iris/core are physically connected: "+d.id+" "+side);
            var h=Interval(helmet,point);var b=Interval(glass,point);var i=Interval(iris,point);var c=Interval(core,point);
            bool outward=side<0?b.x<h.x&&i.x<b.x&&c.x<i.x:b.y>h.y&&i.y>b.y&&c.y>i.y;
            if(!outward)CaptureStudio(d,rig,bounds); // Preserve a diagnostic actual render on a failed support check.
            Check(outward,"Optical layers face outward in visible order: "+d.id+" "+side+" helmet="+h+" glass="+b+" iris="+i+" core="+c);
        }
        rig.SamplePose(2);var endpoint=bones.Select(t=>t.localRotation).ToArray();
        foreach(int fps in new[]{30,60,120}){for(int i=0;i<=fps*2;i++)rig.SamplePose(i/(float)fps);Check(bones.Select((t,i)=>Quaternion.Angle(t.localRotation,endpoint[i])).All(a=>a<.001f),"Frame-rate-independent flight "+fps+" Hz: "+d.id);}
        rig.reducedMotion=true;rig.SamplePose(0);var reduced=bones.Select(t=>t.localRotation).ToArray();rig.SamplePose(1);Check(bones.Select((t,i)=>Quaternion.Angle(t.localRotation,reduced[i])).All(a=>a<.001f),"Reduced Motion holds joints steady: "+d.id);rig.reducedMotion=false;
        for(int i=0;i<32;i++)rig.SamplePose(i/60f);long start=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1200;i++)rig.SamplePose(i/120f);long allocated=GC.GetAllocatedBytesForCurrentThread()-start;Check(allocated==0,"Zero warm pose allocations: "+d.id);
        var views=CaptureStudio(d,rig,bounds);
        birds.Add(new BirdReport{id=d.id,name=d.name,reference=d.reference,sourceSha256=d.sourceSha256,meshParts=filters.Length,vertices=vertices,triangles=triangles,openEdges=open,nonManifoldEdges=nonManifold,depth=bounds.size.z,warmAllocatedBytes=allocated,views=views});
    }
    static Tuple<int,int> Edges(Mesh mesh)
    {
        var vertices=mesh.vertices;var unique=new Dictionary<Vector3Int,int>();var index=new int[vertices.Length];
        for(int i=0;i<vertices.Length;i++){var v=vertices[i];var key=new Vector3Int(Mathf.RoundToInt(v.x*100000),Mathf.RoundToInt(v.y*100000),Mathf.RoundToInt(v.z*100000));if(!unique.TryGetValue(key,out int id)){id=unique.Count;unique.Add(key,id);}index[i]=id;}
        var count=new Dictionary<ulong,int>();void Edge(int a,int b){a=index[a];b=index[b];if(a==b)return;ulong key=((ulong)(uint)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);count.TryGetValue(key,out int n);count[key]=n+1;}
        var tri=mesh.triangles;for(int i=0;i<tri.Length;i+=3){Edge(tri[i],tri[i+1]);Edge(tri[i+1],tri[i+2]);Edge(tri[i+2],tri[i]);}return Tuple.Create(count.Count(p=>p.Value==1),count.Count(p=>p.Value>2));
    }
    static Vector2 Interval(MeshFilter filter,Vector2 point)
    {
        var v=filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();var t=filter.sharedMesh.triangles;float min=float.PositiveInfinity,max=float.NegativeInfinity;
        for(int i=0;i<t.Length;i+=3)
        {
            var a=v[t[i]];var b=v[t[i+1]];var c=v[t[i+2]];float den=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);if(Mathf.Abs(den)<1e-9f)continue;
            float u=((b.y-c.y)*(point.x-c.x)+(c.x-b.x)*(point.y-c.y))/den,w=((c.y-a.y)*(point.x-c.x)+(a.x-c.x)*(point.y-c.y))/den,q=1-u-w;
            if(u>=-.00001f&&w>=-.00001f&&q>=-.00001f){float z=u*a.z+w*b.z+q*c.z;min=Mathf.Min(min,z);max=Mathf.Max(max,z);}
        }
        return new Vector2(min,max);
    }
    static bool Overlap(MeshFilter a,MeshFilter b,Vector2 at){var x=Interval(a,at);var y=Interval(b,at);return Mathf.Min(x.y,y.y)-Mathf.Max(x.x,y.x)>.0003f;}
    static string[] CaptureStudio(SkyPulseBirdRosterWorkshop.Design d,SkyPulseNeonFinchPrototype rig,Bounds bounds)
    {
        var folder=Path.Combine(Output,d.id);Directory.CreateDirectory(folder);var camera=UnityEngine.Object.FindObjectsByType<Camera>().First();camera.enabled=false;camera.orthographic=false;camera.fieldOfView=31;camera.aspect=4f/3;float cameraDistance=Mathf.Max(bounds.size.y*.64f,bounds.size.x*.64f/camera.aspect)/Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad)+bounds.extents.z;
        var target=new RenderTexture(1024,768,24){antiAliasing=4};target.Create();camera.targetTexture=target;var pixels=new Texture2D(1024,768,TextureFormat.RGB24,false);var old=RenderTexture.active;var paths=new List<string>();
        try
        {
            void Frame(string file,Vector3 direction,float time)
            {rig.SamplePose(time);camera.transform.position=bounds.center+direction.normalized*cameraDistance;camera.transform.LookAt(bounds.center,Vector3.up);Save(camera,target,pixels,Path.Combine(folder,file));paths.Add(file);}
            Frame("Side.png",new Vector3(.06f,.03f,-1),0);Frame("ThreeQuarter.png",new Vector3(.8f,.35f,-1),0);Frame("Reverse.png",new Vector3(.2f,.1f,1),0);
            for(int i=0;i<PhaseTimes.Length;i++)Frame("Phase-"+i.ToString("00")+".png",new Vector3(.15f,.08f,-1),PhaseTimes[i]);
            rig.turntable=true;
            foreach(float time in TurntableTimes)Frame("Turntable-"+((int)time).ToString("00")+"s.png",new Vector3(.15f,.08f,-1),time);
            rig.turntable=false;
            var motion=Path.Combine(folder,"motion");Directory.CreateDirectory(motion);
            for(int i=0;i<60;i++){rig.SamplePose(i/30f);camera.transform.position=bounds.center+new Vector3(.15f,.08f,-1).normalized*cameraDistance;camera.transform.LookAt(bounds.center,Vector3.up);Save(camera,target,pixels,Path.Combine(motion,i.ToString("000")+".png"));}
        }
        finally{RenderTexture.active=old;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);rig.ResetPose();}
        return paths.ToArray();
    }
    static void Save(Camera camera,RenderTexture target,Texture2D pixels,string path)
    {camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply(false);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,pixels.EncodeToPNG());}
    static void Tick()
    {
        if(!EditorApplication.isPlaying||!SessionState.GetBool("Roster.GameQA",false))return;SessionState.SetBool("Roster.GameQA",false);
        new GameObject("Disposable roster gameplay captures").AddComponent<SkyPulseRosterChecksRunner>().StartChecks(Guard(Game()));
    }
    static IEnumerator Guard(IEnumerator task)
    {while(true){bool more;try{more=task.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more)yield break;yield return task.Current;}}
    static void Fail(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"FAILURE.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    static IEnumerator Game()
    {
        yield return null;yield return null;
        var g=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();Check(g!=null,"Actual SkyPulse game scene boots for preview");g.enabled=false;Set(g,"hapticsEnabled",false);
        var camera=(Camera)Get(g,"flightCamera");var canvas=((GameObject)Get(g,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var target=new RenderTexture(750,1334,24){antiAliasing=2};target.Create();camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);var pixels=new Texture2D(750,1334,TextureFormat.RGB24,false);Call(g,"RefreshViewportDecor");
        var all=SkyPulseBirdRosterWorkshop.LoadDesigns();int captures=0;
        foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay})
        {
            if(mode==SkyPulseMode.EndlessRoute)Call(g,"StartFlight");else Call(g,"LaunchMission",mode,0);
            Call(g,"Flap");
            // Use the actual pilot and collision rules to bring the first geometry into view.
            for(int i=0;i<90;i++)Advance(g,mode,1f/60);
            foreach(var d in all)
            {
                var hitbox=(Transform)Get(g,"birdHitbox");var pos=hitbox.localPosition;var scale=hitbox.localScale;var rotation=hitbox.localRotation;float y=(float)Get(g,"birdY"),velocity=(float)Get(g,"birdVelocity");
                SkyPulseBirdRosterPreview.Select(d.id,true);SkyPulseBirdRosterPreview.TimeOverride=.23f;SkyPulseBirdRosterPreview.UpdateNow(g,.23f);
                Check((float)Get(g,"birdY")==y&&(float)Get(g,"birdVelocity")==velocity&&hitbox.localPosition==pos&&hitbox.localScale==scale&&hitbox.localRotation==rotation,"3D preview retains actual flight state and hitbox: "+mode+" "+d.id);
                Check(SkyPulseBirdRosterPreview.CurrentRig!=null,"Actual game has the selected 3D model: "+d.id);
                Save(camera,target,pixels,Path.Combine(Output,"gameplay",mode+"-"+d.id+".png"));captures++;
            }
        }
        // Three live clips use normal simulation and the same presentation path as the playable game.
        foreach(string id in new[]{"chrome_raven","prism_hummingbird","newbird10"})
        {
            Call(g,"StartFlight");Call(g,"Flap");SkyPulseBirdRosterPreview.Select(id,true);
            string folder=Path.Combine(Output,"gameplay-motion",id);Directory.CreateDirectory(folder);
            for(int i=0;i<90;i++)
            {Advance(g,SkyPulseMode.EndlessRoute,1f/30);if(Get(g,"state").ToString()!="Playing"){Call(g,"StartFlight");Call(g,"Flap");}SkyPulseBirdRosterPreview.TimeOverride=i/30f;SkyPulseBirdRosterPreview.UpdateNow(g,i/30f);Save(camera,target,pixels,Path.Combine(folder,i.ToString("000")+".png"));}
        }
        // Repeated presentation reuses one visual root without replacing the physical bird.
        SkyPulseBirdRosterPreview.Select("chrome_raven",true);SkyPulseBirdRosterPreview.UpdateNow(g,.4f);var existing=SkyPulseBirdRosterPreview.CurrentRig;
        SkyPulseBirdRosterPreview.UpdateNow(g,.4f);Check(SkyPulseBirdRosterPreview.CurrentRig==existing,"Repeated presentation reuses the current rig without duplication");
        SkyPulseBirdRosterPreview.Select("",false);Check(SkyPulseBirdRosterPreview.CurrentRig==null,"Stopping preview removes temporary model and restores original sprite presentation");
        camera.targetTexture=null;RenderTexture.active=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);
        File.WriteAllText(Path.Combine(Output,"GAMEPLAY-VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",scope="Actual Unity game scene and normal flight/Relay simulation. 28 portrait screenshots plus three live 3D bird clips. Editor visual preview; no changes to collision, economy, saves or installed build.",checks=checks.ToArray(),deviceTested=false},true));
        Debug.Log("SKYPULSE_ROSTER_GAMEPLAY_PASS: "+captures+" actual mode/bird captures; original hitbox, state and physics preserved");EditorApplication.Exit(0);
    }
    static void Advance(SkyPulseNativeGame g,SkyPulseMode mode,float dt)
    {
        if(mode==SkyPulseMode.EndlessRoute)
        {float target=0,nearest=float.MaxValue;foreach(var gate in (Array)Get(g,"pipePool")){float x=(float)Get(gate,"X");if(!(bool)Get(gate,"Passed")&&x>=-3&&x<nearest){nearest=x;target=(float)Get(gate,"GapCenter");}}if((float)Get(g,"birdY")<target-.25f&&(float)Get(g,"birdVelocity")<1)Call(g,"Flap");Call(g,"SimulateFlight",dt);Call(g,"UpdateGameplayWingState",dt);Call(g,"UpdateBirdWingMotion");}
        else{typeof(SkyPulsePresentationChecks).GetMethod("Pilot",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{g});Call(g,"SimulateRelay",dt);Call(g,"UpdateRelayPresentation");}
        Call(g,"UpdateCinematicPresentation",dt);
    }
}
public sealed class SkyPulseRosterChecksRunner:MonoBehaviour{public void StartChecks(IEnumerator run){StartCoroutine(run);}}
