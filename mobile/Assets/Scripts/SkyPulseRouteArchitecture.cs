using UnityEngine;

namespace SkyPulse.Mobile
{
    // One cached shaft/collar pair per world. All profiles stay inside the same
    // tested dimensions; these meshes never own or resize flight colliders.
    internal static class SkyPulseRouteArchitecture
    {
        public static readonly string[] Names={"Neon inlay","Aurora flutes","Solar radiator","Tidal seals","Rose deco","Crystal facets","Jade collars","Violet armour","Obsidian crown","Storm conduits","Copper observatory","Polar glass"};
        public static Color Accent(int world)
        {
            switch(world)
            {
                case 1:return new Color(.22f,1,.73f);case 2:return new Color(1,.65f,.13f);case 3:return new Color(.12f,.72f,1);
                case 4:return new Color(1,.32f,.69f);case 5:return new Color(.70f,.90f,1);case 6:return new Color(.25f,1,.55f);
                case 7:return new Color(.68f,.32f,1);case 8:return new Color(.73f,.47f,1);case 9:return new Color(.18f,.45f,1);
                case 10:return new Color(1,.61f,.23f);case 11:return new Color(.40f,.96f,1);default:return new Color(.02f,.85f,1);
            }
        }
        public static Color Secondary(int world)=>world==2||world==10?new Color(1,.36f,.12f):world==4?new Color(1,.75f,.84f):world==6?new Color(.14f,.73f,.64f):world==7?new Color(1,.25f,.77f):world==8?new Color(1,.70f,.35f):world==0?new Color(1,.03f,.85f):new Color(.76f,.93f,1);
        public static Color Metal(int world)=>world==2||world==10?new Color(.69f,.42f,.24f):world==4?new Color(.58f,.38f,.49f):world==5||world==11?new Color(.64f,.80f,.90f):world==6?new Color(.30f,.52f,.43f):world==8?new Color(.23f,.19f,.32f):world==7?new Color(.35f,.28f,.49f):new Color(.31f,.43f,.62f);
        public static Mesh Build(int world,bool cap)
        {
            var b=new SkyPulseMeshBuilder();var profile=Profile(world,cap);const int sides=48;
            for(int i=0;i<sides;i++)for(int row=1;row<profile.Length;row++)
            {
                float a=i*Mathf.PI*2/sides,c=(i+1)*Mathf.PI*2/sides;var lo=profile[row-1];var hi=profile[row];
                var p=Point(a,lo,world,cap);var q=Point(c,lo,world,cap);var r=Point(c,hi,world,cap);var t=Point(a,hi,world,cap);
                float dy=hi.y-lo.y,dr=hi.x-lo.x;var na=new Vector3(Mathf.Cos(a)*dy,-dr,Mathf.Sin(a)*dy).normalized;var nc=new Vector3(Mathf.Cos(c)*dy,-dr,Mathf.Sin(c)*dy).normalized;
                b.SmoothQuad(p,q,r,t,na,nc,new Color(1,1,1,0));
                if(row==1)b.Face(new Vector3(0,lo.y,0),q,p,new Color(1,1,1,0));
                if(row==profile.Length-1)b.Face(new Vector3(0,hi.y,0),t,r,new Color(1,1,1,0));
            }
            Decorate(b,world,cap);
            return b.Finish(Names[world]+(cap?" / bevelled collar":" / structural shaft"));
        }
        static readonly Color Trim=new Color(1,1,1,.35f),Inlay=new Color(1,1,1,1);
        static void Decorate(SkyPulseMeshBuilder b,int world,bool cap)
        {
            if(cap)
            {
                int seals=world==2||world==6||world==9?4:world==5||world==11?2:3;
                for(int i=0;i<seals;i++)Band(b,-.28f+i*.56f/(seals-1),.015f,world,Trim);
                int panels=world==7?3:world==8?1:2;
                for(int i=0;i<panels;i++){float x=(i-(panels-1)*.5f)*.22f;Plate(b,x,0,.055f,world==5?.26f:.20f,Trim);Strip(b,new Vector2(x,-.065f),new Vector2(x,.065f),.009f,Inlay);}
                return;
            }
            switch(world)
            {
                case 1:
                    for(int side=-1;side<=1;side+=2)for(int i=0;i<12;i++)
                    {float y=-.43f+i*.072f,c=y+.072f;Strip(b,new Vector2(side*(.24f+.045f*Mathf.Sin(y*5)),y),new Vector2(side*(.24f+.045f*Mathf.Sin(c*5)),c),.012f,Inlay);}break;
                case 2:
                    for(int i=0;i<7;i++){Band(b,-.40f+i*.13f,.026f,world,Trim);Strip(b,new Vector2(-.17f,-.36f+i*.12f),new Vector2(.17f,-.36f+i*.12f),.008f,Inlay);}break;
                case 3:
                    for(int i=0;i<4;i++){Band(b,-.36f+i*.24f,.020f,world,Trim);Band(b,-.33f+i*.24f,.006f,world,Inlay);}break;
                case 4:
                    for(int i=0;i<4;i++)for(int side=-1;side<=1;side+=2)Strip(b,new Vector2(side*.26f,-.35f+i*.22f),new Vector2(0,-.23f+i*.22f),.014f,Trim);break;
                case 5:
                    for(int side=-1;side<=1;side+=2){Strip(b,new Vector2(side*.18f,-.44f),new Vector2(side*.27f,0),.025f,Trim);Strip(b,new Vector2(side*.27f,0),new Vector2(side*.18f,.44f),.025f,Trim);}
                    Strip(b,new Vector2(0,-.40f),new Vector2(0,.40f),.011f,Inlay);break;
                case 6:
                    for(int i=0;i<4;i++){Band(b,-.39f+i*.26f,.032f,world,Trim);Band(b,-.36f+i*.26f,.008f,world,Inlay);}break;
                case 7:
                    for(int i=0;i<3;i++){float y=-.3f+i*.3f;Plate(b,0,y,.24f,.20f,Trim);for(int j=0;j<3;j++)Strip(b,new Vector2(-.085f,y-.042f+j*.042f),new Vector2(.085f,y-.042f+j*.042f),.007f,Inlay);}break;
                case 8:
                    Strip(b,new Vector2(0,-.43f),new Vector2(.22f,0),.020f,Trim);Strip(b,new Vector2(.22f,0),new Vector2(0,.43f),.020f,Trim);Strip(b,new Vector2(0,.43f),new Vector2(-.22f,0),.020f,Trim);Strip(b,new Vector2(-.22f,0),new Vector2(0,-.43f),.020f,Trim);
                    for(int i=0;i<3;i++)Band(b,-.32f+i*.32f,.008f,world,Inlay);break;
                case 9:
                    for(int side=-1;side<=1;side+=2)for(int i=0;i<6;i++)Strip(b,new Vector2(side*(i%2==0?.15f:.29f),-.42f+i*.14f),new Vector2(side*(i%2==0?.29f:.15f),-.28f+i*.14f),.012f,Inlay);break;
                case 10:
                    for(int side=-1;side<=1;side+=2){Plate(b,side*.25f,0,.035f,.82f,Trim);Strip(b,new Vector2(side*.25f,-.38f),new Vector2(side*.25f,.38f),.006f,Inlay);}
                    Band(b,-.35f,.024f,world,Trim);Band(b,.35f,.024f,world,Trim);break;
                default:
                    for(int i=0;i<5;i++){float y=-.38f+i*.19f;Strip(b,new Vector2(-.22f,y-.045f),new Vector2(0,y+.045f),.012f,Inlay);Strip(b,new Vector2(0,y+.045f),new Vector2(.22f,y-.045f),.012f,Inlay);}break;
            }
        }
        static void Band(SkyPulseMeshBuilder b,float y,float height,int world,Color colour)
        {
            const int sides=48;
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,c=(i+1)*Mathf.PI*2/sides;
                var p=Point(a,new Vector2(.499f,y),world,true);var q=Point(c,new Vector2(.499f,y),world,true);
                b.SmoothQuad(p,q,q+Vector3.up*height,p+Vector3.up*height,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),new Vector3(Mathf.Cos(c),0,Mathf.Sin(c)),colour);
            }
        }
        static void Strip(SkyPulseMeshBuilder b,Vector2 a,Vector2 c,float width,Color colour)
        {
            var side=new Vector2(-(c-a).y,(c-a).x).normalized*width*.5f;
            b.Quad(new Vector3(a.x-side.x,a.y-side.y,-.532f),new Vector3(c.x-side.x,c.y-side.y,-.532f),new Vector3(c.x+side.x,c.y+side.y,-.532f),new Vector3(a.x+side.x,a.y+side.y,-.532f),colour);
        }
        static void Plate(SkyPulseMeshBuilder b,float x,float y,float width,float height,Color colour)
        {
            float cut=Mathf.Min(width*.18f,height*.12f),w=width*.5f,h=height*.5f;
            var outline=new[]{new Vector2(-w+cut,-h),new Vector2(w-cut,-h),new Vector2(w,-h+cut),new Vector2(w,h-cut),new Vector2(w-cut,h),new Vector2(-w+cut,h),new Vector2(-w,h-cut),new Vector2(-w,-h+cut)};
            for(int i=0;i<8;i++)
            {
                var p=outline[i];var q=outline[(i+1)%8];
                if(width>.20f)
                    b.Quad(new Vector3(x+p.x,y+p.y,-.52f),new Vector3(x+q.x,y+q.y,-.52f),new Vector3(x+q.x*.92f,y+q.y*.92f,-.52f),new Vector3(x+p.x*.92f,y+p.y*.92f,-.52f),colour);
                else b.Face(new Vector3(x,y,-.52f),new Vector3(x+q.x,y+q.y,-.52f),new Vector3(x+p.x,y+p.y,-.52f),colour);
                b.Quad(new Vector3(x+p.x,y+p.y,-.52f),new Vector3(x+q.x,y+q.y,-.52f),new Vector3(x+q.x,y+q.y,-.495f),new Vector3(x+p.x,y+p.y,-.495f),colour);
            }
        }
        static Vector3 Point(float a,Vector2 p,int world,bool cap)
        {
            float twist=cap?0:world==1?p.y*.17f:world==9?p.y*.22f:world==11?p.y*.12f:0;a+=twist;
            float c=Mathf.Cos(a),s=Mathf.Sin(a),radius=1;
            if(world==5||world==8||world==11)
            {
                int count=world==8?8:6;float half=Mathf.PI/count,angle=Mathf.Repeat(a,2*half)-half;radius=Mathf.Cos(half)/Mathf.Cos(angle);
            }
            else if(world==2||world==4||world==6||world==9||world==10)radius=.96f+.04f*Mathf.Cos(a*(world==9?6:world==4||world==10?12:8));
            if(world==7)return new Vector3(Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),.40f)*p.x,p.y,Mathf.Sign(s)*Mathf.Pow(Mathf.Abs(s),.40f)*p.x);
            return new Vector3(c*p.x*radius,p.y,s*p.x*radius*(world==3?.85f:1));
        }
        static Vector2[] Profile(int world,bool cap)
        {
            if(cap)switch(world)
            {
                case 1:return P(.46f,-.5f,.5f,-.38f,.47f,-.20f,.50f,.26f,.49f,.4f,.46f,.5f);
                case 2:return P(.46f,-.5f,.50f,-.40f,.46f,-.22f,.50f,-.16f,.46f,.02f,.50f,.08f,.46f,.26f,.50f,.34f,.46f,.5f);
                case 3:return P(.46f,-.5f,.50f,-.36f,.47f,-.26f,.50f,-.08f,.47f,.04f,.50f,.23f,.49f,.4f,.46f,.5f);
                case 4:return P(.46f,-.5f,.5f,-.37f,.49f,-.20f,.47f,.06f,.50f,.32f,.48f,.43f,.46f,.5f);
                case 5:return P(.46f,-.5f,.50f,-.30f,.50f,.25f,.46f,.5f);
                case 6:return P(.46f,-.5f,.50f,-.35f,.46f,-.16f,.50f,.0f,.46f,.18f,.50f,.35f,.46f,.5f);
                case 7:return P(.46f,-.5f,.5f,-.39f,.5f,-.22f,.47f,-.12f,.47f,.12f,.5f,.22f,.5f,.39f,.46f,.5f);
                case 8:return P(.46f,-.5f,.50f,-.29f,.48f,.16f,.50f,.32f,.46f,.5f);
                case 9:return P(.46f,-.5f,.50f,-.37f,.46f,-.20f,.50f,-.06f,.46f,.10f,.50f,.28f,.48f,.43f,.46f,.5f);
                case 10:return P(.46f,-.5f,.49f,-.42f,.50f,-.27f,.50f,.23f,.49f,.40f,.46f,.5f);
                default:return P(.46f,-.5f,.48f,-.42f,.50f,-.28f,.48f,-.1f,.50f,.14f,.48f,.36f,.46f,.5f);
            }
            switch(world)
            {
                case 1:return P(.48f,-.5f,.5f,-.43f,.46f,-.15f,.5f,.16f,.48f,.44f,.48f,.5f);
                case 2:return P(.48f,-.5f,.50f,-.46f,.47f,-.34f,.50f,-.26f,.47f,-.08f,.50f,.02f,.47f,.2f,.50f,.28f,.47f,.44f,.48f,.5f);
                case 3:return P(.48f,-.5f,.50f,-.44f,.49f,-.34f,.46f,-.30f,.46f,.31f,.49f,.36f,.50f,.44f,.48f,.5f);
                case 4:return P(.48f,-.5f,.50f,-.40f,.46f,-.24f,.47f,.20f,.50f,.39f,.48f,.5f);
                case 5:return P(.48f,-.5f,.50f,-.32f,.50f,.34f,.48f,.5f);
                case 6:return P(.48f,-.5f,.49f,-.43f,.46f,-.32f,.46f,-.1f,.50f,-.04f,.46f,.05f,.46f,.33f,.50f,.39f,.48f,.5f);
                case 7:return P(.48f,-.5f,.50f,-.43f,.50f,-.27f,.47f,-.21f,.47f,.22f,.5f,.28f,.5f,.43f,.48f,.5f);
                case 8:return P(.48f,-.5f,.49f,-.45f,.46f,-.3f,.47f,.26f,.5f,.4f,.48f,.5f);
                case 9:return P(.48f,-.5f,.5f,-.43f,.46f,-.18f,.5f,-.08f,.46f,.14f,.5f,.23f,.47f,.42f,.48f,.5f);
                case 10:return P(.48f,-.5f,.5f,-.41f,.48f,-.30f,.47f,.3f,.5f,.42f,.48f,.5f);
                default:return P(.48f,-.5f,.5f,-.41f,.48f,-.26f,.5f,.12f,.48f,.35f,.48f,.5f);
            }
        }
        static Vector2[] P(params float[] pairs)
        {
            var result=new Vector2[pairs.Length/2];for(int i=0;i<result.Length;i++)result[i]=new Vector2(pairs[i*2],pairs[i*2+1]);return result;
        }
    }
}
