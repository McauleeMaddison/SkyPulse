using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class SkyPulseBirdRosterPreviewWindow:EditorWindow
{
    [MenuItem("Tools/SkyPulse/Bird Workshop/3D roster in actual gameplay")]
    public static void Open(){GetWindow<SkyPulseBirdRosterPreviewWindow>("3D Bird Review").Show();}
    void OnGUI()
    {
        GUILayout.Label("Live 3D bird review",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Play the normal SkyPulse scene. This changes only the Editor bird visual; the equipped bird's stats, controls and player save stay in use.",MessageType.Info);
        var designs=SkyPulseBirdRosterWorkshop.LoadDesigns();var names=new[]{"Use equipped bird","Neon Finch"}.Concat(designs.Select(d=>d.name)).ToArray();var ids=new[]{"","neon_finch"}.Concat(designs.Select(d=>d.id)).ToArray();
        int old=Array.IndexOf(ids,SkyPulseBirdRosterPreview.SelectedId),selected=EditorGUILayout.Popup("Model",Mathf.Max(0,old),names);
        bool enabled=EditorGUILayout.Toggle("Show 3D model",SkyPulseBirdRosterPreview.Enabled);
        if(old!=selected||enabled!=SkyPulseBirdRosterPreview.Enabled)SkyPulseBirdRosterPreview.Select(ids[selected],enabled);
        if(GUILayout.Button("Open SkyPulse game scene"))
        {if(!EditorApplication.isPlaying&&EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");}
        GUILayout.Label("Enter Endless Route or Pulse Relay and press Play.\nUse the dropdown to inspect any saved model.",EditorStyles.wordWrappedLabel);
        if(GUILayout.Button("Stop 3D visual preview"))SkyPulseBirdRosterPreview.Select("",false);
    }
}
