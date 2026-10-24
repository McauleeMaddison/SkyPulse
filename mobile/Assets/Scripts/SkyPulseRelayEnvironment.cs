using UnityEngine;
using System.Collections.Generic;

namespace SkyPulse.Mobile
{
    /// <summary>Pooled, collider-free station hardware in the flight camera.</summary>
    public sealed class SkyPulseRelayEnvironment : MonoBehaviour
    {
        readonly Transform[] turbines=new Transform[2],drones=new Transform[2];
        readonly List<Material> authoredMaterials=new List<Material>(4);
        Mesh turbineMesh,droneMesh,quad;Material material,feed;float width,aspect;
        Transform scene,screen;Camera sceneCamera;SkyPulseOrbitalWorld orbitalWorld;SkyPulseRelayBackgroundEdits authored;Transform[] authoredTargets;RenderTexture image;static int nextScene;
        public Camera WorldCamera=>sceneCamera;public RenderTexture WorldImage=>image;
        static readonly int ClockId=Shader.PropertyToID("_Clock");
        public void Build(float viewWidth,Texture reflection=null)
        {
            width=viewWidth+8;
            scene=new GameObject("Relay layered station scene").transform;scene.position=new Vector3(9000+(nextScene++%32)*100,9000,0);
            var cameraObject=new GameObject("Orbital station perspective camera",typeof(Camera));cameraObject.transform.SetParent(scene,false);cameraObject.transform.localPosition=new Vector3(0,0,-18);
            sceneCamera=cameraObject.GetComponent<Camera>();sceneCamera.enabled=false;sceneCamera.fieldOfView=38;sceneCamera.cullingMask=1<<30;sceneCamera.nearClipPlane=.2f;sceneCamera.farClipPlane=190;sceneCamera.allowMSAA=true;sceneCamera.allowHDR=false;
            sceneCamera.clearFlags=CameraClearFlags.SolidColor;sceneCamera.backgroundColor=new Color(.02f,.07f,.10f);
            quad=new Mesh{name="Station scene presentation quad"};quad.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(-.5f,.5f),new Vector3(.5f,.5f),new Vector3(.5f,-.5f)};quad.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};quad.triangles=new[]{0,1,2,0,2,3};quad.RecalculateBounds();
            var composite=Resources.Load<Shader>("SkyPulse/Shaders/WorldComposite");
            feed=new Material(composite){name="Live orbital station depth feed"};screen=SkyPulseMeshBuilder.Surface(transform,"Perspective station composite",quad,feed,-39).transform;screen.localPosition=new Vector3(0,0,24);
            SetViewport(viewWidth);
            orbitalWorld=new GameObject("Constructed orbital district").AddComponent<SkyPulseOrbitalWorld>();orbitalWorld.Build(scene,reflection);
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
            material=new Material(Resources.Load<Shader>("SkyPulse/Shaders/OrbitalStructure")){name="Reflected orbital titanium"};
            material.SetColor("_Tint",new Color(.23f,.66f,.82f));material.SetColor("_Haze",new Color(.02f,.07f,.11f));material.SetTexture("_Environment",reflection!=null?reflection:Texture2D.grayTexture);
            for(int i=0;i<2;i++){turbines[i]=SkyPulseMeshBuilder.Surface(scene,"Distant air recycler "+i,turbineMesh,material,-25).transform;drones[i]=SkyPulseMeshBuilder.Surface(scene,"Autonomous maintenance drone "+i,droneMesh,material,-15).transform;}
            foreach(var item in scene.GetComponentsInChildren<Transform>(true))item.gameObject.layer=30;
            SetVisual(0,0);
            LoadAuthoredStructures();
            if(authored!=null)SetVisual(0,0);
        }
        void LoadAuthoredStructures()
        {
            authored=Resources.Load<SkyPulseRelayBackgroundEdits>("SkyPulse/BackgroundEdits/relay");
            if(authored==null||authored.Parts==null)return;
            var materials=new Dictionary<Material,Material>();
            authoredTargets=new Transform[authored.Parts.Length];
            for(int i=0;i<authored.Parts.Length;i++)
            {
                var part=authored.Parts[i];if(part==null)continue;
                var target=scene.Find(part.Name);if(target==null)target=orbitalWorld.Find(part.Name);
                if(part.Hidden){if(target!=null)target.gameObject.SetActive(false);continue;}
                if(part.Mesh==null)continue;
                if(target==null&&part.AdditionalStructure)
                {
                    target=SkyPulseMeshBuilder.Surface(scene,part.Name,part.Mesh,material,-22).transform;target.gameObject.layer=30;
                    target.localPosition=part.PositionOffset;target.localRotation=part.RotationOffset;target.localScale=part.ScaleMultiplier;
                }
                if(target==null)continue;
                target.GetComponent<MeshFilter>().sharedMesh=part.Mesh;
                if(part.Material!=null)
                {
                    if(!materials.TryGetValue(part.Material,out var instance)){instance=new Material(part.Material);materials.Add(part.Material,instance);authoredMaterials.Add(instance);}
                    target.GetComponent<MeshRenderer>().sharedMaterial=instance;
                }
                authoredTargets[i]=target;
            }
        }
        void ApplyAuthoredStructures()
        {
            if(authoredTargets==null)return;
            for(int i=0;i<authoredTargets.Length;i++)
            {
                var target=authoredTargets[i];var part=authored.Parts[i];if(target==null||part.AdditionalStructure)continue;
                target.localPosition+=part.PositionOffset;target.localRotation=part.RotationOffset*target.localRotation;
                target.localScale=Vector3.Scale(target.localScale,part.ScaleMultiplier);
            }
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
            orbitalWorld.SetVisual(distance,clock);
            for(int i=0;i<2;i++)
            {
                turbines[i].localPosition=new Vector3(i==0?-6.8f:7.2f,i==0?6.4f:-6.3f,12);turbines[i].localScale=Vector3.one*.55f;turbines[i].localRotation=Quaternion.Euler(0,i==0?24:-30,clock*(i==0?7:-5));
                drones[i].localPosition=new Vector3(Mathf.Repeat(clock*(i==0?.32f:-.23f)+i*width*.6f+width*.5f,width)-width*.5f,i==0?4.7f+Mathf.Sin(clock*.36f)*.35f:-6.1f+Mathf.Sin(clock*.3f)*.3f,3);
                drones[i].localRotation=Quaternion.Euler(12,Mathf.Sin(clock*.21f+i)*14,Mathf.Sin(clock*.32f+i)*4);
            }
            ApplyAuthoredStructures();
            foreach(var authoredMaterial in authoredMaterials)if(authoredMaterial.HasProperty(ClockId))authoredMaterial.SetFloat(ClockId,clock);
            material.SetFloat(ClockId,clock);sceneCamera.transform.localPosition=new Vector3(Mathf.Sin(clock*.12f)*.10f,Mathf.Sin(clock*.10f)*.06f,-18);if(gameObject.activeInHierarchy)sceneCamera.Render();
        }
        public void SetViewport(float viewWidth)
        {
            width=viewWidth+8;float next=Mathf.Max(.3f,viewWidth/18f);if(image!=null&&Mathf.Abs(next-aspect)<.001f)return;aspect=next;
            if(image!=null){image.Release();Destroy(image);}image=new RenderTexture(720,Mathf.Clamp(Mathf.RoundToInt(720/aspect),720,1920),16,RenderTextureFormat.ARGB32){name="Live orbital station",antiAliasing=2};image.Create();
            sceneCamera.targetTexture=image;sceneCamera.aspect=941f/1672;feed.mainTexture=image;screen.localScale=new Vector3(viewWidth+.04f,18.04f,1);
        }
        void OnEnable(){if(scene!=null)scene.gameObject.SetActive(true);}
        void OnDisable(){if(scene!=null)scene.gameObject.SetActive(false);}
        void OnDestroy()
        {
            ReleaseOwned(turbineMesh);ReleaseOwned(droneMesh);ReleaseOwned(quad);
            ReleaseOwned(material);ReleaseOwned(feed);if(scene!=null)ReleaseOwned(scene.gameObject);if(image!=null){image.Release();ReleaseOwned(image);}
            foreach(var owned in authoredMaterials)ReleaseOwned(owned);
        }
        static void ReleaseOwned(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
#if UNITY_EDITOR
        public void ReleaseAuthoringResources(){if(orbitalWorld!=null)orbitalWorld.ReleaseAuthoringResources();OnDestroy();}
#endif
    }
}
