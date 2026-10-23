using UnityEngine;

namespace SkyPulse.Mobile
{
    // Construction-only geometry. Each world has its own structural vocabulary,
    // not a recolour of the Neon City island. Runtime pooling lives in the
    // environment component; no buildings are instantiated during flight.
    internal static class SkyPulseWorldArchitecture
    {
        internal static readonly string[] Styles={
            "Neon City / inhabited rock islands", "Aurora Rise / swept sky bridges",
            "Solar Drift / radial solar docks", "Midnight Tide / tidal sail towers",
            "Velvet Dawn / rose observatory gardens", "Crystal Night / prismatic citadels",
            "Jade Horizon / terraced garden sanctuaries", "Violet Rain / cantilever megacity",
            "Eclipse / obsidian crescent monuments", "Cobalt Storm / braced energy stations",
            "Amber Skies / copper mesa observatories", "Polar Glow / ice-arch habitats"
        };
        static readonly Color[] Energy={
            new Color(.20f,.83f,1,1),new Color(.31f,1,.78f,1),new Color(1,.69f,.22f,1),new Color(.15f,.61f,1,1),
            new Color(1,.39f,.72f,1),new Color(.68f,.88f,1,1),new Color(.25f,.94f,.59f,1),new Color(.74f,.39f,1,1),
            new Color(.85f,.56f,1,1),new Color(.22f,.51f,1,1),new Color(1,.67f,.29f,1),new Color(.39f,.95f,1,1)
        };
        static Color Pane(Color c)=>new Color(c.r*.32f+.04f,c.g*.32f+.05f,c.b*.32f+.10f,.34f);
        public static Mesh Build(int theme)
        {
            theme=Mathf.Clamp(theme,0,11);if(theme==0)return NeonIsland(0);
            var b=new SkyPulseMeshBuilder();var light=Energy[theme];var glass=Pane(light);
            var metal=theme==2||theme==10?new Color(.53f,.36f,.22f,0):theme==11?new Color(.53f,.65f,.72f,0):new Color(.29f,.34f,.43f,0);
            switch(theme)
            {
                case 1: // Aurora: airborne terraces joined by a sweeping bridge.
                    Ice(b,new Vector3(-.86f,-.4f,0),1.3f,1.55f,glass);Ice(b,new Vector3(.94f,-.35f,.1f),1.1f,1.8f,glass);
                    Loft(b,new Vector3(-.85f,.12f,0),.57f,.48f,4.7f,.47f,1,metal,glass,light);
                    Loft(b,new Vector3(.92f,.12f,.08f),.58f,.49f,3.35f,-.40f,1,metal,glass,light);
                    Arch(b,new Vector3(0,1.15f,.03f),1.28f,.63f,.09f,0,180,Quaternion.identity,metal);
                    Arch(b,new Vector3(0,1.15f,-.08f),1.28f,.63f,.016f,0,180,Quaternion.identity,light);
                    Terrace(b,new Vector3(0,-.08f,.18f),2.08f,.66f,0,.11f,metal);break;
                case 2: // Solar: radial collector wings and a mechanical station.
                    Plinth(b,2.02f,1.2f,-.44f,metal,light);Mast(b,new Vector3(0,-2.15f,0),.18f,2.3f,metal,light);
                    Loft(b,new Vector3(0,.10f,0),.62f,.63f,4.65f,0,2,metal,glass,light);
                    for(int wing=0;wing<6;wing++)
                    {
                        float a=wing*Mathf.PI/3;var from=new Vector3(Mathf.Cos(a)*.78f,.19f,Mathf.Sin(a)*.78f);var to=new Vector3(Mathf.Cos(a)*2.15f,.31f,Mathf.Sin(a)*1.45f);
                        Link(b,from,to,.48f,.065f,glass);Link(b,from+Vector3.up*.04f,to+Vector3.up*.04f,.028f,.014f,light);
                    }
                    Arch(b,new Vector3(0,2.45f,0),1.1f,.38f,.035f,0,360,Quaternion.Euler(16,0,0),light);break;
                case 3: // Midnight Tide: glass sails on grounded water caissons.
                    Plinth(b,2.05f,1.25f,-14,metal,light);
                    Loft(b,new Vector3(-.94f,.08f,.05f),.76f,.63f,3.1f,.65f,1,metal,glass,light,-14);
                    Loft(b,new Vector3(0,.08f,.18f),.78f,.65f,4.8f,.72f,1,metal,glass,light,-14);
                    Loft(b,new Vector3(1.05f,.08f,.14f),.67f,.59f,2.55f,.58f,1,metal,glass,light,-14);
                    Arch(b,new Vector3(0,.30f,-.48f),1.88f,.56f,.075f,0,180,Quaternion.identity,metal);
                    Arch(b,new Vector3(0,.30f,-.56f),1.88f,.56f,.012f,0,180,Quaternion.identity,light);break;
                case 4: // Velvet: a domed rose palace with high garden terraces.
                    Plinth(b,2.10f,1.30f,-14,metal,light);
                    Loft(b,new Vector3(0,.13f,.28f),1.25f,.90f,3.65f,0,3,metal,glass,light,-14);
                    Dome(b,new Vector3(0,3.70f,.28f),.68f,.67f,.49f,metal);
                    Mast(b,new Vector3(0,4.18f,.28f),.035f,.9f,metal,light);
                    for(int side=-1;side<=1;side+=2){Loft(b,new Vector3(side*1.10f,.13f,0),.53f,.49f,2.0f,side*.10f,3,metal,glass,light,-14);Dome(b,new Vector3(side*1.10f,2.12f,0),.34f,.40f,.32f,glass);}
                    Garden(b,new Vector3(0,.22f,-.66f),1.72f,light);break;
                case 5: // Crystal: faceted volumes, not a conventional skyline.
                    Ice(b,new Vector3(0,-.16f,0),2.04f,2.05f,glass);
                    Crystal(b,new Vector3(-1.03f,.04f,.08f),.49f,2.8f,-14,glass,light);
                    Crystal(b,new Vector3(-.12f,.06f,.12f),.63f,5.1f,5,glass,light);
                    Crystal(b,new Vector3(.98f,.03f,0),.43f,3.5f,17,glass,light);
                    Crystal(b,new Vector3(.43f,.03f,-.61f),.33f,1.9f,-19,glass,light);break;
                case 6: // Jade: planted, stepped sanctuaries and open balconies.
                    Plinth(b,2.05f,1.35f,-14,new Color(.18f,.27f,.24f,0),light);
                    for(int tier=0;tier<4;tier++)
                    {
                        float radius=1.45f-tier*.28f,y=.13f+tier*.83f;
                        Loft(b,new Vector3(0,y,.12f),radius*1.1f,radius*.82f,.73f,0,3,metal,glass,light,tier==0?-14:0);
                        Terrace(b,new Vector3(0,y+.71f,.12f),radius,radius*.65f,0,.11f,new Color(.24f,.39f,.30f,0));
                        Terrace(b,new Vector3(0,y+.83f,.12f),radius*1.01f,radius*.66f,0,.018f,light);
                    }
                    Mast(b,new Vector3(0,3.6f,.12f),.06f,.95f,metal,light);Garden(b,new Vector3(0,.18f,-.70f),1.75f,light);break;
                case 7: // Violet: offset megatowers and inhabited sky bridges.
                    Plinth(b,2.10f,1.30f,-14,metal,light);
                    Loft(b,new Vector3(-1.10f,.10f,.08f),.77f,.64f,4.65f,-.22f,0,metal,glass,light,-14);
                    Loft(b,new Vector3(.12f,.10f,.36f),.95f,.78f,3.45f,.18f,0,metal,glass,light,-14);
                    Loft(b,new Vector3(1.13f,.10f,0),.72f,.67f,5.35f,.24f,0,metal,glass,light,-14);
                    Link(b,new Vector3(-1.06f,2.6f,-.08f),new Vector3(1.15f,3.08f,-.08f),.45f,.14f,glass);
                    Link(b,new Vector3(-1.06f,2.68f,-.34f),new Vector3(1.15f,3.16f,-.34f),.022f,.018f,light);break;
                case 8: // Eclipse: a crescent sanctuary and leaning obelisks.
                    Plinth(b,2.04f,1.20f,-14,new Color(.13f,.10f,.20f,0),light);
                    Arch(b,new Vector3(0,1.55f,.35f),1.72f,2.15f,.12f,-45,270,Quaternion.Euler(0,-18,0),new Color(.25f,.20f,.33f,0));
                    Arch(b,new Vector3(0,1.55f,.26f),1.55f,2.02f,.018f,-45,270,Quaternion.Euler(0,-18,0),light);
                    Loft(b,new Vector3(-.70f,.12f,0),.39f,.40f,3.8f,-.3f,2,metal,glass,light,-14);
                    Loft(b,new Vector3(.67f,.12f,.03f),.43f,.43f,4.95f,.28f,2,metal,glass,light,-14);break;
                case 9: // Cobalt: braced storm pylons and a suspended generator.
                    Plinth(b,2.05f,1.23f,-14,metal,light);
                    for(int side=-1;side<=1;side+=2)
                    {
                        Mast(b,new Vector3(side*1.22f,.08f,0),.16f,4.75f,metal,light,-14);
                        for(int j=0;j<4;j++)Link(b,new Vector3(side*1.22f,.4f+j,0),new Vector3(-side*1.22f,1.3f+j,0),.055f,.055f,metal);
                    }
                    Arch(b,new Vector3(0,2.30f,-.08f),.77f,.77f,.14f,0,360,Quaternion.Euler(0,20,0),metal);
                    Arch(b,new Vector3(0,2.30f,-.24f),.62f,.62f,.025f,0,360,Quaternion.Euler(0,20,0),light);
                    Crystal(b,new Vector3(0,1.93f,-.08f),.28f,1.10f,0,glass,light);break;
                case 10: // Amber: broad copper observatories on a sandstone mesa.
                    Plinth(b,2.15f,1.37f,-14,new Color(.40f,.25f,.17f,0),light);
                    Loft(b,new Vector3(0,.13f,.22f),1.5f,1.05f,2.9f,0,3,metal,glass,light,-14);
                    Dome(b,new Vector3(0,2.94f,.22f),.88f,.78f,.65f,metal);Mast(b,new Vector3(0,3.6f,.22f),.045f,1.20f,metal,light);
                    for(int side=-1;side<=1;side+=2){Mast(b,new Vector3(side*1.38f,.13f,-.08f),.22f,1.45f,metal,light);Dome(b,new Vector3(side*1.38f,1.58f,-.08f),.40f,.34f,.37f,metal);}
                    for(int pillar=-3;pillar<=3;pillar++)Mast(b,new Vector3(pillar*.42f,.13f,-.87f),.055f,1.08f,metal,light);
                    Link(b,new Vector3(-1.5f,1.22f,-.87f),new Vector3(1.5f,1.22f,-.87f),.18f,.12f,metal);break;
                case 11: // Polar: white ribbed arch habitats above an ice shelf.
                    Plinth(b,2.1f,1.35f,-14,new Color(.29f,.46f,.56f,.3f),light);
                    for(int i=0;i<3;i++)Arch(b,new Vector3(0,.12f,(i-1)*.44f),1.48f-i*.12f,4.45f-i*.21f,.075f,0,180,Quaternion.identity,metal);
                    Arch(b,new Vector3(0,.12f,-.51f),1.50f,4.47f,.016f,0,180,Quaternion.identity,light);
                    for(int side=-1;side<=1;side+=2){Loft(b,new Vector3(side*.78f,.15f,0),.54f,.63f,1.65f,0,3,metal,glass,light,-14);Dome(b,new Vector3(side*.78f,1.78f,0),.40f,.45f,.42f,glass);}
                    Terrace(b,new Vector3(0,2.75f,0),.51f,.38f,0,.11f,metal);break;
            }
            return b.Finish(Styles[theme]);
        }
        static void Plinth(SkyPulseMeshBuilder b,float rx,float rz,float bottom,Color colour,Color light)
        {
            if(bottom> -10)Terrace(b,Vector3.zero,rx*.91f,rz*.91f,bottom,-bottom+.04f,colour);
            Terrace(b,Vector3.zero,rx,rz,.04f,.105f,colour);
            Terrace(b,Vector3.zero,rx*1.005f,rz*1.005f,.15f,.012f,light);
        }
        static void Ice(SkyPulseMeshBuilder b,Vector3 at,float radius,float depth,Color glass)
        {
            const int sides=12;
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,c=(i+1)*Mathf.PI*2/sides;var p=at+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius*.65f);var q=at+new Vector3(Mathf.Cos(c)*radius,0,Mathf.Sin(c)*radius*.65f);
                b.Face(at+Vector3.up*.08f,q,p,glass);b.Quad(p,q,at+new Vector3(Mathf.Cos(c)*radius*.61f,-depth,Mathf.Sin(c)*radius*.45f),at+new Vector3(Mathf.Cos(a)*radius*.61f,-depth,Mathf.Sin(a)*radius*.45f),glass);
            }
        }
        static void Loft(SkyPulseMeshBuilder b,Vector3 at,float width,float depth,float height,float lean,int form,Color metal,Color glass,Color light,float foundation=0)
        {
            const int sides=24;var levels=form==1?new[]{new Vector2(1,0),new Vector2(.99f,.1f),new Vector2(.90f,.35f),new Vector2(.66f,.65f),new Vector2(.3f,.86f),new Vector2(.025f,1)}:
                form==2?new[]{new Vector2(1.2f,0),new Vector2(.92f,.12f),new Vector2(.85f,.65f),new Vector2(.57f,.82f),new Vector2(.025f,1)}:
                form==3?new[]{new Vector2(1,0),new Vector2(1,.08f),new Vector2(.88f,.12f),new Vector2(.88f,.84f),new Vector2(1.05f,.9f),new Vector2(.97f,1)}:
                new[]{new Vector2(1.22f,0),new Vector2(1.15f,.08f),new Vector2(.98f,.11f),new Vector2(.98f,.6f),new Vector2(.72f,.62f),new Vector2(.72f,.84f),new Vector2(.50f,.85f),new Vector2(.46f,.97f),new Vector2(.13f,1.06f)};
            if(foundation<0)
            {
                var grounded=new Vector2[levels.Length+1];grounded[0]=new Vector2(levels[0].x,foundation/height);System.Array.Copy(levels,0,grounded,1,levels.Length);levels=grounded;
            }
            for(int row=1;row<levels.Length;row++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1)*Mathf.PI*2/sides;var lo=levels[row-1];var hi=levels[row];
                Vector3 p=LoftPoint(a,lo,width,depth,height,lean,form),q=LoftPoint(c,lo,width,depth,height,lean,form),r=LoftPoint(c,hi,width,depth,height,lean,form),t=LoftPoint(a,hi,width,depth,height,lean,form);
                b.SmoothQuad(at+p,at+q,at+r,at+t,new Vector3(Mathf.Cos(a),.1f,Mathf.Sin(a)).normalized,new Vector3(Mathf.Cos(c),.1f,Mathf.Sin(c)).normalized,foundation<0&&row==1?glass:row==1||row%3==0?metal:glass);
                if(side%6==0){var e=LoftPoint(a+.025f,lo,width*1.015f,depth*1.015f,height,lean,form);var f=LoftPoint(a+.025f,hi,width*1.015f,depth*1.015f,height,lean,form);b.Quad(at+p,at+e,at+f,at+t,light);}
                if(row==levels.Length-1)b.Face(at+new Vector3(lean*hi.y*hi.y,hi.y*height,0),at+t,at+r,metal);
            }
            for(int row=1;row<16;row++)
            {
                float y=row*.05f;float radius=1;
                for(int l=1;l<levels.Length;l++)if(y>=levels[l-1].y&&y<=levels[l].y){radius=Mathf.Lerp(levels[l-1].x,levels[l].x,Mathf.InverseLerp(levels[l-1].y,levels[l].y,y));break;}
                Terrace(b,at+new Vector3(lean*y*y,0,0),width*radius*.505f,depth*radius*.505f,y*height,.008f,light);
            }
        }
        static Vector3 LoftPoint(float a,Vector2 p,float width,float depth,float height,float lean,int form)
        {
            float exponent=form==1?1:.42f;
            return new Vector3(Mathf.Sign(Mathf.Cos(a))*Mathf.Pow(Mathf.Abs(Mathf.Cos(a)),exponent)*width*p.x*.5f+lean*Mathf.Pow(Mathf.Max(0,p.y),2),p.y*height,Mathf.Sign(Mathf.Sin(a))*Mathf.Pow(Mathf.Abs(Mathf.Sin(a)),exponent)*depth*p.x*.5f);
        }
        static void Mast(SkyPulseMeshBuilder b,Vector3 at,float radius,float height,Color metal,Color light,float foundation=0)
        {
            Terrace(b,at,radius,radius,foundation,height-foundation,metal);Terrace(b,at,radius*1.13f,radius*1.13f,height*.92f,.025f,light);
        }
        static void Dome(SkyPulseMeshBuilder b,Vector3 at,float rx,float ry,float rz,Color colour)
        {
            const int sides=32,rows=10;
            for(int i=0;i<sides;i++)for(int row=0;row<rows;row++)
            {
                float a=i*Mathf.PI*2/sides,c=(i+1)*Mathf.PI*2/sides,u=row*Mathf.PI/(rows*2),v=(row+1)*Mathf.PI/(rows*2);
                b.SmoothQuad(at+Round(a,u,rx,ry,rz),at+Round(c,u,rx,ry,rz),at+Round(c,v,rx,ry,rz),at+Round(a,v,rx,ry,rz),new Vector3(Mathf.Cos(a)*Mathf.Cos(u),Mathf.Sin(u),Mathf.Sin(a)*Mathf.Cos(u)),new Vector3(Mathf.Cos(c)*Mathf.Cos(u),Mathf.Sin(u),Mathf.Sin(c)*Mathf.Cos(u)),colour);
            }
        }
        static Vector3 Round(float a,float u,float rx,float ry,float rz)=>new Vector3(Mathf.Cos(a)*Mathf.Cos(u)*rx,Mathf.Sin(u)*ry,Mathf.Sin(a)*Mathf.Cos(u)*rz);
        static void Crystal(SkyPulseMeshBuilder b,Vector3 at,float radius,float height,float angle,Color glass,Color light)
        {
            var rotation=Quaternion.Euler(0,0,angle);
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3,c=(i+1)*Mathf.PI/3;var p=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);var q=new Vector3(Mathf.Cos(c)*radius,0,Mathf.Sin(c)*radius);
                b.Quad(at+rotation*p,at+rotation*q,at+rotation*(q+Vector3.up*height*.78f),at+rotation*(p+Vector3.up*height*.78f),glass);
                b.Face(at+rotation*(p+Vector3.up*height*.78f),at+rotation*(q+Vector3.up*height*.78f),at+rotation*(Vector3.up*height),glass);
                Link(b,at+rotation*p,at+rotation*(p+Vector3.up*height*.78f),.013f,.013f,light);
            }
        }
        static void Garden(SkyPulseMeshBuilder b,Vector3 at,float span,Color light)
        {
            for(int i=0;i<7;i++)
            {
                var p=at+new Vector3((i-3)*span/3,0,Mathf.Sin(i*2)*.15f);Mast(b,p,.025f,.20f,Metal,light);Dome(b,p+Vector3.up*.20f,.16f,.23f,.16f,new Color(.15f,.42f,.28f,0));
            }
        }
        static void Arch(SkyPulseMeshBuilder b,Vector3 at,float rx,float ry,float thickness,float start,float sweep,Quaternion rotation,Color colour)
        {
            const int steps=64,sides=8;
            for(int i=0;i<steps;i++)for(int j=0;j<sides;j++)
            {
                float a=(start+sweep*i/steps)*Mathf.Deg2Rad,c=(start+sweep*(i+1)/steps)*Mathf.Deg2Rad,u=j*Mathf.PI/4,v=(j+1)*Mathf.PI/4;
                var na=rotation*new Vector3(Mathf.Cos(a)*Mathf.Cos(u),Mathf.Sin(a)*Mathf.Cos(u),Mathf.Sin(u));var nc=rotation*new Vector3(Mathf.Cos(c)*Mathf.Cos(u),Mathf.Sin(c)*Mathf.Cos(u),Mathf.Sin(u));
                b.SmoothQuad(at+rotation*ArcPoint(a,u,rx,ry,thickness),at+rotation*ArcPoint(c,u,rx,ry,thickness),at+rotation*ArcPoint(c,v,rx,ry,thickness),at+rotation*ArcPoint(a,v,rx,ry,thickness),na,nc,colour);
            }
        }
        static Vector3 ArcPoint(float a,float u,float rx,float ry,float thickness)=>new Vector3(Mathf.Cos(a)*(rx+Mathf.Cos(u)*thickness),Mathf.Sin(a)*(ry+Mathf.Cos(u)*thickness),Mathf.Sin(u)*thickness);
        static void Link(SkyPulseMeshBuilder b,Vector3 a,Vector3 c,float width,float height,Color colour)
        {
            var side=Vector3.Cross((c-a).normalized,Vector3.up).normalized*width*.5f;if(side.sqrMagnitude<.00001f)side=Vector3.right*width*.5f;var up=Vector3.up*height;
            b.Quad(a-side,c-side,c+side,a+side,colour);b.Quad(a-side+up,a+side+up,c+side+up,c-side+up,colour);b.Quad(a-side,a-side+up,c-side+up,c-side,colour);b.Quad(a+side,c+side,c+side+up,a+side+up,colour);
        }
        static float Hash(int n){uint x=(uint)n*747796405u+2891336453u;x=((x>>((int)(x>>28)+4))^x)*277803737u;return ((x>>22)^x)/4294967295f;}
        // Each cached district is a floating landform with an inhabited crown.
        // Rock, terraces, glazing and light reveals share one vertex-classified
        // material; no cubes are instantiated and no flight collision is added.
        static readonly Color Metal=new Color(.31f,.29f,.44f,0),Glass=new Color(.10f,.22f,.36f,.34f),Light=new Color(.30f,.82f,1,1);
        static Mesh NeonIsland(int theme)
        {
            var b=new SkyPulseMeshBuilder();float radius=1.65f+Hash(theme*17)*.38f;
            const int sides=40;
            var levels=new[]{new Vector2(.08f,-2.05f),new Vector2(.32f,-1.72f),new Vector2(.57f,-1.22f),new Vector2(.80f,-.65f),new Vector2(.96f,-.25f),new Vector2(1,.02f),new Vector2(.94f,.14f)};
            for(int row=1;row<levels.Length;row++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1)*Mathf.PI*2/sides;
                var lo=levels[row-1];var hi=levels[row];
                var colour=Color.Lerp(new Color(.12f,.085f,.22f,0),new Color(.34f,.20f,.38f,0),row/(float)levels.Length);
                colour*=.82f+Hash(theme*37+side*7+row)*.30f;colour.a=0;
                b.SmoothQuad(Land(a,lo,radius,theme),Land(c,lo,radius,theme),Land(c,hi,radius,theme),Land(a,hi,radius,theme),new Vector3(Mathf.Cos(a),-.3f,Mathf.Sin(a)).normalized,new Vector3(Mathf.Cos(c),-.3f,Mathf.Sin(c)).normalized,colour);
                if(row==levels.Length-1)b.Face(new Vector3(0,.14f,0),Land(a,hi,radius,theme),Land(c,hi,radius,theme),new Color(.23f,.26f,.34f,0));
                // Mineral seams pick up the violet light between strata.
                if(row==3&&side%4==0)b.Quad(Land(a,lo,radius*1.005f,theme),Land(c,lo,radius*1.005f,theme),Land(c,new Vector2(lo.x,lo.y+.025f),radius*1.005f,theme),Land(a,new Vector2(lo.x,lo.y+.025f),radius*1.005f,theme),new Color(.58f,.20f,.82f,.85f));
            }
            Terrace(b,Vector3.zero,radius*.96f,radius*.57f,.18f,.105f,new Color(.44f,.38f,.48f,0));
            Terrace(b,Vector3.zero,radius*.97f,radius*.58f,.215f,.012f,new Color(.94f,.55f,.34f,.94f));
            Terrace(b,new Vector3(0,.24f,0),radius*.72f,radius*.45f,.02f,.13f,Metal);
            Terrace(b,new Vector3(0,.38f,0),radius*.73f,radius*.46f,0,.012f,Light);
            // A varied cluster: tapered central spire, curved satellite towers,
            // and an occupied circular sky terrace, all standing on the island.
            Building(b,new Vector3(0,.40f,.16f),.62f,.55f,3.4f+Hash(theme*9)*1.0f,theme);
            Building(b,new Vector3(-.74f,.27f,0),.42f,.43f,1.65f+Hash(theme*31)*.9f,theme+1);
            Building(b,new Vector3(.78f,.27f,.12f),.40f,.38f,2.0f+Hash(theme*13)*.9f,theme+2);
            Building(b,new Vector3(.42f,.26f,-.48f),.30f,.30f,1.15f,theme+3);
            Terrace(b,new Vector3(-.12f,.49f,-.34f),radius*.69f,radius*.37f,0,.035f,Glass);
            for(int post=0;post<12;post++)
            {
                float angle=post*Mathf.PI*2/12;var at=new Vector3(Mathf.Cos(angle)*radius*.88f,.26f,Mathf.Sin(angle)*radius*.52f);
                Terrace(b,at,.022f,.022f,0,.13f,Metal);Terrace(b,at,.025f,.025f,.13f,.025f,Light);
            }
            return b.Finish(Styles[0]);
        }
        static Vector3 Land(float angle,Vector2 level,float radius,int theme)
        {
            float contour=1+.065f*Mathf.Sin(angle*5+theme)+.035f*Mathf.Sin(angle*9-theme);
            return new Vector3(Mathf.Cos(angle)*radius*level.x*contour+.14f*(1-level.x),level.y+.035f*Mathf.Sin(angle*7+theme),Mathf.Sin(angle)*radius*.60f*level.x*contour);
        }
        static void Terrace(SkyPulseMeshBuilder b,Vector3 centre,float rx,float rz,float bottom,float height,Color colour)
        {
            const int sides=40;
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,c=(i+1)*Mathf.PI*2/sides;
                var p=new Vector3(Mathf.Cos(a)*rx,bottom,Mathf.Sin(a)*rz);var q=new Vector3(Mathf.Cos(c)*rx,bottom,Mathf.Sin(c)*rz);
                b.SmoothQuad(centre+p,centre+q,centre+q+Vector3.up*height,centre+p+Vector3.up*height,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),new Vector3(Mathf.Cos(c),0,Mathf.Sin(c)),colour);
                b.Face(centre+Vector3.up*(bottom+height),centre+p+Vector3.up*height,centre+q+Vector3.up*height,colour);
            }
        }
        static void Building(SkyPulseMeshBuilder b,Vector3 centre,float width,float depth,float height,int variant)
        {
            const int sides=24;var profile=new[]{new Vector2(1.30f,0),new Vector2(1.30f,.055f),new Vector2(1,.12f),new Vector2(.96f,.64f),new Vector2(.76f,.65f),new Vector2(.76f,.82f),new Vector2(.48f,.84f),new Vector2(.44f,.96f),new Vector2(.035f,1.12f)};
            float sweep=(variant%2==0?1:-1)*width*.14f;
            for(int row=1;row<profile.Length;row++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1)*Mathf.PI*2/sides;var lo=profile[row-1];var hi=profile[row];
                var p=TowerPoint(a,lo,width,depth,height,sweep);var q=TowerPoint(c,lo,width,depth,height,sweep);var r=TowerPoint(c,hi,width,depth,height,sweep);var t=TowerPoint(a,hi,width,depth,height,sweep);
                var hue=row==2||row==5?new Color(.43f,.36f,.49f,0):Glass;
                b.SmoothQuad(centre+p,centre+q,centre+r,centre+t,new Vector3(Mathf.Cos(a),.12f,Mathf.Sin(a)).normalized,new Vector3(Mathf.Cos(c),.12f,Mathf.Sin(c)).normalized,hue);
                if(side%6==0)
                {
                    var near=TowerPoint(a+.032f,lo,width*1.009f,depth*1.009f,height,sweep);var far=TowerPoint(a+.032f,hi,width*1.009f,depth*1.009f,height,sweep);
                    b.Quad(centre+p,centre+near,centre+far,centre+t,variant%2==0?Light:new Color(.91f,.25f,.70f,1));
                }
            }
            for(int row=1;row<17;row++)
            {
                float y=row*.032f+.09f,rad=Mathf.Lerp(1,.89f,y/.60f);
                Terrace(b,centre+new Vector3(sweep*y*y,0,0),width*rad*.505f,depth*rad*.505f,y*height,.008f,row%4==0?new Color(.98f,.63f,.31f,.9f):Light);
            }
            Terrace(b,centre+new Vector3(sweep*.79f*.79f,0,0),width*.34f,depth*.34f,height*.79f,.014f,new Color(1,.56f,.29f,1));
        }
        static Vector3 TowerPoint(float angle,Vector2 profile,float width,float depth,float height,float sweep)=>new Vector3(Mathf.Sign(Mathf.Cos(angle))*Mathf.Pow(Mathf.Abs(Mathf.Cos(angle)),.48f)*width*profile.x*.5f+sweep*profile.y*profile.y,profile.y*height,Mathf.Sign(Mathf.Sin(angle))*Mathf.Pow(Mathf.Abs(Mathf.Sin(angle)),.48f)*depth*profile.x*.5f);

    }
}
