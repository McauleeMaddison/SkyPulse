using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public enum BlackoutSurface { Backdrop, Core, Deck, Node, Network, Trace, MiniMap }

    // Original procedural geometry. No raster art, downloaded shader or font input.
    public sealed class SkyPulseBlackoutGraphic : MaskableGraphic
    {
        public BlackoutSurface Surface;
        public int NodeKind, ImpactKind;
        public bool Adjacent, Selected, ReducedMotion, MotionPaused, Animate=true, Celebrate, Powered=true, HasPhaseRouting;
        public int CircuitPhase;
        public float TraceFraction, Travel=1, Impact;
        public Vector2 Origin, Target;
        public Vector2[] EdgeFrom=new Vector2[0],EdgeTo=new Vector2[0],Trail=new Vector2[0];
        public bool[] EdgePowered=new bool[0];
        public BlackoutPuzzle Miniature;
        public float Phase { get; private set; }
        float Clock=>ReducedMotion?0:Phase;
        static readonly Color Mint=new Color(.30f,1,.75f),Ice=new Color(.59f,.91f,1),Amber=new Color(1,.65f,.26f),Red=new Color(1,.33f,.37f),Violet=new Color(.72f,.53f,1);
        bool previousReduced;
        void Update()
        {
            if(previousReduced!=ReducedMotion){previousReduced=ReducedMotion;SetVerticesDirty();}
            if(!Animate || ReducedMotion || MotionPaused)return;
            Phase=Mathf.Repeat(Phase+Time.unscaledDeltaTime*.20f,1);SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(Surface==BlackoutSurface.Backdrop)Backdrop(vh,r);
            else if(Surface==BlackoutSurface.Core)Core(vh,r);
            else if(Surface==BlackoutSurface.Deck)Deck(vh,r);
            else if(Surface==BlackoutSurface.Node)Node(vh,r);
            else if(Surface==BlackoutSurface.Network)Network(vh);
            else if(Surface==BlackoutSurface.Trace)Trace(vh,r);
            else MiniMap(vh,r);
        }
        static Color A(Color c,float a){c.a=a;return c;}
        void Backdrop(VertexHelper vh,Rect r)
        {
            Gradient(vh,r,new Color(.002f,.009f,.023f,.97f),new Color(.012f,.028f,.046f,.82f));
            Glow(vh,new Vector2(0,130),r.width*.70f,A(Mint,.085f));
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
        void Core(VertexHelper vh,Rect r)
        {
            float s=Mathf.Min(r.width,r.height);var c=r.center;float lift=ReducedMotion?0:Mathf.Sin(Clock*Mathf.PI*2)*8;
            Glow(vh,c,s*.49f,A(Mint,.14f));
            Ellipse(vh,c+new Vector2(0,-s*.28f),s*.39f,.24f,10,A(Ice,.10f),0,360);
            Ellipse(vh,c+new Vector2(0,-s*.28f),s*.39f,.24f,2,A(Mint,.65f),Clock*360,85);
            Ellipse(vh,c+new Vector2(0,-s*.25f),s*.32f,.24f,2,A(Mint,.28f),0,360);
            for(int i=0;i<7;i++)Line(vh,c+new Vector2((i-3)*38,-s*.23f),c+new Vector2((i-3)*16,0),1,A(Mint,.10f));
            var core=c+new Vector2(0,18+lift);Crystal(vh,core,s*.19f,Mint);
            for(int i=0;i<4;i++)
            {
                float y=core.y-s*.11f+i*s*.057f;
                Line(vh,new Vector2(core.x-s*.185f,y+s*.062f),new Vector2(core.x,y),1,A(Ice,.48f));
                Line(vh,new Vector2(core.x,y),new Vector2(core.x+s*.185f,y+s*.062f),1,A(Mint,.32f));
            }
            for(int side=-1;side<=1;side+=2)
            {
                var arm=core+new Vector2(side*s*.285f,-s*.055f);
                Polygon(vh,new[]{arm+new Vector2(-15,-42),arm+new Vector2(-15,42),arm+new Vector2(9,27),arm+new Vector2(9,-52)},new Color(.10f,.17f,.20f));
                Line(vh,arm+new Vector2(side*10,-31),arm+new Vector2(side*10,27),3,A(Mint,.75f));
                Crystal(vh,arm+new Vector2(0,67),s*.035f,Ice);
            }
            Ellipse(vh,core,s*.34f,.62f,1,A(Ice,.27f),-Clock*360,260);
            Ellipse(vh,core,s*.40f,.77f,2,A(Mint,.5f),Clock*360,66);
            Ellipse(vh,core,s*.40f,.77f,2,A(Mint,.5f),Clock*360+180,66);
            for(int i=0;i<5;i++)
            {
                float t=(Clock+i*.2f)*Mathf.PI*2;var p=core+new Vector2(Mathf.Cos(t)*s*.34f,Mathf.Sin(t)*s*.21f);
                Glow(vh,p,15,A(Mint,.3f));Diamond(vh,p,5,Mint);
            }
            if(Celebrate)
            {
                for(int i=0;i<9;i++){float x=(i-4)*s*.085f;Line(vh,c+new Vector2(x,-s*.20f),c+new Vector2(x,s*.38f),1,A(Mint,.10f));}
            }
        }
        void Deck(VertexHelper vh,Rect r)
        {
            var shadow=new Rect(r.xMin+5,r.yMin-10,r.width,r.height);Bevel(vh,shadow,18,new Color(0,.003f,.01f,.7f),new Color(0,.008f,.015f,.7f));
            Bevel(vh,r,18,new Color(.012f,.022f,.034f),new Color(.038f,.059f,.078f));Border(vh,r,18,1.2f,A(Ice,.28f));
            var inset=new Rect(r.xMin+7,r.yMin+8,r.width-14,r.height-16);Bevel(vh,inset,14,new Color(.002f,.011f,.017f),new Color(.012f,.025f,.032f));
            // Narrow brushed facets and glass glints retain the obsidian material.
            Polygon(vh,new[]{new Vector2(r.xMin+9,r.yMax-9),new Vector2(r.xMax-24,r.yMax-9),new Vector2(r.xMax-48,r.yMax-23),new Vector2(r.xMin+9,r.yMax-17)},A(Ice,.045f));
            Line(vh,new Vector2(r.xMin+28,r.yMin+11),new Vector2(r.xMax-28,r.yMin+11),1,A(Ice,.10f));
            Line(vh,new Vector2(r.xMin+20,r.yMax-2),new Vector2(r.xMax-20,r.yMax-2),2,A(Ice,.26f));
            Line(vh,new Vector2(r.xMin+20,r.yMax-7),new Vector2(r.xMin+r.width*.35f,r.yMax-7),2,A(Mint,.8f));
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*(r.width*.5f-14);Line(vh,new Vector2(x,r.yMin+28),new Vector2(x,r.yMin+r.height*.3f),1,A(Mint,.34f));
                foreach(float y in new[]{r.yMin+18,r.yMax-18}){Ring(vh,new Vector2(x,y),3,1,A(Ice,.22f));}
            }
        }
        void Node(VertexHelper vh,Rect r)
        {
            var c=r.center;float s=Mathf.Min(r.width,r.height);bool wall=NodeKind==1;
            Color accent=NodeKind==3||NodeKind==11?Amber:NodeKind==8?Red:NodeKind==5||NodeKind==12||NodeKind==14?Ice:NodeKind==13||NodeKind==15?Violet:Mint;
            var top=new Rect(c.x-s*.43f,c.y-s*.30f,s*.86f,s*.68f);
            Bevel(vh,new Rect(top.x+3,top.y-8,top.width,top.height),10,new Color(.001f,.006f,.012f),new Color(.016f,.025f,.037f));
            if(wall){Bevel(vh,top,10,new Color(.012f,.017f,.022f),new Color(.022f,.032f,.040f));for(int i=-2;i<=2;i++)Line(vh,c+new Vector2(-s*.24f,i*9),c+new Vector2(s*.24f,i*9),1,A(Ice,.08f));return;}
            if(Adjacent || Selected || NodeKind==5)Glow(vh,c,s*.55f,A(accent,Selected?.18f:Powered?.12f:.035f));
            Bevel(vh,top,10,new Color(.012f,.028f,.039f),Powered?new Color(.15f,.20f,.24f):new Color(.075f,.10f,.13f));Border(vh,top,10,Selected?2:1,A(accent,Selected?.9f:Adjacent&&Powered?.8f:Powered?.35f:.12f));
            var glass=new Rect(top.xMin+8,top.yMin+7,top.width-16,top.height-14);Bevel(vh,glass,7,new Color(.007f,.019f,.028f),new Color(.055f,.082f,.098f));
            Line(vh,new Vector2(glass.xMin+8,glass.yMax-1),new Vector2(glass.xMax-8,glass.yMax-1),1,A(accent,Powered?.34f:.08f));
            Line(vh,new Vector2(top.xMin+12,top.yMax-1),new Vector2(top.xMax-12,top.yMax-1),2,A(Ice,.22f));
            Polygon(vh,new[]{new Vector2(top.xMax,top.yMin),new Vector2(top.xMax+4,top.yMin-6),new Vector2(top.xMax+4,top.yMax-10),new Vector2(top.xMax,top.yMax-10)},new Color(.036f,.061f,.077f));
            Line(vh,c+new Vector2(-s*.3f,-s*.32f),c+new Vector2(s*.3f,-s*.32f),3,A(accent,Adjacent?.5f:.12f));
            var icon=c+new Vector2(0,s*.07f);float lift=ReducedMotion?0:Mathf.Sin((Clock+.14f)*Mathf.PI*2)*2;
            if(NodeKind==2 || NodeKind==5)
            {
                Glow(vh,icon,s*.38f,A(accent,.25f));Crystal(vh,icon+new Vector2(0,lift),s*.18f,accent);
                Ellipse(vh,icon,s*.30f,.50f,1,A(accent,.45f),Clock*360,125);
                for(int i=0;i<3;i++){float t=(Clock+i/3f)*Mathf.PI*2;Diamond(vh,icon+new Vector2(Mathf.Cos(t)*s*.27f,Mathf.Sin(t)*s*.14f),1.8f,A(Ice,.7f));}
            }
            else if(NodeKind==6){Ring(vh,icon,s*.16f,1,A(Mint,.36f));Line(vh,icon+new Vector2(-9,0),icon+new Vector2(-2,-6),2,Mint);Line(vh,icon+new Vector2(-2,-6),icon+new Vector2(11,8),2,Mint);}
            else if(NodeKind==3)
            {
                for(int i=-1;i<=1;i++){var bar=new Rect(icon.x-s*.22f,icon.y+i*10-2,s*.44f,4);Quad(vh,bar,A(Amber,.8f));Glow(vh,bar.center,s*.24f,A(Amber,.05f));}
                Ring(vh,icon,s*.29f,1,A(Amber,Selected?.65f:.18f));
            }
            else if(NodeKind==4)
            {
                Ring(vh,icon,s*.24f,2,A(Mint,.7f));Ellipse(vh,icon,s*.29f,.76f,1,A(Mint,.5f),Clock*360,80);
                Line(vh,icon+Vector2.down*11,icon+Vector2.up*11,2,Mint);Line(vh,icon+Vector2.up*11,icon+new Vector2(-7,3),2,Mint);Line(vh,icon+Vector2.up*11,icon+new Vector2(7,3),2,Mint);
            }
            else if(NodeKind==8)
            {
                Glow(vh,icon,s*.3f,A(Red,.09f));Diamond(vh,icon,s*.19f,Red);Line(vh,icon+new Vector2(0,-6),icon+new Vector2(0,5),2,new Color(.12f,.02f,.025f));
                Ellipse(vh,icon,s*.28f,.65f,1,A(Red,.55f),Clock*360,120);
            }
            else if(NodeKind==9)
            {
                Glow(vh,icon,s*.34f,A(CircuitPhase==0?Mint:Amber,.21f));
                Ellipse(vh,icon,s*.24f,1,3,Mint,45,155);Ellipse(vh,icon,s*.24f,1,3,Amber,225,155);
                Ring(vh,icon,s*.31f,1,A(Ice,.36f));
                Line(vh,icon+new Vector2(-10,-5),icon+new Vector2(10,-5),2,Amber);Line(vh,icon+new Vector2(-10,5),icon+new Vector2(10,5),2,Mint);
                Line(vh,icon+new Vector2(-10,-5),icon+new Vector2(-4,-11),2,Amber);Line(vh,icon+new Vector2(10,5),icon+new Vector2(4,11),2,Mint);
                Ellipse(vh,icon,s*.34f,1,1,A(Ice,.55f),-Clock*360,55);
            }
            else if(NodeKind==10 || NodeKind==11)
            {
                float light=Powered?1:.22f;Glow(vh,icon,s*.35f,A(accent,Powered?.23f:.025f));
                for(int side=-1;side<=1;side+=2)
                {
                    var baseAt=icon+new Vector2(side*s*.24f,0);Bevel(vh,new Rect(baseAt.x-5,baseAt.y-s*.24f,10,s*.48f),3,new Color(.04f,.07f,.09f),new Color(.19f,.24f,.29f));
                    Line(vh,baseAt+Vector2.down*s*.22f,baseAt+Vector2.up*s*.22f,3,A(accent,light));
                }
                for(int i=-1;i<=1;i++)Line(vh,icon+new Vector2(-s*.17f,i*s*.10f),icon+new Vector2(s*.17f,i*s*.10f),Powered?2:1,A(accent,Powered?.8f:.13f));
                if(Powered){float y=Mathf.Lerp(-s*.2f,s*.2f,Clock);Line(vh,icon+new Vector2(-s*.2f,y),icon+new Vector2(s*.2f,y),3,A(Ice,.6f));}
                else{Line(vh,icon+new Vector2(-8,-8),icon+new Vector2(8,8),1,A(accent,.45f));Line(vh,icon+new Vector2(-8,8),icon+new Vector2(8,-8),1,A(accent,.45f));}
            }
            else if(NodeKind==12 || NodeKind==13)
            {
                var keyAt=icon+new Vector2(0,lift);Glow(vh,keyAt,s*.36f,A(accent,.25f));
                Ring(vh,keyAt+new Vector2(-s*.10f,s*.04f),s*.10f,3,accent);Ring(vh,keyAt+new Vector2(-s*.10f,s*.04f),s*.055f,1,A(Ice,.7f));
                Line(vh,keyAt+new Vector2(0,s*.04f),keyAt+new Vector2(s*.24f,s*.04f),4,accent);
                for(int i=0;i<2;i++)Line(vh,keyAt+new Vector2(s*(.12f+i*.09f),s*.04f),keyAt+new Vector2(s*(.12f+i*.09f),-s*.06f),3,accent);
                Ellipse(vh,keyAt,s*.31f,.77f,1,A(accent,.65f),Clock*360,160);
            }
            else if(NodeKind==14 || NodeKind==15)
            {
                Glow(vh,icon,s*.31f,A(accent,Powered?.20f:.055f));
                float split=Powered?s*.14f:0;
                for(int side=-1;side<=1;side+=2){var door=new Rect(icon.x+(side<0?-s*.19f-split:split),icon.y-s*.13f,s*.19f,s*.30f);Bevel(vh,door,4,new Color(.055f,.075f,.10f),new Color(.18f,.22f,.28f));Border(vh,door,4,1,A(accent,.72f));}
                Ellipse(vh,icon+new Vector2(0,s*.14f),s*.115f,1,2,A(accent,.9f),0,180);
                if(!Powered){Ring(vh,icon,s*.032f,2,accent);Line(vh,icon,icon+Vector2.down*s*.07f,2,accent);}else Diamond(vh,icon,4,accent);
            }
            else{Ring(vh,icon,s*.12f,1,A(Mint,NodeKind==7?.55f:.25f));Quad(vh,new Rect(icon.x-2,icon.y-2,4,4),A(Ice,.7f));}
            if(Selected)for(int i=0;i<4;i++){float t=i*Mathf.PI*.5f;var p=c+new Vector2(Mathf.Cos(t),Mathf.Sin(t))*s*.48f;Diamond(vh,p,3,Amber);}
        }
        void Network(VertexHelper vh)
        {
            Color signal=HasPhaseRouting && CircuitPhase==1?Amber:Mint;
            for(int i=0;i<EdgeFrom.Length;i++)
            {
                bool live=i>=EdgePowered.Length || EdgePowered[i];var a=EdgeFrom[i];var b=EdgeTo[i];
                Line(vh,a+Vector2.down*4,b+Vector2.down*4,4,new Color(.002f,.009f,.014f));
                Line(vh,a,b,7,A(signal,live?.075f:.01f));Line(vh,a,b,1.6f,A(signal,live?.42f:.065f));
                if(!live)continue;
                var p=Vector2.Lerp(a,b,Mathf.Repeat(Clock+i*.097f,1));Glow(vh,p,14,A(signal,.21f));Quad(vh,new Rect(p.x-2,p.y-2,4,4),A(Ice,.95f));
                var tail=Vector2.Lerp(a,b,Mathf.Repeat(Clock+i*.097f-.045f,1));Line(vh,tail,p,1.4f,A(signal,.46f));
            }
            for(int i=1;i<Trail.Length;i++){Line(vh,Trail[i-1],Trail[i],6,A(Ice,.06f));Line(vh,Trail[i-1],Trail[i],2,A(Ice,.45f*(i/(float)Trail.Length)));}
            if(Travel<1)
            {
                var p=Vector2.Lerp(Origin,Target,Travel*Travel*(3-2*Travel));Glow(vh,p,40,A(Ice,.25f));Crystal(vh,p+new Vector2(0,6),17,Ice);
                Line(vh,Origin,p,2,A(Ice,.7f));
            }
            if(Impact>0)
            {
                float age=1-Impact;Color hue=ImpactKind==1?Amber:ImpactKind==2?Red:ImpactKind==3||ImpactKind==5?Ice:ImpactKind==6?Violet:ImpactKind==4?signal:Mint;Ring(vh,Target,20+age*75,2,A(hue,Impact*.65f));Glow(vh,Target,70,A(hue,Impact*.15f));
                for(int i=0;i<12;i++){float t=i*Mathf.PI*2/12;var p=Target+new Vector2(Mathf.Cos(t),Mathf.Sin(t))*(18+age*70);Diamond(vh,p,2+Impact*2,A(hue,Impact));}
                if(ImpactKind==4){Ring(vh,Target,25+age*380,2,A(signal,Impact*.4f));Ring(vh,Target,12+age*240,1,A(Ice,Impact*.26f));}
            }
        }
        void Trace(VertexHelper vh,Rect r)
        {
            Color hue=TraceFraction>.82f?Red:TraceFraction>.65f?Amber:Mint;
            int n=48;float step=r.width/n;
            for(int i=0;i<n;i++)Quad(vh,new Rect(r.xMin+i*step,r.yMin,step-3,r.height),A(hue,(i+.5f)/n<=TraceFraction?.86f:.095f));
            float x=r.xMin+r.width*Mathf.Clamp01(TraceFraction);Glow(vh,new Vector2(x,r.center.y),25,A(hue,.2f));
        }
        void MiniMap(VertexHelper vh,Rect r)
        {
            if(Miniature==null)return;float step=Mathf.Min(r.width/Miniature.Width,r.height/Miniature.Height);
            for(int y=0;y<Miniature.Height;y++)for(int x=0;x<Miniature.Width;x++)
            {
                char tile=Miniature.Tile(y*Miniature.Width+x);if(tile=='#')continue;
                var p=r.center+new Vector2((x-(Miniature.Width-1)*.5f)*step,((Miniature.Height-1)*.5f-y)*step);
                var hue=tile=='P'||tile=='A'?Mint:tile=='F'||tile=='B'?Amber:tile=='T'?Red:tile=='k'||tile=='l'?Violet:tile=='S'||tile=='E'||tile=='K'||tile=='L'?Ice:tile=='C'?Color.Lerp(Mint,Amber,.5f):A(Mint,.25f);
                Quad(vh,new Rect(p.x-step*.34f,p.y-step*.34f,step*.68f,step*.68f),hue);
            }
        }
        static void Crystal(VertexHelper vh,Vector2 c,float s,Color tint)
        {
            Polygon(vh,new[]{c+new Vector2(0,s),c+new Vector2(-s,s*.35f),c+new Vector2(0,-s*.16f),c+new Vector2(s,s*.35f)},Color.Lerp(tint,Color.white,.32f));
            Polygon(vh,new[]{c+new Vector2(-s,s*.35f),c+new Vector2(-s,-s*.6f),c+new Vector2(0,-s),c+new Vector2(0,-s*.16f)},tint);
            Polygon(vh,new[]{c+new Vector2(0,-s*.16f),c+new Vector2(0,-s),c+new Vector2(s,-s*.6f),c+new Vector2(s,s*.35f)},Color.Lerp(tint,Color.black,.46f));
            Line(vh,c+new Vector2(-s,s*.35f),c+new Vector2(0,s),1.5f,A(Color.white,.64f));
        }
        static void Vert(VertexHelper vh,Vector2 p,Color c){var v=UIVertex.simpleVert;v.position=p;v.color=c;vh.AddVert(v);}
        static void Polygon(VertexHelper vh,Vector2[] p,Color c){int n=vh.currentVertCount;foreach(var v in p)Vert(vh,v,c);for(int i=1;i<p.Length-1;i++)vh.AddTriangle(n,n+i,n+i+1);}
        static Vector2[] Corners(Rect r,float cut)=>new[]{new Vector2(r.xMin,r.yMin+cut),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax-cut,r.yMax),new Vector2(r.xMax,r.yMax-cut),new Vector2(r.xMax,r.yMin),new Vector2(r.xMin+cut,r.yMin)};
        static void Border(VertexHelper vh,Rect r,float cut,float width,Color c){var p=Corners(r,cut);for(int i=0;i<p.Length;i++)Line(vh,p[i],p[(i+1)%p.Length],width,c);}
        static void Bevel(VertexHelper vh,Rect r,float cut,Color lower,Color upper){int n=vh.currentVertCount;Vert(vh,r.center,Color.Lerp(lower,upper,.5f));var p=Corners(r,cut);foreach(var v in p)Vert(vh,v,Color.Lerp(lower,upper,Mathf.InverseLerp(r.yMin,r.yMax,v.y)));for(int i=0;i<p.Length;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%p.Length);}
        static void Quad(VertexHelper vh,Rect r,Color c)=>Gradient(vh,r,c,c);
        static void Gradient(VertexHelper vh,Rect r,Color lower,Color upper){int n=vh.currentVertCount;Vert(vh,new Vector2(r.xMin,r.yMin),lower);Vert(vh,new Vector2(r.xMin,r.yMax),upper);Vert(vh,new Vector2(r.xMax,r.yMax),upper);Vert(vh,new Vector2(r.xMax,r.yMin),lower);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float w,Color c){var n=new Vector2(-(b-a).y,(b-a).x).normalized*w*.5f;Polygon(vh,new[]{a-n,a+n,b+n,b-n},c);}
        static void Diamond(VertexHelper vh,Vector2 c,float s,Color tint)=>Polygon(vh,new[]{c+Vector2.left*s,c+Vector2.up*s,c+Vector2.right*s,c+Vector2.down*s},tint);
        static void Ring(VertexHelper vh,Vector2 c,float radius,float width,Color tint)=>Ellipse(vh,c,radius,1,width,tint,0,360);
        static void Ellipse(VertexHelper vh,Vector2 c,float radius,float sy,float width,Color tint,float angle,float span)
        {
            int steps=Mathf.Max(3,Mathf.CeilToInt(span/7));Vector2 old=c+new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad)*sy)*radius;
            for(int i=1;i<=steps;i++){float t=(angle+span*i/steps)*Mathf.Deg2Rad;var next=c+new Vector2(Mathf.Cos(t),Mathf.Sin(t)*sy)*radius;Line(vh,old,next,width,tint);old=next;}
        }
        static void Glow(VertexHelper vh,Vector2 c,float radius,Color tint){int n=vh.currentVertCount;Vert(vh,c,tint);for(int i=0;i<=24;i++){float t=i*Mathf.PI*2/24;Vert(vh,c+new Vector2(Mathf.Cos(t),Mathf.Sin(t))*radius,A(tint,0));}for(int i=0;i<24;i++)vh.AddTriangle(n,n+i+1,n+i+2);}
    }
}
