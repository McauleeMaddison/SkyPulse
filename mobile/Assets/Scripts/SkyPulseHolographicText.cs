using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    // Static glyph coordinates let one shared material light the lettering.
    // No duplicated text layers, moving hit areas or per-frame mesh rebuilds.
    public sealed class SkyPulseHolographicText : BaseMeshEffect
    {
        [Range(0,1)] public float Emphasis = .5f;
        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || mesh.currentVertCount == 0) return;
            var vertex = UIVertex.simpleVert;
            var low = new Vector2(float.MaxValue,float.MaxValue);
            var high = new Vector2(float.MinValue,float.MinValue);
            for (int i=0;i<mesh.currentVertCount;i++)
            {
                mesh.PopulateUIVertex(ref vertex,i);
                low=Vector2.Min(low,vertex.position);high=Vector2.Max(high,vertex.position);
            }
            var span=Vector2.Max(high-low,Vector2.one);
            for (int i=0;i<mesh.currentVertCount;i++)
            {
                mesh.PopulateUIVertex(ref vertex,i);
                vertex.uv1=new Vector4((vertex.position.x-low.x)/span.x,(vertex.position.y-low.y)/span.y,Emphasis,0);
                mesh.SetUIVertex(vertex,i);
            }
        }
    }
}
