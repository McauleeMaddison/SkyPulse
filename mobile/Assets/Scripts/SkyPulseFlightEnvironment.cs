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
        readonly Mesh[] residential=new Mesh[Themes];
        readonly MaterialPropertyBlock[,] authoredLighting=new MaterialPropertyBlock[Themes,2];
        readonly Transform[,] scenery=new Transform[2,Layers*PerLayer];
        readonly Material[] surfaces=new Material[2];
        readonly int[] bankTheme={-1,-1};
        readonly Texture[] reflections=new Texture[Themes];
        Mesh skyMesh;Material skyMaterial;Transform sky;
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
            for(int theme=0;theme<Themes;theme++)
            {
                silhouettes[theme]=Architecture(theme);residential[theme]=Architecture(theme,1);
                for(int variant=0;variant<2;variant++)
                {
                    var data=Resources.Load<SkyPulseBackgroundLighting>("SkyPulse/BackgroundEdits/world-"+theme.ToString("00")+"-"+(variant==0?"civic":"residential")+"-lighting");
                    if(data==null)continue;
                    var properties=new MaterialPropertyBlock();properties.SetVectorArray("_BeaconPoints",data.Points);properties.SetVectorArray("_BeaconColours",data.Colours);authoredLighting[theme,variant]=properties;
                }
            }
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
            skyMaterial.SetFloat("_WorldA",previous);skyMaterial.SetFloat("_WorldB",world);
            skyMaterial.SetColor(Tint,Color.Lerp(previousTint,tint,blend));skyMaterial.SetColor(Haze,Color.Lerp(previousHaze,haze,blend));skyMaterial.SetFloat(Clock,clock);skyMaterial.SetFloat("_World",Mathf.Lerp(previous,world,blend));
        }
        void SetBank(int bank,int theme,float distance,float clock,float visibility,Color tint,Color haze)
        {
            bool changed=bankTheme[bank]!=theme;bankTheme[bank]=theme;
            var material=surfaces[bank];material.SetFloat("_Geology",theme==0?1:0);material.SetFloat("_Design",theme);material.SetFloat(Visibility,visibility);material.SetFloat(Clock,clock);material.SetColor(Tint,tint);material.SetColor(Haze,haze);
            if(changed)
            {
                material.SetTexture("_Environment",reflections[theme]!=null?reflections[theme]:Texture2D.grayTexture);
                SkyPulseWorldArchitecture.ConfigureBeaconLighting(material,theme);
            }
            for(int i=0;i<Layers*PerLayer;i++)
            {
                var item=scenery[bank,i];bool visible=visibility>.0001f;
                if(item.gameObject.activeSelf!=visible)item.gameObject.SetActive(visible);
                if(changed)
                {
                    int variant=(i+theme)%2;item.GetComponent<MeshFilter>().sharedMesh=variant==0?silhouettes[theme]:residential[theme];
                    item.GetComponent<MeshRenderer>().SetPropertyBlock(authoredLighting[theme,variant]);
                }
                if(!visible)continue;
                int layer=i/PerLayer,slot=i%PerLayer;
                float seed=Hash(theme*43+i*17),scale=(.48f+layer*.25f)*(.86f+seed*.24f);
                float x=Mathf.Repeat(slot*span/PerLayer+seed*2.1f+layer*1.7f-distance*(.09f+layer*.15f)+span*.5f,span)-span*.5f;
                // Stagger inhabited islands above the cloud sea; near landforms
                // travel faster while distant cities keep the flight path legible.
                bool floating=theme<3;
                float y=(floating?(layer==0?-.8f:layer==1?-3.2f:-6.0f):(layer==0?-2.5f:layer==1?-3.8f:-5.4f))+seed*(floating?(layer==0?2.6f:1.7f):.6f)+Mathf.Sin(clock*.13f+i)*(floating?.025f:0);
                item.localPosition=new Vector3(x,y,27-layer*8);
                // Small differences in the occupied floor area and skyline
                // proportions break the old repeated-stamp silhouette.
                item.localScale=new Vector3(scale*(.88f+Hash(i*31+theme)*.24f),scale*(.86f+Hash(i*53+theme)*.28f),scale);
                item.localRotation=Quaternion.Euler(0,-34+seed*38,0);
            }
        }
        static float Hash(int n){uint x=(uint)n*747796405u+2891336453u;x=((x>>((int)(x>>28)+4))^x)*277803737u;return ((x>>22)^x)/4294967295f;}
        // Geometry construction is kept separate from the runtime camera/pool.
        static Mesh Architecture(int theme,int variant=0)
        {
            var authored=Resources.Load<Mesh>("SkyPulse/BackgroundEdits/world-"+theme.ToString("00")+"-"+(variant==0?"civic":"residential"));
            return authored!=null?Instantiate(authored):SkyPulseWorldArchitecture.Build(theme,variant);
        }

        void OnDestroy()
        {
            foreach(var mesh in silhouettes)if(mesh!=null)Destroy(mesh);
            foreach(var mesh in residential)if(mesh!=null)Destroy(mesh);
            foreach(var material in surfaces)if(material!=null)Destroy(material);
            if(skyMesh!=null)Destroy(skyMesh);if(skyMaterial!=null)Destroy(skyMaterial);
            if(worldScene!=null)Destroy(worldScene.gameObject);if(worldImage!=null){worldImage.Release();Destroy(worldImage);}if(compositeMesh!=null)Destroy(compositeMesh);if(compositeMaterial!=null)Destroy(compositeMaterial);
        }
    }
}
