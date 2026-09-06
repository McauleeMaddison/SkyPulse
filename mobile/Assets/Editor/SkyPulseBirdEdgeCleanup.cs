#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SkyPulse.Mobile.Editor
{
    // Some source-sheet cells contain tiny fragments from an adjacent bird pose.
    // Remove only detached fragments near the canvas edge, never the main bird.
    // Source PNGs, canvas dimensions, pivot registration and PPU remain unchanged.
    public sealed class SkyPulseBirdEdgeCleanup : AssetPostprocessor
    {
        public override uint GetVersion() => 1;
        private void OnPostprocessTexture(Texture2D texture)
        {
            if (!assetPath.StartsWith("Assets/Resources/SkyPulse/characters/roster/")) return;
            var pixels = texture.GetPixels32();
            var width = texture.width;
            var height = texture.height;
            var labels = new int[pixels.Length];
            var components = new List<List<int>>();
            var largest = 0;
            for (var start = 0; start < pixels.Length; start++)
            {
                if (pixels[start].a <= 8 || labels[start] != 0) continue;
                var component = new List<int>();
                components.Add(component);
                var label = components.Count;
                component.Add(start);
                labels[start] = label;
                for (var cursor = 0; cursor < component.Count; cursor++)
                {
                    var index = component[cursor];
                    var x = index % width;
                    var y = index / width;
                    for (var ny = Mathf.Max(0, y - 1); ny <= Mathf.Min(height - 1, y + 1); ny++)
                    for (var nx = Mathf.Max(0, x - 1); nx <= Mathf.Min(width - 1, x + 1); nx++)
                    {
                        var next = ny * width + nx;
                        if (pixels[next].a <= 8 || labels[next] != 0) continue;
                        labels[next] = label;
                        component.Add(next);
                    }
                }
                largest = Mathf.Max(largest, component.Count);
            }
            var removed = 0;
            var margin = Mathf.Max(2, Mathf.Min(width, height) / 25);
            foreach (var component in components)
            {
                if (component.Count >= largest || component.Count > largest * .02f) continue;
                var nearEdge = false;
                foreach (var index in component)
                {
                    var x = index % width;
                    var y = index / width;
                    if (x < margin || x >= width - margin || y < margin || y >= height - margin) { nearEdge = true; break; }
                }
                if (!nearEdge) continue;
                foreach (var index in component)
                {
                    pixels[index].a = 0;
                    var x = index % width;
                    var y = index / width;
                    for (var ny = Mathf.Max(0, y - 1); ny <= Mathf.Min(height - 1, y + 1); ny++)
                    for (var nx = Mathf.Max(0, x - 1); nx <= Mathf.Min(width - 1, x + 1); nx++)
                    {
                        var adjacent = ny * width + nx;
                        if (labels[adjacent] == 0) pixels[adjacent].a = 0;
                    }
                }
                removed += component.Count;
            }
            if (removed == 0) return;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            Debug.Log($"SkyPulse cleaned {removed} detached edge pixels from {assetPath}");
        }
    }
}
#endif
