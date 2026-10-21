using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public struct HolographicBranch
    {
        public Vector2 From, To;
        public Color Colour;
        public bool Powered, Trunk;
    }
    public struct HolographicRoot
    {
        public Vector2 At;
        public Color Colour;
        public bool Powered;
        public float Size;
    }

    // One flowing mesh behind fixed UI hit areas. The isolated camera follows
    // the pan after ScrollRect's LateUpdate; no moving target or scene physics.
    [DefaultExecutionOrder(100)]
    public sealed class SkyPulseTechTreeView : MonoBehaviour
    {
        const int CurveSteps = 64, TubeSides = 10;
        static readonly int ClockId = Shader.PropertyToID("_Clock");
        Transform stage;
        Camera treeCamera;
        RenderTexture texture;
        Material material;
        Mesh mesh;
        RectTransform content, viewport;
        float clock;
        public bool ReducedMotion, MotionPaused;
        public int BranchCount { get; private set; }
        public int SocketCount { get; private set; }
        public Camera TreeCamera => treeCamera;
        public float AnimationClock => clock;
        public HolographicBranch[] Branches { get; private set; }
        public HolographicRoot[] Roots { get; private set; }

        public void Build(RawImage view, RectTransform content, RectTransform viewport,
            List<HolographicBranch> branches, List<HolographicRoot> roots)
        {
            this.content = content; this.viewport = viewport;
            BranchCount = branches.Count; SocketCount = roots.Count;
            Branches = branches.ToArray(); Roots = roots.ToArray();
            stage = new GameObject("Continuous holographic upgrade tree").transform;
            stage.position = new Vector3(5000, 5000, 0);
            var cameraObject = new GameObject("Holographic viewport camera", typeof(Camera));
            cameraObject.transform.SetParent(stage, false);
            treeCamera = cameraObject.GetComponent<Camera>();
            treeCamera.orthographic = true; treeCamera.nearClipPlane = .1f; treeCamera.farClipPlane = 40;
            treeCamera.cullingMask = 1 << 31; treeCamera.clearFlags = CameraClearFlags.SolidColor;
            treeCamera.backgroundColor = Color.clear; treeCamera.allowMSAA = true; treeCamera.allowHDR = false;
            int height = Mathf.Clamp(Mathf.RoundToInt(1024 * viewport.rect.height / Mathf.Max(1, viewport.rect.width)), 512, 2048);
            texture = new RenderTexture(1024, height, 24, RenderTextureFormat.ARGB32)
            { name = "Free-pan holographic tree", antiAliasing = 4 };
            texture.Create(); treeCamera.targetTexture = texture; view.texture = texture; view.raycastTarget = false;
            material = new Material(Resources.Load<Shader>("SkyPulse/Shaders/HolographicTree"))
            { name = "Spectral glass and flowing energy filaments" };

            var vertices = new List<Vector3>(); var normals = new List<Vector3>();
            var colours = new List<Color>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            foreach (var branch in branches)
            {
                var from = (Vector3)branch.From * .01f; var to = (Vector3)branch.To * .01f;
                float gap = Mathf.Abs(to.y - from.y), spread = to.x - from.x;
                float bend = branch.Trunk ? Mathf.Min(1.7f, gap * .25f) : Mathf.Sign(spread) * Mathf.Min(1.5f, gap * .35f);
                var a = from + new Vector3(branch.Trunk ? 0 : bend, -gap * .38f, .32f);
                var b = to + new Vector3(branch.Trunk ? 0 : -spread * .16f, gap * .27f, -.22f);
                // Continuous shared rings and derivative normals eliminate the
                // old faceted pipe joints. Outer fibres are restrained additive haze.
                Tube(from, a, b, to, branch.Trunk ? .038f : .025f, branch.Colour,
                    branch.Powered ? .86f : .56f, vertices, normals, colours, uv, triangles);
                Tube(from, a, b, to, branch.Trunk ? .13f : .095f, branch.Colour,
                    branch.Powered ? .12f : .065f, vertices, normals, colours, uv, triangles);
            }
            foreach (var root in roots)
            {
                var centre = new Vector3(root.At.x * .01f, root.At.y * .01f, -.16f);
                float radius = root.Size * .0047f;
                // Floating double-pyramid lenses replace the circular node cages.
                // Bevel faces catch different spectral light rather than solid fill.
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4, b = (i + 1) * Mathf.PI / 4;
                    var p = centre + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0);
                    var q = centre + new Vector3(Mathf.Cos(b) * radius, Mathf.Sin(b) * radius, 0);
                    Face(centre + Vector3.back * radius * 1.5f, p, q, root.Colour,
                        root.Powered ? .72f : .44f, vertices, normals, colours, uv, triangles);
                    Face(centre + Vector3.forward * radius, q, p, root.Colour,
                        .14f, vertices, normals, colours, uv, triangles);
                }
            }
            // A quiet, sparse projected lattice supplies perspective/depth without
            // enclosing the field in an opaque black rectangle.
            for (int y = 0; y < 36; y++)
            {
                float row = -y;
                Line(new Vector3(-12, row, 1.6f), new Vector3(12, row - .7f, 2.3f), .007f,
                    new Color(.22f, .53f, .68f, .022f), vertices, normals, colours, uv, triangles);
            }
            for (int x = -12; x <= 12; x++)
                Line(new Vector3(x, 0, 1.6f), new Vector3(x + 1.8f, -36, 2.3f), .007f,
                    new Color(.22f, .53f, .68f, .022f), vertices, normals, colours, uv, triangles);
            mesh = new Mesh { name = "Unbroken spectral tree", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colours);
            mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); mesh.UploadMeshData(true);
            var surface = SkyPulseMeshBuilder.Surface(stage, "Unbroken holographic branches", mesh, material, 0);
            surface.gameObject.layer = 31; Tick(0);
        }

        static Vector3 Curve(Vector3 p, Vector3 a, Vector3 b, Vector3 q, float t)
        { float u = 1 - t; return u*u*u*p + 3*u*u*t*a + 3*u*t*t*b + t*t*t*q; }
        static Vector3 Tangent(Vector3 p, Vector3 a, Vector3 b, Vector3 q, float t)
        { float u = 1 - t; return (3*u*u*(a-p) + 6*u*t*(b-a) + 3*t*t*(q-b)).normalized; }

        static void Tube(Vector3 from, Vector3 a, Vector3 b, Vector3 to, float radius, Color colour, float alpha,
            List<Vector3> v, List<Vector3> n, List<Color> c, List<Vector2> uv, List<int> triangles)
        {
            int start = v.Count; colour.a = alpha;
            for (int step = 0; step <= CurveSteps; step++)
            {
                float t = step / (float)CurveSteps;
                var point = Curve(from, a, b, to, t); var axis = Tangent(from, a, b, to, t);
                var side = Vector3.Cross(axis, Vector3.forward).normalized;
                if (side.sqrMagnitude < .1f) side = Vector3.right;
                var up = Vector3.Cross(axis, side).normalized;
                for (int i = 0; i <= TubeSides; i++)
                {
                    float angle = i * Mathf.PI * 2 / TubeSides;
                    var normal = Mathf.Cos(angle) * side + Mathf.Sin(angle) * up;
                    v.Add(point + normal * radius); n.Add(normal); c.Add(colour);
                    uv.Add(new Vector2(t, i / (float)TubeSides));
                    if (step == 0 || i == 0) continue;
                    int next = start + step * (TubeSides + 1) + i, previous = next - TubeSides - 1;
                    triangles.Add(previous - 1); triangles.Add(previous); triangles.Add(next);
                    triangles.Add(previous - 1); triangles.Add(next); triangles.Add(next - 1);
                }
            }
        }
        static void Line(Vector3 a, Vector3 b, float width, Color tint,
            List<Vector3> v, List<Vector3> n, List<Color> c, List<Vector2> uv, List<int> triangles)
        {
            var side = Vector3.Cross(b-a, Vector3.forward).normalized * width;
            Face(a-side, a+side, b+side, tint, tint.a, v, n, c, uv, triangles);
            Face(a-side, b+side, b-side, tint, tint.a, v, n, c, uv, triangles);
        }
        static void Face(Vector3 a, Vector3 b, Vector3 d, Color colour, float alpha,
            List<Vector3> v, List<Vector3> n, List<Color> c, List<Vector2> uv, List<int> triangles)
        {
            int start = v.Count; var normal = Vector3.Cross(b-a, d-a).normalized; colour.a = alpha;
            v.Add(a); v.Add(b); v.Add(d);
            for (int i = 0; i < 3; i++) { n.Add(normal); c.Add(colour); uv.Add(new Vector2(i*.15f, 0)); triangles.Add(start+i); }
        }
        void LateUpdate() => Tick(Time.unscaledDeltaTime);
        public void Tick(float dt)
        {
            if (treeCamera == null) return;
            treeCamera.orthographicSize = viewport.rect.height * .005f;
            treeCamera.aspect = viewport.rect.width / Mathf.Max(1, viewport.rect.height);
            treeCamera.transform.localPosition = new Vector3(-content.anchoredPosition.x * .01f,
                -(viewport.rect.height * .5f + content.anchoredPosition.y) * .01f, -20);
            if (!MotionPaused && !ReducedMotion) clock += Mathf.Max(0, dt);
            material.SetFloat(ClockId, ReducedMotion ? 0 : clock);
        }
        public void Render() { Tick(0); if (treeCamera != null && isActiveAndEnabled) treeCamera.Render(); }
        void OnEnable() { if (stage != null) stage.gameObject.SetActive(true); }
        void OnDisable() { if (stage != null) stage.gameObject.SetActive(false); }
        void OnDestroy()
        {
            if (stage != null) Destroy(stage.gameObject);
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (mesh != null) Destroy(mesh); if (material != null) Destroy(material);
        }
    }
}
