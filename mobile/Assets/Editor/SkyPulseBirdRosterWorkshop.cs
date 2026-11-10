using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using SkyPulse.Mobile;

/// <summary>Reference-measured, editable roster studies. Saved meshes are solid; source PNGs are evidence only.</summary>
public static partial class SkyPulseBirdRosterWorkshop
{
    public const string Folder=SkyPulseBirdPrototypeWorkshop.Folder+"/Roster";
    public const string DesignsPath="Assets/Editor/SkyPulseBirdRosterDesigns.json";
    public const string GalleryPath=Folder+"/Roster Gallery.unity";
    [Serializable] public struct P {public float x,y;public Vector2 V=>new Vector2(x,y);}
    [Serializable] public sealed class Blade {public P root,tip;public float width,curve;public int material,inset;}
    [Serializable] public sealed class Panel {public P[] outline;public int material;public string surface;}
    [Serializable] public sealed class Design
    {
        public string id,name,reference,description,sourceSha256,eyeShape;
        public int width,height,headMaterial,bodyMaterial,beakMaterial=4;public bool jet;public float bodyDepth,headDepth,beakDepth;
        public P[] body,head,beak;public float[] eye;public P wingRoot,tailRoot;
        public Panel[] crest,panels;public Blade[] primaries,tail;public string[] palette;
    }
    [Serializable] public sealed class Designs {public Design[] birds;}
    public static Design[] LoadDesigns()=>JsonUtility.FromJson<Designs>(File.ReadAllText(DesignsPath)).birds;
    public static string BirdFolder(Design d)=>Folder+"/"+d.name;
    public static string PrefabPath(Design d)=>BirdFolder(d)+"/"+d.name+" Rig.prefab";
    public static string ScenePath(Design d)=>BirdFolder(d)+"/"+d.name+" Review.unity";
    static Design design;static Vector2 origin;static Material[] finishes;static string directory;
    static readonly List<Mesh> meshes=new List<Mesh>();
    static Vector2 Mean(P[] points){var p=Vector2.zero;foreach(var x in points)p+=x.V;return p/points.Length;}
    static Vector2[] Points(P[] p)=>p.Select(v=>v.V).ToArray();
    static Vector3 Point(Vector2 p,float z=0)=>new Vector3((p.x-origin.x)*.01f,(origin.y-p.y)*.01f,z*.01f);
    static Vector3 Point(P p,float z=0)=>Point(p.V,z);
    static Transform Joint(Transform parent,string name,Vector3 at)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=at;return t;}
    [MenuItem("Tools/SkyPulse/Bird Workshop/Create remaining reference roster")]
    public static void Generate()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before authoring roster assets.");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(Application.isBatchMode&&string.IsNullOrEmpty(SceneManager.GetActiveScene().path))EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        foreach(var d in LoadDesigns())CreateBird(d);
        CreateGallery();AssetDatabase.Refresh();
        Debug.Log("SKYPULSE_ROSTER_AUTHORED: fourteen individual editable studies; existing Finch and gameplay retained.");
    }
    static void CreateBird(Design d)
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(d))!=null)
        {if(!File.Exists(ScenePath(d)))CreateScene(d);return;}
        directory=BirdFolder(d);
        if(Directory.Exists(directory)&&Directory.GetFiles(directory).Any(p=>!p.EndsWith(".meta",StringComparison.Ordinal)))
            throw new InvalidOperationException("Incomplete or owner-edited roster folder; preserved: "+directory);
        ValidateDesign(d);Directory.CreateDirectory(directory);AssetDatabase.Refresh();design=d;origin=Mean(d.body);meshes.Clear();finishes=CreateFinishes(d);
        var root=new GameObject(d.name+" · reference mechanical 3D study");var rig=root.AddComponent<SkyPulseNeonFinchPrototype>();
        try
        {
            rig.animate=false;rig.turntable=false;
            BuildEngineeredSculpt(rig);FitShoulders(rig,design);
            // One asset contains named editable mesh sub-assets; no hundreds of loose numbered files.
            for(int i=0;i<meshes.Count;i++)
            {meshes[i].name=(i+1).ToString("000")+" "+meshes[i].name;if(i==0)AssetDatabase.CreateAsset(meshes[i],directory+"/Geometry.asset");else AssetDatabase.AddObjectToAsset(meshes[i],directory+"/Geometry.asset");}
            AssetDatabase.SaveAssetIfDirty(meshes[0]);
            rig.CaptureBindPose();rig.animate=true;rig.turntable=true;
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath(d));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        CreateScene(d);
    }
    static void ValidateDesign(Design d)
    {
        if(d.body.Length<5||d.head.Length<5||d.primaries.Length<7||d.tail.Length<3||d.palette.Length!=7||d.eye.Length!=4)throw new InvalidOperationException("Incomplete native artwork design: "+d.id);
        string source="Assets/Resources/SkyPulse/characters/roster/"+d.reference+".png";
        using(var sha=System.Security.Cryptography.SHA256.Create())
        {string hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(source))).Replace("-","").ToLowerInvariant();if(hash!=d.sourceSha256)throw new InvalidOperationException("Original artwork changed since measurement: "+d.id);}
    }
    static Material[] CreateFinishes(Design d)
    {
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(SkyPulseBirdPrototypeWorkshop.Folder+"/Shaders/FinchArmourFinish.shader");
        if(shader==null)throw new InvalidOperationException("Approved mechanical finish shader required.");
        var result=new Material[d.jet?9:8];string[] names={"Armour","Feather enamel","Recessed chassis","Ivory alloy","Machined trim","Optical iris","Luminous inlay","Fitted optical glass"};
        for(int i=0;i<8;i++)
        {
            ColorUtility.TryParseHtmlString(i==7?"#050b16":d.palette[i],out var color);
            var m=new Material(shader){name=d.name+" · "+names[i]};int kind=i==7?3:i==6?4:i==2?2:i==3||i==4?1:0;
            m.SetColor("_Color",color);m.SetColor("_BladeColor",color);m.SetFloat("_FinishKind",kind);
            m.SetFloat("_Metallic",i==4?.55f:i==7?.06f:i==2?.12f:.22f);m.SetFloat("_Glossiness",i==7?.62f:i==4?.52f:.48f);
            m.SetFloat("_BrushStrength",0);m.SetFloat("_SatinStrength",i==2?.25f:.5f);
            m.SetColor("_EmissionColor",i==5||i==6?color:Color.black);m.SetFloat("_EmissionStrength",i==5?.45f:i==6?.65f:0);
            result[i]=m;if(i==0)AssetDatabase.CreateAsset(m,directory+"/Finishes.asset");else AssetDatabase.AddObjectToAsset(m,directory+"/Finishes.asset");
        }
        if(d.jet)
        {
            var fire=new Material(AssetDatabase.LoadAssetAtPath<Shader>(SkyPulseBirdJetWorkshop.ShaderPath)){name=d.name+" · live engine fire"};
            ColorUtility.TryParseHtmlString(d.palette[6],out var colour);fire.SetColor("_FlameColor",colour*1.4f);fire.SetColor("_CoreColor",new Color(3.2f,3.6f,4,1));fire.SetFloat("_Opacity",.68f);fire.SetFloat("_JetTime",0);fire.SetFloat("_JetStrength",.92f);fire.SetFloat("_JetMotion",1);result[8]=fire;AssetDatabase.AddObjectToAsset(fire,directory+"/Finishes.asset");
        }
        AssetDatabase.SaveAssetIfDirty(result[0]);return result;
    }
    static void Part(Transform parent,string name,Mesh mesh,int finish)
    {
        mesh.name=name;var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);
        // Author points in the common source frame, then retain the native joint pivot.
        Vector3 offset=parent.position;var v=mesh.vertices;for(int i=0;i<v.Length;i++)v[i]-=offset;mesh.vertices=v;mesh.RecalculateBounds();
        g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=finishes[Mathf.Clamp(finish,0,7)];meshes.Add(mesh);
    }
    static void BuildEyes(Transform head,Design d)
    {
        var center=new Vector2(d.eye[0],d.eye[1]);var helmetParts=head.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Closed reference helmet"||f.name.StartsWith("Helmet fitted panel ",StringComparison.Ordinal)).ToArray();
        foreach(int side in new[]{-1,1})
        {
            var shell=Points(d.head);var outer=EyeContour(center,d.eye[2],d.eye[3],d.eyeShape);
            var supports=helmetParts.Select(f=>ActualSkin(f.sharedMesh,head.position,side)).ToArray();
            float HelmetSkin(Vector2 p){float value=0;foreach(var sample in supports)value=Mathf.Max(value,sample(p));return value;}
            var glass=SurfacePanel(outer,shell,d.headDepth,side,1.3f,3.5f,HelmetSkin);
            Part(head,"Seated dark optical glass "+side,glass,7);
            Part(head,"Fitted alloy optical bezel "+side,SurfaceRing(outer,shell,d.headDepth,side,1.8f),4);
            var iris=SurfacePanel(EyeContour(center,d.eye[2]*.52f,d.eye[3]*.52f,d.eyeShape),shell,d.headDepth,side,1.1f,2.2f,ActualSkin(glass,head.position,side));
            Part(head,"Reference luminous iris "+side,iris,5);
            Part(head,"Optical core "+side,SurfacePanel(EyeContour(center+new Vector2(d.eye[2]*.10f,-d.eye[3]*.08f),d.eye[2]*.18f,d.eye[3]*.23f,"round"),shell,d.headDepth,side,.8f,1.5f,ActualSkin(iris,head.position,side)),6);
        }
    }
    // Fit each optical layer to the actual saved support triangles. Independent
    // source shapes otherwise approximate the helmet differently and bury an iris.
    static Func<Vector2,float> ActualSkin(Mesh mesh,Vector3 offset,int side)
    {
        var v=mesh.vertices;for(int i=0;i<v.Length;i++)v[i]+=offset;var t=mesh.triangles;
        return native=>
        {
            var p=Point(native);float surface=float.NegativeInfinity;
            for(int i=0;i<t.Length;i+=3)
            {
                var a=v[t[i]];var b=v[t[i+1]];var c=v[t[i+2]];float den=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);if(Mathf.Abs(den)<1e-10f)continue;
                float u=((b.y-c.y)*(p.x-c.x)+(c.x-b.x)*(p.y-c.y))/den,w=((c.y-a.y)*(p.x-c.x)+(a.x-c.x)*(p.y-c.y))/den,q=1-u-w;
                if(u>=-.00001f&&w>=-.00001f&&q>=-.00001f)surface=Mathf.Max(surface,side*(u*a.z+w*b.z+q*c.z)*100);
            }
            return float.IsNegativeInfinity(surface)?0:surface;
        };
    }
    static void BuildWing(SkyPulseNeonFinchPrototype rig,int side,out Transform wing,out Transform tip)
    {
        var d=design;float hubZ=side*(SkinDepth(Smooth(Points(d.body)),d.wingRoot.V,d.bodyDepth)+3);
        wing=Joint(rig.body,(side<0?"03 Near":"04 Far")+" wing bearing",Point(d.wingRoot,hubZ));
        var mean=Vector2.zero;foreach(var b in d.primaries)mean+=b.root.V;mean/=d.primaries.Length;
        tip=Joint(wing,"Articulated distal feather fan",Point(mean,hubZ+side*4));
        var arm=new Blade{root=d.wingRoot,tip=new P{x=mean.x,y=mean.y},width=9,curve=0};
        if(Vector2.Distance(d.wingRoot.V,mean)>3)
        {Part(wing,"Load-bearing wing forearm",Rod(d.wingRoot.V,mean,hubZ,7),2);Part(wing,"Forearm alloy cover",FeatherInset(arm,hubZ,side,7),3);}
        Part(wing,"Shoulder machined bearing",Ring(d.wingRoot.V,hubZ,13,3.5f,4),4);
        Part(wing,"Shoulder energy core",RoundSolid(d.wingRoot.V,hubZ+side*4,7,7,3),5);
        // A concealed load-bearing spar reaches every root in the fan.
        int i=0;foreach(var b in d.primaries)
        {
            float z=hubZ+side*(4+i*.85f);
            var spar=new Blade{root=new P{x=mean.x,y=mean.y},tip=b.root,width=4,curve=0};
            if(Vector2.Distance(mean,b.root.V)>2)Part(tip,"Buried feather root spar "+i,Rod(mean,b.root.V,z,3),2);
            Part(tip,"Reference primary chassis "+i,Feather(b,z,3.1f,side),b.material);
            Part(tip,"Primary enamel inlay "+i,FeatherInset(b,z,side),b.inset);
            // Root coverts shorten and armour the fan; they are shaped from that bird's actual primary.
            var covert=new Blade{root=b.root,tip=new P{x=Mathf.Lerp(b.root.x,b.tip.x,.42f),y=Mathf.Lerp(b.root.y,b.tip.y,.42f)},width=b.width*.72f,curve=b.curve*.35f};
            Part(tip,"Swept root covert "+i,Feather(covert,z+side*2.3f,1.8f,side),i%3==0?4:design.headMaterial==3?3:0);i++;
        }
    }
    static void BuildFeet(SkyPulseNeonFinchPrototype rig)
    {
        var body=Points(design.body);var c=Mean(design.body);float lower=body.Max(p=>p.y);float y=Mathf.Lerp(c.y,lower,.79f);
        foreach(int side in new[]{-1,1})
        {
            var root=new Vector2(c.x+13,y);float z=side*design.bodyDepth*.58f;
            Part(rig.body,"Landing hinge "+side,RoundSolid(root,z,7,7,5),4);
            var leg=new Blade{root=new P{x=root.x,y=root.y},tip=new P{x=root.x-5,y=lower+10},width=5,curve=-2};
            Part(rig.body,"Folded landing shank "+side,Feather(leg,z,4,side),2);
            for(int n=0;n<3;n++){var claw=new Blade{root=leg.tip,tip=new P{x=leg.tip.x+8+n*3,y=leg.tip.y+6+n*2},width=2.3f,curve=3};Part(rig.body,"Hooked talon "+side+" "+n,Feather(claw,z+(n-1)*4,2.2f,side),4);}
        }
    }
    sealed class G
    {
        public List<Vector3> v=new List<Vector3>();public List<Vector2> uv=new List<Vector2>();public List<int> t=new List<int>();
        public int Add(Vector3 p,Vector2 tex=default){int i=v.Count;v.Add(p);uv.Add(tex);return i;}
        public void Tri(int a,int b,int c){t.Add(a);t.Add(b);t.Add(c);}
        public void Quad(int a,int b,int c,int d){Tri(a,b,c);Tri(a,c,d);}
        public Mesh Finish(string name){var m=new Mesh{name=name};m.SetVertices(v);m.SetUVs(0,uv);double volume=0;for(int i=0;i<t.Count;i+=3)volume+=Vector3.Dot(v[t[i]],Vector3.Cross(v[t[i+1]],v[t[i+2]]))/6.0;if(volume<0)for(int i=0;i<t.Count;i+=3){int a=t[i];t[i]=t[i+2];t[i+2]=a;}m.SetTriangles(t,0);m.SetColors(Enumerable.Repeat(Color.white,v.Count).ToList());m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;}
    }
    static Vector2[] Smooth(Vector2[] input,int perEdge=4)
    {
        var points=new List<Vector2>();int n=input.Length;
        for(int i=0;i<n;i++)for(int j=0;j<perEdge;j++)
        {float t=j/(float)perEdge;var a=input[(i+n-1)%n];var b=input[i];var c=input[(i+1)%n];var d=input[(i+2)%n];points.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));}
        float area=0;for(int i=0;i<points.Count;i++){var a=points[i];var b=points[(i+1)%points.Count];area+=a.x*b.y-b.x*a.y;}
        // Source Y points down; this order makes the front surface face -Z.
        if(area<0)points.Reverse();return points.ToArray();
    }
    static Vector2 Center(Vector2[] p){var c=Vector2.zero;foreach(var v in p)c+=v;return c/p.Length;}
    static Mesh Dome(Vector2[] outline,float depth,int rings=16,float z=0)
    {
        var p=Smooth(outline,outline.Length>20?1:4);int n=p.Length;var center=Center(p);var g=new G();int front=g.Add(Point(center,z-depth));
        for(int ring=1;ring<rings;ring++)
        {float angle=ring*Mathf.PI/rings;float scale=Mathf.Sin(angle);for(int j=0;j<n;j++)g.Add(Point(Vector2.Lerp(center,p[j],scale),z-Mathf.Cos(angle)*depth),new Vector2(j/(float)n,ring/(float)rings));}
        int back=g.Add(Point(center,z+depth));
        for(int j=0;j<n;j++)g.Tri(front,1+(j+1)%n,1+j);
        for(int ring=0;ring<rings-2;ring++)for(int j=0;j<n;j++){int a=1+ring*n+j,b=1+ring*n+(j+1)%n;g.Quad(a,b,b+n,a+n);}
        int last=1+(rings-2)*n;for(int j=0;j<n;j++)g.Tri(back,last+j,last+(j+1)%n);
        return g.Finish("Rounded closed shell");
    }
    static float SkinDepth(Vector2[] outline,Vector2 at,float depth)
    {
        var center=Center(outline);var ray=at-center;if(ray.sqrMagnitude<.0001f)return depth;float boundary=1e9f;
        float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        for(int i=0;i<outline.Length;i++)
        {var a=outline[i]-center;var edge=outline[(i+1)%outline.Length]-outline[i];float det=Cross(ray,edge);if(Mathf.Abs(det)<.00001f)continue;float t=Cross(a,edge)/det,u=Cross(a,ray)/det;if(t>0&&u>=0&&u<=1)boundary=Mathf.Min(boundary,t);}
        float ratio=boundary<1e8f?1/Mathf.Max(.001f,boundary):1;return depth*Mathf.Sqrt(Mathf.Max(0,1-ratio*ratio));
    }
    static Mesh SurfacePanel(Vector2[] outline,Vector2[] support,float depth,int side,float lift,float thickness,Func<Vector2,float> fittedSkin=null)
    {
        var p=Smooth(outline,outline.Length>20?1:3);var shell=Smooth(support,support.Length>20?1:4);var center=Center(p);var g=new G();int n=p.Length;const int radials=6;
        float Surface(Vector2 at)=>fittedSkin!=null?fittedSkin(at):SkinDepth(shell,at,depth);
        // Radial rings avoid a single flat cap across a curved helmet.
        for(int face=0;face<2;face++)
        {
            float offset=face==0?lift:lift-thickness;int start=g.v.Count;
            g.Add(Point(center,side*(Surface(center)+offset)));
            for(int ring=1;ring<=radials;ring++)for(int i=0;i<n;i++)
            {var at=Vector2.Lerp(center,p[i],ring/(float)radials);g.Add(Point(at,side*(Surface(at)+offset)),new Vector2(i/(float)n,ring/(float)radials));}
            for(int i=0;i<n;i++)g.Tri(start,start+1+(i+1)%n,start+1+i);
            for(int ring=0;ring<radials-1;ring++)for(int i=0;i<n;i++){int a=start+1+ring*n+i,b=start+1+ring*n+(i+1)%n;g.Quad(a,b,b+n,a+n);}
            if((face==0?side:-side)>0)for(int i=g.t.Count-(n*3+(radials-1)*n*6);i<g.t.Count;i+=3){int t=g.t[i];g.t[i]=g.t[i+2];g.t[i+2]=t;}
        }
        int stride=1+radials*n;for(int i=0;i<n;i++){int a=1+(radials-1)*n+i,b=1+(radials-1)*n+(i+1)%n;if(side<0)g.Quad(a,b,b+stride,a+stride);else g.Quad(a,a+stride,b+stride,b);}
        return g.Finish("Conformal closed armour inlay");
    }
    static Mesh SurfaceRing(Vector2[] outline,Vector2[] support,float depth,int side,float lift)
    {
        var p=Smooth(outline,outline.Length>20?1:3);var shell=Smooth(support,support.Length>20?1:4);var center=Center(p);var g=new G();int n=p.Length;
        foreach(float size in new[]{1.035f,.975f})for(int j=0;j<8;j++)for(int i=0;i<n;i++)
        {float a=j*Mathf.PI*2/8;var v=Vector2.Lerp(center,p[i],size);var radial=(v-center).normalized;v+=radial*Mathf.Cos(a)*.65f;g.Add(Point(v,side*(SkinDepth(shell,v,depth)+lift+Mathf.Sin(a)*.65f)));}
        // Two independently closed slender rings supply a deliberate thin bezel.
        for(int k=0;k<2;k++)for(int j=0;j<8;j++)for(int i=0;i<n;i++)
        {int a=k*8*n+j*n+i,b=k*8*n+j*n+(i+1)%n,c=k*8*n+((j+1)%8)*n+(i+1)%n,d=k*8*n+((j+1)%8)*n+i;if(side<0)g.Quad(a,b,c,d);else g.Quad(a,d,c,b);}
        return g.Finish("Slim fitted optical bezel");
    }
    static Vector2[] EyeContour(Vector2 c,float rx,float ry,string kind)
    {
        if(kind=="triangle")return new[]{c+new Vector2(-rx,-ry),c+new Vector2(rx*.9f,ry*.22f),c+new Vector2(-rx*.40f,ry)};
        var p=new Vector2[16];for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/p.Length;float x=Mathf.Cos(a);p[i]=c+new Vector2(x*rx,Mathf.Sin(a)*ry*(kind=="almond"?Mathf.Sqrt(Mathf.Max(0,1-x*x)):1));}return p;
    }
    static Vector2[] BladeOutline(Blade b,float length=1,float width=1)
    {
        Vector2 root=b.root.V,tip=Vector2.Lerp(root,b.tip.V,length),axis=tip-root;
        if(axis.sqrMagnitude<.01f)axis=new Vector2(-1,0);var normal=new Vector2(-axis.y,axis.x).normalized;var points=new List<Vector2>();
        // An asymmetric swept perimeter, rounded root and a narrow machined tip.
        for(int side=-1;side<=1;side+=2)for(int j=0;j<=12;j++)
        {float t=side<0?j/12f:1-j/12f;float w=b.width*width*(.04f+.96f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.65f))*(1-t*.08f);var p=Vector2.Lerp(root,tip,t)+normal*(b.curve*Mathf.Sin(t*Mathf.PI)+side*w);points.Add(p);}
        return points.ToArray();
    }
    static Mesh Feather(Blade b,float z,float depth,int side)
    {
        var mesh=Dome(BladeOutline(b),depth,8,z);var v=mesh.vertices;
        // Feather span fans into real Z depth; each root remains anchored.
        var axis=b.tip.V-b.root.V;float length=Mathf.Max(1,axis.sqrMagnitude);
        for(int i=0;i<v.Length;i++){var native=new Vector2(v[i].x*100+origin.x,origin.y-v[i].y*100);float t=Mathf.Clamp01(Vector2.Dot(native-b.root.V,axis)/length);v[i].z+=side*(12*t*t)*.01f;}
        mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static Mesh FeatherInset(Blade b,float z,int side,float halfDepth=3.1f)
    {
        var inset=new Blade{root=new P{x=Mathf.Lerp(b.root.x,b.tip.x,.18f),y=Mathf.Lerp(b.root.y,b.tip.y,.18f)},tip=new P{x=Mathf.Lerp(b.root.x,b.tip.x,.91f),y=Mathf.Lerp(b.root.y,b.tip.y,.91f)},width=b.width*.55f,curve=b.curve*.65f};
        var mesh=SurfacePanel(BladeOutline(inset),BladeOutline(b),halfDepth,side,.45f,1.5f);var vertices=mesh.vertices;var axis=b.tip.V-b.root.V;float length=Mathf.Max(1,axis.sqrMagnitude);
        for(int i=0;i<vertices.Length;i++){var native=new Vector2(vertices[i].x*100+origin.x,origin.y-vertices[i].y*100);float t=Mathf.Clamp01(Vector2.Dot(native-b.root.V,axis)/length);vertices[i].z+=(z+side*12*t*t)*.01f;}
        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static Mesh Rod(Vector2 from,Vector2 to,float z,float radius)
    {
        var a=Point(from,z);var b=Point(to,z);if(Vector3.Distance(a,b)<.005f)b=a+Vector3.left*.005f;
        var axis=(b-a).normalized;var u=Vector3.Cross(axis,Vector3.forward).normalized;var v=Vector3.Cross(axis,u);var g=new G();const int count=16;
        for(int end=0;end<2;end++)for(int i=0;i<count;i++){float angle=i*Mathf.PI*2/count;g.Add((end==0?a:b)+(u*Mathf.Cos(angle)+v*Mathf.Sin(angle))*radius*.01f);}
        int ca=g.Add(a),cb=g.Add(b);for(int i=0;i<count;i++){int j=(i+1)%count;g.Quad(i,j,j+count,i+count);g.Tri(ca,j,i);g.Tri(cb,i+count,j+count);}return g.Finish("Closed mechanical connector");
    }
    static void BuildJet(SkyPulseNeonFinchPrototype rig,Vector2 hub)
    {
        var direction=(hub-design.tailRoot.V).normalized;if(direction.sqrMagnitude<.01f)direction=Vector2.left;
        Part(rig.tailTip,"Source engine nozzle",Rod(hub-direction*9,hub+direction*7,0,7),4);
        var jet=new GameObject("Live source-colour engine fire",typeof(MeshFilter),typeof(MeshRenderer));jet.transform.SetParent(rig.tailTip,false);jet.transform.position=Point(hub+direction*7);jet.transform.rotation=Quaternion.FromToRotation(Vector3.left,new Vector3(direction.x,-direction.y,0));jet.transform.localScale=Vector3.one*.55f;
        jet.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(SkyPulseBirdJetWorkshop.MeshPath);var renderer=jet.GetComponent<MeshRenderer>();renderer.sharedMaterial=finishes[8];renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;rig.jetRenderer=renderer;
    }
    static Mesh RoundSolid(Vector2 at,float z,float rx,float ry,float depth)
    {var p=new Vector2[16];for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/p.Length;p[i]=at+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry);}return Dome(p,depth,10,z);}
    static Mesh Ring(Vector2 at,float z,float radius,float tube,float halfDepth)
    {
        var g=new G();const int around=40,section=10;
        for(int i=0;i<around;i++)for(int j=0;j<section;j++){float a=i*Mathf.PI*2/around,b=j*Mathf.PI*2/section;g.Add(Point(at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(radius+tube*Mathf.Cos(b)),z+halfDepth*Mathf.Sin(b)));}
        for(int i=0;i<around;i++)for(int j=0;j<section;j++)g.Quad(i*section+j,((i+1)%around)*section+j,((i+1)%around)*section+(j+1)%section,i*section+(j+1)%section);return g.Finish("Machined closed bearing");
    }
    static void Lighting()
    {
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.35f,.39f,.46f);RenderSettings.ambientEquatorColor=new Color(.16f,.19f,.23f);RenderSettings.ambientGroundColor=new Color(.07f,.09f,.12f);
        var reflection=AssetDatabase.LoadAssetAtPath<Cubemap>(SkyPulseBirdStudioLighting.ReflectionPath);if(reflection!=null){RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=reflection;RenderSettings.reflectionIntensity=.85f;}
        foreach(var s in new[]{new Vector4(-1,4,-4,1.05f),new Vector4(3,1,3,.42f),new Vector4(-3,3,4,.85f)})
        {var o=new GameObject("Mechanical studio key",typeof(Light));o.transform.position=new Vector3(s.x,s.y,s.z);o.transform.LookAt(Vector3.zero);var l=o.GetComponent<Light>();l.type=LightType.Directional;l.color=Color.white;l.intensity=s.w;}
    }
    static void CreateScene(Design d)
    {
        if(File.Exists(ScenePath(d)))return;var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(d)),scene);Lighting();
            var camera=new GameObject("Reference model inspection camera",typeof(Camera));camera.tag="MainCamera";camera.transform.position=new Vector3(2.8f,1.3f,-9);camera.transform.LookAt(Vector3.zero);var c=camera.GetComponent<Camera>();c.orthographic=true;c.orthographicSize=4.8f;c.fieldOfView=31;c.backgroundColor=new Color(.018f,.031f,.057f);c.clearFlags=CameraClearFlags.SolidColor;c.nearClipPlane=.05f;
            EditorSceneManager.SaveScene(scene,ScenePath(d));
        }
        finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);}
    }
    static void CreateGallery()
    {
        if(File.Exists(GalleryPath))return;var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            var finch=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SkyPulseBirdPrototypeWorkshop.PrefabPath),scene);finch.transform.position=new Vector3(-10,5,0);finch.GetComponent<SkyPulseNeonFinchPrototype>().turntable=false;
            int i=1;foreach(var d in LoadDesigns())
            {var o=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(d)),scene);o.transform.position=new Vector3((i%5-2)*5,(1-i/5)*5,0);var rig=o.GetComponent<SkyPulseNeonFinchPrototype>();rig.turntable=false;i++;}
            Lighting();var camera=new GameObject("Roster gallery camera",typeof(Camera));camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-30);var c=camera.GetComponent<Camera>();c.orthographic=true;c.orthographicSize=10;c.backgroundColor=new Color(.018f,.031f,.057f);c.clearFlags=CameraClearFlags.SolidColor;EditorSceneManager.SaveScene(scene,GalleryPath);
        }
        finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);}
    }
}
