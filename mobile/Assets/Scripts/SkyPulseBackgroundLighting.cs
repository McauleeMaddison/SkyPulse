using UnityEngine;
namespace SkyPulse.Mobile
{
    // Paired with a baked district mesh. Each arrangement retains its own
    // beacon placement when an island is moved or resized in the workshop.
    public sealed class SkyPulseBackgroundLighting : ScriptableObject
    {
        public Vector4[] Points,Colours;
    }
}
