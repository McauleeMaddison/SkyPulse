// Disposable Editor geometry audit and actual Studio captures. No asset writes.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using SkyPulse.Mobile;

public static class SkyPulseWingClearanceChecks
{
    [Serializable] public sealed class Contact { public string sampler,wingPart,landingPart;public float phase;public int wingTriangle,landingTriangle; }
    [Serializable] public sealed class Bird
    {
        public string id,name,prefab;public int wingParts,landingParts,sampledPoses,contactPoses;
        public float leftShoulderY,rightShoulderY,lowestWingY,lowestFootY,highestFootY,outboardWingExtent;
        public Contact[] contacts;public string[] captures;
    }
    [Serializable] public sealed class Report
    {
        public string result,scope;public bool deviceTested,assetsChanged,candidateInstanceOnly;public float candidateSpread;public bool candidateDistalOnly;
        public Bird[] birds;
    }
    sealed class Part
    {
        public MeshFilter owner;public string name;public Vector3[] local,body;public int[] triangles;public Bounds bounds;public Bounds[] triBounds;
        public Part(MeshFilter f){owner=f;name=f.name;local=f.sharedMesh.vertices;triangles=f.sharedMesh.triangles;body=new Vector3[local.Length];triBounds=new Bounds[triangles.Length/3];}
        public void Pose(Transform reference)
        {
            var matrix=reference.worldToLocalMatrix*owner.transform.localToWorldMatrix;
            for(int i=0;i<local.Length;i++)body[i]=matrix.MultiplyPoint3x4(local[i]);
            bounds=new Bounds(body[0],Vector3.zero);for(int i=1;i<body.Length;i++)bounds.Encapsulate(body[i]);
            for(int i=0;i<triBounds.Length;i++){int t=i*3;var b=new Bounds(body[triangles[t]],Vector3.zero);b.Encapsulate(body[triangles[t+1]]);b.Encapsulate(body[triangles[t+2]]);triBounds[i]=b;}
        }
    }
    static string Output=>Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT")??Application.temporaryCachePath,"wing-clearance");
    static void Guard(){if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Disposable SkyPulseQA20261006 batch product required");}
    public static void Run()
    {
        Guard();try{InspectAll();EditorApplication.Exit(0);}catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"FAILURE.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void RunCandidates()
    {
        Guard();try{InspectAll(0,true);InspectAll(60,true);InspectAll(70,true);EditorApplication.Exit(0);}catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"FAILURE.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static Report InspectAll(float candidateSpread=0,bool captures=true,bool candidateDistalOnly=true)
    {
        Guard();Directory.CreateDirectory(Output);var results=new List<Bird>();
        Inspect("neon_finch","Neon Finch",SkyPulseBirdPrototypeWorkshop.PrefabPath);
        foreach(var d in SkyPulseBirdRosterWorkshop.LoadDesigns())Inspect(d.id,d.name,SkyPulseBirdRosterWorkshop.PrefabPath(d));
        var report=new Report{result="AUDITED",deviceTested=false,assetsChanged=false,candidateInstanceOnly=candidateSpread!=0,candidateSpread=candidateSpread,candidateDistalOnly=candidateDistalOnly,
            scope="Actual saved articulated mesh surfaces sampled through Studio, exhibition flight, tap-driven flight and Reduced Motion. Bounds reject noncandidates only; reported contacts require intersecting wing/landing mesh triangles. Separate front/side renders expose visual droop even where meshes remain physically separated. This is a geometric audit, not appearance approval or device performance certification.",birds=results.ToArray()};
        File.WriteAllText(Path.Combine(Output,"CLEARANCE-"+candidateSpread.ToString("0")+(candidateDistalOnly?"-distal":"-root")+".json"),JsonUtility.ToJson(report,true));
        Debug.Log("SKYPULSE_WING_AUDIT: birds="+results.Count+"; contact poses="+results.Sum(r=>r.contactPoses)+"; "+Output);return report;

        void Inspect(string id,string name,string path)
        {
            var preview=new PreviewRenderUtility();GameObject model=null;
            try
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new Exception("Missing canonical rig: "+path);
                model=preview.InstantiatePrefabInScene(prefab);var rig=model.GetComponent<SkyPulseNeonFinchPrototype>();
                rig.animate=false;rig.turntable=false;rig.ResetPose();
                bool longFan=id.StartsWith("newbird",StringComparison.Ordinal);
                var left=candidateDistalOnly?rig.leftWingTip:rig.leftWing;
                var right=candidateDistalOnly?rig.rightWingTip:rig.rightWing;
                var originalLeft=left.localRotation;var originalRight=right.localRotation;
                // Every before/after image uses an identical envelope covering the
                // baseline and both proposed fans, so automatic zoom cannot flatter a candidate.
                var allRenderers=model.GetComponentsInChildren<Renderer>();Bounds envelope=default;bool hasEnvelope=false;
                foreach(float spread in longFan?new[]{0f,60f,70f}:new[]{0f})
                {
                    SetSpread(spread);rig.reducedMotion=false;
                    for(int phase=0;phase<24;phase++)
                    {
                        float time=phase/24f*rig.flapPeriod;rig.SamplePose(time);IncludeEnvelope();
                        rig.SampleFlightPose(time,(Mathf.Sin(phase/24f*Mathf.PI*2)+1)*.5f);IncludeEnvelope();
                    }
                }
                SetSpread(longFan?candidateSpread:0);
                var filters=model.GetComponentsInChildren<MeshFilter>(true);
                var wings=filters.Where(f=>f.sharedMesh!=null&&(f.transform.IsChildOf(rig.leftWing)||f.transform.IsChildOf(rig.rightWing))).Select(f=>new Part(f)).ToArray();
                var feet=filters.Where(f=>f.sharedMesh!=null&&Landing(f.transform,rig.body)).Select(f=>new Part(f)).ToArray();
                if(wings.Length==0||feet.Length==0)throw new Exception("Missing wing/landing geometry classification: "+id);
                foreach(var foot in feet)foot.Pose(rig.body);
                var bird=new Bird{id=id,name=name,prefab=path,wingParts=wings.Length,landingParts=feet.Length,leftShoulderY=rig.body.InverseTransformPoint(rig.leftWing.position).y,rightShoulderY=rig.body.InverseTransformPoint(rig.rightWing.position).y,lowestWingY=float.PositiveInfinity,lowestFootY=feet.Min(f=>f.bounds.min.y),highestFootY=feet.Max(f=>f.bounds.max.y)};
                var hits=new List<Contact>();
                foreach(string sampler in new[]{"Studio","ExhibitionFlight","TapFlight","ReducedMotionStudio","ReducedMotionFlight"})
                for(int phase=0;phase<24;phase++)
                {
                    float fraction=phase/24f,time=fraction*rig.flapPeriod;rig.reducedMotion=sampler.StartsWith("ReducedMotion",StringComparison.Ordinal);
                    if(sampler=="Studio"||sampler=="ReducedMotionStudio")rig.SamplePose(time);
                    else if(sampler=="ExhibitionFlight")rig.SampleFlightPose(time);
                    else rig.SampleFlightPose(time,(Mathf.Sin(fraction*Mathf.PI*2)+1)*.5f);
                    foreach(var wing in wings)wing.Pose(rig.body);
                    bird.lowestWingY=Mathf.Min(bird.lowestWingY,wings.Min(w=>w.bounds.min.y));
                    bird.outboardWingExtent=Mathf.Max(bird.outboardWingExtent,wings.Max(w=>Mathf.Max(Mathf.Abs(w.bounds.min.z),Mathf.Abs(w.bounds.max.z))));
                    bird.sampledPoses++;Contact found=null;
                    foreach(var wing in wings)
                    {
                        foreach(var foot in feet)
                        {
                            if(!wing.bounds.Intersects(foot.bounds))continue;
                            if(Intersect(wing,foot,out int wt,out int ft)){found=new Contact{sampler=sampler,phase=fraction,wingPart=wing.name,landingPart=foot.name,wingTriangle=wt,landingTriangle=ft};break;}
                        }
                        if(found!=null)break;
                    }
                    if(found!=null){bird.contactPoses++;hits.Add(found);}
                }
                bird.contacts=hits.ToArray();var files=new List<string>();
                if(captures)
                {
                    preview.ambientColor=new Color(.12f,.14f,.18f);
                    preview.lights[0].transform.rotation=Quaternion.Euler(35,35,0);preview.lights[0].color=new Color(1,.985f,.96f);preview.lights[0].intensity=1.1f;
                    preview.lights[1].transform.rotation=Quaternion.Euler(335,215,0);preview.lights[1].color=new Color(.88f,.94f,1);preview.lights[1].intensity=.7f;
                    var reflection=AssetDatabase.LoadAssetAtPath<Cubemap>(SkyPulseBirdStudioLighting.ReflectionPath);
                    if(reflection!=null){var o=new GameObject("Audit studio reflection",typeof(ReflectionProbe));preview.AddSingleGO(o);var probe=o.GetComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=reflection;probe.intensity=.85f;probe.size=Vector3.one*100;}
                    foreach(float phase in id=="newbird04"?new[]{0f,.25f,.5f,.75f}:new[]{.25f})
                    foreach(string view in new[]{"front","side"})
                    {
                        rig.reducedMotion=false;rig.SamplePose(phase*rig.flapPeriod);
                        var rotation=Quaternion.Euler(view=="front"?8:14,view=="front"?-90:-18,0);
                        Fit(preview.camera,envelope,rotation,1);
                        preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.025f,.032f,.043f);preview.cameraFieldOfView=30;
                        string file=id+"-spread"+candidateSpread.ToString("0")+(candidateDistalOnly?"-distal":"-root")+"-"+view+"-"+phase.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png";
                        preview.BeginPreview(new Rect(0,0,700,700),GUIStyle.none);try{preview.Render();}finally{var texture=(RenderTexture)preview.EndPreview();Save(texture,Path.Combine(Output,file));}files.Add(file);
                    }
                }
                void SetSpread(float spread)
                {rig.ResetPose();left.localRotation=originalLeft*Quaternion.Euler(spread,0,0);right.localRotation=originalRight*Quaternion.Euler(-spread,0,0);rig.CaptureBindPose();}
                void IncludeEnvelope()
                {foreach(var renderer in allRenderers){if(!hasEnvelope){envelope=renderer.bounds;hasEnvelope=true;}else envelope.Encapsulate(renderer.bounds);}}
                bird.captures=files.ToArray();results.Add(bird);Debug.Log("WING_CLEARANCE_BIRD: "+id+"; contact poses="+bird.contactPoses+"/"+bird.sampledPoses+"; wing minY="+bird.lowestWingY+"; feet="+bird.lowestFootY+".."+bird.highestFootY+"; lateral span="+bird.outboardWingExtent);
            }
            finally{preview.Cleanup();}
        }
    }
    static bool Landing(Transform t,Transform body)
    {
        while(t!=null&&t!=body)
        {string n=t.name.ToLowerInvariant();if(n.Contains("talon")||n.Contains("landing shank")||n.Contains("shank armour")||n.Contains("ankle")||n.Contains("landing claw")||n.Contains("claw hook"))return true;t=t.parent;}
        return false;
    }
    static bool Intersect(Part a,Part b,out int at,out int bt)
    {
        for(int i=0;i<a.triBounds.Length;i++)
        {
            if(!a.triBounds[i].Intersects(b.bounds))continue;
            int ai=i*3;var a0=a.body[a.triangles[ai]];var a1=a.body[a.triangles[ai+1]];var a2=a.body[a.triangles[ai+2]];
            for(int j=0;j<b.triBounds.Length;j++)
            {
                if(!a.triBounds[i].Intersects(b.triBounds[j]))continue;
                int bi=j*3;var b0=b.body[b.triangles[bi]];var b1=b.body[b.triangles[bi+1]];var b2=b.body[b.triangles[bi+2]];
                if(Segment(a0,a1,b0,b1,b2)||Segment(a1,a2,b0,b1,b2)||Segment(a2,a0,b0,b1,b2)||Segment(b0,b1,a0,a1,a2)||Segment(b1,b2,a0,a1,a2)||Segment(b2,b0,a0,a1,a2)){at=i;bt=j;return true;}
            }
        }
        at=bt=-1;return false;
    }
    // Noncoplanar segment/triangle intersection. Broad bounds never certify a hit.
    static bool Segment(Vector3 from,Vector3 to,Vector3 a,Vector3 b,Vector3 c)
    {
        var d=to-from;var e1=b-a;var e2=c-a;var p=Vector3.Cross(d,e2);float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<1e-9f)return false;
        float inverse=1/det;var t=from-a;float u=Vector3.Dot(t,p)*inverse;if(u<0||u>1)return false;
        var q=Vector3.Cross(t,e1);float v=Vector3.Dot(d,q)*inverse;if(v<0||u+v>1)return false;
        float hit=Vector3.Dot(e2,q)*inverse;return hit>.00001f&&hit<.99999f;
    }
    static void Fit(Camera camera,Bounds b,Quaternion rotation,float aspect)
    {
        var inv=Quaternion.Inverse(rotation);float tanY=Mathf.Tan(15*Mathf.Deg2Rad),tanX=tanY*aspect,distance=.1f;
        for(int i=0;i<8;i++){var offset=inv*Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));distance=Mathf.Max(distance,-offset.z+Mathf.Abs(offset.x)/tanX,-offset.z+Mathf.Abs(offset.y)/tanY);}
        camera.transform.SetPositionAndRotation(b.center+rotation*Vector3.back*distance*1.08f,rotation);camera.nearClipPlane=.01f;camera.farClipPlane=distance+b.extents.magnitude*4+10;
    }
    static void Save(RenderTexture target,string path)
    {var prior=RenderTexture.active;RenderTexture.active=target;var t=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,target.width,target.height),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=prior;}
}
