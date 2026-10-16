using UnityEngine;

namespace SkyPulse.Mobile
{
    public sealed class SkyPulseRelayEnvironment : MonoBehaviour
    {
        readonly Transform[] rails=new Transform[6],turbines=new Transform[2],drones=new Transform[2];
        Mesh railMesh,turbineMesh,droneMesh;Material material;float width;
        static readonly int ClockId=Shader.PropertyToID("_Clock");
        public void Build(float viewWidth)
        {
            width=viewWidth+8;
            var rail=new SkyPulseMeshBuilder();
            rail.Box(Vector3.zero,new Vector3(.11f,22,.17f),new Color(.08f,.17f,.20f,0));
            rail.Box(new Vector3(.09f,0,-.11f),new Vector3(.022f,22,.02f),new Color(.25f,.8f,1,1));
            for(int j=-4;j<=4;j++)rail.Box(new Vector3(.12f,j*2.4f,0),new Vector3(.33f,.12f,.28f),new Color(.18f,.25f,.28f,0));
            railMesh=rail.Finish("Orbital rib, braces and inset conductor");
            var turbine=new SkyPulseMeshBuilder();turbine.Torus(1.25f,.08f,48,new Color(.12f,.23f,.27f,0));
            turbine.Torus(1.05f,.025f,48,new Color(.35f,.7f,.8f,1));
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Vector3 p=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);var t=new Vector3(-p.y,p.x,0);turbine.Quad(p*.35f,p*.38f+t*.12f,p*1.18f+t*.23f,p*1.12f,new Color(.16f,.27f,.29f,0));}
            turbineMesh=turbine.Finish("Distant rotating station turbine");
            var drone=new SkyPulseMeshBuilder();drone.Box(Vector3.zero,new Vector3(.45f,.18f,.25f),new Color(.32f,.42f,.44f,0));
            for(int side=-1;side<=1;side+=2){drone.Box(new Vector3(side*.32f,0,.02f),new Vector3(.35f,.055f,.12f),new Color(.09f,.19f,.22f,0));drone.Box(new Vector3(side*.49f,-.04f,-.08f),new Vector3(.1f,.06f,.04f),new Color(.3f,.87f,1,1));}
            droneMesh=drone.Finish("Station maintenance drone");material=new Material(Resources.Load<Shader>("SkyPulse/Shaders/RelayTitanium")){name="Station cool-key titanium"};material.SetFloat("_Open",1);material.SetFloat("_Charge",1);material.SetColor("_Accent",new Color(.10f,.30f,.37f));material.SetFloat("_Exposure",.60f);
            for(int i=0;i<6;i++){rails[i]=SkyPulseMeshBuilder.Surface(transform,"Parallax structural rib "+i,railMesh,material,-23+i/2*3).transform;rails[i].localScale=Vector3.one*(.40f+(i/2)*.12f);}
            for(int i=0;i<2;i++){turbines[i]=SkyPulseMeshBuilder.Surface(transform,"Distant air recycler "+i,turbineMesh,material,-25).transform;drones[i]=SkyPulseMeshBuilder.Surface(transform,"Autonomous maintenance drone "+i,droneMesh,material,-15).transform;}
            SetVisual(0,0);
        }
        public void SetVisual(float distance,float clock)
        {
            for(int i=0;i<rails.Length;i++){int layer=i/2;float x=Mathf.Repeat(i%2*width*.5f-distance*(.06f+layer*.09f)+width*.5f,width)-width*.5f;rails[i].localPosition=new Vector3(x,0,4-layer);}
            for(int i=0;i<2;i++)
            {
                turbines[i].localPosition=new Vector3((i==0?-1:1)*width*.27f-distance*.018f,i==0?5.1f:-5.6f,6);turbines[i].localRotation=Quaternion.Euler(0,i==0?24:-30,clock*(i==0?7:-5));
                drones[i].localPosition=new Vector3(Mathf.Repeat(clock*(i==0?.32f:-.23f)+i*width*.6f+width*.5f,width)-width*.5f,i==0?4.7f+Mathf.Sin(clock*.36f)*.35f:-6.1f+Mathf.Sin(clock*.3f)*.3f,3);
                drones[i].localRotation=Quaternion.Euler(12,Mathf.Sin(clock*.21f+i)*14,Mathf.Sin(clock*.32f+i)*4);
            }
            material.SetFloat(ClockId,clock);
        }
        void OnDestroy(){if(railMesh!=null)Destroy(railMesh);if(turbineMesh!=null)Destroy(turbineMesh);if(droneMesh!=null)Destroy(droneMesh);if(material!=null)Destroy(material);}
    }
}
