using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using SkyPulse.Mobile;

/// <summary>Authors one editable volumetric blue exhaust on the existing review rig.</summary>
public static class SkyPulseBirdJetWorkshop
{
    public const string Folder = SkyPulseBirdPrototypeWorkshop.Folder + "/Effects";
    public const string ShaderPath = Folder + "/NeonFinchJet.shader";
    public const string MeshPath = Folder + "/Blue jet volume.asset";
    public const string MaterialPath = Folder + "/Blue jet fire.mat";
    public const string ChildName = "09 Volumetric blue jet fire";
    public const int VertexBudget = 1500;
    public static readonly Vector3 NozzlePosition = new Vector3(-.30f, -.315f, 0);
    public static readonly Quaternion NozzleRotation = Quaternion.Euler(0, 0, 8.5f);

    [MenuItem("Tools/SkyPulse/Bird Workshop/Add flowing blue jet to existing Finch")]
    public static void ApplyToExistingRig()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before editing the review jet.");
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null || shader.name != "SkyPulse/Workshop/Neon Finch Blue Jet")
            throw new InvalidOperationException("Import the dedicated Finch jet shader first.");
        var path = SkyPulseBirdPrototypeWorkshop.PrefabPath;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            throw new InvalidOperationException("The existing Finch review prefab is required.");

        var root = PrefabUtility.LoadPrefabContents(path);
        Mesh authored = null;
        Material expected = null;
        try
        {
            var rigs = root.GetComponentsInChildren<SkyPulseNeonFinchPrototype>(true);
            if (rigs.Length != 1 || rigs[0].tail == null || !rigs[0].tail.IsChildOf(root.transform))
                throw new InvalidOperationException("Expected one bound Finch rig and its existing tail joint.");
            var rig = rigs[0];
            Transform child = null;
            foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
                if (candidate.name == ChildName)
                {
                    if (child != null || candidate.parent != rig.tail)
                        throw new InvalidOperationException("Unexpected jet object or duplicate name; owner edits preserved.");
                    child = candidate;
                }
            if (child == null && rig.jetRenderer != null)
                throw new InvalidOperationException("The existing jet binding is owner edited; no prefab changes made.");

            authored = CreateJetMesh();
            expected = CreateJetMaterial(shader);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if ((System.IO.File.Exists(MeshPath) && mesh == null)
                || (System.IO.File.Exists(MaterialPath) && material == null))
                throw new InvalidOperationException("An effects asset has unexpected content; owner files preserved.");
            // Repeating the action is a no-op. Unexpected existing content is
            // an owner edit, never an excuse to overwrite an asset silently.
            if (mesh != null && !SameMesh(mesh, authored))
                throw new InvalidOperationException("Existing jet mesh differs from the authored volume; owner edits preserved.");
            if (material != null && EditorJsonUtility.ToJson(material) != EditorJsonUtility.ToJson(expected))
                throw new InvalidOperationException("Existing jet material differs from the authored finish; owner edits preserved.");
            if (child != null) ValidateExistingChild(child, rig, mesh, material);

            if (mesh == null) { AssetDatabase.CreateAsset(authored, MeshPath); mesh = authored; authored = null; }
            if (material == null) { AssetDatabase.CreateAsset(expected, MaterialPath); material = expected; expected = null; }
            bool created = child == null;
            if (created)
            {
                var effect = new GameObject(ChildName, typeof(MeshFilter), typeof(MeshRenderer));
                child = effect.transform;
                child.SetParent(rig.tail, false);
                child.localPosition = NozzlePosition;
                child.localRotation = NozzleRotation;
                child.localScale = Vector3.one;
                effect.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = effect.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.allowOcclusionWhenDynamic = false;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                rig.jetRenderer = renderer;
                // Bind-pose arrays, all eight joints and existing renderers stay untouched.
                rig.SampleJet(0);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            Debug.Log("SKYPULSE_FINCH_BLUE_JET: " + (created ? "added one closed volumetric jet" : "existing jet unchanged")
                + "; " + mesh.vertexCount + " vertices; existing rig and owner assets preserved.");
        }
        finally
        {
            if (authored != null) UnityEngine.Object.DestroyImmediate(authored);
            if (expected != null) UnityEngine.Object.DestroyImmediate(expected);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void ValidateExistingChild(Transform child, SkyPulseNeonFinchPrototype rig, Mesh mesh, Material material)
    {
        var filter = child.GetComponent<MeshFilter>();
        var renderer = child.GetComponent<MeshRenderer>();
        bool valid = child.childCount == 0 && child.GetComponents<Component>().Length == 3
            && child.localPosition == NozzlePosition && child.localRotation == NozzleRotation
            && child.localScale == Vector3.one && child.gameObject.activeSelf
            && filter != null && renderer != null && mesh != null && material != null
            && filter.sharedMesh == mesh && rig.jetRenderer == renderer && renderer.enabled
            && renderer.sharedMaterials.Length == 1 && renderer.sharedMaterial == material
            && renderer.shadowCastingMode == ShadowCastingMode.Off && !renderer.receiveShadows
            && renderer.lightProbeUsage == LightProbeUsage.Off
            && renderer.reflectionProbeUsage == ReflectionProbeUsage.Off
            && !renderer.allowOcclusionWhenDynamic
            && renderer.motionVectorGenerationMode == MotionVectorGenerationMode.ForceNoMotion;
        if (!valid) throw new InvalidOperationException("Existing jet child or binding is owner edited; prefab preserved.");
    }

    static Material CreateJetMaterial(Shader shader)
    {
        var material = new Material(shader) { name = "Blue jet fire" };
        material.SetColor("_FlameColor", new Color(.015f, .38f, 1.7f, 1));
        material.SetColor("_CoreColor", new Color(3.2f, 3.6f, 4, 1));
        material.SetFloat("_Opacity", .68f);
        material.SetFloat("_JetTime", 0);
        material.SetFloat("_JetStrength", .92f);
        material.SetFloat("_JetMotion", 1);
        return material;
    }

    /// <summary>One mesh containing seven independently deforming closed volumes.</summary>
    public static Mesh CreateJetMesh()
    {
        var vertices = new List<Vector3>(1200);
        var uv = new List<Vector2>(1200);
        var colours = new List<Color>(1200);
        var triangles = new List<int>(6500);
        void Add(Vector3 p, Vector2 tex, Color metadata)
        { vertices.Add(p); uv.Add(tex); colours.Add(metadata); }
        void Tube(float length, float radius, Vector2 drift, int rings, int sides, float heat, float seed, float density, float turbulence)
        {
            int start = vertices.Count;
            var metadata = new Color(heat, seed, density, turbulence);
            for (int ring = 0; ring < rings; ring++)
            {
                float t = ring / (float)rings;
                float r = radius * Mathf.Pow(1 - t, .72f) * (.70f + .50f * Mathf.Sin(t * Mathf.PI));
                // Gentle authored branching gives real parallax before the
                // shader advects ripples and changes each tongue's end shape.
                var center = new Vector3(-length * t, drift.x * t * t, drift.y * t * t);
                for (int side = 0; side < sides; side++)
                {
                    float a = side * Mathf.PI * 2 / sides;
                    Add(center + new Vector3(0, Mathf.Cos(a) * r, Mathf.Sin(a) * r),
                        new Vector2(t, side / (float)sides), metadata);
                }
            }
            int root = vertices.Count;
            Add(Vector3.zero, Vector2.zero, metadata);
            int tip = vertices.Count;
            Add(new Vector3(-length, drift.x, drift.y), Vector2.right, metadata);
            for (int ring = 0; ring < rings - 1; ring++)
                for (int side = 0; side < sides; side++)
                {
                    int a = start + ring * sides + side, b = start + ring * sides + (side + 1) % sides;
                    int c = b + sides, d = a + sides;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(a); triangles.Add(d); triangles.Add(c);
                }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                triangles.Add(root); triangles.Add(start + side); triangles.Add(start + next);
                int last = start + (rings - 1) * sides;
                triangles.Add(tip); triangles.Add(last + next); triangles.Add(last + side);
            }
        }
        Tube(1.32f, .145f, new Vector2(-.025f, .015f), 18, 12, 0, .05f, .62f, 1);
        Tube(1.38f, .063f, new Vector2(.15f, .065f), 18, 10, .04f, .28f, .46f, 1);
        Tube(1.17f, .070f, new Vector2(-.18f, -.055f), 18, 10, .06f, .53f, .48f, 1);
        Tube(1.24f, .060f, new Vector2(.015f, -.16f), 18, 10, .03f, .78f, .43f, 1);
        Tube(.69f, .068f, new Vector2(.008f, 0), 14, 10, 1, .16f, 1, .35f);
        Tube(1.02f, .034f, new Vector2(.04f, .09f), 12, 8, .30f, .39f, .54f, .8f);
        Tube(.94f, .031f, new Vector2(-.075f, -.07f), 12, 8, .24f, .67f, .48f, .8f);
        if (vertices.Count > VertexBudget) throw new InvalidOperationException("Jet vertex budget exceeded.");
        var mesh = new Mesh { name = "Blue jet volume" };
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetColors(colours); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateTangents();
        // Bounds include every deterministic shader displacement and tip pulse.
        mesh.bounds = new Bounds(new Vector3(-.75f, 0, 0), new Vector3(1.85f, .95f, .95f));
        return mesh;
    }

    static bool SameMesh(Mesh a, Mesh b)
    {
        if (a.name != b.name || a.vertexCount != b.vertexCount || a.bounds != b.bounds || a.subMeshCount != b.subMeshCount) return false;
        var av = a.vertices; var bv = b.vertices; var an = a.normals; var bn = b.normals;
        var at = a.tangents; var bt = b.tangents; var au = a.uv; var bu = b.uv;
        var ac = a.colors; var bc = b.colors; var ai = a.triangles; var bi = b.triangles;
        if (an.Length != bn.Length || at.Length != bt.Length || au.Length != bu.Length || ac.Length != bc.Length || ai.Length != bi.Length) return false;
        for (int i = 0; i < av.Length; i++)
            if (!av[i].Equals(bv[i]) || !an[i].Equals(bn[i]) || !at[i].Equals(bt[i]) || !au[i].Equals(bu[i]) || !ac[i].Equals(bc[i])) return false;
        for (int i = 0; i < ai.Length; i++) if (ai[i] != bi[i]) return false;
        return true;
    }
}
