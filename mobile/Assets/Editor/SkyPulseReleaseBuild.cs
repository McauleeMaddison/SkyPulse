#if UNITY_EDITOR
using System;
using System.IO;
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
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new BuildFailedException("Install iOS Build Support for this Unity version in Unity Hub, then restart Unity.");
            Configure();
            var destination = Environment.GetEnvironmentVariable("SKYPULSE_IOS_OUTPUT");
            if (string.IsNullOrEmpty(destination)) destination = "Builds/iOS";
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
            Debug.Log($"SKYPULSE_IOS_EXPORT_PASS: {Path.GetFullPath(destination)}");
        }
    }
}
#endif
