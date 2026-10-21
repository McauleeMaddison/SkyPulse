using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public enum SkyPulseSurface { Emblem, Backdrop, Panel, Atmosphere=5 }

    // Authored geometry: gradients, soft light, machined frames and pixel glyphs.
    // No external artwork, shader, font or generation service is required.
    public sealed class SkyPulseCircuitGraphic : MaskableGraphic
    {
        public int Variant;
        public SkyPulseSurface Surface;
        public bool Ambient, ReducedMotion, MotionPaused, Primary, CinematicUnderlay;
        private float phase;
        private bool previousReduced;
        private float Motion => ReducedMotion ? 0f : phase;
        private Color Cyan => new Color(.22f,.91f,1f,1f);
        private Color Gold => new Color(1f,.73f,.25f,1f);
        private Color Green => new Color(.32f,1f,.63f,1f);
        private Color Accent => Variant==2?Green:Variant==1?Gold:new Color(.65f,.50f,1f,1f);
        private void Update()
        {
            if(previousReduced!=ReducedMotion){previousReduced=ReducedMotion;SetVerticesDirty();}
            if(!Ambient || ReducedMotion || MotionPaused)return;
            phase=Mathf.Repeat(phase+Time.unscaledDeltaTime*.065f,1f);SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(Surface==SkyPulseSurface.Backdrop)Backdrop(vh,r);
            else if(Surface==SkyPulseSurface.Panel)Panel(vh,r);
            else if(Surface==SkyPulseSurface.Atmosphere)Atmosphere(vh,r);
            else Emblem(vh,r);
        }
        private static Color A(Color c,float alpha){c.a=alpha;return c;}
        private void Backdrop(VertexHelper vh,Rect r)
        {
            bool terminal=Variant==2;
            float opacity=CinematicUnderlay?.62f:1f;
            Gradient(vh,r,terminal?new Color(.002f,.014f,.011f,opacity):new Color(.014f,.035f,.075f,opacity),terminal?new Color(.006f,.047f,.034f,opacity):new Color(.017f,.10f,.14f,opacity));
            Glow(vh,new Vector2(r.xMin+r.width*.15f,r.yMax-r.height*.24f),r.width*.64f,A(terminal?Green:Cyan,.09f));
            Glow(vh,new Vector2(r.xMax,r.yMin+r.height*.35f),r.width*.7f,A(terminal?Green:Gold,.065f));
            float y=355f;var centre=new Vector2(0,y);
            if(terminal)
            {
                for(int i=-6;i<=6;i++)Line(vh,new Vector2(i*80,y-310),new Vector2(i*80,y+330),1,A(Green,.042f));
                for(int i=-4;i<=4;i++)Line(vh,new Vector2(-500,y+i*80),new Vector2(500,y+i*80),1,A(Green,.042f));
                for(int i=0;i<8;i++)
                {
                    float row=r.yMin+(i+1)*r.height/9;float side=i%2==0?-1:1;
                    var p0=new Vector2(side*r.width*.48f,row);var p1=new Vector2(side*380,row);var p2=p1+new Vector2(-side*54,54);
                    GlowLine(vh,p0,p1,1.2f,A(Green,.22f));GlowLine(vh,p1,p2,1.2f,A(Green,.22f));
                    var packet=Vector2.Lerp(p0,p1,Mathf.Repeat(Motion+i*.137f,1));
                    Quad(vh,new Rect(packet.x-3,packet.y-3,6,6),A(Green,.72f));Quad(vh,new Rect(p2.x-6,p2.y-6,12,12),A(Green,.38f));
                }
                Ring(vh,centre,288,1.3f,A(Green,.15f),0,360);
                Ring(vh,centre,321,3,A(Green,.3f),Motion*360,46);Ring(vh,centre,321,3,A(Green,.3f),Motion*360+180,46);
                for(int i=0;i<14;i++)Quad(vh,new Rect(-335+i*51,y-349,25,3),A(Green,.17f));
            }
            else
            {
                for(int side=-1;side<=1;side+=2)
                {
                    // The shared full-width view has no opaque decorative side strips.
                    for(int i=0;i<9;i++)
                    {
                        float row=r.yMin+(i+.5f)*r.height/9;
                        var start=new Vector2(side*(r.width*.5f-38),row);var knee=new Vector2(side*420,row);var end=new Vector2(side*350,row+70);
                        GlowLine(vh,start,knee,1.5f,A(Cyan,.25f));GlowLine(vh,knee,end,1.5f,A(Cyan,.25f));
                        var packet=Vector2.Lerp(start,knee,Mathf.Repeat(Motion+i*.123f,1));Glow(vh,packet,16,A(Gold,.28f));Quad(vh,new Rect(packet.x-3,packet.y-2,6,4),Gold);
                    }
                }
                Ring(vh,centre,275,1.5f,A(Cyan,.3f),0,360);
                Ring(vh,centre,304,4,A(Gold,.48f),Motion*360,66);Ring(vh,centre,304,4,A(Gold,.48f),Motion*360+180,66);
                Ring(vh,centre,330,1,A(Cyan,.14f),0,360);
                for(int i=0;i<40;i++)
                {
                    float t=i*Mathf.PI*2/40;var d=new Vector2(Mathf.Cos(t),Mathf.Sin(t));Line(vh,centre+d*340,centre+d*(i%5==0?356:347),1,A(Cyan,i%5==0?.45f:.14f));
                }
            }
            for(float scan=r.yMin;scan<r.yMax;scan+=18)Quad(vh,new Rect(r.xMin,scan,r.width,1),new Color(0,0,0,.13f));
            Line(vh,new Vector2(-430,735),new Vector2(-265,735),2,A(Accent,.4f));Line(vh,new Vector2(265,735),new Vector2(430,735),2,A(Accent,.4f));
        }

        private void Atmosphere(VertexHelper vh,Rect r)
        {
            // Soft, mode-specific Home lighting; no camera, gameplay or input changes.
            for(int i=0;i<12;i++)
            {
                float side=i%2==0?-1:1;float y=-820+i*145;float x=side*(420+Mathf.Sin(i*2.1f)*25);
                var point=new Vector2(x,y+Mathf.Sin(Motion*Mathf.PI*2+i)*18);
                Color light=Variant==2?Green:Variant==1?Gold:Cyan;
                Glow(vh,point,Variant==2?34:54,A(light,.06f));
                if(Variant==2){Quad(vh,new Rect(point.x-2,point.y-2,4,4),A(light,.32f));}
                else {Line(vh,point-new Vector2(0,7),point+new Vector2(0,7),1,A(light,.2f));}
            }
            if(Variant==0)
            {
                // Layered vista silhouettes and thin high-altitude cloud banks
                // move at separate speeds while keeping the hero/text clear.
                for(int layer=0;layer<2;layer++)for(int i=0;i<11;i++)
                {
                    float x=Mathf.Repeat(i*108+layer*45-Motion*(layer+1)*48+590,1180)-590;
                    float height=24+Mathf.Sin(i*2.17f+layer)*14;
                    Quad(vh,new Rect(x,120-layer*22,24+layer*8,height),new Color(.05f,.12f,.24f,layer==0?.15f:.23f));
                    Line(vh,new Vector2(x+12,120-layer*22+height),new Vector2(x+12,142-layer*22+height),1,A(Cyan,.18f));
                }
                for(int i=0;i<3;i++)
                {
                    float x=Mathf.Repeat(i*410-Motion*65+640,1280)-640;
                    Glow(vh,new Vector2(x,485+i*40),160,new Color(.36f,.69f,.92f,.028f));
                }
                float t=Motion;var star=Vector2.Lerp(new Vector2(-430,730),new Vector2(435,350),t);
                GlowLine(vh,star-new Vector2(60,-27),star,1,A(Cyan,.14f*Mathf.Sin(t*Mathf.PI)));
            }
            if(Variant==2)
            {
                float y=Mathf.Lerp(145,590,Motion);Line(vh,new Vector2(-355,y),new Vector2(355,y),1,A(Green,.075f));
                for(int i=0;i<4;i++){var at=new Vector2((i%2==0?-1:1)*370,210+(i/2)*250);Ring(vh,at,14,1,A(Green,.22f),0,360);Ring(vh,at,19,2,A(Green,.45f),Motion*360+i*90,70);}
            }
            Glow(vh,new Vector2(-480,390),290,A(Variant==2?Green:Cyan,.065f));
            Glow(vh,new Vector2(490,-420),260,A(Variant==1?Gold:Variant==2?Green:new Color(.7f,.3f,1),.045f));
        }
        private static void Ellipse(VertexHelper vh,Vector2 c,float rx,float ry,float width,Color colour,float angle,float span)
        {
            int steps=Mathf.Max(3,Mathf.CeilToInt(span/6));var previous=c+new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad)*rx,Mathf.Sin(angle*Mathf.Deg2Rad)*ry);
            for(int i=1;i<=steps;i++){float t=(angle+span*i/steps)*Mathf.Deg2Rad;var next=c+new Vector2(Mathf.Cos(t)*rx,Mathf.Sin(t)*ry);Line(vh,previous,next,width,colour);previous=next;}
        }
        private void Panel(VertexHelper vh,Rect r)
        {
            Color accent=color;accent.a=1;
            var shadow=new Rect(r.xMin-2,r.yMin-9,r.width+4,r.height+4);Bevel(vh,shadow,18,new Color(0,0,0,.65f),new Color(0,0,0,.65f));
            var bottom=Variant==2?new Color(.006f,.034f,.024f):new Color(.013f,.027f,.046f);var top=Variant==2?new Color(.035f,.13f,.093f):new Color(.085f,.15f,.20f);
            if(Primary){bottom=Color.Lerp(bottom,accent,.14f);top=Color.Lerp(top,accent,.28f);}
            Bevel(vh,r,18,bottom,top);Border(vh,r,18,1.1f,A(accent,.55f));Border(vh,new Rect(r.xMin+5,r.yMin+5,r.width-10,r.height-10),14,.7f,new Color(1,1,1,.09f));
            var glass=new Rect(r.xMin+8,r.yMax-r.height*.42f,r.width-16,r.height*.42f-8);
            Gradient(vh,glass,new Color(.65f,.91f,1,0),new Color(.65f,.91f,1,Primary?.065f:.032f));
            for(int i=0;i<3;i++)Line(vh,new Vector2(r.xMin+18,r.yMin+17+i*3),new Vector2(r.xMax-18,r.yMin+17+i*3),.5f,new Color(1,1,1,.018f));
            Line(vh,new Vector2(r.xMin+24,r.yMax-9),new Vector2(r.xMax-28,r.yMax-9),.7f,new Color(.75f,.96f,1,.13f));
            GlowLine(vh,new Vector2(r.xMin+24,r.yMax),new Vector2(r.xMin+Mathf.Min(r.width*.44f,205),r.yMax),2,A(accent,.8f));Quad(vh,new Rect(r.xMax-46,r.yMin+10,24,2),A(accent,.62f));
            if(Ambient){float x=r.xMin+26+(r.width-64)*Motion;Glow(vh,new Vector2(x,r.yMax-2),13,A(accent,.24f));}
        }
        private void Emblem(VertexHelper vh,Rect r)
        {
            var c=r.center;float radius=Mathf.Min(r.width,r.height)*.4f;
            Glow(vh,c,radius*1.3f,A(Accent,.13f));Ring(vh,c,radius,1,A(Accent,.35f),0,360);
            Ring(vh,c,radius*.84f,3,A(Accent,.82f),Motion*360,80);Ring(vh,c,radius*.84f,3,A(Accent,.82f),Motion*360+180,80);
            if(Variant==2)
            {
                for(int row=-1;row<=1;row++)for(int col=-1;col<=1;col++)Quad(vh,new Rect(c.x+col*radius*.38f-7,c.y+row*radius*.38f-7,14,14),A(Green,row==0&&col==0?1f:.3f));
                Line(vh,c+new Vector2(-radius*.4f,0),c+new Vector2(radius*.4f,0),2,Green);Line(vh,c+new Vector2(0,-radius*.4f),c+new Vector2(0,radius*.4f),2,Green);
            }
            else if(Variant==1)
            {
                var left=c-new Vector2(radius*.32f,0);var right=c+new Vector2(radius*.32f,0);
                Gradient(vh,new Rect(left.x-7,c.y-radius*.5f,14,radius),A(Cyan,.3f),Cyan);Gradient(vh,new Rect(right.x-7,c.y-radius*.5f,14,radius),A(Cyan,.3f),Cyan);
                for(int i=-1;i<=1;i++)GlowLine(vh,new Vector2(left.x,c.y+i*radius*.25f),new Vector2(right.x,c.y+i*radius*.25f),2,Gold);Diamond(vh,c,radius*.16f,Gold);
            }
            else
            {
                for(int side=-1;side<=1;side+=2)for(int feather=0;feather<3;feather++)Line(vh,c+new Vector2(side*radius*.08f,-feather*radius*.14f),c+new Vector2(side*(radius*.69f-feather*radius*.1f),radius*.31f-feather*radius*.18f),7,Cyan);
            }
        }

        private static void Quad(VertexHelper vh,Rect r,Color c)=>Gradient(vh,r,c,c);
        private static void Gradient(VertexHelper vh,Rect r,Color bottom,Color top)
        {
            int start=vh.currentVertCount;Vert(vh,new Vector2(r.xMin,r.yMin),bottom);Vert(vh,new Vector2(r.xMin,r.yMax),top);Vert(vh,new Vector2(r.xMax,r.yMax),top);Vert(vh,new Vector2(r.xMax,r.yMin),bottom);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        private static void Vert(VertexHelper vh,Vector2 p,Color c){var v=UIVertex.simpleVert;v.color=c;v.position=p;vh.AddVert(v);}
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {
            var normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int start=vh.currentVertCount;Vert(vh,a-normal,c);Vert(vh,a+normal,c);Vert(vh,b+normal,c);Vert(vh,b-normal,c);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        private static void GlowLine(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c){Line(vh,a,b,width+12,A(c,c.a*.035f));Line(vh,a,b,width+6,A(c,c.a*.09f));Line(vh,a,b,width+2,A(c,c.a*.25f));Line(vh,a,b,width,c);}
        private static void Glow(VertexHelper vh,Vector2 c,float radius,Color colour)
        {
            int start=vh.currentVertCount;Vert(vh,c,colour);for(int i=0;i<=32;i++){float t=i*Mathf.PI*2/32;Vert(vh,c+new Vector2(Mathf.Cos(t),Mathf.Sin(t))*radius,A(colour,0));}for(int i=0;i<32;i++)vh.AddTriangle(start,start+i+1,start+i+2);
        }
        private static void Ring(VertexHelper vh,Vector2 c,float radius,float width,Color colour,float angle,float span)
        {
            int steps=Mathf.Max(3,Mathf.CeilToInt(span/6));var old=c+new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad))*radius;
            for(int i=1;i<=steps;i++){float t=(angle+span*i/steps)*Mathf.Deg2Rad;var next=c+new Vector2(Mathf.Cos(t),Mathf.Sin(t))*radius;Line(vh,old,next,width,colour);old=next;}
        }

        private static void Diamond(VertexHelper vh,Vector2 c,float size,Color colour){int start=vh.currentVertCount;Vert(vh,c+Vector2.left*size,colour);Vert(vh,c+Vector2.up*size,colour);Vert(vh,c+Vector2.right*size,colour);Vert(vh,c+Vector2.down*size,colour);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);}
        private static void Polygon(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color colour){int n=vh.currentVertCount;Vert(vh,a,colour);Vert(vh,b,colour);Vert(vh,c,colour);Vert(vh,d,colour);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
        private static Vector2 Corner(Rect r,float cut,int i){switch(i){case 0:return new Vector2(r.xMin,r.yMin+cut);case 1:return new Vector2(r.xMin,r.yMax);case 2:return new Vector2(r.xMax-cut,r.yMax);case 3:return new Vector2(r.xMax,r.yMax-cut);case 4:return new Vector2(r.xMax,r.yMin);default:return new Vector2(r.xMin+cut,r.yMin);}}
        private static void Border(VertexHelper vh,Rect r,float cut,float width,Color colour){for(int i=0;i<6;i++)Line(vh,Corner(r,cut,i),Corner(r,cut,(i+1)%6),width,colour);}
        private static void Bevel(VertexHelper vh,Rect r,float cut,Color bottom,Color top)
        {
            int start=vh.currentVertCount;Vert(vh,r.center,Color.Lerp(bottom,top,.5f));for(int i=0;i<6;i++){var p=Corner(r,cut,i);Vert(vh,p,Color.Lerp(bottom,top,(p.y-r.yMin)/r.height));}for(int i=0;i<6;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%6);
        }
    }
}
