using UnityEngine;

namespace SkyPulse.Mobile
{
    // Eight fixed renderers behind the occupied districts. All meshes and
    // materials are cached; Present only updates transforms and shader values.
    public sealed class SkyPulseAtmosphereDepth : MonoBehaviour
    {
        readonly Mesh[] horizons=new Mesh[12],stars=new Mesh[12];
        readonly Transform[,] layers=new Transform[2,4];
        readonly Material[,] materials=new Material[2,3];
        readonly int[] themes={-1,-1};
        Mesh cloud;
        static readonly int Clock=Shader.PropertyToID("_Clock"),Visibility=Shader.PropertyToID("_Visibility"),Tint=Shader.PropertyToID("_Tint"),Haze=Shader.PropertyToID("_Haze");
        public void Build(Transform scene)
        {
            transform.SetParent(scene,false);
            for(int world=0;world<12;world++)
            {
                var authored=Resources.Load<Mesh>("SkyPulse/BackgroundEdits/world-"+world.ToString("00")+"-horizon");
                horizons[world]=authored!=null?Instantiate(authored):SkyPulseWorldArchitecture.BuildHorizon(world);
                stars[world]=StarField(world);
            }
            cloud=CloudRibbon();
            for(int bank=0;bank<2;bank++)
            {
                materials[bank,0]=new Material(Resources.Load<Shader>("SkyPulse/Shaders/FlightArchitecture")){name="Distant city bank "+bank};
                materials[bank,0].SetFloat("_Architecture",1);materials[bank,0].SetFloat("_Backdrop",1);materials[bank,0].SetFloat("_BeaconGlow",0);
                materials[bank,1]=new Material(Resources.Load<Shader>("SkyPulse/Shaders/AtmosphereVeil")){name="Depth mist bank "+bank};materials[bank,1].SetVector("_Origin",scene.position);
                materials[bank,2]=new Material(Resources.Load<Shader>("SkyPulse/Shaders/AtmosphereStars")){name="LED sky beacons bank "+bank};
                layers[bank,0]=SkyPulseMeshBuilder.Surface(transform,"Distant skyline "+bank,horizons[0],materials[bank,0],-44).transform;
                layers[bank,1]=SkyPulseMeshBuilder.Surface(transform,"Far cloud layer "+bank,cloud,materials[bank,1],-43).transform;
                layers[bank,2]=SkyPulseMeshBuilder.Surface(transform,"Middle cloud layer "+bank,cloud,materials[bank,1],-42).transform;
                layers[bank,3]=SkyPulseMeshBuilder.Surface(transform,"Atmospheric LED stars "+bank,stars[0],materials[bank,2],-41).transform;
            }
            foreach(var item in GetComponentsInChildren<Transform>(true))item.gameObject.layer=30;
        }
        public void Present(int bank,int world,float distance,float clock,float visibility,Color tint,Color haze)
        {
            world=Mathf.Clamp(world,0,11);bool changed=themes[bank]!=world;themes[bank]=world;
            bool show=visibility>.0001f;
            for(int i=0;i<4;i++)
            {
                var item=layers[bank,i];if(item.gameObject.activeSelf!=show)item.gameObject.SetActive(show);
            }
            if(changed)
            {
                layers[bank,0].GetComponent<MeshFilter>().sharedMesh=horizons[world];layers[bank,3].GetComponent<MeshFilter>().sharedMesh=stars[world];
                materials[bank,0].SetFloat("_Design",world);
            }
            if(!show)return;
            for(int i=0;i<3;i++)
            {
                var material=materials[bank,i];material.SetFloat(Clock,clock);material.SetFloat(Visibility,visibility);material.SetColor(Tint,SkyPulseRouteArchitecture.Accent(world));material.SetColor(Haze,haze);material.SetColor("_Secondary",SkyPulseRouteArchitecture.Secondary(world));
            }
            float drift=Mathf.Sin(distance*.0025f)*3;
            // Keep this layer visibly distant: it supplies small occupied-city
            // glints between the detailed islands, without competing with them.
            layers[bank,0].localPosition=new Vector3(-drift,-4.5f,53);layers[bank,0].localScale=new Vector3(1.05f,.40f,1.15f);layers[bank,0].localRotation=Quaternion.Euler(0,-8,0);
            layers[bank,1].localPosition=new Vector3(-drift*.25f,11.5f,67);layers[bank,1].localScale=new Vector3(75,8,4);
            layers[bank,2].localPosition=new Vector3(-drift*.6f,-1.8f,38);layers[bank,2].localScale=new Vector3(62,7.5f,3);
            layers[bank,3].localPosition=new Vector3(-drift*.18f,0,56);
        }
        static Mesh StarField(int world)
        {
            const int count=9;var positions=new Vector3[count*4];var uv=new Vector2[count*4];var colours=new Color[count*4];var indices=new int[count*6];
            for(int i=0;i<count;i++)
            {
                float seed=Hash(world*107+i*37),x=-17+i*4.1f+(seed-.5f)*2.2f,y=5.4f+Hash(world*47+i*53)*15.5f;
                float radius=.32f+Hash(world*61+i*73)*.22f,z=Hash(world*97+i*23)*10;
                for(int corner=0;corner<4;corner++)
                {
                    int k=i*4+corner;float a=corner==0||corner==3?-1:1,b=corner<2?-1:1;
                    positions[k]=new Vector3(x+a*radius*1.7f,y+b*radius*1.7f,z);uv[k]=new Vector2((a+1)*.5f,(b+1)*.5f);colours[k]=new Color(i%3==0?1:0,seed,1,1);
                }
                int t=i*6,v=i*4;indices[t]=v;indices[t+1]=v+2;indices[t+2]=v+1;indices[t+3]=v;indices[t+4]=v+3;indices[t+5]=v+2;
            }
            var mesh=new Mesh{name="World "+world+" / nine atmospheric LED stars"};mesh.vertices=positions;mesh.uv=uv;mesh.colors=colours;mesh.triangles=indices;mesh.RecalculateBounds();mesh.UploadMeshData(true);return mesh;
        }
        static Mesh CloudRibbon()
        {
            const int segments=20;var vertices=new Vector3[(segments+1)*2];var uv=new Vector2[vertices.Length];var triangles=new int[segments*6];
            for(int i=0;i<=segments;i++)
            {
                float x=i/(float)segments;for(int edge=0;edge<2;edge++){vertices[i*2+edge]=new Vector3(x-.5f,edge-.5f,Mathf.Cos(x*Mathf.PI*2)*.5f);uv[i*2+edge]=new Vector2(x,edge);}
                if(i==segments)continue;int k=i*6,v=i*2;triangles[k]=v;triangles[k+1]=v+1;triangles[k+2]=v+3;triangles[k+3]=v;triangles[k+4]=v+3;triangles[k+5]=v+2;
            }
            var mesh=new Mesh{name="Curved atmospheric depth ribbon"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();mesh.UploadMeshData(true);return mesh;
        }
        static float Hash(int seed){uint x=(uint)seed*747796405u+2891336453u;x=((x>>((int)(x>>28)+4))^x)*277803737u;return ((x>>22)^x)/4294967295f;}
        void OnDestroy()
        {
            foreach(var mesh in horizons)if(mesh!=null)Destroy(mesh);foreach(var mesh in stars)if(mesh!=null)Destroy(mesh);
            foreach(var material in materials)if(material!=null)Destroy(material);if(cloud!=null)Destroy(cloud);
        }
    }
}
