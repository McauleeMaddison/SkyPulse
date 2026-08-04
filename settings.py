"""SkyPulse settings — these are the safest values to experiment with first."""

# Movement (increase gravity to fall faster; increase flap strength to rise higher)
GRAVITY = 1_400
FLAP_STRENGTH = 455

# Obstacles
TOWER_SPEED = 180
TOWER_GAP = 225
TOWER_SPAWN_SECONDS = 1.55
TOWER_WIDTH = 58

# Bird placement, art size, and a forgiving body-only collision box.
# The art includes big wings and a light trail; neither should clip a tower.
BIRD_X = 105
BIRD_DRAW_WIDTH = 125
BIRD_HITBOX_HALF_WIDTH = 14
BIRD_HITBOX_HALF_HEIGHT = 13
BIRD_HITBOX_OFFSET_X = 7

# Colours are red, green, blue values from 0 to 1.
DEEP_SPACE = (0.02, 0.01, 0.10)
VIOLET = (0.54, 0.33, 1.0)
PINK = (1.0, 0.35, 0.76)
AQUA = (0.27, 0.93, 1.0)
WHITE = (0.94, 0.98, 1.0)
GOLD = (1.0, 0.75, 0.29)
