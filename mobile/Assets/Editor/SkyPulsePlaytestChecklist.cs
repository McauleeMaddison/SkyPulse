#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SkyPulse.Mobile.Editor
{
    /// <summary>
    /// A compact, editor-only reminder for the feedback that should drive a balance
    /// pass. It mirrors the live, single-route cyber-flight experience and never
    /// ships in a player build.
    /// </summary>
    public static class SkyPulsePlaytestChecklist
    {
        [MenuItem("SkyPulse/Playtest Checklist")]
        public static void Show()
        {
            EditorUtility.DisplayDialog(
                "SkyPulse playtest",
                "Play 12 portrait runs: 4 to score 5, 4 to score 15, and 4 to score 30.\n\n" +
                "Check:\n" +
                "• Do the first three static gates teach the 34% opening without surprise?\n" +
                "• Is every bright cap visibly outside its safe gap? Press F4 to inspect.\n" +
                "• Does the 15-gate Foundry tunnel give a clear recovery beat before its first static gate?\n" +
                "• Are Foundry drift, Bazaar high/low, and post-45 remix patterns telegraphed and reachable?\n" +
                "• Do crystal arcs stay inside the viable line, bank on contact, and remain separate from score?\n" +
                "• Does Aegis recover controllably, and do Time Pulse and Magnet preserve the same handling?\n" +
                "• On a notched/tall and a wide device simulation, does the safe-area HUD stay clear while gate geometry remains 9:16?\n\n" +
                "Record score, world reached, and one sentence after each run. Change one tuning value only after a repeated pattern.",
                "Start testing");
        }
    }
}
#endif
