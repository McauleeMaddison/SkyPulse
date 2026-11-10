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
        readonly MeshRenderer[,] districtRenderers=new MeshRenderer[2,Layers*PerLayer];
        readonly MaterialPropertyBlock[,] districtFinish=new MaterialPropertyBlock[2,Layers*PerLayer];
        readonly Material[] surfaces=new Material[2];
        readonly int[] bankTheme={-1,-1};
        readonly Texture[] reflections=new Texture[Themes];
        Mesh skyMesh;Material skyMaterial;Texture3D cloudNoise;bool ownsCloudNoise;Transform sky;SkyPulseAtmosphereDepth atmosphereDepth;
        Transform worldScene,composite;Camera worldCamera;Light cityKey;RenderTexture worldImage;Mesh compositeMesh;Material compositeMaterial;float viewAspect;
        public Transform WorldScene=>worldScene;
        public Camera WorldCamera=>worldCamera;
        public RenderTexture WorldImage=>worldImage;
        public Texture3D CloudNoise=>cloudNoise;
        static int nextScene;
        static readonly int Visibility=Shader.PropertyToID("_Visibility"), Clock=Shader.PropertyToID("_Clock"), Tint=Shader.PropertyToID("_Tint"), Haze=Shader.PropertyToID("_Haze"), KeyTone=Shader.PropertyToID("_KeyTone"), FillTone=Shader.PropertyToID("_FillTone");
        float span; int current=-1,previous;
        float distanceOffset,clockOffset,presentationClock;bool hasPresentation,motionHeld;
        Color currentTint,currentHaze,previousTint,previousHaze;
        // One authored composition per place: a civic landmark, a lower
        // residential counterpoint and one quiet distant marker. Complete
        // silhouettes live at staggered depths; there is no cropped near clone.
        // Vector4 stores uniform scale, X, ground height and physical depth.
        static readonly Vector4[] CivicFraming={
            new Vector4(1.18f,3.15f,-3.85f,26.5f), // Neon / connected civic triplet
            new Vector4(1.10f,3.25f,-3.75f,27.0f), // Aurora / complete twin-sail skyport
            new Vector4(1.12f,3.40f,-3.70f,27.5f), // Solar / broad research dock
            new Vector4(1.04f,3.20f,-3.90f,29.0f), // Midnight / tall sail quarter
            new Vector4(1.20f,3.00f,-3.65f,27.5f), // Velvet / symmetric rose citadel
            new Vector4(1.06f,3.10f,-4.00f,29.0f), // Crystal / glacial spires
            new Vector4(1.22f,3.25f,-4.20f,25.5f), // Jade / broad garden terraces
            new Vector4(1.02f,3.20f,-4.10f,30.0f), // Violet / tall connected arcology
            new Vector4(1.15f,3.05f,-3.90f,29.0f), // Eclipse / crescent fortress
            new Vector4(1.14f,3.20f,-3.80f,28.0f), // Cobalt / energy harbour
            new Vector4(1.25f,3.15f,-3.85f,27.0f), // Amber / mesa observatory
            new Vector4(1.20f,3.10f,-3.60f,28.0f)  // Polar / vaulted habitat
        };
        static readonly Vector4[] ResidentialFraming={
            new Vector4(.64f,-6.30f,-3.90f,40),new Vector4(.62f,-6.60f,-3.60f,39),
            new Vector4(.60f,-6.80f,-3.80f,40),new Vector4(.58f,-6.50f,-3.80f,42),
            new Vector4(.68f,-6.40f,-3.80f,41),new Vector4(.60f,-6.50f,-4.00f,42),
            new Vector4(.68f,-6.50f,-4.10f,39),new Vector4(.58f,-6.60f,-4.10f,43),
            new Vector4(.64f,-6.40f,-3.90f,42),new Vector4(.63f,-6.60f,-3.90f,41),
            new Vector4(.68f,-6.40f,-3.80f,40),new Vector4(.66f,-6.50f,-3.70f,41)
        };
        static readonly Vector4[] DistantFraming={
            new Vector4(.32f,8.8f,-1.9f,61),new Vector4(.30f,8.4f,-1.8f,61),
            new Vector4(.29f,9.0f,-1.9f,63),new Vector4(.28f,8.8f,-2.0f,65),
            new Vector4(.34f,8.6f,-1.8f,63),new Vector4(.29f,8.5f,-2.1f,65),
            new Vector4(.36f,8.8f,-2.2f,61),new Vector4(.28f,8.7f,-2.2f,66),
            new Vector4(.31f,8.5f,-2.0f,64),new Vector4(.31f,8.8f,-2.0f,63),
            new Vector4(.35f,8.7f,-1.9f,62),new Vector4(.34f,8.6f,-1.8f,63)
        };
        // Architectural illumination follows each sky, while keeping solid
        // facade colours and the authored window/beacon palette intact. Neon
        // retains its exact existing warm key and cool fill.
        static readonly Color[] WorldKeyLight={
            new Color(1,.87f,.73f),new Color(.80f,.94f,1),new Color(1,.87f,.70f),
            new Color(.72f,.85f,1),new Color(1,.82f,.90f),new Color(.82f,.90f,1),
            new Color(.88f,1,.89f),new Color(.89f,.82f,1),new Color(.89f,.83f,.98f),
            new Color(.78f,.88f,1),new Color(1,.86f,.66f),new Color(.82f,.97f,1)
        };
        public static Color KeyLightForWorld(int world)=>WorldKeyLight[Mathf.Clamp(world,0,Themes-1)];
        public static Color FillLightForWorld(int world)
        {
            world=Mathf.Clamp(world,0,Themes-1);
            var neutral=new Color(.18f,.29f,.43f);
            return world==0?neutral:Color.Lerp(neutral,SkyPulseRouteArchitecture.Accent(world)*.52f,.28f);
        }
        static Color FacadeKeyForWorld(int world)=>world==0?new Color(1.05f,.91f,.75f):KeyLightForWorld(world)*1.045f;
        static readonly float[] CivicOrientation={-16,12,-12,-13,-8,-18,-12,-16,-9,-14,-10,-13};
        static readonly float[] ResidentialOrientation={14,-18,16,18,12,16,14,20,11,17,13,16};
        readonly float[] worldViewOrigins=new float[Themes];
        readonly bool[] worldViewOriginKnown=new bool[Themes];
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
            BuildCityLighting();
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
                // Opaque district depth lets the forward pass receive real shadows.
                // The separate sky dome still draws only behind this depth.
                surfaces[bank].renderQueue=2450;
                for(int i=0;i<Layers*PerLayer;i++)
                {
                    int layer=i/PerLayer;
                    var renderer=SkyPulseMeshBuilder.Surface(worldScene,"Flight depth "+bank+" / "+layer+" / "+i,silhouettes[0],surfaces[bank],-27+layer*5);
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.TwoSided;renderer.receiveShadows=true;
                    scenery[bank,i]=renderer.transform;districtRenderers[bank,i]=renderer;districtFinish[bank,i]=new MaterialPropertyBlock();
                }
            }
            atmosphereDepth=new GameObject("Layered distant atmosphere").AddComponent<SkyPulseAtmosphereDepth>();atmosphereDepth.Build(worldScene);
            // The original sky colour lives on an enclosing dome. The city
            // illustration is cropped away and replaced by inhabited mesh islands.
            var dome=new SkyPulseMeshBuilder();const int slices=40,rings=24;
            for(int row=0;row<rings;row++)for(int side=0;side<slices;side++)
            {
                float a=side*Mathf.PI*2/slices,c=(side+1)*Mathf.PI*2/slices,u=row*Mathf.PI/rings,w=(row+1)*Mathf.PI/rings;
                dome.Quad(Sphere(a,u),Sphere(c,u),Sphere(c,w),Sphere(a,w),Color.white);
            }
            skyMesh=dome.Finish("Constructed atmospheric sky dome");skyMaterial=new Material(Resources.Load<Shader>("SkyPulse/Shaders/WorldAtmosphere")){name="Endless physical sky palette"};
            cloudNoise=BuildCloudNoise(out ownsCloudNoise);skyMaterial.SetTexture("_CloudNoise",cloudNoise);
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
        void BuildCityLighting()
        {
            // One bounded key for the isolated city layer. It neither changes
            // project quality settings nor lights the bird, HUD or flight pipes.
            var source=new GameObject("Architectural sun / facade and terrace relief",typeof(Light));
            source.transform.SetParent(worldScene,false);
            source.transform.localRotation=Quaternion.LookRotation(-new Vector3(-.64f,.87f,-.75f).normalized,Vector3.up);
            var key=source.GetComponent<Light>();cityKey=key;key.type=LightType.Directional;
            key.cullingMask=1<<30;key.color=new Color(1,.87f,.73f);key.intensity=1;
            key.shadows=LightShadows.Hard;key.shadowStrength=.84f;
            key.shadowCustomResolution=1024;key.shadowBias=.025f;key.shadowNormalBias=.12f;
            key.renderMode=LightRenderMode.ForcePixel;
        }
        static Texture3D BuildCloudNoise(out bool owned)
        {
            // The coherent 64 cubed RGBA volume is baked once in the Editor.
            // Sharing the resource avoids both runtime noise construction and
            // duplicate texture memory when preview environments coexist.
            var cached=Resources.Load<Texture3D>("SkyPulse/WorldCloudDensity");
            owned=cached==null;if(cached!=null)return cached;
            // A bounded legacy fallback keeps an unbaked source checkout usable.
            // One small, deterministic volume serves all twelve worlds. It is
            // created only with the environment, never while flying or changing
            // palettes. R8 is 32 KiB; older devices use a bounded RGBA fallback.
            const int size=32;bool singleChannel=SystemInfo.SupportsTextureFormat(TextureFormat.R8);
            var volume=new Texture3D(size,size,size,singleChannel?TextureFormat.R8:TextureFormat.RGBA32,false)
            {name="Shared 32 cubed cloud density noise",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};
            // Integer noise avoids floating-point sine precision producing
            // correlated rows at the large seeds used by a complete 3D grid.
            var density=new byte[size*size*size];uint state=0x6d2b79f5;
            for(int i=0;i<density.Length;i++){state^=state<<13;state^=state>>17;state^=state<<5;density[i]=(byte)state;}
            if(singleChannel)volume.SetPixelData(density,0);
            else
            {
                var values=new Color32[density.Length];for(int i=0;i<values.Length;i++){byte value=density[i];values[i]=new Color32(value,value,value,255);}
                volume.SetPixels32(values);
            }
            volume.Apply(false,true);return volume;
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
        public void Render()
        {
            if(worldCamera==null||!gameObject.activeInHierarchy)return;
            // The mobile quality tier's 20-unit shadow range cannot reach the
            // isolated city, 37 units behind its camera. Extend only this manual
            // camera render; the main game and project settings keep their value.
            float previousShadowDistance=QualitySettings.shadowDistance;
            try{QualitySettings.shadowDistance=Mathf.Max(previousShadowDistance,62);worldCamera.Render();}
            finally{QualitySettings.shadowDistance=previousShadowDistance;}
        }
        void OnEnable(){if(worldScene!=null)worldScene.gameObject.SetActive(true);}
        void OnDisable(){if(worldScene!=null)worldScene.gameObject.SetActive(false);}
        public void ResetView(){current=-1;bankTheme[0]=bankTheme[1]=-1;distanceOffset=clockOffset=presentationClock=0;hasPresentation=motionHeld=false;System.Array.Clear(worldViewOrigins,0,Themes);System.Array.Clear(worldViewOriginKnown,0,Themes);}
        // Handoffs operate in displayed coordinates; Reduced Motion may have
        // rebased the input distance. Convert once rather than applying it twice.
        public float InputDistanceForView(float displayed)=>displayed-distanceOffset;
        public float PresentationClock=>presentationClock;
        public void ResetView(float displayedDistance,float displayedClock)
        {
            ResetView();Travel=displayedDistance;presentationClock=displayedClock;
            hasPresentation=true;motionHeld=true;
        }
        public void Present(int world,float distance,float clock,float transition,Color tint,Color haze,bool reduced,float entry=1)
        {
            world=Mathf.Clamp(world,0,Themes-1);
            if(current!=world){previous=current<0?world:current;previousTint=current<0?tint:currentTint;previousHaze=current<0?haze:currentHaze;current=world;currentTint=tint;currentHaze=haze;}
            // Enabling Reduced Motion holds the image the player is already
            // looking at. Turning it off resumes from that position; neither
            // preference change rewinds the city or its lighting to time zero.
            if(!hasPresentation)
            {
                Travel=distance;presentationClock=clock;
                hasPresentation=true;motionHeld=reduced;
            }
            else if(!reduced)
            {
                if(motionHeld){distanceOffset=Travel-distance;clockOffset=presentationClock-clock;motionHeld=false;}
                Travel=distance+distanceOffset;presentationClock=clock+clockOffset;
            }
            else motionHeld=true;
            clock=presentationClock;
            SetBank(0,previous,Travel,clock,previous==world?0:(1-transition)*entry,previousTint,previousHaze);
            SetBank(1,world,Travel,clock,(previous==world?1:transition)*entry,tint,haze);
            atmosphereDepth.Present(0,previous,Travel,clock,previous==world?0:(1-transition)*entry,previousTint,previousHaze);
            atmosphereDepth.Present(1,world,Travel,clock,(previous==world?1:transition)*entry,tint,haze);
            float blend=previous==world?1:Mathf.Clamp01(transition);
            if(cityKey!=null)
            {
                Color keyTone=Color.Lerp(KeyLightForWorld(previous),KeyLightForWorld(world),blend);
                if(cityKey.color!=keyTone)cityKey.color=keyTone;
            }
            skyMaterial.SetTexture("_SkyA",reflections[previous]!=null?reflections[previous]:Texture2D.grayTexture);skyMaterial.SetTexture("_SkyB",reflections[world]!=null?reflections[world]:Texture2D.grayTexture);skyMaterial.SetFloat("_Blend",blend);
            skyMaterial.SetFloat("_WorldA",previous);skyMaterial.SetFloat("_WorldB",world);
            skyMaterial.SetColor(Tint,Color.Lerp(previousTint,tint,blend));skyMaterial.SetColor(Haze,Color.Lerp(previousHaze,haze,blend));skyMaterial.SetFloat(Clock,clock);skyMaterial.SetFloat("_World",Mathf.Lerp(previous,world,blend));
        }
        void SetBank(int bank,int theme,float distance,float clock,float visibility,Color tint,Color haze)
        {
            bool changed=bankTheme[bank]!=theme;bankTheme[bank]=theme;
            // Theme-owned origins keep the outgoing bank registered with the
            // incoming bank it replaces. A new world's authored arrival starts
            // at its actual route-entry distance, even at a nonzero run score.
            if(!worldViewOriginKnown[theme]||(changed&&bank==1))
            {worldViewOrigins[theme]=distance;worldViewOriginKnown[theme]=true;}
            float worldDistance=distance-worldViewOrigins[theme];
            var material=surfaces[bank];
            material.SetColor(KeyTone,FacadeKeyForWorld(theme));material.SetColor(FillTone,FillLightForWorld(theme));
            material.SetFloat("_Geology",theme==0?1:0);material.SetFloat("_Design",theme);material.SetFloat(Visibility,visibility);material.SetFloat(Clock,clock);material.SetColor(Tint,tint);material.SetColor(Haze,haze);
            if(changed)
            {
                material.SetTexture("_Environment",reflections[theme]!=null?reflections[theme]:Texture2D.grayTexture);
                SkyPulseWorldArchitecture.ConfigureBeaconLighting(material,theme);
            }
            for(int i=0;i<Layers*PerLayer;i++)
            {
                var item=scenery[bank,i];bool visible=visibility>.0001f;
                int layer=i/PerLayer,slot=i%PerLayer;
                // Exactly three districts occupy the fixed twelve-item pool.
                // The complete near layer is dormant; no giant cropped tower
                // rises from the screen edge or repeats the landmark at full size.
                int districtCount=layer==0?1:layer==1?2:0;bool show=visible&&slot<districtCount;
                if(item.gameObject.activeSelf!=show)item.gameObject.SetActive(show);
                int variant=layer==1&&slot==1?1:0;
                if(changed&&slot<districtCount)
                {
                    item.GetComponent<MeshFilter>().sharedMesh=variant==0?silhouettes[theme]:residential[theme];
                    var renderer=districtRenderers[bank,i];var finish=districtFinish[bank,i];
                    renderer.SetPropertyBlock(authoredLighting[theme,variant]);renderer.GetPropertyBlock(finish);
                    finish.SetFloat("_DistrictDepth",layer==0?.45f:slot==0?.08f:.23f);
                    finish.SetFloat("_WindowGlow",layer==0?.18f:slot==0?.84f:.48f);
                    finish.SetFloat("_BeaconGlow",layer==0?.10f:slot==0?.50f:.25f);
                    finish.SetFloat("_Backdrop",layer==0?.05f:0);
                    renderer.SetPropertyBlock(finish);
                }
                if(!show)continue;
                Vector4 frame=layer==0?DistantFraming[theme]:slot==0?CivicFraming[theme]:ResidentialFraming[theme];
                float speed=layer==0?.032f:slot==0?.095f:.062f;
                float spacing=span*(layer==0?2.8f:2.2f);
                float x=WrapDistrict(frame.y-worldDistance*speed,spacing);
                item.localPosition=new Vector3(x,frame.z,frame.w);
                item.localScale=Vector3.one*frame.x;
                float orientation=layer==0?CivicOrientation[theme]+8:slot==0?CivicOrientation[theme]:ResidentialOrientation[theme];
                item.localRotation=Quaternion.Euler(0,orientation,0);
            }
        }
        static float WrapDistrict(float position,float spacing)=>Mathf.Repeat(position+spacing*.5f,spacing)-spacing*.5f;
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
            if(skyMesh!=null)Destroy(skyMesh);if(skyMaterial!=null)Destroy(skyMaterial);if(ownsCloudNoise&&cloudNoise!=null)Destroy(cloudNoise);
            if(worldScene!=null)Destroy(worldScene.gameObject);if(worldImage!=null){worldImage.Release();Destroy(worldImage);}if(compositeMesh!=null)Destroy(compositeMesh);if(compositeMaterial!=null)Destroy(compositeMaterial);
        }
    }
}
