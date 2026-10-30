#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

namespace SkyPulse.Mobile.Editor
{
    // SkyPulse constructs its runtime graph in Awake. A script hot reload keeps
    // Unity objects but drops nonserializable catalogue/pool/session objects and
    // cannot resume that graph safely. End the Editor play session before reload;
    // a fresh Play runs the ordinary startup and loads the existing save.
    [InitializeOnLoad]
    public static class SkyPulsePlaySessionGuard
    {
        const string PendingKey = "SkyPulse.StopPlayForCompilation";
        public const string StopCountKey = "SkyPulse.CompilationStopCount";
        static bool Enabled => !Application.isBatchMode ||
            Environment.GetEnvironmentVariable("SKYPULSE_VALIDATE_RELOAD_GUARD") == "1";

        static SkyPulsePlaySessionGuard()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
        }

        static void OnCompilationStarted(object context)
        {
            if (!Enabled || !EditorApplication.isPlaying ||
                UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>() == null) return;
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StopCountKey, SessionState.GetInt(StopCountKey, 0) + 1);
            EditorApplication.ExitPlaymode();
            Debug.Log("SkyPulse: Play stopped safely for script compilation. Press Play after compilation to restart with your saved progress.");
        }

        static void BeforeReload()
        {
            // Fallback if Unity deferred the first exit until the compile ends.
            if (Enabled && SessionState.GetBool(PendingKey, false) && EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
        }

        [DidReloadScripts]
        static void AfterReload()
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            SessionState.EraseBool(PendingKey);
            if (Enabled && EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}
#endif
