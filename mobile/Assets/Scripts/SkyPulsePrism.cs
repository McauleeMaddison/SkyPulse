using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkyPulse.Mobile
{
    // Authored geometry shared by the crystal core and distant station props.
    // Buffers are construction-only; presentation changes uniforms/transforms.
    internal sealed class SkyPulseMeshBuilder
    {
        readonly List<Vector3> v=new List<Vector3>();
        readonly List<Vector3> n=new List<Vector3>();
        readonly List<Color> c=new List<Color>();
        readonly List<int> t=new List<int>();
        public void Face(Vector3 a,Vector3 b,Vector3 d,Color colour)
        {
            int i=v.Count;var normal=Vector3.Cross(b-a,d-a).normalized;
            v.Add(a);v.Add(b);v.Add(d);for(int k=0;k<3;k++){n.Add(normal);c.Add(colour);t.Add(i+k);}
        }
        public void Quad(Vector3 a,Vector3 b,Vector3 d,Vector3 e,Color colour){Face(a,b,d,colour);Face(a,d,e,colour);}
        public void SmoothQuad(Vector3 a,Vector3 b,Vector3 d,Vector3 e,Vector3 na,Vector3 nb,Color colour)
        {
            int i=v.Count;v.Add(a);v.Add(b);v.Add(d);v.Add(e);n.Add(na);n.Add(nb);n.Add(nb);n.Add(na);
            for(int j=0;j<4;j++)c.Add(colour);t.Add(i);t.Add(i+1);t.Add(i+2);t.Add(i);t.Add(i+2);t.Add(i+3);
        }
        public void Box(Vector3 p,Vector3 size,Color colour)
        {
            Vector3 a=p-size*.5f,b=p+size*.5f;
            Quad(new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,a.y,a.z),colour);
            Quad(new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,a.y,b.z),colour);
            Quad(new Vector3(a.x,b.y,a.z),new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,b.y,a.z),colour);
            Quad(new Vector3(a.x,a.y,b.z),new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,a.y,b.z),colour);
            Quad(new Vector3(a.x,a.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,b.y,a.z),new Vector3(a.x,a.y,a.z),colour);
            Quad(new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,a.y,b.z),colour);
        }
        public void Torus(float radius,float thickness,int segments,Color colour)
        {
            for(int i=0;i<segments;i++)for(int j=0;j<6;j++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments,u=j*Mathf.PI/3,w=(j+1)*Mathf.PI/3;
                Quad(Ring(a,u,radius,thickness),Ring(b,u,radius,thickness),Ring(b,w,radius,thickness),Ring(a,w,radius,thickness),colour);
            }
        }
        public void RingArc(float radius,float thickness,int segments,float start,float sweep,Color colour)
        {
            for(int i=0;i<segments;i++)for(int j=0;j<6;j++)
            {
                float a=(start+sweep*i/segments)*Mathf.Deg2Rad,b=(start+sweep*(i+1)/segments)*Mathf.Deg2Rad,u=j*Mathf.PI/3,w=(j+1)*Mathf.PI/3;
                Quad(Ring(a,u,radius,thickness),Ring(b,u,radius,thickness),Ring(b,w,radius,thickness),Ring(a,w,radius,thickness),colour);
            }
        }
        static Vector3 Ring(float a,float b,float r,float s)=>new Vector3(Mathf.Cos(a)*(r+Mathf.Cos(b)*s),Mathf.Sin(a)*(r+Mathf.Cos(b)*s),Mathf.Sin(b)*s);
        public void Profile(Vector2[] profile)
        {
            for(int i=0;i<64;i++)for(int j=1;j<profile.Length;j++)
            {
                float a=i*Mathf.PI/32,c=(i+1)*Mathf.PI/32;var p=profile[j-1];var q=profile[j];
                Quad(new Vector3(Mathf.Cos(a)*p.x,Mathf.Sin(a)*p.x,p.y),new Vector3(Mathf.Cos(c)*p.x,Mathf.Sin(c)*p.x,p.y),new Vector3(Mathf.Cos(c)*q.x,Mathf.Sin(c)*q.x,q.y),new Vector3(Mathf.Cos(a)*q.x,Mathf.Sin(a)*q.x,q.y),new Color(1,1,1,0));
            }
        }
        public void ExtrudePrism(Vector2[] shape,float front,float back)
        {
            Vector3 centre=new Vector3(0,0,front);for(int i=0;i<shape.Length;i++)
            {
                var a=shape[i];var c=shape[(i+1)%shape.Length];Face(centre,new Vector3(c.x,c.y,front),new Vector3(a.x,a.y,front),new Color(1,1,1,.1f));
                Quad(new Vector3(a.x,a.y,front),new Vector3(c.x,c.y,front),new Vector3(c.x,c.y,back),new Vector3(a.x,a.y,back),new Color(.75f,.9f,1,.25f));
            }
        }
        public void BevelBox(float width,float height,float depth)
        {
            float x=width*.5f,y=height*.5f,cut=Mathf.Min(.07f,height*.19f);var edge=new[]{new Vector2(-x+cut,-y),new Vector2(x-cut,-y),new Vector2(x,-y+cut),new Vector2(x,y-cut),new Vector2(x-cut,y),new Vector2(-x+cut,y),new Vector2(-x,y-cut),new Vector2(-x,-y+cut)};
            for(int i=0;i<8;i++)
            {
                var a=edge[i];var c=edge[(i+1)%8];var p=a*.92f;var q=c*.92f;
                Face(new Vector3(0,0,-depth),new Vector3(q.x,q.y,-depth),new Vector3(p.x,p.y,-depth),new Color(1,1,1,0));
                Quad(new Vector3(p.x,p.y,-depth),new Vector3(q.x,q.y,-depth),new Vector3(c.x,c.y,-depth*.65f),new Vector3(a.x,a.y,-depth*.65f),new Color(.9f,.95f,1,0));
                Quad(new Vector3(a.x,a.y,-depth*.65f),new Vector3(c.x,c.y,-depth*.65f),new Vector3(c.x,c.y,0),new Vector3(a.x,a.y,0),new Color(.45f,.6f,.7f,0));
            }
        }
        public Mesh Finish(string name,bool readable=false){var m=new Mesh{name=name};m.SetVertices(v);m.SetNormals(n);m.SetColors(c);m.SetTriangles(t,0);m.RecalculateBounds();m.UploadMeshData(!readable);return m;}
        public static MeshRenderer Surface(Transform parent,string name,Mesh mesh,Material material,int order)
        {
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=order;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;return r;
        }
    }

    public sealed class SkyPulsePrism : MonoBehaviour
    {
        Mesh shell,core;Material material;Transform coreTransform;
        static readonly int ClockId=Shader.PropertyToID("_Clock");
        public MeshRenderer Shell { get; private set; }
        public void Build()
        {
            var shape=new SkyPulseMeshBuilder();
            // Six bevelled faces, an elongated crown and a lower pointed pavilion.
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;
                var p=new Vector3(Mathf.Cos(a)*.29f,.16f,Mathf.Sin(a)*.29f);var q=new Vector3(Mathf.Cos(b)*.29f,.16f,Mathf.Sin(b)*.29f);
                var l=new Vector3(p.x*.79f,-.27f,p.z*.79f);var r=new Vector3(q.x*.79f,-.27f,q.z*.79f);
                shape.Face(new Vector3(0,.58f,0),q,p,new Color(.65f,.93f,1,.83f));
                shape.Quad(p,q,r,l,new Color(.20f,.68f,.92f,.76f));
                shape.Face(l,r,new Vector3(0,-.55f,0),new Color(.19f,.41f,.80f,.90f));
            }
            shell=shape.Finish("Six-sided optical gate crystal");
            var inside=new SkyPulseMeshBuilder();
            for(int i=0;i<4;i++){float a=i*Mathf.PI/2,b=(i+1)*Mathf.PI/2;var p=new Vector3(Mathf.Cos(a)*.09f,0,Mathf.Sin(a)*.09f);var q=new Vector3(Mathf.Cos(b)*.09f,0,Mathf.Sin(b)*.09f);inside.Face(Vector3.up*.29f,p,q,new Color(.7f,1,1,1));inside.Face(Vector3.down*.28f,q,p,new Color(.3f,.9f,1,1));}
            core=inside.Finish("Suspended crystal light core");material=new Material(Resources.Load<Shader>("SkyPulse/Shaders/Prism")){name="SkyPulse optical crystal"};
            coreTransform=SkyPulseMeshBuilder.Surface(transform,"Illuminated crystal core",core,material,10).transform;
            Shell=SkyPulseMeshBuilder.Surface(transform,"Refracting faceted crystal",shell,material,9);
        }
        public void SetVisual(float clock,float offset)
        {
            transform.localRotation=Quaternion.Euler(8,clock*37+offset*57,Mathf.Sin(clock*.7f+offset)*4);
            coreTransform.localRotation=Quaternion.Euler(0,-clock*65,0);material.SetFloat(ClockId,clock);
        }
        void OnDestroy(){if(shell!=null)Destroy(shell);if(core!=null)Destroy(core);if(material!=null)Destroy(material);}
    }
}
