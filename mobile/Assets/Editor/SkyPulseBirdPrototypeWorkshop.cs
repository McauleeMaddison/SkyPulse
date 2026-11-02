using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SkyPulse.Mobile;

/// <summary>Creates the isolated, editable first Neon Finch art-review candidate.</summary>
public static class SkyPulseBirdPrototypeWorkshop
{
    public const string Folder = "Assets/Editor/SkyPulseBirdWorkshop";
    public const string ScenePath = Folder + "/Scenes/Neon Finch Art Review.unity";
    public const string PrefabPath = Folder + "/Prefabs/Neon Finch Rig.prefab";
    static Material blue, cyan, navy, ivory, gold, dark, silver, glow;
    static int meshNumber;
    static bool revisionBuild, refreshHead;
    static int revisedMeshCount;
    public const string OpticsRevision = "Integrated optics and blue jet v10";
    static Transform model;
    struct Section
    {
        public float x,y,h,w;
        public Section(float x,float y,float h,float w) { this.x=x;this.y=y;this.h=h;this.w=w; }
    }
    sealed class Geometry
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<Color> colours = new List<Color>();
        public readonly List<int> t = new List<int>();
        public int Add(Vector3 p,Vector2 tex)=>Add(p,tex,Color.white);
        public int Add(Vector3 p,Vector2 tex,Color colour) {int i=v.Count;v.Add(p);uv.Add(tex);colours.Add(colour);return i;}
        public void Tri(int a,int b,int c) { t.Add(a);t.Add(b);t.Add(c); }
        public void Quad(int a,int b,int c,int d) { Tri(a,b,c);Tri(a,c,d); }
        public Mesh Build(string name)
        {
            var m=new Mesh {name=name};m.SetVertices(v);m.SetUVs(0,uv);m.SetColors(colours);m.SetTriangles(t,0);m.RecalculateBounds();FiniteNormals(m);return m;
        }
    }
    [MenuItem("Tools/SkyPulse/Bird Workshop/Create Neon Finch 3D art review")]
    public static void Generate()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before creating workshop assets.");
        // A repeat is deliberately a no-op: preserve owner-edited assets and GUIDs.
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)!=null)
        { if(!File.Exists(ScenePath))CreateStudio();Debug.Log("SKYPULSE_NEON_FINCH_EXISTS: existing rig and owner edits preserved. Open "+ScenePath);return; }
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        foreach(string part in new[]{"Scenes","Prefabs","Meshes","Materials","Animations"})Directory.CreateDirectory(Folder+"/"+part);
        AssetDatabase.Refresh();meshNumber=0;
        blue=Material("01 Cobalt ceramic metal",1);
        cyan=Material("02 Cyan feather enamel",2);
        navy=Material("03 Cobalt shadow edges",3);
        ivory=Material("04 Warm ivory belly",4);
        gold=Material("05 Brushed champagne gold",5);
        dark=Material("06 Recessed optical black",6);
        silver=Material("07 Machined titanium",7);
        glow=Material("08 Cyan optical emission",8);
        ApplyCurrentFinishes();
        var root=new GameObject("Neon Finch · 3D first art-review candidate");model=root.transform;
        var rig=root.AddComponent<SkyPulseNeonFinchPrototype>();
        var body=Bone(model,"01 Body and flight bank",Vector3.zero);rig.body=body;
        var head=Bone(body,"02 Optical helmet and beak",new Vector3(.35f,.24f,0));rig.head=head;
        var tail=Bone(body,"05 Tail pitch and yaw",new Vector3(-.89f,-.035f,0));rig.tail=tail;
        var tailTip=Bone(tail,"06 Tail tip follow",new Vector3(-.41f,-.035f,0));rig.tailTip=tailTip;
        BuildBody(body);BuildHead(head);BuildTail(tail,tailTip);
        BuildWing(body,-1,out var left,out var leftTip);BuildWing(body,1,out var right,out var rightTip);
        rig.leftWing=left;rig.rightWing=right;rig.leftWingTip=leftTip;rig.rightWingTip=rightTip;
        rig.CaptureBindPose();rig.animate=true;rig.turntable=true;
        PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);UnityEngine.Object.DestroyImmediate(root);
        CreateClips();CreateStudio();AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("SKYPULSE_NEON_FINCH_CREATED: editable mechanical finch art-review candidate only; no gameplay references or source art changed. "+ScenePath);
    }
    [MenuItem("Tools/SkyPulse/Bird Workshop/Refine Finch sculpture and metal finish")]
    public static void ReviseOptics()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before revising the Finch sculpture.");
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)==null)throw new InvalidOperationException("Create the Finch art-review rig first.");
        // Explicit sculpture revision: existing bird buffers 001–125 and
        // eight review materials. Prefab, rig and clips retain
        // their identities and bindings. Studio lighting is a separate step.
        blue=ExistingMaterial("01 Cobalt ceramic metal");cyan=ExistingMaterial("02 Cyan feather enamel");
        navy=ExistingMaterial("03 Cobalt shadow edges");ivory=ExistingMaterial("04 Warm ivory belly");
        gold=ExistingMaterial("05 Brushed champagne gold");dark=ExistingMaterial("06 Recessed optical black");
        silver=ExistingMaterial("07 Machined titanium");glow=ExistingMaterial("08 Cyan optical emission");
        var temporary=new GameObject("Temporary head mesh authoring"){hideFlags=HideFlags.HideAndDontSave};
        meshNumber=0;revisedMeshCount=0;revisionBuild=true;
        try
        {
            // Keep the existing part order and mesh identities while updating
            // the sculpture. Use the original body/head/tail/left/right authoring sequence.
            refreshHead=true;
            BuildBody(temporary.transform);
            BuildHead(Bone(temporary.transform,"Temporary head",Vector3.zero));
            var tail=Bone(temporary.transform,"Temporary tail",new Vector3(-.89f,-.035f,0));
            var tip=Bone(tail,"Temporary tail tip",new Vector3(-.41f,-.035f,0));
            BuildTail(tail,tip);
            BuildWing(temporary.transform,-1,out _,out _);
            BuildWing(temporary.transform,1,out _,out _);
        }
        finally{refreshHead=false;revisionBuild=false;UnityEngine.Object.DestroyImmediate(temporary);}
        ApplyCurrentFinishes();
        SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();
        Debug.Log("SKYPULSE_FINCH_HEAD_REVISED: "+revisedMeshCount+" existing bird meshes updated in place; all asset GUIDs, scene, prefab, rig and animation retained; only the declared review materials refreshed.");
    }
    public static void ReviseOpticsInQa()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Disposable batch QA product required for this entrypoint.");
        ReviseOptics();
    }
    // The single review finish table. Studio reflections and real lighting make
    // the material readable; no painted-in highlights or outline glow are used.
    public static void ApplyReviewFinish(Material material,int finish)
    {
        var shader=Shader.Find("SkyPulse/Workshop/Finch Armour Finish");
        if(shader==null)throw new InvalidOperationException("Import the Finch armour finish shader before revising the workshop.");
        Color colour;float metal,smooth,kind;
        switch(finish)
        {
            case 1:colour=new Color(.015f,.115f,.93f);metal=.22f;smooth=.48f;kind=0;break;
            case 2:colour=new Color(.005f,.72f,.98f);metal=.18f;smooth=.44f;kind=0;break;
            case 3:colour=new Color(.004f,.009f,.022f);metal=.12f;smooth=.30f;kind=2;break;
            case 4:colour=new Color(.91f,.94f,1);metal=.18f;smooth=.48f;kind=1;break;
            case 5:colour=new Color(1,.65f,.065f);metal=.55f;smooth=.52f;kind=1;break;
            case 6:colour=new Color(.002f,.006f,.013f);metal=.06f;smooth=.62f;kind=3;break;
            case 7:colour=new Color(.90f,.96f,1);metal=.20f;smooth=.48f;kind=1;break;
            case 8:colour=new Color(.015f,.85f,1);metal=0;smooth=.40f;kind=4;break;
            default:throw new ArgumentOutOfRangeException(nameof(finish));
        }
        bool changed=false;
        if(material.shader!=shader){material.shader=shader;changed=true;}
        void Float(string key,float value){if(material.GetFloat(key)!=value){material.SetFloat(key,value);changed=true;}}
        void Colour(string key,Color value){if(!material.GetColor(key).Equals(value)){material.SetColor(key,value);changed=true;}}
        Colour("_Color",colour);Colour("_BladeColor",finish==3?new Color(.015f,.115f,.93f):colour);
        Colour("_EmissionColor",finish==8?new Color(.4f,1.1f,1.5f):finish==2?new Color(0,.10f,.16f):finish==4?new Color(.62f,.65f,.70f):finish==7?new Color(.85f,.95f,1):Color.black);
        Float("_Metallic",metal);Float("_Glossiness",smooth);Float("_FinishKind",kind);
        Float("_EmissionStrength",finish==8?.85f:finish==2?.5f:finish==4?.65f:finish==7?.9f:0);
        Float("_BrushStrength",0);Float("_SatinStrength",kind==2?.25f:kind>=3?0:.5f);
        if(changed)EditorUtility.SetDirty(material);
    }
    static void ApplyCurrentFinishes()
    {
        var materials=new[]{blue,cyan,navy,ivory,gold,dark,silver,glow};
        for(int i=0;i<materials.Length;i++){ApplyReviewFinish(materials[i],i+1);AssetDatabase.SaveAssetIfDirty(materials[i]);}
    }
    static Material ExistingMaterial(string name)
    {
        var value=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/"+name+".mat");
        if(value==null)throw new InvalidOperationException("Existing Finch material is missing: "+name);
        return value;
    }
    [MenuItem("Tools/SkyPulse/Bird Workshop/Open Neon Finch art review")]
    public static void Open()
    {
        if(!File.Exists(ScenePath))Generate();
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
    }
    static Material Material(string name,int finish)
    {
        string path=Folder+"/Materials/"+name+".mat";
        var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing!=null)return existing;
        var shader=Shader.Find("SkyPulse/Workshop/Finch Armour Finish");
        if(shader==null)throw new InvalidOperationException("The Finch armour shader must be imported before creating its review assets.");
        var material=new Material(shader){name=name};ApplyReviewFinish(material,finish);
        AssetDatabase.CreateAsset(material,path);return material;
    }
    static Transform Bone(Transform parent,string name,Vector3 local)
    { var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=local;return g.transform; }
    static Mesh Save(Mesh m)
    {
        string name=(++meshNumber).ToString("000")+" "+m.name;
        string path=Folder+"/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old!=null)
        {
            if(refreshHead)
            {
                m.name=old.name;
                if(!SameMesh(m,old))
                {
                    if(!Application.isBatchMode)Undo.RecordObject(old,"Revise Finch mechanical head optics");
                    EditorUtility.CopySerialized(m,old);EditorUtility.SetDirty(old);AssetDatabase.SaveAssetIfDirty(old);revisedMeshCount++;
                }
            }
            UnityEngine.Object.DestroyImmediate(m);return old;
        }
        if(revisionBuild){UnityEngine.Object.DestroyImmediate(m);throw new InvalidOperationException("Revision stopped: expected existing mesh is missing: "+path);}
        m.name=name;AssetDatabase.CreateAsset(m,path);return m;
    }
    static bool SameMesh(Mesh a,Mesh b)
    {
        if(a.vertexCount!=b.vertexCount||a.subMeshCount!=b.subMeshCount||a.bounds!=b.bounds)return false;
        var av=a.vertices;var bv=b.vertices;var an=a.normals;var bn=b.normals;var au=a.uv;var bu=b.uv;var at=a.triangles;var bt=b.triangles;
        var ac=a.colors;var bc=b.colors;
        if(an.Length!=bn.Length||au.Length!=bu.Length||at.Length!=bt.Length||ac.Length!=bc.Length)return false;
        for(int i=0;i<av.Length;i++)if(!av[i].Equals(bv[i]))return false;
        for(int i=0;i<an.Length;i++)if(!an[i].Equals(bn[i]))return false;
        for(int i=0;i<au.Length;i++)if(!au[i].Equals(bu[i]))return false;
        for(int i=0;i<at.Length;i++)if(at[i]!=bt[i])return false;
        for(int i=0;i<ac.Length;i++)if(!ac[i].Equals(bc[i]))return false;
        return true;
    }
    static GameObject Part(Transform parent,string name,Mesh m,Material material,Vector3 position=default)
    {
        var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.transform.localPosition=position;
        g.GetComponent<MeshFilter>().sharedMesh=Save(m);var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;r.receiveShadows=true;return g;
    }
    // Native reference pixel registration, intentionally inside mesh buffers.
    static Vector3 ReferenceBodyPixel(float px,float py,float z=0)
    {return new Vector3((px-285)/100f,(255-py)/100f,z);}
    static Vector3 ReferenceBodySkin(float px,float py,int side,float lift=0)
    {
        var p=ReferenceBodyPixel(px,py);var q=SmoothSection(Hull(),p.x);
        float v=Mathf.Clamp01(Mathf.Abs(p.y-q.y)/Mathf.Max(.001f,q.h));
        p.z=side*(q.w*Mathf.Sqrt(Mathf.Max(0,1-v*v))+lift);return p;
    }
    static Mesh ReferenceBodyOval(string name,Vector3 center,float rx,float ry,float depth,int side,float bow=.012f)
    {
        var g=new Geometry();const int slices=32;float[] radii={0,.48f,.88f,1};
        for(int face=0;face<2;face++)
        {
            int first=g.v.Count;
            for(int ring=0;ring<radii.Length;ring++)for(int i=0;i<=slices;i++)
            {float r=radii[ring],a=i*Mathf.PI*2/slices;g.Add(center+new Vector3(Mathf.Cos(a)*rx*r,Mathf.Sin(a)*ry*r,side*(face==0?depth*.5f+bow*(1-r*r):-depth*.5f)),new Vector2(.5f+Mathf.Cos(a)*r*.5f,.5f+Mathf.Sin(a)*r*.5f));}
            int stride=slices+1;
            for(int i=0;i<slices;i++){int c=first+i,a=first+stride+i,b=a+1;if((face==0)==(side>0))g.Tri(c,a,b);else g.Tri(c,b,a);}
            for(int ring=1;ring<radii.Length-1;ring++)for(int i=0;i<slices;i++)
            {int a=first+ring*stride+i;if((face==0)==(side>0))g.Quad(a,a+stride,a+stride+1,a+1);else g.Quad(a,a+1,a+stride+1,a+stride);}
        }
        int back=4*(slices+1),outer=3*(slices+1);
        for(int i=0;i<slices;i++){int a=outer+i,b=outer+i+1;if(side>0)g.Quad(a,back+a,back+b,b);else g.Quad(a,b,back+b,back+a);}
        return g.Build(name);
    }
    static Mesh ReferenceBodyClean(Mesh mesh)
    {
        var vertices=mesh.vertices;var old=mesh.triangles;var triangles=new List<int>();
        for(int i=0;i<old.Length;i+=3)if(Vector3.Cross(vertices[old[i+1]]-vertices[old[i]],vertices[old[i+2]]-vertices[old[i]]).sqrMagnitude>1e-18f){triangles.Add(old[i]);triangles.Add(old[i+1]);triangles.Add(old[i+2]);}
        mesh.triangles=triangles.ToArray();FiniteNormals(mesh);return mesh;
    }
    static Mesh ReferenceBodyDome(string name,float rx,float ry,float height,int slices,int rings)
    {return ReferenceBodyClean(Dome(name,rx,ry,height,slices,rings));}
    static Mesh ReferenceBodyLoft(string name,Section[] profile,int sides)
    {Array.Sort(profile,(a,b)=>a.x.CompareTo(b.x));return Loft(name,profile,sides);}
    static Mesh ReferenceBodyEllipseRing(string name,Vector3 center,float rx,float ry,float inner,float depth,int side)
    {
        var m=Ring(name,1,inner,depth,40,6);var v=m.vertices;
        for(int i=0;i<v.Length;i++)v[i]=center+new Vector3(v[i].x*rx,v[i].y*ry,v[i].z);
        m.vertices=v;m.RecalculateBounds();FiniteNormals(m);return m;
    }
    static Mesh ReferenceShoulderMount(Vector3 shoulder,Vector3 hinge,int side)
    {
        var g=new Geometry();Append(g,ReferenceBodyOval("wing shoulder saddle rim",shoulder,.395f,.195f,.12f,side,.018f));
        Vector3 start=ReferenceBodyPixel(288,230,side*.20f)-hinge,end=ReferenceBodyPixel(268,205,side*.75f)-hinge;
        float length=Vector3.Distance(start,end);Quaternion rotation=Quaternion.FromToRotation(Vector3.right,end-start);
        var axle=Loft("concealed shoulder axle",new[]{new Section(0,0,.085f,.085f),new Section(length*.25f,0,.09f,.09f),new Section(length,0,.065f,.065f)},20);
        var vertices=axle.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=start+rotation*vertices[i];axle.vertices=vertices;axle.RecalculateBounds();FiniteNormals(axle);Append(g,axle);
        return g.Build("wing shoulder saddle rim");
    }
    static Vector3 ReferenceBodyBelly(float u,float v,int side,float lift)
    {
        Vector2 inner=Cubic2(new Vector2(360,239),new Vector2(347,251),new Vector2(335,291),new Vector2(282,299),u);
        Vector2 outer=Cubic2(new Vector2(381,247),new Vector2(377,281),new Vector2(330,317),new Vector2(281,307),u);
        Vector2 p=Vector2.Lerp(inner,outer,v);return ReferenceBodySkin(p.x,p.y,side,lift);
    }
    static Mesh ReferenceBodyCollar(string name,float inset,float lift)
    {
        var g=new Geometry();
        foreach(int side in new[]{-1,1})
        {
            int ss=side;
            Append(g,CurvedPlate(name,(u,v)=>
            {
                float vv=ss>0?v:1-v;Vector2 a=Vector2.Lerp(new Vector2(305+inset,180+inset),new Vector2(331,172+inset),u);
                Vector2 b=Vector2.Lerp(new Vector2(381-inset,252-inset),new Vector2(389-inset,239),u);
                Vector2 p=Vector2.Lerp(a,b,vv);var r=ReferenceBodyPixel(p.x,p.y,ss*(.265f+lift+.035f*Mathf.Sin(u*Mathf.PI)));return r;
            },12,6,.035f,.005f,.005f,.008f));
        }
        return g.Build(name);
    }
    // A compact breast and rising nape sit beneath independently stepped feather armour.
    static Section[] Hull()=>new[]{
        new Section(-.55f,-.275f,.012f,.035f),new Section(-.53f,-.235f,.115f,.14f),
        new Section(-.40f,-.175f,.275f,.24f),new Section(-.25f,-.125f,.375f,.315f),
        new Section(-.05f,-.085f,.445f,.355f),new Section(.15f,-.015f,.495f,.37f),
        new Section(.35f,.09f,.54f,.355f),new Section(.55f,.19f,.52f,.32f),
        new Section(.75f,.17f,.34f,.245f),new Section(.90f,.185f,.145f,.13f),
        new Section(1.01f,.17f,.015f,.025f)};
    static void BuildBody(Transform body)
    {
        Part(body,"Tapered cobalt fuselage",PlatedFuselage(),blue);
        Part(body,"Ivory chest shield",KeelPlating(),ivory);
        foreach(int side in new[]{-1,1})
        {
            Part(body,"Chest shield gasket "+side,KeelGasket(side),navy);
            for(int p=0;p<3;p++)Part(body,"Swept back armor seam "+side+" "+p,BodyPlateGasket(side,p),navy);
            Vector3 legOrigin=new Vector3(-.49f,-.29f,side*.32f);
            var leg=Bone(body,"Folded landing talon "+side,legOrigin);
            Part(leg,"Gold leg actuator",TalonSocket(side),gold);
            Part(leg,"Talon shank",Feather("folded cobalt shank",ReferenceBodyPixel(271,296,side*.445f)-legOrigin,ReferenceBodyPixel(257,296,side*.45f)-legOrigin,ReferenceBodyPixel(245,312,side*.455f)-legOrigin,ReferenceBodyPixel(240,326,side*.46f)-legOrigin,.075f,.038f,12,10),blue);
            Part(leg,"Talon toe one",Feather("pointed titanium toe",ReferenceBodyPixel(270,307,side*.455f)-legOrigin,ReferenceBodyPixel(265,317,side*.46f)-legOrigin,ReferenceBodyPixel(254,330,side*.47f)-legOrigin,ReferenceBodyPixel(249,340,side*.47f)-legOrigin,.055f,.028f,10,8),navy);
            Part(leg,"Talon toe two",Feather("second pointed toe",ReferenceBodyPixel(279,311,side*.40f)-legOrigin,ReferenceBodyPixel(284,315,side*.40f)-legOrigin,ReferenceBodyPixel(279,320,side*.41f)-legOrigin,ReferenceBodyPixel(272,322,side*.41f)-legOrigin,.042f,.025f,10,8),navy);
        }
        Part(body,"Neck articulated dark collar",CollarShell("neck flexible gasket",.235f,.38f,0),navy);
        Part(body,"Neck collar cobalt bevel",CollarShell("neck blue rim",.325f,.425f,.013f),blue);
    }
    static void BuildHead(Transform head)
    {
        Part(head,"Sculpted cobalt helmet",MechanicalHelmet(),blue);
        Part(head,"Long swept crown blade",HeadCrest("rear crown blade",new[]{HeadPixel2(321,116),HeadPixel2(345,121),HeadPixel2(375,136),HeadPixel2(352,139)},.082f),navy);
        Part(head,"Cobalt crown inlay",HeadCrest("crown enamel",new[]{HeadPixel2(325,119),HeadPixel2(344,123),HeadPixel2(370,135),HeadPixel2(352,136)},.091f),blue);
        Part(head,"Forward swept crown blade",HeadCrest("short crown blade",new[]{HeadPixel2(331,105),HeadPixel2(357,113),HeadPixel2(378,126),HeadPixel2(394,135),HeadPixel2(362,130)},.066f),gold);
        Part(head,"Crown cyan vent",HeadCrest("crest cyan center",new[]{HeadPixel2(344,112),HeadPixel2(358,116),HeadPixel2(378,128),HeadPixel2(389,133),HeadPixel2(362,127)},.076f),blue);
        foreach(int side in new[]{-1,1})
        {
            // Preserve the existing prefab origins; author the traced volume inside each mesh.
            Vector3 optic=new Vector3(.40f,.35f,side*.414f);
            Part(head,"Optic recessed gasket "+side,SensorHousing("optical black recessed surround",side,optic),navy,optic);
            Part(head,"Optic titanium bezel "+side,SensorFrame("optic outer beveled lip",side,optic+new Vector3(0,0,side*.026f)),silver,optic+new Vector3(0,0,side*.026f));
            Part(head,"Champagne gold brow arc "+side,SensorFrame("gold optical crescent",side,optic+new Vector3(0,0,side*.042f)),gold,optic+new Vector3(0,0,side*.042f));
            Part(head,"Dark curved optical glass "+side,SensorPanel("curved black optical lens",side,optic+new Vector3(0,0,side*.049f)),dark,optic+new Vector3(0,0,side*.049f));
            Part(head,"Forward cyan optical chevron "+side,SensorPanel("inset optical direction mark",side,optic+new Vector3(.025f,.015f,side*.113f)),glow,optic+new Vector3(.025f,.015f,side*.113f));
            Part(head,"Ivory jaw rim "+side,CheekPlates("jaw machined trim",side),ivory);
            Part(head,"Brow fastener "+side,Shift(OpticalDisc("brow screw",HeadPixel2(418,182),new Vector2(.05f,.08f),side,Vector3.zero,.006f,.008f,.022f,28,4),-new Vector3(.11f,.67f,side*.35f)),silver,new Vector3(.11f,.67f,side*.35f));
        }
        Part(head,"Gold upper beak",ChiselBeak("gold beveled finch beak",0),gold,new Vector3(.66f,.22f,0));
        Part(head,"Dark lower beak seam",ChiselBeak("beak split gasket",1),navy,new Vector3(.66f,.20f,0));
        Part(head,"Beak lower gold plate",ChiselBeak("lower beak",2),gold,new Vector3(.66f,.185f,0)).transform.localScale=new Vector3(.89f,.28f,.84f);
    }
    // Pixel coordinates measured directly from volt-frame-04-v1.png. The original
    // head bind origin is retained; all reference points are authored in mesh space.
    static Vector2 HeadPixel2(float x,float y)=>new Vector2((x-320)*.01f,(231-y)*.01f);
    static Section HeadSection(float x,float top,float bottom,float depth)=>new Section((x-320)*.01f,(231-bottom)*.01f,(231-top)*.01f,depth);
    static Section BeakSection(float x,float top,float bottom,float depth)=>new Section((x-320)*.01f,(231-(top+bottom)*.5f)*.01f,(bottom-top)*.005f,depth);

    static Mesh HeadCrest(string name,Vector2[] points,float depth)
    {
        points=CounterClockwise(points);var g=new Geometry();Vector2 center=Vector2.zero;foreach(var p in points)center+=p;center/=points.Length;int n=points.Length;const float bevel=.009f;
        for(int ring=0;ring<4;ring++)foreach(var p in points)
        {
            var q=ring==0||ring==3?Vector2.Lerp(center,p,.94f):p;float z=ring==0?-depth:ring==1?-depth+bevel:ring==2?depth-bevel:depth;
            g.Add(new Vector3(q.x,q.y,z),q);
        }
        for(int ring=0;ring<3;ring++)for(int i=0;i<n;i++)g.Quad(ring*n+i,ring*n+(i+1)%n,(ring+1)*n+(i+1)%n,(ring+1)*n+i);
        int front=g.Add(new Vector3(center.x,center.y,-depth),center),back=g.Add(new Vector3(center.x,center.y,depth),center);
        for(int i=0;i<n;i++){g.Tri(front,(i+1)%n,i);g.Tri(back,3*n+i,3*n+(i+1)%n);}return g.Build(name);
    }
    static Mesh OpticalDisc(string name,Vector2 center,Vector2 radius,int side,Vector3 origin,float depth,float bulge,float thickness,int slices,int rings)
    {
        var g=new Geometry();int[] centers=new int[2],starts=new int[2];
        float Front(Vector2 p,float r)
        {
            float support=name=="curved black optical lens"?.78f*UndentedHeadDepth(p):name=="inset optical direction mark"?EyeLensFront(p):name=="brow screw"?EyeIrisFront(p):0;
            return support+depth+bulge*(1-r*r);
        }
        float Depth(Vector2 p,float r,int face)
        {
            float front=Front(p,r);if(face==0)return front;
            // The lens base is embedded in the closed pocket. Iris/core backs
            // overlap the preceding optical layer instead of hovering above it.
            return name=="curved black optical lens"?Mathf.Min(front-thickness,HeadSkinDepth(p)-.008f):front-thickness;
        }
        for(int face=0;face<2;face++)
        {
            centers[face]=g.Add(new Vector3(center.x,center.y,side*Depth(center,0,face))-origin,new Vector2(.5f,.5f));starts[face]=g.v.Count;
            for(int ring=1;ring<=rings;ring++)for(int i=0;i<slices;i++)
            {
                float r=ring/(float)rings,a=i*Mathf.PI*2/slices;var p=center+new Vector2(Mathf.Cos(a)*radius.x*r,Mathf.Sin(a)*radius.y*r);
                g.Add(new Vector3(p.x,p.y,side*Depth(p,r,face))-origin,new Vector2(.5f+Mathf.Cos(a)*r*.5f,.5f+Mathf.Sin(a)*r*.5f));
            }
            bool outward=face==0?side>0:side<0;
            for(int i=0;i<slices;i++){int a=starts[face]+i,b=starts[face]+(i+1)%slices;if(outward)g.Tri(centers[face],a,b);else g.Tri(centers[face],b,a);}
            for(int ring=0;ring<rings-1;ring++)for(int i=0;i<slices;i++)
            {int a=starts[face]+ring*slices+i,b=starts[face]+ring*slices+(i+1)%slices;if(outward)g.Quad(a,a+slices,b+slices,b);else g.Quad(a,b,b+slices,a+slices);}
        }
        for(int i=0;i<slices;i++)
        {
            int a=starts[0]+(rings-1)*slices+i,b=starts[0]+(rings-1)*slices+(i+1)%slices,c=starts[1]+(rings-1)*slices+(i+1)%slices,d=starts[1]+(rings-1)*slices+i;
            if(side>0)g.Quad(a,d,c,b);else g.Quad(a,b,c,d);
        }
        var mesh=g.Build(name);WeldSmoothNormals(mesh);return mesh;
    }
    // Shape-preserving cubic interpolation gives a continuous silhouette with
    // no roof corners, while the independently curated sections avoid a sphere.
    static Section[] HelmetProfile()=>new[]{HeadSection(319,165,169,.025f),HeadSection(329,146,188,.18f),HeadSection(351,136,213,.31f),HeadSection(374,132,220,.365f),HeadSection(392,134,228,.38f),HeadSection(410,140,227,.36f),HeadSection(426,146,220,.29f),HeadSection(438,159,207,.18f),HeadSection(445,176,190,.035f)};
    static float SectionValue(Section s,int field)=>field==0?s.y:field==1?s.h:s.w;
    static float SectionSlope(Section[] p,int index,int field)
    {
        if(index==0)return (SectionValue(p[1],field)-SectionValue(p[0],field))/(p[1].x-p[0].x);
        if(index==p.Length-1)return (SectionValue(p[index],field)-SectionValue(p[index-1],field))/(p[index].x-p[index-1].x);
        float a=(SectionValue(p[index],field)-SectionValue(p[index-1],field))/(p[index].x-p[index-1].x);
        float b=(SectionValue(p[index+1],field)-SectionValue(p[index],field))/(p[index+1].x-p[index].x);
        if(a*b<=0)return 0;
        float before=p[index].x-p[index-1].x,after=p[index+1].x-p[index].x;
        float wa=2*after+before,wb=after+2*before;
        return (wa+wb)/(wa/a+wb/b);
    }
    static Section SmoothSection(Section[] p,float x)
    {
        x=Mathf.Clamp(x,p[0].x,p[p.Length-1].x);int i=1;while(i<p.Length-1&&x>p[i].x)i++;
        var a=p[i-1];var b=p[i];float length=b.x-a.x,t=(x-a.x)/length,t2=t*t,t3=t2*t;
        var value=new float[3];
        for(int field=0;field<3;field++)value[field]=(2*t3-3*t2+1)*SectionValue(a,field)+(t3-2*t2+t)*length*SectionSlope(p,i-1,field)+(-2*t3+3*t2)*SectionValue(b,field)+(t3-t2)*length*SectionSlope(p,i,field);
        return new Section(x,value[0],value[1],value[2]);
    }
    static Section HelmetSection(float x)=>SmoothSection(HelmetProfile(),x);
    static float SignedPower(float value,float power)=>Mathf.Sign(value)*Mathf.Pow(Mathf.Abs(value),power);
    static Vector3 HeadPoint(float x,float angle,float lift=0)
    {
        var section=HelmetSection(x);float a=angle*Mathf.Deg2Rad,c=Mathf.Cos(a),sn=Mathf.Sin(a),center=Mathf.Lerp(section.y,section.h,.47f);
        float height=c>=0?section.h-center:center-section.y;
        var p=new Vector3(x,center+SignedPower(c,.86f)*(height+lift),SignedPower(sn,.91f)*(section.w+lift));
        // A smooth closed recess changes depth alone; the approved outer XY profile stays fixed.
        p.z-=Mathf.Sign(p.z)*HeadPocketDepth(new Vector2(p.x,p.y),Mathf.Abs(p.z));return p;
    }
    static float UndentedHeadDepth(Vector2 p)
    {
        var section=HelmetSection(p.x);float center=Mathf.Lerp(section.y,section.h,.47f),height=p.y>=center?section.h-center:center-section.y;
        float q=Mathf.Clamp01(Mathf.Abs(p.y-center)/Mathf.Max(.001f,height));return section.w*Mathf.Pow(Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(q,2/.86f))),.91f);
    }
    static float OpticRadiusSquared(Vector2 p,Vector2 center,Vector2 radius)
    {var q=p-center;return q.x*q.x/(radius.x*radius.x)+q.y*q.y/(radius.y*radius.y);}
    static float HeadPocketDepth(Vector2 p,float skin)
    {
        float r=Mathf.Sqrt(OpticRadiusSquared(p,HeadPixel2(410,183),new Vector2(.31f,.29f)));
        float fade=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((r-1)/.28f));return Mathf.Min(.055f*fade,skin*.65f);
    }
    static float HeadSkinDepth(Vector2 p)
    {float skin=UndentedHeadDepth(p);return skin-HeadPocketDepth(p,skin);}
    static float EyeLensFront(Vector2 p)
    {float q=Mathf.Clamp01(OpticRadiusSquared(p,HeadPixel2(410,183),new Vector2(.31f,.29f)));return .78f*UndentedHeadDepth(p)+.0594f+.025f*(1-q);}
    static float EyeIrisFront(Vector2 p)
    {float q=Mathf.Clamp01(OpticRadiusSquared(p,HeadPixel2(417,182),new Vector2(.115f,.155f)));return EyeLensFront(p)+.008f+.018f*(1-q);}

    static Vector3 SurfaceNormal(Func<float,float,Vector3> surface,float u,float v)
    {
        const float d=.0005f;Vector3 du=surface(Mathf.Clamp01(u+d),v)-surface(Mathf.Clamp01(u-d),v),dv=surface(u,Mathf.Clamp01(v+d))-surface(u,Mathf.Clamp01(v-d));
        // Finite-difference crosses are much smaller than Unity's Vector3
        // normalization cutoff. Normalize explicitly before applying thickness;
        // a zero here would collapse both plate faces onto the same surface.
        Vector3 normal=Vector3.Cross(dv,du);float magnitude=Mathf.Sqrt(normal.sqrMagnitude);
        return magnitude>1e-12f?normal/magnitude:Vector3.up;
    }
    // A closed curved plate: its front/back grids share four perimeter loops
    // with a narrow rolled chamfer. No flat polygon is laid across curved skin.
    static Mesh CurvedPlate(string name,Func<float,float,Vector3> surface,int along,int across,float thickness,float bevel,float insetU=.008f,float insetV=.025f)
    {
        var g=new Geometry();int stride=across+1;var grids=new int[2];var perimeter=new List<Vector2>();
        for(int i=0;i<=along;i++)perimeter.Add(new Vector2(i/(float)along,0));
        for(int j=1;j<=across;j++)perimeter.Add(new Vector2(1,j/(float)across));
        for(int i=along-1;i>=0;i--)perimeter.Add(new Vector2(i/(float)along,1));
        for(int j=across-1;j>0;j--)perimeter.Add(new Vector2(0,j/(float)across));
        for(int face=0;face<2;face++)
        {
            grids[face]=g.v.Count;
            for(int i=0;i<=along;i++)for(int j=0;j<=across;j++)
            {
                float u=Mathf.Lerp(insetU,1-insetU,i/(float)along),v=Mathf.Lerp(insetV,1-insetV,j/(float)across);Vector3 n=SurfaceNormal(surface,u,v);
                g.Add(surface(u,v)-n*(face==0?0:thickness),new Vector2(u,v));
            }
            for(int i=0;i<along;i++)for(int j=0;j<across;j++)
            {int a=grids[face]+i*stride+j;if(face==0)g.Quad(a,a+stride,a+stride+1,a+1);else g.Quad(a,a+1,a+stride+1,a+stride);}
        }
        int count=perimeter.Count;var rings=new int[4][];
        rings[0]=new int[count];rings[3]=new int[count];
        for(int i=0;i<count;i++)
        {
            var p=perimeter[i];int x=Mathf.RoundToInt(p.x*along),y=Mathf.RoundToInt(p.y*across);
            rings[0][i]=grids[0]+x*stride+y;rings[3][i]=grids[1]+x*stride+y;
        }
        for(int ring=1;ring<3;ring++)
        {
            rings[ring]=new int[count];float depth=ring==1?bevel:thickness-bevel;
            for(int i=0;i<count;i++){var p=perimeter[i];rings[ring][i]=g.Add(surface(p.x,p.y)-SurfaceNormal(surface,p.x,p.y)*depth,p);}
        }
        for(int ring=0;ring<3;ring++)for(int i=0;i<count;i++){int j=(i+1)%count;g.Quad(rings[ring][i],rings[ring+1][i],rings[ring+1][j],rings[ring][j]);}
        var mesh=g.Build(name);ReverseWinding(mesh);FiniteNormals(mesh);return mesh;
    }

    static Mesh PlatedFuselage()
    {
        var profile=new Section[41];var source=Hull();
        for(int i=0;i<profile.Length;i++)profile[i]=SmoothSection(source,Mathf.Lerp(source[0].x,source[source.Length-1].x,i/(float)(profile.Length-1)));
        return Loft("sculpted fuselage",profile,40);
    }
    static Mesh KeelPlating()
    {
        var g=new Geometry();foreach(int side in new[]{-1,1}){int ss=side;Append(g,CurvedPlate("source curved ivory shield",(u,v)=>ReferenceBodyBelly(u,ss>0?v:1-v,ss,.020f),24,8,.018f,.004f,.004f,.012f));}
        Append(g,CurvedPlate("continuous lower shield keel",(u,v)=>Vector3.Lerp(ReferenceBodyBelly(u,1,-1,.020f),ReferenceBodyBelly(u,1,1,.020f),v),24,4,.012f,.0025f,.004f,.012f));
        return g.Build("curved ivory chest plating");
    }
    static Mesh KeelGasket(int side)
    {return CurvedPlate("chest panel gasket",(u,v)=>ReferenceBodyBelly(u,Mathf.Lerp(0,.052f,side>0?v:1-v),side,.011f),24,2,.009f,.002f,.003f,.01f);}
    static Mesh BodyPlateGasket(int side,int panel)
    {
        Vector2[] a={new Vector2(239,253),new Vector2(253,249),new Vector2(284,231)};
        Vector2[] b={new Vector2(226,271),new Vector2(245,270),new Vector2(285,256)};
        Vector2[] c={new Vector2(240,294),new Vector2(262,281),new Vector2(312,255)};
        Vector2[] d={new Vector2(262,301),new Vector2(287,281),new Vector2(337,244)};
        return ReferenceBodyClean(CurvedPlate("swept fuselage panel line",(u,v)=>
        {Vector2 p=Cubic2(a[panel],b[panel],c[panel],d[panel],u),q=Cubic2(a[panel],b[panel],c[panel],d[panel],Mathf.Clamp01(u+.002f));Vector2 delta=q-p;float len=Mathf.Sqrt(delta.x*delta.x+delta.y*delta.y);float w=(v-.5f)*.9f;if(len>.0001f)p+=new Vector2(-delta.y/len,delta.x/len)*w;return ReferenceBodySkin(p.x,p.y,side,.012f);},20,2,.005f,.001f,.004f,.02f));
    }
    static Mesh TalonSocket(int side)
    {
        var g=new Geometry();Vector3 legOrigin=new Vector3(-.49f,-.29f,side*.32f);
        Append(g,ReferenceBodyOval("gold talon hinge",ReferenceBodyPixel(268,300,side*.435f)-legOrigin,.18f,.15f,.055f,side,.018f));
        // A buried Z axle joins the body interior to the existing gold actuator.
        // Its .06 radius stays wholly inside the approved oval X/Y silhouette.
        var axle=Loft("concealed gold talon axle",new[]{new Section(0,0,.06f,.06f),new Section(.305f,0,.06f,.06f)},20);
        var vertices=axle.vertices;Vector3 center=ReferenceBodyPixel(268,300);
        for(int i=0;i<vertices.Length;i++){var v=vertices[i];vertices[i]=center+new Vector3(v.y,v.z,side*(.13f+v.x))-legOrigin;}
        axle.vertices=vertices;if(side<0)ReverseWinding(axle);axle.RecalculateBounds();FiniteNormals(axle);Append(g,axle);return g.Build("gold talon hinge");
    }
    static Mesh CollarShell(string name,float from,float to,float lift)
    {return ReferenceBodyCollar(name,name.Contains("blue")?3:0,lift);}
    static Vector2 Cubic2(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float t)
    {float s=1-t;return s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d;}
    static Vector2[] SensorOutline(float scale)
    {
        var p=new Vector2[40];var center=HeadPixel2(410,183);
        for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/p.Length;p[i]=center+new Vector2(Mathf.Cos(a)*.31f*scale,Mathf.Sin(a)*.29f*scale);}return p;
    }
    static Mesh MechanicalHelmet()
    {
        const int along=36,around=36;var g=new Geometry();int stride=around+1;
        for(int i=0;i<=along;i++)for(int j=0;j<=around;j++)
        {float u=i/(float)along,v=j/(float)around;g.Add(HeadPoint(Mathf.Lerp(-.01f,1.25f,u),v*360),new Vector2(u,v));}
        for(int i=0;i<along;i++)for(int j=0;j<around;j++){int a=i*stride+j;g.Quad(a,a+1,a+stride+1,a+stride);}
        foreach(int end in new[]{0,1})
        {
            int row=end==0?0:along*stride;var section=HelmetSection(end==0?-.01f:1.25f);int center=g.Add(new Vector3(section.x,(section.y+section.h)*.5f,0),Vector2.zero);
            for(int j=0;j<around;j++)if(end==0)g.Tri(center,row+j+1,row+j);else g.Tri(center,row+j,row+j+1);
        }
        var mesh=g.Build("helmet compound sections");RemoveUnusedVertices(mesh);WeldSmoothNormals(mesh);return mesh;
    }
    static Mesh SensorHousing(string name,int side,Vector3 origin)
    {
        var g=new Geometry();float[] scales={1.045f,1.045f,.985f,.985f};const int n=40;
        for(int ring=0;ring<4;ring++)foreach(var p in SensorOutline(scales[ring]))
        {
            float depth=ring==0||ring==3?HeadSkinDepth(p)-.008f:EyeLensFront(p)+(ring==1?.004f:.006f);
            g.Add(new Vector3(p.x,p.y,side*depth)-origin,p);
        }
        for(int ring=0;ring<4;ring++)for(int i=0;i<n;i++)
        {int a=ring*n+i,b=ring*n+(i+1)%n,c=((ring+1)%4)*n+(i+1)%n,d=((ring+1)%4)*n+i;if(side>0)g.Quad(a,b,c,d);else g.Quad(a,d,c,b);}
        var mesh=g.Build(name);WeldSmoothNormals(mesh);return mesh;
    }
    static Mesh SensorFrame(string name,int side,Vector3 origin)
    {
        if(name=="gold optical crescent")
        {
            Vector3 Surface(float t,float v)
            {
                if(side>0)v=1-v;
                var outer=Cubic2(HeadPixel2(376,149),HeadPixel2(348,160),HeadPixel2(347,202),HeadPixel2(376,212),t);
                var inner=Cubic2(HeadPixel2(385,156),HeadPixel2(373,169),HeadPixel2(373,190),HeadPixel2(383,202),t);
                var p=Vector2.Lerp(outer,inner,v);return new Vector3(p.x,p.y,side*(HeadSkinDepth(p)+.018f+.008f*Mathf.Sin(t*Mathf.PI)*Mathf.Sin(v*Mathf.PI)));
            }
            return Shift(CurvedPlate(name,Surface,24,4,.030f,.006f,.006f,.025f),-origin);
        }
        // The reference has a pale lower rim rather than a full silver eyeglass ring.
        Vector3 Rim(float t,float v)
        {
            if(side>0)v=1-v;float a=Mathf.Lerp(198,326,t)*Mathf.Deg2Rad,r=Mathf.Lerp(1.012f,1.033f,v);var p=HeadPixel2(410,183)+new Vector2(Mathf.Cos(a)*.31f*r,Mathf.Sin(a)*.29f*r);
            return new Vector3(p.x,p.y,side*(EyeLensFront(p)+.011f+.002f*Mathf.Sin(v*Mathf.PI)));
        }
        return Shift(CurvedPlate(name,Rim,24,3,.014f,.003f,.005f,.025f),-origin);
    }

    static Mesh SensorPanel(string name,int side,Vector3 origin)=>name=="curved black optical lens"?OpticalDisc(name,HeadPixel2(410,183),new Vector2(.31f,.29f),side,origin,.0594f,.025f,.045f,40,6):OpticalDisc(name,HeadPixel2(417,182),new Vector2(.115f,.155f),side,origin,.008f,.018f,.035f,32,5);
    static void ReverseWinding(Mesh mesh)
    {var t=mesh.triangles;for(int i=0;i<t.Length;i+=3){int a=t[i];t[i]=t[i+2];t[i+2]=a;}mesh.triangles=t;}
    static Vector2[] CounterClockwise(Vector2[] p)
    {
        float area=0;for(int i=0;i<p.Length;i++){var b=p[(i+1)%p.Length];area+=p[i].x*b.y-b.x*p[i].y;}
        if(area<0)Array.Reverse(p);return p;
    }

    static Mesh CheekPlates(string name,int side)
    {
        Vector3 Surface(float t,float v)
        {
            if(side>0)v=1-v;
            var upper=Cubic2(HeadPixel2(378,207),HeadPixel2(388,224),HeadPixel2(414,232),HeadPixel2(435,208),t);
            var lower=Cubic2(HeadPixel2(377,216),HeadPixel2(393,237),HeadPixel2(419,230),HeadPixel2(435,215),t);
            var p=Vector2.Lerp(upper,lower,v);return new Vector3(p.x,p.y,side*(.285f+.024f*Mathf.Sin(t*Mathf.PI)+.006f*Mathf.Sin(v*Mathf.PI)));
        }
        return CurvedPlate(name,Surface,24,4,.027f,.006f,.006f,.025f);
    }
    static Mesh ChiselBeak(string name,int kind)
    {
        Section[] profile;
        if(kind==0)profile=new[]{BeakSection(434,205,210.5f,.042f),BeakSection(440,188,209.8f,.095f),BeakSection(444,180,209f,.11f),BeakSection(455,185.7f,207.3f,.085f),BeakSection(474,195.7f,204f,.038f),BeakSection(486,201.9f,202.6f,.004f)};
        else if(kind==1)profile=new[]{BeakSection(434,210.2f,211.1f,.046f),BeakSection(445,208.4f,209.2f,.089f),BeakSection(472,203.8f,204.5f,.031f),BeakSection(485.4f,201.8f,202.4f,.003f)};
        else profile=new[]{BeakSection(437,211.1f,212.8f,.038f),BeakSection(446,209.6f,211.6f,.074f),BeakSection(471,205f,206.1f,.027f),BeakSection(484,202.7f,203.1f,.003f)};
        const int along=22,around=12;var g=new Geometry();int stride=around+1;
        Vector3 origin=kind==0?new Vector3(.66f,.22f,0):kind==1?new Vector3(.66f,.20f,0):new Vector3(.66f,.185f,0);
        Vector3 Local(Vector3 p){p-=origin;return kind==2?new Vector3(p.x/.89f,p.y/.28f,p.z/.84f):p;}
        for(int i=0;i<=along;i++)for(int j=0;j<=around;j++)
        {
            var section=SmoothSection(profile,Mathf.Lerp(profile[0].x,profile[profile.Length-1].x,i/(float)along));float a=j*Mathf.PI*2/around;
            g.Add(Local(new Vector3(section.x,section.y+SignedPower(Mathf.Cos(a),.65f)*section.h,SignedPower(Mathf.Sin(a),.65f)*section.w)),new Vector2(i/(float)along,j/(float)around));
        }
        for(int i=0;i<along;i++)for(int j=0;j<around;j++){int a=i*stride+j;g.Quad(a,a+1,a+stride+1,a+stride);}
        foreach(int end in new[]{0,1})
        {
            int row=end==0?0:along*stride;var section=profile[end==0?0:profile.Length-1];int center=g.Add(Local(new Vector3(section.x,section.y,0)),Vector2.zero);
            for(int j=0;j<around;j++)if(end==0)g.Tri(center,row+j+1,row+j);else g.Tri(center,row+j,row+j+1);
        }
        var mesh=g.Build(name);WeldSmoothNormals(mesh);return mesh;
    }
    static void RemoveUnusedVertices(Mesh mesh)
    {
        var vertices=mesh.vertices;var uv=mesh.uv;var colours=mesh.colors;var triangles=mesh.triangles;
        var map=new int[vertices.Length];for(int i=0;i<map.Length;i++)map[i]=-1;
        var used=new List<Vector3>();var tex=new List<Vector2>();var tint=new List<Color>();
        for(int i=0;i<triangles.Length;i++)
        {
            int old=triangles[i];if(map[old]<0){map[old]=used.Count;used.Add(vertices[old]);tex.Add(uv[old]);tint.Add(colours.Length==vertices.Length?colours[old]:Color.white);}triangles[i]=map[old];
        }
        mesh.triangles=Array.Empty<int>();mesh.vertices=used.ToArray();mesh.uv=tex.ToArray();mesh.colors=tint.ToArray();mesh.triangles=triangles;mesh.RecalculateBounds();FiniteNormals(mesh);
    }
    static void WeldSmoothNormals(Mesh mesh)
    {
        // Periodic seams and lens centers need the same area-weighted normal
        // on both copies; independent plate lips keep their own rolled normals.
        var vertices=mesh.vertices;var normals=mesh.normals;var groups=new Dictionary<Vector3Int,List<int>>();
        for(int i=0;i<vertices.Length;i++)
        {
            var v=vertices[i];var key=new Vector3Int(Mathf.RoundToInt(v.x*100000),Mathf.RoundToInt(v.y*100000),Mathf.RoundToInt(v.z*100000));
            if(!groups.TryGetValue(key,out var indices)){indices=new List<int>();groups.Add(key,indices);}indices.Add(i);
        }
        foreach(var indices in groups.Values)
        {Vector3 normal=Vector3.zero;foreach(int i in indices)normal+=normals[i];normal=Unit(normal,Unit(vertices[indices[0]]-mesh.bounds.center,Vector3.up));foreach(int i in indices)normals[i]=normal;}
        mesh.normals=normals;
    }
    static Mesh Shift(Mesh mesh,Vector3 offset)
    {var v=mesh.vertices;for(int i=0;i<v.Length;i++)v[i]+=offset;mesh.vertices=v;mesh.RecalculateBounds();return mesh;}
    static void Append(Geometry g,Mesh mesh)
    {
        int first=g.v.Count;var v=mesh.vertices;var uv=mesh.uv;var colours=mesh.colors;var t=mesh.triangles;
        for(int i=0;i<v.Length;i++)g.Add(v[i],uv[i],colours.Length==v.Length?colours[i]:Color.white);for(int i=0;i<t.Length;i+=3)g.Tri(first+t[i],first+t[i+1],first+t[i+2]);UnityEngine.Object.DestroyImmediate(mesh);
    }
    static void BuildWing(Transform body,int side,out Transform root,out Transform tip)
    {
        root=Bone(body,(side<0?"03 Left":"04 Right")+" wing shoulder hinge",new Vector3(-.33f,.22f,side*.39f));
        tip=Bone(root,"Wingtip feather follow",new Vector3(-.64f,.16f,side*.27f));
        Vector2[] roots={new Vector2(292,189),new Vector2(298,207),new Vector2(299,218),new Vector2(300,225),new Vector2(300,229),new Vector2(301,230),new Vector2(303,229),new Vector2(301,214),new Vector2(303,222)};
        Vector2[] bpx={new Vector2(253,169),new Vector2(246,194),new Vector2(245,209),new Vector2(259,220),new Vector2(272,230),new Vector2(282,235),new Vector2(292,235),new Vector2(285,206),new Vector2(286,215)};
        Vector2[] cpx={new Vector2(180,165),new Vector2(132,204),new Vector2(162,224),new Vector2(199,242),new Vector2(229,247),new Vector2(254,246),new Vector2(277,241),new Vector2(261,206),new Vector2(271,219)};
        Vector2[] ends={new Vector2(105,164),new Vector2(69,189),new Vector2(111,226),new Vector2(160,241),new Vector2(203,244),new Vector2(236,245),new Vector2(265,239),new Vector2(246,207),new Vector2(257,224)};
        float[] widths={.14f,.14f,.12f,.10f,.080f,.065f,.055f,.060f,.055f};
        for(int i=0;i<9;i++)
        {
            float z=side*(.20f+i*.013f);Vector3 a=ReferenceBodyPixel(roots[i].x,roots[i].y,root.localPosition.z+z)-root.localPosition,b=ReferenceBodyPixel(bpx[i].x,bpx[i].y,root.localPosition.z+z+side*.025f)-root.localPosition,c=ReferenceBodyPixel(cpx[i].x,cpx[i].y,root.localPosition.z+z+side*.07f)-root.localPosition,end=ReferenceBodyPixel(ends[i].x,ends[i].y,root.localPosition.z+z+side*.11f)-root.localPosition,off=tip.localPosition;
            Part(tip,"Primary "+(i+1).ToString("00")+" cobalt edge",Feather("curved primary chassis "+side+" "+i,a-off,b-off,c-off,end-off,widths[i],.024f,12,8),navy);
            Vector3 ea=Bezier(a,b,c,end,.38f),eb=Bezier(a,b,c,end,.57f),ec=Bezier(a,b,c,end,.83f),ed=Vector3.Lerp(c,end,.985f),outward=new Vector3(0,0,side*.012f);
            Part(tip,"Primary "+(i+1).ToString("00")+" cyan enamel",Feather("curved primary enamel "+side+" "+i,ea-off+outward,eb-off+outward,ec-off+outward,ed-off+outward,widths[i]*.83f,.025f,11,8),(i%3==1)?blue:cyan);
        }
        Vector2[] ends2={new Vector2(176,162),new Vector2(141,188),new Vector2(173,211),new Vector2(215,225),new Vector2(250,232)};
        for(int i=0;i<5;i++)
        {
            Vector3 a=ReferenceBodyPixel(293+i*2,194+i*7,side*(.69f+i*.012f))-root.localPosition,end=ReferenceBodyPixel(ends2[i].x,ends2[i].y,side*(.78f+i*.012f))-root.localPosition,b=Vector3.Lerp(a,end,.30f)+new Vector3(0,.045f,0),c=Vector3.Lerp(a,end,.73f)+new Vector3(0,.025f,0);
            float width=.095f-i*.007f;
            Part(root,"Secondary "+(i+1)+" cobalt edge",Feather("secondary metal feather "+side+" "+i,a,b,c,end,width,.023f,11,8),navy);
            Vector3 o=new Vector3(0,0,side*.012f);Part(root,"Secondary "+(i+1)+" cobalt enamel",Feather("secondary cobalt panel "+side+" "+i,a+o,b+o,c+o,Vector3.Lerp(c,end,.96f)+o,width*.72f,.025f,10,8),i%2==0?blue:cyan);
        }
        Vector3 shoulder=ReferenceBodyPixel(268,205,side*.75f)-root.localPosition;
        Part(root,"Shoulder saddle dark rim",ReferenceShoulderMount(shoulder,root.localPosition,side),navy);
        Part(root,"Gold swept shoulder hub",ReferenceBodyOval("gold shoulder hub plate",shoulder+new Vector3(0,0,side*.055f),.37f,.17f,.09f,side,.028f),gold);
        Vector3 hub=new Vector3(-.14f,.09f,side*.267f),bearing=ReferenceBodyPixel(263,208,side*.87f)-root.localPosition;
        Part(root,"Shoulder cyan bearing bezel",ReferenceBodyEllipseRing("shoulder bearing",bearing-hub,.145f,.103f,.70f,.032f,side),navy,hub);
        Vector3 capOrigin=hub+new Vector3(0,0,side*.017f);
        Part(root,"Shoulder cyan glass bearing",Shift(ReferenceBodyDome("cyan shoulder bearing cap",.105f,.070f,side*.024f,32,6),bearing-capOrigin+new Vector3(0,0,side*.020f)),glow,capOrigin);
        Vector3 actuatorOrigin=new Vector3(.10f,.11f,side*.39f),actuator=ReferenceBodyPixel(318,223,side*.37f);
        Part(body,"Rear gold shoulder actuator "+side,ReferenceBodyEllipseRing("rear shoulder actuator ring",actuator-actuatorOrigin,.165f,.210f,.58f,.048f,side),gold,actuatorOrigin);
        Vector3 pivotOrigin=new Vector3(.10f,.11f,side*.438f);
        Part(body,"Rear cyan shoulder pivot "+side,Shift(ReferenceBodyDome("rear pivot cyan",.092f,.130f,side*.025f,28,6),actuator-pivotOrigin+new Vector3(0,0,side*.024f)),cyan,pivotOrigin);
    }
    static void BuildTail(Transform root,Transform tip)
    {
        Vector3 origin=new Vector3(-.89f,-.035f,0);
        Section S(float px,float py,float h,float w){var p=ReferenceBodyPixel(px,py)-origin;return new Section(p.x,p.y,h,w);}
        Part(root,"Tail graphite swivel",ReferenceBodyLoft("tail graphite swivel",new[]{S(242,276,.11f,.14f),S(225,280,.16f,.18f),S(207,284,.14f,.16f)},32),navy);
        for(int i=0;i<2;i++)Part(root,"Tail cobalt band "+i,ReferenceBodyLoft("tail blue machined band",i==0?new[]{S(242,276,.12f,.16f),S(240,276.6f,.13f,.17f),S(233,278,.15f,.18f),S(231.5f,278.5f,.14f,.17f),S(229,279,.16f,.19f),S(225,280,.168f,.188f)}:new[]{S(223,281,.17f,.189f),S(206,285,.158f,.18f)},32),blue);
        Vector2[] a2={new Vector2(219,276),new Vector2(216,295),new Vector2(215,285),new Vector2(214,283),new Vector2(216,291)};
        Vector2[] e2={new Vector2(112,252),new Vector2(134,341),new Vector2(175,306),new Vector2(155,259),new Vector2(170,322)};
        for(int i=0;i<5;i++)
        {
            float z=(i-2)*.105f;Vector3 a=ReferenceBodyPixel(a2[i].x,a2[i].y,z)-origin,end=ReferenceBodyPixel(e2[i].x,e2[i].y,z*1.5f)-origin,b=Vector3.Lerp(a,end,.30f)+new Vector3(0,i==1?-.025f:.045f,0),c=Vector3.Lerp(a,end,.76f)+new Vector3(0,i==1?-.045f:.025f,0),off=tip.localPosition;float width=i<2?.105f:.063f;
            Part(tip,"Tail flight blade "+(i+1),Feather("articulated tail blade "+i,a-off,b-off,c-off,end-off,width,.024f,12,8),navy);
            Vector3 o=new Vector3(0,0,-.012f);Part(tip,"Tail cobalt enamel "+(i+1),Feather("tail enamel "+i,a-off+o,b-off+o,c-off+o,Vector3.Lerp(c,end,.97f)-off+o,width*.79f,.025f,11,8),i==2?cyan:blue);
        }
        Part(root,"Gold tail power collar",ReferenceBodyLoft("gold tail power sleeve",new[]{S(210,282,.10f,.15f),S(203,284,.12f,.16f),S(173,289,.115f,.135f),S(168,289,.086f,.11f)},36),gold);
        var power=new Geometry();Append(power,ReferenceBodyLoft("tail power inset",new[]{S(170,289,.083f,.105f),S(166,290,.079f,.10f)},32));
        Part(root,"Cyan tail power inset",power.Build("tail power inset"),glow);
    }
    static Mesh Loft(string name,Section[] s,int sides,float from=0,float to=360)
    {
        var g=new Geometry();int stride=sides+1;
        for(int j=0;j<s.Length;j++)for(int i=0;i<=sides;i++)
        {
            float a=Mathf.Lerp(from,to,i/(float)sides)*Mathf.Deg2Rad;
            g.Add(new Vector3(s[j].x,s[j].y+Mathf.Cos(a)*s[j].h,Mathf.Sin(a)*s[j].w),new Vector2(j/(float)(s.Length-1),i/(float)sides));
        }
        for(int j=0;j<s.Length-1;j++)for(int i=0;i<sides;i++)
        { int a=j*stride+i;if(s[s.Length-1].x>=s[0].x)g.Quad(a,a+1,a+stride+1,a+stride);else g.Quad(a,a+stride,a+stride+1,a+1); }
        if(Mathf.Abs(to-from)>359)
        {
            int left=g.Add(new Vector3(s[0].x,s[0].y,0),Vector2.zero),right=g.Add(new Vector3(s[s.Length-1].x,s[s.Length-1].y,0),Vector2.one);
            for(int i=0;i<sides;i++){g.Tri(left,i+1,i);int e=(s.Length-1)*stride+i;g.Tri(right,e,e+1);}
        }
        return g.Build(name);
    }
    static Vector3 Bezier(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float t)
    { float s=1-t;return s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d; }
    static Vector3 Unit(Vector3 value,Vector3 fallback)
    {float m=Mathf.Sqrt(value.sqrMagnitude);return m>1e-15f?value/m:fallback;}
    // Explicit native-pixel traces of the source wing. These are closed curved
    // volumes with bowed faces, machined bevels and inset enamel on both sides.
    // No renderer, material, transform, bone or part order is changed.
    static Vector2[] ReferenceWingCurve(float[] p,int samples)
    {
        var result=new List<Vector2>();Vector2 a=new Vector2(p[0],p[1]);
        for(int i=2;i<p.Length;i+=6)
        {
            Vector2 b=new Vector2(p[i],p[i+1]),c=new Vector2(p[i+2],p[i+3]),d=new Vector2(p[i+4],p[i+5]);
            for(int j=0;j<samples;j++)result.Add(Cubic2(a,b,c,d,j/(float)samples));a=d;
        }
        return result.ToArray();
    }
    static Vector2[] ReferenceWingContour(int index,bool enamel)
    {
        float[] p;
        if(!enamel)switch(index)
        {
            case 0:p=new float[]{104,163,121,155,169,153,204,157,247,161,277,166,289,184,269,181,241,181,209,182,181,181,161,183,145,181,128,178,113,169,104,163};break;
            case 1:p=new float[]{69,189,102,184,169,184,236,184,265,184,287,190,298,203,269,207,230,202,208,204,176,213,141,213,112,208,91,204,77,198,69,189};break;
            case 2:p=new float[]{111,226,143,210,184,206,239,205,261,205,281,211,299,220,268,222,232,220,205,225,174,236,136,238,111,226};break;
            case 3:p=new float[]{160,240,177,228,214,219,249,216,271,215,290,219,300,226,270,228,251,234,232,237,202,247,174,246,160,240};break;
            case 4:p=new float[]{203,244,221,232,250,226,277,224,288,223,299,225,303,229,285,232,270,237,253,241,233,248,214,247,203,244};break;
            case 5:p=new float[]{236,245,249,235,269,229,289,226,296,225,302,227,305,230,291,234,281,239,266,242,253,246,242,247,236,245};break;
            default:p=new float[]{265,239,276,230,287,226,297,226,303,226,307,228,308,230,298,232,289,236,281,238,274,241,268,241,265,239};break;
        }
        else switch(index)
        {
            case 0:p=new float[]{108,164,125,159,148,158,172,160,171,167,183,175,201,180,179,181,159,182,146,179,130,175,115,169,108,164};break;
            case 1:p=new float[]{74,190,93,187,116,187,135,186,131,193,154,204,185,205,163,211,136,211,113,207,94,203,80,197,74,190};break;
            case 2:p=new float[]{115,225,130,217,151,214,171,212,167,218,177,223,195,226,166,233,138,234,115,225};break;
            case 3:p=new float[]{164,239,181,230,198,226,217,223,212,229,220,231,233,233,211,243,184,245,164,239};break;
            case 4:p=new float[]{207,243,220,235,234,232,246,229,243,235,251,237,263,237,244,245,223,246,207,243};break;
            case 5:p=new float[]{240,243,250,237,262,234,273,232,271,237,277,238,284,237,270,243,254,246,240,243};break;
            default:p=new float[]{267,237,275,232,283,230,292,228,291,232,295,232,299,232,289,236,278,239,267,237};break;
        }
        return ReferenceWingCurve(p,index<3?8:6);
    }
    static float ReferenceWingCross(Vector2 a,Vector2 b,Vector2 c)
    {return (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);}
    static float ReferenceWingArea(Vector2[] p)
    {float area=0;for(int i=0;i<p.Length;i++){var a=p[i];var b=p[(i+1)%p.Length];area+=a.x*b.y-b.x*a.y;}return area*.5f;}
    static List<int> ReferenceWingTriangles(Vector2[] p)
    {
        var order=new List<int>();for(int i=0;i<p.Length;i++)order.Add(i);if(ReferenceWingArea(p)<0)order.Reverse();
        var result=new List<int>();int safety=p.Length*p.Length;
        while(order.Count>3&&safety-->0)
        {
            bool found=false;
            for(int j=0;j<order.Count;j++)
            {
                int a=order[(j+order.Count-1)%order.Count],b=order[j],c=order[(j+1)%order.Count];
                if(ReferenceWingCross(p[a],p[b],p[c])<.00001f)continue;bool contains=false;
                foreach(int k in order)if(k!=a&&k!=b&&k!=c&&ReferenceWingCross(p[a],p[b],p[k])>=-.00001f&&ReferenceWingCross(p[b],p[c],p[k])>=-.00001f&&ReferenceWingCross(p[c],p[a],p[k])>=-.00001f){contains=true;break;}
                if(contains)continue;result.Add(a);result.Add(b);result.Add(c);order.RemoveAt(j);found=true;break;
            }
            if(!found)throw new InvalidOperationException("Source wing contour cannot be triangulated.");
        }
        if(order.Count!=3)throw new InvalidOperationException("Source wing contour triangulation failed.");
        result.AddRange(order);return result;
    }
    static Vector2[] ReferenceWingInset(Vector2[] p,float amount)
    {
        var r=new Vector2[p.Length];float sign=ReferenceWingArea(p)>0?1:-1;
        for(int i=0;i<p.Length;i++)
        {
            Vector2 prev=p[(i+p.Length-1)%p.Length],here=p[i],next=p[(i+1)%p.Length],a=here-prev,b=next-here;
            float al=Mathf.Sqrt(a.x*a.x+a.y*a.y),bl=Mathf.Sqrt(b.x*b.x+b.y*b.y);
            Vector2 na=new Vector2(-a.y,a.x)*(sign/Mathf.Max(.00001f,al)),nb=new Vector2(-b.y,b.x)*(sign/Mathf.Max(.00001f,bl)),n=na+nb;float len=Mathf.Sqrt(n.x*n.x+n.y*n.y);n=n/Mathf.Max(.00001f,len);
            float dot=n.x*na.x+n.y*na.y;r[i]=here+n*(amount/Mathf.Max(.45f,dot));
        }
        return r;
    }
    static void ReferenceWingQuad(Geometry g,int a,int b,int c,int d,Vector3 outward)
    {
        Vector3 n=Vector3.Cross(g.v[b]-g.v[a],g.v[c]-g.v[a]);
        if(n.x*outward.x+n.y*outward.y+n.z*outward.z>=0)g.Quad(a,b,c,d);else g.Quad(a,d,c,b);
    }
    // Z-only span: both sides of each closed blade and all inset panels share
    // the same native-X warp. Existing X/Y buffers and every rig origin survive.
    static Mesh ReferenceWingTipRib(float rootZ,int side)
    {
        // A rib between adjacent primary blades shares their existing tip bone.
        // It is buried behind the approved feathers/coverts in source projection.
        Vector3 start=ReferenceBodyPixel(264,177)-new Vector3(-.97f,.38f,0),end=ReferenceBodyPixel(264,193)-new Vector3(-.97f,.38f,0);
        start.z=rootZ+side*(.71f*(28f/188));end.z=rootZ+side*(.013f+.635f*(34f/229));
        Vector3 axis=Unit(end-start,Vector3.up),radial=Vector3.Cross(axis,Vector3.right);float length=Mathf.Sqrt((end-start).sqrMagnitude);
        var rib=Loft("buried primary root rib",new[]{new Section(0,0,.018f,.018f),new Section(length,0,.018f,.018f)},20);
        var vertices=rib.vertices;for(int i=0;i<vertices.Length;i++){var v=vertices[i];vertices[i]=start+axis*v.x+Vector3.right*v.y+radial*v.z;}
        rib.vertices=vertices;rib.RecalculateBounds();FiniteNormals(rib);return rib;
    }
    static float ReferenceWingSpanZ(int index,int side,float u,float amount,float divisor)
    {u=Mathf.Clamp01(u);return side*amount*(1-index/divisor)*u;}
    static void ReferenceWingSpanMesh(Mesh mesh,string name)
    {
        bool primary=name.StartsWith("curved primary chassis ")||name.StartsWith("curved primary enamel ");
        bool secondary=name.StartsWith("secondary metal feather ")||name.StartsWith("secondary cobalt panel ");
        if(!primary&&!secondary)return;var fields=name.Split(' ');
        if(fields.Length!=5||!int.TryParse(fields[3],out int side)||!int.TryParse(fields[4],out int index))return;
        float rootX,tipX,parentX,amount,divisor;
        if(primary){if(index<7)return;rootX=index==7?301:303;tipX=index==7?246:257;parentX=-.97f;amount=.60f;divisor=8;}
        else{float[] tips={176,141,173,215,250};rootX=293+index*2;tipX=tips[index];parentX=-.33f;amount=.40f;divisor=6;}
        var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)
        {float px=(vertices[i].x+parentX)*100+285,u=(rootX-px)/(rootX-tipX);vertices[i].z+=ReferenceWingSpanZ(index,side,u,amount,divisor);}
        mesh.vertices=vertices;mesh.RecalculateBounds();
    }
    static Mesh ReferenceWingSolid(string name,Vector2[] boundary,int index,int skeletonSide,int faceSide,float rootZ,float halfDepth,float lift,float bow,bool chassis,Color tint)
    {
        if(ReferenceWingArea(boundary)<0)Array.Reverse(boundary);
        var inner=ReferenceWingInset(boundary,chassis?.85f:.30f);var triangles=ReferenceWingTriangles(inner);var g=new Geometry();int count=boundary.Length;
        float minX=boundary[0].x,maxX=minX,minY=boundary[0].y,maxY=minY;
        foreach(var p in boundary){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);}
        float cx=(minX+maxX)*.5f,cy=(minY+maxY)*.5f,rx=Mathf.Max(1,(maxX-minX)*.5f),ry=Mathf.Max(1,(maxY-minY)*.5f);
        float rootX=index==0?292:index==1?298:index<5?300:index==5?301:303,tipX=index==0?104:index==1?69:index==2?111:index==3?160:index==4?203:index==5?236:265;
        Vector3 Point(Vector2 p,float depth,bool rounded)
        {
            if(!chassis&&depth<0)depth-=.015f;float u=Mathf.Clamp01((rootX-p.x)/(rootX-tipX));float nx=(p.x-cx)/rx,ny=(p.y-cy)/ry,q=Mathf.Clamp01(1-nx*nx-ny*ny);
            float z=rootZ+skeletonSide*.11f*u+ReferenceWingSpanZ(index,skeletonSide,u,.60f,8)+faceSide*(lift+depth+(rounded?bow*q*Mathf.Sign(depth):0));
            return new Vector3((p.x-285)/100f+.97f,(255-p.y)/100f-.38f,z);
        }
        Vector2 UV(Vector2 p)=>new Vector2((p.x-minX)/Mathf.Max(1,maxX-minX),(p.y-minY)/Mathf.Max(1,maxY-minY));
        Color faceColour=tint;faceColour.a=chassis?0:1;Color edgeColour=tint;edgeColour.a=1;
        for(int face=0;face<2;face++)
        {
            float depth=face==0?halfDepth:-halfDepth;int start=g.v.Count;
            for(int i=0;i<count;i++)g.Add(Point(inner[i],depth,true),UV(inner[i]),faceColour);
            for(int i=0;i<triangles.Count;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];Vector2 p=(inner[a]+inner[b]+inner[c])/3;
                int mid=g.Add(Point(p,depth,true),UV(p),faceColour);bool normalNegative=(face==0?faceSide:-faceSide)<0;
                if(normalNegative){g.Tri(start+a,start+b,mid);g.Tri(start+b,start+c,mid);g.Tri(start+c,start+a,mid);}
                else{g.Tri(start+a,mid,start+b);g.Tri(start+b,mid,start+c);g.Tri(start+c,mid,start+a);}
            }
        }
        int rings=g.v.Count;
        for(int ring=0;ring<4;ring++)for(int i=0;i<count;i++)
        {bool inset=ring==0||ring==3;float depth=ring==0?halfDepth:ring==1?halfDepth*.54f:ring==2?-halfDepth*.54f:-halfDepth;Vector2 p=inset?inner[i]:boundary[i];g.Add(Point(p,depth,inset),UV(p),edgeColour);}
        for(int ring=0;ring<3;ring++)for(int i=0;i<count;i++)
        {
            int j=(i+1)%count,a=rings+ring*count+i,b=rings+ring*count+j,c=rings+(ring+1)*count+j,d=rings+(ring+1)*count+i;
            Vector2 edge=boundary[j]-boundary[i];Vector3 outward=new Vector3(edge.y,edge.x,ring==0?faceSide*.5f:ring==2?-faceSide*.5f:0);ReferenceWingQuad(g,a,b,c,d,outward);
        }
        return g.Build(name);
    }
    static Mesh ReferenceWingFeather(string name,Vector3 a,float thickness)
    {
        bool chassis=name.StartsWith("curved primary chassis "),enamel=name.StartsWith("curved primary enamel ");
        if((!chassis&&!enamel)||name.EndsWith(" inner"))return null;string[] fields=name.Split(' ');
        if(fields.Length!=5||!int.TryParse(fields[3],out int side)||!int.TryParse(fields[4],out int index)||index<0||index>6)return null;
        // Enamel's original authoring point is Bezier(.38)+normal*.012;
        // remove that offset to recover the shared load-bearing blade centre.
        float rootZ=chassis?a.z:a.z-side*.0477922f;Vector2[] boundary=ReferenceWingContour(index,enamel);
        if(chassis){var blade=ReferenceWingSolid(name,boundary,index,side,side,rootZ,thickness,0,.004f,true,Color.white);if(index!=0)return blade;var assembly=new Geometry();Append(assembly,blade);Append(assembly,ReferenceWingTipRib(rootZ,side));return assembly.Build(name);}
        Color tint=index%3==1?new Color(1,6,1,1):Color.white;var g=new Geometry();
        Append(g,ReferenceWingSolid(name+" outer",boundary,index,side,side,rootZ,.008f,.045f,0,false,tint));
        Append(g,ReferenceWingSolid(name+" inner",boundary,index,side,-side,rootZ,.008f,.045f,0,false,tint));return g.Build(name);
    }
    static Mesh Feather(string name,Vector3 a,Vector3 b,Vector3 c,Vector3 d,float width,float thickness,int length,int around)
    {
        var sourceWing=ReferenceWingFeather(name,a,thickness);if(sourceWing!=null)return sourceWing;
        // Source has broad swept rounded blades; closed narrow bevels preserve volume.
        var section=new[]{new Vector2(-1,0),new Vector2(-.96f,.52f),new Vector2(-.10f,1),new Vector2(.96f,.52f),new Vector2(1,0),new Vector2(.96f,-.52f),new Vector2(-.10f,-.72f),new Vector2(-.96f,-.52f)};
        var g=new Geometry();length=8;const int faces=8;
        bool blade=name.StartsWith("curved primary chassis")||name.StartsWith("secondary metal feather")||name.StartsWith("articulated tail blade");
        Vector3 Point(float u,int edge)
        {
            Vector3 tangent=Unit(3*(1-u)*(1-u)*(b-a)+6*(1-u)*u*(c-b)+3*u*u*(d-c),Vector3.left),cross=Unit(Vector3.Cross(tangent,Vector3.forward),Vector3.up),normal=Unit(Vector3.Cross(tangent,cross),Vector3.back);
            float chord=u<.20f?Mathf.Lerp(.22f,.90f,u/.20f):u<.47f?Mathf.Lerp(.90f,1,(u-.20f)/.27f):Mathf.Lerp(1,.012f,Mathf.Pow((u-.47f)/.53f,1.28f));
            float thick=thickness*Mathf.Lerp(.62f,1,Mathf.Sin(u*Mathf.PI))*(u>.90f?Mathf.Lerp(1,.06f,(u-.90f)/.10f):1);Vector2 q=section[edge%faces];
            return Bezier(a,b,c,d,u)+cross*(q.x*width*chord)+normal*(q.y*thick);
        }
        for(int face=0;face<faces;face++)
        {
            int first=g.v.Count;Color colour=blade&&(face==1||face==2||face==5||face==6)?new Color(1,1,1,0):Color.white;
            for(int j=0;j<=length;j++){float u=j/(float)length;g.Add(Point(u,face),new Vector2(u,face/(float)faces),colour);g.Add(Point(u,face+1),new Vector2(u,(face+1)/(float)faces),colour);}
            for(int j=0;j<length;j++){int k=first+j*2;g.Quad(k,k+2,k+3,k+1);}
        }
        foreach(int end in new[]{0,1})
        {
            float u=end;int center=g.Add(Bezier(a,b,c,d,u),new Vector2(u,.5f)),first=g.v.Count;
            for(int face=0;face<faces;face++)g.Add(Point(u,face),new Vector2(u,face/(float)faces));
            for(int face=0;face<faces;face++){int next=first+(face+1)%faces;if(end==0)g.Tri(center,first+face,next);else g.Tri(center,next,first+face);}
        }
        if(!name.EndsWith(" inner"))
        {
            float side=name.Contains(" -1 ")?-1:1;Vector3 offset=Vector3.zero;
            if(name.StartsWith("curved primary enamel"))offset=new Vector3(0,0,side*.012f);
            else if(name.StartsWith("secondary cobalt panel"))offset=new Vector3(0,0,side*.012f);
            else if(name.StartsWith("tail enamel"))offset=new Vector3(0,0,-.012f);
            if(offset.sqrMagnitude>0)Append(g,Feather(name+" inner",a-offset*2,b-offset*2,c-offset*2,d-offset*2,width,thickness,length,around));
        }
        var mesh=g.Build(name);if(!name.EndsWith(" inner"))ReferenceWingSpanMesh(mesh,name);FiniteNormals(mesh);return mesh;
    }
    static void FiniteNormals(Mesh mesh)
    {
        var vertices=mesh.vertices;var triangles=mesh.triangles;var normals=new Vector3[vertices.Length];
        for(int i=0;i<triangles.Length;i+=3)
        {int a=triangles[i],b=triangles[i+1],c=triangles[i+2];Vector3 n=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);normals[a]+=n;normals[b]+=n;normals[c]+=n;}
        for(int i=0;i<normals.Length;i++)normals[i]=Unit(normals[i],Unit(vertices[i]-mesh.bounds.center,Vector3.up));
        mesh.normals=normals;
    }
    static Mesh Ring(string name,float outer,float inner,float depth,int slices,int bevel,float start=0,float end=360)
    {
        var g=new Geometry();float mid=(outer+inner)*.5f,r=(outer-inner)*.5f;
        for(int i=0;i<=slices;i++)for(int j=0;j<=bevel;j++)
        {
            float a=Mathf.Lerp(start,end,i/(float)slices)*Mathf.Deg2Rad,b=j*Mathf.PI*2/bevel;
            float rad=mid+r*Mathf.Cos(b);g.Add(new Vector3(Mathf.Cos(a)*rad,Mathf.Sin(a)*rad,Mathf.Sin(b)*depth*.5f),new Vector2(i/(float)slices,j/(float)bevel));
        }
        int stride=bevel+1;for(int i=0;i<slices;i++)for(int j=0;j<bevel;j++){int k=i*stride+j;g.Quad(k,k+stride,k+stride+1,k+1);}
        return g.Build(name);
    }
    static Mesh Dome(string name,float rx,float ry,float height,int slices,int rings)
    {
        var g=new Geometry();
        for(int j=0;j<=rings;j++)for(int i=0;i<=slices;i++)
        {
            float rad=j/(float)rings,a=i*Mathf.PI*2/slices;
            g.Add(new Vector3(Mathf.Cos(a)*rad*rx,Mathf.Sin(a)*rad*ry,height*Mathf.Sqrt(Mathf.Max(0,1-rad*rad))),new Vector2(.5f+Mathf.Cos(a)*rad*.5f,.5f+Mathf.Sin(a)*rad*.5f));
        }
        int stride=slices+1;for(int j=0;j<rings;j++)for(int i=0;i<slices;i++)
        {int k=j*stride+i;if(height>0)g.Quad(k,k+stride,k+stride+1,k+1);else g.Quad(k,k+1,k+stride+1,k+stride);}
        return g.Build(name);
    }
    static void CreateClips()
    {
        string path=Folder+"/Animations/Flight flap and feather follow.anim";if(AssetDatabase.LoadAssetAtPath<AnimationClip>(path)!=null)return;
        var clip=new AnimationClip{name="Flight flap and feather follow",legacy=false,frameRate=60};
        var curve=new AnimationCurve();for(int i=0;i<=48;i++){float t=i*1.65f/48;curve.AddKey(new Keyframe(t,Mathf.Sin(i*Mathf.PI*2/48)*28));}
        string b="01 Body and flight bank/";
        clip.SetCurve(b+"03 Left wing shoulder hinge",typeof(Transform),"localEulerAnglesRaw.x",curve);
        var right=new AnimationCurve();for(int i=0;i<=48;i++){float t=i*1.65f/48;right.AddKey(new Keyframe(t,-Mathf.Sin(i*Mathf.PI*2/48)*28));}
        clip.SetCurve(b+"04 Right wing shoulder hinge",typeof(Transform),"localEulerAnglesRaw.x",right);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
        AssetDatabase.CreateAsset(clip,path);
        string tailPath=Folder+"/Animations/Tail pitch and delayed tip.anim";
        if(AssetDatabase.LoadAssetAtPath<AnimationClip>(tailPath)==null)
        {
            var tailClip=new AnimationClip{name="Tail pitch and delayed tip",frameRate=60};
            foreach(string joint in new[]{"05 Tail pitch and yaw","05 Tail pitch and yaw/06 Tail tip follow"})
            {
                var c=new AnimationCurve();for(int i=0;i<=48;i++)c.AddKey(i*1.65f/48,Mathf.Sin(i*Mathf.PI*2/48-(joint.Contains("/")?.9f:.6f))*5f);
                tailClip.SetCurve(b+joint,typeof(Transform),"localEulerAnglesRaw.z",c);
            }
            settings=AnimationUtility.GetAnimationClipSettings(tailClip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(tailClip,settings);AssetDatabase.CreateAsset(tailClip,tailPath);
        }
    }
    static void CreateStudio()
    {
        if(File.Exists(ScenePath))return;
        var previous=SceneManager.GetActiveScene();if(string.IsNullOrEmpty(previous.path))throw new InvalidOperationException("Save the active scene before creating the bird studio. Existing model assets are preserved; run again after saving.");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            var rig=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
            var camera=new GameObject("Art review camera · editable framing",typeof(Camera));camera.transform.position=new Vector3(3.5f,1.9f,-7.4f);camera.transform.LookAt(new Vector3(-.45f,.15f,0));
            var c=camera.GetComponent<Camera>();c.fieldOfView=31;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.026f,.043f,.069f);c.nearClipPlane=.05f;c.farClipPlane=60;c.allowHDR=true;c.tag="MainCamera";
            AddLight("Key · broad cool studio",new Vector3(-1,4,-4),new Color(.78f,.87f,1),1.35f);
            AddLight("Fill · soft cyan bounce",new Vector3(3,1,3),new Color(.42f,.76f,1),.85f);
            AddLight("Rim · warm metal edge",new Vector3(-3,3,4),new Color(1,.77f,.44f),1.8f);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.25f,.32f,.44f);RenderSettings.ambientEquatorColor=new Color(.12f,.16f,.23f);RenderSettings.ambientGroundColor=new Color(.045f,.061f,.08f);RenderSettings.ambientIntensity=1;
            // Only the reference stand uses the original image; bird geometry is solid 3D.
            var reference=Resources.Load<Texture2D>("SkyPulse/characters/roster/volt-frame-04-v1");
            if(reference!=null)
            {
                string path=Folder+"/Materials/10 Original artwork reference only.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(Shader.Find("Unlit/Texture")){name="Original artwork · reference card only",mainTexture=reference};AssetDatabase.CreateAsset(mat,path);}
                var card=new GameObject("REFERENCE ONLY · source image (not part of bird)",typeof(MeshFilter),typeof(MeshRenderer));card.transform.position=new Vector3(2.4f,.32f,1.8f);card.transform.rotation=Quaternion.Euler(0,-25,0);
                var g=new Geometry();g.Add(new Vector3(-.90f,-.675f,0),new Vector2(0,0));g.Add(new Vector3(-.90f,.675f,0),new Vector2(0,1));g.Add(new Vector3(.90f,.675f,0),new Vector2(1,1));g.Add(new Vector3(.90f,-.675f,0),new Vector2(1,0));g.Quad(0,1,2,3);
                card.GetComponent<MeshFilter>().sharedMesh=Save(g.Build("reference card only"));card.GetComponent<MeshRenderer>().sharedMaterial=mat;
            }
            var note=new GameObject("README · FIRST ART REVIEW CANDIDATE · source silhouette study; no game integration");note.transform.position=new Vector3(0,-1,0);
            EditorSceneManager.SaveScene(scene,ScenePath);
        }
        finally{EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
    }
    static void AddLight(string name,Vector3 position,Color colour,float intensity)
    {
        var g=new GameObject(name,typeof(Light));g.transform.position=position;g.transform.LookAt(new Vector3(-.35f,.2f,0));var l=g.GetComponent<Light>();l.type=LightType.Directional;l.color=colour;l.intensity=intensity;l.shadows=LightShadows.Soft;l.shadowStrength=.65f;
    }
}
