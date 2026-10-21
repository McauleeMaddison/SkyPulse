using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public struct HolographicBranch {public Vector2 From,To;public Color Colour;public bool Powered;}
    public struct HolographicRoot {public Vector2 At;public Color Colour;public bool Powered;public float Size;}
    // A single continuous, three-dimensional fibre tree behind fixed UI labels
    // and hit targets. The viewport camera follows scrolling, not the player.
    [DefaultExecutionOrder(100)]
    public sealed class SkyPulseTechTreeView : MonoBehaviour
    {
        Transform stage;Camera treeCamera;RenderTexture texture;Material material;Mesh mesh;RectTransform content,viewport;float clock;
        public bool ReducedMotion,MotionPaused;
        public int BranchCount {get;private set;}
        public int SocketCount {get;private set;}
        public Camera TreeCamera=>treeCamera;
        public float AnimationClock=>clock;
        // Read-only topology supports QA of the one-root connected tree.
        public HolographicBranch[] Branches { get; private set; }
        public HolographicRoot[] Roots { get; private set; }
        public void Build(RawImage view,RectTransform content,RectTransform viewport,List<HolographicBranch> branches,List<HolographicRoot> roots)
        {
            this.content=content;this.viewport=viewport;BranchCount=branches.Count;SocketCount=roots.Count;Branches=branches.ToArray();Roots=roots.ToArray();
            stage=new GameObject("Continuous holographic upgrade tree").transform;stage.position=new Vector3(5000,5000,0);
            var o=new GameObject("Holographic viewport camera",typeof(Camera));o.transform.SetParent(stage,false);treeCamera=o.GetComponent<Camera>();treeCamera.orthographic=true;treeCamera.nearClipPlane=.1f;treeCamera.farClipPlane=40;treeCamera.cullingMask=1<<31;treeCamera.clearFlags=CameraClearFlags.SolidColor;treeCamera.backgroundColor=Color.clear;treeCamera.allowMSAA=true;treeCamera.allowHDR=false;
            int height=Mathf.Clamp(Mathf.RoundToInt(1024*viewport.rect.height/Mathf.Max(1,viewport.rect.width)),512,2048);texture=new RenderTexture(1024,height,24,RenderTextureFormat.ARGB32){name="Holographic family tree",antiAliasing=4};texture.Create();treeCamera.targetTexture=texture;view.texture=texture;view.raycastTarget=false;
            material=new Material(Resources.Load<Shader>("SkyPulse/Shaders/HolographicTree")){name="Flowing holographic tree fibres"};
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var colours=new List<Color>();var uv=new List<Vector2>();var triangles=new List<int>();
            foreach(var branch in branches)
            {
                var from=(Vector3)branch.From*.01f;var to=(Vector3)branch.To*.01f;float gap=Mathf.Abs(to.y-from.y);
                var a=from+new Vector3(0,-gap*.45f,.24f);var b=to+new Vector3(0,gap*.45f,-.18f);
                Tube(from,a,b,to,.065f,branch.Colour,branch.Powered?.88f:.56f,vertices,normals,colours,uv,triangles);
                Tube(from,a,b,to,.18f,branch.Colour,branch.Powered?.15f:.075f,vertices,normals,colours,uv,triangles);
            }
            foreach(var root in roots)
            {
                var centre=new Vector3(root.At.x*.01f,root.At.y*.01f,-.13f);float radius=root.Size*.01f;
                for(int i=0;i<48;i++)
                {
                    float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;var p=centre+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,Mathf.Sin(a)*.12f);var q=centre+new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,Mathf.Sin(b)*.12f);
                    Segment(p,q,.025f,root.Colour,root.Powered?.92f:.62f,a/6.28f,vertices,normals,colours,uv,triangles);
                }
                // Raised faceted junction, continuous with its branch tubes.
                for(int i=0;i<6;i++)
                {
                    float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;var p=centre+new Vector3(Mathf.Cos(a)*radius*.45f,Mathf.Sin(a)*radius*.45f,0);var q=centre+new Vector3(Mathf.Cos(b)*radius*.45f,Mathf.Sin(b)*radius*.45f,0);
                    Face(centre+Vector3.back*radius*.7f,p,q,root.Colour,root.Powered?.9f:.60f,vertices,normals,colours,uv,triangles);
                }
            }
            mesh=new Mesh{name="Connected volumetric family tree",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colours);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.UploadMeshData(true);
            var surface=SkyPulseMeshBuilder.Surface(stage,"Unbroken holographic branches",mesh,material,0);surface.gameObject.layer=31;Tick(0);
        }
        static Vector3 Curve(Vector3 from,Vector3 a,Vector3 b,Vector3 to,float t){float u=1-t;return u*u*u*from+3*u*u*t*a+3*u*t*t*b+t*t*t*to;}
        static void Tube(Vector3 from,Vector3 a,Vector3 b,Vector3 to,float r,Color colour,float alpha,List<Vector3> v,List<Vector3> n,List<Color> c,List<Vector2> uv,List<int> triangles)
        {
            var old=from;for(int i=1;i<=40;i++){float t=i/40f;var next=Curve(from,a,b,to,t);Segment(old,next,r,colour,alpha,t,v,n,c,uv,triangles);old=next;}
        }
        static void Segment(Vector3 a,Vector3 b,float radius,Color colour,float alpha,float t,List<Vector3> v,List<Vector3> n,List<Color> c,List<Vector2> uv,List<int> triangles)
        {
            var axis=(b-a).normalized;var side=Vector3.Cross(axis,Vector3.forward).normalized;if(side.sqrMagnitude<.1f)side=Vector3.right;var up=Vector3.Cross(axis,side).normalized;
            for(int i=0;i<8;i++)
            {
                float x=i*Mathf.PI/4,y=(i+1)*Mathf.PI/4;var p=(Mathf.Cos(x)*side+Mathf.Sin(x)*up)*radius;var q=(Mathf.Cos(y)*side+Mathf.Sin(y)*up)*radius;int start=v.Count;
                v.Add(a+p);v.Add(a+q);v.Add(b+q);v.Add(b+p);n.Add(p.normalized);n.Add(q.normalized);n.Add(q.normalized);n.Add(p.normalized);var tint=colour;tint.a=alpha;for(int j=0;j<4;j++){c.Add(tint);uv.Add(new Vector2(t,0));}
                triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);triangles.Add(start);triangles.Add(start+2);triangles.Add(start+3);
            }
        }
        static void Face(Vector3 a,Vector3 b,Vector3 d,Color colour,float alpha,List<Vector3> v,List<Vector3> n,List<Color> c,List<Vector2> uv,List<int> triangles)
        {
            int start=v.Count;var normal=Vector3.Cross(b-a,d-a).normalized;var tint=colour;tint.a=alpha;v.Add(a);v.Add(b);v.Add(d);for(int i=0;i<3;i++){n.Add(normal);c.Add(tint);uv.Add(new Vector2(i*.15f,0));triangles.Add(start+i);}
        }
        void LateUpdate()=>Tick(Time.unscaledDeltaTime);
        public void Tick(float dt)
        {
            if(treeCamera==null)return;treeCamera.orthographicSize=viewport.rect.height*.005f;treeCamera.aspect=viewport.rect.width/Mathf.Max(1,viewport.rect.height);treeCamera.transform.localPosition=new Vector3(-content.anchoredPosition.x*.01f,-(viewport.rect.height*.5f+content.anchoredPosition.y)*.01f,-20);
            if(!MotionPaused&&!ReducedMotion)clock+=Mathf.Max(0,dt);material.SetFloat("_Clock",ReducedMotion?0:clock);
        }
        public void Render(){Tick(0);if(treeCamera!=null&&isActiveAndEnabled)treeCamera.Render();}
        void OnEnable(){if(stage!=null)stage.gameObject.SetActive(true);}
        void OnDisable(){if(stage!=null)stage.gameObject.SetActive(false);}
        void OnDestroy(){if(stage!=null)Destroy(stage.gameObject);if(texture!=null){texture.Release();Destroy(texture);}if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
