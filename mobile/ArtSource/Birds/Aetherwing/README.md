# Aetherwing source art — Krita to Unity

This folder holds the editable production artwork for Aetherwing. It is deliberately **outside** `Assets/`, so Unity does not include source PSD files in the game build.

## First deliverable

Create and save this file here:

```text
aetherwing-rig-source.psd
```

Use a transparent 2048 x 1536 px canvas. The bird faces right and sits centred on the canvas. Keep the same canvas dimensions for every future bird skin.

## Required Krita layer names

Use these names exactly. Do not merge them before saving the PSD.

```text
farWing
tail
body
nearUpperWing
nearLowerWing
nearPrimaryFeathers
head
beak
eye
rimLight
```

The `body` must overlap every wing at its shoulder by at least 40 px. This prevents transparent cracks while the rig bends the wing.

## Painting rules

- Paint the full form of each part that sits behind another part; do not crop it tightly to what is visible in the resting pose.
- Use solid, readable cobalt forms first. Add cyan rim light only after the silhouette works on a dark background.
- Keep the wing in three sections: upper arm, forewing, and primary-feather fan. Those pieces are what create a curved, living flap.
- Keep beak and eye independent so they remain crisp while the body moves.
- Do not include a rectangular background, glow card, or baked drop shadow.

## Export for Unity

When the source PSD is ready, export a flattened check image next to it:

```text
aetherwing-rig-preview.png
```

The PSD is the rigging input. The PNG is only for checking the artwork in Finder/Krita; do not use it as the animated bird.

## Rig contract

Every SkyPulse bird will use the same bone hierarchy:

```text
root
└── body
    ├── nearShoulder → nearElbow → nearWrist → nearTip
    ├── farShoulder  → farElbow  → farWrist
    └── tailBase     → tailTip
```

This shared anatomy lets one high-quality Glide / Flap / Recover animator work for every bird theme. A skin may change its colour, visor, feathers, trail and small accessories, but it must not change the structure of the body or wings.

## Visual acceptance check

Before importing, zoom the Krita canvas out until the bird is roughly 250 px wide. At that size it must still have a clear head, body, wing shape and tail. If it reads only because of glow or tiny detail, simplify it first.
