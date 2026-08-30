# SkyPulse

SkyPulse is a polished, touch-first **neon-noir flight game**. The native Unity game is the source of truth: it is the version we build, tune, test, and ship.

## The technical decision

The game stays in **Unity 6 + C#**. That is the right language and engine for a fast mobile game with touch input, animation, particle effects, sound, Android/iPhone builds, and a single shared codebase.

The old Python/Kivy desktop reference is intentionally removed. Python is excellent for tools, but it is not a better fit for this real-time mobile game. We are not moving the game to a different language and rebuilding a working C# game for no benefit.

## Clean project map

```text
SkyPulse/
├── README.md                         # This guide: setup, testing and art hand-off
├── .vscode/                          # Small shared VS Code settings only
├── assets/                           # Live browser-beta images and sound only
├── web/                              # Browser friend beta; kept separate from Unity
└── mobile/                           # Native game
    ├── Assets/
    │   ├── Scenes/SkyPulse.unity      # Open this scene to play
    │   ├── Scripts/SkyPulseNativeGame.cs
    │   ├── Editor/                    # Editor-only play-test tools
    │   └── Resources/SkyPulse/        # Runtime birds, worlds, pipe and pickup art
    ├── ArtSource/Birds/               # Editable source art, never shipped in a build
    │   └── Aetherwing/                # Your saved Krita work and rig master live here
    ├── Packages/
    └── ProjectSettings/
```

`assets/` and `web/` remain because the live browser beta still uses them. They are not Unity clutter. Unity-generated folders (`Library`, `Logs`, `Temp`, build outputs, solution files, and per-machine VS Code state) are ignored and never committed.

## What is in the native game now

- C# gameplay with touch-first input and a fixed, stable flight simulation.
- A portrait-first launch collection of five distinct robotic-cartoon birds: **Volt** is the starter, with **Prism**, **Verdant**, **Cinder** and **Steel** earned with crystals. Every bird runs a six-step flap timeline and owns a separate **hit** frame and **unlock** frame—eight animation states per bird.
- Customisable world, background, trail and pipe looks. Their lighting is neon-noir: dark metal, vivid core lights and clear silhouettes.
- Gates with properly matched body and cap collision bounds, animated pipe channels and fair upper/lower gap scaling.
- Crystals appear randomly during a run; clearing every gate does **not** automatically give a crystal.
- Adventure-only power-ups: Slow Field, Pulse Shield, Crystal Cache, Sky Surge, Score Prism, Magnet Halo and Phase Shift. Air Brakes are removed.
- Fourteen persistent Flight Tech upgrades. Classic and Daily routes stay fair by keeping upgrades out of their competitive flight rules.

## Open and play the Unity game

1. Open **Unity Hub**.
2. Choose **Add**, then select the `mobile` folder in this project.
3. Open `Assets/Scenes/SkyPulse.unity`.
4. Press the triangular **Play** button at the top of Unity.
5. Tap/click to flap. `F1`, `F2` and `F3` switch the flight modes. `F4` shows the pink pipe-body and amber cap collision guides.
6. For the quick formal check, use Unity’s menu: **SkyPulse → Playtest Checklist**.

Play at least ten Classic runs and five Adventure runs before changing tuning. Record a death only when it feels visibly unfair, then use `F4` to check the collision shape before changing any numbers.

## Artwork: your simple job, step by step

This is the only artwork workflow to use. It keeps the game’s birds consistent and prevents the old cartoon-looking, mismatched work from returning.

### The look to keep

- Birds face **right**, use the established side view, and remain centred on a transparent canvas.
- They are **futuristic neon-noir**: dark mechanical/crystal surfaces, controlled colour glow, sharp readable feathers or armour, and a subtle energy trail.
- Each bird keeps its own colours, feather shapes, metal details and wing proportions. Do **not** turn every bird into the same mascot.
- No white rectangle, black background, UI text, crystals, pipes, baked glow-card or drop shadow in the exported bird image.

### Bird animation contract

The launch collection is deliberately compact: one starter plus four meaningful unlocks. Every current or future bird must keep this exact eight-state contract:

1. Six wing-flap positions, ordered from raised wing through downstroke. The runtime moves through all six on every tap with no translucent cross-fade.
2. One bespoke hit pose, shown for a short impact beat before the result card.
3. One bespoke unlock pose, used in the collection reveal.

Add a future bird by putting its transparent artwork in `Assets/Resources/SkyPulse/characters/`, adding one `Skin` entry in `SkyPulseNativeGame.cs`, and providing all six `FlapFramePaths` plus unique hit and unlock paths. The game validates that there are six flight frames and that hit/unlock art is never shared between roster birds. Keep every frame right-facing and registered on the same canvas so it stays stable on a phone.

### Artwork: Aetherwing 2D wing rig (legacy source reference)

The saved Aetherwing files are retained as a legacy art reference only; they are not in the launch collection. The shipped roster uses the five dedicated robotic-cartoon eight-frame sets described above.

You do **not** need to trace six full flap drawings now. The saved `aetherwing-flap1.kra` is a useful legacy outline reference; keep it, but do not use it for the production roster.

Unity now has a safe rig foundation. It keeps the present full-body Aetherwing animation until all six pieces below exist, so unfinished art can never break the game. When the complete set arrives, Unity keeps the body crisp and rotates/moves only the wing and tail pieces smoothly at 60 FPS.

Make one Krita source file called `aetherwing-rig-master.kra`. It stays at **2048 × 1536 px**, transparent, facing right. Every exported piece must remain on that exact same canvas, in that exact same position. Never crop a piece and never add a white/black background.

Create these layers, exactly named:

1. `GUIDE-GLIDE` — your faded, locked reference photo; it is never exported.
2. `BODY` — head, beak, eye, chest, torso, legs and fixed shoulder mechanism. **No wing and no tail.**
3. `FAR-WING` — the wing behind the body.
4. `UPPER-WING` — shoulder to elbow armour/feathers.
5. `LOWER-WING` — elbow to outer-wing armour/feathers.
6. `FEATHER-FAN` — the long primary feather tips.
7. `TAIL` — the crystal/metal tail only.

Do one layer at a time. The body must be complete and coloured before starting a wing. Paint the dark mechanical/crystal base first; add the controlled neon edge light last. Keep the Aetherwing’s own crown, metal joints, crystal shapes and proportions—do not simplify it into a mascot.

### Current Aetherwing workboard: no retracing

Your one finished full outline is saved in `aetherwing-rig-master.kra`. It is the master backup; do not paint over or delete it.

`aetherwing-rig-split.ora` is the working file prepared from that outline. It has separate transparent layers for `BODY`, `FAR-WING`, `UPPER-WING`, `LOWER-WING`, `FEATHER-FAN`, and `TAIL`, all on the same 2048 × 1536 canvas. It is a clean technical hand-off: **do not trace the bird again**.

1. In Krita choose **File → Open** and open `aetherwing-rig-split.ora` from `mobile/ArtSource/Birds/Aetherwing/`.
2. Immediately choose **File → Save As** and save your colouring copy as `aetherwing-rig-colour.kra` in the same folder.
3. Leave `MASTER-OUTLINE` at the bottom. It is your safety reference and is never exported.
4. Start with `BODY-COLOUR`, which is already directly under `BODY`. Keep `BODY` untouched; paint the dark mechanical base colour on `BODY-COLOUR`.
5. Do not colour every moving part at once. Finish and screenshot `BODY-COLOUR`, then colour the wing layers one at a time using their matching `…-COLOUR` layers.

The line work is a guide only, not a game-ready export. We do not put it in Unity until its metallic dark surfaces, individual crystals and controlled neon edge lights are painted.

### Finished rig exports

When all six drawing layers are finished, hide `GUIDE-GLIDE` and export each art layer separately as a transparent PNG. The precise names are:

```text
aetherwing-body-v1.png
aetherwing-far-wing-v1.png
aetherwing-upper-wing-v1.png
aetherwing-lower-wing-v1.png
aetherwing-feather-fan-v1.png
aetherwing-tail-v1.png
```

Keep the hit and unlock drawings separate full-bird artwork. A later bird uses the same layer structure but its own colours, feathers, crystals and metalwork—never a universal bird image.

## Sensible next move

My next move, if this were my game, would be to **stop adding systems** and prove the flight loop on a real phone. Keep a short note of unfair deaths, missed crystal pickups, unclear pipe caps and frame-rate hitches. Fix only repeated evidence, not one unlucky run. Once that is solid, finish one truly hand-authored bird pose set using the workflow above, then repeat that quality bar for the remaining birds.

That is how the game becomes sharp and release-ready rather than bloated: one native C# game, one visual direction, one art hand-off, and only the assets that are actually live.
