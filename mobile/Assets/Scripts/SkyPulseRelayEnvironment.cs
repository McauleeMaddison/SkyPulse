using UnityEngine;

namespace SkyPulse.Mobile
{
    /// <summary>Pooled, collider-free station hardware in the flight camera.</summary>
    public sealed class SkyPulseRelayEnvironment : MonoBehaviour
    {
        readonly Transform[] rails=new Transform[6],turbines=new Transform[2],drones=new Transform[2];
        Mesh railMesh,turbineMesh,droneMesh;Material material;float width;
        static readonly int ClockId=Shader.PropertyToID("_Clock");
        public void Build(float viewWidth,Texture reflection=null)
        {
            width=viewWidth+8;
            var rail=new SkyPulseMeshBuilder();
            Arc(rail,Vector3.zero,7.2f,.085f,-73,146,new Color(.25f,.35f,.43f,0));
            Arc(rail,new Vector3(0,0,-.045f),7.09f,.012f,-73,146,new Color(.28f,.74f,.88f,1));
            for(int j=-3;j<=3;j++)
            {
                float a=j*.31f;var at=new Vector3(Mathf.Cos(a)*7.2f,Mathf.Sin(a)*7.2f,-.02f);
                Arc(rail,at,.13f,.018f,0,360,new Color(.35f,.47f,.56f,0));
            }
            railMesh=rail.Finish("Swept titanium ribs with recessed optical conductors");
            var turbine=new SkyPulseMeshBuilder();Arc(turbine,Vector3.zero,1.25f,.065f,0,360,new Color(.23f,.35f,.43f,0));
            Arc(turbine,new Vector3(0,0,-.03f),1.11f,.012f,0,360,new Color(.30f,.76f,.87f,1));
            for(int i=0;i<12;i++)for(int section=0;section<4;section++)
            {
                float a=i*Mathf.PI/6,r=.31f+section*.205f,next=r+.205f;
                var p=new Vector3(Mathf.Cos(a+section*.07f)*r,Mathf.Sin(a+section*.07f)*r,-.04f);
                var q=new Vector3(Mathf.Cos(a+(section+1)*.07f)*next,Mathf.Sin(a+(section+1)*.07f)*next,-.04f);
                var tangent=new Vector3(-Mathf.Sin(a),Mathf.Cos(a),0)*(.045f+section*.017f);
                turbine.Quad(p-tangent,q-tangent,q+tangent,p+tangent,new Color(.28f,.40f,.48f,0));
                turbine.Quad(p+tangent,q+tangent,q+tangent+Vector3.forward*.07f,p+tangent+Vector3.forward*.07f,new Color(.16f,.25f,.31f,0));
            }
            Hull(turbine,Vector3.zero,new Vector3(.24f,.24f,.16f),new Color(.32f,.45f,.52f,0));turbineMesh=turbine.Finish("Curved titanium recycler blades and enclosed motor");
            var drone=new SkyPulseMeshBuilder();Hull(drone,Vector3.zero,new Vector3(.31f,.12f,.23f),new Color(.36f,.48f,.56f,0));
            Hull(drone,new Vector3(0,0,-.21f),new Vector3(.10f,.045f,.035f),new Color(.27f,.74f,.88f,1));
            for(int side=-1;side<=1;side+=2)
            {
                Hull(drone,new Vector3(side*.34f,-.015f,.025f),new Vector3(.21f,.045f,.11f),new Color(.21f,.33f,.40f,0));
                Hull(drone,new Vector3(side*.45f,-.025f,-.07f),new Vector3(.07f,.018f,.028f),new Color(.30f,.80f,.94f,1));
            }
            droneMesh=drone.Finish("Streamlined ceramic service craft with optical nacelles");
            material=new Material(Resources.Load<Shader>("SkyPulse/Shaders/FlightArchitecture")){name="Reflected orbital titanium"};
            material.SetColor("_Tint",new Color(.23f,.66f,.82f));material.SetColor("_Haze",new Color(.02f,.07f,.11f));material.SetTexture("_Environment",reflection!=null?reflection:Texture2D.grayTexture);
            for(int i=0;i<6;i++)rails[i]=SkyPulseMeshBuilder.Surface(transform,"Parallax structural rib "+i,railMesh,material,-23+i/2*3).transform;
            for(int i=0;i<2;i++){turbines[i]=SkyPulseMeshBuilder.Surface(transform,"Distant air recycler "+i,turbineMesh,material,-25).transform;drones[i]=SkyPulseMeshBuilder.Surface(transform,"Autonomous maintenance drone "+i,droneMesh,material,-15).transform;}
            SetVisual(0,0);
        }
        static void Arc(SkyPulseMeshBuilder b,Vector3 centre,float radius,float tube,float start,float sweep,Color colour)
        {
            int segments=Mathf.Clamp(Mathf.CeilToInt(sweep*Mathf.Deg2Rad*radius/.15f),16,64);const int sides=12;
            for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
            {
                float a=(start+sweep*i/segments)*Mathf.Deg2Rad,c=(start+sweep*(i+1)/segments)*Mathf.Deg2Rad,u=j*Mathf.PI*2/sides,w=(j+1)*Mathf.PI*2/sides,m=(u+w)*.5f;
                var na=new Vector3(Mathf.Cos(a)*Mathf.Cos(m),Mathf.Sin(a)*Mathf.Cos(m),Mathf.Sin(m));var nc=new Vector3(Mathf.Cos(c)*Mathf.Cos(m),Mathf.Sin(c)*Mathf.Cos(m),Mathf.Sin(m));
                b.SmoothQuad(centre+Ring(a,u,radius,tube),centre+Ring(c,u,radius,tube),centre+Ring(c,w,radius,tube),centre+Ring(a,w,radius,tube),na,nc,colour);
            }
        }
        static Vector3 Ring(float a,float u,float radius,float tube)=>new Vector3(Mathf.Cos(a)*(radius+Mathf.Cos(u)*tube),Mathf.Sin(a)*(radius+Mathf.Cos(u)*tube),Mathf.Sin(u)*tube);
        static void Hull(SkyPulseMeshBuilder b,Vector3 centre,Vector3 radii,Color colour)
        {
            const int segments=28,rings=12;
            for(int i=0;i<segments;i++)for(int j=0;j<rings;j++)
            {
                float a=i*Mathf.PI*2/segments,c=(i+1)*Mathf.PI*2/segments,u=-Mathf.PI*.5f+j*Mathf.PI/rings,w=u+Mathf.PI/rings,m=(u+w)*.5f;
                var na=new Vector3(Mathf.Cos(a)*Mathf.Cos(m)/radii.x,Mathf.Sin(m)/radii.y,Mathf.Sin(a)*Mathf.Cos(m)/radii.z).normalized;
                var nc=new Vector3(Mathf.Cos(c)*Mathf.Cos(m)/radii.x,Mathf.Sin(m)/radii.y,Mathf.Sin(c)*Mathf.Cos(m)/radii.z).normalized;
                b.SmoothQuad(centre+Ellipsoid(a,u,radii),centre+Ellipsoid(c,u,radii),centre+Ellipsoid(c,w,radii),centre+Ellipsoid(a,w,radii),na,nc,colour);
            }
        }
        static Vector3 Ellipsoid(float a,float u,Vector3 r)=>new Vector3(Mathf.Cos(a)*Mathf.Cos(u)*r.x,Mathf.Sin(u)*r.y,Mathf.Sin(a)*Mathf.Cos(u)*r.z);
        public void SetVisual(float distance,float clock)
        {
            for(int i=0;i<rails.Length;i++)
            {
                int layer=i/2;float side=i%2==0?-1:1,scale=.40f+layer*.12f;
                float edge=(width-8)*.5f+2+layer*.6f;
                float x=side*(edge+Mathf.Sin(distance*(.02f+layer*.01f)+i)*.15f);
                rails[i].localPosition=new Vector3(x,0,4-layer);rails[i].localScale=new Vector3(-side*scale,scale,scale);
            }
            for(int i=0;i<2;i++)
            {
                turbines[i].localPosition=new Vector3(Mathf.Repeat((i==0?-1:1)*width*.27f-distance*.018f+width*.5f,width)-width*.5f,i==0?5.1f:-5.6f,6);turbines[i].localRotation=Quaternion.Euler(0,i==0?24:-30,clock*(i==0?7:-5));
                drones[i].localPosition=new Vector3(Mathf.Repeat(clock*(i==0?.32f:-.23f)+i*width*.6f+width*.5f,width)-width*.5f,i==0?4.7f+Mathf.Sin(clock*.36f)*.35f:-6.1f+Mathf.Sin(clock*.3f)*.3f,3);
                drones[i].localRotation=Quaternion.Euler(12,Mathf.Sin(clock*.21f+i)*14,Mathf.Sin(clock*.32f+i)*4);
            }
            material.SetFloat(ClockId,clock);
        }
        void OnDestroy(){if(railMesh!=null)Destroy(railMesh);if(turbineMesh!=null)Destroy(turbineMesh);if(droneMesh!=null)Destroy(droneMesh);if(material!=null)Destroy(material);}
    }
}
