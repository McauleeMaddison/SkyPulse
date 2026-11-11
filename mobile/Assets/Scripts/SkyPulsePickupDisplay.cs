using UnityEngine;

namespace SkyPulse.Mobile
{
    /// <summary>Five prebuilt, collider-free power housings. The existing pickup
    /// pool/session owns availability and rewards; this component only presents it.</summary>
    [DisallowMultipleComponent]
    public sealed class SkyPulsePickupDisplay : MonoBehaviour
    {
        public const int KindCount=5;
        readonly Transform[] models=new Transform[KindCount];
        readonly Mesh[] meshes=new Mesh[KindCount*2];
        readonly MeshRenderer[] shells=new MeshRenderer[KindCount],lights=new MeshRenderer[KindCount];
        Material metal,optic;MaterialPropertyBlock metalProperties,lightProperties;
        int selected=-1;
        static readonly int BaseId=Shader.PropertyToID("_BaseColor"),GlowId=Shader.PropertyToID("_GlowColor"),EnergyId=Shader.PropertyToID("_Energy");
        static readonly int ClockId=Shader.PropertyToID("_Clock"),LightId=Shader.PropertyToID("_LightTint");
        public int CurrentKind=>selected;
        public int BuildCount {get;private set;}
        public int MaterialCount=>metal==null?0:2;
        public Mesh ShellMesh(int kind)=>meshes[Mathf.Clamp(kind,0,KindCount-1)*2];
        public Mesh LightMesh(int kind)=>meshes[Mathf.Clamp(kind,0,KindCount-1)*2+1];
        public Transform Model(int kind)=>models[Mathf.Clamp(kind,0,KindCount-1)];
        public MeshRenderer Shell(int kind)=>shells[Mathf.Clamp(kind,0,KindCount-1)];
        public MeshRenderer Light(int kind)=>lights[Mathf.Clamp(kind,0,KindCount-1)];

        public void Build()
        {
            if(BuildCount!=0)return;
            var shader=Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics");
            metal=new Material(shader){name="Pickup satin titanium · shared"};
            metal.SetColor(BaseId,new Color(.24f,.31f,.37f));metal.SetFloat("_OpticFinish",1);metal.SetFloat("_Roughness",.43f);
            optic=new Material(shader){name="Pickup cut optical cores · shared"};
            optic.SetColor(BaseId,new Color(.12f,.31f,.38f));optic.SetFloat("_OpticFinish",1);optic.SetFloat("_Glass",.25f);optic.SetFloat("_Roughness",.22f);optic.SetFloat(EnergyId,.56f);
            metalProperties=new MaterialPropertyBlock();lightProperties=new MaterialPropertyBlock();
            string[] names={"Aegis · shield casting","Crystal Magnet · open horseshoe","Time Pulse · orbital clock","Crystal Echo · twin quartz","Route Lens · optical aperture"};
            for(int i=0;i<KindCount;i++)
            {
                var model=new GameObject(names[i]);model.layer=gameObject.layer;model.transform.SetParent(transform,false);models[i]=model.transform;
                var body=new SkyPulseMeshBuilder();var energy=new SkyPulseMeshBuilder();
                switch(i){case 0:Shield(body,energy);break;case 1:Magnet(body,energy);break;case 2:Clock(body,energy);break;case 3:Echo(body,energy);break;default:Lens(body,energy);break;}
                meshes[i*2]=body.Finish(names[i]+" · closed alloy");meshes[i*2+1]=energy.Finish(names[i]+" · recessed light");
                shells[i]=SkyPulseMeshBuilder.Surface(model.transform,"Machined shell",meshes[i*2],metal,13);
                lights[i]=SkyPulseMeshBuilder.Surface(model.transform,"Optical core",meshes[i*2+1],optic,14);
                shells[i].gameObject.layer=lights[i].gameObject.layer=gameObject.layer;model.SetActive(false);
            }
            BuildCount=1;
        }
        public void Present(int kind,float absoluteTime,bool reducedMotion,Color accent,Color secondary)
        {
            if(BuildCount==0)Build();kind=Mathf.Clamp(kind,0,KindCount-1);
            if(selected!=kind)
            {for(int i=0;i<KindCount;i++)models[i].gameObject.SetActive(i==kind);selected=kind;}
            float clock=float.IsFinite(absoluteTime)&&!reducedMotion?absoluteTime:0;
            // Keep the recognisable face readable throughout the motion: gentle
            // banking reveals real side walls without turning the pickup edge-on.
            models[kind].localRotation=Quaternion.Euler(12+Mathf.Sin(clock*.90f)*5,-18+Mathf.Sin(clock*.60f)*26,Mathf.Sin(clock*.70f)*4);
            models[kind].localPosition=Vector3.zero;models[kind].localScale=Vector3.one;
            metalProperties.Clear();metalProperties.SetColor(LightId,new Color(.81f,.91f,1));metalProperties.SetColor(GlowId,accent);
            shells[kind].SetPropertyBlock(metalProperties);
            lightProperties.Clear();lightProperties.SetColor(BaseId,Color.Lerp(accent,secondary,.22f)*.40f+new Color(0,0,0,.60f));
            lightProperties.SetColor(GlowId,Color.Lerp(accent,secondary,.18f));lightProperties.SetFloat(EnergyId,.56f+Mathf.Sin(clock*1.8f)*.035f);lightProperties.SetFloat(ClockId,clock);
            lights[kind].SetPropertyBlock(lightProperties);
        }
        static void Shield(SkyPulseMeshBuilder body,SkyPulseMeshBuilder energy)
        {
            var outline=new[]{new Vector2(-.33f,.43f),new Vector2(.33f,.43f),new Vector2(.46f,.20f),new Vector2(.31f,-.23f),new Vector2(0,-.51f),new Vector2(-.31f,-.23f),new Vector2(-.46f,.20f)};
            Bevel(body,outline,.12f,Vector3.zero,Quaternion.identity);
            var inner=new Vector2[outline.Length];for(int i=0;i<inner.Length;i++)inner[i]=outline[i]*.66f;
            Bevel(energy,inner,.025f,new Vector3(0,0,-.131f),Quaternion.identity);
            Facet(body,new Vector3(0,.02f,-.17f),.095f,.16f,6);
            Tube(energy,.425f,.012f,38,20,140,Quaternion.identity,new Vector3(0,.03f,-.145f));
        }
        static void Magnet(SkyPulseMeshBuilder body,SkyPulseMeshBuilder energy)
        {
            Tube(body,.345f,.112f,50,-35,250,Quaternion.identity,new Vector3(0,.055f,0));
            Tube(energy,.345f,.116f,6,-35,17,Quaternion.identity,new Vector3(0,.055f,0));
            Tube(energy,.345f,.116f,6,198,17,Quaternion.identity,new Vector3(0,.055f,0));
            Tube(energy,.345f,.022f,40,-14,208,Quaternion.identity,new Vector3(0,.055f,-.11f));
            Facet(energy,new Vector3(0,.04f,-.025f),.085f,.16f,6);
        }
        static void Clock(SkyPulseMeshBuilder body,SkyPulseMeshBuilder energy)
        {
            Tube(body,.49f,.045f,64,0,360,Quaternion.identity,Vector3.zero);
            Tube(body,.365f,.035f,52,0,360,Quaternion.Euler(0,62,0),Vector3.zero);
            Tube(energy,.25f,.026f,48,0,360,Quaternion.Euler(65,0,0),Vector3.zero);
            Facet(energy,Vector3.zero,.115f,.20f,6);
            for(int i=0;i<8;i++)Tube(energy,.49f,.049f,3,i*45+5,9,Quaternion.identity,Vector3.zero);
            var needle=new[]{new Vector2(-.025f,-.08f),new Vector2(.025f,-.08f),new Vector2(.025f,.28f),new Vector2(0,.36f),new Vector2(-.025f,.28f)};
            Bevel(body,needle,.022f,new Vector3(0,0,-.11f),Quaternion.Euler(0,0,-35));
        }
        static void Echo(SkyPulseMeshBuilder body,SkyPulseMeshBuilder energy)
        {
            for(int i=0;i<2;i++)
            {
                var at=new Vector3(i==0?-.16f:.16f,i==0?-.045f:.045f,i==0?-.04f:.04f);
                Facet(energy,at,.17f,.43f,6);
                Tube(body,.178f,.027f,36,0,360,Quaternion.Euler(90,0,0),at+Vector3.down*.045f);
                Facet(body,at,.060f,.28f,4);
            }
        }
        static void Lens(SkyPulseMeshBuilder body,SkyPulseMeshBuilder energy)
        {
            body.Profile(new[]{new Vector2(.315f,-.11f),new Vector2(.475f,-.11f),new Vector2(.515f,-.07f),new Vector2(.515f,.07f),new Vector2(.475f,.11f),new Vector2(.315f,.11f),new Vector2(.29f,.07f),new Vector2(.29f,-.07f),new Vector2(.315f,-.11f)});
            for(int i=0;i<6;i++)Tube(energy,.455f,.015f,8,i*60+8,35,Quaternion.identity,new Vector3(0,0,-.113f));
            for(int i=0;i<4;i++)
            {
                float a=(45+i*90)*Mathf.Deg2Rad;var at=new Vector3(Mathf.Cos(a)*.345f,Mathf.Sin(a)*.345f,-.119f);
                var chip=new[]{new Vector2(-.035f,-.06f),new Vector2(.035f,-.06f),new Vector2(.035f,.06f),new Vector2(-.035f,.06f)};
                Bevel(energy,chip,.013f,at,Quaternion.Euler(0,0,45+i*90));
            }
        }
        static void Facet(SkyPulseMeshBuilder mesh,Vector3 centre,float radius,float halfHeight,int sides)
        {
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                var p=new Vector3(Mathf.Cos(a)*radius,.10f*halfHeight,Mathf.Sin(a)*radius);var q=new Vector3(Mathf.Cos(b)*radius,.10f*halfHeight,Mathf.Sin(b)*radius);
                var l=new Vector3(p.x*.78f,-halfHeight*.48f,p.z*.78f);var r=new Vector3(q.x*.78f,-halfHeight*.48f,q.z*.78f);
                mesh.Face(centre+Vector3.up*halfHeight,centre+q,centre+p,new Color(.88f,.96f,1,1));
                mesh.Quad(centre+p,centre+q,centre+r,centre+l,new Color(.46f,.77f,.91f,1));
                mesh.Face(centre+l,centre+r,centre+Vector3.down*halfHeight,new Color(.25f,.50f,.70f,1));
            }
        }
        static void Bevel(SkyPulseMeshBuilder mesh,Vector2[] outline,float halfDepth,Vector3 centre,Quaternion rotation)
        {
            for(int i=0;i<outline.Length;i++)
            {
                var a=outline[i];var b=outline[(i+1)%outline.Length];Vector3 p=At(a*.90f,-halfDepth),q=At(b*.90f,-halfDepth),r=At(a,-halfDepth*.60f),s=At(b,-halfDepth*.60f),u=At(a,halfDepth*.60f),v=At(b,halfDepth*.60f);
                mesh.Face(At(Vector2.zero,-halfDepth),q,p,Color.white);mesh.Quad(p,q,s,r,new Color(.88f,.94f,1,0));mesh.Quad(r,s,v,u,new Color(.55f,.66f,.77f,0));
                var x=At(a*.90f,halfDepth);var y=At(b*.90f,halfDepth);mesh.Quad(u,v,y,x,new Color(.70f,.80f,.88f,0));mesh.Face(At(Vector2.zero,halfDepth),x,y,Color.white);
            }
            Vector3 At(Vector2 p,float z)=>centre+rotation*new Vector3(p.x,p.y,z);
        }
        static void Tube(SkyPulseMeshBuilder mesh,float radius,float thickness,int segments,float start,float sweep,Quaternion rotation,Vector3 centre)
        {
            const int sides=8;
            for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
            {
                float a=(start+sweep*i/segments)*Mathf.Deg2Rad,b=(start+sweep*(i+1)/segments)*Mathf.Deg2Rad,u=j*Mathf.PI*2/sides,v=(j+1)*Mathf.PI*2/sides;
                mesh.Quad(Point(a,u),Point(b,u),Point(b,v),Point(a,v),new Color(.83f,.92f,1,1));
            }
            if(sweep<359.99f)for(int j=0;j<sides;j++)
            {
                float a=start*Mathf.Deg2Rad,b=(start+sweep)*Mathf.Deg2Rad,u=j*Mathf.PI*2/sides,v=(j+1)*Mathf.PI*2/sides;
                var from=centre+rotation*new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);var to=centre+rotation*new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0);
                mesh.Face(from,Point(a,v),Point(a,u),Color.white);mesh.Face(to,Point(b,u),Point(b,v),Color.white);
            }
            Vector3 Point(float a,float b)=>centre+rotation*new Vector3(Mathf.Cos(a)*(radius+Mathf.Cos(b)*thickness),Mathf.Sin(a)*(radius+Mathf.Cos(b)*thickness),Mathf.Sin(b)*thickness);
        }
        static void Release(Object item)
        {if(item==null)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        void OnDestroy()
        {foreach(var mesh in meshes)Release(mesh);Release(metal);Release(optic);}
    }
}
