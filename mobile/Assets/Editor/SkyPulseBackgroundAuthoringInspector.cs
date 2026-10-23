using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkyPulseBackgroundAuthoring))]
public sealed class SkyPulseBackgroundAuthoringInspector : Editor
{
    public override void OnInspectorGUI()
    {
        var root=(SkyPulseBackgroundAuthoring)target;
        EditorGUILayout.HelpBox("Select a child structure in the Hierarchy. Move, rotate or resize it with Unity's normal tools. Save the scene, then bake to apply these edits to gameplay. Keep structures under this root.",MessageType.Info);
        if(GUILayout.Button("Bake this background into gameplay",GUILayout.Height(34)))SkyPulseBackgroundWorkshop.Bake(root);
        EditorGUILayout.HelpBox("This edits scenery only. Flight width, pipes, gates, collision rules and progress are unchanged. Generated authoring scenes stay out of the app build.",MessageType.None);
    }
}
