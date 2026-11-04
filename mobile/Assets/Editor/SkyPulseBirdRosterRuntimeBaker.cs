using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using SkyPulse.Mobile;

/// <summary>
/// Exports the editable roster to Resources for the live Hangar. Geometry is
/// combined only inside one articulated bone and one material. Authoring meshes,
/// joint bind poses, materials and canonical shaders are never saved or edited.
/// </summary>
public static class SkyPulseBirdRosterRuntimeBaker
{
    public const string OutputFolder = "Assets/Resources/SkyPulse/Birds3D";
    public const string ResourcePrefix = "SkyPulse/Birds3D/";
    public const string ManifestPath = OutputFolder + "/BakeManifest.json";
    public const string ArmourShaderPath = "Assets/Resources/SkyPulse/Shaders/RosterArmour.generated.shader";
    public const string JetShaderPath = "Assets/Resources/SkyPulse/Shaders/RosterJet.generated.shader";
    const string ArmourSource = SkyPulseBirdPrototypeWorkshop.Folder + "/Shaders/FinchArmourFinish.shader";
    const string JetSource = SkyPulseBirdPrototypeWorkshop.Folder + "/Effects/NeonFinchJet.shader";
    public const string ReflectionPath = OutputFolder + "/StudioReflection.asset";
    const string ReflectionSource = SkyPulseBirdPrototypeWorkshop.Folder + "/Lighting/Finch Studio Reflection.asset";
    const string ArmourName = "SkyPulse/Birds3D/Armour Finish";
    const string JetName = "SkyPulse/Birds3D/Blue Jet";
    const string BakerSource = "Assets/Editor/SkyPulseBirdRosterRuntimeBaker.cs";
    const int Format = 1;

    [Serializable] public sealed class Stamp { public string path, sha256; }
    [Serializable] public sealed class BirdRecord
    {
        public string id, name, sourcePrefab, runtimePrefab, container;
        public int sourceRenderers, runtimeRenderers, combinedMeshes, materials, vertices, triangles;
        public bool hasJet, editorDependencies;
    }
    [Serializable] public sealed class Manifest
    {
        public int format;
        public string purpose, inputDigest, studioReflection;
        public Stamp[] inputs, outputs;
        public BirdRecord[] birds;
        public bool editableSourcesPreserved;
        public string[] validation;
    }
    sealed class Source
    {
        public string id, name, prefab;
        public Source(string id, string name, string prefab) { this.id = id; this.name = name; this.prefab = prefab; }
    }
    sealed class Group
    {
        public Transform bone;
        public Material material;
        public MeshRenderer template;
        public readonly List<CombineInstance> instances = new List<CombineInstance>();
        public int vertices;
    }
    public static string PrefabPath(string id) => OutputFolder + "/" + id + ".prefab";

    [MenuItem("Tools/SkyPulse/Bird Workshop/Bake reference roster for live Hangar")]
    public static void BakeAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before baking the live roster.");
        var sources = Sources();
        var inputs = InputStamps(sources);
        string digest = Digest(inputs);
        var before = AuthoringSnapshot();
        if (File.Exists(ManifestPath))
        {
            var prior = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            if (prior == null || prior.format != Format || prior.inputs == null || prior.outputs == null || prior.birds == null || prior.birds.Length != 15)
                throw new InvalidOperationException("The runtime roster manifest is incomplete; owner outputs preserved.");
            if (!Same(prior.outputs, OutputStamps()))
                throw new InvalidOperationException("Generated roster outputs contain owner edits or missing files; nothing was overwritten.");
            if (prior.inputDigest != digest || !Same(prior.inputs, inputs))
                throw new InvalidOperationException("Roster authoring inputs changed since this bake. Existing runtime output is preserved; create an explicit reviewed rebake before replacing it.");
            ValidateRuntime(sources);
            if (!Same(before, AuthoringSnapshot())) throw new InvalidOperationException("Read-only repeat changed an editable source.");
            Debug.Log("SKYPULSE_ROSTER_RUNTIME_BAKE_PASS: existing fifteen-bird bake is byte-identical; no assets saved.");
            return;
        }
        if ((Directory.Exists(OutputFolder) && Directory.GetFiles(OutputFolder, "*", SearchOption.AllDirectories).Length > 0)
            || File.Exists(ArmourShaderPath) || File.Exists(JetShaderPath))
            throw new InvalidOperationException("Untracked runtime roster output already exists; owner files preserved.");

        EnsureFolder(OutputFolder);
        EnsureFolder(Path.GetDirectoryName(ArmourShaderPath).Replace('\\', '/'));
        var armour = ExportShader(ArmourSource, ArmourShaderPath, "SkyPulse/Workshop/Finch Armour Finish", ArmourName);
        var jet = ExportShader(JetSource, JetShaderPath, "SkyPulse/Workshop/Neon Finch Blue Jet", JetName);
        ExportReflection();
        var records = new List<BirdRecord>();
        foreach (var source in sources) records.Add(BakeBird(source, armour, jet));
        ValidateRuntime(sources);
        if (!Same(before, AuthoringSnapshot()))
            throw new InvalidOperationException("Editable source bytes changed while baking; do not promote these outputs.");
        // Import the manifest once so its generated .meta is included without
        // introducing a recursive hash of the manifest JSON itself.
        File.WriteAllText(ManifestPath, "{}\n", new UTF8Encoding(false));
        AssetDatabase.ImportAsset(ManifestPath, ImportAssetOptions.ForceSynchronousImport);
        var report = new Manifest
        {
            format = Format,
            purpose = "Generated live-Hangar roster; editable workshop remains canonical. No gameplay control, save or unlock rules are authored here.",
            inputDigest = digest, studioReflection = ReflectionPath, inputs = inputs, outputs = OutputStamps(), birds = records.ToArray(),
            editableSourcesPreserved = true,
            validation = new[] {
                "Fifteen catalogue IDs map to individually authored source prefabs",
                "Eight joint transforms and existing bind-pose arrays retained",
                "Opaque geometry combined within the nearest bound bone and original material only",
                "Original vertex normals, tangents, UVs and colours retained by mesh combination",
                "Jet mesh and nozzle transform retained separately for vertex deformation",
                "Runtime prefab dependency graphs contain no Editor-folder asset",
                "Every bird uses flapPeriod 1.65, flapDegrees 28, bankDegrees 5, turntableDegreesPerSecond 12; animate false",
                "Repeated bake is a no-op; changed inputs or edited outputs require a reviewed rebake"
            }
        };
        File.WriteAllText(ManifestPath, JsonUtility.ToJson(report, true) + "\n", new UTF8Encoding(false));
        AssetDatabase.ImportAsset(ManifestPath, ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("SKYPULSE_ROSTER_RUNTIME_BAKE_PASS: fifteen independent runtime roster prefabs; "
            + records.Sum(r => r.sourceRenderers) + " source renderers combined to " + records.Sum(r => r.runtimeRenderers)
            + "; source workshop bytes preserved; " + ManifestPath);
    }

    static Source[] Sources()
    {
        var list = new List<Source> { new Source("neon_finch", "Neon Finch", SkyPulseBirdPrototypeWorkshop.PrefabPath) };
        foreach (var design in SkyPulseBirdRosterWorkshop.LoadDesigns())
            list.Add(new Source(design.id, design.name, SkyPulseBirdRosterWorkshop.PrefabPath(design)));
        if (list.Count != 15 || list.Select(s => s.id).Distinct(StringComparer.Ordinal).Count() != 15)
            throw new InvalidOperationException("Expected exactly fifteen unique catalogue birds.");
        foreach (var source in list)
        {
            if (source.id.Any(c => !(char.IsLetterOrDigit(c) || c == '_')) || AssetDatabase.LoadAssetAtPath<GameObject>(source.prefab) == null)
                throw new InvalidOperationException("Missing or invalid source roster prefab: " + source.id);
        }
        return list.ToArray();
    }

    static BirdRecord BakeBird(Source source, Shader armourShader, Shader jetShader)
    {
        string container = OutputFolder + "/" + source.id + ".asset";
        var root = PrefabUtility.LoadPrefabContents(source.prefab);
        if (PrefabUtility.IsAnyPrefabInstanceRoot(root))
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var owned = new List<UnityEngine.Object>();
        bool containerCreated = false;
        try
        {
            var rigs = root.GetComponentsInChildren<SkyPulseNeonFinchPrototype>(true);
            if (rigs.Length != 1 || rigs[0].gameObject != root)
                throw new InvalidOperationException("Expected one root-bound roster rig: " + source.id);
            var rig = rigs[0];
            rig.animate = false; rig.turntable = false; rig.ResetPose();
            Transform[] bones = { rig.body, rig.head, rig.leftWing, rig.rightWing, rig.leftWingTip, rig.rightWingTip, rig.tail, rig.tailTip };
            if (bones.Any(b => b == null || !b.IsChildOf(root.transform)) || bones.Distinct().Count() != 8)
                throw new InvalidOperationException("Incomplete eight-bone source rig: " + source.id);
            var boneSet = new HashSet<Transform>(bones);
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (!(component is Transform) && !(component is MeshRenderer) && !(component is MeshFilter) && component != rig)
                    throw new InvalidOperationException("Unexpected source component requires explicit export support: " + component.GetType().Name);
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Source rig has no geometry: " + source.id);
            var sourceJet = rig.jetRenderer as MeshRenderer;
            if (rig.jetRenderer != null && sourceJet == null) throw new InvalidOperationException("Jet must be a rigid mesh renderer.");
            var materialCopies = new Dictionary<Material, Material>();
            Material CopyMaterial(Material original)
            {
                if (original == null) throw new InvalidOperationException("A source roster part has no material.");
                if (materialCopies.TryGetValue(original, out var existing)) return existing;
                string shader = AssetDatabase.GetAssetPath(original.shader);
                Shader replacement = shader == ArmourSource ? armourShader : shader == JetSource ? jetShader : null;
                if (replacement == null) throw new InvalidOperationException("Unexpected roster shader: " + shader);
                var copy = new Material(original) { name = original.name, shader = replacement };
                // The shader variants have identical properties. New Material
                // preserves queues, keyword state, textures and all saved values.
                materialCopies.Add(original, copy); owned.Add(copy); return copy;
            }
            var groups = new List<Group>();
            foreach (var renderer in renderers.Where(r => r != sourceJet))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Hidden source parts require an explicit export decision: " + renderer.name);
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || renderer.sharedMaterials.Length != filter.sharedMesh.subMeshCount)
                    throw new InvalidOperationException("Source mesh/material binding is incomplete: " + renderer.name);
                Transform bone = renderer.transform;
                while (bone != null && !boneSet.Contains(bone)) bone = bone.parent;
                if (bone == null) throw new InvalidOperationException("An unbound source mesh cannot be exported: " + renderer.name);
                for (int sub = 0; sub < filter.sharedMesh.subMeshCount; sub++)
                {
                    Material original = renderer.sharedMaterials[sub];
                    var group = groups.FirstOrDefault(g => g.bone == bone && g.material == original);
                    if (group == null) { group = new Group { bone = bone, material = original, template = renderer }; groups.Add(group); }
                    group.instances.Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = sub,
                        transform = bone.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                    group.vertices += filter.sharedMesh.vertexCount;
                }
            }
            void Store(UnityEngine.Object asset)
            {
                owned.Add(asset);
                if (!containerCreated) { AssetDatabase.CreateAsset(asset, container); containerCreated = true; }
                else AssetDatabase.AddObjectToAsset(asset, container);
            }
            int groupIndex = 0;
            foreach (var group in groups)
            {
                var combined = new Mesh { name = "Combined " + group.bone.name + " · " + group.material.name,
                    indexFormat = group.vertices > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                combined.CombineMeshes(group.instances.ToArray(), true, true, false);
                combined.RecalculateBounds(); Store(combined);
                var child = new GameObject("Baked articulated surface " + (++groupIndex).ToString("00"), typeof(MeshFilter), typeof(MeshRenderer));
                child.transform.SetParent(group.bone, false);
                child.GetComponent<MeshFilter>().sharedMesh = combined;
                var target = child.GetComponent<MeshRenderer>();
                target.sharedMaterial = CopyMaterial(group.material); CopyRendererState(group.template, target);
            }
            if (sourceJet != null)
            {
                var filter = sourceJet.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || sourceJet.sharedMaterials.Length != 1)
                    throw new InvalidOperationException("Source jet binding is incomplete.");
                var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh); mesh.name = "Volumetric jet · " + filter.sharedMesh.name; Store(mesh);
                filter.sharedMesh = mesh; sourceJet.sharedMaterial = CopyMaterial(sourceJet.sharedMaterial);
            }
            // Retain the exact source joint hierarchy, but remove the old leaf
            // surfaces after their transforms have been baked into bone space.
            foreach (var renderer in renderers.Where(r => r != sourceJet))
            {
                var transform = renderer.transform; var filter = renderer.GetComponent<MeshFilter>();
                UnityEngine.Object.DestroyImmediate(renderer); if (filter != null) UnityEngine.Object.DestroyImmediate(filter);
                if (transform.childCount == 0 && !boneSet.Contains(transform) && transform != root.transform)
                    UnityEngine.Object.DestroyImmediate(transform.gameObject);
            }
            foreach (var material in materialCopies.Values) AssetDatabase.AddObjectToAsset(material, container);
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(container);
            EditorUtility.SetDirty(mainAsset);
            AssetDatabase.SaveAssetIfDirty(mainAsset);
            root.name = source.name;
            rig.flapPeriod = 1.65f; rig.flapDegrees = 28; rig.bankDegrees = 5; rig.turntableDegreesPerSecond = 12;
            rig.animate = false; rig.turntable = true; rig.reducedMotion = false; rig.ResetPose();
            var runtime = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(source.id));
            if (runtime == null) throw new InvalidOperationException("Runtime prefab save failed: " + source.id);
            var outputRenderers = runtime.GetComponentsInChildren<MeshRenderer>(true);
            var outputMeshes = outputRenderers.Select(r => r.GetComponent<MeshFilter>().sharedMesh).ToArray();
            return new BirdRecord { id = source.id, name = source.name, sourcePrefab = source.prefab,
                runtimePrefab = PrefabPath(source.id), container = container, sourceRenderers = renderers.Length,
                runtimeRenderers = outputRenderers.Length, combinedMeshes = groups.Count, materials = materialCopies.Count,
                vertices = outputMeshes.Sum(m => m.vertexCount), triangles = outputMeshes.Sum(m => m.triangles.Length / 3), hasJet = sourceJet != null, editorDependencies = false };
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
            // Objects saved as sub-assets belong to AssetDatabase. Only destroy
            // an unsaved temporary object after a failed first bake.
            foreach (var asset in owned) if (asset != null && !AssetDatabase.Contains(asset)) UnityEngine.Object.DestroyImmediate(asset);
        }
    }
    static void CopyRendererState(MeshRenderer source, MeshRenderer target)
    {
        target.shadowCastingMode = source.shadowCastingMode; target.receiveShadows = source.receiveShadows;
        target.lightProbeUsage = source.lightProbeUsage; target.reflectionProbeUsage = source.reflectionProbeUsage;
        target.allowOcclusionWhenDynamic = source.allowOcclusionWhenDynamic; target.motionVectorGenerationMode = source.motionVectorGenerationMode;
        target.sortingLayerID = source.sortingLayerID; target.sortingOrder = source.sortingOrder;
        target.renderingLayerMask = source.renderingLayerMask;
    }
    static void ValidateRuntime(Source[] sources)
    {
        var reflection = AssetDatabase.LoadAssetAtPath<Cubemap>(ReflectionPath);
        if (reflection == null || reflection.width != 128 || AssetDatabase.GetDependencies(ReflectionPath, true).Any(p => p.IndexOf("/Editor/", StringComparison.Ordinal) >= 0))
            throw new InvalidOperationException("Runtime stage reflection must be an independent 128px cubemap.");
        foreach (var source in sources)
        {
            string path = PrefabPath(source.id);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing runtime prefab: " + source.id);
            string[] editor = AssetDatabase.GetDependencies(path, true).Where(p => p.StartsWith("Assets/Editor/", StringComparison.Ordinal) || p.IndexOf("/Editor/", StringComparison.Ordinal) >= 0).ToArray();
            if (editor.Length != 0) throw new InvalidOperationException("Runtime prefab retains Editor-only dependencies: " + string.Join(", ", editor));
            var rig = prefab.GetComponent<SkyPulseNeonFinchPrototype>();
            if (rig == null || rig.animate || rig.flapPeriod != 1.65f || rig.flapDegrees != 28 || rig.bankDegrees != 5 || rig.turntableDegreesPerSecond != 12)
                throw new InvalidOperationException("Runtime motion settings are inconsistent: " + source.id);
            if (prefab.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.sharedMaterials.Any(m => m == null || m.shader == null)))
                throw new InvalidOperationException("Runtime material/shader binding is incomplete: " + source.id);
        }
    }
    static void ExportReflection()
    {
        var source = AssetDatabase.LoadAssetAtPath<Cubemap>(ReflectionSource);
        if (source == null || source.width != 128)
            throw new InvalidOperationException("The approved 128px studio reflection cubemap is required.");
        var copy = UnityEngine.Object.Instantiate(source);
        copy.name = "SkyPulse bird studio reflection";
        AssetDatabase.CreateAsset(copy, ReflectionPath);
    }
    static Shader ExportShader(string source, string target, string oldName, string newName)
    {
        string text = File.ReadAllText(source), marker = "Shader \"" + oldName + "\"";
        if (text.IndexOf(marker, StringComparison.Ordinal) < 0) throw new InvalidOperationException("Canonical roster shader name changed: " + source);
        string generated = "// GENERATED by SkyPulseBirdRosterRuntimeBaker. Edit the canonical authoring shader, not this variant.\n"
            + "// Canonical: " + source + "\n// Source SHA256: " + Hash(source) + "\n"
            + text.Replace(marker, "Shader \"" + newName + "\"");
        File.WriteAllText(target, generated, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(target);
        if (shader == null || !shader.isSupported || ShaderUtil.GetShaderMessages(shader).Any(m => m.severity.ToString() == "Error"))
            throw new InvalidOperationException("Generated runtime shader did not compile: " + target);
        return shader;
    }
    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
    static string Hash(string path)
    { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
    static Stamp[] InputStamps(Source[] sources)
    {
        var paths = new SortedSet<string>(StringComparer.Ordinal) { BakerSource, SkyPulseBirdRosterWorkshop.DesignsPath, ArmourSource, JetSource, ReflectionSource };
        foreach (var source in sources)
            foreach (string path in AssetDatabase.GetDependencies(source.prefab, true)) if (File.Exists(path)) paths.Add(path);
        foreach (string path in paths.ToArray()) if (File.Exists(path + ".meta")) paths.Add(path + ".meta");
        foreach (string path in paths.Where(p => !p.EndsWith(".meta", StringComparison.Ordinal)))
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                // Text-source assets can be marked dirty by Unity's import/compile cache.
                // Their on-disk source and importer metadata are already hashed;
                // editable serialized meshes/materials/prefabs still require a save.
                if (asset != null && !(asset is Shader) && !(asset is TextAsset) && EditorUtility.IsDirty(asset)) throw new InvalidOperationException("Unsaved source edit; save deliberately before export: " + path);
        return paths.Select(p => new Stamp { path = p, sha256 = Hash(p) }).ToArray();
    }
    static Stamp[] OutputStamps()
    {
        var paths = new SortedSet<string>(StringComparer.Ordinal);
        if (Directory.Exists(OutputFolder))
            foreach (string path in Directory.GetFiles(OutputFolder, "*", SearchOption.AllDirectories))
                if (path.Replace('\\', '/') != ManifestPath && Path.GetFileName(path) != ".DS_Store") paths.Add(path.Replace('\\', '/'));
        foreach (string path in new[] { OutputFolder + ".meta", ArmourShaderPath, ArmourShaderPath + ".meta", JetShaderPath, JetShaderPath + ".meta" }) if (File.Exists(path)) paths.Add(path);
        return paths.Select(p => new Stamp { path = p, sha256 = Hash(p) }).ToArray();
    }
    static Stamp[] AuthoringSnapshot()
    {
        return Directory.GetFiles(SkyPulseBirdPrototypeWorkshop.Folder, "*", SearchOption.AllDirectories)
            .Where(p => Path.GetFileName(p) != ".DS_Store").OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => new Stamp { path = p.Replace('\\', '/'), sha256 = Hash(p) }).ToArray();
    }
    static bool Same(Stamp[] a, Stamp[] b)
    { return a != null && b != null && a.Length == b.Length && a.Select(s => s.path + ":" + s.sha256).SequenceEqual(b.Select(s => s.path + ":" + s.sha256), StringComparer.Ordinal); }
    static string Digest(Stamp[] stamps)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(string.Join("\n", stamps.Select(s => s.path + ":" + s.sha256)));
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }
}
