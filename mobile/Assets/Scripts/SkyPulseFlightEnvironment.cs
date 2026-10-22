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
        static readonly int Visibility=Shader.PropertyToID("_Visibility"), Clock=Shader.PropertyToID("_Clock"), Tint=Shader.PropertyToID("_Tint"), Haze=Shader.PropertyToID("_Haze");
        float span; int current=-1,previous;
        Color currentTint,currentHaze,previousTint,previousHaze;
        public int CurrentWorld=>current;
        public float Travel { get; private set; }
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
            for(int i=0;i<Layers*PerLayer;i++)
            {
                var item=scenery[bank,i];bool visible=visibility>.0001f;
                if(item.gameObject.activeSelf!=visible)item.gameObject.SetActive(visible);
                if(changed)item.GetComponent<MeshFilter>().sharedMesh=silhouettes[theme];
                if(!visible)continue;
                int layer=i/PerLayer,slot=i%PerLayer;
                float seed=Hash(theme*43+i*17),scale=(.60f+layer*.42f)*( .70f+seed*.60f);
                float x=Mathf.Repeat(slot*span/PerLayer+seed*1.4f-distance*(.10f+layer*.16f)+span*.5f,span)-span*.5f;
                float y=layer==0?(slot%3==0?6.9f:-6.7f):(slot%3==0?8.8f:-8.7f);
                // Near silhouettes remain on the margins, never over the pilot.
                if(layer==2)y=slot%2==0?10.6f:-10.9f;
                y+=Mathf.Sin(clock*(.15f+seed*.1f)+seed*6)*.065f;
                item.localPosition=new Vector3(x,y,14-layer*4);
                item.localScale=new Vector3(scale,slot%3==0?-scale:scale,scale);
                item.localRotation=Quaternion.Euler(3+seed*7,18+seed*35,Mathf.Sin(clock*.12f+seed*5)*.6f);
            }
        }
        static float Hash(int n){uint x=(uint)n*747796405u+2891336453u;x=((x>>((int)(x>>28)+4))^x)*277803737u;return ((x>>22)^x)/4294967295f;}
        static readonly Color Metal=new Color(.32f,.45f,.58f,0),Light=new Color(.55f,.9f,1,1);
        static Mesh Architecture(int theme)
        {
            var b=new SkyPulseMeshBuilder();int kind=theme%6;
            // Twelve distinct signatures share a small vocabulary of curved,
            // faceted forms. The second cycle changes silhouette, not just colour.
            int count=3+theme%4;float height=2.1f+theme*.055f;
            if(kind==0 || kind==5)
            {
                for(int i=0;i<count;i++)
                {
                    float x=(i-(count-1)*.5f)*.52f,h=height*(.6f+Hash(theme*23+i)*.65f);
                    Spire(b,new Vector3(x,0,i%2*.28f),.24f,h,kind==5?5:8);
                    b.Box(new Vector3(x-.16f,h*.48f,-.2f),new Vector3(.024f,h*.55f,.026f),Light);
                }
            }
            else if(kind==1 || kind==3)
            {
                float radius=kind==1?1.25f:1.65f;
                b.RingArc(radius,.10f,36,theme>5?18:32,theme>5?310:220,Metal);
                b.RingArc(radius-.14f,.018f,36,theme>5?18:32,theme>5?310:220,Light);
                for(int i=0;i<4;i++)Spire(b,new Vector3((i-1.5f)*.54f,-1.2f,.45f),.15f,.85f+Hash(i+theme)*.8f,6);
            }
            else
            {
                // Suspended tapered landforms and solar sails have actual volume.
                Spire(b,new Vector3(0,-1.7f,0),1.35f,1.7f,7);
                for(int i=0;i<count;i++)
                {
                    float x=(i-(count-1)*.5f)*.48f;
                    if(kind==2)Spire(b,new Vector3(x,0,i%2*.3f),.13f,1.3f+Hash(i+theme)*1.2f,5);
                    else {b.RingArc(.40f+i*.06f,.035f,24,15,235,i%2==0?Metal:Light);Spire(b,new Vector3(x,0,.1f),.12f,.9f+Hash(i+theme)*.7f,7);}
                }
            }
            return b.Finish("Authored 3D world silhouette "+theme);
        }
        static void Spire(SkyPulseMeshBuilder b,Vector3 centre,float radius,float height,int sides)
        {
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,c=(i+1)*Mathf.PI*2/sides;
                var p=centre+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                var q=centre+new Vector3(Mathf.Cos(c)*radius,0,Mathf.Sin(c)*radius);
                var r=centre+new Vector3(Mathf.Cos(c)*radius*.63f,height*.72f,Mathf.Sin(c)*radius*.63f);
                var s=centre+new Vector3(Mathf.Cos(a)*radius*.63f,height*.72f,Mathf.Sin(a)*radius*.63f);
                b.Quad(p,q,r,s,Metal);b.Face(s,r,centre+Vector3.up*height,new Color(.43f,.56f,.68f,0));
                b.Face(centre,q,p,Metal);
            }
        }
        void OnDestroy()
        {
            foreach(var mesh in silhouettes)if(mesh!=null)Destroy(mesh);
            foreach(var material in surfaces)if(material!=null)Destroy(material);
        }
    }
}
