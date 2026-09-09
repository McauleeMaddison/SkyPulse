# Crystal artwork provenance

Asset: `Assets/Resources/SkyPulse/art/powerups/generated/crystal-prism-neon-v4.png`

AI-generated artwork recovered from this visual-update session. The exact generation prompt is not retained in this note. The transparent cyan/violet faceted gem follows the gameplay visual direction supplied by the user. It is rendered as a local texture; gameplay uses no AI service.

The original transparent PNG is retained. Unity imports it at a maximum of 512 pixels on iOS and Android with ASTC 6×6 compression, bilinear filtering and no mipmaps, matching the existing pickup texture settings. The same sprite is used by gameplay pickups and currency balance chips.

Visual changes do not change crystal value, spawn probability, spacing, collection radius, magnet strengths or stored progression. Reduced Motion disables decorative gem movement and uses a short stationary pickup ring instead of traveling sparkles. Collection feedback uses a fixed six-slot pool.
