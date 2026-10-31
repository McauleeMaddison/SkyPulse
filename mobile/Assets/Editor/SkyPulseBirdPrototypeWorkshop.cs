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
    public const string OpticsRevision = "Inset mechanical optics v2";
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
        blue=Material("01 Cobalt ceramic metal",new Color(.018f,.12f,.67f),.68f,.31f);
        cyan=Material("02 Cyan feather enamel",new Color(.015f,.67f,.91f),.58f,.35f);
        navy=Material("03 Cobalt shadow edges",new Color(.008f,.027f,.095f),.66f,.4f);
        ivory=Material("04 Warm ivory belly",new Color(.70f,.78f,.82f),.48f,.34f);
        gold=Material("05 Brushed champagne gold",new Color(.94f,.57f,.08f),.72f,.52f);
        dark=Material("06 Recessed optical black",new Color(.004f,.012f,.018f),.3f,.79f);
        silver=Material("07 Machined titanium",new Color(.39f,.53f,.63f),.75f,.52f);
        glow=Material("08 Cyan optical emission",new Color(.015f,.9f,1),.2f,.7f,new Color(.0f,1.5f,1.8f));
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
    [MenuItem("Tools/SkyPulse/Bird Workshop/Revise Finch head to inset mechanical optics")]
    public static void ReviseOptics()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before revising the Finch head.");
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)==null)throw new InvalidOperationException("Create the Finch art-review rig first.");
        // This explicit revision changes only existing head mesh buffers. It never
        // rebuilds the owner's prefab, scene, materials, joints or animation clips.
        blue=ExistingMaterial("01 Cobalt ceramic metal");cyan=ExistingMaterial("02 Cyan feather enamel");
        navy=ExistingMaterial("03 Cobalt shadow edges");ivory=ExistingMaterial("04 Warm ivory belly");
        gold=ExistingMaterial("05 Brushed champagne gold");dark=ExistingMaterial("06 Recessed optical black");
        silver=ExistingMaterial("07 Machined titanium");glow=ExistingMaterial("08 Cyan optical emission");
        var temporary=new GameObject("Temporary head mesh authoring"){hideFlags=HideFlags.HideAndDontSave};
        meshNumber=0;revisedMeshCount=0;revisionBuild=true;
        try
        {
            // Count through the same stable authored sequence; Save returns these
            // existing body buffers untouched while refreshHead is false.
            BuildBody(temporary.transform);
            refreshHead=true;BuildHead(Bone(temporary.transform,"Temporary head",Vector3.zero));
        }
        finally{refreshHead=false;revisionBuild=false;UnityEngine.Object.DestroyImmediate(temporary);}
        SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();
        Debug.Log("SKYPULSE_FINCH_HEAD_REVISED: "+revisedMeshCount+" existing head meshes updated in place; all asset GUIDs, non-head geometry, scene, prefab, materials and animation retained.");
    }
    public static void ReviseOpticsInQa()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Disposable batch QA product required for this entrypoint.");
        ReviseOptics();
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
    static Material Material(string name,Color colour,float metallic,float smooth,Color? emission=null)
    {
        string path=Folder+"/Materials/"+name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing!=null)return existing;
        var m=new Material(Shader.Find("Standard")){name=name};m.color=colour;m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);
        if(emission.HasValue){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission.Value);}
        AssetDatabase.CreateAsset(m,path);return m;
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
    // Curated cross-sections, rather than Unity primitive body shapes.
    static Section[] Hull()=>new[]{new Section(-1.07f,-.065f,.07f,.10f),new Section(-.96f,-.045f,.21f,.22f),new Section(-.79f,-.02f,.34f,.32f),new Section(-.58f,.0f,.43f,.39f),new Section(-.33f,.025f,.47f,.425f),new Section(-.06f,.07f,.455f,.42f),new Section(.18f,.13f,.40f,.375f),new Section(.40f,.22f,.30f,.29f),new Section(.56f,.32f,.14f,.16f),new Section(.59f,.34f,.025f,.035f)};
    static void BuildBody(Transform body)
    {
        var sections=Hull();Part(body,"Tapered cobalt fuselage",Loft("sculpted fuselage",sections,64),blue);
        var belly=new Section[sections.Length];for(int i=0;i<belly.Length;i++){var s=sections[i];belly[i]=new Section(s.x,s.y-.007f,s.h+.014f,s.w+.014f);}
        Part(body,"Ivory chest shield",Loft("curved ivory chest plating",belly,40,100,260),ivory);
        // The two dark edge seams follow the actual compound-curved chest.
        foreach(int side in new[]{-1,1})
        {
            var seam=new Vector3[sections.Length];for(int i=0;i<seam.Length;i++){var s=belly[i];float a=(side<0?260:100)*Mathf.Deg2Rad;seam[i]=new Vector3(s.x,s.y+Mathf.Cos(a)*s.h,Mathf.Sin(a)*s.w);}
            Part(body,"Chest shield gasket "+side,Tube("chest panel gasket",seam,.012f,7),navy);
            for(int p=0;p<3;p++)
            {
                float x=-.84f+p*.28f;
                var profile=new[]{new Vector3(x-.10f,.05f,side*.388f),new Vector3(x-.07f,.24f,side*.36f),new Vector3(x+.10f,.37f,side*.27f),new Vector3(x+.2f,.35f,side*.235f)};
                Part(body,"Swept back armor seam "+side+" "+p,Tube("swept fuselage panel line",profile,.011f,7),navy);
            }
            var leg=Bone(body,"Folded landing talon "+side,new Vector3(-.49f,-.29f,side*.32f));
            Part(leg,"Gold leg actuator",Ring("gold talon hinge",.095f,.06f,.095f,32,6),gold);
            Part(leg,"Talon shank",Feather("folded cobalt shank",Vector3.zero,new Vector3(-.12f,-.08f,side*.025f),new Vector3(-.24f,-.22f,side*.03f),new Vector3(-.25f,-.26f,side*.035f),.052f,.027f,12,10),blue);
            Part(leg,"Talon toe one",Feather("pointed titanium toe",new Vector3(-.21f,-.17f,side*.035f),new Vector3(-.28f,-.22f,side*.05f),new Vector3(-.37f,-.24f,side*.06f),new Vector3(-.43f,-.24f,side*.06f),.035f,.023f,10,8),navy);
            Part(leg,"Talon toe two",Feather("second pointed toe",new Vector3(-.19f,-.2f,side*.04f),new Vector3(-.20f,-.30f,side*.08f),new Vector3(-.3f,-.35f,side*.10f),new Vector3(-.37f,-.35f,side*.10f),.033f,.020f,10,8),navy);
        }
        Part(body,"Neck articulated dark collar",Loft("neck flexible gasket",new[]{new Section(.24f,.27f,.29f,.30f),new Section(.31f,.31f,.31f,.31f),new Section(.36f,.35f,.31f,.31f)},48),navy);
        Part(body,"Neck collar cobalt bevel",Loft("neck blue rim",new[]{new Section(.33f,.34f,.322f,.322f),new Section(.37f,.37f,.325f,.325f),new Section(.40f,.39f,.29f,.30f)},48),blue);
    }
    static void BuildHead(Transform head)
    {
        // In head-local coordinates, a compact aerodynamic helmet around the optics.
        Part(head,"Sculpted cobalt helmet",MechanicalHelmet(),blue);
        // Layered crest echoes the two swept blades in the source silhouette.
        Part(head,"Long swept crown blade",Feather("rear crown blade",new Vector3(.15f,.75f,0),new Vector3(-.09f,.86f,0),new Vector3(-.31f,.98f,0),new Vector3(-.47f,1.06f,0),.095f,.055f,18,10),navy);
        Part(head,"Cobalt crown inlay",Feather("crown enamel",new Vector3(.14f,.78f,-.012f),new Vector3(-.08f,.9f,-.012f),new Vector3(-.29f,1.0f,-.012f),new Vector3(-.44f,1.055f,-.012f),.071f,.043f,16,10),blue);
        Part(head,"Forward swept crown blade",Feather("short crown blade",new Vector3(.3f,.76f,.02f),new Vector3(.03f,.94f,.02f),new Vector3(-.16f,1.1f,.02f),new Vector3(-.34f,1.16f,.02f),.074f,.046f,16,10),gold);
        Part(head,"Crown cyan vent",Feather("crest cyan center",new Vector3(.26f,.78f,-.027f),new Vector3(.03f,.94f,-.027f),new Vector3(-.13f,1.06f,-.027f),new Vector3(-.27f,1.12f,-.027f),.046f,.027f,14,8),blue);
        foreach(int side in new[]{-1,1})
        {
            // Keep every existing part transform, name and material binding. The
            // revised shape is baked into that part's existing local mesh buffer.
            Vector3 optic=new Vector3(.40f,.35f,side*.414f);
            Part(head,"Optic recessed gasket "+side,OpticSaddle("optical black recessed surround",side,optic),navy,optic);
            Part(head,"Optic titanium bezel "+side,ConformalOpticRim("optic outer beveled lip",side,.224f,.207f,.213f,.196f,.454f,0,360,optic+new Vector3(0,0,side*.026f)),silver,optic+new Vector3(0,0,side*.026f));
            Part(head,"Champagne gold brow arc "+side,ConformalOpticRim("gold optical crescent",side,.260f,.239f,.239f,.218f,.441f,102,254,optic+new Vector3(0,0,side*.042f)),gold,optic+new Vector3(0,0,side*.042f));
            var lens=Shift(Dome("curved black optical lens",.212f,.195f,side*.008f,64,10),new Vector3(.015f,.035f,side*(.449f-.463f)));
            Part(head,"Dark curved optical glass "+side,lens,dark,optic+new Vector3(0,0,side*.049f));
            // Two independently convex bars produce an open, precise chevron.
            Part(head,"Forward cyan optical chevron "+side,ChevronMark("inset optical direction mark",side,optic+new Vector3(.025f,.015f,side*.113f)),glow,optic+new Vector3(.025f,.015f,side*.113f));
            var rim=new[]{new Vector3(-.04f,.12f,side*.35f),new Vector3(.03f,-.02f,side*.34f),new Vector3(.22f,-.13f,side*.30f),new Vector3(.42f,-.12f,side*.30f),new Vector3(.60f,-.02f,side*.28f)};
            Part(head,"Ivory jaw rim "+side,Tube("jaw machined trim",rim,.031f,10),ivory);
            // Two subtle brow fasteners, modeled with beveled ring sections.
            Part(head,"Brow fastener "+side,Ring("brow screw",.021f,.009f,.015f,16,4),silver,new Vector3(.11f,.67f,side*.35f));
        }
        // Closed six-faced, beveled wedge; genuinely spans the face's depth.
        Part(head,"Gold upper beak",Beak("gold beveled finch beak",false),gold,new Vector3(.66f,.22f,0));
        Part(head,"Dark lower beak seam",Beak("beak split gasket",true),navy,new Vector3(.66f,.20f,0));
        Part(head,"Beak lower gold plate",Beak("lower beak",false),gold,new Vector3(.66f,.185f,0)).transform.localScale=new Vector3(.89f,.28f,.84f);
    }
    static readonly Vector2 OpticCenter=new Vector2(.415f,.385f);
    static Section[] HelmetProfile()=>new[]{new Section(-.22f,.28f,.05f,.06f),new Section(-.15f,.29f,.27f,.25f),new Section(-.02f,.32f,.40f,.37f),new Section(.14f,.35f,.46f,.43f),new Section(.32f,.35f,.455f,.455f),new Section(.47f,.35f,.415f,.44f),new Section(.61f,.33f,.325f,.37f),new Section(.71f,.30f,.20f,.26f),new Section(.75f,.27f,.04f,.055f)};
    static Section HelmetSection(float x)
    {
        var profile=HelmetProfile();
        for(int i=1;i<profile.Length;i++)if(x<=profile[i].x)
        {
            var a=profile[i-1];var b=profile[i];float t=Mathf.InverseLerp(a.x,b.x,x);
            return new Section(x,Mathf.Lerp(a.y,b.y,t),Mathf.Lerp(a.h,b.h,t),Mathf.Lerp(a.w,b.w,t));
        }
        return profile[profile.Length-1];
    }
    static float HelmetDepth(float x,float y)
    {
        var s=HelmetSection(x);float c=Mathf.Clamp((y-s.y)/s.h,-.999f,.999f);
        c=Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),1f/.87f);
        return s.w*Mathf.Pow(Mathf.Sqrt(Mathf.Max(.0001f,1-c*c)),.91f);
    }
    static Mesh MechanicalHelmet()
    {
        const int along=32,around=64;var g=new Geometry();int stride=around+1;
        for(int j=0;j<=along;j++)
        {
            float x=Mathf.Lerp(-.22f,.75f,j/(float)along);var s=HelmetSection(x);
            for(int i=0;i<=around;i++)
            {
                float a=i*Mathf.PI*2/around,c=Mathf.Cos(a),z=Mathf.Sin(a);
                g.Add(new Vector3(x,s.y+s.h*Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),.87f),s.w*Mathf.Sign(z)*Mathf.Pow(Mathf.Abs(z),.91f)),new Vector2(j/(float)along,i/(float)around));
            }
        }
        for(int j=0;j<along;j++)for(int i=0;i<around;i++)
        {
            int k=j*stride+i;var p=(g.v[k]+g.v[k+1]+g.v[k+stride]+g.v[k+stride+1])*.25f;
            float dx=(p.x-OpticCenter.x)/.241f,dy=(p.y-OpticCenter.y)/.223f;
            if(dx*dx+dy*dy<1)continue; // Actual bilateral aperture behind the lens.
            g.Quad(k,k+1,k+stride+1,k+stride);
        }
        int aCap=g.Add(new Vector3(-.22f,.28f,0),Vector2.zero),bCap=g.Add(new Vector3(.75f,.27f,0),Vector2.one);
        for(int i=0;i<around;i++){g.Tri(aCap,i+1,i);int k=along*stride+i;g.Tri(bCap,k,k+1);}
        // Angular brow panels are integrated into this same cobalt helmet mesh.
        foreach(int side in new[]{-1,1})
        {
            float[] xs={.10f,.20f,.36f,.50f,.61f},upper={.56f,.665f,.66f,.57f,.46f},lower={.49f,.565f,.57f,.50f,.405f};
            int first=g.v.Count;
            for(int layer=0;layer<2;layer++)for(int i=0;i<xs.Length;i++)for(int row=0;row<2;row++)
            {
                float y=row==0?lower[i]:upper[i],z=HelmetDepth(xs[i],y)+(layer==0?.004f:.017f);
                g.Add(new Vector3(xs[i],y,side*z),new Vector2(i/4f,row));
            }
            for(int i=0;i<4;i++)
            {
                int k=first+10+i*2;
                if(side<0)g.Quad(k,k+1,k+3,k+2);else g.Quad(k,k+2,k+3,k+1);
                g.Quad(first+i*2,first+(i+1)*2,k+2,k);
                g.Quad(first+i*2+1,k+1,k+3,first+(i+1)*2+1);
            }
            g.Quad(first,first+10,first+11,first+1);g.Quad(first+8,first+9,first+19,first+18);
        }
        return g.Build("helmet compound sections");
    }
    static Mesh OpticSaddle(string name,int side,Vector3 origin)
    {
        const int slices=64;var g=new Geometry();
        // A broad conformal rear flange meets the helmet; an angled shoulder
        // carries the small lens recess. No separate floating circular housing.
        for(int ring=0;ring<5;ring++)for(int i=0;i<=slices;i++)
        {
            float a=i*Mathf.PI*2/slices;
            float rx=ring==0?.294f:ring==1?.278f:ring==2?.246f:ring==3?.219f:.213f;
            float ry=ring==0?.265f:ring==1?.250f:ring==2?.226f:ring==3?.204f:.196f;
            float x=OpticCenter.x+rx*Mathf.Cos(a),y=OpticCenter.y+ry*Mathf.Sin(a);
            float z=ring==0?HelmetDepth(x,y)+.003f:ring==1?HelmetDepth(x,y)+.009f:ring==2?Mathf.Lerp(HelmetDepth(x,y),.448f,.70f)+.006f:ring==3?.450f:.441f;
            g.Add(new Vector3(x,y,side*z)-origin,new Vector2(i/(float)slices,ring/4f));
        }
        for(int ring=0;ring<4;ring++)for(int i=0;i<slices;i++)
        {
            int k=ring*(slices+1)+i;
            if(side<0)g.Quad(k,k+slices+1,k+slices+2,k+1);else g.Quad(k,k+1,k+slices+2,k+slices+1);
        }
        return g.Build(name);
    }
    static Mesh ConformalOpticRim(string name,int side,float rx,float ry,float innerX,float innerY,float front,float start,float end,Vector3 origin)
    {
        int slices=Mathf.CeilToInt((end-start)/5);const int around=6;var g=new Geometry();
        bool goldArc=end-start<359;
        for(int i=0;i<=slices;i++)for(int j=0;j<=around;j++)
        {
            float a=Mathf.Lerp(start,end,i/(float)slices)*Mathf.Deg2Rad,b=j*Mathf.PI*2/around;
            float radialX=Mathf.Lerp(innerX,rx,(Mathf.Cos(b)+1)*.5f);
            float x=OpticCenter.x+radialX*Mathf.Cos(a);
            float y=OpticCenter.y+(Mathf.Lerp(innerY,ry,(Mathf.Cos(b)+1)*.5f))*Mathf.Sin(a);
            float z=(goldArc?Mathf.Lerp(HelmetDepth(x,y)+.009f,.451f,Mathf.InverseLerp(.278f,.246f,radialX))+.004f:front)+Mathf.Sin(b)*.004f;
            g.Add(new Vector3(x,y,side*z)-origin,new Vector2(i/(float)slices,j/(float)around));
        }
        for(int i=0;i<slices;i++)for(int j=0;j<around;j++)
        {int k=i*(around+1)+j;if(side<0)g.Quad(k,k+1,k+around+2,k+around+1);else g.Quad(k,k+around+1,k+around+2,k+1);}
        return g.Build(name);
    }
    static Mesh Shift(Mesh mesh,Vector3 offset)
    {
        var v=mesh.vertices;for(int i=0;i<v.Length;i++)v[i]+=offset;mesh.vertices=v;mesh.RecalculateBounds();return mesh;
    }
    static Mesh ChevronMark(string name,int side,Vector3 origin)
    {
        var top=BeveledPolygon(name,new[]{new Vector2(.042f,-.009f),new Vector2(.063f,.008f),new Vector2(-.034f,.074f),new Vector2(-.054f,.066f)},.006f);
        var bottom=BeveledPolygon(name,new[]{new Vector2(-.054f,-.066f),new Vector2(-.034f,-.074f),new Vector2(.063f,-.008f),new Vector2(.042f,.009f)},.006f);
        var offset=new Vector3(OpticCenter.x,OpticCenter.y,side*.457f)-origin;
        var g=new Geometry();
        foreach(var mesh in new[]{top,bottom})
        {
            int first=g.v.Count;var uv=mesh.uv;var v=mesh.vertices;var tris=mesh.triangles;
            for(int i=0;i<v.Length;i++)g.Add(v[i]+offset,uv[i]);
            for(int i=0;i<tris.Length;i+=3)g.Tri(first+tris[i],first+tris[i+1],first+tris[i+2]);
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        return g.Build(name);
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
    static Mesh Tube(string name,Vector3[] points,float radius,int sides)
    {
        var g=new Geometry();int stride=sides+1;
        for(int j=0;j<points.Length;j++)
        {
            var tangent=(points[Mathf.Min(points.Length-1,j+1)]-points[Mathf.Max(0,j-1)]).normalized;
            var right=Vector3.Cross(tangent,Vector3.forward).normalized;if(right.sqrMagnitude<.01f)right=Vector3.right;var up=Vector3.Cross(tangent,right).normalized;
            for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;g.Add(points[j]+radius*(right*Mathf.Cos(a)+up*Mathf.Sin(a)),new Vector2(j/(float)(points.Length-1),i/(float)sides));}
        }
        for(int j=0;j<points.Length-1;j++)for(int i=0;i<sides;i++){int k=j*stride+i;g.Quad(k,k+stride,k+stride+1,k+1);}return g.Build(name);
    }
    static Mesh BeveledPolygon(string name,Vector2[] outline,float depth)
    {
        var g=new Geometry();Vector2 center=Vector2.zero;foreach(var p in outline)center+=p;center/=outline.Length;
        // Four perimeter loops provide a real bevel and preserve hard hardware edges.
        for(int k=0;k<4;k++)for(int i=0;i<outline.Length;i++)
        {float shrink=(k==0||k==3)?.88f:1;var p=Vector2.Lerp(center,outline[i],shrink);g.Add(new Vector3(p.x,p.y,(k==0?-.5f:k==1?-.25f:k==2?.25f:.5f)*depth),p);}
        int n=outline.Length;for(int k=0;k<3;k++)for(int i=0;i<n;i++){int next=(i+1)%n;g.Quad(k*n+i,k*n+next,(k+1)*n+next,(k+1)*n+i);}
        for(int i=1;i<n-1;i++){g.Tri(0,i+1,i);g.Tri(3*n,3*n+i,3*n+i+1);}return g.Build(name);
    }
    static Mesh Beak(string name,bool seam)
    {
        var g=new Geometry();
        Vector3[] points={new Vector3(0,.15f,-.16f),new Vector3(0,.15f,.16f),new Vector3(0,-.06f,-.14f),new Vector3(0,-.06f,.14f),new Vector3(.48f,-.035f,0),new Vector3(.10f,.135f,0)};
        if(seam)for(int i=0;i<points.Length;i++)points[i].y*=.13f;
        // Duplicated triangle vertices intentionally keep the gold facets crisp.
        int[] triangles={0,5,4,5,1,4,0,4,2,1,3,4,2,4,3,0,2,3,0,3,1,0,1,5};
        for(int i=0;i<triangles.Length;i+=3){int k=g.v.Count;for(int j=0;j<3;j++)g.Add(points[triangles[i+j]],Vector2.zero);g.Tri(k,k+1,k+2);}return g.Build(name);
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
