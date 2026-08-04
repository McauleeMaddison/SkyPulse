# SkyPulse

SkyPulse is a touch-first neon flight game built with **Python and Kivy**.

## The whole project

```text
SkyPulse/
├── main.py           # The game: controls, screens, movement and drawing
├── settings.py       # Easy values to change: speed, gravity, colours
├── requirements.txt  # Kivy, the game's only dependency
├── assets/
│   └── images/
│       ├── backgrounds/neon-city.png  # Cinematic neon city sky
│       └── characters/                # Four unlockable SkyPulse birds
│           ├── nova.png
│           ├── lumen.png
│           ├── ember.png
│           └── sol.png
└── README.md         # This guide
```

Nova, Lumen, Ember, Sol, and the neon city are original game assets. The animated star layer, energy towers, crystals, flight trail, trail particles, runway scan, menus, and glow effects are drawn in Python, keeping the project compact and easy to understand.

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

Collect crystals during a run. They are saved locally in `skypulse_progress.json` and can be spent in **Shop** to unlock Lumen, Ember, and Sol. Use **Customize** to switch between the birds you own.

## What this Mac can do

This Mac can build and test the complete Kivy desktop game. That includes gameplay, graphics, sound, menus, score saving, and all of the content work.

When SkyPulse is finished, copy or clone this same project onto a newer compatible Mac for the Xcode/iPhone/TestFlight step. Keep gameplay and presentation code in `main.py`, assets in `assets/`, and game-tuning numbers in `settings.py`.
