using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using SkyPulse.Mobile;

// Chrome Raven's own plate layout, measured in the 512x384 reference frame.
// This is deliberately separate from the generic roster studies: no Finch or
// other bird is regenerated when the Raven is revised.
public static partial class SkyPulseBirdRosterWorkshop
{
    const string RavenRevision="Chrome Raven · reference plated sculpture v2";
    static readonly string[] RavenFiles={"Geometry.asset","Finishes.asset","Chrome Raven Rig.prefab","Chrome Raven Review.unity"};
    static readonly string[] RavenOriginalHashes={
        "f9f431c58e157189992853a5cbb653e53618d8211e2781e2bd722f892bb1398b",
        "3ca2a012b5faa4be51f7158402684646a60a16ac185ccdfcff7b824c6d551cd4",
        "1b8617a7696dbe1b8b2b547fd91dff4203815dad61f50457ce434e58c0cac5e4",
        "e60f564ce9e950a2d706b8a3aad24aa46297b27fc4a19ff72ef9eb8a3e358dfd"};
    static string RavenHash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    [MenuItem("Tools/SkyPulse/Bird Workshop/Revise Chrome Raven reference sculpture")]
    public static void ReviseChromeRaven()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before revising the Raven study.");
        var d=LoadDesigns().Single(b=>b.id=="chrome_raven");ValidateDesign(d);
        string folder=BirdFolder(d),prefabPath=PrefabPath(d),geometryPath=folder+"/Geometry.asset";
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        // A repeat never overwrites a saved revised sculpture or owner edits.
        var savedRig=existing!=null?existing.GetComponent<SkyPulseNeonFinchPrototype>():null;
        // Unity imports the main prefab object under its filename. Identify the
        // revised structure by its authored joints, not that importer label.
        if(savedRig!=null&&savedRig.body!=null&&savedRig.head!=null&&
            savedRig.body.name=="01 Sectioned fuselage and flight bank"&&
            savedRig.head.name=="02 Interlocking helmet and inset sensor"&&
            savedRig.head.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.name=="Closed reference helmet"))
        {Debug.Log("SKYPULSE_RAVEN_REVISION_EXISTS: saved sculpture and owner edits preserved.");return;}
        void Guard()
        {
            for(int i=0;i<RavenFiles.Length;i++)
            {
                string path=folder+"/"+RavenFiles[i];
                if(!File.Exists(path)||RavenHash(path)!=RavenOriginalHashes[i])throw new InvalidOperationException("Raven revision preserved an owner-edited or unknown asset: "+path);
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset!=null&&EditorUtility.IsDirty(asset))throw new InvalidOperationException("Save or discard unsaved Raven edits before revising: "+path);
            }
        }
        Guard();
        var backups=RavenFiles.ToDictionary(f=>f,f=>File.ReadAllBytes(folder+"/"+f));
        var meta=RavenFiles.ToDictionary(f=>f,f=>File.ReadAllBytes(folder+"/"+f+".meta"));
        string staging="Assets/Editor/__SkyPulseRavenRevision";
        if(Directory.Exists(staging))throw new InvalidOperationException("An unfinished Raven staging folder is preserved: "+staging);
        Directory.CreateDirectory(staging);AssetDatabase.Refresh();
        GameObject root=null;bool promoted=false;
        try
        {
            design=d;origin=Mean(d.body);directory=folder;meshes.Clear();finishes=RavenExistingFinishes(d,folder);
            root=new GameObject(RavenRevision);var rig=root.AddComponent<SkyPulseNeonFinchPrototype>();
            rig.animate=false;rig.turntable=false;BuildRavenSculpt(rig);
            string tempGeometry=staging+"/Geometry.asset",tempPrefab=staging+"/Chrome Raven Rig.prefab";
            for(int i=0;i<meshes.Count;i++)
            {
                meshes[i].name=(i+1).ToString("000")+" "+meshes[i].name;
                if(i==0)AssetDatabase.CreateAsset(meshes[i],tempGeometry);else AssetDatabase.AddObjectToAsset(meshes[i],tempGeometry);
            }
            AssetDatabase.SaveAssetIfDirty(meshes[0]);rig.CaptureBindPose();rig.animate=true;rig.turntable=true;
            PrefabUtility.SaveAsPrefabAsset(root,tempPrefab);
            string from=AssetDatabase.AssetPathToGUID(tempGeometry),to=AssetDatabase.AssetPathToGUID(geometryPath);
            string prefab=RavenPreserveBindings(existing,AssetDatabase.LoadAssetAtPath<GameObject>(tempPrefab),File.ReadAllText(tempPrefab));
            prefab=prefab.Replace("guid: "+from,"guid: "+to);
            if(prefab.Contains("guid: "+from)||!prefab.Contains("guid: "+to))throw new InvalidOperationException("Raven mesh reference remapping failed.");
            Guard();
            // Replace serialized buffers, retaining the four existing .meta
            // files and prefab GUID used by the review scene and gallery.
            promoted=true;File.Copy(tempGeometry,geometryPath,true);File.WriteAllText(prefabPath,prefab);
            AssetDatabase.ImportAsset(geometryPath,ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(prefabPath,ImportAssetOptions.ForceUpdate);
            RavenApplyFinishes(finishes);
            for(int i=0;i<RavenFiles.Length;i++)if(!File.ReadAllBytes(folder+"/"+RavenFiles[i]+".meta").SequenceEqual(meta[RavenFiles[i]]))throw new InvalidOperationException("Raven revision changed an asset identity.");
            Debug.Log("SKYPULSE_RAVEN_SCULPT_REVISED: Raven only; native polygon blades, layered armour, supported optics, connected talons; original source and all asset identities retained.");
        }
        catch
        {
            if(promoted){foreach(var pair in backups)File.WriteAllBytes(folder+"/"+pair.Key,pair.Value);AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);}
            throw;
        }
        finally
        {
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.DeleteAsset(staging);meshes.Clear();AssetDatabase.Refresh();
        }
    }
    static Material[] RavenExistingFinishes(Design d,string folder)
    {
        var all=AssetDatabase.LoadAllAssetsAtPath(folder+"/Finishes.asset").OfType<Material>().ToArray();
        string[] names={"Armour","Feather enamel","Recessed chassis","Ivory alloy","Machined trim","Optical iris","Luminous inlay","Fitted optical glass"};
        var result=new Material[names.Length];
        // Unity names a main sub-asset after its filename on import.
        result[0]=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Finishes.asset");
        for(int i=1;i<result.Length;i++)result[i]=all.Single(m=>m.name==d.name+" · "+names[i]);
        return result;
    }
    static string RavenPreserveBindings(GameObject original,GameObject staged,string yaml)
    {
        if(original==null||staged==null)throw new InvalidOperationException("Raven prefab identities are missing.");
        var from=staged.GetComponent<SkyPulseNeonFinchPrototype>();var to=original.GetComponent<SkyPulseNeonFinchPrototype>();
        if(from==null||to==null)throw new InvalidOperationException("Raven rig identity is missing.");
        var map=new Dictionary<long,long>();
        long Id(UnityEngine.Object asset)
        {if(asset==null||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long id))throw new InvalidOperationException("Cannot read a Raven local file identity.");return id;}
        void Pair(UnityEngine.Object a,UnityEngine.Object b)
        {long first=Id(a),last=Id(b);if(map.TryGetValue(first,out long found)&&found!=last)throw new InvalidOperationException("Conflicting Raven identity mapping.");map[first]=last;}
        Pair(staged,original);Pair(staged.transform,original.transform);Pair(from,to);
        Transform[] first={from.body,from.head,from.leftWing,from.rightWing,from.leftWingTip,from.rightWingTip,from.tail,from.tailTip};
        Transform[] last={to.body,to.head,to.leftWing,to.rightWing,to.leftWingTip,to.rightWingTip,to.tail,to.tailTip};
        for(int i=0;i<first.Length;i++){Pair(first[i],last[i]);Pair(first[i].gameObject,last[i].gameObject);}
        if(map.Count!=19||map.Values.Distinct().Count()!=19)throw new InvalidOperationException("Raven root, rig and eight joints must have distinct preserved identities.");
        var declarations=Regex.Matches(yaml,@"(?m)^--- !u!\d+ &(-?\d+)$").Cast<Match>().Select(m=>long.Parse(m.Groups[1].Value)).ToArray();
        if(map.Values.Any(id=>declarations.Contains(id)&&!map.ContainsKey(id)))throw new InvalidOperationException("A Raven preserved identity collides with a new part.");
        string guidOriginal=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));
        // Existing review/gallery placement and motion overrides must continue
        // targeting the same root, rig and joint local IDs. Unexpected child
        // overrides are preserved by refusing this explicit replacement.
        foreach(string scene in new[]{ScenePath(design),GalleryPath})
        {
            if(!File.Exists(scene))continue;
            var references=Regex.Matches(File.ReadAllText(scene),@"fileID: (-?\d+), guid: "+guidOriginal+@"(?:,|})");
            foreach(Match match in references)
            {long id=long.Parse(match.Groups[1].Value);if(id!=0&&id!=100100000&&!map.Values.Contains(id))throw new InvalidOperationException("Saved Raven scene contains an unsupported part override; preserved: "+scene);}
        }
        yaml=Regex.Replace(yaml,@"(?m)^(--- !u!\d+ &)(-?\d+)$",m=>m.Groups[1].Value+(map.TryGetValue(long.Parse(m.Groups[2].Value),out long id)?id:long.Parse(m.Groups[2].Value)));
        // Only local references are remapped. Mesh/material external file IDs
        // retain their independently authored sub-asset identities.
        yaml=Regex.Replace(yaml,@"(\{fileID: )(-?\d+)(\})",m=>m.Groups[1].Value+(map.TryGetValue(long.Parse(m.Groups[2].Value),out long id)?id:long.Parse(m.Groups[2].Value))+m.Groups[3].Value);
        var remapped=Regex.Matches(yaml,@"(?m)^--- !u!\d+ &(-?\d+)$").Cast<Match>().Select(m=>long.Parse(m.Groups[1].Value)).ToArray();
        if(remapped.Distinct().Count()!=declarations.Length||map.Values.Any(id=>!remapped.Contains(id)))throw new InvalidOperationException("Raven local identity remapping failed.");
        return yaml;
    }
    static void RavenApplyFinishes(Material[] materials)
    {
        // Palette values remain unchanged. Different machined surfaces catch
        // the studio light without using an all-over emissive silver coating.
        for(int i=0;i<materials.Length;i++)
        {
            var m=materials[i];m.SetFloat("_Metallic",i==3||i==4?.66f:i==2?.24f:i==7?.08f:.38f);
            m.SetFloat("_Glossiness",i==2?.30f:i==7?.59f:i==3||i==4?.48f:.42f);
            m.SetFloat("_EmissionStrength",i==5?.65f:i==6?.70f:0);
            EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);
        }
    }
    struct RvSection
    {
        public float x,top,bottom,depth;
        public RvSection(float x,float top,float bottom,float depth){this.x=x;this.top=top;this.bottom=bottom;this.depth=depth;}
    }
    static readonly RvSection[] RavenBody={
        new RvSection(190,223,250,1),new RvSection(214,200,270,17),new RvSection(244,181,284,26),
        new RvSection(275,172,293,32),new RvSection(305,175,293,35),new RvSection(329,185,288,32),
        new RvSection(350,200,276,25),new RvSection(366,216,258,16),new RvSection(376,233,242,1)};
    static readonly RvSection[] RavenHead={
        new RvSection(329,142,182,1),new RvSection(341,144,204,15),new RvSection(358,149,219,23),
        new RvSection(379,156,227,28),new RvSection(399,168,225,25),new RvSection(414,179,215,17),
        new RvSection(423,190,207,1)};
    static RvSection RvAt(RvSection[] list,float x)
    {
        int i=1;while(i<list.Length-1&&x>list[i].x)i++;
        var a=list[i-1];var b=list[i];float t=Mathf.Clamp01((x-a.x)/(b.x-a.x));t=t*t*(3-2*t);
        return new RvSection(x,Mathf.Lerp(a.top,b.top,t),Mathf.Lerp(a.bottom,b.bottom,t),Mathf.Lerp(a.depth,b.depth,t));
    }
    static float RvSkin(RvSection[] list,Vector2 p)
    {
        var q=RvAt(list,p.x);float center=Mathf.Lerp(q.top,q.bottom,.49f);
        float height=p.y<center?center-q.top:q.bottom-center;
        float r=Mathf.Clamp01(Mathf.Abs(p.y-center)/Mathf.Max(1,height));
        return q.depth*Mathf.Pow(Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(r,2f/.82f))),.94f);
    }
    static Mesh RvHull(RvSection[] list)
    {
        var g=new G();const int slices=40,around=24;
        for(int row=0;row<=slices;row++)
        {
            float x=Mathf.Lerp(list[0].x,list[list.Length-1].x,row/(float)slices);var q=RvAt(list,x);float center=Mathf.Lerp(q.top,q.bottom,.49f);
            for(int i=0;i<around;i++)
            {
                float a=i*Mathf.PI*2/around,c=Mathf.Cos(a),s=Mathf.Sin(a);float h=c<0?center-q.top:q.bottom-center;
                float y=center+Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),.82f)*h;
                float z=Mathf.Sign(s)*Mathf.Pow(Mathf.Abs(s),.94f)*q.depth;
                g.Add(Point(new Vector2(x,y),z),new Vector2(row/(float)slices,i/(float)around));
            }
        }
        for(int row=0;row<slices;row++)for(int i=0;i<around;i++){int j=(i+1)%around;g.Quad(row*around+i,row*around+j,(row+1)*around+j,(row+1)*around+i);}
        int start=g.Add(Point(new Vector2(list[0].x,(list[0].top+list[0].bottom)*.5f))),end=g.Add(Point(new Vector2(list[list.Length-1].x,(list[list.Length-1].top+list[list.Length-1].bottom)*.5f)));
        for(int i=0;i<around;i++){int j=(i+1)%around;g.Tri(start,j,i);g.Tri(end,slices*around+i,slices*around+j);}
        return g.Finish("Sectioned closed raven armour shell");
    }
    static Vector2[] RvPoints(params float[] xy){var p=new Vector2[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Vector2(xy[i*2],xy[i*2+1]);return p;}
    static Vector2[] RvInset(Vector2[] polygon,float inset)
    {
        var c=Center(polygon);var result=new Vector2[polygon.Length];
        for(int i=0;i<result.Length;i++){var v=polygon[i]-c;float length=v.magnitude;result[i]=c+v*(Mathf.Max(0,length-inset)/Mathf.Max(.001f,length));}return result;
    }
    static float RvCross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static List<int> RvTriangles(Vector2[] polygon)
    {
        var ids=Enumerable.Range(0,polygon.Length).ToList();float area=0;for(int i=0;i<polygon.Length;i++)area+=RvCross(polygon[i],polygon[(i+1)%polygon.Length]);
        if(area<0)ids.Reverse();var result=new List<int>();int guard=0;
        while(ids.Count>3&&guard++<polygon.Length*polygon.Length)
        {
            bool cut=false;
            for(int k=0;k<ids.Count;k++)
            {
                int a=ids[(k+ids.Count-1)%ids.Count],b=ids[k],c=ids[(k+1)%ids.Count];var pa=polygon[a];var pb=polygon[b];var pc=polygon[c];
                if(RvCross(pb-pa,pc-pb)<=.0001f)continue;
                bool contains=false;
                foreach(int point in ids)
                {if(point==a||point==b||point==c)continue;var p=polygon[point];if(RvCross(pb-pa,p-pa)>.0001f&&RvCross(pc-pb,p-pb)>.0001f&&RvCross(pa-pc,p-pc)>.0001f){contains=true;break;}}
                if(contains)continue;result.Add(a);result.Add(b);result.Add(c);ids.RemoveAt(k);cut=true;break;
            }
            if(!cut)throw new InvalidOperationException("Invalid traced Raven plate polygon.");
        }
        if(ids.Count==3)result.AddRange(ids);return result;
    }
    // Closed bevel plate. The cap and all four bevel rings share the same
    // adaptively split perimeter, so a fitted curved face has no T-junctions.
    static Mesh RvSolid(Vector2[] polygon,Func<Vector2,float> depth,int side,float halfThickness=1.05f,float bevel=.65f,float edgeLimit=16)
    {
        float area=0;for(int i=0;i<polygon.Length;i++)area+=RvCross(polygon[i],polygon[(i+1)%polygon.Length]);
        if(area<0)polygon=Enumerable.Reverse(polygon).ToArray();
        var inner=RvInset(polygon,bevel);var points=inner.ToList();var triangles=RvTriangles(inner);
        var perimeter=Enumerable.Range(0,inner.Length).ToList();
        var outer=Enumerable.Range(0,inner.Length).ToDictionary(i=>i,i=>polygon[i]);
        float lengthLimit=edgeLimit*edgeLimit;
        for(int iteration=0;iteration<8192;iteration++)
        {
            int first=-1,last=-1;float longest=lengthLimit;
            for(int t=0;t<triangles.Count;t+=3)for(int k=0;k<3;k++)
            {
                int a=triangles[t+k],b=triangles[t+(k+1)%3];float length=(points[a]-points[b]).sqrMagnitude;
                if(length>longest){longest=length;first=a;last=b;}
            }
            if(first<0)break;
            if(iteration==8191)throw new InvalidOperationException("Raven plate subdivision budget exceeded.");
            int middle=points.Count;points.Add((points[first]+points[last])*.5f);
            int count=triangles.Count;
            for(int t=0;t<count;t+=3)
            {
                for(int k=0;k<3;k++)
                {
                    int a=triangles[t+k],b=triangles[t+(k+1)%3];
                    if(!((a==first&&b==last)||(a==last&&b==first)))continue;
                    int c=triangles[t+(k+2)%3];
                    triangles[t]=a;triangles[t+1]=middle;triangles[t+2]=c;
                    triangles.Add(middle);triangles.Add(b);triangles.Add(c);break;
                }
            }
            for(int p=0;p<perimeter.Count;p++)
            {
                int a=perimeter[p],b=perimeter[(p+1)%perimeter.Count];
                if(!((a==first&&b==last)||(a==last&&b==first)))continue;
                outer.Add(middle,(outer[a]+outer[b])*.5f);perimeter.Insert(p+1,middle);break;
            }
        }
        var g=new G();int[] fronts=new int[points.Count],backs=new int[points.Count];
        for(int face=0;face<2;face++)for(int i=0;i<points.Count;i++)
        {
            var p=points[i];int id=g.Add(Point(p,side*(depth(p)+(face==0?halfThickness:-halfThickness))),p*.01f);
            if(face==0)fronts[i]=id;else backs[i]=id;
        }
        for(int t=0;t<triangles.Count;t+=3)
        {
            int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
            if(side<0){g.Tri(fronts[a],fronts[b],fronts[c]);g.Tri(backs[a],backs[c],backs[b]);}
            else{g.Tri(fronts[a],fronts[c],fronts[b]);g.Tri(backs[a],backs[b],backs[c]);}
        }
        int n=perimeter.Count;var rings=new int[4][];
        for(int r=0;r<4;r++)
        {
            rings[r]=new int[n];
            for(int i=0;i<n;i++)
            {
                int point=perimeter[i];
                if(r==0){rings[r][i]=fronts[point];continue;}
                if(r==3){rings[r][i]=backs[point];continue;}
                var p=outer[point];rings[r][i]=g.Add(Point(p,side*(depth(p)+(r==1?halfThickness*.42f:-halfThickness*.42f))),p*.01f);
            }
        }
        for(int r=0;r<3;r++)for(int i=0;i<n;i++)
        {int a=rings[r][i],b=rings[r][(i+1)%n],c=rings[r+1][(i+1)%n],d=rings[r+1][i];if(side>0)g.Quad(a,b,c,d);else g.Quad(a,d,c,b);}
        return g.Finish("Conformal closed bevelled mechanical plate");
    }
    struct RvSupportTriangle
    {
        public Vector2 a,b,c;public float az,bz,cz,den;
    }
    static readonly Dictionary<(Mesh,int),Func<Vector2,float>> RavenSupports=new Dictionary<(Mesh,int),Func<Vector2,float>>();
    static Func<Vector2,float> RvSupport(Mesh mesh,Vector3 offset,int side)
    {
        var cacheKey=(mesh,side);
        if(RavenSupports.TryGetValue(cacheKey,out var prior))return prior;
        var vertices=mesh.vertices;var indices=mesh.triangles;
        var bins=new Dictionary<Vector2Int,List<RvSupportTriangle>>();const float cell=8;
        Vector2 Native(Vector3 p)=>new Vector2(p.x*100+origin.x,origin.y-p.y*100);
        for(int i=0;i<indices.Length;i+=3)
        {
            var av=vertices[indices[i]]+offset;var bv=vertices[indices[i+1]]+offset;var cv=vertices[indices[i+2]]+offset;
            var t=new RvSupportTriangle{a=Native(av),b=Native(bv),c=Native(cv),az=side*av.z*100,bz=side*bv.z*100,cz=side*cv.z*100};
            t.den=(t.b.y-t.c.y)*(t.a.x-t.c.x)+(t.c.x-t.b.x)*(t.a.y-t.c.y);if(Mathf.Abs(t.den)<1e-7f)continue;
            int minX=Mathf.FloorToInt(Mathf.Min(t.a.x,Mathf.Min(t.b.x,t.c.x))/cell),maxX=Mathf.FloorToInt(Mathf.Max(t.a.x,Mathf.Max(t.b.x,t.c.x))/cell);
            int minY=Mathf.FloorToInt(Mathf.Min(t.a.y,Mathf.Min(t.b.y,t.c.y))/cell),maxY=Mathf.FloorToInt(Mathf.Max(t.a.y,Mathf.Max(t.b.y,t.c.y))/cell);
            for(int x=minX;x<=maxX;x++)for(int y=minY;y<=maxY;y++)
            {var key=new Vector2Int(x,y);if(!bins.TryGetValue(key,out var list)){list=new List<RvSupportTriangle>();bins.Add(key,list);}list.Add(t);}
        }
        float Sample(Vector2 p)
        {
            if(!bins.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x/cell),Mathf.FloorToInt(p.y/cell)),out var list))return 0;
            float height=float.NegativeInfinity;
            foreach(var t in list)
            {
                float u=((t.b.y-t.c.y)*(p.x-t.c.x)+(t.c.x-t.b.x)*(p.y-t.c.y))/t.den;
                float v=((t.c.y-t.a.y)*(p.x-t.c.x)+(t.a.x-t.c.x)*(p.y-t.c.y))/t.den,w=1-u-v;
                if(u>=-.00001f&&v>=-.00001f&&w>=-.00001f)height=Mathf.Max(height,u*t.az+v*t.bz+w*t.cz);
            }
            return float.IsNegativeInfinity(height)?0:height;
        }
        Func<Vector2,float> sample=Sample;RavenSupports.Add(cacheKey,sample);return sample;
    }
    static void RvPlate(Transform parent,string name,Vector2[] p,RvSection[] surface,int side,int finish,float lift=1.3f,float halfThickness=1)
    {
        string shellName=surface==RavenBody?"Closed tapered load-bearing fuselage":"Closed reference helmet";
        var shell=parent.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name==shellName);
        var skin=RvSupport(shell.sharedMesh,parent.position,side);
        if(name.StartsWith("Keel energy cell ",StringComparison.Ordinal))
        {
            // A light slot must follow the already-installed sternum face.
            // Sampling only the base hull made the old LED coplanar with that
            // shield and produced fragmented cyan triangles in the render.
            var support=parent.GetComponentsInChildren<MeshFilter>(true)
                .Where(f=>f.sharedMesh!=null&&(f.name==shellName||f.name.StartsWith("Lower sternum armoured segment "+side,StringComparison.Ordinal)||f.name.StartsWith("Silver chest shield "+side,StringComparison.Ordinal)))
                .Select(f=>RvSupport(f.sharedMesh,parent.position,side)).ToArray();
            float Assembly(Vector2 point){float z=0;foreach(var sample in support)z=Mathf.Max(z,sample(point));return z;}
            var slot=RvSolid(p,q=>Assembly(q)-.3f,side,.75f,.6f,3);
            Part(parent,name+" graphite gasket",slot,2);
            var slotSkin=RvSupport(slot,parent.position,side);
            Part(parent,name+" shaped metal face",RvSolid(RvInset(p,.85f),q=>slotSkin(q)+halfThickness*.45f,side,halfThickness,.35f,3),finish);
            return;
        }
        Part(parent,name+" graphite gasket",RvSolid(p,q=>skin(q)+lift-.9f,side,.75f,.6f,6),2);
        Part(parent,name+" shaped metal face",RvSolid(RvInset(p,.85f),q=>skin(q)+lift+.35f,side,halfThickness,.45f,6),finish);
    }
    static void BuildRavenSculpt(SkyPulseNeonFinchPrototype rig)
    {
        RavenSupports.Clear();
        rig.body=Joint(rig.transform,"01 Sectioned fuselage and flight bank",Vector3.zero);
        rig.head=Joint(rig.body,"02 Interlocking helmet and inset sensor",Point(new Vector2(376,192)));
        rig.tail=Joint(rig.body,"05 Connected tail power spine",Point(new Vector2(216,250)));
        rig.tailTip=Joint(rig.tail,"06 Four machined tail blades",Point(new Vector2(185,242)));
        Part(rig.body,"Closed tapered load-bearing fuselage",RvHull(RavenBody),0);
        Part(rig.head,"Closed reference helmet",RvHull(RavenHead),0);
        foreach(int side in new[]{-1,1})
        {
            RvPlate(rig.body,"Forward neck shield "+side,RvPoints(328,184,342,193,354,209,367,226,374,237,369,253,361,262,355,245,347,228,335,212,320,203),RavenBody,side,0,1.2f,.8f);
            RvPlate(rig.body,"Silver chest shield "+side,RvPoints(342,217,350,227,351,240,343,257,328,270,311,278,289,279,282,273,297,258,313,242,326,226),RavenBody,side,3,1.4f,1);
            RvPlate(rig.body,"Lower sternum armoured segment "+side,RvPoints(304,281,324,279,340,270,350,258,353,268,342,279,326,286,306,289,289,286),RavenBody,side,0,1.8f,.8f);
            // Separated light cells sit inside the lower keel, not a solid neon sticker.
            RvPlate(rig.body,"Keel energy cell A "+side,RvPoints(310,282,325,279,328,285,316,290,308,290),RavenBody,side,6,2,.6f);
            RvPlate(rig.body,"Keel energy cell B "+side,RvPoints(330,277,340,270,344,277,333,284,328,285),RavenBody,side,6,2,.6f);
            RvPlate(rig.body,"Keel energy cell C "+side,RvPoints(344,266,350,257,353,267,348,274),RavenBody,side,6,2,.6f);
            RvPlate(rig.body,"Upper back armour I "+side,RvPoints(211,201,237,185,260,177,280,175,296,178,278,184,254,185,230,194),RavenBody,side,0,1.1f,.75f);
            RvPlate(rig.body,"Upper back armour II "+side,RvPoints(242,187,265,180,286,181,303,186,292,193,269,191,252,196,226,207),RavenBody,side,4,1.3f,.75f);
            RvPlate(rig.head,"Swept helmet crown "+side,RvPoints(334,145,351,148,374,155,398,168,418,185,422,192,404,185,382,177,360,170,343,165,354,162,342,156),RavenHead,side,4,1.6f,.8f);
            RvPlate(rig.head,"Forward brow ridge "+side,RvPoints(356,169,376,170,398,180,416,193,410,195,392,185,374,179,351,176),RavenHead,side,3,2.1f,.7f);
            RvPlate(rig.head,"Lower articulated cheek "+side,RvPoints(345,190,357,198,367,212,382,220,396,217,405,210,399,224,383,230,366,223,352,210),RavenHead,side,0,1.2f,.8f);
            BuildRavenEye(rig.head,side);
        }
        // A three-piece hooked beak keeps its hard lip and lower mandible split.
        Part(rig.head,"Raven hooked upper beak",RvSolid(RvPoints(410,201,426,197,439,205,449,216,452,221,438,216,423,213,410,212),p=>0,1,7,.75f),2);
        foreach(int side in new[]{-1,1})
        {
            Part(rig.head,"Beak machined upper cheek "+side,RvSolid(RvPoints(414,202,426,200,438,207,447,217,430,211,417,210),p=>4.4f,side,1.8f,.6f),0);
            Part(rig.head,"Lower hooked mandible "+side,RvSolid(RvPoints(410,215,427,215,443,219,451,222,432,220,413,220,406,218),p=>2.5f,side,1.6f,.65f),2);
        }
        BuildRavenWing(rig,-1,out rig.leftWing,out rig.leftWingTip);
        BuildRavenWing(rig,1,out rig.rightWing,out rig.rightWingTip);
        BuildRavenTail(rig);BuildRavenTalons(rig);
    }
    static void BuildRavenEye(Transform head,int side)
    {
        var shell=RvPoints(373,175,387,178,408,190,419,199,409,209,396,216,384,212,376,201);
        var helmet=head.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Closed reference helmet");
        var helmetSkin=RvSupport(helmet.sharedMesh,head.position,side);
        var glass=RvSolid(shell,p=>helmetSkin(p)+1.2f,side,2.4f,.75f,2.5f);
        Part(head,"Seated dark optical glass "+side,glass,7);
        var glassSkin=RvSupport(glass,head.position,side);
        var pane=RvPoints(381,182,391,185,406,194,408,198,400,204,394,206,388,201,384,193);
        var iris=RvSolid(pane,p=>glassSkin(p)+.25f,side,.65f,.5f,2);
        Part(head,"Reference luminous iris "+side,iris,5);
        var irisSkin=RvSupport(iris,head.position,side);
        // A tiny triangular emitter continues the pane geometry; no round pupil.
        Part(head,"Optical core "+side,RvSolid(RvPoints(388,188,403,196,394,202,389,196),p=>irisSkin(p)+.15f,side,.35f,.25f,1.5f),6);
        Part(head,"Brow recessed fastener "+side,RoundSolid(new Vector2(425,199),side*5.4f,1.3f,1.3f,.6f),2);
    }
    static readonly float[][] RavenPrimaryContours={
        new float[]{306,191,285,188,260,193,235,201,214,210,189,220,214,215,243,208,273,202,296,199},
        new float[]{307,199,284,199,260,207,237,219,214,232,190,246,164,259,193,251,225,236,253,223,282,211,303,207},
        new float[]{307,207,283,209,257,223,230,241,204,259,177,277,146,289,175,284,205,271,234,253,264,234,292,220},
        new float[]{306,214,286,217,262,233,234,254,207,274,177,288,123,299,153,283,178,265,203,246,233,227,263,215,290,208},
        new float[]{290,218,280,219,263,227,244,240,220,255,199,273,174,291,204,281,236,270,260,256,281,240,291,227},
        new float[]{299,217,285,231,271,245,252,257,229,266,209,274,231,275,250,269,271,259,288,243,301,226},
        new float[]{305,211,289,221,278,232,262,244,245,252,260,253,281,244,297,231,311,217},
        new float[]{305,204,291,208,279,216,263,225,250,231,265,231,282,224,297,216,312,208}
    };
    static void BuildRavenWing(SkyPulseNeonFinchPrototype rig,int side,out Transform wing,out Transform tip)
    {
        var hub=new Vector2(302,202);float hubDepth=RvSkin(RavenBody,hub)+2;
        wing=Joint(rig.body,(side<0?"03 Near":"04 Far")+" connected wing saddle",Point(hub,side*hubDepth));
        tip=Joint(wing,"Interlocking machined flight blades",Point(new Vector2(295,214),side*(hubDepth+2)));
        Part(wing,"Supported shoulder axle",Rod(new Vector2(288,205),hub,side*(hubDepth-6),8),2);
        var saddle=RvPoints(303,178,316,181,326,190,330,202,323,218,310,234,293,248,276,256,282,242,294,227,305,213,312,199,309,187);
        Part(wing,"Dark shoulder saddle rim",RvSolid(saddle,p=>hubDepth+.8f,side,2,.7f),2);
        Part(wing,"Swept shoulder titanium saddle",RvSolid(RvInset(saddle,1.8f),p=>hubDepth+2.5f,side,1.1f,.55f),4);
        Part(wing,"Outer dark bearing housing",Ring(hub,side*(hubDepth+5.2f),11.5f,1.8f,1.8f),2);
        Part(wing,"Machined bearing bevel",Ring(hub,side*(hubDepth+5.7f),9.3f,.9f,1.2f),4);
        Part(wing,"Recessed cyan bearing ring",Ring(hub,side*(hubDepth+6),7.5f,.65f,.7f),5);
        Part(wing,"Dark inner bearing seat",RoundSolid(hub,side*(hubDepth+5.9f),6.7f,6.7f,.6f),2);
        Part(wing,"Cyan convex sensor bearing",RoundSolid(hub,side*(hubDepth+6.6f),5.2f,5.2f,1.1f),5);
        for(int i=0;i<RavenPrimaryContours.Length;i++)
        {
            var poly=RvPoints(RavenPrimaryContours[i]);int index=i;
            float Depth(Vector2 p)
            {
                float span=Mathf.Clamp01((302-p.x)/180);
                // A small transverse crown shapes the metal reflection. It is
                // below 1.4 native pixels, retaining a thin machined blade.
                float edge=float.PositiveInfinity;
                for(int e=0;e<poly.Length;e++)
                {
                    var a=poly[e];var b=poly[(e+1)%poly.Length];var delta=b-a;
                    float t=Mathf.Clamp01(Vector2.Dot(p-a,delta)/Mathf.Max(.0001f,delta.sqrMagnitude));
                    edge=Mathf.Min(edge,Vector2.Distance(p,a+delta*t));
                }
                float crown=1.4f*Mathf.Sin(Mathf.Clamp01(edge/9)*Mathf.PI*.5f);
                return hubDepth+2.5f+index*.48f+span*6+.8f*Mathf.Sin(span*Mathf.PI)+crown;
            }
            var chassis=RvSolid(poly,Depth,side,1.25f,.7f);
            Part(tip,"Primary "+(i+1)+" dark closed chassis",chassis,2);
            var chassisSkin=RvSupport(chassis,tip.position,side);
            var face=RvSolid(RvInset(poly,1.45f),p=>chassisSkin(p)+.15f,side,.55f,.42f);
            Part(tip,"Primary "+(i+1)+" tapered alloy flight face",face,i==3||i==4||i==5?4:1);
            if(i==4||i==5)
            {
                var channel=i==4?RvPoints(279,224,265,232,245,246,225,259,208,276,227,267,247,251,266,237,280,227):RvPoints(286,234,274,246,255,260,236,269,243,267,261,255,278,241,290,229);
                var support=RvSupport(face,tip.position,side);
                Part(tip,"Primary "+(i+1)+" recessed blue conductor",RvSolid(channel,p=>support(p)+.1f,side,.25f,.3f,6),1);
            }
        }
        // Six individually shaped short coverts armour the root, rather than
        // scaling copies of the long primary's generic leaf shape.
        float[][] coverts={
            new float[]{296,185,280,184,262,189,247,197,262,195,280,192,294,191},
            new float[]{299,191,282,191,264,198,248,208,265,204,282,200,298,199},
            new float[]{300,198,285,199,270,208,254,220,270,215,287,208,301,205},
            new float[]{298,206,286,210,273,221,259,233,276,226,290,217,303,211},
            new float[]{294,216,282,222,269,235,256,247,273,239,287,228,298,221},
            new float[]{283,227,270,236,256,249,247,259,264,251,279,240,291,231}};
        for(int i=0;i<coverts.Length;i++)
        {
            float depth=hubDepth+9+i*.32f;var poly=RvPoints(coverts[i]);
            Part(tip,"Root covert "+(i+1)+" graphite gasket",RvSolid(poly,p=>depth,side,.9f,.55f),2);
            Part(tip,"Root covert "+(i+1)+" machined blue plate",RvSolid(RvInset(poly,1),p=>depth+1,side,.5f,.35f),i%3==0?4:0);
        }
    }
    static readonly float[][] RavenTailContours={
        new float[]{215,239,181,220,160,214,127,205,92,198,62,198,75,207,97,214,128,220,158,223,185,232},
        new float[]{213,246,178,230,149,221,108,214,71,214,35,220,49,227,82,231,117,232,147,236,182,241},
        new float[]{211,251,172,237,135,233,98,234,61,236,25,249,39,259,62,267,83,271,116,260,150,248,181,247},
        new float[]{210,256,169,245,147,246,114,253,85,265,58,278,68,284,83,287,112,277,141,263,170,253,187,255}};
    static void BuildRavenTail(SkyPulseNeonFinchPrototype rig)
    {
        Part(rig.tail,"Connected graphite tail driveshaft",Rod(new Vector2(227,252),new Vector2(180,242),0,8),2);
        Part(rig.tail,"Tail root mechanical collar",Ring(new Vector2(207,249),0,9,1.5f,5),0);
        for(int i=0;i<4;i++)
        {
            var poly=RvPoints(RavenTailContours[i]);float z=(i-1.5f)*3.2f;
            Part(rig.tailTip,"Tail "+(i+1)+" closed graphite load-bearing blade",RvSolid(poly,p=>z,1,1.45f,.9f),2);
            foreach(int side in new[]{-1,1})
                Part(rig.tailTip,"Tail "+(i+1)+" titanium face "+side,RvSolid(RvInset(poly,2),p=>side*z+1.6f,side,.55f,.6f),4);
        }
        float[][] cells={
            new float[]{83,202,112,205,139,213,121,215,102,210},
            new float[]{75,219,104,219,128,224,109,229,88,226},
            new float[]{71,244,97,239,121,239,108,250,88,260,72,257},
            new float[]{91,272,115,260,139,253,126,267,105,278}};
        for(int i=0;i<cells.Length;i++)foreach(int side in new[]{-1,1})
        {
            float z=(i-1.5f)*3.2f;
            Part(rig.tailTip,"Tail inset energy aperture "+(i+1)+" "+side,RvSolid(RvPoints(cells[i]),p=>side*z+2.35f,side,.4f,.45f),6);
        }
    }
    static void BuildRavenTalons(SkyPulseNeonFinchPrototype rig)
    {
        foreach(int side in new[]{-1,1})
        {
            float x=side<0?224:245,y=side<0?289:287,z=side*18;
            Part(rig.body,"Connected landing hinge "+side,RoundSolid(new Vector2(x,y),z,5,5,4),2);
            var shank=RvPoints(x-4,y-1,x+3,y+1,x+9,y+14,x+7,y+22,x+1,y+23,x+2,y+14,x-3,y+9);
            Part(rig.body,"Segmented graphite landing shank "+side,RvSolid(shank,p=>side*z,side,3,1),2);
            Part(rig.body,"Landing shank silver facing "+side,RvSolid(RvInset(shank,1.1f),p=>side*z+3.1f,side,.6f,.45f),0);
            float[][] toes={
                new float[]{x+5,y+17,x+11,y+24,x+10,y+33,x+3,y+39,x-7,y+42,x-13,y+40,x-5,y+35,x+1,y+29},
                new float[]{x+8,y+18,x+16,y+23,x+17,y+31,x+14,y+36,x+6,y+36,x+11,y+31,x+11,y+27},
                new float[]{x+2,y+17,x-5,y+21,x-9,y+28,x-8,y+33,x-3,y+35,x-3,y+29,x+1,y+25}};
            for(int i=0;i<toes.Length;i++)
            {
                Part(rig.body,"Hooked articulated talon "+side+" "+i,RvSolid(RvPoints(toes[i]),p=>side*z+i*3,side,2,.7f),2);
                Part(rig.body,"Talon machined edge "+side+" "+i,RvSolid(RvInset(RvPoints(toes[i]),.9f),p=>side*z+i*3+2.1f,side,.45f,.35f),0);
            }
        }
    }
}
