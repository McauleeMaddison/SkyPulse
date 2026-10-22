using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    // Static, bevelled glass UI geometry. Animation never rebuilds this mesh.
    public sealed class SkyPulseProgressionSurface : MaskableGraphic
    {
        public enum Finish { Sheet, Inset, Action, Quiet, Overlay }
        public Finish Style;
        public Color Accent = new Color(.4f,.9f,1);
        public bool Capsule;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;float cut=Style==Finish.Sheet?28:16;
            if(Capsule){RoundedGlass(mesh,r);return;}
            if(Style==Finish.Overlay)
            {
                var line=Accent;line.a=.3f;Quad(mesh,new Vector2(r.xMin+cut,r.yMax-1),new Vector2(r.xMax-cut,r.yMax),line,line);return;
            }
            bool action=Style==Finish.Action,quiet=Style==Finish.Quiet;
            Color top=action?Color.Lerp(Accent,Color.white,.23f):new Color(.075f,.13f,.18f,quiet?.2f:.98f);
            Color bottom=action?Color.Lerp(Accent,new Color(.08f,.25f,.32f),.22f):new Color(.018f,.037f,.064f,quiet?.2f:.98f);
            // The lower machined edge is visible beneath a chamfered glass face.
            Face(mesh,new Rect(r.xMin,r.yMin-5,r.width,r.height),cut,new Color(.005f,.012f,.024f,quiet?0:.95f),new Color(.005f,.012f,.024f,quiet?0:.95f));
            Face(mesh,r,cut,top,bottom);
            float lineAlpha=action?.55f:quiet?.13f:.24f;
            var rim=Color.Lerp(Accent,Color.white,.45f);rim.a=lineAlpha;
            Quad(mesh,new Vector2(r.xMin+cut,r.yMax-1.5f),new Vector2(r.xMax-cut,r.yMax),rim,rim);
            if(Style==Finish.Sheet)
            {
                rim=Accent;rim.a=.8f;
                Quad(mesh,new Vector2(r.xMin+52,r.yMax-2),new Vector2(r.xMin+160,r.yMax),rim,rim);
                rim.a=.12f;Quad(mesh,new Vector2(r.xMin,r.yMin+cut),new Vector2(r.xMin+1,r.yMax-cut),rim,rim);
            }
        }
        void RoundedGlass(VertexHelper mesh,Rect r)
        {
            bool action=Style==Finish.Action;float radius=Mathf.Min(r.height*.5f,r.width*.5f);
            var top=action?Color.Lerp(Accent,Color.white,.25f):new Color(.15f,.28f,.35f,Style==Finish.Quiet?.24f:.7f);
            var bottom=action?Color.Lerp(Accent,new Color(.1f,.3f,.36f),.18f):new Color(.035f,.08f,.13f,Style==Finish.Quiet?.16f:.7f);
            int centre=mesh.currentVertCount;mesh.AddVert(r.center,Tint(Color.Lerp(top,bottom,.5f)),Vector2.zero);
            for(int corner=0;corner<4;corner++)for(int step=0;step<=8;step++)
            {
                float angle=(corner*90+step*90f/8)*Mathf.Deg2Rad;
                var c=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                var p=c+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                mesh.AddVert(p,Tint(Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,p.y))),Vector2.zero);
            }
            for(int i=0;i<36;i++)mesh.AddTriangle(centre,centre+1+i,centre+1+(i+1)%36);
            var rim=Color.Lerp(Accent,Color.white,.6f);rim.a=action?.35f:.22f;
            Quad(mesh,new Vector2(r.xMin+radius,r.yMax-1),new Vector2(r.xMax-radius,r.yMax),rim,rim);
        }
        void Face(VertexHelper m,Rect r,float cut,Color top,Color bottom)
        {
            int first=m.currentVertCount;
            m.AddVert(r.center,Tint(Color.Lerp(bottom,top,.5f)),Vector2.zero);
            for(int i=0;i<8;i++)
            {
                Vector2 p;
                switch(i)
                {
                    case 0:p=new Vector2(r.xMin+cut,r.yMin);break;
                    case 1:p=new Vector2(r.xMax-cut,r.yMin);break;
                    case 2:p=new Vector2(r.xMax,r.yMin+cut);break;
                    case 3:p=new Vector2(r.xMax,r.yMax-cut);break;
                    case 4:p=new Vector2(r.xMax-cut,r.yMax);break;
                    case 5:p=new Vector2(r.xMin+cut,r.yMax);break;
                    case 6:p=new Vector2(r.xMin,r.yMax-cut);break;
                    default:p=new Vector2(r.xMin,r.yMin+cut);break;
                }
                m.AddVert(p,Tint(Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,p.y))),Vector2.zero);
                m.AddTriangle(first,first+1+i,first+1+(i+1)%8);
            }
        }
        Color Tint(Color c)=>c*color;
        void Quad(VertexHelper m,Vector2 lo,Vector2 hi,Color bottom,Color top)
        {
            int i=m.currentVertCount;m.AddVert(lo,Tint(bottom),Vector2.zero);m.AddVert(new Vector2(lo.x,hi.y),Tint(top),Vector2.zero);
            m.AddVert(hi,Tint(top),Vector2.zero);m.AddVert(new Vector2(hi.x,lo.y),Tint(bottom),Vector2.zero);
            m.AddTriangle(i,i+1,i+2);m.AddTriangle(i,i+2,i+3);
        }
    }
}
