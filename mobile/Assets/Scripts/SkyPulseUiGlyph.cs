using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    /// <summary>SkyPulse's wing, docking and circuit geometry, drawn directly on the UI canvas.</summary>
    public sealed class SkyPulseUiGlyph : MaskableGraphic
    {
        public enum Kind { WingMark, DockRing, CircuitHex, Horizon, DockPlatform, RecoveryCore }
        public Kind Shape;
        public int Variant;
        public bool Animate;
        public Func<bool> MotionReduced;

        private void Update()
        {
            if (!Animate) return;
            // Only the light intensity breathes. The geometry is cached by the canvas,
            // so a dock needs no particles, textures, or mesh rebuilds every frame.
            canvasRenderer.SetAlpha(!(MotionReduced?.Invoke() ?? false)
                ? .88f + .12f * Mathf.Sin(Time.unscaledTime * 1.65f) : 1f);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            switch (Shape)
            {
                case Kind.DockPlatform:
                    DrawDockPlatform(mesh);
                    break;
                case Kind.RecoveryCore:
                    Arc(mesh, .36f, 15f, 150f, 3f, .9f);
                    Arc(mesh, .36f, 195f, 330f, 3f, .9f);
                    Line(mesh, new Vector2(-.312f, .18f), new Vector2(-.32f, .37f), 3f, .9f);
                    Line(mesh, new Vector2(-.312f, .18f), new Vector2(-.12f, .21f), 3f, .9f);
                    Line(mesh, new Vector2(.312f, -.18f), new Vector2(.32f, -.37f), 3f, .9f);
                    Line(mesh, new Vector2(.312f, -.18f), new Vector2(.12f, -.21f), 3f, .9f);
                    Line(mesh, new Vector2(-.14f, 0f), new Vector2(.14f, 0f), 4f, 1f);
                    Line(mesh, new Vector2(0f, -.14f), new Vector2(0f, .14f), 4f, 1f);
                    break;
                case Kind.WingMark:
                    for (var side = -1; side <= 1; side += 2)
                    {
                        Feather(mesh, side, new Vector2(.46f, .38f), new Vector2(.32f, .02f), new Vector2(.07f, -.08f), new Vector2(.17f, .13f));
                        Feather(mesh, side, new Vector2(.38f, .03f), new Vector2(.26f, -.25f), new Vector2(.055f, -.30f), new Vector2(.14f, -.10f));
                        Feather(mesh, side, new Vector2(.28f, -.26f), new Vector2(.18f, -.43f), new Vector2(.015f, -.46f), new Vector2(.075f, -.31f));
                    }
                    break;
                case Kind.DockRing:
                    for (var i = 0; i < 6; i++) Arc(mesh, .455f, i * 60f + 8f, i * 60f + 48f, 1.8f, .8f);
                    Arc(mesh, .375f, 198f, 342f, 1.2f, .35f);
                    Arc(mesh, .375f, 18f, 162f, 1.2f, .35f);
                    for (var i = 0; i < 24; i++)
                    {
                        var a = i * Mathf.PI / 12f;
                        var vector = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        Line(mesh, vector * .478f, vector * (i % 6 == 0 ? .422f : .463f), i % 6 == 0 ? 2.4f : 1f, i % 6 == 0 ? .9f : .3f);
                    }
                    Line(mesh, new Vector2(-.22f, -.23f), new Vector2(0f, -.31f), 1.5f, .5f);
                    Line(mesh, new Vector2(0f, -.31f), new Vector2(.22f, -.23f), 1.5f, .5f);
                    break;
                case Kind.CircuitHex:
                    for (var i = 0; i < 6; i++)
                    {
                        var a = PointOnCircle(i * 60f + 30f, .44f);
                        var b = PointOnCircle((i + 1) * 60f + 30f, .44f);
                        Line(mesh, a, b, 1.2f, .28f);
                        Line(mesh, a, Vector2.Lerp(a, b, .3f), 2.4f, 1f);
                        Line(mesh, Vector2.Lerp(a, b, .74f), b, 2.4f, 1f);
                    }
                    break;
                case Kind.Horizon:
                    Line(mesh, new Vector2(-.5f, .2f), new Vector2(-.12f, .2f), 1.5f, .55f);
                    Line(mesh, new Vector2(-.12f, .2f), new Vector2(0f, -.35f), 2f, .95f);
                    Line(mesh, new Vector2(0f, -.35f), new Vector2(.12f, .2f), 2f, .95f);
                    Line(mesh, new Vector2(.12f, .2f), new Vector2(.5f, .2f), 1.5f, .55f);
                    Line(mesh, new Vector2(-.4f, -.22f), new Vector2(-.19f, -.22f), 1.2f, .3f);
                    Line(mesh, new Vector2(.19f, -.22f), new Vector2(.4f, -.22f), 1.2f, .3f);
                    break;
            }
        }

        private void DrawDockPlatform(VertexHelper mesh)
        {
            // Perspective floor, front fascia and rear gantries create a dimensional
            // bay while keeping the bird a crisp, independently animated 2D sprite.
            Surface(mesh, new Vector2(-.46f, -.27f), new Vector2(-.24f, -.11f),
                new Vector2(.24f, -.11f), new Vector2(.46f, -.27f), .16f, .30f, .95f);
            Surface(mesh, new Vector2(-.46f, -.27f), new Vector2(.46f, -.27f),
                new Vector2(.24f, -.40f), new Vector2(-.24f, -.40f), .26f, .08f, .95f);
            Surface(mesh, new Vector2(-.46f, -.27f), new Vector2(-.24f, -.40f),
                new Vector2(-.24f, -.48f), new Vector2(-.46f, -.35f), .08f, .02f, 1f);
            Surface(mesh, new Vector2(-.24f, -.40f), new Vector2(.24f, -.40f),
                new Vector2(.24f, -.48f), new Vector2(-.24f, -.48f), .19f, .035f, 1f);
            Surface(mesh, new Vector2(.24f, -.40f), new Vector2(.46f, -.27f),
                new Vector2(.46f, -.35f), new Vector2(.24f, -.48f), .24f, .045f, 1f);
            Line(mesh, new Vector2(-.46f, -.27f), new Vector2(-.24f, -.40f), 2.5f, .65f);
            Line(mesh, new Vector2(-.24f, -.40f), new Vector2(.24f, -.40f), 2.5f, .9f);
            Line(mesh, new Vector2(.24f, -.40f), new Vector2(.46f, -.27f), 2.5f, .65f);
            for (var side = -1; side <= 1; side += 2)
            {
                var s = (float)side;
                Line(mesh, new Vector2(s * .11f, -.37f), new Vector2(s * .19f, -.19f), 1.7f, .4f);
                Surface(mesh, new Vector2(s * .20f, -.19f), new Vector2(s * .30f, -.19f),
                    new Vector2(s * .38f, .35f), new Vector2(s * .19f, .35f), .2f, .01f, .14f);
                if (Variant == 2)
                {
                    // Prism pylons: lit facets, rather than the reactor's arch.
                    var a = new Vector2(s * .43f, .40f);
                    var b = new Vector2(s * .36f, .12f);
                    var c = new Vector2(s * .43f, -.16f);
                    var d = new Vector2(s * .50f, .12f);
                    Surface(mesh, a, b, c, d, .55f, .09f, .85f);
                    Line(mesh, a, b, 2f, .9f); Line(mesh, a, d, 2f, .5f);
                    Line(mesh, a, c, 1.5f, .65f);
                }
                else if (Variant == 3)
                {
                    // Solar vanes echo the long, outward-spreading flight feathers.
                    for (var vane = 0; vane < 3; vane++)
                    {
                        var x = .31f + vane * .064f;
                        Surface(mesh, new Vector2(s * x, -.19f), new Vector2(s * (x + .029f), -.14f),
                            new Vector2(s * (.36f + vane * .065f), .39f - vane * .09f),
                            new Vector2(s * (.31f + vane * .055f), .35f - vane * .09f), .30f, .08f, .9f);
                        Line(mesh, new Vector2(s * x, -.19f), new Vector2(s * (.36f + vane * .065f), .39f - vane * .09f), 1.4f, .5f);
                    }
                }
                else
                {
                    var height = Variant == 1 ? .34f : .16f;
                    Surface(mesh, new Vector2(s * .48f, -.22f), new Vector2(s * .44f, -.18f),
                        new Vector2(s * .44f, height), new Vector2(s * .49f, height - .04f), .22f, .065f, 1f);
                    Line(mesh, new Vector2(s * .44f, -.16f), new Vector2(s * .44f, height), 2.2f, .72f);
                    if (Variant == 1)
                    {
                        Surface(mesh, new Vector2(s * .44f, .34f), new Vector2(s * .27f, .46f),
                            new Vector2(s * .20f, .42f), new Vector2(s * .44f, .28f), .25f, .08f, 1f);
                        Line(mesh, new Vector2(s * .44f, .34f), new Vector2(s * .27f, .46f), 2.2f, .7f);
                    }
                    else
                    {
                        Surface(mesh, new Vector2(s * .44f, .16f), new Vector2(s * .27f, .30f),
                            new Vector2(s * .32f, .12f), new Vector2(s * .44f, .04f), .34f, .07f, 1f);
                        Line(mesh, new Vector2(s * .44f, .16f), new Vector2(s * .27f, .30f), 2.2f, .75f);
                    }
                }
            }
            for (var i = -2; i <= 2; i++)
                Line(mesh, new Vector2(i * .055f, -.445f), new Vector2(i * .055f + .022f, -.445f), 2f, .65f);
        }

        private void Surface(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d,
            float lit, float shade, float opacity)
        {
            var darkMetal = new Color(.025f, .044f, .085f, color.a * opacity);
            var top = Color.Lerp(darkMetal, color, lit); top.a = color.a * opacity;
            var bottom = Color.Lerp(darkMetal, color, shade); bottom.a = top.a;
            var first = mesh.currentVertCount;
            mesh.AddVert(Local(a), top, Vector2.zero);
            mesh.AddVert(Local(b), top, Vector2.zero);
            mesh.AddVert(Local(c), bottom, Vector2.zero);
            mesh.AddVert(Local(d), bottom, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        private static Vector2 PointOnCircle(float degrees, float radius)
        {
            var angle = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private void Arc(VertexHelper mesh, float radius, float start, float end, float width, float opacity)
        {
            var steps = Mathf.CeilToInt((end - start) / 5f);
            for (var i = 0; i < steps; i++)
                Line(mesh, PointOnCircle(Mathf.Lerp(start, end, i / (float)steps), radius),
                    PointOnCircle(Mathf.Lerp(start, end, (i + 1f) / steps), radius), width, opacity);
        }

        private Vector2 Local(Vector2 point)
        {
            var rect = rectTransform.rect;
            return rect.center + Vector2.Scale(point, rect.size);
        }

        private void Feather(VertexHelper mesh, int side, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var mirror = new Vector2(side, 1f);
            Quad(mesh, Local(Vector2.Scale(a, mirror)), Local(Vector2.Scale(b, mirror)),
                Local(Vector2.Scale(c, mirror)), Local(Vector2.Scale(d, mirror)), .85f);
        }

        private void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, float opacity)
        {
            a = Local(a);
            b = Local(b);
            var tangent = (b - a).normalized;
            var normal = new Vector2(-tangent.y, tangent.x);
            // Restrained glow behind a crisp conductor, without requiring bloom.
            var glow = normal * (width * .5f + 2f);
            Quad(mesh, a - glow, a + glow, b + glow, b - glow, opacity * .10f);
            var core = normal * width * .5f;
            Quad(mesh, a - core, a + core, b + core, b - core, opacity);
        }

        private void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, float opacity)
        {
            var first = mesh.currentVertCount;
            var tint = color;
            tint.a *= opacity;
            mesh.AddVert(a, tint, Vector2.zero);
            mesh.AddVert(b, tint, Vector2.zero);
            mesh.AddVert(c, tint, Vector2.zero);
            mesh.AddVert(d, tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
