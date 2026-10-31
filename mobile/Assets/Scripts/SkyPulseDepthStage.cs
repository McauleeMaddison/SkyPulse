using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    // Shared 3D display used by all three mode Homes and result screens. The
    // camera is isolated from gameplay, so depth/parallax cannot alter hitboxes.
    public sealed class SkyPulseDepthStage : MonoBehaviour
    {
        Transform scene,endless,relay,blackout,core,orbit;SkyPulseRelayGate hoop;Camera camera3D;RenderTexture image;
        Material metal,glass,composite,obsidian,signalLight,opticHousing,silver;Mesh ring,crystal;readonly List<Mesh> owned=new List<Mesh>();float clock;static int nextStage;
        Transform relayChargeA,relayChargeB,heistDesk,signalPacket;
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
            metal=new Material(Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics")){name="Home brushed titanium"};metal.SetColor("_BaseColor",new Color(.27f,.39f,.47f));metal.SetFloat("_Roughness",.25f);
            glass=new Material(Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics")){name="Home illuminated glass",renderQueue=3000};glass.SetColor("_BaseColor",new Color(.14f,.47f,.61f,.50f));glass.SetFloat("_Glass",1);glass.SetFloat("_ZWrite",0);glass.SetFloat("_Energy",.23f);
            var b=new SkyPulseMeshBuilder();b.Torus(1.4f,.032f,80,new Color(1,1,1,1));ring=b.Finish("Three-dimensional orbital light guide");owned.Add(ring);
            b=new SkyPulseMeshBuilder();for(int i=0;i<6;i++){float a=i*Mathf.PI/3,c=(i+1)*Mathf.PI/3;var p=new Vector3(Mathf.Cos(a)*.63f,0,Mathf.Sin(a)*.63f);var q=new Vector3(Mathf.Cos(c)*.63f,0,Mathf.Sin(c)*.63f);b.Face(Vector3.up*1.15f,q,p,new Color(1,1,1,.2f));b.Face(Vector3.down*.8f,p,q,new Color(.7f,.85f,1,.8f));}crystal=b.Finish("Suspended heist core volume");owned.Add(crystal);
            endless=Group("Endless sky balcony");relay=Group("Relay orbital launch dock");blackout=Group("Blackout optical command chamber");
            // Each Home shows actual mode equipment in a transparent, isolated
            // 3D scene. Blackout previews a real source-to-uplink optical route.
            BuildCommandConsole();
            var h=new GameObject("Relay Home hollow titanium hoop");h.transform.SetParent(relay,false);h.transform.localPosition=new Vector3(1.8f,.15f,.6f);h.transform.localScale=Vector3.one*.82f;hoop=h.AddComponent<SkyPulseRelayGate>();hoop.Build(false);
            // Endless Home leaves the original bird silhouette unobstructed.
            // The former blue observation rail read as a stray line behind it.
            b=new SkyPulseMeshBuilder();Tube(b,new Vector3(1.8f,.15f,1.2f),2.24f,2.30f,.075f,35,145,Color.white);Tube(b,new Vector3(1.8f,.15f,1.8f),2.24f,2.30f,.045f,215,325,Color.white);
            var gantry=b.Finish("Swept orbital launch cradle");owned.Add(gantry);SkyPulseMeshBuilder.Surface(relay,"Orbital launch cradle",gantry,metal,0);
            var chargeProperties=new MaterialPropertyBlock();chargeProperties.SetColor("_GlowColor",new Color(.30f,.88f,1));chargeProperties.SetFloat("_Energy",.45f);
            relayChargeA=SkyPulseMeshBuilder.Surface(relay,"First gate-charge core",crystal,glass,2).transform;relayChargeA.localPosition=new Vector3(-2.1f,-.95f,0);relayChargeA.localScale=Vector3.one*.25f;relayChargeA.GetComponent<MeshRenderer>().SetPropertyBlock(chargeProperties);
            relayChargeB=SkyPulseMeshBuilder.Surface(relay,"Second gate-charge core",crystal,glass,2).transform;relayChargeB.localPosition=new Vector3(-.55f,.92f,.35f);relayChargeB.localScale=Vector3.one*.25f;relayChargeB.GetComponent<MeshRenderer>().SetPropertyBlock(chargeProperties);
            foreach(var t in scene.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;Tick(0);
        }
        void BuildCommandConsole()
        {
            heistDesk=new GameObject("Signal Heist tabletop demonstration").transform;
            heistDesk.SetParent(blackout,false);heistDesk.localPosition=new Vector3(0,.05f,.2f);
            heistDesk.localRotation=Quaternion.Euler(42,-18,0);
            obsidian=new Material(metal){name="Home obsidian optical worktop"};
            obsidian.SetColor("_BaseColor",new Color(.065f,.115f,.155f));obsidian.SetFloat("_BoardDeck",1);obsidian.SetFloat("_Roughness",.4f);
            signalLight=new Material(Resources.Load<Shader>("SkyPulse/Shaders/FlightLight")){name="Home live optical route"};
            var b=new SkyPulseMeshBuilder();b.BevelBox(6.2f,3.15f,.30f);
            Part(heistDesk,"Raised optical worktop",Keep(b,"Home bevelled optical chassis"),obsidian);
            b=new SkyPulseMeshBuilder();b.BevelBox(6.02f,2.98f,.42f);
            var lower=Part(heistDesk,"Deep titanium console housing",Keep(b,"Home supported lower housing"),metal).transform;
            lower.localPosition=new Vector3(0,0,.42f);
            b=new SkyPulseMeshBuilder();
            // Machined perimeter, corner fasteners and recessed side vents reveal
            // a physical housing without a large decorative backing card.
            for(int side=-1;side<=1;side+=2)
            {
                b.Box(new Vector3(side*3.02f,0,-.23f),new Vector3(.045f,2.9f,.06f),new Color(.8f,.95f,1,0));
                b.Box(new Vector3(0,side*1.49f,-.25f),new Vector3(5.9f,.035f,.04f),new Color(.8f,.95f,1,0));
                for(int end=-1;end<=1;end+=2)
                {
                    b.Box(new Vector3(side*2.86f,end*1.34f,-.31f),new Vector3(.09f,.09f,.035f),Color.white);
                    for(int vent=0;vent<4;vent++)b.Box(new Vector3(side*3.06f,end*.75f+vent*.11f,-.10f),new Vector3(.025f,.045f,.10f),new Color(.17f,.25f,.29f,0));
                }
            }
            Part(heistDesk,"Titanium perimeter and service details",Keep(b,"Home console rim and vents"),metal);
            b=new SkyPulseMeshBuilder();b.Profile(new[]{new Vector2(0,.05f),new Vector2(.36f,.05f),new Vector2(.41f,-.03f),new Vector2(.41f,-.12f),new Vector2(.34f,-.18f),new Vector2(0,-.18f)});
            var socket=Keep(b,"Home recessed optical socket");
            opticHousing=new Material(metal){name="Home graphite optic housings"};opticHousing.SetColor("_BaseColor",new Color(.045f,.085f,.11f));opticHousing.SetFloat("_Roughness",.27f);
            silver=new Material(metal){name="Home polished silver optical faces"};silver.SetColor("_BaseColor",new Color(.57f,.74f,.80f));silver.SetFloat("_Roughness",.10f);
            var source=Part(heistDesk,"Directional source housing",socket,opticHousing).transform;source.localPosition=new Vector3(-2.2f,-.65f,-.30f);
            var receive=Part(heistDesk,"Uplink receiver housing",socket,opticHousing).transform;receive.localPosition=new Vector3(2.2f,.65f,-.30f);
            b=new SkyPulseMeshBuilder();b.ExtrudePrism(new[]{new Vector2(-.23f,-.11f),new Vector2(.03f,-.11f),new Vector2(.03f,-.21f),new Vector2(.28f,0),new Vector2(.03f,.21f),new Vector2(.03f,.11f),new Vector2(-.23f,.11f)},-.15f,-.02f);
            var arrow=Part(source,"Raised source direction",Keep(b,"Home energy-source arrow"),silver).transform;arrow.localPosition=Vector3.back*.19f;
            b=new SkyPulseMeshBuilder();b.BevelBox(.72f,.16f,.20f);var mirror=Keep(b,"Home silver prism with depth");
            for(int index=0;index<2;index++)
            {
                var hub=Part(heistDesk,"Mirror socket "+index,socket,opticHousing).transform;hub.localPosition=new Vector3(-.7f,index==0?-.65f:.65f,-.30f);
                var m=Part(hub,"Signal-routing mirror "+index,mirror,silver).transform;m.localPosition=Vector3.back*.25f;m.localRotation=Quaternion.Euler(0,0,45);
                var trim=Part(hub,"Silver machined socket collar",ring,silver).transform;trim.localPosition=Vector3.back*.19f;trim.localScale=Vector3.one*.275f;
            }
            core=Part(heistDesk,"Levitating 3D vault core",crystal,glass).transform;core.localScale=Vector3.one*.40f;
            var cradle=Part(heistDesk,"Core precision cradle",socket,opticHousing).transform;cradle.localPosition=new Vector3(.60f,.65f,-.30f);
            orbit=Part(cradle,"Core live index ring",ring,glass).transform;orbit.localPosition=Vector3.back*.25f;orbit.localScale=Vector3.one*.24f;
            b=new SkyPulseMeshBuilder();
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f;var at=new Vector3(Mathf.Cos(a)*.24f,Mathf.Sin(a)*.24f,-.22f);
                b.Box(at,new Vector3(.055f,.055f,.22f),new Color(.8f,.95f,1,0));
                b.Box(at+Vector3.back*.13f,new Vector3(.07f,.07f,.035f),new Color(.7f,1,.9f,1));
            }
            Part(receive,"Four-post uplink crown",Keep(b,"Home receiver crown"),silver);
            var lens=Part(receive,"Refracting receiver lens",crystal,glass).transform;lens.localPosition=Vector3.back*.38f;lens.localScale=Vector3.one*.24f;
            var sourceRim=Part(source,"Silver source collar",ring,silver).transform;sourceRim.localPosition=Vector3.back*.19f;sourceRim.localScale=Vector3.one*.275f;
            var receiveRim=Part(receive,"Silver receiver collar",ring,silver).transform;receiveRim.localPosition=Vector3.back*.19f;receiveRim.localScale=Vector3.one*.275f;
            b=new SkyPulseMeshBuilder();
            HomeBeam(b,new Vector2(-2.2f,-.65f),new Vector2(-.7f,-.65f));
            HomeBeam(b,new Vector2(-.7f,-.65f),new Vector2(-.7f,.65f));
            HomeBeam(b,new Vector2(-.7f,.65f),new Vector2(2.2f,.65f));
            Part(heistDesk,"Source to core to uplink light route",Keep(b,"Home live energy fibre"),signalLight);
            b=new SkyPulseMeshBuilder();b.Box(Vector3.zero,new Vector3(.09f,.09f,.045f),new Color(.7f,1,.95f,.9f));
            signalPacket=Part(heistDesk,"Travelling optical energy packet",Keep(b,"Home signal packet"),signalLight).transform;
            networkProperties=new MaterialPropertyBlock();
            for(int i=0;i<8;i++)
            {
                networkLights[i]=Part(heistDesk,"Restored system beacon "+i,socket,metal);
                networkLights[i].transform.localPosition=new Vector3(-2.28f+i*.65f,-1.27f,-.34f);
                networkLights[i].transform.localScale=new Vector3(.24f,.24f,.24f);
            }
        }
        static void HomeBeam(SkyPulseMeshBuilder b,Vector2 from,Vector2 to)
        {
            var delta=to-from;var centre=(from+to)*.5f;
            var size=new Vector3(Mathf.Abs(delta.x)+.025f,Mathf.Abs(delta.y)+.025f,.012f);
            b.Box(new Vector3(centre.x,centre.y,-.54f),size,new Color(.19f,1,.80f,.48f));
            size.x=delta.x==0?.008f:size.x;size.y=delta.y==0?.008f:size.y;
            b.Box(new Vector3(centre.x,centre.y,-.55f),size,new Color(.80f,1,.96f,.75f));
        }
        Mesh Keep(SkyPulseMeshBuilder builder,string name){var value=builder.Finish(name);owned.Add(value);return value;}
        static MeshRenderer Part(Transform parent,string name,Mesh mesh,Material finish)=>SkyPulseMeshBuilder.Surface(parent,name,mesh,finish,0);
        Transform Group(string name){var g=new GameObject(name).transform;g.SetParent(scene,false);return g;}
        static void Tube(SkyPulseMeshBuilder b,Vector3 centre,float rx,float ry,float thickness,float start,float end,Color colour)
        {
            const int segments=64,sides=8;
            for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
            {
                float a=(start+(end-start)*i/segments)*Mathf.Deg2Rad,c=(start+(end-start)*(i+1)/segments)*Mathf.Deg2Rad,u=j*Mathf.PI*2/sides,w=(j+1)*Mathf.PI*2/sides;
                var na=new Vector3(Mathf.Cos(a)*Mathf.Cos(u),Mathf.Sin(a)*Mathf.Cos(u),Mathf.Sin(u));var nc=new Vector3(Mathf.Cos(c)*Mathf.Cos(u),Mathf.Sin(c)*Mathf.Cos(u),Mathf.Sin(u));
                b.SmoothQuad(centre+TubePoint(a,u,rx,ry,thickness),centre+TubePoint(c,u,rx,ry,thickness),centre+TubePoint(c,w,rx,ry,thickness),centre+TubePoint(a,w,rx,ry,thickness),na,nc,colour);
            }
        }
        static Vector3 TubePoint(float a,float u,float rx,float ry,float t)=>new Vector3(Mathf.Cos(a)*(rx+Mathf.Cos(u)*t),Mathf.Sin(a)*(ry+Mathf.Cos(u)*t),Mathf.Sin(u)*t);
        void Update()=>Tick(Time.unscaledDeltaTime);
        public void Tick(float dt)
        {
            if(scene==null||MotionPaused)return;if(!ReducedMotion)clock+=Mathf.Max(0,dt);
            endless.gameObject.SetActive(Mode==SkyPulseMode.EndlessRoute);relay.gameObject.SetActive(Mode==SkyPulseMode.PulseRelay);blackout.gameObject.SetActive(Mode==SkyPulseMode.BlackoutProtocol);
            // A long-lens perspective view gives the Blackout exhibit real
            // foreshortening; the flight-mode hero composition stays unchanged.
            camera3D.orthographic=Mode!=SkyPulseMode.BlackoutProtocol;camera3D.fieldOfView=24;
            float t=ReducedMotion?0:clock;glass.SetFloat("_Clock",t);glass.SetFloat("_Hologram",Mode==SkyPulseMode.BlackoutProtocol?1:0);var parallax=Quaternion.Euler(Mathf.Sin(t*.21f)*1.2f,Mathf.Sin(t*.18f)*2,0);endless.localRotation=relay.localRotation=blackout.localRotation=parallax;
            // Preserve the offscreen scene's position while giving every visible
            // face a small, smooth change in reflection and geometric parallax.
            core.localRotation=Quaternion.Euler(10,25+Mathf.Sin(t*.55f)*12,4);
            core.localPosition=new Vector3(.60f,.66f+(ReducedMotion?0:Mathf.Sin(t*1.1f)*.025f),-.58f);
            orbit.localRotation=Quaternion.Euler(0,0,t*8);
            relayChargeA.localRotation=Quaternion.Euler(12,t*22,8);relayChargeB.localRotation=Quaternion.Euler(-8,45-t*19,-12);
            float demo=ReducedMotion?4:Mathf.Repeat(t,7);
            hoop.SetCharge(demo<1.5f?0:demo<3?1:2,demo>=3,t*.3f,0);
            glass.SetColor("_GlowColor",Mode==SkyPulseMode.BlackoutProtocol?new Color(.15f,1,.73f):Mode==SkyPulseMode.PulseRelay?new Color(.30f,.88f,1):new Color(.24f,.72f,1));
            float path=ReducedMotion?4.2f:Mathf.Repeat(t*.95f,5.7f);
            signalPacket.localPosition=path<1.5f?new Vector3(-2.2f+path,-.65f,-.58f):path<2.8f?new Vector3(-.7f,-.65f+path-1.5f,-.58f):new Vector3(-.7f+path-2.8f,.65f,-.58f);
            for(int i=0;i<8;i++){networkProperties.SetColor("_GlowColor",i<RestoredSystems?new Color(.20f,1,.70f):new Color(.2f,.34f,.4f));networkProperties.SetFloat("_Energy",i<RestoredSystems?.85f:.02f);networkLights[i].SetPropertyBlock(networkProperties);}
        }
        public void Render(){if(camera3D!=null&&isActiveAndEnabled)camera3D.Render();}
        void OnEnable(){if(scene!=null)scene.gameObject.SetActive(true);if(camera3D!=null)camera3D.enabled=true;}
        void OnDisable(){if(camera3D!=null)camera3D.enabled=false;if(scene!=null)scene.gameObject.SetActive(false);}
        void OnDestroy(){if(scene!=null)Destroy(scene.gameObject);if(image!=null){image.Release();Destroy(image);}foreach(var m in owned)Destroy(m);if(metal!=null)Destroy(metal);if(glass!=null)Destroy(glass);if(composite!=null)Destroy(composite);if(obsidian!=null)Destroy(obsidian);if(signalLight!=null)Destroy(signalLight);if(opticHousing!=null)Destroy(opticHousing);if(silver!=null)Destroy(silver);}
    }
}
