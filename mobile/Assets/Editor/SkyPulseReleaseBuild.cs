#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SkyPulse.Mobile.Editor
{
    public static class SkyPulseReleaseBuild
    {
        public const string BundleId = "com.mcauleemaddison.skypulse";
        public const string Scene = "Assets/Scenes/SkyPulse.unity";

        [Serializable]
        private sealed class BuildEvidence
        {
            public string version;
            public string build;
            public string bundleId;
            public string unityVersion;
            public string sdk;
            public string createdUtc;
            public string gameplaySourceSha256;
            public string iconSha256;
            public string publicLinksSha256;
        }

        private static string FileHash(string path)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        [MenuItem("SkyPulse/Release/Configure iPhone Release")]
        public static void Configure()
        {
            PlayerSettings.companyName = "SkyPulse";
            PlayerSettings.productName = "SkyPulse";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Branding/SkyPulseAppIcon.png");
            if (icon == null) throw new BuildFailedException("Missing SkyPulse app icon.");
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
            {
                var slots = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
                foreach (var slot in slots) slot.SetTexture(icon);
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, slots);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("SkyPulse iPhone release settings configured. Apple team selection and device validation remain required.");
        }

        [MenuItem("SkyPulse/Release/Export Xcode Project")]
        public static void Export()
        {
            ExportProject(false);
        }

        [MenuItem("SkyPulse/Release/Export Xcode Simulator Project")]
        public static void ExportSimulator()
        {
            ExportProject(true);
        }

        [MenuItem("SkyPulse/Release/Export App Store Candidate")]
        public static void ExportAppStore()
        {
            ValidateAppStore();
            ExportProject(false);
        }

        [MenuItem("SkyPulse/Release/Validate App Store Configuration")]
        public static void ValidateAppStore()
        {
            var links = SkyPulsePublicLinks.Load();
            if (!SkyPulsePublicLinks.IsPublicHttpsUrl(links.privacyUrl) ||
                !SkyPulsePublicLinks.IsPublicHttpsUrl(links.supportUrl))
                throw new BuildFailedException("Set real public HTTPS privacyUrl and supportUrl in Assets/Resources/SkyPulsePublicLinks.json. Verify both pages and their contact details before submission.");
            if (string.IsNullOrWhiteSpace(PlayerSettings.iOS.appleDeveloperTeamID))
                throw new BuildFailedException("Select your paid Apple Developer signing team in iOS Player Settings before exporting an App Store candidate.");
            if (!int.TryParse(PlayerSettings.iOS.buildNumber, out var number) || number <= 0)
                throw new BuildFailedException("Set a positive, unused iOS build number before export.");
            Debug.Log("SKYPULSE_STORE_CONFIGURATION_PASS: local configuration only; signed archive validation, live URLs, device testing and App Store Connect metadata still require verification.");
        }

        private static void ExportProject(bool simulator)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new BuildFailedException("Install iOS Build Support for this Unity version in Unity Hub, then restart Unity.");
            Configure();
            if (simulator) PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
            var destination = Environment.GetEnvironmentVariable("SKYPULSE_IOS_OUTPUT");
            if (string.IsNullOrEmpty(destination)) destination = (simulator ? "Builds/iOS-simulator-" : "Builds/iOS-beta-") + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            // A clean destination prevents stale native files entering a release.
            if (Directory.Exists(destination) && Directory.GetFileSystemEntries(destination).Length > 0)
                throw new BuildFailedException("Export folder is not empty. Choose a fresh SKYPULSE_IOS_OUTPUT folder or move the existing export first.");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene }, target = BuildTarget.iOS,
                locationPathName = destination, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"iOS export failed: {report.summary.result}, {report.summary.totalErrors} errors.");
            var evidence = new BuildEvidence
            {
                version = PlayerSettings.bundleVersion,
                build = PlayerSettings.iOS.buildNumber,
                bundleId = BundleId,
                unityVersion = Application.unityVersion,
                sdk = simulator ? "iphonesimulator" : "iphoneos",
                createdUtc = DateTime.UtcNow.ToString("O"),
                gameplaySourceSha256 = FileHash("Assets/Scripts/SkyPulseNativeGame.cs"),
                iconSha256 = FileHash("Assets/Branding/SkyPulseAppIcon.png"),
                publicLinksSha256 = FileHash("Assets/Resources/SkyPulsePublicLinks.json"),
            };
            File.WriteAllText(Path.Combine(destination, "SkyPulse-build-info.json"), JsonUtility.ToJson(evidence, true));
            Debug.Log($"SKYPULSE_IOS_EXPORT_PASS: {Path.GetFullPath(destination)}");
        }
    }
}
#endif
