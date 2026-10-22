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
                float seed=Hash(theme*43+i*17),scale=(.44f+layer*.28f)*(.80f+seed*.35f);
                float x=Mathf.Repeat(slot*span/PerLayer+seed*1.4f-distance*(.10f+layer*.16f)+span*.5f,span)-span*.5f;
                // City/dock forms belong on the lower horizon. Only symmetrical
                // orbital machinery can frame the upper edge; no dangling towers.
                bool upper=(theme%6==1||theme%6==3)&&slot%3==0;
                float y=layer==0?(upper?8.9f:-7.3f):layer==1?(upper?9.6f:-8.1f):(upper?11.2f:-9.1f);
                // Architecture frames the horizon rather than filling the route.
                y+=Mathf.Sin(clock*(.15f+seed*.1f)+seed*6)*.065f;
                item.localPosition=new Vector3(x,y,14-layer*4);
                item.localScale=new Vector3(scale,upper?-scale:scale,scale);
                item.localRotation=Quaternion.Euler(3+seed*5,12+seed*24,Mathf.Sin(clock*.12f+seed*5)*.4f);
            }
        }
        static float Hash(int n){uint x=(uint)n*747796405u+2891336453u;x=((x>>((int)(x>>28)+4))^x)*277803737u;return ((x>>22)^x)/4294967295f;}
        static readonly Color Metal=new Color(.27f,.36f,.48f,0),Light=new Color(.28f,.74f,.88f,.85f);
        static Mesh Architecture(int theme)
        {
            var b=new SkyPulseMeshBuilder();int kind=theme%6;
            // Rounded machined forms, not triangular spires or inverted landforms.
            // Shared geometry is built only once; no new objects during flight.
            int count=3+theme%2;float height=1.65f+theme*.035f;
            if(kind==0 || kind==5)
            {
                for(int i=0;i<count;i++)
                {
                    float x=(i-(count-1)*.5f)*.60f,h=height*(.72f+Hash(theme*23+i)*.30f);
                    RoundedPod(b,new Vector3(x,0,i%2*.18f),kind==5?.27f:.23f,h);
                    for(int j=0;j<3;j++)b.Box(new Vector3(x-.09f+j*.09f,h*.48f,-.225f),new Vector3(.012f,h*.40f,.012f),Light);
                }
                RoundedPod(b,new Vector3(0,-.12f,.10f),1.15f,.18f);
            }
            else if(kind==1 || kind==3)
            {
                float radius=kind==1?.83f:1.05f;
                RoundedRing(b,new Vector3(0,.8f,0),radius,.085f,Metal);
                RoundedRing(b,new Vector3(0,.8f,-.08f),radius,.012f,Light);
                RoundedPod(b,new Vector3(0,-.18f,.22f),1.13f,.22f);
                for(int i=0;i<2;i++)RoundedPod(b,new Vector3((i==0?-1:1)*.68f,0,.25f),.16f,.52f);
                if(theme>5)RoundedRing(b,new Vector3(0,.8f,.3f),radius*.72f,.055f,Metal);
            }
            else
            {
                // Low, layered terraces and lenticular sky docks; broad rounded
                // shoulders give depth without hanging a pointed pyramid below.
                for(int i=0;i<3;i++)
                {
                    float radius=1.2f-i*.26f;
                    RoundedPod(b,new Vector3(0,i*.27f,0),radius,.18f);
                    b.Box(new Vector3(0,i*.27f+.085f,-radius*.88f),new Vector3(radius*.86f,.014f,.014f),Light);
                }
                if(kind==2)for(int i=0;i<3;i++)RoundedPod(b,new Vector3((i-1)*.4f,.71f,0),.13f,.40f+theme*.018f);
                else RoundedRing(b,new Vector3(0,1.1f,.12f),.46f+theme*.015f,.045f,Metal);
            }
            return b.Finish("Rounded world architecture "+theme);
        }
        static void RoundedPod(SkyPulseMeshBuilder b,Vector3 centre,float radius,float height)
        {
            const int sides=32;
            var profile=new[]{new Vector2(0,0),new Vector2(.82f,0),new Vector2(.96f,.07f),new Vector2(1,.16f),new Vector2(1,.18f),new Vector2(1,.20f),new Vector2(1,.80f),new Vector2(1,.82f),new Vector2(1,.84f),new Vector2(.96f,.93f),new Vector2(.82f,1),new Vector2(0,1)};
            for(int i=0;i<sides;i++)for(int j=1;j<profile.Length;j++)
            {
                float a=i*Mathf.PI*2/sides,c=(i+1)*Mathf.PI*2/sides;
                var p=profile[j-1];var q=profile[j];float dy=(q.y-p.y)*height,dr=(q.x-p.x)*radius;
                var na=new Vector3(Mathf.Cos(a)*dy,-dr,Mathf.Sin(a)*dy).normalized;var nc=new Vector3(Mathf.Cos(c)*dy,-dr,Mathf.Sin(c)*dy).normalized;
                b.SmoothQuad(centre+new Vector3(Mathf.Cos(a)*radius*p.x,p.y*height,Mathf.Sin(a)*radius*p.x),centre+new Vector3(Mathf.Cos(c)*radius*p.x,p.y*height,Mathf.Sin(c)*radius*p.x),
                    centre+new Vector3(Mathf.Cos(c)*radius*q.x,q.y*height,Mathf.Sin(c)*radius*q.x),centre+new Vector3(Mathf.Cos(a)*radius*q.x,q.y*height,Mathf.Sin(a)*radius*q.x),na,nc,j==5||j==7?Light:Metal);
            }
        }
        static void RoundedRing(SkyPulseMeshBuilder b,Vector3 centre,float radius,float tube,Color colour)
        {
            const int segments=40,sides=8;
            for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
            {
                float a=i*Mathf.PI*2/segments,c=(i+1)*Mathf.PI*2/segments,u=j*Mathf.PI*2/sides,w=(j+1)*Mathf.PI*2/sides,m=(u+w)*.5f;
                var na=new Vector3(Mathf.Cos(a)*Mathf.Cos(m),Mathf.Sin(a)*Mathf.Cos(m),Mathf.Sin(m));var nc=new Vector3(Mathf.Cos(c)*Mathf.Cos(m),Mathf.Sin(c)*Mathf.Cos(m),Mathf.Sin(m));
                b.SmoothQuad(centre+RingPoint(a,u,radius,tube),centre+RingPoint(c,u,radius,tube),centre+RingPoint(c,w,radius,tube),centre+RingPoint(a,w,radius,tube),na,nc,colour);
            }
        }
        static Vector3 RingPoint(float a,float u,float radius,float tube)=>new Vector3(Mathf.Cos(a)*(radius+Mathf.Cos(u)*tube),Mathf.Sin(a)*(radius+Mathf.Cos(u)*tube),Mathf.Sin(u)*tube);
        void OnDestroy()
        {
            foreach(var mesh in silhouettes)if(mesh!=null)Destroy(mesh);
            foreach(var material in surfaces)if(material!=null)Destroy(material);
        }
    }
}
