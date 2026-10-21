using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    // One isolated render target for a training dock or the whole lineage display.
    // Reuses registered artwork and the flight rig over real machined 3D plinths.
    public sealed class SkyPulseProgressionStage : MonoBehaviour
    {
        public struct Portrait
        {
            public Vector2 At,Size;
            public Color Accent;
            public Sprite[] Frames;
            public Sprite Still;
        }
        sealed class Pilot
        {
            public Portrait Data;public SpriteRenderer Bird;public SkyPulseBirdRig Rig;
            public Transform Dock;public Vector3 Home;public float Phase;
        }
        readonly List<Pilot> pilots=new List<Pilot>(3);
        readonly List<Mesh> meshes=new List<Mesh>(4);
        readonly List<Transform> packets=new List<Transform>(2);
        static readonly HashSet<int> slots=new HashSet<int>();
        Transform stage;Camera camera3D;RenderTexture texture;Material metal,light;
        float clock;int slot;bool lineage;
        public bool ReducedMotion,MotionPaused;
        public Camera StageCamera=>camera3D;
        public float AnimationClock=>clock;
        public int PilotCount=>pilots.Count;

        public void Build(RawImage view,Portrait[] birds,bool connect)
        {
            lineage=connect;while(!slots.Add(slot))slot++;
            stage=new GameObject("Progression optical studio").transform;stage.position=new Vector3(12000+slot*64,12000,0);
            var c=new GameObject("Progression studio camera",typeof(Camera));c.transform.SetParent(stage,false);
            camera3D=c.GetComponent<Camera>();camera3D.transform.localPosition=new Vector3(0,0,-20);
            camera3D.orthographic=true;camera3D.orthographicSize=view.rectTransform.rect.height*.005f;
            camera3D.aspect=view.rectTransform.rect.width/view.rectTransform.rect.height;
            camera3D.nearClipPlane=.1f;camera3D.farClipPlane=40;camera3D.cullingMask=1<<31;
            camera3D.clearFlags=CameraClearFlags.SolidColor;camera3D.backgroundColor=Color.clear;camera3D.allowHDR=false;camera3D.allowMSAA=true;
            int height=Mathf.RoundToInt(1024/camera3D.aspect);
            texture=new RenderTexture(1024,height,24,RenderTextureFormat.ARGB32){name="Bird progression studio",antiAliasing=4};texture.Create();camera3D.targetTexture=texture;view.texture=texture;view.raycastTarget=false;
            metal=new Material(Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics")){name="Progression satin titanium"};
            metal.SetColor("_BaseColor",new Color(.10f,.17f,.22f));metal.SetFloat("_Roughness",.23f);
            light=new Material(metal){name="Progression optical light guides"};light.SetColor("_BaseColor",new Color(.19f,.38f,.45f));light.SetFloat("_Energy",.65f);
            var b=new SkyPulseMeshBuilder();b.Profile(new[]{new Vector2(0,.10f),new Vector2(.91f,.10f),new Vector2(1,.025f),new Vector2(1,-.10f),new Vector2(.91f,-.16f),new Vector2(0,-.16f)});
            var disk=b.Finish("Lathed titanium bird pedestal");meshes.Add(disk);
            b=new SkyPulseMeshBuilder();b.Torus(.94f,.016f,80,Color.white);var ring=b.Finish("Recessed pedestal light rim");meshes.Add(ring);
            foreach(var descriptor in birds)
            {
                if(descriptor.Still==null)continue;
                var root=new GameObject("Bird studio portrait").transform;root.SetParent(stage,false);
                var go=new GameObject("Registered animated bird",typeof(SpriteRenderer));go.transform.SetParent(root,false);
                var bird=go.GetComponent<SpriteRenderer>();bird.sprite=descriptor.Still;bird.sortingOrder=10;
                var reference=descriptor.Frames!=null&&descriptor.Frames.Length>0?descriptor.Frames[0]:descriptor.Still;
                bird.transform.localScale=Vector3.one*(Mathf.Min(descriptor.Size.x/reference.bounds.size.x,descriptor.Size.y/reference.bounds.size.y)*.01f);
                var rig=go.AddComponent<SkyPulseBirdRig>();rig.Bind(bird);rig.PrepareFrames(descriptor.Frames);
                var dock=new GameObject("Machined levitation cradle").transform;dock.SetParent(stage,false);
                dock.localPosition=new Vector3(descriptor.At.x*.01f,(descriptor.At.y-descriptor.Size.y*.48f)*.01f,.75f);
                dock.localRotation=Quaternion.Euler(65,-8,0);dock.localScale=Vector3.one*descriptor.Size.x*.0038f;
                SkyPulseMeshBuilder.Surface(dock,"Bevelled pedestal body",disk,metal,0);
                var rim=SkyPulseMeshBuilder.Surface(dock,"Embedded luminous rim",ring,light,1);rim.transform.localPosition=Vector3.back*.17f;
                var props=new MaterialPropertyBlock();props.SetColor("_GlowColor",descriptor.Accent);rim.SetPropertyBlock(props);
                var pilot=new Pilot{Data=descriptor,Bird=bird,Rig=rig,Dock=dock,Home=(Vector3)descriptor.At*.01f,Phase=pilots.Count*.53f};pilots.Add(pilot);
            }
            if(connect&&birds.Length==3)BuildInheritance(birds);
            foreach(var t in stage.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
            Tick(0);
        }
        static Vector3 Route(Vector3 a,Vector3 d,float t)
        {
            float u=1-t;var b=a+new Vector3(0,-.9f,.35f);var c=d+new Vector3(0,.7f,.35f);
            return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;
        }
        void BuildInheritance(Portrait[] birds)
        {
            var b=new SkyPulseMeshBuilder();
            for(int side=0;side<2;side++)
            {
                // Signals emerge below the parent identity strip, keeping the
                // names unobscured while visually joining the two parent cards.
                var a=new Vector3(birds[side].At.x*.01f,(birds[side].At.y-birds[side].Size.y*.48f)*.01f-1.24f,1);
                var d=new Vector3(0,(birds[2].At.y-birds[2].Size.y*.48f)*.01f,1);
                for(int i=0;i<48;i++)
                {
                    var p=Route(a,d,i/48f);var q=Route(a,d,(i+1)/48f);var normal=Vector3.Cross(q-p,Vector3.forward).normalized*.012f;
                    b.Quad(p-normal,p+normal,q+normal,q-normal,new Color(.5f,.85f,1,.35f));
                }
                var packet=new SkyPulseMeshBuilder();packet.Torus(.035f,.025f,12,Color.white);var mesh=packet.Finish("Inherited signal bead");meshes.Add(mesh);
                var pgo=SkyPulseMeshBuilder.Surface(stage,"Travelling inheritance signal",mesh,light,2);packets.Add(pgo.transform);
            }
            var route=b.Finish("Continuous parent to offspring light guides");meshes.Add(route);SkyPulseMeshBuilder.Surface(stage,"Paired inheritance paths",route,light,0);
        }
        void Update()=>Tick(Time.unscaledDeltaTime);
        public void Tick(float dt)
        {
            if(stage==null||MotionPaused)return;if(!ReducedMotion)clock+=Mathf.Max(0,dt);
            float t=ReducedMotion?0:clock;
            for(int i=0;i<pilots.Count;i++)
            {
                var p=pilots[i];float phase=t+p.Phase;var frames=p.Data.Frames;
                if(frames!=null&&frames.Length>1)
                {
                    int count=(frames.Length*2)-2,step=Mathf.FloorToInt(Mathf.Repeat(phase*.55f,1)*count);
                    p.Bird.sprite=ReducedMotion?p.Data.Still:frames[step<frames.Length?step:count-step];
                }
                p.Bird.transform.localPosition=p.Home+Vector3.up*(ReducedMotion?0:Mathf.Sin(phase*1.3f)*.045f);
                p.Bird.transform.localRotation=Quaternion.Euler(0,0,ReducedMotion?0:Mathf.Sin(phase*.7f)*1.4f);
                p.Rig.Present(t,.15f,0,ReducedMotion,p.Data.Accent);
                p.Dock.localRotation=Quaternion.Euler(65,-8+(ReducedMotion?0:Mathf.Sin(phase*.3f)*2),0);
            }
            if(lineage&&pilots.Count==3)for(int i=0;i<packets.Count;i++)
            {
                var a=pilots[i].Dock.localPosition-Vector3.up*1.24f;var d=pilots[2].Dock.localPosition;a.z=d.z=1;
                packets[i].localPosition=Route(a,d,ReducedMotion?.5f:Mathf.Repeat(t*.32f+i*.25f,1));
            }
        }
        public void Render(){if(camera3D!=null&&isActiveAndEnabled)camera3D.Render();}
        void OnEnable(){if(stage!=null)stage.gameObject.SetActive(true);}
        void OnDisable(){if(stage!=null)stage.gameObject.SetActive(false);}
        void OnDestroy()
        {
            if(stage!=null)Destroy(stage.gameObject);slots.Remove(slot);
            if(texture!=null){texture.Release();Destroy(texture);}foreach(var m in meshes)Destroy(m);
            if(metal!=null)Destroy(metal);if(light!=null)Destroy(light);
        }
    }
}
