# SkyPulse bird reward poses

The 16 installed `*-unlock-v2.png` files are the real game birds in their own
high-wing flight poses. They deliberately use the established source sprites:
their exact silhouette, feather or armour structure, materials, colours, and
neon trail are preserved. The prior generated unlock illustrations have been
removed from the runtime folder and retained only in
`mobile/ArtSource/Birds/Archive/RejectedUnlockConcepts/`.

Each bird has an individual unlock file and its own reveal motion profile in
`SkyPulseNativeGame`; the runtime never swaps in a generic cartoon bird.

## Naming

```text
{bird-id}-unlock-v2.png  # Frame 8: purchase / unlock reveal (high-wing pose)
{bird-id}-hit-v2.png     # Frame 7: impact / compact-wing pose
```

Examples:

```text
nova-unlock-v2.png
emberwing-unlock-v2.png
nova-hit-v2.png
```

Both poses are installed now, each from the corresponding real bird asset:
the unlock sprite is its open/high-wing pose and the hit sprite is its compact
wing pose. These are separate transparent PNGs so later bespoke drawings can
replace a single bird without touching the rest of the system.

## Drawing rules for future exports

- Start from the existing side-on bird, not a front-facing character.
- Keep a generous transparent border around every wing tip and tail sparkle.
- Keep the background transparent; do not paint UI text, crystals, or a dark
  scene behind the bird—the game provides those.
- Make an unlock pose feel open and proud: a high near wing, a small visible far
  wing, and an upturned head. A hit pose should instead be compact and startled.
- Export PNG. Unity limits bird textures for mobile at import time, so source art
  can be larger, but the in-game silhouette is what matters most.
