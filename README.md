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
- A free six-frame **Aetherwing Test** bird is the default playtest avatar. The sixteen distinct collectible skins remain intact; every collectible has its own transparent **hit** and **unlock** artwork, neon material/trail treatment, and reveal movement. There is no generic cartoon unlock bird.
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

### Artwork: Aetherwing 2D wing rig (the current approach)

The supplied mechanical Aetherwing drawings now run in-game as the **Aetherwing Test** bird: six sharp flap poses, then its own hit and unlock pose. This is the visible motion/timing test; it does not replace the completed neon skins.

You do **not** need to trace six full flap drawings now. The game already uses all six supplied flap poses for the playtest. The saved `aetherwing-flap1.kra` is a useful outline test: keep it, but stop tracing that full frame.

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

### Your first small drawing task

1. Press **⌘S**. This saves your current outline-test file. Do not delete it and do not keep drawing on it.
2. In Krita choose **File → New**. Set the canvas to **2048 × 1536 px**. Leave the background transparent.
3. Choose **File → Save As**. Save the new file as `aetherwing-rig-master.kra` in `mobile/ArtSource/Birds/Aetherwing/`.
4. Drag the tucked-wing **glide** photo (the sixth flap photo) from your Desktop onto the canvas. In the Layers panel, rename that photo layer `GUIDE-GLIDE`.
5. With `GUIDE-GLIDE` selected, set **Opacity** to **45%**, then click its padlock. It should look faded and you should not be able to draw on it.
6. Click the **+** at the bottom of Layers. Rename the new empty layer `BODY`. Make sure it is **above** `GUIDE-GLIDE`, selected, and at **100%** opacity.
7. Choose **Basic-5 Size Opacity**. Set the brush to **3 px**. Zoom to roughly **400–600%**; use the mouse wheel or trackpad to move around the bird rather than zooming to 1100%.
8. Trace only these fixed pieces on `BODY`: head, beak, eye, crown crystals, neck, chest, metal torso, legs and the round shoulder hinge. Trace a short line, stop, then start the next short line. Press **⌘Z** immediately if a line goes wrong.
9. Leave the wing feathers and tail completely blank. They will get their own layers later, so they can move without making the body wobble.
10. Press **⌘S**, hide the eye beside `GUIDE-GLIDE`, and inspect your black outline on its own. If the silhouette looks clean at normal zoom, send a screenshot before starting `FAR-WING`.

The first time you work on a layer, only make the clean outline. Do not colour it, add glow, or trace a second flap photo yet. We will use the same one clean body and separate moving wing pieces to make the animation smooth.

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
