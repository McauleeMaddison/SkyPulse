using UnityEngine;
using UnityEngine.UI;
namespace SkyPulse.Mobile
{
    // Runtime-authored vector/pixel circuits; no image, shader or external font inputs.
    public sealed class SkyPulseCircuitGraphic : MaskableGraphic
    {
        public int Variant;
        public bool Ambient, ReducedMotion;
        private float phase;
        private bool previousReduced;
        private void Update()
        {
            if(previousReduced!=ReducedMotion){previousReduced=ReducedMotion;SetVerticesDirty();}
            if(!Ambient || ReducedMotion)return;
            phase=Mathf.Repeat(phase+Time.unscaledDeltaTime*.12f,1f);SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            for(int i=0;i<7;i++)
            {
                float y=r.yMin+(i+1)*r.height/8;
                if(Variant==2)
                {
                    for(int x=0;x<5;x++)if((x+i)%3!=0)Quad(vh,new Rect(r.xMin+14+x*32,y,16,16),color);
                }
                else
                {
                    float start=r.xMin+12+(i%3)*15;
                    Quad(vh,new Rect(start,y,r.width-30-(i%3)*20,Variant==0?3:5),color);
                    Quad(vh,new Rect(r.xMax-30-(i%3)*20,y,5,20),color);
                    Quad(vh,new Rect(start-6,y-6,14,14),Color.Lerp(color,Color.white,.4f));
                    if(Ambient)Quad(vh,new Rect(start+(r.width-70)*(ReducedMotion?0:phase),y-3,10,10),color);
                }
            }
        }
        static void Quad(VertexHelper vh,Rect r,Color c)
        {
            int start=vh.currentVertCount;var v=UIVertex.simpleVert;v.color=c;
            v.position=new Vector3(r.xMin,r.yMin);vh.AddVert(v);v.position=new Vector3(r.xMin,r.yMax);vh.AddVert(v);
            v.position=new Vector3(r.xMax,r.yMax);vh.AddVert(v);v.position=new Vector3(r.xMax,r.yMin);vh.AddVert(v);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
