using UnityEngine;

namespace SkyPulse.Mobile
{
    public static class SkyPulseBootstrap
    {
        // Editing and inspection scenes must never acquire gameplay cameras,
        // HUD, saves or pooled obstacles. Player startup semantics stay intact.
        public static bool ShouldStartInScene(string path,bool isEditor)
            => !isEditor || string.Equals(path,"Assets/Scenes/SkyPulse.unity",System.StringComparison.Ordinal);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartSkyPulse()
        {
            if (!ShouldStartInScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,Application.isEditor)) return;
            if (Object.FindAnyObjectByType<SkyPulseNativeGame>() != null) return;

            var root = new GameObject("SkyPulse Native");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<SkyPulseNativeGame>();
        }
    }
}
