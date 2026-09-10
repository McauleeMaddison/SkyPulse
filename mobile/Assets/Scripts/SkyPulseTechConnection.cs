using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    /// <summary>One canvas mesh for a softly lit, curved connection in the upgrade tree.</summary>
    public sealed class SkyPulseTechConnection : MaskableGraphic
    {
        public Vector2 StartPoint;
        public Vector2 EndPoint;
        public bool Powered;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var middleY = (StartPoint.y + EndPoint.y) * .5f;
            var controlA = new Vector2(StartPoint.x, middleY);
            var controlB = new Vector2(EndPoint.x, middleY);
            // Layered geometry supplies mobile-friendly glow without a bloom pass.
            DrawCurve(mesh, controlA, controlB, 22f, Powered ? .075f : .025f);
            DrawCurve(mesh, controlA, controlB, 10f, Powered ? .24f : .065f);
            DrawCurve(mesh, controlA, controlB, 3.5f, Powered ? .92f : .30f);
            DrawCurve(mesh, controlA, controlB, 1.2f, Powered ? 1f : .34f, true);
        }

        private void DrawCurve(VertexHelper mesh, Vector2 controlA, Vector2 controlB, float width, float alpha, bool whiteCore = false)
        {
            const int steps = 28;
            var previous = StartPoint;
            var tint = whiteCore ? Color.Lerp(color, Color.white, .66f) : color;
            tint.a = color.a * alpha;
            for (var index = 1; index <= steps; index++)
            {
                var t = index / (float)steps;
                var u = 1f - t;
                var next = u * u * u * StartPoint + 3f * u * u * t * controlA + 3f * u * t * t * controlB + t * t * t * EndPoint;
                var tangent = next - previous;
                if (tangent.sqrMagnitude > .0001f)
                {
                    var normal = new Vector2(-tangent.y, tangent.x).normalized * width * .5f;
                    var first = mesh.currentVertCount;
                    mesh.AddVert(previous - normal, tint, Vector2.zero);
                    mesh.AddVert(previous + normal, tint, Vector2.zero);
                    mesh.AddVert(next + normal, tint, Vector2.zero);
                    mesh.AddVert(next - normal, tint, Vector2.zero);
                    mesh.AddTriangle(first, first + 1, first + 2);
                    mesh.AddTriangle(first, first + 2, first + 3);
                }
                previous = next;
            }
        }
    }
}
