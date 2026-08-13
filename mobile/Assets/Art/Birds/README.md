# Runtime bird art

This is the Unity-imported home for rigged bird artwork. Keep editable PSD source files in `mobile/ArtSource/Birds/`, not here.

When Aetherwing source art is complete:

1. Install **2D Animation** and **2D PSD Importer** in Unity's Package Manager.
2. In Krita, open `mobile/ArtSource/Birds/Aetherwing/aetherwing-master.kra` and export a layered copy named `aetherwing-rig-source.psd`.
3. Copy that exported PSD into `Assets/Art/Birds/Aetherwing/`.
4. Import it as layered sprites, then use **Sprite Editor → Skinning Editor** to build the shared SkyPulse bone hierarchy.
5. Save the resulting rig prefab in this folder as `AetherwingRig.prefab`.

Do not place a flattened full-bird PNG here as the animated runtime asset. The rig must preserve individual body and wing layers.
