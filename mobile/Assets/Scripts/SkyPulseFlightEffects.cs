using UnityEngine;

namespace SkyPulse.Mobile
{
    // Fixed-capacity, continuous light ribbons. Geometry/UV buffers are reused:
    // flaps and passages create no objects, textures or managed allocations.
    public sealed class SkyPulseFlightEffects : MonoBehaviour
    {
        const float SampleStep = 1f / 60, DriftSpeed = 4.2f;
        struct Spark { public Vector2 Point, Velocity; public float Life, Duration; public Color Colour; }
        struct Ripple { public Vector2 Point; public float Life; }
        readonly Spark[] sparks = new Spark[48];
        readonly Ripple[] ripples = new Ripple[6];
        readonly Vector2[] history = new Vector2[24];
        readonly Vector2[] path = new Vector2[25], normals = new Vector2[25];
        readonly Vector3[] vertices = new Vector3[1600];
        readonly Vector2[] uv = new Vector2[1600];
        readonly Color[] colours = new Color[1600];
        Mesh mesh;
        Material material;
        int sparkAt, rippleAt, count, used, pathCount;
        float boost, sample;
        Vector2 previousNozzle;
        public float Boost => boost;

        public void Build()
        {
            mesh = new Mesh { name = "Pooled feathered engine ribbon and passage ripples" };
            mesh.MarkDynamic();
            mesh.vertices = vertices; mesh.colors = colours; mesh.uv = uv;
            var triangles = new int[vertices.Length / 4 * 6];
            for (int i = 0; i < vertices.Length / 4; i++)
            {
                int t = i * 6, v = i * 4;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(50, 50, 4));
            material = new Material(Resources.Load<Shader>("SkyPulse/Shaders/FlightLight")) { name = "SkyPulse feathered spectral exhaust" };
            material.SetFloat("_Soft", 1);
            SkyPulseMeshBuilder.Surface(transform, "Engine ribbon and pooled confirmation", mesh, material, 13);
        }

        public void ResetPresentation()
        {
            count = used = pathCount = sparkAt = rippleAt = 0; boost = sample = 0;
            for (int i = 0; i < sparks.Length; i++) sparks[i].Life = 0;
            for (int i = 0; i < ripples.Length; i++) ripples[i].Life = 0;
            System.Array.Clear(colours, 0, colours.Length);
            if (mesh != null) mesh.colors = colours;
        }

        // The caller supplies the banked exhaust socket, shared by plume/history.
        public void Flap(Vector2 nozzle, Color colour, bool reduced)
        {
            boost = 1;
            if (reduced) return;
            for (int i = 0; i < 3; i++)
                Emit(nozzle, new Vector2(-1.6f - i * .3f, (i - 1) * .18f), .20f, colour);
        }
        void Emit(Vector2 at, Vector2 velocity, float duration, Color colour)
        {
            sparks[sparkAt] = new Spark { Point = at, Velocity = velocity, Life = duration, Duration = duration, Colour = colour };
            sparkAt = (sparkAt + 1) % sparks.Length;
        }
        public void Passage(Vector2 at, Color colour, bool reduced)
        {
            if (reduced) return;
            ripples[rippleAt] = new Ripple { Point = at, Life = .55f }; rippleAt = (rippleAt + 1) % ripples.Length;
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6;
                Emit(at, new Vector2(Mathf.Cos(a) * 1.7f - 1.1f, Mathf.Sin(a) * 1.7f), .45f, colour);
            }
        }

        public void Tick(float dt, Vector2 nozzle, Vector2 forward, Color colour, bool reduced)
        {
            dt = Mathf.Max(0, dt);
            boost = Mathf.Max(0, boost - dt * 4.8f);
            used = 0; System.Array.Clear(colours, 0, colours.Length);
            if (reduced)
            {
                count = pathCount = 0; sample = 0;
                for (int i = 0; i < sparks.Length; i++) sparks[i].Life = 0;
                for (int i = 0; i < ripples.Length; i++) ripples[i].Life = 0;
            }
            else
            {
                SampleHistory(dt, nozzle);
                Ribbon(colour, .115f + boost * .035f, .20f);
                Ribbon(Color.Lerp(colour, Color.white, .18f), .038f + boost * .018f, .68f);
                for (int i = 0; i < sparks.Length; i++)
                {
                    if (sparks[i].Life <= 0) continue;
                    sparks[i].Life = Mathf.Max(0, sparks[i].Life - dt); sparks[i].Point += sparks[i].Velocity * dt;
                    float f = sparks[i].Life / sparks[i].Duration;
                    SoftLine(sparks[i].Point - sparks[i].Velocity * .04f, sparks[i].Point, .025f * f, Alpha(sparks[i].Colour, f * .45f));
                }
                for (int i = 0; i < ripples.Length; i++)
                {
                    if (ripples[i].Life <= 0) continue;
                    ripples[i].Life = Mathf.Max(0, ripples[i].Life - dt);
                    float f = ripples[i].Life / .55f, r = 1.7f + (1 - f) * .75f;
                    ripples[i].Point += Vector2.left * dt * 2;
                    for (int j = 0; j < 32; j++)
                    {
                        float a = j * Mathf.PI / 16, b = (j + 1) * Mathf.PI / 16;
                        SoftLine(ripples[i].Point + new Vector2(Mathf.Cos(a) * r * .45f, Mathf.Sin(a) * r), ripples[i].Point + new Vector2(Mathf.Cos(b) * r * .45f, Mathf.Sin(b) * r), .035f, Alpha(new Color(.45f, 1, .9f), f * .55f));
                    }
                }
            }
            // Rounded, tapered teardrop instead of the old white rectangular bar.
            // Reduced Motion keeps this compact, static shape without pumping.
            float thrust = reduced ? 0 : boost;
            Plume(nozzle, -forward.normalized, .38f + thrust * .25f, .10f + thrust * .025f, Alpha(colour, .48f));
            Plume(nozzle, -forward.normalized, .27f + thrust * .14f, .037f + thrust * .012f, Alpha(Color.Lerp(colour, Color.white, .2f), .72f));
            mesh.vertices = vertices; mesh.colors = colours; mesh.uv = uv;
        }

        void SampleHistory(float dt, Vector2 nozzle)
        {
            if (count == 0) { previousNozzle = nozzle; history[0] = nozzle; count = 1; }
            for (int i = 0; i < count; i++) history[i] += Vector2.left * (DriftSpeed * dt);
            sample += dt;
            while (sample >= SampleStep)
            {
                sample -= SampleStep;
                float f = dt > 0 ? Mathf.Clamp01(1 - sample / dt) : 1;
                for (int i = Mathf.Min(count, history.Length - 1); i > 0; i--) history[i] = history[i - 1];
                // Newly emitted samples also drift for their remainder of this
                // frame. At 30 FPS the two new points cannot stack at one x.
                history[0] = Vector2.Lerp(previousNozzle, nozzle, f) + Vector2.left * (DriftSpeed * sample);
                count = Mathf.Min(history.Length, count + 1);
            }
            previousNozzle = nozzle; pathCount = 1; path[0] = nozzle;
            for (int i = 0; i < count; i++)
                if ((history[i] - path[pathCount - 1]).sqrMagnitude > .000001f) path[pathCount++] = history[i];
            for (int i = 0; i < pathCount; i++)
            {
                var incoming = i > 0 ? (path[i] - path[i - 1]).normalized : (path[1] - path[0]).normalized;
                var outgoing = i + 1 < pathCount ? (path[i + 1] - path[i]).normalized : incoming;
                var a = new Vector2(-incoming.y, incoming.x); var b = new Vector2(-outgoing.y, outgoing.x);
                var miter = (a + b).normalized;
                normals[i] = miter / Mathf.Max(.8f, Vector2.Dot(miter, a));
            }
        }
        void Ribbon(Color colour, float width, float opacity)
        {
            for (int i = 1; i < pathCount; i++)
            {
                float u = (i - 1f) / (pathCount - 1), v = i / (pathCount - 1f);
                float a = Mathf.Pow(1 - u, 1.35f), b = Mathf.Pow(1 - v, 1.35f);
                Quad(path[i - 1], path[i], normals[i - 1] * (width * a), normals[i] * (width * b), Alpha(colour, opacity * a), Alpha(colour, opacity * b), u, v);
            }
        }
        void Plume(Vector2 nozzle, Vector2 direction, float length, float width, Color colour)
        {
            var normal = new Vector2(-direction.y, direction.x);
            const int segments = 10;
            for (int i = 0; i < segments; i++)
            {
                float u = i / (float)segments, v = (i + 1f) / segments;
                float a = Mathf.Sin(Mathf.PI * u) * (1 - u), b = Mathf.Sin(Mathf.PI * v) * (1 - v);
                Quad(nozzle + direction * (length * u), nozzle + direction * (length * v), normal * (width * a), normal * (width * b), colour, colour, u, v);
            }
        }
        static Color Alpha(Color c, float a) { c.a = a; return c; }
        void SoftLine(Vector2 a, Vector2 b, float width, Color colour)
        {
            var n = new Vector2(-(b - a).y, (b - a).x).normalized * width;
            Quad(a, b, n, n, colour, colour, 0, 1);
        }
        void Quad(Vector2 a, Vector2 b, Vector2 na, Vector2 nb, Color ca, Color cb, float u, float v)
        {
            if (used + 4 > vertices.Length) return;
            vertices[used] = a - na; vertices[used + 1] = a + na; vertices[used + 2] = b + nb; vertices[used + 3] = b - nb;
            colours[used] = colours[used + 1] = ca; colours[used + 2] = colours[used + 3] = cb;
            uv[used] = new Vector2(u, 0); uv[used + 1] = new Vector2(u, 1); uv[used + 2] = new Vector2(v, 1); uv[used + 3] = new Vector2(v, 0);
            used += 4;
        }
        void OnDestroy() { if (mesh != null) Destroy(mesh); if (material != null) Destroy(material); }
    }
}
