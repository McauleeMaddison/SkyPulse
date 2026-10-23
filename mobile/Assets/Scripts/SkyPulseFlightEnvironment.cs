using UnityEngine;

namespace SkyPulse.Mobile
{
    /// <summary>Shared, collider-free architecture in the existing flight camera.
    /// Two fixed banks crossfade in a dedicated perspective scene, composited
    /// behind the unchanged flight camera. Meshes are built once; parallax uses
    /// absolute simulation distance, not frame count.</summary>
    public sealed class SkyPulseFlightEnvironment : MonoBehaviour
    {
        const int Themes=12, Layers=3, PerLayer=6;
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
            span=Mathf.Max(18,viewWidth+12);
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
            // A real enclosing mesh, not a city illustration on a background
            // sprite. Its procedural sky is lit from the current world's palette.
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
            moon=SkyPulseMeshBuilder.Surface(worldScene,"Real spherical moon",skyMesh,moonMaterial,-44).transform;moon.localPosition=new Vector3(5.2f,8.5f,30);moon.localScale=Vector3.one*.065f;
            foreach(var item in worldScene.GetComponentsInChildren<Transform>(true))item.gameObject.layer=30;
            compositeMesh=new Mesh{name="Live perspective world composite"};compositeMesh.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(-.5f,.5f),new Vector3(.5f,.5f),new Vector3(.5f,-.5f)};compositeMesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};compositeMesh.triangles=new[]{0,1,2,0,2,3};compositeMesh.RecalculateBounds();
            compositeMaterial=new Material(Shader.Find("Unlit/Texture")){name="Live 3D world feed"};compositeMaterial.renderQueue=3000;
            composite=SkyPulseMeshBuilder.Surface(transform,"Live constructed background",compositeMesh,compositeMaterial,-48).transform;composite.localPosition=new Vector3(0,0,24);
            SetViewport(viewWidth);
        }
        static Vector3 Sphere(float a,float b)=>new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a))*34;
        public void SetViewport(float width)
        {
            span=Mathf.Max(18,width+12);
            float aspect=Mathf.Max(.3f,width/18f);if(worldImage!=null&&Mathf.Abs(viewAspect-aspect)<.001f)return;viewAspect=aspect;
            if(worldImage!=null){worldImage.Release();Destroy(worldImage);}
            worldImage=new RenderTexture(720,Mathf.Clamp(Mathf.RoundToInt(720/aspect),720,1920),16,RenderTextureFormat.ARGB32){name="Live Endless perspective world",antiAliasing=2};worldImage.Create();
            worldCamera.targetTexture=worldImage;worldCamera.aspect=aspect;compositeMaterial.mainTexture=worldImage;composite.localScale=new Vector3(width+.04f,18.04f,1);
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
                float seed=Hash(theme*43+i*17),scale=(.66f+layer*.32f)*(.86f+seed*.24f);
                float x=Mathf.Repeat(slot*span/PerLayer+seed*2.1f+layer*1.7f-distance*(.09f+layer*.15f)+span*.5f,span)-span*.5f;
                // Tall, grounded districts form the visible world behind flight,
                // rather than tiny black silhouettes hidden beneath the frame.
                float y=layer==0?-4.7f:layer==1?-7.1f:-9.2f;
                item.localPosition=new Vector3(x,y,21-layer*6);
                item.localScale=Vector3.one*scale;
                item.localRotation=Quaternion.Euler(0,-18+seed*48,0);
            }
        }
        static float Hash(int n){uint x=(uint)n*747796405u+2891336453u;x=((x>>((int)(x>>28)+4))^x)*277803737u;return ((x>>22)^x)/4294967295f;}
        // Vertex alpha selects metal, glazing or a lit window in the shared
        // material. These are authored mesh details, not runtime spawned props.
        static readonly Color Metal=new Color(.33f,.38f,.44f,0),Glass=new Color(.12f,.20f,.29f,.34f),Light=new Color(.35f,.76f,.91f,1);
        static Mesh Architecture(int theme)
        {
            var b=new SkyPulseMeshBuilder();int count=2+theme%3;
            for(int i=0;i<count;i++)
            {
                float seed=Hash(theme*31+i*53),width=.78f+seed*.45f;
                float x=(i-(count-1)*.5f)*1.08f,height=5.5f+seed*6.0f+(theme%4)*.30f;
                Building(b,new Vector3(x,0,(i%2)*.48f),width,.82f+seed*.30f,height,theme+i);
            }
            Skyway(b,new Vector3(0,2.2f,.08f),count*.93f);
            return b.Finish("Glazed skyline architecture "+theme);
        }
        static void Building(SkyPulseMeshBuilder b,Vector3 centre,float width,float depth,float height,int variant)
        {
            // Chamfered curtain-wall tower: recessed glazing, stepped crown,
            // thin floor reveals and a foundation extending below the viewport.
            const int sides=32;int form=variant%3;
            var plan=new Vector2[sides];
            for(int side=0;side<sides;side++)
            {
                float angle=side*Mathf.PI*2/sides,c=Mathf.Cos(angle),sn=Mathf.Sin(angle);
                float exponent=form==0?.38f:form==1?.55f:.82f;
                plan[side]=new Vector2(Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),exponent)*.5f,Mathf.Sign(sn)*Mathf.Pow(Mathf.Abs(sn),exponent)*.5f);
            }
            float bodyTop=form==1?.72f:.92f;
            var profile=form==1?
                new[]{new Vector2(.98f,-20f),new Vector2(.98f,.08f),new Vector2(.94f,.15f),new Vector2(.94f,height*.72f),new Vector2(.72f,height*.73f),new Vector2(.72f,height*.88f),new Vector2(.52f,height*.89f),new Vector2(.52f,height*.99f),new Vector2(.48f,height+.04f)}:
                new[]{new Vector2(.98f,-20f),new Vector2(.98f,.08f),new Vector2(.94f,.15f),new Vector2(.94f,height*.92f),new Vector2(.90f,height*.96f),new Vector2(.86f,height),new Vector2(.81f,height+.04f)};
            float lean=variant%2==0?.09f:-.07f;
            for(int j=1;j<profile.Length;j++)for(int side=0;side<sides;side++)
            {
                var a=plan[side];var c=plan[(side+1)%sides];var low=profile[j-1];var high=profile[j];
                var na=new Vector3(a.x/(width*width),0,a.y/(depth*depth)).normalized;var nc=new Vector3(c.x/(width*width),0,c.y/(depth*depth)).normalized;
                float lowerSweep=lean*Mathf.Pow(Mathf.Max(0,low.y/height),2),upperSweep=lean*Mathf.Pow(Mathf.Max(0,high.y/height),2);
                b.SmoothQuad(centre+new Vector3(a.x*width*low.x+lowerSweep,low.y,a.y*depth*low.x),centre+new Vector3(c.x*width*low.x+lowerSweep,low.y,c.y*depth*low.x),
                    centre+new Vector3(c.x*width*high.x+upperSweep,high.y,c.y*depth*high.x),centre+new Vector3(a.x*width*high.x+upperSweep,high.y,a.y*depth*high.x),na,nc,j==3||form==1&&(j==5||j==7)?Glass:Metal);
                if(j==profile.Length-1)b.Face(centre+new Vector3(upperSweep,high.y,0),centre+new Vector3(a.x*width*high.x+upperSweep,high.y,a.y*depth*high.x),centre+new Vector3(c.x*width*high.x+upperSweep,high.y,c.y*depth*high.x),Metal);
            }
            for(int row=0;row<20;row++)
            {
                float y=.25f+row*(height*(bodyTop-.04f)/20),sweep=lean*Mathf.Pow(y/height,2);
                for(int side=17;side<31;side++)
                {
                    var a=Vector2.Lerp(plan[side],plan[(side+1)%sides],.15f);var c=Vector2.Lerp(plan[side],plan[(side+1)%sides],.78f);
                    float seed=Hash(variant*97+row*11+side*7);bool lit=seed>.44f;
                    var hue=lit?Color.Lerp(new Color(.35f,.65f,.82f,.86f),new Color(.95f,.78f,.52f,.86f),seed>.87f?.72f:0):new Color(.12f,.20f,.28f,.30f);
                    b.Quad(centre+new Vector3(a.x*width*.946f+sweep,y,a.y*depth*.946f),centre+new Vector3(c.x*width*.946f+sweep,y,c.y*depth*.946f),
                        centre+new Vector3(c.x*width*.946f+sweep,y+.035f,c.y*depth*.946f),centre+new Vector3(a.x*width*.946f+sweep,y+.035f,a.y*depth*.946f),hue);
                }
            }
            // Continuous curved light reveals wrap the crown and glass facade.
            for(int band=0;band<3;band++)
            {
                float y=band==2?height+.015f:height*(band==0?.20f:bodyTop*.96f),radius=band==2?profile[profile.Length-1].x+.01f:.947f,sweep=lean*Mathf.Pow(y/height,2);
                for(int side=0;side<sides;side++)
                {
                    var a=plan[side];var c=plan[(side+1)%sides];
                    b.Quad(centre+new Vector3(a.x*width*radius+sweep,y,a.y*depth*radius),centre+new Vector3(c.x*width*radius+sweep,y,c.y*depth*radius),
                        centre+new Vector3(c.x*width*radius+sweep,y+.010f,c.y*depth*radius),centre+new Vector3(a.x*width*radius+sweep,y+.010f,a.y*depth*radius),Light);
                }
            }
        }
        static void Skyway(SkyPulseMeshBuilder b,Vector3 centre,float width)
        {
            // Enclosed glass skyway with eight bevelled faces and a slim LED rail.
            var cross=new[]{new Vector2(-.16f,-.18f),new Vector2(.12f,-.18f),new Vector2(.18f,-.12f),new Vector2(.18f,.12f),new Vector2(.12f,.18f),new Vector2(-.12f,.18f),new Vector2(-.18f,.12f),new Vector2(-.18f,-.12f)};
            for(int i=0;i<cross.Length;i++)
            {
                var a=cross[i];var c=cross[(i+1)%cross.Length];
                b.Quad(centre+new Vector3(-width*.5f,a.y,a.x),centre+new Vector3(width*.5f,a.y,a.x),centre+new Vector3(width*.5f,c.y,c.x),centre+new Vector3(-width*.5f,c.y,c.x),i==4?Light:i==6?Glass:Metal);
            }
        }

        void OnDestroy()
        {
            foreach(var mesh in silhouettes)if(mesh!=null)Destroy(mesh);
            foreach(var material in surfaces)if(material!=null)Destroy(material);
            if(skyMesh!=null)Destroy(skyMesh);if(skyMaterial!=null)Destroy(skyMaterial);if(moonMaterial!=null)Destroy(moonMaterial);
            if(worldScene!=null)Destroy(worldScene.gameObject);if(worldImage!=null){worldImage.Release();Destroy(worldImage);}if(compositeMesh!=null)Destroy(compositeMesh);if(compositeMaterial!=null)Destroy(compositeMaterial);
        }
    }
}
