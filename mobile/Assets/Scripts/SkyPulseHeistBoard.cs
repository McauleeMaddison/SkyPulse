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
            public Transform Root,Rotor,Mirror,Splitter,Core,Lens,LeftDoor,RightDoor,Sensor;
            public MeshRenderer Ring,Glass,Base,Collar;
            public float Angle,Power,Hit,Door;
            public int Target;
            public char Kind;
        }
        const int Layer=31,Capacity=200*24;
        readonly Module[] modules=new Module[49];
        readonly Vector3[] beamVertices=new Vector3[Capacity];readonly Color[] beamColours=new Color[Capacity];
        readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> materials=new List<Material>();
        MaterialPropertyBlock properties;
        static readonly int EnergyId=Shader.PropertyToID("_Energy"),GlowId=Shader.PropertyToID("_GlowColor");
        Transform scene,console;Camera opticalCamera;RenderTexture texture;Mesh beamMesh;Material metal,accent,glass,dark,light,whiteGlass;
        Mesh baseMesh,ringMesh,mirrorMesh,splitterMesh,coreMesh,lensMesh,doorMesh,sensorMesh;
        int beamUsed;float clock;
        public bool ReducedMotion,MotionPaused;
        public BlackoutSession Circuit {get;private set;}
        public BlackoutSession Preview {get;private set;}
        public int PreviewTile {get;private set;}=-1;
        public float Phase=>clock;
        public float Reveal;
        public RenderTexture Texture=>texture;
        public int PooledModules=>modules.Length;
        public float VisualAngle(int tile)=>modules[tile].Angle;
        static Color Mint=>new Color(.14f,1,.78f);
        static Color Tint(char c)=>c=='!'?new Color(1,.2f,.18f):c=='Q'||c=='H'?new Color(.63f,.39f,1):c=='E'?new Color(1,.68f,.27f):Mint;
        public void Build(RawImage view)
        {
            properties=new MaterialPropertyBlock();
            scene=new GameObject("SkyPulse optical stage (private layer)").transform;scene.position=new Vector3(2000,2000,0);
            texture=new RenderTexture(1080,1080,24,RenderTextureFormat.ARGB32){name="Signal Heist 3D console",antiAliasing=4};texture.Create();view.texture=texture;view.raycastTarget=false;
            var composite=new Material(Resources.Load<Shader>("SkyPulse/Shaders/TechTreeField")){name="Optical console premultiplied compositing"};materials.Add(composite);view.material=composite;
            var cameraObject=new GameObject("Optical console camera",typeof(Camera));opticalCamera=cameraObject.GetComponent<Camera>();cameraObject.transform.SetParent(scene,false);cameraObject.transform.localPosition=new Vector3(0,0,-20);
            opticalCamera.orthographic=true;opticalCamera.orthographicSize=5.4f;opticalCamera.nearClipPlane=.1f;opticalCamera.farClipPlane=40;opticalCamera.cullingMask=1<<Layer;opticalCamera.clearFlags=CameraClearFlags.SolidColor;opticalCamera.backgroundColor=Color.clear;opticalCamera.targetTexture=texture;opticalCamera.allowHDR=false;opticalCamera.allowMSAA=true;
            metal=Material("Brushed titanium",new Color(.105f,.17f,.205f),.27f);accent=Material("Satin nickel accents",new Color(.20f,.30f,.34f),.24f);dark=Material("Graphite ceramic",new Color(.014f,.028f,.039f),.65f);
            glass=Material("Optical cyan glass",new Color(.12f,.64f,.79f,.75f),.08f,true);whiteGlass=Material("Silver mirror coating",new Color(.48f,.72f,.79f),.04f,true);
            light=new Material(Resources.Load<Shader>("SkyPulse/Shaders/FlightLight")){name="Heist optical fibre"};materials.Add(light);
            console=new GameObject("Angled physical optical desk").transform;console.SetParent(scene,false);console.localRotation=Quaternion.Euler(10,-7,0);
            BuildMeshes();
            for(int i=0;i<49;i++)
            {
                var m=new Module();m.Root=new GameObject("Optical module "+i).transform;m.Root.SetParent(console,false);
                m.Base=Part(m.Root,"Ceramic socket",baseMesh,dark);m.Collar=Part(m.Root,"Machined lens collar",ringMesh,metal);m.Collar.transform.localPosition=new Vector3(0,0,-.12f);
                m.Ring=Part(m.Root,"Inner live circuit",ringMesh,accent);m.Ring.transform.localScale=Vector3.one*.82f;m.Ring.transform.localPosition=new Vector3(0,0,-.18f);
                m.Rotor=new GameObject("Optical rotor").transform;m.Rotor.SetParent(m.Root,false);
                m.Mirror=Part(m.Rotor,"Silver glass mirror",mirrorMesh,whiteGlass).transform;m.Mirror.localRotation=Quaternion.Euler(0,0,45);
                m.Splitter=Part(m.Rotor,"Triangular beam splitter",splitterMesh,glass).transform;
                m.Core=Part(m.Root,"Suspended six-sided data crystal",coreMesh,glass).transform;m.Core.localPosition=new Vector3(0,0,-.30f);m.Glass=m.Core.GetComponent<MeshRenderer>();
                m.Lens=Part(m.Root,"Uplink lens",lensMesh,whiteGlass).transform;m.Lens.localPosition=new Vector3(0,0,-.20f);
                m.LeftDoor=Part(m.Root,"Left shutter blade",doorMesh,metal).transform;m.RightDoor=Part(m.Root,"Right shutter blade",doorMesh,metal).transform;
                m.Sensor=Part(m.Root,"Security scanner",sensorMesh,accent).transform;
                modules[i]=m;
            }
            beamMesh=new Mesh{name="Pooled live optical fibre"};beamMesh.MarkDynamic();beamMesh.vertices=beamVertices;beamMesh.colors=beamColours;var triangles=new int[Capacity/4*6];
            for(int i=0;i<Capacity/4;i++){int a=i*4,b=i*6;triangles[b]=a;triangles[b+1]=a+1;triangles[b+2]=a+2;triangles[b+3]=a;triangles[b+4]=a+2;triangles[b+5]=a+3;}beamMesh.triangles=triangles;beamMesh.bounds=new Bounds(Vector3.zero,new Vector3(15,15,5));meshes.Add(beamMesh);Part(console,"Flowing beam and light packets",beamMesh,light);
            foreach(var t in scene.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
        }
        Material Material(string name,Color colour,float roughness,bool transparent=false)
        {
            var m=new Material(Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics")){name=name,enableInstancing=true};m.SetColor("_BaseColor",colour);m.SetFloat("_Roughness",roughness);m.SetFloat("_Glass",transparent?1:0);if(transparent){m.SetFloat("_ZWrite",0);m.renderQueue=3000;}materials.Add(m);return m;
        }
        MeshRenderer Part(Transform parent,string name,Mesh mesh,Material material)=>SkyPulseMeshBuilder.Surface(parent,name,mesh,material,0);
        Mesh Keep(SkyPulseMeshBuilder builder,string name){var mesh=builder.Finish(name);meshes.Add(mesh);return mesh;}
        void BuildMeshes()
        {
            var b=new SkyPulseMeshBuilder();b.Profile(new[]{new Vector2(0,.13f),new Vector2(.48f,.13f),new Vector2(.55f,.04f),new Vector2(.55f,-.02f),new Vector2(.48f,-.10f),new Vector2(0,-.10f)});baseMesh=Keep(b,"Chamfered ceramic optical socket");
            b=new SkyPulseMeshBuilder();b.Torus(.49f,.035f,64,new Color(1,1,1,0));for(int i=0;i<4;i++){float a=i*Mathf.PI/2+Mathf.PI/4;b.Box(new Vector3(Mathf.Cos(a)*.40f,Mathf.Sin(a)*.40f,-.08f),new Vector3(.05f,.05f,.04f),new Color(.7f,.8f,.9f,0));}ringMesh=Keep(b,"Precision lens collar and captive screws");
            b=new SkyPulseMeshBuilder();b.BevelBox(.78f,.13f,.23f);mirrorMesh=Keep(b,"Bevelled glass mirror volume");
            b=new SkyPulseMeshBuilder();b.ExtrudePrism(new[]{new Vector2(-.29f,-.23f),new Vector2(-.29f,.23f),new Vector2(.31f,0)},-.43f,-.10f);splitterMesh=Keep(b,"Triangular optical splitter volume");
            b=new SkyPulseMeshBuilder();for(int i=0;i<6;i++){float a=i*Mathf.PI/3,c=(i+1)*Mathf.PI/3;var p=new Vector3(Mathf.Cos(a)*.24f,Mathf.Sin(a)*.24f,0);var q=new Vector3(Mathf.Cos(c)*.24f,Mathf.Sin(c)*.24f,0);b.Face(new Vector3(.025f,.03f,-.43f),q,p,new Color(1,1,1,.3f));b.Face(new Vector3(0,0,.19f),p,q,new Color(.55f,.75f,.9f,.6f));}coreMesh=Keep(b,"Six-sided faceted data crystal");
            b=new SkyPulseMeshBuilder();b.Profile(new[]{new Vector2(0,-.18f),new Vector2(.24f,-.12f),new Vector2(.33f,-.04f),new Vector2(.35f,.04f),new Vector2(.22f,.10f),new Vector2(0,.10f)});lensMesh=Keep(b,"Ground optical uplink lens");
            b=new SkyPulseMeshBuilder();b.Box(Vector3.zero,new Vector3(.34f,.67f,.18f),new Color(1,1,1,0));for(int i=-2;i<=2;i++)b.Box(new Vector3(0,i*.10f,-.10f),new Vector3(.28f,.018f,.02f),new Color(.7f,.8f,.9f,1));doorMesh=Keep(b,"Vented interlocking relay shutter");
            b=new SkyPulseMeshBuilder();b.Torus(.22f,.04f,48,new Color(1,1,1,1));b.Box(new Vector3(0,.025f,-.10f),new Vector3(.035f,.19f,.06f),new Color(1,1,1,1));b.Box(new Vector3(0,-.13f,-.10f),new Vector3(.035f,.035f,.06f),new Color(1,1,1,1));sensorMesh=Keep(b,"Machined security warning lens");
        }
        public void Bind(BlackoutSession session,Vector2[] positions)
        {
            Circuit=session;Preview=null;PreviewTile=-1;var p=session.Puzzle;
            for(int i=0;i<49;i++)
            {
                var m=modules[i];bool on=i<p.Count&&p.Tile(i)!='#'&&p.Tile(i)!='.';m.Root.gameObject.SetActive(on);if(!on)continue;
                m.Kind=p.Tile(i);m.Root.localPosition=new Vector3(positions[i].x*.01f,positions[i].y*.01f,0);m.Target=session.Rotations[i];m.Angle=m.Target*90;m.Power=0;m.Hit=m.Door=0;
                float socketScale=p.Rotatable(i)?1:.68f;m.Base.transform.localScale=m.Collar.transform.localScale=Vector3.one*socketScale;m.Ring.transform.localScale=Vector3.one*(socketScale*.82f);
                m.Mirror.gameObject.SetActive(m.Kind=='M');m.Splitter.gameObject.SetActive(m.Kind=='Y');m.Core.gameObject.SetActive(m.Kind=='C'||m.Kind=='R'||m.Kind=='Q');m.Lens.gameObject.SetActive(m.Kind=='S'||m.Kind=='E');m.Sensor.gameObject.SetActive(m.Kind=='!');m.LeftDoor.gameObject.SetActive(m.Kind=='G'||m.Kind=='H');m.RightDoor.gameObject.SetActive(m.Kind=='G'||m.Kind=='H');
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
            var visible=Preview??Circuit;var p=visible.Puzzle;float smooth=1-Mathf.Exp(-dt*22),powerSmooth=1-Mathf.Exp(-dt*12);
            for(int i=0;i<p.Count;i++)
            {
                var m=modules[i];if(!m.Root.gameObject.activeSelf)continue;m.Target=visible.Rotations[i];m.Angle=ReducedMotion?m.Target*90:Mathf.LerpAngle(m.Angle,m.Target*90,smooth);
                float target=visible.Powered[i]?1:0;m.Power=ReducedMotion?target:Mathf.Lerp(m.Power,target,powerSmooth);m.Hit=Mathf.Max(0,m.Hit-dt*4);
                m.Rotor.localRotation=Quaternion.Euler(0,0,m.Angle);m.Rotor.localScale=Vector3.one*(1+(ReducedMotion?0:Mathf.Sin(m.Hit*Mathf.PI)*.035f));
                var hue=i==PreviewTile?new Color(.7f,.85f,1):Tint(m.Kind);properties.Clear();properties.SetColor(GlowId,hue);properties.SetFloat(EnergyId,.06f+m.Power*.45f+(ReducedMotion?0:m.Hit*.15f)+(i==PreviewTile?.35f:0));m.Ring.SetPropertyBlock(properties);m.Glass.SetPropertyBlock(properties);
                if(m.Kind=='C'||m.Kind=='R'||m.Kind=='Q')
                {
                    m.Core.localRotation=Quaternion.Euler(ReducedMotion?14:14+Mathf.Sin(clock*.9f+i)*7,ReducedMotion?-16:-16+Mathf.Sin(clock*.7f+i)*10,ReducedMotion?0:Mathf.Sin(clock*.5f+i)*8);
                    m.Core.localPosition=new Vector3(0,ReducedMotion?0:Mathf.Sin(clock*1.2f+i)*.025f,-.30f-m.Power*.07f);
                }
                if(m.Kind=='G'||m.Kind=='H')
                {
                    bool open=(visible.RelayMask&(m.Kind=='G'?1:2))!=0;m.Door=ReducedMotion?(open?1:0):Mathf.Lerp(m.Door,open?1:0,smooth);
                    m.LeftDoor.localPosition=new Vector3(-.17f-m.Door*.28f,0,-.30f);m.RightDoor.localPosition=new Vector3(.17f+m.Door*.28f,0,-.30f);
                }
                if(m.Kind=='!')m.Sensor.localRotation=Quaternion.Euler(0,0,ReducedMotion?0:Mathf.Sin(clock*.8f)*8);
            }
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
                else{Line(a,b,.045f,new Color(hue.r,hue.g,hue.b,.065f));Line(a,b,.014f,new Color(hue.r,hue.g,hue.b,.55f));Line(a,b,.004f,new Color(.80f,1,1,.8f));}
                if(Preview==null&&!ReducedMotion&&!beam.Blocked){var at=Vector2.Lerp(a,b,Mathf.Repeat(clock*.9f+i*.173f,1));Line(at-d*.06f,at+d*.01f,.010f,new Color(.8f,1,1,.72f));}
            }
            beamMesh.vertices=beamVertices;beamMesh.colors=beamColours;
        }
        Vector2 Point(int tile){var p=Circuit.Puzzle;return new Vector2((tile%p.Width-(p.Width-1)*.5f)*1.55f,((p.Height-1)*.5f-tile/p.Width)*1.60f);}
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
