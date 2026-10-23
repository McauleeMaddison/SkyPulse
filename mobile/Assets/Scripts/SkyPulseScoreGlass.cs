using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    // A single static quad. The shared shader supplies the glass and edge light;
    // no animated geometry, duplicate text or extra input surface is needed.
    public sealed class SkyPulseScoreGlass : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;
            var v=UIVertex.simpleVert;v.color=color;v.uv1=new Vector4(r.width,r.height,0,0);
            v.position=new Vector3(r.xMin,r.yMin);v.uv0=new Vector2(0,0);mesh.AddVert(v);
            v.position=new Vector3(r.xMin,r.yMax);v.uv0=new Vector2(0,1);mesh.AddVert(v);
            v.position=new Vector3(r.xMax,r.yMax);v.uv0=new Vector2(1,1);mesh.AddVert(v);
            v.position=new Vector3(r.xMax,r.yMin);v.uv0=new Vector2(1,0);mesh.AddVert(v);
            mesh.AddTriangle(0,1,2);mesh.AddTriangle(0,2,3);
        }
    }
}
