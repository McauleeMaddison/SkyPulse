using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public enum SkyPulseSurface { Emblem, Backdrop, Panel, TerminalNode }

    // Authored geometry: gradients, soft light, machined frames and pixel glyphs.
    // No external artwork, shader, font or generation service is required.
    public sealed class SkyPulseCircuitGraphic : MaskableGraphic
    {
        public int Variant, NodeKind;
        public SkyPulseSurface Surface;
        public bool Ambient, ReducedMotion, MotionPaused, Adjacent, Primary, CinematicUnderlay;
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
            else if(Surface==SkyPulseSurface.TerminalNode)Node(vh,r);
            else Emblem(vh,r);
        }
        private static Color A(Color c,float alpha){c.a=alpha;return c;}
        private void Backdrop(VertexHelper vh,Rect r)
        {
            bool terminal=Variant==2;
            float opacity=CinematicUnderlay?.62f:1f;
            Gradient(vh,r,terminal?new Color(.002f,.014f,.011f):new Color(.014f,.035f,.075f,opacity),terminal?new Color(.006f,.047f,.034f):new Color(.017f,.10f,.14f,opacity));
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
                    Gradient(vh,new Rect(side<0?r.xMin:r.xMax-84,r.yMin,84,r.height),new Color(.024f,.06f,.10f),new Color(.005f,.02f,.04f));
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
        private void Panel(VertexHelper vh,Rect r)
        {
            Color accent=color;accent.a=1;
            var shadow=new Rect(r.xMin-2,r.yMin-9,r.width+4,r.height+4);Bevel(vh,shadow,18,new Color(0,0,0,.65f),new Color(0,0,0,.65f));
            var bottom=Variant==2?new Color(.012f,.065f,.045f):new Color(.025f,.055f,.095f);var top=Variant==2?new Color(.025f,.11f,.073f):new Color(.075f,.13f,.18f);
            if(Primary){bottom=Color.Lerp(bottom,accent,.14f);top=Color.Lerp(top,accent,.28f);}
            Bevel(vh,r,18,bottom,top);Border(vh,r,18,1.1f,A(accent,.55f));Border(vh,new Rect(r.xMin+5,r.yMin+5,r.width-10,r.height-10),14,.7f,new Color(1,1,1,.09f));
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
        private void Node(VertexHelper vh,Rect r)
        {
            bool wall=NodeKind==1;var c=r.center;float s=Mathf.Min(r.width,r.height);Color accent=NodeKind==3?Gold:NodeKind==5?Color.white:Green;
            Bevel(vh,r,6,wall?new Color(.009f,.02f,.022f):new Color(.012f,.05f,.034f),wall?new Color(.022f,.039f,.035f):new Color(.025f,.12f,.075f));Border(vh,r,6,Adjacent?1.8f:.8f,A(accent,wall?.08f:Adjacent?.85f:.23f));
            if(wall){for(int i=-2;i<=2;i++)Line(vh,c+new Vector2(-s*.25f,i*s*.12f),c+new Vector2(s*.25f,i*s*.12f),2,A(Green,.11f));return;}
            var icon=c+new Vector2(0,12);float radius=s*.18f;
            if(NodeKind==5 || NodeKind==2)Glow(vh,icon,s*.40f,A(accent,.10f+Mathf.Sin(Motion*Mathf.PI*2)*.03f));
            if(NodeKind==2 || NodeKind==6)
            {
                Diamond(vh,icon,radius,A(Green,NodeKind==6?.28f:.85f));Diamond(vh,icon,radius*.47f,new Color(.8f,1f,.86f,NodeKind==6?.4f:1));for(int i=0;i<3;i++)Quad(vh,new Rect(icon.x-radius-5,icon.y-9+i*8,3,3),Green);
            }
            else if(NodeKind==3)
            {
                for(int row=-1;row<=1;row++)for(int col=0;col<2;col++)Quad(vh,new Rect(icon.x-radius+col*radius+(row==0?0:3),icon.y+row*9-3,radius-3,6),A(Gold,.82f));Line(vh,icon+new Vector2(-radius-6,-18),icon+new Vector2(-radius-6,18),2,A(Gold,.4f));
            }
            else if(NodeKind==5)
            {
                Ring(vh,icon,radius+5,1.5f,A(Green,.8f),Motion*360,145);Ring(vh,icon,radius+5,1.5f,A(Green,.8f),Motion*360+180,145);Diamond(vh,icon,radius*.72f,Color.white);Diamond(vh,icon,radius*.26f,Green);Quad(vh,new Rect(r.xMin+4,r.yMax-8,18,2),Green);Quad(vh,new Rect(r.xMax-22,r.yMin+6,18,2),Green);
            }
            else if(NodeKind==4)
            {
                Border(vh,new Rect(icon.x-radius,icon.y-radius,radius*2,radius*2),2,2,Green);Line(vh,icon-new Vector2(8,0),icon+new Vector2(8,0),2,Color.white);Line(vh,icon+new Vector2(4,-5),icon+new Vector2(9,0),2,Color.white);Line(vh,icon+new Vector2(4,5),icon+new Vector2(9,0),2,Color.white);
            }
            else{Ring(vh,icon,NodeKind==7?radius:7,1.4f,A(Green,.45f),0,360);Quad(vh,new Rect(icon.x-2,icon.y-2,4,4),A(Green,.6f));}
            if(Adjacent)for(int side=-1;side<=1;side+=2)Line(vh,c+new Vector2(side*s*.4f,-s*.30f),c+new Vector2(side*s*.4f,-s*.12f),2,A(accent,.7f));
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
        private static Vector2[] Corners(Rect r,float cut)=>new[]{new Vector2(r.xMin,r.yMin+cut),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax-cut,r.yMax),new Vector2(r.xMax,r.yMax-cut),new Vector2(r.xMax,r.yMin),new Vector2(r.xMin+cut,r.yMin)};
        private static void Border(VertexHelper vh,Rect r,float cut,float width,Color colour){var points=Corners(r,cut);for(int i=0;i<points.Length;i++)Line(vh,points[i],points[(i+1)%points.Length],width,colour);}
        private static void Bevel(VertexHelper vh,Rect r,float cut,Color bottom,Color top)
        {
            int start=vh.currentVertCount;Vert(vh,r.center,Color.Lerp(bottom,top,.5f));var points=Corners(r,cut);foreach(var p in points)Vert(vh,p,Color.Lerp(bottom,top,(p.y-r.yMin)/r.height));for(int i=0;i<points.Length;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%points.Length);
        }
    }
}
