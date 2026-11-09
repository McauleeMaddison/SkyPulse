using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkyPulse.Mobile;

// Guarded design refresh. Existing scene objects, transforms and mesh GUIDs are
// never recreated. A changed artist mesh is authoritative and is left alone.
public static class SkyPulseDistrictDesignWorkshop
{
    public const string BaselinePath=SkyPulseBackgroundWorkshop.Folder+"/district-design-baseline-v1.json";
    const string MeshFolder=SkyPulseBackgroundWorkshop.Folder+"/Meshes/";
    const int CurrentDesignVersion=5;
    static readonly Type Factory=typeof(SkyPulseNativeGame).Assembly.GetType("SkyPulse.Mobile.SkyPulseWorldArchitecture");
    const BindingFlags Static=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
    static readonly FieldInfo CaptureField=Factory.GetField("EditorPartCapture",Static);
    [Serializable] public sealed class PartRecord { public string path,name,sha256,installedSha256;public bool generated;public Vector3 pivot; }
    [Serializable] public sealed class Baseline { public int version=1,designVersion=1,neonCityRevision;public string sourceCommit="17a8f6de216413ee9af6f2d99a9cbd612ec067a8";public PartRecord[] parts; }
    sealed class Part { public string name;public Mesh mesh;public Vector3 pivot; }
    static int captureDepth,start,piece;static object builder;
    static readonly List<Vector2Int> ranges=new List<Vector2Int>();static readonly List<Part> captured=new List<Part>();
    static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);
    public static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    static void Guard(){if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Use the disposable batch QA project for bulk district design updates.");}
    static void Capture(string name,object source,bool begin)
    {
        builder=source;
        if(begin){if(captureDepth++==0)start=Field<List<int>>(source,"t").Count;}
        else if(--captureDepth==0){int end=Field<List<int>>(source,"t").Count;Add(name,source,start,end);ranges.Add(new Vector2Int(start,end));}
    }
    static void Add(string name,object source,int first,int last)
    {
        if(first>=last)return;
        var oldV=Field<List<Vector3>>(source,"v");var oldN=Field<List<Vector3>>(source,"n");var oldC=Field<List<Color>>(source,"c");var oldT=Field<List<int>>(source,"t");
        var map=new Dictionary<int,int>();var v=new List<Vector3>();var n=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
        for(int i=first;i<last;i++){int old=oldT[i];if(!map.TryGetValue(old,out int index)){index=v.Count;map.Add(old,index);v.Add(oldV[old]);n.Add(oldN[old]);c.Add(oldC[old]);}t.Add(index);}
        var bounds=new Bounds(v[0],Vector3.zero);foreach(var at in v)bounds.Encapsulate(at);
        var pivot=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        var mesh=new Mesh{name=(++piece).ToString("00")+" "+name};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateBounds();
        captured.Add(new Part{name=name,mesh=mesh,pivot=pivot});
    }
    static Part[] Generate(int world,int variant)
    {
        piece=0;captureDepth=0;builder=null;ranges.Clear();captured.Clear();Mesh complete=null;
        CaptureField.SetValue(null,(Action<string,object,bool>)Capture);
        try
        {
            complete=(Mesh)Factory.GetMethod("Build",Static).Invoke(null,new object[]{world,variant});
            if(builder!=null){int from=0;var indices=Field<List<int>>(builder,"t");foreach(var range in ranges){if(range.x>from)Add("Architectural details",builder,from,range.x);from=range.y;}if(from<indices.Count)Add("Architectural details",builder,from,indices.Count);}
            return captured.ToArray();
        }
        finally{CaptureField.SetValue(null,null);if(complete!=null)UnityEngine.Object.DestroyImmediate(complete);}
    }
    static string Prefix(int world,int variant)=>MeshFolder+"world-"+world.ToString("00")+"-"+(variant==0?"civic":"residential");
    public static void CaptureBaseline()
    {
        Guard();if(File.Exists(BaselinePath))throw new InvalidOperationException("The original design baseline already exists; it must not be replaced with a later design.");
        var records=new List<PartRecord>();
        for(int world=0;world<12;world++)for(int variant=0;variant<2;variant++)
        {
            var parts=Generate(world,variant);
            try
            {
                for(int i=0;i<parts.Length;i++)
                {
                    string path=Prefix(world,variant)+"-"+(i+1).ToString("00")+".asset";
                    if(!File.Exists(path)||AssetDatabase.LoadAssetAtPath<Mesh>(path)==null)throw new InvalidOperationException("Missing existing authored mesh "+path);
                    var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);var raw=parts[i].mesh.vertices;for(int v=0;v<raw.Length;v++)raw[v]-=parts[i].pivot;
                    bool generated=existing.vertices.SequenceEqual(raw)&&existing.normals.SequenceEqual(parts[i].mesh.normals)&&existing.colors.SequenceEqual(parts[i].mesh.colors)&&existing.triangles.SequenceEqual(parts[i].mesh.triangles);
                    records.Add(new PartRecord{path=path,name=parts[i].name,sha256=Hash(path),pivot=parts[i].pivot,generated=generated});
                }
            }
            finally{foreach(var part in parts)UnityEngine.Object.DestroyImmediate(part.mesh);}
        }
        File.WriteAllText(BaselinePath,JsonUtility.ToJson(new Baseline{parts=records.ToArray()},true));AssetDatabase.Refresh();
        Debug.Log("SKYPULSE_DISTRICT_BASELINE_PASS: "+records.Count+" exact original mesh hashes and source pivots recorded / "+records.Count(r=>r.generated)+" match the generator; no mesh or layout edited");EditorApplication.Exit(0);
    }
    public const string CloudDensityPath="Assets/Resources/SkyPulse/WorldCloudDensity.asset";
    const string CloudDensityName="SkyPulse coherent Perlin-Worley density / v1";
    // Editor construction only. The player loads one 1 MiB cached Texture3D;
    // none of this Perlin/Worley work runs during launch or a world transition.
    public static Color32[] GenerateCloudDensityPixels()
    {
        const int size=64;
        var pixels=new Color32[size*size*size];
        var coarse=CloudFeatures(6,0x8d29a4e3u);var fine=CloudFeatures(12,0x143d72abu);
        for(int z=0;z<size;z++)for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=(x+.5f)/size,v=(y+.5f)/size,w=(z+.5f)/size;
            float perlin=CloudPerlin(u,v,w,3,0x385a91d7u)*.56f+CloudPerlin(u,v,w,6,0x9a7d35c1u)*.27f+CloudPerlin(u,v,w,12,0x517ea20bu)*.12f+CloudPerlin(u,v,w,24,0x6c79d8efu)*.05f;
            perlin=Mathf.Clamp01((perlin-.5f)*1.55f+.5f);
            float worley6=CloudWorley(u,v,w,6,coarse),worley12=CloudWorley(u,v,w,12,fine);
            float density=perlin*.72f+(worley6*.72f+worley12*.28f)*.28f;
            float curl=CloudPerlin(u,v,w,8,0x37b68d21u);
            pixels[x+size*(y+size*z)]=new Color32(CloudByte(density),CloudByte(worley6),CloudByte(worley12),CloudByte(curl));
        }
        return pixels;
    }
    public static void BakeCloudDensityInQa()
    {
        Guard();var pixels=GenerateCloudDensityPixels();
        var existing=AssetDatabase.LoadAssetAtPath<Texture3D>(CloudDensityPath);
        if(existing!=null)
        {
            // A repeated bake is a no-op only for the exact known generation.
            // Saved artist changes remain authoritative, including pixel edits.
            if(EditorUtility.IsDirty(existing)||existing.name!=CloudDensityName||existing.width!=64||existing.height!=64||existing.depth!=64||existing.format!=TextureFormat.RGBA32||!existing.isReadable||!existing.GetPixels32().SequenceEqual(pixels))
                throw new InvalidOperationException("The saved cloud density differs from this exact generator. It has been preserved: "+CloudDensityPath);
            Debug.Log("SKYPULSE_COHERENT_CLOUD_DENSITY_PASS: exact existing 64 cubed RGBA density preserved / 1048576 bytes.");return;
        }
        if(File.Exists(CloudDensityPath))throw new InvalidOperationException("An existing non-texture cloud asset was preserved: "+CloudDensityPath);
        var volume=new Texture3D(64,64,64,TextureFormat.RGBA32,false){name=CloudDensityName,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=0};
        volume.SetPixels32(pixels);volume.Apply(false,false);AssetDatabase.CreateAsset(volume,CloudDensityPath);AssetDatabase.SaveAssets();
        Debug.Log("SKYPULSE_COHERENT_CLOUD_DENSITY_PASS: deterministic periodic 64 cubed RGBA density / R Perlin-Worley FBM, G 6-cell erosion, B 12-cell erosion, A 8-cell curl / 1048576 bytes.");
    }
    static byte CloudByte(float value)=>(byte)Mathf.Clamp(Mathf.RoundToInt(value*255),0,255);
    static int CloudWrap(int value,int size){int wrapped=value%size;return wrapped<0?wrapped+size:wrapped;}
    static uint CloudHash(int x,int y,int z,uint seed)
    {
        uint h=(uint)x*0x8da6b343u^(uint)y*0xd8163841u^(uint)z*0xcb1ab31fu^seed;
        h^=h>>16;h*=0x7feb352du;h^=h>>15;h*=0x846ca68bu;return h^(h>>16);
    }
    static float CloudUnit(uint value)=>(value&0x00ffffffu)/16777216f;
    static Vector3[] CloudFeatures(int cells,uint seed)
    {
        var features=new Vector3[cells*cells*cells];
        for(int z=0;z<cells;z++)for(int y=0;y<cells;y++)for(int x=0;x<cells;x++)
        {
            uint h=CloudHash(x,y,z,seed);
            features[x+cells*(y+cells*z)]=new Vector3(.08f+.84f*CloudUnit(h),.08f+.84f*CloudUnit(CloudHash(x,y,z,h^0x6395a7d1u)),.08f+.84f*CloudUnit(CloudHash(x,y,z,h^0xb3d8c91fu)));
        }
        return features;
    }
    static float CloudWorley(float u,float v,float w,int cells,Vector3[] features)
    {
        float x=u*cells,y=v*cells,z=w*cells;int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y),iz=Mathf.FloorToInt(z);float nearest=4;
        for(int dz=-1;dz<=1;dz++)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
        {
            int cx=ix+dx,cy=iy+dy,cz=iz+dz;
            var feature=features[CloudWrap(cx,cells)+cells*(CloudWrap(cy,cells)+cells*CloudWrap(cz,cells))];
            float a=cx+feature.x-x,b=cy+feature.y-y,c=cz+feature.z-z;nearest=Mathf.Min(nearest,a*a+b*b+c*c);
        }
        return Mathf.Clamp01(1-Mathf.Sqrt(nearest)*.95f);
    }
    static float CloudFade(float value)=>value*value*value*(value*(value*6-15)+10);
    static float CloudGradient(int x,int y,int z,float dx,float dy,float dz,int cells,uint seed)
    {
        int h=(int)(CloudHash(CloudWrap(x,cells),CloudWrap(y,cells),CloudWrap(z,cells),seed)&15);
        float u=h<8?dx:dy,v=h<4?dy:h==12||h==14?dx:dz;
        return ((h&1)==0?u:-u)+((h&2)==0?v:-v);
    }
    static float CloudPerlin(float u,float v,float w,int cells,uint seed)
    {
        float x=u*cells,y=v*cells,z=w*cells;int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y),iz=Mathf.FloorToInt(z);
        x-=ix;y-=iy;z-=iz;float a=CloudFade(x),b=CloudFade(y),c=CloudFade(z);
        float front=Mathf.LerpUnclamped(Mathf.LerpUnclamped(CloudGradient(ix,iy,iz,x,y,z,cells,seed),CloudGradient(ix+1,iy,iz,x-1,y,z,cells,seed),a),Mathf.LerpUnclamped(CloudGradient(ix,iy+1,iz,x,y-1,z,cells,seed),CloudGradient(ix+1,iy+1,iz,x-1,y-1,z,cells,seed),a),b);
        float back=Mathf.LerpUnclamped(Mathf.LerpUnclamped(CloudGradient(ix,iy,iz+1,x,y,z-1,cells,seed),CloudGradient(ix+1,iy,iz+1,x-1,y,z-1,cells,seed),a),Mathf.LerpUnclamped(CloudGradient(ix,iy+1,iz+1,x,y-1,z-1,cells,seed),CloudGradient(ix+1,iy+1,iz+1,x-1,y-1,z-1,cells,seed),a),b);
        return Mathf.Clamp01(.5f+.5f*Mathf.LerpUnclamped(front,back,c));
    }
    public static bool MayRefresh(PartRecord part,string currentHash)
        => part.generated&&(currentHash==part.sha256||(!string.IsNullOrEmpty(part.installedSha256)&&currentHash==part.installedSha256));
    [MenuItem("Tools/SkyPulse/Background Workshop/Refine original district structures")]
    public static void RefreshKnownStructures()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before refreshing the editable districts.");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        Upgrade();
    }
    public static void UpgradeInQa(){Guard();Upgrade();}
    public static string ScenePath(int world,int variant)=>SkyPulseBackgroundWorkshop.Folder+"/Scenes/"+(world+1).ToString("00")+" "+Worlds[world]+(variant==0?" Civic":" Residential")+".unity";
    static readonly string[] Worlds={"Neon City","Aurora Rise","Solar Drift","Midnight Tide","Velvet Dawn","Crystal Night","Jade Horizon","Violet Rain","Eclipse","Cobalt Storm","Amber Skies","Polar Glow"};
    public static void UpgradeNeonCityInQa()
    {
        Guard();
        var baseline=JsonUtility.FromJson<Baseline>(File.ReadAllText(BaselinePath));
        if(baseline.version!=1)throw new InvalidOperationException("Unsupported source district baseline.");
        var records=baseline.parts.ToDictionary(p=>p.path);int changed=0,custom=0;
        for(int variant=0;variant<2;variant++)
        {
            var parts=Generate(0,variant);
            try
            {
                if(parts.Length!=8)throw new InvalidOperationException("Neon City must keep all eight original editable part identities.");
                for(int i=0;i<parts.Length;i++)
                {
                    string path=Prefix(0,variant)+"-"+(i+1).ToString("00")+".asset";
                    if(!records.TryGetValue(path,out var record)||record.name!=parts[i].name||!File.Exists(path)||!MayRefresh(record,Hash(path))){custom++;continue;}
                    var target=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(target==null||EditorUtility.IsDirty(target)){custom++;continue;}
                    var candidate=parts[i].mesh;candidate.name=Path.GetFileNameWithoutExtension(path);
                    var vertices=candidate.vertices;for(int n=0;n<vertices.Length;n++)vertices[n]-=record.pivot;
                    candidate.vertices=vertices;candidate.RecalculateBounds();
                    bool same=target.vertices.SequenceEqual(candidate.vertices)&&target.normals.SequenceEqual(candidate.normals)&&target.colors.SequenceEqual(candidate.colors)&&target.triangles.SequenceEqual(candidate.triangles);
                    if(same)continue;
                    EditorUtility.CopySerialized(candidate,target);EditorUtility.SetDirty(target);
                    target.vertices=target.vertices;target.normals=target.normals;target.colors=target.colors;
                    for(int sub=0;sub<target.subMeshCount;sub++)target.SetIndices(target.GetIndices(sub),target.GetTopology(sub),sub,false);
                    target.UploadMeshData(false);AssetDatabase.SaveAssetIfDirty(target);record.installedSha256=Hash(path);changed++;
                }
            }
            finally{foreach(var part in parts)UnityEngine.Object.DestroyImmediate(part.mesh);}
        }
        if(changed>0){baseline.neonCityRevision++;File.WriteAllText(BaselinePath,JsonUtility.ToJson(baseline,true));AssetDatabase.ImportAsset(BaselinePath,ImportAssetOptions.ForceUpdate);}
        for(int variant=0;variant<2;variant++)
        {
            string path=ScenePath(0,variant);var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try{var root=scene.GetRootGameObjects().Select(g=>g.GetComponent<SkyPulseBackgroundAuthoring>()).Single(a=>a!=null);SkyPulseBackgroundWorkshop.Bake(root);}
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        Debug.Log("SKYPULSE_NEON_CONNECTED_CITY_CANDIDATE: "+changed+" known-owned parts revised; "+custom+" custom parts preserved. Only Neon City civic/residential bakes touched; scene objects, original pivots and asset GUIDs retained.");
    }
    static void Upgrade()
    {
        if(!File.Exists(BaselinePath))throw new InvalidOperationException("The guarded original-mesh baseline is required. No district has been changed.");
        var baseline=JsonUtility.FromJson<Baseline>(File.ReadAllText(BaselinePath));if(baseline.version!=1)throw new InvalidOperationException("Unsupported district baseline.");
        var index=baseline.parts.ToDictionary(p=>p.path);var replaced=new HashSet<string>();int changed=0,custom=0,unchanged=0;
        for(int world=0;world<12;world++)for(int variant=0;variant<2;variant++)
        {
            var parts=Generate(world,variant);
            try
            {
                for(int i=0;i<parts.Length;i++)
                {
                    string path=Prefix(world,variant)+"-"+(i+1).ToString("00")+".asset";
                    if(!index.TryGetValue(path,out var record)||record.name!=parts[i].name||!File.Exists(path)){custom++;continue;}
                    string current=Hash(path);
                    if(!MayRefresh(record,current)){custom++;continue;}
                    if(baseline.designVersion>=CurrentDesignVersion&&record.installedSha256==current){unchanged++;continue;}
                    var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing==null||EditorUtility.IsDirty(existing)){custom++;continue;}
                    var replacement=parts[i].mesh;replacement.name=Path.GetFileNameWithoutExtension(path);
                    var vertices=replacement.vertices;for(int v=0;v<vertices.Length;v++)vertices[v]-=record.pivot;
                    replacement.vertices=vertices;replacement.RecalculateBounds();
                    if(!Application.isBatchMode)Undo.RecordObject(existing,"Refine original district structure");
                    EditorUtility.CopySerialized(replacement,existing);EditorUtility.SetDirty(existing);
                    // A mesh copied in the Editor can retain the previous GPU
                    // vertex buffer until fresh upload; keep same-process previews honest.
                    existing.vertices=existing.vertices;existing.normals=existing.normals;existing.colors=existing.colors;
                    for(int sub=0;sub<existing.subMeshCount;sub++)existing.SetIndices(existing.GetIndices(sub),existing.GetTopology(sub),sub,false);
                    existing.UploadMeshData(false);AssetDatabase.SaveAssetIfDirty(existing);replaced.Add(path);changed++;
                }
            }
            finally{foreach(var part in parts)UnityEngine.Object.DestroyImmediate(part.mesh);}
        }
        foreach(var part in baseline.parts)if(replaced.Contains(part.path))part.installedSha256=Hash(part.path);
        baseline.designVersion=CurrentDesignVersion;
        File.WriteAllText(BaselinePath,JsonUtility.ToJson(baseline,true));AssetDatabase.Refresh();
        for(int world=0;world<12;world++)for(int variant=0;variant<2;variant++)
        {
            string path=ScenePath(world,variant);var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var root=scene.GetRootGameObjects().Select(g=>g.GetComponent<SkyPulseBackgroundAuthoring>()).Single(a=>a!=null);
                SkyPulseBackgroundWorkshop.Bake(root);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        AssetDatabase.SaveAssets();
        Debug.Log("SKYPULSE_DISTRICT_DESIGN_UPDATED: "+changed+" original generated parts refined; "+custom+" custom/missing parts retained; "+unchanged+" already installed parts retained. Existing scene/prefab layouts and mesh GUIDs preserved.");
    }
}
