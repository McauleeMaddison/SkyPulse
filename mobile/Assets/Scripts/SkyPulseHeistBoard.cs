using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    // One offscreen 3D scene, one render target, and a fixed pool of 49 modules.
    // This renderer observes the optical simulation; it never rotates a logical
    // piece, spends trace, decides a win, or postpones a player's next input.
    public sealed class SkyPulseHeistBoard : MonoBehaviour
    {
        sealed class Module
        {
            public Transform Root,Rotor,Mirror,Splitter,Core,Lens,LeftDoor,RightDoor,Sensor,Emitter,Receiver;
            public MeshRenderer Ring,Glass,Base,Collar,LensRenderer,MirrorRenderer,SplitterRenderer,EmitterRenderer,ReceiverRenderer,MirrorFrame,Ripple;
            public float Angle,TargetAngle,Power,Hit,Door,LastPower,Charge;
            public bool WasPreview;
            public int Target;
            public char Kind;
        }
        const int Layer=31,Capacity=200*24;
        const float DeskPitch=28;
        static readonly float VerticalPitch=1.025f/Mathf.Cos(DeskPitch*Mathf.Deg2Rad);
        readonly Module[] modules=new Module[49];
        readonly Vector3[] beamVertices=new Vector3[Capacity];readonly Color[] beamColours=new Color[Capacity];
        readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> materials=new List<Material>();
        readonly Mesh[] platens=new Mesh[8],channels=new Mesh[8];
        MaterialPropertyBlock properties;
        static readonly int EnergyId=Shader.PropertyToID("_Energy"),GlowId=Shader.PropertyToID("_GlowColor"),VisibilityId=Shader.PropertyToID("_Visibility"),RevealId=Shader.PropertyToID("_Reveal");
        Transform scene,console,projectionField;Camera opticalCamera;RenderTexture texture;Mesh beamMesh;Material metal,accent,glass,dark,light,mirrorGlass,contact,projection;
        Mesh baseMesh,ringMesh,signalRingMesh,rippleMesh,mirrorMesh,mirrorFrameMesh,splitterMesh,coreMesh,lensMesh,doorMesh,sensorMesh,contactMesh,emitterMesh,receiverMesh;
        MeshRenderer platenRenderer,channelRenderer;
        int beamUsed;float clock,pitch=1.55f;RectInt layout;
        public bool ReducedMotion,MotionPaused;
        public BlackoutSession Circuit {get;private set;}
        public BlackoutSession Preview {get;private set;}
        public int PreviewTile {get;private set;}=-1;
        public float Phase=>clock;
        public float Reveal;
        public RenderTexture Texture=>texture;
        public int PooledModules=>modules.Length;
        public float OpticScale {get;private set;}=1;
        public float VisualAngle(int tile)
        {
            // Mirrors repeat every half turn. Return the equivalent near the
            // logical orientation while the physical rotor continues forward.
            var m=modules[tile];float period=Circuit!=null&&Circuit.Puzzle.Tile(tile)=='M'?180:360;
            return m.Target*90+Mathf.Repeat(m.Angle-m.Target*90+period*.5f,period)-period*.5f;
        }
        static Color Mint=>new Color(.14f,1,.78f);
        static Color Tint(char c)=>c=='!'?new Color(1,.2f,.18f):c=='Q'||c=='H'?new Color(.63f,.39f,1):c=='E'?new Color(1,.68f,.27f):Mint;
        public void Build(RawImage view)
        {
            properties=new MaterialPropertyBlock();
            scene=new GameObject("SkyPulse optical stage (private layer)").transform;scene.position=new Vector3(2000,2000,0);
            texture=new RenderTexture(1080,1080,24,RenderTextureFormat.ARGB32){name="Signal Heist 3D console",antiAliasing=4};texture.Create();view.texture=texture;view.raycastTarget=false;
            var composite=new Material(Resources.Load<Shader>("SkyPulse/Shaders/TechTreeField")){name="Optical console premultiplied compositing"};materials.Add(composite);view.material=composite;
            var cameraObject=new GameObject("Optical console camera",typeof(Camera));opticalCamera=cameraObject.GetComponent<Camera>();cameraObject.transform.SetParent(scene,false);cameraObject.transform.localPosition=new Vector3(0,0,-20);
            opticalCamera.orthographic=true;opticalCamera.orthographicSize=5.5f;opticalCamera.nearClipPlane=.1f;opticalCamera.farClipPlane=40;opticalCamera.cullingMask=1<<Layer;opticalCamera.clearFlags=CameraClearFlags.SolidColor;opticalCamera.backgroundColor=Color.clear;opticalCamera.targetTexture=texture;opticalCamera.allowHDR=false;opticalCamera.allowMSAA=true;
            metal=Material("Satin black titanium edges",new Color(.26f,.36f,.44f),.38f);accent=Material("Recessed optical guides",new Color(.045f,.16f,.18f),.38f);dark=Material("Smoked ceramic mounts",new Color(.055f,.078f,.10f),.72f);
            glass=Material("Smoked holographic optical glass",new Color(.045f,.25f,.29f,.40f),.12f,true);glass.SetFloat("_Hologram",.48f);
            mirrorGlass=Material("Smoked glass mirror prism",new Color(.075f,.18f,.22f,.48f),.08f,true);mirrorGlass.SetFloat("_Hologram",.16f);
            projection=Material("Suspended holographic field",new Color(.025f,.13f,.18f,.028f),.18f,true);projection.SetFloat("_Hologram",1);projection.SetColor("_GlowColor",new Color(.14f,.72f,.92f));
            light=new Material(Resources.Load<Shader>("SkyPulse/Shaders/HeistBeam")){name="Feathered live optical fibre"};materials.Add(light);
            var receiverMetal=Material("Titanium uplink frame",new Color(.25f,.31f,.34f),.40f);
            var sourceGuide=Material("Source directional projection",new Color(.02f,.18f,.20f,.40f),.20f,true);sourceGuide.SetFloat("_Hologram",1);
            var pulse=Material("Transient optical charge wave",new Color(.10f,.40f,.43f,.18f),.13f,true);pulse.SetFloat("_Hologram",1);
            // A fixed, deeper viewing angle reveals the real housing and optic
            // thickness. Compensated row spacing keeps thumb targets generous.
            console=new GameObject("Angled physical optical desk").transform;console.SetParent(scene,false);console.localPosition=Vector3.up*.16f;console.localRotation=Quaternion.Euler(DeskPitch,0,0);
            contact=Material("Soft optical contact shadow",new Color(0,0,0,.20f),1);contact.SetFloat("_ContactShadow",1);contact.SetFloat("_ZWrite",0);contact.renderQueue=2990;
            BuildMeshes();
            var platenMaterial=Material("Obsidian optical worktop",new Color(.042f,.062f,.078f),.76f);platenMaterial.SetFloat("_BoardDeck",1);
            platenRenderer=Part(console,"Raised optical console chassis",platens[0],platenMaterial);
            var channelMaterial=Material("Recessed signal guides",new Color(.021f,.042f,.055f),.85f);
            channelRenderer=Part(console,"Etched possible signal channels",channels[0],channelMaterial);channelRenderer.transform.localPosition=new Vector3(0,0,.215f);
            var field=new SkyPulseMeshBuilder();field.BevelBox(9.7f,9.7f,.020f);projectionField=Part(console,"Transparent suspended projection field",Keep(field,"Optical field with bevelled depth"),projection).transform;projectionField.localPosition=new Vector3(0,0,.225f);
            for(int i=0;i<49;i++)
            {
                var m=new Module();m.Root=new GameObject("Optical module "+i).transform;m.Root.SetParent(console,false);
                var shadow=Part(m.Root,"Soft floating contact",contactMesh,contact);shadow.transform.localPosition=new Vector3(.04f,-.08f,.18f);
                m.Base=Part(m.Root,"Ceramic socket",baseMesh,dark);m.Collar=Part(m.Root,"Machined lens collar",ringMesh,metal);m.Collar.transform.localPosition=new Vector3(0,0,-.12f);
                m.Ring=Part(m.Root,"Recessed luminous circuit gasket",signalRingMesh,accent);m.Ring.transform.localPosition=new Vector3(0,0,-.255f);
                m.Ripple=Part(m.Root,"Pooled charge impulse",rippleMesh,pulse);m.Ripple.transform.localPosition=new Vector3(0,0,-.33f);m.Ripple.enabled=false;
                m.Rotor=new GameObject("Optical rotor").transform;m.Rotor.SetParent(m.Root,false);m.Rotor.localPosition=Vector3.back*.10f;
                m.MirrorRenderer=Part(m.Rotor,"Faceted smoked-glass mirror prism",mirrorMesh,mirrorGlass);m.Mirror=m.MirrorRenderer.transform;m.Mirror.localRotation=Quaternion.Euler(0,0,45);
                m.MirrorFrame=Part(m.Rotor,"Fine titanium prism edge frame",mirrorFrameMesh,metal);m.MirrorFrame.transform.localRotation=Quaternion.Euler(0,0,45);
                m.SplitterRenderer=Part(m.Rotor,"Triangular beam splitter",splitterMesh,glass);m.Splitter=m.SplitterRenderer.transform;
                m.Core=Part(m.Root,"Suspended faceted optical core",coreMesh,glass).transform;m.Core.localPosition=new Vector3(0,0,-.30f);m.Glass=m.Core.GetComponent<MeshRenderer>();
                m.LensRenderer=Part(m.Root,"Uplink lens",lensMesh,glass);m.Lens=m.LensRenderer.transform;m.Lens.localPosition=new Vector3(0,0,-.20f);
                m.EmitterRenderer=Part(m.Root,"Directional source emitter",emitterMesh,sourceGuide);m.Emitter=m.EmitterRenderer.transform;
                m.ReceiverRenderer=Part(m.Root,"Amber uplink receiver crown",receiverMesh,receiverMetal);m.Receiver=m.ReceiverRenderer.transform;
                m.LeftDoor=Part(m.Root,"Left shutter blade",doorMesh,metal).transform;m.RightDoor=Part(m.Root,"Right shutter blade",doorMesh,metal).transform;
                m.Sensor=Part(m.Root,"Security scanner",sensorMesh,accent).transform;
                modules[i]=m;
            }
            beamMesh=new Mesh{name="Pooled live optical fibre"};beamMesh.MarkDynamic();beamMesh.vertices=beamVertices;beamMesh.colors=beamColours;var triangles=new int[Capacity/4*6];
            var uv=new Vector2[Capacity];
            for(int i=0;i<Capacity/4;i++){int a=i*4,b=i*6;triangles[b]=a;triangles[b+1]=a+1;triangles[b+2]=a+2;triangles[b+3]=a;triangles[b+4]=a+2;triangles[b+5]=a+3;uv[a]=Vector2.zero;uv[a+1]=Vector2.up;uv[a+2]=Vector2.one;uv[a+3]=Vector2.right;}beamMesh.uv=uv;beamMesh.triangles=triangles;beamMesh.bounds=new Bounds(Vector3.zero,new Vector3(15,15,5));meshes.Add(beamMesh);Part(console,"Flowing beam and light packets",beamMesh,light);
            foreach(var t in scene.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
        }
        Material Material(string name,Color colour,float roughness,bool transparent=false)
        {
            var m=new Material(Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics")){name=name,enableInstancing=true};m.SetColor("_BaseColor",colour);m.SetFloat("_Roughness",roughness);m.SetFloat("_OpticFinish",1);m.SetFloat("_Glass",transparent?1:0);if(transparent){m.SetFloat("_ZWrite",0);m.renderQueue=3000;}materials.Add(m);return m;
        }
        MeshRenderer Part(Transform parent,string name,Mesh mesh,Material material)=>SkyPulseMeshBuilder.Surface(parent,name,mesh,material,0);
        Mesh Keep(SkyPulseMeshBuilder builder,string name){var mesh=builder.Finish(name);meshes.Add(mesh);return mesh;}
        void BuildMeshes()
        {
            var b=new SkyPulseMeshBuilder();b.Quad(new Vector3(-.8f,-.8f,0),new Vector3(-.8f,.8f,0),new Vector3(.8f,.8f,0),new Vector3(.8f,-.8f,0),Color.white);contactMesh=Keep(b,"Feathered optical contact plane");
            b=new SkyPulseMeshBuilder();
            // A low smoked mount leaves the light-bending glass, rather than a
            // thick control knob, as the player's visible interactive object.
            var mount=new[]{new Vector2(-.37f,-.22f),new Vector2(.37f,-.22f),new Vector2(.44f,-.12f),new Vector2(.44f,.12f),new Vector2(.37f,.22f),new Vector2(-.37f,.22f),new Vector2(-.44f,.12f),new Vector2(-.44f,-.12f)};
            b.ExtrudePrism(mount,-.065f,.13f);baseMesh=Keep(b,"Low-profile smoked ceramic prism mount");
            b=new SkyPulseMeshBuilder();
            // Four short clamps establish physical depth without enclosing the
            // optic in a bulky white annular sleeve.
            for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)
            {
                var at=new Vector3(side*.365f,end*.225f,-.065f);
                b.Box(at,new Vector3(.075f,.028f,.12f),new Color(.60f,.75f,.82f,0));
                b.Box(at+new Vector3(side*.026f,-end*.024f,0),new Vector3(.024f,.060f,.12f),new Color(.36f,.53f,.62f,0));
            }
            ringMesh=Keep(b,"Four narrow titanium retaining clamps");
            b=new SkyPulseMeshBuilder();
            for(int i=0;i<4;i++)b.RingArc(.37f,.0055f,12,i*90+24,36,new Color(.45f,.78f,.87f,1));
            signalRingMesh=Keep(b,"Minimal broken optical alignment guides");
            b=new SkyPulseMeshBuilder();b.Torus(.43f,.0055f,48,new Color(.35f,.82f,.90f,1));rippleMesh=Keep(b,"Restrained pooled optical impulse");
            var contour=new[]{new Vector2(-.43f,-.052f),new Vector2(-.36f,-.105f),new Vector2(.36f,-.105f),new Vector2(.43f,-.052f),new Vector2(.43f,.052f),new Vector2(.36f,.105f),new Vector2(-.36f,.105f),new Vector2(-.43f,.052f)};
            b=new SkyPulseMeshBuilder();b.ExtrudePrism(contour,-.345f,-.185f);mirrorMesh=Keep(b,"Slim faceted smoked-glass light-bending prism");
            b=new SkyPulseMeshBuilder();
            for(int i=0;i<contour.Length;i++)
            {
                var a=contour[i];var c=contour[(i+1)%contour.Length];var innerA=Vector2.Scale(a,new Vector2(.975f,.77f));var innerC=Vector2.Scale(c,new Vector2(.975f,.77f));
                b.Quad(new Vector3(a.x,a.y,-.352f),new Vector3(c.x,c.y,-.352f),new Vector3(innerC.x,innerC.y,-.352f),new Vector3(innerA.x,innerA.y,-.352f),new Color(.78f,.90f,1,0));
                b.Quad(new Vector3(a.x,a.y,-.352f),new Vector3(a.x,a.y,-.182f),new Vector3(c.x,c.y,-.182f),new Vector3(c.x,c.y,-.352f),new Color(.34f,.48f,.60f,0));
            }
            mirrorFrameMesh=Keep(b,"Narrow titanium edges around the glass prism");
            b=new SkyPulseMeshBuilder();b.ExtrudePrism(new[]{new Vector2(-.29f,-.25f),new Vector2(-.29f,.25f),new Vector2(.34f,0)},-.44f,-.08f);
            for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;b.Box(new Vector3(Mathf.Cos(a)*.19f,Mathf.Sin(a)*.19f,-.07f),new Vector3(.055f,.055f,.11f),new Color(.18f,.27f,.32f,0));}
            splitterMesh=Keep(b,"Solid refracting prism with three mounting feet");
            b=new SkyPulseMeshBuilder();
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI*.25f,c=(i+1)*Mathf.PI*.25f;var p=new Vector3(Mathf.Cos(a)*.18f,Mathf.Sin(a)*.18f,-.075f);var q=new Vector3(Mathf.Cos(c)*.18f,Mathf.Sin(c)*.18f,-.075f);
                var r=new Vector3(Mathf.Cos(c)*.115f,Mathf.Sin(c)*.115f,-.35f);var t=new Vector3(Mathf.Cos(a)*.115f,Mathf.Sin(a)*.115f,-.35f);
                b.Quad(p,q,r,t,new Color(.86f,.98f,1,.22f));b.Face(new Vector3(0,0,-.46f),t,r,new Color(1,1,1,.65f));b.Face(new Vector3(0,0,.18f),q,p,new Color(.35f,.65f,.75f,.2f));
            }
            coreMesh=Keep(b,"Eight-sided cut optical core with bevelled crown");
            b=new SkyPulseMeshBuilder();
            b.Profile(new[]{new Vector2(.14f,.07f),new Vector2(.21f,.035f),new Vector2(.235f,-.04f),new Vector2(.21f,-.12f),new Vector2(.17f,-.17f),new Vector2(0,-.195f)});
            b.Torus(.212f,.007f,48,new Color(.62f,.78f,.86f,.25f));lensMesh=Keep(b,"Aspheric receiver lens and machined optical bezel");
            b=new SkyPulseMeshBuilder();b.BevelBox(.25f,.48f,.12f);for(int i=-1;i<=1;i++)b.Box(new Vector3(0,i*.10f,-.13f),new Vector3(.20f,.006f,.009f),new Color(.7f,.8f,.9f,.85f));doorMesh=Keep(b,"Bevelled retracting relay shutter");
            b=new SkyPulseMeshBuilder();b.Torus(.22f,.012f,48,new Color(.6f,.7f,.8f,.4f));b.Box(new Vector3(0,.025f,-.10f),new Vector3(.028f,.19f,.045f),new Color(1,1,1,1));b.Box(new Vector3(0,-.13f,-.10f),new Vector3(.028f,.028f,.045f),new Color(1,1,1,1));sensorMesh=Keep(b,"Red sensor with recessed warning optic");
            b=new SkyPulseMeshBuilder();b.ExtrudePrism(new[]{new Vector2(-.16f,-.016f),new Vector2(.025f,-.016f),new Vector2(.025f,-.078f),new Vector2(.18f,0),new Vector2(.025f,.078f),new Vector2(.025f,.016f),new Vector2(-.16f,.016f)},-.47f,-.445f);emitterMesh=Keep(b,"Sculpted directional source indicator");
            b=new SkyPulseMeshBuilder();
            b.Profile(new[]{new Vector2(.235f,-.23f),new Vector2(.235f,-.39f),new Vector2(.25f,-.415f),new Vector2(.28f,-.39f),new Vector2(.28f,-.22f),new Vector2(.26f,-.19f),new Vector2(.235f,-.23f)});
            for(int i=0;i<8;i++){float a=i*Mathf.PI*.25f;var at=new Vector3(Mathf.Cos(a)*.255f,Mathf.Sin(a)*.255f,-.42f);b.Box(at,new Vector3(.013f,.013f,.012f),new Color(.85f,.95f,1,1));}
            receiverMesh=Keep(b,"Titanium uplink frame with eight optical contacts");
            // Eight fixed work surfaces are cached once. Smaller opening vaults
            // use larger optics and the same camera, rather than a tiny diagram.
            for(int vault=0;vault<8;vault++)
            {
                var p=SkyPulseBlackoutCampaign.Stages[vault];var bounds=ActiveBounds(p);float step=Pitch(bounds);
                b=new SkyPulseMeshBuilder();
                float halfWidth=((bounds.width-1)*step+1.15f)*.5f,halfHeight=((bounds.height-1)*step*VerticalPitch+1.15f)*.5f,cut=.18f;
                var rim=new[]{new Vector2(-halfWidth+cut,-halfHeight),new Vector2(halfWidth-cut,-halfHeight),new Vector2(halfWidth,-halfHeight+cut),new Vector2(halfWidth,halfHeight-cut),new Vector2(halfWidth-cut,halfHeight),new Vector2(-halfWidth+cut,halfHeight),new Vector2(-halfWidth,halfHeight-cut),new Vector2(-halfWidth,-halfHeight+cut)};
                b.ExtrudePrism(rim,.24f,.72f);
                for(int edge=0;edge<8;edge++)
                {
                    var a=rim[edge];var c=rim[(edge+1)%8];var insideA=a-new Vector2(Mathf.Sign(a.x)*.045f,Mathf.Sign(a.y)*.045f);var insideC=c-new Vector2(Mathf.Sign(c.x)*.045f,Mathf.Sign(c.y)*.045f);
                    b.Quad(new Vector3(a.x,a.y,.23f),new Vector3(c.x,c.y,.23f),new Vector3(insideC.x,insideC.y,.23f),new Vector3(insideA.x,insideA.y,.23f),new Color(.58f,.76f,.85f,0));
                    b.Quad(new Vector3(a.x,a.y,.23f),new Vector3(a.x,a.y,.72f),new Vector3(c.x,c.y,.72f),new Vector3(c.x,c.y,.23f),new Color(.48f,.64f,.75f,0));
                }
                for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)
                {
                    var at=new Vector3(side*(halfWidth-.20f),end*(halfHeight-.25f),.20f);b.Box(at,new Vector3(.038f,.038f,.035f),new Color(1.25f,1.4f,1.5f,0));
                    for(int vent=0;vent<4;vent++)b.Box(new Vector3(side*(halfWidth-.006f),end*(halfHeight-.62f)+vent*.07f,.49f),new Vector3(.018f,.025f,.20f),new Color(.16f,.22f,.29f,0));
                }
                for(int indicator=0;indicator<8;indicator++)b.Box(new Vector3((indicator-3.5f)*.17f,-halfHeight+.032f,.205f),new Vector3(.10f,.016f,.016f),new Color(.45f,.85f,1,1));
                platens[vault]=Keep(b,"Raised vault chassis / bevelled rim and service vents "+vault);
                b=new SkyPulseMeshBuilder();
                for(int tile=0;tile<p.Count;tile++)
                {
                    if(p.Tile(tile)=='#')continue;var point=Point(bounds,p.Width,tile,step);
                    if(tile%p.Width<p.Width-1&&p.Tile(tile+1)!='#')b.Box(new Vector3(point.x+step*.5f,point.y,0),new Vector3(step,.009f,.007f),new Color(.6f,.8f,1,0));
                    if(tile/p.Width<p.Height-1&&p.Tile(tile+p.Width)!='#')b.Box(new Vector3(point.x,point.y-step*VerticalPitch*.5f,0),new Vector3(.009f,step*VerticalPitch,.007f),new Color(.6f,.8f,1,0));
                }
                channels[vault]=Keep(b,"Recessed route channels "+vault);
            }
        }
        public void Bind(BlackoutSession session,Vector2[] positions)
        {
            Circuit=session;Preview=null;PreviewTile=-1;var p=session.Puzzle;
            layout=ActiveBounds(p);pitch=Pitch(layout);OpticScale=Mathf.Min(1.3f,pitch/1.55f);
            int vault=System.Array.IndexOf(SkyPulseBlackoutCampaign.Stages,p);if(vault<0)vault=0;
            platenRenderer.GetComponent<MeshFilter>().sharedMesh=platens[vault];channelRenderer.GetComponent<MeshFilter>().sharedMesh=channels[vault];
            projectionField.localScale=new Vector3(platens[vault].bounds.size.x/9.7f,platens[vault].bounds.size.y/9.7f,1);
            for(int i=0;i<49;i++)
            {
                var m=modules[i];bool on=i<p.Count&&p.Tile(i)!='#'&&p.Tile(i)!='.';m.Root.gameObject.SetActive(on);if(!on)continue;
                var at=Point(i);m.Kind=p.Tile(i);m.Root.localPosition=new Vector3(at.x,at.y,0);m.Root.localScale=Vector3.one*OpticScale;m.Target=session.Rotations[i];m.Angle=m.TargetAngle=m.Target*90;m.Power=0;m.Hit=m.Door=m.Charge=0;m.LastPower=session.Powered[i]?1:0;m.WasPreview=false;m.Ripple.enabled=false;
                float socketScale=p.Rotatable(i)?1:.68f;m.Base.transform.localScale=m.Collar.transform.localScale=Vector3.one*socketScale;m.Ring.transform.localScale=Vector3.one*socketScale;
                m.Mirror.gameObject.SetActive(m.Kind=='M');m.MirrorFrame.gameObject.SetActive(m.Kind=='M');m.Splitter.gameObject.SetActive(m.Kind=='Y');m.Core.gameObject.SetActive(m.Kind=='C'||m.Kind=='R'||m.Kind=='Q');m.Lens.gameObject.SetActive(m.Kind=='S'||m.Kind=='E');m.Sensor.gameObject.SetActive(m.Kind=='!');m.LeftDoor.gameObject.SetActive(m.Kind=='G'||m.Kind=='H');m.RightDoor.gameObject.SetActive(m.Kind=='G'||m.Kind=='H');
                m.Core.localScale=Vector3.one*1.10f;m.Lens.localScale=Vector3.one*1.08f;
                m.Emitter.gameObject.SetActive(m.Kind=='S');m.Emitter.localRotation=Quaternion.Euler(0,0,p.SourceDirection*90);m.Receiver.gameObject.SetActive(m.Kind=='E');
            }
            Tick(0);
            // Touch sockets follow the true camera projection of the angled desk.
            // The hit rectangles themselves stay still during all optical effects.
            for(int i=0;i<p.Count;i++)
            {
                var point=Point(i);var v=opticalCamera.WorldToViewportPoint(console.TransformPoint(new Vector3(point.x,point.y,-.20f)));
                positions[i]=new Vector2((v.x-.5f)*1080,(v.y-.5f)*1080);
            }
        }
        public void SetPreview(BlackoutSession preview,int tile){Preview=preview;PreviewTile=preview==null?-1:tile;Tick(0);}
        public void Clear(){Circuit=Preview=null;PreviewTile=-1;Array.Clear(beamColours,0,beamColours.Length);if(beamMesh!=null)beamMesh.colors=beamColours;}
        public void Hit(int tile){modules[tile].Hit=1;}
        void Update()=>Tick(Time.unscaledDeltaTime);
        public void Tick(float dt)
        {
            if(Circuit==null||MotionPaused)return;dt=Mathf.Max(0,dt);if(!ReducedMotion)clock+=dt;Reveal=Mathf.Max(0,Reveal-dt*3);
            glass.SetFloat("_Clock",clock);projection.SetFloat("_Clock",clock);
            var visible=Preview??Circuit;var p=visible.Puzzle;float smooth=1-Mathf.Exp(-dt*30),powerSmooth=1-Mathf.Exp(-dt*15);
            metal.SetFloat("_Clock",clock);mirrorGlass.SetFloat("_Clock",clock);
            for(int i=0;i<p.Count;i++)
            {
                var m=modules[i];if(!m.Root.gameObject.activeSelf)continue;
                int targetRotation=visible.Rotations[i];bool previewing=Preview!=null;
                if(ReducedMotion){m.TargetAngle=targetRotation*90;m.Angle=m.TargetAngle;}
                else if(previewing||m.WasPreview)
                {
                    // A preview may reverse freely; committed thumb turns use
                    // an unwrapped target, so fast taps never reverse a rotor.
                    m.TargetAngle=m.Angle+Mathf.DeltaAngle(m.Angle,targetRotation*90);
                    m.Angle=Mathf.Lerp(m.Angle,m.TargetAngle,smooth);
                }
                else
                {
                    int orientations=p.Rotatable(i)?p.Orientations(i):4;
                    int turns=(targetRotation-m.Target+orientations)%orientations;
                    m.TargetAngle+=turns*90;m.Angle=Mathf.Lerp(m.Angle,m.TargetAngle,smooth);
                }
                m.Target=targetRotation;m.WasPreview=previewing;
                float target=visible.Powered[i]?1:0;
                if(!previewing&&target>m.LastPower)m.Charge=1;
                if(!previewing)m.LastPower=target;
                m.Power=ReducedMotion?target:Mathf.Lerp(m.Power,target,powerSmooth);m.Hit=Mathf.Max(0,m.Hit-dt*5);m.Charge=Mathf.Max(0,m.Charge-dt*2.8f);
                m.Rotor.localRotation=Quaternion.Euler(0,0,m.Angle);m.Rotor.localScale=Vector3.one*(1+(ReducedMotion?0:Mathf.Sin(m.Hit*Mathf.PI)*.009f));
                var hue=i==PreviewTile?new Color(.7f,.85f,1):Tint(m.Kind);properties.Clear();properties.SetColor(GlowId,hue);properties.SetFloat(EnergyId,.04f+m.Power*.53f+(ReducedMotion?0:m.Hit*.10f)+(i==PreviewTile?.27f:0));m.Ring.SetPropertyBlock(properties);m.Glass.SetPropertyBlock(properties);
                m.LensRenderer.SetPropertyBlock(properties);m.MirrorRenderer.SetPropertyBlock(properties);m.SplitterRenderer.SetPropertyBlock(properties);
                m.EmitterRenderer.SetPropertyBlock(properties);
                // An unpowered amber receiver remains a discoverable destination;
                // this is a small optical indicator, never a full-board glow.
                properties.SetFloat(EnergyId,.12f+m.Power*.48f);m.ReceiverRenderer.SetPropertyBlock(properties);
                properties.SetFloat(EnergyId,p.Rotatable(i)?.025f:.010f);m.Collar.SetPropertyBlock(properties);m.MirrorFrame.SetPropertyBlock(properties);
                float impulse=Mathf.Max(m.Hit,m.Charge);m.Ripple.enabled=!ReducedMotion&&impulse>.001f;
                if(m.Ripple.enabled)
                {
                    m.Ripple.transform.localScale=Vector3.one*(.98f+(1-impulse)*.13f);
                    properties.SetFloat(EnergyId,.22f);properties.SetFloat(VisibilityId,impulse*.20f);m.Ripple.SetPropertyBlock(properties);properties.SetFloat(VisibilityId,1);
                }
                if(m.Kind=='C'||m.Kind=='R'||m.Kind=='Q')
                {
                    m.Core.localRotation=Quaternion.Euler(ReducedMotion?14:14+Mathf.Sin(clock*.9f+i)*2,ReducedMotion?-16:-16+Mathf.Sin(clock*.7f+i)*3,ReducedMotion?0:clock*8+i*17);
                    m.Core.localPosition=new Vector3(0,ReducedMotion?0:Mathf.Sin(clock*1.2f+i)*.008f,-.30f-m.Power*.025f);
                }
                if(m.Kind=='G'||m.Kind=='H')
                {
                    bool open=(visible.RelayMask&(m.Kind=='G'?1:2))!=0;m.Door=ReducedMotion?(open?1:0):Mathf.Lerp(m.Door,open?1:0,smooth);
                    m.LeftDoor.localPosition=new Vector3(-.17f-m.Door*.28f,0,-.30f);m.RightDoor.localPosition=new Vector3(.17f+m.Door*.28f,0,-.30f);
                }
                if(m.Kind=='!')m.Sensor.localRotation=Quaternion.Euler(0,0,ReducedMotion?0:Mathf.Sin(clock*.8f)*8);
            }
            // The deck's output contacts show actual live power, never the
            // speculative preview. A solved-sector sweep accompanies the next
            // board while its new controls are already available.
            properties.Clear();properties.SetColor(GlowId,Mint);properties.SetFloat(EnergyId,(Circuit.CoresPowered/(float)Circuit.Puzzle.CoreCount+(Circuit.UplinkPowered?1:0))*.22f);properties.SetFloat(RevealId,ReducedMotion?0:Reveal);properties.SetFloat("_Clock",clock);platenRenderer.SetPropertyBlock(properties);
            DrawBeams();
        }
        void DrawBeams()
        {
            beamUsed=0;Array.Clear(beamColours,0,beamColours.Length);var visible=Preview??Circuit;
            for(int i=0;i<visible.BeamCount;i++)
            {
                var beam=visible.Beams[i];var a=Point(beam.From);var d=Direction(beam.Direction);var b=beam.To>=0?Point(beam.To):a+d*.68f;
                if(beam.Blocked&&beam.To>=0)b=Vector2.Lerp(a,b,.65f);var hue=beam.Blocked?new Color(1,.36f,.26f):Preview!=null?new Color(.42f,.72f,1):Mint;
                if(Preview!=null){for(int dash=0;dash<6;dash++)Line(Vector2.Lerp(a,b,dash/6f),Vector2.Lerp(a,b,(dash+.65f)/6f),.014f,new Color(hue.r,hue.g,hue.b,.72f));}
                else{Line(a,b,.045f,new Color(hue.r,hue.g,hue.b,.045f));Line(a,b,.014f,new Color(hue.r,hue.g,hue.b,.55f));Line(a,b,.0045f,new Color(.32f,.85f,.76f,.46f));}
                if(Preview==null&&!ReducedMotion&&!beam.Blocked){var at=Vector2.Lerp(a,b,Mathf.Repeat(clock*1.15f+i*.173f,1));Line(at-d*.045f,at+d*.008f,.006f,new Color(.58f,.95f,.87f,.42f));}
            }
            beamMesh.vertices=beamVertices;beamMesh.colors=beamColours;
        }
        static RectInt ActiveBounds(BlackoutPuzzle p)
        {
            int left=p.Width,right=0,top=p.Height,bottom=0;
            for(int i=0;i<p.Count;i++)if(p.Tile(i)!='#'){int x=i%p.Width,y=i/p.Width;left=Mathf.Min(left,x);right=Mathf.Max(right,x);top=Mathf.Min(top,y);bottom=Mathf.Max(bottom,y);}
            return new RectInt(left,top,right-left+1,bottom-top+1);
        }
        static float Pitch(RectInt bounds)=>Mathf.Min(2.30f,9.30f/Mathf.Max(1,Mathf.Max(bounds.width-1,bounds.height-1)));
        static Vector2 Point(RectInt bounds,int columns,int tile,float step)=>new Vector2((tile%columns-(bounds.xMin+(bounds.width-1)*.5f))*step,(bounds.yMin+(bounds.height-1)*.5f-tile/columns)*step*VerticalPitch);
        Vector2 Point(int tile)=>Point(layout,Circuit.Puzzle.Width,tile,pitch);
        static Vector2 Direction(int d)=>d==0?Vector2.right:d==1?Vector2.up:d==2?Vector2.left:Vector2.down;
        void Line(Vector2 a,Vector2 b,float width,Color colour)
        {
            if(beamUsed+4>Capacity)return;var n=new Vector2(-(b-a).y,(b-a).x).normalized*width;beamVertices[beamUsed]=new Vector3(a.x-n.x,a.y-n.y,-.48f);beamVertices[beamUsed+1]=new Vector3(a.x+n.x,a.y+n.y,-.48f);beamVertices[beamUsed+2]=new Vector3(b.x+n.x,b.y+n.y,-.48f);beamVertices[beamUsed+3]=new Vector3(b.x-n.x,b.y-n.y,-.48f);for(int i=0;i<4;i++)beamColours[beamUsed+i]=colour;beamUsed+=4;
        }
        public void Render(){if(opticalCamera!=null&&isActiveAndEnabled)opticalCamera.Render();}
        void OnEnable(){if(scene!=null)scene.gameObject.SetActive(true);if(opticalCamera!=null)opticalCamera.enabled=true;}
        void OnDisable(){if(opticalCamera!=null)opticalCamera.enabled=false;if(scene!=null)scene.gameObject.SetActive(false);}
        void OnDestroy(){if(scene!=null)Destroy(scene.gameObject);if(texture!=null){texture.Release();Destroy(texture);}foreach(var m in meshes)Destroy(m);foreach(var m in materials)Destroy(m);}
    }
}
