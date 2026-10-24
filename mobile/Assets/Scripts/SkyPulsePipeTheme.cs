using UnityEngine;
namespace SkyPulse.Mobile
{
    // Optional workshop output. One barrel/collar pair is shared by every
    // pooled gate in this world; physics always uses the existing colliders.
    public sealed class SkyPulsePipeTheme : ScriptableObject
    {
        public Mesh Barrel,Collar;
        public Color Accent,Energy,Metal;
        [Range(.4f,1.6f)] public float LightRate=1;
        [Range(0,1.5f)] public float Detail=1;
    }
}
