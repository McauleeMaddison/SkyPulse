# Background artwork — first visual update

Generated with the built-in image generation tool, using the user's gameplay image as a style reference. No API key or CLI image-generation fallback was used. Original generated PNGs are preserved; project assets are copies, not overwrites of the launch artwork.

## Project assets

- `Assets/Resources/SkyPulse/backgrounds/neon-flightdeck-v2.png` — Neon City. Source output `exec-c23d7e20-25db-4f4c-91b3-5e026f7c2c1b.png`.
- `Assets/Resources/SkyPulse/backgrounds/themes/aurora-rise-v3.png` — Aurora Rise. Recovered completed output `exec-e114bd3f-2c06-439c-addc-24b848aaa139.png`.
- `Assets/Resources/SkyPulse/backgrounds/themes/solar-drift-v3.png` — Solar Drift. Recovered completed output `exec-8c1052b6-94a9-499b-8257-4ca98d675ce8.png`.

Reference: `references/gameplay-target.png`. The other two supplied images are saved alongside it as style references for later interface work. Mockup text, extra currencies and proposed gameplay upgrades have not been treated as game requirements.

Imports preserve the source aspect ratio, disable mipmaps/readability, and use the existing 2048-pixel maximum with ASTC 6×6 on iPhone/Android. The three route images are warmed before flight through the existing sprite cache. A lighter world colour veil preserves authored contrast; the existing drifting backdrop, independent stars and matched world dissolve continue to provide motion.

## Final Neon City prompt

Use case: stylized-concept. Production Unity mobile game BACKGROUND texture, portrait 1024x2048, original environment using supplied image ONLY as style and atmosphere reference. Match its polished illustrated 2.5D cyberpunk space-city visual quality. BACKGROUND ONLY: no birds, pipes, gems, gameplay objects, user interface, lettering, logos or frame. World NEON CITY: deep cobalt/midnight-blue starfield with dramatic fuchsia/violet nebula clouds on far left/right margins, a tiny purple galaxy high near left edge, bright coral-pink sunrise far down near bottom horizon, floating futuristic city platforms at bottom quarter with crisp electric cyan windows, slender dark navy towers, deeply layered cloud banks. Maintain a broad open dark blue centre for gameplay: middle 65% of width and middle 65% of height are sparse small distant stars and subtle deep nebula, very few bright points; high contrast art lives at outer edges and bottom. Sharp cloud rim light, convincing atmospheric depth in city platforms, rich saturation cyan and magenta, cohesive sci-fi architecture. No road/floor across bottom. Full-bleed opaque image. Do not paint any part of the reference game foreground.

## Aurora Rise / Solar Drift prompt specification

Production Unity mobile game background, portrait 1024×2048 requested. Supplied gameplay image was a style/environment reference only. Original background-only scene: no bird, pipes, crystals, gameplay objects, UI, words, logos, watermark or border. Polished illustrated 2.5D science-fiction space, luminous clouds and floating futuristic city platforms; keep the central 65% of width and middle 65% of height dark and open for flight, with small restrained stars. Place bright clouds on the outer edges and below the bottom quarter, and the cohesive skyline in the bottom 28%. Strong atmospheric perspective, detailed cloud contours, sharp cyan-lit towers, opaque full bleed.

Aurora Rise: mint/teal and cyan aurora ribbons on high outer edges, purple-blue centre, violet/turquoise clouds, cyan city lights, hints of magenta, fresh cool depth rather than green haze.

Solar Drift: amber/coral and magenta cloud rims on edges/bottom, cobalt/indigo centre, distant solar halo cropped at upper right, cyan windows at lower corners, warm solar dusk surrounding clear cold space. Avoid a huge central sun.
