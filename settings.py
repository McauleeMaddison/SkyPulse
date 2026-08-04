"""SkyPulse settings — these are the safest values to experiment with first."""

# Flight is tuned as a responsive arc: one decisive flap, a soft apex, then a
# capped dive. These values are all in game-pixels per second.
GRAVITY = 1_100
FLAP_STRENGTH = 405
FLAP_REBOUND = 95
MAX_RISE_SPEED = 455
MAX_FALL_SPEED = 540

# Obstacles
TOWER_SPEED = 180
TOWER_GAP = 225
TOWER_SPAWN_SECONDS = 1.55
TOWER_WIDTH = 58

# Bird placement, art size, and a body-only collision ellipse.
# The art includes wide wings, feet, and a light trail, so collision follows the
# luminous core rather than the decorative silhouette.
BIRD_X = 105
BIRD_DRAW_WIDTH = 125
BIRD_HITBOX_HALF_WIDTH = 17
BIRD_HITBOX_HALF_HEIGHT = 18
BIRD_HITBOX_OFFSET_X = 16
BIRD_HITBOX_OFFSET_Y = 0

# Colours are red, green, blue values from 0 to 1.
DEEP_SPACE = (0.02, 0.01, 0.10)
VIOLET = (0.54, 0.33, 1.0)
PINK = (1.0, 0.35, 0.76)
AQUA = (0.27, 0.93, 1.0)
WHITE = (0.94, 0.98, 1.0)
GOLD = (1.0, 0.75, 0.29)
