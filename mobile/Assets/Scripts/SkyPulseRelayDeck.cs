using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkyPulse.Mobile
{
    /// <summary>One pooled 3D deck mesh. Its fixed front light is GroundY;
    /// shader-driven bevelled panels slide beneath it without gameplay colliders.</summary>
    public sealed class SkyPulseRelayDeck : MonoBehaviour
    {
        Mesh mesh;Material material;
        const float Step=1.25f;
        static readonly int DistanceId=Shader.PropertyToID("_Distance"),ClockId=Shader.PropertyToID("_Clock");
        public void Build(float width,Texture reflection=null)
        {
            int count=Mathf.CeilToInt(width/Step)+4;float span=count*Step;
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var tiles=new List<Vector2>();var triangles=new List<int>();
            for(int i=0;i<count;i++)
            {
                float centre=(i-(count-1)*.5f)*Step,x=Step*.5f-.009f,left=centre-x,right=centre+x,cut=.025f;
                var a=new Vector3(left+cut,0,.06f);var b=new Vector3(right-cut,0,.06f);var c=new Vector3(right-cut,.32f,3);var d=new Vector3(left+cut,.32f,3);
                var e=new Vector3(left,-.075f,-.025f);var f=new Vector3(right,-.075f,-.025f);var g=new Vector3(right,-.53f,-.025f);var h=new Vector3(left,-.53f,-.025f);
                var key=new Vector2(centre,1);
                Face(vertices,normals,uv,tiles,triangles,a,d,c,b,0,1,key);
                Face(vertices,normals,uv,tiles,triangles,a,b,f,e,0,-.075f,key);
                Face(vertices,normals,uv,tiles,triangles,e,f,g,h,-.075f,-1,key);
                Face(vertices,normals,uv,tiles,triangles,e,h,d,a,-1,-1,key);
                Face(vertices,normals,uv,tiles,triangles,b,c,g,f,-1,-1,key);
            }
            // Fixed fascia covers panel seams. The upper edge stays exactly at
            // the existing collision height, including after retries and wraps.
            float half=span*.5f+Step;
            Face(vertices,normals,uv,tiles,triangles,new Vector3(-half,-.025f,.08f),new Vector3(-half,.30f,3.02f),new Vector3(half,.30f,3.02f),new Vector3(half,-.025f,.08f),0,1,Vector2.zero);
            Face(vertices,normals,uv,tiles,triangles,new Vector3(-half,-.075f,-.075f),new Vector3(half,-.075f,-.075f),new Vector3(half,-.60f,-.075f),new Vector3(-half,-.60f,-.075f),-.12f,-1,Vector2.zero);
            Face(vertices,normals,uv,tiles,triangles,new Vector3(-half,0,-.065f),new Vector3(half,0,-.065f),new Vector3(half,-.018f,-.065f),new Vector3(-half,-.018f,-.065f),0,0,Vector2.zero);
            mesh=new Mesh{name="Bevelled titanium deck panels and fixed ground guide"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetUVs(1,tiles);mesh.SetTriangles(triangles,0);mesh.bounds=new Bounds(new Vector3(0,.15f,1.5f),new Vector3(span+4,2,5));mesh.UploadMeshData(true);
            material=new Material(Resources.Load<Shader>("SkyPulse/Shaders/RelayRunway")){name="Reflected orbital landing deck"};material.SetFloat("_PanelSpan",span);material.SetTexture("_Environment",reflection!=null?reflection:Texture2D.grayTexture);
            var filter=gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=9;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }
        static void Face(List<Vector3> vertices,List<Vector3> normals,List<Vector2> uv,List<Vector2> tiles,List<int> triangles,Vector3 a,Vector3 b,Vector3 c,Vector3 d,float lower,float upper,Vector2 tile)
        {
            int index=vertices.Count;var n=Vector3.Cross(b-a,c-a).normalized;
            vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
            for(int i=0;i<4;i++){normals.Add(n);tiles.Add(tile);}
            for(int i=0;i<4;i++){var p=vertices[index+i];uv.Add(new Vector2((p.x-tile.x)/Step+.5f,Mathf.Lerp(lower,upper,Mathf.InverseLerp(a.y,c.y,p.y))));}
            triangles.Add(index);triangles.Add(index+1);triangles.Add(index+2);triangles.Add(index);triangles.Add(index+2);triangles.Add(index+3);
        }
        public void SetVisual(float distance,float clock){material.SetFloat(DistanceId,distance);material.SetFloat(ClockId,clock);}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
