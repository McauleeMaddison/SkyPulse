using UnityEngine;

namespace SkyPulse.Mobile
{
    /// <summary>Shared, collider-free architecture in the existing flight camera.
    /// Two fixed banks crossfade in a dedicated perspective scene, composited
    /// behind the unchanged flight camera. Meshes are built once; parallax uses
    /// absolute simulation distance, not frame count.</summary>
    public sealed class SkyPulseFlightEnvironment : MonoBehaviour
    {
        const int Themes=12, Layers=3, PerLayer=4;
        readonly Mesh[] silhouettes=new Mesh[Themes];
        readonly Transform[,] scenery=new Transform[2,Layers*PerLayer];
        readonly Material[] surfaces=new Material[2];
        readonly int[] bankTheme={-1,-1};
        readonly Texture[] reflections=new Texture[Themes];
        Mesh skyMesh;Material skyMaterial,moonMaterial;Transform sky,moon;
        Transform worldScene,composite;Camera worldCamera;RenderTexture worldImage;Mesh compositeMesh;Material compositeMaterial;float viewAspect;
        public Transform WorldScene=>worldScene;
        public Camera WorldCamera=>worldCamera;
        public RenderTexture WorldImage=>worldImage;
        static int nextScene;
        static readonly int Visibility=Shader.PropertyToID("_Visibility"), Clock=Shader.PropertyToID("_Clock"), Tint=Shader.PropertyToID("_Tint"), Haze=Shader.PropertyToID("_Haze");
        float span; int current=-1,previous;
        Color currentTint,currentHaze,previousTint,previousHaze;
        public int CurrentWorld=>current;
        public float Travel { get; private set; }
        public int ReflectionWorld {get;private set;}=-1;
        public void SetReflection(int world,Texture image){world=Mathf.Clamp(world,0,Themes-1);reflections[world]=image;ReflectionWorld=world;}
        public void Build(float viewWidth)
        {
            span=Mathf.Max(23,viewWidth+15);
            worldScene=new GameObject("Constructed Endless city scene").transform;worldScene.position=new Vector3(6000+(nextScene++%32)*90,6000,0);
            var cameraObject=new GameObject("Perspective city camera",typeof(Camera));cameraObject.transform.SetParent(worldScene,false);
            worldCamera=cameraObject.GetComponent<Camera>();worldCamera.enabled=false;worldCamera.orthographic=false;worldCamera.fieldOfView=38;worldCamera.cullingMask=1<<30;
            cameraObject.transform.localPosition=new Vector3(0,1.8f,-18);cameraObject.transform.localRotation=Quaternion.Euler(5,0,0);
            worldCamera.clearFlags=CameraClearFlags.SolidColor;worldCamera.backgroundColor=new Color(.008f,.015f,.04f);worldCamera.nearClipPlane=.2f;worldCamera.farClipPlane=125;worldCamera.allowHDR=false;worldCamera.allowMSAA=true;
            for(int theme=0;theme<Themes;theme++)silhouettes[theme]=Architecture(theme);
            var shader=Resources.Load<Shader>("SkyPulse/Shaders/FlightArchitecture");
            for(int bank=0;bank<2;bank++)
            {
                surfaces[bank]=new Material(shader){name="Shared world architecture bank "+bank};
                surfaces[bank].SetFloat("_Architecture",1);
                for(int i=0;i<Layers*PerLayer;i++)
                {
                    int layer=i/PerLayer;
                    var renderer=SkyPulseMeshBuilder.Surface(worldScene,"Flight depth "+bank+" / "+layer+" / "+i,silhouettes[0],surfaces[bank],-27+layer*5);
                    scenery[bank,i]=renderer.transform;
                }
            }
            // The original sky colour lives on an enclosing dome. The city
            // illustration is cropped away and replaced by inhabited mesh islands.
            var dome=new SkyPulseMeshBuilder();const int slices=40,rings=24;
            for(int row=0;row<rings;row++)for(int side=0;side<slices;side++)
            {
                float a=side*Mathf.PI*2/slices,c=(side+1)*Mathf.PI*2/slices,u=row*Mathf.PI/rings,w=(row+1)*Mathf.PI/rings;
                dome.Quad(Sphere(a,u),Sphere(c,u),Sphere(c,w),Sphere(a,w),Color.white);
            }
            skyMesh=dome.Finish("Constructed atmospheric sky dome");skyMaterial=new Material(Resources.Load<Shader>("SkyPulse/Shaders/WorldAtmosphere")){name="Endless physical sky palette"};
            sky=SkyPulseMeshBuilder.Surface(worldScene,"3D atmospheric sky dome",skyMesh,skyMaterial,-48).transform;
            // The camera sits inside the dome, so each ray crosses one sky
            // surface. An outside view of both halves produced latitude bands.
            sky.localPosition=Vector3.zero;sky.localScale=Vector3.one*3;
            moonMaterial=new Material(Resources.Load<Shader>("SkyPulse/Shaders/WorldMoon")){name="Lit mineral moon"};
            moon=SkyPulseMeshBuilder.Surface(worldScene,"Real spherical moon",skyMesh,moonMaterial,-44).transform;moon.localPosition=new Vector3(10.2f,11.5f,38);moon.localScale=Vector3.one*.035f;
            foreach(var item in worldScene.GetComponentsInChildren<Transform>(true))item.gameObject.layer=30;
            compositeMesh=new Mesh{name="Live perspective world composite"};compositeMesh.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(-.5f,.5f),new Vector3(.5f,.5f),new Vector3(.5f,-.5f)};compositeMesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};compositeMesh.triangles=new[]{0,1,2,0,2,3};compositeMesh.RecalculateBounds();
            // Keep the shader under Resources so native shader stripping cannot
            // remove a material referenced only by this procedural scene.
            compositeMaterial=new Material(Resources.Load<Shader>("SkyPulse/Shaders/WorldComposite")){name="Live 3D world feed"};compositeMaterial.renderQueue=3000;
            composite=SkyPulseMeshBuilder.Surface(transform,"Live constructed background",compositeMesh,compositeMaterial,-48).transform;composite.localPosition=new Vector3(0,0,24);
            SetViewport(viewWidth);
        }
        static Vector3 Sphere(float a,float b)=>new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a))*34;
        public void SetViewport(float width)
        {
            span=Mathf.Max(23,width+15);
            float aspect=Mathf.Max(.3f,width/18f);if(worldImage!=null&&Mathf.Abs(viewAspect-aspect)<.001f)return;viewAspect=aspect;
            if(worldImage!=null){worldImage.Release();Destroy(worldImage);}
            worldImage=new RenderTexture(720,Mathf.Clamp(Mathf.RoundToInt(720/aspect),720,1920),16,RenderTextureFormat.ARGB32){name="Live Endless perspective world",antiAliasing=2};worldImage.Create();
            worldCamera.targetTexture=worldImage;worldCamera.aspect=aspect;skyMaterial.SetFloat("_Aspect",aspect);compositeMaterial.mainTexture=worldImage;composite.localScale=new Vector3(width+.04f,18.04f,1);
        }
        public void Render(){if(worldCamera!=null&&gameObject.activeInHierarchy)worldCamera.Render();}
        void OnEnable(){if(worldScene!=null)worldScene.gameObject.SetActive(true);}
        void OnDisable(){if(worldScene!=null)worldScene.gameObject.SetActive(false);}
        public void ResetView(){current=-1;bankTheme[0]=bankTheme[1]=-1;}
        public void Present(int world,float distance,float clock,float transition,Color tint,Color haze,bool reduced,float entry=1)
        {
            world=Mathf.Clamp(world,0,Themes-1);
            if(current!=world){previous=current<0?world:current;previousTint=current<0?tint:currentTint;previousHaze=current<0?haze:currentHaze;current=world;currentTint=tint;currentHaze=haze;}
            Travel=reduced?0:distance; clock=reduced?0:clock;
            SetBank(0,previous,Travel,clock,previous==world?0:(1-transition)*entry,previousTint,previousHaze);
            SetBank(1,world,Travel,clock,(previous==world?1:transition)*entry,tint,haze);
            float blend=previous==world?1:Mathf.Clamp01(transition);
            skyMaterial.SetTexture("_SkyA",reflections[previous]!=null?reflections[previous]:Texture2D.grayTexture);skyMaterial.SetTexture("_SkyB",reflections[world]!=null?reflections[world]:Texture2D.grayTexture);skyMaterial.SetFloat("_Blend",blend);
            skyMaterial.SetColor(Tint,Color.Lerp(previousTint,tint,blend));skyMaterial.SetColor(Haze,Color.Lerp(previousHaze,haze,blend));skyMaterial.SetFloat(Clock,clock);skyMaterial.SetFloat("_World",Mathf.Lerp(previous,world,blend));
            moonMaterial.SetColor(Tint,Color.Lerp(previousTint,tint,blend));moon.localRotation=Quaternion.Euler(12,clock*.6f,18);
        }
        void SetBank(int bank,int theme,float distance,float clock,float visibility,Color tint,Color haze)
        {
            bool changed=bankTheme[bank]!=theme;bankTheme[bank]=theme;
            var material=surfaces[bank];material.SetFloat(Visibility,visibility);material.SetFloat(Clock,clock);material.SetColor(Tint,tint);material.SetColor(Haze,haze);
            if(changed)material.SetTexture("_Environment",reflections[theme]!=null?reflections[theme]:Texture2D.grayTexture);
            for(int i=0;i<Layers*PerLayer;i++)
            {
                var item=scenery[bank,i];bool visible=visibility>.0001f;
                if(item.gameObject.activeSelf!=visible)item.gameObject.SetActive(visible);
                if(changed)item.GetComponent<MeshFilter>().sharedMesh=silhouettes[theme];
                if(!visible)continue;
                int layer=i/PerLayer,slot=i%PerLayer;
                float seed=Hash(theme*43+i*17),scale=(.48f+layer*.25f)*(.86f+seed*.24f);
                float x=Mathf.Repeat(slot*span/PerLayer+seed*2.1f+layer*1.7f-distance*(.09f+layer*.15f)+span*.5f,span)-span*.5f;
                // Stagger inhabited islands above the cloud sea; near landforms
                // travel faster while distant cities keep the flight path legible.
                float y=(layer==0?-.5f:layer==1?-3.5f:-7.4f)+seed*(layer==0?4.2f:3.2f)+Mathf.Sin(clock*.13f+i)*.045f;
                item.localPosition=new Vector3(x,y,27-layer*8);
                item.localScale=Vector3.one*scale;
                item.localRotation=Quaternion.Euler(0,-22+seed*44,0);
            }
        }
        static float Hash(int n){uint x=(uint)n*747796405u+2891336453u;x=((x>>((int)(x>>28)+4))^x)*277803737u;return ((x>>22)^x)/4294967295f;}
        // Each cached district is a floating landform with an inhabited crown.
        // Rock, terraces, glazing and light reveals share one vertex-classified
        // material; no cubes are instantiated and no flight collision is added.
        static readonly Color Metal=new Color(.31f,.29f,.44f,0),Glass=new Color(.10f,.22f,.36f,.34f),Light=new Color(.30f,.82f,1,1);
        static Mesh Architecture(int theme)
        {
            var b=new SkyPulseMeshBuilder();float radius=1.65f+Hash(theme*17)*.38f;
            const int sides=40;
            var levels=new[]{new Vector2(.08f,-2.05f),new Vector2(.32f,-1.72f),new Vector2(.57f,-1.22f),new Vector2(.80f,-.65f),new Vector2(.96f,-.25f),new Vector2(1,.02f),new Vector2(.94f,.14f)};
            for(int row=1;row<levels.Length;row++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1)*Mathf.PI*2/sides;
                var lo=levels[row-1];var hi=levels[row];
                var colour=Color.Lerp(new Color(.12f,.085f,.22f,0),new Color(.34f,.20f,.38f,0),row/(float)levels.Length);
                colour*=.82f+Hash(theme*37+side*7+row)*.30f;colour.a=0;
                b.SmoothQuad(Land(a,lo,radius,theme),Land(c,lo,radius,theme),Land(c,hi,radius,theme),Land(a,hi,radius,theme),new Vector3(Mathf.Cos(a),-.3f,Mathf.Sin(a)).normalized,new Vector3(Mathf.Cos(c),-.3f,Mathf.Sin(c)).normalized,colour);
                if(row==levels.Length-1)b.Face(new Vector3(0,.14f,0),Land(a,hi,radius,theme),Land(c,hi,radius,theme),new Color(.23f,.26f,.34f,0));
                // Mineral seams pick up the violet light between strata.
                if(row==3&&side%4==0)b.Quad(Land(a,lo,radius*1.005f,theme),Land(c,lo,radius*1.005f,theme),Land(c,new Vector2(lo.x,lo.y+.025f),radius*1.005f,theme),Land(a,new Vector2(lo.x,lo.y+.025f),radius*1.005f,theme),new Color(.58f,.20f,.82f,.85f));
            }
            Terrace(b,Vector3.zero,radius*.96f,radius*.57f,.18f,.105f,new Color(.44f,.38f,.48f,0));
            Terrace(b,Vector3.zero,radius*.97f,radius*.58f,.215f,.012f,new Color(.94f,.55f,.34f,.94f));
            Terrace(b,new Vector3(0,.24f,0),radius*.72f,radius*.45f,.02f,.13f,Metal);
            Terrace(b,new Vector3(0,.38f,0),radius*.73f,radius*.46f,0,.012f,Light);
            // A varied cluster: tapered central spire, curved satellite towers,
            // and an occupied circular sky terrace, all standing on the island.
            Building(b,new Vector3(0,.40f,.16f),.62f,.55f,3.4f+Hash(theme*9)*1.0f,theme);
            Building(b,new Vector3(-.74f,.27f,0),.42f,.43f,1.65f+Hash(theme*31)*.9f,theme+1);
            Building(b,new Vector3(.78f,.27f,.12f),.40f,.38f,2.0f+Hash(theme*13)*.9f,theme+2);
            Building(b,new Vector3(.42f,.26f,-.48f),.30f,.30f,1.15f,theme+3);
            Terrace(b,new Vector3(-.12f,.49f,-.34f),radius*.69f,radius*.37f,0,.035f,Glass);
            for(int post=0;post<12;post++)
            {
                float angle=post*Mathf.PI*2/12;var at=new Vector3(Mathf.Cos(angle)*radius*.88f,.26f,Mathf.Sin(angle)*radius*.52f);
                Terrace(b,at,.022f,.022f,0,.13f,Metal);Terrace(b,at,.025f,.025f,.13f,.025f,Light);
            }
            return b.Finish("Floating island / mineral strata / inhabited spires "+theme);
        }
        static Vector3 Land(float angle,Vector2 level,float radius,int theme)
        {
            float contour=1+.065f*Mathf.Sin(angle*5+theme)+.035f*Mathf.Sin(angle*9-theme);
            return new Vector3(Mathf.Cos(angle)*radius*level.x*contour+.14f*(1-level.x),level.y+.035f*Mathf.Sin(angle*7+theme),Mathf.Sin(angle)*radius*.60f*level.x*contour);
        }
        static void Terrace(SkyPulseMeshBuilder b,Vector3 centre,float rx,float rz,float bottom,float height,Color colour)
        {
            const int sides=40;
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,c=(i+1)*Mathf.PI*2/sides;
                var p=new Vector3(Mathf.Cos(a)*rx,bottom,Mathf.Sin(a)*rz);var q=new Vector3(Mathf.Cos(c)*rx,bottom,Mathf.Sin(c)*rz);
                b.SmoothQuad(centre+p,centre+q,centre+q+Vector3.up*height,centre+p+Vector3.up*height,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),new Vector3(Mathf.Cos(c),0,Mathf.Sin(c)),colour);
                b.Face(centre+Vector3.up*(bottom+height),centre+p+Vector3.up*height,centre+q+Vector3.up*height,colour);
            }
        }
        static void Building(SkyPulseMeshBuilder b,Vector3 centre,float width,float depth,float height,int variant)
        {
            const int sides=24;var profile=new[]{new Vector2(1.30f,0),new Vector2(1.30f,.055f),new Vector2(1,.12f),new Vector2(.96f,.64f),new Vector2(.76f,.65f),new Vector2(.76f,.82f),new Vector2(.48f,.84f),new Vector2(.44f,.96f),new Vector2(.035f,1.12f)};
            float sweep=(variant%2==0?1:-1)*width*.14f;
            for(int row=1;row<profile.Length;row++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1)*Mathf.PI*2/sides;var lo=profile[row-1];var hi=profile[row];
                var p=TowerPoint(a,lo,width,depth,height,sweep);var q=TowerPoint(c,lo,width,depth,height,sweep);var r=TowerPoint(c,hi,width,depth,height,sweep);var t=TowerPoint(a,hi,width,depth,height,sweep);
                var hue=row==2||row==5?new Color(.43f,.36f,.49f,0):Glass;
                b.SmoothQuad(centre+p,centre+q,centre+r,centre+t,new Vector3(Mathf.Cos(a),.12f,Mathf.Sin(a)).normalized,new Vector3(Mathf.Cos(c),.12f,Mathf.Sin(c)).normalized,hue);
                if(side%6==0)
                {
                    var near=TowerPoint(a+.032f,lo,width*1.009f,depth*1.009f,height,sweep);var far=TowerPoint(a+.032f,hi,width*1.009f,depth*1.009f,height,sweep);
                    b.Quad(centre+p,centre+near,centre+far,centre+t,variant%2==0?Light:new Color(.91f,.25f,.70f,1));
                }
            }
            for(int row=1;row<17;row++)
            {
                float y=row*.032f+.09f,rad=Mathf.Lerp(1,.89f,y/.60f);
                Terrace(b,centre+new Vector3(sweep*y*y,0,0),width*rad*.505f,depth*rad*.505f,y*height,.008f,row%4==0?new Color(.98f,.63f,.31f,.9f):Light);
            }
            Terrace(b,centre+new Vector3(sweep*.79f*.79f,0,0),width*.34f,depth*.34f,height*.79f,.014f,new Color(1,.56f,.29f,1));
        }
        static Vector3 TowerPoint(float angle,Vector2 profile,float width,float depth,float height,float sweep)=>new Vector3(Mathf.Sign(Mathf.Cos(angle))*Mathf.Pow(Mathf.Abs(Mathf.Cos(angle)),.48f)*width*profile.x*.5f+sweep*profile.y*profile.y,profile.y*height,Mathf.Sign(Mathf.Sin(angle))*Mathf.Pow(Mathf.Abs(Mathf.Sin(angle)),.48f)*depth*profile.x*.5f);

        void OnDestroy()
        {
            foreach(var mesh in silhouettes)if(mesh!=null)Destroy(mesh);
            foreach(var material in surfaces)if(material!=null)Destroy(material);
            if(skyMesh!=null)Destroy(skyMesh);if(skyMaterial!=null)Destroy(skyMaterial);if(moonMaterial!=null)Destroy(moonMaterial);
            if(worldScene!=null)Destroy(worldScene.gameObject);if(worldImage!=null){worldImage.Release();Destroy(worldImage);}if(compositeMesh!=null)Destroy(compositeMesh);if(compositeMaterial!=null)Destroy(compositeMaterial);
        }
    }
}
