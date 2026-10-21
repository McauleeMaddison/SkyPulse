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
        static readonly int ClockId = Shader.PropertyToID("_Clock"), PressId = Shader.PropertyToID("_Press"), RevealId = Shader.PropertyToID("_Reveal");
        sealed class Sculpture
        {
            public Transform Group, Crystal, Inner, Orbit, SecondOrbit;
            public Vector3 Point;
            public float Size, Phase;
            public bool IsCore;
        }
        readonly List<Sculpture> sculptures = new List<Sculpture>(10);
        readonly List<Mesh> sculptureMeshes = new List<Mesh>(4);
        static readonly HashSet<int> occupiedStages = new HashSet<int>();
        int stageSlot;
        Material crystalMaterial, projectorMaterial, coreHaloMaterial;
        public int CoreRank = 1;
        Mesh crystalMesh, orbitMesh, frameMesh;
        ScrollRect scroll;
        Vector2 focusFrom, pressPoint;
        float focusTime, pressAge = 10, revealTime;
        bool focusing;
        public bool IsFocusing => focusing;
        public int SculptureCount => sculptures.Count;
        Transform stage;
        Camera treeCamera;
        RenderTexture texture;
        Material material, fieldMaterial;
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
            this.content = content; this.viewport = viewport; scroll = viewport.GetComponent<ScrollRect>();
            BranchCount = branches.Count; SocketCount = roots.Count;
            Branches = branches.ToArray(); Roots = roots.ToArray();
            stage = new GameObject("Continuous holographic upgrade tree").transform;
            while (!occupiedStages.Add(stageSlot)) stageSlot++;
            stage.position = new Vector3(5000 + stageSlot * 64, 5000, 0);
            CoreRank = Mathf.Clamp(CoreRank, 1, 5);
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
            fieldMaterial = new Material(Resources.Load<Shader>("SkyPulse/Shaders/TechTreeField")) { name = "Tree viewport feathering" };
            view.material = fieldMaterial;
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
                // A substantial shaded glass conductor carries a narrow light
                // vein on its front face. Shared rings keep every curve seamless.
                Tube(from, a, b, to, branch.Trunk ? .105f : .072f, branch.Colour,
                    branch.Powered ? .94f : .58f, vertices, normals, colours, uv, triangles, 2);
                var front = new Vector3(0, 0, -.10f);
                Tube(from+front, a+front, b+front, to+front, branch.Trunk ? .024f : .016f, branch.Colour,
                    branch.Powered ? .92f : .25f, vertices, normals, colours, uv, triangles);
                Tube(from, a, b, to, branch.Trunk ? .18f : .13f, branch.Colour,
                    branch.Powered ? .055f : .025f, vertices, normals, colours, uv, triangles, 4);
            }
            foreach (var root in roots)
            {
                if (root.Size >= 30f) continue; // Larger roots use real animated optical sculptures.
                var centre = new Vector3(root.At.x * .01f, root.At.y * .01f, -.16f);
                float radius = root.Size * .0047f;
                // Floating double-pyramid lenses replace the circular node cages.
                // Bevel faces catch different spectral light rather than solid fill.
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4, b = (i + 1) * Mathf.PI / 4;
                    var p = centre + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0);
                    var q = centre + new Vector3(Mathf.Cos(b) * radius, Mathf.Sin(b) * radius, 0);
                    Face(centre + Vector3.back * radius * 1.5f, q, p, root.Colour,
                        root.Powered ? .72f : .44f, vertices, normals, colours, uv, triangles);
                    Face(centre + Vector3.forward * radius, p, q, root.Colour,
                        .14f, vertices, normals, colours, uv, triangles);
                }
            }
            mesh = new Mesh { name = "Unbroken spectral tree", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colours);
            mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); mesh.UploadMeshData(true);
            var surface = SkyPulseMeshBuilder.Surface(stage, "Unbroken holographic branches", mesh, material, 0);
            surface.gameObject.layer = 31;
            BuildSculptures(roots);
            Tick(0);
        }

        static Vector3 Curve(Vector3 p, Vector3 a, Vector3 b, Vector3 q, float t)
        { float u = 1 - t; return u*u*u*p + 3*u*u*t*a + 3*u*t*t*b + t*t*t*q; }
        static Vector3 Tangent(Vector3 p, Vector3 a, Vector3 b, Vector3 q, float t)
        { float u = 1 - t; return (3*u*u*(a-p) + 6*u*t*(b-a) + 3*t*t*(q-b)).normalized; }

        static void Tube(Vector3 from, Vector3 a, Vector3 b, Vector3 to, float radius, Color colour, float alpha,
            List<Vector3> v, List<Vector3> n, List<Color> c, List<Vector2> uv, List<int> triangles, float layer = 0)
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
                    uv.Add(new Vector2(t, layer + i / (float)TubeSides));
                    if (step == 0 || i == 0) continue;
                    int next = start + step * (TubeSides + 1) + i, previous = next - TubeSides - 1;
                    triangles.Add(previous - 1); triangles.Add(previous); triangles.Add(next);
                    triangles.Add(previous - 1); triangles.Add(next); triangles.Add(next - 1);
                }
            }
        }
        static void Face(Vector3 a, Vector3 b, Vector3 d, Color colour, float alpha,
            List<Vector3> v, List<Vector3> n, List<Color> c, List<Vector2> uv, List<int> triangles)
        {
            int start = v.Count; var normal = Vector3.Cross(b-a, d-a).normalized; colour.a = alpha;
            v.Add(a); v.Add(b); v.Add(d);
            for (int i = 0; i < 3; i++) { n.Add(normal); c.Add(colour); uv.Add(new Vector2(i*.15f, 0)); triangles.Add(start+i); }
        }
        void BuildSculptures(List<HolographicRoot> roots)
        {
            crystalMaterial = new Material(Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics")) { name = "Tree optical crystal", renderQueue = 3001 };
            crystalMaterial.SetFloat("_Glass", 1); crystalMaterial.SetFloat("_Roughness", .14f); crystalMaterial.SetFloat("_ZWrite", 0);
            projectorMaterial = new Material(Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics")) { name = "Tree projector platinum" };
            projectorMaterial.SetColor("_BaseColor", new Color(.09f,.16f,.21f)); projectorMaterial.SetFloat("_Roughness", .21f);
            coreHaloMaterial = new Material(material) {name="Core level halo"};
            coreHaloMaterial.SetColor("_Tint",SkyPulseBirdProgression.CoreColours[CoreRank-1]);
            var builder = new SkyPulseMeshBuilder();
            for (int i = 0; i < 8; i++)
            {
                float a = i*Mathf.PI/4, b = (i+1)*Mathf.PI/4;
                var p = new Vector3(Mathf.Cos(a)*.36f,.22f,Mathf.Sin(a)*.36f);
                var q = new Vector3(Mathf.Cos(b)*.36f,.22f,Mathf.Sin(b)*.36f);
                var l = new Vector3(p.x*.72f,-.25f,p.z*.72f); var r = new Vector3(q.x*.72f,-.25f,q.z*.72f);
                builder.Face(Vector3.up*.9f,q,p,new Color(.8f,.96f,1,.2f));
                builder.Quad(p,q,r,l,new Color(.45f,.78f,.94f,.4f));
                builder.Face(l,r,Vector3.down*.72f,new Color(.25f,.58f,.85f,.6f));
            }
            crystalMesh = builder.Finish("Bevelled eight-sided upgrade prism"); sculptureMeshes.Add(crystalMesh);
            builder = new SkyPulseMeshBuilder(); builder.Torus(1.04f,.022f,96,new Color(.8f,.95f,1,.7f));
            orbitMesh = builder.Finish("Optical gyroscope light guide"); sculptureMeshes.Add(orbitMesh);
            builder = new SkyPulseMeshBuilder();
            // Machined open projector crescent; no enclosing node/card cage.
            for (int i = 0; i < 64; i++)
            {
                if (i%16 >= 12) continue;
                float a=i*Mathf.PI/32, b=(i+1)*Mathf.PI/32;
                for (int j=0;j<4;j++)
                {
                    float u=j*Mathf.PI/2, v=(j+1)*Mathf.PI/2;
                    builder.Quad(ProjectorPoint(a,u),ProjectorPoint(b,u),ProjectorPoint(b,v),ProjectorPoint(a,v),new Color(.8f,.92f,1,.3f));
                }
            }
            frameMesh = builder.Finish("Segmented floating projector cradle"); sculptureMeshes.Add(frameMesh);
            foreach (var root in roots)
            {
                if (root.Size < 30) continue;
                bool isCore = root.Size > 50;
                var group = new GameObject(isCore ? "Living SkyPulse core projector" : "Optical upgrade branch hub").transform;
                group.SetParent(stage,false); group.localPosition = new Vector3(root.At.x*.01f,root.At.y*.01f,-.35f);
                float size = isCore ? 1f : .62f; group.localScale = Vector3.one*size;
                var crystal = SkyPulseMeshBuilder.Surface(group,"Levitating upgrade prism",crystalMesh,crystalMaterial,3);
                var properties = new MaterialPropertyBlock();
                properties.SetColor("_BaseColor",new Color(root.Colour.r*.28f,root.Colour.g*.37f,root.Colour.b*.46f,.64f));
                properties.SetColor("_GlowColor",Color.Lerp(root.Colour,Color.white,.22f));
                properties.SetFloat("_Energy",root.Powered?.29f:.11f); crystal.SetPropertyBlock(properties);
                var inner = SkyPulseMeshBuilder.Surface(group,"Luminous inner lattice",crystalMesh,material,4).transform;
                inner.localScale = new Vector3(.3f,.52f,.3f);
                Transform orbit = null, secondOrbit = null;
                if (isCore)
                {
                    orbit = SkyPulseMeshBuilder.Surface(group,"Core level halo",orbitMesh,coreHaloMaterial,2).transform;
                    if (CoreRank >= 3)
                    {
                        secondOrbit=SkyPulseMeshBuilder.Surface(group,"Secondary core halo",orbitMesh,coreHaloMaterial,2).transform;
                        secondOrbit.localScale=Vector3.one*1.14f;
                    }
                    var frame = SkyPulseMeshBuilder.Surface(group,"Open core projector",frameMesh,projectorMaterial,0).transform;
                    frame.localPosition = new Vector3(0,-.7f,.1f); frame.localRotation = Quaternion.Euler(67,0,0); frame.localScale = new Vector3(.82f,.82f,.82f);
                }
                sculptures.Add(new Sculpture { Group=group, Crystal=crystal.transform, Inner=inner, Orbit=orbit, SecondOrbit=secondOrbit, Point=group.localPosition, Size=size, Phase=roots.IndexOf(root)*.73f, IsCore=isCore });
            }
            foreach (var item in stage.GetComponentsInChildren<Transform>()) item.gameObject.layer=31;
        }
        static Vector3 ProjectorPoint(float angle,float side) => new Vector3(Mathf.Cos(angle)*(1+Mathf.Cos(side)*.055f),Mathf.Sin(angle)*(1+Mathf.Cos(side)*.055f),Mathf.Sin(side)*.05f);

        public void FocusCore()
        {
            if (scroll != null) scroll.StopMovement();
            if (ReducedMotion) { content.anchoredPosition=Vector2.zero; focusing=false; return; }
            focusFrom=content.anchoredPosition; focusTime=0; focusing=true;
        }
        public void CancelFocus() { focusing=false; }
        public void Press(Vector2 point)
        {
            CancelFocus(); pressPoint=point*.01f; pressAge=0;
        }
        public void CancelPress() { pressAge=10; }
        void LateUpdate() => Tick(Time.unscaledDeltaTime);
        public void Tick(float dt)
        {
            if (treeCamera == null) return;
            dt=Mathf.Max(0,dt);
            if (!MotionPaused)
            {
                revealTime+=dt; pressAge+=dt;
                if (!ReducedMotion) clock+=dt;
                if (focusing)
                {
                    focusTime+=dt;
                    float progress=ReducedMotion?1:Mathf.Clamp01(focusTime/.52f);
                    float ease=1-Mathf.Pow(1-progress,3);
                    content.anchoredPosition=Vector2.LerpUnclamped(focusFrom,Vector2.zero,ease);
                    if (progress>=1) focusing=false;
                }
            }
            treeCamera.orthographicSize = viewport.rect.height*.005f;
            treeCamera.aspect = viewport.rect.width/Mathf.Max(1,viewport.rect.height);
            treeCamera.transform.localPosition = new Vector3(-content.anchoredPosition.x*.01f,-(viewport.rect.height*.5f+content.anchoredPosition.y)*.01f,-20);
            float time=ReducedMotion?0:clock;
            material.SetFloat(ClockId,time);
            material.SetFloat(RevealId,ReducedMotion?1:.65f+.35f*Mathf.SmoothStep(0,1,revealTime/.6f));
            material.SetVector(PressId,new Vector4(pressPoint.x,pressPoint.y,pressAge,ReducedMotion?0:1));
            if(coreHaloMaterial!=null)coreHaloMaterial.SetFloat(ClockId,time*(.7f+CoreRank*.2f));
            for (int i=0;i<sculptures.Count;i++)
            {
                var root=sculptures[i]; float t=time+root.Phase;
                float response=ReducedMotion?0:Mathf.Max(0,1-pressAge*2)*Mathf.Max(0,1-Vector2.Distance((Vector2)root.Point,pressPoint)/1.2f);
                root.Group.localScale=Vector3.one*(root.Size*(1+response*.045f));
                root.Crystal.localRotation=Quaternion.Euler(8+Mathf.Sin(t*.32f)*1.4f,35+(root.IsCore?t*6:Mathf.Sin(t*.27f)*7),Mathf.Sin(t*.36f)*1.2f);
                root.Crystal.localPosition=new Vector3(0,Mathf.Sin(t*.7f)*.018f,0);
                root.Inner.localRotation=Quaternion.Euler(-12,-t*10,9);
                if (root.Orbit!=null)
                {
                    float precession=CoreRank>=2?Mathf.Sin(t*.45f)*9:0;
                    root.Orbit.localRotation=Quaternion.Euler(64+precession,t*(3+CoreRank*2),16);
                    root.Orbit.localScale=Vector3.one*(1+(CoreRank>=4?Mathf.Sin(t*.8f)*.025f:0));
                }
                if(root.SecondOrbit!=null)root.SecondOrbit.localRotation=Quaternion.Euler(38,-t*(CoreRank>=5?18:9),-22+(CoreRank>=5?Mathf.Sin(t*.5f)*8:0));
            }
        }
        public void Render() { Tick(0); if (treeCamera != null && isActiveAndEnabled) treeCamera.Render(); }
        void OnEnable() { if (stage != null) stage.gameObject.SetActive(true); }
        void OnDisable() { if (stage != null) stage.gameObject.SetActive(false); }
        void OnDestroy()
        {
            if (stage != null) Destroy(stage.gameObject);
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (mesh != null) Destroy(mesh); if (material != null) Destroy(material);
            if (fieldMaterial != null) Destroy(fieldMaterial);
            if(coreHaloMaterial!=null)Destroy(coreHaloMaterial);
            occupiedStages.Remove(stageSlot);
            foreach (var owned in sculptureMeshes) Destroy(owned);
            if (crystalMaterial != null) Destroy(crystalMaterial); if (projectorMaterial != null) Destroy(projectorMaterial);
        }
    }
}
