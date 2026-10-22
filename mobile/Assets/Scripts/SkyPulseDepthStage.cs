using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    // Shared 3D display used by all three mode Homes and result screens. The
    // camera is isolated from gameplay, so depth/parallax cannot alter hitboxes.
    public sealed class SkyPulseDepthStage : MonoBehaviour
    {
        Transform scene,endless,relay,blackout,core,orbit,platform;SkyPulseRelayGate hoop;Camera camera3D;RenderTexture image;
        Material metal,glass,composite;Mesh ring,crystal;readonly List<Mesh> owned=new List<Mesh>();float clock;static int nextStage;
        readonly MeshRenderer[] networkLights=new MeshRenderer[8];MaterialPropertyBlock networkProperties;
        public int RestoredSystems;
        public SkyPulseMode Mode;public bool ReducedMotion,MotionPaused;
        public Camera StageCamera=>camera3D;
        public void Build(RawImage view)
        {
            scene=new GameObject("SkyPulse dimensional Home stage").transform;scene.position=new Vector3(3000+(nextStage++%64)*30,3000,0);
            image=new RenderTexture(1020,612,24,RenderTextureFormat.ARGB32){name="SkyPulse open Home stage",antiAliasing=4};image.Create();view.texture=image;view.raycastTarget=false;
            composite=new Material(Resources.Load<Shader>("SkyPulse/Shaders/TechTreeField")){name="Home premultiplied edge compositing"};view.material=composite;
            var o=new GameObject("Home optical camera",typeof(Camera));o.transform.SetParent(scene,false);o.transform.localPosition=new Vector3(0,0,-14);camera3D=o.GetComponent<Camera>();camera3D.orthographic=true;camera3D.orthographicSize=3;camera3D.cullingMask=1<<31;camera3D.clearFlags=CameraClearFlags.SolidColor;camera3D.backgroundColor=Color.clear;camera3D.targetTexture=image;camera3D.nearClipPlane=.1f;camera3D.farClipPlane=25;camera3D.allowMSAA=true;camera3D.allowHDR=false;
            metal=new Material(Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics")){name="Home brushed titanium"};metal.SetColor("_BaseColor",new Color(.24f,.34f,.41f));metal.SetFloat("_Roughness",.25f);
            glass=new Material(Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics")){name="Home illuminated glass",renderQueue=3000};glass.SetColor("_BaseColor",new Color(.10f,.32f,.43f,.58f));glass.SetFloat("_Glass",1);glass.SetFloat("_ZWrite",0);glass.SetFloat("_Energy",.06f);
            var b=new SkyPulseMeshBuilder();b.Torus(1.4f,.032f,80,new Color(1,1,1,1));ring=b.Finish("Three-dimensional orbital light guide");owned.Add(ring);
            b=new SkyPulseMeshBuilder();for(int i=0;i<6;i++){float a=i*Mathf.PI/3,c=(i+1)*Mathf.PI/3;var p=new Vector3(Mathf.Cos(a)*.63f,0,Mathf.Sin(a)*.63f);var q=new Vector3(Mathf.Cos(c)*.63f,0,Mathf.Sin(c)*.63f);b.Face(Vector3.up*1.15f,q,p,new Color(1,1,1,.2f));b.Face(Vector3.down*.8f,p,q,new Color(.7f,.85f,1,.8f));}crystal=b.Finish("Suspended heist core volume");owned.Add(crystal);
            endless=Group("Endless sky balcony");relay=Group("Relay orbital launch dock");blackout=Group("Blackout optical command chamber");
            // Open geometry lets the actual world show through. No broad slab,
            // shoulder bars or rectangular support frame sits behind the hero.
            orbit=SkyPulseMeshBuilder.Surface(blackout,"Floating light orbit",ring,glass,2).transform;orbit.localRotation=Quaternion.Euler(68,0,8);orbit.localPosition=new Vector3(0,-.1f,0);
            core=SkyPulseMeshBuilder.Surface(blackout,"Levitating 3D vault core",crystal,glass,3).transform;
            var mechanism=SkyPulseMeshBuilder.Surface(core,"Inner optical mechanism",crystal,metal,1).transform;mechanism.localScale=Vector3.one*.35f;mechanism.localPosition=new Vector3(0,-.10f,0);
            var baseRing=SkyPulseMeshBuilder.Surface(blackout,"Core cradle",ring,metal,0).transform;baseRing.localRotation=Quaternion.Euler(72,0,0);baseRing.localPosition=new Vector3(0,-1.28f,0);baseRing.localScale=Vector3.one*.85f;
            var h=new GameObject("Relay Home hollow titanium hoop");h.transform.SetParent(relay,false);h.transform.localPosition=new Vector3(1.8f,.15f,.6f);h.transform.localScale=Vector3.one*.82f;hoop=h.AddComponent<SkyPulseRelayGate>();hoop.Build(false);
            platform=SkyPulseMeshBuilder.Surface(endless,"Sky balcony engine ring",ring,glass,2).transform;platform.localPosition=new Vector3(0,-.9f,0);platform.localRotation=Quaternion.Euler(70,0,0);platform.localScale=new Vector3(1.7f,1,1);
            for(int i=0;i<3;i++)
            {
                var halo=SkyPulseMeshBuilder.Surface(endless,"Open sky light horizon "+i,ring,i==1?metal:glass,0).transform;halo.localPosition=new Vector3(0,-.85f-i*.19f,1+i*.8f);halo.localRotation=Quaternion.Euler(73,0,i*3);halo.localScale=new Vector3(2.2f-i*.3f,1.2f-i*.1f,1);
            }
            networkProperties=new MaterialPropertyBlock();
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;networkLights[i]=SkyPulseMeshBuilder.Surface(blackout,"Restored system beacon "+i,crystal,metal,3);
                networkLights[i].transform.localPosition=new Vector3(Mathf.Cos(a)*2.12f,Mathf.Sin(a)*1.2f-.15f,.35f);networkLights[i].transform.localScale=Vector3.one*.115f;
            }
            foreach(var t in scene.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;Tick(0);
        }
        Transform Group(string name){var g=new GameObject(name).transform;g.SetParent(scene,false);return g;}
        void Update()=>Tick(Time.unscaledDeltaTime);
        public void Tick(float dt)
        {
            if(scene==null||MotionPaused)return;if(!ReducedMotion)clock+=Mathf.Max(0,dt);
            endless.gameObject.SetActive(Mode==SkyPulseMode.EndlessRoute);relay.gameObject.SetActive(Mode==SkyPulseMode.PulseRelay);blackout.gameObject.SetActive(Mode==SkyPulseMode.BlackoutProtocol);
            float t=ReducedMotion?0:clock;var parallax=Quaternion.Euler(Mathf.Sin(t*.21f)*1.2f,Mathf.Sin(t*.18f)*2,0);endless.localRotation=relay.localRotation=blackout.localRotation=parallax;
            // Preserve the offscreen scene's position while giving every visible
            // face a small, smooth change in reflection and geometric parallax.
            core.localRotation=Quaternion.Euler(12,25+t*13,4);core.localPosition=new Vector3(0,.12f+Mathf.Sin(t*1.1f)*.08f,-.1f);
            orbit.localRotation=Quaternion.Euler(68,Mathf.Sin(t*.3f)*8,t*8);platform.localRotation=Quaternion.Euler(70,Mathf.Sin(t*.25f)*5,0);
            hoop.SetVisual(true,t*.3f,0);glass.SetColor("_GlowColor",Mode==SkyPulseMode.BlackoutProtocol?new Color(.15f,1,.73f):Mode==SkyPulseMode.PulseRelay?new Color(1,.64f,.22f):new Color(.24f,.72f,1));
            for(int i=0;i<8;i++){networkProperties.SetColor("_GlowColor",i<RestoredSystems?new Color(.20f,1,.70f):new Color(.2f,.34f,.4f));networkProperties.SetFloat("_Energy",i<RestoredSystems?.85f:.02f);networkLights[i].SetPropertyBlock(networkProperties);}
        }
        public void Render(){if(camera3D!=null&&isActiveAndEnabled)camera3D.Render();}
        void OnEnable(){if(scene!=null)scene.gameObject.SetActive(true);if(camera3D!=null)camera3D.enabled=true;}
        void OnDisable(){if(camera3D!=null)camera3D.enabled=false;if(scene!=null)scene.gameObject.SetActive(false);}
        void OnDestroy(){if(scene!=null)Destroy(scene.gameObject);if(image!=null){image.Release();Destroy(image);}foreach(var m in owned)Destroy(m);if(metal!=null)Destroy(metal);if(glass!=null)Destroy(glass);if(composite!=null)Destroy(composite);}
    }
}
