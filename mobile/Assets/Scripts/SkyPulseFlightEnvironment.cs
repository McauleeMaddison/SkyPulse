using UnityEngine;

namespace SkyPulse.Mobile
{
    /// <summary>Shared, collider-free architecture in the existing flight camera.
    /// Two fixed banks crossfade with the authored world; no RenderTexture camera.
    /// All art is built once, and animation consumes absolute simulation distance.</summary>
    public sealed class SkyPulseFlightEnvironment : MonoBehaviour
    {
        const int Themes=12, Layers=3, PerLayer=6;
        readonly Mesh[] silhouettes=new Mesh[Themes];
        readonly Transform[,] scenery=new Transform[2,Layers*PerLayer];
        readonly Material[] surfaces=new Material[2];
        readonly int[] bankTheme={-1,-1};
        readonly Texture[] reflections=new Texture[Themes];
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
            for(int theme=0;theme<Themes;theme++)silhouettes[theme]=Architecture(theme);
            var shader=Resources.Load<Shader>("SkyPulse/Shaders/FlightArchitecture");
            for(int bank=0;bank<2;bank++)
            {
                surfaces[bank]=new Material(shader){name="Shared world architecture bank "+bank};
                for(int i=0;i<Layers*PerLayer;i++)
                {
                    int layer=i/PerLayer;
                    var renderer=SkyPulseMeshBuilder.Surface(transform,"Flight depth "+bank+" / "+layer+" / "+i,silhouettes[0],surfaces[bank],-27+layer*5);
                    scenery[bank,i]=renderer.transform;
                }
            }
        }
        public void ResetView(){current=-1;bankTheme[0]=bankTheme[1]=-1;}
        public void Present(int world,float distance,float clock,float transition,Color tint,Color haze,bool reduced,float entry=1)
        {
            world=Mathf.Clamp(world,0,Themes-1);
            if(current!=world){previous=current<0?world:current;previousTint=current<0?tint:currentTint;previousHaze=current<0?haze:currentHaze;current=world;currentTint=tint;currentHaze=haze;}
            Travel=reduced?0:distance; clock=reduced?0:clock;
            SetBank(0,previous,Travel,clock,previous==world?0:(1-transition)*entry,previousTint,previousHaze);
            SetBank(1,world,Travel,clock,(previous==world?1:transition)*entry,tint,haze);
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
                float seed=Hash(theme*43+i*17),scale=(.34f+layer*.15f)*(.88f+seed*.20f);
                float x=Mathf.Repeat(slot*span/PerLayer+seed*1.4f-distance*(.10f+layer*.16f)+span*.5f,span)-span*.5f;
                // The skyline is anchored below the route. Architecture never
                // bobs, flips upside down or floats on exposed circular plates.
                float y=layer==0?-7.65f:layer==1?-8.45f:-9.35f;
                item.localPosition=new Vector3(x,y,14-layer*4);
                item.localScale=Vector3.one*scale;
                item.localRotation=Quaternion.Euler(0,14+seed*18,0);
            }
        }
        static float Hash(int n){uint x=(uint)n*747796405u+2891336453u;x=((x>>((int)(x>>28)+4))^x)*277803737u;return ((x>>22)^x)/4294967295f;}
        // Vertex alpha selects metal, glazing or a lit window in the shared
        // material. These are authored mesh details, not runtime spawned props.
        static readonly Color Metal=new Color(.28f,.40f,.53f,0),Glass=new Color(.16f,.32f,.45f,.34f),Light=new Color(.38f,.77f,.92f,1);
        static Mesh Architecture(int theme)
        {
            var b=new SkyPulseMeshBuilder();int count=2+theme%3;
            for(int i=0;i<count;i++)
            {
                float seed=Hash(theme*31+i*53),width=.46f+seed*.24f;
                float x=(i-(count-1)*.5f)*.62f,height=1.8f+seed*1.3f+(theme%4)*.10f;
                Building(b,new Vector3(x,0,(i%2)*.26f),width,.55f+seed*.21f,height,theme+i);
            }
            return b.Finish("Glazed skyline architecture "+theme);
        }
        static void Building(SkyPulseMeshBuilder b,Vector3 centre,float width,float depth,float height,int variant)
        {
            // Chamfered curtain-wall tower: recessed glazing, stepped crown,
            // thin floor reveals and a foundation extending below the viewport.
            const int sides=32;
            var plan=new Vector2[sides];
            for(int side=0;side<sides;side++)
            {
                float angle=side*Mathf.PI*2/sides,c=Mathf.Cos(angle),sn=Mathf.Sin(angle);
                plan[side]=new Vector2(Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),.70f)*.5f,Mathf.Sign(sn)*Mathf.Pow(Mathf.Abs(sn),.70f)*.5f);
            }
            var profile=new[]{new Vector2(.98f,-1.8f),new Vector2(.98f,.08f),new Vector2(.94f,.15f),new Vector2(.94f,height*.88f),new Vector2(.90f,height*.94f),new Vector2(.77f,height),new Vector2(.65f,height+.04f)};
            float lean=variant%2==0?.09f:-.07f;
            for(int j=1;j<profile.Length;j++)for(int side=0;side<sides;side++)
            {
                var a=plan[side];var c=plan[(side+1)%sides];var low=profile[j-1];var high=profile[j];
                var na=new Vector3(a.x/(width*width),0,a.y/(depth*depth)).normalized;var nc=new Vector3(c.x/(width*width),0,c.y/(depth*depth)).normalized;
                float lowerSweep=lean*Mathf.Pow(Mathf.Max(0,low.y/height),2),upperSweep=lean*Mathf.Pow(Mathf.Max(0,high.y/height),2);
                b.SmoothQuad(centre+new Vector3(a.x*width*low.x+lowerSweep,low.y,a.y*depth*low.x),centre+new Vector3(c.x*width*low.x+lowerSweep,low.y,c.y*depth*low.x),
                    centre+new Vector3(c.x*width*high.x+upperSweep,high.y,c.y*depth*high.x),centre+new Vector3(a.x*width*high.x+upperSweep,high.y,a.y*depth*high.x),na,nc,j==3?Glass:Metal);
                if(j==profile.Length-1)b.Face(centre+new Vector3(upperSweep,high.y,0),centre+new Vector3(a.x*width*high.x+upperSweep,high.y,a.y*depth*high.x),centre+new Vector3(c.x*width*high.x+upperSweep,high.y,c.y*depth*high.x),Metal);
            }
            for(int row=0;row<12;row++)
            {
                float y=.23f+row*(height*.61f/12),sweep=lean*Mathf.Pow(y/height,2);
                for(int side=17;side<31;side++)
                {
                    var a=Vector2.Lerp(plan[side],plan[(side+1)%sides],.15f);var c=Vector2.Lerp(plan[side],plan[(side+1)%sides],.78f);
                    bool lit=Hash(variant*97+row*11+side*7)>.28f;var hue=lit?new Color(.30f,.64f,.82f,.86f):new Color(.10f,.20f,.29f,.30f);
                    b.Quad(centre+new Vector3(a.x*width*.946f+sweep,y,a.y*depth*.946f),centre+new Vector3(c.x*width*.946f+sweep,y,c.y*depth*.946f),
                        centre+new Vector3(c.x*width*.946f+sweep,y+.022f,c.y*depth*.946f),centre+new Vector3(a.x*width*.946f+sweep,y+.022f,a.y*depth*.946f),hue);
                }
            }
            // Continuous curved light reveals wrap the crown and glass facade.
            for(int band=0;band<3;band++)
            {
                float y=band==2?height+.015f:height*(band==0?.20f:.78f),radius=band==2?.72f:.947f,sweep=lean*Mathf.Pow(y/height,2);
                for(int side=0;side<sides;side++)
                {
                    var a=plan[side];var c=plan[(side+1)%sides];
                    b.Quad(centre+new Vector3(a.x*width*radius+sweep,y,a.y*depth*radius),centre+new Vector3(c.x*width*radius+sweep,y,c.y*depth*radius),
                        centre+new Vector3(c.x*width*radius+sweep,y+.010f,c.y*depth*radius),centre+new Vector3(a.x*width*radius+sweep,y+.010f,a.y*depth*radius),Light);
                }
            }
        }

        void OnDestroy()
        {
            foreach(var mesh in silhouettes)if(mesh!=null)Destroy(mesh);
            foreach(var material in surfaces)if(material!=null)Destroy(material);
        }
    }
}
