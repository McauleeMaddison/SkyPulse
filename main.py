"""SkyPulse — a touch-first arcade game made with Python + Kivy.

Click/tap anywhere or press Space / Up Arrow to fly.
Press P to pause or resume.
"""

import json
import random
from math import hypot, sin
from pathlib import Path

from kivy.config import Config

# A compact portrait window makes desktop testing feel like a phone game.
Config.set("graphics", "width", "420")
Config.set("graphics", "height", "760")
Config.set("graphics", "resizable", "1")

from kivy.app import App
from kivy.clock import Clock
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
    Scale,
    Translate,
)
from kivy.uix.widget import Widget

from settings import (
    AQUA,
    BIRD_DRAW_WIDTH,
    BIRD_HITBOX_HALF_HEIGHT,
    BIRD_HITBOX_HALF_WIDTH,
    BIRD_HITBOX_OFFSET_X,
    BIRD_HITBOX_OFFSET_Y,
    BIRD_X,
    DEEP_SPACE,
    FLAP_STRENGTH,
    GOLD,
    GRAVITY,
    PINK,
    TOWER_GAP,
    TOWER_SPAWN_SECONDS,
    TOWER_SPEED,
    TOWER_WIDTH,
    VIOLET,
    WHITE,
)


# Each bird is a real art asset with its own flight-light palette. Prices are
# intentionally small in this first build, so new players can unlock birds by
# simply playing rather than facing a long grind.
SKINS = (
    {
        "id": "nova",
        "name": "NOVA",
        "asset": "assets/images/characters/nova.png",
        "price": 0,
        "accent": VIOLET,
        "trail": (VIOLET, AQUA),
    },
    {
        "id": "lumen",
        "name": "LUMEN",
        "asset": "assets/images/characters/lumen.png",
        "price": 20,
        "accent": AQUA,
        "trail": (AQUA, VIOLET),
    },
    {
        "id": "ember",
        "name": "EMBER",
        "asset": "assets/images/characters/ember.png",
        "price": 55,
        "accent": PINK,
        "trail": (PINK, GOLD),
    },
    {
        "id": "sol",
        "name": "SOL",
        "asset": "assets/images/characters/sol.png",
        "price": 110,
        "accent": GOLD,
        "trail": (GOLD, AQUA),
    },
)

SKINS_BY_ID = {skin["id"]: skin for skin in SKINS}
SAVE_PATH = Path(__file__).parent / "skypulse_progress.json"


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
        self.skin_textures = {skin["id"]: self.load_texture(skin["asset"]) for skin in SKINS}
        self.progress = self.load_progress()
        self.best_score = self.progress["best_score"]
        self.crystal_bank = self.progress["crystal_bank"]
        self.unlocked_skins = self.progress["unlocked"]
        self.equipped_skin_id = self.progress["equipped"]
        self.hitboxes = []
        self.notice = ""
        self.notice_timer = 0
        self.reset("menu")
        Clock.schedule_interval(self.update, 1 / 60)

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

    @staticmethod
    def load_progress():
        default = {"best_score": 0, "crystal_bank": 0, "unlocked": ["nova"], "equipped": "nova"}
        try:
            saved = json.loads(SAVE_PATH.read_text())
            unlocked = [skin_id for skin_id in saved.get("unlocked", []) if skin_id in SKINS_BY_ID]
            if "nova" not in unlocked:
                unlocked.insert(0, "nova")
            equipped = saved.get("equipped", "nova")
            if equipped not in unlocked:
                equipped = "nova"
            return {
                "best_score": max(0, int(saved.get("best_score", 0))),
                "crystal_bank": max(0, int(saved.get("crystal_bank", 0))),
                "unlocked": unlocked,
                "equipped": equipped,
            }
        except (OSError, ValueError, TypeError):
            return default

    def save_progress(self):
        self.progress = {
            "best_score": self.best_score,
            "crystal_bank": self.crystal_bank,
            "unlocked": self.unlocked_skins,
            "equipped": self.equipped_skin_id,
        }
        try:
            SAVE_PATH.write_text(json.dumps(self.progress, indent=2) + "\n")
        except OSError:
            pass

    @property
    def current_skin(self):
        return SKINS_BY_ID[self.equipped_skin_id]

    @property
    def scale(self):
        """Keeps the same game proportions if you resize the desktop window."""
        return self.width / 420 if self.width else 1

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
        self.score = 0
        self.crystals_collected = 0
        self.towers = []
        self.crystals = []
        self.sparks = []
        self.flight_trail = []
        self.spawn_timer = 0.8
        self.trail_timer = 0
        self.screen_shake = 0
        self.time = 0

    def flap(self):
        if self.state != "playing":
            return
        self.bird_speed = FLAP_STRENGTH * self.scale
        primary, secondary = self.current_skin["trail"]
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
            {"x": self.width + 35 * self.scale, "gap_y": gap_y, "gap": gap, "passed": False}
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
        self.save_progress()
        self.screen_shake = 0.32
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

    def update(self, dt):
        self.time += dt
        self.screen_shake = max(0, self.screen_shake - dt)
        self.notice_timer = max(0, self.notice_timer - dt)
        self.sparks = [
            {
                **spark,
                "x": spark["x"] + spark.get("vx", -95 * self.scale) * dt,
                "y": spark["y"] + spark.get("vy", 0) * dt,
                "life": spark["life"] - dt,
            }
            for spark in self.sparks
            if spark["life"] > 0
        ]

        if self.state != "playing":
            self.draw()
            return

        scale = self.scale
        bird_x = BIRD_X * scale
        bird_hit_x, bird_hit_y, bird_half_width, bird_half_height = self.bird_collider()
        self.bird_speed -= GRAVITY * scale * dt
        self.bird_y += self.bird_speed * dt
        self.flight_trail.insert(0, (bird_x, self.bird_y))
        self.flight_trail = self.flight_trail[:16]
        self.spawn_timer -= dt
        self.trail_timer -= dt
        speed = (TOWER_SPEED + min(self.score, 25) * 4) * scale

        if self.trail_timer <= 0:
            self.trail_timer = 0.05
            primary, secondary = self.current_skin["trail"]
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

        for crystal in self.crystals:
            crystal["x"] -= speed * dt
            if hypot(crystal["x"] - bird_x, crystal["y"] - self.bird_y) < 35 * scale:
                crystal["x"] = -100
                self.crystals_collected += 1
                self.crystal_bank += 1
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

        self.towers = [tower for tower in self.towers if tower["x"] > -100 * scale]
        self.crystals = [crystal for crystal in self.crystals if crystal["x"] > -40 * scale]

        for tower in self.towers:
            gap_bottom = tower["gap_y"] - tower["gap"] / 2
            gap_top = tower["gap_y"] + tower["gap"] / 2
            if self.ellipse_hits_rectangle(
                bird_hit_x,
                bird_hit_y,
                bird_half_width,
                bird_half_height,
                tower["x"],
                self.ground_y,
                TOWER_WIDTH * scale,
                gap_bottom - self.ground_y,
            ) or self.ellipse_hits_rectangle(
                bird_hit_x,
                bird_hit_y,
                bird_half_width,
                bird_half_height,
                tower["x"],
                gap_top,
                TOWER_WIDTH * scale,
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

    def draw_stat_chip(self, label, value, center_x, y, width, accent):
        """A compact information tile used where a full button would be misleading."""
        scale = self.scale
        height = 45 * scale
        self.draw_panel(center_x - width / 2, y, width, height, accent, 0.10)
        self.draw_label(label, center_x, y + 25 * scale, 8, WHITE, alpha=0.72)
        self.draw_label(value, center_x, y + 8 * scale, 13, accent)

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
        """A non-rectangular, softly pulsing arcade-style call-to-action."""
        scale = self.scale
        pulse = 0.13 + sin(self.time * 3.0) * 0.035
        left = center_x - width / 2
        self.colour(accent, pulse)
        RoundedRectangle(
            pos=(left - 7 * scale, y - 7 * scale),
            size=(width + 14 * scale, height + 14 * scale),
            radius=[height * 0.46],
        )
        self.colour((0.14, 0.035, 0.30), 0.92)
        RoundedRectangle(pos=(left, y), size=(width, height), radius=[height * 0.40])
        self.colour(accent, 0.95)
        Line(rectangle=(left, y, width, height), width=1.35 * scale)
        self.colour(accent, 0.72)
        Line(
            points=[left + 16 * scale, y + height / 2, left + 22 * scale, y + height / 2 + 5 * scale,
                    left + 16 * scale, y + height / 2 + 10 * scale],
            width=max(0.8, 1.1 * scale),
        )
        self.draw_label(text, center_x, y + height * 0.30, 15, WHITE)
        if action:
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

    def draw_background(self):
        """Animated sky, city parallax, horizon glow, and a perspective runway."""
        if self.backdrop_texture:
            self.colour(WHITE)
            Rectangle(texture=self.backdrop_texture, pos=(0, 0), size=self.size)
            # A thin tint makes the artwork sit naturally with the game's neon effects.
            self.colour(DEEP_SPACE, 0.13)
            Rectangle(pos=(0, 0), size=self.size)
        else:
            stripe_height = max(4, int(self.height / 76))
            for y in range(0, int(self.height), stripe_height):
                level = y / max(self.height, 1)
                self.colour((0.035 + level * 0.035, 0.008 + level * 0.013, 0.125 + level * 0.18))
                Rectangle(pos=(0, y), size=(self.width, stripe_height + 1))

        # A separate moving star layer sells motion even while the world art stays crisp.
        for index in range(4):
            drift = (self.time * (10 + index * 3) + index * 133) % (self.width + 180) - 90
            cloud_y = self.height * (0.38 + (index % 3) * 0.17)
            cloud_size = (120 + index * 35) * self.scale
            self.colour(VIOLET if index % 2 else PINK, 0.025)
            Ellipse(pos=(drift - cloud_size / 2, cloud_y - cloud_size / 5), size=(cloud_size, cloud_size / 2.5))

        for x, y, radius in self.stars:
            shimmer = 0.55 + sin(self.time * 2 + x * 12) * 0.2
            star_x = (x * self.width - self.time * (7 + radius * 4) * self.scale) % self.width
            self.colour(AQUA if radius == 2 else WHITE, shimmer)
            Ellipse(pos=(star_x, y * self.height), size=(radius * 2, radius * 2))

        # A breathing horizon glow is layered over the cinematic city illustration.
        glow_center_x = self.width * 0.50
        glow_center_y = self.height * 0.105
        horizon_breath = sin(self.time * 1.7) * 0.025
        for multiplier, alpha, colour in ((1.20, 0.020, VIOLET), (0.90, 0.045 + horizon_breath, PINK), (0.58, 0.08, GOLD)):
            diameter = self.width * multiplier
            self.colour(colour, alpha)
            Ellipse(pos=(glow_center_x - diameter / 2, glow_center_y - diameter / 2), size=(diameter, diameter))

        # A scan line moves inside the runway, never crossing its collision rail.
        scan_y = self.ground_y * (0.16 + (self.time * 0.52 % 0.74))
        self.colour(AQUA, 0.34)
        Line(points=[0, scan_y, self.width, scan_y], width=1.1 * self.scale)
        for index in range(3):
            phase = (self.time * (0.15 + index * 0.04) + index * 0.34) % 1
            comet_x = self.width * (1 - phase) + 40 * self.scale
            comet_y = self.height * (0.72 + index * 0.07)
            self.colour(AQUA if index % 2 else PINK, 0.18)
            Line(
                points=[comet_x, comet_y, comet_x + 34 * self.scale, comet_y + 12 * self.scale],
                width=max(0.7, 1.2 * self.scale),
            )

    def draw_runway(self):
        """Draw the playable floor so its bright edge exactly matches ground collision."""
        scale = self.scale
        floor_y = self.ground_y
        horizon_x = self.width * 0.50

        # A soft darkening gives the physical runway a distinct surface while
        # preserving the illustrated reflection beneath it.
        self.colour(DEEP_SPACE, 0.20)
        Rectangle(pos=(0, 0), size=(self.width, floor_y))
        for index, alpha in enumerate((0.025, 0.040, 0.070)):
            glow_height = (12 + index * 13) * scale
            self.colour(AQUA if index != 1 else VIOLET, alpha)
            Rectangle(
                pos=(0, max(0, floor_y - glow_height)),
                size=(self.width, glow_height),
            )

        # Perspective rails make the landing plane read at a glance on a phone.
        for index, ratio in enumerate((0.11, 0.28, 0.72, 0.89)):
            end_x = self.width * ratio
            self.colour(AQUA if index in (1, 2) else VIOLET, 0.15)
            Line(
                points=[horizon_x + (end_x - horizon_x) * 0.16, floor_y - 2 * scale, end_x, 0],
                width=max(0.55, 0.8 * scale),
            )
        for ratio, alpha in ((0.25, 0.07), (0.55, 0.11), (0.80, 0.16)):
            y = floor_y * ratio
            self.colour(VIOLET, alpha)
            Line(points=[0, y, self.width, y], width=max(0.45, 0.7 * scale))

        # This is the collision line: its top edge is the precise floor limit.
        pulse = 0.72 + sin(self.time * 3.2) * 0.12
        self.colour(AQUA, 0.10)
        Rectangle(pos=(0, floor_y - 5 * scale), size=(self.width, 10 * scale))
        self.colour(VIOLET, 0.78)
        Line(points=[0, floor_y - 1.1 * scale, self.width, floor_y - 1.1 * scale], width=3.5 * scale)
        self.colour(AQUA, pulse)
        Line(points=[0, floor_y, self.width, floor_y], width=1.15 * scale)
        self.colour(WHITE, 0.64)
        Line(points=[0, floor_y + 1.2 * scale, self.width, floor_y + 1.2 * scale], width=0.48 * scale)

    def draw_tower(self, tower):
        scale = self.scale
        width = TOWER_WIDTH * scale
        gap_bottom = tower["gap_y"] - tower["gap"] / 2
        gap_top = tower["gap_y"] + tower["gap"] / 2
        sections = ((self.ground_y, gap_bottom - self.ground_y, True), (gap_top, self.height - gap_top, False))
        for y, height, cap_on_top in sections:
            if height <= 0:
                continue
            pulse = 0.55 + sin(self.time * 5 + tower["x"] * 0.025) * 0.20
            self.colour(AQUA, 0.055)
            RoundedRectangle(
                pos=(tower["x"] - 10 * scale, y - 5 * scale),
                size=(width + 20 * scale, height + 10 * scale),
                radius=[12 * scale],
            )
            self.colour((0.025, 0.035, 0.19), 0.98)
            RoundedRectangle(pos=(tower["x"], y), size=(width, height), radius=[8 * scale])
            self.colour(VIOLET, 0.55)
            Rectangle(pos=(tower["x"] + 9 * scale, y + 3 * scale), size=(width - 18 * scale, max(0, height - 6 * scale)))
            self.colour((0.04, 0.18, 0.48), 0.95)
            Rectangle(pos=(tower["x"] + 16 * scale, y + 6 * scale), size=(width - 32 * scale, max(0, height - 12 * scale)))
            self.colour(AQUA, pulse)
            Rectangle(pos=(tower["x"] + 29 * scale, y + 8 * scale), size=(7 * scale, max(0, height - 16 * scale)))
            energy_y = y + (self.time * 145 * scale) % max(height, 1)
            self.colour(WHITE, 0.65)
            Rectangle(pos=(tower["x"] + 15 * scale, energy_y), size=(width - 30 * scale, 3 * scale))
            self.colour(AQUA, 0.92)
            Line(rectangle=(tower["x"], y, width, height), width=1.4 * scale)
            cap_y = y + height - 14 * scale if cap_on_top else y
            self.colour(GOLD, 0.17)
            RoundedRectangle(
                pos=(tower["x"] - 12 * scale, cap_y - 4 * scale),
                size=(width + 24 * scale, 25 * scale),
                radius=[9 * scale],
            )
            self.colour(GOLD)
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

    def draw_bird(self, x=None, y=None, size=1.0, skin=None, preview=False):
        """Draw the equipped bird in-game or as an interactive shop preview."""
        scale = self.scale * size
        x = BIRD_X * self.scale if x is None else x
        y = self.bird_y if y is None else y
        skin = self.current_skin if skin is None else skin
        primary, secondary = skin["trail"]
        wing_breath = 1 + sin(self.time * 10) * (0.018 if preview else 0.035)
        tilt = 0 if preview else max(-22, min(22, self.bird_speed / max(self.scale, 0.01) * 0.052))

        # Layered bloom makes every skin feel luminous without obscuring its silhouette.
        self.colour(primary, 0.075)
        Ellipse(pos=(x - 72 * scale, y - 55 * scale), size=(144 * scale, 110 * scale))
        self.colour(secondary, 0.10)
        Ellipse(pos=(x - 50 * scale, y - 37 * scale), size=(100 * scale, 74 * scale))

        texture = self.skin_textures.get(skin["id"])
        if texture:
            art_width = BIRD_DRAW_WIDTH * scale
            art_height = art_width * texture.height / max(texture.width, 1)
            PushMatrix()
            Translate(x, y)
            Rotate(angle=tilt, origin=(0, 0))
            Scale(wing_breath, wing_breath, 1)
            self.colour(WHITE)
            Rectangle(
                texture=texture,
                pos=(-art_width * 0.60, -art_height * 0.50),
                size=(art_width, art_height),
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

    def draw_hud(self):
        scale = self.scale
        top_y = self.height - 49 * scale
        self.draw_panel(11 * scale, top_y, 47 * scale, 33 * scale, AQUA, 0.12)
        self.draw_panel(self.width - 108 * scale, top_y, 97 * scale, 33 * scale, AQUA, 0.12)
        self.draw_label("II", 34.5 * scale, self.height - 39 * scale, 12, WHITE)
        self.draw_panel(self.width / 2 - 35 * scale, self.height - 70 * scale, 70 * scale, 42 * scale, VIOLET, 0.08)
        self.draw_label(str(self.score), self.width / 2, self.height - 62 * scale, 34, WHITE)
        self.draw_label("◆ " + str(self.crystal_bank), self.width - 59 * scale, self.height - 39 * scale, 13, AQUA)
        if self.state == "playing":
            self.hitboxes.append((11 * scale, top_y, 47 * scale, 33 * scale, "pause"))

    def draw_menu(self):
        center, scale = self.width / 2, self.scale
        self.draw_panel(self.width * 0.035, self.height * 0.025, self.width * 0.93, self.height * 0.95, VIOLET, 0.20)
        self.draw_panel(20 * scale, self.height * 0.895, 79 * scale, 30 * scale, VIOLET, 0.17)
        self.draw_label("ARCADE", 59 * scale, self.height * 0.904, 9, VIOLET)
        self.draw_panel(self.width - 116 * scale, self.height * 0.895, 96 * scale, 30 * scale, AQUA, 0.15)
        self.draw_label("◆ " + str(self.crystal_bank), self.width - 68 * scale, self.height * 0.904, 13, AQUA)
        self.draw_label("SKYPULSE", center, self.height * 0.765, 40, WHITE)
        self.colour(PINK, 0.62)
        Line(points=[center - 92 * scale, self.height * 0.746, center - 39 * scale, self.height * 0.746], width=1.1 * scale)
        Line(points=[center + 39 * scale, self.height * 0.746, center + 92 * scale, self.height * 0.746], width=1.1 * scale)
        self.draw_label("TAP TO FLY  •  SURVIVE THE GLOW", center, self.height * 0.717, 10, AQUA)
        self.draw_stat_chip("BEST SCORE", str(self.best_score), center - 58 * scale, self.height * 0.635, 104 * scale, VIOLET)
        self.draw_stat_chip("CRYSTALS", "◆ " + str(self.crystal_bank), center + 58 * scale, self.height * 0.635, 104 * scale, AQUA)
        self.draw_bird(center, self.height * 0.515, size=0.66, preview=True)
        self.draw_action_button("PLAY", center, self.height * 0.350, 246 * scale, 42 * scale, PINK, "play")
        self.draw_action_button("SHOP", center, self.height * 0.279, 246 * scale, 38 * scale, AQUA, "shop")
        self.draw_action_button("CUSTOMIZE", center, self.height * 0.216, 246 * scale, 38 * scale, GOLD, "hangar")
        self.draw_action_button("WORLD", center, self.height * 0.153, 246 * scale, 38 * scale, (0.38, 0.96, 0.70), "backgrounds")
        self.draw_label(
            self.current_skin["name"] + " EQUIPPED",
            center,
            self.height * 0.083,
            10,
            self.current_skin["accent"],
            alpha=0.85,
        )

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
        heading = "BIRD HANGAR" if hangar else "SKYPULSE SHOP"
        subtitle = "SELECT YOUR BIRD" if hangar else "UNLOCK WITH CRYSTALS"
        self.draw_panel(self.width * 0.035, self.height * 0.045, self.width * 0.93, self.height * 0.91, accent, 0.22)
        self.draw_label(heading, center, self.height * 0.855, 27, WHITE)
        self.draw_label(subtitle, center, self.height * 0.815, 11, accent)
        self.draw_panel(center - 63 * scale, self.height * 0.755, 126 * scale, 31 * scale, AQUA, 0.13)
        self.draw_label("◆  " + str(self.crystal_bank), center, self.height * 0.764, 14, AQUA)

        card_width, card_height = 178 * scale, 155 * scale
        positions = (
            (22 * scale, self.height * 0.515),
            (220 * scale, self.height * 0.515),
            (22 * scale, self.height * 0.290),
            (220 * scale, self.height * 0.290),
        )
        for skin, (x, y) in zip(SKINS, positions):
            self.draw_skin_card(skin, x, y, card_width, card_height, hangar=hangar)
        self.draw_action_button("<  MENU", center, self.height * 0.115, 178 * scale, 38 * scale, VIOLET, "menu")

    def draw_pause(self):
        center, scale = self.width / 2, self.scale
        self.draw_panel(self.width * 0.12, self.height * 0.35, self.width * 0.76, self.height * 0.30, VIOLET, 0.22)
        self.draw_label("PAUSED", center, self.height * 0.55, 38, WHITE)
        self.draw_action_button("RESUME", center, self.height * 0.445, 205 * scale, 40 * scale, AQUA, "resume")
        self.draw_label("Press P or tap resume", center, self.height * 0.385, 12, WHITE)

    def draw_game_over(self):
        center, scale = self.width / 2, self.scale
        self.draw_panel(self.width * 0.10, self.height * 0.145, self.width * 0.80, self.height * 0.61, PINK, 0.22)
        self.draw_label("GAME OVER", center, self.height * 0.665, 31, PINK)
        self.draw_label("SCORE  " + str(self.score), center, self.height * 0.575, 20, WHITE)
        self.draw_label("BEST SCORE  " + str(self.best_score), center, self.height * 0.525, 14, WHITE)
        self.draw_label("CRYSTALS  +" + str(self.crystals_collected), center, self.height * 0.470, 15, AQUA)
        self.draw_action_button("RETRY", center, self.height * 0.365, 220 * scale, 39 * scale, PINK, "retry")
        self.draw_action_button("SHOP", center, self.height * 0.300, 220 * scale, 36 * scale, AQUA, "shop")
        self.draw_action_button("MENU", center, self.height * 0.240, 220 * scale, 36 * scale, VIOLET, "menu")

    def draw_overlay(self):
        if self.state == "playing":
            return
        overlay_alpha = 0.44 if self.state == "menu" else 0.60
        self.colour(DEEP_SPACE, overlay_alpha)
        Rectangle(pos=(0, 0), size=self.size)
        if self.state == "menu":
            self.draw_menu()
        elif self.state == "shop":
            self.draw_collection()
        elif self.state == "hangar":
            self.draw_collection(hangar=True)
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
                if len(self.flight_trail) > 1:
                    primary, secondary = self.current_skin["trail"]
                    trail_points = [coordinate for point in reversed(self.flight_trail) for coordinate in point]
                    self.colour(primary, 0.12)
                    Line(points=trail_points, width=16 * self.scale)
                    self.colour(secondary, 0.26)
                    Line(points=trail_points, width=8 * self.scale)
                    self.colour(primary, 0.80)
                    Line(points=trail_points, width=2.1 * self.scale)
                for spark in self.sparks:
                    self.colour(spark["colour"], max(0, spark["life"]))
                    size = max(2, 7 * spark["life"]) * self.scale
                    Ellipse(pos=(spark["x"] - size / 2, spark["y"] - size / 2), size=(size, size))
                self.draw_bird()
            PopMatrix()
            if self.state in ("playing", "paused", "game_over"):
                self.draw_hud()
            self.draw_overlay()

    def activate(self, action):
        """Run a simple visual-button action without a separate widget tree."""
        if action in ("play", "retry"):
            self.reset("playing")
            self.flap()
        elif action == "menu":
            self.reset("menu")
        elif action == "shop":
            self.state = "shop"
        elif action == "hangar":
            self.state = "hangar"
        elif action == "backgrounds":
            self.state = "backgrounds"
        elif action == "resume":
            self.state = "playing"
        elif action == "pause":
            self.state = "paused"
        elif action.startswith("skin:"):
            self.select_skin(action.split(":", 1)[1])

    def select_skin(self, skin_id):
        skin = SKINS_BY_ID[skin_id]
        if skin_id in self.unlocked_skins:
            self.equipped_skin_id = skin_id
            self.notice = skin["name"] + " EQUIPPED"
        elif self.state == "shop" and self.crystal_bank >= skin["price"]:
            self.crystal_bank -= skin["price"]
            self.unlocked_skins.append(skin_id)
            self.equipped_skin_id = skin_id
            self.notice = skin["name"] + " UNLOCKED"
        else:
            required = max(0, skin["price"] - self.crystal_bank)
            self.notice = "COLLECT " + str(required) + " MORE CRYSTALS"
            self.notice_timer = 1.7
            return
        self.notice_timer = 1.5
        self.save_progress()

    def on_touch_down(self, touch):
        for x, y, width, height, action in reversed(self.hitboxes):
            if x <= touch.x <= x + width and y <= touch.y <= y + height:
                self.activate(action)
                return True
        if self.state == "playing":
            self.flap()
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
    def build(self):
        self.title = "SkyPulse"
        return SkyPulseGame()


if __name__ == "__main__":
    SkyPulseApp().run()
