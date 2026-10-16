using UnityEngine;

namespace SkyPulse.Mobile
{
    // Fixed-capacity ribbons, passage ripples and sparks. No Instantiate, Destroy,
    // coroutine or particle allocation on flaps/pickups/passes.
    public sealed class SkyPulseFlightEffects : MonoBehaviour
    {
        struct Spark { public Vector2 Point,Velocity;public float Life,Duration;public Color Colour; }
        struct Ripple { public Vector2 Point;public float Life; }
        readonly Spark[] sparks=new Spark[48];readonly Ripple[] ripples=new Ripple[6];
        readonly Vector2[] trail=new Vector2[24];readonly Vector3[] vertices=new Vector3[1600];readonly Color[] colours=new Color[1600];
        Mesh mesh;Material material;int sparkAt,rippleAt,count,used;float boost,sample;Color tint;Vector2 previousPilot;
        public float Boost=>boost;
        public void Build()
        {
            mesh=new Mesh{name="Pooled engine ribbon and passage ripples"};mesh.MarkDynamic();mesh.vertices=vertices;mesh.colors=colours;
            var triangles=new int[vertices.Length/4*6];for(int i=0;i<vertices.Length/4;i++){int t=i*6,v=i*4;triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;}mesh.triangles=triangles;mesh.bounds=new Bounds(Vector3.zero,new Vector3(50,50,4));
            material=new Material(Resources.Load<Shader>("SkyPulse/Shaders/FlightLight")){name="SkyPulse restrained additive light"};SkyPulseMeshBuilder.Surface(transform,"Engine ribbon and pooled confirmation",mesh,material,13);
        }
        public void ResetPresentation(){count=used=0;boost=sample=0;for(int i=0;i<sparks.Length;i++)sparks[i].Life=0;for(int i=0;i<ripples.Length;i++)ripples[i].Life=0;System.Array.Clear(colours,0,colours.Length);if(mesh!=null)mesh.colors=colours;}
        public void Flap(Vector2 at,Color colour,bool reduced)
        {
            boost=1;if(reduced)return;
            for(int i=0;i<5;i++)Emit(at+Vector2.left*.74f,new Vector2(-1.5f-i*.3f,(i-2)*.27f),.25f,colour);
        }
        void Emit(Vector2 at,Vector2 velocity,float duration,Color colour){sparks[sparkAt]=new Spark{Point=at,Velocity=velocity,Life=duration,Duration=duration,Colour=colour};sparkAt=(sparkAt+1)%sparks.Length;}
        public void Passage(Vector2 at,Color colour,bool reduced)
        {
            if(reduced)return;ripples[rippleAt]=new Ripple{Point=at,Life=.55f};rippleAt=(rippleAt+1)%ripples.Length;
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Emit(at,new Vector2(Mathf.Cos(a)*1.7f-1.1f,Mathf.Sin(a)*1.7f),.45f,colour);}
        }
        public void Tick(float dt,Vector2 pilot,float glide,Color colour,bool reduced)
        {
            tint=colour;boost=Mathf.Max(0,boost-dt*4.8f);used=0;System.Array.Clear(colours,0,colours.Length);
            if(reduced){count=0;for(int i=0;i<sparks.Length;i++)sparks[i].Life=0;for(int i=0;i<ripples.Length;i++)ripples[i].Life=0;}
            else
            {
                for(int i=1;i<count;i++)trail[i]+=Vector2.left*(4.2f*dt);
                if(count==0){previousPilot=pilot;trail[0]=pilot+new Vector2(-.82f,-.07f);count=1;}
                sample+=dt;
                // Fixed-time samples retain the same ribbon duration at every frame rate.
                while(sample>=1f/60)
                {
                    sample-=1f/60;float f=dt>0?Mathf.Clamp01(1-sample/dt):1;
                    for(int i=Mathf.Min(count,trail.Length-1);i>0;i--)trail[i]=trail[i-1];
                    trail[0]=Vector2.Lerp(previousPilot,pilot,f)+new Vector2(-.82f,-.07f);count=Mathf.Min(trail.Length,count+1);
                }
                previousPilot=pilot;
                for(int i=1;i<count;i++)
                {
                    float age=1-i/(float)count,w=(.035f+boost*.035f)*age;
                    Line(trail[i-1],trail[i],w*3,A(tint,age*.07f));Line(trail[i-1],trail[i],w,A(Color.Lerp(tint,Color.white,.65f),age*.45f));
                }
                for(int i=0;i<sparks.Length;i++)
                {
                    if(sparks[i].Life<=0)continue;sparks[i].Life=Mathf.Max(0,sparks[i].Life-dt);sparks[i].Point+=sparks[i].Velocity*dt;float f=sparks[i].Life/sparks[i].Duration;
                    Line(sparks[i].Point-sparks[i].Velocity*.035f,sparks[i].Point,.018f*f,A(sparks[i].Colour,f*.7f));
                }
                for(int i=0;i<ripples.Length;i++)
                {
                    if(ripples[i].Life<=0)continue;ripples[i].Life=Mathf.Max(0,ripples[i].Life-dt);float f=ripples[i].Life/.55f,r=1.7f+(1-f)*.75f;ripples[i].Point+=Vector2.left*dt*2;
                    for(int j=0;j<32;j++){float a=j*Mathf.PI/16,b=(j+1)*Mathf.PI/16;Line(ripples[i].Point+new Vector2(Mathf.Cos(a)*r*.45f,Mathf.Sin(a)*r),ripples[i].Point+new Vector2(Mathf.Cos(b)*r*.45f,Mathf.Sin(b)*r),.025f,A(new Color(.45f,1,.9f),f*.55f));}
                }
            }
            // Small fixed exhaust core remains readable with Reduced Motion.
            float length=.24f+boost*.35f;var nozzle=pilot+new Vector2(-.77f,-.08f);
            Line(nozzle,nozzle+Vector2.left*length,.045f,A(Color.Lerp(tint,Color.white,.7f),.72f));
            mesh.vertices=vertices;mesh.colors=colours;
        }
        static Color A(Color c,float a){c.a=a;return c;}
        void Line(Vector2 a,Vector2 b,float width,Color c)
        {
            if(used+4>vertices.Length)return;var n=new Vector2(-(b-a).y,(b-a).x).normalized*width;
            vertices[used]=a-n;vertices[used+1]=a+n;vertices[used+2]=b+n;vertices[used+3]=b-n;for(int i=0;i<4;i++)colours[used+i]=c;used+=4;
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
