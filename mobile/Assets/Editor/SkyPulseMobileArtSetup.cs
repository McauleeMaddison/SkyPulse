#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SkyPulse.Mobile.Editor
{
    /// <summary>
    /// Applies the import budget used by SkyPulse's native release build. Run this after
    /// adding art via the Unity menu; it keeps detail where a player can see it while
    /// preventing large transparent sprites from exhausting older phones.
    /// </summary>
    public static class SkyPulseMobileArtSetup
    {
        private const string ArtRoot = "Assets/Resources/SkyPulse";

        [MenuItem("SkyPulse/Optimise Mobile Art")]
        public static void OptimiseMobileArt()
        {
            var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot });
            var updated = 0;

            foreach (var guid in textureGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                var maxSize = MaximumSizeFor(path);
                // SkyPulse turns its source textures into purpose-sized sprites at
                // runtime, so preserve Texture2D import compatibility here.
                var changed = importer.textureType != TextureImporterType.Default
                    || importer.mipmapEnabled
                    || importer.isReadable
                    || !importer.alphaIsTransparency
                    || importer.maxTextureSize != maxSize
                    || importer.wrapMode != TextureWrapMode.Clamp
                    || importer.filterMode != FilterMode.Bilinear
                    || importer.textureCompression != TextureImporterCompression.Compressed;

                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = maxSize;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Compressed;

                changed |= ApplyPlatformCompression(importer, "Android", maxSize);
                changed |= ApplyPlatformCompression(importer, "iPhone", maxSize);
                if (!changed) continue;

                importer.SaveAndReimport();
                updated += 1;
            }

            Debug.Log($"SkyPulse mobile-art budget applied to {updated} texture(s).");
        }

        private static int MaximumSizeFor(string path)
        {
            // Backgrounds carry the world's atmosphere and are already only one
            // portrait texture each. Preserve their source resolution, while ASTC
            // compression keeps the mobile memory cost predictable.
            if (path.Contains("/backgrounds/")) return 2048;
            if (path.Contains("/characters/")) return 1024;
            if (path.Contains("/powerups/") || path.Contains("/art/")) return 512;
            return 512;
        }

        private static bool ApplyPlatformCompression(TextureImporter importer, string platform, int maxSize)
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            var changed = !settings.overridden
                || settings.maxTextureSize != maxSize
                || settings.format != TextureImporterFormat.ASTC_6x6;

            settings.name = platform;
            settings.overridden = true;
            settings.maxTextureSize = maxSize;
            settings.format = TextureImporterFormat.ASTC_6x6;
            settings.textureCompression = TextureImporterCompression.Compressed;
            settings.compressionQuality = 50;
            importer.SetPlatformTextureSettings(settings);
            return changed;
        }
    }
}
#endif
