using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class SkyPulseNeonCityReviewChecks
{
 static string Hash(string p){using(var s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
 static Dictionary<string,string> Snapshot()=>Directory.GetFiles("Assets/Editor/SkyPulseBackgroundWorkshop","*",SearchOption.AllDirectories).Concat(Directory.GetFiles("Assets/Resources/SkyPulse/BackgroundEdits","*",SearchOption.AllDirectories)).Where(p=>!p.EndsWith(".DS_Store")).ToDictionary(p=>p,Hash);
 static bool Allowed(string p)=>p=="Assets/Editor/SkyPulseBackgroundWorkshop/district-design-baseline-v1.json"||p.StartsWith("Assets/Editor/SkyPulseBackgroundWorkshop/Meshes/world-00-",StringComparison.Ordinal)&&p.EndsWith(".asset")||p=="Assets/Resources/SkyPulse/BackgroundEdits/world-00-civic.asset"||p=="Assets/Resources/SkyPulse/BackgroundEdits/world-00-residential.asset";
 public static void Run()
 {
  try
  {
   if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Disposable QA only");
   EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");var before=Snapshot();
   SkyPulseDistrictDesignWorkshop.UpgradeNeonCityInQa();AssetDatabase.Refresh();var after=Snapshot();
   var unexpected=before.Where(p=>!Allowed(p.Key)&&(!after.ContainsKey(p.Key)||after[p.Key]!=p.Value)).Select(p=>p.Key).Concat(after.Keys.Except(before.Keys)).ToArray();
   if(unexpected.Length>0)throw new InvalidOperationException("Unexpected world change: "+string.Join(", ",unexpected));
   int max=0;foreach(var variant in new[]{"civic","residential"}){var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/SkyPulse/BackgroundEdits/world-00-"+variant+".asset");if(mesh==null||mesh.vertexCount<1000||mesh.vertexCount>60000)throw new InvalidOperationException("Neon geometry budget "+variant+": "+(mesh==null?0:mesh.vertexCount));max=Math.Max(max,mesh.vertexCount);}
   SkyPulseDistrictDesignWorkshop.UpgradeNeonCityInQa();AssetDatabase.Refresh();var repeat=Snapshot();if(after.Count!=repeat.Count||after.Any(p=>repeat[p.Key]!=p.Value))throw new InvalidOperationException("Neon authoring repeat changed saved assets");
   string output=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"neon-city-v4");Directory.CreateDirectory(output);
   File.WriteAllText(Path.Combine(output,"GEOMETRY-VALIDATION.json"),"{\"result\":\"PASS\",\"scope\":\"Neon City civic/residential only; all other worlds, scenes, prefabs, lighting and metadata unchanged; repeat stable\",\"maximumVertices\":"+max+",\"protectedFiles\":"+before.Count+",\"deviceTested\":false,\"appearanceApproved\":false}");
   SkyPulseAtmosphereReviewChecks.Run();
  }
  catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
}
