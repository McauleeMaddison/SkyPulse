using UnityEngine;

namespace SkyPulse.Mobile
{
    // A real perspective station replaces the panorama. The original illustration
    // is used only as a material reflection reference, never as a backdrop quad.
    public sealed class SkyPulseOrbitalWorld : MonoBehaviour
    {
        readonly Transform[] parts=new Transform[40];readonly Vector3[] positions=new Vector3[40],scales=new Vector3[40];readonly Quaternion[] rotations=new Quaternion[40];readonly bool[] connected=new bool[40];
        Mesh nearTower,farTower,concourse,bridge,sphere,hub,rib,apron,beacons;Material metal,habitat,sky,planet,rockMoon,iceMoon,apronMaterial,beaconMaterial;int count;Transform planetTransform;
        static readonly int Clock=Shader.PropertyToID("_Clock");
        public int StructureCount=>count;
        public void Build(Transform parent,Texture reflection)
        {
            transform.SetParent(parent,false);nearTower=SkyPulseOrbitalGeometry.Tower(false);farTower=SkyPulseOrbitalGeometry.Tower(true);concourse=SkyPulseOrbitalGeometry.Concourse();bridge=SkyPulseOrbitalGeometry.Bridge();sphere=SkyPulseOrbitalGeometry.Sphere("Orbital planet and enclosing sky volume");hub=SkyPulseOrbitalGeometry.Hub();rib=SkyPulseOrbitalGeometry.OrbitalRib();apron=SkyPulseOrbitalGeometry.Apron();
            metal=new Material(Resources.Load<Shader>("SkyPulse/Shaders/OrbitalStructure")){name="Orbital titanium / cyan conductors and gold fittings"};metal.SetTexture("_Environment",reflection!=null?reflection:Texture2D.grayTexture);metal.SetColor("_Tint",new Color(.20f,.79f,.96f));metal.SetColor("_Haze",new Color(.055f,.075f,.20f));
            habitat=new Material(Resources.Load<Shader>("SkyPulse/Shaders/FlightArchitecture")){name="Transit spine / framed alloy, recessed glazing and optical channels"};habitat.SetFloat("_Architecture",1);habitat.SetFloat("_Design",9);habitat.SetFloat("_BeaconGlow",0);habitat.SetTexture("_Environment",reflection!=null?reflection:Texture2D.grayTexture);habitat.SetColor("_Tint",new Color(.27f,.74f,.96f));habitat.SetColor("_Haze",new Color(.060f,.080f,.22f));
            sky=new Material(Resources.Load<Shader>("SkyPulse/Shaders/OrbitalAtmosphere")){name="Orbital atmospheric depth"};planet=new Material(Resources.Load<Shader>("SkyPulse/Shaders/OrbitalPlanet")){name="Pelagic world / cobalt ocean, continents and layered cloud cover"};
            ConfigureCelestialMaterial(planet,0);
            rockMoon=new Material(planet){name="West moon / copper-grey impact basins and crater rims"};ConfigureCelestialMaterial(rockMoon,1);
            iceMoon=new Material(planet){name="Lower moon / fractured glacial shelves"};ConfigureCelestialMaterial(iceMoon,2);
            beacons=SkyPulseOrbitalGeometry.SkyBeacons();beaconMaterial=new Material(Resources.Load<Shader>("SkyPulse/Shaders/AtmosphereStars")){name="Orbital LED stars / cyan lenses with warm secondary beacons"};beaconMaterial.SetColor("_Tint",new Color(.15f,.79f,1));beaconMaterial.SetColor("_Secondary",new Color(1,.63f,.29f));
            apronMaterial=new Material(metal){name="Angled arrival apron / polished alloy and inset lights"};apronMaterial.SetFloat("_Apron",1);
            var surface=Resources.Load<Texture2D>("SkyPulse/materials/orbital-alloy-v1");
            if(surface!=null)
            {
                foreach(var finish in new[]{metal,habitat}){finish.SetTexture("_SurfaceTex",surface);finish.SetFloat("_SurfaceAmount",.25f);}
                metal.SetFloat("_SurfaceScale",.65f);habitat.SetFloat("_SurfaceScale",.45f);
            }
            Add("Orbital atmosphere",sphere,sky,new Vector3(0,0,-18),Vector3.one*145,Quaternion.identity,-48);
            planetTransform=Add("Distant blue planet",sphere,planet,new Vector3(7,12,85),Vector3.one*15,Quaternion.Euler(0,0,-18),-46);
            Add("West distant moon",sphere,rockMoon,new Vector3(-18,-10,85),Vector3.one*4,Quaternion.Euler(0,17,0),-46);
            Add("Lower distant moon",sphere,iceMoon,new Vector3(-5,-15,92),Vector3.one*3,Quaternion.Euler(0,71,0),-46);
            Add("Orbital LED star field",beacons,beaconMaterial,new Vector3(0,0,100),Vector3.one*1.35f,Quaternion.identity,-44);
            var launchHub=Add("Suspended launch hub",hub,metal,new Vector3(-4,6.1f,3),new Vector3(.90f,1,.90f),Quaternion.Euler(4,8,-9),-33);
            Add("East transit tower",nearTower,habitat,new Vector3(8.4f,-18,25),new Vector3(1.75f,2.3f,1.75f),Quaternion.Euler(0,-18,0),-35);
            Add("West transfer tower",nearTower,habitat,new Vector3(-10,-18,31),new Vector3(1.10f,2.15f,1.10f),Quaternion.Euler(0,16,0),-35);
            for(int i=0;i<5;i++)Add("Distant service spire "+(i+1),farTower,habitat,new Vector3(-17+i*8.5f,-17,55+(i%2)*8),new Vector3(.20f,.70f+(i%3)*.13f,.20f),Quaternion.Euler(0,i*17,0),-40);
            // Bridge endpoints are derived from real landing decks. Their shoes
            // overlap the ring surfaces; no route is positioned independently.
            string[] levels={"upper","middle","lower","arrival"};float[] heights={8.2f,-1.8f,-10.5f,-15f};
            for(int i=0;i<levels.Length;i++)
            {
                var east=new Vector3(8.4f,heights[i],25);var west=new Vector3(-10,heights[i],31);var direction=(east-west).normalized;
                Add("East "+levels[i]+" concourse",concourse,metal,east,new Vector3(.83f,1,.83f),Quaternion.identity,-34);
                Add("West "+levels[i]+" concourse",concourse,metal,west,new Vector3(.62f,1,.62f),Quaternion.identity,-35);
                Connect(char.ToUpperInvariant(levels[i][0])+levels[i].Substring(1)+" transit bridge",west+direction*3.45f,east-direction*4.70f,1.2f);
            }
            var hubExit=transform.InverseTransformPoint(launchHub.TransformPoint(new Vector3(1.7f,0,2.85f)));
            var upperDeck=new Vector3(8.4f,8.2f,25);var approach=upperDeck-hubExit;approach.y=0;
            Connect("Overhead optical gantry",hubExit,upperDeck-approach.normalized*4.70f,1.2f);
            Add("Inner orbital megastructure",rib,metal,new Vector3(14,15,62),new Vector3(28,43,6),Quaternion.Euler(5,-48,-7),-42);
            Add("Outer orbital megastructure",rib,metal,new Vector3(18,12,75),new Vector3(32,50,7),Quaternion.Euler(2,-50,-7),-43);
            Add("Angled arrival apron",apron,apronMaterial,new Vector3(0,-9.4f,4),Vector3.one,Quaternion.Euler(0,0,-14),-31);
            foreach(var item in GetComponentsInChildren<Transform>(true))item.gameObject.layer=30;
        }
        // Shared by runtime and the editable workshop migration so a newly
        // exported station uses the same geological identities and sun.
        public static void ConfigureCelestialMaterial(Material value,int kind)
        {
            value.SetFloat("_Kind",kind);value.SetVector("_LightDirection",new Vector4(-.66f,.58f,-.42f,0));
            // One shared imported map supplies authored geology. Light and
            // drifting clouds remain shader work on the real curved sphere.
            var surface=kind==0?Resources.Load<Texture2D>("SkyPulse/materials/relay-ocean-albedo-v2"):null;
            value.SetTexture("_SurfaceMap",surface);value.SetFloat("_SurfaceMapAmount",surface!=null?1:0);
            if(kind==1)
            {
                value.SetFloat("_Seed",8.93f);value.SetColor("_Deep",new Color(.19f,.10f,.085f));value.SetColor("_Light",new Color(.64f,.43f,.29f));value.SetColor("_Rim",new Color(.13f,.44f,1f));value.SetFloat("_Atmosphere",0);
            }
            else if(kind==2)
            {
                value.SetFloat("_Seed",16.47f);value.SetColor("_Deep",new Color(.045f,.19f,.32f));value.SetColor("_Light",new Color(.58f,.79f,.92f));value.SetColor("_Rim",new Color(.20f,.54f,.72f));value.SetFloat("_Atmosphere",.008f);
            }
            else
            {
                value.SetFloat("_Seed",2.71f);value.SetColor("_Deep",new Color(.012f,.105f,.33f));value.SetColor("_Light",new Color(.38f,.55f,.32f));value.SetColor("_Rim",new Color(.13f,.44f,1f));value.SetFloat("_Atmosphere",.024f);
            }
        }
        void Connect(string name,Vector3 start,Vector3 end,float width)
        {
            var span=end-start;
            Add(name,bridge,metal,(start+end)*.5f,new Vector3(span.magnitude/14f,1,width),Quaternion.FromToRotation(Vector3.right,span.normalized),-34);
        }
        Transform Add(string name,Mesh mesh,Material material,Vector3 at,Vector3 scale,Quaternion rotation,int order)
        {
            var t=SkyPulseMeshBuilder.Surface(transform,name,mesh,material,order).transform;parts[count]=t;positions[count]=at;scales[count]=scale;rotations[count]=rotation;count++;
            connected[count-1]=mesh==nearTower||mesh==concourse||mesh==bridge||mesh==hub;
            t.localPosition=at;t.localScale=scale;t.localRotation=rotation;return t;
        }
        public Transform Find(string name){for(int i=0;i<count;i++)if(parts[i].name==name)return parts[i];return null;}
        public void SetVisual(float distance,float clock)
        {
            float drift=Mathf.Sin(distance*.022f)*.75f;
            for(int i=0;i<count;i++)
            {
                // The station moves as one rigid assembly. Independent depth
                // drift would visibly pull bridges away from their sockets.
                var t=parts[i];float depthWeight=connected[i]?.72f:i<2?0:Mathf.Lerp(1,.12f,Mathf.InverseLerp(8,65,positions[i].z));
                t.localPosition=positions[i]-Vector3.right*(drift*depthWeight);t.localScale=scales[i];t.localRotation=rotations[i];
            }
            planetTransform.localRotation=Quaternion.Euler(0,clock*.10f,-18);metal.SetFloat(Clock,clock);habitat.SetFloat(Clock,clock);sky.SetFloat(Clock,clock);planet.SetFloat(Clock,clock);rockMoon.SetFloat(Clock,clock);iceMoon.SetFloat(Clock,clock);apronMaterial.SetFloat(Clock,clock);beaconMaterial.SetFloat(Clock,clock);
        }
        static void Release(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
#if UNITY_EDITOR
        public void ReleaseAuthoringResources(){OnDestroy();}
#endif
        void OnDestroy(){Release(nearTower);Release(farTower);Release(concourse);Release(bridge);Release(sphere);Release(hub);Release(rib);Release(apron);Release(beacons);Release(metal);Release(habitat);Release(sky);Release(planet);Release(rockMoon);Release(iceMoon);Release(apronMaterial);Release(beaconMaterial);}
    }
}
