# SkyPulse

SkyPulse is a touch-first neon flight game with two play surfaces:

- the full desktop game, built with **Python and Kivy**;
- a touch-first Safari/PWA test build in `web/`, for immediate iPhone play-testing.

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

## Native iPhone release handoff

This Mac has full Xcode 15.2 installed. It can build the project’s existing desktop tooling, but cannot directly deploy a native build to an iPhone running iOS 27 because that iOS version requires a much newer Xcode and macOS than this 2017 Mac supports. This is an Apple toolchain compatibility limit, not a problem with SkyPulse or the iPhone.

The Safari/PWA build above works around that limitation for immediate gameplay testing. For a later native/TestFlight release, move the project to a newer Xcode-compatible Mac. The release art is ready for the Xcode project:

- `assets/images/branding/skypulse-app-icon.png` — square source icon
- `assets/images/branding/skypulse-launch-art.png` — portrait launch-screen artwork

On a Mac with Xcode, add those files to an iOS asset catalog, test on at least one notched iPhone and one smaller iPhone, verify sound/haptics/settings, then archive through Xcode for TestFlight.

## What this Mac can do

This Mac can build and test the complete Kivy desktop game and the iPhone-friendly Safari build. Both cover gameplay, graphics, menus, score saving, and the cosmetic collection; this lets you test the game immediately on your own iPhone and with friends before native packaging.
