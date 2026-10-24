using UnityEngine;

namespace SkyPulse.Mobile
{
    // One continuous engineered assembly per world. Channels follow the barrel
    // surface; there are no front-facing plates or floating sticker decorations.
    internal static class SkyPulseRouteArchitecture
    {
#if UNITY_EDITOR
        internal static System.Action<object> EditorMeshCapture;
#endif
        static Mesh Finish(SkyPulseMeshBuilder builder,string name)
        {
#if UNITY_EDITOR
            EditorMeshCapture?.Invoke(builder);
#endif
            return builder.Finish(name);
        }
        public static readonly string[] Names={"Neon precision alloy","Aurora ceramic channels","Solar collector housing","Tidal titanium seals","Rose ceramic alloy","Prismatic silver shell","Jade bioceramic housing","Violet arcology alloy","Obsidian precision crown","Cobalt induction housing","Amber bronze housing","Polar insulated shell"};
        public static Color Accent(int world)
        {
            switch(world){case 1:return new Color(.25f,1,.81f);case 2:return new Color(1,.66f,.22f);case 3:return new Color(.14f,.70f,1);case 4:return new Color(1,.37f,.62f);case 5:return new Color(.62f,.82f,1);case 6:return new Color(.29f,.93f,.57f);case 7:return new Color(.73f,.37f,1);case 8:return new Color(.98f,.72f,.36f);case 9:return new Color(.22f,.50f,1);case 10:return new Color(1,.65f,.28f);case 11:return new Color(.43f,.92f,1);default:return new Color(.02f,.85f,1);}
        }
        public static Color Secondary(int world)
        {
            switch(world){case 1:return new Color(.72f,.58f,1);case 2:return new Color(1,.28f,.10f);case 3:return new Color(.32f,1,.83f);case 4:return new Color(1,.78f,.44f);case 5:return new Color(.78f,.38f,1);case 6:return new Color(.92f,.81f,.40f);case 7:return new Color(.20f,.88f,1);case 8:return new Color(.68f,.46f,1);case 9:return new Color(.68f,.91f,1);case 10:return new Color(1,.38f,.16f);case 11:return new Color(.72f,.61f,1);default:return new Color(1,.03f,.85f);}
        }
        public static Color Metal(int world)
        {
            switch(world){case 1:return new Color(.66f,.73f,.73f);case 2:return new Color(.39f,.34f,.29f);case 3:return new Color(.16f,.29f,.38f);case 4:return new Color(.57f,.43f,.46f);case 5:return new Color(.52f,.61f,.74f);case 6:return new Color(.22f,.37f,.32f);case 7:return new Color(.25f,.22f,.36f);case 8:return new Color(.14f,.14f,.21f);case 9:return new Color(.22f,.34f,.53f);case 10:return new Color(.53f,.36f,.24f);case 11:return new Color(.64f,.74f,.78f);default:return new Color(.24f,.31f,.39f);}
        }
        static readonly Color Base=new Color(1,1,1,0),Trim=new Color(1,1,1,.25f),Seal=new Color(1,1,1,.55f),Light=new Color(1,1,1,1);
        public static Mesh Build(int world,bool cap)
        {
            world=Mathf.Clamp(world,0,11);if(world==0)return BuildOriginal(cap);
            var b=new SkyPulseMeshBuilder();var profile=Profile(world,cap);const int sides=64;
            for(int row=1;row<profile.Length;row++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1f)*Mathf.PI*2/sides;var lo=profile[row-1];var hi=profile[row];
                var p=Point(a,lo,world,cap);var q=Point(c,lo,world,cap);var r=Point(c,hi,world,cap);var t=Point(a,hi,world,cap);
                float dy=hi.y-lo.y,dr=hi.x-lo.x;var na=Normal(a,lo,world,cap,dy,dr);var nb=Normal(c,lo,world,cap,dy,dr);
                Color material=cap&&(row==2||row==profile.Length-2)?Trim:Base;
                b.SmoothQuad(p,q,r,t,na,nb,material);
                if(row==1)b.Face(new Vector3(0,lo.y,0),q,p,Base);
                if(row==profile.Length-1)b.Face(new Vector3(0,hi.y,0),t,r,Trim);
            }
            if(cap)
            {
                // Recessed dark seals and a single light reveal belong to the
                // housing rather than crossing it as big surface-mounted bands.
                Band(b,world,profile,-.28f,.018f,Seal);Band(b,world,profile,.24f,.012f,Light);
                for(int i=0;i<8;i++)Channel(b,world,profile,i*Mathf.PI/4,-.17f,.12f,.028f,Trim,true);
            }
            else
            {
                int rails=world==2||world==9?6:world==5||world==11?3:4;
                for(int rail=0;rail<rails;rail++)
                {
                    float a=Mathf.PI*2*(rail+.5f)/rails;
                    bool forward=rails==6&&rail==4;
                    Channel(b,world,profile,a,-.475f,.475f,forward?.13f:.095f,Seal,false);
                    Channel(b,world,profile,a,-.455f,.455f,forward?.046f:.025f,Light,false);
                }
                // A recessed forward diffuser stays readable on the small
                // portrait camera. It follows the barrel, not a flat overlay.
                if(rails!=6)
                {
                    Channel(b,world,profile,-Mathf.PI*.5f,-.47f,.47f,.13f,Seal,false);
                    Channel(b,world,profile,-Mathf.PI*.5f,-.455f,.455f,.046f,Light,false);
                }
                Band(b,world,profile,-.43f,.014f,Trim);Band(b,world,profile,.416f,.014f,Trim);
                if(world==2||world==9)for(int i=0;i<3;i++)
                {
                    float y=-.20f+i*.20f;
                    Band(b,world,profile,y,.009f,Seal);
                    if(world==9)Band(b,world,profile,y+.010f,.0035f,Light);
                }
            }
            return Finish(b,Names[world]+(cap?" / machined collar":" / continuous barrel"));
        }
        static Mesh BuildOriginal(bool cap)
        {
            var b=new SkyPulseMeshBuilder();int segments=48;var profile=cap?new[]{new Vector2(.46f,-.5f),new Vector2(.50f,-.39f),new Vector2(.50f,.39f),new Vector2(.46f,.5f)}:new[]{new Vector2(.47f,-.5f),new Vector2(.5f,-.48f),new Vector2(.5f,.48f),new Vector2(.47f,.5f)};
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,c=(i+1)*Mathf.PI*2/segments;
                for(int j=1;j<profile.Length;j++)
                {
                    var p=profile[j-1];var q=profile[j];Color colour=cap?new Color(.7f,.9f,1,j==2?0:1):new Color(.85f,.95f,1,i%12==0?1:0);
                    float dy=q.y-p.y,dr=q.x-p.x;
                    var na=new Vector3(Mathf.Cos(a)*dy,-dr,Mathf.Sin(a)*dy).normalized;var nb=new Vector3(Mathf.Cos(c)*dy,-dr,Mathf.Sin(c)*dy).normalized;
                    b.SmoothQuad(new Vector3(Mathf.Cos(a)*p.x,p.y,Mathf.Sin(a)*p.x),new Vector3(Mathf.Cos(c)*p.x,p.y,Mathf.Sin(c)*p.x),new Vector3(Mathf.Cos(c)*q.x,q.y,Mathf.Sin(c)*q.x),new Vector3(Mathf.Cos(a)*q.x,q.y,Mathf.Sin(a)*q.x),na,nb,colour);
                }
                foreach(int side in new[]{-1,1})b.Face(new Vector3(0,side*.5f,0),new Vector3(Mathf.Cos(a)*.46f,side*.5f,Mathf.Sin(a)*.46f),new Vector3(Mathf.Cos(c)*.46f,side*.5f,Mathf.Sin(c)*.46f),new Color(.9f,.95f,1,.2f));
            }
            return Finish(b,cap?"Extruded precision gate collar":"Reflective cylindrical Endless column");
        }
        static void Channel(SkyPulseMeshBuilder b,int world,Vector2[] profile,float angle,float bottom,float top,float width,Color colour,bool cap)
        {
            const int steps=24;
            for(int i=0;i<steps;i++)
            {
                float y=Mathf.Lerp(bottom,top,i/(float)steps),z=Mathf.Lerp(bottom,top,(i+1f)/steps);
                float sweep=cap?0:world==1?.055f*Mathf.Sin(y*3):world==7?.035f*y:world==11?.024f*Mathf.Sin(y*5):0;
                float next=cap?0:world==1?.055f*Mathf.Sin(z*3):world==7?.035f*z:world==11?.024f*Mathf.Sin(z*5):0;
                float a=angle+sweep-width*.5f,c=angle+sweep+width*.5f,d=angle+next+width*.5f,e=angle+next-width*.5f;
                float relief=colour.a>.8f?.002f:colour.a>.4f?.0004f:.001f;
                float r=Mathf.Min(.50f,Radius(profile,y)+relief),s=Mathf.Min(.50f,Radius(profile,z)+relief);
                b.SmoothQuad(Point(a,new Vector2(r,y),world,cap),Point(c,new Vector2(r,y),world,cap),Point(d,new Vector2(s,z),world,cap),Point(e,new Vector2(s,z),world,cap),Normal(a,new Vector2(r,y),world,cap,1,0),Normal(c,new Vector2(r,y),world,cap,1,0),colour);
            }
        }
        static void Band(SkyPulseMeshBuilder b,int world,Vector2[] profile,float bottom,float height,Color colour)
        {
            const int sides=64;
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,c=(i+1f)*Mathf.PI*2/sides;
                float r=Mathf.Min(.5f,Radius(profile,bottom)+.0012f),s=Mathf.Min(.5f,Radius(profile,bottom+height)+.0012f);
                b.SmoothQuad(Point(a,new Vector2(r,bottom),world,true),Point(c,new Vector2(r,bottom),world,true),Point(c,new Vector2(s,bottom+height),world,true),Point(a,new Vector2(s,bottom+height),world,true),Normal(a,new Vector2(r,bottom),world,true,1,0),Normal(c,new Vector2(r,bottom),world,true,1,0),colour);
            }
        }
        static float Radius(Vector2[] profile,float y)
        {
            for(int i=1;i<profile.Length;i++)if(y<=profile[i].y)return Mathf.Lerp(profile[i-1].x,profile[i].x,Mathf.InverseLerp(profile[i-1].y,profile[i].y,y));
            return profile[profile.Length-1].x;
        }
        static Vector3 Normal(float a,Vector2 p,int world,bool cap,float dy,float dr)
        {
            var tangent=Point(a+.001f,p,world,cap)-Point(a-.001f,p,world,cap);
            var radial=Point(a,new Vector2(p.x+dr*.001f,p.y+dy*.001f),world,cap)-Point(a,p,world,cap);
            // Unity treats vectors shorter than 1e-5 as zero. Differentiate in
            // mesh units, then scale before normalising the cross product.
            return Vector3.Cross(radial*10000,tangent*100).normalized;
        }
        static Vector3 Point(float a,Vector2 p,int world,bool cap)
        {
            float c=Mathf.Cos(a),s=Mathf.Sin(a),r=1;
            if(world==5||world==8){int sides=world==5?8:12;float h=Mathf.PI/sides;float d=Mathf.Repeat(a,2*h)-h;r=Mathf.Cos(h)/Mathf.Cos(d);}
            else if(world==2||world==9||world==10)r=.973f+.027f*Mathf.Cos(a*(world==9?8:12));
            if(world==7)return new Vector3(Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),.68f)*p.x,p.y,Mathf.Sign(s)*Mathf.Pow(Mathf.Abs(s),.68f)*p.x);
            return new Vector3(c*p.x*r,p.y,s*p.x*r*(world==3?.90f:1));
        }
        static Vector2[] Profile(int world,bool cap)
        {
            if(cap)
            {
                if(world==1)return P(.455f,-.5f,.482f,-.46f,.498f,-.36f,.498f,-.28f,.475f,-.22f,.475f,.13f,.490f,.19f,.498f,.29f,.488f,.43f,.455f,.5f);
                if(world==2)return P(.455f,-.5f,.490f,-.44f,.5f,-.34f,.478f,-.22f,.478f,-.12f,.498f,-.06f,.498f,.12f,.478f,.18f,.5f,.30f,.490f,.44f,.455f,.5f);
                if(world==3)return P(.455f,-.5f,.484f,-.44f,.5f,-.32f,.486f,-.22f,.474f,-.12f,.474f,.12f,.486f,.22f,.5f,.32f,.484f,.44f,.455f,.5f);
                if(world==4)return P(.455f,-.5f,.484f,-.44f,.497f,-.32f,.5f,-.18f,.484f,.14f,.497f,.27f,.488f,.42f,.455f,.5f);
                if(world==5)return P(.455f,-.5f,.5f,-.34f,.5f,-.24f,.48f,-.15f,.48f,.15f,.5f,.24f,.5f,.34f,.455f,.5f);
                if(world==6)return P(.455f,-.5f,.486f,-.44f,.498f,-.30f,.481f,-.19f,.490f,-.08f,.490f,.08f,.481f,.19f,.498f,.30f,.486f,.44f,.455f,.5f);
                if(world==7)return P(.455f,-.5f,.498f,-.37f,.498f,-.29f,.475f,-.18f,.475f,.18f,.498f,.29f,.498f,.37f,.455f,.5f);
                if(world==8)return P(.455f,-.5f,.5f,-.36f,.5f,-.25f,.47f,-.16f,.47f,.16f,.495f,.25f,.495f,.37f,.455f,.5f);
                if(world==9)return P(.455f,-.5f,.49f,-.43f,.5f,-.33f,.476f,-.23f,.496f,-.14f,.496f,.14f,.476f,.23f,.5f,.33f,.49f,.43f,.455f,.5f);
                if(world==10)return P(.455f,-.5f,.488f,-.43f,.5f,-.32f,.484f,-.22f,.478f,0,.484f,.22f,.5f,.32f,.488f,.43f,.455f,.5f);
                return P(.455f,-.5f,.486f,-.45f,.5f,-.34f,.5f,-.27f,.478f,-.21f,.478f,.17f,.498f,.22f,.5f,.33f,.486f,.45f,.455f,.5f);
            }
            if(world==1)return P(.475f,-.5f,.494f,-.46f,.5f,-.37f,.487f,-.26f,.487f,.26f,.5f,.37f,.494f,.46f,.475f,.5f);
            if(world==2)return P(.478f,-.5f,.495f,-.46f,.5f,-.42f,.482f,-.35f,.480f,-.05f,.493f,0,.480f,.05f,.482f,.35f,.5f,.42f,.495f,.46f,.478f,.5f);
            if(world==3)return P(.475f,-.5f,.495f,-.44f,.5f,-.32f,.489f,-.18f,.483f,0,.489f,.18f,.5f,.32f,.495f,.44f,.475f,.5f);
            if(world==4)return P(.475f,-.5f,.493f,-.45f,.5f,-.34f,.488f,-.18f,.483f,.02f,.488f,.18f,.5f,.34f,.493f,.45f,.475f,.5f);
            if(world==6)return P(.475f,-.5f,.5f,-.40f,.483f,-.28f,.483f,-.10f,.497f,-.04f,.483f,.04f,.483f,.28f,.5f,.40f,.475f,.5f);
            if(world==5)return P(.475f,-.5f,.497f,-.45f,.5f,-.39f,.49f,-.31f,.49f,.31f,.5f,.39f,.497f,.45f,.475f,.5f);
            if(world==8)return P(.475f,-.5f,.497f,-.43f,.5f,-.37f,.482f,-.28f,.482f,.28f,.5f,.37f,.497f,.43f,.475f,.5f);
            if(world==9)return P(.475f,-.5f,.498f,-.43f,.486f,-.34f,.486f,-.23f,.498f,-.19f,.486f,-.14f,.486f,.14f,.498f,.19f,.486f,.23f,.486f,.34f,.498f,.43f,.475f,.5f);
            if(world==10)return P(.475f,-.5f,.498f,-.42f,.487f,-.31f,.480f,0,.487f,.31f,.498f,.42f,.475f,.5f);
            if(world==11)return P(.475f,-.5f,.498f,-.43f,.498f,-.35f,.483f,-.29f,.483f,.29f,.498f,.35f,.498f,.43f,.475f,.5f);
            return P(.475f,-.5f,.496f,-.46f,.5f,-.39f,.484f,-.30f,.484f,.30f,.5f,.39f,.496f,.46f,.475f,.5f);
        }
        static Vector2[] P(params float[] xy){var p=new Vector2[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Vector2(Mathf.Min(.498f,xy[i*2]),xy[i*2+1]);return p;}
    }
}
