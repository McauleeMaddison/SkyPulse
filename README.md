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
    │   └── Aetherwing/aetherwing-master.kra
    ├── Packages/
    └── ProjectSettings/
```

`assets/` and `web/` remain because the live browser beta still uses them. They are not Unity clutter. Unity-generated folders (`Library`, `Logs`, `Temp`, build outputs, solution files, and per-machine VS Code state) are ignored and never committed.

## What is in the native game now

- C# gameplay with touch-first input and a fixed, stable flight simulation.
- Sixteen distinct bird skins. Every skin has its own transparent **hit** and **unlock** artwork, its own neon material/trail treatment, and its own reveal movement. There is no generic cartoon unlock bird.
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

### Start here: Aetherwing in Krita

1. Open Krita.
2. Choose **File → Open**.
3. Open `mobile/ArtSource/Birds/Aetherwing/aetherwing-master.kra`.
4. Do **not** paint over or delete the original master. Choose **File → Save As** and make a working copy first.
5. Keep the canvas at **2048 × 1536 px**, transparent, with the bird facing right. Keep every new frame on this same camera/view so it does not jump in-game.
6. Keep these layers separate while you paint: `farWing`, `tail`, `body`, `nearUpperWing`, `nearLowerWing`, `nearPrimaryFeathers`, `head`, `beak`, `eye`, `rimLight`.
7. Paint the dark body shape first. Then paint the wings, head, beak and eye. Add the neon edge light last. If it looks good only because of a huge glow, simplify it.
8. Zoom out until the bird is about 250 px wide. You should still instantly see the head, body, wings and tail. If you cannot, fix the silhouette before exporting.

### The exact drawings to make for one bird

Do one bird completely before starting the next. A fully hand-authored skin needs these five transparent poses:

1. **Glide** — wings tucked/relaxed; the normal flying pose.
2. **Downstroke** — wings low and powerful; the flap’s push.
3. **Lift** — wings high and open; the flap’s recovery.
4. **Hit** — compact wings, startled/impacted posture; brief only.
5. **Unlock** — proud, open high-wing reveal; it must look like *that exact bird* has just been bought.

For an unlock pose, keep the body, head, materials, colour and individual wing design of the actual bird. Change the pose and reveal energy only. Never substitute a universal bird or a front-facing cartoon character.

The current collection is: `nova`, `lumen`, `ember`, `sol`, `aurora`, `orchid`, `coral`, `glacier`, `prism`, `verdant`, `cinder`, `tide`, `wisp`, `bloom`, `emberwing`, and `steel`. Each needs its own art; none should borrow another bird’s image.

### Export and hand back safely

1. Save the editable work as a `.kra` source file in `mobile/ArtSource/Birds/<bird-id>/`.
2. Export each finished pose as a **PNG with transparency**. Use the same canvas and placement for all five poses.
3. Name the source exports clearly, for example `nova-glide.png`, `nova-downstroke.png`, `nova-lift.png`, `nova-hit.png`, `nova-unlock.png`.
4. Do not manually place new files into Unity’s `Resources` folder and do not overwrite a live bird at random.
5. Send the finished source/PNGs back here. I will check the silhouette, optimise the import settings, place the assets in the right runtime folder, wire that bird’s exact animation/reveal, and run the Unity check.

If you want to use your original eight-frame drawing plan, make the five poses above first. Keep the three extra wing-beat drawings in the source folder until we wire an eight-frame renderer; the live game currently uses the five pose roles above.

## Sensible next move

My next move, if this were my game, would be to **stop adding systems** and prove the flight loop on a real phone. Keep a short note of unfair deaths, missed crystal pickups, unclear pipe caps and frame-rate hitches. Fix only repeated evidence, not one unlucky run. Once that is solid, finish one truly hand-authored bird pose set using the workflow above, then repeat that quality bar for the remaining birds.

That is how the game becomes sharp and release-ready rather than bloated: one native C# game, one visual direction, one art hand-off, and only the assets that are actually live.
