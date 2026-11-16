#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkyPulse.Mobile.Editor
{
    // Editor-only acceptance entry point. It opens the actual shipping scene;
    // QA currency/unlocks, resets and capture helpers never run on the owner save.
    [InitializeOnLoad]
    public static class SkyPulsePlaytestChecklist
    {
        const string GameScene = "Assets/Scenes/SkyPulse.unity";
        const string ReviewPending = "SkyPulse.VisualAcceptancePending";
        static SkyPulsePlaytestChecklist() { EditorApplication.playModeStateChanged += OnPlayReady; }

        [MenuItem("SkyPulse/Playtest Checklist")]
        public static void Show()
        {
            int action = EditorUtility.DisplayDialogComplex(
                "SkyPulse visual acceptance",
                "Review the current local source in portrait 9:16. iPhone Build 21 is separate.\n\n" +
                "• Endless: selected 3D bird, full tap-driven wing stroke, all 12 worlds and clean pipe joins.\n" +
                "• Relay: matching crystals open gates immediately; fly through the real hoop opening.\n" +
                "• Blackout: readable live beam, separate city window, named restoration and instant next puzzle.\n" +
                "• Hangar, training, breeding and Tech Tree: clear benefits and comfortable navigation.\n" +
                "• Homes, guides and results: legible labels, safe-area spacing, pause/retry/Home and Reduced Motion.\n\n" +
                "Use your own saved progress; do not reset it. Mark visual approval only after your review. " +
                "No phone installation or App Review submission happens here.",
                "Play Main Game", "Close", "Read Checklist");
            if (action == 0) PlayMainGame();
            else if (action == 2)
                Application.OpenURL(new System.Uri(Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../Development/Current/ACCEPTANCE_CHECKLIST.md"))).AbsoluteUri);
        }

        [MenuItem("SkyPulse/Play Main Game")]
        public static void PlayMainGame()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Do not replace a live run or an unsaved workshop scene.
                if (SceneManager.GetActiveScene().path != GameScene)
                    Debug.LogWarning("SkyPulse: Stop the current Play session, then choose SkyPulse → Play Main Game.");
                FocusGame();
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (SceneManager.GetActiveScene().path != GameScene)
                EditorSceneManager.OpenScene(GameScene);
            SkyPulseNativeGame.ReviewOriginalBirdArtwork = false;
            SkyPulseBirdRosterPreview.Select("", false);
            foreach (var window in Resources.FindObjectsOfTypeAll<SkyPulseBirdRosterPreviewWindow>()) window.Close();
            SessionState.SetBool(ReviewPending, true);
            EditorApplication.EnterPlaymode();
            EditorApplication.delayCall += FocusGame;
        }

        static void OnPlayReady(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(ReviewPending,false)) return;
            SessionState.EraseBool(ReviewPending);
            FocusGame();
            var game = Object.FindAnyObjectByType<SkyPulseNativeGame>();
            if (game != null && game.RuntimeSolidBirdsActive)
                Debug.Log("SkyPulse: Main game is running for visual acceptance with native 3D birds. Existing saved progress is retained.");
        }

        static void FocusGame()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
    }
}
#endif
