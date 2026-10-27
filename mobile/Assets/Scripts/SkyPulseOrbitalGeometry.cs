using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
#endif

namespace SkyPulse.Mobile
{
    // Shared station models. Windows reuse the district's actual recessed bays;
    // rings are walkable volumes with fascias and optical edge conductors.
    internal static class SkyPulseOrbitalGeometry
    {
        static readonly Color Hull=new Color(.31f,.35f,.39f,0),Edge=new Color(.58f,.40f,.20f,0),Dark=new Color(.035f,.075f,.11f,.33f),Ice=new Color(.12f,.77f,.95f,.48f),Warm=new Color(.95f,.60f,.28f,.48f),Led=new Color(.18f,.86f,1,1);
        static Mesh Finish(SkyPulseMeshBuilder b,string name)
        {
#if UNITY_EDITOR
            // Authoring retains the builder buffers without changing the
            // protected crystal/shared-builder implementation used by players.
            const BindingFlags fields=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(SkyPulseMeshBuilder);var mesh=new Mesh{name=name};
            mesh.SetVertices((List<Vector3>)type.GetField("v",fields).GetValue(b));mesh.SetNormals((List<Vector3>)type.GetField("n",fields).GetValue(b));mesh.SetColors((List<Color>)type.GetField("c",fields).GetValue(b));mesh.SetTriangles((List<int>)type.GetField("t",fields).GetValue(b),0);mesh.RecalculateBounds();mesh.UploadMeshData(false);return mesh;
#else
            return b.Finish(name);
#endif
        }
        static void Fitting(SkyPulseMeshBuilder b,Vector3 at,Vector3 size,Color colour)=>SkyPulseWorldArchitecture.RoundedSlab(b,at,size.x,size.z,size.y,colour);
        static float Radius(float y)=>1.8f*(1-.16f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.56f,.64f,y))-.21f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.81f,.89f,y)));
        static Vector3 Point(float a,float y)=>new Vector3(Mathf.Cos(a)*Radius(y),y*18,Mathf.Sin(a)*Radius(y));
        static Vector3 Normal(float a,float y){float dr=Radius(y+.001f)-Radius(y-.001f);return new Vector3(Mathf.Cos(a)*.036f,-dr,Mathf.Sin(a)*.036f).normalized;}
        public static Mesh Tower(bool distant)
        {
            var b=new SkyPulseMeshBuilder();int sides=distant?24:40,floors=distant?8:12;
            for(int row=0;row<floors;row++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1f)*Mathf.PI*2/sides,y=row/(float)floors,u=(row+1f)/floors;
                var p=Point(a,y);var q=Point(c,y);var r=Point(c,u);var s=Point(a,u);var na=Normal(a,y);var nb=Normal(c,y);
                if(row>0&&row<floors-1&&side%5!=0)
                {
                    int seed=(side*31+row/2*17)%13;var glass=side%8==3?Ice:seed>9?Warm:Dark;
                    SkyPulseWorldArchitecture.WindowBay(b,Vector3.zero,p,q,r,s,na,nb,Hull,glass);
                }
                else b.SmoothQuad(p,q,r,s,na,nb,Hull);
            }
            foreach(float y in new[]{.2f,4.6f,9.6f,14.4f,17.95f})
            {
                Deck(b,y,Radius(y/18)+.20f,.22f,.20f,distant?48:80);
                if(!distant)for(int k=0;k<12;k++)
                {
                    float angle=k*Mathf.PI/6;var n=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    SkyPulseWorldArchitecture.Beam(b,n*(Radius(y/18)-.03f)+Vector3.up*(y-.50f),n*(Radius(y/18)+.20f)+Vector3.up*y,.12f,.10f,Edge);
                    Fitting(b,n*(Radius(y/18)+.22f)+Vector3.up*(y-.04f),new Vector3(.055f,.065f,.055f),Warm);
                }
            }
            for(int side=0;side<8;side++)
            {
                float a=side*Mathf.PI/4;var at=new Vector3(Mathf.Cos(a)*1.19f,18.16f,Mathf.Sin(a)*1.19f);
                Fitting(b,at,new Vector3(.09f,.36f,.09f),Edge);Fitting(b,at+Vector3.up*.20f,new Vector3(.065f,.045f,.065f),Led);
            }
            Deck(b,18.10f,1.25f,.99f,.16f,64);
            if(!distant)
            {
                // One recognisable observation/service level interrupts the
                // repeated window floors. Its gallery remains inside the
                // existing collar envelope and joins the hull with braces.
                float serviceRadius=Radius(12.25f/18)+.35f;
                Deck(b,12.25f,serviceRadius,.44f,.20f,72);
                for(int bay=0;bay<12;bay++)
                {
                    float a=bay*Mathf.PI/6,c=(bay+.78f)*Mathf.PI/6;var n=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var m=new Vector3(Mathf.Cos(c),0,Mathf.Sin(c));
                    float r=serviceRadius-.08f;
                    SkyPulseWorldArchitecture.WindowBay(b,Vector3.zero,n*r+Vector3.up*11.52f,m*r+Vector3.up*11.52f,m*r+Vector3.up*12.16f,n*r+Vector3.up*12.16f,n,m,Hull,bay%4==0?Warm:Dark);
                    SkyPulseWorldArchitecture.Beam(b,n*(r-.03f)+Vector3.up*11.48f,n*(r-.28f)+Vector3.up*11.10f,.075f,.07f,Edge);
                    SkyPulseWorldArchitecture.Beam(b,n*(r+.025f)+Vector3.up*12.18f,m*(r+.025f)+Vector3.up*12.18f,.025f,.025f,Led);
                }
                for(int fin=0;fin<8;fin++)
                {
                    float a=fin*Mathf.PI/4;var n=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    SkyPulseWorldArchitecture.Beam(b,n*1.18f+Vector3.up*18.12f,n*.82f+Vector3.up*18.72f,.11f,.075f,Hull);
                    SkyPulseWorldArchitecture.Beam(b,n*1.20f+Vector3.up*18.19f,n*.84f+Vector3.up*18.74f,.018f,.018f,Led);
                }
                // External service ribs follow the stepped hull instead of
                // hovering beside it. Recessed vents break up large wall bays.
                for(int side=0;side<12;side++)for(int row=1;row<11;row++)
                {
                    float a=side*Mathf.PI/6;var n=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var tangent=new Vector3(-n.z,0,n.x);
                    float y=row/12f,u=(row+.86f)/12f;
                    SkyPulseWorldArchitecture.Beam(b,Point(a,y)+n*.07f,Point(a,u)+n*.07f,.065f,.075f,Edge);
                    if(row%3==1)
                    {
                        var centre=Point(a+.11f,y+.035f)+n*.07f;
                        for(int vent=0;vent<4;vent++)SkyPulseWorldArchitecture.Beam(b,centre-tangent*.17f+Vector3.up*(vent*.045f),centre+tangent*.17f+Vector3.up*(vent*.045f),.025f,.022f,Dark);
                        Fitting(b,centre+Vector3.up*.24f,new Vector3(.065f,.035f,.065f),Warm);
                    }
                }
                for(int side=0;side<4;side++)
                {
                    float a=side*Mathf.PI*.5f;var n=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    var at=n*1.92f+Vector3.up*7.25f;
                    Fitting(b,at,new Vector3(.42f,.65f,.42f),Hull);Fitting(b,at+n*.13f,new Vector3(.22f,.39f,.22f),Ice);
                    SkyPulseWorldArchitecture.Beam(b,n*1.76f+Vector3.up*6.45f,at-Vector3.up*.25f,.12f,.12f,Edge);
                }
            }
            return Finish(b,distant?"Orbital habitat / distant framed bays":"Orbital habitat / recessed glazing and machined crowns");
        }
        static void Deck(SkyPulseMeshBuilder b,float y,float radius,float width,float depth,int segments)
        {
            float inner=Mathf.Max(.05f,radius-width);Color led=new Color(.17f,.77f,.91f,1);
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,c=(i+1)*Mathf.PI*2/segments;Vector3 n=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),m=new Vector3(Mathf.Cos(c),0,Mathf.Sin(c));
                var p=n*inner+Vector3.up*y;var q=m*inner+Vector3.up*y;var r=m*radius+Vector3.up*y;var s=n*radius+Vector3.up*y;
                b.Quad(p,q,r,s,i%8==0?Edge:Hull);b.Quad(s,r,r-Vector3.up*depth,s-Vector3.up*depth,Edge);
                b.Quad(p-Vector3.up*depth,q-Vector3.up*depth,q,p,Hull);
                b.SmoothQuad(s-Vector3.up*.04f,r-Vector3.up*.04f,r-Vector3.up*.065f,s-Vector3.up*.065f,n,m,led);
            }
        }
        public static Mesh Concourse()
        {
            var b=new SkyPulseMeshBuilder();Deck(b,0,6.2f,.92f,.28f,112);Deck(b,.36f,6.19f,.035f,.045f,112);Deck(b,.36f,5.30f,.035f,.045f,112);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;var n=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                SkyPulseWorldArchitecture.Beam(b,n*1.5f-Vector3.up*.11f,n*5.78f-Vector3.up*.11f,.56f,.22f,Hull);
                SkyPulseWorldArchitecture.Beam(b,n*1.6f-Vector3.up*1.15f,n*5.85f-Vector3.up*.30f,.09f,.12f,Edge);
            }
            for(int i=0;i<56;i++)
            {
                float a=i*Mathf.PI/28;var n=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Fitting(b,n*6.18f+Vector3.up*.18f,new Vector3(.035f,.36f,.035f),Edge);
                if(i%4==0)Fitting(b,n*5.74f+Vector3.down*.20f,new Vector3(.11f,.10f,.11f),Edge);
            }
            return Finish(b,"Orbital concourse / solid deck, parapets and LED fascias");
        }
        public static Mesh Bridge()
        {
            var b=new SkyPulseMeshBuilder();const int count=40;const float half=.42f;
            for(int i=0;i<count;i++)
            {
                float a=i/(float)count,c=(i+1f)/count;var p=new Vector3((a-.5f)*14,Mathf.Sin(a*Mathf.PI)*.45f,Mathf.Sin(a*Mathf.PI)*1.6f);var q=new Vector3((c-.5f)*14,Mathf.Sin(c*Mathf.PI)*.45f,Mathf.Sin(c*Mathf.PI)*1.6f);
                b.Quad(p+Vector3.back*half,q+Vector3.back*half,q+Vector3.forward*half,p+Vector3.forward*half,Hull);
                b.Quad(p+Vector3.forward*half-Vector3.up*.28f,q+Vector3.forward*half-Vector3.up*.28f,q+Vector3.back*half-Vector3.up*.28f,p+Vector3.back*half-Vector3.up*.28f,Dark);
                foreach(int side in new[]{-1,1})
                {
                    var offset=Vector3.forward*(side*half);b.Quad(p+offset,q+offset,q+offset-Vector3.up*.28f,p+offset-Vector3.up*.28f,Edge);
                    b.Quad(p+offset-Vector3.up*.05f,q+offset-Vector3.up*.05f,q+offset-Vector3.up*.14f,p+offset-Vector3.up*.14f,Led);
                    b.Quad(p+offset+Vector3.up*.31f,q+offset+Vector3.up*.31f,q+offset+Vector3.up*.335f,p+offset+Vector3.up*.335f,Led);
                    if(i%4==0)Fitting(b,p+offset+Vector3.up*.15f,new Vector3(.035f,.33f,.035f),Hull);
                }
            }
            // Each end has a physical docking shoe, glazed entry cheeks and a
            // lit canopy. The endpoint sits inside the receiving concourse.
            foreach(int end in new[]{-1,1})
            {
                float x=end*6.75f;
                Fitting(b,new Vector3(x,-.15f,0),new Vector3(1.10f,.30f,1.30f),Hull);
                Fitting(b,new Vector3(x,.02f,0),new Vector3(.72f,.045f,1.15f),Edge);
                foreach(int side in new[]{-1,1})
                {
                    Fitting(b,new Vector3(x,.62f,side*.59f),new Vector3(.85f,1.25f,.14f),Hull);
                    Fitting(b,new Vector3(x,.72f,side*.68f),new Vector3(.49f,.62f,.025f),Ice);
                    Fitting(b,new Vector3(x-end*.45f,.63f,side*.59f),new Vector3(.065f,1.30f,.18f),Edge);
                    Fitting(b,new Vector3(x-end*.49f,.67f,side*.50f),new Vector3(.025f,1.12f,.025f),Led);
                    SkyPulseWorldArchitecture.Beam(b,new Vector3(end*5.1f,-.3f,side*.37f),new Vector3(end*7.05f,-1.05f,side*.49f),.13f,.17f,Edge);
                }
                Fitting(b,new Vector3(x,1.30f,0),new Vector3(1.12f,.18f,1.44f),Hull);
                Fitting(b,new Vector3(x-end*.53f,1.23f,0),new Vector3(.035f,.035f,1.08f),Led);
            }
            return Finish(b,"Curved skybridge / solid deck, docking shoes, glazed entries and braced landings");
        }
        public static Mesh Hub()
        {
            var b=new SkyPulseMeshBuilder();Deck(b,0,3.8f,1.04f,.28f,112);Deck(b,0,2.94f,2.90f,.38f,96);Deck(b,.28f,3.72f,.045f,.055f,112);
            // A low, inhabited observation cabin replaces the long pointed
            // spindle. Windows, roof ribs and service pods give it a purpose.
            for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI/24,c=(i+1)*Mathf.PI/24;var n=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var m=new Vector3(Mathf.Cos(c),0,Mathf.Sin(c));
                SkyPulseWorldArchitecture.WindowBay(b,Vector3.zero,n*2.15f+Vector3.up*.07f,m*2.15f+Vector3.up*.07f,m*1.94f+Vector3.up*1.26f,n*1.94f+Vector3.up*1.26f,n,m,Hull,i%7==0?Warm:Ice);
                b.SmoothQuad(n*2.84f-Vector3.up*.36f,m*2.84f-Vector3.up*.36f,m*1.56f-Vector3.up*.92f,n*1.56f-Vector3.up*.92f,n,m,i%6==0?Edge:Hull);
            }
            Deck(b,1.28f,2.04f,2f,.13f,80);Deck(b,-.93f,1.60f,1.55f,.10f,80);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;var n=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                SkyPulseWorldArchitecture.Beam(b,n*.42f+Vector3.up*1.35f,n*1.95f+Vector3.up*1.35f,.07f,.05f,Edge);
                SkyPulseWorldArchitecture.Beam(b,n*1.62f-Vector3.up*.83f,n*3.52f-Vector3.up*.23f,.17f,.14f,Edge);
                Fitting(b,n*3.28f-Vector3.up*.43f,new Vector3(.30f,.29f,.30f),Hull);
                Fitting(b,n*3.30f-Vector3.up*.48f,new Vector3(.17f,.055f,.17f),Led);
            }
            Fitting(b,new Vector3(0,1.40f,0),new Vector3(.54f,.11f,.54f),Hull);Fitting(b,new Vector3(0,1.47f,0),new Vector3(.30f,.045f,.30f),Led);
            return Finish(b,"Orbital observation terminal / glazed cabin, framed canopy and compact service pods");
        }
        public static Mesh OrbitalRib()
        {
            var b=new SkyPulseMeshBuilder();const int segments=160,sides=12;
            for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
            {
                float a=i*Mathf.PI*2/segments,c=(i+1)*Mathf.PI*2/segments,u=j*Mathf.PI*2/sides,v=(j+1)*Mathf.PI*2/sides;
                var p=RibPoint(a,u);var q=RibPoint(c,u);var r=RibPoint(c,v);var s=RibPoint(a,v);var n=new Vector3(Mathf.Cos(a)*Mathf.Cos(u),Mathf.Sin(a)*Mathf.Cos(u),Mathf.Sin(u));var m=new Vector3(Mathf.Cos(c)*Mathf.Cos(u),Mathf.Sin(c)*Mathf.Cos(u),Mathf.Sin(u));
                var colour=j==2?new Color(.025f,.20f,.27f,1):j==3?Edge:Hull;b.SmoothQuad(p,q,r,s,n,m,colour);
            }
            return Finish(b,"Distant orbital megastructure / solid curved rib and optical service lane");
        }
        static Vector3 RibPoint(float a,float u)=>new Vector3(Mathf.Cos(a)*(1+Mathf.Cos(u)*.022f),Mathf.Sin(a)*(1+Mathf.Cos(u)*.022f),Mathf.Sin(u)*.022f);
        public static Mesh Apron()
        {
            var b=new SkyPulseMeshBuilder();Fitting(b,new Vector3(0,-.25f,12.5f),new Vector3(30,.48f,30),Hull);
            for(int x=0;x<8;x++)for(int z=0;z<8;z++)Fitting(b,new Vector3((x-3.5f)*3.75f,.005f,z*3.75f-.625f),new Vector3(3.72f,.035f,3.72f),new Color(.24f,.30f,.34f,.60f));
            for(int side=-1;side<=1;side+=2)
            {
                Fitting(b,new Vector3(side*14.7f,.05f,12.5f),new Vector3(.45f,.24f,30),Edge);
                SkyPulseWorldArchitecture.Beam(b,new Vector3(side*14.42f,.16f,-2.4f),new Vector3(side*14.42f,.16f,27.4f),.065f,.035f,Led);
                for(int z=0;z<10;z++)
                {
                    float at=z*3f-1;
                    Fitting(b,new Vector3(side*14.85f,-.27f,at),new Vector3(.55f,.28f,.52f),Dark);
                    Fitting(b,new Vector3(side*14.9f,-.22f,at),new Vector3(.08f,.10f,.29f),Warm);
                    SkyPulseWorldArchitecture.Beam(b,new Vector3(side*14.6f,-.4f,at),new Vector3(side*12.7f,-1.35f,at),.20f,.22f,Hull);
                }
                for(int z=0;z<8;z++)
                {
                    float at=z*3.6f;
                    Fitting(b,new Vector3(side*4.6f,.035f,at),new Vector3(.28f,.035f,1.15f),Led);
                    Fitting(b,new Vector3(side*10.3f,.045f,at+1.1f),new Vector3(2.1f,.045f,1.2f),Edge);
                    Fitting(b,new Vector3(side*10.3f,.073f,at+1.1f),new Vector3(1.91f,.035f,1.01f),Dark);
                    for(int vent=0;vent<5;vent++)Fitting(b,new Vector3(side*10.3f,.10f,at+.78f+vent*.16f),new Vector3(1.56f,.025f,.035f),Hull);
                }
            }
            for(int i=0;i<5;i++)SkyPulseWorldArchitecture.Beam(b,new Vector3(-14.4f,.045f,i*6),new Vector3(14.4f,.045f,i*6),.035f,.025f,Edge);
            Fitting(b,new Vector3(0,.11f,-2.35f),new Vector3(29,.26f,.42f),Hull);
            return Finish(b,"Arrival deck / framed edges, recessed service hatches, guidance lights and cantilever supports");
        }
        public static Mesh SkyBeacons()
        {
            const int count=28;var vertices=new Vector3[count*4];var uv=new Vector2[count*4];var colours=new Color[count*4];var triangles=new int[count*6];
            for(int i=0;i<count;i++)
            {
                // Deterministic placement leaves the gameplay RNG untouched.
                float phase=Mathf.Repeat(i*.618034f,1f),x=-25+Mathf.Repeat(i*.381966f+.11f,1)*50,y=-25+phase*60,z=(i%4)*4;
                float radius=.45f+(i%5)*.09f;
                for(int corner=0;corner<4;corner++){int k=i*4+corner;float a=corner==0||corner==3?-1:1,b=corner<2?-1:1;vertices[k]=new Vector3(x+a*radius*1.7f,y+b*radius*1.7f,z);uv[k]=new Vector2((a+1)*.5f,(b+1)*.5f);colours[k]=new Color(i%5==0?1:0,phase,1,1);}
                int t=i*6,v=i*4;triangles[t]=v;triangles[t+1]=v+2;triangles[t+2]=v+1;triangles[t+3]=v;triangles[t+4]=v+3;triangles[t+5]=v+2;
            }
            var mesh=new Mesh{name="Orbital sky / twenty-eight luminous LED star lenses"};mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colours;mesh.triangles=triangles;mesh.RecalculateBounds();
#if UNITY_EDITOR
            mesh.UploadMeshData(false);
#else
            mesh.UploadMeshData(true);
#endif
            return mesh;
        }
        public static Mesh Sphere(string name)
        {
            var b=new SkyPulseMeshBuilder();const int sides=72,rows=40;
            for(int row=0;row<rows;row++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1f)*Mathf.PI*2/sides,u=row*Mathf.PI/rows,v=(row+1f)*Mathf.PI/rows;
                var p=Spherical(a,u);var q=Spherical(c,u);var r=Spherical(c,v);var s=Spherical(a,v);b.SmoothQuad(p,q,r,s,p,q,Color.white);
            }
            return Finish(b,name);
        }
        static Vector3 Spherical(float a,float b)=>new Vector3(Mathf.Cos(a)*Mathf.Sin(b),Mathf.Cos(b),Mathf.Sin(a)*Mathf.Sin(b));
    }
}
