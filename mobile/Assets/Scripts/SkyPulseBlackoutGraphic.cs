using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public enum BlackoutSurface { Backdrop, Trace=5, MiniMap }
    public sealed class SkyPulseBlackoutGraphic : MaskableGraphic
    {
        public BlackoutSurface Surface;
        public bool ReducedMotion,MotionPaused,Quiet,Animate=true;
        public float TraceFraction;
        // Assigned by the menu at runtime; puzzle definitions are not scene data.
        [System.NonSerialized] public BlackoutPuzzle Miniature;
        public float Phase { get; private set; }
        bool previousReduced;
        float Clock=>ReducedMotion?0:Phase;
        static readonly Color Mint=new Color(.30f,1,.75f),Ice=new Color(.59f,.91f,1),Amber=new Color(1,.65f,.26f),Red=new Color(1,.33f,.37f),Violet=new Color(.72f,.53f,1);
        void Update()
        {
            if(MotionPaused)return;bool dirty=previousReduced!=ReducedMotion;previousReduced=ReducedMotion;
            if(Animate&&!ReducedMotion){Phase=Mathf.Repeat(Phase+Time.unscaledDeltaTime*.22f,1);dirty=true;}if(dirty)SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            switch(Surface)
            {
                case BlackoutSurface.Backdrop:Backdrop(vh,r);break;
                case BlackoutSurface.Trace:Trace(vh,r);break;
                default:MiniMap(vh,r);break;
            }
        }
        static Color A(Color c,float a){c.a=a;return c;}
        void Backdrop(VertexHelper vh,Rect r)
        {
            if(Quiet)
            {
                Gradient(vh,r,new Color(.004f,.010f,.022f,1),new Color(.010f,.026f,.044f,1));
                for(int side=-1;side<=1;side+=2)
                {
                    var a=new Vector2(side*r.width*.485f,r.yMin);var b=new Vector2(side*r.width*.485f,r.yMax);
                    Line(vh,a,b,1,A(Ice,.09f));
                }
                return;
            }
            Gradient(vh,r,new Color(.003f,.010f,.018f,.88f),new Color(.008f,.025f,.035f,.80f));
            Glow(vh,new Vector2(0,130),r.width*.70f,A(Mint,.055f));
            Glow(vh,new Vector2(-r.width*.45f,650),r.width*.40f,A(Ice,.045f));
            Glow(vh,new Vector2(r.width*.45f,-540),r.width*.48f,A(Mint,.055f));
            for(int i=-6;i<=6;i++)
            {
                var a=new Vector2(i*38,-280);var b=new Vector2(i*164,r.yMin);
                Line(vh,a,b,1,A(Mint,.025f));
            }
            for(int i=0;i<7;i++)
            {
                float y=-340-i*i*18;Line(vh,new Vector2(r.xMin,y),new Vector2(r.xMax,y),1,A(Ice,.021f));
            }
            for(int side=-1;side<=1;side+=2)for(int i=0;i<9;i++)
            {
                float y=r.yMin+(i+1)*r.height/10;var a=new Vector2(side*r.width*.49f,y);var b=new Vector2(side*r.width*.41f,y+44);
                Line(vh,a,b,1,A(Mint,.14f));var dot=Vector2.Lerp(a,b,Mathf.Repeat(Clock+i*.173f,1));Glow(vh,dot,10,A(Mint,.28f));
                Quad(vh,new Rect(dot.x-2,dot.y-2,4,4),A(Mint,.7f));
            }
        }

        void Trace(VertexHelper vh,Rect r)
        {
            Color hue=TraceFraction>.85f?Red:TraceFraction>.65f?Amber:Mint;int n=48;float step=r.width/n;
            for(int i=0;i<n;i++)Quad(vh,new Rect(r.xMin+i*step,r.yMin,step-3,r.height),A(hue,(i+.5f)/n<=TraceFraction?.85f:.09f));
        }
        void MiniMap(VertexHelper vh,Rect r)
        {
            if(Miniature==null)return;float step=Mathf.Min(r.width/Miniature.Width,r.height/Miniature.Height);
            for(int i=0;i<Miniature.Count;i++)
            {
                char c=Miniature.Tile(i);if(c=='#')continue;var p=r.center+new Vector2((i%Miniature.Width-(Miniature.Width-1)*.5f)*step,((Miniature.Height-1)*.5f-i/Miniature.Width)*step);
                Color hue=c=='S'?Ice:c=='E'?Amber:c=='!'?Red:c=='Q'||c=='H'?Violet:Mint;
                if(c=='M')Line(vh,p-new Vector2(step*.23f,step*.23f),p+new Vector2(step*.23f,step*.23f),1.5f,hue);else Diamond(vh,p,step*.22f,A(hue,c=='.'?.15f:.7f));
            }
        }
        static void Vert(VertexHelper vh,Vector2 p,Color c){var v=UIVertex.simpleVert;v.position=p;v.color=c;vh.AddVert(v);}
        static void Polygon(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint){int n=vh.currentVertCount;Vert(vh,a,tint);Vert(vh,b,tint);Vert(vh,c,tint);Vert(vh,d,tint);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
        static void Quad(VertexHelper vh,Rect r,Color c)=>Gradient(vh,r,c,c);
        static void Gradient(VertexHelper vh,Rect r,Color lower,Color upper){int n=vh.currentVertCount;Vert(vh,new Vector2(r.xMin,r.yMin),lower);Vert(vh,new Vector2(r.xMin,r.yMax),upper);Vert(vh,new Vector2(r.xMax,r.yMax),upper);Vert(vh,new Vector2(r.xMax,r.yMin),lower);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float w,Color c){var n=new Vector2(-(b-a).y,(b-a).x).normalized*w*.5f;Polygon(vh,a-n,a+n,b+n,b-n,c);}
        static void Diamond(VertexHelper vh,Vector2 c,float s,Color tint)=>Polygon(vh,c+Vector2.left*s,c+Vector2.up*s,c+Vector2.right*s,c+Vector2.down*s,tint);
        static void Ellipse(VertexHelper vh,Vector2 c,float radius,float sy,float width,Color tint,float angle,float span)
        {
            int steps=Mathf.Max(3,Mathf.CeilToInt(span/7));Vector2 old=c+new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad)*sy)*radius;
            for(int i=1;i<=steps;i++){float t=(angle+span*i/steps)*Mathf.Deg2Rad;var next=c+new Vector2(Mathf.Cos(t),Mathf.Sin(t)*sy)*radius;Line(vh,old,next,width,tint);old=next;}
        }
        static void Glow(VertexHelper vh,Vector2 c,float radius,Color tint){int n=vh.currentVertCount;Vert(vh,c,tint);for(int i=0;i<=24;i++){float t=i*Mathf.PI*2/24;Vert(vh,c+new Vector2(Mathf.Cos(t),Mathf.Sin(t))*radius,A(tint,0));}for(int i=0;i<24;i++)vh.AddTriangle(n,n+i+1,n+i+2);}
    }
}
