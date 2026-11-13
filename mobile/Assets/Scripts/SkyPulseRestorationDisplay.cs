using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    // A real, lit model of the same authored Neon island used by Endless Route.
    // This is a view of campaign state, not another save or reward system.
    public sealed class SkyPulseRestorationDisplay : MonoBehaviour
    {
        Transform stage,island;Camera sceneCamera;RenderTexture texture;Material finish,composite;MeshRenderer district;MaterialPropertyBlock properties;
        static readonly int Clock=Shader.PropertyToID("_Clock"),Windows=Shader.PropertyToID("_WindowGlow"),Beacons=Shader.PropertyToID("_BeaconGlow"),Pulse=Shader.PropertyToID("_RestorePulse");
        float clock,power,pulse;int mask=-1,system;public bool ReducedMotion,MotionPaused;
        public int RestoredMask=>mask&255;public int ActiveVault=>system;public float Phase=>clock;public float LivePower=>power;
        public RenderTexture Texture=>texture;
        public void Build(RawImage view)
        {
            stage=new GameObject("Live restoration island / private stage").transform;stage.position=new Vector3(14000,14000,0);
            var cameraObject=new GameObject("Restoration island perspective",typeof(Camera));cameraObject.transform.SetParent(stage,false);
            sceneCamera=cameraObject.GetComponent<Camera>();sceneCamera.enabled=false;sceneCamera.orthographic=false;sceneCamera.fieldOfView=29;sceneCamera.aspect=640f/232;
            sceneCamera.nearClipPlane=.1f;sceneCamera.farClipPlane=60;sceneCamera.cullingMask=1<<30;sceneCamera.clearFlags=CameraClearFlags.SolidColor;sceneCamera.backgroundColor=Color.clear;sceneCamera.allowHDR=false;sceneCamera.allowMSAA=true;
            var mesh=Resources.Load<Mesh>("SkyPulse/BackgroundEdits/world-00-civic");
            finish=new Material(Resources.Load<Shader>("SkyPulse/Shaders/FlightArchitecture")){name="Restoration island / saved city lights"};finish.SetFloat("_Architecture",1);finish.SetFloat("_Geology",1);finish.SetColor("_Tint",new Color(.28f,.76f,1));finish.SetColor("_Haze",new Color(.014f,.025f,.055f));finish.SetColor("_KeyTone",SkyPulseFlightEnvironment.KeyLightForWorld(0));finish.SetColor("_FillTone",SkyPulseFlightEnvironment.FillLightForWorld(0));
            district=SkyPulseMeshBuilder.Surface(stage,"Neon civic island / restoration miniature",mesh,finish,0);island=district.transform;island.localRotation=Quaternion.Euler(0,-16,0);district.gameObject.layer=30;
            properties=new MaterialPropertyBlock();var data=Resources.Load<SkyPulseBackgroundLighting>("SkyPulse/BackgroundEdits/world-00-civic-lighting");
            if(data!=null){properties.SetVectorArray("_BeaconPoints",data.Points);properties.SetVectorArray("_BeaconColours",data.Colours);}
            // Fit the complete authored bounds; never clip a manually edited tower.
            var bounds=district.bounds;float height=Mathf.Max(bounds.size.y,bounds.size.x/sceneCamera.aspect)*1.10f;
            var centre=stage.InverseTransformPoint(bounds.center);float distance=height*.5f/Mathf.Tan(sceneCamera.fieldOfView*.5f*Mathf.Deg2Rad)+bounds.extents.z;
            cameraObject.transform.localPosition=centre+new Vector3(0,height*.10f,-distance);cameraObject.transform.LookAt(stage.TransformPoint(centre));
            texture=new RenderTexture(640,232,24,RenderTextureFormat.ARGB32){name="Live district restoration",antiAliasing=2};texture.Create();sceneCamera.targetTexture=texture;
            composite=new Material(Resources.Load<Shader>("SkyPulse/Shaders/TechTreeField")){name="Restoration premultiplied composite"};view.texture=texture;view.material=composite;view.raycastTarget=false;
            Present(0,0,0);Render();
        }
        public void Present(int restored,int activeSystem,float livePower)
        {
            int next=restored&255;if(mask>=0&&(next&~mask)!=0)pulse=1;mask=next;system=Mathf.Clamp(activeSystem,0,7);power=Mathf.Clamp01(livePower);Apply();
        }
        void Apply()
        {
            if(district==null)return;
            properties.SetFloat(Pulse,ReducedMotion?0:pulse);properties.SetFloat(Windows,.84f);properties.SetFloat(Beacons,.50f);properties.SetFloat(Clock,clock);SkyPulseWorldRestoration.Apply(properties,mask,power,system);district.SetPropertyBlock(properties);finish.SetFloat(Clock,clock);
        }
        void Update(){Tick(Time.unscaledDeltaTime);Render();}
        public void Tick(float dt){if(MotionPaused)return;dt=Mathf.Max(0,dt);if(!ReducedMotion)clock+=dt;pulse=ReducedMotion?0:Mathf.Max(0,pulse-dt*1.15f);Apply();}
        public void Render(){if(sceneCamera!=null&&isActiveAndEnabled)sceneCamera.Render();}
        void OnEnable(){if(stage!=null)stage.gameObject.SetActive(true);}
        void OnDisable(){if(stage!=null)stage.gameObject.SetActive(false);}
        void OnDestroy(){if(stage!=null)Destroy(stage.gameObject);if(texture!=null){texture.Release();Destroy(texture);}if(finish!=null)Destroy(finish);if(composite!=null)Destroy(composite);}
    }
}
