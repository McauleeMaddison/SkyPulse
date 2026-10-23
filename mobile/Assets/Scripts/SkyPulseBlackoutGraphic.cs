using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public enum BlackoutSurface { Backdrop, Deck=2, Trace=5, MiniMap, Network }
    public sealed class SkyPulseBlackoutGraphic : MaskableGraphic
    {
        public BlackoutSurface Surface;
        public bool ReducedMotion,MotionPaused,Animate=true;
        public float TraceFraction;
        public BlackoutPuzzle Miniature;
        public int RestoredMask,ActiveVault;
        public float LivePower;
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
                case BlackoutSurface.Deck:Deck(vh,r);break;
                case BlackoutSurface.Trace:Trace(vh,r);break;
                case BlackoutSurface.Network:Network(vh,r);break;
                default:MiniMap(vh,r);break;
            }
        }
        static Color A(Color c,float a){c.a=a;return c;}
        void Backdrop(VertexHelper vh,Rect r)
        {
            Gradient(vh,r,new Color(.002f,.014f,.025f,.80f),new Color(.012f,.046f,.052f,.66f));
            Glow(vh,new Vector2(0,130),r.width*.70f,A(Mint,.10f));
            Glow(vh,new Vector2(-r.width*.45f,650),r.width*.40f,A(Ice,.045f));
            Glow(vh,new Vector2(r.width*.45f,-540),r.width*.48f,A(Mint,.055f));
            for(int i=-6;i<=6;i++)
            {
                var a=new Vector2(i*38,-280);var b=new Vector2(i*164,r.yMin);
                Line(vh,a,b,1,A(Mint,.055f));
            }
            for(int i=0;i<7;i++)
            {
                float y=-340-i*i*18;Line(vh,new Vector2(r.xMin,y),new Vector2(r.xMax,y),1,A(Ice,.035f));
            }
            for(int side=-1;side<=1;side+=2)for(int i=0;i<9;i++)
            {
                float y=r.yMin+(i+1)*r.height/10;var a=new Vector2(side*r.width*.49f,y);var b=new Vector2(side*r.width*.41f,y+44);
                Line(vh,a,b,1,A(Mint,.14f));var dot=Vector2.Lerp(a,b,Mathf.Repeat(Clock+i*.173f,1));Glow(vh,dot,10,A(Mint,.28f));
                Quad(vh,new Rect(dot.x-2,dot.y-2,4,4),A(Mint,.7f));
            }
        }

        void Deck(VertexHelper vh,Rect r)
        {
            Bevel(vh,new Rect(r.xMin+2,r.yMin-9,r.width,r.height),14,new Color(0,0,0,.65f),new Color(0,0,0,.65f));
            Bevel(vh,r,14,new Color(.013f,.025f,.035f,.96f),new Color(.052f,.085f,.10f,.94f));
            Border(vh,r,14,1,A(Ice,.17f));
            Gradient(vh,new Rect(r.xMin+8,r.yMax-r.height*.4f,r.width-16,r.height*.4f-7),A(Ice,0),A(Ice,.055f));
            Line(vh,new Vector2(r.xMin+23,r.yMax-2),new Vector2(r.xMax-23,r.yMax-2),1,A(Ice,.25f));
            Line(vh,new Vector2(r.xMin+23,r.yMax-2),new Vector2(r.xMin+r.width*.25f,r.yMax-2),1,A(Mint,.7f));
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
        void Network(VertexHelper vh,Rect r)
        {
            // Each district represents a persisted vault completion. The active
            // district also responds to live core/uplink power before extraction.
            float step=r.width/8;
            for(int i=0;i<8;i++)
            {
                bool restored=(RestoredMask&(1<<i))!=0,active=i==ActiveVault;
                var point=new Vector2(r.xMin+step*(i+.5f),-3);
                var hue=restored?Mint:active?Amber:Ice;
                if(i<7)Line(vh,point+Vector2.right*25,point+Vector2.right*(step-25),2,A(restored?Mint:Ice,restored?.60f:.10f));
                float power=restored?1:active?.20f+LivePower*.55f:.07f;
                if(active){Ring(vh,point,25,1.3f,A(hue,.65f));if(!ReducedMotion)Glow(vh,point,31,A(hue,.05f+.035f*Mathf.Sin(Clock*6.28f)));}
                for(int tower=0;tower<3;tower++)
                {
                    float x=point.x+(tower-1)*10,h=tower==1?29:20;
                    var face=new Rect(x-4,-13,8,h);
                    Gradient(vh,face,new Color(.02f,.04f,.06f,.8f),A(hue,.10f+power*.22f));
                    Line(vh,new Vector2(x-4,-13),new Vector2(x-4,-13+h),1,A(hue,.20f+power*.45f));
                    for(int floor=0;floor<3;floor++)Quad(vh,new Rect(x-1,-8+floor*6,3,2),A(hue,power));
                }
                Line(vh,point+new Vector2(-18,-16),point+new Vector2(18,-16),1.5f,A(hue,.18f+power*.55f));
            }
        }

        static void Vert(VertexHelper vh,Vector2 p,Color c){var v=UIVertex.simpleVert;v.position=p;v.color=c;vh.AddVert(v);}
        static void Polygon(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint){int n=vh.currentVertCount;Vert(vh,a,tint);Vert(vh,b,tint);Vert(vh,c,tint);Vert(vh,d,tint);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
        static Vector2 Corner(Rect r,float cut,int i){switch(i){case 0:return new Vector2(r.xMin,r.yMin+cut);case 1:return new Vector2(r.xMin,r.yMax);case 2:return new Vector2(r.xMax-cut,r.yMax);case 3:return new Vector2(r.xMax,r.yMax-cut);case 4:return new Vector2(r.xMax,r.yMin);default:return new Vector2(r.xMin+cut,r.yMin);}}
        static void Border(VertexHelper vh,Rect r,float cut,float width,Color c){for(int i=0;i<6;i++)Line(vh,Corner(r,cut,i),Corner(r,cut,(i+1)%6),width,c);}
        static void Bevel(VertexHelper vh,Rect r,float cut,Color lower,Color upper){int n=vh.currentVertCount;Vert(vh,r.center,Color.Lerp(lower,upper,.5f));for(int i=0;i<6;i++){var v=Corner(r,cut,i);Vert(vh,v,Color.Lerp(lower,upper,Mathf.InverseLerp(r.yMin,r.yMax,v.y)));}for(int i=0;i<6;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%6);}
        static void Quad(VertexHelper vh,Rect r,Color c)=>Gradient(vh,r,c,c);
        static void Gradient(VertexHelper vh,Rect r,Color lower,Color upper){int n=vh.currentVertCount;Vert(vh,new Vector2(r.xMin,r.yMin),lower);Vert(vh,new Vector2(r.xMin,r.yMax),upper);Vert(vh,new Vector2(r.xMax,r.yMax),upper);Vert(vh,new Vector2(r.xMax,r.yMin),lower);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float w,Color c){var n=new Vector2(-(b-a).y,(b-a).x).normalized*w*.5f;Polygon(vh,a-n,a+n,b+n,b-n,c);}
        static void Diamond(VertexHelper vh,Vector2 c,float s,Color tint)=>Polygon(vh,c+Vector2.left*s,c+Vector2.up*s,c+Vector2.right*s,c+Vector2.down*s,tint);
        static void Ring(VertexHelper vh,Vector2 c,float radius,float width,Color tint)=>Ellipse(vh,c,radius,1,width,tint,0,360);
        static void Ellipse(VertexHelper vh,Vector2 c,float radius,float sy,float width,Color tint,float angle,float span)
        {
            int steps=Mathf.Max(3,Mathf.CeilToInt(span/7));Vector2 old=c+new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad)*sy)*radius;
            for(int i=1;i<=steps;i++){float t=(angle+span*i/steps)*Mathf.Deg2Rad;var next=c+new Vector2(Mathf.Cos(t),Mathf.Sin(t)*sy)*radius;Line(vh,old,next,width,tint);old=next;}
        }
        static void Glow(VertexHelper vh,Vector2 c,float radius,Color tint){int n=vh.currentVertCount;Vert(vh,c,tint);for(int i=0;i<=24;i++){float t=i*Mathf.PI*2/24;Vert(vh,c+new Vector2(Mathf.Cos(t),Mathf.Sin(t))*radius,A(tint,0));}for(int i=0;i<24;i++)vh.AddTriangle(n,n+i+1,n+i+2);}
    }
}
