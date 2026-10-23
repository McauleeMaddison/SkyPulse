using UnityEngine;

namespace SkyPulse.Mobile
{
    // Optional output of the Unity Background Workshop. Original animation and
    // pooling remain in RelayEnvironment; these are authored structural edits.
    public sealed class SkyPulseRelayBackgroundEdits : ScriptableObject
    {
        [System.Serializable]
        public sealed class Part
        {
            public string Name;
            public Mesh Mesh;
            public Vector3 PositionOffset,ScaleMultiplier=Vector3.one;
            public Quaternion RotationOffset=Quaternion.identity;
            public bool AdditionalStructure,Hidden;
        }
        public Part[] Parts;
    }
}
