#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SkyPulse.Mobile.Editor
{
    /// <summary>
    /// A compact, editor-only reminder for the only feedback that should drive the
    /// next balance pass. It never ships in a player build.
    /// </summary>
    public static class SkyPulsePlaytestChecklist
    {
        [MenuItem("SkyPulse/Playtest Checklist")]
        public static void Show()
        {
            EditorUtility.DisplayDialog(
                "SkyPulse playtest",
                "Play 10 Classic runs, then 5 Adventure runs.\n\n" +
                "Check:\n" +
                "• Is every cap visibly outside the safe gap? Press F4 to inspect.\n" +
                "• Can you recover into low and high gaps without fighting the controls?\n" +
                "• Do crystal drops feel occasional and worth chasing?\n" +
                "• Are power-ups helpful without replacing skill?\n\n" +
                "Record the score and one sentence after each run. Change one tuning value only after you see a repeated pattern.",
                "Start testing");
        }
    }
}
#endif
