# SkyPulse

SkyPulse is a touch-first neon flight game with three clearly separated surfaces:

- `mobile/`: the new **Unity 6 + C#** native, cross-platform mobile game;
- `web/`: the live touch-first PWA friend beta, for immediate iPhone and Android testing;
- the old Python/Kivy desktop game, frozen as a private visual and gameplay reference.

## The whole project

```text
SkyPulse/
├── main.py           # The game: controls, screens, movement and drawing
├── settings.py       # Easy values to change: speed, gravity, colours
├── requirements.txt  # Kivy, the game's only dependency
├── assets/
│   ├── audio/                         # Original low-latency gameplay effects
│   └── images/
│       ├── backgrounds/neon-city.png  # Cinematic neon city sky
│       ├── branding/                  # App icon and portrait launch artwork
│       └── characters/                # Eight unlockable SkyPulse birds and colourways
│           ├── nova.png
│           ├── lumen.png
│           ├── ember.png
│           └── sol.png
├── web/              # iPhone-friendly Safari/PWA test build
│   ├── index.html
│   ├── game.js
│   └── styles.css
├── mobile/            # Unity 6 + C# native mobile project
│   ├── Assets/
│   ├── Packages/
│   └── ProjectSettings/
└── README.md         # This guide
```

Nova, Lumen, Ember, Sol, their colourways, and the neon city are original game assets. The animated star layer, energy towers, crystals, flight trail, trail particles, runway scan, menus, sound effects, and glow effects are drawn in Python, keeping the project compact and easy to understand.

## Run on this Mac

Open the SkyPulse folder in VS Code. Then open **Terminal → New Terminal** and run:

```bash
python3 -m venv .venv
./.venv/bin/python -m pip install -r requirements.txt
./.venv/bin/python main.py
```

## Controls

- Click or tap: fly upward
- Space or Up Arrow: fly upward
- P: pause or resume

## Crystal shop

Collect crystals during a run. They are saved locally in `skypulse_progress.json` and can be spent in **Shop** to unlock birds, themes, trails, and pipe finishes. Use **Birds** to switch between the birds you own.

## Game-feel and progression

- Original flap, score, crystal, crash, new-best, and unlock sounds
- Optional sound, haptics, and reduced-motion controls in **Settings**
- A non-blocking first-flight tutorial that teaches flapping, scoring, and crystals
- A daily score challenge plus three daily missions
- Daily missions directly unlock a **trail**, **pipe finish**, and **world theme**; the daily score target unlocks a bonus cosmetic
- Achievement rewards, score bursts, impact flashes, new-best celebration, city parallax, and purely visual weather/comet events

## Test on an iPhone now

Open the `web/` build in Safari on your iPhone. It has touch controls, flight physics, pause/retry, game-over blur, and the full bird, world, trail, and pipe customisation collection. In Safari, use **Share → Add to Home Screen** for an app-like full-screen icon.

For a same-Wi-Fi test, serve only the `web/` folder and `assets/images/` from a small static host, then open its `/web/` address on the iPhone. This is a quick play-test route; the desktop and web builds save their progress separately on the device running them.

## Friend beta link

The friend beta is published from a dedicated GitHub Pages branch that contains only the mobile game and its game assets. In GitHub, open **SkyPulse → Settings → Pages**, choose **Deploy from a branch**, then select **`gh-pages`** and **`/(root)`** once. The secure beta link is:

`https://mcauleemaddison.github.io/SkyPulse/web/`

This is an unlisted-style friend beta: it is not in the App Store, but anyone with the link can open it.

## Native mobile route

SkyPulse’s native mobile game lives in `mobile/` and uses Unity 6 + C#. This is the correct long-term route for one high-performance game that can export to both Android and iPhone.

The first native flight loop already has direct tap input, native physics, pooled pipes, a compact trail, Nova/Neon City art, and native sound hooks. The web beta remains live while the native project reaches feature parity.

This Mac has full Xcode 15.2 installed, but cannot directly deploy a native build to an iPhone running iOS 27 because that iOS version requires a much newer Xcode and macOS than this 2017 Mac supports. This is an Apple toolchain compatibility limit, not a problem with SkyPulse or the iPhone. The Unity install also needs its free Personal licence activated in Unity Hub, plus Android and iOS export modules, before it can make device packages.

The Safari/PWA build above remains the immediate route for mobile gameplay testing. For a later native/TestFlight or Android release, use a newer Xcode-compatible Mac for iPhone packaging and install the Unity Android export support. The release art is ready for the project:

- `assets/images/branding/skypulse-app-icon.png` — square source icon
- `assets/images/branding/skypulse-launch-art.png` — portrait launch-screen artwork

On a Mac with Xcode, add those files to an iOS asset catalog, test on at least one notched iPhone and one smaller iPhone, verify sound/haptics/settings, then archive through Xcode for TestFlight.

## What this Mac can do now

This Mac can run the frozen Kivy desktop reference and the live mobile web beta immediately. Once Unity Personal is activated, it can run the native SkyPulse simulator too. Device packaging still needs Unity’s respective export modules and, for your current iPhone, a newer Xcode-compatible Mac.
