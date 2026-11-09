using UnityEngine;

namespace SkyPulse.Mobile
{
    // District plans, not interchangeable monuments. Everything here is built once
    // into a single mesh per world. The live scene only moves the pooled meshes.
    internal static class SkyPulseWorldArchitecture
    {
        internal static readonly string[] Styles={
            "Neon City / inhabited island district", "Aurora Rise / suspended skyport",
            "Solar Drift / solar research dock", "Midnight Tide / ocean sail quarter",
            "Velvet Dawn / rose citadel and gardens", "Crystal Night / glacier research city",
            "Jade Horizon / terraced garden quarter", "Violet Rain / connected arcology",
            "Eclipse / obsidian crescent fortress", "Cobalt Storm / storm-energy harbour",
            "Amber Skies / mesa citadel and observatory", "Polar Glow / polar habitat port"
        };
#if UNITY_EDITOR
        internal static System.Action<string,object,bool> EditorPartCapture;
        internal static Vector4[][] ExportBeaconLighting(int world)
        {
            var layout=Beacons(world);return new[]{(Vector4[])layout.Points.Clone(),(Vector4[])layout.Colours.Clone()};
        }
#endif
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        static void CapturePart(string name,SkyPulseMeshBuilder builder,bool begin)
        {
#if UNITY_EDITOR
            EditorPartCapture?.Invoke(name,builder,begin);
#endif
        }
        static readonly Color Frame=new Color(.16f,.22f,.28f,0),Edge=new Color(.61f,.69f,.75f,0),Shadow=new Color(.026f,.043f,.063f,0);
        // Mesh hardware and the local light spill share one cached layout, so
        // a beacon's glow cannot drift away from its actual cliff mounting.
        sealed class BeaconLayout
        {
            public readonly Vector4[] Points=new Vector4[5],Colours=new Vector4[5];
            public readonly Vector3[] Normals=new Vector3[5];
            public readonly bool[] Secondary=new bool[5];
            public int Count;
        }
        static readonly BeaconLayout[] beaconLayouts=new BeaconLayout[12];
        static BeaconLayout Beacons(int world)
        {
            world%=12;if(beaconLayouts[world]!=null)return beaconLayouts[world];
            var layout=new BeaconLayout();bool dock=world==2||world==7||world==9,basin=world==3;
            float rx=2.38f,rz=1.38f,depth=2.28f;
            switch(world)
            {
                case 0:rx=2.30f;rz=1.35f;depth=2.35f;break;
                case 1:rx=2.25f;rz=1.30f;depth=2.10f;break;
                case 4:rx=2.35f;rz=1.40f;depth=2.05f;break;
                case 5:rx=2.38f;rz=1.32f;depth=2.30f;break;
                case 6:rx=2.40f;rz=1.42f;depth=2.15f;break;
                case 8:rx=2.35f;rz=1.38f;depth=2.40f;break;
                case 10:rx=2.48f;rz=1.44f;depth=2.20f;break;
            }
            layout.Count=dock||basin?4:5;
            for(int i=0;i<layout.Count;i++)
            {
                Vector3 at,normal;float radius;bool secondary;
                if(dock||basin)
                {
                    at=new Vector3((i-1.5f)*.77f,basin?-.48f-(i%2)*.04f:-.20f,basin?-1.415f:-1.265f);
                    normal=Vector3.back;radius=i==2?.14f:.105f;secondary=i%3==0;
                }
                else
                {
                    float a=-Mathf.PI*.5f+Mathf.Lerp(-.76f,.76f,i/4f),t=i==2?.35f:i%2==0?.30f:.48f;
                    normal=RockNormal(a,t,rx,rz,depth,world);at=Rock(a,t,rx,rz,depth,world)+normal*.035f;
                    radius=i==2?.165f:i%2==0?.14f:.105f;secondary=i==1||i==4;
                }
                layout.Points[i]=new Vector4(at.x,at.y,at.z,radius);layout.Normals[i]=normal;layout.Secondary[i]=secondary;
                var colour=secondary?SkyPulseRouteArchitecture.Secondary(world):Light(world);layout.Colours[i]=new Vector4(colour.r,colour.g,colour.b,1);
            }
            return beaconLayouts[world]=layout;
        }
        internal static void ConfigureBeaconLighting(Material material,int world)
        {
            var layout=Beacons(world);
            material.SetVectorArray("_BeaconPoints",layout.Points);material.SetVectorArray("_BeaconColours",layout.Colours);
        }
        static void BeaconStars(SkyPulseMeshBuilder b,int world)
        {
            var layout=Beacons(world);
            for(int i=0;i<layout.Count;i++)
            {
                var p=layout.Points[i];NeonStar(b,new Vector3(p.x,p.y,p.z),layout.Normals[i],p.w,world,layout.Secondary[i]);
            }
        }
        static Color Glass(int world)
        {
            world%=12;
            return world==2||world==10?new Color(.18f,.14f,.10f,.34f):world==4?new Color(.17f,.11f,.15f,.34f):world==6?new Color(.07f,.16f,.12f,.34f):world==8?new Color(.10f,.085f,.13f,.34f):world==11?new Color(.13f,.19f,.23f,.34f):new Color(.07f,.13f,.19f,.34f);
        }
        static Color Light(int world)
        {
            var colour=SkyPulseRouteArchitecture.Accent(world%12);colour.a=1;return colour;
        }
        static Color WindowGlass(int world,int building,int row,int side)
        {
            // Stable, two-floor clusters look occupied without blinking at
            // random. Alpha .48 identifies lit glazing in the shared shader;
            // the RGB is the same primary/secondary palette as the pipe LEDs.
            int seed=world*947+building*173+side*43+(row/2)*97;
            if(Hash(seed)<.70f)return Glass(world);
            var colour=Hash(seed+side*239+row*29)>.72f?SkyPulseRouteArchitecture.Secondary(world%12):SkyPulseRouteArchitecture.Accent(world%12);
            colour.a=.48f;return colour;
        }
        static Color Shell(int world)
        {
            world%=12;
            // Solid facade palettes distinguish each civic family before any
            // emissive window or post effect. These are not twelve grey shells.
            switch(world)
            {
                case 0:return new Color(.32f,.43f,.54f,0); // midnight ceramic / titanium
                case 1:return new Color(.70f,.77f,.76f,0); // pearl skyport
                case 2:return new Color(.49f,.42f,.30f,0); // champagne research alloy
                case 3:return new Color(.30f,.48f,.53f,0); // coastal blue stone
                case 4:return new Color(.62f,.47f,.43f,.12f); // rose limestone
                case 5:return new Color(.56f,.67f,.76f,0); // glacial ceramic
                case 6:return new Color(.28f,.41f,.33f,.12f); // jade garden stone
                case 7:return new Color(.39f,.34f,.55f,0); // violet architectural alloy
                case 8:return new Color(.20f,.17f,.29f,.12f); // obsidian fortress
                case 9:return new Color(.32f,.46f,.64f,0); // cobalt harbour steel
                case 10:return new Color(.57f,.38f,.24f,.12f); // warm mesa sandstone
                default:return new Color(.64f,.70f,.73f,0); // polar ivory habitat
            }
        }
        public static Mesh Build(int world,int variant=0)
        {
            world=Mathf.Clamp(world,0,11)+Mathf.Clamp(variant,0,1)*12;var b=new SkyPulseMeshBuilder();var shell=Shell(world);var light=Light(world);
            switch(world%12)
            {
                case 0:
                    NeonConnectedCity(b,world);break;
                case 1:
                    Landform(b,2.25f,1.30f,2.10f,1);Promenade(b,2.13f,1.18f,world);
                    Tower(b,new Vector3(-1.06f,.29f,.20f),.95f,.88f,5.65f,.58f,1,world);
                    Tower(b,new Vector3(1.04f,.29f,.13f),1.06f,.91f,4.20f,-.48f,1,world);
                    Skyway(b,new Vector3(-.91f,2.5f,.03f),new Vector3(.79f,2.5f,.03f),.29f,world);
                    Arch(b,new Vector3(0,2.53f,.04f),1.10f,.73f,.055f,0,180,Quaternion.identity,Edge);
                    Pavilion(b,new Vector3(0,.30f,-.68f),1.40f,.60f,.65f,world);break;
                case 2:
                    Dock(b,world);Tower(b,new Vector3(.12f,.29f,.20f),1.18f,.97f,4.35f,-.10f,2,world);
                    Tower(b,new Vector3(-1.24f,.29f,.10f),.72f,.70f,2.3f,.12f,1,world);
                    for(int side=-1;side<=1;side+=2)
                    {
                        var p=new Vector3(side*1.70f,.35f,-.08f);SolarWing(b,p,world);
                        Skyway(b,new Vector3(side*.66f,.46f,.1f),p,.24f,world);
                    }
                    Arch(b,new Vector3(.10f,3.08f,.24f),.98f,.98f,.045f,-30,290,Quaternion.Euler(10,-25,0),Edge);break;
                case 3:
                    Basin(b,2.44f,1.40f,world);Promenade(b,2.16f,1.16f,world);
                    Tower(b,new Vector3(-1.28f,.29f,.30f),.90f,.82f,3.8f,.66f,1,world);
                    Tower(b,new Vector3(-.13f,.29f,.38f),1.28f,.93f,5.95f,.80f,1,world);
                    Tower(b,new Vector3(1.23f,.29f,.32f),.88f,.80f,3.15f,.42f,1,world);
                    Pavilion(b,new Vector3(.16f,.30f,-.76f),2.70f,.58f,.62f,world);
                    Arch(b,new Vector3(0,.45f,-.83f),1.91f,.60f,.052f,0,180,Quaternion.identity,Edge);break;
                case 4:
                    Landform(b,2.35f,1.40f,2.05f,4);Promenade(b,2.16f,1.22f,world);
                    Tower(b,new Vector3(-.15f,.29f,.45f),1.55f,1.20f,4.60f,0,2,world);
                    Dome(b,new Vector3(-.15f,4.88f,.45f),.64f,.47f,.50f,new Color(.37f,.24f,.27f,0));
                    Turret(b,new Vector3(-1.48f,.29f,.26f),.45f,3.58f,world);
                    Turret(b,new Vector3(1.43f,.29f,.30f),.47f,3.94f,world);
                    Gatehouse(b,new Vector3(0,.29f,-.88f),3.05f,1.52f,world);
                    Skyway(b,new Vector3(-1.40f,2.10f,.18f),new Vector3(1.38f,2.10f,.18f),.24f,world);
                    Garden(b,new Vector3(-1.52f,.30f,-.61f),.27f,world);Garden(b,new Vector3(1.50f,.30f,-.58f),.25f,world);break;
                case 5:
                    Landform(b,2.38f,1.32f,2.30f,5);Promenade(b,2.13f,1.18f,world);
                    Tower(b,new Vector3(-1.21f,.29f,.18f),.86f,.88f,3.65f,-.30f,3,world);
                    Tower(b,new Vector3(-.04f,.29f,.32f),1.26f,1.00f,6.15f,.20f,3,world);
                    Tower(b,new Vector3(1.27f,.29f,.20f),.82f,.81f,4.35f,.37f,3,world);
                    Pavilion(b,new Vector3(.10f,.30f,-.70f),2.40f,.62f,.58f,world);break;
                case 6:
                    Landform(b,2.4f,1.42f,2.15f,6);Promenade(b,2.2f,1.23f,world);
                    for(int tier=0;tier<4;tier++)
                    {
                        float y=.3f+tier*.86f,w=3.50f-tier*.60f,d=1.68f-tier*.20f;
                        Pavilion(b,new Vector3(-.20f,y,.20f),w,d,.69f,world);
                        RoundedSlab(b,new Vector3(-.20f,y+.68f,.20f),w+.15f,d+.14f,.09f,new Color(.21f,.31f,.24f,0));
                        Garden(b,new Vector3(-.20f,y+.75f,-d*.37f),w*.36f,world);
                    }
                    Tower(b,new Vector3(.12f,3.74f,.35f),.68f,.64f,1.70f,.13f,1,world);break;
                case 7:
                    Dock(b,world);Tower(b,new Vector3(-1.22f,.29f,.22f),.96f,.96f,5.75f,-.22f,0,world);
                    Tower(b,new Vector3(.20f,.29f,.47f),1.27f,1.12f,4.25f,.21f,2,world);
                    Tower(b,new Vector3(1.43f,.29f,.20f),.95f,.91f,6.35f,.32f,0,world);
                    Skyway(b,new Vector3(-1.08f,3.17f,-.08f),new Vector3(1.53f,3.51f,-.08f),.44f,world);
                    Skyway(b,new Vector3(-1.07f,1.42f,-.05f),new Vector3(.25f,1.42f,-.05f),.28f,world);
                    Pavilion(b,new Vector3(-.10f,.30f,-.78f),2.66f,.56f,.76f,world);break;
                case 8:
                    Landform(b,2.35f,1.38f,2.4f,8);Promenade(b,2.16f,1.20f,world);
                    Tower(b,new Vector3(.22f,.29f,.35f),1.55f,1.12f,5.48f,.14f,3,world);
                    Turret(b,new Vector3(-1.36f,.29f,.05f),.46f,3.68f,world);
                    Turret(b,new Vector3(1.38f,.29f,.06f),.47f,4.10f,world);
                    Gatehouse(b,new Vector3(0,.29f,-.85f),3.00f,1.45f,world);
                    Arch(b,new Vector3(.10f,3.42f,.42f),1.55f,1.60f,.080f,-36,284,Quaternion.Euler(0,-15,0),shell);
                    Arch(b,new Vector3(.10f,3.42f,.34f),1.47f,1.52f,.012f,-36,284,Quaternion.Euler(0,-15,0),light);break;
                case 9:
                    Dock(b,world);Tower(b,new Vector3(-1.30f,.29f,.22f),.80f,.83f,5.10f,-.14f,2,world);
                    Tower(b,new Vector3(1.28f,.29f,.26f),.89f,.85f,5.50f,.20f,2,world);
                    Pavilion(b,new Vector3(0,.30f,-.38f),1.75f,1.38f,1.35f,world);
                    for(int i=0;i<3;i++)Arch(b,new Vector3(0,2.9f,.16f+i*.12f),.87f,1.17f,.07f,0,360,Quaternion.Euler(0,-12,0),i==1?light:Edge);
                    for(int side=-1;side<=1;side+=2)for(int level=0;level<3;level++)Skyway(b,new Vector3(side*.52f,1.65f+level*.83f,.22f),new Vector3(side*1.25f,2.12f+level*.83f,.22f),.12f,world);break;
                case 10:
                    Landform(b,2.48f,1.44f,2.20f,10);Promenade(b,2.24f,1.25f,world);
                    Tower(b,new Vector3(-.43f,.29f,.40f),1.44f,1.08f,3.35f,.08f,2,world);
                    Dome(b,new Vector3(-.41f,3.59f,.40f),.67f,.57f,.51f,shell);
                    Turret(b,new Vector3(1.28f,.29f,.22f),.49f,4.55f,world);
                    Gatehouse(b,new Vector3(-.06f,.30f,-.82f),3.20f,1.25f,world);
                    Dome(b,new Vector3(-1.56f,1.04f,-.61f),.44f,.33f,.37f,shell);
                    for(int i=0;i<7;i++)RoundedSlab(b,new Vector3(-1.15f+i*.35f,.32f,-1.08f),.07f,.14f,.64f,Edge);break;
                default:
                    Landform(b,2.38f,1.38f,2.28f,11);Promenade(b,2.18f,1.22f,world);
                    Tower(b,new Vector3(-1.24f,.29f,.25f),.90f,.89f,3.38f,.12f,2,world);
                    Tower(b,new Vector3(1.23f,.29f,.30f),.99f,.95f,4.10f,-.15f,1,world);
                    Pavilion(b,new Vector3(0,.30f,-.26f),1.85f,1.49f,1.25f,world);
                    for(int i=0;i<5;i++)Arch(b,new Vector3(.05f,.31f,-.55f+i*.23f),1.04f,4.80f-i*.08f,.060f,0,180,Quaternion.identity,shell);
                    Skyway(b,new Vector3(-1.14f,1.82f,.05f),new Vector3(1.15f,1.82f,.05f),.26f,world);break;
            }
            return b.Finish(Styles[world%12]+(variant==0?" / civic district":" / residential district"));
        }
        // Neon City is one connected piece of inhabited architecture. This
        // candidate keeps the eight original editable-part identities while
        // replacing the repeated leaning spires with a civic podium and three
        // solid, chamfered setback towers. Other worlds keep their own plans.
        static void NeonConnectedCity(SkyPulseMeshBuilder b,int world)
        {
            Landform(b,2.30f,1.35f,2.35f,0);
            Promenade(b,2.15f,1.20f,world);
            bool residential=world>=12;
            NeonTower(b,new Vector3(-.20f,.29f,.25f),1.19f,1.02f,residential?4.15f:4.95f,0,world);
            NeonTower(b,new Vector3(-1.20f,.29f,.10f),.94f,.91f,residential?3.67f:3.12f,1,world);
            NeonTower(b,new Vector3(1.10f,.29f,.30f),1.02f,.94f,residential?3.18f:3.89f,2,world);
            NeonCivicPodium(b,world);
            CapturePart("Sky bridge",b,true);
            foreach(float x in new[]{-1.20f,-.20f,1.10f})
            {
                RoundedSlab(b,new Vector3(x,2.165f,-.22f),.40f,.43f,.065f,Color.Lerp(Frame,Shadow,.55f));
                for(int side=-1;side<=1;side+=2)Beam(b,new Vector3(x+side*.17f,2.13f,-.17f),new Vector3(x+side*.17f,2.06f,.02f),.025f,.035f,Frame);
            }
            Skyway(b,new Vector3(-1.20f,2.22f,-.22f),new Vector3(-.20f,2.22f,-.22f),.27f,world);
            Skyway(b,new Vector3(-.20f,2.22f,-.22f),new Vector3(1.10f,2.22f,-.22f),.27f,world);
            CapturePart("Sky bridge",b,false);
            NeonRoofGarden(b,world);
        }
        static Vector2[] NeonFootprint(float width,float depth,float inset)
        {
            float x=width*.5f,z=depth*.5f,c=Mathf.Min(width,depth)*inset;
            return new[]{new Vector2(-x+c,-z),new Vector2(x-c,-z),new Vector2(x,-z+c),new Vector2(x,z-c),new Vector2(x-c,z),new Vector2(-x+c,z),new Vector2(-x,z-c),new Vector2(-x,-z+c)};
        }
        static float NeonSetback(float t,int identity)
        {
            float shoulder=identity==0?.69f:identity==1?.77f:.62f;
            if(t<.065f)return Mathf.Lerp(1.10f,1,t/.065f);
            if(t<shoulder)return 1;
            if(t<shoulder+.045f)return Mathf.Lerp(1,.81f,(t-shoulder)/.045f);
            if(t<.925f)return .81f;
            return Mathf.Lerp(.81f,.73f,(t-.925f)/.075f);
        }
        static Color NeonWindow(int world,int tower,int floor,int bay)
        {
            int seed=world*613+tower*157+(floor/2)*71+bay*29;
            if(Hash(seed)<.74f)return Glass(world);
            var colour=(bay+floor+tower)%5==0?SkyPulseRouteArchitecture.Secondary(world%12):Light(world);
            colour=Color.Lerp(colour,new Color(.78f,.90f,1),.16f);colour.a=.48f;return colour;
        }
        static void NeonTower(SkyPulseMeshBuilder b,Vector3 at,float width,float depth,float height,int identity,int world)
        {
            // The city shares its craft vocabulary with the other districts,
            // but keeps an asymmetric triplet of setbacks and occupied loggias.
            CapturePart("Building",b,true);
            Tower(b,at,width,depth,height,identity==0?.075f:identity==1?-.10f:.14f,identity==1?2:0,world);
            float terrace=identity==0?.62f:identity==1?.70f:.55f;
            OccupiedTerrace(b,at+new Vector3((identity-1)*.06f,height*terrace,-depth*.48f),width*.91f,.28f,world,identity==0);
            // Broad angled shoulders give the central civic crown a recognisable
            // silhouette instead of three identical rooftop service boxes.
            if(identity==0)
            {
                var roof=at+new Vector3(.075f,height,0);
                for(int side=-1;side<=1;side+=2)
                {
                    var foot=roof+new Vector3(side*width*.39f,.02f,depth*.05f);
                    var crest=roof+new Vector3(side*width*.17f,.44f,-depth*.12f);
                    Beam(b,foot,crest,.065f,.075f,Shell(world));
                    Beam(b,foot+Vector3.back*.044f,crest+Vector3.back*.044f,.013f,.014f,Light(world));
                }
                Beam(b,roof+new Vector3(-width*.17f,.44f,-depth*.12f),roof+new Vector3(width*.17f,.44f,-depth*.12f),.050f,.045f,Edge);
            }
            CapturePart("Building",b,false);
        }
        static void NeonCivicPodium(SkyPulseMeshBuilder b,int world)
        {
            CapturePart("Building",b,true);
            var at=new Vector3(-.02f,.29f,-.60f);float width=3.53f,depth=.88f,height=1.06f;
            var wall=Color.Lerp(Shell(world),Edge,.18f);
            // A single inhabited podium physically joins all three tower feet.
            // The podium envelope has a real open-front window wall. Dark
            // interior beams and deep sill seats sit behind its separate piers;
            // no black floating plane is used to pretend there is a shadow.
            Beam(b,at+new Vector3(-width*.50f,height*.49f,depth*.45f),at+new Vector3(width*.50f,height*.49f,depth*.45f),.13f,height*.96f,wall);
            for(int side=-1;side<=1;side+=2)Beam(b,at+new Vector3(side*width*.485f,height*.49f,-depth*.43f),at+new Vector3(side*width*.485f,height*.49f,depth*.45f),.12f,height*.96f,wall);
            RoundedSlab(b,at+Vector3.up*.07f,width*1.025f,depth*1.04f,.14f,Frame);
            RoundedSlab(b,at+Vector3.up*(height-.058f),width*1.015f,depth*.99f,.065f,Color.Lerp(Frame,Shadow,.60f));
            RoundedSlab(b,at+Vector3.up*(height+.015f),width*1.045f,depth*1.10f,.10f,wall);
            for(int bay=0;bay<7;bay++)
            {
                float left=(bay-3.5f)*width/7,right=(bay-2.5f)*width/7;
                var lowL=at+new Vector3(left,.10f,-depth*.50f);var lowR=at+new Vector3(right,.10f,-depth*.50f);
                var highL=at+new Vector3(left,height-.095f,-depth*.50f);var highR=at+new Vector3(right,height-.095f,-depth*.50f);
                WindowBay(b,Vector3.zero,lowL,lowR,highR,highL,Vector3.back,Vector3.back,wall,NeonWindow(world,3,bay,0));
                Beam(b,lowL+new Vector3(.022f,.05f,.15f),highL+new Vector3(.022f,-.04f,.15f),.045f,.065f,Color.Lerp(Frame,Shadow,.55f));
            }
            // The central arcade has a recessed portal, canopy and steps aligned
            // to the civic walking surface; every detail has a structural seat.
            var entry=at+new Vector3(.03f,0,-depth*.50f);
            RoundedSlab(b,entry+new Vector3(0,.70f,-.17f),.74f,.40f,.065f,wall);
            for(int side=-1;side<=1;side+=2)Beam(b,entry+new Vector3(side*.29f,.04f,-.24f),entry+new Vector3(side*.29f,.68f,-.24f),.045f,.045f,Edge);
            for(int step=0;step<3;step++)RoundedSlab(b,entry+new Vector3(0,-.018f-step*.026f,-.14f-step*.10f),.80f+step*.10f,.30f,.044f,Frame);
            Beam(b,entry+new Vector3(-.33f,.675f,-.36f),entry+new Vector3(.33f,.675f,-.36f),.010f,.012f,Light(world));
            // Low parapet frames the occupied roof garden without hiding it.
            Beam(b,at+new Vector3(-width*.48f,height+.18f,-depth*.48f),at+new Vector3(width*.48f,height+.18f,-depth*.48f),.018f,.050f,Glass(world));
            for(int post=-3;post<=3;post++)Beam(b,at+new Vector3(post*.52f,height+.055f,-depth*.48f),at+new Vector3(post*.52f,height+.23f,-depth*.48f),.016f,.016f,Edge);
            CapturePart("Building",b,false);
        }
        static void NeonRoofGarden(SkyPulseMeshBuilder b,int world)
        {
            CapturePart("Garden terrace",b,true);
            for(int bed=0;bed<3;bed++)
            {
                var at=new Vector3(-1.18f+bed*.70f,1.44f,-.66f);
                RoundedSlab(b,at,.58f,.27f,.11f,Frame);
                RoundedSlab(b,at+Vector3.up*.055f,.52f,.21f,.015f,new Color(.13f,.24f,.18f,0));
                for(int leaf=0;leaf<4;leaf++)
                {
                    var root=at+new Vector3((leaf-1.5f)*.115f,.065f,0);
                    Beam(b,root,root+Vector3.up*.15f,.012f,.012f,new Color(.18f,.29f,.20f,0));
                    for(int side=-1;side<=1;side+=2)
                    {
                        var tip=root+new Vector3(side*.070f,.19f,.018f);
                        b.Face(root+new Vector3(0,.09f,-.028f),tip,root+new Vector3(side*.030f,.10f,.044f),new Color(.19f,.35f,.25f,.20f));
                    }
                }
            }
            CapturePart("Garden terrace",b,false);
        }
        // Separate low-detail skyline behind the inhabited islands. Reuses the
        // same tower profiles and normals, without drawing full window meshes
        // at a distance where they would be subpixel geometry.
        internal static Mesh BuildHorizon(int world)
        {
            world=Mathf.Clamp(world,0,11);var b=new SkyPulseMeshBuilder();
            for(int i=0;i<7;i++)
            {
                float x=(i-3)*1.75f,z=.40f+Hash(world*61+i*23)*1.40f;
                float height=2.8f+Hash(world*173+i*31)*2.6f;
                int form=world==4||world==10?2:world==5||world==8?3:world==1||world==3?1:0;
                if(world==6)height*=.72f;
                DistantTower(b,new Vector3(x,.22f,z),.85f+(i%3)*.15f,.80f,height,world==1||world==3?(i%2==0?.45f:-.25f):.12f,form,world);
                if(i%2==0)RoundedSlab(b,new Vector3(x,.05f,z),1.9f,1.65f,.22f,Frame);
                if(i>0&&(world==0||world==1||world==7||world==9))
                    Beam(b,new Vector3(x-1.75f,1.35f,z),new Vector3(x,1.35f,z),.17f,.10f,Edge);
            }
            if(world==4||world==8||world==10)
                Arch(b,new Vector3(0,.16f,.20f),5.4f,1.25f,.15f,0,180,Quaternion.identity,Shell(world));
            if(world==2||world==9)
                for(int i=-2;i<=2;i++)SolarWing(b,new Vector3(i*2.6f,.12f,-1.20f),world,true);
            return b.Finish(Styles[world]+" / distant skyline");
        }
        static void DistantTower(SkyPulseMeshBuilder b,Vector3 at,float width,float depth,float height,float lean,int form,int world)
        {
            CapturePart("Distant building",b,true);
            const int sides=12,rows=5;
            for(int row=0;row<rows;row++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1f)*Mathf.PI*2/sides,y=row/(float)rows,u=(row+1f)/rows;
                var p=TowerPoint(a,y,width,depth,height,lean,form);var q=TowerPoint(c,y,width,depth,height,lean,form);
                var r=TowerPoint(c,u,width,depth,height,lean,form);var t=TowerPoint(a,u,width,depth,height,lean,form);
                var n=TowerNormal(a,y,width,depth,height,lean,form);var m=TowerNormal(c,y,width,depth,height,lean,form);
                // The fixed gameplay camera sees the front arc. Spend distant
                // window geometry there so the horizon reads as occupied.
                if(row>0&&side>=7&&side<=10)
                {
                    var colour=side%2==0?SkyPulseRouteArchitecture.Secondary(world):Light(world);colour.a=.48f;
                    WindowBay(b,at,p,q,r,t,n,m,Shell(world),colour);
                }
                else b.SmoothQuad(at+p,at+q,at+r,at+t,n,m,Shell(world));
            }
            var crown=at+new Vector3(lean,height,0);RoundedSlab(b,crown,width*.66f,depth*.70f,.10f,Edge);
            Beam(b,crown,crown+Vector3.up*.23f,.018f,.018f,Light(world));
            if(world==4||world==10)Dome(b,crown+Vector3.up*.04f,width*.30f,.30f,depth*.30f,Shell(world),12,4);
            CapturePart("Distant building",b,false);
        }
        // Solid framed towers with recessed window bays. Profile changes are setbacks and
        // rounded shoulders. Floor sills follow the facade instead of intersecting
        // it as circular discs, which made the previous buildings look like toys.
        static void Tower(SkyPulseMeshBuilder b,Vector3 at,float width,float depth,float height,float lean,int form,int world)
        {
            CapturePart("Building",b,true);
            if(world>=12&&!((world%12==4||world%12==10)&&form==2))
            {
                float seed=Hash(world*171+Mathf.RoundToInt(at.x*31+at.z*53));
                height*=.75f+seed*.36f;width*=1.18f-seed*.23f;depth*=.91f+seed*.16f;
                lean=-lean*.73f;at.x+=Mathf.Lerp(-.10f,.10f,seed);
            }
            int theme=world%12,sides=form==1||form==4?16:8;
            int floors=Mathf.Max(4,Mathf.CeilToInt(height/.40f));var shell=Shell(world);
            // Eight explicitly chamfered planes give solid towers broad walls,
            // while sail and round citadel families retain curved profiles.
            var footprint=NeonFootprint(width,depth,form==3?.16f:.075f);
            for(int row=0;row<floors;row++)
            {
                float y=row/(float)floors,c=(row+1f)/floors;
                for(int side=0;side<sides;side++)
                {
                    float a=form==1||form==4?side*Mathf.PI*2/sides:Mathf.Atan2(footprint[side].y,footprint[side].x);
                    int next=(side+1)%sides;
                    float d=form==1||form==4?(side+1f)*Mathf.PI*2/sides:Mathf.Atan2(footprint[next].y,footprint[next].x);
                    var p=TowerPoint(a,y,width,depth,height,lean,form);var q=TowerPoint(d,y,width,depth,height,lean,form);
                    var r=TowerPoint(d,c,width,depth,height,lean,form);var ss=TowerPoint(a,c,width,depth,height,lean,form);
                    var na=form==1||form==4?TowerNormal(a,y,width,depth,height,lean,form):Vector3.Cross(ss-p,q-p).normalized;
                    var nb=form==1||form==4?TowerNormal(d,y,width,depth,height,lean,form):na;
                    // Curved towers use sixteen narrow facade segments. The
                    // eight-sided wall threshold suppressed all their windows,
                    // leaving the sail towers as blank pale slabs.
                    float bayThreshold=form==1||form==4?.12f:.35f;
                    bool wide=(q-p).magnitude>Mathf.Min(width,depth)*bayThreshold;
                    bool window=row>0&&row<floors-1&&wide;
                    if(window)
                    {
                        int bays=(q-p).magnitude>.62f?2:1;
                        for(int bay=0;bay<bays;bay++)
                        {
                            float l=bay/(float)bays,u=(bay+1f)/bays;
                            var glass=WindowGlass(world,Mathf.RoundToInt(at.x*127+at.z*61),row,bay+side*3);
                            // Occupied rooms are sparse. Most of a facade reads
                            // as cool reflective glass behind pale load-bearing ribs.
                            WindowBay(b,at,Vector3.Lerp(p,q,l),Vector3.Lerp(p,q,u),Vector3.Lerp(ss,r,u),Vector3.Lerp(ss,r,l),na,nb,shell,glass);
                        }
                    }
                    else b.SmoothQuad(at+p,at+q,at+r,at+ss,na,nb,wide?shell:Color.Lerp(shell,Edge,.36f));
                    // Every third floor has a real projecting spandrel and dark
                    // underside. Intermediate floors keep one clean curtain wall.
                    if(row>0&&row%3==0&&wide)
                    {
                        var outA=na*.018f;var outB=nb*.018f;
                        b.SmoothQuad(at+p+outA,at+q+outB,at+q+outB+Vector3.up*.030f,at+p+outA+Vector3.up*.030f,na,nb,Frame);
                        b.SmoothQuad(at+p+outA+Vector3.up*.030f,at+q+outB+Vector3.up*.030f,at+q+Vector3.up*.030f,at+p+Vector3.up*.030f,Vector3.up,Vector3.up,Edge);
                    }
                }
            }
            // The envelope has closed horizontal seats at the foundation and
            // crown; chamfered corner columns run the full structural height.
            float cap=.985f;var centre=at+new Vector3(lean*cap*cap,height*cap,0);
            RoundedSlab(b,centre,Shape(cap,form)*width,Shape(cap,form)*depth,.075f,shell);
            RoundedSlab(b,at+Vector3.up*.045f,width*1.10f,depth*1.10f,.10f,Shadow);
            for(int corner=0;corner<4;corner++)
            {
                float angle=Mathf.PI*.25f+corner*Mathf.PI*.5f;
                for(int section=0;section<4;section++)
                {
                    float y=.055f+section*.23f,c=Mathf.Min(.985f,y+.23f);
                    var p=at+TowerPoint(angle,y,width*1.016f,depth*1.016f,height,lean,form);
                    var q=at+TowerPoint(angle,c,width*1.016f,depth*1.016f,height,lean,form);
                    Beam(b,p,q,form==4?.048f:.035f,form==4?.055f:.040f,Color.Lerp(shell,Edge,.46f));
                }
            }
            // Distinct occupied ledges complement the district's silhouette.
            // Technical towers have screened plant; gardens have shaded loggias;
            // citadels retain their buttressed masonry and lantern crowns.
            FacadeStructure(b,at,width,depth,height,lean,form,world);
            Entrance(b,at,width,depth,height,world);
            RoofDetails(b,centre,Shape(cap,form)*width,Shape(cap,form)*depth,world);
            ArchitecturalCrown(b,centre,Shape(cap,form)*width,Shape(cap,form)*depth,form,world);
            ServiceDetail(b,at,width,depth,height,lean,form,world);
            if(height>3&&form!=4)
            {
                float y=theme==1||theme==3?.34f:theme==5||theme==11?.64f:.48f;
                var face=at+TowerPoint(-Mathf.PI*.5f,y,width,depth,height,lean,form);
                OccupiedTerrace(b,face,width*Shape(y,form)*.87f,.24f,world,theme==6||theme==4||theme==7);
            }
            CapturePart("Building",b,false);
        }
        static void OccupiedTerrace(SkyPulseMeshBuilder b,Vector3 at,float width,float depth,int world,bool planted)
        {
            // A terrace is a supported slab, a recessed glass parapet and a
            // shaded overhang. All these surfaces occupy real front-to-back depth.
            RoundedSlab(b,at+new Vector3(0,-.018f,-depth*.40f),width,depth,.070f,Shell(world));
            Beam(b,at+new Vector3(-width*.46f,.16f,-depth*.78f),at+new Vector3(width*.46f,.16f,-depth*.78f),.017f,.105f,Glass(world));
            Beam(b,at+new Vector3(-width*.48f,.22f,-depth*.83f),at+new Vector3(width*.48f,.22f,-depth*.83f),.018f,.018f,Edge);
            for(int side=-1;side<=1;side+=2)
            {
                Beam(b,at+new Vector3(side*width*.46f,.015f,-depth*.78f),at+new Vector3(side*width*.46f,.22f,-depth*.78f),.022f,.023f,Frame);
                Beam(b,at+new Vector3(side*width*.35f,-.18f,.018f),at+new Vector3(side*width*.35f,-.045f,-depth*.70f),.035f,.040f,Frame);
            }
            Beam(b,at+new Vector3(-width*.45f,-.047f,-depth*.90f),at+new Vector3(width*.45f,-.047f,-depth*.90f),.011f,.012f,Light(world));
            if(planted)
            {
                RoundedSlab(b,at+new Vector3(0,.045f,-depth*.30f),width*.72f,depth*.38f,.085f,new Color(.14f,.24f,.18f,0));
                for(int leaf=0;leaf<4;leaf++)
                {
                    var p=at+new Vector3((leaf-1.5f)*width*.15f,.08f,-depth*.30f);
                    b.Face(p+Vector3.left*.045f,p+new Vector3(.025f,.19f,-.018f),p+new Vector3(.075f,.045f,.025f),new Color(.23f,.42f,.28f,.20f));
                }
            }
        }
        static void ServiceDetail(SkyPulseMeshBuilder b,Vector3 at,float width,float depth,float height,float lean,int form,int world)
        {
            if(height<1.8f)return;int theme=world%12;float t=.43f+(theme%3)*.055f;
            var face=at+TowerPoint(-Mathf.PI*.5f,t,width,depth,height,lean,form);float span=width*Shape(t,form)*.55f;
            // A legible service floor, kept within the existing massing and
            // entrance envelope. Its construction follows the world's purpose.
            if(theme==4||theme==8||theme==10)
            {
                float rise=Mathf.Min(.28f,height*.10f);
                Arch(b,face+new Vector3(0,-rise*.4f,-.035f),span*.35f,rise,.025f,0,180,Quaternion.identity,theme==8?Edge:Shell(world),16);
                for(int side=-1;side<=1;side+=2)Beam(b,face+new Vector3(side*span*.44f,-rise*.45f,-.04f),face+new Vector3(side*span*.44f,rise*.72f,-.04f),.045f,.055f,Shell(world));
                Beam(b,face+new Vector3(-span*.40f,-rise*.48f,-.06f),face+new Vector3(span*.40f,-rise*.48f,-.06f),.025f,.014f,Light(world));
            }
            else if(theme==2||theme==5||theme==9||theme==11)
            {
                for(int slat=0;slat<5;slat++)
                {
                    float y=(slat-2)*.045f;float offset=theme==5?slat*.006f:0;
                    Beam(b,face+new Vector3(-span*.43f,y,-.055f-offset),face+new Vector3(span*.43f,y,-.055f-offset),.038f,.018f,slat==2?Edge:Frame);
                }
                for(int side=-1;side<=1;side+=2)Beam(b,face+new Vector3(side*span*.49f,-.12f,-.035f),face+new Vector3(side*span*.49f,.12f,-.035f),.032f,.035f,Edge);
                Beam(b,face+new Vector3(-span*.44f,-.14f,-.05f),face+new Vector3(span*.44f,-.14f,-.05f),.018f,.012f,Light(world));
            }
            else
            {
                RoundedSlab(b,face+new Vector3(0,-.11f,-.045f),span,.16f,.045f,theme==6?new Color(.19f,.28f,.21f,0):Frame);
                Beam(b,face+new Vector3(-span*.48f,.13f,-.08f),face+new Vector3(span*.48f,.13f,-.08f),.09f,.025f,Shell(world));
                for(int side=-1;side<=1;side+=2)
                {
                    Beam(b,face+new Vector3(side*span*.44f,-.09f,-.08f),face+new Vector3(side*span*.44f,.14f,-.08f),.025f,.025f,Edge);
                    if(theme==7)Beam(b,face+new Vector3(side*span*.44f,-.09f,-.085f),face+new Vector3(-side*span*.44f,.13f,-.085f),.014f,.014f,Edge);
                }
                Beam(b,face+new Vector3(-span*.43f,-.07f,-.13f),face+new Vector3(span*.43f,-.07f,-.13f),.012f,.012f,Light(world));
                if(theme==6)for(int leaf=0;leaf<4;leaf++)
                {
                    var root=face+new Vector3((leaf-1.5f)*span*.19f,-.07f,-.07f);
                    b.Face(root,root+new Vector3(.035f,.13f,-.02f),root+new Vector3(.07f,.04f,.01f),new Color(.25f,.44f,.28f,0));
                }
            }
        }
        static void FacadeStructure(SkyPulseMeshBuilder b,Vector3 at,float width,float depth,float height,float lean,int form,int world)
        {
            // Raised corner ribs follow each building's existing tapered or
            // swept profile. Keep the approved massing, rather than replacing
            // towers/castles with another glass shell.
            for(int rib=0;rib<4;rib++)
            {
                float angle=Mathf.PI*.25f+rib*Mathf.PI*.5f;
                for(int section=0;section<5;section++)
                {
                    float t=.11f+section*.16f,u=t+.16f;
                    var from=at+TowerPoint(angle,t,width*1.06f,depth*1.06f,height,lean,form);
                    var to=at+TowerPoint(angle,u,width*1.06f,depth*1.06f,height,lean,form);
                    Beam(b,from,to,form==4?.045f:.044f,form==4?.055f:.045f,form==4?Shell(world):Frame);
                }
            }
            if(form==4)
            {
                for(int i=0;i<4;i++)
                {
                    float a=Mathf.PI*.25f+i*Mathf.PI*.5f;
                    var foot=at+TowerPoint(a,0,width*1.34f,depth*1.34f,height,0,form);
                    var shoulder=at+TowerPoint(a,.24f,width*1.03f,depth*1.03f,height,0,form);
                    Beam(b,foot,shoulder,.10f,.11f,Shell(world));
                }
            }
            else if(height>3)
            {
                // Two short projecting ledges give the facade actual relief.
                // They occupy only the front face, not giant rings per floor.
                foreach(float t in new[]{.30f,.67f})
                {
                    var p=at+TowerPoint(-Mathf.PI*.5f,t,width,depth,height,lean,form);
                    // A supported terrace and transparent parapet make this
                    // a real occupied floor, not a pasted horizontal stripe.
                    Beam(b,p+new Vector3(-width*.32f,0,-.075f),p+new Vector3(width*.32f,0,-.075f),.20f,.035f,Edge);
                    Beam(b,p+new Vector3(-width*.32f,.11f,-.16f),p+new Vector3(width*.32f,.11f,-.16f),.015f,.055f,Glass(world));
                    for(int side=-1;side<=1;side+=2)
                        Beam(b,p+new Vector3(side*width*.29f,-.13f,.015f),p+new Vector3(side*width*.29f,0,-.12f),.025f,.025f,Frame);
                }
                if(world%12==7)
                {
                    var a=at+TowerPoint(-Mathf.PI*.5f,.34f,width,depth,height,lean,form);
                    var c=at+TowerPoint(-Mathf.PI*.5f,.65f,width,depth,height,lean,form);
                    Beam(b,a+new Vector3(-width*.35f,0,-.045f),c+new Vector3(width*.31f,0,-.045f),.023f,.025f,Edge);
                }
            }
        }
        static void Entrance(SkyPulseMeshBuilder b,Vector3 at,float width,float depth,float height,int world)
        {
            float h=Mathf.Min(.49f,height*.20f),w=width*.31f;
            var p=at+new Vector3(0,.055f,-depth*.54f);
            // The dark inner doorway sits behind its projected jambs/canopy.
            b.Quad(p+new Vector3(-w*.5f,0,-.012f),p+new Vector3(w*.5f,0,-.012f),p+new Vector3(w*.5f,h,-.012f),p+new Vector3(-w*.5f,h,-.012f),Glass(world));
            Beam(b,p+new Vector3(-w*.58f,0,-.04f),p+new Vector3(-w*.58f,h,-.04f),.035f,.045f,Edge);
            Beam(b,p+new Vector3(w*.58f,0,-.04f),p+new Vector3(w*.58f,h,-.04f),.035f,.045f,Edge);
            RoundedSlab(b,p+new Vector3(0,h+.025f,-.08f),w*1.66f,.26f,.055f,Shell(world));
            Beam(b,p+new Vector3(-w*.78f,h-.015f,-.19f),p+new Vector3(w*.78f,h-.015f,-.19f),.014f,.014f,Light(world));
            RoundedSlab(b,p+new Vector3(0,-.01f,-.13f),w*1.70f,.33f,.055f,Frame);
            RoundedSlab(b,p+new Vector3(0,-.045f,-.22f),w*1.98f,.37f,.030f,Shell(world));
        }
        static void RoofDetails(SkyPulseMeshBuilder b,Vector3 at,float width,float depth,int world)
        {
            // Low parapets and screened plant give roofs a readable thickness.
            for(int side=-1;side<=1;side+=2)
            {
                Beam(b,at+new Vector3(side*width*.43f,.10f,-depth*.38f),at+new Vector3(side*width*.43f,.10f,depth*.38f),.030f,.10f,Frame);
                Beam(b,at+new Vector3(-width*.43f,.10f,side*depth*.38f),at+new Vector3(width*.43f,.10f,side*depth*.38f),.030f,.10f,Frame);
            }
            for(int i=0;i<3;i++)Beam(b,at+new Vector3(-width*.15f,.17f,(i-1)*depth*.12f),at+new Vector3(width*.15f,.17f,(i-1)*depth*.12f),.016f,.030f,Shadow);
            var mast=at+new Vector3(width*.29f,.14f,depth*.22f);
            Beam(b,mast,mast+Vector3.up*.20f,.016f,.016f,Edge);
            Beam(b,mast+Vector3.up*.18f,mast+Vector3.up*.21f,.025f,.025f,Light(world));
        }
        static void ArchitecturalCrown(SkyPulseMeshBuilder b,Vector3 at,float width,float depth,int form,int world)
        {
            int theme=world%12;if(form==4)return; // Castle parapets remain masonry.
            if(theme==2||theme==5||theme==9||theme==11)
            {
                // A shaded technical crown has actual clearance, four posts,
                // a thin fascia and a recessed luminous leading edge.
                float y=.31f;
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                    Beam(b,at+new Vector3(x*width*.32f,.08f,z*depth*.29f),at+new Vector3(x*width*.32f,y,z*depth*.29f),.026f,.026f,Frame);
                RoundedSlab(b,at+Vector3.up*y,width*1.03f,depth*.97f,.045f,Shell(world));
                RoundedSlab(b,at+Vector3.up*(y+.027f),width*.85f,depth*.80f,.012f,Glass(world));
                Beam(b,at+new Vector3(-width*.49f,y-.018f,-depth*.47f),at+new Vector3(width*.49f,y-.018f,-depth*.47f),.012f,.014f,Light(world));
            }
            else if(theme==4||theme==8||theme==10)
            {
                // A solid lantern/observatory crown complements the world's
                // citadel massing without replacing it with a glass skyscraper.
                RoundedSlab(b,at+Vector3.up*.20f,width*.42f,depth*.46f,.24f,Shell(world));
                for(int side=-1;side<=1;side+=2)
                    Beam(b,at+new Vector3(side*width*.19f,.12f,-depth*.25f),at+new Vector3(side*width*.19f,.30f,-depth*.25f),.020f,.022f,Edge);
                RoundedSlab(b,at+Vector3.up*.35f,width*.56f,depth*.60f,.055f,Frame);
                Beam(b,at+new Vector3(-width*.25f,.34f,-depth*.30f),at+new Vector3(width*.25f,.34f,-depth*.30f),.012f,.013f,Light(world));
            }
            else
            {
                // Two structural shoulders converge around a dark open crown.
                // Large clean lines survive phone scale better than more tiny
                // window boxes or a generic antenna on every building.
                for(int side=-1;side<=1;side+=2)
                {
                    var foot=at+new Vector3(side*width*.43f,.04f,-depth*.35f);
                    var crest=at+new Vector3(side*width*.15f,.38f,-depth*.24f);
                    Beam(b,foot,crest,.050f,.055f,Shell(world));
                    Beam(b,foot+Vector3.back*.029f,crest+Vector3.back*.029f,.010f,.013f,Light(world));
                    Beam(b,crest,crest+Vector3.forward*depth*.56f,.035f,.035f,Frame);
                }
                Beam(b,at+new Vector3(-width*.15f,.38f,-depth*.24f),at+new Vector3(width*.15f,.38f,-depth*.24f),.035f,.035f,Edge);
            }
        }
        static float Shape(float t,int form)
        {
            if(form==4)return t<.08f?1.14f:t<.82f?1:Mathf.Lerp(1,1.12f,Mathf.SmoothStep(0,1,(t-.82f)/.18f));
            if(form==1)return 1-.42f*Mathf.Pow(t,2.8f); // a full-height aerodynamic sail
            // Stable vertical walls, purposeful upper setbacks and a readable
            // crown replace a continuously tapering pile of floor plates.
            if(form==2)return t<.12f?Mathf.Lerp(1.08f,.96f,Mathf.SmoothStep(0,1,t/.12f)):t<.77f?.96f:Mathf.Lerp(.96f,.76f,Mathf.SmoothStep(0,1,(t-.77f)/.23f));
            if(form==3)return t<.68f?1-.09f*t:Mathf.Lerp(.94f,.66f,(t-.68f)/.32f);
            return t<.10f?Mathf.Lerp(1.06f,.98f,t/.10f):t<.64f?.98f:t<.73f?Mathf.Lerp(.98f,.77f,(t-.64f)/.09f):t<.88f?.77f:Mathf.Lerp(.77f,.62f,(t-.88f)/.12f);
        }
        static Vector3 TowerPoint(float a,float t,float width,float depth,float height,float lean,int form)
        {
            float c=Mathf.Cos(a),s=Mathf.Sin(a),r=Shape(t,form),x,z;
            if(form==1||form==4)
            {
                float exponent=form==4?1:.55f;
                x=Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),exponent)*width*.5f;
                z=Mathf.Sign(s)*Mathf.Pow(Mathf.Abs(s),exponent)*depth*.5f;
            }
            else
            {
                // Ray intersection with a true chamfered rectangle. Long flat
                // facades and their cut corners now have separate physical normals.
                float cut=Mathf.Min(width,depth)*(form==3?.16f:.075f);
                float limit=Mathf.Min(width*.5f/Mathf.Max(.0001f,Mathf.Abs(c)),depth*.5f/Mathf.Max(.0001f,Mathf.Abs(s)));
                limit=Mathf.Min(limit,(width*.5f+depth*.5f-cut)/Mathf.Max(.0001f,Mathf.Abs(c)+Mathf.Abs(s)));
                x=c*limit;z=s*limit;
            }
            return new Vector3(x*r+lean*t*t,t*height,z*r);
        }
        static Vector3 TowerNormal(float a,float t,float width,float depth,float height,float lean,int form)
        {
            var along=TowerPoint(a+.002f,t,width,depth,height,lean,form)-TowerPoint(a-.002f,t,width,depth,height,lean,form);
            var up=TowerPoint(a,t+.002f,width,depth,height,lean,form)-TowerPoint(a,t-.002f,width,depth,height,lean,form);
            return Vector3.Cross(up.normalized,along.normalized).normalized;
        }
        static void Pavilion(SkyPulseMeshBuilder b,Vector3 at,float width,float depth,float height,int world)
        {
            CapturePart("Pavilion",b,true);
            RoundedSlab(b,at+Vector3.up*(height*.48f),width,depth,height*.92f,Shell(world));
            RoundedSlab(b,at+Vector3.up*.04f,width*1.03f,depth*1.03f,.08f,Shadow);
            RoundedSlab(b,at+Vector3.up*height,width*1.09f,depth*1.12f,.10f,Shell(world));
            // Thin edge diffusers replace the old fully luminous roof plate.
            for(int side=-1;side<=1;side+=2)
                Beam(b,at+new Vector3(side*width*.515f,height+.054f,-depth*.49f),at+new Vector3(side*width*.515f,height+.054f,depth*.49f),.018f,.012f,Light(world));
            Beam(b,at+new Vector3(-width*.515f,height+.054f,-depth*.53f),at+new Vector3(width*.515f,height+.054f,-depth*.53f),.018f,.012f,Light(world));
            for(int i=-2;i<=2;i++)
            {
                var p=at+new Vector3(i*width*.17f,height*.47f,-depth*.505f);
                RoundedSlab(b,p,width*.135f,.032f,height*.55f,Frame);
                RoundedSlab(b,p+new Vector3(0,0,-.019f),width*.111f,.012f,height*.43f,WindowGlass(world,Mathf.RoundToInt(at.y*137+at.x*127),i+3,3));
            }
            for(int side=-1;side<=1;side+=2)Beam(b,at+new Vector3(side*width*.46f,.08f,-depth*.55f),at+new Vector3(side*width*.46f,height-.045f,-depth*.55f),.030f,.045f,Edge);
            if(world%12==6)
                for(int side=-1;side<=1;side+=2)for(int fin=0;fin<3;fin++)
                {
                    float z=(fin-1)*depth*.25f;
                    Beam(b,at+new Vector3(side*width*.50f,height*.45f,z),at+new Vector3(side*width*.50f,height+.04f,z),.023f,.035f,Frame);
                    Beam(b,at+new Vector3(side*width*.50f,height+.05f,z-.04f),at+new Vector3(side*width*.50f,height+.05f,z+.04f),.014f,.012f,Light(world));
                }
            CapturePart("Pavilion",b,false);
        }
        internal static void WindowBay(SkyPulseMeshBuilder b,Vector3 at,Vector3 p,Vector3 q,Vector3 r,Vector3 s,Vector3 na,Vector3 nb,Color wall,Color glass)
        {
            const float left=.085f,right=.915f,bottom=.15f,top=.86f;
            var l0=Vector3.Lerp(p,q,left);var l1=Vector3.Lerp(s,r,left);var r0=Vector3.Lerp(p,q,right);var r1=Vector3.Lerp(s,r,right);
            var a=Vector3.Lerp(l0,l1,bottom);var c=Vector3.Lerp(r0,r1,bottom);var d=Vector3.Lerp(r0,r1,top);var e=Vector3.Lerp(l0,l1,top);
            b.SmoothQuad(at+p,at+l0,at+l1,at+s,na,na,wall);b.SmoothQuad(at+r0,at+q,at+r,at+r1,nb,nb,wall);
            b.SmoothQuad(at+l0,at+r0,at+c,at+a,na,nb,wall);b.SmoothQuad(at+e,at+d,at+r1,at+l1,na,nb,wall);
            var inward=(na+nb).normalized*-.052f;
            WindowReveal(b,at+a,at+c,inward);WindowReveal(b,at+c,at+d,inward);
            WindowReveal(b,at+d,at+e,inward);WindowReveal(b,at+e,at+a,inward);
            b.SmoothQuad(at+a+inward,at+c+inward,at+d+inward,at+e+inward,na,nb,glass);
            var sill=(na+nb).normalized*.014f;
            var edge=Color.Lerp(wall,Edge,.48f);
            b.SmoothQuad(at+a+sill,at+c+sill,at+c+sill+Vector3.up*.018f,at+a+sill+Vector3.up*.018f,na,nb,edge);
            b.SmoothQuad(at+a+sill+Vector3.up*.018f,at+c+sill+Vector3.up*.018f,at+c+Vector3.up*.018f,at+a+Vector3.up*.018f,Vector3.up,Vector3.up,edge);
        }
        static void WindowReveal(SkyPulseMeshBuilder b,Vector3 from,Vector3 to,Vector3 inward)
        {
            var normal=Vector3.Cross((to-from)*100,inward*100).normalized;
            b.SmoothQuad(from,to,to+inward,from+inward,normal,normal,Shadow);
        }
        static void Turret(SkyPulseMeshBuilder b,Vector3 at,float radius,float height,int world)
        {
            CapturePart("Castle tower",b,true);
            if(world>=12){height*=.85f+Hash(world*13+Mathf.RoundToInt(at.x*31))*.22f;radius*=.94f+Hash(world*53)*.08f;}
            Tower(b,at,radius*2,radius*2,height,0,4,world%12);
            // An overhanging parapet with rounded merlons gives a castle its
            // actual silhouette; these are solid masonry, not glass ornaments.
            for(int i=0;i<10;i++)
            {
                float angle=i*Mathf.PI*.2f;
                RoundedSlab(b,at+new Vector3(Mathf.Cos(angle)*radius*.98f,height+.07f,Mathf.Sin(angle)*radius*.98f),radius*.24f,radius*.24f,.20f,Shell(world));
            }
            CapturePart("Castle tower",b,false);
        }
        static void Gatehouse(SkyPulseMeshBuilder b,Vector3 at,float span,float height,int world)
        {
            CapturePart("Gatehouse",b,true);
            var wall=Shell(world);
            Pavilion(b,at+new Vector3(-span*.32f,0,0),span*.33f,.48f,height,world);
            Pavilion(b,at+new Vector3(span*.32f,0,0),span*.33f,.48f,height,world);
            Arch(b,at+new Vector3(0,.07f,-.04f),span*.17f,height*.80f,.12f,0,180,Quaternion.identity,wall);
            Beam(b,at+new Vector3(-span*.49f,height+.04f,0),at+new Vector3(span*.49f,height+.04f,0),.54f,.14f,wall);
            for(int i=-4;i<=4;i++)RoundedSlab(b,at+new Vector3(i*span*.106f,height+.16f,0),span*.063f,.40f,.16f,wall);
            CapturePart("Gatehouse",b,false);
        }
        static void Promenade(SkyPulseMeshBuilder b,float rx,float rz,int world)
        {
            CapturePart("Promenade",b,true);
            RoundedSlab(b,new Vector3(0,.04f,0),rx*1.88f,rz*1.85f,.19f,Shadow);
            RoundedSlab(b,new Vector3(0,.15f,0),rx*1.91f,rz*1.88f,.085f,Shell(world));
            RoundedSlab(b,new Vector3(0,.20f,0),rx*1.85f,rz*1.82f,.025f,Color.Lerp(Shell(world),Frame,.38f));
            // Open promenade rail has posts and two slim rails, not a solid disc.
            for(int i=0;i<28;i++)
            {
                float a=i*Mathf.PI*2/28,c=(i+1f)*Mathf.PI*2/28;
                var p=Plan(a,rx*1.85f,rz*1.82f)+Vector3.up*.23f;var q=Plan(c,rx*1.85f,rz*1.82f)+Vector3.up*.23f;
                Beam(b,p,p+Vector3.up*.18f,.012f,.012f,Edge);Beam(b,p+Vector3.up*.17f,q+Vector3.up*.17f,.012f,.015f,Edge);
                if(i%4==0)Beam(b,p+Vector3.up*.025f,q+Vector3.up*.025f,.010f,.012f,Light(world));
            }
            // A second inset surface, perimeter supports and expansion joints
            // make the district ground read as an occupied civic terrace.
            RoundedSlab(b,new Vector3(0,.219f,0),rx*1.64f,rz*1.58f,.024f,Color.Lerp(Shell(world),Edge,.24f));
            for(int bay=0;bay<12;bay++)
            {
                float a=-Mathf.PI+.14f+bay*(Mathf.PI-.28f)/12f;
                var p=Plan(a,rx*1.88f,rz*1.85f);
                Beam(b,p+Vector3.down*.025f,p+Vector3.up*.19f,.045f,.055f,Shell(world));
                if(bay%3==1)
                {
                    var q=Plan(a+.13f,rx*1.88f,rz*1.85f);
                    Beam(b,p+Vector3.up*.035f,q+Vector3.up*.035f,.016f,.012f,Light(world));
                }
            }
            for(int joint=-2;joint<=2;joint++)
            {
                float x=joint*rx*.29f;
                Beam(b,new Vector3(x,.234f,-rz*.71f),new Vector3(x,.234f,-rz*.18f),.010f,.003f,Frame);
            }
            // A continuous inhabited retaining plinth joins the tower feet.
            // This is one architectural ground storey with deep bay openings,
            // shaded reveals and pilasters, rather than three objects on a disc.
            var podium=new Vector3(0,.24f,.20f);float span=rx*1.55f,d=rz*.93f,h=.49f;
            var wall=Color.Lerp(Shell(world),Edge,.16f);
            RoundedSlab(b,podium+new Vector3(0,h*.48f,d*.12f),span,d,h*.96f,wall);
            RoundedSlab(b,podium+new Vector3(0,h+.026f,d*.08f),span*1.035f,d*1.10f,.075f,wall);
            RoundedSlab(b,podium+new Vector3(0,.025f,0),span*1.03f,d*1.05f,.050f,Frame);
            for(int bay=0;bay<8;bay++)
            {
                float left=(bay-4)*span/8,right=(bay-3)*span/8,z=-d*.51f;
                WindowBay(b,podium,new Vector3(left,.07f,z),new Vector3(right,.07f,z),new Vector3(right,h-.045f,z),new Vector3(left,h-.045f,z),Vector3.back,Vector3.back,wall,WindowGlass(world,117,bay,2));
            }
            // A broad stair at the front gives every district a legible arrival
            // and two seated service benches establish its human scale.
            for(int stair=0;stair<3;stair++)RoundedSlab(b,new Vector3(0,.228f+stair*.025f,-rz*.57f+stair*.07f),.91f-stair*.06f,.29f,.035f,wall);
            for(int side=-1;side<=1;side+=2)
            {
                RoundedSlab(b,new Vector3(side*rx*.60f,.285f,-rz*.62f),.45f,.16f,.075f,Frame);
                RoundedSlab(b,new Vector3(side*rx*.60f,.326f,-rz*.62f),.48f,.17f,.025f,Color.Lerp(Shell(world),Edge,.30f));
            }
            CapturePart("Promenade",b,false);
        }
        static void Dock(SkyPulseMeshBuilder b,int world)
        {
            CapturePart("Station foundation",b,true);
            RoundedSlab(b,new Vector3(0,-.20f,0),4.70f,2.50f,.46f,Frame);Promenade(b,2.28f,1.27f,world);
            for(int side=-1;side<=1;side+=2)
            {
                RoundedSlab(b,new Vector3(side*1.42f,-.62f,.28f),.68f,1.40f,.58f,Shadow);
                RoundedSlab(b,new Vector3(side*1.42f,-.95f,.28f),.54f,1.15f,.11f,Edge);
                RoundedSlab(b,new Vector3(side*1.42f,-1.03f,.28f),.44f,.94f,.065f,Light(world));
                RoundedSlab(b,new Vector3(side*.68f,-1.13f,.34f),.48f,.82f,.88f,Frame);
                RoundedSlab(b,new Vector3(side*.68f,-1.64f,.34f),.39f,.68f,.24f,Edge);
                RoundedSlab(b,new Vector3(0,-1.86f,.34f),.54f,.70f,.18f,Shadow);
            }
            for(int side=-1;side<=1;side+=2)
            {
                Beam(b,new Vector3(side*1.95f,-.17f,-.87f),new Vector3(side*1.45f,-.72f,-.70f),.065f,.075f,Edge);
            }
            BeaconStars(b,world);
            CapturePart("Station foundation",b,false);
        }
        static void Basin(SkyPulseMeshBuilder b,float rx,float rz,int world)
        {
            CapturePart("Harbour foundation",b,true);
            RoundedSlab(b,new Vector3(0,-.50f,0),rx*2,rz*2,.85f,new Color(.09f,.17f,.21f,.12f));
            RoundedSlab(b,new Vector3(0,-.03f,-.14f),rx*1.98f,rz*1.98f,.025f,new Color(.04f,.13f,.19f,.34f));
            for(int side=-1;side<=1;side+=2)RoundedSlab(b,new Vector3(side*1.65f,-1.30f,.24f),.72f,1.20f,1.12f,Frame);
            BeaconStars(b,world);
            CapturePart("Harbour foundation",b,false);
        }
        static void Landform(SkyPulseMeshBuilder b,float rx,float rz,float depth,int world)
        {
            CapturePart("Cliff island",b,true);
            world%=12;
            const int sides=36,rows=7;
            for(int row=0;row<rows;row++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1f)*Mathf.PI*2/sides;
                float t=row/(float)rows,u=(row+1f)/rows;
                var p=Rock(a,t,rx,rz,depth,world);var q=Rock(c,t,rx,rz,depth,world);var r=Rock(c,u,rx,rz,depth,world);var s=Rock(a,u,rx,rz,depth,world);
                Color stone=world==1||world==5||world==11?new Color(.30f,.42f,.50f,.18f):world==10?new Color(.35f,.23f,.17f,.18f):world==6?new Color(.17f,.23f,.19f,.18f):new Color(.24f,.18f,.24f,.18f);
                float mineral=Hash(world*1237+side*61+row*173);
                stone=Color.Lerp(stone,world==10?new Color(.55f,.38f,.23f,.18f):world==6?new Color(.29f,.43f,.33f,.18f):new Color(.43f,.48f,.55f,.18f),.12f+mineral*.30f);
                // Hard geological planes retain physical light and dark relief.
                // Paired triangles expose broken rock instead of a smooth bowl.
                b.Quad(p,q,r,s,stone);
                if(row==0)b.Face(new Vector3(0,-.02f,0),q,p,new Color(.18f,.22f,.24f,0));
                if(row==rows-1)b.Face(new Vector3(.22f,-depth,0),s,r,stone);
            }
            // Broken cliff ledges reveal physical strata, without turning the
            // island into another stack of uniform metal rings.
            for(int level=0;level<3;level++)for(int side=0;side<18;side++)
            {
                if((side+level*2)%7==0)continue;
                float a=-Mathf.PI+.10f+side*(Mathf.PI-.20f)/18,c=-Mathf.PI+.10f+(side+1)*(Mathf.PI-.20f)/18;
                float t=.16f+level*.275f+(side%3)*.009f;
                var p=Rock(a,t,rx,rz,depth,world);var q=Rock(c,t,rx,rz,depth,world);
                var na=RockNormal(a,t,rx,rz,depth,world);var nb=RockNormal(c,t,rx,rz,depth,world);
                float shelf=.10f+Hash(side*73+level*97+world)*.08f;var u=p+na*shelf;var v=q+nb*shelf;
                var stone=world==1||world==5||world==11?new Color(.32f,.43f,.50f,.18f):world==10?new Color(.39f,.27f,.19f,.18f):new Color(.27f,.24f,.28f,.18f);
                b.SmoothQuad(p,q,v,u,Vector3.up,Vector3.up,stone);
                b.SmoothQuad(u,v,v-Vector3.up*.080f,u-Vector3.up*.080f,na,nb,stone);
            }
            FoundationSeats(b,rx,rz,depth,world);
            BeaconStars(b,world);
            CapturePart("Cliff island",b,false);
        }
        static void FoundationSeats(SkyPulseMeshBuilder b,float rx,float rz,float depth,int world)
        {
            // Retaining piers connect the civic deck to the rock shoulder.
            // They occupy the gaps between the existing star mountings, with
            // a restrained material appropriate to stone/ceramic civic plans.
            Color stone=Shell(world);stone=Color.Lerp(stone,Edge,.20f);stone.a=world==4||world==6||world==8||world==10?.12f:0;
            for(int pier=0;pier<6;pier++)
            {
                float a=-Mathf.PI*.5f-.95f+pier*.38f;
                var upper=Rock(a,.025f,rx,rz,depth,world);
                var lower=Rock(a,.23f,rx,rz,depth,world);
                var normal=RockNormal(a,.13f,rx,rz,depth,world);
                var cap=upper+normal*.035f;
                Beam(b,cap,lower+normal*.045f,.135f,.105f,stone);
                Beam(b,cap+normal*.037f,Vector3.Lerp(upper,lower,.72f)+normal*.082f,.018f,.012f,Frame);
                var edge=Vector3.Cross(Vector3.up,normal).normalized*.11f;
                Beam(b,cap-edge,cap+edge,.060f,.045f,Edge);
            }
            // Broken rock facets carry visible depth under the promenade.
            // Existing beacon receivers and their five-source light layout
            // remain exactly on the original middle band above these shelves.
            for(int shelf=0;shelf<4;shelf++)
            {
                float a=-2.67f+shelf*.60f,c=a+.37f,t=.65f+(shelf%2)*.10f;
                var p=Rock(a,t,rx,rz,depth,world);var q=Rock(c,t,rx,rz,depth,world);
                var n=RockNormal(a,t,rx,rz,depth,world);var m=RockNormal(c,t,rx,rz,depth,world);
                var outerP=p+n*.075f;var outerQ=q+m*.075f;
                Color mineral=world==10?new Color(.40f,.29f,.20f,.18f):world==6?new Color(.25f,.34f,.28f,.18f):world==1||world==5||world==11?new Color(.35f,.46f,.53f,.18f):new Color(.31f,.28f,.35f,.18f);
                b.SmoothQuad(p,q,outerQ,outerP,Vector3.up,Vector3.up,mineral);
                b.SmoothQuad(outerP,outerQ,outerQ-Vector3.up*.07f,outerP-Vector3.up*.07f,n,m,mineral);
            }
        }
        static void NeonStar(SkyPulseMeshBuilder b,Vector3 at,Vector3 normal,float radius,int world,bool secondary)
        {
            var rotation=Quaternion.FromToRotation(Vector3.forward,normal);
            var colour=secondary?SkyPulseRouteArchitecture.Secondary(world%12):Light(world);colour.a=1;
            var accent=secondary?Light(world):SkyPulseRouteArchitecture.Secondary(world%12);accent.a=1;
            // Layered five-point hardware: side wall, reflective bevel,
            // luminous gasket, dark recess and a faceted glass core.
            for(int i=0;i<10;i++)
            {
                float a=Mathf.PI*.5f+i*Mathf.PI/5,c=a+Mathf.PI/5;
                float ra=(i%2==0?1:.46f)*radius,rc=(i%2==0?.46f:1)*radius;
                var p=new Vector3(Mathf.Cos(a)*ra,Mathf.Sin(a)*ra,0);var q=new Vector3(Mathf.Cos(c)*rc,Mathf.Sin(c)*rc,0);
                var u=p*.87f+Vector3.forward*.020f;var v=q*.87f+Vector3.forward*.020f;
                var g=p*.77f+Vector3.forward*.018f;var h=q*.77f+Vector3.forward*.018f;
                var j=p*.70f+Vector3.forward*.012f;var k=q*.70f+Vector3.forward*.012f;
                b.Quad(at+rotation*p,at+rotation*(p-Vector3.forward*.022f),at+rotation*(q-Vector3.forward*.022f),at+rotation*q,Frame);
                b.Quad(at+rotation*p,at+rotation*q,at+rotation*v,at+rotation*u,i%2==0?Edge:Frame);
                b.Quad(at+rotation*u,at+rotation*v,at+rotation*h,at+rotation*g,accent);
                b.Quad(at+rotation*g,at+rotation*h,at+rotation*k,at+rotation*j,Shadow);
                float brightness=i%2==0?.86f:1;
                var facet=new Color(colour.r*brightness,colour.g*brightness,colour.b*brightness,1);
                b.Face(at+rotation*new Vector3(0,0,.017f),at+rotation*j,at+rotation*k,facet);
            }
            var centreColour=Color.Lerp(colour,Color.white,.36f);centreColour.a=1;
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f,c=(i+1)*Mathf.PI*.5f;
                b.Face(at+rotation*new Vector3(0,0,.020f),at+rotation*new Vector3(Mathf.Cos(a)*radius*.10f,Mathf.Sin(a)*radius*.10f,.018f),at+rotation*new Vector3(Mathf.Cos(c)*radius*.10f,Mathf.Sin(c)*radius*.10f,.018f),centreColour);
            }
        }
        static Vector3 Rock(float a,float t,float rx,float rz,float depth,int world)
        {
            // Broad shoulders and broken lower cliff buttresses read as a
            // landmass. The middle LED mounting band stays byte-for-byte in
            // place, keeping authored beacon light positions registered.
            float shoulder=(1-Mathf.SmoothStep(0,1,(t-.12f)/.12f))*.075f*Mathf.Clamp01(t/.10f);
            float buttress=Mathf.SmoothStep(0,1,(t-.56f)/.32f)*(.14f+.045f*Mathf.Sin(a*3+world*.7f));
            float radius=(1-.58f*Mathf.Pow(t,1.18f)+shoulder+buttress)*(1+.10f*Mathf.Sin(a*5+world)+.055f*Mathf.Sin(a*11+t*4));
            // Real plateau shoulders overhang eroded cliff faces. Keep the
            // registered middle beacon band exactly in place; outside it the
            // upper retaining terrace and fractured lower buttresses change depth.
            float upper=(1-Mathf.SmoothStep(0,1,(t-.17f)/.105f))*Mathf.Sin(Mathf.Clamp01(t/.19f)*Mathf.PI*.5f);
            float lowerShelf=Mathf.SmoothStep(0,1,(t-.55f)/.12f)*(1-Mathf.SmoothStep(0,1,(t-.80f)/.15f));
            radius+=upper*(.11f+.035f*Mathf.Sin(a*4+world))+lowerShelf*(.07f+.04f*Mathf.Sin(a*5+world));
            // Preserve the upper shoulder and every registered LED mounting
            // point. Below that band, three buttressed rock lobes replace the
            // broad flat underside: a carved landmass rather than a metal bowl.
            float lower=Mathf.SmoothStep(0,1,(t-.55f)/.45f);
            float lobe=.5f+.5f*Mathf.Cos(a*3+world*.39f);
            radius*=1-lower*(.50f-.18f*lobe*lobe);
            float bedrock=lower*.18f*(.35f+.65f*lobe*lobe);
            return new Vector3(Mathf.Cos(a)*rx*radius+.22f*t, -.04f-depth*t+.14f*Mathf.Sin(a*7+world)*t-bedrock,Mathf.Sin(a)*rz*radius);
        }
        static Vector3 RockNormal(float a,float t,float rx,float rz,float depth,int world)
        {
            var around=Rock(a+.01f,t,rx,rz,depth,world)-Rock(a-.01f,t,rx,rz,depth,world);
            var down=Rock(a,t+.01f,rx,rz,depth,world)-Rock(a,t-.01f,rx,rz,depth,world);
            return Vector3.Cross(around.normalized,down.normalized).normalized;
        }
        static void Garden(SkyPulseMeshBuilder b,Vector3 at,float span,int world)
        {
            CapturePart("Garden terrace",b,true);
            RoundedSlab(b,at,span*2,.30f,.08f,new Color(.16f,.24f,.18f,0));
            for(int i=0;i<5;i++)
            {
                var p=at+new Vector3((i-2)*span*.40f,.10f,Mathf.Sin(i*2)*.08f);
                Beam(b,p,p+Vector3.up*.20f,.024f,.026f,Frame);
                Dome(b,p+Vector3.up*.21f,.15f,.24f,.14f,new Color(.12f,.29f,.18f,.20f),12,5);
                Dome(b,p+Vector3.up*.23f,.11f,.18f,.13f,new Color(.18f,.37f,.23f,.20f),12,5);
            }
            CapturePart("Garden terrace",b,false);
        }
        static void Skyway(SkyPulseMeshBuilder b,Vector3 from,Vector3 to,float width,int world)
        {
            CapturePart("Sky bridge",b,true);
            var axis=(to-from).normalized;var side=Vector3.Cross(axis,Vector3.up).normalized*width*.5f;
            Beam(b,from,to,width,.12f,Frame);
            Beam(b,from+Vector3.up*.265f,to+Vector3.up*.265f,width*1.03f,.035f,Shell(world));
            // A thin roof and individual recessed glazing bays retain an open
            // interior. Solid portals sit inside the existing end structures;
            // a continuous opaque glass beam no longer fills the passage.
            for(int bay=0;bay<4;bay++)
            {
                var a=Vector3.Lerp(from,to,bay*.25f);var c=Vector3.Lerp(from,to,(bay+1f)*.25f);
                var p=Vector3.Lerp(a,c,.07f);var q=Vector3.Lerp(a,c,.93f);
                foreach(int face in new[]{-1,1})
                {
                    var edge=side*face*.88f;
                    b.Quad(p+edge+Vector3.up*.055f,q+edge+Vector3.up*.055f,q+edge+Vector3.up*.232f,p+edge+Vector3.up*.232f,Glass(world));
                }
                Beam(b,a+side-Vector3.up*.115f,c+side-Vector3.up*.115f,.022f,.025f,Edge);
                Beam(b,a+side-Vector3.up*.105f,c+side-Vector3.up*.020f,.014f,.014f,Frame);
            }
            for(int portal=0;portal<=4;portal++)
            {
                var p=Vector3.Lerp(from,to,portal*.25f);
                Beam(b,p-side+Vector3.up*.025f,p-side+Vector3.up*.255f,.020f,.022f,Frame);
                Beam(b,p+side+Vector3.up*.025f,p+side+Vector3.up*.255f,.020f,.022f,Frame);
                Beam(b,p-side+Vector3.up*.255f,p+side+Vector3.up*.255f,.022f,.022f,Edge);
            }
            Beam(b,from+side+Vector3.up*.04f,to+side+Vector3.up*.04f,.012f,.014f,Light(world));
            Beam(b,from-side+Vector3.up*.04f,to-side+Vector3.up*.04f,.020f,.020f,Edge);
            CapturePart("Sky bridge",b,false);
        }
        static void SolarWing(SkyPulseMeshBuilder b,Vector3 at,int world,bool distant=false)
        {
            CapturePart("Solar array",b,true);
            RoundedSlab(b,at,1.26f,1.55f,.085f,Frame);
            // Distant panels retain their framed silhouette without spending
            // foreground geometry on cells that are smaller than one pixel.
            int columns=distant?2:4,rows=distant?2:5;
            for(int x=0;x<columns;x++)for(int z=0;z<rows;z++)
            {
                var cell=at+new Vector3((x-(columns-1)*.5f)*(distant?.57f:.285f),.055f,(z-(rows-1)*.5f)*(distant?.71f:.285f));var colour=new Color(.06f,.12f,.23f,.34f);
                if(distant)
                {
                    cell.y+=.009f;
                    b.Quad(cell+new Vector3(-.27f,0,-.335f),cell+new Vector3(-.27f,0,.335f),cell+new Vector3(.27f,0,.335f),cell+new Vector3(.27f,0,-.335f),colour);
                }
                else RoundedSlab(b,cell,.26f,.26f,.018f,colour);
            }
            Beam(b,at+new Vector3(-.58f,.08f,-.68f),at+new Vector3(.58f,.08f,-.68f),.018f,.012f,Light(world));
            CapturePart("Solar array",b,false);
        }
        static Vector3 Plan(float a,float width,float depth)=>new Vector3(Mathf.Sign(Mathf.Cos(a))*Mathf.Pow(Mathf.Abs(Mathf.Cos(a)),.38f)*width*.5f,0,Mathf.Sign(Mathf.Sin(a))*Mathf.Pow(Mathf.Abs(Mathf.Sin(a)),.38f)*depth*.5f);
        internal static void RoundedSlab(SkyPulseMeshBuilder b,Vector3 centre,float width,float depth,float height,Color colour)
        {
            int sides=width<.3f||depth<.16f?8:16;float bevel=Mathf.Min(height*.23f,.035f);
            for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1f)*Mathf.PI*2/sides;
                var p=Plan(a,width,depth);var q=Plan(c,width,depth);var n=Vector3.Cross(Vector3.up,q-p).normalized;
                b.SmoothQuad(centre+p-Vector3.up*(height*.5f-bevel),centre+q-Vector3.up*(height*.5f-bevel),centre+q+Vector3.up*(height*.5f-bevel),centre+p+Vector3.up*(height*.5f-bevel),n,n,colour);
                var u=Plan(a,width-bevel*2,depth-bevel*2)+Vector3.up*height*.5f;var v=Plan(c,width-bevel*2,depth-bevel*2)+Vector3.up*height*.5f;
                b.Quad(centre+p+Vector3.up*(height*.5f-bevel),centre+q+Vector3.up*(height*.5f-bevel),centre+v,centre+u,colour);b.Face(centre+Vector3.up*height*.5f,centre+u,centre+v,colour);
                b.Face(centre-Vector3.up*height*.5f,centre+q-Vector3.up*(height*.5f-bevel),centre+p-Vector3.up*(height*.5f-bevel),Shadow);
            }
        }
        static void Dome(SkyPulseMeshBuilder b,Vector3 at,float rx,float ry,float rz,Color colour,int sides=32,int rows=10)
        {
            CapturePart("Roof dome",b,true);
            for(int side=0;side<sides;side++)for(int row=0;row<rows;row++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1f)*Mathf.PI*2/sides,u=row*Mathf.PI/(rows*2),v=(row+1f)*Mathf.PI/(rows*2);
                var p=new Vector3(Mathf.Cos(a)*Mathf.Cos(u)*rx,Mathf.Sin(u)*ry,Mathf.Sin(a)*Mathf.Cos(u)*rz);var q=new Vector3(Mathf.Cos(c)*Mathf.Cos(u)*rx,Mathf.Sin(u)*ry,Mathf.Sin(c)*Mathf.Cos(u)*rz);
                var r=new Vector3(Mathf.Cos(c)*Mathf.Cos(v)*rx,Mathf.Sin(v)*ry,Mathf.Sin(c)*Mathf.Cos(v)*rz);var s=new Vector3(Mathf.Cos(a)*Mathf.Cos(v)*rx,Mathf.Sin(v)*ry,Mathf.Sin(a)*Mathf.Cos(v)*rz);
                b.SmoothQuad(at+p,at+q,at+r,at+s,new Vector3(p.x/rx,p.y/ry,p.z/rz).normalized,new Vector3(q.x/rx,q.y/ry,q.z/rz).normalized,colour);
            }
            CapturePart("Roof dome",b,false);
        }
        static void Arch(SkyPulseMeshBuilder b,Vector3 at,float rx,float ry,float thickness,float start,float sweep,Quaternion rotation,Color colour,int steps=64)
        {
            CapturePart("Architectural arch",b,true);
            const int sides=8;
            for(int i=0;i<steps;i++)for(int j=0;j<sides;j++)
            {
                float a=(start+sweep*i/steps)*Mathf.Deg2Rad,c=(start+sweep*(i+1f)/steps)*Mathf.Deg2Rad,u=j*Mathf.PI/4,v=(j+1f)*Mathf.PI/4;
                var na=rotation*new Vector3(Mathf.Cos(a)*Mathf.Cos(u),Mathf.Sin(a)*Mathf.Cos(u),Mathf.Sin(u));var nc=rotation*new Vector3(Mathf.Cos(c)*Mathf.Cos(u),Mathf.Sin(c)*Mathf.Cos(u),Mathf.Sin(u));
                b.SmoothQuad(at+rotation*Arc(a,u,rx,ry,thickness),at+rotation*Arc(c,u,rx,ry,thickness),at+rotation*Arc(c,v,rx,ry,thickness),at+rotation*Arc(a,v,rx,ry,thickness),na,nc,colour);
            }
            CapturePart("Architectural arch",b,false);
        }
        static Vector3 Arc(float a,float u,float rx,float ry,float t)=>new Vector3(Mathf.Cos(a)*(rx+Mathf.Cos(u)*t),Mathf.Sin(a)*(ry+Mathf.Cos(u)*t),Mathf.Sin(u)*t);
        internal static void Beam(SkyPulseMeshBuilder b,Vector3 from,Vector3 to,float width,float height,Color colour)
        {
            var direction=(to-from).normalized;var side=Vector3.Cross(direction,Vector3.up).normalized*width*.5f;if(side.sqrMagnitude<.000001f)side=Vector3.right*width*.5f;var up=Vector3.Cross(direction,side.normalized).normalized*height*.5f;
            b.Quad(from-side-up,to-side-up,to+side-up,from+side-up,colour);b.Quad(from-side+up,from+side+up,to+side+up,to-side+up,colour);
            b.Quad(from-side-up,from-side+up,to-side+up,to-side-up,colour);b.Quad(from+side-up,to+side-up,to+side+up,from+side+up,colour);b.Quad(from-side-up,from+side-up,from+side+up,from-side+up,colour);b.Quad(to-side-up,to-side+up,to+side+up,to+side-up,colour);
        }
        static float Hash(int n){uint x=(uint)n*747796405u+2891336453u;x=((x>>((int)(x>>28)+4))^x)*277803737u;return ((x>>22)^x)/4294967295f;}
    }
}
