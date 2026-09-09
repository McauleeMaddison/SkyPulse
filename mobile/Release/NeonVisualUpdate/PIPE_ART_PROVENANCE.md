# Pipe artwork provenance

Assets:

- `Assets/Resources/SkyPulse/art/pipes/pipe-shaft-neon-v1.png`
- `Assets/Resources/SkyPulse/art/pipes/pipe-collar-neon-v1.png`

AI-generated assets recovered from this visual-update session. The exact generation prompts are not retained in this note. Their visual direction follows the user's gameplay reference: reflective graphite mechanical panels, a cyan illuminated edge, magenta power channels and a separate open neon collar. Gameplay uses these local textures and no AI service.

The original RGBA PNGs are copied unchanged. The shaft source is 1024 × 1536; the collar source is 1934 × 813. Read-only alpha inspection identified the visible shaft at source rectangle (266, 0, 493, 1536) and collar at (107, 114, 1715, 608), using top-left image coordinates. These bounds exclude the broad transparent presentation canvas and negligible stray pixels below 4% opacity. The runtime creates sprites with equivalent normalized UV rectangles; it does not process or duplicate the texture pixels. This keeps the visible silhouette aligned with the existing obstacle width and cap boundary.

Unity imports both textures at maximum 1024 pixels with ASTC 6×6 on iOS and Android, bilinear filtering, alpha transparency, no mipmaps, no power-of-two distortion and Read/Write disabled. The slightly larger budget than a pickup sprite preserves panel detail on the tall shaft. Cap and shaft art share a consistent cyan edge; only the vertical axis flips for top gates.

The original `PipeCap` resource is still measured before loading the replacement art, preserving the exact shipped cap collision height. Pipe widths, body/cap colliders, gap heights, route movement, spacing and scoring remain unchanged. Soft light layers reuse the existing renderer pool; the decorative breathing rim and power streak freeze under Reduced Motion. The original art path remains the fallback if either new texture is missing.
