#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace SkyPulse.Mobile.Editor
{
    public static class SkyPulseIosPostprocess
    {
        [PostProcessBuild(1000)]
        public static void OnBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            var infoPath = Path.Combine(path, "Info.plist");
            var info = new PlistDocument();
            info.ReadFromFile(infoPath);
            info.root.SetString("CFBundleDisplayName", "SkyPulse");
            // The offline game implements no encryption or network transport.
            info.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            info.WriteToFile(infoPath);

            // Extend Unity's manifest, preserving its existing engine declarations.
            const string manifestName = "PrivacyInfo.xcprivacy";
            var manifestPath = Path.Combine(path, manifestName);
            var manifest = new PlistDocument();
            if (File.Exists(manifestPath)) manifest.ReadFromFile(manifestPath);
            manifest.root.SetBoolean("NSPrivacyTracking", false);
            if (!manifest.root.values.ContainsKey("NSPrivacyTrackingDomains")) manifest.root.CreateArray("NSPrivacyTrackingDomains");
            if (!manifest.root.values.ContainsKey("NSPrivacyCollectedDataTypes")) manifest.root.CreateArray("NSPrivacyCollectedDataTypes");
            var apis = manifest.root.values.TryGetValue("NSPrivacyAccessedAPITypes", out var existing)
                ? existing.AsArray() : manifest.root.CreateArray("NSPrivacyAccessedAPITypes");
            var found = false;
            foreach (var api in apis.values)
            {
                var entry = api.AsDict();
                if (entry.values.TryGetValue("NSPrivacyAccessedAPIType", out var type) && type.AsString() == "NSPrivacyAccessedAPICategoryUserDefaults")
                {
                    var reasons = entry.values.TryGetValue("NSPrivacyAccessedAPITypeReasons", out var values)
                        ? values.AsArray() : entry.CreateArray("NSPrivacyAccessedAPITypeReasons");
                    var declared = false;
                    foreach (var reason in reasons.values) if (reason.AsString() == "CA92.1") declared = true;
                    if (!declared) reasons.AddString("CA92.1");
                    found = true;
                }
            }
            if (!found)
            {
                var entry = apis.AddDict();
                entry.SetString("NSPrivacyAccessedAPIType", "NSPrivacyAccessedAPICategoryUserDefaults");
                entry.CreateArray("NSPrivacyAccessedAPITypeReasons").AddString("CA92.1");
            }
            manifest.WriteToFile(manifestPath);
            var projectPath = PBXProject.GetPBXProjectPath(path);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            var file = project.FindFileGuidByProjectPath(manifestName);
            if (string.IsNullOrEmpty(file)) file = project.AddFile(manifestName, manifestName);
            project.AddFileToBuild(project.GetUnityMainTargetGuid(), file);
            project.WriteToFile(projectPath);
        }
    }
}
#endif
