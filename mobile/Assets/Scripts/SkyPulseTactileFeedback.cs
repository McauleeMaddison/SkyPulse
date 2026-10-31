using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace SkyPulse.Mobile
{
    // Presentation only. The game coordinator owns the enabled preference,
    // event selection and cooldown; this bridge never writes game or save state.
    internal static class SkyPulseTactileFeedback
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal", EntryPoint="SkyPulseTactilePrepare")]
        static extern void NativePrepare();
        [DllImport("__Internal", EntryPoint="SkyPulseTactilePulse")]
        static extern void NativePulse(int impact);
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void PrepareAtLaunch()
        {
#if UNITY_IOS && !UNITY_EDITOR
            // Preparation is silent and optional. Honour the existing saved
            // preference rather than waking the motor for disabled feedback.
            if(Application.isFocused && PlayerPrefs.GetInt("skypulse.native.haptics",1)==1)
                NativePrepare();
#endif
        }
        internal static void Pulse(bool impact)
        {
#if UNITY_IOS && !UNITY_EDITOR
            NativePulse(impact?1:0);
#elif !UNITY_EDITOR
            // Preserve the existing behaviour on other mobile platforms.
            Handheld.Vibrate();
#endif
        }
    }
}
