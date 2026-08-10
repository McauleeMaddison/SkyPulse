# Runtime bird art

This is the Unity-imported home for rigged bird artwork. Keep editable PSD source files in `mobile/ArtSource/Birds/`, not here.

When Aetherwing source art is complete:

1. Install **2D Animation** and **2D PSD Importer** in Unity's Package Manager.
2. Copy `mobile/ArtSource/Birds/Aetherwing/aetherwing-rig-source.psd` into `Assets/Art/Birds/Aetherwing/`.
3. Import it as layered sprites, then use **Sprite Editor → Skinning Editor** to build the shared SkyPulse bone hierarchy.
4. Save the resulting rig prefab in this folder as `AetherwingRig.prefab`.

Do not place a flattened full-bird PNG here as the animated runtime asset. The rig must preserve individual body and wing layers.
