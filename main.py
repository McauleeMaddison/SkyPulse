"""SkyPulse — a touch-first arcade game made with Python + Kivy.

Click/tap anywhere or press Space / Up Arrow to fly.
Press P to pause or resume.
"""

import json
import random
from datetime import date
from math import exp, hypot, sin
from pathlib import Path

from kivy.config import Config

# A compact portrait window makes desktop testing feel like a phone game.
Config.set("graphics", "width", "420")
Config.set("graphics", "height", "760")
Config.set("graphics", "resizable", "1")
Config.set("graphics", "multisamples", "4")

from kivy.app import App
from kivy.clock import Clock
from kivy.core.audio import SoundLoader
from kivy.core.image import Image as CoreImage
from kivy.core.text import Label as CoreLabel
from kivy.core.window import Window
from kivy.graphics import (
    Color,
    Ellipse,
    Line,
    PopMatrix,
    PushMatrix,
    Rectangle,
    Rotate,
    RoundedRectangle,
    Translate,
)
from kivy.uix.widget import Widget
from kivy.utils import platform

from settings import (
    AQUA,
    BIRD_DRAW_WIDTH,
    BIRD_HITBOX_HALF_HEIGHT,
    BIRD_HITBOX_HALF_WIDTH,
    BIRD_HITBOX_OFFSET_X,
    BIRD_HITBOX_OFFSET_Y,
    BIRD_X,
    DEEP_SPACE,
    FLAP_REBOUND,
    FLAP_STRENGTH,
    GOLD,
    GRAVITY,
    MAX_FALL_SPEED,
    MAX_PARTICLES,
    MAX_RISE_SPEED,
    PINK,
    TOWER_GAP,
    TOWER_SPAWN_SECONDS,
    TOWER_SPEED,
    TOWER_WIDTH,
    VIOLET,
    WHITE,
)


MINT = (0.38, 0.96, 0.70)

# Each bird is a real art asset with its own flight-light palette. Prices are
# intentionally small in this first build, so new players can unlock birds by
# simply playing rather than facing a long grind.
SKINS = (
    {
        "id": "nova",
        "name": "NOVA",
        "asset": "assets/images/characters/nova.png",
        "flap_asset": "assets/images/characters/nova-flap.png",
        "price": 0,
        "accent": VIOLET,
        "trail": (VIOLET, AQUA),
    },
    {
        "id": "lumen",
        "name": "LUMEN",
        "asset": "assets/images/characters/lumen.png",
        "flap_asset": "assets/images/characters/lumen-flap.png",
        "price": 20,
        "accent": AQUA,
        "trail": (AQUA, VIOLET),
    },
    {
        "id": "ember",
        "name": "EMBER",
        "asset": "assets/images/characters/ember.png",
        "flap_asset": "assets/images/characters/ember-flap.png",
        "price": 55,
        "accent": PINK,
        "trail": (PINK, GOLD),
    },
    {
        "id": "sol",
        "name": "SOL",
        "asset": "assets/images/characters/sol.png",
        "flap_asset": "assets/images/characters/sol-flap.png",
        "price": 110,
        "accent": GOLD,
        "trail": (GOLD, AQUA),
    },
    {
        "id": "aurora",
        "name": "AURORA",
        "asset": "assets/images/characters/lumen.png",
        "flap_asset": "assets/images/characters/lumen-flap.png",
        "price": 80,
        "accent": MINT,
        "trail": (MINT, AQUA),
        "tint": (0.72, 1.0, 0.84),
    },
    {
        "id": "orchid",
        "name": "ORCHID",
        "asset": "assets/images/characters/nova.png",
        "flap_asset": "assets/images/characters/nova-flap.png",
        "price": 105,
        "accent": VIOLET,
        "trail": (VIOLET, PINK),
        "tint": (0.88, 0.60, 1.0),
    },
    {
        "id": "coral",
        "name": "CORAL",
        "asset": "assets/images/characters/ember.png",
        "flap_asset": "assets/images/characters/ember-flap.png",
        "price": 135,
        "accent": PINK,
        "trail": (PINK, GOLD),
        "tint": (1.0, 0.70, 0.84),
    },
    {
        "id": "glacier",
        "name": "GLACIER",
        "asset": "assets/images/characters/sol.png",
        "flap_asset": "assets/images/characters/sol-flap.png",
        "price": 165,
        "accent": WHITE,
        "trail": (WHITE, AQUA),
        "tint": (0.72, 0.92, 1.0),
    },
)

SKINS_BY_ID = {skin["id"]: skin for skin in SKINS}

# World style is deliberately modular: players can mix a bird, a live trail,
# a city treatment, and a pipe finish instead of being locked to one preset.
TRAILS = (
    {"id": "pulse", "name": "PULSE", "price": 0, "accent": AQUA, "colours": (VIOLET, AQUA)},
    {"id": "solar", "name": "SOLAR", "price": 35, "accent": GOLD, "colours": (GOLD, PINK)},
    {"id": "aurora", "name": "AURORA", "price": 60, "accent": MINT, "colours": (MINT, VIOLET)},
    {"id": "comet", "name": "COMET", "price": 85, "accent": WHITE, "colours": (WHITE, AQUA)},
    {"id": "ember", "name": "EMBER", "price": 115, "accent": PINK, "colours": (PINK, GOLD)},
)

THEMES = (
    {
        "id": "neon_city",
        "name": "NEON CITY",
        "price": 0,
        "accent": AQUA,
        "tint": DEEP_SPACE,
        "tint_alpha": 0.13,
        "sky_colours": (VIOLET, PINK, GOLD),
        "floor": (0.012, 0.008, 0.045),
    },
    {
        "id": "aurora_rise",
        "name": "AURORA RISE",
        "price": 55,
        "accent": MINT,
        "tint": (0.008, 0.09, 0.09),
        "tint_alpha": 0.24,
        "sky_colours": (MINT, AQUA, VIOLET),
        "floor": (0.006, 0.045, 0.055),
    },
    {
        "id": "solar_drift",
        "name": "SOLAR DRIFT",
        "price": 90,
        "accent": GOLD,
        "tint": (0.12, 0.028, 0.018),
        "tint_alpha": 0.22,
        "sky_colours": (GOLD, PINK, VIOLET),
        "floor": (0.065, 0.018, 0.025),
    },
    {
        "id": "midnight_tide",
        "name": "MIDNIGHT TIDE",
        "price": 125,
        "accent": AQUA,
        "tint": (0.005, 0.025, 0.11),
        "tint_alpha": 0.36,
        "sky_colours": (AQUA, VIOLET, WHITE),
        "floor": (0.004, 0.016, 0.075),
    },
    {
        "id": "velvet_dawn",
        "name": "VELVET DAWN",
        "price": 160,
        "accent": PINK,
        "tint": (0.11, 0.008, 0.075),
        "tint_alpha": 0.30,
        "sky_colours": (PINK, GOLD, VIOLET),
        "floor": (0.060, 0.006, 0.052),
    },
)

PIPE_STYLES = (
    {
        "id": "ion", "name": "ION", "price": 0, "accent": AQUA, "frame": VIOLET,
        "panel": (0.04, 0.18, 0.48), "energy": AQUA, "cap": GOLD,
    },
    {
        "id": "rose", "name": "ROSE", "price": 40, "accent": PINK, "frame": PINK,
        "panel": (0.33, 0.04, 0.26), "energy": VIOLET, "cap": GOLD,
    },
    {
        "id": "solar", "name": "SOLAR", "price": 70, "accent": GOLD, "frame": GOLD,
        "panel": (0.35, 0.14, 0.04), "energy": PINK, "cap": AQUA,
    },
    {
        "id": "mint", "name": "MINT", "price": 105, "accent": MINT, "frame": MINT,
        "panel": (0.025, 0.26, 0.18), "energy": AQUA, "cap": WHITE,
    },
    {
        "id": "prism", "name": "PRISM", "price": 145, "accent": WHITE, "frame": WHITE,
        "panel": (0.16, 0.06, 0.34), "energy": PINK, "cap": AQUA,
    },
)

TRAILS_BY_ID = {trail["id"]: trail for trail in TRAILS}
THEMES_BY_ID = {theme["id"]: theme for theme in THEMES}
PIPE_STYLES_BY_ID = {style["id"]: style for style in PIPE_STYLES}
SAVE_PATH = Path(__file__).parent / "skypulse_progress.json"

SOUND_FILES = {
    "flap": "assets/audio/flap.wav",
    "score": "assets/audio/score.wav",
    "crystal": "assets/audio/crystal.wav",
    "crash": "assets/audio/crash.wav",
    "new_best": "assets/audio/new-best.wav",
    "unlock": "assets/audio/unlock.wav",
}
SOUND_VOLUMES = {
    "flap": 0.34,
    "score": 0.38,
    "crystal": 0.32,
    "crash": 0.44,
    "new_best": 0.48,
    "unlock": 0.44,
}

# The daily layer is deliberately small: three clear tasks, one score target,
# and rewards that let players reach a cosmetic faster without turning play
# into a grind.
MISSIONS = (
    {
        "id": "flaps",
        "name": "RIDE THE CURRENT",
        "summary": "Flap 25 times across any flights.",
        "metric": "flaps",
        "target": 25,
        "style_category": "trail",
        "reward_label": "TRAIL STYLE",
    },
    {
        "id": "score",
        "name": "CLEAR THE GLOW",
        "summary": "Pass 8 pipes across any flights.",
        "metric": "score",
        "target": 8,
        "style_category": "pipe",
        "reward_label": "PIPE FINISH",
    },
    {
        "id": "crystals",
        "name": "CRYSTAL HUNT",
        "summary": "Collect 6 crystals across any flights.",
        "metric": "crystals",
        "target": 6,
        "style_category": "theme",
        "reward_label": "WORLD THEME",
    },
)
ACHIEVEMENTS = {
    "first_flight": {"name": "FIRST FLIGHT", "reward": 5},
    "sky_runner": {"name": "SKY RUNNER", "reward": 15},
    "crystal_keeper": {"name": "CRYSTAL KEEPER", "reward": 15},
    "style_icon": {"name": "STYLE ICON", "reward": 10},
}


class SkyPulseGame(Widget):
    """The entire game world: rules, drawing, and touch controls."""

    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        Window.bind(on_key_down=self.on_key_down)
        self.bind(size=self.on_resize)
        self.stars = [
            (random.random(), random.uniform(0.35, 0.98), random.choice((1, 1, 2)))
            for _ in range(72)
        ]
        self.backdrop_texture = self.load_texture("assets/images/backgrounds/neon-city.png")
        self.app_icon_texture = self.load_texture("assets/images/branding/skypulse-app-icon.png")
        self.launch_texture = self.load_texture("assets/images/branding/skypulse-launch-art.png")
        self.skin_textures = {
            skin["id"]: {
                "up": self.load_texture(skin["asset"]),
                "down": self.load_texture(skin["flap_asset"]),
            }
            for skin in SKINS
        }
        self.progress = self.load_progress()
        self.best_score = self.progress["best_score"]
        self.crystal_bank = self.progress["crystal_bank"]
        self.sound_enabled = self.progress["sound_enabled"]
        self.haptics_enabled = self.progress["haptics_enabled"]
        self.reduce_motion = self.progress["reduce_motion"]
        self.tutorial_complete = self.progress["tutorial_complete"]
        self.achievements = self.progress["achievements"]
        self.lifetime = self.progress["lifetime"]
        self.daily_state = self.prepare_daily_state(self.progress["daily"])
        self.unlocked_skins = self.progress["unlocked"]
        self.equipped_skin_id = self.progress["equipped"]
        self.unlocked_trails = self.progress["unlocked_trails"]
        self.equipped_trail_id = self.progress["equipped_trail"]
        self.unlocked_themes = self.progress["unlocked_themes"]
        self.equipped_theme_id = self.progress["equipped_theme"]
        self.unlocked_pipes = self.progress["unlocked_pipes"]
        self.equipped_pipe_id = self.progress["equipped_pipe"]
        self.hitboxes = []
        self.notice = ""
        self.notice_timer = 0
        self.sounds = self.load_sounds()
        city_rng = random.Random(7391)
        self.city_layers = tuple(
            tuple(
                (
                    city_rng.random(),
                    city_rng.uniform(28, 100),
                    city_rng.uniform(13, 27),
                )
                for _ in range(count)
            )
            for count in (9, 13)
        )
        self.launch_timer = 1.25
        self.reset("splash")
        # Draw on every display frame instead of a fixed 60 Hz tick; all motion
        # below is delta-time based, so high-refresh screens stay fluid too.
        Clock.schedule_interval(self.update, 0)

    @staticmethod
    def load_texture(relative_path):
        """Load a project art asset, keeping a safe code-only fallback if it is moved."""
        asset_path = Path(__file__).parent / relative_path
        try:
            texture = CoreImage(str(asset_path), mipmap=True).texture
            texture.mag_filter = "linear"
            texture.min_filter = "linear"
            return texture
        except Exception:
            return None

    def load_sounds(self):
        """Load small local effects once; missing audio never blocks a flight."""
        sounds = {}
        for name, relative_path in SOUND_FILES.items():
            try:
                sound = SoundLoader.load(str(Path(__file__).parent / relative_path))
                if sound:
                    sound.volume = SOUND_VOLUMES[name]
                sounds[name] = sound
            except Exception:
                sounds[name] = None
        return sounds

    def play_sound(self, name):
        if not self.sound_enabled:
            return
        sound = self.sounds.get(name)
        if sound:
            try:
                sound.stop()
                sound.play()
            except Exception:
                pass

    def trigger_haptic(self, style="light"):
        """Use native feedback where the packaged mobile build makes it available."""
        if not self.haptics_enabled:
            return
        try:
            if platform == "android":
                from jnius import autoclass

                activity = autoclass("org.kivy.android.PythonActivity").mActivity
                context = autoclass("android.content.Context")
                vibrator = activity.getSystemService(context.VIBRATOR_SERVICE)
                vibrator.vibrate({"light": 8, "medium": 16, "heavy": 28}[style])
            elif platform == "ios":
                from pyobjus import autoclass

                feedback = autoclass("UIImpactFeedbackGenerator").alloc().initWithStyle_(
                    {"light": 0, "medium": 1, "heavy": 2}[style]
                )
                feedback.prepare()
                feedback.impactOccurred()
        except Exception:
            # Desktop and unsupported mobile runtimes simply skip haptics.
            pass

    @staticmethod
    def daily_seed(day_token):
        return sum((index + 1) * ord(character) for index, character in enumerate(day_token))

    def prepare_daily_state(self, saved_daily):
        """Keep one deterministic daily challenge and three missions per calendar day."""
        today = date.today().isoformat()
        seed = self.daily_seed(today)
        fresh = {
            "date": today,
            "target": 8 + seed % 7,
            "best": 0,
            "reward_claimed": False,
            "missions": {mission["id"]: 0 for mission in MISSIONS},
            "completed": [],
        }
        if not isinstance(saved_daily, dict) or saved_daily.get("date") != today:
            return fresh

        saved_missions = saved_daily.get("missions", {})
        saved_missions = saved_missions if isinstance(saved_missions, dict) else {}
        completed = [mission_id for mission_id in saved_daily.get("completed", []) if mission_id in saved_missions]
        return {
            "date": today,
            "target": fresh["target"],
            "best": max(0, int(saved_daily.get("best", 0))),
            "reward_claimed": bool(saved_daily.get("reward_claimed", False)),
            "missions": {
                mission["id"]: max(0, int(saved_missions.get(mission["id"], 0)))
                for mission in MISSIONS
            },
            "completed": completed,
        }

    @staticmethod
    def load_progress():
        default = {
            "best_score": 0,
            "crystal_bank": 0,
            "sound_enabled": True,
            "haptics_enabled": True,
            "reduce_motion": False,
            "tutorial_complete": False,
            "achievements": [],
            "lifetime": {"runs": 0, "flaps": 0, "score": 0, "crystals": 0},
            "daily": {},
            "unlocked": ["nova"],
            "equipped": "nova",
            "unlocked_trails": ["pulse"],
            "equipped_trail": "pulse",
            "unlocked_themes": ["neon_city"],
            "equipped_theme": "neon_city",
            "unlocked_pipes": ["ion"],
            "equipped_pipe": "ion",
        }
        try:
            saved = json.loads(SAVE_PATH.read_text())
            def owned_ids(saved_key, catalog, starter):
                owned = [item_id for item_id in saved.get(saved_key, []) if item_id in catalog]
                if starter not in owned:
                    owned.insert(0, starter)
                return owned

            unlocked = owned_ids("unlocked", SKINS_BY_ID, "nova")
            unlocked_trails = owned_ids("unlocked_trails", TRAILS_BY_ID, "pulse")
            unlocked_themes = owned_ids("unlocked_themes", THEMES_BY_ID, "neon_city")
            unlocked_pipes = owned_ids("unlocked_pipes", PIPE_STYLES_BY_ID, "ion")

            equipped = saved.get("equipped", "nova") if saved.get("equipped") in unlocked else "nova"
            equipped_trail = saved.get("equipped_trail", "pulse")
            equipped_theme = saved.get("equipped_theme", "neon_city")
            equipped_pipe = saved.get("equipped_pipe", "ion")
            lifetime_saved = saved.get("lifetime", {})
            lifetime_saved = lifetime_saved if isinstance(lifetime_saved, dict) else {}
            lifetime = {
                key: max(0, int(lifetime_saved.get(key, 0)))
                for key in ("runs", "flaps", "score", "crystals")
            }
            achievements = [
                achievement_id
                for achievement_id in saved.get("achievements", [])
                if achievement_id in ACHIEVEMENTS
            ]
            daily = saved.get("daily", {})
            daily = daily if isinstance(daily, dict) else {}
            return {
                "best_score": max(0, int(saved.get("best_score", 0))),
                "crystal_bank": max(0, int(saved.get("crystal_bank", 0))),
                "sound_enabled": bool(saved.get("sound_enabled", True)),
                "haptics_enabled": bool(saved.get("haptics_enabled", True)),
                "reduce_motion": bool(saved.get("reduce_motion", False)),
                "tutorial_complete": bool(saved.get("tutorial_complete", False)),
                "achievements": achievements,
                "lifetime": lifetime,
                "daily": daily,
                "unlocked": unlocked,
                "equipped": equipped,
                "unlocked_trails": unlocked_trails,
                "equipped_trail": equipped_trail if equipped_trail in unlocked_trails else "pulse",
                "unlocked_themes": unlocked_themes,
                "equipped_theme": equipped_theme if equipped_theme in unlocked_themes else "neon_city",
                "unlocked_pipes": unlocked_pipes,
                "equipped_pipe": equipped_pipe if equipped_pipe in unlocked_pipes else "ion",
            }
        except (OSError, ValueError, TypeError):
            return default

    def save_progress(self):
        self.progress = {
            "best_score": self.best_score,
            "crystal_bank": self.crystal_bank,
            "sound_enabled": self.sound_enabled,
            "haptics_enabled": self.haptics_enabled,
            "reduce_motion": self.reduce_motion,
            "tutorial_complete": self.tutorial_complete,
            "achievements": self.achievements,
            "lifetime": self.lifetime,
            "daily": self.daily_state,
            "unlocked": self.unlocked_skins,
            "equipped": self.equipped_skin_id,
            "unlocked_trails": self.unlocked_trails,
            "equipped_trail": self.equipped_trail_id,
            "unlocked_themes": self.unlocked_themes,
            "equipped_theme": self.equipped_theme_id,
            "unlocked_pipes": self.unlocked_pipes,
            "equipped_pipe": self.equipped_pipe_id,
        }
        try:
            SAVE_PATH.write_text(json.dumps(self.progress, indent=2) + "\n")
        except OSError:
            pass

    @property
    def current_skin(self):
        return SKINS_BY_ID[self.equipped_skin_id]

    @property
    def current_trail(self):
        return TRAILS_BY_ID[self.equipped_trail_id]

    @property
    def current_theme(self):
        return THEMES_BY_ID[self.equipped_theme_id]

    @property
    def current_pipe(self):
        return PIPE_STYLES_BY_ID[self.equipped_pipe_id]

    @property
    def scale(self):
        """Keeps the same game proportions if you resize the desktop window."""
        return self.width / 420 if self.width else 1

    @property
    def safe_top_padding(self):
        """Conservative inset for a notch/Dynamic Island in the iPhone package."""
        return (46 if platform == "ios" else 0) * self.scale

    @property
    def safe_bottom_padding(self):
        """Keep interactive controls clear of the iPhone home indicator."""
        return (30 if platform == "ios" else 0) * self.scale

    @property
    def ground_y(self):
        return self.height * 0.12

    def bird_collider(self):
        """Return the centre and radii of the bird's core-body collision ellipse."""
        scale = self.scale
        return (
            BIRD_X * scale + BIRD_HITBOX_OFFSET_X * scale,
            self.bird_y + BIRD_HITBOX_OFFSET_Y * scale,
            BIRD_HITBOX_HALF_WIDTH * scale,
            BIRD_HITBOX_HALF_HEIGHT * scale,
        )

    @staticmethod
    def ellipse_hits_rectangle(center_x, center_y, radius_x, radius_y, x, y, width, height):
        """Test an elliptical bird body against one rectangular tower section."""
        nearest_x = max(x, min(center_x, x + width))
        nearest_y = max(y, min(center_y, y + height))
        distance_x = (center_x - nearest_x) / max(radius_x, 0.001)
        distance_y = (center_y - nearest_y) / max(radius_y, 0.001)
        return distance_x * distance_x + distance_y * distance_y < 1

    def on_resize(self, *_args):
        if getattr(self, "state", None) == "menu" and not getattr(self, "towers", []):
            self.bird_y = self.height * 0.53

    def reset(self, state="playing"):
        self.state = state
        self.bird_y = self.height * 0.53 if self.height else 400
        self.bird_speed = 0
        self.bird_tilt = 0
        self.wing_phase = 0
        self.flap_energy = 0
        self.flap_bounce = 0
        self.score = 0
        self.crystals_collected = 0
        self.run_flaps = 0
        self.run_best_before = self.best_score
        self.new_best_this_run = False
        self.new_best_timer = 0
        self.daily_reward_earned = False
        self.daily_rewards_this_run = []
        self.is_daily_run = False
        self.towers = []
        self.crystals = []
        self.sparks = []
        self.score_bursts = []
        self.flight_trail = []
        self.spawn_timer = 0.8
        self.trail_timer = 0
        self.screen_shake = 0
        self.impact_flash = 0
        self.achievement_banner = ""
        self.achievement_timer = 0
        self.tutorial_active = state == "playing" and not self.tutorial_complete
        self.tutorial_phase = 0
        self.world_event = None
        self.world_event_timer = random.uniform(10, 16)
        self.world_event_duration = 0
        self.time = 0

    def start_daily(self):
        self.reset("playing")
        self.is_daily_run = True
        self.lifetime["runs"] += 1
        self.flap()

    def toggle_setting(self, setting):
        values = {
            "sound": "sound_enabled",
            "haptics": "haptics_enabled",
            "motion": "reduce_motion",
        }
        attribute = values[setting]
        setattr(self, attribute, not getattr(self, attribute))
        if setting == "motion" and self.reduce_motion:
            self.world_event = None
            self.screen_shake = 0
        self.notice = {
            "sound": "SOUND " + ("ON" if self.sound_enabled else "OFF"),
            "haptics": "HAPTICS " + ("ON" if self.haptics_enabled else "OFF"),
            "motion": "REDUCED MOTION " + ("ON" if self.reduce_motion else "OFF"),
        }[setting]
        self.notice_timer = 1.4
        self.save_progress()

    def unlock_style_reward(self, category):
        """Unlock and equip the next cosmetic in a category as a daily prize."""
        collections = {
            "theme": (THEMES, self.unlocked_themes, "equipped_theme_id", "WORLD THEME"),
            "trail": (TRAILS, self.unlocked_trails, "equipped_trail_id", "TRAIL STYLE"),
            "pipe": (PIPE_STYLES, self.unlocked_pipes, "equipped_pipe_id", "PIPE FINISH"),
        }
        items, unlocked, equipped_attribute, label = collections[category]
        locked = [item for item in items if item["id"] not in unlocked]
        if not locked:
            return None, label + " COLLECTION COMPLETE"
        item = min(locked, key=lambda option: option["price"])
        unlocked.append(item["id"])
        setattr(self, equipped_attribute, item["id"])
        return item, item["name"] + " " + label + " UNLOCKED"

    def unlock_bonus_style_reward(self):
        """Use the daily seed to rotate the score-challenge cosmetic reward."""
        order = ("theme", "trail", "pipe")
        start = self.daily_seed(self.daily_state["date"]) % len(order)
        for offset in range(len(order)):
            item, message = self.unlock_style_reward(order[(start + offset) % len(order)])
            if item:
                return item, message
        return None, "STYLE COLLECTION COMPLETE"

    def advance_mission(self, metric, amount=1):
        """Advance one of today's tiny, visible goals and celebrate completion."""
        for mission in MISSIONS:
            if mission["metric"] != metric:
                continue
            mission_id = mission["id"]
            previous = self.daily_state["missions"][mission_id]
            current = min(mission["target"], previous + amount)
            self.daily_state["missions"][mission_id] = current
            if current >= mission["target"] and mission_id not in self.daily_state["completed"]:
                self.daily_state["completed"].append(mission_id)
                item, _message = self.unlock_style_reward(mission["style_category"])
                if item:
                    self.daily_rewards_this_run.append((mission["reward_label"], item["name"]))
                self.play_sound("unlock")
                self.trigger_haptic("medium")
                self.save_progress()

    def unlock_achievement(self, achievement_id):
        if achievement_id in self.achievements:
            return
        achievement = ACHIEVEMENTS[achievement_id]
        self.achievements.append(achievement_id)
        self.crystal_bank += achievement["reward"]
        self.achievement_banner = achievement["name"] + "  +" + str(achievement["reward"]) + " ◆"
        self.achievement_timer = 3.4
        self.play_sound("unlock")
        self.trigger_haptic("medium")
        self.save_progress()

    def update_tutorial(self):
        if not self.tutorial_active:
            return
        if self.tutorial_phase == 0 and self.run_flaps >= 2:
            self.tutorial_phase = 1
        if self.tutorial_phase == 1 and self.score >= 1:
            self.tutorial_phase = 2
        if self.tutorial_phase == 2 and (self.crystals_collected >= 1 or self.score >= 3):
            self.tutorial_active = False
            self.tutorial_complete = True
            self.achievement_banner = "TRAINING COMPLETE"
            self.achievement_timer = 2.5
            self.save_progress()

    def update_world_event(self, dt):
        if self.reduce_motion:
            return
        if self.world_event:
            self.world_event_duration -= dt
            if self.world_event_duration <= 0:
                self.world_event = None
                self.world_event_timer = random.uniform(12, 20)
            return
        self.world_event_timer -= dt
        if self.world_event_timer <= 0:
            self.world_event = random.choice(("meteor", "ion_rain"))
            self.world_event_duration = 4.6

    def flap(self):
        if self.state != "playing":
            return
        scale = self.scale
        # A flap restores lift without making fast consecutive taps jerk upward.
        self.bird_speed = min(
            MAX_RISE_SPEED * scale,
            max(FLAP_STRENGTH * scale, self.bird_speed + FLAP_REBOUND * scale),
        )
        self.flap_energy = 1.0
        self.flap_bounce = 1.0
        self.wing_phase = -1.5708
        # A real bird snaps its head up with the wing beat, then settles into
        # the dive rather than rotating mechanically with raw velocity.
        self.bird_tilt = max(self.bird_tilt, 17)
        self.run_flaps += 1
        self.lifetime["flaps"] += 1
        self.advance_mission("flaps")
        self.unlock_achievement("first_flight")
        self.play_sound("flap")
        self.trigger_haptic("light")
        if not self.reduce_motion:
            self.screen_shake = max(self.screen_shake, 0.025)
        primary, secondary = self.current_trail["colours"]
        for index in range(8):
            self.sparks.append(
                {
                    "x": BIRD_X * self.scale - 22 - index * 5,
                    "y": self.bird_y + random.randint(-12, 12),
                    "life": 0.45,
                    "colour": primary if index % 2 else secondary,
                    "vx": -135 * self.scale - index * 12,
                    "vy": random.randint(-35, 35),
                }
            )

    def start_or_flap(self):
        if self.state in ("menu", "game_over"):
            self.reset("playing")
            self.lifetime["runs"] += 1
        elif self.state == "paused":
            self.state = "playing"
            return
        self.flap()

    def add_tower(self):
        gap = (TOWER_GAP - min(self.score, 24) * 2) * self.scale
        lower_limit = self.ground_y + gap / 2 + 74 * self.scale
        upper_limit = self.height - gap / 2 - 110 * self.scale
        gap_y = random.uniform(lower_limit, max(lower_limit + 1, upper_limit))
        self.towers.append(
            {
                "x": self.width + 35 * self.scale,
                "gap_y": gap_y,
                "gap": gap,
                "passed": False,
                "variant": random.choices(("standard", "striped", "beacon"), weights=(0.66, 0.24, 0.10))[0],
                "phase": random.random() * 6.28,
            }
        )
        if random.random() < 0.72:
            self.crystals.append(
                {
                    "x": self.width + 74 * self.scale,
                    "y": gap_y + random.uniform(-gap * 0.24, gap * 0.24),
                    "spin": random.random() * 6.28,
                }
            )

    def game_over(self):
        if self.state != "playing":
            return
        self.state = "game_over"
        self.best_score = max(self.best_score, self.score)
        self.play_sound("crash")
        self.trigger_haptic("heavy")
        self.impact_flash = 0.42
        self.screen_shake = 0 if self.reduce_motion else 0.32
        if self.is_daily_run:
            self.daily_state["best"] = max(self.daily_state["best"], self.score)
            if self.score >= self.daily_state["target"] and not self.daily_state["reward_claimed"]:
                self.daily_state["reward_claimed"] = True
                item, _message = self.unlock_bonus_style_reward()
                self.daily_reward_earned = item is not None
                if item:
                    self.daily_rewards_this_run.append(("BONUS STYLE", item["name"]))
                self.play_sound("new_best")
        for _ in range(18):
            self.sparks.append(
                {
                    "x": BIRD_X * self.scale + random.randint(-22, 22),
                    "y": self.bird_y + random.randint(-22, 22),
                    "life": 0.9,
                    "colour": PINK,
                    "vx": random.randint(-160, 160),
                    "vy": random.randint(-160, 160),
                }
            )
        self.sparks = self.sparks[-MAX_PARTICLES:]
        self.save_progress()

    def update(self, dt):
        # Prevent a momentary window stall from causing a visible jump or a
        # surprise collision when the next frame arrives.
        dt = min(dt, 1 / 30)
        self.time += dt
        self.screen_shake = max(0, self.screen_shake - dt)
        self.notice_timer = max(0, self.notice_timer - dt)
        self.impact_flash = max(0, self.impact_flash - dt * 1.9)
        self.new_best_timer = max(0, self.new_best_timer - dt)
        self.achievement_timer = max(0, self.achievement_timer - dt)
        self.flap_energy = max(0, self.flap_energy - dt * 2.8)
        self.flap_bounce = max(0, self.flap_bounce - dt * 3.6)
        # A short, even wing cycle reads as a deliberate flap rather than a
        # rapid sprite flicker, including on high-refresh displays.
        self.wing_phase += dt * (6.8 + self.flap_energy * 15)
        self.sparks = [
            {
                **spark,
                "x": spark["x"] + spark.get("vx", -95 * self.scale) * dt,
                "y": spark["y"] + spark.get("vy", 0) * dt,
                "life": spark["life"] - dt,
            }
            for spark in self.sparks
            if spark["life"] > 0
        ][-MAX_PARTICLES:]
        self.score_bursts = [
            {**burst, "y": burst["y"] + 42 * self.scale * dt, "life": burst["life"] - dt}
            for burst in self.score_bursts
            if burst["life"] > 0
        ]

        if self.state == "splash":
            self.launch_timer -= dt
            if self.launch_timer <= 0:
                self.reset("menu")
            self.draw()
            return

        if self.state != "playing":
            self.draw()
            return

        scale = self.scale
        bird_x = BIRD_X * scale
        fall_multiplier = 1.0 if self.bird_speed >= 0 else 1.16
        self.bird_speed = max(
            -MAX_FALL_SPEED * scale,
            self.bird_speed - GRAVITY * fall_multiplier * scale * dt,
        )
        self.bird_y += self.bird_speed * dt
        target_tilt = max(-42, min(27, self.bird_speed / max(scale, 0.01) * 0.080))
        tilt_response = 17 if target_tilt > self.bird_tilt else 7
        self.bird_tilt += (target_tilt - self.bird_tilt) * (1 - exp(-tilt_response * dt))
        bird_hit_x, bird_hit_y, bird_half_width, bird_half_height = self.bird_collider()
        self.flight_trail = [
            (trail_x, trail_y, age + dt)
            for trail_x, trail_y, age in self.flight_trail
            if age + dt < 0.48
        ]
        self.flight_trail.insert(0, (bird_x, self.bird_y, 0))
        self.flight_trail = self.flight_trail[:32]
        self.spawn_timer -= dt
        self.trail_timer -= dt
        speed = (TOWER_SPEED + min(self.score, 25) * 4) * scale
        self.update_world_event(dt)

        if self.trail_timer <= 0:
            self.trail_timer = 0.05
            primary, secondary = self.current_trail["colours"]
            self.sparks.append(
                {
                    "x": bird_x - 30 * scale,
                    "y": self.bird_y + random.randint(-10, 10),
                    "life": 0.36,
                    "colour": primary if random.random() < 0.55 else secondary,
                    "vx": -145 * scale,
                    "vy": random.randint(-30, 30),
                }
            )

        if self.spawn_timer <= 0:
            self.add_tower()
            self.spawn_timer = TOWER_SPAWN_SECONDS

        for tower in self.towers:
            tower["x"] -= speed * dt
            if not tower["passed"] and tower["x"] + TOWER_WIDTH * scale < bird_hit_x - bird_half_width:
                tower["passed"] = True
                self.score += 1
                self.lifetime["score"] += 1
                self.advance_mission("score")
                self.play_sound("score")
                self.trigger_haptic("light")
                self.impact_flash = max(self.impact_flash, 0.12)
                if not self.reduce_motion:
                    self.screen_shake = max(self.screen_shake, 0.07)
                self.score_bursts.append(
                    {
                        "x": bird_x + 6 * scale,
                        "y": self.bird_y + 28 * scale,
                        "life": 0.72,
                        "colour": GOLD,
                        "text": "+1",
                    }
                )
                self.sparks.append(
                    {
                        "x": bird_x,
                        "y": self.bird_y,
                        "life": 0.6,
                        "colour": GOLD,
                        "vx": -20,
                        "vy": 75,
                    }
                )
                if not self.new_best_this_run and self.score > self.run_best_before:
                    self.new_best_this_run = True
                    self.best_score = self.score
                    self.new_best_timer = 2.7
                    self.achievement_banner = "NEW BEST!"
                    self.achievement_timer = 2.7
                    self.play_sound("new_best")
                    self.trigger_haptic("medium")
                    self.impact_flash = max(self.impact_flash, 0.22)
                if self.score >= 10:
                    self.unlock_achievement("sky_runner")

        for crystal in self.crystals:
            crystal["x"] -= speed * dt
            if hypot(crystal["x"] - bird_x, crystal["y"] - self.bird_y) < 35 * scale:
                crystal["x"] = -100
                self.crystals_collected += 1
                self.crystal_bank += 1
                self.lifetime["crystals"] += 1
                self.advance_mission("crystals")
                self.play_sound("crystal")
                self.trigger_haptic("light")
                self.impact_flash = max(self.impact_flash, 0.08)
                for _ in range(12):
                    self.sparks.append(
                        {
                            "x": bird_x + random.randint(-12, 12),
                            "y": self.bird_y + random.randint(-12, 12),
                            "life": 0.7,
                            "colour": AQUA,
                            "vx": random.randint(-100, 100),
                            "vy": random.randint(-100, 100),
                        }
                    )
                if self.lifetime["crystals"] >= 25:
                    self.unlock_achievement("crystal_keeper")

        self.towers = [tower for tower in self.towers if tower["x"] > -100 * scale]
        self.crystals = [crystal for crystal in self.crystals if crystal["x"] > -40 * scale]
        self.update_tutorial()

        for tower in self.towers:
            gap_bottom = tower["gap_y"] - tower["gap"] / 2
            gap_top = tower["gap_y"] + tower["gap"] / 2
            # Every solid part of a pipe is dangerous, including its wider cap.
            # The soft ambient glow remains decorative and does not cause hits.
            pipe_x = tower["x"] - 8 * scale
            pipe_width = TOWER_WIDTH * scale + 16 * scale
            if self.ellipse_hits_rectangle(
                bird_hit_x,
                bird_hit_y,
                bird_half_width,
                bird_half_height,
                pipe_x,
                self.ground_y,
                pipe_width,
                gap_bottom - self.ground_y + 3 * scale,
            ) or self.ellipse_hits_rectangle(
                bird_hit_x,
                bird_hit_y,
                bird_half_width,
                bird_half_height,
                pipe_x,
                gap_top,
                pipe_width,
                self.height - gap_top,
            ):
                self.game_over()

        if bird_hit_y - bird_half_height < self.ground_y or bird_hit_y + bird_half_height > self.height:
            self.game_over()
        self.draw()

    def colour(self, rgb, alpha=1):
        Color(rgb[0], rgb[1], rgb[2], alpha)

    def draw_label(self, text, center_x, y, size, colour, alpha=1, shadow=True):
        """Draw crisp arcade text with a restrained shadow for legibility."""
        label = CoreLabel(
            text=text,
            font_size=size * self.scale,
            bold=True,
            # Keep the label texture white so the canvas colour below can supply
            # the intended neon tint without multiplying it twice.
            color=(1, 1, 1, 1),
        )
        label.refresh()
        texture = label.texture
        if shadow:
            self.colour(DEEP_SPACE, min(0.72, alpha * 0.72))
            Rectangle(
                texture=texture,
                pos=(center_x - texture.width / 2 + 1.4 * self.scale, y - 1.4 * self.scale),
                size=texture.size,
            )
        self.colour(colour, alpha)
        Rectangle(texture=texture, pos=(center_x - texture.width / 2, y), size=texture.size)

    def draw_panel(self, x, y, width, height, accent, alpha=0.16):
        """A translucent, bordered panel used by the HUD and menus."""
        scale = self.scale
        self.colour(accent, alpha * 0.32)
        RoundedRectangle(
            pos=(x - 3 * scale, y - 3 * scale),
            size=(width + 6 * scale, height + 6 * scale),
            radius=[14 * scale],
        )
        self.colour((0.025, 0.018, 0.12), alpha + 0.22)
        RoundedRectangle(pos=(x, y), size=(width, height), radius=[11 * scale])
        self.colour(accent, min(0.72, alpha + 0.32))
        Line(rectangle=(x, y, width, height), width=max(0.8, 1.1 * scale))

    def draw_action_button(self, text, center_x, y, width, height, accent=VIOLET, action=None):
        """A restrained neon call-to-action with one clear visual emphasis."""
        scale = self.scale
        pulse = 0.065 + sin(self.time * 3.0) * 0.018
        left = center_x - width / 2
        self.colour(accent, pulse)
        RoundedRectangle(
            pos=(left - 5 * scale, y - 5 * scale),
            size=(width + 10 * scale, height + 10 * scale),
            radius=[height * 0.42],
        )
        self.colour((0.055, 0.025, 0.14), 0.90)
        RoundedRectangle(pos=(left, y), size=(width, height), radius=[height * 0.34])
        self.colour(accent, 0.70)
        Line(rectangle=(left, y, width, height), width=max(0.8, 1.0 * scale))
        self.colour(WHITE, 0.16)
        Line(
            points=[left + 15 * scale, y + height - 4 * scale, left + width - 15 * scale, y + height - 4 * scale],
            width=max(0.45, 0.65 * scale),
        )
        self.draw_label(text, center_x, y + height * 0.30, 14, WHITE)
        if action:
            self.hitboxes.append((left, y, width, height, action))

    def draw_secondary_button(self, text, center_x, y, width, accent, action):
        """A low-profile utility action for navigation that should not compete with FLY."""
        scale = self.scale
        height = 34 * scale
        left = center_x - width / 2
        self.colour((0.025, 0.015, 0.09), 0.58)
        RoundedRectangle(pos=(left, y), size=(width, height), radius=[10 * scale])
        self.colour(accent, 0.48)
        Line(rectangle=(left, y, width, height), width=max(0.65, 0.8 * scale))
        self.draw_label(text, center_x, y + 10 * scale, 10, WHITE, alpha=0.88)
        self.hitboxes.append((left, y, width, height, action))

    def draw_skin_card(self, skin, x, y, width, height, hangar=False):
        """A compact, tappable bird card for the shop and hangar screens."""
        owned = skin["id"] in self.unlocked_skins
        equipped = skin["id"] == self.equipped_skin_id
        accent = skin["accent"]
        self.draw_panel(x, y, width, height, accent, 0.26 if equipped else 0.14)
        self.draw_bird(x + width * 0.53, y + height * 0.64, size=0.43, skin=skin, preview=True)
        self.draw_label(skin["name"], x + width / 2, y + 27 * self.scale, 13, accent)
        if equipped:
            status, status_colour = "EQUIPPED", WHITE
        elif owned:
            status, status_colour = "TAP TO EQUIP", AQUA
        elif hangar:
            status, status_colour = "LOCKED", PINK
        else:
            status, status_colour = "◆ " + str(skin["price"]), GOLD
        self.draw_label(status, x + width / 2, y + 8 * self.scale, 10, status_colour)
        if not hangar or owned:
            self.hitboxes.append((x, y, width, height, "skin:" + skin["id"]))

    def draw_city_parallax(self, theme, motion_time):
        """Low-cost foreground silhouettes give the city depth at every speed."""
        base_y = self.ground_y
        layer_specs = (
            (self.city_layers[0], 7, 0.11, (0.015, 0.018, 0.09)),
            (self.city_layers[1], 16, 0.20, (0.018, 0.025, 0.14)),
        )
        for buildings, speed, alpha, colour in layer_specs:
            for index, (position, height, width) in enumerate(buildings):
                building_width = width * self.scale
                x = (position * (self.width + building_width) - motion_time * speed * self.scale) % (
                    self.width + building_width
                ) - building_width
                building_height = height * self.scale
                self.colour(colour, 0.90)
                Rectangle(pos=(x, base_y), size=(building_width, building_height))
                self.colour(theme["accent"], alpha)
                for window_y in range(int(base_y + 11 * self.scale), int(base_y + building_height), int(15 * self.scale)):
                    window_x = x + (6 + (index % 3) * 3) * self.scale
                    Rectangle(pos=(window_x, window_y), size=(max(1, 2 * self.scale), max(1, 3 * self.scale)))

    def draw_cinematic_event(self, theme, motion_time):
        """Rare backdrop-only events enrich a run without changing collision rules."""
        if not self.world_event or self.reduce_motion:
            return
        event_alpha = min(1, self.world_event_duration / 0.65)
        if self.world_event == "meteor":
            for index in range(9):
                phase = (motion_time * (0.34 + index * 0.025) + index * 0.17) % 1
                x = self.width * (1.08 - phase)
                y = self.height * (0.45 + ((index * 0.13 + phase * 0.17) % 0.45))
                self.colour(theme["sky_colours"][index % 3], 0.28 * event_alpha)
                Line(points=[x, y, x + 34 * self.scale, y + 13 * self.scale], width=1.1 * self.scale)
        else:
            for index in range(18):
                x = (index * 47 * self.scale + motion_time * 22 * self.scale) % (self.width + 22 * self.scale) - 11 * self.scale
                y = (index * 73 * self.scale - motion_time * 270 * self.scale) % self.height
                self.colour(theme["accent"], 0.18 * event_alpha)
                Line(points=[x, y, x - 8 * self.scale, y - 34 * self.scale], width=0.85 * self.scale)

    def draw_background(self):
        """Animated sky, city parallax, horizon glow, and a perspective runway."""
        theme = self.current_theme
        motion_time = self.time * (0.16 if self.reduce_motion else 1)
        if self.backdrop_texture:
            self.colour(WHITE)
            Rectangle(texture=self.backdrop_texture, pos=(0, 0), size=self.size)
            self.colour(theme["tint"], theme["tint_alpha"])
            Rectangle(pos=(0, 0), size=self.size)
        else:
            stripe_height = max(4, int(self.height / 76))
            for y in range(0, int(self.height), stripe_height):
                level = y / max(self.height, 1)
                self.colour((0.035 + level * 0.035, 0.008 + level * 0.013, 0.125 + level * 0.18))
                Rectangle(pos=(0, y), size=(self.width, stripe_height + 1))

        # A separate moving star layer sells motion even while the world art stays crisp.
        for index in range(4):
            drift = (motion_time * (10 + index * 3) + index * 133) % (self.width + 180) - 90
            cloud_y = self.height * (0.38 + (index % 3) * 0.17)
            cloud_size = (120 + index * 35) * self.scale
            self.colour(theme["sky_colours"][index % 3], 0.025)
            Ellipse(pos=(drift - cloud_size / 2, cloud_y - cloud_size / 5), size=(cloud_size, cloud_size / 2.5))

        for x, y, radius in self.stars:
            shimmer = 0.55 + sin(motion_time * 2 + x * 12) * 0.2
            star_x = (x * self.width - motion_time * (7 + radius * 4) * self.scale) % self.width
            self.colour(theme["accent"] if radius == 2 else WHITE, shimmer)
            Ellipse(pos=(star_x, y * self.height), size=(radius * 2, radius * 2))

        # A breathing horizon glow is layered over the cinematic city illustration.
        glow_center_x = self.width * 0.50
        glow_center_y = self.height * 0.105
        horizon_breath = sin(motion_time * 1.7) * 0.025
        for multiplier, alpha, colour in zip(
            (1.20, 0.90, 0.58),
            (0.020, 0.045 + horizon_breath, 0.08),
            theme["sky_colours"],
        ):
            diameter = self.width * multiplier
            self.colour(colour, alpha)
            Ellipse(pos=(glow_center_x - diameter / 2, glow_center_y - diameter / 2), size=(diameter, diameter))

        for index in range(3):
            phase = (motion_time * (0.15 + index * 0.04) + index * 0.34) % 1
            comet_x = self.width * (1 - phase) + 40 * self.scale
            comet_y = self.height * (0.72 + index * 0.07)
            self.colour(theme["accent"] if index % 2 else theme["sky_colours"][1], 0.18)
            Line(
                points=[comet_x, comet_y, comet_x + 34 * self.scale, comet_y + 12 * self.scale],
                width=max(0.7, 1.2 * self.scale),
            )
        self.draw_city_parallax(theme, motion_time)
        self.draw_cinematic_event(theme, motion_time)

    def draw_runway(self):
        """Draw the playable floor so its bright edge exactly matches ground collision."""
        scale = self.scale
        floor_y = self.ground_y
        horizon_x = self.width * 0.50
        theme = self.current_theme
        accent = theme["accent"]

        # An opaque deck makes the playable floor unmistakable, while retaining
        # a little neon depth through layered surface lighting.
        self.colour(theme["floor"], 1)
        Rectangle(pos=(0, 0), size=(self.width, floor_y))
        for index, (height_ratio, colour, alpha) in enumerate(
            (
                (0.38, tuple(channel * 0.55 for channel in VIOLET), 0.52),
                (0.24, tuple(channel * 0.42 for channel in accent), 0.58),
                (0.14, tuple(channel * 0.34 for channel in theme["sky_colours"][1]), 0.48),
            )
        ):
            glow_height = floor_y * height_ratio
            self.colour(colour, alpha)
            Rectangle(
                pos=(0, max(0, floor_y - glow_height - index * 3 * scale)),
                size=(self.width, glow_height),
            )

        # Perspective rails make the landing plane read at a glance on a phone.
        for index, ratio in enumerate((0.11, 0.28, 0.72, 0.89)):
            end_x = self.width * ratio
            self.colour(accent if index in (1, 2) else theme["sky_colours"][0], 0.15)
            Line(
                points=[horizon_x + (end_x - horizon_x) * 0.16, floor_y - 2 * scale, end_x, 0],
                width=max(0.55, 0.8 * scale),
            )
        for ratio, alpha in ((0.25, 0.07), (0.55, 0.11), (0.80, 0.16)):
            y = floor_y * ratio
            self.colour(theme["sky_colours"][0], alpha)
            Line(points=[0, y, self.width, y], width=max(0.45, 0.7 * scale))

        # A reflected scan sweeps across the actual floor surface, not the sky.
        scan_y = floor_y * (0.16 + (self.time * 0.52 % 0.64))
        self.colour(accent, 0.26)
        Line(points=[0, scan_y, self.width, scan_y], width=1.0 * scale)

        # A dense, raised lip makes the floor unmistakable at a glance.
        self.colour(tuple(channel * 0.28 for channel in theme["sky_colours"][0]), 1)
        Rectangle(pos=(0, floor_y - 10 * scale), size=(self.width, 10 * scale))
        self.colour(tuple(channel * 0.48 for channel in accent), 1)
        Rectangle(pos=(0, floor_y - 4 * scale), size=(self.width, 4 * scale))
        for x in range(18, int(self.width), int(48 * scale)):
            self.colour(accent, 0.38)
            Ellipse(
                pos=(x - 1.5 * scale, floor_y - 7 * scale),
                size=(3 * scale, 3 * scale),
            )

        # This rail is the collision line: its top edge is the precise floor limit.
        pulse = 0.72 + sin(self.time * 3.2) * 0.12
        self.colour(theme["sky_colours"][0], 0.78)
        Line(points=[0, floor_y - 1.1 * scale, self.width, floor_y - 1.1 * scale], width=3.5 * scale)
        self.colour(accent, pulse)
        Line(points=[0, floor_y, self.width, floor_y], width=1.15 * scale)
        self.colour(WHITE, 0.64)
        Line(points=[0, floor_y + 1.2 * scale, self.width, floor_y + 1.2 * scale], width=0.48 * scale)

    def draw_tower(self, tower):
        scale = self.scale
        width = TOWER_WIDTH * scale
        pipe = self.current_pipe
        variant = tower.get("variant", "standard")
        gap_bottom = tower["gap_y"] - tower["gap"] / 2
        gap_top = tower["gap_y"] + tower["gap"] / 2
        sections = ((self.ground_y, gap_bottom - self.ground_y, True), (gap_top, self.height - gap_top, False))
        for y, height, cap_on_top in sections:
            if height <= 0:
                continue
            pulse = 0.55 + sin(self.time * 5 + tower["x"] * 0.025) * 0.20
            self.colour(pipe["energy"], 0.055)
            RoundedRectangle(
                pos=(tower["x"] - 10 * scale, y - 5 * scale),
                size=(width + 20 * scale, height + 10 * scale),
                radius=[12 * scale],
            )
            self.colour((0.025, 0.035, 0.19), 0.98)
            RoundedRectangle(pos=(tower["x"], y), size=(width, height), radius=[8 * scale])
            self.colour(pipe["frame"], 0.55)
            Rectangle(pos=(tower["x"] + 9 * scale, y + 3 * scale), size=(width - 18 * scale, max(0, height - 6 * scale)))
            self.colour(pipe["panel"], 0.95)
            Rectangle(pos=(tower["x"] + 16 * scale, y + 6 * scale), size=(width - 32 * scale, max(0, height - 12 * scale)))
            self.colour(pipe["energy"], pulse)
            Rectangle(pos=(tower["x"] + 29 * scale, y + 8 * scale), size=(7 * scale, max(0, height - 16 * scale)))
            energy_y = y + (self.time * 145 * scale) % max(height, 1)
            self.colour(WHITE, 0.65)
            Rectangle(pos=(tower["x"] + 15 * scale, energy_y), size=(width - 30 * scale, 3 * scale))
            self.colour(pipe["energy"], 0.92)
            Line(rectangle=(tower["x"], y, width, height), width=1.4 * scale)
            if variant == "striped":
                for stripe_y in range(int(y + 18 * scale), int(y + height - 8 * scale), int(30 * scale)):
                    self.colour(pipe["cap"], 0.28)
                    Line(
                        points=[tower["x"] + 6 * scale, stripe_y, tower["x"] + width - 6 * scale, stripe_y + 12 * scale],
                        width=2.2 * scale,
                    )
            elif variant == "beacon":
                beacon_y = y + height * 0.5
                beacon_alpha = 0.40 + sin(self.time * 7 + tower.get("phase", 0)) * 0.28
                self.colour(pipe["energy"], beacon_alpha)
                Ellipse(pos=(tower["x"] + width * 0.5 - 6 * scale, beacon_y - 6 * scale), size=(12 * scale, 12 * scale))
                self.colour(WHITE, 0.78)
                Ellipse(pos=(tower["x"] + width * 0.5 - 2 * scale, beacon_y - 2 * scale), size=(4 * scale, 4 * scale))
            cap_y = y + height - 14 * scale if cap_on_top else y
            self.colour(pipe["cap"], 0.17)
            RoundedRectangle(
                pos=(tower["x"] - 12 * scale, cap_y - 4 * scale),
                size=(width + 24 * scale, 25 * scale),
                radius=[9 * scale],
            )
            self.colour(pipe["cap"])
            RoundedRectangle(pos=(tower["x"] - 8 * scale, cap_y), size=(width + 16 * scale, 17 * scale), radius=[7 * scale])
            self.colour(WHITE, 0.85)
            Line(rectangle=(tower["x"] - 8 * scale, cap_y, width + 16 * scale, 17 * scale), width=1.1 * scale)

    def draw_crystal(self, crystal):
        scale = self.scale
        x = crystal["x"]
        y = crystal["y"] + sin(self.time * 5 + crystal["spin"]) * 5 * scale
        self.colour(AQUA, 0.12)
        Ellipse(pos=(x - 27 * scale, y - 27 * scale), size=(54 * scale, 54 * scale))
        PushMatrix()
        Translate(x, y)
        Rotate(angle=(self.time * 105 + crystal["spin"] * 57) % 360, origin=(0, 0))
        points = [0, 17 * scale, 12 * scale, 0, 0, -18 * scale, -12 * scale, 0]
        self.colour(AQUA, 0.78)
        Line(points=points, close=True, width=2.2 * scale)
        self.colour(VIOLET, 0.78)
        Line(points=[0, 17 * scale, 0, -18 * scale], width=1.3 * scale)
        self.colour(WHITE)
        Line(points=[-9 * scale, 0, 9 * scale, 0], width=1.1 * scale)
        PopMatrix()

    def draw_living_trail(self):
        """Render the flight path as a gently undulating, pulsing energy ribbon."""
        if len(self.flight_trail) < 2:
            return
        scale = self.scale
        primary, secondary = self.current_trail["colours"]
        trail_duration = 0.48
        ordered_points = list(reversed(self.flight_trail))

        # Three offset ribbons keep the energy in motion without obscuring play.
        for colour, alpha, width, phase in (
            (primary, 0.10, 18, 0.0),
            (secondary, 0.24, 8, 2.1),
            (primary, 0.84, 2.0, 4.2),
        ):
            points = []
            for index, (x, y, age) in enumerate(ordered_points):
                life = max(0, 1 - age / trail_duration)
                wave = sin(self.time * 12 - age * 19 + index * 0.58 + phase)
                offset = wave * (1.8 + self.flap_energy * 2.5) * life * scale
                points.extend((x, y + offset))
            self.colour(colour, alpha)
            Line(points=points, width=width * scale)

        # Bright pulses flow from the bird out through the ribbon like charged air.
        point_count = len(ordered_points) - 1
        for pulse_index in range(3):
            travel = 1 - ((self.time * 1.85 + pulse_index * 0.33) % 1)
            position = travel * point_count
            left_index = min(point_count - 1, int(position))
            blend = position - left_index
            tail_x, tail_y, tail_age = ordered_points[left_index]
            head_x, head_y, head_age = ordered_points[left_index + 1]
            x = tail_x + (head_x - tail_x) * blend
            y = tail_y + (head_y - tail_y) * blend
            life = max(0, 1 - (tail_age + (head_age - tail_age) * blend) / trail_duration)
            size = (3.0 + sin(self.time * 9 + pulse_index) * 0.8) * life * scale
            self.colour(WHITE if pulse_index == 1 else secondary, 0.78 * life)
            Ellipse(pos=(x - size / 2, y - size / 2), size=(size, size))

    def draw_bird(self, x=None, y=None, size=1.0, skin=None, preview=False):
        """Draw the equipped bird in-game or as an interactive shop preview."""
        scale = self.scale * size
        x = BIRD_X * self.scale if x is None else x
        y = self.bird_y if y is None else y
        skin = self.current_skin if skin is None else skin
        flutter = sin(self.wing_phase)
        flap_mix = (
            0.5 - sin(self.time * 4.5) * 0.5
            if preview
            else (0.5 - flutter * 0.5) * self.flap_energy
        )
        tilt = 0 if preview else self.bird_tilt + flutter * self.flap_energy * 0.8
        if preview:
            y += sin(self.time * 2.2) * 2.2 * scale
        else:
            # The wing's downstroke gives the body a small, natural lift.
            y -= flutter * self.flap_bounce * 2.4 * scale

        frames = self.skin_textures.get(skin["id"], {})
        up_texture = frames.get("up")
        down_texture = frames.get("down")
        tint = skin.get("tint", WHITE)
        if up_texture:
            art_width = BIRD_DRAW_WIDTH * scale
            PushMatrix()
            Translate(x, y)
            Rotate(angle=tilt, origin=(0, 0))
            # The eased sine mix turns two drawn wing poses into a fluid flap
            # with no hard sprite pop at the top or bottom of the beat.
            up_height = art_width * up_texture.height / max(up_texture.width, 1)
            self.colour(tint, 1 - flap_mix if down_texture else 1)
            Rectangle(
                texture=up_texture,
                pos=(-art_width * 0.60, -up_height * 0.50),
                size=(art_width, up_height),
            )
            if down_texture and flap_mix > 0:
                down_height = art_width * down_texture.height / max(down_texture.width, 1)
                self.colour(tint, flap_mix)
                Rectangle(
                    texture=down_texture,
                    pos=(-art_width * 0.60, -down_height * 0.50),
                    size=(art_width, down_height),
                )
            PopMatrix()
            return

        # A simple fallback keeps the game usable even if the image is accidentally moved.
        self.colour(primary)
        Ellipse(pos=(x - 28 * scale, y - 23 * scale), size=(54 * scale, 46 * scale))
        self.colour((0.43, 0.66, 1.0))
        Ellipse(pos=(x - 4 * scale, y - 15 * scale), size=(39 * scale, 39 * scale))
        self.colour((0.62, 0.40, 1.0))
        Ellipse(pos=(x - 46 * scale, y - 15 * scale), size=(38 * scale, 32 * scale))
        self.colour(WHITE)
        Ellipse(pos=(x + 11 * scale, y + 8 * scale), size=(12 * scale, 12 * scale))
        self.colour(DEEP_SPACE)
        Ellipse(pos=(x + 16 * scale, y + 11 * scale), size=(5 * scale, 5 * scale))
        self.colour(GOLD)
        Ellipse(pos=(x + 31 * scale, y - 1 * scale), size=(17 * scale, 9 * scale))

    def draw_gameplay_fx(self):
        """Keep feedback readable: a tiny score lift, a soft hit flash, then celebration."""
        scale = self.scale
        if self.impact_flash > 0:
            self.colour(PINK if self.state == "game_over" else WHITE, min(0.20, self.impact_flash * 0.42))
            Rectangle(pos=(0, 0), size=self.size)
        for burst in self.score_bursts:
            alpha = max(0, burst["life"] / 0.72)
            self.draw_label(burst["text"], burst["x"], burst["y"], 17, burst["colour"], alpha=alpha)
        if self.new_best_timer > 0:
            breathe = 1 + sin(self.time * 9) * 0.06
            self.draw_panel(
                self.width * 0.5 - 78 * scale * breathe,
                self.height * 0.735,
                156 * scale * breathe,
                29 * scale,
                GOLD,
                0.22,
            )
            self.draw_label("NEW BEST!", self.width / 2, self.height * 0.744, 15, GOLD)
        if self.achievement_timer > 0 and self.achievement_banner:
            self.draw_panel(
                self.width * 0.5 - 126 * scale,
                self.height * 0.682,
                252 * scale,
                25 * scale,
                AQUA,
                0.16,
            )
            self.draw_label(self.achievement_banner, self.width / 2, self.height * 0.690, 10, WHITE)

    def draw_hud(self):
        scale = self.scale
        top_y = self.height - 46 * scale - self.safe_top_padding
        self.draw_panel(12 * scale, top_y, 34 * scale, 29 * scale, AQUA, 0.08)
        self.draw_label("II", 29 * scale, self.height - 37 * scale - self.safe_top_padding, 11, WHITE)
        self.draw_label(str(self.score), self.width / 2, self.height - 64 * scale - self.safe_top_padding, 38, WHITE)
        self.draw_label("◆ " + str(self.crystal_bank), self.width - 39 * scale, self.height - 37 * scale - self.safe_top_padding, 12, AQUA, alpha=0.82)
        if self.state == "playing":
            self.hitboxes.append((12 * scale, top_y, 34 * scale, 29 * scale, "pause"))

    def draw_menu(self):
        center, scale = self.width / 2, self.scale
        top_y = self.height - 72 * scale - self.safe_top_padding
        self.draw_panel(12 * scale, top_y, 58 * scale, 28 * scale, VIOLET, 0.08)
        self.draw_label("SET", 41 * scale, top_y + 9 * scale, 9, WHITE)
        self.hitboxes.append((12 * scale, top_y, 58 * scale, 28 * scale, "settings"))
        self.draw_panel(self.width - 100 * scale, top_y, 80 * scale, 28 * scale, AQUA, 0.08)
        self.draw_label("◆ " + str(self.crystal_bank), self.width - 60 * scale, top_y + 8 * scale, 11, AQUA)
        self.draw_label("SKYPULSE", center, self.height * 0.780, 38, WHITE)
        self.draw_label("FLY THROUGH THE GLOW", center, self.height * 0.738, 10, AQUA, alpha=0.88)
        self.draw_bird(center, self.height * 0.555, size=0.76, preview=True)
        self.draw_label("BEST  •  " + str(self.best_score), center, self.height * 0.425, 12, WHITE, alpha=0.82)
        self.draw_action_button("FLY", center, self.height * 0.335, 210 * scale, 44 * scale, PINK, "play")
        self.draw_label("TAP ANYWHERE TO START", center, self.height * 0.295, 9, WHITE, alpha=0.62)
        self.draw_secondary_button("CUSTOMIZE", center, self.height * 0.210, 210 * scale, AQUA, "shop")
        self.draw_label(
            self.current_skin["name"] + " EQUIPPED",
            center,
            self.height * 0.145,
            9,
            self.current_skin["accent"],
            alpha=0.72,
        )
        self.draw_secondary_button("DAILY + MISSIONS", center, self.height * 0.050 + self.safe_bottom_padding, 178 * scale, VIOLET, "daily")

    def draw_splash(self):
        """A brief branded launch moment that also doubles as iOS splash artwork."""
        if self.launch_texture:
            self.colour(WHITE)
            Rectangle(texture=self.launch_texture, pos=(0, 0), size=self.size)
        else:
            self.colour(DEEP_SPACE)
            Rectangle(pos=(0, 0), size=self.size)
        center, scale = self.width / 2, self.scale
        self.colour(DEEP_SPACE, 0.20)
        Rectangle(pos=(0, 0), size=self.size)
        if self.app_icon_texture:
            icon_size = 94 * scale
            self.colour(WHITE)
            Rectangle(
                texture=self.app_icon_texture,
                pos=(center - icon_size / 2, self.height * 0.515),
                size=(icon_size, icon_size),
            )
        self.draw_label("SKYPULSE", center, self.height * 0.445, 28, WHITE)
        self.draw_label("FLY THROUGH THE GLOW", center, self.height * 0.405, 9, AQUA)

    def draw_shop_hub(self):
        """A real storefront: birds are separate from world and effects purchases."""
        center, scale = self.width / 2, self.scale
        self.draw_panel(self.width * 0.08, self.height * 0.105, self.width * 0.84, self.height * 0.79, AQUA, 0.18)
        self.draw_label("CUSTOMIZE", center, self.height * 0.810, 28, WHITE)
        self.draw_label("BUILD YOUR FLIGHT", center, self.height * 0.772, 10, AQUA)
        self.draw_panel(center - 55 * scale, self.height * 0.715, 110 * scale, 28 * scale, AQUA, 0.10)
        self.draw_label("◆ " + str(self.crystal_bank), center, self.height * 0.723, 12, AQUA)
        self.draw_action_button("BIRD SHOP", center, self.height * 0.600, 244 * scale, 39 * scale, GOLD, "shop_birds")
        self.draw_action_button("WORLD THEMES", center, self.height * 0.485, 244 * scale, 39 * scale, MINT, "themes")
        self.draw_action_button("TRAIL STYLES", center, self.height * 0.370, 244 * scale, 39 * scale, PINK, "trails")
        self.draw_action_button("PIPE COLOURS", center, self.height * 0.255, 244 * scale, 39 * scale, VIOLET, "pipes")
        self.draw_action_button("<  MENU", center, self.height * 0.135, 178 * scale, 36 * scale, VIOLET, "menu")

    def draw_style_card(self, item, category, x, y, width, height):
        """One purchase/equip card shared by theme, trail, and pipe collections."""
        ownership = {
            "theme": (self.unlocked_themes, self.equipped_theme_id),
            "trail": (self.unlocked_trails, self.equipped_trail_id),
            "pipe": (self.unlocked_pipes, self.equipped_pipe_id),
        }
        owned, equipped_id = ownership[category]
        is_equipped = item["id"] == equipped_id
        is_owned = item["id"] in owned
        scale = self.scale
        accent = item["accent"]
        self.draw_panel(x, y, width, height, accent, 0.24 if is_equipped else 0.12)

        preview_left = x + 22 * scale
        preview_y = y + height * 0.32
        if category == "theme":
            self.colour(item["tint"], 0.95)
            RoundedRectangle(pos=(preview_left, preview_y), size=(88 * scale, 40 * scale), radius=[8 * scale])
            self.colour(item["accent"], 0.72)
            Line(points=[preview_left, preview_y + 10 * scale, preview_left + 88 * scale, preview_y + 10 * scale], width=1.1 * scale)
            self.colour(item["sky_colours"][1], 0.75)
            Line(points=[preview_left, preview_y + 25 * scale, preview_left + 88 * scale, preview_y + 25 * scale], width=2 * scale)
        elif category == "trail":
            primary, secondary = item["colours"]
            for offset, colour, width_scale in ((-6, primary, 5), (0, secondary, 3), (6, primary, 1.2)):
                self.colour(colour, 0.88)
                Line(
                    points=[preview_left, preview_y + 18 * scale + offset * scale, preview_left + 88 * scale, preview_y + 18 * scale],
                    width=width_scale * scale,
                )
        else:
            self.colour(item["frame"], 0.80)
            RoundedRectangle(pos=(preview_left + 28 * scale, preview_y - 5 * scale), size=(32 * scale, 50 * scale), radius=[6 * scale])
            self.colour(item["panel"], 1)
            Rectangle(pos=(preview_left + 35 * scale, preview_y), size=(18 * scale, 40 * scale))
            self.colour(item["energy"], 0.95)
            Line(points=[preview_left + 44 * scale, preview_y + 3 * scale, preview_left + 44 * scale, preview_y + 37 * scale], width=3 * scale)
            self.colour(item["cap"], 0.95)
            Line(points=[preview_left + 20 * scale, preview_y + 44 * scale, preview_left + 68 * scale, preview_y + 44 * scale], width=5 * scale)

        text_x = x + width * 0.67
        self.draw_label(item["name"], text_x, y + height * 0.59, 15, accent)
        if is_equipped:
            status, status_colour = "EQUIPPED", WHITE
        elif is_owned:
            status, status_colour = "TAP TO EQUIP", AQUA
        else:
            status, status_colour = "◆ " + str(item["price"]), GOLD
        self.draw_label(status, text_x, y + height * 0.27, 10, status_colour)
        self.hitboxes.append((x, y, width, height, "style:" + category + ":" + item["id"]))

    def draw_style_collection(self, category):
        collections = {
            "theme": (THEMES, "WORLD THEMES", "CITY TREATMENTS"),
            "trail": (TRAILS, "TRAIL STYLES", "ENERGY IN MOTION"),
            "pipe": (PIPE_STYLES, "PIPE COLOURS", "MAKE EVERY RUN YOURS"),
        }
        items, heading, subtitle = collections[category]
        center, scale = self.width / 2, self.scale
        accent = items[0]["accent"]
        self.draw_panel(self.width * 0.055, self.height * 0.045, self.width * 0.89, self.height * 0.91, accent, 0.18)
        self.draw_label(heading, center, self.height * 0.855, 26, WHITE)
        self.draw_label(subtitle, center, self.height * 0.815, 10, accent)
        self.draw_panel(center - 57 * scale, self.height * 0.755, 114 * scale, 29 * scale, AQUA, 0.11)
        self.draw_label("◆ " + str(self.crystal_bank), center, self.height * 0.763, 13, AQUA)
        if len(items) <= 3:
            card_height = 102 * scale
            card_positions = (self.height * 0.575, self.height * 0.390, self.height * 0.205)
        else:
            card_height = 72 * scale
            card_positions = (
                self.height * 0.655,
                self.height * 0.530,
                self.height * 0.405,
                self.height * 0.280,
                self.height * 0.155,
            )
        card_width = 332 * scale
        for item, y in zip(items, card_positions):
            self.draw_style_card(item, category, center - card_width / 2, y, card_width, card_height)
        self.draw_action_button("<  SHOP", center, self.height * 0.095, 178 * scale, 35 * scale, VIOLET, "shop")

    def draw_settings(self):
        center, scale = self.width / 2, self.scale
        self.draw_panel(self.width * 0.11, self.height * 0.18, self.width * 0.78, self.height * 0.61, VIOLET, 0.22)
        self.draw_label("SETTINGS", center, self.height * 0.705, 28, WHITE)
        self.draw_label("MAKE FLIGHT FEEL RIGHT", center, self.height * 0.665, 10, AQUA)
        toggles = (
            ("SOUND", self.sound_enabled, AQUA, "sound"),
            ("HAPTICS", self.haptics_enabled, PINK, "haptics"),
            ("REDUCED MOTION", self.reduce_motion, MINT, "motion"),
        )
        for (label, enabled, colour, setting), y in zip(toggles, (self.height * 0.555, self.height * 0.465, self.height * 0.375)):
            self.draw_action_button(
                label + "  " + ("ON" if enabled else "OFF"),
                center,
                y,
                244 * scale,
                35 * scale,
                colour,
                "toggle:" + setting,
            )
        self.draw_action_button("<  MENU", center, self.height * 0.245, 178 * scale, 35 * scale, VIOLET, "menu")

    def draw_daily(self):
        """A compact retention screen: one daily score target and three visible missions."""
        center, scale = self.width / 2, self.scale
        daily = self.daily_state
        self.draw_panel(self.width * 0.055, self.height * 0.050, self.width * 0.89, self.height * 0.90, VIOLET, 0.20)
        self.draw_label("DAILY FLIGHT", center, self.height * 0.862, 26, WHITE)
        self.draw_label(daily["date"].replace("-", " · "), center, self.height * 0.823, 10, AQUA)
        self.draw_panel(center - 138 * scale, self.height * 0.674, 276 * scale, 92 * scale, GOLD, 0.15)
        self.draw_label("REACH SCORE " + str(daily["target"]), center, self.height * 0.744, 16, GOLD)
        self.draw_label("IN ONE DAILY FLIGHT", center, self.height * 0.719, 8, WHITE, alpha=0.78)
        self.draw_label("BEST  " + str(daily["best"]), center, self.height * 0.696, 9, WHITE, alpha=0.82)
        reward_label = "BONUS STYLE CLAIMED" if daily["reward_claimed"] else "BONUS COSMETIC UNLOCK"
        self.draw_label(reward_label, center, self.height * 0.679, 9, MINT if daily["reward_claimed"] else AQUA)
        self.draw_action_button("PLAY TODAY", center, self.height * 0.610, 216 * scale, 37 * scale, PINK, "daily_play")
        self.draw_label("TODAY'S MISSIONS", center, self.height * 0.548, 11, WHITE)
        card_width, card_height = 318 * scale, 55 * scale
        for mission, y in zip(MISSIONS, (self.height * 0.450, self.height * 0.350, self.height * 0.250)):
            progress = self.daily_state["missions"][mission["id"]]
            complete = mission["id"] in self.daily_state["completed"]
            colour = MINT if complete else AQUA
            self.draw_panel(center - card_width / 2, y, card_width, card_height, colour, 0.18 if complete else 0.10)
            self.draw_label(mission["name"], center - 47 * scale, y + 32 * scale, 10, WHITE)
            self.draw_label(mission["summary"], center - 47 * scale, y + 14 * scale, 8, WHITE, alpha=0.72)
            status = "DONE" if complete else str(progress) + " / " + str(mission["target"])
            self.draw_label(status, center + 112 * scale, y + 32 * scale, 10, colour)
            if not complete:
                progress_width = 154 * scale * min(1, progress / mission["target"])
                self.colour(colour, 0.58)
                Rectangle(pos=(center - 124 * scale, y + 13 * scale), size=(progress_width, 2 * scale))
            else:
                self.draw_label(mission["reward_label"] + " UNLOCKED", center, y + 10 * scale, 8, MINT)
        self.draw_label(
            "ACHIEVEMENTS  " + str(len(self.achievements)) + " / " + str(len(ACHIEVEMENTS)),
            center,
            self.height * 0.158,
            9,
            AQUA,
            alpha=0.82,
        )
        self.draw_action_button("<  MENU", center, self.height * 0.055 + self.safe_bottom_padding, 170 * scale, 32 * scale, VIOLET, "menu")

    def draw_backgrounds(self):
        center, scale = self.width / 2, self.scale
        self.draw_panel(self.width * 0.08, self.height * 0.12, self.width * 0.84, self.height * 0.74, AQUA, 0.20)
        self.draw_label("BACKGROUNDS", center, self.height * 0.765, 28, WHITE)
        self.draw_label("CITY SKY", center, self.height * 0.695, 12, AQUA)
        self.draw_panel(55 * scale, self.height * 0.345, 310 * scale, 205 * scale, VIOLET, 0.18)
        if self.backdrop_texture:
            self.colour(WHITE)
            Rectangle(
                texture=self.backdrop_texture,
                pos=(64 * scale, self.height * 0.360),
                size=(292 * scale, 148 * scale),
            )
            self.colour(AQUA, 0.72)
            Line(rectangle=(64 * scale, self.height * 0.360, 292 * scale, 148 * scale), width=1.0 * scale)
        self.draw_label("NEON CITY", center, self.height * 0.470, 20, WHITE)
        self.draw_label("EQUIPPED", center, self.height * 0.425, 11, (0.38, 0.96, 0.70))
        self.draw_action_button("<  MENU", center, self.height * 0.210, 185 * scale, 38 * scale, VIOLET, "menu")

    def draw_collection(self, hangar=False):
        center, scale = self.width / 2, self.scale
        accent = GOLD if hangar else AQUA
        heading = "BIRD HANGAR" if hangar else "BIRD SHOP"
        subtitle = "SELECT YOUR BIRD" if hangar else "UNLOCK WITH CRYSTALS"
        self.draw_panel(self.width * 0.035, self.height * 0.045, self.width * 0.93, self.height * 0.91, accent, 0.22)
        self.draw_label(heading, center, self.height * 0.855, 27, WHITE)
        self.draw_label(subtitle, center, self.height * 0.815, 11, accent)
        self.draw_panel(center - 63 * scale, self.height * 0.755, 126 * scale, 31 * scale, AQUA, 0.13)
        self.draw_label("◆  " + str(self.crystal_bank), center, self.height * 0.764, 14, AQUA)

        card_width, card_height = 178 * scale, 95 * scale
        positions = (
            (22 * scale, self.height * 0.600),
            (220 * scale, self.height * 0.600),
            (22 * scale, self.height * 0.460),
            (220 * scale, self.height * 0.460),
            (22 * scale, self.height * 0.320),
            (220 * scale, self.height * 0.320),
            (22 * scale, self.height * 0.180),
            (220 * scale, self.height * 0.180),
        )
        for skin, (x, y) in zip(SKINS, positions):
            self.draw_skin_card(skin, x, y, card_width, card_height, hangar=hangar)
        self.draw_action_button("<  MENU", center, self.height * 0.115, 178 * scale, 38 * scale, VIOLET, "menu")

    def draw_pause(self):
        center, scale = self.width / 2, self.scale
        self.draw_panel(self.width * 0.12, self.height * 0.275, self.width * 0.76, self.height * 0.42, VIOLET, 0.22)
        self.draw_label("PAUSED", center, self.height * 0.610, 36, WHITE)
        self.draw_label("TAKE A BREATH", center, self.height * 0.568, 10, AQUA)
        self.draw_action_button("RESUME", center, self.height * 0.490, 205 * scale, 38 * scale, AQUA, "resume")
        self.draw_action_button("RESTART", center, self.height * 0.425, 205 * scale, 34 * scale, PINK, "restart")
        self.draw_action_button("MENU", center, self.height * 0.365, 205 * scale, 32 * scale, VIOLET, "menu")
        self.draw_label("Press P to resume", center, self.height * 0.305, 9, WHITE, alpha=0.68)

    def draw_game_over(self):
        center, scale = self.width / 2, self.scale
        self.draw_panel(self.width * 0.10, self.height * 0.085, self.width * 0.80, self.height * 0.70, PINK, 0.22)
        self.draw_label("GAME OVER", center, self.height * 0.705, 31, PINK)
        self.draw_label("SCORE  " + str(self.score), center, self.height * 0.625, 20, WHITE)
        best_text = "NEW BEST  " + str(self.best_score) if self.new_best_this_run else "BEST SCORE  " + str(self.best_score)
        self.draw_label(best_text, center, self.height * 0.580, 14, GOLD if self.new_best_this_run else WHITE)
        self.draw_label("CRYSTALS  +" + str(self.crystals_collected), center, self.height * 0.535, 15, AQUA)
        if self.is_daily_run:
            daily_text = "DAILY STYLE UNLOCKED" if self.daily_reward_earned else "DAILY BEST  " + str(self.daily_state["best"])
            self.draw_label(daily_text, center, self.height * 0.495, 10, MINT if self.daily_reward_earned else VIOLET)
        if self.daily_rewards_this_run:
            rewards = self.daily_rewards_this_run[-3:]
            self.draw_panel(center - 132 * scale, self.height * 0.330, 264 * scale, 88 * scale, MINT, 0.15)
            self.draw_label("DAILY UNLOCKS", center, self.height * 0.405, 10, MINT)
            for index, (category, item_name) in enumerate(rewards):
                self.draw_label(
                    item_name + "  " + category,
                    center,
                    self.height * (0.380 - index * 0.025),
                    8,
                    WHITE,
                    alpha=0.88,
                )
            hidden_count = len(self.daily_rewards_this_run) - len(rewards)
            if hidden_count:
                self.draw_label("+" + str(hidden_count) + " MORE STYLE", center, self.height * 0.337, 8, MINT)
        self.draw_action_button("RETRY", center, self.height * 0.250, 220 * scale, 39 * scale, PINK, "daily_retry" if self.is_daily_run else "retry")
        self.draw_action_button("CUSTOMIZE", center, self.height * 0.190, 220 * scale, 34 * scale, AQUA, "shop")
        self.draw_action_button("MENU", center, self.height * 0.130, 220 * scale, 34 * scale, VIOLET, "menu")

    def draw_overlay(self):
        if self.state == "splash":
            self.draw_splash()
            return
        if self.state == "playing":
            return
        overlay_alpha = 0.36 if self.state == "menu" else 0.60
        self.colour(DEEP_SPACE, overlay_alpha)
        Rectangle(pos=(0, 0), size=self.size)
        if self.state == "menu":
            self.draw_menu()
        elif self.state == "shop":
            self.draw_shop_hub()
        elif self.state == "shop_birds":
            self.draw_collection()
        elif self.state == "hangar":
            self.draw_collection(hangar=True)
        elif self.state == "themes":
            self.draw_style_collection("theme")
        elif self.state == "trails":
            self.draw_style_collection("trail")
        elif self.state == "pipes":
            self.draw_style_collection("pipe")
        elif self.state == "settings":
            self.draw_settings()
        elif self.state == "daily":
            self.draw_daily()
        elif self.state == "backgrounds":
            self.draw_backgrounds()
        elif self.state == "paused":
            self.draw_pause()
        elif self.state == "game_over":
            self.draw_game_over()
        if self.notice_timer > 0:
            self.draw_label(self.notice, self.width / 2, self.height * 0.065, 12, PINK)

    def draw(self):
        self.hitboxes = []
        self.canvas.clear()
        with self.canvas:
            shake_x = random.uniform(-1, 1) * self.screen_shake * 22 * self.scale
            shake_y = random.uniform(-1, 1) * self.screen_shake * 16 * self.scale
            PushMatrix()
            Translate(shake_x, shake_y)
            self.draw_background()
            self.draw_runway()
            # Menus are clean, curated screens. Only an active or interrupted
            # flight keeps the live game world (bird, HUD, towers, and effects).
            if self.state in ("playing", "paused", "game_over"):
                for tower in self.towers:
                    self.draw_tower(tower)
                for crystal in self.crystals:
                    self.draw_crystal(crystal)
                self.draw_living_trail()
                for spark in self.sparks:
                    self.colour(spark["colour"], max(0, spark["life"]))
                    size = max(2, 7 * spark["life"]) * self.scale
                    Ellipse(pos=(spark["x"] - size / 2, spark["y"] - size / 2), size=(size, size))
                self.draw_bird()
            PopMatrix()
            if self.state in ("playing", "paused", "game_over"):
                self.draw_gameplay_fx()
                self.draw_hud()
            self.draw_overlay()

    def activate(self, action):
        """Run a simple visual-button action without a separate widget tree."""
        if action in ("daily_play", "daily_retry") or (action == "restart" and self.is_daily_run):
            self.start_daily()
        elif action in ("play", "retry", "restart"):
            self.reset("playing")
            self.lifetime["runs"] += 1
            self.flap()
        elif action == "menu":
            self.reset("menu")
        elif action == "shop":
            self.state = "shop"
        elif action == "shop_birds":
            self.state = "shop_birds"
        elif action == "hangar":
            self.state = "hangar"
        elif action == "themes":
            self.state = "themes"
        elif action == "trails":
            self.state = "trails"
        elif action == "pipes":
            self.state = "pipes"
        elif action == "settings":
            self.state = "settings"
        elif action == "daily":
            self.state = "daily"
        elif action == "backgrounds":
            self.state = "backgrounds"
        elif action == "resume":
            self.state = "playing"
        elif action == "pause":
            self.state = "paused"
        elif action.startswith("toggle:"):
            self.toggle_setting(action.split(":", 1)[1])
        elif action.startswith("skin:"):
            self.select_skin(action.split(":", 1)[1])
        elif action.startswith("style:"):
            _, category, item_id = action.split(":", 2)
            self.select_style(category, item_id)

    def select_skin(self, skin_id):
        skin = SKINS_BY_ID[skin_id]
        unlocked_now = False
        if skin_id in self.unlocked_skins:
            self.equipped_skin_id = skin_id
            self.notice = skin["name"] + " EQUIPPED"
        elif self.state == "shop_birds" and self.crystal_bank >= skin["price"]:
            self.crystal_bank -= skin["price"]
            self.unlocked_skins.append(skin_id)
            self.equipped_skin_id = skin_id
            self.notice = skin["name"] + " UNLOCKED"
            unlocked_now = True
        else:
            required = max(0, skin["price"] - self.crystal_bank)
            self.notice = "COLLECT " + str(required) + " MORE CRYSTALS"
            self.notice_timer = 1.7
            return
        self.notice_timer = 1.5
        if unlocked_now:
            self.unlock_achievement("style_icon")
        self.save_progress()

    def select_style(self, category, item_id):
        """Purchase or equip a trail, theme, or pipe skin from the style shop."""
        styles = {
            "theme": (THEMES_BY_ID, self.unlocked_themes, "equipped_theme_id"),
            "trail": (TRAILS_BY_ID, self.unlocked_trails, "equipped_trail_id"),
            "pipe": (PIPE_STYLES_BY_ID, self.unlocked_pipes, "equipped_pipe_id"),
        }
        catalog, unlocked, equipped_attribute = styles[category]
        item = catalog[item_id]
        unlocked_now = False
        if item_id in unlocked:
            setattr(self, equipped_attribute, item_id)
            self.notice = item["name"] + " EQUIPPED"
        elif self.crystal_bank >= item["price"]:
            self.crystal_bank -= item["price"]
            unlocked.append(item_id)
            setattr(self, equipped_attribute, item_id)
            self.notice = item["name"] + " UNLOCKED"
            unlocked_now = True
        else:
            self.notice = "COLLECT " + str(item["price"] - self.crystal_bank) + " MORE CRYSTALS"
            self.notice_timer = 1.7
            return
        self.notice_timer = 1.5
        if unlocked_now:
            self.unlock_achievement("style_icon")
        self.save_progress()

    def on_touch_down(self, touch):
        for x, y, width, height, action in reversed(self.hitboxes):
            if x <= touch.x <= x + width and y <= touch.y <= y + height:
                self.activate(action)
                return True
        if self.state == "playing":
            self.flap()
        elif self.state == "menu":
            self.start_or_flap()
        elif self.state == "paused":
            self.state = "playing"
        return True

    def on_key_down(self, _window, key, _scancode, _codepoint, _modifiers):
        if key in (32, 273):  # Space and Up Arrow
            self.start_or_flap()
        elif key == 112:  # P
            if self.state == "playing":
                self.state = "paused"
            elif self.state == "paused":
                self.state = "playing"
        return True


class SkyPulseApp(App):
    icon = str(Path(__file__).parent / "assets/images/branding/skypulse-app-icon.png")

    def build(self):
        self.title = "SkyPulse"
        return SkyPulseGame()


if __name__ == "__main__":
    SkyPulseApp().run()
