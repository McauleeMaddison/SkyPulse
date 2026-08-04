# SkyPulse

SkyPulse is a touch-first neon flight game built with **Python and Kivy**.

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

## iPhone release handoff

This Mac has Apple Command Line Tools but not Xcode, so it can run and test the desktop build but cannot produce an iPhone archive here. The release art is ready for the Xcode project:

- `assets/images/branding/skypulse-app-icon.png` — square source icon
- `assets/images/branding/skypulse-launch-art.png` — portrait launch-screen artwork

On a Mac with Xcode, add those files to an iOS asset catalog, test on at least one notched iPhone and one smaller iPhone, verify sound/haptics/settings, then archive through Xcode for TestFlight.

## What this Mac can do

This Mac can build and test the complete Kivy desktop game. That includes gameplay, graphics, sound, menus, score saving, and all of the content work.

When SkyPulse is finished, copy or clone this same project onto a newer compatible Mac for the Xcode/iPhone/TestFlight step. Keep gameplay and presentation code in `main.py`, assets in `assets/`, and game-tuning numbers in `settings.py`.
