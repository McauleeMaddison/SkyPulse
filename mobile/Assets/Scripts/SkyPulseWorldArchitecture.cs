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
        static readonly Color Frame=new Color(.19f,.23f,.29f,0),Edge=new Color(.42f,.48f,.55f,0),Shadow=new Color(.035f,.052f,.075f,0);
        static Color Glass(int world)
        {
            world%=12;
            return world==2||world==10?new Color(.18f,.14f,.10f,.34f):world==4?new Color(.17f,.11f,.15f,.34f):world==6?new Color(.07f,.16f,.12f,.34f):world==8?new Color(.10f,.085f,.13f,.34f):world==11?new Color(.13f,.19f,.23f,.34f):new Color(.07f,.13f,.19f,.34f);
        }
        static Color Light(int world)
        {
            world%=12;
            switch(world){case 1:return new Color(.26f,.93f,.79f,1);case 2:return new Color(1,.62f,.20f,1);case 3:return new Color(.14f,.65f,.96f,1);case 4:return new Color(1,.45f,.64f,1);case 5:return new Color(.56f,.78f,1,1);case 6:return new Color(.34f,.91f,.62f,1);case 7:return new Color(.78f,.38f,1,1);case 8:return new Color(.87f,.64f,.34f,1);case 9:return new Color(.27f,.58f,1,1);case 10:return new Color(1,.68f,.32f,1);case 11:return new Color(.48f,.90f,.96f,1);default:return new Color(.25f,.77f,1,1);}
        }
        static Color Shell(int world)
        {
            world%=12;
            return world==4?new Color(.57f,.49f,.44f,.12f):world==8?new Color(.24f,.22f,.29f,.12f):world==10?new Color(.50f,.36f,.24f,.12f):world==1||world==3||world==11?new Color(.54f,.59f,.61f,0):world==2?new Color(.40f,.36f,.29f,0):world==6?new Color(.30f,.36f,.30f,.12f):new Color(.29f,.33f,.39f,0);
        }
        public static Mesh Build(int world,int variant=0)
        {
            world=Mathf.Clamp(world,0,11)+Mathf.Clamp(variant,0,1)*12;var b=new SkyPulseMeshBuilder();var glass=Glass(world);var shell=Shell(world);var light=Light(world);
            switch(world%12)
            {
                case 0: // Rock is terrain; the occupied city has its own engineered promenade.
                    Landform(b,2.30f,1.35f,2.35f,0);Promenade(b,2.15f,1.20f,world);
                    Tower(b,new Vector3(-.20f,.29f,.25f),1.08f,.88f,5.45f,.36f,0,world);
                    Tower(b,new Vector3(-1.20f,.29f,.10f),.78f,.77f,3.15f,-.12f,1,world);
                    Tower(b,new Vector3(1.10f,.29f,.30f),.82f,.78f,3.90f,-.21f,0,world);
                    Tower(b,new Vector3(.60f,.29f,-.66f),.88f,.60f,1.50f,0,2,world);
                    Skyway(b,new Vector3(-1.15f,1.91f,.02f),new Vector3(.96f,1.91f,.02f),.22f,world);
                    Garden(b,new Vector3(-.95f,.31f,-.72f),.65f,world);break;
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
        // Solid framed towers with recessed window bays. Profile changes are setbacks and
        // rounded shoulders. Floor sills follow the facade instead of intersecting
        // it as circular discs, which made the previous buildings look like toys.
        static void Tower(SkyPulseMeshBuilder b,Vector3 at,float width,float depth,float height,float lean,int form,int world)
        {
            if(world>=12&&!((world%12==4||world%12==10)&&form==2))
            {
                float seed=Hash(world*171+Mathf.RoundToInt(at.x*31+at.z*53));
                height*=.75f+seed*.36f;width*=1.18f-seed*.23f;depth*=.91f+seed*.16f;
                lean=-lean*.73f;at.x+=Mathf.Lerp(-.10f,.10f,seed);
            }
            const int sides=24;int floors=Mathf.CeilToInt(height/.38f);var glass=Glass(world);var shell=Shell(world);
            for(int row=0;row<floors;row++)
            {
                float y=row/(float)floors,c=(row+1f)/floors;
                for(int side=0;side<sides;side++)
                {
                    float a=side*Mathf.PI*2/sides,d=(side+1f)*Mathf.PI*2/sides;
                    var p=TowerPoint(a,y,width,depth,height,lean,form);var q=TowerPoint(d,y,width,depth,height,lean,form);
                    var r=TowerPoint(d,c,width,depth,height,lean,form);var s=TowerPoint(a,c,width,depth,height,lean,form);
                    var na=TowerNormal(a,y,width,depth,height,lean,form);var nb=TowerNormal(d,y,width,depth,height,lean,form);
                    // A solid structural bay surrounds a genuinely recessed
                    // window. Opaque piers continue through several floors;
                    // the building is never a single full-height glass skin.
                    bool window=row>0&&row<floors-1&&side%5!=0;
                    if(window)WindowBay(b,at,p,q,r,s,na,nb,shell,glass);
                    else b.SmoothQuad(at+p,at+q,at+r,at+s,na,nb,shell);
                    float sill=Mathf.Min(.015f/height,(c-y)*.18f);
                    b.SmoothQuad(at+Vector3.Scale(p,new Vector3(1.006f,1,1.006f)),at+Vector3.Scale(q,new Vector3(1.006f,1,1.006f)),at+TowerPoint(d,y+sill,width*1.006f,depth*1.006f,height,lean,form),at+TowerPoint(a,y+sill,width*1.006f,depth*1.006f,height,lean,form),na,nb,Frame);
                    if(side%10==2)
                    {
                        float end=a+.020f;
                        b.SmoothQuad(at+TowerPoint(a,y,width*1.015f,depth*1.015f,height,lean,form),at+TowerPoint(end,y,width*1.015f,depth*1.015f,height,lean,form),at+TowerPoint(end,c,width*1.015f,depth*1.015f,height,lean,form),at+TowerPoint(a,c,width*1.015f,depth*1.015f,height,lean,form),na,na,side==2?Light(world):Edge);
                    }
                }
            }
            float cap=.985f;var centre=at+new Vector3(lean*cap*cap,height*cap,0);
            RoundedSlab(b,centre,Shape(cap,form)*width,Shape(cap,form)*depth,.075f,shell);
            RoundedSlab(b,centre+Vector3.up*.052f,width*.46f,depth*.54f,.095f,Frame);
            for(int i=0;i<3;i++)RoundedSlab(b,centre+new Vector3((i-1)*width*.12f,.11f,0),width*.045f,depth*.32f,.028f,Edge);
            RoundedSlab(b,at+Vector3.up*.035f,width*1.16f,depth*1.16f,.075f,Shadow);
        }
        static float Shape(float t,int form)
        {
            if(form==4)return t<.08f?1.14f:t<.82f?1:Mathf.Lerp(1,1.12f,Mathf.SmoothStep(0,1,(t-.82f)/.18f));
            if(form==1)return 1-.62f*Mathf.Pow(t,2.4f); // sail with a softly closed crown
            if(form==2)return t<.16f?1.08f-Mathf.SmoothStep(0,1,t/.16f)*.08f:t<.75f?1:1-.20f*Mathf.SmoothStep(0,1,(t-.75f)/.25f);
            if(form==3)return 1-.46f*t; // bevelled prismatic research building
            return t<.60f?1-.07f*t:t<.67f?Mathf.Lerp(.958f,.76f,(t-.60f)/.07f):t<.86f?.76f:Mathf.Lerp(.76f,.59f,(t-.86f)/.14f);
        }
        static Vector3 TowerPoint(float a,float t,float width,float depth,float height,float lean,int form)
        {
            float c=Mathf.Cos(a),s=Mathf.Sin(a),exponent=form==4?1:form==3?.58f:.45f,r=Shape(t,form);
            return new Vector3(Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),exponent)*width*.5f*r+lean*t*t,t*height,Mathf.Sign(s)*Mathf.Pow(Mathf.Abs(s),exponent)*depth*.5f*r);
        }
        static Vector3 TowerNormal(float a,float t,float width,float depth,float height,float lean,int form)
        {
            var along=TowerPoint(a+.002f,t,width,depth,height,lean,form)-TowerPoint(a-.002f,t,width,depth,height,lean,form);
            var up=TowerPoint(a,t+.002f,width,depth,height,lean,form)-TowerPoint(a,t-.002f,width,depth,height,lean,form);
            return Vector3.Cross(up.normalized,along.normalized).normalized;
        }
        static void Pavilion(SkyPulseMeshBuilder b,Vector3 at,float width,float depth,float height,int world)
        {
            RoundedSlab(b,at+Vector3.up*(height*.48f),width,depth,height*.92f,Shell(world));
            RoundedSlab(b,at+Vector3.up*.04f,width*1.03f,depth*1.03f,.08f,Shadow);
            RoundedSlab(b,at+Vector3.up*height,width*1.09f,depth*1.12f,.10f,Shell(world));
            RoundedSlab(b,at+Vector3.up*(height+.053f),width*1.06f,depth*1.09f,.012f,Light(world));
            for(int i=-2;i<=2;i++)
            {
                var p=at+new Vector3(i*width*.17f,height*.47f,-depth*.505f);
                RoundedSlab(b,p,width*.135f,.032f,height*.55f,Frame);
                RoundedSlab(b,p+new Vector3(0,0,-.019f),width*.111f,.012f,height*.43f,Glass(world));
            }
        }
        static void WindowBay(SkyPulseMeshBuilder b,Vector3 at,Vector3 p,Vector3 q,Vector3 r,Vector3 s,Vector3 na,Vector3 nb,Color wall,Color glass)
        {
            const float left=.13f,right=.87f,bottom=.19f,top=.76f;
            var l0=Vector3.Lerp(p,q,left);var l1=Vector3.Lerp(s,r,left);var r0=Vector3.Lerp(p,q,right);var r1=Vector3.Lerp(s,r,right);
            var a=Vector3.Lerp(l0,l1,bottom);var c=Vector3.Lerp(r0,r1,bottom);var d=Vector3.Lerp(r0,r1,top);var e=Vector3.Lerp(l0,l1,top);
            b.SmoothQuad(at+p,at+l0,at+l1,at+s,na,na,wall);b.SmoothQuad(at+r0,at+q,at+r,at+r1,nb,nb,wall);
            b.SmoothQuad(at+l0,at+r0,at+c,at+a,na,nb,wall);b.SmoothQuad(at+e,at+d,at+r1,at+l1,na,nb,wall);
            var inward=(na+nb).normalized*-.018f;
            WindowReveal(b,at+a,at+c,inward);WindowReveal(b,at+c,at+d,inward);
            WindowReveal(b,at+d,at+e,inward);WindowReveal(b,at+e,at+a,inward);
            b.SmoothQuad(at+a+inward,at+c+inward,at+d+inward,at+e+inward,na,nb,glass);
        }
        static void WindowReveal(SkyPulseMeshBuilder b,Vector3 from,Vector3 to,Vector3 inward)
        {
            var normal=Vector3.Cross(to-from,inward).normalized;
            b.SmoothQuad(from,to,to+inward,from+inward,normal,normal,Shadow);
        }
        static void Turret(SkyPulseMeshBuilder b,Vector3 at,float radius,float height,int world)
        {
            if(world>=12){height*=.85f+Hash(world*13+Mathf.RoundToInt(at.x*31))*.22f;radius*=.94f+Hash(world*53)*.08f;}
            Tower(b,at,radius*2,radius*2,height,0,4,world%12);
            // An overhanging parapet with rounded merlons gives a castle its
            // actual silhouette; these are solid masonry, not glass ornaments.
            for(int i=0;i<10;i++)
            {
                float angle=i*Mathf.PI*.2f;
                RoundedSlab(b,at+new Vector3(Mathf.Cos(angle)*radius*.98f,height+.07f,Mathf.Sin(angle)*radius*.98f),radius*.24f,radius*.24f,.20f,Shell(world));
            }
        }
        static void Gatehouse(SkyPulseMeshBuilder b,Vector3 at,float span,float height,int world)
        {
            var wall=Shell(world);
            Pavilion(b,at+new Vector3(-span*.32f,0,0),span*.33f,.48f,height,world);
            Pavilion(b,at+new Vector3(span*.32f,0,0),span*.33f,.48f,height,world);
            Arch(b,at+new Vector3(0,.07f,-.04f),span*.17f,height*.80f,.12f,0,180,Quaternion.identity,wall);
            Beam(b,at+new Vector3(-span*.49f,height+.04f,0),at+new Vector3(span*.49f,height+.04f,0),.54f,.14f,wall);
            for(int i=-4;i<=4;i++)RoundedSlab(b,at+new Vector3(i*span*.106f,height+.16f,0),span*.063f,.40f,.16f,wall);
        }
        static void Promenade(SkyPulseMeshBuilder b,float rx,float rz,int world)
        {
            RoundedSlab(b,new Vector3(0,.04f,0),rx*1.88f,rz*1.85f,.19f,Shadow);
            RoundedSlab(b,new Vector3(0,.15f,0),rx*1.91f,rz*1.88f,.085f,Shell(world));
            RoundedSlab(b,new Vector3(0,.20f,0),rx*1.85f,rz*1.82f,.025f,new Color(.24f,.28f,.32f,0));
            // Open promenade rail has posts and two slim rails, not a solid disc.
            for(int i=0;i<28;i++)
            {
                float a=i*Mathf.PI*2/28,c=(i+1f)*Mathf.PI*2/28;
                var p=Plan(a,rx*1.85f,rz*1.82f)+Vector3.up*.23f;var q=Plan(c,rx*1.85f,rz*1.82f)+Vector3.up*.23f;
                Beam(b,p,p+Vector3.up*.18f,.012f,.012f,Edge);Beam(b,p+Vector3.up*.17f,q+Vector3.up*.17f,.012f,.015f,Edge);
                if(i%4==0)Beam(b,p+Vector3.up*.025f,q+Vector3.up*.025f,.010f,.012f,Light(world));
            }
        }
        static void Dock(SkyPulseMeshBuilder b,int world)
        {
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
        }
        static void Basin(SkyPulseMeshBuilder b,float rx,float rz,int world)
        {
            RoundedSlab(b,new Vector3(0,-.50f,0),rx*2,rz*2,.85f,new Color(.09f,.17f,.21f,.12f));
            RoundedSlab(b,new Vector3(0,-.03f,-.14f),rx*1.98f,rz*1.98f,.025f,new Color(.04f,.13f,.19f,.34f));
            for(int side=-1;side<=1;side+=2)RoundedSlab(b,new Vector3(side*1.65f,-1.30f,.24f),.72f,1.20f,1.12f,Frame);
        }
        static void Landform(SkyPulseMeshBuilder b,float rx,float rz,float depth,int world)
        {
            world%=12;
            const int sides=36,rows=7;
            for(int row=0;row<rows;row++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1f)*Mathf.PI*2/sides;
                float t=row/(float)rows,u=(row+1f)/rows;
                var p=Rock(a,t,rx,rz,depth,world);var q=Rock(c,t,rx,rz,depth,world);var r=Rock(c,u,rx,rz,depth,world);var s=Rock(a,u,rx,rz,depth,world);
                Color stone=world==1||world==5||world==11?new Color(.30f,.42f,.50f,.12f):world==10?new Color(.35f,.23f,.17f,.12f):world==6?new Color(.17f,.23f,.19f,.12f):new Color(.24f,.18f,.24f,.12f);
                stone*=.76f+Hash(world*97+side*17+row*13)*.32f;stone.a=.12f;b.Quad(p,q,r,s,stone);
                if(row==0)b.Face(new Vector3(0,-.02f,0),q,p,new Color(.18f,.22f,.24f,0));
                if(row==rows-1)b.Face(new Vector3(.22f,-depth,0),s,r,stone);
            }
        }
        static Vector3 Rock(float a,float t,float rx,float rz,float depth,int world)
        {
            float radius=(1-.58f*Mathf.Pow(t,1.18f))*(1+.10f*Mathf.Sin(a*5+world)+.055f*Mathf.Sin(a*11+t*4));
            return new Vector3(Mathf.Cos(a)*rx*radius+.22f*t, -.04f-depth*t+.14f*Mathf.Sin(a*7+world)*t,Mathf.Sin(a)*rz*radius);
        }
        static void Garden(SkyPulseMeshBuilder b,Vector3 at,float span,int world)
        {
            RoundedSlab(b,at,span*2,.30f,.08f,new Color(.16f,.24f,.18f,0));
            for(int i=0;i<5;i++)
            {
                var p=at+new Vector3((i-2)*span*.40f,.10f,Mathf.Sin(i*2)*.08f);
                Beam(b,p,p+Vector3.up*.20f,.024f,.026f,Frame);
                Dome(b,p+Vector3.up*.21f,.15f,.24f,.14f,new Color(.12f,.29f,.18f,.12f),12,5);
                Dome(b,p+Vector3.up*.23f,.11f,.18f,.13f,new Color(.18f,.37f,.23f,.12f),12,5);
            }
        }
        static void Skyway(SkyPulseMeshBuilder b,Vector3 from,Vector3 to,float width,int world)
        {
            Beam(b,from,to,width,.12f,Frame);Beam(b,from+Vector3.up*.13f,to+Vector3.up*.13f,width*.89f,.16f,Glass(world));
            var side=Vector3.Cross((to-from).normalized,Vector3.up).normalized*width*.5f;
            Beam(b,from+side+Vector3.up*.10f,to+side+Vector3.up*.10f,.018f,.018f,Light(world));
            Beam(b,from-side+Vector3.up*.10f,to-side+Vector3.up*.10f,.025f,.025f,Edge);
        }
        static void SolarWing(SkyPulseMeshBuilder b,Vector3 at,int world)
        {
            RoundedSlab(b,at,1.26f,1.55f,.085f,Frame);
            for(int x=0;x<4;x++)for(int z=0;z<5;z++)RoundedSlab(b,at+new Vector3((x-1.5f)*.285f,.055f,(z-2)*.285f),.26f,.26f,.018f,new Color(.06f,.12f,.23f,.34f));
            Beam(b,at+new Vector3(-.58f,.08f,-.68f),at+new Vector3(.58f,.08f,-.68f),.018f,.012f,Light(world));
        }
        static Vector3 Plan(float a,float width,float depth)=>new Vector3(Mathf.Sign(Mathf.Cos(a))*Mathf.Pow(Mathf.Abs(Mathf.Cos(a)),.38f)*width*.5f,0,Mathf.Sign(Mathf.Sin(a))*Mathf.Pow(Mathf.Abs(Mathf.Sin(a)),.38f)*depth*.5f);
        static void RoundedSlab(SkyPulseMeshBuilder b,Vector3 centre,float width,float depth,float height,Color colour)
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
            for(int side=0;side<sides;side++)for(int row=0;row<rows;row++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1f)*Mathf.PI*2/sides,u=row*Mathf.PI/(rows*2),v=(row+1f)*Mathf.PI/(rows*2);
                var p=new Vector3(Mathf.Cos(a)*Mathf.Cos(u)*rx,Mathf.Sin(u)*ry,Mathf.Sin(a)*Mathf.Cos(u)*rz);var q=new Vector3(Mathf.Cos(c)*Mathf.Cos(u)*rx,Mathf.Sin(u)*ry,Mathf.Sin(c)*Mathf.Cos(u)*rz);
                var r=new Vector3(Mathf.Cos(c)*Mathf.Cos(v)*rx,Mathf.Sin(v)*ry,Mathf.Sin(c)*Mathf.Cos(v)*rz);var s=new Vector3(Mathf.Cos(a)*Mathf.Cos(v)*rx,Mathf.Sin(v)*ry,Mathf.Sin(a)*Mathf.Cos(v)*rz);
                b.SmoothQuad(at+p,at+q,at+r,at+s,new Vector3(p.x/rx,p.y/ry,p.z/rz).normalized,new Vector3(q.x/rx,q.y/ry,q.z/rz).normalized,colour);
            }
        }
        static void Arch(SkyPulseMeshBuilder b,Vector3 at,float rx,float ry,float thickness,float start,float sweep,Quaternion rotation,Color colour)
        {
            const int steps=64,sides=8;
            for(int i=0;i<steps;i++)for(int j=0;j<sides;j++)
            {
                float a=(start+sweep*i/steps)*Mathf.Deg2Rad,c=(start+sweep*(i+1f)/steps)*Mathf.Deg2Rad,u=j*Mathf.PI/4,v=(j+1f)*Mathf.PI/4;
                var na=rotation*new Vector3(Mathf.Cos(a)*Mathf.Cos(u),Mathf.Sin(a)*Mathf.Cos(u),Mathf.Sin(u));var nc=rotation*new Vector3(Mathf.Cos(c)*Mathf.Cos(u),Mathf.Sin(c)*Mathf.Cos(u),Mathf.Sin(u));
                b.SmoothQuad(at+rotation*Arc(a,u,rx,ry,thickness),at+rotation*Arc(c,u,rx,ry,thickness),at+rotation*Arc(c,v,rx,ry,thickness),at+rotation*Arc(a,v,rx,ry,thickness),na,nc,colour);
            }
        }
        static Vector3 Arc(float a,float u,float rx,float ry,float t)=>new Vector3(Mathf.Cos(a)*(rx+Mathf.Cos(u)*t),Mathf.Sin(a)*(ry+Mathf.Cos(u)*t),Mathf.Sin(u)*t);
        static void Beam(SkyPulseMeshBuilder b,Vector3 from,Vector3 to,float width,float height,Color colour)
        {
            var direction=(to-from).normalized;var side=Vector3.Cross(direction,Vector3.up).normalized*width*.5f;if(side.sqrMagnitude<.000001f)side=Vector3.right*width*.5f;var up=Vector3.Cross(direction,side.normalized).normalized*height*.5f;
            b.Quad(from-side-up,to-side-up,to+side-up,from+side-up,colour);b.Quad(from-side+up,from+side+up,to+side+up,to-side+up,colour);
            b.Quad(from-side-up,from-side+up,to-side+up,to-side-up,colour);b.Quad(from+side-up,to+side-up,to+side+up,from+side+up,colour);b.Quad(from-side-up,from+side-up,from+side+up,from-side+up,colour);b.Quad(to-side-up,to-side+up,to+side+up,to+side-up,colour);
        }
        static float Hash(int n){uint x=(uint)n*747796405u+2891336453u;x=((x>>((int)(x>>28)+4))^x)*277803737u;return ((x>>22)^x)/4294967295f;}
    }
}
