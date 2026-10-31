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
        // Assigned by the menu at runtime; puzzle definitions are not scene data.
        [System.NonSerialized] public BlackoutPuzzle Miniature;
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
            // Distinct infrastructure glyphs make the campaign's eight real
            // restoration targets visible. Only persisted medals light a whole
            // system; live core/uplink power illuminates the current target.
            float step=r.width/8;
            for(int i=0;i<8;i++)
            {
                bool restored=(RestoredMask&(1<<i))!=0,active=i==ActiveVault;
                var centre=new Vector2(r.xMin+step*(i+.5f),0);
                Color hue=restored?Mint:active?Amber:Ice;
                float power=restored?1:active?.18f+LivePower*.62f:.06f;
                if(i<7)
                {
                    var a=centre+Vector2.right*29;var b=centre+Vector2.right*(step-29);
                    Line(vh,a,b,1.2f,A(restored?Mint:Ice,restored?.34f:.065f));
                    if(restored&&!ReducedMotion)
                    {
                        var at=Vector2.Lerp(a,b,Mathf.Repeat(Clock+i*.13f,1));Glow(vh,at,5,A(Mint,.16f));
                        Quad(vh,new Rect(at.x-1,at.y-1,2,2),A(Mint,.65f));
                    }
                }
                if(active)
                {
                    Ellipse(vh,centre,25,1,1,A(hue,.47f),24,312);
                    if(!ReducedMotion)Glow(vh,centre,29,A(hue,.025f+.025f*Mathf.Sin(Clock*6.283f)));
                }
                SystemGlyph(vh,centre,i,hue,power);
                Line(vh,centre+new Vector2(-17,-20),centre+new Vector2(17,-20),1.2f,A(hue,.08f+power*.50f));
            }
        }
        static void SystemGlyph(VertexHelper vh,Vector2 p,int system,Color hue,float power)
        {
            Color line=A(hue,.20f+power*.62f),dark=A(hue,.05f+power*.12f),lit=A(hue,.18f+power*.78f);
            switch(system)
            {
                case 0: // Dock beacons: three towers, each with a lens.
                    for(int i=-1;i<=1;i++)
                    {
                        float h=i==0?17:10;var at=p+new Vector2(i*11,0);
                        Line(vh,at+Vector2.down*14,at+Vector2.up*h,1.4f,line);
                        Diamond(vh,at+Vector2.up*h,2.8f,lit);
                        Line(vh,at+new Vector2(-3,-14),at+new Vector2(3,-14),1,line);
                    }break;
                case 1: // Cargo link: paired secured containers.
                    for(int i=-1;i<=1;i++)
                    {
                        var box=new Rect(p.x+i*11-4,p.y-10,8,i==0?24:18);Quad(vh,box,dark);Border(vh,box,1,1,line);
                        Line(vh,new Vector2(box.center.x,box.yMin+3),new Vector2(box.center.x,box.yMax-3),1,lit);
                    }break;
                case 2: // Transit grid.
                    for(int i=-1;i<=1;i++)Line(vh,p+new Vector2(-18,i*9),p+new Vector2(18,i*9),1.3f,line);
                    for(int i=-1;i<=1;i++)Line(vh,p+new Vector2(i*12,-13),p+new Vector2(i*12,13),1,line);
                    Diamond(vh,p,3,lit);break;
                case 3: // Relay station dish and centre mast.
                    Ellipse(vh,p+Vector2.up*3,14,.62f,1.4f,line,0,360);Line(vh,p+Vector2.down*15,p+Vector2.up*15,1.2f,line);
                    Diamond(vh,p+Vector2.up*3,3,lit);Line(vh,p+new Vector2(-11,-16),p+new Vector2(11,-16),1,line);break;
                case 4: // Power exchange: three coupled cores.
                    Diamond(vh,p,10,dark);Diamond(vh,p,5,lit);
                    for(int i=-1;i<=1;i+=2){Diamond(vh,p+Vector2.right*i*15,4,line);Line(vh,p+Vector2.right*i*5,p+Vector2.right*i*11,1.2f,line);}break;
                case 5: // Skyway bridge.
                    for(int i=-1;i<=1;i+=2)Line(vh,p+new Vector2(i*14,-15),p+new Vector2(i*14,16),1.5f,line);
                    Line(vh,p+new Vector2(-20,-3),p+new Vector2(20,-3),2,lit);Line(vh,p+new Vector2(-14,14),p+new Vector2(0,-3),1,line);Line(vh,p+new Vector2(14,14),p+new Vector2(0,-3),1,line);break;
                case 6: // City shield.
                    Ellipse(vh,p+Vector2.down*7,19,1,1.4f,line,0,180);Line(vh,p+new Vector2(-20,-8),p+new Vector2(20,-8),1.5f,lit);
                    for(int i=-1;i<=1;i++)Line(vh,p+new Vector2(i*9,-8),p+new Vector2(i*9,5-Mathf.Abs(i)*3),2,line);break;
                default: // SkyPulse uplink satellite.
                    Diamond(vh,p,5,lit);Line(vh,p+Vector2.down*15,p+Vector2.up*15,1.2f,line);
                    for(int i=-1;i<=1;i+=2){var box=new Rect(p.x+i*13-6,p.y-8,12,16);Quad(vh,box,dark);Border(vh,box,1,1,line);Line(vh,p+Vector2.right*i*5,p+Vector2.right*i*8,1,line);}break;
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
