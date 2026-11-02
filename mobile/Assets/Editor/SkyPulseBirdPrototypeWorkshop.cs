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
    public const string OpticsRevision = "Contoured mechanical finch v4";
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
        public readonly List<int> t = new List<int>();
        public int Add(Vector3 p, Vector2 tex) { int i=v.Count;v.Add(p);uv.Add(tex);return i; }
        public void Tri(int a,int b,int c) { t.Add(a);t.Add(b);t.Add(c); }
        public void Quad(int a,int b,int c,int d) { Tri(a,b,c);Tri(a,c,d); }
        public Mesh Build(string name)
        {
            var m=new Mesh {name=name};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
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
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before revising the Finch head.");
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)==null)throw new InvalidOperationException("Create the Finch art-review rig first.");
        // Explicit sculpture revision: existing body/head buffers 001–042 and
        // eight review materials. Prefab, rig, wing/tail buffers and clips retain
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
            // the sculpture. Wing/tail builders are deliberately not called.
            refreshHead=true;
            BuildBody(temporary.transform);
            BuildHead(Bone(temporary.transform,"Temporary head",Vector3.zero));
        }
        finally{refreshHead=false;revisionBuild=false;UnityEngine.Object.DestroyImmediate(temporary);}
        ApplyCurrentFinishes();
        SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();
        Debug.Log("SKYPULSE_FINCH_HEAD_REVISED: "+revisedMeshCount+" existing head meshes updated in place; all asset GUIDs, wing/tail geometry, scene, prefab and animation retained; only the declared review materials refreshed.");
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
        Color colour;float metal,smooth,kind,seam;
        switch(finish)
        {
            case 1:colour=new Color(.055f,.19f,.40f);metal=.42f;smooth=.59f;kind=0;seam=.15f;break;
            case 2:colour=new Color(.035f,.50f,.65f);metal=.38f;smooth=.62f;kind=0;seam=.10f;break;
            case 3:colour=new Color(.018f,.03f,.055f);metal=.35f;smooth=.40f;kind=2;seam=0;break;
            case 4:colour=new Color(.62f,.70f,.78f);metal=.30f;smooth=.60f;kind=0;seam=.08f;break;
            case 5:colour=new Color(.65f,.45f,.16f);metal=.85f;smooth=.68f;kind=1;seam=.04f;break;
            case 6:colour=new Color(.005f,.014f,.025f);metal=.18f;smooth=.91f;kind=3;seam=0;break;
            case 7:colour=new Color(.48f,.57f,.65f);metal=.92f;smooth=.76f;kind=1;seam=.08f;break;
            case 8:colour=new Color(.01f,.50f,.58f);metal=.18f;smooth=.76f;kind=4;seam=0;break;
            default:throw new ArgumentOutOfRangeException(nameof(finish));
        }
        bool changed=false;
        if(material.shader!=shader){material.shader=shader;changed=true;}
        void Float(string key,float value){if(material.GetFloat(key)!=value){material.SetFloat(key,value);changed=true;}}
        void Colour(string key,Color value){if(!material.GetColor(key).Equals(value)){material.SetColor(key,value);changed=true;}}
        Colour("_Color",colour);Colour("_EmissionColor",finish==8?new Color(0,.85f,1.15f):Color.black);
        Float("_Metallic",metal);Float("_Glossiness",smooth);Float("_FinishKind",kind);
        Float("_DetailScale",160);Float("_SeamStrength",seam);Float("_EmissionStrength",finish==8?.65f:0);
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
        if(an.Length!=bn.Length||au.Length!=bu.Length||at.Length!=bt.Length)return false;
        for(int i=0;i<av.Length;i++)if(!av[i].Equals(bv[i]))return false;
        for(int i=0;i<an.Length;i++)if(!an[i].Equals(bn[i]))return false;
        for(int i=0;i<au.Length;i++)if(!au[i].Equals(bu[i]))return false;
        for(int i=0;i<at.Length;i++)if(at[i]!=bt[i])return false;
        return true;
    }
    static GameObject Part(Transform parent,string name,Mesh m,Material material,Vector3 position=default)
    {
        var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.transform.localPosition=position;
        g.GetComponent<MeshFilter>().sharedMesh=Save(m);var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;r.receiveShadows=true;return g;
    }
    // The load-bearing shape is a shallow, tapered keel. Armour sits above it
    // as separate closed shells, so panel boundaries are physical stepped lips.
    static Section[] Hull()=>new[]{new Section(-1.06f,-.035f,.035f,.055f),new Section(-.96f,-.025f,.105f,.135f),new Section(-.80f,-.005f,.205f,.225f),new Section(-.59f,.02f,.285f,.305f),new Section(-.34f,.06f,.335f,.355f),new Section(-.10f,.115f,.335f,.376f),new Section(.12f,.19f,.285f,.374f),new Section(.30f,.27f,.215f,.29f),new Section(.45f,.325f,.125f,.185f),new Section(.55f,.355f,.042f,.07f),new Section(.59f,.36f,.008f,.012f)};
    static void BuildBody(Transform body)
    {
        Part(body,"Tapered cobalt fuselage",PlatedFuselage(),blue);
        Part(body,"Ivory chest shield",KeelPlating(),ivory);
        foreach(int side in new[]{-1,1})
        {
            Part(body,"Chest shield gasket "+side,KeelGasket(side),navy);
            for(int p=0;p<3;p++)
                Part(body,"Swept back armor seam "+side+" "+p,BodyPlateGasket(side,p),navy);
            var leg=Bone(body,"Folded landing talon "+side,new Vector3(-.49f,-.29f,side*.32f));
            Part(leg,"Gold leg actuator",TalonSocket(side),gold);
            Part(leg,"Talon shank",Feather("folded cobalt shank",new Vector3(0,.035f,-side*.115f),new Vector3(-.12f,-.025f,-side*.08f),new Vector3(-.21f,-.12f,side*.005f),new Vector3(-.23f,-.16f,side*.015f),.041f,.022f,12,10),blue);
            Part(leg,"Talon toe one",Feather("pointed titanium toe",new Vector3(-.19f,-.10f,side*.018f),new Vector3(-.25f,-.15f,side*.033f),new Vector3(-.34f,-.17f,side*.044f),new Vector3(-.40f,-.16f,side*.047f),.026f,.017f,10,8),navy);
            Part(leg,"Talon toe two",Feather("second pointed toe",new Vector3(-.18f,-.13f,side*.02f),new Vector3(-.20f,-.21f,side*.052f),new Vector3(-.28f,-.25f,side*.065f),new Vector3(-.34f,-.24f,side*.071f),.024f,.016f,10,8),navy);
        }
        Part(body,"Neck articulated dark collar",CollarShell("neck flexible gasket",.235f,.38f,0),navy);
        Part(body,"Neck collar cobalt bevel",CollarShell("neck blue rim",.325f,.425f,.013f),blue);
    }
    static void BuildHead(Transform head)
    {
        Part(head,"Sculpted cobalt helmet",MechanicalHelmet(),blue);
        Part(head,"Long swept crown blade",Feather("rear crown blade",new Vector3(.18f,.50f,0),new Vector3(.035f,.582f,0),new Vector3(-.22f,.62f,0),new Vector3(-.43f,.585f,0),.052f,.023f,22,12),navy);
        Part(head,"Cobalt crown inlay",Feather("crown enamel",new Vector3(.15f,.519f,-.004f),new Vector3(.02f,.590f,-.004f),new Vector3(-.21f,.618f,-.004f),new Vector3(-.405f,.590f,-.004f),.038f,.017f,20,10),blue);
        Part(head,"Forward swept crown blade",Feather("short crown blade",new Vector3(.31f,.483f,.012f),new Vector3(.17f,.558f,.012f),new Vector3(-.04f,.603f,.012f),new Vector3(-.23f,.586f,.012f),.031f,.014f,20,10),gold);
        Part(head,"Crown cyan vent",Feather("crest cyan center",new Vector3(.275f,.511f,-.016f),new Vector3(.14f,.563f,-.016f),new Vector3(-.04f,.596f,-.016f),new Vector3(-.20f,.590f,-.016f),.017f,.009f,18,8),blue);
        foreach(int side in new[]{-1,1})
        {
            // These origins are the original prefab values. The sculpture is
            // authored around them inside its existing mesh asset buffers.
            Vector3 optic=new Vector3(.40f,.35f,side*.414f);
            Part(head,"Optic recessed gasket "+side,SensorHousing("optical black recessed surround",side,optic),navy,optic);
            Part(head,"Optic titanium bezel "+side,SensorFrame("optic outer beveled lip",side,optic+new Vector3(0,0,side*.026f),1.105f,1.055f,-.004f,0,40),silver,optic+new Vector3(0,0,side*.026f));
            Part(head,"Champagne gold brow arc "+side,SensorFrame("gold optical crescent",side,optic+new Vector3(0,0,side*.042f),1.23f,1.17f,.007f,0,20),gold,optic+new Vector3(0,0,side*.042f));
            Part(head,"Dark curved optical glass "+side,SensorPanel("curved black optical lens",SensorOutline(1),side,optic+new Vector3(0,0,side*.049f),.004f,-.027f),dark,optic+new Vector3(0,0,side*.049f));
            Part(head,"Forward cyan optical chevron "+side,SensorPanel("inset optical direction mark",new[]{new Vector2(.285f,.354f),new Vector2(.33f,.377f),new Vector2(.448f,.359f),new Vector2(.472f,.341f),new Vector2(.389f,.338f),new Vector2(.33f,.345f)},side,optic+new Vector3(.025f,.015f,side*.113f),.0018f,-.021f),glow,optic+new Vector3(.025f,.015f,side*.113f));
            Part(head,"Ivory jaw rim "+side,CheekPlates("jaw machined trim",side),ivory);
            Part(head,"Brow fastener "+side,Shift(ArmourPanel("brow screw",new[]{new Vector2(-.075f,.235f),new Vector2(.025f,.215f),new Vector2(.031f,.224f),new Vector2(-.068f,.245f)},side,.0015f,.003f),-new Vector3(.11f,.67f,side*.35f)),silver,new Vector3(.11f,.67f,side*.35f));
        }
        Part(head,"Gold upper beak",ChiselBeak("gold beveled finch beak",0),gold,new Vector3(.66f,.22f,0));
        Part(head,"Dark lower beak seam",ChiselBeak("beak split gasket",1),navy,new Vector3(.66f,.20f,0));
        Part(head,"Beak lower gold plate",ChiselBeak("lower beak",2),gold,new Vector3(.66f,.185f,0)).transform.localScale=new Vector3(.89f,.28f,.84f);
    }
    // Shape-preserving cubic interpolation gives a continuous silhouette with
    // no roof corners, while the independently curated sections avoid a sphere.
    static Section[] HelmetProfile()=>new[]{new Section(-.31f,.18f,.31f,.025f),new Section(-.25f,.085f,.39f,.14f),new Section(-.15f,.025f,.46f,.225f),new Section(-.02f,.015f,.50f,.267f),new Section(.12f,.028f,.515f,.283f),new Section(.27f,.05f,.495f,.274f),new Section(.42f,.09f,.455f,.246f),new Section(.55f,.14f,.385f,.194f),new Section(.66f,.205f,.315f,.06f),new Section(.69f,.235f,.29f,.009f)};
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
    static Vector3 BodyPoint(float x,float angle,float lift=0)
    {
        var s=SmoothSection(Hull(),x);float a=angle*Mathf.Deg2Rad;
        // A soft chine keeps the flank flatter than an egg, with a narrow keel.
        float c=Mathf.Cos(a),sn=Mathf.Sin(a);float vertical=SignedPower(c,c>=0?.88f:1.10f);
        return new Vector3(x,s.y+vertical*(s.h+lift),SignedPower(sn,.88f)*(s.w+lift));
    }
    static Vector3 HeadPoint(float x,float angle,float lift=0)
    {
        var s=HelmetSection(x);float a=angle*Mathf.Deg2Rad,c=Mathf.Cos(a),sn=Mathf.Sin(a),center=Mathf.Lerp(s.y,s.h,.47f);
        float height=c>=0?s.h-center:center-s.y;
        return new Vector3(x,center+SignedPower(c,.86f)*(height+lift),SignedPower(sn,.91f)*(s.w+lift));
    }
    static float HeadSkinDepth(float x,float y)
    {
        var s=HelmetSection(x);float center=Mathf.Lerp(s.y,s.h,.47f),height=y>=center?s.h-center:center-s.y;
        float q=Mathf.Clamp01(Mathf.Abs(y-center)/Mathf.Max(.001f,height));
        return s.w*Mathf.Pow(Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(q,2/.86f))),.91f);
    }
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
        var mesh=g.Build(name);ReverseWinding(mesh);mesh.RecalculateNormals();return mesh;
    }
    static float PlateLeft(int panel)=>panel==0?-.99f:panel==1?-.72f:-.41f;
    static float PlateRight(int panel)=>panel==0?-.37f:panel==1?-.015f:.405f;
    static Vector3 BodyPlatePoint(int side,int panel,float u,float v,float lift=0)
    {
        float sweep=.145f*Mathf.Pow(1-v,1.4f)-.035f*v;
        float x=Mathf.Lerp(PlateLeft(panel),PlateRight(panel),u)+sweep;
        return BodyPoint(x,side*Mathf.Lerp(54,126,v),.006f+panel*.007f+lift);
    }
    static Mesh PlatedFuselage()
    {
        var g=new Geometry();
        Append(g,CurvedPlate("dorsal shell",(u,v)=>BodyPoint(Mathf.Lerp(-1.04f,.555f,u),Mathf.Lerp(-65,65,v),.004f),32,16,.015f,.003f));
        foreach(int side in new[]{-1,1})for(int panel=0;panel<3;panel++)
        {
            int p=panel,s=side;Func<float,float,Vector3> surface=side>0?(Func<float,float,Vector3>)((u,v)=>BodyPlatePoint(s,p,u,v)):((u,v)=>BodyPlatePoint(s,p,u,1-v));
            Append(g,CurvedPlate("overlapping cobalt armour",surface,18,10,.017f,.0035f,.009f,.022f));
        }
        return g.Build("sculpted fuselage");
    }
    static Mesh KeelPlating()
    {
        var g=new Geometry();float[] ends={-.95f,-.48f,-.035f,.455f};
        for(int panel=0;panel<3;panel++)
        {
            int p=panel;Append(g,CurvedPlate("keel panel",(u,v)=>BodyPoint(Mathf.Lerp(ends[p]+.006f,ends[p+1]-.006f,u)+.023f*Mathf.Sin(v*Mathf.PI),Mathf.Lerp(127,233,v),.010f+p*.002f),16,14,.014f,.003f));
        }
        return g.Build("curved ivory chest plating");
    }
    static Mesh KeelGasket(int side)
    {
        return CurvedPlate("chest panel gasket",(u,v)=>BodyPoint(Mathf.Lerp(-.98f,.48f,u),side*Mathf.Lerp(side>0?124:128,side>0?128:124,v),.003f),32,2,.008f,.0015f,.003f,.08f);
    }
    static Mesh BodyPlateGasket(int side,int panel)
    {
        int p=panel,s=side;
        // The strip lies behind the open trailing edge of each cobalt shell.
        return CurvedPlate("swept fuselage panel line",(u,v)=>BodyPlatePoint(s,p,Mathf.Lerp(0,.032f,v),side>0?1-u:u,-.007f),18,2,.008f,.0015f,.005f,.08f);
    }
    static Mesh TalonSocket(int side)
    {
        var g=new Geometry();const int slices=32;float cy=.037f,cz=-side*.115f;
        for(int ring=0;ring<4;ring++)for(int i=0;i<slices;i++)
        {
            float a=i*Mathf.PI*2/slices,r=ring==0?.046f:ring==1?.052f:ring==2?.052f:.043f;
            float z=cz+side*(ring==0?-.006f:ring==1?-.002f:ring==2?.009f:.012f);
            g.Add(new Vector3(Mathf.Cos(a)*r,cy+Mathf.Sin(a)*r*.72f,z),new Vector2(i/(float)slices,ring/3f));
        }
        for(int ring=0;ring<3;ring++)for(int i=0;i<slices;i++)
        {int j=(i+1)%slices,a=ring*slices+i,b=ring*slices+j;if(side>0)g.Quad(a,b,b+slices,a+slices);else g.Quad(a,a+slices,b+slices,b);}
        int cap=g.Add(new Vector3(0,cy,cz+side*.012f),new Vector2(.5f,.5f));
        for(int i=0;i<slices;i++)if(side>0)g.Tri(cap,3*slices+i,3*slices+(i+1)%slices);else g.Tri(cap,3*slices+(i+1)%slices,3*slices+i);
        return g.Build("gold talon hinge");
    }
    static Mesh CollarShell(string name,float from,float to,float lift)
    {
        return CurvedPlate(name,(u,v)=>BodyPoint(Mathf.Lerp(from,to,u),Mathf.Lerp(-179.8f,179.8f,v),lift),6,40,.016f,.003f,.025f,.002f);
    }
    static Vector2 Cubic2(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float t)
    {float s=1-t;return s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d;}
    static Vector2[] SensorOutline(float scale)
    {
        var p=new Vector2[40];var center=new Vector2(.315f,.36f);
        for(int i=0;i<20;i++)p[i]=Cubic2(new Vector2(.10f,.356f),new Vector2(.17f,.436f),new Vector2(.37f,.432f),new Vector2(.53f,.34f),i/20f);
        for(int i=0;i<20;i++)p[i+20]=Cubic2(new Vector2(.53f,.34f),new Vector2(.40f,.293f),new Vector2(.20f,.292f),new Vector2(.10f,.356f),i/20f);
        for(int i=0;i<p.Length;i++)p[i]=center+(p[i]-center)*scale;return p;
    }
    static bool Inside(Vector2 p,Vector2[] polygon)
    {
        bool inside=false;for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
            if((polygon[i].y>p.y)!=(polygon[j].y>p.y)&&p.x<(polygon[j].x-polygon[i].x)*(p.y-polygon[i].y)/(polygon[j].y-polygon[i].y)+polygon[i].x)inside=!inside;
        return inside;
    }
    static Mesh MechanicalHelmet()
    {
        const int along=72,around=72;var g=new Geometry();var aperture=SensorOutline(1.15f);int stride=around+1;
        for(int i=0;i<=along;i++)for(int j=0;j<=around;j++)
        {float u=i/(float)along,v=j/(float)around;g.Add(HeadPoint(Mathf.Lerp(-.31f,.69f,u),v*360),new Vector2(u,v));}
        for(int i=0;i<along;i++)for(int j=0;j<around;j++)
        {
            int a=i*stride+j;var center=(g.v[a]+g.v[a+1]+g.v[a+stride]+g.v[a+stride+1])*.25f;
            if(Inside(new Vector2(center.x,center.y),aperture))continue;
            g.Quad(a,a+1,a+stride+1,a+stride);
        }
        foreach(int end in new[]{0,1})
        {
            int row=end==0?0:along*stride;var s=HelmetSection(end==0?-.31f:.69f);int center=g.Add(new Vector3(s.x,(s.y+s.h)*.5f,0),Vector2.zero);
            for(int j=0;j<around;j++)if(end==0)g.Tri(center,row+j+1,row+j);else g.Tri(center,row+j,row+j+1);
        }
        // Two swept crown laminations sit on the compound shell. They remain
        // thin, with rounded lips, rather than introducing helmet roof planes.
        Append(g,CurvedPlate("rear crown armour",(u,v)=>HeadPoint(Mathf.Lerp(-.28f,.12f,u)+.022f*Mathf.Cos(v*Mathf.PI),Mathf.Lerp(-57,57,v),.004f),18,12,.011f,.0025f));
        Append(g,CurvedPlate("fore crown armour",(u,v)=>HeadPoint(Mathf.Lerp(-.055f,.49f,u)+.035f*Mathf.Cos(v*Mathf.PI),Mathf.Lerp(-48,48,v),.009f),22,12,.011f,.0025f));
        var mesh=g.Build("helmet compound sections");RemoveUnusedVertices(mesh);WeldSmoothNormals(mesh);return mesh;
    }
    static Vector3 SensorVertex(Vector2 p,int side,float lift)=>new Vector3(p.x,p.y,side*(HeadSkinDepth(p.x,p.y)+lift));
    static Mesh SensorHousing(string name,int side,Vector3 origin)
    {
        var g=new Geometry();float[] scales={1.28f,1.19f,1.055f,1.005f},depths={.002f,.008f,-.016f,-.028f};int n=40;
        for(int ring=0;ring<4;ring++)foreach(var p in SensorOutline(scales[ring]))g.Add(SensorVertex(p,side,depths[ring])-origin,p);
        for(int ring=0;ring<3;ring++)for(int i=0;i<n;i++)
        {int a=ring*n+i,b=ring*n+(i+1)%n;if(side>0)g.Quad(a,a+n,b+n,b);else g.Quad(a,b,b+n,a+n);}
        return g.Build(name);
    }
    static Mesh SensorFrame(string name,int side,Vector3 origin,float outside,float inside,float lift,int firstEdge,int edges)
    {
        var outer=SensorOutline(outside);var inner=SensorOutline(inside);var g=new Geometry();int n=outer.Length;
        for(int ring=0;ring<4;ring++)for(int i=0;i<=edges;i++)
        {
            int k=(firstEdge+i)%n;var p=Vector2.Lerp(inner[k],outer[k],ring==0?.12f:ring==1?0:ring==2?1:.88f);
            float depth=lift+(ring==0||ring==3?0:.0025f);
            g.Add(SensorVertex(p,side,depth)-origin,p);
        }
        int stride=edges+1;for(int ring=0;ring<3;ring++)for(int i=0;i<edges;i++)
        {int a=ring*stride+i;if(side>0)g.Quad(a,a+1,a+stride+1,a+stride);else g.Quad(a,a+stride,a+stride+1,a+1);}
        var m=g.Build(name);WeldSmoothNormals(m);return m;
    }
    static Mesh SurfacePolygon(string name,Vector2[] outline,int side,float thickness,float lift)
    {
        var p=CounterClockwise(outline);var g=new Geometry();Vector2 center=Vector2.zero;foreach(var a in p)center+=a;center/=p.Length;
        const int rings=5;int n=p.Length;
        for(int ring=0;ring<=rings;ring++)for(int i=0;i<n;i++)
        {
            float r=ring/(float)rings;var a=Vector2.Lerp(center,p[i],r);
            float bevel=ring==rings?-.0015f:0;g.Add(SensorVertex(a,side,lift+bevel),a);
        }
        for(int ring=0;ring<rings;ring++)for(int i=0;i<n;i++)
        {int a=ring*n+i,b=ring*n+(i+1)%n;if(side>0)g.Quad(a,b,b+n,a+n);else g.Quad(a,a+n,b+n,b);}
        int back=g.v.Count;for(int i=0;i<n;i++)g.Add(SensorVertex(p[i],side,lift-thickness),p[i]);
        for(int i=0;i<n;i++){int a=rings*n+i,b=rings*n+(i+1)%n,c=back+(i+1)%n,d=back+i;if(side>0)g.Quad(a,b,c,d);else g.Quad(a,d,c,b);}
        var mesh=g.Build(name);ReverseWinding(mesh);mesh.RecalculateNormals();WeldSmoothNormals(mesh);return mesh;
    }
    static Mesh SensorPanel(string name,Vector2[] outline,int side,Vector3 origin,float thickness,float lift)=>Shift(SurfacePolygon(name,outline,side,thickness,lift),-origin);
    static void ReverseWinding(Mesh mesh)
    {var t=mesh.triangles;for(int i=0;i<t.Length;i+=3){int a=t[i];t[i]=t[i+2];t[i+2]=a;}mesh.triangles=t;}
    static Vector2[] CounterClockwise(Vector2[] p)
    {
        float area=0;for(int i=0;i<p.Length;i++){var b=p[(i+1)%p.Length];area+=p[i].x*b.y-b.x*p[i].y;}
        if(area<0)Array.Reverse(p);return p;
    }
    static Mesh ArmourPanel(string name,Vector2[] outline,int side,float lift,float thickness)=>SurfacePolygon(name,outline,side,thickness,lift);
    static Mesh CheekPlates(string name,int side)
    {
        var g=new Geometry();float[] cuts={-.13f,.175f,.585f};
        for(int p=0;p<2;p++)
        {
            int panel=p,s=side;
            Append(g,CurvedPlate(name,(u,v)=>HeadPoint(Mathf.Lerp(cuts[panel]+.006f,cuts[panel+1]-.006f,u),s*Mathf.Lerp(s>0?122:139,s>0?139:122,v),.005f),16,4,.011f,.0025f,.012f,.06f));
        }
        return g.Build(name);
    }
    static Mesh ChiselBeak(string name,int kind)
    {
        Section[] p;
        if(kind==0)p=new[]{new Section(.615f,.272f,.047f,.068f),new Section(.70f,.275f,.044f,.081f),new Section(.80f,.261f,.030f,.061f),new Section(.90f,.245f,.012f,.031f),new Section(.975f,.234f,.0008f,.001f)};
        else if(kind==1)p=new[]{new Section(.635f,.230f,.0022f,.064f),new Section(.72f,.231f,.0023f,.076f),new Section(.83f,.232f,.0018f,.052f),new Section(.963f,.233f,.0005f,.001f)};
        else p=new[]{new Section(.632f,.215f,.015f,.058f),new Section(.72f,.215f,.014f,.068f),new Section(.82f,.222f,.009f,.045f),new Section(.945f,.230f,.0006f,.001f)};
        for(int i=0;i<p.Length;i++){var section=p[i];section.x=.615f+(section.x-.615f)*1.16f;p[i]=section;}
        const int along=22,around=16;var g=new Geometry();int stride=around+1;
        Vector3 origin=kind==0?new Vector3(.66f,.22f,0):kind==1?new Vector3(.66f,.20f,0):new Vector3(.66f,.185f,0);
        for(int i=0;i<=along;i++)for(int j=0;j<=around;j++)
        {
            var s=SmoothSection(p,Mathf.Lerp(p[0].x,p[p.Length-1].x,i/(float)along));float a=j*Mathf.PI*2/around;
            var vertex=new Vector3(s.x,s.y+SignedPower(Mathf.Cos(a),.72f)*s.h,SignedPower(Mathf.Sin(a),.72f)*s.w)-origin;
            if(kind==2)vertex=new Vector3(vertex.x/.89f,vertex.y/.28f,vertex.z/.84f);g.Add(vertex,new Vector2(i/(float)along,j/(float)around));
        }
        for(int i=0;i<along;i++)for(int j=0;j<around;j++){int a=i*stride+j;g.Quad(a,a+1,a+stride+1,a+stride);}
        foreach(int end in new[]{0,1})
        {
            int row=end==0?0:along*stride;var s=p[end==0?0:p.Length-1];var v=new Vector3(s.x,s.y,0)-origin;if(kind==2)v=new Vector3(v.x/.89f,v.y/.28f,v.z/.84f);int center=g.Add(v,Vector2.zero);
            for(int j=0;j<around;j++)if(end==0)g.Tri(center,row+j+1,row+j);else g.Tri(center,row+j,row+j+1);
        }
        var m=g.Build(name);WeldSmoothNormals(m);return m;
    }
    static void RemoveUnusedVertices(Mesh mesh)
    {
        var vertices=mesh.vertices;var uv=mesh.uv;var triangles=mesh.triangles;
        var map=new int[vertices.Length];for(int i=0;i<map.Length;i++)map[i]=-1;
        var used=new List<Vector3>();var tex=new List<Vector2>();
        for(int i=0;i<triangles.Length;i++)
        {
            int old=triangles[i];if(map[old]<0){map[old]=used.Count;used.Add(vertices[old]);tex.Add(uv[old]);}triangles[i]=map[old];
        }
        mesh.triangles=Array.Empty<int>();mesh.vertices=used.ToArray();mesh.uv=tex.ToArray();mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
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
        {Vector3 normal=Vector3.zero;foreach(int i in indices)normal+=normals[i];normal.Normalize();foreach(int i in indices)normals[i]=normal;}
        mesh.normals=normals;
    }
    static Mesh Shift(Mesh mesh,Vector3 offset)
    {var v=mesh.vertices;for(int i=0;i<v.Length;i++)v[i]+=offset;mesh.vertices=v;mesh.RecalculateBounds();return mesh;}
    static void Append(Geometry g,Mesh mesh)
    {
        int first=g.v.Count;var v=mesh.vertices;var uv=mesh.uv;var t=mesh.triangles;
        for(int i=0;i<v.Length;i++)g.Add(v[i],uv[i]);for(int i=0;i<t.Length;i+=3)g.Tri(first+t[i],first+t[i+1],first+t[i+2]);UnityEngine.Object.DestroyImmediate(mesh);
    }
    static void BuildWing(Transform body,int side,out Transform root,out Transform tip)
    {
        root=Bone(body,(side<0?"03 Left":"04 Right")+" wing shoulder hinge",new Vector3(-.33f,.22f,side*.39f));
        tip=Bone(root,"Wingtip feather follow",new Vector3(-.64f,.16f,side*.27f));
        // Primary flight blades are separately selectable and have a closed airfoil.
        for(int i=0;i<9;i++)
        {
            float u=i/8f;
            Vector3 a=new Vector3(-.10f-u*.10f,.045f-u*.11f,side*(.03f+u*.045f));
            Vector3 end=new Vector3(-1.75f+u*.70f,.43f-u*.66f,side*(.55f+u*.35f));
            Vector3 b=Vector3.Lerp(a,end,.28f)+new Vector3(-.03f,.17f,side*.05f);
            Vector3 c=Vector3.Lerp(a,end,.73f)+new Vector3(-.10f,.10f,side*.12f);
            float width=.145f-u*.022f;
            Vector3 off=tip.localPosition;
            Part(tip,"Primary "+(i+1).ToString("00")+" cobalt edge",Feather("curved primary chassis "+side+" "+i,a-off,b-off,c-off,end-off,width,.038f,17,10),navy);
            Part(tip,"Primary "+(i+1).ToString("00")+" cyan enamel",Feather("curved primary enamel "+side+" "+i,a-off+new Vector3(0,0,side*.016f),b-off+new Vector3(0,0,side*.016f),c-off+new Vector3(0,0,side*.016f),Vector3.Lerp(c,end,.94f)-off+new Vector3(0,0,side*.016f),width*.86f,.033f,15,8),(i%3==1)?blue:cyan);
        }
        // Shorter overlapping secondaries give a real layered finch silhouette.
        for(int i=0;i<5;i++)
        {
            float u=i/4f;Vector3 a=new Vector3(.02f-u*.13f,.075f-u*.08f,side*(.035f+u*.025f));
            Vector3 end=new Vector3(-1.05f+u*.35f,.30f-u*.58f,side*(.36f+u*.27f));
            Vector3 b=Vector3.Lerp(a,end,.3f)+new Vector3(-.06f,.13f,side*.03f),c=Vector3.Lerp(a,end,.75f)+new Vector3(-.03f,.09f,side*.06f);
            Part(root,"Secondary "+(i+1)+" cobalt edge",Feather("secondary metal feather "+side+" "+i,a,b,c,end,.125f,.043f,16,10),navy);
            Part(root,"Secondary "+(i+1)+" cobalt enamel",Feather("secondary cobalt panel "+side+" "+i,a+new Vector3(0,0,side*.026f),b+new Vector3(0,0,side*.026f),c+new Vector3(0,0,side*.026f),Vector3.Lerp(c,end,.9f)+new Vector3(0,0,side*.026f),.105f,.032f,14,8),i%2==0?blue:cyan);
        }
        // Cobalt wing saddle is a curved plated blade, not a flattened primitive.
        Part(root,"Shoulder saddle dark rim",Feather("wing shoulder saddle rim",new Vector3(.16f,.04f,side*.035f),new Vector3(-.1f,.17f,side*.12f),new Vector3(-.37f,.13f,side*.12f),new Vector3(-.64f,.065f,side*.11f),.19f,.095f,17,12),navy);
        Part(root,"Gold swept shoulder hub",Feather("gold shoulder hub plate",new Vector3(.12f,.045f,side*.09f),new Vector3(-.12f,.12f,side*.20f),new Vector3(-.38f,.10f,side*.20f),new Vector3(-.56f,.065f,side*.14f),.165f,.076f,17,12),gold);
        Vector3 hub=new Vector3(-.14f,.09f,side*.267f);
        Part(root,"Shoulder cyan bearing bezel",Ring("shoulder bearing",.103f,.068f,.035f,40,6),navy,hub);
        Part(root,"Shoulder cyan glass bearing",Dome("cyan shoulder bearing cap",.069f,.059f,side*.027f,36,7),glow,hub+new Vector3(0,0,side*.017f));
        Part(body,"Rear gold shoulder actuator "+side,Ring("rear shoulder actuator ring",.115f,.070f,.05f,32,6),gold,new Vector3(.10f,.11f,side*.39f));
        Part(body,"Rear cyan shoulder pivot "+side,Dome("rear pivot cyan",.065f,.074f,side*.025f,28,6),cyan,new Vector3(.10f,.11f,side*.438f));
    }
    static void BuildTail(Transform root,Transform tip)
    {
        Part(root,"Tail graphite swivel",Loft("tail graphite swivel",new[]{new Section(.04f,0,.15f,.16f),new Section(-.13f,-.01f,.17f,.18f),new Section(-.27f,-.02f,.14f,.15f)},40),navy);
        for(int i=0;i<2;i++)Part(root,"Tail cobalt band "+i,Loft("tail blue machined band",new[]{new Section(-.06f-i*.14f,-.01f,.172f,.183f),new Section(-.095f-i*.14f,-.01f,.175f,.184f)},40),blue);
        for(int i=0;i<5;i++)
        {
            float spread=(i-2)*.16f;Vector3 a=new Vector3(-.18f,-.02f,spread*.25f),end=new Vector3(-1.12f-Mathf.Abs(i-2)*.04f,-.08f+(i%2)*.12f,spread*1.8f);
            Vector3 b=Vector3.Lerp(a,end,.32f)+new Vector3(0,.10f,0),c=Vector3.Lerp(a,end,.78f)+new Vector3(-.03f,-.03f,spread*.35f),off=tip.localPosition;
            Part(tip,"Tail flight blade "+(i+1),Feather("articulated tail blade "+i,a-off,b-off,c-off,end-off,.115f,.037f,18,10),navy);
            Part(tip,"Tail cobalt enamel "+(i+1),Feather("tail enamel "+i,a-off+new Vector3(0,.018f,0),b-off+new Vector3(0,.018f,0),c-off+new Vector3(0,.018f,0),Vector3.Lerp(c,end,.91f)-off+new Vector3(0,.018f,0),.091f,.03f,15,8),i==2?cyan:blue);
        }
        Part(root,"Gold tail power collar",Loft("gold tail power sleeve",new[]{new Section(-.28f,-.02f,.105f,.115f),new Section(-.36f,-.02f,.116f,.126f),new Section(-.51f,-.025f,.09f,.10f)},40),gold);
        Part(root,"Cyan tail power inset",Loft("tail power inset",new[]{new Section(-.50f,-.025f,.075f,.084f),new Section(-.55f,-.025f,.073f,.081f)},40),glow);
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
    static Mesh Feather(string name,Vector3 a,Vector3 b,Vector3 c,Vector3 d,float width,float thickness,int length,int around)
    {
        var g=new Geometry();int stride=around+1;
        for(int j=0;j<=length;j++)
        {
            float u=j/(float)length;Vector3 p=Bezier(a,b,c,d,u);
            Vector3 tangent=(3*(1-u)*(1-u)*(b-a)+6*(1-u)*u*(c-b)+3*u*u*(d-c)).normalized;
            // Aerodynamic closed section with camber, unequal leading/trailing edges,
            // twisted tip and a narrow root. These are solid curved blades.
            Vector3 cross=Vector3.Cross(tangent,Vector3.forward).normalized;
            if(cross.sqrMagnitude<.01f)cross=Vector3.up;
            Vector3 normal=Vector3.Cross(tangent,cross).normalized;
            float taper=Mathf.Pow(Mathf.Sin(Mathf.PI*Mathf.Clamp(u,.006f,.994f)),.72f)*Mathf.Lerp(1.12f,.71f,u);
            float chord=width*taper,thick=thickness*Mathf.Pow(Mathf.Sin(Mathf.PI*Mathf.Clamp(u,.005f,.995f)),.65f);
            for(int i=0;i<=around;i++)
            {
                float rad=i*Mathf.PI*2/around;
                float across=Mathf.Sin(rad),camber=Mathf.Cos(rad);
                Vector3 v=p+cross*(across*chord)+normal*(camber*thick+.20f*thick*(1-across*across));
                g.Add(v,new Vector2(u,i/(float)around));
            }
        }
        for(int j=0;j<length;j++)for(int i=0;i<around;i++){int v=j*stride+i;g.Quad(v,v+stride,v+stride+1,v+1);}
        int capA=g.Add(a,Vector2.zero),capB=g.Add(d,Vector2.one);
        for(int i=0;i<around;i++){g.Tri(capA,i+1,i);int k=length*stride+i;g.Tri(capB,k,k+1);}
        return g.Build(name);
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
