using UnityEngine;

namespace SkyPulse.Mobile
{
    public static class SkyPulseBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartSkyPulse()
        {
            if (Object.FindFirstObjectByType<SkyPulseNativeGame>() != null) return;

            var root = new GameObject("SkyPulse Native");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<SkyPulseNativeGame>();
        }
    }
}
