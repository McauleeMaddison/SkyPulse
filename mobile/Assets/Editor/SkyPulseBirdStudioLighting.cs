#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Neutral reflected studio light for the isolated Finch review scene.</summary>
public static class SkyPulseBirdStudioLighting
{
    public const string ReflectionPath = SkyPulseBirdPrototypeWorkshop.Folder + "/Lighting/Finch Studio Reflection.asset";
    const string MenuPath = "Tools/SkyPulse/Bird Workshop/Apply neutral metal review lighting";
    const int ReflectionSize = 128;

    static readonly string[] LightNames =
    {
        "Key · broad cool studio",
        "Fill · soft cyan bounce",
        "Rim · warm metal edge"
    };

    [MenuItem(MenuPath)]
    static void ApplyAndSave()
    {
        ApplyToOpenReviewScene(true);
    }

    [MenuItem(MenuPath, true)]
    static bool CanApplyAndSave()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode
            && string.Equals(SceneManager.GetActiveScene().path, SkyPulseBirdPrototypeWorkshop.ScenePath, StringComparison.Ordinal);
    }

    /// <summary>
    /// Changes only the open review scene's environment and its three existing
    /// light colours/intensities. Camera, geometry, transforms and pose are retained.
    /// A reflection asset is authored once; subsequent calls retain owner edits.
    /// </summary>
    public static void ApplyToOpenReviewScene(bool save)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before applying Finch review lighting.");

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded
            || !string.Equals(scene.path, SkyPulseBirdPrototypeWorkshop.ScenePath, StringComparison.Ordinal))
            throw new InvalidOperationException("Open the existing Finch art-review scene before applying its lighting.");

        // Validate every required object before creating assets or changing settings.
        Light[] lights = FindExistingLights(scene);
        Cubemap reflection = LoadOrCreateReflection();
        if (!Application.isBatchMode)
            Undo.RecordObjects(lights, "Apply neutral Finch studio lighting");

        lights[0].color = new Color(1f, .985f, .96f);
        lights[0].intensity = 1.05f;
        lights[1].color = new Color(.94f, .965f, 1f);
        lights[1].intensity = .42f;
        lights[2].color = new Color(1f, 1f, 1f);
        lights[2].intensity = .85f;
        foreach (Light light in lights)
            EditorUtility.SetDirty(light);

        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = reflection;
        RenderSettings.reflectionIntensity = .85f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.35f, .39f, .46f);
        RenderSettings.ambientEquatorColor = new Color(.16f, .19f, .23f);
        RenderSettings.ambientGroundColor = new Color(.07f, .09f, .12f);
        RenderSettings.ambientIntensity = 1f;

        EditorSceneManager.MarkSceneDirty(scene);
        if (save && !EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Finch review lighting was applied, but the scene could not be saved.");
        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
        Debug.Log("SKYPULSE_FINCH_STUDIO_LIGHTING: neutral three-light environment and existing reflection asset applied; scene "
            + (save ? "saved." : "left unsaved."));
    }

    static Light[] FindExistingLights(Scene scene)
    {
        var found = new Light[LightNames.Length];
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                for (int i = 0; i < LightNames.Length; i++)
                {
                    if (!string.Equals(light.gameObject.name, LightNames[i], StringComparison.Ordinal))
                        continue;
                    if (found[i] != null)
                        throw new InvalidOperationException("Review scene contains duplicate studio lights: " + LightNames[i]);
                    if (light.type != LightType.Directional)
                        throw new InvalidOperationException("Existing studio light must remain directional: " + LightNames[i]);
                    found[i] = light;
                }
            }
        }
        for (int i = 0; i < found.Length; i++)
            if (found[i] == null)
                throw new InvalidOperationException("Existing studio light is missing: " + LightNames[i]);
        return found;
    }

    static Cubemap LoadOrCreateReflection()
    {
        UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(ReflectionPath);
        if (existing != null)
        {
            var existingCube = existing as Cubemap;
            if (existingCube == null)
                throw new InvalidOperationException("Finch reflection path is occupied by a different asset type: " + ReflectionPath);
            return existingCube;
        }
        if (System.IO.File.Exists(ReflectionPath))
            throw new InvalidOperationException("Existing Finch reflection asset could not be loaded; it has been preserved: " + ReflectionPath);

        string folder = SkyPulseBirdPrototypeWorkshop.Folder + "/Lighting";
        if (!AssetDatabase.IsValidFolder(SkyPulseBirdPrototypeWorkshop.Folder))
            throw new InvalidOperationException("Existing Finch workshop folder is missing.");
        if (!AssetDatabase.IsValidFolder(folder))
        {
            string guid = AssetDatabase.CreateFolder(SkyPulseBirdPrototypeWorkshop.Folder, "Lighting");
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException("Could not create the Finch workshop lighting folder.");
        }

        // Unity exposes RGBAHalf, not RGBHalf; alpha remains one and carries no light.
        var cube = new Cubemap(ReflectionSize, TextureFormat.RGBAHalf, true)
        {
            name = "Finch Studio Reflection",
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp,
            anisoLevel = 1
        };
        try
        {
            var pixels = new Color[ReflectionSize * ReflectionSize];
            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                var face = (CubemapFace)faceIndex;
                for (int y = 0; y < ReflectionSize; y++)
                {
                    float v = (y + .5f) * 2f / ReflectionSize - 1f;
                    for (int x = 0; x < ReflectionSize; x++)
                    {
                        float u = (x + .5f) * 2f / ReflectionSize - 1f;
                        pixels[y * ReflectionSize + x] = StudioRadiance(FaceDirection(face, u, v));
                    }
                }
                cube.SetPixels(pixels, face, 0);
            }
            // RGBAHalf stores linear HDR radiance. Mips soften the broad source
            // shapes on rough surfaces without adding scene geometry or effects.
            cube.Apply(true, false);
            AssetDatabase.CreateAsset(cube, ReflectionPath);
            AssetDatabase.SaveAssetIfDirty(cube);
            return cube;
        }
        catch
        {
            if (!AssetDatabase.Contains(cube))
                UnityEngine.Object.DestroyImmediate(cube);
            throw;
        }
    }

    static Vector3 FaceDirection(CubemapFace face, float u, float v)
    {
        switch (face)
        {
            case CubemapFace.PositiveX: return new Vector3(1f, -v, -u).normalized;
            case CubemapFace.NegativeX: return new Vector3(-1f, -v, u).normalized;
            case CubemapFace.PositiveY: return new Vector3(u, 1f, v).normalized;
            case CubemapFace.NegativeY: return new Vector3(u, -1f, -v).normalized;
            case CubemapFace.PositiveZ: return new Vector3(u, -v, 1f).normalized;
            default: return new Vector3(-u, -v, -1f).normalized;
        }
    }

    static Color StudioRadiance(Vector3 direction)
    {
        float height = Mathf.SmoothStep(0f, 1f, (direction.y + 1f) * .5f);
        Color radiance = Color.Lerp(new Color(.25f, .27f, .30f), new Color(.5f, .52f, .55f), height);
        radiance += new Color(1f, .985f, .96f) * (1.5f * Softbox(direction, new Vector3(-.45f, .55f, -.70f), 44f, 22f, 18f));
        radiance += new Color(.95f, .975f, 1f) * (.75f * Softbox(direction, new Vector3(.65f, .25f, -.72f), 26f, 46f, -10f));
        radiance += Color.white * (1.1f * Softbox(direction, new Vector3(-.50f, .40f, .77f), 12f, 48f, -15f));
        radiance.a = 1f;
        return radiance;
    }

    static float Softbox(Vector3 direction, Vector3 centre, float halfWidth, float halfHeight, float rotation)
    {
        centre.Normalize();
        float forward = Vector3.Dot(direction, centre);
        if (forward <= 0f)
            return 0f;
        Vector3 right = Vector3.Cross(Vector3.up, centre).normalized;
        Vector3 up = Vector3.Cross(centre, right);
        Quaternion spin = Quaternion.AngleAxis(rotation, centre);
        right = spin * right;
        up = spin * up;
        float horizontal = Mathf.Abs(Mathf.Atan2(Vector3.Dot(direction, right), forward) * Mathf.Rad2Deg) / halfWidth;
        float vertical = Mathf.Abs(Mathf.Atan2(Vector3.Dot(direction, up), forward) * Mathf.Rad2Deg) / halfHeight;
        float across = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.55f, 1f, horizontal));
        float along = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.55f, 1f, vertical));
        return (1f - across) * (1f - along);
    }
}
#endif
