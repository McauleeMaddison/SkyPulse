using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    /// <summary>
    /// Native, portrait-first SkyPulse presentation and flight loop.  This deliberately
    /// uses a small fixed pool of renderers: the game stays smooth on older phones while
    /// retaining the layered, neon look of the web beta.
    /// </summary>
    public sealed class SkyPulseNativeGame : MonoBehaviour
    {
        private enum FlightState { Menu, Playing, Impact, Paused, GameOver, Customize }
        // Authored gameplay art has a single, input-driven stroke. Keeping this
        // explicit prevents a resting bird from advancing frames on its own.
        private enum GameplayWingState { Settled, Upstroke, Downstroke }
        private enum CosmeticCategory { Birds, Worlds, Pipes, Upgrades }
        // These are tactical pickup effects only.  The permanent economy is kept
        // deliberately separate so no purchase can change score potential or
        // flight handling.
        private enum PowerUpKind { Aegis, TimePulse, CrystalMagnet }
        private enum PendingPurchase { None, Skin, Upgrade }

        /// <summary>
        /// One place for all values that influence the way a flight feels. Keeping the
        /// values together makes a play-test change deliberate and preserves the same
        /// handling regardless of the selected bird.
        /// </summary>
        private sealed class FlightTuning
        {
            public readonly float Gravity;
            public readonly float FlapVelocity;
            public readonly float MaxFallVelocity;
            public readonly float CollisionRadius;
            public readonly float PerfectPassWindow;
            public readonly float InputBufferSeconds;

            public FlightTuning(
                float gravity, float flapVelocity, float maxFallVelocity,
                float collisionRadius, float perfectPassWindow, float inputBufferSeconds)
            {
                Gravity = gravity;
                FlapVelocity = flapVelocity;
                MaxFallVelocity = maxFallVelocity;
                CollisionRadius = collisionRadius;
                PerfectPassWindow = perfectPassWindow;
                InputBufferSeconds = inputBufferSeconds;
            }
        }

        private sealed class Skin
        {
            public string Id;
            public string Name;
            public string ArtPath;
            public string FlapPath;
            public string RisePath;
            // Every launch bird supplies these six flight positions in sequence:
            // raised, lift, high glide, neutral glide, low glide, downstroke.
            // Keeping the list on the skin makes adding a future bird a data-and-art
            // task, not a risky change to the flight loop.
            public string[] FlapFramePaths;
            // Optional, authored-frame registration pivots. They correct source
            // canvas placement once when sprites are loaded; gameplay never moves
            // the bird artwork transform from frame to frame.
            public Vector2[] FlapFramePivots;
            // These are distinct, per-bird poses. They must never point at a shared
            // bird image: its plumage, metalwork and silhouette are part of the
            // reward the player just earned.
            public string HitPath;
            public string UnlockPath;
            public Color Accent;
            public Color Trail;
            public int Price;

            public Skin(string id, string name, string artPath, string flapPath, string accent, string trail, int price, string risePath = null, string hitPath = null, string unlockPath = null, string[] flapFramePaths = null, Vector2[] flapFramePivots = null)
            {
                Id = id;
                Name = name;
                ArtPath = artPath;
                FlapPath = flapPath;
                RisePath = risePath;
                HitPath = hitPath;
                UnlockPath = unlockPath;
                FlapFramePaths = flapFramePaths;
                FlapFramePivots = flapFramePivots;
                Accent = Hex(accent);
                Trail = Hex(trail);
                Price = price;
            }
        }
        private sealed class BirdHangarProfile
        {
            public readonly string Rarity;
            public readonly string Description;
            public readonly int Speed;
            public readonly int Maneuverability;
            public readonly int Stability;
            public readonly Color RarityColour;

            public BirdHangarProfile(
                string rarity,
                string description,
                int speed,
                int maneuverability,
                int stability,
                Color rarityColour)
            {
                Rarity = rarity;
                Description = description;
                Speed = Mathf.Clamp(speed, 1, 5);
                Maneuverability = Mathf.Clamp(maneuverability, 1, 5);
                Stability = Mathf.Clamp(stability, 1, 5);
                RarityColour = rarityColour;
            }
        }
        [Serializable]
        private sealed class BirdPoseBounds
        {
            public string path;
            public int x, y, width, height;
        }
        [Serializable]
        private sealed class BirdPoseBoundsFile
        {
            public BirdPoseBounds[] frames;
        }
        private readonly Dictionary<string, BirdPoseBounds> poseBounds = new Dictionary<string, BirdPoseBounds>();
        private bool poseBoundsLoaded;

        [Serializable]
        private sealed class BirdFrameRegistration
        {
            public string path;
            public float pivotX;
            public float pivotY;
            public float scale = 1f;
        }

        [Serializable]
        private sealed class BirdFrameRegistrationFile
        {
            public BirdFrameRegistration[] frames;
        }

        private sealed class WorldTheme
        {
            public string Id;
            public string Name;
            public string BackgroundPath;
            public Color Accent;
            public Color Floor;
            public string DifficultyLabel;
            public float ScrollMultiplier;
            public float GapSize;
            public string PresetPipeId;
            public string PresetTrailId;

            public WorldTheme(string id, string name, string backgroundPath, string accent, string floor, string difficultyLabel, float scrollMultiplier, float gapSize, string presetPipeId, string presetTrailId)
            {
                Id = id;
                Name = name;
                BackgroundPath = backgroundPath;
                Accent = Hex(accent);
                Floor = Hex(floor);
                DifficultyLabel = difficultyLabel;
                ScrollMultiplier = scrollMultiplier;
                GapSize = gapSize;
                PresetPipeId = presetPipeId;
                PresetTrailId = presetTrailId;
            }
        }

        private sealed class Upgrade
        {
            public string Id;
            public string Name;
            public string[] LevelEffects;
            public int[] LevelPrices;
            public Color Accent;
            public string Branch;
            public string PrerequisiteId;
            public int PrerequisiteLevel;
            public int Tier;

            public Upgrade(
                string id,
                string name,
                string[] levelEffects,
                int[] levelPrices,
                string accent,
                string branch = "COLLECTION",
                string prerequisiteId = null,
                int prerequisiteLevel = 0,
                int tier = 1)
            {
                Id = id;
                Name = name;
                LevelEffects = levelEffects;
                LevelPrices = levelPrices;
                Accent = Hex(accent);
                Branch = branch;
                PrerequisiteId = prerequisiteId;
                PrerequisiteLevel = prerequisiteLevel;
                Tier = Mathf.Max(1, tier);
            }

            public int MaxLevel => Mathf.Min(LevelEffects == null ? 0 : LevelEffects.Length, LevelPrices == null ? 0 : LevelPrices.Length);
            public bool HasPrerequisite => !string.IsNullOrEmpty(PrerequisiteId) && PrerequisiteLevel > 0;

            public int PriceAtLevel(int currentLevel)
            {
                return currentLevel >= 0 && currentLevel < MaxLevel ? LevelPrices[currentLevel] : 0;
            }

            public string EffectAtLevel(int currentLevel)
            {
                return currentLevel >= 0 && currentLevel < MaxLevel ? LevelEffects[currentLevel] : string.Empty;
            }
        }

        private sealed class TrailStyle
        {
            public string Id;
            public string Name;
            public Color Core;
            public Color Glow;

            public TrailStyle(string id, string name, string core, string glow)
            {
                Id = id;
                Name = name;
                Core = Hex(core);
                Glow = Hex(glow);
            }
        }

        private sealed class PipeStyle
        {
            public string Id;
            public string Name;
            public Color Accent;
            public Color Panel;
            public Color Energy;

            public PipeStyle(string id, string name, string accent, string panel, string energy)
            {
                Id = id;
                Name = name;
                Accent = Hex(accent);
                Panel = Hex(panel);
                Energy = Hex(energy);
            }
        }

        private sealed class PipeSurface
        {
            // Visual layers stay separate from the two simple collision shapes: body
            // and cap. Glow, seams, and scan effects must never become obstacles.
            public SpriteRenderer Artwork;
            public SpriteRenderer Outer;
            public SpriteRenderer Panel;
            public SpriteRenderer Shade;
            public SpriteRenderer RailLeft;
            public SpriteRenderer RailRight;
            public SpriteRenderer Core;
            public SpriteRenderer CorePulse;
            public SpriteRenderer Highlight;
            public SpriteRenderer Energy;
            public SpriteRenderer Scan;
            public SpriteRenderer Beacon;
            public SpriteRenderer CapGlow;
            public SpriteRenderer CapOuter;
            public SpriteRenderer CapAccent;
            public SpriteRenderer CapPanel;
            public SpriteRenderer CapEnergy;
            public BoxCollider2D BodyCollider;
            public BoxCollider2D CapCollider;
        }

        private sealed class PipePair
        {
            public GameObject Root;
            public PipeSurface Top;
            public PipeSurface Bottom;
            public float X;
            public float GapCenter;
            public float BaseGapCenter;
            public float GapHeight;
            public float DriftAmplitude;
            public float DriftPhase;
            public int RouteWorldIndex;
            public int RouteScore;
            public int Sequence;
            public bool IsStatic;
            public bool Passed;
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
            public SpriteRenderer DebugTopBody;
            public SpriteRenderer DebugTopCap;
            public SpriteRenderer DebugBottomBody;
            public SpriteRenderer DebugBottomCap;
#endif
        }

        private sealed class AmbientStar
        {
            public Transform Transform;
            public float X;
            public float ViewportFraction;
            public float Y;
            public float Phase;
            public float Speed;
            public float BaseSize;
        }

        private sealed class PowerUpPickup
        {
            public GameObject Root;
            public Transform Transform;
            public SpriteRenderer Glow;
            public SpriteRenderer Depth;
            public SpriteRenderer Artwork;
            public SpriteRenderer Spark;
            public PowerUpKind Kind;
            public PipePair Gate;
            public float X;
            public float Y;
            public float GapOffset;
            public float LocalXOffset;
            public float ArcYOffset;
            public float Phase;
            public float RespawnTimer;
            public Vector3 ArtworkBaseScale;
            public bool Active;
        }

        private sealed class CrystalPickupBurst
        {
            public GameObject Root;
            public SpriteRenderer Ring;
            public SpriteRenderer[] Sparks;
            public float Remaining;
            public float Duration;
        }

        private const float CameraHeight = 18f;
        private const float HudFeedbackY = 686f;
        private const float PortraitPlayfieldAspect = 9f / 16f;
        // Keep the authored neon sky's contrast; a heavy accent wash flattened
        // its cloud lighting and reduced separation from the gate glow.
        private const float WorldAtmosphereTintAlpha = .035f;
        private const float GroundY = -8.45f;
        private const float BirdX = -2.45f;
        // One body-only gameplay hitbox shared by all birds. Its capsule excludes
        // wing tips, tail, beak, and glow.
        private const float BirdHitboxWidth = .98f;
        private const float BirdHitboxHeight = .76f;
        private const float BirdHitboxOffsetX = .20f;
        private const float BirdHitboxOffsetY = -.05f;
        private const float BirdHitboxRadius = BirdHitboxHeight * .5f;
        // Pickups remain deliberately generous around the illustrated bird; this is
        // separate from the smaller physical collision capsule.
        private const float BirdPickupRadius = .81f;
        // The bird is the primary focal point, so it must remain readable against a
        // busy world at a real phone scale—not shrink into a sparkle at the centre.
        private const float BirdDisplayWidth = 2.30f;
        // Pipe tuning is deliberately centralised: the body can stretch only along
        // its length. New artwork fits the original cap dimensions so visual
        // upgrades never change the established collision geometry.
        private const float PipeWidth = 1.72f;
        private const float PipeCapWidth = PipeWidth + .34f;
        private const float PipeCollisionWidth = PipeCapWidth;
        private const float PipeFallbackCapHeight = .62f;
        private const float TopPipeOverscan = .68f;
        private const float BottomPipeFloorOverlap = .62f;
        private const float PipeSpacingFraction = .52f;
        private const int PipeBodyCropTopPixels = 145;
        private const int PipeBodyCropBottomPixels = 145;
        private const float PipeMinimumVisibleHeight = 1.56f;
        // This corridor gives the route meaningful high and low gates without
        // spawning a first obstacle against a screen edge or creating tiny pipes.
        private const float GapCenterMinimum = -2.55f;
        private const float GapCenterMaximum = 3.15f;
        private const int PipeCount = 4;
        // Crystals are deliberate pickups, not a payment for simply surviving each
        // gate. One visible pellet keeps a good run rewarding without making the
        // collection economy collapse after a handful of flights.
        // Four live gates can each carry a three-crystal arc.  The pool avoids
        // runtime allocations and lets every generated arc remain visible.
        private const int CrystalPickupCount = 12;
        private const int CrystalPickupBurstCount = 6;
        private const string CrystalArtworkPath = "SkyPulse/art/powerups/generated/crystal-prism-neon-v4";
        private const int PowerUpCount = 1;
        private const float PickupRadius = .43f;
        private const float CrystalPickupRadius = .34f;
        private const float CrystalPickupRespawnMinimum = 8.5f;
        private const float CrystalPickupRespawnMaximum = 12.5f;
        private const float InputLockoutSeconds = .07f;
        private const float WorldTransitionSeconds = 1.8f;
        private const float WorldRecoverySeconds = .9f;
        private const float AegisImmunitySeconds = .6f;
        private const float AegisHitStopSeconds = .07f;
        private const float ImpactFreezeSeconds = .07f;
        private const float ImpactTumbleSeconds = .70f;
        private const float SimulationStep = 1f / 120f;
        private const float MaximumSimulationCatchup = 1f / 12f;
        // Physics response after a tap. This does not control the authored wing artwork.
        private const float WingCycleSeconds = .30f;

        // Presentation-only menu rhythm. Gameplay must not inherit this duration.
        private const float SharedWingAnimationSeconds = 1.18f;
        // Gameplay uses the same authored ordering as the home bird, but
        // traverses one short input-triggered stroke instead of the menu loop.
        private const float GameplayWingAnimationSeconds = .40f;
        private const float GameplayWingSettledPhase = .5f;
        private const float WingLiftPhase = .31f;
        private const float WingDownstrokeDelay = .075f;
        private const float WingDownstrokeSpan = .90f;
        private const int LaunchBirdCount = 15;
        private const float CosmeticCardHeight = 260f;
        private const float CosmeticCardRowStride = 286f;
        private const float BirdHangarCardWidth = 432f;
        private const float BirdHangarCardHeight = 390f;
        private const float BirdHangarColumnStride = 454f;
        private const float BirdHangarRowStride = 416f;

        // One route, one handling model. Values are expressed against the 15.82-unit
        // flight corridor above the lower hazard: ~2.2 corridor-heights/s² gravity,
        // .72 heights/s flap lift, and .95 heights/s terminal fall. Bird choice
        // never changes it.
        private static readonly FlightTuning EndlessTuning = new FlightTuning(
            gravity: -34.8f, flapVelocity: 11.4f, maxFallVelocity: -15.05f,
            collisionRadius: BirdPickupRadius, perfectPassWindow: .34f, inputBufferSeconds: .07f);
        private static BirdHangarProfile GetBirdHangarProfile(Skin skin)
        {
            switch (skin.Id)
            {
                case "neon_finch":
                    return new BirdHangarProfile(
                        "COMMON",
                        "Balanced and dependable. Built for smooth flows.",
                        3, 4, 4,
                        Hex("#45eaff"));

                case "chrome_raven":
                    return new BirdHangarProfile(
                        "COMMON",
                        "A composed mechanical flyer with a stable profile.",
                        3, 3, 5,
                        Hex("#45eaff"));

                case "prism_hummingbird":
                    return new BirdHangarProfile(
                        "RARE",
                        "Light and responsive with an agile flight identity.",
                        4, 5, 2,
                        Hex("#ffc34d"));

                case "koiwing_glider":
                    return new BirdHangarProfile(
                        "RARE",
                        "Graceful movement with a balanced aerial profile.",
                        3, 4, 4,
                        Hex("#ffc34d"));

                case "verdant_kite":
                    return new BirdHangarProfile(
                        "RARE",
                        "A confident all-rounder with strong forward energy.",
                        4, 3, 4,
                        Hex("#ffc34d"));

                case "newbird01":
                    return new BirdHangarProfile(
                        "EPIC",
                        "Solar-charged styling with controlled movement.",
                        4, 3, 4,
                        Hex("#f05bc6"));

                case "newbird02":
                    return new BirdHangarProfile(
                        "EPIC",
                        "Fast and adaptable with a precise aerial character.",
                        4, 4, 3,
                        Hex("#f05bc6"));

                case "newbird03":
                    return new BirdHangarProfile(
                        "EPIC",
                        "High-energy styling built around speed and response.",
                        5, 4, 2,
                        Hex("#f05bc6"));

                case "newbird04":
                    return new BirdHangarProfile(
                        "EPIC",
                        "Highly responsive with an aggressive neon silhouette.",
                        4, 5, 2,
                        Hex("#f05bc6"));

                case "newbird05":
                    return new BirdHangarProfile(
                        "EPIC",
                        "A fast mechanical hunter with a direct flight identity.",
                        5, 3, 3,
                        Hex("#f05bc6"));

                case "newbird06":
                    return new BirdHangarProfile(
                        "LEGENDARY",
                        "Heavy visual presence with an exceptionally stable profile.",
                        3, 3, 5,
                        Hex("#b17cff"));

                case "newbird07":
                    return new BirdHangarProfile(
                        "LEGENDARY",
                        "Elegant and agile with a highly responsive character.",
                        3, 5, 3,
                        Hex("#b17cff"));

                case "newbird08":
                    return new BirdHangarProfile(
                        "LEGENDARY",
                        "Maximum visual thrust with an aggressive speed profile.",
                        5, 3, 2,
                        Hex("#b17cff"));

                case "newbird09":
                    return new BirdHangarProfile(
                        "LEGENDARY",
                        "Fast and elusive with a precision-focused character.",
                        4, 5, 3,
                        Hex("#b17cff"));

                case "newbird10":
                    return new BirdHangarProfile(
                        "LEGENDARY",
                        "Elite ion-powered styling with a fast balanced profile.",
                        5, 4, 3,
                        Hex("#b17cff"));

                default:
                    return new BirdHangarProfile(
                        "COMMON",
                        "A balanced mechanical companion.",
                        3, 3, 3,
                        Hex("#45eaff"));
            }
        }
        // The data-driven hangar has one free cyber-bird and fourteen crystal unlocks.
        // Their art and accent vary, but their shared collision and flight tuning
        // preserve a single fair score route. Future birds belong here as data-only additions.
        private static readonly Skin[] Skins =
        {
            new Skin("neon_finch", "NEON FINCH", "SkyPulse/characters/roster/volt-frame-04-v1", "SkyPulse/characters/roster/volt-frame-06-v1", "#3197ff", "#45eaff", 0, "SkyPulse/characters/roster/volt-frame-01-v1", "SkyPulse/characters/roster/volt-frame-07-v1", "SkyPulse/characters/roster/volt-frame-08-v1", new []
            {
                "SkyPulse/characters/roster/volt-frame-01-v1", "SkyPulse/characters/roster/volt-frame-02-v1", "SkyPulse/characters/roster/volt-frame-03-v1",
                "SkyPulse/characters/roster/volt-frame-04-v1", "SkyPulse/characters/roster/volt-frame-05-v1", "SkyPulse/characters/roster/volt-frame-06-v1",
            }),
            new Skin("chrome_raven", "CHROME RAVEN", "SkyPulse/characters/roster/steel-frame-04-v1", "SkyPulse/characters/roster/steel-frame-06-v1", "#b8d5e8", "#45eaff", 250, "SkyPulse/characters/roster/steel-frame-01-v1", "SkyPulse/characters/roster/steel-frame-07-v1", "SkyPulse/characters/roster/steel-frame-08-v1", new []
            {
                "SkyPulse/characters/roster/steel-frame-01-v1", "SkyPulse/characters/roster/steel-frame-02-v1", "SkyPulse/characters/roster/steel-frame-03-v1",
                "SkyPulse/characters/roster/steel-frame-04-v1", "SkyPulse/characters/roster/steel-frame-05-v1", "SkyPulse/characters/roster/steel-frame-06-v1",
            }, new []
            {
                // These artboards were composed at different canvas origins.
                // Static pivots align the raven's body/eye to frame 06 while
                // leaving the parent bird transform unchanged during flight.
                new Vector2(.5811f, .0154f),
                new Vector2(.4615f, .0208f),
                new Vector2(.5832f, .1518f),
                new Vector2(.4941f, .1669f),
                new Vector2(.5635f, .4794f),
                new Vector2(.5000f, .5000f),
            }),
            new Skin("prism_hummingbird", "PRISM HUMMINGBIRD", "SkyPulse/characters/roster/prism-frame-04-v1", "SkyPulse/characters/roster/prism-frame-06-v1", "#f4bf47", "#45eaff", 500, "SkyPulse/characters/roster/prism-frame-01-v1", "SkyPulse/characters/roster/prism-frame-07-v1", "SkyPulse/characters/roster/prism-frame-08-v1", new []
            {
                "SkyPulse/characters/roster/prism-frame-01-v1", "SkyPulse/characters/roster/prism-frame-02-v1", "SkyPulse/characters/roster/prism-frame-03-v1",
                "SkyPulse/characters/roster/prism-frame-04-v1", "SkyPulse/characters/roster/prism-frame-05-v1", "SkyPulse/characters/roster/prism-frame-06-v1",
            }),
            new Skin("koiwing_glider", "KOIWING GLIDER", "SkyPulse/characters/roster/cinder-frame-04-v1", "SkyPulse/characters/roster/cinder-frame-06-v1", "#f65b89", "#ffc34d", 800, "SkyPulse/characters/roster/cinder-frame-01-v1", "SkyPulse/characters/roster/cinder-frame-07-v1", "SkyPulse/characters/roster/cinder-frame-08-v1", new []
            {
                "SkyPulse/characters/roster/cinder-frame-01-v1", "SkyPulse/characters/roster/cinder-frame-02-v1", "SkyPulse/characters/roster/cinder-frame-03-v1",
                "SkyPulse/characters/roster/cinder-frame-04-v1", "SkyPulse/characters/roster/cinder-frame-05-v1", "SkyPulse/characters/roster/cinder-frame-06-v1",
            }),
            new Skin("verdant_kite", "VERDANT KITE", "SkyPulse/characters/roster/verdant-frame-04-v1", "SkyPulse/characters/roster/verdant-frame-06-v1", "#7ee870", "#45eaff", 1200, "SkyPulse/characters/roster/verdant-frame-01-v1", "SkyPulse/characters/roster/verdant-frame-07-v1", "SkyPulse/characters/roster/verdant-frame-08-v1", new []
            {
                "SkyPulse/characters/roster/verdant-frame-01-v1", "SkyPulse/characters/roster/verdant-frame-02-v1", "SkyPulse/characters/roster/verdant-frame-03-v1",
                "SkyPulse/characters/roster/verdant-frame-04-v1", "SkyPulse/characters/roster/verdant-frame-05-v1", "SkyPulse/characters/roster/verdant-frame-06-v1",
            }),
            new Skin("newbird01", "SOLARA", "SkyPulse/characters/roster/newbird01-frame-04", "SkyPulse/characters/roster/newbird01-frame-06", "#ff6f61", "#ffc34d", 1500, "SkyPulse/characters/roster/newbird01-frame-01", "SkyPulse/characters/roster/newbird01-frame-07", "SkyPulse/characters/roster/newbird01-frame-08", new []
            {
                "SkyPulse/characters/roster/newbird01-frame-01", "SkyPulse/characters/roster/newbird01-frame-02", "SkyPulse/characters/roster/newbird01-frame-03",
                "SkyPulse/characters/roster/newbird01-frame-04", "SkyPulse/characters/roster/newbird01-frame-05", "SkyPulse/characters/roster/newbird01-frame-06",
            }),
            new Skin("newbird02", "ASTRA", "SkyPulse/characters/roster/newbird02-frame-04", "SkyPulse/characters/roster/newbird02-frame-06", "#3197ff", "#45eaff", 1800, "SkyPulse/characters/roster/newbird02-frame-01", "SkyPulse/characters/roster/newbird02-frame-07", "SkyPulse/characters/roster/newbird02-frame-08", new []
            {
                "SkyPulse/characters/roster/newbird02-frame-01", "SkyPulse/characters/roster/newbird02-frame-02", "SkyPulse/characters/roster/newbird02-frame-03",
                "SkyPulse/characters/roster/newbird02-frame-04", "SkyPulse/characters/roster/newbird02-frame-05", "SkyPulse/characters/roster/newbird02-frame-06",
            }),
            new Skin("newbird03", "VOLTCREST", "SkyPulse/characters/roster/newbird03-frame-04", "SkyPulse/characters/roster/newbird03-frame-06", "#45eaff", "#ea6aff", 2200, "SkyPulse/characters/roster/newbird03-frame-01", "SkyPulse/characters/roster/newbird03-frame-07", "SkyPulse/characters/roster/newbird03-frame-08", new []
            {
                "SkyPulse/characters/roster/newbird03-frame-01", "SkyPulse/characters/roster/newbird03-frame-02", "SkyPulse/characters/roster/newbird03-frame-03",
                "SkyPulse/characters/roster/newbird03-frame-04", "SkyPulse/characters/roster/newbird03-frame-05", "SkyPulse/characters/roster/newbird03-frame-06",
            }),
            new Skin("newbird04", "CHROMAFLUX", "SkyPulse/characters/roster/newbird04-frame-04", "SkyPulse/characters/roster/newbird04-frame-06", "#e558cf", "#ffc34d", 2600, "SkyPulse/characters/roster/newbird04-frame-01", "SkyPulse/characters/roster/newbird04-frame-07", "SkyPulse/characters/roster/newbird04-frame-08", new []
            {
                "SkyPulse/characters/roster/newbird04-frame-01", "SkyPulse/characters/roster/newbird04-frame-02", "SkyPulse/characters/roster/newbird04-frame-03",
                "SkyPulse/characters/roster/newbird04-frame-04", "SkyPulse/characters/roster/newbird04-frame-05", "SkyPulse/characters/roster/newbird04-frame-06",
            }),
            new Skin("newbird05", "VIREX", "SkyPulse/characters/roster/newbird05-frame-04", "SkyPulse/characters/roster/newbird05-frame-06", "#f65b89", "#ffc34d", 3100, "SkyPulse/characters/roster/newbird05-frame-01", "SkyPulse/characters/roster/newbird05-frame-07", "SkyPulse/characters/roster/newbird05-frame-08", new []
            {
                "SkyPulse/characters/roster/newbird05-frame-01", "SkyPulse/characters/roster/newbird05-frame-02", "SkyPulse/characters/roster/newbird05-frame-03",
                "SkyPulse/characters/roster/newbird05-frame-04", "SkyPulse/characters/roster/newbird05-frame-05", "SkyPulse/characters/roster/newbird05-frame-06",
            }),
            new Skin("newbird06", "BOREALIS", "SkyPulse/characters/roster/newbird06-frame-04", "SkyPulse/characters/roster/newbird06-frame-06", "#a875ff", "#ea6aff", 3700, "SkyPulse/characters/roster/newbird06-frame-01", "SkyPulse/characters/roster/newbird06-frame-07", "SkyPulse/characters/roster/newbird06-frame-08", new []
            {
                "SkyPulse/characters/roster/newbird06-frame-01", "SkyPulse/characters/roster/newbird06-frame-02", "SkyPulse/characters/roster/newbird06-frame-03",
                "SkyPulse/characters/roster/newbird06-frame-04", "SkyPulse/characters/roster/newbird06-frame-05", "SkyPulse/characters/roster/newbird06-frame-06",
            }),
            new Skin("newbird07", "ROSELIGHT", "SkyPulse/characters/roster/newbird07-frame-04", "SkyPulse/characters/roster/newbird07-frame-06", "#7ee870", "#d8ff84", 4400, "SkyPulse/characters/roster/newbird07-frame-01", "SkyPulse/characters/roster/newbird07-frame-07", "SkyPulse/characters/roster/newbird07-frame-08", new []
            {
                "SkyPulse/characters/roster/newbird07-frame-01", "SkyPulse/characters/roster/newbird07-frame-02", "SkyPulse/characters/roster/newbird07-frame-03",
                "SkyPulse/characters/roster/newbird07-frame-04", "SkyPulse/characters/roster/newbird07-frame-05", "SkyPulse/characters/roster/newbird07-frame-06",
            }),
            new Skin("newbird08", "PYRESTORM", "SkyPulse/characters/roster/newbird08-frame-04", "SkyPulse/characters/roster/newbird08-frame-06", "#3197ff", "#45eaff", 5200, "SkyPulse/characters/roster/newbird08-frame-01", "SkyPulse/characters/roster/newbird08-frame-07", "SkyPulse/characters/roster/newbird08-frame-08", new []
            {
                "SkyPulse/characters/roster/newbird08-frame-01", "SkyPulse/characters/roster/newbird08-frame-02", "SkyPulse/characters/roster/newbird08-frame-03",
                "SkyPulse/characters/roster/newbird08-frame-04", "SkyPulse/characters/roster/newbird08-frame-05", "SkyPulse/characters/roster/newbird08-frame-06",
            }),
            new Skin("newbird09", "NIGHTVEIL", "SkyPulse/characters/roster/newbird09-frame-04", "SkyPulse/characters/roster/newbird09-frame-06", "#7ee870", "#d8ff84", 6100, "SkyPulse/characters/roster/newbird09-frame-01", "SkyPulse/characters/roster/newbird09-frame-07", "SkyPulse/characters/roster/newbird09-frame-08", new []
            {
                "SkyPulse/characters/roster/newbird09-frame-01", "SkyPulse/characters/roster/newbird09-frame-02", "SkyPulse/characters/roster/newbird09-frame-03",
                "SkyPulse/characters/roster/newbird09-frame-04", "SkyPulse/characters/roster/newbird09-frame-05", "SkyPulse/characters/roster/newbird09-frame-06",
            }),
            new Skin("newbird10", "IONWING", "SkyPulse/characters/roster/newbird10-frame-04", "SkyPulse/characters/roster/newbird10-frame-06", "#b8d5e8", "#c28cff", 7100, "SkyPulse/characters/roster/newbird10-frame-01", "SkyPulse/characters/roster/newbird10-frame-07", "SkyPulse/characters/roster/newbird10-frame-08", new []
            {
                "SkyPulse/characters/roster/newbird10-frame-01", "SkyPulse/characters/roster/newbird10-frame-02", "SkyPulse/characters/roster/newbird10-frame-03",
                "SkyPulse/characters/roster/newbird10-frame-04", "SkyPulse/characters/roster/newbird10-frame-05", "SkyPulse/characters/roster/newbird10-frame-06",
            }),
        };

        private static readonly WorldTheme[] Worlds =
 {
new WorldTheme(
"neon_city",
"NEON CITY",
"SkyPulse/backgrounds/neon-flightdeck-v2",
"#45eaff",
"#0a0522",
"ROUTE 01",
1f,
5.38f,
"ion",
"pulse"
),

new WorldTheme(
"aurora_rise",
"AURORA RISE",
"SkyPulse/backgrounds/themes/aurora-rise-v3",
"#61f5b3",
"#05251e",
"ROUTE 02",
1f,
4.92f,
"frost",
"aurora"
),

new WorldTheme(
"solar_drift",
"SOLAR DRIFT",
"SkyPulse/backgrounds/themes/solar-drift-v3",
"#ffc34d",
"#2b0d10",
"ROUTE 03",
1f,
4.46f,
"solar",
"solar"
),

new WorldTheme(
"midnight_tide",
"MIDNIGHT TIDE",
"SkyPulse/backgrounds/themes/midnight-tide-v2",
"#45eaff",
"#07113d",
"ROUTE 04",
1f,
4.30f,
"cobalt",
"seaglass"
),

new WorldTheme(
"velvet_dawn",
"VELVET DAWN",
"SkyPulse/backgrounds/themes/velvet-dawn-v2",
"#f05bc6",
"#26051f",
"ROUTE 05",
1f,
4.14f,
"rose",
"sakura"
),

new WorldTheme(
"crystal_night",
"CRYSTAL NIGHT",
"SkyPulse/backgrounds/themes/crystal-night-v2",
"#edf7ff",
"#071239",
"ROUTE 06",
1f,
4.00f,
"prism",
"glacial"
),

new WorldTheme(
"jade_horizon",
"JADE HORIZON",
"SkyPulse/backgrounds/themes/jade-horizon-v2",
"#61f5b3",
"#063523",
"ROUTE 07",
1f,
3.86f,
"jade",
"mintwave"
),

new WorldTheme(
"violet_rain",
"VIOLET RAIN",
"SkyPulse/backgrounds/themes/violet-rain-v2",
"#b17cff",
"#210842",
"ROUTE 08",
1f,
3.72f,
"amethyst",
"nebula"
),

new WorldTheme(
"eclipse",
"ECLIPSE",
"SkyPulse/backgrounds/themes/eclipse-v2",
"#b17cff",
"#10051f",
"ROUTE 09",
1f,
3.56f,
"obsidian",
"starlight"
),
};

        private static readonly Upgrade[] Upgrades =
        {
            // COLLECTION · stronger pickup reach and crystal value, never easier flight.
            new Upgrade("crystal_resonator", "CRYSTAL RESONATOR", new []
            {
                "Attract crystals within 8% of screen width",
                "Attract crystals within 13% of screen width",
                "Attract crystals within 18% of screen width",
            }, new [] { 150, 400, 900 }, "#45eaff", "COLLECTION", null, 0, 1),
            new Upgrade("prism_conduit", "PRISM CONDUIT", new []
            {
                "Collected crystals gain +15% value",
                "Collected crystals gain +30% value",
                "Collected crystals gain +50% value",
            }, new [] { 350, 700, 1350 }, "#3197ff", "COLLECTION", "crystal_resonator", 2, 2),
            new Upgrade("gravity_well", "GRAVITY WELL", new []
            {
                "Add 3% screen width to pickup reach",
                "Add 6% screen width to pickup reach",
                "Add 10% screen width to pickup reach",
            }, new [] { 650, 1300, 2400 }, "#8f64ff", "COLLECTION", "prism_conduit", 2, 3),

            // RECOVERY · post-run economy rewards only.
            new Upgrade("salvage_codec", "SALVAGE CODEC", new []
            {
                "Earn +20% crystals at run end",
                "Earn +35% crystals at run end",
                "Earn +50% crystals at run end",
            }, new [] { 200, 500, 1000 }, "#ffc34d", "RECOVERY", null, 0, 1),
            new Upgrade("recovery_cache", "RECOVERY CACHE", new []
            {
                "Finish a run for +8 crystals",
                "Finish a run for +16 crystals",
                "Finish a run for +25 crystals",
            }, new [] { 400, 800, 1450 }, "#ff9f43", "RECOVERY", "salvage_codec", 2, 2),
            new Upgrade("archive_engine", "ARCHIVE ENGINE", new []
            {
                "Set a new high score for +25 crystals",
                "Set a new high score for +50 crystals",
                "Set a new high score for +100 crystals",
            }, new [] { 700, 1400, 2500 }, "#ffd166", "RECOVERY", "recovery_cache", 2, 3),

            // MASTERY · skill milestones reward currency without changing score rules.
            new Upgrade("precision_harvester", "PRECISION HARVESTER", new []
            {
                "+1 crystal every 3 perfect passes",
                "+1 crystal every 2 perfect passes",
                "+1 crystal on every perfect pass",
            }, new [] { 250, 550, 950 }, "#f05bc6", "MASTERY", null, 0, 1),
            new Upgrade("streak_capacitor", "STREAK CAPACITOR", new []
            {
                "+5 crystals every 15 gates",
                "+10 crystals every 15 gates",
                "+15 crystals every 15 gates",
            }, new [] { 450, 850, 1500 }, "#d65cff", "MASTERY", "precision_harvester", 2, 2),
            new Upgrade("apex_matrix", "APEX MATRIX", new []
            {
                "Reach 10 gates for +15% result crystals",
                "Reach 10 gates for +30% result crystals",
                "Reach 10 gates for +50% result crystals",
            }, new [] { 800, 1600, 2500 }, "#b17cff", "MASTERY", "streak_capacitor", 2, 3),
        };

        private static readonly TrailStyle[] Trails =
        {
            new TrailStyle("pulse", "PULSE", "#8f64ff", "#45eaff"),
            new TrailStyle("solar", "SOLAR", "#ffc34d", "#f05bc6"),
            new TrailStyle("aurora", "AURORA", "#61f5b3", "#8f64ff"),
            new TrailStyle("comet", "COMET", "#edf7ff", "#45eaff"),
            new TrailStyle("ember", "EMBER", "#f05bc6", "#ffc34d"),
            new TrailStyle("nebula", "NEBULA", "#8f64ff", "#f05bc6"),
            new TrailStyle("mintwave", "MINTWAVE", "#61f5b3", "#45eaff"),
            new TrailStyle("sakura", "SAKURA", "#f05bc6", "#edf7ff"),
            new TrailStyle("glacial", "GLACIAL", "#edf7ff", "#8f64ff"),
            new TrailStyle("voltage", "VOLTAGE", "#ffc34d", "#45eaff"),
            new TrailStyle("cinder", "CINDER", "#f05bc6", "#8f64ff"),
            new TrailStyle("seaglass", "SEAGLASS", "#61f5b3", "#edf7ff"),
            new TrailStyle("starlight", "STARLIGHT", "#edf7ff", "#ffc34d"),
        };

        private static readonly PipeStyle[] PipeStyles =
        {
            new PipeStyle("ion", "ION", "#45eaff", "#0b3076", "#45eaff"),
            new PipeStyle("rose", "ROSE", "#f05bc6", "#501144", "#b17cff"),
            new PipeStyle("solar", "SOLAR", "#ffc34d", "#592409", "#f05bc6"),
            new PipeStyle("mint", "MINT", "#61f5b3", "#0a442f", "#45eaff"),
            new PipeStyle("prism", "PRISM", "#edf7ff", "#2b1257", "#f05bc6"),
            new PipeStyle("cobalt", "COBALT", "#45eaff", "#102c80", "#edf7ff"),
            new PipeStyle("jade", "JADE", "#61f5b3", "#0b4827", "#45eaff"),
            new PipeStyle("emberline", "EMBERLINE", "#f05bc6", "#5b110e", "#ffc34d"),
            new PipeStyle("amethyst", "AMETHYST", "#b17cff", "#35115c", "#edf7ff"),
            new PipeStyle("frost", "FROST", "#edf7ff", "#183f60", "#45eaff"),
            new PipeStyle("sunset", "SUNSET", "#ffc34d", "#6a1b0a", "#ffc34d"),
            new PipeStyle("seafoam", "SEAFOAM", "#61f5b3", "#0b4a42", "#edf7ff"),
            new PipeStyle("obsidian", "OBSIDIAN", "#edf7ff", "#130d28", "#f05bc6"),
        };

        private readonly PipePair[] pipePool = new PipePair[PipeCount];
        private readonly PowerUpPickup[] crystalPickupPool = new PowerUpPickup[CrystalPickupCount];
        private readonly CrystalPickupBurst[] crystalPickupBursts = new CrystalPickupBurst[CrystalPickupBurstCount];
        private int nextCrystalPickupBurst;
        private readonly PowerUpPickup[] powerUpPool = new PowerUpPickup[PowerUpCount];
        private SpriteRenderer incomingBackground;
        private Color transitionVeilStart, transitionFloorStart, transitionRailStart, transitionLipStart;
        private SpriteRenderer[] transitionRenderers;
        private Color[] transitionColours;
        private bool departingObjectsVisible;
        private readonly List<AmbientStar> ambientStars = new List<AmbientStar>();
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite> registeredFlapSpriteCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, BirdFrameRegistration> birdFrameRegistration = new Dictionary<string, BirdFrameRegistration>();
        private bool birdFrameRegistrationLoaded;
        private Sprite birdRegistrationReference;
        private readonly Dictionary<string, Sprite> worldFallbackSprites = new Dictionary<string, Sprite>();
        private readonly HashSet<string> ownedSkinIds = new HashSet<string>();
        private readonly HashSet<string> ownedUpgradeIds = new HashSet<string>();
        // A level is stored per permanent economy track. The legacy id set stays as
        // a compatibility bridge for old saves and for any unported visual code.
        private readonly Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();

        private FlightState state;
        private CosmeticCategory cosmeticCategory;
        private Camera flightCamera;
        private Sprite whiteSprite;
        private Sprite midnightSprite;
        private Sprite softCircleSprite;
        private Sprite ringSprite;
        private Sprite roundedPanelSprite;
        private Sprite interfacePanelSprite;
        private Sprite pipeBodySprite;
        private Sprite pipeCapSprite;
        private Sprite pipeGlowSprite;
        private bool hasAuthoredPipeBody;
        private bool hasAuthoredPipeCap;
        private bool hasAuthoredPipeGlow;
        private bool hasNeonPipeArtwork;
        private float pipeCapHeight = PipeFallbackCapHeight;
        private float PipeCapHeight => pipeCapHeight;
        private Sprite emergencyBirdSprite;
        private Sprite idleBirdSprite;
        private Sprite flapBirdSprite;
        private Sprite riseBirdSprite;
        private Sprite hitBirdSprite;
        private Sprite[] flapFrameBirdSprites;
        private int activeFlapFrameIndex;
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer backgroundVeil;
        private SpriteRenderer floorBase;
        private SpriteRenderer floorSurface;
        private SpriteRenderer floorLip;
        private SpriteRenderer floorGlow;
        private SpriteRenderer floorHighlight;
        private Transform bird;
        private Transform birdHitbox;
        private Transform birdArt;
        private Transform birdFlapArt;
        private Transform birdRiseArt;
        private SpriteRenderer birdRenderer;
        private SpriteRenderer birdSafetyRenderer;
        private SpriteRenderer birdFlapRenderer;
        private SpriteRenderer birdRiseRenderer;
        private SpriteRenderer birdParallaxRenderer;
        private SpriteRenderer birdDepthRenderer;
        private SpriteRenderer birdEyeGlintRenderer;
        private CapsuleCollider2D birdBodyCollider;
        private SpriteRenderer shieldAuraRenderer;
        private SpriteRenderer slowAuraRenderer;
        private SpriteRenderer effectAuraRenderer;
        private AudioSource audioSource;
        private AudioClip flapSound;
        private AudioClip scoreSound;
        private AudioClip crashSound;
        private AudioClip crystalSound;
        private AudioClip unlockSound;
        private Font uiFont;

        private GameObject uiRoot;
        private GameObject privacyScreen;
        private RectTransform safeAreaRoot;
        private RectTransform interfaceContentRoot;
        private readonly List<RectTransform> interfaceBackdrops = new List<RectTransform>();
        private Rect appliedSafeArea;
        private Vector2Int appliedScreenSize;
        private float appliedViewportWidth = -1f;
        private GameObject homeScreen;
        private GameObject hudScreen;
        private GameObject pauseScreen;
        private GameObject gameOverScreen;
        private GameObject customizeScreen;
        private ScrollRect customizeScroll;
        private int hangarPageIndex;
        private RectTransform hangarPageRoot;
        private Text hangarPageIndicatorText;
        private RectTransform hangarCarouselTrack;
        private RectTransform hangarPreviousBirdRoot;
        private RectTransform hangarCurrentBirdRoot;
        private RectTransform hangarNextBirdRoot;
        private CanvasGroup hangarPreviousBirdCanvas;
        private CanvasGroup hangarCurrentBirdCanvas;
        private CanvasGroup hangarNextBirdCanvas;
        private RectTransform hangarHeroBirdArt;
        private Vector2 hangarHeroBirdBasePosition;
        private float hangarHeroAnimationTime;
        private Image hangarHeroBirdImage;
        private Sprite[] hangarHeroWingFrames;
        private Image hangarHeroAura;
        private float hangarArrivalPulseTime = -1f;
        private const float HangarArrivalPulseDuration = .28f;
        private RectTransform hangarGlassShimmer;
        private Image hangarGlassShimmerImage;
        private CanvasGroup hangarInformationCanvas;
        private float hangarInformationFadeTime = -1f;
        private const float HangarInformationFadeDuration = .20f;

        private bool hangarDragging;
        private bool hangarSwipeAnimating;
        private Vector2 hangarDragStartPointer;
        private Vector2 hangarTrackStartPosition;

        private const float HangarCarouselSpacing = 365f;
        private const float HangarSwipeThreshold = 90f;
        private const float HangarSwipeDuration = .22f;
        private GameObject purchaseModal;
        private GameObject unlockRevealModal;
        private Text menuCrystalText;
        private Text customizeCrystalText;
        private Text menuBestText;
        private Text menuEquippedText;
        private Text difficultyText;
        private Text menuModeDetailText;
        private Text menuRouteText;
        private Text hudScoreText;
        private Text hudBestText;
        private Text hudCrystalText;
        private Text hudPowerUpText;
        private Text hudModeText;
        private Text hudCoachText;
        private Text scoreBurstText;
        private Text resultScoreText;
        private Text resultBestText;
        private Text resultCrystalsText;
        private Text resultBonusText;
        private Text resultBalanceText;
        private Text resultWorldText;
        private Text resultShareText;
        private Text resultNewBestText;
        private Text resultModeText;
        private Text resultReasonText;
        private Text menuTitleText;
        private Image menuBirdImage;
        private Image menuBirdSafetyImage;
        private Image menuBirdFlapImage;
        private Image menuBirdRiseImage;
        private Image menuBirdShadowImage;
        private Image menuBirdEyeGlintImage;
        private RectTransform menuBirdTransform;
        private RectTransform menuHeroTransform;
        private RectTransform customizeContent;
        private Text customizeTitle;
        private Text customizeSubtitle;
        private readonly List<Button> collectionTabs = new List<Button>();
        private readonly List<Image> collectionTabRails = new List<Image>();
        private Text purchaseTitleText;
        private Text purchaseDetailText;
        private Text purchaseBalanceText;
        private Text purchaseConfirmText;
        private Text reduceMotionText;
        private Text hapticsText;
        private Image purchasePreviewImage;
        private Image purchaseHalo;
        private SkyPulseUiGlyph purchaseUpgradeGlyph;
        private Button purchaseConfirmButton;
        private RectTransform unlockRevealCard;
        private RectTransform unlockRevealBirdTransform;
        private Image unlockRevealBirdImage;
        private Image unlockRevealHalo;
        private Image unlockRevealFlash;
        private Text unlockRevealTitle;
        private Text unlockRevealDetail;
        private Button unlockRevealContinueButton;

        private Skin equippedSkin;
        private Skin activeUnlockSkin;
        private WorldTheme equippedWorld;
        private WorldTheme routeWorld;
        private TrailStyle equippedTrail;
        private PipeStyle equippedPipe;
        private Skin pendingSkin;
        private Upgrade pendingUpgrade;
        private PendingPurchase pendingPurchase;
        private int score;
        private int best;
        private int crystals;
        private int runCrystalsCollected;
        private int runCrystalBonus;
        private float prismConduitCarry;
        private int farthestWorldIndex;
        private int runFarthestWorldIndex;
        private int routeWorldIndex;
        private int nextGateRouteScore;
        private int nextGateSequence;
        private int nextPowerUpRouteScore;
        private bool resultCrystalBonusApplied;
        private int flightCoachStage;
        private bool reduceMotionEnabled;
        private bool hapticsEnabled = true;
        private float birdY;
        private float birdVelocity;
        private float birdTilt;
        private float birdTiltVelocity;
        private float wingTimer;

        // Shared gameplay wing controller for every authored bird.  The active
        // pose is discrete so a rendered frame can never skip across artwork.
        private GameplayWingState gameplayWingState = GameplayWingState.Settled;
        private int gameplayWingFrameIndex;
        private float gameplayWingFrameTimer;
        private float impactFrameTimer;
        private float impactTumbleTimer;
        private float menuWingTimer;
        private float menuPresentationTime;
        private float unlockRevealTimer;
        private float spawnX;
        private float scoreBurstTimer;
        private float scoreBurstDuration = .36f;
        private bool scoreBurstIsCrystal;
        private float ambientTime;
        private float slowFieldTimer;
        private float shieldFlashTimer;
        private float magnetHaloTimer;
        private float shieldImmunityTimer;
        private float shieldHitStopTimer;
        private float worldTransitionTimer;
        private float worldRecoveryTimer;
        private float lastFlapInputTime = -100f;
        private bool firstGateAfterTransition;
        private float simulationAccumulator;
        private float bufferedFlapUntil = -1f;
        private float flightFeedbackTimer;
        private float flightFeedbackDuration;
        private string lastCrashReason = "GATE IMPACT";
#if !UNITY_EDITOR
        private float hapticCooldownUntil;
#endif
        private int shieldCharges;
        private int perfectPasses;
        private int displayedSlowTenths = -1;
        private int displayedPowerUpCode = -1;
        private bool newBest;
        private Color flightFeedbackColour;
        private SpriteRenderer flightFeedbackRenderer;
        private SpriteRenderer flightFeedbackRingRenderer;
        private Vector3 idleBirdBaseScale = Vector3.one;
        private Vector3 safetyBirdBaseScale = Vector3.one;
        private Vector3 parallaxBirdBaseScale = Vector3.one;
        private Vector3 flapBirdBaseScale = Vector3.one;
        private Vector3 riseBirdBaseScale = Vector3.one;
        private Vector3 authoredBirdBaseScale = Vector3.one;

#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
        private bool collisionDebugEnabled;
        private SpriteRenderer collisionBirdDebug;

#endif

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            // The flight simulation has its own 120 Hz accumulator. This ceiling only
            // prevents unrelated Unity systems from trying to catch up an entire pause.
            Time.maximumDeltaTime = MaximumSimulationCatchup;

            LoadProgress();
            ValidateBirdRewardPoseContracts();
            ValidateTechTreeContracts();
            CreateCamera();
            CreateVisuals();
            CreateInterface();
            ApplyEquippedVisuals();
            UpdateComfortCopy();
            ResetToMenu();
        }

        private static void ValidateBirdRewardPoseContracts()
        {
            if (Skins.Length != LaunchBirdCount)
            {
                Debug.LogError($"SkyPulse: launch collection must contain exactly {LaunchBirdCount} birds; it currently has {Skins.Length}.");
            }

            var hitPaths = new HashSet<string>(StringComparer.Ordinal);
            var unlockPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var skin in Skins)
            {
                if (skin.FlapFramePaths == null || skin.FlapFramePaths.Length != 6)
                {
                    Debug.LogError($"SkyPulse: {skin.Name} must define exactly six flight frames.");
                    continue;
                }
                if (skin.FlapFramePivots != null)
                {
                    if (skin.FlapFramePivots.Length != skin.FlapFramePaths.Length)
                    {
                        Debug.LogError($"SkyPulse: {skin.Name}'s frame registration must contain one pivot per flight frame.");
                    }
                    else
                    {
                        foreach (var pivot in skin.FlapFramePivots)
                        {
                            if (pivot.x < 0f || pivot.x > 1f || pivot.y < 0f || pivot.y > 1f)
                                Debug.LogError($"SkyPulse: {skin.Name}'s frame registration pivot must be normalized.");
                        }
                    }
                }
                if (string.IsNullOrEmpty(skin.HitPath) || string.IsNullOrEmpty(skin.UnlockPath))
                {
                    Debug.LogError($"SkyPulse: {skin.Name} must define both a hit pose and an unlock pose.");
                    continue;
                }
                var uniqueBirdFrames = new HashSet<string>(skin.FlapFramePaths, StringComparer.Ordinal);
                if (uniqueBirdFrames.Count != 6 || uniqueBirdFrames.Contains(string.Empty))
                {
                    Debug.LogError($"SkyPulse: {skin.Name}'s six flight frame paths must be unique and non-empty.");
                }
                if (!uniqueBirdFrames.Add(skin.HitPath))
                {
                    Debug.LogError($"SkyPulse: {skin.Name}'s hit pose must not reuse a flap frame.");
                }
                if (!uniqueBirdFrames.Add(skin.UnlockPath))
                {
                    Debug.LogError($"SkyPulse: {skin.Name}'s unlock pose must not reuse a flap or hit frame.");
                }
                if (uniqueBirdFrames.Count != 8)
                {
                    Debug.LogError($"SkyPulse: {skin.Name} must resolve to exactly eight unique frames: six flap, one hit, and one unlock.");
                }
                if (!hitPaths.Add(skin.HitPath)) Debug.LogError($"SkyPulse: hit pose path is shared by more than one bird: {skin.HitPath}");
                if (!unlockPaths.Add(skin.UnlockPath)) Debug.LogError($"SkyPulse: unlock pose path is shared by more than one bird: {skin.UnlockPath}");
                if (string.Equals(skin.ArtPath, skin.UnlockPath, StringComparison.Ordinal))
                {
                    Debug.LogError($"SkyPulse: {skin.Name}'s unlock pose must be bespoke rather than its normal flight art.");
                }
            }
        }

        private static void ValidateTechTreeContracts()
        {
            if (Upgrades.Length != 9)
            {
                Debug.LogError($"SkyPulse: Tech Tree must contain exactly 9 nodes; it currently has {Upgrades.Length}.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var upgrade in Upgrades)
            {
                if (upgrade == null) continue;
                if (!ids.Add(upgrade.Id)) Debug.LogError($"SkyPulse: duplicate Tech id: {upgrade.Id}");
                if (upgrade.MaxLevel != 3) Debug.LogError($"SkyPulse: {upgrade.Name} must define exactly three levels.");
                if (upgrade.HasPrerequisite && FindById(Upgrades, upgrade.PrerequisiteId) == null)
                {
                    Debug.LogError($"SkyPulse: {upgrade.Name} requires missing Tech id {upgrade.PrerequisiteId}.");
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) PauseFlight();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) PauseFlight();
        }

        private void CreateCamera()
        {
            flightCamera = Camera.main;
            if (flightCamera == null)
            {
                var cameraObject = new GameObject("SkyPulse Camera");
                flightCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            flightCamera.transform.position = new Vector3(0f, 0f, -10f);
            flightCamera.orthographic = true;
            flightCamera.orthographicSize = CameraHeight * .5f;
            flightCamera.clearFlags = CameraClearFlags.SolidColor;
            flightCamera.backgroundColor = Hex("#04051c");
        }

        private void CreateVisuals()
        {
            whiteSprite = CreateSprite(Texture2D.whiteTexture, 1f);
            midnightSprite = CreateSolidSprite("Midnight fallback", Hex("#060817"));
            softCircleSprite = CreateRadialSprite("Soft neon orb", 96, 0f, .5f);
            ringSprite = CreateRadialSprite("Neon ring", 96, .31f, .5f);
            roundedPanelSprite = CreateRoundedRectSprite("Premium rounded panel", 128, 28);
            interfacePanelSprite = CreateFlightPanelSprite();
            // Supplied pipe PNGs use a white presentation canvas. Turn their
            // connected white background into alpha once at load time so the actual
            // mechanical art can be used without white slabs or invisible geometry.
            pipeBodySprite = LoadKeyedPipeSprite("PipeBody", PipeBodyCropTopPixels, PipeBodyCropBottomPixels);
            hasAuthoredPipeBody = pipeBodySprite != null;
            if (!hasAuthoredPipeBody) pipeBodySprite = CreateCylindricalPipeSprite("Cylindrical pipe metal", 128, 128);
            pipeCapSprite = LoadKeyedPipeSprite("PipeCap");
            hasAuthoredPipeCap = pipeCapSprite != null;
            pipeCapHeight = hasAuthoredPipeCap
                ? PipeCapWidth / Mathf.Max(.01f, pipeCapSprite.bounds.size.x / pipeCapSprite.bounds.size.y)
                : PipeFallbackCapHeight;
            pipeGlowSprite = LoadKeyedPipeSprite("PipeGlow");
            hasAuthoredPipeGlow = pipeGlowSprite != null;
            // Measure the shipped cap above before loading the new visuals. Its
            // height is part of route collision geometry, not an art import setting.
            // These rectangles exclude faint alpha specks and presentation margins
            // measured in the original PNGs, even when iOS scales the textures.
            var neonShaft = LoadPipeCutoutSprite("SkyPulse/art/pipes/pipe-shaft-neon-v1",
                new Rect(266f / 1024f, 0f, 493f / 1024f, 1f));
            var neonCollar =
    LoadPipeCutoutSprite(
        "SkyPulse/art/pipes/pipe-collar-neon-v1",
        new Rect(
            225f / 1934f,
            91f / 813f,
            1484f / 1934f,
            608f / 813f));
            hasNeonPipeArtwork = neonShaft != null && neonCollar != null;
            if (hasNeonPipeArtwork)
            {
                pipeBodySprite = neonShaft;
                pipeCapSprite = neonCollar;
                hasAuthoredPipeBody = true;
                hasAuthoredPipeCap = true;
            }

            backgroundRenderer = CreateRenderer("Cinematic world", WorldBackdrop(equippedWorld), Color.white, -40);
            backgroundRenderer.transform.position = new Vector3(0f, .12f, 0f);
            FitBackgroundToCamera(backgroundRenderer, 1.1f);

            incomingBackground = CreateRenderer("Incoming world dissolve", backgroundRenderer.sprite, Color.clear, -39);
            incomingBackground.enabled = false;
            FitBackgroundToCamera(incomingBackground, 1.1f);
            // Warm the route backdrops before play, avoiding first-use Resources
            // loading at gate 15 or 30. The sprite cache retains them for remixes.
            for (var worldIndex = 0; worldIndex < 3; worldIndex += 1) WorldBackdrop(Worlds[worldIndex]);
            backgroundVeil = CreateRenderer("World colour veil", whiteSprite, new Color(.015f, .01f, .08f, .20f), -38);
            backgroundVeil.transform.position = new Vector3(0f, .1f, 0f);
            backgroundVeil.transform.localScale = new Vector3(GetViewportWidth() + 1f, CameraHeight + .5f, 1f);

            CreateAmbientStars();
            CreateFloor();
            CreateBird();
            CreateFlightFeedback();
            CreateCrystalPickupBursts();

            for (var index = 0; index < pipePool.Length; index += 1) pipePool[index] = CreatePipePair(index);
            for (var index = 0; index < crystalPickupPool.Length; index += 1) crystalPickupPool[index] = CreateCrystalPickup(index);
            for (var index = 0; index < powerUpPool.Length; index += 1) powerUpPool[index] = CreatePowerUp(index);

            var departureRenderers = new List<SpriteRenderer>();
            foreach (var pair in pipePool) departureRenderers.AddRange(pair.Root.GetComponentsInChildren<SpriteRenderer>(true));
            foreach (var pickup in crystalPickupPool) departureRenderers.AddRange(pickup.Root.GetComponentsInChildren<SpriteRenderer>(true));
            foreach (var pickup in powerUpPool) departureRenderers.AddRange(pickup.Root.GetComponentsInChildren<SpriteRenderer>(true));
            transitionRenderers = departureRenderers.ToArray();
            transitionColours = new Color[transitionRenderers.Length];

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            flapSound = Resources.Load<AudioClip>("SkyPulse/audio/flap");
            scoreSound = Resources.Load<AudioClip>("SkyPulse/audio/score");
            crashSound = Resources.Load<AudioClip>("SkyPulse/audio/crash");
            crystalSound = Resources.Load<AudioClip>("SkyPulse/audio/crystal");
            unlockSound = Resources.Load<AudioClip>("SkyPulse/audio/unlock");
        }

        private void CreateAmbientStars()
        {
            var random = new System.Random(742);
            // Three depths of soft lights cross the scene behind all gameplay objects.
            // Reuse these renderers for the whole session; wrapping is off-screen.
            for (var index = 0; index < 30; index += 1)
            {
                var star = CreateRenderer($"Ambient light {index + 1}", softCircleSprite, new Color(.60f, .84f, 1f, .20f + (index % 3) * .055f), -35 + index % 3);
                var viewportFraction = Mathf.Lerp(-.48f, .48f, (float)random.NextDouble());
                var x = GetViewportWidth() * viewportFraction;
                var y = Mathf.Lerp(-5.8f, 8.3f, (float)random.NextDouble());
                var size = Mathf.Lerp(.045f, .085f, (float)random.NextDouble()) * (1f + (index % 3) * .3f);
                star.transform.position = new Vector3(x, y, 0f);
                star.transform.localScale = Vector3.one * size;
                ambientStars.Add(new AmbientStar
                {
                    Transform = star.transform,
                    X = x,
                    ViewportFraction = viewportFraction,
                    Y = y,
                    Phase = (float)random.NextDouble() * Mathf.PI * 2f,
                    Speed = .18f + (index % 3) * .23f + (float)random.NextDouble() * .08f,
                    BaseSize = size,
                });
            }
        }

        private void CreateFloor()
        {
            var width = GetWorldWidth() + 1f;

            // Legacy floor base.
            // Keep the renderer because other code expects the reference,
            // but never draw the old giant black slab.
            floorBase = CreateRenderer(
                "Solid floor base",
                whiteSprite,
                Color.clear,
                -9
            );
            floorBase.transform.position = new Vector3(0f, GroundY, 0f);
            floorBase.transform.localScale = new Vector3(width, .01f, 1f);
            floorBase.enabled = false;

            // Legacy floor surface.
            // Also kept for compatibility but completely hidden.
            // This was the remaining dark slab covering the background.
            floorSurface = CreateRenderer(
                "Floor material",
                whiteSprite,
                Color.clear,
                -8
            );
            floorSurface.transform.position = new Vector3(0f, GroundY, 0f);
            floorSurface.transform.localScale = new Vector3(width, .01f, 1f);
            floorSurface.enabled = false;

            // Thin dark physical edge directly underneath the collision boundary.
            floorLip = CreateRenderer(
                "Floor solid edge",
                whiteSprite,
                new Color(.015f, .035f, .065f, .92f),
                -7
            );
            floorLip.transform.position = new Vector3(
                0f,
                GroundY - .035f,
                0f
            );
            floorLip.transform.localScale = new Vector3(
                width,
                .070f,
                1f
            );

            // Main cyan collision rail.
            floorGlow = CreateRenderer(
                "Floor energy rail",
                whiteSprite,
                new Color(.27f, .86f, 1f, .82f),
                -6
            );
            floorGlow.transform.position = new Vector3(
                0f,
                GroundY + .010f,
                0f
            );
            floorGlow.transform.localScale = new Vector3(
                width,
                .024f,
                1f
            );

            // Fine highlight to keep the floor crisp on a phone display.
            floorHighlight = CreateRenderer(
                "Floor edge highlight",
                whiteSprite,
                new Color(.78f, .94f, 1f, .48f),
                -5
            );
            floorHighlight.transform.position = new Vector3(
                0f,
                GroundY + .038f,
                0f
            );
            floorHighlight.transform.localScale = new Vector3(
                width,
                .006f,
                1f
            );
            // Keep references for world colour transitions, but leave the
            // bottom of the view clear. GroundY still defines floor collisions.
            floorLip.enabled = false;
            floorGlow.enabled = false;
            floorHighlight.enabled = false;
        }
        private void CreateBird()
        {
            bird = new GameObject("Flight bird").transform;
            bird.SetParent(transform, false);
            CreateBirdHitbox();

            // This compact, opaque inner silhouette is deliberately independent of
            // imported artwork. It gives every bird a readable body against bright
            // worlds and makes a missing/unsupported texture impossible to turn the
            // player avatar invisible on a phone.
            if (emergencyBirdSprite == null) emergencyBirdSprite = CreateEmergencyBirdSprite();
            var slowAura = CreateRenderer("Slow field aura", CreatePowerFieldSprite(PowerUpKind.TimePulse), new Color(.45f, .3f, 1f, 0f), 12, bird);
            slowAura.transform.localScale = Vector3.one * 1.42f;
            slowAuraRenderer = slowAura;
            var effectAura = CreateRenderer("Active power aura", CreatePowerFieldSprite(PowerUpKind.CrystalMagnet), new Color(.45f, .9f, 1f, 0f), 12, bird);
            effectAura.transform.localScale = Vector3.one * 1.16f;
            effectAuraRenderer = effectAura;
            var shieldAura = CreateRenderer("Pulse shield aura", CreatePowerFieldSprite(PowerUpKind.Aegis), new Color(.38f, 1f, .70f, 0f), 13, bird);
            shieldAura.transform.localScale = Vector3.one * 1.22f;
            shieldAuraRenderer = shieldAura;
            var bodyDepth = CreateRenderer("Bird dimensional bloom", softCircleSprite, new Color(.35f, .85f, 1f, 0f), 12, bird);
            bodyDepth.transform.localScale = new Vector3(1.52f, .62f, 1f);
            birdDepthRenderer = bodyDepth;
            birdArt = new GameObject("Bird idle artwork").transform;
            birdArt.SetParent(bird, false);
            birdRenderer = birdArt.gameObject.AddComponent<SpriteRenderer>();
            birdRenderer.sortingOrder = 14;
            // Never let a renderer spend even one frame with Unity's default white
            // graphic while artwork is resolving from Resources.
            birdRenderer.enabled = false;
            var parallaxArt = new GameObject("Bird parallax body artwork").transform;
            parallaxArt.SetParent(bird, false);
            birdParallaxRenderer = parallaxArt.gameObject.AddComponent<SpriteRenderer>();
            birdParallaxRenderer.sortingOrder = 13;
            birdParallaxRenderer.color = new Color(1f, 1f, 1f, 0f);
            var safetyArt = new GameObject("Bird visibility silhouette").transform;
            safetyArt.SetParent(bird, false);
            birdSafetyRenderer = safetyArt.gameObject.AddComponent<SpriteRenderer>();
            birdSafetyRenderer.sprite = emergencyBirdSprite;
            birdSafetyRenderer.sortingOrder = 16;
            birdSafetyRenderer.color = Color.white;
            birdRiseArt = new GameObject("Bird rise artwork").transform;
            birdRiseArt.SetParent(bird, false);
            birdRiseRenderer = birdRiseArt.gameObject.AddComponent<SpriteRenderer>();
            birdRiseRenderer.sortingOrder = 13;
            birdRiseRenderer.color = new Color(1f, 1f, 1f, 0f);
            birdFlapArt = new GameObject("Bird wing motion artwork").transform;
            birdFlapArt.SetParent(bird, false);
            birdFlapRenderer = birdFlapArt.gameObject.AddComponent<SpriteRenderer>();
            birdFlapRenderer.sortingOrder = 15;
            birdFlapRenderer.color = new Color(1f, 1f, 1f, 0f);
            var eyeGlint = CreateRenderer("Bird living eye glint", softCircleSprite, new Color(1f, 1f, 1f, 0f), 16, bird);
            eyeGlint.transform.localScale = Vector3.one * .062f;
            birdEyeGlintRenderer = eyeGlint;

#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
            collisionBirdDebug = CreateRenderer("Bird body collision guide", ringSprite, new Color(.35f, 1f, .72f, .78f), 31, birdHitbox);
            collisionBirdDebug.enabled = false;
#endif
        }

        private void CreateBirdHitbox()
        {
            // The game uses its own deterministic flight loop. This one capsule is
            // the authoritative queried shape; no Rigidbody2D is added to compete
            // with that proven motion/rotation system.
            birdHitbox = new GameObject("BirdHitbox").transform;
            birdHitbox.SetParent(transform, false);
            birdBodyCollider = birdHitbox.gameObject.AddComponent<CapsuleCollider2D>();
            birdBodyCollider.direction = CapsuleDirection2D.Horizontal;
            birdBodyCollider.size = new Vector2(BirdHitboxWidth, BirdHitboxHeight);
            birdBodyCollider.offset = Vector2.zero;
            birdBodyCollider.isTrigger = false;
            birdBodyCollider.enabled = false;
            SyncBirdHitbox();
        }

        private void CreateFlightFeedback()
        {
            flightFeedbackRenderer = CreateRenderer("Flight feedback bloom", softCircleSprite, new Color(1f, 1f, 1f, 0f), 11);
            flightFeedbackRenderer.enabled = false;
            flightFeedbackRingRenderer = CreateRenderer("Flight feedback pulse",
                CreateRadialSprite("Fine feedback ring", 128, .43f, .5f), Color.clear, 12);
            flightFeedbackRingRenderer.enabled = false;
        }

        private PowerUpPickup CreatePowerUp(int index)
        {
            return CreatePickup($"Power-up pickup {index + 1}");
        }

        private PowerUpPickup CreateCrystalPickup(int index)
        {
            return CreatePickup($"Crystal pellet {index + 1}");
        }

        private PowerUpPickup CreatePickup(string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);
            var glow = CreateRenderer("Pickup halo", softCircleSprite, Color.white, 10, root.transform);
            glow.transform.localScale = Vector3.one * 1.12f;
            var depth = CreateRenderer("Pickup dimensional bloom", softCircleSprite, Color.white, 12, root.transform);
            depth.transform.localScale = Vector3.one * 1.22f;
            var artwork = CreateRenderer("Pickup artwork", whiteSprite, Color.white, 13, root.transform);
            var spark = CreateRenderer("Pickup glint", softCircleSprite, Color.white, 14, root.transform);
            spark.transform.localScale = Vector3.one * .075f;
            return new PowerUpPickup
            {
                Root = root,
                Transform = root.transform,
                Glow = glow,
                Depth = depth,
                Artwork = artwork,
                Spark = spark,
                ArtworkBaseScale = Vector3.one,
            };
        }

        private PipePair CreatePipePair(int index)
        {
            var root = new GameObject($"SkyPulse pipe {index + 1}");
            root.transform.SetParent(transform, false);
            var pair = new PipePair
            {
                Root = root,
                Top = CreatePipeSurface(root.transform, "Top pipe"),
                Bottom = CreatePipeSurface(root.transform, "Bottom pipe"),
            };
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
            pair.DebugTopBody = CreateRenderer("Top collision body guide", whiteSprite, new Color(1f, .26f, .55f, .13f), 29, root.transform);
            pair.DebugTopCap = CreateRenderer("Top collision cap guide", whiteSprite, new Color(1f, .74f, .25f, .30f), 30, root.transform);
            pair.DebugBottomBody = CreateRenderer("Bottom collision body guide", whiteSprite, new Color(1f, .26f, .55f, .13f), 29, root.transform);
            pair.DebugBottomCap = CreateRenderer("Bottom collision cap guide", whiteSprite, new Color(1f, .74f, .25f, .30f), 30, root.transform);
            pair.DebugTopBody.enabled = false;
            pair.DebugTopCap.enabled = false;
            pair.DebugBottomBody.enabled = false;
            pair.DebugBottomCap.enabled = false;
#endif
            return pair;
        }

        private PipeSurface CreatePipeSurface(Transform parent, string label)
        {
            var surface = new PipeSurface
            {
                Artwork = CreateRenderer($"{label} cylindrical reflection", pipeBodySprite, new Color(1f, 1f, 1f, .25f), 5, parent),
                Outer = CreateRenderer($"{label} outer", roundedPanelSprite, Hex("#030613"), 2, parent),
                Panel = CreateRenderer($"{label} metal body", roundedPanelSprite, Hex("#0b3076"), 3, parent),
                Shade = CreateRenderer($"{label} side shade", pipeBodySprite, new Color(0f, 0f, 0f, .28f), 4, parent),
                RailLeft = CreateRenderer($"{label} left neon rail", whiteSprite, new Color(.27f, .92f, 1f, .25f), 5, parent),
                RailRight = CreateRenderer($"{label} right neon rail", whiteSprite, new Color(.27f, .92f, 1f, .25f), 5, parent),
                Core = CreateRenderer($"{label} powered core channel", softCircleSprite, new Color(.27f, .92f, 1f, .18f), 6, parent),
                CorePulse = CreateRenderer($"{label} travelling core pulse", softCircleSprite, new Color(.27f, .92f, 1f, 0f), 7, parent),
                Highlight = CreateRenderer($"{label} inner lip highlight", whiteSprite, new Color(.8f, .95f, 1f, .18f), 8, parent),
                Energy = CreateRenderer($"{label} energy seam", whiteSprite, Hex("#45eaff"), 9, parent),
                Scan = CreateRenderer($"{label} scan line", whiteSprite, new Color(.27f, .92f, 1f, 0f), 10, parent),
                Beacon = CreateRenderer($"{label} gateway beacon", ringSprite, new Color(.27f, .92f, 1f, 0f), 12, parent),
                CapGlow = CreateRenderer($"{label} collar neon bloom", softCircleSprite, new Color(.27f, .92f, 1f, 0f), 8, parent),
                CapOuter = CreateRenderer($"{label} plumbing collar shell", roundedPanelSprite, Hex("#030613"), 7, parent),
                CapAccent = CreateRenderer($"{label} plumbing collar accent", roundedPanelSprite, Hex("#45eaff"), 8, parent),
                CapPanel = CreateRenderer($"{label} authored plumbing collar", pipeCapSprite ?? roundedPanelSprite, Color.white, 9, parent),
                CapEnergy = CreateRenderer($"{label} collar energy seam", whiteSprite, Hex("#45eaff"), 11, parent),
            };
            surface.BodyCollider = CreatePipeCollider(parent, $"{label} body hitbox");
            surface.CapCollider = CreatePipeCollider(parent, $"{label} cap hitbox");
            return surface;
        }

        private static BoxCollider2D CreatePipeCollider(Transform parent, string name)
        {
            var hitbox = new GameObject(name);
            hitbox.transform.SetParent(parent, false);
            var collider = hitbox.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
            collider.offset = Vector2.zero;
            return collider;
        }

        private void CreateInterface()
        {
            uiFont = Font.CreateDynamicFontFromOSFont("Avenir Next", 16);
            if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            uiRoot = new GameObject("SkyPulse interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(uiRoot);
            var canvas = uiRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = uiRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            // Fit the complete authored interface on tall phones and wide editor
            // previews. Height-only scaling clipped edge controls on notched iPhones.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var safeRoot = new GameObject("Safe area", typeof(RectTransform));
            safeRoot.transform.SetParent(uiRoot.transform, false);
            safeAreaRoot = safeRoot.GetComponent<RectTransform>();
            ApplySafeArea();

            if (EventSystem.current == null)
            {
                var eventSystem = new GameObject("SkyPulse input", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(eventSystem);
            }

            var content = new GameObject("Fitted interface", typeof(RectTransform));
            content.transform.SetParent(safeAreaRoot, false);
            interfaceContentRoot = content.GetComponent<RectTransform>();
            interfaceContentRoot.anchorMin = interfaceContentRoot.anchorMax = new Vector2(.5f, .5f);
            interfaceContentRoot.sizeDelta = new Vector2(1080f, 2040f);
            homeScreen = CreateHomeScreen(interfaceContentRoot);
            hudScreen = CreateHud(interfaceContentRoot);
            pauseScreen = CreatePauseScreen(interfaceContentRoot);
            gameOverScreen = CreateGameOverScreen(interfaceContentRoot);
            customizeScreen = CreateCustomizeScreen(interfaceContentRoot);
            purchaseModal = CreatePurchaseModal(interfaceContentRoot);
            purchaseModal.SetActive(false);
            unlockRevealModal = CreateUnlockReveal(interfaceContentRoot);
            unlockRevealModal.SetActive(false);
            privacyScreen = CreatePrivacyScreen(interfaceContentRoot);
            privacyScreen.SetActive(false);
        }

        private void ApplySafeArea()
        {
            if (safeAreaRoot == null) return;
            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (safeArea == appliedSafeArea && screenSize == appliedScreenSize)
            {
                FitInterfaceToSafeArea();
                return;
            }

            appliedSafeArea = safeArea;
            appliedScreenSize = screenSize;
            if (screenSize.x <= 0 || screenSize.y <= 0) return;
            safeAreaRoot.anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            safeAreaRoot.anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
            FitInterfaceToSafeArea();
        }

        private void FitInterfaceToSafeArea()
        {
            if (interfaceContentRoot == null || safeAreaRoot == null) return;
            var available = safeAreaRoot.rect.size;
            if (available.x <= 0f || available.y <= 0f) return;
            var scale = Mathf.Min(available.x / 1080f, available.y / 2040f);
            interfaceContentRoot.localScale = Vector3.one * scale;
            // Controls fit the safe area; dimmers still cover the entire display.
            var canvasRect = uiRoot.GetComponent<RectTransform>();
            foreach (var backdrop in interfaceBackdrops)
            {
                backdrop.sizeDelta = canvasRect.rect.size / scale;
                backdrop.position = canvasRect.position;
            }
        }

        private void RefreshViewportDecor()
        {
            if (flightCamera == null) return;
            var viewportWidth = GetViewportWidth();
            if (Mathf.Approximately(viewportWidth, appliedViewportWidth)) return;
            appliedViewportWidth = viewportWidth;

            // Safe-area layout handles HUD controls. This complementary pass keeps
            // non-gameplay art fitted when a phone changes size, an editor Game view
            // is resized, or a wide desktop preview exposes decorative side margins.
            if (backgroundRenderer != null) FitBackgroundToCamera(backgroundRenderer, 1.1f);
            if (incomingBackground != null) FitBackgroundToCamera(incomingBackground, 1.1f);
            if (backgroundVeil != null) backgroundVeil.transform.localScale = new Vector3(viewportWidth + 1f, CameraHeight + .5f, 1f);

            var floorWidth = viewportWidth + 1f;
            if (floorBase != null) floorBase.transform.localScale = new Vector3(floorWidth, 2.24f, 1f);
            if (floorSurface != null) floorSurface.transform.localScale = new Vector3(floorWidth, 1.90f, 1f);
            if (floorLip != null) floorLip.transform.localScale = new Vector3(floorWidth, .12f, 1f);
            if (floorGlow != null) floorGlow.transform.localScale = new Vector3(floorWidth, .026f, 1f);
            if (floorHighlight != null) floorHighlight.transform.localScale = new Vector3(floorWidth, .010f, 1f);
            if (incomingBackground != null && incomingBackground.enabled)
                incomingBackground.transform.position = backgroundRenderer.transform.position;
            foreach (var star in ambientStars)
            {
                star.X = viewportWidth * star.ViewportFraction;
            }
        }

        private GameObject CreateHomeScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Home screen");
            // The sky remains visible through the launch deck; the bright brand,
            // original bird and cyan launch control form the primary reading path.
            CreateFullPanel(root.transform, "Home contrast veil", new Color(.005f, .012f, .05f, .12f));

            difficultyText = CreateChip(root.transform, new Vector2(-355f, 950f), "ENDLESS ROUTE", Hex("#8f64ff"));
            difficultyText.resizeTextForBestFit = true;
            difficultyText.resizeTextMinSize = 13;
            difficultyText.resizeTextMaxSize = 24;
            menuCrystalText = CreateCrystalChip(root.transform, new Vector2(355f, 950f), "✦  0", Hex("#45eaff"));

            menuTitleText = CreateText(root.transform, "SKYPULSE", new Vector2(0f, 835f), new Vector2(930f, 126f), 100, Hex("#d8fbff"), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic);
            var titleShadow = menuTitleText.GetComponent<Shadow>();
            if (titleShadow != null) titleShadow.enabled = false;
            AddOutline(menuTitleText.gameObject, new Color(.15f, .90f, 1f, .56f), .8f);
            CreateText(root.transform, "A  R  C  A  D  E", new Vector2(0f, 753f), new Vector2(720f, 52f), 37, Hex("#ff54df"), TextAnchor.MiddleCenter, FontStyle.Bold);
            CreateUiGlyph(root.transform, "Brand wing port", new Vector2(-326f, 753f), new Vector2(76f, 34f), Hex("#45eaff"), SkyPulseUiGlyph.Kind.WingMark);
            CreateUiGlyph(root.transform, "Brand wing starboard", new Vector2(326f, 753f), new Vector2(76f, 34f), Hex("#45eaff"), SkyPulseUiGlyph.Kind.WingMark);
            CreateText(root.transform, "F I N D  Y O U R  R H Y T H M", new Vector2(0f, 680f), new Vector2(860f, 40f), 26, Hex("#8eeeff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var titleRule = CreateImage(root.transform, "Title energy rule", new Vector2(0f, 639f), new Vector2(150f, 2f), new Color(.25f, .91f, 1f, .62f));
            titleRule.sprite = whiteSprite;
            titleRule.raycastTarget = false;

            var flightDeck = CreateLuminousPanel(root.transform, "Flight deck", new Vector2(0f, -315f), new Vector2(860f, 660f), new Color(.004f, .012f, .038f, .90f), new Color(.27f, .92f, 1f, .48f));

            flightDeck.GetComponent<Image>().raycastTarget = false;

            // Thin illuminated rails make the panel read as a floating flight console
            // rather than a single coloured rectangle.
            var deckTopRail = CreateImage(
                flightDeck,
                "Flight deck top energy rail",
                new Vector2(0f, 323f),
                new Vector2(730f, 2f),
                new Color(.32f, .95f, 1f, .72f));
            deckTopRail.sprite = whiteSprite;
            deckTopRail.raycastTarget = false;

            var deckTopGlow = CreateImage(
                flightDeck,
                "Flight deck top glow",
                new Vector2(0f, 319f),
                new Vector2(610f, 8f),
                new Color(.20f, .86f, 1f, .10f));
            deckTopGlow.sprite = whiteSprite;
            deckTopGlow.raycastTarget = false;

            var deckRule = CreateImage(
                flightDeck,
                "Flight deck rule",
                new Vector2(0f, -113f),
                new Vector2(690f, 1f),
                new Color(.27f, .86f, 1f, .22f));
            deckRule.sprite = whiteSprite;
            deckRule.raycastTarget = false;

            var deckBottomRail = CreateImage(
                flightDeck,
                "Flight deck lower energy rail",
                new Vector2(0f, -320f),
                new Vector2(620f, 2f),
                new Color(.25f, .82f, 1f, .34f));
            deckBottomRail.sprite = whiteSprite;
            deckBottomRail.raycastTarget = false;

            var deckLeftAccent = CreateImage(
                flightDeck,
                "Flight deck port accent",
                new Vector2(-424f, 0f),
                new Vector2(2f, 510f),
                new Color(.31f, .95f, 1f, .34f));
            deckLeftAccent.sprite = whiteSprite;
            deckLeftAccent.raycastTarget = false;

            var deckRightAccent = CreateImage(
                flightDeck,
                "Flight deck starboard accent",
                new Vector2(424f, 0f),
                new Vector2(2f, 510f),
                new Color(.31f, .95f, 1f, .34f));
            deckRightAccent.sprite = whiteSprite;
            deckRightAccent.raycastTarget = false;

            CreateUiGlyph(root.transform, "Home flight dock", new Vector2(0f, 355f), new Vector2(820f, 448f), new Color(.27f, .92f, 1f, .50f), SkyPulseUiGlyph.Kind.DockRing).Animate = true;

            var heroObject = new GameObject("Animated menu hero", typeof(RectTransform));
            heroObject.transform.SetParent(root.transform, false);
            menuHeroTransform = heroObject.GetComponent<RectTransform>();
            menuHeroTransform.anchorMin = new Vector2(.5f, .5f);
            menuHeroTransform.anchorMax = new Vector2(.5f, .5f);
            menuHeroTransform.pivot = new Vector2(.5f, .5f);
            menuHeroTransform.anchoredPosition = new Vector2(0f, 355f);
            menuHeroTransform.sizeDelta = new Vector2(850f, 480f);

            // No diffuse circles or faux glass behind the bird. Its silhouette and
            // animation carry the presentation, which stays crisp on phone screens.
            menuBirdSafetyImage = CreateImage(menuHeroTransform, "Menu bird guaranteed hero", Vector2.zero, new Vector2(880f, 440f), Color.white);
            menuBirdSafetyImage.sprite = emergencyBirdSprite;
            menuBirdSafetyImage.preserveAspect = true;
            menuBirdSafetyImage.raycastTarget = false;
            menuBirdShadowImage = CreateImage(menuHeroTransform, "Menu bird depth extrusion", new Vector2(-16f, -14f), new Vector2(850f, 420f), new Color(.004f, .010f, .040f, .48f));
            menuBirdShadowImage.preserveAspect = true;
            menuBirdShadowImage.raycastTarget = false;
            menuBirdRiseImage = CreateImage(menuHeroTransform, "Menu bird wing rise", Vector2.zero, new Vector2(850f, 420f), new Color(1f, 1f, 1f, 0f));
            menuBirdRiseImage.preserveAspect = true;
            menuBirdRiseImage.raycastTarget = false;
            menuBirdImage = CreateImage(menuHeroTransform, "Menu bird", Vector2.zero, new Vector2(850f, 420f), Color.white);
            menuBirdImage.preserveAspect = true;
            menuBirdImage.raycastTarget = false;
            menuBirdTransform = menuBirdImage.rectTransform;
            menuBirdFlapImage = CreateImage(menuHeroTransform, "Menu bird wing motion", Vector2.zero, new Vector2(850f, 420f), new Color(1f, 1f, 1f, 0f));
            menuBirdFlapImage.preserveAspect = true;
            menuBirdFlapImage.raycastTarget = false;
            menuBirdEyeGlintImage = CreateImage(menuHeroTransform, "Menu bird living eye glint", new Vector2(132f, 36f), new Vector2(24f, 24f), new Color(1f, 1f, 1f, .42f));
            menuBirdEyeGlintImage.sprite = softCircleSprite;
            menuBirdEyeGlintImage.raycastTarget = false;
            // This is a complete, coloured bird—not a transparent safety tint. Make
            // it the front-most hero layer so broken imported UI art cannot obscure
            // the character on the menu.
            menuBirdSafetyImage.transform.SetAsLastSibling();

            menuEquippedText = CreateText(root.transform, "EQUIPPED  ·  NEON FINCH", new Vector2(0f, 84f), new Vector2(790f, 46f), 30, Hex("#c7f4ff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var bestPanel = CreateLuminousPanel(root.transform, "Personal best", new Vector2(0f, -35f), new Vector2(580f, 92f), new Color(.012f, .034f, .094f, .84f), new Color(1f, .76f, .30f, .68f));
            bestPanel.GetComponent<Image>().raycastTarget = false;
            CreateText(bestPanel, "PERSONAL BEST", new Vector2(-105f, 0f), new Vector2(240f, 48f), 24, Hex("#ffc34d"), TextAnchor.MiddleLeft, FontStyle.Bold);
            menuBestText = CreateText(bestPanel, "0", new Vector2(134f, 0f), new Vector2(196f, 64f), 44, Hex("#f4fbff"), TextAnchor.MiddleRight, FontStyle.Bold);
            menuBestText.resizeTextForBestFit = true;
            menuBestText.resizeTextMinSize = 26;
            menuBestText.resizeTextMaxSize = 44;
            var fly = CreateNeonButton(root.transform, "PLAY", new Vector2(0f, -165f), new Vector2(720f, 128f), Hex("#45eaff"));
            fly.GetComponentInChildren<Text>().fontSize = 40;
            fly.onClick.AddListener(StartFlight);
            CreateText(root.transform, "TAP TO FLAP  ·  FIND THE GAP", new Vector2(0f, -269f), new Vector2(740f, 38f), 25, Hex("#d0e5f5"), TextAnchor.MiddleCenter, FontStyle.Normal);

            var hangar = CreateNeonButton(root.transform, "BIRD HANGAR", new Vector2(-187f, -355f), new Vector2(346f, 112f), Hex("#45eaff"));
            hangar.GetComponentInChildren<Text>().fontSize = 29;
            hangar.onClick.AddListener(OpenHangar);
            var upgrades = CreateNeonButton(root.transform, "UPGRADES", new Vector2(187f, -355f), new Vector2(346f, 112f), Hex("#ffc34d"));
            upgrades.GetComponentInChildren<Text>().fontSize = 29;
            upgrades.onClick.AddListener(OpenUpgrades);
            menuModeDetailText = CreateText(root.transform, "COLLECT CRYSTALS  ·  MASTER THE FLOW", new Vector2(0f, -481f), new Vector2(790f, 42f), 25, Hex("#8eeeff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            menuRouteText = CreateText(root.transform, "", new Vector2(0f, -540f), new Vector2(790f, 42f), 24, Hex("#c0d1e9"), TextAnchor.MiddleCenter, FontStyle.Normal);
            var privacy = CreateNeonButton(root.transform, "PRIVACY", new Vector2(0f, -755f), new Vector2(280f, 96f), Hex("#8fa7c4"));
            privacy.GetComponentInChildren<Text>().fontSize = 26;
            privacy.onClick.AddListener(() => privacyScreen.SetActive(true));
            return root;
        }

        private GameObject CreatePrivacyScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Privacy screen");
            CreateFullPanel(root.transform, "Privacy backdrop", new Color(.004f, .008f, .025f, .97f));
            var card = CreatePanel(root.transform, "Privacy card", Vector2.zero, new Vector2(880f, 1100f), Hex("#10182c"));
            AddOutline(card.gameObject, Hex("#45eaff"), 1.5f);
            CreateText(card, "YOUR GAME. YOUR DATA.", new Vector2(0f, 442f), new Vector2(800f, 64f), 36, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            const string notice = "SkyPulse plays offline. No account is required.\n\n" +
                "Your high score, crystals, unlocked birds, upgrades and preferences are saved on this device. Deleting the app may remove that progress.\n\n" +
                "This version includes no advertising, tracking, analytics or real-money purchases. Crystals are earned by playing.\n\n" +
                "Sharing a score copies text to your clipboard only when you choose Share. You decide where to paste it.\n\n" +
                "Apple may separately process App Store, device backup and diagnostic information under your Apple settings and its privacy policy.";
            var noticeText = CreateText(card, notice, new Vector2(0f, 30f), new Vector2(758f, 690f), 27, Hex("#d2def0"), TextAnchor.UpperLeft, FontStyle.Normal);
            noticeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            noticeText.verticalOverflow = VerticalWrapMode.Truncate;
            var links = SkyPulsePublicLinks.Load();
            if (SkyPulsePublicLinks.IsPublicHttpsUrl(links.privacyUrl))
            {
                var policy = CreateNeonButton(card, "PRIVACY POLICY", new Vector2(-202f, -352f), new Vector2(380f, 64f), Hex("#8fa7c4"));
                policy.onClick.AddListener(() => Application.OpenURL(links.privacyUrl));
            }
            if (SkyPulsePublicLinks.IsPublicHttpsUrl(links.supportUrl))
            {
                var support = CreateNeonButton(card, "SUPPORT", new Vector2(202f, -352f), new Vector2(380f, 64f), Hex("#8fa7c4"));
                support.onClick.AddListener(() => Application.OpenURL(links.supportUrl));
            }
            var close = CreateNeonButton(card, "BACK TO MENU", new Vector2(0f, -435f), new Vector2(470f, 88f), Hex("#45eaff"));
            close.onClick.AddListener(() => root.SetActive(false));
            return root;
        }

        private GameObject CreateHud(Transform parent)
        {
            var root = CreateScreen(parent, "Flight HUD");
            // Place the instrument clusters in the corners and leave the flight
            // corridor clear. The score remains readable over every route palette.
            var scoreCard = CreatePanel(root.transform, "Score instrument", new Vector2(-336f, 870f), new Vector2(260f, 220f), new Color(.008f, .022f, .060f, .92f));
            scoreCard.GetComponent<Image>().raycastTarget = false;
            AddOutline(scoreCard.gameObject, new Color(.27f, .92f, 1f, .65f), 1.2f);
            CreateText(scoreCard, "SCORE", new Vector2(0f, 70f), new Vector2(216f, 30f), 22, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            hudScoreText = CreateText(scoreCard, "0", new Vector2(0f, 9f), new Vector2(224f, 96f), 82, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            hudScoreText.resizeTextForBestFit = true;
            hudScoreText.resizeTextMinSize = 34;
            hudScoreText.resizeTextMaxSize = 82;
            hudBestText = CreateText(scoreCard, "BEST  0", new Vector2(0f, -77f), new Vector2(226f, 32f), 20, Hex("#b5c8de"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var pause = CreateNeonButton(root.transform, "Ⅱ", new Vector2(421f, 934f), new Vector2(88f, 88f), Hex("#b17cff"));
            pause.onClick.AddListener(PauseFlight);
            hudCrystalText = CreateCrystalChip(root.transform, new Vector2(365f, 816f), "✦  0", Hex("#45eaff"));
            hudModeText = CreateText(root.transform, "NEON CITY", new Vector2(-326f, 724f), new Vector2(280f, 32f), 17, Hex("#45eaff"), TextAnchor.MiddleLeft, FontStyle.Bold);
            scoreBurstText = CreateText(root.transform, "+1", new Vector2(0f, HudFeedbackY), new Vector2(720f, 54f), 27, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            scoreBurstText.gameObject.SetActive(false);
            hudPowerUpText = CreateText(root.transform, "", new Vector2(296f, 744f), new Vector2(340f, 34f), 17, Hex("#61f5b3"), TextAnchor.MiddleRight, FontStyle.Bold);
            hudPowerUpText.gameObject.SetActive(false);
            hudCoachText = CreateText(root.transform, "", new Vector2(0f, 617f), new Vector2(760f, 36f), 18, Hex("#b4f4ff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            hudCoachText.gameObject.SetActive(false);
            return root;
        }

        private GameObject CreatePauseScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Pause screen");
            CreateFullPanel(root.transform, "Pause dim", new Color(.005f, .012f, .04f, .78f));
            var card = CreatePanel(root.transform, "Pause card", new Vector2(0f, 20f), new Vector2(760f, 840f), Hex("#07152b"));
            AddOutline(card.gameObject, new Color(.27f, .92f, 1f, .66f), 1.5f);
            CreateUiGlyph(card, "Pause wing", new Vector2(0f, 354f), new Vector2(64f, 34f), Hex("#45eaff"), SkyPulseUiGlyph.Kind.WingMark);
            CreateText(card, "PAUSED", new Vector2(0f, 284f), new Vector2(650f, 72f), 54, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            CreateText(card, "YOUR FLIGHT IS WAITING", new Vector2(0f, 226f), new Vector2(620f, 36f), 24, Hex("#a4bfd8"), TextAnchor.MiddleCenter, FontStyle.Normal);
            var resume = CreateNeonButton(card, "RESUME", new Vector2(0f, 128f), new Vector2(600f, 104f), Hex("#45eaff"));
            resume.onClick.AddListener(ResumeFlight);
            CreateText(card, "FLIGHT COMFORT", new Vector2(0f, 36f), new Vector2(500f, 32f), 22, Hex("#91adc8"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var reduceMotion = CreateNeonButton(card, "", new Vector2(0f, -40f), new Vector2(600f, 92f), Hex("#61f5b3"));
            reduceMotionText = reduceMotion.GetComponentInChildren<Text>();
            reduceMotionText.fontSize = 26;
            reduceMotion.onClick.AddListener(ToggleReduceMotion);
            var haptics = CreateNeonButton(card, "", new Vector2(0f, -150f), new Vector2(600f, 92f), Hex("#b17cff"));
            hapticsText = haptics.GetComponentInChildren<Text>();
            hapticsText.fontSize = 26;
            haptics.onClick.AddListener(ToggleHaptics);
            var menu = CreateNeonButton(card, "RETURN TO MENU", new Vector2(0f, -284f), new Vector2(600f, 92f), Hex("#8fa7c4"));
            menu.GetComponentInChildren<Text>().fontSize = 26;
            menu.onClick.AddListener(ResetToMenu);
            return root;
        }

        private GameObject CreateGameOverScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Game over screen");
            CreateFullPanel(root.transform, "Game over dim", new Color(.005f, .012f, .04f, .82f));
            var card = CreatePanel(root.transform, "Game over card", Vector2.zero, new Vector2(820f, 1190f), Hex("#07152b"));
            AddOutline(card.gameObject, new Color(.27f, .92f, 1f, .66f), 1.5f);
            CreateText(card, "RUN COMPLETE", new Vector2(0f, 505f), new Vector2(720f, 70f), 48, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultModeText = CreateText(card, "ENDLESS ROUTE", new Vector2(0f, 451f), new Vector2(600f, 36f), 24, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            CreateUiGlyph(card, "Result flight horizon", new Vector2(0f, 408f), new Vector2(640f, 24f), new Color(.27f, .92f, 1f, .48f), SkyPulseUiGlyph.Kind.Horizon);
            CreateText(card, "SCORE", new Vector2(0f, 365f), new Vector2(600f, 36f), 26, Hex("#91adc8"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultScoreText = CreateText(card, "0", new Vector2(0f, 289f), new Vector2(650f, 130f), 108, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultScoreText.resizeTextForBestFit = true;
            resultScoreText.resizeTextMinSize = 54;
            resultScoreText.resizeTextMaxSize = 108;
            resultBestText = CreateText(card, "PERSONAL BEST  0", new Vector2(0f, 202f), new Vector2(660f, 40f), 28, Hex("#b5c8de"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultNewBestText = CreateText(card, "NEW PERSONAL BEST", new Vector2(0f, 153f), new Vector2(620f, 38f), 26, Hex("#ffc34d"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultReasonText = CreateText(card, "GATE IMPACT", new Vector2(0f, 106f), new Vector2(620f, 34f), 22, Hex("#e6b4d6"), TextAnchor.MiddleCenter, FontStyle.Normal);

            var rewards = CreatePanel(card, "Run rewards", new Vector2(0f, -38f), new Vector2(680f, 224f), Hex("#0b2137"));
            rewards.GetComponent<Image>().raycastTarget = false;
            var rewardIcon = CreateImage(rewards, "Run reward crystal", new Vector2(-271f, 22f), new Vector2(86f, 104f), Color.white);
            rewardIcon.sprite = LoadSprite(CrystalArtworkPath);
            rewardIcon.preserveAspect = true;
            rewardIcon.raycastTarget = false;
            resultCrystalsText = CreateText(rewards, "RUN CRYSTALS  ·  0", new Vector2(57f, 65f), new Vector2(500f, 42f), 28, Hex("#8eeeff"), TextAnchor.MiddleLeft, FontStyle.Bold);
            resultBonusText = CreateText(rewards, "TECH REWARD BONUS  ·  +0", new Vector2(57f, 17f), new Vector2(500f, 36f), 24, Hex("#ffc34d"), TextAnchor.MiddleLeft, FontStyle.Normal);
            var rewardRule = CreateImage(rewards, "Reward balance divider", new Vector2(0f, -20f), new Vector2(604f, 1f), new Color(.27f, .92f, 1f, .24f));
            rewardRule.raycastTarget = false;
            resultBalanceText = CreateText(rewards, "TOTAL BALANCE  ·  0", new Vector2(0f, -68f), new Vector2(604f, 42f), 28, Hex("#d6e9f6"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultWorldText = CreateText(card, "ROUTE REACHED  ·  NEON CITY", new Vector2(0f, -191f), new Vector2(710f, 38f), 24, Hex("#b5c8de"), TextAnchor.MiddleCenter, FontStyle.Normal);
            var flyAgain = CreateNeonButton(card, "RETRY", new Vector2(0f, -276f), new Vector2(680f, 108f), Hex("#45eaff"));
            flyAgain.onClick.AddListener(RestartFlight);
            var hangar = CreateNeonButton(card, "BIRD HANGAR", new Vector2(-177f, -397f), new Vector2(326f, 100f), Hex("#45eaff"));
            hangar.GetComponentInChildren<Text>().fontSize = 26;
            hangar.onClick.AddListener(OpenHangar);
            var upgrades = CreateNeonButton(card, "UPGRADES", new Vector2(177f, -397f), new Vector2(326f, 100f), Hex("#ffc34d"));
            upgrades.GetComponentInChildren<Text>().fontSize = 26;
            upgrades.onClick.AddListener(OpenUpgrades);
            var share = CreateNeonButton(card, "SHARE", new Vector2(0f, -509f), new Vector2(680f, 88f), Hex("#8fa7c4"));
            resultShareText = share.GetComponentInChildren<Text>();
            resultShareText.fontSize = 26;
            share.onClick.AddListener(CopyRunSummaryToClipboard);
            return root;
        }

        private GameObject CreateCustomizeScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Customize screen");
            var startSurface = root.AddComponent<SkyPulseRoundStartSurface>();
            startSurface.StartRound = StartRoundFromCustomize;
            var veil = CreateFullPanel(root.transform, "Customize veil", new Color(.006f, .012f, .036f, .74f));
            veil.GetComponent<Image>().raycastTarget = true;
            var back = CreateNeonButton(root.transform, "‹  MENU", new Vector2(-376f, 802f), new Vector2(220f, 76f), Hex("#8f64ff"));
            back.onClick.AddListener(ResetToMenu);
            customizeCrystalText = CreateCrystalChip(root.transform, new Vector2(365f, 802f), "✦  0", Hex("#45eaff"));
            CreateUiGlyph(root.transform, "Hangar flight insignia", new Vector2(0f, 800f), new Vector2(190f, 64f), Hex("#45eaff"), SkyPulseUiGlyph.Kind.WingMark);
            customizeTitle = CreateText(root.transform, "BIRD HANGAR", new Vector2(0f, 701f), new Vector2(880f, 76f), 52, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            customizeSubtitle = CreateText(root.transform, "15 BIRDS · ONE FAIR FLIGHT", new Vector2(0f, 647f), new Vector2(880f, 40f), 27, Hex("#87cde0"), TextAnchor.MiddleCenter, FontStyle.Bold);

            var labels = new[] { "HANGAR", "UPGRADES" };
            var categories = new[] { CosmeticCategory.Birds, CosmeticCategory.Upgrades };
            for (var index = 0; index < labels.Length; index += 1)
            {
                var tab = CreateNeonButton(root.transform, labels[index], new Vector2(-222f + index * 444f, 570f), new Vector2(422f, 76f), index == 0 ? Hex("#45eaff") : Hex("#ffc34d"));
                var category = categories[index];
                tab.onClick.AddListener(() => SetCosmeticCategory(category));
                collectionTabs.Add(tab);
                var rail = CreateImage(tab.transform, "Selected collection rail", new Vector2(0f, -36f), new Vector2(310f, 4f), Hex("#45eaff"));
                rail.raycastTarget = false;
                collectionTabRails.Add(rail);
            }

            var viewport = CreatePanel(root.transform, "Collection viewport", new Vector2(0f, -152f), new Vector2(950f, 1280f), new Color(.015f, .027f, .067f, .87f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            customizeScroll = scroll;
            viewport.GetComponent<Image>().raycastTarget = true;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;
            scroll.decelerationRate = .12f;
            scroll.scrollSensitivity = 80f;
            scroll.viewport = viewport;

            var contentObject = new GameObject("Collection content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            customizeContent = contentObject.GetComponent<RectTransform>();
            customizeContent.anchorMin = new Vector2(0f, 1f);
            customizeContent.anchorMax = new Vector2(1f, 1f);
            customizeContent.pivot = new Vector2(.5f, 1f);
            customizeContent.anchoredPosition = Vector2.zero;
            scroll.content = customizeContent;

            var scrollTrack = CreatePanel(viewport, "Collection scroll track", new Vector2(462f, 0f), new Vector2(10f, 1192f), new Color(.035f, .07f, .16f, .92f));
            scrollTrack.GetComponent<Image>().raycastTarget = true;
            var scrollbar = scrollTrack.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            var scrollHandle = CreatePanel(scrollTrack, "Collection scroll handle", Vector2.zero, new Vector2(10f, 112f), Hex("#45eaff"));
            var handleImage = scrollHandle.GetComponent<Image>();
            handleImage.raycastTarget = true;
            scrollbar.handleRect = scrollHandle;
            scrollbar.targetGraphic = handleImage;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalNormalizedPosition = 1f;
            return root;
        }

        private GameObject CreatePurchaseModal(Transform parent)
        {
            var root = CreateScreen(parent, "Bird skin purchase confirmation");
            CreateFullPanel(root.transform, "Purchase dim", new Color(.008f, .004f, .04f, .86f));
            var card = CreatePanel(root.transform, "Purchase card", new Vector2(0f, 18f), new Vector2(850f, 800f), Hex("#08132a"));
            AddOutline(card.gameObject, new Color(.27f, .92f, 1f, .48f), 1.5f);
            CreateUiGlyph(card, "Acquisition wing", new Vector2(0f, 352f), new Vector2(55f, 36f), Hex("#45eaff"), SkyPulseUiGlyph.Kind.WingMark);
            CreateUiGlyph(card, "Acquisition dock", new Vector2(0f, 128f), new Vector2(580f, 250f), new Color(.27f, .92f, 1f, .65f), SkyPulseUiGlyph.Kind.DockRing).Animate = true;
            CreateUiGlyph(card, "Acquisition runway", new Vector2(0f, -190f), new Vector2(640f, 32f), new Color(.27f, .92f, 1f, .48f), SkyPulseUiGlyph.Kind.Horizon);
            purchaseHalo = CreateImage(card, "Purchase focus ring", new Vector2(0f, 128f), new Vector2(340f, 340f), new Color(.27f, .92f, 1f, .20f));
            purchaseHalo.sprite = ringSprite;
            purchaseHalo.raycastTarget = false;
            purchasePreviewImage = CreateImage(card, "Bird skin preview", new Vector2(0f, 128f), new Vector2(465f, 248f), Color.white);
            purchasePreviewImage.preserveAspect = true;
            purchasePreviewImage.raycastTarget = false;
            purchaseUpgradeGlyph = CreateUiGlyph(card, "Upgrade branch emblem", new Vector2(0f, 128f), new Vector2(132f, 124f),
                Hex("#45eaff"), SkyPulseUiGlyph.Kind.WingMark);
            purchaseUpgradeGlyph.gameObject.SetActive(false);
            purchaseTitleText = CreateText(card, "UNLOCK BIRD?", new Vector2(0f, 278f), new Vector2(710f, 52f), 35, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            purchaseTitleText.resizeTextForBestFit = true;
            purchaseTitleText.resizeTextMinSize = 24;
            purchaseTitleText.resizeTextMaxSize = 35;
            purchaseDetailText = CreateText(card, "", new Vector2(0f, -38f), new Vector2(740f, 100f), 24, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            purchaseDetailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            purchaseDetailText.resizeTextForBestFit = true;
            purchaseDetailText.resizeTextMinSize = 21;
            purchaseDetailText.resizeTextMaxSize = 24;
            purchaseBalanceText = CreateText(card, "", new Vector2(0f, -120f), new Vector2(760f, 52f), 19, new Color(.9f, .94f, 1f, .72f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var cancel = CreateNeonButton(card, "CANCEL", new Vector2(-190f, -294f), new Vector2(330f, 84f), Hex("#8f64ff"));
            cancel.onClick.AddListener(ClosePurchaseModal);
            purchaseConfirmButton = CreateNeonButton(card, "UNLOCK", new Vector2(190f, -294f), new Vector2(330f, 84f), Hex("#45eaff"));
            purchaseConfirmText = purchaseConfirmButton.GetComponentInChildren<Text>();
            purchaseConfirmText.resizeTextForBestFit = true;
            purchaseConfirmText.resizeTextMinSize = 18;
            purchaseConfirmText.resizeTextMaxSize = 22;
            purchaseConfirmButton.onClick.AddListener(ConfirmPurchase);
            return root;
        }

        private GameObject CreateUnlockReveal(Transform parent)
        {
            var root = CreateScreen(parent, "Bird unlock reveal");
            CreateFullPanel(root.transform, "Unlock reveal dim", new Color(.005f, .004f, .026f, .92f));
            unlockRevealCard = CreatePanel(root.transform, "Unlock reveal card", new Vector2(0f, 22f), new Vector2(900f, 920f), Hex("#08132a"));
            AddOutline(unlockRevealCard.gameObject, new Color(.27f, .92f, 1f, .42f), 1.5f);
            CreateUiGlyph(unlockRevealCard, "New flight dock", new Vector2(0f, 80f), new Vector2(780f, 420f), new Color(.27f, .92f, 1f, .66f), SkyPulseUiGlyph.Kind.DockRing).Animate = true;
            CreateUiGlyph(unlockRevealCard, "New flight runway", new Vector2(0f, -210f), new Vector2(720f, 48f), Hex("#45eaff"), SkyPulseUiGlyph.Kind.Horizon);
            CreateUiGlyph(unlockRevealCard, "New flight wing port", new Vector2(-360f, 390f), new Vector2(50f, 42f), Hex("#45eaff"), SkyPulseUiGlyph.Kind.WingMark);
            CreateUiGlyph(unlockRevealCard, "New flight wing starboard", new Vector2(360f, 390f), new Vector2(50f, 42f), Hex("#45eaff"), SkyPulseUiGlyph.Kind.WingMark);

            unlockRevealFlash = CreateImage(unlockRevealCard, "Unlock flare", new Vector2(0f, 150f), new Vector2(720f, 720f), Color.clear);
            unlockRevealFlash.sprite = softCircleSprite;
            unlockRevealFlash.raycastTarget = false;
            unlockRevealHalo = CreateImage(unlockRevealCard, "Unlock halo", new Vector2(0f, 150f), new Vector2(560f, 560f), Color.clear);
            unlockRevealHalo.sprite = ringSprite;
            unlockRevealHalo.raycastTarget = false;
            unlockRevealBirdImage = CreateImage(unlockRevealCard, "Unlocked bird hero", new Vector2(0f, 150f), new Vector2(660f, 420f), Color.white);
            unlockRevealBirdImage.preserveAspect = true;
            unlockRevealBirdImage.raycastTarget = false;
            unlockRevealBirdTransform = unlockRevealBirdImage.rectTransform;

            CreateText(unlockRevealCard, "NEW BIRD UNLOCKED", new Vector2(0f, 398f), new Vector2(760f, 36f), 24, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            unlockRevealTitle = CreateText(unlockRevealCard, "NEW FLIGHT FORM", new Vector2(0f, 338f), new Vector2(780f, 68f), 44, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            unlockRevealTitle.resizeTextForBestFit = true;
            unlockRevealTitle.resizeTextMinSize = 28;
            unlockRevealTitle.resizeTextMaxSize = 44;
            unlockRevealTitle.verticalOverflow = VerticalWrapMode.Truncate;
            unlockRevealDetail = CreateText(unlockRevealCard, "EQUIPPED · READY TO FLY", new Vector2(0f, -145f), new Vector2(760f, 44f), 23, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var continueButton = CreateNeonButton(unlockRevealCard, "CONTINUE", new Vector2(0f, -292f), new Vector2(510f, 84f), Hex("#45eaff"));
            unlockRevealContinueButton = continueButton;
            continueButton.onClick.AddListener(CloseUnlockReveal);
            return root;
        }

        private void ShowUnlockReveal(Skin skin)
        {
            if (skin == null || unlockRevealModal == null || unlockRevealBirdImage == null) return;

            // This lookup deliberately does not go through the emergency-bird
            // fallback. A missing unlock frame must never become a different
            // bird's silhouette.
            var unlockPose = LoadPresentationPose(skin.UnlockPath);
            if (unlockPose == null)
            {
                Debug.LogWarning($"SkyPulse: {skin.Name} is missing its bespoke unlock frame at '{skin.UnlockPath}'. Showing its own current bird art until that frame is supplied.");
                unlockPose = LoadSprite(skin.ArtPath);
            }

            unlockRevealBirdImage.sprite = unlockPose;
            // Fit both portrait and landscape poses inside the same protected
            // art area, with room for the entrance rotation and feather tips.
            if (unlockPose != null)
            {
                var size = unlockPose.rect.size;
                var fit = Mathf.Min(600f / Mathf.Max(1f, size.x), 360f / Mathf.Max(1f, size.y));
                unlockRevealBirdTransform.sizeDelta = size * fit;
            }
            unlockRevealBirdTransform.pivot = new Vector2(.5f, .5f);
            activeUnlockSkin = skin;
            unlockRevealBirdImage.color = Color.white;
            unlockRevealTitle.text = skin.Name;
            unlockRevealDetail.text = "EQUIPPED  ·  NEW FLIGHT FORM ACQUIRED";
            unlockRevealTitle.color = Hex("#f4fbff");
            unlockRevealDetail.color = skin.Accent;
            unlockRevealHalo.color = new Color(skin.Accent.r, skin.Accent.g, skin.Accent.b, 0f);
            unlockRevealFlash.color = new Color(skin.Accent.r, skin.Accent.g, skin.Accent.b, 0f);
            unlockRevealTimer = 0f;
            unlockRevealCard.localScale = Vector3.one * .96f;
            unlockRevealBirdTransform.anchoredPosition = new Vector2(0f, 44f);
            unlockRevealBirdTransform.localScale = Vector3.one * .92f;
            unlockRevealBirdTransform.localRotation = Quaternion.Euler(0f, 0f, reduceMotionEnabled ? 0f : UnlockMotionFor(skin).x);
            unlockRevealHalo.rectTransform.localScale = Vector3.one * .48f;
            unlockRevealHalo.rectTransform.localRotation = Quaternion.identity;
            unlockRevealFlash.rectTransform.localScale = Vector3.one * .28f;
            unlockRevealContinueButton.interactable = false;
            unlockRevealModal.SetActive(true);
        }

        private void CloseUnlockReveal()
        {
            if (unlockRevealModal != null) unlockRevealModal.SetActive(false);
            activeUnlockSkin = null;
        }

        private void UpdateUnlockReveal(float deltaTime)
        {
            if (unlockRevealModal == null || !unlockRevealModal.activeSelf || unlockRevealCard == null) return;

            // Reduced Motion retains the celebratory confirmation while stripping
            // the overshoot and continuous spinning from the reveal.
            var duration = reduceMotionEnabled ? .24f : .72f;
            unlockRevealTimer = Mathf.Min(duration, unlockRevealTimer + deltaTime);
            var progress = Mathf.Clamp01(unlockRevealTimer / duration);
            var arrival = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / .58f));
            var pulse = 1f;
            var motion = UnlockMotionFor(activeUnlockSkin);

            unlockRevealCard.localScale = Vector3.one * Mathf.Lerp(.96f, 1f, arrival);
            unlockRevealBirdTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(44f, 80f, arrival));
            unlockRevealBirdTransform.localScale = Vector3.one * Mathf.Lerp(.92f, 1f, arrival);
            unlockRevealBirdTransform.localRotation = Quaternion.Euler(0f, 0f, reduceMotionEnabled ? 0f : Mathf.Lerp(motion.x, 0f, arrival));

            var haloColor = unlockRevealHalo.color;
            haloColor.a = .14f + (1f - progress) * .12f;
            unlockRevealHalo.color = haloColor;
            unlockRevealHalo.rectTransform.localScale = Vector3.one * (Mathf.Lerp(.48f, 1.16f, arrival) * pulse);
            unlockRevealHalo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, reduceMotionEnabled ? 0f : -progress * 92f * motion.z);

            var flashColor = unlockRevealFlash.color;
            flashColor.a = Mathf.Sin(Mathf.Clamp01(progress / .42f) * Mathf.PI) * .16f;
            unlockRevealFlash.color = flashColor;
            unlockRevealFlash.rectTransform.localScale = Vector3.one * Mathf.Lerp(.28f, 1.42f, Mathf.Clamp01(progress / .46f));
            if (progress >= 1f) unlockRevealContinueButton.interactable = true;
        }

        // A dedicated frame for each bird deserves a matching entrance. These subtle
        // per-skin values avoid turning the collection into sixteen copies of the same reveal.
        // X = entry tilt, Y = settled lift in UI pixels, Z = halo sweep multiplier.
        private static Vector3 UnlockMotionFor(Skin skin)
        {
            if (skin == null) return new Vector3(-10f, 0f, 1f);
            switch (skin.Id)
            {
                case "neon_finch": return new Vector3(-14f, 14f, 1.40f);
                case "chrome_raven": return new Vector3(-8f, 22f, .90f);
                case "prism_hummingbird": return new Vector3(-13f, 17f, 1.80f);
                case "koiwing_glider": return new Vector3(-20f, 6f, 1.65f);
                case "verdant_kite": return new Vector3(-9f, 27f, .70f);
                default: return new Vector3(-10f, 0f, 1f);
            }
        }

        private void Update()
        {
            ApplySafeArea();
            RefreshViewportDecor();
            UpdateHangarHeroPresentation();
            var frameDelta = Mathf.Min(Time.unscaledDeltaTime, MaximumSimulationCatchup);
            ambientTime += frameDelta;
            UpdateAmbientVisuals();

            UpdateMenuBird(frameDelta);
            UpdateUnlockReveal(frameDelta);
            UpdateScoreBurst(frameDelta);
            UpdateFlightFeedback(frameDelta);
            UpdateCrystalPickupBursts(frameDelta);

#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
            UpdateDevelopmentQualityControls();
#endif

            if (state == FlightState.Playing && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
            {
                PauseFlight();
                return;
            }
            if (state == FlightState.Paused && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
            {
                ResumeFlight();
                return;
            }

            if (state == FlightState.Menu)
            {
                if (privacyScreen != null && privacyScreen.activeSelf) return;
                if (WasTapped() && !PointerOverUi()) StartFlight();
                return;
            }

            if (state == FlightState.Customize)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
                    StartRoundFromCustomize();
                return;
            }

            if (state == FlightState.GameOver)
            {
                // A tap outside the result card has the same promise as the explicit
                // FLY AGAIN button: begin a clean Neon City route.
                if (WasTapped() && !PointerOverUi()) RestartFlight();
                return;
            }

            if (state == FlightState.Impact)
            {
                if (impactFrameTimer > 0f)
                {
                    impactFrameTimer = Mathf.Max(0f, impactFrameTimer - frameDelta);
                    return;
                }
                if (impactTumbleTimer > 0f)
                {
                    impactTumbleTimer = Mathf.Max(0f, impactTumbleTimer - frameDelta);
                    UpdateImpactTumble(frameDelta);
                    return;
                }
                state = FlightState.GameOver;
                RefreshScreens();
                return;
            }

            if (state != FlightState.Playing) return;

            if (WasTapped() && !PointerOverUi()) BufferFlapInput();

            // Simulating the same short steps at 30, 60, and 120 FPS makes the flight
            // path repeatable. Rendering remains frame-rate independent and smooth.
            simulationAccumulator = Mathf.Min(simulationAccumulator + frameDelta, MaximumSimulationCatchup);
            while (simulationAccumulator >= SimulationStep && state == FlightState.Playing)
            {
                SimulateFlight(SimulationStep);
                simulationAccumulator -= SimulationStep;
            }

            // Physics can run more than once before Unity presents the next
            // image. Advance the authored artwork once per rendered frame so
            // the player sees every adjacent wing pose rather than a skip.
            if (state == FlightState.Playing)
            {
                UpdateGameplayWingState(frameDelta);
                UpdateBirdWingMotion();
            }

            UpdateFlightCoach();

#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
            UpdateCollisionDebug();
#endif
        }

#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
        private void UpdateDevelopmentQualityControls()
        {
            if (Input.GetKeyDown(KeyCode.F1)) SetDevelopmentFrameRateCap(30);
            if (Input.GetKeyDown(KeyCode.F2)) SetDevelopmentFrameRateCap(60);
            if (Input.GetKeyDown(KeyCode.F3)) SetDevelopmentFrameRateCap(120);
            if (Input.GetKeyDown(KeyCode.F4)) collisionDebugEnabled = !collisionDebugEnabled;
        }

        private void SetDevelopmentFrameRateCap(int frameRate)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = frameRate;
        }
#endif

        private void SimulateFlight(float deltaTime)
        {
            ConsumeBufferedFlap();
            UpdatePowerUpEffects(deltaTime);
            if (shieldHitStopTimer > 0f)
            {
                shieldHitStopTimer = Mathf.Max(0f, shieldHitStopTimer - deltaTime);

                return;
            }

            // Time Pulse scales the entire simulation step, rather than just the
            // gate speed, so the bird keeps the exact same handling relationship.
            var simulationDelta = slowFieldTimer > 0f ? deltaTime * .70f : deltaTime;
            UpdateWorldTransition(simulationDelta);
            UpdateBird(simulationDelta);
            if (state != FlightState.Playing) return;
            if (worldTransitionTimer > 0f || worldRecoveryTimer > 0f)
            {

                return;
            }
            UpdatePipes(simulationDelta);
            if (state != FlightState.Playing) return;
            if (worldTransitionTimer <= 0f)
            {
                UpdateCrystalPickups(simulationDelta);
                UpdatePowerUps(simulationDelta);
            }

        }

        private void BufferFlapInput()
        {
            if (Time.unscaledTime - lastFlapInputTime < InputLockoutSeconds) return;
            lastFlapInputTime = Time.unscaledTime;
            var bufferSeconds = ActiveTuning().InputBufferSeconds;
            bufferedFlapUntil = Time.unscaledTime + bufferSeconds;
        }

        private void ConsumeBufferedFlap()
        {
            if (bufferedFlapUntil < 0f) return;
            if (Time.unscaledTime > bufferedFlapUntil)
            {
                bufferedFlapUntil = -1f;
                return;
            }

            bufferedFlapUntil = -1f;
            Flap();
        }

        private void UpdateAmbientVisuals()
        {
            var ambientMotion = reduceMotionEnabled ? 0f : 1f;
            if (backgroundRenderer != null)
            {
                backgroundRenderer.transform.position = new Vector3(Mathf.Sin(ambientTime * .18f) * .22f * ambientMotion, .12f + Mathf.Sin(ambientTime * .23f) * .14f * ambientMotion, 0f);
            }
            if (incomingBackground != null && incomingBackground.enabled)
                incomingBackground.transform.position = backgroundRenderer.transform.position;
            foreach (var star in ambientStars)
            {
                var span = GetViewportWidth() + 1f;
                var x = Mathf.Repeat(star.X + span * .5f - ambientTime * star.Speed * ambientMotion, span) - span * .5f;
                var y = star.Y + Mathf.Sin(ambientTime * .35f + star.Phase) * .18f * ambientMotion;
                star.Transform.position = new Vector3(x, y, 0f);
                var scale = .96f + Mathf.Sin(ambientTime * star.Speed * 1.6f + star.Phase) * .04f * ambientMotion;
                star.Transform.localScale = Vector3.one * Mathf.Max(.012f, star.BaseSize * scale);
            }
        }

        private void UpdateMenuBird(float deltaTime)
        {
            if (state != FlightState.Menu || menuBirdImage == null || menuBirdTransform == null || equippedSkin == null) return;
            menuPresentationTime += deltaTime;
            var menuMotion = reduceMotionEnabled ? .35f : 1f;
            menuWingTimer += deltaTime * menuMotion;
            menuWingTimer = Mathf.Repeat(menuWingTimer, SharedWingAnimationSeconds);

            var wingPhase =
                menuWingTimer / SharedWingAnimationSeconds;
            GetWingWeights(wingPhase, out var riseStrength, out var flapStrength);
            var usesFlapFrameSequence = UsesFlapFrameSequence();
            if (usesFlapFrameSequence)
            {
                var pose = SelectFlapFrame(wingPhase);
                if (pose != null && menuBirdImage.sprite != pose) menuBirdImage.sprite = pose;
                if (pose != null) menuBirdImage.enabled = true;
                if (pose != null && menuBirdShadowImage != null && menuBirdShadowImage.sprite != pose) menuBirdShadowImage.sprite = pose;
                if (pose != null && menuBirdShadowImage != null) menuBirdShadowImage.enabled = true;
                LayoutRegisteredMenuFrame(menuBirdImage, pose);
                LayoutRegisteredMenuFrame(menuBirdShadowImage, pose);
                menuBirdImage.color = Color.white;
                if (menuBirdRiseImage != null) menuBirdRiseImage.color = Color.clear;
                if (menuBirdFlapImage != null) menuBirdFlapImage.color = Color.clear;
            }
            else
            {
                menuBirdImage.color = new Color(1f, 1f, 1f, 1f - Mathf.Max(riseStrength * .78f, flapStrength * .50f));
                if (menuBirdRiseImage != null)
                {
                    var showRise = menuBirdRiseImage.sprite != null;
                    menuBirdRiseImage.color = new Color(1f, 1f, 1f, showRise ? riseStrength * .92f : 0f);
                    menuBirdRiseImage.rectTransform.anchoredPosition = new Vector2(-riseStrength * 6f, riseStrength * 10f);
                    menuBirdRiseImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, riseStrength * 4.4f);
                    menuBirdRiseImage.rectTransform.localScale = Vector3.one * (1f + riseStrength * .06f);
                }
                if (menuBirdFlapImage != null)
                {
                    menuBirdFlapImage.color = new Color(1f, 1f, 1f, flapStrength * (1f - riseStrength * .84f) * .84f);
                    menuBirdFlapImage.rectTransform.anchoredPosition = new Vector2(flapStrength * 5f, flapStrength * 7f);
                    menuBirdFlapImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -flapStrength * 4.5f);
                }
            }

            var hover = Mathf.Sin(ambientTime * 1.7f) * menuMotion;
            var authoredFlight = usesFlapFrameSequence;
            var flightTilt = authoredFlight ? hover * .75f - flapStrength * .85f : hover * 2.2f - flapStrength * 1.6f;
            var glideLean = (authoredFlight ? Mathf.Sin(ambientTime * 1.16f) * .24f : Mathf.Sin(ambientTime * 1.16f) * .65f) * menuMotion;
            var intro = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(menuPresentationTime / .48f));
            if (menuHeroTransform != null)
            {
                menuHeroTransform.anchoredPosition = new Vector2(Mathf.Sin(ambientTime * .82f) * (authoredFlight ? 3f : 8f) * menuMotion, 355f + hover * (authoredFlight ? 5f : 13f));
                menuHeroTransform.localScale = Vector3.one * Mathf.Lerp(.94f, 1f + Mathf.Sin(ambientTime * 3.4f) * (authoredFlight ? .006f : .018f) * menuMotion, intro);
            }
            menuBirdTransform.localRotation = Quaternion.Euler(0f, 0f, flightTilt + glideLean);
            menuBirdTransform.localScale = authoredFlight
                ? new Vector3(1f + riseStrength * .012f, 1f - riseStrength * .008f, 1f)
                : new Vector3(1f + Mathf.Sin(ambientTime * 3.4f) * .018f + riseStrength * .035f, 1f - riseStrength * .018f, 1f);
            if (menuBirdShadowImage != null)
            {
                // A crisp, offset silhouette gives the hero real spatial separation
                // from the deck without a bloom, blur, or translucent glass effect.
                var showPremiumDepth = authoredFlight && menuBirdShadowImage.sprite != null;
                menuBirdShadowImage.enabled = showPremiumDepth;
                menuBirdShadowImage.color = showPremiumDepth ? new Color(.004f, .010f, .040f, .48f) : Color.clear;
                menuBirdShadowImage.rectTransform.anchoredPosition = authoredFlight
                    ? new Vector2(-17f - riseStrength * 3f, -16f - flapStrength * 2f)
                    : Vector2.zero;
                menuBirdShadowImage.rectTransform.localRotation = menuBirdTransform.localRotation;
                menuBirdShadowImage.rectTransform.localScale = authoredFlight
                    ? menuBirdTransform.localScale * 1.012f
                    : Vector3.one;
            }
            if (menuBirdSafetyImage != null)
            {
                // This is a genuine fallback, not a permanent layer above the
                // selected bird. It is only shown when that bird's art cannot load.
                var usesEmergencyFallback = menuBirdSafetyImage.sprite == emergencyBirdSprite;
                menuBirdSafetyImage.enabled = usesEmergencyFallback;
                if (usesEmergencyFallback)
                {
                    menuBirdSafetyImage.color = Color.white;
                    menuBirdSafetyImage.rectTransform.localRotation = menuBirdTransform.localRotation;
                    menuBirdSafetyImage.rectTransform.localScale = menuBirdTransform.localScale * (1f + riseStrength * .018f);
                }
            }
            if (menuBirdEyeGlintImage != null)
            {
                menuBirdEyeGlintImage.gameObject.SetActive(!usesFlapFrameSequence);
                if (!usesFlapFrameSequence)
                {
                    var blinkCycle = Mathf.Repeat(ambientTime * .27f + .18f, 1f);
                    var eyelid = blinkCycle < .045f ? Mathf.SmoothStep(.12f, 1f, blinkCycle / .045f) : 1f;
                    var glint = Color.Lerp(Color.white, equippedSkin.Accent, .18f);
                    glint.a = (.34f + riseStrength * .12f) * eyelid;
                    menuBirdEyeGlintImage.color = glint;
                    menuBirdEyeGlintImage.rectTransform.anchoredPosition = new Vector2(132f + riseStrength * 5f, 36f + hover * 2.4f);
                    menuBirdEyeGlintImage.rectTransform.localScale = Vector3.one * (.92f + riseStrength * .18f);
                }
            }
            if (menuTitleText != null)
            {
                menuTitleText.rectTransform.localScale = reduceMotionEnabled
                    ? Vector3.one
                    : Vector3.one * (1f + Mathf.Sin(ambientTime * 2.1f) * .006f);
            }
        }

        private void UpdateScoreBurst(float deltaTime)
        {
            if (scoreBurstTimer <= 0f || scoreBurstText == null)
                return;

            scoreBurstTimer -= deltaTime;

            if (scoreBurstTimer <= 0f)
            {
                scoreBurstText.gameObject.SetActive(false);
                scoreBurstText.rectTransform.localScale = Vector3.one;
                return;
            }

            var duration = Mathf.Max(.01f, scoreBurstDuration);

            var t =
                1f -
                Mathf.Clamp01(
                    scoreBurstTimer / duration
                );

            var riseDistance =
                scoreBurstIsCrystal
                    ? 58f
                    : 44f;

            scoreBurstText.rectTransform.anchoredPosition =
                new Vector2(
                    0f,
                    HudFeedbackY + t * riseDistance
                );

            // Keep each event's colour, including the incoming world's accent.
            var color = scoreBurstText.color;

            color.a =
                Mathf.Clamp01(
                    scoreBurstTimer / .13f
                );

            scoreBurstText.color = color;

            var popStrength =
                scoreBurstIsCrystal
                    ? .16f
                    : .08f;

            var popPhase =
                Mathf.Clamp01(t / .55f);

            var pop =
                Mathf.Sin(
                    Mathf.PI * popPhase
                ) * popStrength;

            scoreBurstText.rectTransform.localScale =
                Vector3.one * (1f + pop);
        }
        private void UpdateBird(float deltaTime)
        {
            birdVelocity = Mathf.Max(ActiveMaxFallVelocity(), birdVelocity + ActiveGravity() * deltaTime);
            birdY += birdVelocity * deltaTime;
            // This transient timer drives only the flight impulse, banking and
            // thrust feedback. Authored wing frames advance solely through the
            // input-driven state controller below.
            wingTimer = Mathf.Min(WingCycleSeconds, wingTimer + deltaTime);
            bird.position = new Vector3(BirdX, birdY, 0f);
            var flapKick = Mathf.Exp(-wingTimer * 12f);
            // Rise is a compact -18° bank; a full terminal fall reaches +70°.
            // The sprite body is shared, so this remains visual personality rather
            // than a hidden per-bird handling difference.
            var targetTilt = Mathf.Clamp(
     birdVelocity * 1.65f + flapKick * 3.0f,
     -18f,
     11f
 );

            birdTilt = Mathf.SmoothDamp(
                birdTilt,
                targetTilt,
                ref birdTiltVelocity,
                .10f,
                180f,
                deltaTime
            );
            bird.rotation = Quaternion.Euler(0f, 0f, birdTilt);
            var collisionRadius = BirdHitboxVerticalExtent();
            var hitboxOffset = BirdHitboxWorldOffset();

            var ceiling = CameraHeight * .5f - collisionRadius;
            if (birdY + hitboxOffset.y >= ceiling)
            {
                // The ceiling is a soft clamp, never an off-screen death.
                birdY = ceiling - hitboxOffset.y;
                if (birdVelocity > 0f) birdVelocity = 0f;
                bird.position = new Vector3(BirdX, birdY, 0f);
            }

            if (birdY + hitboxOffset.y - collisionRadius <= GroundY)
            {
                // A recently shattered Aegis protects every collision surface for
                // its full recovery beat, including the lower hazard. Re-centering
                // prevents a harmless floor touch from repeatedly consuming checks.
                if (shieldImmunityTimer <= 0f && !UseShield())
                {
                    lastCrashReason = "GROUND CONTACT";
                    EndFlight();
                    return;
                }

                birdY = GroundY + collisionRadius + .14f - hitboxOffset.y;
                birdVelocity = 0f;
                bird.position = new Vector3(BirdX, birdY, 0f);
            }

            SyncBirdHitbox();
        }

        private void UpdateGameplayWingState(float deltaTime)
        {
            if (!UsesFlapFrameSequence())
            {
                ResetGameplayWingState();
                return;
            }

            var settledFrame = flapFrameBirdSprites.Length - 1;
            if (gameplayWingState == GameplayWingState.Settled)
            {
                gameplayWingFrameIndex = settledFrame;
                gameplayWingFrameTimer = 0f;
                return;
            }

            gameplayWingFrameIndex =
                Mathf.Clamp(gameplayWingFrameIndex, 0, settledFrame);

            // Five adjacent moves lift a six-frame bird and five return it to
            // glide. Do not use a while loop here: a slow render must extend
            // the stroke, never make a complete drawing disappear between
            // presented frames.
            var frameStepCount = (flapFrameBirdSprites.Length * 2) - 2;
            var frameSeconds = GameplayWingAnimationSeconds / frameStepCount;
            gameplayWingFrameTimer += Mathf.Max(0f, deltaTime);
            if (gameplayWingFrameTimer < frameSeconds)
                return;

            // Keep the fractional cadence, but discard whole overdue steps after
            // a hitch so the next frames do not rush through a timing backlog.
            gameplayWingFrameTimer %= frameSeconds;

            switch (gameplayWingState)
            {
                case GameplayWingState.Upstroke:
                    // Settled -> raised: 5 -> 4 -> 3 -> 2 -> 1 -> 0.
                    gameplayWingFrameIndex =
                        Mathf.Max(0, gameplayWingFrameIndex - 1);
                    if (gameplayWingFrameIndex == 0)
                        gameplayWingState = GameplayWingState.Downstroke;
                    break;

                case GameplayWingState.Downstroke:
                    // Raised -> settled: 0 -> 1 -> 2 -> 3 -> 4 -> 5.
                    gameplayWingFrameIndex =
                        Mathf.Min(settledFrame, gameplayWingFrameIndex + 1);
                    if (gameplayWingFrameIndex == settledFrame)
                    {
                        gameplayWingState = GameplayWingState.Settled;
                        gameplayWingFrameTimer = 0f;
                    }
                    break;

                default:
                    ResetGameplayWingState();
                    break;
            }
        }

        private void ResetGameplayWingState()
        {
            gameplayWingState = GameplayWingState.Settled;
            gameplayWingFrameTimer = 0f;
            gameplayWingFrameIndex =
                UsesFlapFrameSequence()
                    ? flapFrameBirdSprites.Length - 1
                    : 0;
        }

        private float GameplayWingFramePhase()
        {
            if (!UsesFlapFrameSequence() || gameplayWingState == GameplayWingState.Settled)
                return GameplayWingSettledPhase;

            var frameStepCount = (flapFrameBirdSprites.Length * 2) - 2;
            var frameIndex =
                Mathf.Clamp(gameplayWingFrameIndex, 0, flapFrameBirdSprites.Length - 1);

            // Sample the middle of the menu selector's matching frame band.
            // This retains its authored ordering while keeping the chosen pose
            // stable until this controller explicitly advances it.
            return (frameIndex + .5f) / frameStepCount;
        }

        private void SyncBirdHitbox()
        {
            if (bird == null || birdHitbox == null) return;
            birdHitbox.position = bird.position + BirdHitboxWorldOffset();
            birdHitbox.rotation = bird.rotation;
            birdHitbox.localScale = Vector3.one;
        }

        private Vector3 BirdHitboxWorldOffset()
        {
            return bird == null
                ? new Vector3(BirdHitboxOffsetX, BirdHitboxOffsetY, 0f)
                : bird.rotation * new Vector3(BirdHitboxOffsetX, BirdHitboxOffsetY, 0f);
        }

        private float BirdHitboxVerticalExtent()
        {
            // A horizontal capsule is a central line segment with two round ends.
            // This is its exact upright extent after the bird's current rotation.
            var halfRadius = BirdHitboxRadius;
            var halfSegment = Mathf.Max(0f, BirdHitboxWidth - BirdHitboxHeight) * .5f;
            return halfRadius + Mathf.Abs(Mathf.Sin(birdTilt * Mathf.Deg2Rad)) * halfSegment;
        }

        private float BirdHitboxHorizontalExtent()
        {
            var halfRadius = BirdHitboxRadius;
            var halfSegment = Mathf.Max(0f, BirdHitboxWidth - BirdHitboxHeight) * .5f;
            return halfRadius + Mathf.Abs(Mathf.Cos(birdTilt * Mathf.Deg2Rad)) * halfSegment;
        }

        private static bool CollidersOverlap(Collider2D first, Collider2D second)
        {
            return first != null && second != null && first.enabled && second.enabled && first.Distance(second).isOverlapped;
        }

        private void UpdatePipes(float deltaTime)
        {
            var speed = ActiveScrollSpeed();
            var furthestX = float.MinValue;
            foreach (var pair in pipePool)
            {
                if (pair != null && pair.Root.activeSelf && pair.X > furthestX) furthestX = pair.X;
            }
            if (furthestX == float.MinValue) furthestX = GetWorldWidth() * .5f + 2f;

            foreach (var pair in pipePool)
            {
                if (pair == null || !pair.Root.activeSelf) continue;
                pair.X -= speed * deltaTime;
                pair.Root.transform.localPosition = new Vector3(pair.X, 0f, 0f);
                if (pair.X < -GetWorldWidth() * .5f - PipeWidth)
                {
                    ConfigurePipe(pair, furthestX + RoutePipeSpacing());
                }
                furthestX = Mathf.Max(furthestX, pair.X);
                UpdateRouteGateMotion(pair, deltaTime);
                AnimatePipePair(pair);
            }

            // Both the bird capsule and the pipe body/cap boxes are moved by the
            // deterministic simulation. Sync once, then query those exact shapes—
            // no wing, trail, glow, or invisible visual layer participates.
            Physics2D.SyncTransforms();

            foreach (var pair in pipePool)
            {
                if (pair == null || !pair.Root.activeSelf) continue;
                if (shieldImmunityTimer <= 0f && BirdCollidesWithPipe(pair))
                {
                    if (!UseShield())
                    {
                        lastCrashReason = "GATE IMPACT";
                        EndFlight();
                        return;
                    }

                    pair.X = BirdX - PipeCollisionWidth - .22f;
                    pair.Root.transform.localPosition = new Vector3(pair.X, 0f, 0f);
                    pair.Passed = true;
                    RetireCrystalPickupsForGate(pair);
                    RetirePowerUpsForGate(pair);
                    continue;
                }

                var birdBodyX = birdHitbox == null ? BirdX : birdHitbox.position.x;
                if (!pair.Passed && pair.X + PipeCollisionWidth * .5f < birdBodyX - BirdHitboxHorizontalExtent())
                {
                    pair.Passed = true;
                    var perfect = Mathf.Abs(birdY - pair.GapCenter) <= ActiveTuning().PerfectPassWindow;
                    if (perfect)
                    {
                        perfectPasses += 1;
                        ApplyPrecisionHarvesterReward();
                    }
                    // One physical gate is always exactly one score. Crystals,
                    // birds and permanent economy never leak into this number.
                    score += 1;
                    ApplyStreakCapacitorReward();
                    hudScoreText.text = score.ToString();
                    AdvanceFlightCoach();
                    ShowScoreBurst(1, perfect);
                    TriggerFlightFeedback(perfect ? equippedSkin.Accent : Hex("#45eaff"), perfect ? .26f : .13f);
                    if (perfect) PulseHaptic(.10f);
                    Play(scoreSound);

                    var nextWorld = WorldIndexForScore(score);
                    if (nextWorld != routeWorldIndex)
                    {
                        BeginWorldTransition(nextWorld);
                        return;
                    }
                }
            }
        }

        private bool BirdCollidesWithPipe(PipePair pair)
        {
            if (pair == null || birdBodyCollider == null || !birdBodyCollider.enabled) return false;
            return CollidersOverlap(birdBodyCollider, pair.Top.BodyCollider)
                || CollidersOverlap(birdBodyCollider, pair.Top.CapCollider)
                || CollidersOverlap(birdBodyCollider, pair.Bottom.BodyCollider)
                || CollidersOverlap(birdBodyCollider, pair.Bottom.CapCollider);
        }

        private float ActiveScrollSpeed()
        {
            return GetWorldWidth() * RouteSpeedFraction(score);
        }

        private static float RouteSpeedFraction(int routeScore)
        {
            // Continuous course progression from the first gate.
            if (routeScore < 15) return Mathf.Lerp(.345f, .395f, routeScore / 14f);
            if (routeScore < 25) return .40f;
            if (routeScore < 30) return Mathf.Lerp(.408f, .424f, (routeScore - 25) / 4f);
            if (routeScore < 35) return Mathf.Lerp(.43f, .44f, (routeScore - 30) / 4f);
            if (routeScore < 40) return .44f;
            // Modest increases at 40 and 60, settling over four more gates.
            if (routeScore < 45) return Mathf.Lerp(.45f, .46f, (routeScore - 40) / 4f);
            if (routeScore < 60) return .46f;
            if (routeScore < 65) return Mathf.Lerp(.47f, .48f, (routeScore - 60) / 4f);
            return .48f;
        }

        private float RoutePipeSpacing()
        {
            return GetWorldWidth() * PipeSpacingFraction;
        }

        private float ActiveGravity()
        {
            return EndlessTuning.Gravity;
        }

        private float ActiveMaxFallVelocity()
        {
            return ActiveTuning().MaxFallVelocity;
        }

        private float ActiveFlapVelocity()
        {
            return EndlessTuning.FlapVelocity;
        }

        private FlightTuning ActiveTuning()
        {
            return EndlessTuning;
        }

        private bool AllowsPowerUps()
        {
            return true;
        }

        private void UpdatePowerUps(float deltaTime)
        {
            foreach (var pickup in powerUpPool)
            {
                if (!pickup.Active) continue;

                // A pickup belongs to a live gate, so it is always presented in open air
                // rather than floating into a pipe body or spawning at a random height.
                if (HasActivePowerEffect() || pickup.Gate == null || !pickup.Gate.Root.activeSelf || pickup.Gate.Passed)
                {
                    DeferPowerUp(pickup, 0f);
                    continue;
                }

                var targetX = pickup.Gate.X + pickup.LocalXOffset;
                var targetY = pickup.Gate.GapCenter + pickup.GapOffset;
                pickup.X = targetX;
                pickup.Y = targetY;
                var bob = Mathf.Sin(ambientTime * 3.2f + pickup.Phase) * .12f;
                pickup.Transform.localPosition = new Vector3(pickup.X, pickup.Y + bob, 0f);
                var pulse = 1f + Mathf.Sin(ambientTime * 4.2f + pickup.Phase) * .10f;
                pickup.Glow.transform.localScale = Vector3.one * (1.14f * pulse);
                var spin = ambientTime * 2.1f + pickup.Phase;
                var depthShift = new Vector3(Mathf.Cos(spin) * .035f, Mathf.Sin(spin * 1.3f) * .028f, 0f);
                pickup.Artwork.transform.localPosition = depthShift;
                pickup.Artwork.transform.localScale = pickup.ArtworkBaseScale * (1f + Mathf.Sin(ambientTime * 3.5f + pickup.Phase) * .025f);
                pickup.Artwork.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(spin) * 3.8f);
                pickup.Depth.transform.localPosition = -depthShift * 1.35f;
                pickup.Depth.transform.localScale = pickup.ArtworkBaseScale * (1.10f + Mathf.Sin(spin) * .035f);
                var depthColour = pickup.Glow.color;
                depthColour.a = .15f + Mathf.Sin(spin) * .025f;
                pickup.Depth.color = depthColour;
                pickup.Spark.transform.localPosition = new Vector3(Mathf.Cos(ambientTime * 4.3f + pickup.Phase) * .46f, Mathf.Sin(ambientTime * 4.3f + pickup.Phase) * .46f, 0f);

                if (Vector2.Distance(new Vector2(BirdX, birdY), new Vector2(pickup.X, pickup.Y + bob)) <= ActiveTuning().CollisionRadius + PickupRadius)
                {
                    CollectPowerUp(pickup);
                }
            }
        }

        // Currency appears as a rare, visible pellet in the safe gap. It is kept
        // separate from tactical power-ups so collection never changes flight handling.
        private void UpdateCrystalPickups(float deltaTime)
        {
            foreach (var pickup in crystalPickupPool)
            {
                if (!pickup.Active) continue;

                if (pickup.Gate == null || !pickup.Gate.Root.activeSelf || pickup.Gate.Passed)
                {
                    DeferCrystalPickup(pickup, 0f);
                    continue;
                }

                var targetX = pickup.Gate.X + pickup.LocalXOffset;
                var targetY = pickup.Gate.GapCenter + pickup.GapOffset;
                var distance = Vector2.Distance(new Vector2(BirdX, birdY), new Vector2(pickup.X, pickup.Y));
                var attractionRadius = Mathf.Max(
                    magnetHaloTimer > 0f ? GetWorldWidth() * .25f : 0f,
                    GetWorldWidth() * TotalCrystalAttractionRadiusFraction());
                if (attractionRadius > 0f && distance <= attractionRadius)
                {
                    var pullSpeed = magnetHaloTimer > 0f ? 9.2f : 5.4f;
                    pickup.X = Mathf.MoveTowards(pickup.X, BirdX, pullSpeed * deltaTime);
                    pickup.Y = Mathf.MoveTowards(pickup.Y, birdY, pullSpeed * .78f * deltaTime);
                }
                else
                {
                    pickup.X = targetX;
                    pickup.Y = targetY;
                }

                var motion = reduceMotionEnabled ? 0f : 1f;
                var bob = Mathf.Sin(ambientTime * 3.8f + pickup.Phase) * .09f * motion;
                pickup.Transform.localPosition = new Vector3(pickup.X, pickup.Y + bob, 0f);
                var pulse = 1f + Mathf.Sin(ambientTime * 4.8f + pickup.Phase) * .08f * motion;
                var spin = ambientTime * 2.8f + pickup.Phase;
                pickup.Glow.transform.localScale = new Vector3(.80f, 1.06f, 1f) * pulse;
                pickup.Artwork.transform.localScale = pickup.ArtworkBaseScale * (1f + Mathf.Sin(spin) * .025f * motion);
                pickup.Artwork.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(spin) * 3.5f * motion);
                pickup.Depth.transform.localScale = new Vector3(.42f, .78f, 1f) * pulse;
                // A tiny facet highlight, rather than an orbiting dot, keeps the
                // gem silhouette clear when three crystals form a close arc.
                pickup.Spark.transform.localPosition = new Vector3(-.09f, .23f, 0f);
                var glint = .052f + (.5f + .5f * Mathf.Sin(spin * 1.8f)) * .022f * motion;
                pickup.Spark.transform.localScale = Vector3.one * glint;

                if (Vector2.Distance(new Vector2(BirdX, birdY), new Vector2(pickup.X, pickup.Y + bob)) <= ActiveTuning().CollisionRadius + CrystalPickupRadius)
                {
                    CollectCrystalPickup(pickup);
                }
            }
        }

        private void CreateCrystalPickupBursts()
        {
            var fineRing = CreateRadialSprite("Crystal collection ring", 96, .42f, .5f);
            // Six reusable bursts cover two full crystal arcs. Collecting currency
            // never allocates particles, materials or GameObjects during a run.
            for (var index = 0; index < crystalPickupBursts.Length; index += 1)
            {
                var root = new GameObject($"Crystal collection burst {index + 1}");
                root.transform.SetParent(transform, false);
                var burst = new CrystalPickupBurst
                {
                    Root = root,
                    Ring = CreateRenderer("Collection ring", fineRing, Color.clear, 16, root.transform),
                    Sparks = new SpriteRenderer[4],
                };
                for (var sparkIndex = 0; sparkIndex < burst.Sparks.Length; sparkIndex += 1)
                    burst.Sparks[sparkIndex] = CreateRenderer("Facet sparkle", whiteSprite, Color.clear, 17, root.transform);
                root.SetActive(false);
                crystalPickupBursts[index] = burst;
            }
        }

        private void ShowCrystalPickupBurst(Vector3 position)
        {
            var burst = crystalPickupBursts[nextCrystalPickupBurst];
            if (burst == null) return;
            nextCrystalPickupBurst = (nextCrystalPickupBurst + 1) % crystalPickupBursts.Length;
            burst.Root.transform.localPosition = position;
            burst.Duration = reduceMotionEnabled ? .22f : .40f;
            burst.Remaining = burst.Duration;
            burst.Root.SetActive(true);
            RenderCrystalPickupBurst(burst);
        }

        private void UpdateCrystalPickupBursts(float deltaTime)
        {
            if (state == FlightState.Paused) return;
            if (state != FlightState.Playing)
            {
                ClearCrystalPickupBursts();
                return;
            }
            foreach (var burst in crystalPickupBursts)
            {
                if (burst == null || burst.Remaining <= 0f) continue;
                burst.Remaining = Mathf.Max(0f, burst.Remaining - deltaTime);
                if (burst.Remaining <= 0f) burst.Root.SetActive(false);
                else RenderCrystalPickupBurst(burst);
            }
        }

        private void RenderCrystalPickupBurst(CrystalPickupBurst burst)
        {
            var progress = 1f - burst.Remaining / burst.Duration;
            var fade = (1f - progress) * (1f - progress);
            var travel = 1f - (1f - progress) * (1f - progress);
            // Reduced Motion retains a brief, stationary acknowledgement. There
            // is no expanding ring or flying debris in that accessibility mode.
            var ringSize = reduceMotionEnabled ? .48f : Mathf.Lerp(.36f, 1.02f, travel);
            burst.Ring.transform.localScale = Vector3.one * ringSize;
            burst.Ring.color = new Color(.30f, .94f, 1f, .66f * fade);
            for (var index = 0; index < burst.Sparks.Length; index += 1)
            {
                var spark = burst.Sparks[index];
                spark.enabled = !reduceMotionEnabled;
                if (reduceMotionEnabled) continue;
                var angle = (45f + index * 90f) * Mathf.Deg2Rad;
                var radius = Mathf.Lerp(.12f, .54f, travel);
                // The shared white sprite's bounds follow Unity's built-in texture
                // dimensions. Express chips in world units to keep them tiny.
                SetSpriteBlock(spark, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                    new Vector2(Mathf.Lerp(.16f, .025f, progress), .035f));
                spark.transform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
                spark.color = index % 2 == 0
                    ? new Color(.78f, 1f, 1f, .90f * fade)
                    : new Color(.75f, .46f, 1f, .78f * fade);
            }
        }

        private void ClearCrystalPickupBursts()
        {
            foreach (var burst in crystalPickupBursts)
            {
                if (burst == null || burst.Remaining <= 0f) continue;
                burst.Remaining = 0f;
                burst.Root.SetActive(false);
            }
            nextCrystalPickupBurst = 0;
        }

        private static void DeferCrystalPickup(PowerUpPickup pickup, float delay)
        {
            pickup.Active = false;
            pickup.Gate = null;
            pickup.RespawnTimer = delay;
            pickup.Root.SetActive(false);
        }

        private void ConfigureGateCrystals(PipePair gate)
        {
            if (gate == null || gate.IsStatic && gate.RouteScore < 0)
                return;

            // Crystal gates should feel like intentional mini-routes,
            // not random loose currency.
            //
            // Old system:
            // 60% chance x average 2 crystals = 1.2 crystals per gate.
            //
            // New system:
            // 40% chance x 3 crystals = 1.2 crystals per gate.
            //
            // The economy therefore stays essentially unchanged.
            if (GateHasPowerUp(gate) || RouteRange(0f, 1f) > .40f)
                return;

            const int count = 3;

            var safeOffset =
                Mathf.Max(
                    .30f,
                    gate.GapHeight * .5f - 1.05f
                );

            // Keep the crystal arc closer to the safe centre
            // of the playable gap.
            var baseOffset =
                RouteRange(
                    -safeOffset * .35f,
                    safeOffset * .35f
                );

            for (var index = 0; index < count; index += 1)
            {
                var pickup = FindInactiveCrystalPickup();

                if (pickup == null)
                    return;

                ConfigureCrystalPickup(
                    pickup,
                    gate,
                    index,
                    count,
                    baseOffset,
                    safeOffset
                );
            }
        }
        private PowerUpPickup FindInactiveCrystalPickup()
        {
            foreach (var pickup in crystalPickupPool)
            {
                if (pickup != null && !pickup.Active) return pickup;
            }
            return null;
        }

        private bool GateHasPowerUp(PipePair gate)
        {
            foreach (var pickup in powerUpPool)
            {
                if (pickup != null && pickup.Active && pickup.Gate == gate) return true;
            }
            return false;
        }

        private void ConfigureCrystalPickup(PowerUpPickup pickup, PipePair gate)
        {
            ConfigureCrystalPickup(pickup, gate, 0, 1, 0f, gate == null ? 0f : Mathf.Max(.30f, gate.GapHeight * .5f - 1.05f));
        }

        private void ConfigureCrystalPickup(PowerUpPickup pickup, PipePair gate, int arcIndex, int arcCount, float baseOffset, float safeOffset)
        {
            if (gate == null)
            {
                DeferCrystalPickup(pickup, 0f);
                return;
            }

            pickup.Root.SetActive(true);
            pickup.Active = true;
            pickup.RespawnTimer = 0f;
            pickup.Gate = gate;
            var centredIndex = arcIndex - (arcCount - 1) * .5f;
            pickup.LocalXOffset = centredIndex * .48f;
            pickup.ArcYOffset = .20f - Mathf.Abs(centredIndex) * .19f;
            pickup.GapOffset = Mathf.Clamp(baseOffset + pickup.ArcYOffset, -safeOffset, safeOffset);
            pickup.X = gate.X + pickup.LocalXOffset;
            pickup.Y = gate.GapCenter + pickup.GapOffset;
            pickup.Phase = RouteRange(0f, Mathf.PI * 2f);
            var crystal = LoadSprite(CrystalArtworkPath);
            var cyan = Hex("#45eaff");
            var violet = Hex("#975dff");
            pickup.Glow.color = new Color(cyan.r, cyan.g, cyan.b, .30f);
            pickup.Glow.transform.localScale = new Vector3(.80f, 1.06f, 1f);
            pickup.Artwork.sprite = crystal ?? softCircleSprite;
            pickup.Artwork.color = crystal == null ? cyan : Color.white;
            // The source canvas includes soft transparent glow. At this scale its
            // faceted body is about .4 units wide, fitting the existing .48 arc.
            pickup.ArtworkBaseScale = ArtworkScale(pickup.Artwork.sprite, .88f);
            pickup.Artwork.transform.localScale = pickup.ArtworkBaseScale;
            pickup.Artwork.transform.localRotation = Quaternion.identity;
            pickup.Artwork.transform.localPosition = Vector3.zero;
            pickup.Depth.sprite = softCircleSprite;
            pickup.Depth.transform.localPosition = Vector3.zero;
            pickup.Depth.transform.localScale = new Vector3(.42f, .78f, 1f);
            pickup.Depth.color = new Color(violet.r, violet.g, violet.b, .30f);
            pickup.Spark.color = new Color(.87f, 1f, 1f, .96f);
            pickup.Spark.transform.localPosition = new Vector3(-.09f, .23f, 0f);
            pickup.Spark.transform.localScale = Vector3.one * .065f;
            pickup.Transform.localPosition = new Vector3(pickup.X, pickup.Y, 0f);
        }

        private void CollectCrystalPickup(PowerUpPickup pickup)
        {
            ShowCrystalPickupBurst(pickup.Transform.localPosition);
            DeferCrystalPickup(pickup, 0f);
            // Currency is banked on contact—even a failed run keeps the find.
            // Prism Conduit adds value through a fractional carry so +5/+10/+15%
            // remains mathematically fair even though the wallet stores whole crystals.
            var crystalValue = CrystalPickupValue(1);
            BankCollectedCrystals(crystalValue);
            ShowCrystalBurst(crystalValue);
            TriggerFlightFeedback(Hex("#45eaff"), .26f);
            PulseHaptic(.08f);
            Play(crystalSound);
        }

        private void DeferPowerUp(PowerUpPickup pickup, float delay)
        {
            pickup.Active = false;
            pickup.Gate = null;
            pickup.RespawnTimer = delay;
            pickup.Root.SetActive(false);
        }

        private void ConfigurePowerUp(PowerUpPickup pickup, PipePair gate)
        {
            if (gate == null)
            {
                DeferPowerUp(pickup, 0f);
                return;
            }
            pickup.Root.SetActive(true);
            pickup.Active = true;
            pickup.RespawnTimer = 0f;
            pickup.Gate = gate;
            pickup.LocalXOffset = 0f;
            pickup.X = gate.X;
            var safeGapOffset = Mathf.Max(.28f, gate.GapHeight * .5f - .92f);
            pickup.GapOffset = RouteRange(-safeGapOffset, safeGapOffset);
            pickup.Y = gate.GapCenter + pickup.GapOffset;
            pickup.Phase = RouteRange(0f, Mathf.PI * 2f);
            pickup.Kind = (PowerUpKind)RouteRange(0, 3);
            var colour = Hex("#8f64ff");
            var secondary = Hex("#45eaff");
            switch (pickup.Kind)
            {
                case PowerUpKind.Aegis:
                    colour = Hex("#61f5b3");
                    secondary = Hex("#edf7ff");
                    break;
                case PowerUpKind.CrystalMagnet:
                    colour = Hex("#45eaff");
                    secondary = Hex("#61f5b3");
                    break;
                case PowerUpKind.TimePulse:
                    colour = Hex("#b17cff");
                    secondary = Hex("#45eaff");
                    break;
            }

            var artwork = LoadSprite(PowerUpArtworkPath(pickup.Kind));
            pickup.Glow.color = new Color(colour.r, colour.g, colour.b, .20f);
            pickup.Artwork.sprite = artwork ?? softCircleSprite;
            pickup.Artwork.color = artwork == null ? colour : Color.white;
            pickup.ArtworkBaseScale = ArtworkScale(pickup.Artwork.sprite, 1.24f);
            pickup.Artwork.transform.localScale = pickup.ArtworkBaseScale;
            pickup.Artwork.transform.localRotation = Quaternion.identity;
            pickup.Artwork.transform.localPosition = Vector3.zero;
            pickup.Depth.sprite = artwork ?? softCircleSprite;
            pickup.Depth.transform.localPosition = Vector3.zero;
            pickup.Depth.transform.localScale = pickup.ArtworkBaseScale * 1.10f;
            pickup.Depth.color = new Color(colour.r, colour.g, colour.b, .15f);
            pickup.Spark.color = new Color(secondary.r, secondary.g, secondary.b, .92f);
            pickup.Transform.localPosition = new Vector3(pickup.X, pickup.Y, 0f);
        }

        private static string PowerUpArtworkPath(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.Aegis: return "SkyPulse/art/powerups/generated/pulse-shield-v3";
                case PowerUpKind.CrystalMagnet: return "SkyPulse/art/powerups/generated/magnet-halo-v3";
                default: return "SkyPulse/art/powerups/generated/slow-field-v3";
            }
        }

        private void CollectPowerUp(PowerUpPickup pickup)
        {
            pickup.Active = false;
            pickup.Gate = null;
            pickup.Root.SetActive(false);
            pickup.RespawnTimer = 0f;
            TriggerFlightFeedback(pickup.Glow.color, .34f);
            PulseHaptic(.12f);
            switch (pickup.Kind)
            {
                case PowerUpKind.Aegis:
                    shieldCharges = 1;
                    shieldFlashTimer = .6f;
                    Play(unlockSound);
                    break;
                case PowerUpKind.CrystalMagnet:
                    magnetHaloTimer = 6f;
                    Play(unlockSound);
                    break;
                default:
                    slowFieldTimer = 4f;
                    Play(crystalSound);
                    break;
            }
            UpdatePowerUpHud();
        }

        private void UpdatePowerUpEffects(float deltaTime)
        {
            if (slowFieldTimer > 0f) slowFieldTimer = Mathf.Max(0f, slowFieldTimer - deltaTime);
            if (shieldFlashTimer > 0f) shieldFlashTimer = Mathf.Max(0f, shieldFlashTimer - deltaTime);
            if (magnetHaloTimer > 0f) magnetHaloTimer = Mathf.Max(0f, magnetHaloTimer - deltaTime);
            if (shieldImmunityTimer > 0f) shieldImmunityTimer = Mathf.Max(0f, shieldImmunityTimer - deltaTime);
            UpdatePowerUpHud();
        }

        private bool HasActivePowerEffect()
        {
            return shieldCharges > 0 || shieldImmunityTimer > 0f || slowFieldTimer > 0f || magnetHaloTimer > 0f;
        }

        private void UpdatePowerUpHud()
        {
            if (hudPowerUpText == null) return;
            var code = -1;
            var timer = 0f;
            var label = string.Empty;
            var colour = Hex("#f4fbff");
            if (slowFieldTimer > 0f) { code = 0; timer = slowFieldTimer; label = "◌  TIME PULSE"; colour = Hex("#b17cff"); }
            else if (magnetHaloTimer > 0f) { code = 1; timer = magnetHaloTimer; label = "◌  CRYSTAL MAGNET"; colour = Hex("#45eaff"); }
            else if (shieldImmunityTimer > 0f) { code = 2; timer = shieldImmunityTimer; label = "◈  AEGIS RECOVERY"; colour = Hex("#61f5b3"); }
            else if (shieldCharges > 0) { code = 3; label = "◈  AEGIS READY"; colour = Hex("#61f5b3"); }
            else
            {
                hudPowerUpText.gameObject.SetActive(false);
                displayedSlowTenths = -1;
                displayedPowerUpCode = -1;
                return;
            }

            var remainingTenths = timer > 0f ? Mathf.CeilToInt(timer * 10f) : 0;
            if (code != displayedPowerUpCode || remainingTenths != displayedSlowTenths || !hudPowerUpText.gameObject.activeSelf)
            {
                hudPowerUpText.text = timer > 0f ? $"{label}  {remainingTenths / 10f:0.0}s" : label;
                displayedPowerUpCode = code;
                displayedSlowTenths = remainingTenths;
            }
            hudPowerUpText.color = colour;
            hudPowerUpText.gameObject.SetActive(true);
        }

        private bool UseShield()
        {
            if (shieldCharges > 0)
            {
                shieldCharges = 0;
                shieldFlashTimer = .60f;
                shieldImmunityTimer = AegisImmunitySeconds;
                shieldHitStopTimer = AegisHitStopSeconds;
                if (birdVelocity < 0f) birdVelocity = 0f;
                TriggerFlightFeedback(Hex("#61f5b3"), .34f);
                PulseHaptic(.16f);
                Play(unlockSound);
                UpdatePowerUpHud();
                return true;
            }
            return false;
        }

        private void ShowScoreBurst(int scoreReward, bool perfect)
        {
            if (scoreBurstText == null) return;

            scoreBurstDuration = .36f;
            scoreBurstTimer = scoreBurstDuration;
            scoreBurstIsCrystal = false;

            scoreBurstText.text = perfect
                ? scoreReward > 1
                    ? $"PERFECT  ·  +{scoreReward} SCORE"
                    : "PERFECT  ·  +1 SCORE"
                : scoreReward > 1
                    ? $"+{scoreReward} SCORE"
                    : "+1 SCORE";

            scoreBurstText.color =
                equippedSkin != null
                    ? equippedSkin.Accent
                    : Color.white;

            scoreBurstText.rectTransform.anchoredPosition =
                new Vector2(0f, HudFeedbackY);

            scoreBurstText.rectTransform.localScale =
                Vector3.one;

            scoreBurstText.gameObject.SetActive(true);
        }
        private void ShowCrystalBurst(int crystalReward, bool cache = false)
        {
            if (scoreBurstText == null) return;

            scoreBurstDuration = .48f;
            scoreBurstTimer = scoreBurstDuration;
            scoreBurstIsCrystal = true;

            scoreBurstText.text = cache
                ? $"CRYSTAL CACHE  ·  +{crystalReward} ✦"
                : $"CRYSTAL  ·  +{crystalReward} ✦";

            scoreBurstText.color = Hex("#ffc34d");

            scoreBurstText.rectTransform.anchoredPosition =
                new Vector2(0f, HudFeedbackY);

            scoreBurstText.rectTransform.localScale =
                Vector3.one * 1.06f;

            scoreBurstText.gameObject.SetActive(true);
        }
        private void TriggerFlightFeedback(Color colour, float duration)
        {
            if (flightFeedbackRenderer == null || bird == null) return;
            flightFeedbackColour = colour;
            flightFeedbackDuration = Mathf.Max(.01f, duration);
            flightFeedbackTimer = flightFeedbackDuration;
            var origin = bird.position + new Vector3(0f, 0f, .2f);
            flightFeedbackRenderer.transform.position = origin;
            flightFeedbackRingRenderer.transform.position = origin;
            // Initialise immediately: never expose the preceding event's size/alpha.
            UpdateFlightFeedback(0f);
        }

        private void UpdateFlightFeedback(float deltaTime)
        {
            if (flightFeedbackRenderer == null) return;
            flightFeedbackTimer = Mathf.Max(0f, flightFeedbackTimer - deltaTime);
            var visible = flightFeedbackTimer > 0f;
            flightFeedbackRenderer.enabled = visible;
            flightFeedbackRingRenderer.enabled = visible;
            if (!visible) return;

            var progress = 1f - flightFeedbackTimer / flightFeedbackDuration;
            var fade = (1f - progress) * (1f - progress);
            var expansion = 1f - (1f - progress) * (1f - progress);
            var colour = flightFeedbackColour;
            colour.a = .20f * fade;
            flightFeedbackRenderer.color = colour;
            flightFeedbackRenderer.transform.localScale = Vector3.one * Mathf.Lerp(.85f, 1.65f, expansion);
            colour.a = .55f * fade;
            flightFeedbackRingRenderer.color = colour;
            flightFeedbackRingRenderer.transform.localScale = Vector3.one *
                Mathf.Lerp(1.15f, reduceMotionEnabled ? 1.32f : 2.05f, expansion);
        }

#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
        /// <summary>
        /// Press F4 in an Editor or Development build to expose the exact collision
        /// space. It is compiled out of release builds, so it can never distract a
        /// player or cost production frame time.
        /// </summary>
        private void UpdateCollisionDebug()
        {
            var visible = collisionDebugEnabled && state == FlightState.Playing;
            if (collisionBirdDebug != null)
            {
                collisionBirdDebug.enabled = visible;
                collisionBirdDebug.transform.localScale = new Vector3(BirdHitboxWidth, BirdHitboxHeight, 1f);
            }

            var physicalWidth = PipeCapWidth;
            foreach (var pair in pipePool)
            {
                if (pair == null || pair.DebugTopBody == null || pair.DebugTopCap == null || pair.DebugBottomBody == null || pair.DebugBottomCap == null) continue;
                var showPair = visible && pair.Root.activeSelf;
                pair.DebugTopBody.enabled = showPair;
                pair.DebugTopCap.enabled = showPair;
                pair.DebugBottomBody.enabled = showPair;
                pair.DebugBottomCap.enabled = showPair;
                if (!showPair) continue;

                var halfGap = pair.GapHeight * .5f;
                var topEdge = pair.GapCenter + halfGap;
                var topHeight = Mathf.Max(0f, CameraHeight * .5f + TopPipeOverscan - topEdge);
                pair.DebugTopBody.transform.localPosition = new Vector3(0f, topEdge + topHeight * .5f, 0f);
                pair.DebugTopBody.transform.localScale = new Vector3(PipeWidth, topHeight, 1f);
                pair.DebugTopCap.transform.localPosition = new Vector3(0f, topEdge + PipeCapHeight * .5f, 0f);
                pair.DebugTopCap.transform.localScale = new Vector3(physicalWidth, PipeCapHeight, 1f);

                var bottomEdge = pair.GapCenter - halfGap;
                var bottomBase = GroundY - BottomPipeFloorOverlap;
                var bottomHeight = Mathf.Max(0f, bottomEdge - bottomBase);
                pair.DebugBottomBody.transform.localPosition = new Vector3(0f, bottomBase + bottomHeight * .5f, 0f);
                pair.DebugBottomBody.transform.localScale = new Vector3(PipeWidth, bottomHeight, 1f);
                pair.DebugBottomCap.transform.localPosition = new Vector3(0f, bottomEdge - PipeCapHeight * .5f, 0f);
                pair.DebugBottomCap.transform.localScale = new Vector3(physicalWidth, PipeCapHeight, 1f);
            }
        }
#endif

        private void StartFlight()
        {
            BeginFlight();
        }

        private void RestartFlight()
        {
            BeginFlight();
        }

        private void BeginFlight()
        {
            ClearCrystalPickupBursts();
            ClosePurchaseModal();
            state = FlightState.Playing;
            score = 0;
            perfectPasses = 0;
            newBest = false;
            BeginProgressionRun();
            routeWorldIndex = 0;
            routeWorld = Worlds[routeWorldIndex];
            nextGateRouteScore = 0;
            nextGateSequence = 0;
            nextPowerUpRouteScore = RouteRange(8, 13);
            firstGateAfterTransition = false;
            worldTransitionTimer = 0f;
            worldRecoveryTimer = 0f;
            RecordFarthestWorld(routeWorldIndex);
            simulationAccumulator = 0f;
            bufferedFlapUntil = -1f;
            lastFlapInputTime = -100f;
            flightFeedbackTimer = 0f;
            UpdateFlightFeedback(0f);
            lastCrashReason = "GATE IMPACT";
            birdY = 0f;
            birdVelocity = 0f;
            birdTilt = 0f;
            birdTiltVelocity = 0f;
            wingTimer = WingCycleSeconds;
            ResetGameplayWingState();
            impactFrameTimer = 0f;
            impactTumbleTimer = 0f;
            slowFieldTimer = 0f;
            shieldFlashTimer = 0f;
            magnetHaloTimer = 0f;
            shieldImmunityTimer = 0f;
            shieldHitStopTimer = 0f;
            shieldCharges = 0;
            UpdateBirdPowerUpVisuals();

            ApplyRouteWorldVisuals();
            spawnX = GetWorldWidth() * .5f + 3.2f;
            foreach (var pickup in powerUpPool)
            {
                pickup.Active = false;
                pickup.Gate = null;
                pickup.Root.SetActive(false);
            }
            foreach (var pickup in crystalPickupPool)
            {
                pickup.Active = false;
                pickup.Gate = null;
                pickup.RespawnTimer = 0f;
                pickup.Root.SetActive(false);
            }
            for (var index = 0; index < pipePool.Length; index += 1) ConfigurePipe(pipePool[index], spawnX + index * RoutePipeSpacing());
            RefreshScreens();
            hudScoreText.text = "0";
            UpdateModeCopy();
            UpdatePowerUpHud();
            SetBirdArtwork();
            bird.gameObject.SetActive(true);
            if (birdBodyCollider != null)
            {
                birdBodyCollider.enabled = true;
                SyncBirdHitbox();
            }
            // PLAY and RETRY only begin a run. The bird remains settled until an
            // actual gameplay tap requests its first flap.
        }

        private void ResetToMenu()
        {
            ClearCrystalPickupBursts();
            ResetWorldTransition();
            ClosePurchaseModal();
            state = FlightState.Menu;
            menuPresentationTime = 0f;
            menuWingTimer = 0f;
            simulationAccumulator = 0f;
            bufferedFlapUntil = -1f;
            flightFeedbackTimer = 0f;
            UpdateFlightFeedback(0f);
            if (flightFeedbackRenderer != null) flightFeedbackRenderer.enabled = false;
            birdY = .15f;
            birdVelocity = 0f;
            birdTilt = 0f;
            birdTiltVelocity = 0f;
            bird.position = new Vector3(BirdX, birdY, 0f);
            bird.gameObject.SetActive(false);

            if (birdBodyCollider != null) birdBodyCollider.enabled = false;
            foreach (var pair in pipePool) pair.Root.SetActive(false);
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
            if (collisionBirdDebug != null) collisionBirdDebug.enabled = false;
            foreach (var pair in pipePool)
            {
                if (pair.DebugTopBody != null) pair.DebugTopBody.enabled = false;
                if (pair.DebugTopCap != null) pair.DebugTopCap.enabled = false;
                if (pair.DebugBottomBody != null) pair.DebugBottomBody.enabled = false;
                if (pair.DebugBottomCap != null) pair.DebugBottomCap.enabled = false;
            }
#endif
            foreach (var pickup in powerUpPool)
            {
                pickup.Active = false;
                pickup.Root.SetActive(false);
            }
            foreach (var pickup in crystalPickupPool)
            {
                pickup.Active = false;
                pickup.Gate = null;
                pickup.Root.SetActive(false);
            }
            slowFieldTimer = 0f;
            shieldFlashTimer = 0f;
            magnetHaloTimer = 0f;
            shieldCharges = 0;
            UpdatePowerUpHud();
            SaveProgress();
            RefreshScreens();
        }

        private void PauseFlight()
        {
            if (state != FlightState.Playing) return;
            state = FlightState.Paused;
            RefreshScreens();
        }

        private void ResumeFlight()
        {
            if (state != FlightState.Paused) return;
            state = FlightState.Playing;
            RefreshScreens();
        }

        private void ConfigurePipe(PipePair pair, float x)
        {
            RetirePowerUpsForGate(pair);
            RetireCrystalPickupsForGate(pair);
            pair.Root.SetActive(true);
            pair.X = x;
            pair.Passed = false;
            pair.RouteScore = nextGateRouteScore++;
            pair.Sequence = nextGateSequence++;
            pair.RouteWorldIndex = routeWorldIndex;
            pair.IsStatic = firstGateAfterTransition;
            if (firstGateAfterTransition) firstGateAfterTransition = false;
            // Tune the gate being built, not the score while it is still off-screen.
            pair.GapHeight = CameraHeight * RouteGapFraction(pair.RouteScore);
            var halfGap = pair.GapHeight * .5f;
            // Keep enough visible body above and below every opening. This makes a
            // low gate read as a deliberate lower route, not a clipped top pipe.
            var centreMinimum = Mathf.Max(GapCenterMinimum, GroundY + PipeMinimumVisibleHeight + halfGap);
            var centreMaximum = Mathf.Min(GapCenterMaximum, CameraHeight * .5f - PipeMinimumVisibleHeight - halfGap);
            var nextCentre = RouteRange(centreMinimum, centreMaximum);
            var precedingPair = FindPrecedingPipe(pair, x);
            if (precedingPair != null)
            {
                var maximumStep = RouteMaximumCenterStep(pair.RouteScore);
                nextCentre = Mathf.Clamp(nextCentre, precedingPair.GapCenter - maximumStep, precedingPair.GapCenter + maximumStep);
                nextCentre = Mathf.Clamp(nextCentre, centreMinimum, centreMaximum);
            }
            var usesRemixPatterns = pair.RouteScore >= 45;
            if ((pair.RouteWorldIndex == 2 || usesRemixPatterns) && precedingPair != null)
            {
                // Bazaar gates—and every post-45 remix—alternate their preferred
                // opening without ever breaking the bounded reachable-path rule.
                var desired = (pair.Sequence & 1) == 0 ? 1.10f : -1.10f;
                nextCentre = Mathf.Clamp(desired, centreMinimum, centreMaximum);
                nextCentre = Mathf.Clamp(nextCentre, precedingPair.GapCenter - CameraHeight * .20f, precedingPair.GapCenter + CameraHeight * .20f);
            }
            pair.BaseGapCenter = nextCentre;
            pair.GapCenter = nextCentre;
            // From score 45 on, remixes deliberately combine Foundry drift with
            // Bazaar alternation while preserving the familiar single opening.
            pair.DriftAmplitude = (pair.RouteWorldIndex == 1 || usesRemixPatterns) && !pair.IsStatic
                ? CameraHeight * RouteDriftFraction(pair.RouteScore) : 0f;
            pair.DriftPhase = RouteRange(0f, Mathf.PI * 2f);
            pair.Root.transform.localPosition = new Vector3(x, 0f, 0f);
            LayoutPipePair(pair);
            ConfigureRoutePowerUp(pair);
            ConfigureGateCrystals(pair);
        }

        private void LayoutPipePair(PipePair pair)
        {
            var halfGap = pair.GapHeight * .5f;
            var topLowerEdge = pair.GapCenter + halfGap;
            // Keep the non-playable pipe ends tucked outside the visible camera and
            // floor, while the cap remains exactly flush with the readable gap edge.
            var topHeight = CameraHeight * .5f + TopPipeOverscan - topLowerEdge;
            LayoutPipeSurface(pair.Top, topLowerEdge + topHeight * .5f, topHeight, topLowerEdge, true);

            var bottomUpperEdge = pair.GapCenter - halfGap;
            var bottomBase = GroundY - BottomPipeFloorOverlap;
            var bottomHeight = bottomUpperEdge - bottomBase;
            LayoutPipeSurface(pair.Bottom, bottomBase + bottomHeight * .5f, bottomHeight, bottomUpperEdge, false);
        }

        private void ConfigureRoutePowerUp(PipePair gate)
        {
            if (gate == null || gate.RouteScore < 3 || HasActivePowerEffect()) return;
            if (gate.RouteScore < nextPowerUpRouteScore) return;
            // A power-up must never be the final gate immediately before a world
            // transition. Move its schedule forward without making the next one
            // predictable to the player.
            if (WorldIndexForScore(gate.RouteScore + 1) != WorldIndexForScore(gate.RouteScore))
            {
                nextPowerUpRouteScore = gate.RouteScore + RouteRange(2, 4);
                return;
            }

            var pickup = powerUpPool.Length > 0 ? powerUpPool[0] : null;
            if (pickup == null || pickup.Active) return;
            ConfigurePowerUp(pickup, gate);
            nextPowerUpRouteScore = gate.RouteScore + RouteRange(8, 13);
        }

        private static int WorldIndexForScore(int routeScore)
        {
            if (routeScore < 15) return 0;
            if (routeScore < 30) return 1;
            if (routeScore < 45) return 2;
            return Mathf.FloorToInt((routeScore - 45) / 15f) % 3;
        }

        private void BeginWorldTransition(int nextWorldIndex)
        {
            nextWorldIndex = Mathf.Clamp(nextWorldIndex, 0, Worlds.Length - 1);
            if (nextWorldIndex == routeWorldIndex || worldTransitionTimer > 0f) return;

            ClearCrystalPickupBursts();

            transitionVeilStart = backgroundVeil.color;
            transitionFloorStart = floorSurface.color;
            transitionRailStart = floorGlow.color;
            transitionLipStart = floorLip.color;
            for (var index = 0; index < transitionRenderers.Length; index += 1)
                transitionColours[index] = transitionRenderers[index].color;
            departingObjectsVisible = true;

            routeWorldIndex = nextWorldIndex;
            routeWorld = Worlds[routeWorldIndex];
            RecordFarthestWorld(routeWorldIndex);
            worldTransitionTimer = WorldTransitionSeconds;
            worldRecoveryTimer = 0f;
            firstGateAfterTransition = true;
            nextGateRouteScore = score;
            // Keep the old backdrop opaque beneath the incoming one. Fading both
            // layers creates a dark dip halfway through an otherwise smooth blend.
            incomingBackground.sprite = WorldBackdrop(routeWorld);
            incomingBackground.color = Color.clear;
            incomingBackground.transform.position = backgroundRenderer.transform.position;
            FitBackgroundToCamera(incomingBackground, 1.1f);
            incomingBackground.enabled = true;
            equippedWorld = routeWorld;
            equippedPipe = FindById(PipeStyles, routeWorld.PresetPipeId) ?? PipeStyles[0];
            if (scoreBurstText != null)
            {
                scoreBurstDuration = .80f;
                scoreBurstTimer = scoreBurstDuration;
                scoreBurstIsCrystal = false;
                scoreBurstText.text = routeWorld.Name;
                scoreBurstText.color = routeWorld.Accent;
                scoreBurstText.rectTransform.anchoredPosition = new Vector2(0f, HudFeedbackY);
                scoreBurstText.gameObject.SetActive(true);
            }
        }

        private void UpdateWorldTransition(float deltaTime)
        {
            if (worldTransitionTimer > 0f)
            {
                worldTransitionTimer = Mathf.Max(0f, worldTransitionTimer - deltaTime);
                var progress = 1f - worldTransitionTimer / WorldTransitionSeconds;
                var blend = Mathf.SmoothStep(0f, 1f, progress);
                incomingBackground.color = new Color(1f, 1f, 1f, blend);
                var veilTarget = routeWorld.Accent;
                veilTarget.a = WorldAtmosphereTintAlpha;
                backgroundVeil.color = Color.Lerp(transitionVeilStart, veilTarget, blend);
                var floorTarget = routeWorld.Floor;
                floorTarget.a = .54f;
                floorSurface.color = Color.Lerp(transitionFloorStart, floorTarget, blend);
                var railTarget = routeWorld.Accent;
                railTarget.a = .38f;
                floorGlow.color = Color.Lerp(transitionRailStart, railTarget, blend);
                var lipTarget = Darken(routeWorld.Floor, .65f);
                lipTarget.a = .78f;
                floorLip.color = Color.Lerp(transitionLipStart, lipTarget, blend);
                if (hudModeText != null)
                {
                    // Change the label at zero opacity, not in the middle of a
                    // fully visible word. World artwork remains an uninterrupted blend.
                    if (progress >= .5f) hudModeText.text = routeWorld.Name;
                    var label = Color.Lerp(transitionRailStart, routeWorld.Accent, blend);
                    label.a = Mathf.Abs(2f * blend - 1f);
                    hudModeText.color = label;
                }
                FadeDepartingWorld(deltaTime, progress);
                if (worldTransitionTimer > 0f) return;

                // Commit exactly the image already shown at full opacity.
                backgroundRenderer.sprite = incomingBackground.sprite;
                FitBackgroundToCamera(backgroundRenderer, 1.1f);
                incomingBackground.enabled = false;
                spawnX = GetWorldWidth() * .5f + 2.4f;
                for (var index = 0; index < pipePool.Length; index += 1)
                    ConfigurePipe(pipePool[index], spawnX + index * RoutePipeSpacing());
                worldRecoveryTimer = WorldRecoverySeconds;
                return;
            }
            if (worldRecoveryTimer > 0f)
                worldRecoveryTimer = Mathf.Max(0f, worldRecoveryTimer - deltaTime);
        }

        private void FadeDepartingWorld(float deltaTime, float progress)
        {
            if (!departingObjectsVisible) return;
            var fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / .25f));
            for (var index = 0; index < transitionRenderers.Length; index += 1)
            {
                var colour = transitionColours[index];
                colour.a *= fade;
                transitionRenderers[index].color = colour;
            }
            var movement = Vector3.left * (ActiveScrollSpeed() * deltaTime);
            foreach (var pair in pipePool)
            {
                if (!pair.Root.activeSelf) continue;
                pair.X += movement.x;
                pair.Root.transform.localPosition += movement;
            }
            foreach (var pickup in powerUpPool)
                if (pickup.Root.activeSelf) pickup.Root.transform.position += movement;
            foreach (var pickup in crystalPickupPool)
                if (pickup.Root.activeSelf) pickup.Root.transform.position += movement;
            if (progress < .25f) return;
            foreach (var pair in pipePool) pair.Root.SetActive(false);
            foreach (var pickup in powerUpPool) DeferPowerUp(pickup, 0f);
            foreach (var pickup in crystalPickupPool) DeferCrystalPickup(pickup, 0f);
            RestoreDepartingColours();
        }

        private void RestoreDepartingColours()
        {
            if (!departingObjectsVisible) return;
            for (var index = 0; index < transitionRenderers.Length; index += 1)
                transitionRenderers[index].color = transitionColours[index];
            departingObjectsVisible = false;
        }

        private void ResetWorldTransition()
        {
            RestoreDepartingColours();
            worldTransitionTimer = 0f;
            worldRecoveryTimer = 0f;
            if (incomingBackground != null) incomingBackground.enabled = false;
        }

        private void ApplyRouteWorldVisuals()
        {
            ResetWorldTransition();
            if (routeWorld == null) routeWorld = Worlds[Mathf.Clamp(routeWorldIndex, 0, Worlds.Length - 1)];
            equippedWorld = routeWorld;
            equippedPipe = FindById(PipeStyles, routeWorld.PresetPipeId) ?? PipeStyles[0];
            if (backgroundRenderer != null)
            {
                backgroundRenderer.sprite = WorldBackdrop(routeWorld);
                FitBackgroundToCamera(backgroundRenderer, 1.1f);
            }
            if (backgroundVeil != null) backgroundVeil.color = new Color(routeWorld.Accent.r, routeWorld.Accent.g, routeWorld.Accent.b, WorldAtmosphereTintAlpha);
            if (floorSurface != null)
            {
                var floorColour = routeWorld.Floor;
                floorColour.a = .54f;
                floorSurface.color = floorColour;
            }
            if (floorGlow != null)
            {
                var railColour = routeWorld.Accent;
                railColour.a = .38f;
                floorGlow.color = railColour;
            }
            if (floorLip != null)
            {
                var lipColour = Darken(routeWorld.Floor, .65f);
                lipColour.a = .78f;
                floorLip.color = lipColour;
            }
            if (hudModeText != null)
            {
                hudModeText.text = routeWorld.Name;
                hudModeText.color = routeWorld.Accent;
            }
        }

        private PipePair FindPrecedingPipe(PipePair ignoredPair, float x)
        {
            PipePair preceding = null;
            foreach (var candidate in pipePool)
            {
                if (candidate == null || candidate == ignoredPair || !candidate.Root.activeSelf || candidate.X >= x) continue;
                if (preceding == null || candidate.X > preceding.X) preceding = candidate;
            }
            return preceding;
        }

        private void UpdateRouteGateMotion(PipePair pair, float deltaTime)
        {
            if (pair == null || pair.DriftAmplitude <= 0f || pair.IsStatic) return;
            // Foundry and remix gate drift is a slow, obvious ±4%-of-height sweep over 1.4 s.
            // The complete gap moves together, so no collision extends invisibly
            // into the opening.
            var target = pair.BaseGapCenter + Mathf.Sin(ambientTime * (Mathf.PI * 2f / 1.4f) + pair.DriftPhase) * pair.DriftAmplitude;
            var halfGap = pair.GapHeight * .5f;
            var lowerBound = GroundY + PipeMinimumVisibleHeight + halfGap;
            var upperBound = CameraHeight * .5f - PipeMinimumVisibleHeight - halfGap;
            pair.GapCenter = Mathf.Clamp(target, lowerBound, upperBound);
            LayoutPipePair(pair);
        }

        private void AnimatePipePair(PipePair pair)
        {
            var halfGap = pair.GapHeight * .5f;
            AnimatePipeSurface(pair.Top, pair.GapCenter + halfGap, true, pair.X);
            AnimatePipeSurface(pair.Bottom, pair.GapCenter - halfGap, false, pair.X);
        }

        private void AnimatePipeSurface(PipeSurface surface, float capY, bool topPipe, float pipeX)
        {
            if (hasNeonPipeArtwork)
            {
                AnimateNeonPipeSurface(surface, capY, topPipe, pipeX);
                return;
            }
            // Movement stays within the non-colliding light layers. The gateway feels
            // alive, while the bright visual opening always remains the safe opening.
            var gateMotion = reduceMotionEnabled ? 0f : 1f;
            var pulse = reduceMotionEnabled ? .56f : .5f + .5f * Mathf.Sin(ambientTime * 6.4f + pipeX * 1.7f);
            var direction = topPipe ? 1f : -1f;
            var capBodyInset = .23f;
            // Theme identity belongs to the narrow energy parts. The cylindrical
            // body stays graphite, with only a restrained metal reflection.
            var metal = Color.Lerp(Hex("#0a1222"), equippedPipe.Panel, .15f);
            var reflectionColour = Color.Lerp(metal, Color.white, .22f + pulse * .10f);
            reflectionColour.a = Mathf.Lerp(.24f, .44f, pulse);

            if (hasAuthoredPipeBody)
            {
                var authoredTint = Color.Lerp(
                    Color.white,
                    equippedPipe.Panel,
                    .035f + pulse * .030f);

                authoredTint = Color.Lerp(
                    authoredTint,
                    equippedPipe.Energy,
                    .015f + pulse * .015f);

                authoredTint.a = 1f;
                surface.Artwork.color = authoredTint;

                if (surface.Shade != null)
                {
                    var authoredShade = surface.Shade.color;
                    authoredShade.a = Mathf.Lerp(.10f, .18f, pulse);
                    surface.Shade.color = authoredShade;
                }
            }
            else
            {
                surface.Artwork.color = reflectionColour;
            }

            var coreColour = equippedPipe.Energy;
            coreColour.a = Mathf.Lerp(.08f, .18f, pulse);
            if (!hasAuthoredPipeBody) surface.Core.color = coreColour;
            var corePulseColour = Color.Lerp(equippedPipe.Energy, Color.white, .42f);
            corePulseColour.a = Mathf.Lerp(.18f, .56f, pulse);
            surface.CorePulse.color = corePulseColour;
            var corePhase = reduceMotionEnabled ? .48f : Mathf.Repeat(ambientTime * .68f + pipeX * .17f, 1f);
            var bodyHeight = hasAuthoredPipeBody &&
            surface.Artwork != null &&
            surface.Artwork.sprite != null ? Mathf.Max(.12f,
            surface.Artwork.sprite.bounds.size.y * Mathf.Abs(surface.Artwork.transform.localScale.y)) : Mathf.Max(.12f,
            surface.Panel.transform.localScale.y);
            if (surface.RailLeft != null && surface.RailLeft.enabled)
            {
                var railLeft = equippedPipe.Energy;
                railLeft.a = Mathf.Lerp(.10f, .22f, pulse);
                surface.RailLeft.color = railLeft;
            }

            if (surface.RailRight != null && surface.RailRight.enabled)
            {
                var railRight = Color.Lerp(equippedPipe.Energy, Color.white, .35f);
                railRight.a = Mathf.Lerp(.06f, .14f, pulse);
                surface.RailRight.color = railRight;
            }
            var corePulseHeight = .44f + pulse * .10f;
            var corePulseStart = .18f + corePulseHeight * .5f;
            var corePulseTravel = Mathf.Max(0f, bodyHeight - .18f - corePulseHeight);
            surface.CorePulse.transform.localPosition = new Vector3(
     Mathf.Sin((ambientTime * 3.2f + pipeX) * gateMotion) * .024f,
     capY + direction * (corePulseStart + corePhase * corePulseTravel), 0f);
            surface.CorePulse.transform.localScale = new Vector3(.34f + pulse * .08f, corePulseHeight, 1f);

            var seamColour = surface.Energy.color;
            seamColour.a = Mathf.Lerp(.62f, .98f, pulse);
            surface.Energy.color = seamColour;
            surface.Energy.transform.localPosition = new Vector3(0f, capY + direction * (.055f + Mathf.Sin(ambientTime * 8.8f + pipeX) * .012f * gateMotion), 0f);
            surface.Energy.transform.localScale = new Vector3(PipeWidth * Mathf.Lerp(.60f, .72f, pulse), .018f + pulse * .014f, 1f);

            var highlightColour = surface.Highlight.color;
            highlightColour.a = Mathf.Lerp(.08f, .28f, pulse);
            surface.Highlight.color = highlightColour;
            surface.Highlight.transform.localPosition = new Vector3(0f, capY + direction * (.035f + Mathf.Cos(ambientTime * 7.2f + pipeX) * .010f * gateMotion), 0f);
            surface.Highlight.transform.localScale = new Vector3(PipeWidth * Mathf.Lerp(.56f, .68f, pulse), .008f + pulse * .009f, 1f);
            var scanPhase = reduceMotionEnabled ? .48f : Mathf.Repeat(ambientTime * 1.05f + pipeX * .11f,1f);
            var scanColour = Color.Lerp(equippedPipe.Energy,Color.white,.48f);scanColour.a = Mathf.Lerp(.16f,.48f,pulse);
            surface.Scan.color = scanColour;
            surface.Scan.transform.localPosition = new Vector3(0f, capY + direction * (capBodyInset + scanPhase * Mathf.Max(.08f, bodyHeight - .30f)), 0f);
            surface.Scan.transform.localScale = new Vector3(PipeWidth * .72f, .010f, 1f);
            var beaconColour = surface.Beacon.color;
            beaconColour.a = Mathf.Lerp(.18f, .52f, pulse);
            surface.Beacon.color = beaconColour;
            surface.Beacon.transform.localPosition = new Vector3(0f, capY + direction * .115f, 0f);
            surface.Beacon.transform.localScale = Vector3.one * Mathf.Lerp(.26f, .38f, pulse);
            if (hasAuthoredPipeCap && hasAuthoredPipeGlow && surface.CapGlow.enabled)
            {
                var collarGlow = equippedPipe.Energy;
                collarGlow.a = Mathf.Lerp(.14f, .28f, pulse);
                surface.CapGlow.color = collarGlow;
                var capCentre = capY + direction * (PipeCapHeight * .5f);
                SetSpriteBlock(surface.CapGlow, Vector2.up * capCentre, new Vector2(
                    PipeCapWidth * (.80f + pulse * .04f),
                    PipeCapHeight * (.27f + pulse * .02f)));
                surface.CapGlow.transform.localRotation = Quaternion.identity;
            }

            var capEnergyColour = surface.CapEnergy.color;
            capEnergyColour.a = Mathf.Lerp(.72f, 1f, pulse);
            surface.CapEnergy.color = capEnergyColour;
            surface.CapEnergy.transform.localPosition = new Vector3(0f, capY + direction * (.030f + Mathf.Sin(ambientTime * 8.8f + pipeX) * .008f * gateMotion), 0f);
            surface.CapEnergy.transform.localScale = new Vector3(PipeWidth * Mathf.Lerp(.62f, .72f, pulse), .018f + pulse * .010f, 1f);
        }

        private void RetirePowerUpsForGate(PipePair gate)
        {
            foreach (var pickup in powerUpPool)
            {
                if (pickup != null && pickup.Active && pickup.Gate == gate)
                {
                    DeferPowerUp(pickup, RouteRange(.8f, 1.4f));
                }
            }
        }

        private void RetireCrystalPickupsForGate(PipePair gate)
        {
            foreach (var pickup in crystalPickupPool)
            {
                if (pickup != null && pickup.Active && pickup.Gate == gate)
                {
                    DeferCrystalPickup(pickup, RandomCrystalRange(CrystalPickupRespawnMinimum, CrystalPickupRespawnMaximum));
                }
            }
        }

      private void LayoutNeonPipeSurface(
    PipeSurface surface,
    float centreY,
    float height,
    float capY,
    bool topPipe)
{
    var direction =
        topPipe ? 1f : -1f;

    // ---------------------------------------------------------
    // CLEAN PREMIUM BASE
    // ---------------------------------------------------------

    surface.Outer.enabled = false;
    surface.Panel.enabled = false;
    surface.Shade.enabled = false;
    surface.Energy.enabled = false;
    surface.Beacon.enabled = false;

    surface.CapGlow.enabled = false;
    surface.CapOuter.enabled = false;
    surface.CapAccent.enabled = false;
    surface.CapEnergy.enabled = false;

    // ---------------------------------------------------------
    // PREMIUM AUTHORED COLLAR
    // ---------------------------------------------------------

    var visualCapWidth =
        PipeWidth * 1.035f;

    var visualCapHeight =
        PipeCapHeight * .82f;

    var visualCapCentre =
        capY +
        direction *
        visualCapHeight * .5f;

    surface.CapPanel.enabled = true;
    surface.CapPanel.sprite = pipeCapSprite;
    surface.CapPanel.sortingOrder = 11;
    surface.CapPanel.flipY = topPipe;
    surface.CapPanel.transform.localRotation =
        Quaternion.identity;

    surface.CapPanel.color =
        Color.white;

    SetSpriteBlock(
        surface.CapPanel,
        Vector2.up * visualCapCentre,
        new Vector2(
            visualCapWidth,
            visualCapHeight));

    // ---------------------------------------------------------
    // AUTHORED SHAFT
    //
    // CRITICAL FIX:
    // The shaft must stop at the BODY side of the collar,
    // NOT at capY on the playable-gap side.
    // ---------------------------------------------------------

    var farBodyY =
        centreY +
        direction *
        height * .5f;

    // Slight overlap underneath the BACK of the collar prevents
    // a hairline gap while keeping the cyan artwork completely
    // away from the visible opening.
    var shaftJoinY =
        capY +
        direction *
        (visualCapHeight - .035f);

    var visualShaftHeight =
        Mathf.Max(
            .12f,
            Mathf.Abs(
                farBodyY -
                shaftJoinY));

    var visualShaftCentre =
        (farBodyY + shaftJoinY) * .5f;

    surface.Artwork.enabled = true;
    surface.Artwork.sprite = pipeBodySprite;
    surface.Artwork.sortingOrder = 5;
    surface.Artwork.flipY = topPipe;
    surface.Artwork.transform.localRotation =
        Quaternion.identity;

    surface.Artwork.color =
        Color.white;

    SetSpriteBlock(
        surface.Artwork,
        Vector2.up * visualShaftCentre,
        new Vector2(
            PipeWidth,
            visualShaftHeight));

    // ---------------------------------------------------------
    // POWER RAILS
    //
    // Stop BEFORE the collar begins.
    // ---------------------------------------------------------

    var railNearY =
        shaftJoinY +
        direction * .14f;

    var railFarY =
        farBodyY -
        direction * .12f;

    var railHeight =
        Mathf.Max(
            .12f,
            Mathf.Abs(
                railFarY -
                railNearY));

    var railCentre =
        (railFarY + railNearY) * .5f;

    surface.RailLeft.enabled = true;
    surface.RailRight.enabled = true;

    surface.RailLeft.sortingOrder = 6;
    surface.RailRight.sortingOrder = 6;

    SetSpriteBlock(
        surface.RailLeft,
        new Vector2(
            -PipeWidth * .465f,
            railCentre),
        new Vector2(
            .018f,
            railHeight));

    SetSpriteBlock(
        surface.RailRight,
        new Vector2(
            PipeWidth * .405f,
            railCentre),
        new Vector2(
            .024f,
            railHeight));

    // ---------------------------------------------------------
    // TRAVELLING ENERGY LAYERS
    // ---------------------------------------------------------

    surface.Core.enabled = false;

    surface.CorePulse.enabled = true;
    surface.CorePulse.sprite = softCircleSprite;
    surface.CorePulse.sortingOrder = 8;

    surface.Highlight.enabled = true;
    surface.Highlight.sprite = softCircleSprite;
    surface.Highlight.sortingOrder = 8;

    surface.Scan.enabled = true;
    surface.Scan.sprite = softCircleSprite;
    surface.Scan.sortingOrder = 8;

    AnimateNeonPipeSurface(
        surface,
        capY,
        topPipe,
        0f);
}
     private void AnimateNeonPipeSurface(
    PipeSurface surface,
    float capY,
    bool topPipe,
    float pipeX)
{
    var direction =
        topPipe ? 1f : -1f;

    var gateOffset =
        pipeX * .097f;

    var breathe =
        reduceMotionEnabled
            ? .58f
            : .5f +
              .5f *
              Mathf.Sin(
                  ambientTime * 2.1f +
                  gateOffset * 4.1f);

    // ---------------------------------------------------------
    // EXACT VISIBLE SHAFT BOUNDS
    // ---------------------------------------------------------

    var shaftHeight =
        surface.Artwork.sprite != null
            ? surface.Artwork.sprite.bounds.size.y *
              Mathf.Abs(
                  surface.Artwork.transform.localScale.y)
            : .12f;

    shaftHeight =
        Mathf.Max(
            .12f,
            shaftHeight);

    var shaftCentre =
        surface.Artwork.transform.localPosition.y;

    // Edge nearest the collar.
    var shaftNearY =
        shaftCentre -
        direction *
        shaftHeight * .5f;

    // Opposite/far end of pipe.
    var shaftFarY =
        shaftCentre +
        direction *
        shaftHeight * .5f;

    // Every moving light stays safely INSIDE the shaft.
    var packetNearY =
        shaftNearY +
        direction * .20f;

    var packetFarY =
        shaftFarY -
        direction * .20f;

    // ---------------------------------------------------------
    // PREMIUM ENERGY PALETTE
    // ---------------------------------------------------------

    var cyan =
        Color.Lerp(
            new Color(.08f, .94f, 1f),
            equippedPipe.Accent,
            .12f);

    var magenta =
        Color.Lerp(
            new Color(1f, .015f, .72f),
            equippedPipe.Energy,
            .10f);

    // ---------------------------------------------------------
    // SUBTLE METAL RESPONSE
    // ---------------------------------------------------------

    surface.Artwork.color =
        Color.Lerp(
            Color.white,
            equippedPipe.Accent,
            .008f +
            breathe * .012f);

    surface.CapPanel.color =
        Color.Lerp(
            Color.white,
            equippedPipe.Energy,
            .004f +
            breathe * .009f);

    // ---------------------------------------------------------
    // BASE RAIL ENERGY
    // ---------------------------------------------------------

    var leftRail =
        cyan;

    leftRail.a =
        .28f +
        breathe * .15f;

    surface.RailLeft.color =
        leftRail;

    var rightRail =
        Color.Lerp(
            cyan,
            Color.white,
            .20f);

    rightRail.a =
        .26f +
        breathe * .16f;

    surface.RailRight.color =
        rightRail;

    // ---------------------------------------------------------
    // CENTRE MAGENTA PACKET
    // ---------------------------------------------------------

    var centrePhase =
        reduceMotionEnabled
            ? .50f
            : Mathf.Repeat(
                ambientTime * .66f +
                gateOffset,
                1f);

    var centreMove =
        Mathf.SmoothStep(
            0f,
            1f,
            centrePhase);

    var centreEnvelope =
        Mathf.Pow(
            Mathf.Clamp01(
                Mathf.Sin(
                    centrePhase *
                    Mathf.PI)),
            .70f);

    var centreY =
        Mathf.Lerp(
            packetFarY,
            packetNearY,
            centreMove);

    var centreColour =
        Color.Lerp(
            magenta,
            Color.white,
            .48f);

    centreColour.a =
        .04f +
        centreEnvelope * .90f;

    surface.CorePulse.color =
        centreColour;

    SetSpriteBlock(
        surface.CorePulse,
        new Vector2(
            0f,
            centreY),
        new Vector2(
            PipeWidth *
            (.095f +
             centreEnvelope * .030f),
            .50f +
            centreEnvelope * .16f));

    // ---------------------------------------------------------
    // LEFT CYAN PACKET
    // ---------------------------------------------------------

    var cyanPhase =
        reduceMotionEnabled
            ? .38f
            : Mathf.Repeat(
                ambientTime * .53f +
                gateOffset +
                .34f,
                1f);

    var cyanMove =
        Mathf.SmoothStep(
            0f,
            1f,
            cyanPhase);

    var cyanEnvelope =
        Mathf.Pow(
            Mathf.Clamp01(
                Mathf.Sin(
                    cyanPhase *
                    Mathf.PI)),
            .66f);

    var cyanY =
        Mathf.Lerp(
            packetFarY,
            packetNearY,
            cyanMove);

    var cyanPacket =
        Color.Lerp(
            cyan,
            Color.white,
            .66f);

    cyanPacket.a =
        .03f +
        cyanEnvelope * .94f;

    surface.Highlight.color =
        cyanPacket;

    SetSpriteBlock(
        surface.Highlight,
        new Vector2(
            -PipeWidth * .465f,
            cyanY),
        new Vector2(
            .050f +
            cyanEnvelope * .020f,
            .56f +
            cyanEnvelope * .18f));

    // ---------------------------------------------------------
    // RIGHT MAGENTA / WHITE PACKET
    // ---------------------------------------------------------

    var secondaryPhase =
        reduceMotionEnabled
            ? .65f
            : Mathf.Repeat(
                ambientTime * .59f +
                gateOffset +
                .69f,
                1f);

    var secondaryMove =
        Mathf.SmoothStep(
            0f,
            1f,
            secondaryPhase);

    var secondaryEnvelope =
        Mathf.Pow(
            Mathf.Clamp01(
                Mathf.Sin(
                    secondaryPhase *
                    Mathf.PI)),
            .70f);

    var secondaryY =
        Mathf.Lerp(
            packetFarY,
            packetNearY,
            secondaryMove);

    var secondaryPacket =
        Color.Lerp(
            magenta,
            Color.white,
            .60f);

    secondaryPacket.a =
        .03f +
        secondaryEnvelope * .86f;

    surface.Scan.color =
        secondaryPacket;

    SetSpriteBlock(
        surface.Scan,
        new Vector2(
            PipeWidth * .405f,
            secondaryY),
        new Vector2(
            .052f +
            secondaryEnvelope * .018f,
            .48f +
            secondaryEnvelope * .15f));

    // ---------------------------------------------------------
    // ENERGY SURGE THROUGH THE STATIC RAILS
    // ---------------------------------------------------------

    var surge =
        Mathf.Max(
            cyanEnvelope,
            secondaryEnvelope);

    var surgedLeft =
        surface.RailLeft.color;

    surgedLeft.a =
        Mathf.Clamp01(
            surgedLeft.a +
            surge * .10f);

    surface.RailLeft.color =
        surgedLeft;

    var surgedRight =
        surface.RailRight.color;

    surgedRight.a =
        Mathf.Clamp01(
            surgedRight.a +
            surge * .11f);

    surface.RailRight.color =
        surgedRight;

    // ---------------------------------------------------------
    // CAP SAFETY
    // ---------------------------------------------------------

    surface.CapGlow.enabled = false;
    surface.CapOuter.enabled = false;
    surface.CapAccent.enabled = false;
    surface.CapEnergy.enabled = false;
}
     private void LayoutPlumbingGate(
     PipeSurface surface,
     float centreY,
     float height,
     float capY,
     bool topPipe,
     PipeStyle style)
        {
            if (hasNeonPipeArtwork)
            {
                LayoutNeonPipeSurface(surface, centreY, height, capY, topPipe);
                return;
            }
            var direction = topPipe ? 1f : -1f;
            var bodyHeight = Mathf.Max(.12f, height - .08f);
            var metal = Color.Lerp(Hex("#0a1222"), style.Panel, .08f);
            var metalDark = Darken(metal, .72f);
            var collarMetal = Color.Lerp(metal, style.Accent, .24f);

            // Prefer the authored mechanical pipe supplied for SkyPulse.
            // Keep subtle visual depth and energy around authored artwork.
            surface.Artwork.enabled = hasAuthoredPipeBody;
            surface.Outer.enabled = !hasAuthoredPipeBody;
            surface.Panel.enabled = !hasAuthoredPipeBody;

            surface.Shade.enabled = true;
            surface.RailLeft.enabled = true;
            surface.RailRight.enabled = true;

            surface.Core.enabled = !hasAuthoredPipeBody;
            surface.CorePulse.enabled = true;

            if (hasAuthoredPipeBody)
            {
                surface.Artwork.sprite = pipeBodySprite;

                var authoredTint = Color.Lerp(
                    Color.white,
                    style.Accent,
                    .06f);

                authoredTint.a = 1f;

                surface.Artwork.color = authoredTint;
                surface.Artwork.sortingOrder = 5;

                SetSpriteBlock(
                    surface.Artwork,
                    new Vector2(0f, centreY),
                    new Vector2(PipeWidth, height));

                surface.Artwork.transform.localRotation =
                    topPipe
                        ? Quaternion.Euler(0f, 0f, 180f)
                        : Quaternion.identity;

                // Soft depth copy behind the authored pipe.
                surface.Shade.sprite = pipeBodySprite;
                surface.Shade.sortingOrder = 4;
                surface.Shade.color = new Color(0f, 0f, 0f, .14f);

                SetSpriteBlock(
                    surface.Shade,
                    new Vector2(-.045f, centreY - .030f),
                    new Vector2(
                        PipeWidth * 1.01f,
                        height));

                surface.Shade.transform.localRotation =
                    surface.Artwork.transform.localRotation;

                // Restrained illuminated edges.
                var leftRail = style.Energy;
                leftRail.a = .13f;

                surface.RailLeft.color = leftRail;
                surface.RailLeft.sortingOrder = 6;

                SetBlock(
                    surface.RailLeft,
                    new Vector2(
                        -PipeWidth * .38f,
                        centreY),
                    new Vector2(
                        .018f,
                        Mathf.Max(.12f, height - .18f)));

                var rightRail = Color.Lerp(
                    style.Energy,
                    Color.white,
                    .28f);

                rightRail.a = .08f;

                surface.RailRight.color = rightRail;
                surface.RailRight.sortingOrder = 6;

                SetBlock(
                    surface.RailRight,
                    new Vector2(
                        PipeWidth * .38f,
                        centreY),
                    new Vector2(
                        .014f,
                        Mathf.Max(.12f, height - .22f)));
            }
            else
            {
                SetBlock(
                    surface.Outer,
                    Vector2.up * centreY,
                    new Vector2(PipeWidth, height));

                SetBlock(
                    surface.Panel,
                    Vector2.up * centreY,
                    new Vector2(PipeWidth - .06f, bodyHeight));

                SetBlock(
                    surface.Shade,
                    new Vector2(-PipeWidth * .32f, centreY),
                    new Vector2(
                        PipeWidth * .18f,
                        Mathf.Max(.12f, height - .14f)));

                SetBlock(
                    surface.Artwork,
                    new Vector2(PipeWidth * .07f, centreY),
                    new Vector2(
                        PipeWidth * .19f,
                        Mathf.Max(.12f, height - .18f)));

                SetBlock(
                    surface.RailLeft,
                    new Vector2(-PipeWidth * .36f, centreY),
                    new Vector2(
                        .028f,
                        Mathf.Max(.12f, height - .18f)));

                SetBlock(
                    surface.RailRight,
                    new Vector2(PipeWidth * .36f, centreY),
                    new Vector2(
                        .020f,
                        Mathf.Max(.12f, height - .22f)));

                surface.Panel.color =
                    new Color(metal.r, metal.g, metal.b, 1f);

                surface.Outer.color =
                    new Color(metalDark.r, metalDark.g, metalDark.b, 1f);

                surface.Shade.color =
                    new Color(0f, 0f, 0f, .18f);

                var reflection =
                    Color.Lerp(metal, Color.white, .23f);

                reflection.a = .30f;
                surface.Artwork.color = reflection;

                var leftRail = style.Energy;
                leftRail.a = .28f;
                surface.RailLeft.color = leftRail;

                var rightRail =
                    Color.Lerp(style.Energy, Color.white, .28f);

                rightRail.a = .15f;
                surface.RailRight.color = rightRail;
            }

            // The separate authored cap sits precisely at the playable gap edge.
            var capCentre =
                capY + direction * (PipeCapHeight * .5f);

            surface.CapOuter.enabled = !hasAuthoredPipeCap;
            surface.CapAccent.enabled = false;
            surface.CapPanel.enabled = true;
            surface.CapEnergy.enabled = !hasAuthoredPipeCap;

            if (hasAuthoredPipeCap)
            {
                surface.CapPanel.sprite = pipeCapSprite;
                surface.CapPanel.color = Color.white;
                surface.CapPanel.sortingOrder = 11;

                SetSpriteBlock(
                    surface.CapPanel,
                    Vector2.up * capCentre,
                    new Vector2(
                        PipeCollisionWidth,
                        PipeCapHeight));

                surface.CapPanel.transform.localRotation =
                    topPipe
                        ? Quaternion.Euler(0f, 0f, 180f)
                        : Quaternion.identity;

                surface.CapGlow.enabled = hasAuthoredPipeGlow;

                if (hasAuthoredPipeGlow)
                {
                    var authoredGlow = style.Energy;
                    authoredGlow.a = .20f;

                    surface.CapGlow.color = authoredGlow;
                    surface.CapGlow.sprite = pipeGlowSprite;

                    SetSpriteBlock(
                        surface.CapGlow,
                        Vector2.up * capCentre,
                        new Vector2(
                            PipeCapWidth * .82f,
                            PipeCapHeight * .28f));

                    surface.CapGlow.transform.localRotation =
                        Quaternion.identity;
                }
            }
            else
            {
                surface.CapGlow.enabled = false;

                SetBlock(
                    surface.CapOuter,
                    Vector2.up * capCentre,
                    new Vector2(
                        PipeWidth + .18f,
                        .42f));

                SetBlock(
                    surface.CapAccent,
                    Vector2.up * capCentre,
                    new Vector2(
                        PipeWidth + .10f,
                        .34f));

                SetBlock(
                    surface.CapPanel,
                    Vector2.up * capCentre,
                    new Vector2(
                        PipeWidth - .08f,
                        .28f));

                SetBlock(
                    surface.CapEnergy,
                    Vector2.up *
                        (capY + direction * .030f),
                    new Vector2(
                        PipeWidth * .64f,
                        .018f));

                surface.CapOuter.color =
                    Darken(metalDark, .18f);

                surface.CapAccent.color =
                    collarMetal;

                surface.CapPanel.color =
                    Darken(metal, .40f);

                var capEnergy = style.Energy;
                capEnergy.a = .82f;
                surface.CapEnergy.color = capEnergy;
            }
        }
        private static float RouteGapFraction(int routeScore)
        {
            // Every opening belongs to the same steadily tightening course.
            if (routeScore < 15) return Mathf.Lerp(.32f, .282f, routeScore / 14f);
            if (routeScore < 25) return .28f;
            if (routeScore < 30) return Mathf.Lerp(.277f, .265f, (routeScore - 25) / 4f);
            if (routeScore < 40) return .26f;
            if (routeScore < 45) return Mathf.Lerp(.257f, .245f, (routeScore - 40) / 4f);
            return .24f;
        }

        private static float RouteMaximumCenterStep(int routeScore)
        {
            if (routeScore < 15) return Mathf.Lerp(2.2f, CameraHeight * .20f, routeScore / 14f);
            return CameraHeight * .20f;
        }

        private static float RouteDriftFraction(int routeScore)
        {
            // Foundry introduces movement over four gates after its static arrival.
            if (routeScore < 15) return 0f;
            if (routeScore < 20) return Mathf.Lerp(0f, .04f, (routeScore - 15) / 4f);
            return .04f;
        }

        private float RouteRange(float minimum, float maximum)
        {
            return UnityEngine.Random.Range(minimum, maximum);
        }

        private static float RandomCrystalRange(float minimum, float maximum)
        {
            return UnityEngine.Random.Range(minimum, maximum);
        }

        private int RouteRange(int minimumInclusive, int maximumExclusive)
        {
            return UnityEngine.Random.Range(minimumInclusive, maximumExclusive);
        }

        private void Flap()
        {
            // Every accepted tap immediately affects flight physics.
            birdVelocity =
                Mathf.Max(
                    ActiveFlapVelocity(),
                    birdVelocity * .18f);

            wingTimer = 0f;

            if (UsesFlapFrameSequence())
            {
                if (gameplayWingState == GameplayWingState.Settled)
                {
                    // Start at the authored glide pose. The first visible move
                    // will be its adjacent upstroke frame, never a frame-one snap.
                    gameplayWingFrameIndex = flapFrameBirdSprites.Length - 1;
                    gameplayWingFrameTimer = 0f;
                    gameplayWingState = GameplayWingState.Upstroke;
                }
                else if (gameplayWingState == GameplayWingState.Downstroke)
                {
                    // Another tap during the downstroke reverses the
                    // wings naturally from their CURRENT position.
                    // Keeping this frame and its timer avoids a snap.
                    gameplayWingState = GameplayWingState.Upstroke;
                }

                // If the wings are already moving upward, keep that
                // smooth upstroke going. The bird still receives lift.
            }

            Play(flapSound);
        }
        private void EndFlight()
        {
            if (state != FlightState.Playing) return;
            state = FlightState.Impact;
            ResetGameplayWingState();
            if (birdBodyCollider != null) birdBodyCollider.enabled = false;
            impactFrameTimer = ImpactFreezeSeconds;
            impactTumbleTimer = ImpactTumbleSeconds;
            newBest = score > best && score > 0;
            best = Mathf.Max(best, score);
            ShowHitFrame();
            // Give every collision a clear downward reaction before the tumble.
            birdVelocity = Mathf.Min(birdVelocity, -2.4f);
            birdTiltVelocity = 0f;


            TriggerFlightFeedback(Hex("#f05bc6"), .36f);
            PulseHaptic(.28f);
            Play(crashSound);
            resultScoreText.text = score.ToString();
            resultBestText.text = $"PERSONAL BEST  {best}";
            if (resultReasonText != null) resultReasonText.text = lastCrashReason;
            if (resultModeText != null)
            {
                resultModeText.text = "ENDLESS ROUTE";
                resultModeText.color = routeWorld == null ? Hex("#45eaff") : routeWorld.Accent;
            }
            resultNewBestText.gameObject.SetActive(newBest);
            RefreshProgressionResultLabels();
            SaveProgress();
        }

        private void UpdateImpactTumble(float deltaTime)
        {
            if (bird == null) return;
            birdVelocity = Mathf.Max(ActiveMaxFallVelocity(), birdVelocity + ActiveGravity() * deltaTime);
            birdY = Mathf.Max(GroundY + BirdHitboxVerticalExtent(), birdY + birdVelocity * deltaTime);
            bird.position = new Vector3(BirdX, birdY, 0f);
            var fallStrength = Mathf.Clamp01(
           -birdVelocity / Mathf.Abs(ActiveMaxFallVelocity()));

            var tumbleSpeed = Mathf.Lerp(220f, 360f, fallStrength);

            birdTilt += tumbleSpeed * deltaTime;
            bird.rotation = Quaternion.Euler(0f, 0f, birdTilt);
        }

        private void ShowHitFrame()
        {
            // The hit cell is visible for a short beat before the result card. It
            // makes a collision legible without interrupting the immediate retry flow.
            var hitPose = hitBirdSprite ?? LoadPresentationPose(equippedSkin?.HitPath);
            if (hitPose == null || birdRenderer == null) return;

            birdRenderer.sprite = hitPose;
            birdRenderer.color = Color.white;
            birdRenderer.enabled = true;
            birdArt.localScale = ArtworkScale(hitPose, BirdDisplayWidth);
            // The parent preserves the impact position and banking. Do not add
            // a second translation or rotation as the authored hit pose arrives.
            birdArt.localPosition = Vector3.zero;
            birdArt.localRotation = Quaternion.identity;
            if (birdFlapRenderer != null) birdFlapRenderer.enabled = false;
            if (birdRiseRenderer != null) birdRiseRenderer.enabled = false;
            if (birdParallaxRenderer != null) birdParallaxRenderer.enabled = false;
            if (birdDepthRenderer != null) birdDepthRenderer.enabled = false;
            if (birdEyeGlintRenderer != null) birdEyeGlintRenderer.enabled = false;
            if (birdSafetyRenderer != null) birdSafetyRenderer.enabled = false;
        }

        private void StartRoundFromCustomize()
        {
            if (state != FlightState.Customize) return;
            if (purchaseModal != null && purchaseModal.activeSelf) return;
            if (unlockRevealModal != null && unlockRevealModal.activeSelf) return;
            StartFlight();
        }

        private void OpenCustomize()
        {
            state = FlightState.Customize;
            bird.gameObject.SetActive(false);
            cosmeticCategory = CosmeticCategory.Birds;
            RebuildCustomizeGrid();
            RefreshScreens();
        }

        private void OpenHangar()
        {
            OpenCustomize();
        }

        private void OpenUpgrades()
        {
            state = FlightState.Customize;
            bird.gameObject.SetActive(false);
            cosmeticCategory = CosmeticCategory.Upgrades;
            RebuildCustomizeGrid();
            RefreshScreens();
        }

        private void SetCosmeticCategory(CosmeticCategory category)
        {
            cosmeticCategory = category;
            RebuildCustomizeGrid();
        }

        private void RebuildCustomizeGrid()
        {
            if (customizeContent == null) return;
            // A fling in the previous tab must not move the newly opened list.
            if (customizeScroll != null)
            {
                customizeScroll.StopMovement();
                customizeScroll.vertical = cosmeticCategory != CosmeticCategory.Birds;
                customizeScroll.horizontal = false;
            }
            for (var index = customizeContent.childCount - 1; index >= 0; index -= 1) Destroy(customizeContent.GetChild(index).gameObject);

            RefreshCollectionNavigation();
            switch (cosmeticCategory)
            {
                case CosmeticCategory.Birds:
                    customizeTitle.text = "BIRD HANGAR";

                    if (customizeScroll != null)
                    {
                        customizeScroll.StopMovement();
                        customizeScroll.vertical = false;
                        customizeScroll.horizontal = false;
                    }
                    BuildBirdHangarPage();
                    break;

                case CosmeticCategory.Worlds:
                    customizeTitle.text = "WORLD COLLECTION";
                    for (var index = 0; index < Worlds.Length; index += 1)
                    {
                        var world = Worlds[index];
                        var presetPipe = FindById(PipeStyles, world.PresetPipeId);
                        var pipeName = presetPipe != null ? presetPipe.Name : "PIPE PRESET";
                        var status = equippedWorld.Id == world.Id ? $"EQUIPPED · {pipeName}" : $"{world.DifficultyLabel} · {pipeName}";
                        CreateCosmeticCard(index, world.Name, status, world.Accent, LoadSprite(world.BackgroundPath), () => EquipWorld(world));
                    }
                    SetContentRows(Worlds.Length, CosmeticCardRowStride);
                    break;
                case CosmeticCategory.Pipes:
                    customizeTitle.text = "PIPE COLLECTION";
                    for (var index = 0; index < PipeStyles.Length; index += 1)
                    {
                        var style = PipeStyles[index];
                        CreateCosmeticCard(index, style.Name, equippedPipe.Id == style.Id ? "EQUIPPED" : "TAP TO EQUIP", style.Accent, null, () => EquipPipe(style), style.Panel, style.Energy, true);
                    }
                    SetContentRows(PipeStyles.Length, CosmeticCardRowStride);
                    break;
                default:
                    customizeTitle.text = "TECH TREE";
                    BuildTechTree();
                    break;
            }
        }

        private void RefreshCollectionNavigation()
        {
            var tech = cosmeticCategory == CosmeticCategory.Upgrades;
            if (customizeScroll != null && customizeScroll.viewport != null)
            {
                var viewportImage = customizeScroll.viewport.GetComponent<Image>();
                if (viewportImage != null)
                {
                    viewportImage.color = tech
                        ? new Color(.015f, .027f, .067f, .87f)
                        : new Color(.003f, .008f, .024f, .12f);
                }
            }
            var accent = tech ? Hex("#ffc34d") : Hex("#45eaff");
            var owned = 0;
            foreach (var skin in Skins) if (IsSkinOwned(skin)) owned++;
            if (customizeSubtitle != null)
                customizeSubtitle.text = tech ? "EARN CRYSTALS · POWER YOUR COLLECTION" : $"{owned} / {Skins.Length} BIRDS UNLOCKED · FIND YOUR SIGNATURE";
            for (var index = 0; index < collectionTabs.Count; index++)
            {
                var selected = tech ? index == 1 : index == 0;
                var tab = collectionTabs[index];
                tab.targetGraphic.color = selected ? Color.Lerp(Hex("#081428"), accent, .25f) : Hex("#071022");
                var label = tab.GetComponentInChildren<Text>();
                if (label != null) label.color = selected ? Hex("#f4fbff") : Hex("#8da5c4");
                collectionTabRails[index].color = new Color(accent.r, accent.g, accent.b, selected ? 1f : .08f);
            }
        }

        private void BuildTechTree()
        {
            var installed = 0;
            var capacity = 0;
            foreach (var upgrade in Upgrades) { installed += GetUpgradeLevel(upgrade.Id); capacity += upgrade.MaxLevel; }
            var branches = new[] { "COLLECTION", "RECOVERY", "MASTERY" };
            var colours = new[] { Hex("#45eaff"), Hex("#ffc34d"), Hex("#ed69ff") };

            // Connections are rendered first so the glass nodes sit above their light.
            // Each lane reflects its real prerequisite chain; the decorative root
            // does not introduce a new purchasable upgrade or a cross-branch gate.
            for (var branchIndex = 0; branchIndex < branches.Length; branchIndex++)
            {
                var x = (branchIndex - 1) * 310f;
                var colour = colours[branchIndex];
                CreateTechPath(new Vector2(0f, -177f), new Vector2(x, -295f), colour, true);
                foreach (var upgrade in Upgrades)
                {
                    if (upgrade.Branch != branches[branchIndex] || upgrade.Tier <= 1) continue;
                    var y = -430f - (upgrade.Tier - 1) * 330f;
                    CreateTechPath(new Vector2(x, y + 195f), new Vector2(x, y + 135f), colour, IsUpgradePrerequisiteMet(upgrade));
                }
            }

            var core = CreateLuminousPanel(customizeContent, "Tech network core", new Vector2(0f, -100f), new Vector2(412f, 154f), Hex("#081c34"), Hex("#45eaff"));
            core.anchorMin = core.anchorMax = new Vector2(.5f, 1f);
            core.GetComponent<Image>().raycastTarget = false;
            CreateUiGlyph(core, "Core wings", new Vector2(0f, 36f), new Vector2(122f, 56f), Hex("#73f3ff"), SkyPulseUiGlyph.Kind.WingMark).Animate = true;
            CreateText(core, "SKYPULSE CORE", new Vector2(0f, -14f), new Vector2(370f, 39f), 30, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            CreateText(core, $"{installed} / {capacity} LEVELS INSTALLED", new Vector2(0f, -52f), new Vector2(370f, 32f), 24, Hex("#9bdded"), TextAnchor.MiddleCenter, FontStyle.Bold);

            for (var branchIndex = 0; branchIndex < branches.Length; branchIndex++)
            {
                var x = (branchIndex - 1) * 310f;
                var colour = colours[branchIndex];
                var progress = 0;
                var branchCapacity = 0;
                foreach (var upgrade in Upgrades)
                    if (upgrade.Branch == branches[branchIndex]) { progress += GetUpgradeLevel(upgrade.Id); branchCapacity += upgrade.MaxLevel; }
                var labelPlate = CreatePanel(customizeContent, branches[branchIndex] + " branch label", new Vector2(x, -257f), new Vector2(272f, 44f), Hex("#061327"));
                labelPlate.anchorMin = labelPlate.anchorMax = new Vector2(.5f, 1f);
                labelPlate.GetComponent<Image>().raycastTarget = false;
                CreateText(labelPlate, $"{branches[branchIndex]}  {progress}/{branchCapacity}", Vector2.zero, new Vector2(272f, 40f), 24, colour, TextAnchor.MiddleCenter, FontStyle.Bold);
                foreach (var upgrade in Upgrades)
                    if (upgrade.Branch == branches[branchIndex])
                        CreateTechMapNode(upgrade, new Vector2(x, -430f - (upgrade.Tier - 1) * 330f), colour);
            }
            CreateTopAnchoredText("TAP TO EXPLORE · LEVEL 2 POWERS THE NEXT NODE", -1260f, 24, Hex("#aac3df"), FontStyle.Bold);
            customizeContent.sizeDelta = new Vector2(0f, 1300f);
            customizeContent.anchoredPosition = Vector2.zero;
        }

        private void CreateTechPath(Vector2 from, Vector2 to, Color colour, bool powered)
        {
            var pathObject = new GameObject(powered ? "Powered tech path" : "Dormant tech path", typeof(RectTransform), typeof(CanvasRenderer), typeof(SkyPulseTechConnection));
            pathObject.transform.SetParent(customizeContent, false);
            var rect = pathObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.sizeDelta = new Vector2(Mathf.Abs(to.x - from.x) + 32f, Mathf.Abs(to.y - from.y) + 32f);
            rect.anchoredPosition = (from + to) * .5f;
            var path = pathObject.GetComponent<SkyPulseTechConnection>();
            path.StartPoint = from - rect.anchoredPosition;
            path.EndPoint = to - rect.anchoredPosition;
            path.Powered = powered;
            path.color = colour;
            path.raycastTarget = false;
        }

        private void CreateTechMapNode(Upgrade upgrade, Vector2 position, Color colour)
        {
            var level = GetUpgradeLevel(upgrade.Id);
            var maxed = level >= upgrade.MaxLevel;
            var unlocked = IsUpgradePrerequisiteMet(upgrade);
            var affordable = unlocked && !maxed && crystals >= upgrade.PriceAtLevel(level);
            var frameColour = new Color(colour.r, colour.g, colour.b, unlocked ? 1f : .45f);
            var node = CreateLuminousPanel(customizeContent, $"{upgrade.Name} tech node", position, new Vector2(274f, 270f),
                Color.Lerp(Hex("#071022"), colour, unlocked ? .11f : .018f), frameColour);
            node.anchorMin = node.anchorMax = new Vector2(.5f, 1f);
            var button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = node.GetComponent<Image>();
            button.onClick.AddListener(() => InspectTechNode(upgrade));
            node.gameObject.AddComponent<SkyPulseButtonFeedback>();
            var emblemColour = new Color(colour.r, colour.g, colour.b, unlocked ? .95f : .36f);
            CreateUiGlyph(node, "Tech socket", new Vector2(0f, 72f), new Vector2(97f, 91f), emblemColour, SkyPulseUiGlyph.Kind.CircuitHex);
            if (!unlocked)
            {
                CreateTechLockEmblem(node, new Vector2(0f, 72f), Hex("#a4aecd"));
            }
            else if (upgrade.Branch == "COLLECTION")
            {
                var icon = CreateImage(node, "Crystal collection emblem", new Vector2(0f, 72f), new Vector2(60f, 79f), Color.white);
                icon.sprite = LoadSprite(CrystalArtworkPath) ?? softCircleSprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
            else
            {
                var kind = upgrade.Branch == "RECOVERY" ? SkyPulseUiGlyph.Kind.RecoveryCore : SkyPulseUiGlyph.Kind.WingMark;
                var size = upgrade.Branch == "RECOVERY" ? new Vector2(62f, 62f) : new Vector2(68f, 52f);
                CreateUiGlyph(node, upgrade.Branch + " tech emblem", new Vector2(0f, 72f), size, colour, kind);
            }
            var title = CreateText(node, upgrade.Name.Replace(" ", "\n"), new Vector2(0f, -5f), new Vector2(252f, 68f), 28,
                unlocked ? Hex("#f4fbff") : Hex("#b3bdd8"), TextAnchor.MiddleCenter, FontStyle.Bold);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            for (var index = 0; index < upgrade.MaxLevel; index++)
            {
                var filled = index < level;
                var pip = CreateLuminousPanel(node, $"Tech level pip {index + 1}", new Vector2((index - 1) * 55f, -58f), new Vector2(43f, 11f),
                    filled ? colour : Hex("#182a43"), new Color(colour.r, colour.g, colour.b, filled ? .95f : .22f));
                pip.GetComponent<Image>().raycastTarget = false;
            }
            CreateText(node, $"LV {level} / {upgrade.MaxLevel}", new Vector2(0f, -84f), new Vector2(242f, 30f), 24,
                maxed ? colour : Hex("#bdcce1"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var status = maxed ? "FULLY POWERED" : !unlocked ? $"NEEDS LV {upgrade.PrerequisiteLevel} ABOVE" : affordable ? $"UPGRADE · {upgrade.PriceAtLevel(level)} ✦" : $"{upgrade.PriceAtLevel(level)} ✦";
            CreateText(node, status, new Vector2(0f, -114f), new Vector2(254f, 30f), 23,
                maxed || affordable ? colour : Hex("#9eafc8"), TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private void CreateTechLockEmblem(Transform parent, Vector2 position, Color colour)
        {
            // A geometric lock stays legible on devices without a padlock font glyph.
            var lockRoot = new GameObject("Locked tech emblem", typeof(RectTransform));
            lockRoot.transform.SetParent(parent, false);
            var rect = lockRoot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(56f, 64f);
            CreateImage(rect, "Lock left shackle", new Vector2(-16f, 14f), new Vector2(6f, 26f), colour).raycastTarget = false;
            CreateImage(rect, "Lock right shackle", new Vector2(16f, 14f), new Vector2(6f, 26f), colour).raycastTarget = false;
            CreatePanel(rect, "Lock shackle top", new Vector2(0f, 26f), new Vector2(38f, 7f), colour).GetComponent<Image>().raycastTarget = false;
            CreatePanel(rect, "Lock body", new Vector2(0f, -8f), new Vector2(49f, 36f), colour).GetComponent<Image>().raycastTarget = false;
            CreateImage(rect, "Lock keyway", new Vector2(0f, -8f), new Vector2(6f, 13f), Hex("#14203d")).raycastTarget = false;
        }

        private void InspectTechNode(Upgrade upgrade)
        {
            var level = GetUpgradeLevel(upgrade.Id);
            var maxed = level >= upgrade.MaxLevel;
            var unlocked = IsUpgradePrerequisiteMet(upgrade);
            if (unlocked && !maxed)
            {
                SelectUpgrade(upgrade);
            }
            else
            {
                pendingPurchase = PendingPurchase.None;
                pendingUpgrade = null;
                pendingSkin = null;
                OpenPurchaseModal(upgrade.Name, "", 0, upgrade.Accent, GetUpgradeArtwork(upgrade) ?? softCircleSprite);
                purchaseTitleText.text = upgrade.Name;
                purchaseConfirmButton.interactable = false;
                purchaseConfirmText.text = maxed ? "FULLY UPGRADED" : "LOCKED";
                var prerequisite = FindById(Upgrades, upgrade.PrerequisiteId);
                purchaseBalanceText.text = maxed ? "ALL THREE LEVELS ACTIVE" : $"Requires {prerequisite?.Name} · level {upgrade.PrerequisiteLevel}";
            }
            var active = level > 0 ? upgrade.EffectAtLevel(level - 1) : "Not installed";
            SetPurchaseUpgradeArtwork(upgrade);
            purchaseDetailText.text = maxed ? $"ACTIVE · {active}" : $"ACTIVE · {active}\nNEXT · {upgrade.EffectAtLevel(level)}";
        }

        private Text CreateTopAnchoredText(string value, float y, int fontSize, Color colour, FontStyle style)
        {
            var text = CreateText(customizeContent, value, new Vector2(0f, y), new Vector2(850f, 38f), fontSize, colour, TextAnchor.MiddleCenter, style);
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(.5f, 1f);
            rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            return text;
        }

        private void SetContentRows(int count, float rowStride = 255f)
        {
            var rows = Mathf.CeilToInt(count / 2f);
            customizeContent.sizeDelta = new Vector2(0f, Mathf.Max(1360f, rows * rowStride + 22f));
            customizeContent.anchoredPosition = Vector2.zero;
        }
        private void SetBirdHangarContentHeight(int count)
        {
            var rows = Mathf.CeilToInt(count / 2f);
            customizeContent.sizeDelta = new Vector2(0f, 290f + rows * BirdHangarRowStride + 32f);
            customizeContent.anchoredPosition = Vector2.zero;
        }
        private void BuildBirdHangarPage()
        {
            if (Skins == null || Skins.Length == 0) return;

            hangarPageIndex = Mathf.Clamp(
                hangarPageIndex,
                0,
                Skins.Length - 1);

            var skin = Skins[hangarPageIndex];
            var owned = IsSkinOwned(skin);
            var equipped =
                equippedSkin != null &&
                equippedSkin.Id == skin.Id;

            var profile = GetBirdHangarProfile(skin);
            var accent = skin.Accent;

            var previousIndex = hangarPageIndex - 1;
            if (previousIndex < 0)
                previousIndex = Skins.Length - 1;

            var nextIndex = hangarPageIndex + 1;
            if (nextIndex >= Skins.Length)
                nextIndex = 0;

            customizeContent.sizeDelta =
                new Vector2(0f, 1110f);

            customizeContent.anchoredPosition =
                Vector2.zero;

            // Transparent container only.
            // This is NOT another giant blue card.
            var pageObject = new GameObject(
                "Bird carousel page",
                typeof(RectTransform));

            pageObject.transform.SetParent(
                customizeContent,
                false);

            hangarPageRoot =
                pageObject.GetComponent<RectTransform>();

            hangarPageRoot.anchorMin =
                new Vector2(.5f, 1f);

            hangarPageRoot.anchorMax =
                new Vector2(.5f, 1f);

            hangarPageRoot.pivot =
                new Vector2(.5f, 1f);

            hangarPageRoot.anchoredPosition =
                new Vector2(0f, -12f);

            hangarPageRoot.sizeDelta =
                new Vector2(920f, 1080f);

            // Page number.
            hangarPageIndicatorText = CreateText(
                hangarPageRoot,
                $"BAY {hangarPageIndex + 1:00}  /  {Skins.Length:00}",
                new Vector2(0f, 502f),
                new Vector2(500f, 38f),
                23,
                Hex("#a9d8ea"),
                TextAnchor.MiddleCenter,
                FontStyle.Bold);

            // ==========================================================
            // REAL CAROUSEL TRACK
            // ==========================================================

            var trackObject = new GameObject(
                "Bird carousel track",
                typeof(RectTransform));

            trackObject.transform.SetParent(
                hangarPageRoot,
                false);

            hangarCarouselTrack =
                trackObject.GetComponent<RectTransform>();

            hangarCarouselTrack.anchorMin =
                new Vector2(.5f, .5f);

            hangarCarouselTrack.anchorMax =
                new Vector2(.5f, .5f);

            hangarCarouselTrack.pivot =
                new Vector2(.5f, .5f);

            hangarCarouselTrack.sizeDelta =
                new Vector2(1250f, 420f);

            hangarCarouselTrack.anchoredPosition =
                new Vector2(0f, 245f);

            // Previous bird first, so it renders behind centre.
            hangarPreviousBirdRoot = CreateHangarCarouselBird(
            hangarCarouselTrack,
            Skins[previousIndex],
            -HangarCarouselSpacing,
            false,
            ShowPreviousHangarBird);

            hangarNextBirdRoot = CreateHangarCarouselBird(
                hangarCarouselTrack,
                Skins[nextIndex],
                HangarCarouselSpacing,
                false,
                ShowNextHangarBird);

            hangarCurrentBirdRoot = CreateHangarCarouselBird(
                hangarCarouselTrack,
                skin,
                0f,
                true,
                null);
            hangarPreviousBirdCanvas =
hangarPreviousBirdRoot != null
    ? hangarPreviousBirdRoot.GetComponent<CanvasGroup>()
    : null;

            hangarCurrentBirdCanvas =
                hangarCurrentBirdRoot != null
                    ? hangarCurrentBirdRoot.GetComponent<CanvasGroup>()
                    : null;

            hangarNextBirdCanvas =
                hangarNextBirdRoot != null
                    ? hangarNextBirdRoot.GetComponent<CanvasGroup>()
                    : null;

            var swipeSurface = CreateImage(
            hangarPageRoot,
            "Hangar swipe surface",
            new Vector2(0f, 245f),
            new Vector2(900f, 420f),
            new Color(1f, 1f, 1f, .001f));

            swipeSurface.sprite = whiteSprite;
            swipeSurface.raycastTarget = true;

            var swipeTrigger = swipeSurface.gameObject.AddComponent<EventTrigger>();

            AddHangarSwipeEvent(
                swipeTrigger,
                EventTriggerType.BeginDrag,
                OnHangarBeginDrag);

            AddHangarSwipeEvent(
                swipeTrigger,
                EventTriggerType.Drag,
                OnHangarDrag);

            AddHangarSwipeEvent(
                swipeTrigger,
                EventTriggerType.EndDrag,
                OnHangarEndDrag);

            // ==========================================================
            // FLOATING GLASS INFORMATION CONSOLE
            // ==========================================================

            var informationGlass = CreateHangarGlassPanel(
                hangarPageRoot,
                "Flight profile glass",
                new Vector2(0f, -225f),
                new Vector2(790f, 470f),
                accent,
                .11f,
                .34f);
            hangarInformationCanvas =
        informationGlass.gameObject.AddComponent<CanvasGroup>();
            hangarInformationCanvas.alpha =
            hangarInformationFadeTime >= 0f ? 0f : 1f;
            var shimmer = CreateImage(informationGlass, "Glass travelling reflection",
            new Vector2(-315f, 0f),
            new Vector2(105f, 390f),
            new Color(.82f, .96f, 1f, .055f));

            shimmer.sprite = softCircleSprite;
            shimmer.raycastTarget = false;

            hangarGlassShimmer = shimmer.rectTransform;
            hangarGlassShimmerImage = shimmer;

            hangarGlassShimmer.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    -9f);

            var birdName = CreateText(
                informationGlass,
                skin.Name,
                new Vector2(0f, 172f),
                new Vector2(680f, 60f),
                44,
                Hex("#f5fbff"),
                TextAnchor.MiddleCenter,
                FontStyle.Bold);

            birdName.resizeTextForBestFit = true;
            birdName.resizeTextMinSize = 30;
            birdName.resizeTextMaxSize = 44;

            CreateText(
                informationGlass,
                profile.Rarity,
                new Vector2(0f, 126f),
                new Vector2(420f, 34f),
                25,
                profile.RarityColour,
                TextAnchor.MiddleCenter,
                FontStyle.Bold);

            var description = CreateText(
                informationGlass,
                profile.Description,
                new Vector2(0f, 82f),
                new Vector2(660f, 44f),
                21,
                Hex("#b7cede"),
                TextAnchor.MiddleCenter,
                FontStyle.Normal);

            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 17;
            description.resizeTextMaxSize = 21;

            CreateHangarStatRow(
                informationGlass,
                "SPEED",
                profile.Speed,
                10f,
                accent);

            CreateHangarStatRow(
                informationGlass,
                "MANEUVERABILITY",
                profile.Maneuverability,
                -48f,
                accent);

            CreateHangarStatRow(
                informationGlass,
                "STABILITY",
                profile.Stability,
                -106f,
                accent);

            var actionLabel =
                equipped
                    ? "EQUIPPED"
                    : owned
                        ? "EQUIP BIRD"
                        : $"UNLOCK   ·   {skin.Price:N0} ✦";

            var actionButton = CreateNeonButton(
                informationGlass,
                actionLabel,
                new Vector2(0f, -182f),
                new Vector2(570f, 72f),
                equipped
                    ? Hex("#45eaff")
                    : accent);

            actionButton
                .GetComponentInChildren<Text>()
                .fontSize = 29;

            if (equipped)
            {
                actionButton.interactable = false;
            }
            else
            {
                actionButton.onClick.AddListener(
                    () => SelectSkin(skin));
            }
        }

        private void AddHangarSwipeEvent(
            EventTrigger trigger,
            EventTriggerType type,
            Action<PointerEventData> callback)
        {
            var entry = new EventTrigger.Entry
            {
                eventID = type
            };

            entry.callback.AddListener(data =>
            {
                if (data is PointerEventData pointerData)
                    callback(pointerData);
            });

            trigger.triggers.Add(entry);
        }

        private void OnHangarBeginDrag(PointerEventData eventData)
        {
            if (hangarCarouselTrack == null ||
                hangarSwipeAnimating)
                return;
            hangarDragging = true;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                hangarPageRoot,
                eventData.position,
                eventData.pressEventCamera,
                out hangarDragStartPointer);

            hangarTrackStartPosition =
                hangarCarouselTrack.anchoredPosition;
        }

        private void OnHangarDrag(PointerEventData eventData)
        {
            if (!hangarDragging || hangarCarouselTrack == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                hangarPageRoot,
                eventData.position,
                eventData.pressEventCamera,
                out var pointer);

            var dragX =
                Mathf.Clamp(
                    pointer.x - hangarDragStartPointer.x,
                    -HangarCarouselSpacing,
                    HangarCarouselSpacing);

            hangarCarouselTrack.anchoredPosition =
                new Vector2(
                    hangarTrackStartPosition.x + dragX,
                    hangarTrackStartPosition.y);

            UpdateHangarDragPresentation(dragX);
        }

        private void OnHangarEndDrag(PointerEventData eventData)
        {
            if (!hangarDragging ||
    hangarSwipeAnimating)
                return;

            hangarDragging = false;
            hangarSwipeAnimating = true;

            var dragX =
                hangarCarouselTrack.anchoredPosition.x -
                hangarTrackStartPosition.x;

            if (dragX <= -HangarSwipeThreshold)
            {
                StartCoroutine(
                    AnimateHangarSwipe(
                        -HangarCarouselSpacing,
                        1));
            }
            else if (dragX >= HangarSwipeThreshold)
            {
                StartCoroutine(
                    AnimateHangarSwipe(
                        HangarCarouselSpacing,
                        -1));
            }
            else
            {
                StartCoroutine(
                    AnimateHangarSwipe(
                        0f,
                        0));
            }
        }
        private void UpdateHangarHeroPresentation()
        {
            if (state != FlightState.Customize)
                return;

            if (cosmeticCategory != CosmeticCategory.Birds)
                return;

            if (hangarHeroBirdArt == null)
                return;

            hangarHeroAnimationTime += Time.unscaledDeltaTime;
            if (hangarHeroBirdImage != null &&
            hangarHeroWingFrames != null &&
            hangarHeroWingFrames.Length > 0)
            {
                // Ping-pong through the authored flight poses so the loop
                // returns smoothly rather than snapping from last to first.
                var frameCount = hangarHeroWingFrames.Length;

                var cycleLength =
                    Mathf.Max(1, frameCount * 2 - 2);

                var normalized =
                    Mathf.Repeat(
                        hangarHeroAnimationTime /
                        SharedWingAnimationSeconds,
                        1f);

                var sequenceIndex =
                    Mathf.FloorToInt(
                        normalized * cycleLength);

                sequenceIndex =
                    Mathf.Clamp(
                        sequenceIndex,
                        0,
                        cycleLength - 1);

                var frameIndex =
                    sequenceIndex < frameCount
                        ? sequenceIndex
                        : cycleLength - sequenceIndex;

                var frame =
                    hangarHeroWingFrames[frameIndex];

                if (frame != null)
                    hangarHeroBirdImage.sprite = frame;
                if (hangarInformationFadeTime >= 0f &&
hangarInformationCanvas != null)
                {
                    hangarInformationFadeTime +=
                        Time.unscaledDeltaTime;

                    var fadeProgress =
                        Mathf.Clamp01(
                            hangarInformationFadeTime /
                            HangarInformationFadeDuration);

                    fadeProgress =
                        fadeProgress *
                        fadeProgress *
                        (3f - 2f * fadeProgress);

                    hangarInformationCanvas.alpha =
                        fadeProgress;

                    if (fadeProgress >= 1f)
                    {
                        hangarInformationCanvas.alpha = 1f;
                        hangarInformationFadeTime = -1f;
                    }
                }
            }

            // Very slow vertical hover.
            var hover =
                Mathf.Sin(hangarHeroAnimationTime * 1.65f) * 7f;

            // Tiny aircraft-like bank. Keep this subtle.
            var bank =
                Mathf.Sin(hangarHeroAnimationTime * 1.15f) * 1.25f;

            // Small breathing scale keeps the bird alive without looking cartoony.
            var breathe =
                1f +
                Mathf.Sin(hangarHeroAnimationTime * 1.35f) * .012f;
            var arrivalScale = 1f;

            if (hangarArrivalPulseTime >= 0f)
            {
                hangarArrivalPulseTime += Time.unscaledDeltaTime;

                var arrivalProgress =
                    Mathf.Clamp01(
                        hangarArrivalPulseTime /
                        HangarArrivalPulseDuration);

                arrivalScale =
                    1f +
                    Mathf.Sin(
                        arrivalProgress * Mathf.PI) *
                    .035f;

                if (arrivalProgress >= 1f)
                    hangarArrivalPulseTime = -1f;
            }

            hangarHeroBirdArt.anchoredPosition =
                hangarHeroBirdBasePosition +
                new Vector2(0f, hover);

            hangarHeroBirdArt.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    bank);

            hangarHeroBirdArt.localScale =
            Vector3.one *
            breathe *
            arrivalScale;
            if (hangarHeroAura != null)
            {
                var auraPulse =
                    .94f +
                    Mathf.Sin(
                        hangarHeroAnimationTime * 1.8f) *
                    .06f;

                hangarHeroAura.rectTransform.localScale =
                    Vector3.one * auraPulse;

                var auraColour = hangarHeroAura.color;

                auraColour.a =
                    .13f +
                    (
                        Mathf.Sin(
                            hangarHeroAnimationTime * 1.55f)
                        * .5f + .5f
                    ) * .07f;

                hangarHeroAura.color = auraColour;
            }
        }
        private void UpdateHangarDragPresentation(float dragX)
        {
            if (hangarCurrentBirdRoot == null) return;

            var progress =
                Mathf.Clamp01(
                    Mathf.Abs(dragX) /
                    HangarCarouselSpacing);
            if (hangarInformationCanvas != null)
            {
                hangarInformationCanvas.alpha =
                    Mathf.Lerp(
                        1f,
                        .18f,
                        progress);
            }

            // Current hero shrinks and fades as it leaves centre.
            hangarCurrentBirdRoot.localScale =
                Vector3.one *
                Mathf.Lerp(1f, .68f, progress);

            if (hangarCurrentBirdCanvas != null)
            {
                hangarCurrentBirdCanvas.alpha =
                    Mathf.Lerp(
                        1f,
                        .32f,
                        progress);
            }

            // Reset neighbours before applying the active swipe direction.
            if (hangarPreviousBirdRoot != null)
                hangarPreviousBirdRoot.localScale = Vector3.one;

            if (hangarNextBirdRoot != null)
                hangarNextBirdRoot.localScale = Vector3.one;

            if (hangarPreviousBirdCanvas != null)
                hangarPreviousBirdCanvas.alpha = .50f;

            if (hangarNextBirdCanvas != null)
                hangarNextBirdCanvas.alpha = .50f;
            // Reset vertical depth before applying the active direction.
            if (hangarCurrentBirdRoot != null)
            {
                var position = hangarCurrentBirdRoot.anchoredPosition;
                position.y = 0f;
                hangarCurrentBirdRoot.anchoredPosition = position;
            }

            if (hangarPreviousBirdRoot != null)
            {
                var position = hangarPreviousBirdRoot.anchoredPosition;
                position.y = 0f;
                hangarPreviousBirdRoot.anchoredPosition = position;
            }

            if (hangarNextBirdRoot != null)
            {
                var position = hangarNextBirdRoot.anchoredPosition;
                position.y = 0f;
                hangarNextBirdRoot.anchoredPosition = position;
            }

            // Swipe LEFT:
            // next bird moves into focus.
            if (dragX < 0f && hangarNextBirdRoot != null)
            {
                hangarNextBirdRoot.localScale =
                    Vector3.one *
                    Mathf.Lerp(
                        1f,
                        1.75f,
                        progress);
                var nextPosition =
    hangarNextBirdRoot.anchoredPosition;

                nextPosition.y =
                    Mathf.Lerp(
                        0f,
                        14f,
                        progress);

                hangarNextBirdRoot.anchoredPosition =
                    nextPosition;

                var currentPosition =
                    hangarCurrentBirdRoot.anchoredPosition;

                currentPosition.y =
                    Mathf.Lerp(
                        0f,
                        -6f,
                        progress);

                hangarCurrentBirdRoot.anchoredPosition =
                    currentPosition;

                if (hangarNextBirdCanvas != null)
                {
                    hangarNextBirdCanvas.alpha =
                        Mathf.Lerp(
                            .50f,
                            1f,
                            progress);
                }

                if (hangarPreviousBirdCanvas != null)
                {
                    hangarPreviousBirdCanvas.alpha =
                        Mathf.Lerp(
                            .50f,
                            .16f,
                            progress);
                }
            }
            // Swipe RIGHT:
            // previous bird moves into focus.
            else if (dragX > 0f && hangarPreviousBirdRoot != null)
            {
                hangarPreviousBirdRoot.localScale =
                    Vector3.one *
                    Mathf.Lerp(
                        1f,
                        1.75f,
                        progress);
                var previousPosition =
    hangarPreviousBirdRoot.anchoredPosition;

                previousPosition.y =
                    Mathf.Lerp(
                        0f,
                        14f,
                        progress);

                hangarPreviousBirdRoot.anchoredPosition =
                    previousPosition;

                var currentPosition =
                    hangarCurrentBirdRoot.anchoredPosition;

                currentPosition.y =
                    Mathf.Lerp(
                        0f,
                        -6f,
                        progress);

                hangarCurrentBirdRoot.anchoredPosition =
                    currentPosition;

                if (hangarPreviousBirdCanvas != null)
                {
                    hangarPreviousBirdCanvas.alpha =
                        Mathf.Lerp(
                            .50f,
                            1f,
                            progress);
                }

                if (hangarNextBirdCanvas != null)
                {
                    hangarNextBirdCanvas.alpha =
                        Mathf.Lerp(.50f, .22f, progress);
                }
            }
        }
        private System.Collections.IEnumerator AnimateHangarSwipe(
    float targetOffset,
    int pageDirection)
        {
            if (hangarCarouselTrack == null)
            {
                hangarSwipeAnimating = false;
                yield break;
            }

            var start =
                hangarCarouselTrack.anchoredPosition;

            var target =
                new Vector2(
                    targetOffset,
                    start.y);

            var elapsed = 0f;

            while (elapsed < HangarSwipeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                var t =
                    Mathf.Clamp01(
                        elapsed / HangarSwipeDuration);

                // Smooth ease-out.
                t = 1f - Mathf.Pow(1f - t, 3f);

                hangarCarouselTrack.anchoredPosition =
                    Vector2.Lerp(
                        start,
                        target,
                        t);

                UpdateHangarDragPresentation(
                    hangarCarouselTrack.anchoredPosition.x);

                yield return null;
            }

            // Make absolutely sure we finish at the intended position.
            hangarCarouselTrack.anchoredPosition = target;

            if (pageDirection != 0)
            {
                hangarPageIndex += pageDirection;

                if (hangarPageIndex < 0)
                    hangarPageIndex = Skins.Length - 1;

                if (hangarPageIndex >= Skins.Length)
                    hangarPageIndex = 0;

                // New information panel will fade back in.
                hangarInformationFadeTime = 0f;

                RebuildCustomizeGrid();

                // Tiny arrival settle on the new centre bird.
                hangarArrivalPulseTime = 0f;
            }
            else
            {
                // Swipe was too small, so return everything to its normal state.
                hangarCarouselTrack.anchoredPosition =
                    new Vector2(
                        0f,
                        target.y);

                if (hangarCurrentBirdRoot != null)
                {
                    hangarCurrentBirdRoot.localScale =
                        Vector3.one;

                    var position =
                        hangarCurrentBirdRoot.anchoredPosition;

                    position.y = 0f;

                    hangarCurrentBirdRoot.anchoredPosition =
                        position;
                }

                if (hangarPreviousBirdRoot != null)
                {
                    hangarPreviousBirdRoot.localScale =
                        Vector3.one;

                    var position =
                        hangarPreviousBirdRoot.anchoredPosition;

                    position.y = 0f;

                    hangarPreviousBirdRoot.anchoredPosition =
                        position;
                }

                if (hangarNextBirdRoot != null)
                {
                    hangarNextBirdRoot.localScale =
                        Vector3.one;

                    var position =
                        hangarNextBirdRoot.anchoredPosition;

                    position.y = 0f;

                    hangarNextBirdRoot.anchoredPosition =
                        position;
                }

                if (hangarCurrentBirdCanvas != null)
                    hangarCurrentBirdCanvas.alpha = 1f;

                if (hangarPreviousBirdCanvas != null)
                    hangarPreviousBirdCanvas.alpha = .50f;

                if (hangarNextBirdCanvas != null)
                    hangarNextBirdCanvas.alpha = .50f;

                if (hangarInformationCanvas != null)
                    hangarInformationCanvas.alpha = 1f;
            }

            // Snap has completely finished.
            // New swipe input is allowed again.
            hangarSwipeAnimating = false;
        }
        private RectTransform CreateHangarGlassPanel(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color accent,
            float fillAlpha,
            float edgeAlpha)
        {
            var glass = CreateImage(
                parent,
                name,
                position,
                size,
                new Color(
                    .015f,
                    .030f,
                    .050f,
                    Mathf.Clamp01(fillAlpha)));

            // Rounded glass instead of a square slab.
            glass.sprite = roundedPanelSprite;
            glass.type = Image.Type.Sliced;
            glass.raycastTarget = false;

            AddOutline(
                glass.gameObject,
                new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    edgeAlpha),
                1.15f);

            // Soft depth below the floating glass.
            var shadow = glass.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .38f);
            shadow.effectDistance = new Vector2(0f, -7f);

            // Large subtle coloured atmosphere.
            var atmosphere = CreateImage(
                glass.rectTransform,
                "Glass ambient glow",
                new Vector2(0f, 12f),
                new Vector2(
                    size.x * .90f,
                    size.y * .78f),
                new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    .035f));

            atmosphere.sprite = softCircleSprite;
            atmosphere.raycastTarget = false;

            // Thin bright reflection across the upper glass edge.
            var reflection = CreateImage(
                glass.rectTransform,
                "Glass upper reflection",
                new Vector2(
                    0f,
                    size.y * .5f - 10f),
                new Vector2(
                    size.x - 50f,
                    3f),
                new Color(
                    .80f,
                    .96f,
                    1f,
                    .28f));

            reflection.sprite = whiteSprite;
            reflection.raycastTarget = false;

            // Secondary inner edge gives the panel physical thickness.
            var innerFrame = CreateImage(
                glass.rectTransform,
                "Glass inner frame",
                Vector2.zero,
                new Vector2(
                    size.x - 18f,
                    size.y - 18f),
                new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    .035f));

            innerFrame.sprite = roundedPanelSprite;
            innerFrame.type = Image.Type.Sliced;
            innerFrame.raycastTarget = false;

            AddOutline(
                innerFrame.gameObject,
                new Color(
                    .70f,
                    .92f,
                    1f,
                    .10f),
                .7f);

            // Bottom edge reflection, much dimmer.
            var lowerReflection = CreateImage(
                glass.rectTransform,
                "Glass lower reflection",
                new Vector2(
                    0f,
                    -size.y * .5f + 10f),
                new Vector2(
                    size.x - 72f,
                    2f),
                new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    .12f));

            lowerReflection.sprite = whiteSprite;
            lowerReflection.raycastTarget = false;

            return glass.rectTransform;
        }
        private RectTransform CreateHangarCarouselBird(
            Transform parent,
            Skin skin,
            float x,
            bool selected,
            Action onPressed)
        {
            var owned = IsSkinOwned(skin);
            var accent = skin.Accent;
            var profile = GetBirdHangarProfile(skin);

            var presentationColour = selected
                ? Color.Lerp(
                    accent,
                    profile.RarityColour,
                    .55f)
                : accent;

            // No visible rectangular card.
            var rootObject = new GameObject(
                selected
                    ? skin.Name + " hero"
                    : skin.Name + " neighbour",
                typeof(RectTransform));

            rootObject.transform.SetParent(parent, false);

            var root = rootObject.GetComponent<RectTransform>();
            var canvasGroup =
    rootObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha =
                selected ? 1f : .50f;

            root.anchorMin = root.anchorMax =
                new Vector2(.5f, .5f);

            root.pivot =
                new Vector2(.5f, .5f);

            root.anchoredPosition =
                new Vector2(x, 0f);

            root.sizeDelta = selected
                ? new Vector2(520f, 390f)
                : new Vector2(260f, 290f);

            // Invisible touch target for neighbouring birds.
            if (!selected && onPressed != null)
            {
                var hitArea = CreateImage(
                    root,
                    "Neighbour touch area",
                    Vector2.zero,
                    root.sizeDelta,
                    new Color(1f, 1f, 1f, .001f));

                hitArea.sprite = whiteSprite;
                hitArea.raycastTarget = true;

                var button =
                    hitArea.gameObject.AddComponent<Button>();

                button.targetGraphic = hitArea;

                button.onClick.AddListener(
                    () => onPressed());

                hitArea.gameObject
                    .AddComponent<SkyPulseButtonFeedback>();
            }

            // Soft colour atmosphere behind each bird.
            var aura = CreateImage(
                root,
                selected
                    ? "Hero atmosphere"
                    : "Neighbour atmosphere",
                new Vector2(0f, 5f),
                selected
                    ? new Vector2(460f, 310f)
                    : new Vector2(210f, 180f),
                new Color(
    presentationColour.r,
    presentationColour.g,
    presentationColour.b,
    selected ? .16f : .045f));

            aura.sprite = softCircleSprite;
            aura.raycastTarget = false;
            if (selected)
                hangarHeroAura = aura;

            // Holographic scanner.
            var scanner = CreateUiGlyph(
                root,
                selected
                    ? "Hero scanner"
                    : "Neighbour scanner",
                new Vector2(0f, selected ? 8f : 14f),
                selected
                    ? new Vector2(455f, 315f)
                    : new Vector2(205f, 170f),
                new Color(
    presentationColour.r,
    presentationColour.g,
    presentationColour.b,
    selected ? .68f : .17f),
                SkyPulseUiGlyph.Kind.DockRing);

            scanner.Animate = selected;

            // Only the featured bird gets the full platform.
            if (selected)
            {
                var platform = CreateUiGlyph(
                    root,
                    "Hero docking platform",
                    new Vector2(0f, -22f),
                    new Vector2(470f, 320f),
                   new Color(
    presentationColour.r,
    presentationColour.g,
    presentationColour.b,
    .76f),
                    SkyPulseUiGlyph.Kind.DockPlatform);

                platform.Variant =
                    DockVariantFor(skin);

                CreateUiGlyph(
                    root,
                    "Hero horizon",
                    new Vector2(0f, -118f),
                    new Vector2(405f, 70f),
                     new Color(
        presentationColour.r,
        presentationColour.g,
        presentationColour.b,
        .38f),
                    SkyPulseUiGlyph.Kind.Horizon);
            }

            // Bird itself.
            var preview = CreateImage(
                root,
                selected
                    ? "Selected bird"
                    : "Neighbour bird",
                new Vector2(0f, selected ? 8f : 8f),
                selected
                    ? new Vector2(480f, 315f)
                    : new Vector2(230f, 180f),
                selected
                    ? owned
                        ? Color.white
                        : new Color(.55f, .62f, .76f, .88f)
                    : owned
                        ? new Color(1f, 1f, 1f, .48f)
                        : new Color(.45f, .52f, .66f, .26f));

            preview.sprite =
                LoadPresentationPose(skin.UnlockPath)
                ??
                LoadSprite(skin.ArtPath);

            preview.preserveAspect = true;
            preview.raycastTarget = false;
            if (selected)
            {
                hangarHeroBirdArt = preview.rectTransform;
                hangarHeroBirdImage = preview;

                hangarHeroBirdBasePosition =
                    hangarHeroBirdArt.anchoredPosition;

                hangarHeroAnimationTime = 0f;

                if (skin.FlapFramePaths != null &&
                    skin.FlapFramePaths.Length > 0)
                {
                    hangarHeroWingFrames =
                        new Sprite[skin.FlapFramePaths.Length];

                    for (var frameIndex = 0;
                         frameIndex < skin.FlapFramePaths.Length;
                         frameIndex++)
                    {
                        hangarHeroWingFrames[frameIndex] =
                            LoadSprite(
                                skin.FlapFramePaths[frameIndex]);
                    }
                }
                else
                {
                    hangarHeroWingFrames = null;
                }
            }

            // Side birds only need a subtle identity label.
            if (!selected)
            {
                var neighbourName = CreateText(
                    root,
                    skin.Name,
                    new Vector2(0f, -112f),
                    new Vector2(235f, 36f),
                    19,
                    new Color(.82f, .91f, 1f, .58f),
                    TextAnchor.MiddleCenter,
                    FontStyle.Bold);

                neighbourName.resizeTextForBestFit = true;
                neighbourName.resizeTextMinSize = 13;
                neighbourName.resizeTextMaxSize = 19;
            }

            return root;
        }
        private void CreateHangarStatRow(
            Transform parent,
            string label,
            int value,
            float y,
            Color accent)
        {
            CreateText(
                parent,
                label,
                new Vector2(-220f, y),
                new Vector2(260f, 40f),
                23,
                Hex("#c2d8e9"),
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            for (var index = 0; index < 5; index++)
            {
                var filled = index < value;

                var segment = CreateImage(
                    parent,
                    $"{label} segment {index + 1}",
                    new Vector2(65f + index * 58f, y),
                    new Vector2(42f, 12f),
                    filled
                        ? new Color(
                            accent.r,
                            accent.g,
                            accent.b,
                            .96f)
                        : new Color(
                            .14f,
                            .22f,
                            .32f,
                            .72f));

                segment.sprite = whiteSprite;
                segment.raycastTarget = false;

                if (filled)
                {
                    var glow = CreateImage(
                        parent,
                        $"{label} glow {index + 1}",
                        new Vector2(65f + index * 58f, y),
                        new Vector2(50f, 20f),
                        new Color(
                            accent.r,
                            accent.g,
                            accent.b,
                            .10f));

                    glow.sprite = whiteSprite;
                    glow.raycastTarget = false;
                    glow.transform.SetSiblingIndex(
                        Mathf.Max(0, segment.transform.GetSiblingIndex() - 1));
                }
            }
        }

        private void CreateHangarRatingRow(
            Transform parent,
            string label,
            int value,
            float y,
            Color accent)
        {
            CreateText(
                parent,
                label,
                new Vector2(-235f, y),
                new Vector2(270f, 42f),
                24,
                Hex("#b5cbe0"),
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            for (var index = 0; index < 5; index++)
            {
                var filled = index < value;

                var dot = CreateImage(
                    parent,
                    $"{label} stat {index + 1}",
                    new Vector2(70f + index * 60f, y),
                    new Vector2(28f, 28f),
                    filled
                        ? new Color(accent.r, accent.g, accent.b, .95f)
                        : new Color(.20f, .28f, .40f, .55f));

                dot.sprite = softCircleSprite;
                dot.raycastTarget = false;
            }
        }

        private void ShowPreviousHangarBird()
        {
            if (Skins == null || Skins.Length == 0) return;

            hangarPageIndex--;

            if (hangarPageIndex < 0)
                hangarPageIndex = Skins.Length - 1;

            RebuildCustomizeGrid();
        }

        private void ShowNextHangarBird()
        {
            if (Skins == null || Skins.Length == 0) return;

            hangarPageIndex++;

            if (hangarPageIndex >= Skins.Length)
                hangarPageIndex = 0;

            RebuildCustomizeGrid();
        }
        private void CreateBirdHangarCard(int index, Skin skin)
        {
            var column = index % 2;
            var row = index / 2;
            var owned = IsSkinOwned(skin);
            var equipped = equippedSkin != null && equippedSkin.Id == skin.Id;
            var profile = GetBirdHangarProfile(skin);
            var light = equipped ? Hex("#45eaff") : skin.Accent;
            var card = CreatePanel(customizeContent, $"{skin.Name} hangar card",
                new Vector2((column - .5f) * BirdHangarColumnStride, -278f - row * BirdHangarRowStride),
                new Vector2(BirdHangarCardWidth, BirdHangarCardHeight),
                new Color(.018f, .040f, .080f, equipped ? .42f : owned ? .22f : .12f));
            card.anchorMin = card.anchorMax = new Vector2(.5f, 1f);
            card.pivot = new Vector2(.5f, 1f);
            if (equipped) AddOutline(card.gameObject, new Color(light.r, light.g, light.b, .42f), 1.2f);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() => SelectSkin(skin));
            card.gameObject.AddComponent<SkyPulseButtonFeedback>();
            var platform = CreateUiGlyph(card, "Signature bird docking platform", new Vector2(0f, 22f), new Vector2(392f, 242f),
                new Color(skin.Accent.r, skin.Accent.g, skin.Accent.b, owned ? .80f : .42f), SkyPulseUiGlyph.Kind.DockPlatform);
            platform.Variant = DockVariantFor(skin);
            var ring = CreateUiGlyph(card, "Bird docking scanner", new Vector2(0f, 19f), new Vector2(359f, 224f),
                new Color(light.r, light.g, light.b, equipped ? .58f : owned ? .26f : .13f), SkyPulseUiGlyph.Kind.DockRing);
            ring.Animate = equipped;
            CreateUiGlyph(card, "Dock runway", new Vector2(0f, -78f), new Vector2(332f, 70f),
                new Color(light.r, light.g, light.b, equipped ? .6f : .2f), SkyPulseUiGlyph.Kind.Horizon);
            CreateText(card, $"BAY {index + 1:00}", new Vector2(-120f, 165f), new Vector2(146f, 30f), 21, Hex("#6985a4"), TextAnchor.MiddleLeft, FontStyle.Bold);
            CreateText(card, equipped ? "EQUIPPED" : owned ? "READY" : "LOCKED", new Vector2(107f, 165f), new Vector2(174f, 36f), 24,
                equipped ? Hex("#45eaff") : owned ? Hex("#b2d1df") : Hex("#7b8ba7"), TextAnchor.MiddleRight, FontStyle.Bold);
            var preview = CreateImage(card, "Bird preview", new Vector2(0f, 39f), new Vector2(366f, 216f),
                owned ? Color.white : new Color(.48f, .56f, .72f, .76f));
            preview.sprite = LoadPresentationPose(skin.UnlockPath) ?? LoadSprite(skin.ArtPath);
            preview.preserveAspect = true;
            preview.raycastTarget = false;
            var name = CreateText(card, skin.Name, new Vector2(0f, -104f), new Vector2(396f, 48f), 31, owned ? Hex("#f4fbff") : Hex("#b9c7de"), TextAnchor.MiddleCenter, FontStyle.Bold);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 26;
            name.resizeTextMaxSize = 31;
            CreateText(card, profile.Rarity, new Vector2(-85f, -156f), new Vector2(220f, 36f), 24,
                new Color(profile.RarityColour.r, profile.RarityColour.g, profile.RarityColour.b, owned ? .95f : .65f), TextAnchor.MiddleLeft, FontStyle.Bold);
            var status = equipped ? "ACTIVE" : owned ? "EQUIP ›" : $"{skin.Price:N0} ✦";
            CreateText(card, status, new Vector2(118f, -156f), new Vector2(164f, 36f), 28,
                equipped ? Hex("#45eaff") : owned ? Hex("#dcefff") : Hex("#cbd7e8"), TextAnchor.MiddleRight, FontStyle.Bold);
        }

        private void CreateBirdHangarDetailPanel(Skin skin)
        {
            if (skin == null) return;
            var panel = CreatePanel(customizeContent, "Equipped bird profile", new Vector2(0f, -20f), new Vector2(888f, 226f), new Color(.018f, .055f, .09f, .46f));
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, 1f);
            panel.pivot = new Vector2(.5f, 1f);
            var platform = CreateUiGlyph(panel, "Equipped signature docking platform", new Vector2(-290f, 0f), new Vector2(285f, 196f),
                new Color(skin.Accent.r, skin.Accent.g, skin.Accent.b, .86f), SkyPulseUiGlyph.Kind.DockPlatform);
            platform.Variant = DockVariantFor(skin);
            CreateUiGlyph(panel, "Equipped flight dock", new Vector2(-290f, 0f), new Vector2(260f, 180f), new Color(.27f, .92f, 1f, .42f), SkyPulseUiGlyph.Kind.DockRing).Animate = true;
            var preview = CreateImage(panel, "Equipped bird portrait", new Vector2(-289f, 4f), new Vector2(240f, 177f), Color.white);
            preview.sprite = LoadPresentationPose(skin.UnlockPath) ?? LoadSprite(skin.ArtPath);
            preview.preserveAspect = true;
            preview.raycastTarget = false;
            CreateText(panel, "ACTIVE FLIGHT SIGNATURE", new Vector2(122f, 74f), new Vector2(524f, 33f), 21, Hex("#45eaff"), TextAnchor.MiddleLeft, FontStyle.Bold);
            var name = CreateText(panel, skin.Name, new Vector2(122f, 25f), new Vector2(524f, 53f), 34, Hex("#f4fbff"), TextAnchor.MiddleLeft, FontStyle.Bold);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 28;
            name.resizeTextMaxSize = 34;
            CreateText(panel, "Your bird. Your neon signature.", new Vector2(122f, -25f), new Vector2(524f, 40f), 24, Hex("#adc6dd"), TextAnchor.MiddleLeft, FontStyle.Normal);
            CreateText(panel, "SHARED HANDLING · EVERY BIRD", new Vector2(122f, -75f), new Vector2(524f, 38f), 24, Hex("#7d9cb8"), TextAnchor.MiddleLeft, FontStyle.Bold);
        }

        private static int DockVariantFor(Skin skin)
        {
            // A structural signature accompanies the existing bird artwork:
            // swept flight decks, reactor cradles, prism spires and solar docks.
            switch (skin.Id)
            {
                case "chrome_raven":
                case "newbird03":
                case "newbird09":
                case "newbird10": return 1;
                case "prism_hummingbird":
                case "newbird02":
                case "newbird04":
                case "newbird06": return 2;
                case "koiwing_glider":
                case "newbird01":
                case "newbird07":
                case "newbird08": return 3;
                default: return 0;
            }
        }

        private void CreateCosmeticCard(int index, string title, string status, Color accent, Sprite preview, Action select, Color secondary = default, Color tertiary = default, bool pipePreview = false)
        {
            var column = index % 2;
            var row = index / 2;
            var card = CreatePanel(customizeContent, $"{title} card", new Vector2(column == 0 ? -235f : 235f, -14f - row * CosmeticCardRowStride), new Vector2(440f, CosmeticCardHeight), Hex("#0b1022"));
            card.anchorMin = new Vector2(.5f, 1f);
            card.anchorMax = new Vector2(.5f, 1f);
            card.pivot = new Vector2(.5f, 1f);
            AddOutline(card.gameObject, accent, status == "EQUIPPED" ? 3f : 1.5f);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() => select());

            if (preview != null)
            {
                var image = CreateImage(card, "Preview", new Vector2(0f, 43f), new Vector2(392f, 142f), Color.white);
                image.sprite = preview;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            else if (pipePreview)
            {
                var outer = CreateImage(card, "Pipe preview shell", new Vector2(0f, 45f), new Vector2(206f, 80f), Hex("#030613"));
                var panel = CreateImage(card, "Pipe preview panel", new Vector2(0f, 45f), new Vector2(182f, 62f), secondary);
                var core = CreateImage(card, "Pipe preview core", new Vector2(0f, 45f), new Vector2(8f, 62f), tertiary);
                outer.raycastTarget = panel.raycastTarget = core.raycastTarget = false;
            }
            else
            {
                var glow = CreateImage(card, "Trail glow", new Vector2(0f, 46f), new Vector2(236f, 25f), secondary);
                var core = CreateImage(card, "Trail core", new Vector2(0f, 46f), new Vector2(204f, 8f), accent);
                glow.raycastTarget = core.raycastTarget = false;
            }

            var nameText = CreateText(card, title, new Vector2(0f, -64f), new Vector2(400f, 38f), 28, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 18;
            nameText.resizeTextMaxSize = 28;
            nameText.raycastTarget = false;

            var statusPanel = CreatePanel(card, "Selection status", new Vector2(0f, -108f), new Vector2(398f, 30f), new Color(.018f, .035f, .10f, .92f));
            statusPanel.GetComponent<Image>().raycastTarget = false;
            AddOutline(statusPanel.gameObject, new Color(accent.r, accent.g, accent.b, .42f), .8f);
            var statusText = CreateText(statusPanel, status, Vector2.zero, new Vector2(378f, 28f), 20, status == "EQUIPPED" ? accent : new Color(.90f, .94f, 1f, .86f), TextAnchor.MiddleCenter, FontStyle.Bold);
            statusText.resizeTextForBestFit = true;
            statusText.resizeTextMinSize = 13;
            statusText.resizeTextMaxSize = 20;
            statusText.raycastTarget = false;
        }

        private Sprite GetUpgradeArtwork(Upgrade upgrade)
        {
            PowerUpKind kind;
            if (string.Equals(upgrade.Branch, "COLLECTION", StringComparison.Ordinal)) kind = PowerUpKind.CrystalMagnet;
            else if (string.Equals(upgrade.Branch, "RECOVERY", StringComparison.Ordinal)) kind = PowerUpKind.Aegis;
            else kind = PowerUpKind.TimePulse;
            return LoadSprite(PowerUpArtworkPath(kind));
        }

        private void EquipSkin(Skin skin)
        {
            equippedSkin = skin;
            equippedTrail = GetTrailForSkin(skin);

            ApplyEquippedVisuals();
            SaveProgress();
            RebuildCustomizeGrid();
        }
        private TrailStyle GetTrailForSkin(Skin skin)
        {
            if (skin == null)
                return Trails[0];

            var bestMatch = Trails[0];
            var bestDistance = float.MaxValue;

            foreach (var trail in Trails)
            {
                var dr = trail.Core.r - skin.Trail.r;
                var dg = trail.Core.g - skin.Trail.g;
                var db = trail.Core.b - skin.Trail.b;

                var distance = dr * dr + dg * dg + db * db;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestMatch = trail;
                }
            }

            return bestMatch;
        }
        private bool IsSkinOwned(Skin skin)
        {
            return skin.Price <= 0 || ownedSkinIds.Contains(skin.Id);
        }

        private int GetUpgradeLevel(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            return upgradeLevels.TryGetValue(id, out var level)
                ? level
                : ownedUpgradeIds.Contains(id) ? 1 : 0;
        }

        private static int UpgradeAliasLevel(string id, string prefix)
        {
            if (string.IsNullOrEmpty(id) || !id.StartsWith(prefix, StringComparison.Ordinal)) return 0;
            return int.TryParse(id.Substring(prefix.Length), out var level) ? level : 0;
        }

        private bool IsUpgradePrerequisiteMet(Upgrade upgrade)
        {
            if (upgrade == null || !upgrade.HasPrerequisite) return true;
            return GetUpgradeLevel(upgrade.PrerequisiteId) >= upgrade.PrerequisiteLevel;
        }

        // Permanent Tech helpers are deliberately isolated here. They affect crystal
        // collection and rewards only, never the bird, score, gates, route difficulty,
        // scroll speed, collision, or power-up timing.
        private float CrystalResonatorRadiusFraction()
        {
            switch (GetUpgradeLevel("crystal_resonator"))
            {
                case 3: return 0.18f;
                case 2: return 0.13f;
                case 1: return 0.08f;
                default: return 0f;
            }
        }

        private float SalvageCodecBonusFraction()
        {
            switch (GetUpgradeLevel("salvage_codec"))
            {
                case 3: return 0.5f;
                case 2: return 0.35f;
                case 1: return 0.2f;
                default: return 0f;
            }
        }

        private float PrismConduitBonusFraction()
        {
            switch (GetUpgradeLevel("prism_conduit"))
            {
                case 3: return 0.5f;
                case 2: return 0.3f;
                case 1: return 0.15f;
                default: return 0f;
            }
        }

        private float GravityWellRadiusFraction()
        {
            switch (GetUpgradeLevel("gravity_well"))
            {
                case 3: return 0.1f;
                case 2: return 0.06f;
                case 1: return 0.03f;
                default: return 0f;
            }
        }

        private int RecoveryCacheBonus()
        {
            switch (GetUpgradeLevel("recovery_cache"))
            {
                case 3: return 25;
                case 2: return 16;
                case 1: return 8;
                default: return 0;
            }
        }

        private int ArchiveEngineBestBonus()
        {
            switch (GetUpgradeLevel("archive_engine"))
            {
                case 3: return 100;
                case 2: return 50;
                case 1: return 25;
                default: return 0;
            }
        }

        private int PrecisionHarvesterPassInterval()
        {
            switch (GetUpgradeLevel("precision_harvester"))
            {
                case 3: return 1;
                case 2: return 2;
                case 1: return 3;
                default: return 0;
            }
        }

        private int StreakCapacitorGateReward()
        {
            switch (GetUpgradeLevel("streak_capacitor"))
            {
                case 3: return 15;
                case 2: return 10;
                case 1: return 5;
                default: return 0;
            }
        }

        private float ApexMatrixBonusFraction()
        {
            switch (GetUpgradeLevel("apex_matrix"))
            {
                case 3: return 0.5f;
                case 2: return 0.3f;
                case 1: return 0.15f;
                default: return 0f;
            }
        }

        private float TotalCrystalAttractionRadiusFraction()
        {
            return CrystalResonatorRadiusFraction() + GravityWellRadiusFraction();
        }

        private int CrystalPickupValue(int baseAmount)
        {
            if (baseAmount <= 0) return 0;
            prismConduitCarry += baseAmount * PrismConduitBonusFraction();
            var bonus = Mathf.FloorToInt(prismConduitCarry + .0001f);
            if (bonus > 0) prismConduitCarry -= bonus;
            return baseAmount + bonus;
        }

        private void ApplyPrecisionHarvesterReward()
        {
            var interval = PrecisionHarvesterPassInterval();
            if (interval <= 0 || perfectPasses <= 0 || perfectPasses % interval != 0) return;
            BankCollectedCrystals(1);
            ShowCrystalBurst(1);
        }

        private void ApplyStreakCapacitorReward()
        {
            var reward = StreakCapacitorGateReward();
            if (reward <= 0 || score <= 0 || score % 15 != 0) return;
            BankCollectedCrystals(reward);
            ShowCrystalBurst(reward);
        }

        private void BeginProgressionRun()
        {
            runCrystalsCollected = 0;
            runCrystalBonus = 0;
            prismConduitCarry = 0f;
            runFarthestWorldIndex = 0;
            resultCrystalBonusApplied = false;
        }

        private void BankCollectedCrystals(int amount)
        {
            if (amount <= 0) return;
            crystals += amount;
            runCrystalsCollected += amount;
            UpdateCrystalLabels();
            SaveProgress();
        }

        private int ApplyResultTechBonuses()
        {
            if (resultCrystalBonusApplied) return runCrystalBonus;
            resultCrystalBonusApplied = true;

            // Percentage bonuses share the same pre-result reward base, so stacking
            // never compounds a bonus on top of another bonus by accident.
            var rewardBase = runCrystalsCollected;
            var salvageBonus = Mathf.FloorToInt(rewardBase * SalvageCodecBonusFraction() + .0001f);
            var apexBonus = score >= 10
                ? Mathf.FloorToInt(rewardBase * ApexMatrixBonusFraction() + .0001f)
                : 0;
            var recoveryBonus = RecoveryCacheBonus();
            var archiveBonus = newBest ? ArchiveEngineBestBonus() : 0;
            runCrystalBonus = salvageBonus + apexBonus + recoveryBonus + archiveBonus;

            if (runCrystalBonus > 0)
            {
                crystals += runCrystalBonus;
                UpdateCrystalLabels();
                SaveProgress();
            }
            return runCrystalBonus;
        }

        private void RecordFarthestWorld(int worldIndex)
        {
            runFarthestWorldIndex = Mathf.Max(runFarthestWorldIndex, Mathf.Clamp(worldIndex, 0, 2));
            farthestWorldIndex = Mathf.Max(farthestWorldIndex, runFarthestWorldIndex);
            SaveProgress();
        }

        private static string RouteWorldName(int worldIndex)
        {
            return Worlds[Mathf.Clamp(worldIndex, 0, Worlds.Length - 1)].Name;
        }

        private void RefreshProgressionResultLabels()
        {
            var bonus = ApplyResultTechBonuses();
            if (resultCrystalsText != null) resultCrystalsText.text = $"RUN CRYSTALS  ·  {runCrystalsCollected}";
            if (resultBonusText != null) resultBonusText.text = $"TECH REWARD BONUS  ·  +{bonus}";
            if (resultBalanceText != null) resultBalanceText.text = $"TOTAL BALANCE  ·  {crystals}";
            if (resultWorldText != null) resultWorldText.text = $"ROUTE REACHED  ·  {RouteWorldName(runFarthestWorldIndex)}";
            if (resultShareText != null) resultShareText.text = "SHARE";
        }

        private void CopyRunSummaryToClipboard()
        {
            GUIUtility.systemCopyBuffer = $"I scored {score} in SkyPulse and reached {RouteWorldName(runFarthestWorldIndex)} with {runCrystalsCollected} crystals.";
            if (resultShareText != null) resultShareText.text = "COPIED TO CLIPBOARD";
        }

        private void SelectSkin(Skin skin)
        {
            if (IsSkinOwned(skin))
            {
                EquipSkin(skin);
                return;
            }
            OpenPurchaseModal(skin);
        }

        private void SelectUpgrade(Upgrade upgrade)
        {
            if (upgrade == null || !IsUpgradePrerequisiteMet(upgrade)) return;
            var level = GetUpgradeLevel(upgrade.Id);
            if (level >= upgrade.MaxLevel) return;
            pendingUpgrade = upgrade;
            pendingSkin = null;
            pendingPurchase = PendingPurchase.Upgrade;
            OpenPurchaseModal($"{upgrade.Name}  ·  L{level + 1}", upgrade.EffectAtLevel(level), upgrade.PriceAtLevel(level), upgrade.Accent, GetUpgradeArtwork(upgrade) ?? softCircleSprite);
            SetPurchaseUpgradeArtwork(upgrade);
        }

        private void OpenPurchaseModal(Skin skin)
        {
            pendingSkin = skin;
            pendingUpgrade = null;
            pendingPurchase = PendingPurchase.Skin;
            OpenPurchaseModal(skin.Name, "THIS BIRD WILL BE EQUIPPED AFTER UNLOCKING", skin.Price, skin.Accent, LoadSprite(skin.ArtPath));
        }

        private void OpenPurchaseModal(string itemName, string detail, int price, Color accent, Sprite preview)
        {
            purchaseUpgradeGlyph.gameObject.SetActive(false);
            purchasePreviewImage.enabled = true;
            purchasePreviewImage.rectTransform.sizeDelta = new Vector2(465f, 248f);
            purchasePreviewImage.sprite = preview;
            purchasePreviewImage.color = preview == softCircleSprite ? new Color(accent.r, accent.g, accent.b, .88f) : Color.white;
            purchaseHalo.color = new Color(accent.r, accent.g, accent.b, .10f);
            purchaseTitleText.text = pendingPurchase == PendingPurchase.Upgrade ? $"INSTALL {itemName}?" : $"UNLOCK {itemName}?";
            purchaseDetailText.text = $"SPEND  {price}  ✦  ·  {detail}";
            var remainder = Mathf.Max(0, price - crystals);
            purchaseBalanceText.text = remainder == 0
                ? $"YOUR BALANCE · {crystals} ✦"
                : $"YOUR BALANCE · {crystals} ✦   ·   NEED {remainder} MORE";
            purchaseConfirmButton.interactable = crystals >= price;
            purchaseConfirmText.text = crystals >= price ? $"CONFIRM · {price} ✦" : "NOT ENOUGH ✦";
            purchaseModal.SetActive(true);
        }

        private void SetPurchaseUpgradeArtwork(Upgrade upgrade)
        {
            if (upgrade.Branch == "COLLECTION")
            {
                purchasePreviewImage.sprite = LoadSprite(CrystalArtworkPath);
                purchasePreviewImage.rectTransform.sizeDelta = new Vector2(110f, 148f);
                return;
            }
            purchasePreviewImage.enabled = false;
            purchaseUpgradeGlyph.Shape = upgrade.Branch == "RECOVERY" ? SkyPulseUiGlyph.Kind.RecoveryCore : SkyPulseUiGlyph.Kind.WingMark;
            purchaseUpgradeGlyph.color = upgrade.Accent;
            purchaseUpgradeGlyph.SetVerticesDirty();
            purchaseUpgradeGlyph.gameObject.SetActive(true);
        }

        private void ClosePurchaseModal()
        {
            if (purchaseModal != null) purchaseModal.SetActive(false);
            pendingSkin = null;
            pendingUpgrade = null;
            pendingPurchase = PendingPurchase.None;
        }

        private void ConfirmPurchase()
        {
            if (pendingPurchase == PendingPurchase.Upgrade &&
                (pendingUpgrade == null || !IsUpgradePrerequisiteMet(pendingUpgrade) || GetUpgradeLevel(pendingUpgrade.Id) >= pendingUpgrade.MaxLevel))
            {
                ClosePurchaseModal();
                return;
            }
            if (purchaseConfirmButton != null) purchaseConfirmButton.interactable = false;
            var price = pendingPurchase == PendingPurchase.Skin && pendingSkin != null ? pendingSkin.Price
                : pendingPurchase == PendingPurchase.Upgrade && pendingUpgrade != null
                    ? pendingUpgrade.PriceAtLevel(GetUpgradeLevel(pendingUpgrade.Id)) : -1;
            if (price < 0 || crystals < price)
            {
                if (purchaseConfirmButton != null) purchaseConfirmButton.interactable = crystals >= Mathf.Max(0, price);
                return;
            }
            var unlockedSkin = pendingPurchase == PendingPurchase.Skin ? pendingSkin : null;
            var keepTreePosition = pendingPurchase == PendingPurchase.Upgrade && customizeContent != null;
            var treePosition = keepTreePosition ? customizeContent.anchoredPosition : Vector2.zero;
            if (pendingPurchase == PendingPurchase.Upgrade)
            {
                var level = GetUpgradeLevel(pendingUpgrade.Id);
                if (level >= pendingUpgrade.MaxLevel || !IsUpgradePrerequisiteMet(pendingUpgrade))
                {
                    ClosePurchaseModal();
                    return;
                }
            }

            crystals -= price;
            if (pendingPurchase == PendingPurchase.Skin)
            {
                ownedSkinIds.Add(pendingSkin.Id);
                equippedSkin = pendingSkin;
                equippedTrail = GetTrailForSkin(pendingSkin);
            }
            else
            {
                var level = GetUpgradeLevel(pendingUpgrade.Id);
                upgradeLevels[pendingUpgrade.Id] = level + 1;
                ownedUpgradeIds.Add(pendingUpgrade.Id);
            }
            ClosePurchaseModal();
            ApplyEquippedVisuals();
            SaveProgress();
            Play(unlockSound);
            RebuildCustomizeGrid();
            if (keepTreePosition)
            {
                var maxScroll = Mathf.Max(0f, customizeContent.sizeDelta.y - customizeScroll.viewport.rect.height);
                customizeContent.anchoredPosition = new Vector2(0f, Mathf.Clamp(treePosition.y, 0f, maxScroll));
            }
            if (unlockedSkin != null)
            {
                PulseHaptic(.30f);
                ShowUnlockReveal(unlockedSkin);
            }
        }

        private void EquipWorld(WorldTheme world)
        {
            equippedWorld = world;
            equippedPipe = FindById(PipeStyles, world.PresetPipeId) ?? equippedPipe ?? PipeStyles[0];
            ApplyEquippedVisuals();
            SaveProgress();
            RebuildCustomizeGrid();
        }

        private void EquipPipe(PipeStyle style)
        {
            equippedPipe = style;
            ApplyEquippedVisuals();
            SaveProgress();
            RebuildCustomizeGrid();
        }

        private void ApplyEquippedVisuals()
        {
            ResetWorldTransition();
            if (equippedSkin == null) equippedSkin = Skins[0];
            if (equippedTrail == null) equippedTrail = GetTrailForSkin(equippedSkin);
            if (equippedWorld == null) equippedWorld = Worlds[0];
            if (equippedPipe == null) equippedPipe = PipeStyles[0];

            backgroundRenderer.sprite = WorldBackdrop(equippedWorld);
            FitBackgroundToCamera(backgroundRenderer, 1.1f);
            backgroundVeil.color = new Color(equippedWorld.Accent.r, equippedWorld.Accent.g, equippedWorld.Accent.b, WorldAtmosphereTintAlpha);
            var floorColour = equippedWorld.Floor;
            floorColour.a = .54f;
            floorSurface.color = floorColour;
            var railColour = equippedWorld.Accent;
            railColour.a = .38f;
            floorGlow.color = railColour;
            var lipColour = Darken(equippedWorld.Floor, .65f);
            lipColour.a = .78f;
            floorLip.color = lipColour;
            SetBirdArtwork();
            SetArtworkImage(menuBirdImage, idleBirdSprite);
            SetArtworkImage(menuBirdFlapImage, flapBirdSprite);
            SetArtworkImage(menuBirdRiseImage, riseBirdSprite);
            SetArtworkImage(menuBirdShadowImage, idleBirdSprite);
            if (menuBirdSafetyImage != null)
            {
                var usesEmergencyFallback = idleBirdSprite == emergencyBirdSprite;
                menuBirdSafetyImage.sprite = usesEmergencyFallback ? emergencyBirdSprite : null;
                menuBirdSafetyImage.enabled = usesEmergencyFallback;
            }
            if (menuBirdEyeGlintImage != null) menuBirdEyeGlintImage.gameObject.SetActive(!UsesFlapFrameSequence());
            UpdateCrystalLabels();
            if (menuEquippedText != null) menuEquippedText.text = $"EQUIPPED  ·  {equippedSkin.Name}";
            UpdateModeCopy();

            foreach (var pair in pipePool)
            {
                if (pair != null && pair.Root.activeSelf) ConfigurePipe(pair, pair.X);
            }
        }

        private void UpdateCrystalLabels()
        {
            if (menuCrystalText != null) menuCrystalText.text = crystals.ToString();
            if (hudCrystalText != null) hudCrystalText.text = crystals.ToString();
            if (customizeCrystalText != null) customizeCrystalText.text = crystals.ToString();
        }

        private Sprite WorldBackdrop(WorldTheme world)
        {
            if (world == null) return midnightSprite;
            var authored = LoadSprite(world.BackgroundPath);
            if (authored != null) return authored;
            if (worldFallbackSprites.TryGetValue(world.Id, out var fallback)) return fallback;

            // A missing background must still look like an intentional world, not a
            // black frame. This small atmospheric fallback is created only when an
            // authored texture cannot be resolved and is cached for the session.
            const int width = 192;
            const int height = 384;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = world.Name + " atmospheric fallback",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color[width * height];
            var deep = Darken(world.Floor, .48f);
            var horizon = Darken(world.Accent, .28f);
            for (var y = 0; y < height; y += 1)
            {
                var vertical = y / (float)(height - 1);
                var glowBand = Mathf.Exp(-Mathf.Pow((vertical - .54f) / .18f, 2f));
                for (var x = 0; x < width; x += 1)
                {
                    var horizontal = x / (float)(width - 1) - .5f;
                    var centreFalloff = 1f - Mathf.Clamp01(Mathf.Abs(horizontal) * 1.55f);
                    var ray = Mathf.Pow(Mathf.Clamp01(Mathf.Sin((horizontal + vertical * .16f) * 14f) * .5f + .5f), 12f) * .10f;
                    var colour = Color.Lerp(deep, horizon, glowBand * (.28f + centreFalloff * .34f) + ray);
                    colour.a = 1f;
                    pixels[y * width + x] = colour;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            fallback = CreateSprite(texture, 64f);
            worldFallbackSprites[world.Id] = fallback;
            return fallback;
        }

        private void SetBirdArtwork()
        {
            if (birdRenderer == null || birdFlapRenderer == null || birdRiseRenderer == null) return;
            var hasFlapFrameSequence = LoadFlapFrameSequence(equippedSkin);
            hitBirdSprite = LoadPresentationPose(equippedSkin?.HitPath);
            ResetGameplayWingState();
            activeFlapFrameIndex =
                hasFlapFrameSequence
                    ? flapFrameBirdSprites.Length - 1
                    : 0;
            if (hasFlapFrameSequence)
            {
                // The final authored pose is the settled glide. The named fields
                // keep the visual fallback and hangar preview resilient; flight
                // itself uses all six distinct poses.
                idleBirdSprite = flapFrameBirdSprites[flapFrameBirdSprites.Length - 1];
                flapBirdSprite = flapFrameBirdSprites[Mathf.Min(3, flapFrameBirdSprites.Length - 1)];
                riseBirdSprite = flapFrameBirdSprites[0];
            }
            else
            {
                // A missing optional frame never makes the player invisible. The
                // skin's normal art remains the safe visual fallback.
                idleBirdSprite = LoadSprite(equippedSkin?.ArtPath);
                flapBirdSprite = LoadSprite(equippedSkin?.FlapPath) ?? idleBirdSprite;
                riseBirdSprite = string.IsNullOrEmpty(equippedSkin.RisePath)
                    ? flapBirdSprite
                    : LoadSprite(equippedSkin.RisePath) ?? flapBirdSprite;
            }
            if (idleBirdSprite != null)
            {
                birdRenderer.sprite = idleBirdSprite;
                idleBirdBaseScale = ArtworkScale(birdRegistrationReference ?? idleBirdSprite, BirdDisplayWidth);
                authoredBirdBaseScale = idleBirdBaseScale;
                birdArt.localScale = idleBirdBaseScale;
                if (birdParallaxRenderer != null)
                {
                    birdParallaxRenderer.sprite = idleBirdSprite;
                    parallaxBirdBaseScale = idleBirdBaseScale;
                    birdParallaxRenderer.transform.localScale = parallaxBirdBaseScale;
                }
            }
            if (flapBirdSprite != null)
            {
                birdFlapRenderer.sprite = flapBirdSprite;
                flapBirdBaseScale = ArtworkScale(flapBirdSprite, BirdDisplayWidth);
                birdFlapArt.localScale = flapBirdBaseScale;
            }
            if (riseBirdSprite != null)
            {
                birdRiseRenderer.sprite = riseBirdSprite;
                riseBirdBaseScale = ArtworkScale(riseBirdSprite, BirdDisplayWidth);
                birdRiseArt.localScale = riseBirdBaseScale;
            }
            birdRenderer.enabled = idleBirdSprite != null;
            birdFlapRenderer.enabled = !hasFlapFrameSequence && flapBirdSprite != null;
            birdRiseRenderer.enabled = !hasFlapFrameSequence && riseBirdSprite != null;
            if (birdParallaxRenderer != null) birdParallaxRenderer.enabled = idleBirdSprite != null && !hasFlapFrameSequence;
            if (birdSafetyRenderer != null)
            {
                var usesEmergencyFallback = idleBirdSprite == emergencyBirdSprite;
                birdSafetyRenderer.sprite = emergencyBirdSprite;
                // Keep a missing import readable, but never cover a valid cosmetic
                // with a different bird's silhouette.
                safetyBirdBaseScale = ArtworkScale(emergencyBirdSprite, BirdDisplayWidth * 1.08f);
                birdSafetyRenderer.transform.localScale = safetyBirdBaseScale;
                birdSafetyRenderer.transform.localPosition = new Vector3(-.005f, -.012f, 0f);
                birdSafetyRenderer.color = Color.white;
                birdSafetyRenderer.enabled = usesEmergencyFallback;
            }
            if (birdDepthRenderer != null) birdDepthRenderer.enabled = idleBirdSprite != null;
            if (birdEyeGlintRenderer != null) birdEyeGlintRenderer.enabled = !hasFlapFrameSequence;

        }

        private bool LoadFlapFrameSequence(Skin skin)
        {
            flapFrameBirdSprites = null;
            birdRegistrationReference = null;
            if (skin == null || skin.FlapFramePaths == null || skin.FlapFramePaths.Length != 6) return false;
            if (skin.FlapFramePivots != null && skin.FlapFramePivots.Length != skin.FlapFramePaths.Length) return false;
            var frames = new Sprite[skin.FlapFramePaths.Length];
            for (var index = 0; index < frames.Length; index += 1)
            {
                var pivot =
                    skin.FlapFramePivots == null
                        ? new Vector2(.5f, .5f)
                        : skin.FlapFramePivots[index];

                frames[index] = LoadRegisteredFlapFrame(skin.FlapFramePaths[index], pivot);
                if (frames[index] == null) return false;
            }

            // The newer sheets end with another raised pose, not a downstroke.
            // Place that pose beside the other raised wings before ping-pong playback.
            if (skin.Id.StartsWith("newbird", StringComparison.Ordinal))
                frames = new[] { frames[0], frames[5], frames[1], frames[2], frames[3], frames[4] };

            birdRegistrationReference = LoadOptionalSprite(skin.ArtPath);
            flapFrameBirdSprites = frames;
            return true;
        }

        private Sprite LoadPresentationPose(string path)
        {
            var source = LoadOptionalSprite(path);
            if (source == null) return null;
            var key = "pose:" + path;
            if (registeredFlapSpriteCache.TryGetValue(key, out var cached)) return cached;
            if (!poseBoundsLoaded)
            {
                poseBoundsLoaded = true;
                var data = Resources.Load<TextAsset>("SkyPulse/characters/bird-pose-bounds");
                var file = data == null ? null : JsonUtility.FromJson<BirdPoseBoundsFile>(data.text);
                if (file?.frames != null)
                    foreach (var frame in file.frames) poseBounds[frame.path] = frame;
            }
            if (!poseBounds.TryGetValue(path, out var bounds)) return source;
            var rect = new Rect(bounds.x, bounds.y, bounds.width, bounds.height);
            if (rect.width <= 0f || rect.height <= 0f || rect.xMin < 0f || rect.yMin < 0f
                || rect.xMax > source.texture.width || rect.yMax > source.texture.height) return source;
            var pose = Sprite.Create(source.texture, rect, new Vector2(.5f, .5f), source.pixelsPerUnit);
            pose.name = source.name + " presentation";
            registeredFlapSpriteCache[key] = pose;
            return pose;
        }

        private Sprite LoadRegisteredFlapFrame(string path, Vector2 pivot)
        {
            var source = LoadOptionalSprite(path);
            if (source == null) return null;

            if (!birdFrameRegistrationLoaded)
            {
                birdFrameRegistrationLoaded = true;
                var data = Resources.Load<TextAsset>("SkyPulse/characters/bird-frame-registration");
                var file = data == null ? null : JsonUtility.FromJson<BirdFrameRegistrationFile>(data.text);
                if (file?.frames != null)
                    foreach (var frame in file.frames)
                        if (!string.IsNullOrEmpty(frame.path) && frame.scale > 0f)
                            birdFrameRegistration[frame.path] = frame;
            }
            var scale = 1f;
            if (birdFrameRegistration.TryGetValue(path, out var registration))
            {
                pivot = new Vector2(registration.pivotX, registration.pivotY);
                scale = registration.scale;
            }

            // Centered art requires no additional sprite. Registration is only
            // used for an authored sequence whose source canvases were offset.
            if (Mathf.Approximately(pivot.x, .5f) && Mathf.Approximately(pivot.y, .5f) && Mathf.Approximately(scale, 1f))
                return source;

            if (registeredFlapSpriteCache.TryGetValue(path, out var registered))
                return registered;

            if (source.texture == null) return source;
            registered = Sprite.Create(
                source.texture,
                source.rect,
                new Vector2(Mathf.Clamp01(pivot.x), Mathf.Clamp01(pivot.y)),
                source.pixelsPerUnit * scale);
            registered.name = source.name + " registered";
            registeredFlapSpriteCache[path] = registered;
            return registered;
        }

        private void LayoutRegisteredMenuFrame(Image target, Sprite pose)
        {
            if (target == null || pose == null || birdRegistrationReference == null) return;
            // UI Images ignore Sprite pivots and PPU when fitting a fixed rect.
            // Reproduce the gameplay registration explicitly for the hangar too.
            var referenceSize = birdRegistrationReference.rect.size / birdRegistrationReference.pixelsPerUnit;
            var units = Mathf.Min(850f / referenceSize.x, 420f / referenceSize.y);
            target.rectTransform.pivot = pose.pivot / pose.rect.size;
            target.rectTransform.sizeDelta = pose.rect.size / pose.pixelsPerUnit * units;
        }

        private bool UsesFlapFrameSequence()
        {
            return flapFrameBirdSprites != null && flapFrameBirdSprites.Length >= 2;
        }

        private Sprite SelectFlapFrame(float normalizedProgress)
        {
            if (!UsesFlapFrameSequence()) return idleBirdSprite;
            return flapFrameBirdSprites[SelectFlapFrameIndex(normalizedProgress)];
        }
        private int SelectFlapFrameIndex(float normalizedProgress)
        {
            if (!UsesFlapFrameSequence()) return 0;

            var frameCount = flapFrameBirdSprites.Length;
            if (frameCount <= 1) return 0;

            var progress = Mathf.Clamp01(normalizedProgress);
            var cycleStepCount = (frameCount * 2) - 2;
            var step = Mathf.FloorToInt(progress * cycleStepCount);
            step = Mathf.Clamp(step, 0, cycleStepCount - 1);

            // Full flap: 0,1,2,3,4,5,4,3,2,1 then loop.
            return step < frameCount ? step : cycleStepCount - step;
        }
        private static void GetWingWeights(float normalizedPhase, out float riseWeight, out float downstrokeWeight)
        {
            normalizedPhase = Mathf.Clamp01(normalizedPhase);
            riseWeight = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedPhase / WingLiftPhase));
            downstrokeWeight = Mathf.Sin(Mathf.PI * Mathf.Clamp01((normalizedPhase - WingDownstrokeDelay) / WingDownstrokeSpan));
        }

        private static Vector3 ArtworkScale(Sprite sprite, float targetWidth)
        {
            var sourceWidth = Mathf.Max(.01f, sprite.bounds.size.x);
            return Vector3.one * (targetWidth / sourceWidth);
        }

        private void UpdateBirdWingMotion()
        {
            if (birdRenderer == null || birdFlapRenderer == null)
                return;

            var usesFlapFrameSequence =
                UsesFlapFrameSequence();

            if (usesFlapFrameSequence)
            {
                // Gameplay moves a discrete authored index, then uses the same
                // selector as UpdateMenuBird to resolve that exact pose.
                if (gameplayWingState == GameplayWingState.Settled)
                    gameplayWingFrameIndex = flapFrameBirdSprites.Length - 1;

                var wingPhase = GameplayWingFramePhase();

                activeFlapFrameIndex =
                    SelectFlapFrameIndex(wingPhase);

                var pose = SelectFlapFrame(wingPhase);

                if (
                    pose != null &&
                    birdRenderer.sprite != pose)
                {
                    birdRenderer.sprite = pose;
                }

                birdRenderer.enabled =
                    pose != null;

                birdRenderer.color =
                    Color.white;

                // Each authored PNG already contains the complete bird.
                // Never display two complete bird drawings together.
                birdFlapRenderer.enabled = false;

                if (birdRiseRenderer != null)
                    birdRiseRenderer.enabled = false;

                if (birdParallaxRenderer != null)
                    birdParallaxRenderer.enabled = false;

                // Keep every authored wing frame on one stable artwork
                // transform. The parent bird object still handles the
                // smooth rise, fall and banking during gameplay.
                bird.localScale =
                    Vector3.one;

                birdArt.localScale =
                    authoredBirdBaseScale;

                birdArt.localPosition =
                    Vector3.zero;

                birdArt.localRotation =
                    Quaternion.identity;

                var authoredGlide =
                    Mathf.Clamp(
                        birdVelocity /
                        Mathf.Abs(
                            ActiveMaxFallVelocity()),
                        -1f,
                        1f);

                UpdateBirdLifeDepth(
                    0f,
                    0f,
                    authoredGlide);

                UpdateBirdPowerUpVisuals();

                return;
            }

            // Fallback behaviour for any future bird that does
            // not provide the six authored flight frames.
            var flapProgress =
                Mathf.Clamp01(
                    wingTimer / WingCycleSeconds);

            var impulseProgress =
                Mathf.Clamp01(
                    wingTimer / WingCycleSeconds);

            var flapKick =
                1f -
                impulseProgress *
                impulseProgress *
                (3f - 2f * impulseProgress);

            GetWingWeights(
                flapProgress,
                out var riseWeight,
                out var wingWave);

            var flapColour =
                Color.white;

            flapColour.a =
                (.10f + wingWave * .82f) *
                flapKick *
                (1f - riseWeight * .84f);

            birdFlapRenderer.enabled =
                flapBirdSprite != null;

            birdFlapRenderer.color =
                flapColour;

            birdRenderer.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    1f -
                    Mathf.Max(
                        riseWeight * .78f,
                        wingWave * .50f));

            if (birdRiseRenderer != null)
            {
                var showRise =
                    birdRiseRenderer.enabled &&
                    birdRiseRenderer.sprite != null;

                birdRiseRenderer.color =
                    new Color(
                        1f,
                        1f,
                        1f,
                        showRise
                            ? riseWeight * .94f
                            : 0f);

                birdRiseArt.localScale =
                    Vector3.Scale(
                        riseBirdBaseScale,
                        new Vector3(
                            1f + riseWeight * .065f,
                            1f - riseWeight * .025f,
                            1f));

                birdRiseArt.localPosition =
                    new Vector3(
                        -riseWeight * .060f,
                        riseWeight * .065f,
                        0f);

                birdRiseArt.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        riseWeight * 4.7f);
            }
            var lifeMotion = reduceMotionEnabled ? .38f : 1f;
            var authoredFlight = usesFlapFrameSequence;
            var breathing = 1f + Mathf.Sin(ambientTime * 5.2f) * .010f * lifeMotion;
            var glide = Mathf.Clamp(birdVelocity / Mathf.Abs(ActiveMaxFallVelocity()), -1f, 1f);
            var liftSquash = (flapKick - riseWeight * .34f) * (authoredFlight ? .035f : .065f);
            var diveStretch = Mathf.Clamp01(-glide) * (authoredFlight ? .016f : .024f);

            var bodyRoll = authoredFlight
                ? riseWeight * 2.20f
                    - wingWave * 1.40f
                    + glide * 1.25f
                : riseWeight * 3.2f
                    - wingWave * 2.4f
                    + glide * 1.8f;

            var depthPulse = authoredFlight
                ? riseWeight * .025f
                    + wingWave * .018f
                : riseWeight * .075f
                    + wingWave * .050f;
            bird.localScale = new Vector3(1f + depthPulse + diveStretch * .30f, 1f - depthPulse * .62f + diveStretch * .12f, 1f);
            var activeArtworkBaseScale = usesFlapFrameSequence
                ? authoredBirdBaseScale
                : idleBirdBaseScale;
            birdArt.localScale = Vector3.Scale(activeArtworkBaseScale, new Vector3(breathing + liftSquash + diveStretch, breathing - liftSquash - diveStretch * .55f, 1f));
            birdArt.localPosition = authoredFlight
                ? new Vector3(
                    -flapKick * .035f - wingWave * .012f - glide * .012f,
                    Mathf.Sin(ambientTime * 7f) * .006f * lifeMotion
                        + flapKick * .018f
                        + riseWeight * .025f
                        + wingWave * .008f,
                    0f)
                : new Vector3(
                    -flapKick * .052f - glide * .020f,
                    Mathf.Sin(ambientTime * 7f) * .014f + riseWeight * .018f,
                    0f);
            birdArt.localRotation = Quaternion.Euler(0f, 0f, bodyRoll);
            if (!usesFlapFrameSequence)
            {
                birdFlapArt.localScale = Vector3.Scale(flapBirdBaseScale, new Vector3(1f + wingWave * .075f, 1f - wingWave * .050f, 1f));
                birdFlapArt.localPosition = new Vector3(flapKick * .032f, .025f + wingWave * .052f, 0f);
                birdFlapArt.localRotation = Quaternion.Euler(0f, 0f, -flapKick * 6.6f + wingWave * 4.4f + glide * 1.2f);
            }
            UpdateBirdLifeDepth(riseWeight, wingWave, glide);
            UpdateBirdPowerUpVisuals();
        }
        private void UpdateBirdLifeDepth(float riseWeight, float wingWave, float glide)
        {
            if (equippedSkin == null) return;
            if (birdDepthRenderer != null)
            {
                var bodyLight = equippedSkin.Accent;
                bodyLight.a = .085f + riseWeight * .09f + wingWave * .045f;
                birdDepthRenderer.color = bodyLight;
                birdDepthRenderer.transform.localPosition = new Vector3(-.10f - glide * .025f, -.035f, 0f);
                birdDepthRenderer.transform.localScale = new Vector3(1.52f + riseWeight * .16f + wingWave * .10f, .62f + riseWeight * .08f, 1f);
            }
            if (birdParallaxRenderer != null)
            {
                if (UsesFlapFrameSequence())
                {
                    birdParallaxRenderer.enabled = false;
                }
                else
                {
                    var parallaxColour = equippedSkin.Accent;
                    parallaxColour.a = .055f + riseWeight * .045f + wingWave * .025f;
                    birdParallaxRenderer.color = parallaxColour;
                    birdParallaxRenderer.transform.localPosition = new Vector3(-.075f - glide * .055f, -.020f - riseWeight * .025f, 0f);
                    birdParallaxRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, glide * -2.5f + riseWeight * 2.4f);
                    birdParallaxRenderer.transform.localScale = parallaxBirdBaseScale * (1.025f + riseWeight * .045f + wingWave * .020f);
                }
            }
            if (birdEyeGlintRenderer != null && !UsesFlapFrameSequence())
            {
                var blinkPhase = Mathf.Repeat(ambientTime * .27f + .18f, 1f);
                var blink = blinkPhase < .045f ? Mathf.SmoothStep(.16f, 1f, blinkPhase / .045f) : 1f;
                var glint = Color.Lerp(Color.white, equippedSkin.Accent, .18f);
                glint.a = .36f * blink + riseWeight * .10f;
                birdEyeGlintRenderer.color = glint;
                birdEyeGlintRenderer.transform.localPosition = new Vector3(.53f + glide * .020f, .19f + Mathf.Sin(ambientTime * 4.2f) * .010f, 0f);
                birdEyeGlintRenderer.transform.localScale = Vector3.one * (.060f + riseWeight * .010f);
            }
        }

        private void UpdateBirdPowerUpVisuals()
        {
            // Stable, fine geometry leaves the authored bird as the visual focus.
            var motion = reduceMotionEnabled ? 0f : 1f;
            var breath = Mathf.Sin(ambientTime * 3f) * .025f * motion;
            if (slowAuraRenderer != null)
            {
                slowAuraRenderer.enabled = slowFieldTimer > 0f;
                slowAuraRenderer.color = new Color(.69f, .49f, 1f, .66f);
                slowAuraRenderer.transform.localPosition = new Vector3(0f, -.18f, 0f);
                slowAuraRenderer.transform.localScale = Vector3.one * (2.02f + breath);
                slowAuraRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -ambientTime * 24f * motion);
            }
            if (effectAuraRenderer != null)
            {
                effectAuraRenderer.enabled = magnetHaloTimer > 0f;
                effectAuraRenderer.color = new Color(.27f, .92f, 1f, .68f);
                effectAuraRenderer.transform.localPosition = new Vector3(0f, -.18f, 0f);
                effectAuraRenderer.transform.localScale = Vector3.one * (2.08f - breath);
                effectAuraRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, ambientTime * 18f * motion);
            }
            if (shieldAuraRenderer != null)
            {
                shieldAuraRenderer.enabled = shieldCharges > 0 || shieldFlashTimer > 0f;
                var activation = Mathf.Clamp01(shieldFlashTimer / .6f);
                shieldAuraRenderer.color = new Color(.38f, 1f, .70f, .52f + activation * .22f);
                shieldAuraRenderer.transform.localPosition = new Vector3(0f, -.18f, 0f);
                shieldAuraRenderer.transform.localScale = Vector3.one * (2.08f + activation * .10f + breath);
                shieldAuraRenderer.transform.localRotation = Quaternion.identity;
            }
        }

        private void UpdateModeCopy()
        {
            if (difficultyText != null)
            {
                difficultyText.text = "ENDLESS ROUTE";
                difficultyText.color = Hex("#8f64ff");
            }
            if (menuModeDetailText != null)
            {
                menuModeDetailText.text = "COLLECT CRYSTALS  ·  MASTER THE FLOW";
                menuModeDetailText.color = Hex("#45eaff");
            }
            if (menuRouteText != null)
            {
                menuRouteText.text = $"{Worlds[0].Name}  →  {Worlds[1].Name}  →  {Worlds[2].Name}";
            }
            if (hudModeText != null)
            {
                hudModeText.text = RouteWorldName(routeWorldIndex);
                hudModeText.color = routeWorld == null ? Hex("#45eaff") : routeWorld.Accent;
            }
            if (menuBestText != null) menuBestText.text = best.ToString();
            if (hudBestText != null) hudBestText.text = $"BEST  {best}";
        }

        private void UpdateFlightCoach()
        {
            if (hudCoachText == null) return;
            var activePowerUp = hudPowerUpText != null && hudPowerUpText.gameObject.activeSelf;
            var showCoach = state == FlightState.Playing && flightCoachStage < 2 && !activePowerUp;
            hudCoachText.gameObject.SetActive(showCoach);
            if (!showCoach) return;

            var firstStep = flightCoachStage == 0;
            hudCoachText.text = firstStep
                ? "TAP TO FLAP  ·  CLEAR THE GLOWING GATE"
                : "CRYSTALS UNLOCK BIRDS  ·  IMPROVE COLLECTION";
            var colour = firstStep ? Hex("#45eaff") : Hex("#ffc34d");
            colour.a = .66f + Mathf.Sin(ambientTime * 2.4f) * .12f;
            hudCoachText.color = colour;
        }

        private void AdvanceFlightCoach()
        {
            if (flightCoachStage >= 2) return;
            flightCoachStage += 1;
            SaveProgress();
        }

        private void ToggleReduceMotion()
        {
            reduceMotionEnabled = !reduceMotionEnabled;
            UpdateComfortCopy();
            SaveProgress();
        }

        private void ToggleHaptics()
        {
            hapticsEnabled = !hapticsEnabled;
            UpdateComfortCopy();
            SaveProgress();
        }

        private void UpdateComfortCopy()
        {
            if (reduceMotionText != null) reduceMotionText.text = reduceMotionEnabled ? "REDUCED MOTION  ·  ON" : "REDUCED MOTION  ·  OFF";
            if (hapticsText != null) hapticsText.text = hapticsEnabled ? "HAPTICS  ·  ON" : "HAPTICS  ·  OFF";
        }

        private void RefreshScreens()
        {
            homeScreen.SetActive(state == FlightState.Menu);
            hudScreen.SetActive(state == FlightState.Playing || state == FlightState.Impact);
            pauseScreen.SetActive(state == FlightState.Paused);
            gameOverScreen.SetActive(state == FlightState.GameOver);
            customizeScreen.SetActive(state == FlightState.Customize);
            if (state == FlightState.Menu)
            {
                UpdateModeCopy();
                menuEquippedText.text = $"EQUIPPED  ·  {equippedSkin.Name}";
            }
        }

        private bool WasTapped()
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
            return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow);
        }

        private static bool PointerOverUi()
        {
            if (EventSystem.current == null) return false;
            if (Input.touchCount > 0) return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return EventSystem.current.IsPointerOverGameObject();
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
        }

        private void PulseHaptic(float cooldown)
        {
#if !UNITY_EDITOR
            // Keep tactile feedback reserved for high-value moments. Flaps remain
            // silent to the haptic motor, while perfect passes, pickups, saves and
            // impacts earn a single crisp pulse without rapid-fire vibration.
            if (!hapticsEnabled || Time.unscaledTime < hapticCooldownUntil) return;
            hapticCooldownUntil = Time.unscaledTime + cooldown;
            Handheld.Vibrate();
#endif
        }

        private void LoadProgress()
        {
            best = PlayerPrefs.GetInt("skypulse.native.best", 0);
            crystals = PlayerPrefs.GetInt("skypulse.native.crystals", 0);
            flightCoachStage = Mathf.Clamp(PlayerPrefs.GetInt("skypulse.native.flight-coach-stage", 0), 0, 2);
            reduceMotionEnabled = PlayerPrefs.GetInt("skypulse.native.reduce-motion", 0) == 1;
            hapticsEnabled = PlayerPrefs.GetInt("skypulse.native.haptics", 1) == 1;
            farthestWorldIndex = Mathf.Clamp(PlayerPrefs.GetInt("skypulse.native.farthest-world", 0), 0, 2);
            var savedSkinId = MigrateRosterSkinId(PlayerPrefs.GetString("skypulse.native.skin", "neon_finch"));
            equippedSkin = FindById(Skins, savedSkinId) ?? Skins[0];
            equippedWorld = FindById(Worlds, PlayerPrefs.GetString("skypulse.native.world", "neon_city")) ?? Worlds[0];
            equippedTrail = GetTrailForSkin(equippedSkin);
            equippedPipe = FindById(PipeStyles, PlayerPrefs.GetString("skypulse.native.pipe", "ion")) ?? PipeStyles[0];
            var savedOwnedSkins = PlayerPrefs.GetString("skypulse.native.owned-skins", string.Empty);
            if (!string.IsNullOrEmpty(savedOwnedSkins))
            {
                foreach (var id in savedOwnedSkins.Split(','))
                {
                    var migratedId = MigrateRosterSkinId(id);
                    if (!string.IsNullOrEmpty(migratedId)) ownedSkinIds.Add(migratedId);
                }
            }
            else
            {
                // Existing native players keep the bird they were already using when the
                // collection gains unlock states; new players simply begin with Nova.
                ownedSkinIds.Add(Skins[0].Id);
                if (equippedSkin != null) ownedSkinIds.Add(equippedSkin.Id);
            }
            if (PlayerPrefs.GetInt("skypulse.native.cyber-roster-v2", 0) == 0)
            {
                // Keep legacy hangar ownership where it maps to a current bird. New
                // pilots start with Neon Finch only; existing owned birds stay earned.
                ownedSkinIds.Add(Skins[0].Id);
                PlayerPrefs.SetInt("skypulse.native.cyber-roster-v2", 1);
            }
            var savedOwnedUpgrades = PlayerPrefs.GetString("skypulse.native.owned-upgrades", string.Empty);
            if (!string.IsNullOrEmpty(savedOwnedUpgrades))
            {
                foreach (var id in savedOwnedUpgrades.Split(','))
                {
                    if (FindById(Upgrades, id) != null)
                    {
                        ownedUpgradeIds.Add(id);
                        upgradeLevels[id] = Mathf.Max(GetUpgradeLevel(id), 1);
                        continue;
                    }

                    // Older builds stored separate level IDs such as
                    // crystal_resonator_2. Collapse those aliases into the current
                    // levelled node without touching any other save data.
                    var resonanceLevel = UpgradeAliasLevel(id, "crystal_resonator_");
                    if (resonanceLevel > 0)
                    {
                        ownedUpgradeIds.Add("crystal_resonator");
                        upgradeLevels["crystal_resonator"] = Mathf.Max(GetUpgradeLevel("crystal_resonator"), resonanceLevel);
                        continue;
                    }
                    var salvageLevel = UpgradeAliasLevel(id, "salvage_codec_");
                    if (salvageLevel > 0)
                    {
                        ownedUpgradeIds.Add("salvage_codec");
                        upgradeLevels["salvage_codec"] = Mathf.Max(GetUpgradeLevel("salvage_codec"), salvageLevel);
                    }
                }
            }
            foreach (var upgrade in Upgrades)
            {
                var level = Mathf.Clamp(PlayerPrefs.GetInt($"skypulse.native.upgrade.{upgrade.Id}", GetUpgradeLevel(upgrade.Id)), 0, upgrade.MaxLevel);
                if (level <= 0) continue;
                upgradeLevels[upgrade.Id] = level;
                ownedUpgradeIds.Add(upgrade.Id);
            }
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt("skypulse.native.best", best);
            PlayerPrefs.SetInt("skypulse.native.crystals", crystals);
            PlayerPrefs.SetInt("skypulse.native.farthest-world", farthestWorldIndex);
            PlayerPrefs.SetInt("skypulse.native.flight-coach-stage", flightCoachStage);
            PlayerPrefs.SetInt("skypulse.native.reduce-motion", reduceMotionEnabled ? 1 : 0);
            PlayerPrefs.SetInt("skypulse.native.haptics", hapticsEnabled ? 1 : 0);
            PlayerPrefs.SetString("skypulse.native.skin", equippedSkin.Id);
            PlayerPrefs.SetString("skypulse.native.world", equippedWorld.Id);
            PlayerPrefs.SetString("skypulse.native.pipe", equippedPipe.Id);
            PlayerPrefs.SetString("skypulse.native.owned-skins", string.Join(",", ownedSkinIds));
            PlayerPrefs.SetString("skypulse.native.owned-upgrades", string.Join(",", ownedUpgradeIds));
            foreach (var upgrade in Upgrades)
            {
                PlayerPrefs.SetInt($"skypulse.native.upgrade.{upgrade.Id}", GetUpgradeLevel(upgrade.Id));
            }
            PlayerPrefs.Save();
        }

        private static string MigrateRosterSkinId(string id)
        {
            switch (id)
            {
                case "volt": return "neon_finch";
                case "steel": return "chrome_raven";
                case "prism": return "prism_hummingbird";
                case "cinder": return "koiwing_glider";
                case "verdant": return "verdant_kite";
                default: return id;
            }
        }

        private static T FindById<T>(IEnumerable<T> items, string id) where T : class
        {
            foreach (var item in items)
            {
                var field = typeof(T).GetField("Id");
                if (field != null && string.Equals(field.GetValue(item) as string, id, StringComparison.Ordinal)) return item;
            }
            return null;
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, Color color, int sortingOrder, Transform parent = null)
        {
            var visual = new GameObject(name);
            if (parent != null) visual.transform.SetParent(parent, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite ?? whiteSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static Sprite CreateSprite(Texture2D texture, float pixelsPerUnit = 100f)
        {
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(.5f, .5f), pixelsPerUnit);
        }

        private static Sprite CreateSolidSprite(string name, Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            return CreateSprite(texture, 1f);
        }

        private static Sprite CreateFlightPanelSprite()
        {
            const int size = 96;
            const int cut = 18;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "SkyPulse cut-corner flight panel",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    // Opposing chamfers echo the swept wing tips. Nine-slicing keeps
                    // their angle consistent across compact buttons and large panels.
                    var edge = Mathf.Min(x + y + 1f - cut, 2f * size - x - y - 1f - cut);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(edge * .7071f + .5f));
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(cut + 1, cut + 1, cut + 1, cut + 1));
        }

        private static Sprite CreateRoundedRectSprite(string name, int size, int radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color[size * size];
            var half = size * .5f;
            var straightEdge = half - radius;
            for (var y = 0; y < size; y += 1)
            {
                for (var x = 0; x < size; x += 1)
                {
                    var point = new Vector2(Mathf.Abs(x + .5f - half), Mathf.Abs(y + .5f - half));
                    var corner = new Vector2(Mathf.Max(point.x - straightEdge, 0f), Mathf.Max(point.y - straightEdge, 0f));
                    var signedDistance = corner.magnitude + Mathf.Min(Mathf.Max(point.x - straightEdge, point.y - straightEdge), 0f) - radius;
                    var alpha = 1f - Mathf.SmoothStep(-1f, 1f, signedDistance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), size, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private static Sprite CreateCylindricalPipeSprite(string name, int width, int height)
        {
            // A neutral greyscale cylinder: SpriteRenderer tint supplies the material
            // colour while this texture supplies the curved metal light across it.
            // It avoids the flat poster-board appearance of scaled white rectangles.
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y += 1)
            {
                var vertical = y / (float)(height - 1);
                var endShade = .90f + Mathf.Sin(vertical * Mathf.PI) * .10f;
                for (var x = 0; x < width; x += 1)
                {
                    var horizontal = Mathf.Abs((x + .5f) / width - .5f) * 2f;
                    var edge = 1f - Mathf.SmoothStep(.86f, 1f, horizontal);
                    var curvedLight = .38f + Mathf.Pow(Mathf.Clamp01(1f - horizontal), .48f) * .62f;
                    var value = curvedLight * endShade;
                    pixels[y * width + x] = new Color(value, value, value, edge);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return CreateSprite(texture, width);
        }

        private static Sprite CreatePowerFieldSprite(PowerUpKind kind)
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = kind + " precision field",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var p = new Vector2((x + .5f) / size - .5f, (y + .5f) / size - .5f);
                    var radius = p.magnitude;
                    var angle = Mathf.Atan2(p.y, p.x);
                    var alpha = 0f;
                    if (kind == PowerUpKind.Aegis)
                    {
                        // Six plated edges with bright corner anchors, not a solid halo.
                        var sector = Mathf.Repeat(angle + Mathf.PI / 6f, Mathf.PI / 3f) - Mathf.PI / 6f;
                        var edge = Mathf.Abs(radius * Mathf.Cos(sector) - .36f);
                        alpha = Mathf.Max(SoftFieldLine(edge, .004f), SoftFieldLine(edge, .014f) * .15f);
                        for (var corner = 0; corner < 6; corner++)
                        {
                            var a = (corner + .5f) * Mathf.PI / 3f;
                            var anchor = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (.36f / Mathf.Cos(Mathf.PI / 6f));
                            alpha = Mathf.Max(alpha, SoftFieldLine(Vector2.Distance(p, anchor), .008f));
                        }
                    }
                    else if (kind == PowerUpKind.TimePulse)
                    {
                        // Three interrupted clock arcs and twelve fine timing marks.
                        var arc = Mathf.Repeat(angle, Mathf.PI * 2f / 3f);
                        if (arc > .20f && arc < 1.88f) alpha = SoftFieldLine(Mathf.Abs(radius - .39f), .004f);
                        var tick = Mathf.Abs(Mathf.Repeat(angle + Mathf.PI / 12f, Mathf.PI / 6f) - Mathf.PI / 12f);
                        if (radius > .425f && radius < .46f) alpha = Mathf.Max(alpha, SoftFieldLine(tick * radius, .003f) * .8f);
                    }
                    else
                    {
                        // Opposed magnetic field lines; open front/rear keeps flight readable.
                        var arc = Mathf.Repeat(angle, Mathf.PI);
                        if (arc > .48f && arc < 2.66f)
                        {
                            alpha = SoftFieldLine(Mathf.Abs(radius - .42f), .004f);
                            alpha = Mathf.Max(alpha, SoftFieldLine(Mathf.Abs(radius - .35f), .003f) * .42f);
                        }
                        for (var pole = 0; pole < 2; pole++)
                        {
                            var a = .48f + pole * Mathf.PI;
                            var anchor = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * .42f;
                            alpha = Mathf.Max(alpha, SoftFieldLine(Vector2.Distance(p, anchor), .009f));
                        }
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return CreateSprite(texture, size);
        }

        private static float SoftFieldLine(float distance, float halfWidth)
        {
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(halfWidth, halfWidth + .005f, distance));
        }

        private static Sprite CreateRadialSprite(string name, int size, float innerRadius, float outerRadius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y += 1)
            {
                for (var x = 0; x < size; x += 1)
                {
                    var point = new Vector2((x + .5f) / size - .5f, (y + .5f) / size - .5f);
                    var distance = point.magnitude;
                    var alpha = 0f;
                    if (innerRadius <= 0f)
                    {
                        // SmoothStep interpolates output values; normalise distance first
                        // so the entire outer edge (including corners) is transparent.
                        alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, outerRadius, distance));
                    }
                    else if (distance >= innerRadius && distance <= outerRadius)
                    {
                        var innerFade = Mathf.InverseLerp(innerRadius, innerRadius + .045f, distance);
                        var outerFade = 1f - Mathf.InverseLerp(outerRadius - .06f, outerRadius, distance);
                        alpha = Mathf.Min(innerFade, outerFade);
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return CreateSprite(texture, size);
        }

        private Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var sprite = LoadOptionalSprite(path);
            if (sprite != null) return sprite;
            if (path.StartsWith("SkyPulse/characters/", StringComparison.Ordinal))
            {
                // A public build must never turn a missing cosmetic into an invisible
                // player. This is only reached if an authored asset was omitted from
                // the player; normal play uses the selected roster artwork.
                if (emergencyBirdSprite == null) emergencyBirdSprite = CreateEmergencyBirdSprite();
                spriteCache[path] = emergencyBirdSprite;
                return emergencyBirdSprite;
            }
            return null;
        }

        /// <summary>
        /// Loads authored optional art without silently replacing it with the emergency
        /// silhouette. Reward poses use this so each bird keeps its own identity.
        /// </summary>
        private Sprite LoadOptionalSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (spriteCache.TryGetValue(path, out var sprite)) return sprite;
            // Imported art can be authored as either a Sprite or a default texture.
            // Supporting both makes Resources loading robust across Unity reimports.
            sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                spriteCache[path] = sprite;
                return sprite;
            }
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) texture = LoadTextureFromResourceFolder(path);
#if UNITY_EDITOR
            // Unity's Device Simulator can occasionally omit a dynamically-created
            // Sprite from its Resources lookup even though the imported texture is
            // present. Resolve the same project asset directly in the editor so the
            // preview is faithful to the native build instead of losing the bird.
            if (texture == null)
            {
                texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/{path}.png");
            }
#endif
            if (texture == null) return null;
            sprite = CreateSprite(texture);
            spriteCache[path] = sprite;
            return sprite;
        }

        private Sprite LoadPipeCutoutSprite(string path, Rect normalizedRect)
        {
            if (spriteCache.TryGetValue(path, out var cached)) return cached;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            // Crop UVs only: retain compressed, non-readable GPU textures instead
            // of allocating/decompressing another full texture at runtime.
            var pixels = new Rect(normalizedRect.x * texture.width, normalizedRect.y * texture.height,
                normalizedRect.width * texture.width, normalizedRect.height * texture.height);
            var sprite = Sprite.Create(texture, pixels, new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name + " aligned gameplay cutout";
            spriteCache[path] = sprite;
            return sprite;
        }

        private Sprite LoadKeyedPipeSprite(string path, int cropTopPixels = 0, int cropBottomPixels = 0)
        {
            var source = Resources.Load<Texture2D>(path);
            if (source == null)
            {
                Debug.LogWarning($"SkyPulse: pipe artwork '{path}' was not found; using the mechanical fallback.");
                return null;
            }
            if (!source.isReadable)
            {
                Debug.LogWarning($"SkyPulse: pipe artwork '{path}' must have Read/Write enabled to remove its white presentation canvas; using the mechanical fallback.");
                return null;
            }

            try
            {
                var sourcePixels = source.GetPixels32();
                var hasSourceAlpha = false;
                for (var index = 0; index < sourcePixels.Length; index += 1)
                {
                    if (sourcePixels[index].a < 250)
                    {
                        hasSourceAlpha = true;
                        break;
                    }
                }

                var whiteBackground = new bool[sourcePixels.Length];
                if (!hasSourceAlpha) MarkConnectedWhiteCanvas(sourcePixels, source.width, source.height, whiteBackground);

                var cropBottom = Mathf.Clamp(cropBottomPixels, 0, source.height - 1);
                var cropTop = Mathf.Clamp(cropTopPixels, 0, source.height - cropBottom - 1);
                var outputHeight = source.height - cropBottom - cropTop;
                var outputPixels = new Color32[source.width * outputHeight];
                var minimumX = source.width;
                var minimumY = outputHeight;
                var maximumX = -1;
                var maximumY = -1;
                for (var y = 0; y < outputHeight; y += 1)
                {
                    for (var x = 0; x < source.width; x += 1)
                    {
                        var sourceIndex = (y + cropBottom) * source.width + x;
                        var pixel = sourcePixels[sourceIndex];
                        if (whiteBackground[sourceIndex]) pixel.a = 0;
                        outputPixels[y * source.width + x] = pixel;
                        if (pixel.a == 0) continue;
                        minimumX = Mathf.Min(minimumX, x);
                        minimumY = Mathf.Min(minimumY, y);
                        maximumX = Mathf.Max(maximumX, x);
                        maximumY = Mathf.Max(maximumY, y);
                    }
                }
                if (maximumX < minimumX || maximumY < minimumY) return null;

                var trimmedWidth = maximumX - minimumX + 1;
                var trimmedHeight = maximumY - minimumY + 1;
                var trimmedPixels = new Color32[trimmedWidth * trimmedHeight];
                for (var y = 0; y < trimmedHeight; y += 1)
                {
                    Array.Copy(outputPixels, (minimumY + y) * source.width + minimumX, trimmedPixels, y * trimmedWidth, trimmedWidth);
                }
                var texture = new Texture2D(trimmedWidth, trimmedHeight, TextureFormat.RGBA32, false)
                {
                    name = source.name + " transparent gameplay cutout",
                    filterMode = source.filterMode,
                    wrapMode = TextureWrapMode.Clamp,
                };
                texture.SetPixels32(trimmedPixels);
                texture.Apply(false, true);
                return CreateSprite(texture);
            }
            catch (UnityException exception)
            {
                Debug.LogWarning($"SkyPulse: could not prepare pipe artwork '{path}' ({exception.Message}); using the mechanical fallback.");
                return null;
            }
        }

        private static void MarkConnectedWhiteCanvas(Color32[] pixels, int width, int height, bool[] whiteBackground)
        {
            var queue = new List<int>(width * 2 + height * 2);
            for (var x = 0; x < width; x += 1)
            {
                QueueWhiteCanvasPixel(x, pixels, whiteBackground, queue);
                QueueWhiteCanvasPixel((height - 1) * width + x, pixels, whiteBackground, queue);
            }
            for (var y = 1; y < height - 1; y += 1)
            {
                QueueWhiteCanvasPixel(y * width, pixels, whiteBackground, queue);
                QueueWhiteCanvasPixel(y * width + width - 1, pixels, whiteBackground, queue);
            }

            for (var readIndex = 0; readIndex < queue.Count; readIndex += 1)
            {
                var pixelIndex = queue[readIndex];
                var x = pixelIndex % width;
                if (x > 0) QueueWhiteCanvasPixel(pixelIndex - 1, pixels, whiteBackground, queue);
                if (x + 1 < width) QueueWhiteCanvasPixel(pixelIndex + 1, pixels, whiteBackground, queue);
                if (pixelIndex >= width) QueueWhiteCanvasPixel(pixelIndex - width, pixels, whiteBackground, queue);
                if (pixelIndex + width < pixels.Length) QueueWhiteCanvasPixel(pixelIndex + width, pixels, whiteBackground, queue);
            }
        }

        private static void QueueWhiteCanvasPixel(int index, Color32[] pixels, bool[] whiteBackground, List<int> queue)
        {
            if (whiteBackground[index] || !IsWhiteCanvasPixel(pixels[index])) return;
            whiteBackground[index] = true;
            queue.Add(index);
        }

        private static bool IsWhiteCanvasPixel(Color32 pixel)
        {
            return pixel.r >= 240 && pixel.g >= 240 && pixel.b >= 240;
        }

        private static Texture2D LoadTextureFromResourceFolder(string path)
        {
            var separator = path.LastIndexOf('/');
            if (separator <= 0 || separator >= path.Length - 1) return null;
            var folder = path.Substring(0, separator);
            var assetName = path.Substring(separator + 1);
            var candidates = Resources.LoadAll<Texture2D>(folder);
            foreach (var candidate in candidates)
            {
                if (candidate != null && string.Equals(candidate.name, assetName, StringComparison.Ordinal)) return candidate;
            }
            return null;
        }

        private static Sprite CreateEmergencyBirdSprite()
        {
            // A clean, high-resolution neon swift rendered procedurally. It is not
            // part of the normal art path; it protects the player silhouette in the
            // unlikely event a cosmetic texture is unavailable in a release build.
            const int width = 512;
            const int height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "SkyPulse emergency bird silhouette",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y += 1)
            {
                for (var x = 0; x < width; x += 1)
                {
                    var px = (x + .5f) / width - .5f;
                    var py = (y + .5f) / height - .5f;
                    var colour = Color.clear;

                    var tail = SoftEllipse(px, py, -.315f, -.055f, .255f, .072f);
                    colour = AlphaComposite(colour, new Color(.035f, .075f, .20f, 1f), tail);
                    var lowerWing = SoftEllipse(px, py, -.085f, -.115f, .355f, .112f);
                    colour = AlphaComposite(colour, new Color(.045f, .11f, .31f, 1f), lowerWing);
                    var wing = SoftEllipse(px, py, -.055f, .105f, .395f, .135f);
                    var wingShade = new Color(.05f, .12f + Mathf.Clamp01(py + .18f) * .10f, .34f + Mathf.Clamp01(px + .42f) * .12f, 1f);
                    colour = AlphaComposite(colour, wingShade, wing);
                    var featherEdge = Mathf.Clamp01(wing - SoftEllipse(px, py, -.055f, .105f, .365f, .105f)) * 7f;
                    colour = AlphaComposite(colour, new Color(.16f, .86f, 1f, 1f), featherEdge * .62f);

                    var body = SoftEllipse(px, py, .075f, -.015f, .325f, .175f);
                    var bodyShade = new Color(.13f + Mathf.Clamp01(py + .15f) * .22f, .20f + Mathf.Clamp01(py + .15f) * .21f, .47f + Mathf.Clamp01(px + .25f) * .24f, 1f);
                    colour = AlphaComposite(colour, bodyShade, body);
                    var breast = SoftEllipse(px, py, .145f, -.09f, .215f, .095f);
                    colour = AlphaComposite(colour, new Color(.62f, .76f, 1f, 1f), breast * .76f);
                    var head = SoftEllipse(px, py, .335f, .035f, .125f, .128f);
                    colour = AlphaComposite(colour, new Color(.10f, .20f, .49f, 1f), head);
                    var face = SoftEllipse(px, py, .385f, -.005f, .065f, .075f);
                    colour = AlphaComposite(colour, new Color(.80f, .88f, 1f, 1f), face * .76f);
                    var beak = SoftEllipse(px, py, .455f, -.010f, .055f, .032f);
                    colour = AlphaComposite(colour, new Color(.85f, .94f, 1f, 1f), beak);
                    var eye = SoftEllipse(px, py, .365f, .058f, .024f, .024f);
                    colour = AlphaComposite(colour, new Color(.04f, .96f, 1f, 1f), eye);
                    var eyeCore = SoftEllipse(px, py, .365f, .058f, .010f, .010f);
                    colour = AlphaComposite(colour, new Color(.005f, .02f, .08f, 1f), eyeCore);
                    pixels[y * width + x] = colour;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return CreateSprite(texture);
        }

        private static float SoftEllipse(float x, float y, float centreX, float centreY, float radiusX, float radiusY)
        {
            var normalizedX = (x - centreX) / radiusX;
            var normalizedY = (y - centreY) / radiusY;
            var distance = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);
            return 1f - Mathf.SmoothStep(.94f, 1.02f, distance);
        }

        private static Color AlphaComposite(Color background, Color foreground, float opacity)
        {
            var sourceAlpha = Mathf.Clamp01(foreground.a * opacity);
            if (sourceAlpha <= 0f) return background;
            var destinationAlpha = background.a;
            var alpha = sourceAlpha + destinationAlpha * (1f - sourceAlpha);
            if (alpha <= .0001f) return Color.clear;
            return new Color(
                (foreground.r * sourceAlpha + background.r * destinationAlpha * (1f - sourceAlpha)) / alpha,
                (foreground.g * sourceAlpha + background.g * destinationAlpha * (1f - sourceAlpha)) / alpha,
                (foreground.b * sourceAlpha + background.b * destinationAlpha * (1f - sourceAlpha)) / alpha,
                alpha);
        }

        private static void SetArtworkImage(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            // An Image without a sprite renders Unity's built-in white rectangle.
            // Disabling it is both visually safe and cheaper on the UI canvas.
            image.enabled = sprite != null;
        }

        private void FitBackgroundToCamera(SpriteRenderer renderer, float padding)
        {
            if (renderer == null || renderer.sprite == null) return;
            var sourceHeight = Mathf.Max(.01f, renderer.sprite.bounds.size.y);
            var sourceWidth = Mathf.Max(.01f, renderer.sprite.bounds.size.x);
            var heightScale = (CameraHeight + padding) / sourceHeight;
            var widthScale = (GetViewportWidth() + padding) / sourceWidth;
            renderer.transform.localScale = Vector3.one * Mathf.Max(heightScale, widthScale);
        }

        private static void SetBlock(SpriteRenderer renderer, Vector2 position, Vector2 size)
        {
            renderer.transform.localPosition = new Vector3(position.x, position.y, 0f);
            renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        private static void SetPipeCollider(BoxCollider2D collider, Vector2 position, Vector2 size)
        {
            if (collider == null) return;
            collider.transform.localPosition = new Vector3(position.x, position.y, 0f);
            collider.transform.localRotation = Quaternion.identity;
            collider.transform.localScale = Vector3.one;
            collider.size = new Vector2(Mathf.Max(.01f, size.x), Mathf.Max(.01f, size.y));
        }

        private static void SetSpriteBlock(SpriteRenderer renderer, Vector2 position, Vector2 worldSize)
        {
            if (renderer == null || renderer.sprite == null) return;
            renderer.transform.localPosition = new Vector3(position.x, position.y, 0f);
            var bounds = renderer.sprite.bounds.size;
            var width = Mathf.Max(.0001f, bounds.x);
            var height = Mathf.Max(.0001f, bounds.y);
            renderer.transform.localScale = new Vector3(worldSize.x / width, worldSize.y / height, 1f);
        }

        private float GetWorldWidth()
        {
            // Collision, gate spacing, crystal radii, and speed all use this fixed
            // 9:16 logical rectangle. A wide desktop viewport only reveals more
            // decorative background; it never changes flight geometry.
            return CameraHeight * PortraitPlayfieldAspect;
        }

        private float GetViewportWidth()
        {
            var aspect = flightCamera == null ? PortraitPlayfieldAspect : flightCamera.aspect;
            return CameraHeight * aspect;
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString(value, out var color) ? color : Color.white;
        }

        private static Color Darken(Color color, float multiplier)
        {
            return new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);
        }

        private GameObject CreateScreen(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return root;
        }

        private RectTransform CreateFullPanel(Transform parent, string name, Color color)
        {
            var objectRoot = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            objectRoot.transform.SetParent(parent, false);
            var rect = objectRoot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            interfaceBackdrops.Add(rect);
            var image = objectRoot.GetComponent<Image>();
            image.color = color;
            return rect;
        }

        private RectTransform CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var image = CreateImage(parent, name, position, size, color);
            if (interfacePanelSprite != null)
            {
                image.sprite = interfacePanelSprite;
                image.type = Image.Type.Sliced;
            }
            return image.rectTransform;
        }
        private RectTransform CreateLuminousPanel(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color fillColor,
            Color outlineColor)
        {
            var panel = CreatePanel(parent, name, position, size, fillColor);
            AddOutline(panel.gameObject, outlineColor, 1.5f);
            return panel;
        }
        private Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var objectRoot = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            objectRoot.transform.SetParent(parent, false);
            var rect = objectRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = objectRoot.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment, FontStyle style)
        {
            var objectRoot = new GameObject("Text · " + value, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            objectRoot.transform.SetParent(parent, false);
            var rect = objectRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = objectRoot.GetComponent<Text>();
            text.font = uiFont;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var shadow = objectRoot.AddComponent<Shadow>();
            shadow.effectColor = new Color(.002f, .004f, .025f, .86f);
            shadow.effectDistance = new Vector2(.75f, -.75f);
            return text;
        }

        private SkyPulseUiGlyph CreateUiGlyph(Transform parent, string name, Vector2 position, Vector2 size,
            Color color, SkyPulseUiGlyph.Kind kind)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(SkyPulseUiGlyph));
            root.transform.SetParent(parent, false);
            var glyph = root.GetComponent<SkyPulseUiGlyph>();
            glyph.rectTransform.anchorMin = glyph.rectTransform.anchorMax = new Vector2(.5f, .5f);
            glyph.rectTransform.pivot = new Vector2(.5f, .5f);
            glyph.rectTransform.anchoredPosition = position;
            glyph.rectTransform.sizeDelta = size;
            glyph.Shape = kind;
            glyph.color = color;
            glyph.raycastTarget = false;
            glyph.MotionReduced = () => reduceMotionEnabled;
            return glyph;
        }

        private Text CreateChip(Transform parent, Vector2 position, string value, Color accent)
        {
            var shell = CreatePanel(parent, "Crystal chip", position, new Vector2(200f, 68f), Hex("#0a0f20"));
            AddOutline(shell.gameObject, new Color(accent.r, accent.g, accent.b, .40f), 1f);
            var signal = CreateImage(shell, "Flight telemetry keyline", new Vector2(0f, 30f), new Vector2(132f, 2f), accent);
            signal.raycastTarget = false;
            return CreateText(shell, value, Vector2.zero, new Vector2(180f, 48f), 23, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private Text CreateCrystalChip(Transform parent, Vector2 position, string value, Color accent)
        {
            var text = CreateChip(parent, position, value, accent);
            text.rectTransform.anchoredPosition = new Vector2(20f, 0f);
            text.rectTransform.sizeDelta = new Vector2(132f, 48f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 17;
            text.resizeTextMaxSize = 25;
            var icon = CreateImage(text.transform.parent, "Crystal balance icon", new Vector2(-62f, 0f), new Vector2(52f, 52f), Color.white);
            icon.sprite = LoadSprite(CrystalArtworkPath);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return text;
        }

        private Button CreateNeonButton(Transform parent, string label, Vector2 position, Vector2 size, Color accent)
        {
            var shell = CreatePanel(parent, "Button · " + label, position, size, Hex("#090e1e"));
            AddOutline(shell.gameObject, new Color(accent.r, accent.g, accent.b, .65f), 1f);
            var primaryAction = label == "PLAY" || label == "RETRY" || label == "FLY" || label == "RESUME";
            var fill = primaryAction ? Color.Lerp(accent, Color.white, .12f) : Color.Lerp(Hex("#0c172e"), accent, .055f);
            fill.a = 1f;
            var inner = CreatePanel(shell, "Button inner", Vector2.zero, size - new Vector2(8f, 8f), fill);
            var innerImage = inner.GetComponent<Image>();
            innerImage.raycastTarget = false;
            var energy = CreateImage(shell, "Flight control keyline", new Vector2(0f, size.y * .5f - 6f),
                new Vector2(Mathf.Max(20f, size.x - 48f), 1.5f), primaryAction ? new Color(1f, 1f, 1f, .65f) : new Color(accent.r, accent.g, accent.b, .35f));
            energy.raycastTarget = false;
            if (primaryAction && size.x >= 300f)
            {
                CreateUiGlyph(shell, "Launch wing insignia", new Vector2(-size.x * .5f + 56f, 0f),
                    new Vector2(45f, 36f), Hex("#07354e"), SkyPulseUiGlyph.Kind.WingMark);
                CreateUiGlyph(shell, "Launch vector", new Vector2(size.x * .5f - 56f, 0f),
                    new Vector2(40f, 18f), Hex("#07354e"), SkyPulseUiGlyph.Kind.Horizon);
            }
            var text = CreateText(shell, label, Vector2.zero, size - new Vector2(22f, 14f), primaryAction ? 34 : 22,
                primaryAction ? Hex("#03162d") : Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            if (primaryAction) text.GetComponent<Shadow>().enabled = false;
            text.raycastTarget = false;
            var button = shell.gameObject.AddComponent<Button>();
            button.targetGraphic = innerImage;
            shell.gameObject.AddComponent<SkyPulseButtonFeedback>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, .94f);
            colors.pressedColor = new Color(.86f, .86f, .98f, 1f);
            colors.fadeDuration = .06f;
            button.colors = colors;
            return button;
        }

        private static void AddOutline(GameObject target, Color color, float distance)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
        }
    }
}
