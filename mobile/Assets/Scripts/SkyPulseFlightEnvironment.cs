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
            var plan=new[]{new Vector2(-.40f,-.50f),new Vector2(.40f,-.50f),new Vector2(.50f,-.40f),new Vector2(.50f,.40f),new Vector2(.40f,.50f),new Vector2(-.40f,.50f),new Vector2(-.50f,.40f),new Vector2(-.50f,-.40f)};
            var profile=new[]{new Vector2(1.04f,-1.8f),new Vector2(1.04f,.08f),new Vector2(.96f,.13f),new Vector2(.96f,height*.82f),new Vector2(.79f,height*.87f),new Vector2(.79f,height),new Vector2(.68f,height+.07f)};
            for(int j=1;j<profile.Length;j++)for(int side=0;side<8;side++)
            {
                var a=plan[side];var c=plan[(side+1)%8];var low=profile[j-1];var high=profile[j];
                var colour=j==3&&(side==0||side==4)?Glass:Metal;
                b.Quad(centre+new Vector3(a.x*width*low.x,low.y,a.y*depth*low.x),centre+new Vector3(c.x*width*low.x,low.y,c.y*depth*low.x),
                    centre+new Vector3(c.x*width*high.x,high.y,c.y*depth*high.x),centre+new Vector3(a.x*width*high.x,high.y,a.y*depth*high.x),colour);
                if(j==profile.Length-1)b.Face(centre+new Vector3(0,high.y,0),centre+new Vector3(a.x*width*high.x,high.y,a.y*depth*high.x),centre+new Vector3(c.x*width*high.x,high.y,c.y*depth*high.x),Metal);
            }
            float front=-depth*.48f-.003f;
            for(int row=0;row<12;row++)
            {
                float y=.23f+row*(height*.56f/12);
                b.Box(centre+new Vector3(0,y,front),new Vector3(width*.73f,.009f,.009f),Metal);
                for(int column=0;column<3;column++)
                {
                    float x=(column-1)*width*.22f;
                    bool lit=Hash(variant*97+row*11+column*7)>.34f;
                    var hue=lit?new Color(.30f,.61f,.77f,.82f):new Color(.06f,.12f,.18f,.30f);
                    b.Box(centre+new Vector3(x,y+.035f,front-.004f),new Vector3(width*.13f,.026f,.004f),hue);
                }
            }
            for(int side=-1;side<=1;side+=2)
                b.Box(centre+new Vector3(side*width*.39f,height*.44f,front-.004f),new Vector3(.009f,height*.66f,.010f),Light);
            b.Box(centre+new Vector3(0,height+.073f,0),new Vector3(width*.55f,.012f,depth*.45f),new Color(.35f,.65f,.83f,.85f));
            if(variant%3==0)b.Box(centre+new Vector3(width*.18f,height+.17f,0),new Vector3(.012f,.20f,.012f),Metal);
        }
        void OnDestroy()
        {
            foreach(var mesh in silhouettes)if(mesh!=null)Destroy(mesh);
            foreach(var material in surfaces)if(material!=null)Destroy(material);
        }
    }
}
