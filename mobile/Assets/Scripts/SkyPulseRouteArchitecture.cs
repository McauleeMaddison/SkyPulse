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
        public static Mesh Build(int world,bool cap)=>BuildShell(world,cap,true);
        // Workshop base remains separate from editable hardware children.
        public static Mesh BuildBase(int world,bool cap)=>BuildShell(world,cap,false);
        static Mesh BuildShell(int world,bool cap,bool details)
        {
            world=Mathf.Clamp(world,0,11);if(cap)return BuildPhysicalCap(world,details);if(world==0)return BuildOriginalBarrel(details);
            var b=new SkyPulseMeshBuilder();var profile=Profile(world,false);const int sides=64;
            for(int row=1;row<profile.Length;row++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1f)*Mathf.PI*2/sides;var lo=profile[row-1];var hi=profile[row];
                var p=Point(a,lo,world,false);var q=Point(c,lo,world,false);var r=Point(c,hi,world,false);var t=Point(a,hi,world,false);
                float dy=hi.y-lo.y,dr=hi.x-lo.x;var na=Normal(a,lo,world,false,dy,dr);var nb=Normal(c,lo,world,false,dy,dr);
                b.SmoothQuad(p,q,r,t,na,nb,Base);
                if(row==1)b.Face(new Vector3(0,lo.y,0),q,p,Base);
                if(row==profile.Length-1)b.Face(new Vector3(0,hi.y,0),t,r,Trim);
            }
            int rails=world==2||world==9?6:world==5||world==11?3:4;
            for(int rail=0;rail<rails;rail++)
            {
                float a=Mathf.PI*2*(rail+.5f)/rails;bool forward=rails==6&&rail==4;
                Channel(b,world,profile,a,-.475f,.475f,forward?.13f:.095f,Seal,false);
                Channel(b,world,profile,a,-.455f,.455f,forward?.046f:.025f,Light,false);
            }
            if(rails!=6)
            {
                Channel(b,world,profile,-Mathf.PI*.5f,-.47f,.47f,.13f,Seal,false);
                Channel(b,world,profile,-Mathf.PI*.5f,-.455f,.455f,.046f,Light,false);
            }
            Band(b,world,profile,-.43f,.014f,Trim);Band(b,world,profile,.416f,.014f,Trim);
            if(world==2||world==9)for(int i=0;i<3;i++)
            {
                float y=-.20f+i*.20f;Band(b,world,profile,y,.009f,Seal);
                if(world==9)Band(b,world,profile,y+.010f,.0035f,Light);
            }
            if(details){AddHardware(b,world,2);AddHardware(b,world,3);}
            return Finish(b,Names[world]+" / continuous barrel"+(details?" / structure-v1":""));
        }
        static Mesh BuildOriginalBarrel(bool details)
        {
            var b=new SkyPulseMeshBuilder();const int segments=48;
            var profile=new[]{new Vector2(.47f,-.5f),new Vector2(.5f,-.48f),new Vector2(.5f,.48f),new Vector2(.47f,.5f)};
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,c=(i+1)*Mathf.PI*2/segments;
                for(int j=1;j<profile.Length;j++)
                {
                    var p=profile[j-1];var q=profile[j];Color colour=new Color(.85f,.95f,1,i%12==0?1:0);
                    float dy=q.y-p.y,dr=q.x-p.x;
                    var na=new Vector3(Mathf.Cos(a)*dy,-dr,Mathf.Sin(a)*dy).normalized;var nb=new Vector3(Mathf.Cos(c)*dy,-dr,Mathf.Sin(c)*dy).normalized;
                    b.SmoothQuad(new Vector3(Mathf.Cos(a)*p.x,p.y,Mathf.Sin(a)*p.x),new Vector3(Mathf.Cos(c)*p.x,p.y,Mathf.Sin(c)*p.x),new Vector3(Mathf.Cos(c)*q.x,q.y,Mathf.Sin(c)*q.x),new Vector3(Mathf.Cos(a)*q.x,q.y,Mathf.Sin(a)*q.x),na,nb,colour);
                }
                foreach(int side in new[]{-1,1})b.Face(new Vector3(0,side*.5f,0),new Vector3(Mathf.Cos(a)*.46f,side*.5f,Mathf.Sin(a)*.46f),new Vector3(Mathf.Cos(c)*.46f,side*.5f,Mathf.Sin(c)*.46f),new Color(.9f,.95f,1,.2f));
            }
            if(details){AddHardware(b,0,2);AddHardware(b,0,3);}
            return Finish(b,"Reflective cylindrical Endless column"+(details?" / structure-v1":""));
        }
        // A real hollow collar: the inclined circular mouth, annular shoulder,
        // inner wall and rolled edge share one physical construction. It is not
        // an ellipse pasted over the front of a solid cylinder.
        static Mesh BuildPhysicalCap(int world,bool details)
        {
            var b=new SkyPulseMeshBuilder();const int sides=64;
            var shape=world==0?P(.455f,-.5f,.485f,-.45f,.5f,-.34f,.49f,-.17f):Profile(world,true);
            var rows=new System.Collections.Generic.List<Vector2>();
            foreach(var row in shape)if(row.y<=-.1f)rows.Add(row);
            rows.Add(new Vector2(.49f,-.04f));rows.Add(new Vector2(.5f,.30f));
            for(int row=1;row<rows.Count;row++)for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1f)*Mathf.PI*2/sides;bool inclined=row==rows.Count-1;
                var p=CapShellPoint(world,a,rows[row-1],false);var q=CapShellPoint(world,c,rows[row-1],false);
                var r=CapShellPoint(world,c,rows[row],inclined);var t=CapShellPoint(world,a,rows[row],inclined);
                var ta=CapShellPoint(world,a+.002f,rows[row],inclined)-CapShellPoint(world,a-.002f,rows[row],inclined);
                var tc=CapShellPoint(world,c+.002f,rows[row],inclined)-CapShellPoint(world,c-.002f,rows[row],inclined);
                var na=Vector3.Cross((t-p)*100,ta*100).normalized;var nc=Vector3.Cross((r-q)*100,tc*100).normalized;
                b.SmoothQuad(p,q,r,t,na,nc,row==2?Trim:Base);
            }
            float depth=world==3?.9f:1;var mouthNormal=new Vector3(0,1,-.4f/depth).normalized;
            for(int side=0;side<sides;side++)
            {
                float a=side*Mathf.PI*2/sides,c=(side+1f)*Mathf.PI*2/sides;
                var outerA=CapShellPoint(world,a,new Vector2(.5f,.3f),true);var outerC=CapShellPoint(world,c,new Vector2(.5f,.3f),true);
                var innerA=Bore(a,depth,false);var innerC=Bore(c,depth,false);
                b.SmoothQuad(outerA,outerC,innerC,innerA,mouthNormal,mouthNormal,Trim);
                b.Face(new Vector3(0,-.5f,0),CapShellPoint(world,c,rows[0],false),CapShellPoint(world,a,rows[0],false),Base);
            }
            if(details){AddHardware(b,world,0);AddHardware(b,world,1);}
            return Finish(b,Names[world]+" / inclined hollow collar"+(details?" / structure-v2":""));
        }
        static Vector3 CapShellPoint(int world,float angle,Vector2 profile,bool inclined)
        {
            var p=Point(angle,profile,world,true);
            if(inclined)p.y=.30f+.4f*p.z/(world==3?.9f:1);
            return p;
        }
        // Four physical parts are shared by runtime meshes and the editable
        // workshop children. Their XY extents stay inside the tested housing.
        public static Mesh Hardware(int world,int part)
        {
            var b=new SkyPulseMeshBuilder();AddHardware(b,Mathf.Clamp(world,0,11),part);
            string[] names={"Rolled cap bezel","Recessed cap bore","Barrel support ribs","Inset light guides"};
            return b.Finish(names[Mathf.Clamp(part,0,3)]+" / structure-v1",true);
        }
        static void AddHardware(SkyPulseMeshBuilder b,int world,int part)
        {
            const int sides=64;float depth=world==3?.90f:1;
            Color bezel=new Color(1,1,1,.72f),wall=new Color(1,1,1,.74f),well=new Color(1,1,1,.76f);
            if(part==0)
            {
                // A rolled circular lip on the real inclined mouth plane.
                // It sits within the original visible collar, not in the gap.
                const int tubeSides=8;
                for(int i=0;i<sides;i++)for(int j=0;j<tubeSides;j++)
                {
                    float a=i*Mathf.PI*2/sides,c=(i+1)*Mathf.PI*2/sides,u=j*Mathf.PI*2/tubeSides,v=(j+1)*Mathf.PI*2/tubeSides;
                    b.SmoothQuad(Bezel(a,u,depth),Bezel(c,u,depth),Bezel(c,v,depth),Bezel(a,v,depth),BezelNormal(a,(u+v)*.5f,depth),BezelNormal(c,(u+v)*.5f,depth),j==4?new Color(1,1,1,.78f):bezel);
                }
            }
            else if(part==1)
            {
                for(int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2/sides,c=(i+1)*Mathf.PI*2/sides;
                    Vector3 p=Bore(a,depth,false),q=Bore(c,depth,false),r=Bore(c,depth,true),t=Bore(a,depth,true);
                    var na=new Vector3(-Mathf.Cos(a),0,-Mathf.Sin(a)/depth).normalized;var nc=new Vector3(-Mathf.Cos(c),0,-Mathf.Sin(c)/depth).normalized;
                    b.SmoothQuad(p,q,r,t,na,nc,wall);
                    b.Face(new Vector3(0,-.245f,0),r,t,well);
                }
            }
            else
            {
                var profile=world==0?new[]{new Vector2(.47f,-.5f),new Vector2(.5f,-.48f),new Vector2(.5f,.48f),new Vector2(.47f,.5f)}:Profile(world,false);
                if(part==2)
                {
                    // Paired load-bearing rails have side walls and bevelled
                    // shoulders, leaving the original central LEDs exposed.
                    for(int side=0;side<2;side++)for(int panel=0;panel<3;panel++)
                    {
                        float a=side==0?-2.15f:-.99f,lo=-.43f+panel*.29f,hi=lo+.255f;
                        float r0=Mathf.Min(.497f,Radius(profile,lo)+.004f),r1=Mathf.Min(.497f,Radius(profile,hi)+.004f);
                        float outer0=Mathf.Min(.499f,r0+.011f),outer1=Mathf.Min(.499f,r1+.011f),w=.045f;
                        var p=Point(a-w,new Vector2(r0,lo),world,false);var q=Point(a+w,new Vector2(r0,lo),world,false);
                        var t=Point(a-w,new Vector2(r1,hi),world,false);var r=Point(a+w,new Vector2(r1,hi),world,false);
                        var p1=Point(a-w*.65f,new Vector2(outer0,lo+.009f),world,false);var q1=Point(a+w*.65f,new Vector2(outer0,lo+.009f),world,false);
                        var t1=Point(a-w*.65f,new Vector2(outer1,hi-.009f),world,false);var r2=Point(a+w*.65f,new Vector2(outer1,hi-.009f),world,false);
                        var colour=new Color(1,1,1,.68f);
                        b.Quad(p,p1,t1,t,colour);b.Quad(q,r,r2,q1,colour);b.Quad(p,q,q1,p1,colour);b.Quad(t1,r2,r,t,colour);
                        var normal=Normal(a,new Vector2(outer0,lo),world,false,1,0);b.SmoothQuad(p1,q1,r2,t1,normal,normal,colour);
                    }
                }
                else if(part==3)
                {
                    // Narrow physical diffuser cores are recessed between the
                    // rail shoulders. World colours and packets stay material-led.
                    for(int side=0;side<2;side++)
                    {
                        float angle=side==0?-2.15f:-.99f;
                        for(int segment=0;segment<16;segment++)
                        {
                            float lo=Mathf.Lerp(-.419f,.405f,segment/16f),hi=Mathf.Lerp(-.419f,.405f,(segment+1)/16f);
                            float r0=Mathf.Min(.5f,Radius(profile,lo)+.0065f),r1=Mathf.Min(.5f,Radius(profile,hi)+.0065f),w=.008f;
                            var normal=Normal(angle,new Vector2(r0,lo),world,false,1,0);
                            b.SmoothQuad(Point(angle-w,new Vector2(r0,lo),world,false),Point(angle+w,new Vector2(r0,lo),world,false),Point(angle+w,new Vector2(r1,hi),world,false),Point(angle-w,new Vector2(r1,hi),world,false),normal,normal,new Color(1,1,1,.67f));
                        }
                    }
                }
            }
        }
        static Vector3 Bezel(float angle,float section,float depth)
        {
            const float radius=.465f,tube=.014f;
            var radial=new Vector3(Mathf.Cos(angle),.4f*Mathf.Sin(angle),Mathf.Sin(angle)).normalized;
            var normal=new Vector3(0,1,-.4f).normalized;
            var p=new Vector3(Mathf.Cos(angle)*radius,.30f+.4f*Mathf.Sin(angle)*radius,Mathf.Sin(angle)*radius);
            p+=(radial*Mathf.Cos(section)+normal*Mathf.Sin(section))*tube;p.z*=depth;return p;
        }
        static Vector3 BezelNormal(float angle,float section,float depth)
        {
            var radial=new Vector3(Mathf.Cos(angle),.4f*Mathf.Sin(angle),Mathf.Sin(angle)).normalized;
            var normal=new Vector3(0,1,-.4f).normalized;var n=radial*Mathf.Cos(section)+normal*Mathf.Sin(section);n.z/=depth;return n.normalized;
        }
        static Vector3 Bore(float angle,float depth,bool bottom)
        {
            float x=Mathf.Cos(angle)*.449f,z=Mathf.Sin(angle)*.449f;
            return new Vector3(x,bottom?-.245f:.30f+.4f*z,z*depth);
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
