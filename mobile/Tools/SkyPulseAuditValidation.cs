// Copy into Assets/Editor only in an isolated audit project. Does not enter Play mode.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class SkyPulseAuditValidation {
 public static void Run() {
  int missing=0;
  foreach(var scene in EditorBuildSettings.scenes) {
   Debug.Log("AUDIT_SCENE: "+scene.enabled+" "+scene.path);
   if(!scene.enabled) continue;
   var opened=EditorSceneManager.OpenScene(scene.path,OpenSceneMode.Single);
   foreach(var root in opened.GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true)) missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
   Debug.Log("AUDIT_DEPENDENCIES: "+string.Join(";",AssetDatabase.GetDependencies(scene.path,true)));
  }
  int resources=0;
  foreach(var path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!AssetDatabase.IsValidFolder(p))) {
   var obj=AssetDatabase.LoadMainAssetAtPath(path);
   if(obj==null) {Debug.LogError("AUDIT_MISSING_ASSET: "+path);missing++;}
   if(path.Contains("/Resources/")) resources++;
  }
  Debug.Log("AUDIT_RESOURCE_ASSETS: "+resources);
  Debug.Log("AUDIT_RUNTIME_SCRIPTS: "+string.Join(";",UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Player).SelectMany(a=>a.sourceFiles).Where(p=>p.StartsWith("Assets/"))));
  if(missing!=0) throw new Exception("Missing assets/scripts: "+missing);
  Debug.Log("SKYPULSE_43A_STATIC_UNITY_PASS");
 }
}
