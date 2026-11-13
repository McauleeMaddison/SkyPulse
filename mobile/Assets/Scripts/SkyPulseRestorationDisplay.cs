using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SkyPulse.Mobile
{
    // A separate city viewport above the optical board. Real authored districts
    // and connected skyways share the same saved light groups as Endless Route.
    public sealed class SkyPulseRestorationDisplay : MonoBehaviour
    {
        Transform stage;Camera sceneCamera;RenderTexture texture;Material finish,composite;
        readonly List<Mesh> owned=new List<Mesh>();
        readonly MeshRenderer[] districts=new MeshRenderer[7];readonly MaterialPropertyBlock[] properties=new MaterialPropertyBlock[7];
        static readonly int Clock=Shader.PropertyToID("_Clock"),Windows=Shader.PropertyToID("_WindowGlow"),Beacons=Shader.PropertyToID("_BeaconGlow"),Pulse=Shader.PropertyToID("_RestorePulse");
        float clock,power,pulse;int mask=-1,system;public bool ReducedMotion,MotionPaused;
        public int RestoredMask=>mask&255;public int ActiveVault=>system;public float Phase=>clock;public float LivePower=>power;public RenderTexture Texture=>texture;
        public void Build(RawImage view)
        {
            stage=new GameObject("City restoration overlook / private stage").transform;stage.position=new Vector3(14000,14000,0);
            var cameraObject=new GameObject("City overlook perspective",typeof(Camera));cameraObject.transform.SetParent(stage,false);
            sceneCamera=cameraObject.GetComponent<Camera>();sceneCamera.enabled=false;sceneCamera.fieldOfView=23;sceneCamera.aspect=990f/224;sceneCamera.nearClipPlane=.1f;sceneCamera.farClipPlane=120;sceneCamera.cullingMask=1<<30;sceneCamera.clearFlags=CameraClearFlags.SolidColor;sceneCamera.backgroundColor=new Color(.016f,.032f,.065f);sceneCamera.allowHDR=false;sceneCamera.allowMSAA=true;
            cameraObject.transform.localPosition=new Vector3(0,3.4f,-21);cameraObject.transform.LookAt(stage.TransformPoint(new Vector3(0,1.15f,9)));
            finish=new Material(Resources.Load<Shader>("SkyPulse/Shaders/FlightArchitecture")){name="Overlook architecture / eight saved light networks"};finish.SetFloat("_Architecture",1);finish.SetFloat("_Geology",1);finish.SetColor("_Tint",new Color(.28f,.76f,1));finish.SetColor("_Haze",new Color(.014f,.025f,.055f));finish.SetColor("_KeyTone",SkyPulseFlightEnvironment.KeyLightForWorld(0));finish.SetColor("_FillTone",SkyPulseFlightEnvironment.FillLightForWorld(0));
            var civic=Resources.Load<Mesh>("SkyPulse/BackgroundEdits/world-00-civic");var homes=Resources.Load<Mesh>("SkyPulse/BackgroundEdits/world-00-residential");
            Place(0,civic,new Vector3(-.5f,-1,7),1.30f,-16,0);
            Place(1,homes,new Vector3(-10,-.6f,10),1.05f,13,.14f);
            Place(2,homes,new Vector3(11,-1,12),1.35f,-18,.16f);
            Place(3,civic,new Vector3(-34,.2f,31),1.50f,14,.35f);
            Place(4,homes,new Vector3(35,0,35),1.80f,-12,.40f);
            Bridge(5,Landing(1,1.8f),Landing(0,-1.8f));
            Bridge(6,Landing(0,1.8f),Landing(2,-1.8f));
            texture=new RenderTexture(990,224,24,RenderTextureFormat.ARGB32){name="Panoramic restored city",antiAliasing=2};texture.Create();sceneCamera.targetTexture=texture;
            composite=new Material(Resources.Load<Shader>("SkyPulse/Shaders/TechTreeField")){name="City window composite"};view.texture=texture;view.material=composite;view.raycastTarget=false;
            Present(0,0,0);Render();
        }
        void Place(int slot,Mesh mesh,Vector3 position,float scale,float yaw,float depth)
        {
            var renderer=SkyPulseMeshBuilder.Surface(stage,slot<5?"Occupied city district "+slot:"Connected restoration skyway "+slot,mesh,finish,0);districts[slot]=renderer;renderer.gameObject.layer=30;renderer.transform.localPosition=position;renderer.transform.localScale=Vector3.one*scale;renderer.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var block=new MaterialPropertyBlock();properties[slot]=block;block.SetFloat("_DistrictDepth",depth);block.SetFloat("_RestorationHeight",mesh.bounds.max.y);
            var data=Resources.Load<SkyPulseBackgroundLighting>("SkyPulse/BackgroundEdits/world-00-"+(slot==0||slot==3?"civic":"residential")+"-lighting");
            if(data!=null&&slot<5){block.SetVectorArray("_BeaconPoints",data.Points);block.SetVectorArray("_BeaconColours",data.Colours);}
        }
        Vector3 Landing(int district,float side)=>stage.InverseTransformPoint(districts[district].transform.TransformPoint(new Vector3(side,.29f,0)));
        void Bridge(int slot,Vector3 from,Vector3 to)
        {
            float length=Vector3.Distance(from,to);var b=new SkyPulseMeshBuilder();
            b.Box(Vector3.zero,new Vector3(length,.12f,.55f),new Color(.22f,.32f,.43f,0));
            for(int side=-1;side<=1;side+=2)
            {
                b.Box(new Vector3(0,.28f,side*.25f),new Vector3(length,.035f,.035f),new Color(.36f,.50f,.62f,0));
                b.Box(new Vector3(0,.072f,side*.23f),new Vector3(length,.015f,.022f),new Color(.12f,.70f,.88f,1));
                int posts=Mathf.CeilToInt(length/.65f);for(int i=0;i<=posts;i++)b.Box(new Vector3(Mathf.Lerp(-length*.5f,length*.5f,i/(float)posts),.16f,side*.25f),new Vector3(.025f,.23f,.025f),new Color(.33f,.45f,.57f,0));
            }
            var mesh=b.Finish("Connected city skyway / inset lights and structural handrails");owned.Add(mesh);Place(slot,mesh,(from+to)*.5f,1,0,.08f);districts[slot].transform.localRotation=Quaternion.FromToRotation(Vector3.right,(to-from).normalized);
        }
        public void Present(int restored,int activeSystem,float livePower)
        {
            int next=restored&255;if(mask>=0&&(next&~mask)!=0)pulse=1;mask=next;system=Mathf.Clamp(activeSystem,0,7);power=Mathf.Clamp01(livePower);Apply();
        }
        void Apply()
        {
            for(int i=0;i<districts.Length;i++)
            {
                if(districts[i]==null)continue;var block=properties[i];block.SetFloat(Pulse,ReducedMotion?0:pulse);block.SetFloat(Windows,.84f);block.SetFloat(Beacons,.50f);block.SetFloat(Clock,clock);SkyPulseWorldRestoration.Apply(block,mask,power,system);districts[i].SetPropertyBlock(block);
            }
            if(finish!=null)finish.SetFloat(Clock,clock);
        }
        void Update(){Tick(Time.unscaledDeltaTime);Render();}
        public void Tick(float dt){if(MotionPaused)return;dt=Mathf.Max(0,dt);if(!ReducedMotion)clock+=dt;pulse=ReducedMotion?0:Mathf.Max(0,pulse-dt*1.15f);Apply();}
        public void Render(){if(sceneCamera!=null&&isActiveAndEnabled)sceneCamera.Render();}
        void OnEnable(){if(stage!=null)stage.gameObject.SetActive(true);}
        void OnDisable(){if(stage!=null)stage.gameObject.SetActive(false);}
        void OnDestroy(){if(stage!=null)Destroy(stage.gameObject);if(texture!=null){texture.Release();Destroy(texture);}if(finish!=null)Destroy(finish);if(composite!=null)Destroy(composite);foreach(var mesh in owned)Destroy(mesh);}
    }
}
