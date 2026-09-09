using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

// QA-only helper: copy into Assets/Editor of an isolated project, never shipping Assets.
// Graphics are required. SKYPULSE_QA_OUTPUT names the destination; optional
// SKYPULSE_NEON_BASELINE names a previous geometry.txt to compare with this run.
[InitializeOnLoad]
public static class SkyPulseNeonVisualCapture
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const int Width = 660, Height = 1434;
    static bool Updated => Environment.GetEnvironmentVariable("SKYPULSE_NEON_EXPECT_UPDATED") == "1";
    static object Get(object o, string n) => o.GetType().GetField(n, F).GetValue(o);
    static void Set(object o, string n, object v) => o.GetType().GetField(n, F).SetValue(o, v);
    static object Call(object o, string n, params object[] a)
    {
        foreach (var m in o.GetType().GetMethods(F | BindingFlags.Static))
            if (m.Name == n && m.GetParameters().Length == a.Length) return m.Invoke(o, a);
        throw new MissingMethodException(n);
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static SkyPulseNeonVisualCapture() { EditorApplication.update += Tick; }
    public static void Run()
    {
        Check(Application.productName.Contains("QA"), "Use an isolated QA product to protect player saves");
        PlayerPrefs.DeleteAll();
        SessionState.SetBool("SkyPulseNeonVisualCapture", true);
        EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if (!SessionState.GetBool("SkyPulseNeonVisualCapture", false) || !EditorApplication.isPlaying) return;
        SessionState.SetBool("SkyPulseNeonVisualCapture", false);
        try { Capture(); Debug.Log("SKYPULSE_NEON_VISUAL_PASS"); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Capture()
    {
        var folder = Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");
        Check(!string.IsNullOrEmpty(folder), "Set SKYPULSE_QA_OUTPUT"); Directory.CreateDirectory(folder);
        var game = UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();
        if (game == null) game = new GameObject("Neon visual QA fixture").AddComponent<SkyPulseNativeGame>();
        game.enabled = false; Set(game, "hapticsEnabled", false);
        var camera = (Camera)Get(game, "flightCamera");
        var rt = RenderTexture.GetTemporary(Width, Height, 24);
        camera.targetTexture = rt; camera.rect = new Rect(0, 0, 1, 1);
        var canvas = ((GameObject)Get(game, "uiRoot")).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
        Call(game, "RefreshViewportDecor");
        var worlds = (Array)typeof(SkyPulseNativeGame).GetField("Worlds", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var geometry = new List<string>();
        var reducedPickupStatic = true;
        for (var world = 0; world < 3; world++)
        {
            foreach (var reduced in new[] { false, true })
            {
                PrepareFlight(game, worlds.GetValue(world), world, reduced);
                var pipe = ((IList)Get(game, "pipePool"))[0];
                if (!reduced) { CheckGeometry(pipe, geometry, world); CheckCollisionEdges(game, pipe); }
                var pickup = ((IList)Get(game, "crystalPickupPool"))[0];
                if (reduced)
                {
                    var root = (Transform)Get(pickup, "Transform");
                    var art = (SpriteRenderer)Get(pickup, "Artwork");
                    var glow = (SpriteRenderer)Get(pickup, "Glow");
                    var spark = (SpriteRenderer)Get(pickup, "Spark");
                    var p = root.localPosition; var scale = art.transform.localScale; var rotation = art.transform.localRotation;
                    var glowScale = glow.transform.localScale; var sparkPosition = spark.transform.localPosition;
                    Set(game, "ambientTime", 4.7f); Call(game, "UpdateCrystalPickups", 0f);
                    reducedPickupStatic &= p == root.localPosition && scale == art.transform.localScale && rotation == art.transform.localRotation
                        && glowScale == glow.transform.localScale && sparkPosition == spark.transform.localPosition;
                    Set(game, "ambientTime", 1.2f); Call(game, "UpdateCrystalPickups", 0f);
                }
                Save(game, camera, Path.Combine(folder, $"world-{world}-{(reduced ? "reduced" : "normal")}.png"));
            }
        }
        var geometryText = string.Join("\n", geometry) + "\n";
        File.WriteAllText(Path.Combine(folder, "geometry.txt"), geometryText);
        var baseline = Environment.GetEnvironmentVariable("SKYPULSE_NEON_BASELINE");
        if (!string.IsNullOrEmpty(baseline)) Check(File.ReadAllText(baseline) == geometryText, "Gameplay collider geometry changed from baseline");
        if (Updated)
            Check(reducedPickupStatic, "Reduced Motion pickup moved, rotated, pulsed or orbited");

        PrepareFlight(game, worlds.GetValue(0), 0, false);
        var crystal = ((IList)Get(game, "crystalPickupPool"))[0];
        var wallet = (int)Get(game, "crystals");
        Call(game, "CollectCrystalPickup", crystal);
        Check(!(bool)Get(crystal, "Active") && !((GameObject)Get(crystal, "Root")).activeSelf, "Collected pickup remained visible");
        Check((int)Get(game, "crystals") > wallet, "Collection failed to bank currency");
        if (Updated) CheckActiveBurst(game, false);
        Call(game, "UpdateFlightFeedback", .07f);
        UpdateCrystalBurst(game, .07f);
        Save(game, camera, Path.Combine(folder, "pickup-contact.png"));
        Call(game, "UpdateFlightFeedback", 1f);
        UpdateCrystalBurst(game, 1f);
        CheckEffectsCleared(game);
        Save(game, camera, Path.Combine(folder, "pickup-expired.png"));
        if (Updated)
        {
            // Every reset starts with an active effect, rather than checking an
            // already expired one. These use the shipping collection handler.
            PrepareFlight(game, worlds.GetValue(0), 0, false);
            Call(game, "CollectCrystalPickup", ((IList)Get(game, "crystalPickupPool"))[0]);
            CheckActiveBurst(game, false); Call(game, "StartFlight"); CheckEffectsCleared(game);
            PrepareFlight(game, worlds.GetValue(0), 0, false);
            Call(game, "CollectCrystalPickup", ((IList)Get(game, "crystalPickupPool"))[0]);
            CheckActiveBurst(game, false); Call(game, "ResetToMenu"); CheckEffectsCleared(game);
            PrepareFlight(game, worlds.GetValue(0), 0, false);
            Call(game, "CollectCrystalPickup", ((IList)Get(game, "crystalPickupPool"))[0]);
            CheckActiveBurst(game, false); Call(game, "BeginWorldTransition", 1); CheckCrystalBurstsCleared(game);

            PrepareFlight(game, worlds.GetValue(0), 0, true);
            Call(game, "CollectCrystalPickup", ((IList)Get(game, "crystalPickupPool"))[0]);
            var burst = CheckActiveBurst(game, true);
            var root = (GameObject)Get(burst, "Root");
            var ring = (SpriteRenderer)Get(burst, "Ring");
            var position = root.transform.localPosition; var scale = ring.transform.localScale;
            UpdateCrystalBurst(game, .08f);
            Check(root.transform.localPosition == position && ring.transform.localScale == scale, "Reduced Motion collection burst moved or expanded");
            CheckActiveBurst(game, true); UpdateCrystalBurst(game, 1f); CheckCrystalBurstsCleared(game);
        }
        foreach (var screen in new[] { "home", "hangar", "upgrades" })
        {
            Call(game, "ResetToMenu");
            if (screen == "hangar") Call(game, "OpenHangar");
            if (screen == "upgrades") Call(game, "OpenUpgrades");
            Call(game, "UpdateMenuBird", .1f);
            Save(game, camera, Path.Combine(folder, "menu-" + screen + ".png"));
        }
        camera.targetTexture = null; RenderTexture.active = null; RenderTexture.ReleaseTemporary(rt);
        File.WriteAllText(Path.Combine(folder, "PASS.txt"), "11 actual Unity Camera.Render captures at 660x1434. Three worlds, normal/reduced motion, pickup contact/expiration, home/hangar/upgrades. Collider bounds align with physical gap; contact banks currency; effects expire and reset.\nReduced Motion pickup static: " + reducedPickupStatic + "\nNew crystal burst lifecycle checked: " + Updated + "\nGeometry baseline compared: " + (!string.IsNullOrEmpty(baseline)) + "\nEditor rendering only; does not certify a physical iPhone build.\n");
    }
    static void PrepareFlight(SkyPulseNativeGame game, object world, int index, bool reduced)
    {
        Call(game, "StartFlight"); Set(game, "reduceMotionEnabled", reduced); Set(game, "ambientTime", 1.2f);
        Set(game, "routeWorldIndex", index); Set(game, "routeWorld", world); Call(game, "ApplyRouteWorldVisuals");
        Set(game, "birdY", 0f); Set(game, "birdVelocity", 0f); Call(game, "UpdateBird", 0f); Call(game, "UpdateAmbientVisuals");
        foreach (var p in (IList)Get(game, "powerUpPool")) Call(game, "DeferPowerUp", p, 10f);
        foreach (var p in (IList)Get(game, "crystalPickupPool")) Call(game, "DeferCrystalPickup", p, 10f);
        var pipes = (IList)Get(game, "pipePool");
        for (var i = 0; i < pipes.Count; i++)
        {
            var p = pipes[i]; ((GameObject)Get(p, "Root")).SetActive(i < 2);
            Set(p, "X", i == 0 ? 1.75f : -5.6f); Set(p, "GapCenter", i == 0 ? 0f : 1f);
            Set(p, "BaseGapCenter", (float)Get(p, "GapCenter")); Set(p, "GapHeight", 5.8f);
            Set(p, "IsStatic", true); Set(p, "DriftAmplitude", 0f); Set(p, "Passed", i != 0);
            ((GameObject)Get(p, "Root")).transform.localPosition = new Vector3((float)Get(p, "X"), 0, 0);
            Call(game, "LayoutPipePair", p); Call(game, "AnimatePipePair", p);
        }
        var pickup = ((IList)Get(game, "crystalPickupPool"))[0];
        Call(game, "ConfigureCrystalPickup", pickup, pipes[0]); Set(pickup, "Phase", 0f); Set(pickup, "GapOffset", -.25f);
        Call(game, "UpdateCrystalPickups", 0f); Physics2D.SyncTransforms();
        Check(!((bool)Call(game, "BirdCollidesWithPipe", pipes[0])), "Deterministic clear opening collides");
    }
    static void CheckGeometry(object pipe, List<string> lines, int world)
    {
        var center = (float)Get(pipe, "GapCenter"); var halfGap = (float)Get(pipe, "GapHeight") * .5f;
        foreach (var name in new[] { "Top", "Bottom" })
        {
            var surface = Get(pipe, name);
            var body = (BoxCollider2D)Get(surface, "BodyCollider"); var cap = (BoxCollider2D)Get(surface, "CapCollider");
            Check(Mathf.Abs(body.size.x - 1.72f) < .0001f && Mathf.Abs(cap.size.x - 2.06f) < .0001f, "Obstacle collision width changed");
            var edge = name == "Top" ? cap.bounds.min.y : cap.bounds.max.y;
            Check(Mathf.Abs(edge - (center + (name == "Top" ? halfGap : -halfGap))) < .0001f, "Cap collider encroaches into visible gap");
            var capArt = (SpriteRenderer)Get(surface, "CapOuter");
            if (capArt.enabled) Check(Mathf.Abs(capArt.bounds.center.y - cap.bounds.center.y) < .001f, "Cap artwork center misaligns with collider");
            lines.Add($"world={world} {name} body={body.size:F6} at={body.transform.localPosition:F6} cap={cap.size:F6} at={cap.transform.localPosition:F6}");
        }
    }
    static void CheckEffectsCleared(SkyPulseNativeGame game)
    {
        foreach (var name in new[] { "flightFeedbackRenderer", "flightFeedbackRingRenderer" })
            Check(!((SpriteRenderer)Get(game, name)).enabled, "Feedback survived expiration/reset: " + name);
        CheckCrystalBurstsCleared(game);
    }
    static void CheckCollisionEdges(SkyPulseNativeGame game, object pipe)
    {
        var bird = (CapsuleCollider2D)Get(game, "birdBodyCollider");
        var position = bird.transform.position; var rotation = bird.transform.rotation;
        try
        {
            bird.transform.rotation = Quaternion.identity;
            var halfBird = bird.size.y * Mathf.Abs(bird.transform.lossyScale.y) * .5f;
            var centre = (float)Get(pipe, "GapCenter"); var halfGap = (float)Get(pipe, "GapHeight") * .5f;
            var x = ((GameObject)Get(pipe, "Root")).transform.position.x;
            foreach (var direction in new[] { -1f, 1f })
            {
                bird.transform.position = new Vector3(x, centre + direction * (halfGap - halfBird - .02f), position.z);
                Physics2D.SyncTransforms();
                Check(!(bool)Call(game, "BirdCollidesWithPipe", pipe), "Bird collides while fully inside the visible gate gap");
                bird.transform.position += Vector3.up * direction * .04f;
                Physics2D.SyncTransforms();
                Check((bool)Call(game, "BirdCollidesWithPipe", pipe), "Bird passes through a solid gate collar");
            }
        }
        finally { bird.transform.position = position; bird.transform.rotation = rotation; Physics2D.SyncTransforms(); }
    }
    static void UpdateCrystalBurst(SkyPulseNativeGame game, float deltaTime)
    {
        if (Updated) Call(game, "UpdateCrystalPickupBursts", deltaTime);
    }
    static object CheckActiveBurst(SkyPulseNativeGame game, bool reduced)
    {
        object active = null;
        foreach (var burst in (IList)Get(game, "crystalPickupBursts"))
        {
            if (!((GameObject)Get(burst, "Root")).activeSelf) continue;
            active = burst;
            Check((float)Get(burst, "Remaining") > 0f, "Visible burst has expired timer");
            foreach (var spark in (SpriteRenderer[])Get(burst, "Sparks"))
                Check(spark.enabled == !reduced, "Collection spark violates motion preference");
        }
        Check(active != null, "Collection did not create pickup feedback");
        return active;
    }
    static void CheckCrystalBurstsCleared(SkyPulseNativeGame game)
    {
        if (!Updated) return;
        foreach (var burst in (IList)Get(game, "crystalPickupBursts"))
            Check(!((GameObject)Get(burst, "Root")).activeSelf && (float)Get(burst, "Remaining") == 0f, "Crystal burst survived expiration/reset");
    }
    static void Save(SkyPulseNativeGame game, Camera camera, string path)
    {
        Canvas.ForceUpdateCanvases();
        var safe = (RectTransform)Get(game, "safeAreaRoot");
        safe.anchorMin = new Vector2(0, .035f); safe.anchorMax = new Vector2(1, .935f); safe.offsetMin = safe.offsetMax = Vector2.zero;
        Canvas.ForceUpdateCanvases(); Call(game, "FitInterfaceToSafeArea"); Canvas.ForceUpdateCanvases();
        camera.Render(); RenderTexture.active = camera.targetTexture;
        var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); RenderTexture.active = null;
    }
}
