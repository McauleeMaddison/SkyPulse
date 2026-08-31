using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    /// <summary>Small, allocation-free press response for touch-first controls.</summary>
    public sealed class SkyPulseButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private RectTransform target;
        private Vector3 restingScale;
        private float pressAmount;

        private void Awake()
        {
            target = transform as RectTransform;
            restingScale = target != null ? target.localScale : Vector3.one;
        }

        private void OnEnable()
        {
            pressAmount = 0f;
            if (target != null) target.localScale = restingScale;
        }

        public void OnPointerDown(PointerEventData eventData) => pressAmount = 1f;
        public void OnPointerUp(PointerEventData eventData) => pressAmount = 0f;
        public void OnPointerExit(PointerEventData eventData) => pressAmount = 0f;

        private void Update()
        {
            if (target == null) return;
            var scale = Vector3.one * (1f - pressAmount * .045f);
            target.localScale = Vector3.Lerp(target.localScale, Vector3.Scale(restingScale, scale), 1f - Mathf.Exp(-Time.unscaledDeltaTime * 24f));
        }
    }

    /// <summary>
    /// Native, portrait-first SkyPulse presentation and flight loop.  This deliberately
    /// uses a small fixed pool of renderers: the game stays smooth on older phones while
    /// retaining the layered, neon look of the web beta.
    /// </summary>
    public sealed class SkyPulseNativeGame : MonoBehaviour
    {
        private enum FlightState { Menu, Playing, Impact, Paused, GameOver, Customize }
        // Classic is the score-first, leaderboard-ready route. Adventure deliberately
        // keeps the expressive upgrades and power-ups that make collection rewarding.
        // Daily shares Classic's fixed rules, plus a seeded obstacle sequence.
        private enum FlightMode { Classic, Adventure, Daily }
        private enum CosmeticCategory { Birds, Worlds, Pipes, Upgrades }
        // These are tactical pickup effects only.  The permanent economy is kept
        // deliberately separate so no purchase can change score potential or
        // flight handling.
        private enum PowerUpKind { Aegis, TimePulse, CrystalMagnet }
        private enum PendingPurchase { None, Skin, Upgrade }

        /// <summary>
        /// One place for all values that influence the way a flight feels. Keeping the
        /// values together makes a play-test change deliberate and keeps Classic and
        /// Daily perfectly comparable, regardless of cosmetic world selection.
        /// </summary>
        private sealed class FlightTuning
        {
            public readonly float Gravity;
            public readonly float FlapVelocity;
            public readonly float MaxFallVelocity;
            public readonly float StartingGap;
            public readonly float MinimumGap;
            public readonly float GapShrinkPerGate;
            public readonly float StartingScrollSpeed;
            public readonly float ScrollRampPerGate;
            public readonly float CollisionRadius;
            public readonly float PerfectPassWindow;
            public readonly float InputBufferSeconds;
            public readonly float MaximumGapCenterStep;
            public readonly int PowerUpSlots;
            public readonly float PowerUpRespawnMinimum;
            public readonly float PowerUpRespawnMaximum;
            public readonly bool AllowsUpgrades;
            public readonly bool AllowsPowerUps;

            public FlightTuning(
                float gravity, float flapVelocity, float maxFallVelocity,
                float startingGap, float minimumGap, float gapShrinkPerGate,
                float startingScrollSpeed, float scrollRampPerGate,
                float collisionRadius, float perfectPassWindow, float inputBufferSeconds,
                float maximumGapCenterStep,
                int powerUpSlots, float powerUpRespawnMinimum, float powerUpRespawnMaximum,
                bool allowsUpgrades, bool allowsPowerUps)
            {
                Gravity = gravity;
                FlapVelocity = flapVelocity;
                MaxFallVelocity = maxFallVelocity;
                StartingGap = startingGap;
                MinimumGap = minimumGap;
                GapShrinkPerGate = gapShrinkPerGate;
                StartingScrollSpeed = startingScrollSpeed;
                ScrollRampPerGate = scrollRampPerGate;
                CollisionRadius = collisionRadius;
                PerfectPassWindow = perfectPassWindow;
                InputBufferSeconds = inputBufferSeconds;
                MaximumGapCenterStep = maximumGapCenterStep;
                PowerUpSlots = powerUpSlots;
                PowerUpRespawnMinimum = powerUpRespawnMinimum;
                PowerUpRespawnMaximum = powerUpRespawnMaximum;
                AllowsUpgrades = allowsUpgrades;
                AllowsPowerUps = allowsPowerUps;
            }
        }

        private sealed class RigMotionProfile
        {
            public readonly float FarWingLift;
            public readonly float FarWingDownstroke;
            public readonly float UpperWingLift;
            public readonly float UpperWingDownstroke;
            public readonly float LowerWingLift;
            public readonly float LowerWingDownstroke;
            public readonly float FeatherFanLift;
            public readonly float FeatherFanDownstroke;
            public readonly float TailLift;
            public readonly float TailDownstroke;

            public RigMotionProfile(
                float farWingLift, float farWingDownstroke,
                float upperWingLift, float upperWingDownstroke,
                float lowerWingLift, float lowerWingDownstroke,
                float featherFanLift, float featherFanDownstroke,
                float tailLift, float tailDownstroke)
            {
                FarWingLift = farWingLift;
                FarWingDownstroke = farWingDownstroke;
                UpperWingLift = upperWingLift;
                UpperWingDownstroke = upperWingDownstroke;
                LowerWingLift = lowerWingLift;
                LowerWingDownstroke = lowerWingDownstroke;
                FeatherFanLift = featherFanLift;
                FeatherFanDownstroke = featherFanDownstroke;
                TailLift = tailLift;
                TailDownstroke = tailDownstroke;
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
            // These are distinct, per-bird poses. They must never point at a shared
            // bird image: its plumage, metalwork and silhouette are part of the
            // reward the player just earned.
            public string HitPath;
            public string UnlockPath;
            // A rig is six small transparent PNG layers sharing a canvas and
            // registration. Keeping its prefix on the skin lets every unlocked
            // bird use the same lightweight mobile animation code.
            public string RigResourcePrefix;
            public RigMotionProfile RigMotion;
            public Color Accent;
            public Color Trail;
            public int Price;

            public Skin(string id, string name, string artPath, string flapPath, string accent, string trail, int price, string risePath = null, string hitPath = null, string unlockPath = null, string[] flapFramePaths = null, string rigResourcePrefix = null, RigMotionProfile rigMotion = null)
            {
                Id = id;
                Name = name;
                ArtPath = artPath;
                FlapPath = flapPath;
                RisePath = risePath;
                HitPath = hitPath;
                UnlockPath = unlockPath;
                FlapFramePaths = flapFramePaths;
                RigResourcePrefix = rigResourcePrefix;
                RigMotion = rigMotion;
                Accent = Hex(accent);
                Trail = Hex(trail);
                Price = price;
            }
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

            public Upgrade(string id, string name, string[] levelEffects, int[] levelPrices, string accent)
            {
                Id = id;
                Name = name;
                LevelEffects = levelEffects;
                LevelPrices = levelPrices;
                Accent = Hex(accent);
            }

            public int MaxLevel => Mathf.Min(LevelEffects == null ? 0 : LevelEffects.Length, LevelPrices == null ? 0 : LevelPrices.Length);

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
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

        private const float CameraHeight = 18f;
        private const float PortraitPlayfieldAspect = 9f / 16f;
        private const float GroundY = -8.45f;
        private const float BirdX = -2.45f;
        // One body-only gameplay hitbox shared by all birds. Its capsule excludes
        // wing tips, tail, beak, glow, thrust and the cosmetic trail.
        private const float BirdHitboxWidth = .98f;
        private const float BirdHitboxHeight = .76f;
        private const float BirdHitboxOffsetX = .20f;
        private const float BirdHitboxOffsetY = -.05f;
        private const float BirdHitboxRadius = BirdHitboxHeight * .5f;
        // Pickups remain deliberately generous around the illustrated bird; this is
        // separate from the smaller physical collision capsule.
        private const float BirdPickupRadius = .81f;
        // Cosmetic propulsion is deliberately independent of the bird's body size.
        private const float BirdThrustAnchorX = -.55f;
        private const float BirdThrustAnchorY = -.24f;
        private const float BirdThrustCoreLength = .42f;
        private const float BirdThrustGlowLength = .78f;
        private const float BirdThrustPulseLength = .22f;
        private const float BirdThrustCoreHeight = .095f;
        private const float BirdThrustGlowHeight = .28f;
        // The bird is the primary focal point, so it must remain readable against a
        // busy world at a real phone scale—not shrink into a sparkle at the centre.
        private const float BirdDisplayWidth = 2.30f;
        // Pipe tuning is deliberately centralised: the body can stretch only along
        // its length, while the cap keeps the authored aspect ratio at this width.
        private const float PipeWidth = 1.72f;
        private const float PipeCapWidth = PipeWidth + .34f;
        private const float PipeCollisionWidth = PipeCapWidth;
        private const float PipeFallbackCapHeight = .62f;
        private const float TopPipeOverscan = .68f;
        private const float BottomPipeFloorOverlap = .10f;
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
        private const int PowerUpCount = 1;
        private const float PickupRadius = .43f;
        private const float CrystalPickupRadius = .34f;
        private const float CrystalPickupRespawnMinimum = 8.5f;
        private const float CrystalPickupRespawnMaximum = 12.5f;
        private const float InputLockoutSeconds = .07f;
        private const float WorldTransitionSeconds = 1.2f;
        private const float WorldRecoverySeconds = .9f;
        private const float AegisImmunitySeconds = .6f;
        private const float AegisHitStopSeconds = .07f;
        private const float ImpactFreezeSeconds = .07f;
        private const float ImpactTumbleSeconds = .70f;
        private const float SimulationStep = 1f / 120f;
        private const float MaximumSimulationCatchup = 1f / 12f;
        // Full-body Aetherwing drawings stay stepped while the layered wing rig is
        // being authored. Once all six rig pieces are present, only the wings and
        // tail move; the body remains a single sharp drawing at every point in the
        // flap. This avoids the blurry double-bird effect on a phone.
        // Six authored flight poses over .30 s display at 20 fps, inside the
        // requested 18–24 fps animation range while transforms stay smooth at render rate.
        private const float WingCycleSeconds = .30f;
        private const float WingLiftPhase = .31f;
        private const float WingDownstrokeDelay = .075f;
        private const float WingDownstrokeSpan = .90f;
        private const float WingLiftPoseThreshold = .32f;
        private const float WingDownstrokePoseThreshold = .14f;
        private const float ImpactFrameSeconds = .26f;
        private const int LaunchBirdCount = 5;
        // Version 4 is a matched temporary flight set: tucked glide, raised upstroke
        // and decisive downstroke. The buy reward is intentionally separate—each
        // collectible bird has its own hit and open-wing unlock artwork rather than
        // borrowing another bird's silhouette.
        private const string AetherwingGlidePath = "SkyPulse/characters/aetherwing_v2/aetherwing-glide-v4";
        private const string AetherwingFlapPath = "SkyPulse/characters/aetherwing_v2/aetherwing-downstroke-v4";
        private const string AetherwingRisePath = "SkyPulse/characters/aetherwing_v2/aetherwing-lift-v4";
        private const string AetherwingHeroPath = "SkyPulse/characters/aetherwing_v2/aetherwing-hero-v4";
        // All six files use the same transparent 2048 x 1536 canvas and the same
        // registration. Do not put a background in any of them. The rig switches on
        // only when this complete authored set exists; until then the current, proven
        // full-body Aetherwing animation is kept as the safe fallback.
        private const string AetherwingRigResourcePrefix = "SkyPulse/characters/aetherwing_rig/aetherwing";
        // The original mechanical line art is deliberately kept as a labelled test
        // bird. It lets us inspect a six-cell flap in real game conditions before
        // committing the final coloured, layered rig art.
        private const string TestAetherwingFlap01Path = "SkyPulse/characters/aetherwing_test/aetherwing-test-flap-01-v1";
        private const string TestAetherwingFlap02Path = "SkyPulse/characters/aetherwing_test/aetherwing-test-flap-02-v1";
        private const string TestAetherwingFlap03Path = "SkyPulse/characters/aetherwing_test/aetherwing-test-flap-03-v1";
        private const string TestAetherwingFlap04Path = "SkyPulse/characters/aetherwing_test/aetherwing-test-flap-04-v1";
        private const string TestAetherwingFlap05Path = "SkyPulse/characters/aetherwing_test/aetherwing-test-flap-05-v1";
        private const string TestAetherwingFlap06Path = "SkyPulse/characters/aetherwing_test/aetherwing-test-flap-06-v1";
        private const string TestAetherwingHitPath = "SkyPulse/characters/aetherwing_test/aetherwing-test-hit-v1";
        private const string TestAetherwingUnlockPath = "SkyPulse/characters/aetherwing_test/aetherwing-test-unlock-v1";

        private static readonly RigMotionProfile AetherwingRigMotion = new RigMotionProfile(
            farWingLift: -20f, farWingDownstroke: 13f,
            upperWingLift: -27f, upperWingDownstroke: 18f,
            lowerWingLift: -15f, lowerWingDownstroke: 12f,
            featherFanLift: -9f, featherFanDownstroke: 16f,
            tailLift: 4f, tailDownstroke: -3f);

        // These profiles are deliberately conservative. A play-test should alter one
        // value here at a time, never spread physics magic numbers through the loop.
        private static readonly FlightTuning ClassicTuning = new FlightTuning(
            gravity: -18.2f, flapVelocity: 6.25f, maxFallVelocity: -11.2f,
            startingGap: 4.46f, minimumGap: 3.88f, gapShrinkPerGate: .022f,
            startingScrollSpeed: 4.38f, scrollRampPerGate: .038f,
            collisionRadius: .255f, perfectPassWindow: .34f, inputBufferSeconds: .095f, maximumGapCenterStep: .70f,
            powerUpSlots: 0, powerUpRespawnMinimum: 0f, powerUpRespawnMaximum: 0f,
            allowsUpgrades: false, allowsPowerUps: false);

        private static readonly FlightTuning AdventureTuning = new FlightTuning(
            gravity: -18.2f, flapVelocity: 6.25f, maxFallVelocity: -11.2f,
            startingGap: 4.46f, minimumGap: 3.42f, gapShrinkPerGate: .030f,
            startingScrollSpeed: 4.30f, scrollRampPerGate: .045f,
            collisionRadius: BirdPickupRadius, perfectPassWindow: .32f, inputBufferSeconds: .095f, maximumGapCenterStep: .90f,
            powerUpSlots: 1, powerUpRespawnMinimum: 7.5f, powerUpRespawnMaximum: 10.5f,
            allowsUpgrades: true, allowsPowerUps: true);

        // One route, one handling model. Values are expressed against the 15.82-unit
        // flight corridor above the lower hazard: ~2.2 corridor-heights/s² gravity,
        // .72 heights/s flap lift, and .95 heights/s terminal fall. Bird choice
        // never changes it.
        private static readonly FlightTuning EndlessTuning = new FlightTuning(
            gravity: -34.8f, flapVelocity: 11.4f, maxFallVelocity: -15.05f,
            startingGap: 5.38f, minimumGap: 3.96f, gapShrinkPerGate: 0f,
            startingScrollSpeed: 3.24f, scrollRampPerGate: 0f,
            collisionRadius: BirdPickupRadius, perfectPassWindow: .34f, inputBufferSeconds: .07f, maximumGapCenterStep: 3.16f,
            powerUpSlots: 1, powerUpRespawnMinimum: 0f, powerUpRespawnMaximum: 0f,
            allowsUpgrades: false, allowsPowerUps: true);

        // The launch hangar has one free cyber-bird and four crystal unlocks. Their
        // art and accent vary, but their shared collision and flight tuning preserve
        // a single fair score route. Future birds belong here as data-only additions.
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
        };

        private static readonly WorldTheme[] Worlds =
 {
new WorldTheme(
"neon_city",
"NEON CITY",
"SkyPulse/backgrounds/neon-flightdeck-v1",
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
"SkyPulse/backgrounds/themes/aurora-rise-v2",
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
"SkyPulse/backgrounds/themes/solar-drift-v2",
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
"SkyPulse/backgrounds/themes/velvet-dawn-v3",
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
            new Upgrade("crystal_resonator", "CRYSTAL RESONATOR", new []
            {
                "LEVEL 1 · ATTRACT CRYSTALS WITHIN 6% OF SCREEN WIDTH",
                "LEVEL 2 · ATTRACT CRYSTALS WITHIN 10% OF SCREEN WIDTH",
                "LEVEL 3 · ATTRACT CRYSTALS WITHIN 14% OF SCREEN WIDTH",
            }, new [] { 150, 400, 900 }, "#45eaff"),
            new Upgrade("salvage_codec", "SALVAGE CODEC", new []
            {
                "LEVEL 1 · RESULTS AWARD +10% CRYSTALS",
                "LEVEL 2 · RESULTS AWARD +20% CRYSTALS",
                "LEVEL 3 · RESULTS AWARD +30% CRYSTALS",
            }, new [] { 200, 500, 1000 }, "#ffc34d"),
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
        private readonly PowerUpPickup[] powerUpPool = new PowerUpPickup[PowerUpCount];
        private readonly Vector3[] trailPoints = new Vector3[9];
        private readonly List<AmbientStar> ambientStars = new List<AmbientStar>();
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
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
        private Sprite pipeBodySprite;
        private Sprite pipeCapSprite;
        private Sprite pipeGlowSprite;
        private bool hasAuthoredPipeBody;
        private bool hasAuthoredPipeCap;
        private bool hasAuthoredPipeGlow;
        private float pipeCapHeight = PipeFallbackCapHeight;
        private float PipeCapHeight => pipeCapHeight;
        private Sprite emergencyBirdSprite;
        private Sprite idleBirdSprite;
        private Sprite flapBirdSprite;
        private Sprite riseBirdSprite;
        private Sprite hitBirdSprite;
        private Sprite[] flapFrameBirdSprites;
        private int activeFlapFrameIndex;
        private Vector3 flapFrameBaseScale = Vector3.one;
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
        private Transform aetherwingRig;
        private Transform aetherwingFarWingJoint;
        private Transform aetherwingUpperWingJoint;
        private Transform aetherwingLowerWingJoint;
        private Transform aetherwingFeatherFanJoint;
        private Transform aetherwingTailJoint;
        private SpriteRenderer birdRenderer;
        private SpriteRenderer birdSafetyRenderer;
        private SpriteRenderer birdFlapRenderer;
        private SpriteRenderer birdRiseRenderer;
        private SpriteRenderer birdParallaxRenderer;
        private SpriteRenderer birdDepthRenderer;
        private SpriteRenderer birdEyeGlintRenderer;
        private CapsuleCollider2D birdBodyCollider;
        private Transform birdThrust;
        private SpriteRenderer birdThrustGlowRenderer;
        private SpriteRenderer birdThrustCoreRenderer;
        private Color birdThrustGlowColour;
        private Color birdThrustCoreColour;
        private SpriteRenderer aetherwingBodyRenderer;
        private SpriteRenderer aetherwingFarWingRenderer;
        private SpriteRenderer aetherwingUpperWingRenderer;
        private SpriteRenderer aetherwingLowerWingRenderer;
        private SpriteRenderer aetherwingFeatherFanRenderer;
        private SpriteRenderer aetherwingTailRenderer;
        private SpriteRenderer shieldAuraRenderer;
        private SpriteRenderer slowAuraRenderer;
        private SpriteRenderer effectAuraRenderer;
        private LineRenderer trailGlow;
        private LineRenderer trailCore;
        private LineRenderer trailSafety;
        private AudioSource audioSource;
        private AudioClip flapSound;
        private AudioClip scoreSound;
        private AudioClip crashSound;
        private AudioClip crystalSound;
        private AudioClip unlockSound;
        private Font uiFont;
        private Vector3 aetherwingRigBaseScale = Vector3.one;
        private bool aetherwingRigReady;

        private GameObject uiRoot;
        private RectTransform safeAreaRoot;
        private Rect appliedSafeArea;
        private Vector2Int appliedScreenSize;
        private float appliedViewportWidth = -1f;
        private GameObject homeScreen;
        private GameObject hudScreen;
        private GameObject pauseScreen;
        private GameObject gameOverScreen;
        private GameObject customizeScreen;
        private GameObject purchaseModal;
        private GameObject unlockRevealModal;
        private Text menuCrystalText;
        private Text customizeCrystalText;
        private Text menuBestText;
        private Text menuEquippedText;
        private Text difficultyText;
        private Text menuModeDetailText;
        private Text menuDailyText;
        private Text hudScoreText;
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
        private Text purchaseTitleText;
        private Text purchaseDetailText;
        private Text purchaseBalanceText;
        private Text purchaseConfirmText;
        private Text reduceMotionText;
        private Text hapticsText;
        private Image purchasePreviewImage;
        private Image purchaseHalo;
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
        private int adventureBest;
        private int dailyBest;
        private int crystals;
        private int runCrystalsCollected;
        private int runCrystalBonus;
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
        private FlightMode selectedFlightMode = FlightMode.Classic;
        private FlightMode flightMode = FlightMode.Classic;
        private System.Random dailyRouteRandom;
        private string activeDailyRouteKey = string.Empty;
        private float birdY;
        private float birdVelocity;
        private float birdTilt;
        private float birdTiltVelocity;
        private float wingTimer;
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
        private float skySurgeTimer;
        private float scorePrismTimer;
        private float magnetHaloTimer;
        private float phaseShiftTimer;
        private float shieldImmunityTimer;
        private float shieldHitStopTimer;
        private float worldTransitionTimer;
        private float worldRecoveryTimer;
        private float lastFlapInputTime = -100f;
        private bool firstGateAfterTransition;
        private float simulationAccumulator;
        private float bufferedFlapUntil = -1f;
        private float flightFeedbackTimer;
        private string lastCrashReason = "GATE IMPACT";
#if !UNITY_EDITOR
        private float hapticCooldownUntil;
#endif
        private int shieldCharges;
        private int rescueCharges;
        private int gatesSinceStarheart;
        private int perfectPasses;
        private int displayedSlowTenths = -1;
        private int displayedPowerUpCode = -1;
        private bool newBest;
        private Color flightFeedbackColour;
        private SpriteRenderer flightFeedbackRenderer;
        private Vector3 idleBirdBaseScale = Vector3.one;
        private Vector3 safetyBirdBaseScale = Vector3.one;
        private Vector3 parallaxBirdBaseScale = Vector3.one;
        private Vector3 flapBirdBaseScale = Vector3.one;
        private Vector3 riseBirdBaseScale = Vector3.one;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool collisionDebugEnabled;
        private SpriteRenderer collisionBirdDebug;

        // These caps exercise the exact same fixed-step route at the refresh rates
        // we need to validate before a real-device session. They never alter the
        // flight tuning itself, which keeps a comparison meaningful.
        private int developmentFrameRateCap = 60;
#endif

#if UNITY_EDITOR
        private Text editorQualityText;
        private float editorFrameSampleTime;
        private int editorFrameSampleCount;
        private float editorDisplayedFps;
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

            backgroundRenderer = CreateRenderer("Cinematic world", WorldBackdrop(equippedWorld), Color.white, -40);
            backgroundRenderer.transform.position = new Vector3(0f, .12f, 0f);
            FitBackgroundToCamera(backgroundRenderer, .5f);

            backgroundVeil = CreateRenderer("World colour veil", whiteSprite, new Color(.015f, .01f, .08f, .20f), -39);
            backgroundVeil.transform.position = new Vector3(0f, .1f, 0f);
            backgroundVeil.transform.localScale = new Vector3(GetViewportWidth() + 1f, CameraHeight + .5f, 1f);

            CreateAmbientStars();
            CreateFloor();
            CreateBird();
            CreateFlightFeedback();
            CreateTrail();
            for (var index = 0; index < pipePool.Length; index += 1) pipePool[index] = CreatePipePair(index);
            for (var index = 0; index < crystalPickupPool.Length; index += 1) crystalPickupPool[index] = CreateCrystalPickup(index);
            for (var index = 0; index < powerUpPool.Length; index += 1) powerUpPool[index] = CreatePowerUp(index);

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
            // The backdrop already carries the detail. These are only a whisper of
            // parallax depth, never the large square particles of the old treatment.
            for (var index = 0; index < 8; index += 1)
            {
                var star = CreateRenderer($"Ambient light {index + 1}", softCircleSprite, new Color(.60f, .84f, 1f, .14f), -33);
                var viewportFraction = Mathf.Lerp(-.48f, .48f, (float)random.NextDouble());
                var x = GetViewportWidth() * viewportFraction;
                var y = Mathf.Lerp(-5.8f, 8.3f, (float)random.NextDouble());
                var size = Mathf.Lerp(.016f, .034f, (float)random.NextDouble());
                star.transform.position = new Vector3(x, y, 0f);
                star.transform.localScale = Vector3.one * size;
                ambientStars.Add(new AmbientStar
                {
                    Transform = star.transform,
                    X = x,
                    ViewportFraction = viewportFraction,
                    Y = y,
                    Phase = (float)random.NextDouble() * Mathf.PI * 2f,
                    Speed = Mathf.Lerp(.16f, .32f, (float)random.NextDouble()),
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
        }
        private void CreateBird()
        {
            bird = new GameObject("Flight bird").transform;
            bird.SetParent(transform, false);
            CreateBirdHitbox();
            CreateBirdThrust();
            // This compact, opaque inner silhouette is deliberately independent of
            // imported artwork. It gives every bird a readable body against bright
            // worlds and makes a missing/unsupported texture impossible to turn the
            // player avatar invisible on a phone.
            if (emergencyBirdSprite == null) emergencyBirdSprite = LoadSprite(AetherwingHeroPath) ?? CreateEmergencyBirdSprite();
            var slowAura = CreateRenderer("Slow field aura", ringSprite, new Color(.45f, .3f, 1f, 0f), 12, bird);
            slowAura.transform.localScale = Vector3.one * 1.42f;
            slowAuraRenderer = slowAura;
            var effectAura = CreateRenderer("Active power aura", softCircleSprite, new Color(.45f, .9f, 1f, 0f), 12, bird);
            effectAura.transform.localScale = Vector3.one * 1.16f;
            effectAuraRenderer = effectAura;
            var shieldAura = CreateRenderer("Pulse shield aura", ringSprite, new Color(.38f, 1f, .70f, 0f), 13, bird);
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
            CreateAetherwingRig();
            var eyeGlint = CreateRenderer("Bird living eye glint", softCircleSprite, new Color(1f, 1f, 1f, 0f), 16, bird);
            eyeGlint.transform.localScale = Vector3.one * .062f;
            birdEyeGlintRenderer = eyeGlint;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
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

        private void CreateBirdThrust()
        {
            // The existing neon trail remains the long propulsion ribbon. These two
            // small layers give it a living engine root without adding any collider.
            birdThrust = new GameObject("Rear propulsion thrust").transform;
            birdThrust.SetParent(bird, false);
            birdThrust.localPosition = new Vector3(BirdThrustAnchorX, BirdThrustAnchorY, 0f);
            birdThrustGlowRenderer = CreateRenderer("Rear thrust glow", softCircleSprite, Color.clear, 11, birdThrust);
            birdThrustCoreRenderer = CreateRenderer("Rear thrust core", softCircleSprite, Color.clear, 13, birdThrust);
            birdThrustGlowRenderer.enabled = false;
            birdThrustCoreRenderer.enabled = false;
        }

        private void CreateAetherwingRig()
        {
            aetherwingRig = new GameObject("Aetherwing 2D wing rig").transform;
            aetherwingRig.SetParent(bird, false);
            aetherwingRig.gameObject.SetActive(false);

            // The roots are deliberately kept in one small, readable place. After
            // the first art export, a single play-test lets us nudge these values if
            // the drawn shoulder or tail hinge needs a pixel-perfect adjustment.
            aetherwingFarWingJoint = CreateAetherwingRigJoint("Far wing root", new Vector3(-.07f, .05f, 0f));
            aetherwingUpperWingJoint = CreateAetherwingRigJoint("Upper wing root", new Vector3(-.05f, .04f, 0f));
            aetherwingLowerWingJoint = CreateAetherwingRigJoint("Lower wing hinge", new Vector3(-.17f, .00f, 0f));
            aetherwingFeatherFanJoint = CreateAetherwingRigJoint("Primary feather root", new Vector3(-.30f, -.08f, 0f));
            aetherwingTailJoint = CreateAetherwingRigJoint("Tail root", new Vector3(-.43f, -.13f, 0f));

            aetherwingFarWingRenderer = CreateAetherwingRigLayer("Aetherwing far wing", 13, aetherwingFarWingJoint);
            aetherwingTailRenderer = CreateAetherwingRigLayer("Aetherwing tail", 13, aetherwingTailJoint);
            aetherwingBodyRenderer = CreateAetherwingRigLayer("Aetherwing body", 14, aetherwingRig);
            aetherwingLowerWingRenderer = CreateAetherwingRigLayer("Aetherwing lower wing", 15, aetherwingLowerWingJoint);
            aetherwingUpperWingRenderer = CreateAetherwingRigLayer("Aetherwing upper wing", 16, aetherwingUpperWingJoint);
            aetherwingFeatherFanRenderer = CreateAetherwingRigLayer("Aetherwing primary feather fan", 17, aetherwingFeatherFanJoint);
        }

        private Transform CreateAetherwingRigJoint(string name, Vector3 position)
        {
            var joint = new GameObject(name).transform;
            joint.SetParent(aetherwingRig, false);
            joint.localPosition = position;
            return joint;
        }

        private SpriteRenderer CreateAetherwingRigLayer(string name, int sortingOrder, Transform parent)
        {
            var layer = new GameObject(name).transform;
            layer.SetParent(parent, false);
            // Each export remains registered to the body canvas. Moving the child by
            // the inverse root makes its unanimated position line up exactly, while
            // its parent gives us a clean, natural rotation hinge.
            if (parent != aetherwingRig) layer.localPosition = -parent.localPosition;
            var renderer = layer.gameObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = false;
            return renderer;
        }

        private void CreateFlightFeedback()
        {
            flightFeedbackRenderer = CreateRenderer("Flight feedback bloom", softCircleSprite, new Color(1f, 1f, 1f, 0f), 30);
            flightFeedbackRenderer.enabled = false;
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

        private void CreateTrail()
        {
            // The safety core is intentionally thin and restrained. It is the visual
            // guarantee beneath every cosmetic trail, never a second noisy effect.
            trailSafety = CreateTrailRenderer("Trail visibility core", 10, .048f, .010f);
            trailGlow = CreateTrailRenderer("Trail glow", 11, .19f, .035f);
            trailCore = CreateTrailRenderer("Trail core", 12, .082f, .012f);
        }

        private LineRenderer CreateTrailRenderer(string name, int sortingOrder, float startWidth, float endWidth)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(transform, false);
            var renderer = holder.AddComponent<LineRenderer>();
            renderer.useWorldSpace = true;
            renderer.positionCount = 0;
            renderer.sortingOrder = sortingOrder;
            renderer.startWidth = startWidth;
            renderer.endWidth = endWidth;
            renderer.numCapVertices = 4;
            renderer.numCornerVertices = 2;
            ConfigureLineMaterial(renderer);
            return renderer;
        }

        private static void ConfigureLineMaterial(LineRenderer renderer)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) renderer.material = new Material(shader);
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // This is a portrait game. Matching height keeps the whole menu visible
            // in Unity's Free Aspect preview as well as on a phone.
            scaler.matchWidthOrHeight = 1f;

            var safeRoot = new GameObject("Safe area", typeof(RectTransform));
            safeRoot.transform.SetParent(uiRoot.transform, false);
            safeAreaRoot = safeRoot.GetComponent<RectTransform>();
            ApplySafeArea();

            if (EventSystem.current == null)
            {
                var eventSystem = new GameObject("SkyPulse input", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(eventSystem);
            }

            homeScreen = CreateHomeScreen(safeAreaRoot);
            hudScreen = CreateHud(safeAreaRoot);
            pauseScreen = CreatePauseScreen(safeAreaRoot);
            gameOverScreen = CreateGameOverScreen(safeAreaRoot);
            customizeScreen = CreateCustomizeScreen(safeAreaRoot);
            purchaseModal = CreatePurchaseModal(safeAreaRoot);
            purchaseModal.SetActive(false);
            unlockRevealModal = CreateUnlockReveal(safeAreaRoot);
            unlockRevealModal.SetActive(false);

#if UNITY_EDITOR
            CreateEditorQualityHarness(safeAreaRoot);
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only mobile QA readout. It makes frame-rate and collision checks
        /// visible in the Game view without adding a single element to a player build.
        /// F1/F2/F3 set a 30/60/120 FPS render cap; F4 shows the collision volumes.
        /// </summary>
        private void CreateEditorQualityHarness(Transform parent)
        {
            var bar = CreatePanel(parent, "Editor mobile quality harness", new Vector2(0f, -856f), new Vector2(796f, 44f), new Color(.008f, .014f, .04f, .78f));
            var image = bar.GetComponent<Image>();
            image.raycastTarget = false;
            AddOutline(bar.gameObject, new Color(.27f, .86f, 1f, .23f), .75f);
            editorQualityText = CreateText(bar, "EDITOR QA  ·  STARTING…", Vector2.zero, new Vector2(760f, 34f), 13, new Color(.75f, .92f, 1f, .82f), TextAnchor.MiddleCenter, FontStyle.Bold);
            editorQualityText.raycastTarget = false;
            UpdateEditorQualityHarness(0f);
        }
#endif

        private void ApplySafeArea()
        {
            if (safeAreaRoot == null) return;
            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (safeArea == appliedSafeArea && screenSize == appliedScreenSize) return;

            appliedSafeArea = safeArea;
            appliedScreenSize = screenSize;
            if (screenSize.x <= 0 || screenSize.y <= 0) return;
            safeAreaRoot.anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            safeAreaRoot.anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
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
            if (backgroundRenderer != null) FitBackgroundToCamera(backgroundRenderer, .5f);
            if (backgroundVeil != null) backgroundVeil.transform.localScale = new Vector3(viewportWidth + 1f, CameraHeight + .5f, 1f);

            var floorWidth = viewportWidth + 1f;
            if (floorBase != null) floorBase.transform.localScale = new Vector3(floorWidth, 2.24f, 1f);
            if (floorSurface != null) floorSurface.transform.localScale = new Vector3(floorWidth, 1.90f, 1f);
            if (floorLip != null) floorLip.transform.localScale = new Vector3(floorWidth, .12f, 1f);
            if (floorGlow != null) floorGlow.transform.localScale = new Vector3(floorWidth, .026f, 1f);
            if (floorHighlight != null) floorHighlight.transform.localScale = new Vector3(floorWidth, .010f, 1f);
            foreach (var star in ambientStars)
            {
                star.X = viewportWidth * star.ViewportFraction;
            }
        }

        private GameObject CreateHomeScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Home screen");
            // The home screen is a clear flight deck, not a frosted layer over the
            // world. Keep the world visible, then give controls a solid place to sit.
            CreateFullPanel(root.transform, "Home contrast veil", new Color(.005f, .012f, .05f, .10f));

            difficultyText = CreateChip(root.transform, new Vector2(-355f, 790f), "ENDLESS ROUTE", Hex("#8f64ff"));
            difficultyText.resizeTextForBestFit = true;
            difficultyText.resizeTextMinSize = 13;
            difficultyText.resizeTextMaxSize = 20;
            menuCrystalText = CreateChip(root.transform, new Vector2(355f, 790f), "✦  0", Hex("#45eaff"));

            menuTitleText = CreateText(root.transform, "SKYPULSE", new Vector2(0f, 622f), new Vector2(900f, 112f), 78, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            AddOutline(menuTitleText.gameObject, new Color(.22f, .86f, 1f, .62f), 1.25f);
            CreateText(root.transform, "FLAP  ·  FLOW  ·  FLY", new Vector2(0f, 548f), new Vector2(700f, 36f), 20, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var titleRule = CreateImage(root.transform, "Title energy rule", new Vector2(0f, 510f), new Vector2(180f, 2f), new Color(.25f, .91f, 1f, .62f));
            titleRule.sprite = softCircleSprite;
            titleRule.raycastTarget = false;

            var flightDeck = CreatePanel(root.transform, "Flight deck", new Vector2(0f, -392f), new Vector2(770f, 508f), new Color(.018f, .030f, .078f, .94f));
            AddOutline(flightDeck.gameObject, new Color(.27f, .86f, 1f, .28f), 1f);
            var deckRule = CreateImage(flightDeck, "Flight deck rule", new Vector2(0f, 202f), new Vector2(618f, 1.5f), new Color(.27f, .86f, 1f, .36f));
            deckRule.sprite = whiteSprite;
            deckRule.raycastTarget = false;

            var heroObject = new GameObject("Animated menu hero", typeof(RectTransform));
            heroObject.transform.SetParent(root.transform, false);
            menuHeroTransform = heroObject.GetComponent<RectTransform>();
            menuHeroTransform.anchorMin = new Vector2(.5f, .5f);
            menuHeroTransform.anchorMax = new Vector2(.5f, .5f);
            menuHeroTransform.pivot = new Vector2(.5f, .5f);
            menuHeroTransform.anchoredPosition = new Vector2(0f, 164f);
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

            menuBestText = CreateChip(root.transform, new Vector2(0f, -160f), "BEST · 0", Hex("#8fa7c4"));
            menuModeDetailText = CreateText(root.transform, "ONE FAIR ROUTE · COLLECT CRYSTALS · MASTER THE FLOW", new Vector2(0f, -211f), new Vector2(780f, 32f), 16, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var fly = CreateNeonButton(root.transform, "PLAY", new Vector2(0f, -292f), new Vector2(592f, 108f), Hex("#f05bc6"));
            fly.onClick.AddListener(StartFlight);
            CreateText(root.transform, "TAP ANYWHERE TO FLAP", new Vector2(0f, -370f), new Vector2(650f, 34f), 15, new Color(.91f, .92f, 1f, .68f), TextAnchor.MiddleCenter, FontStyle.Bold);

            var hangar = CreateNeonButton(root.transform, "BIRD HANGAR", new Vector2(-154f, -456f), new Vector2(284f, 78f), Hex("#45eaff"));
            hangar.onClick.AddListener(OpenHangar);
            var upgrades = CreateNeonButton(root.transform, "UPGRADES", new Vector2(154f, -456f), new Vector2(284f, 78f), Hex("#ffc34d"));
            upgrades.onClick.AddListener(OpenUpgrades);
            menuDailyText = CreateText(root.transform, "NEON CITY  →  ACID FOUNDRY  →  ORBITAL BAZAAR", new Vector2(0f, -538f), new Vector2(760f, 40f), 17, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            menuEquippedText = CreateText(root.transform, "SELECTED  ·  NEON FINCH", new Vector2(0f, -600f), new Vector2(650f, 36f), 16, Hex("#8f64ff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            return root;
        }

        private GameObject CreateHud(Transform parent)
        {
            var root = CreateScreen(parent, "Flight HUD");
            var horizonRule = CreateImage(root.transform, "Flight HUD energy rail", new Vector2(0f, 756f), new Vector2(810f, 1.5f), new Color(.27f, .86f, 1f, .34f));
            horizonRule.sprite = whiteSprite;
            horizonRule.raycastTarget = false;
            var pause = CreateNeonButton(root.transform, "Ⅱ", new Vector2(-425f, 804f), new Vector2(82f, 70f), Hex("#8f64ff"));
            pause.onClick.AddListener(PauseFlight);
            hudCrystalText = CreateChip(root.transform, new Vector2(365f, 804f), "✦  0", Hex("#45eaff"));
            hudScoreText = CreateText(root.transform, "0", new Vector2(0f, 708f), new Vector2(260f, 120f), 76, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            AddOutline(hudScoreText.gameObject, new Color(.27f, .86f, 1f, .23f), 1f);
            hudModeText = CreateText(root.transform, "NEON CITY", new Vector2(0f, 652f), new Vector2(600f, 30f), 16, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            scoreBurstText = CreateText(root.transform, "+1", new Vector2(0f, 612f), new Vector2(220f, 70f), 34, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            scoreBurstText.gameObject.SetActive(false);
            // Keep the active tactical effect beside the crystal bank; it stays out
            // of the flight corridor and leaves score as the largest top-centre cue.
            hudPowerUpText = CreateText(root.transform, "", new Vector2(286f, 730f), new Vector2(320f, 34f), 17, Hex("#61f5b3"), TextAnchor.MiddleRight, FontStyle.Bold);
            hudPowerUpText.gameObject.SetActive(false);
            hudCoachText = CreateText(root.transform, "", new Vector2(0f, 522f), new Vector2(760f, 34f), 16, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            hudCoachText.gameObject.SetActive(false);
            return root;
        }

        private GameObject CreatePauseScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Pause screen");
            CreateFullPanel(root.transform, "Pause dim", new Color(.015f, .008f, .06f, .72f));
            var card = CreatePanel(root.transform, "Pause card", new Vector2(0f, 20f), new Vector2(760f, 590f), Hex("#11132a"));
            AddOutline(card.gameObject, Hex("#8f64ff"), 3f);
            CreateText(card, "PAUSED", new Vector2(0f, 190f), new Vector2(650f, 80f), 52, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var resume = CreateNeonButton(card, "RESUME", new Vector2(0f, 78f), new Vector2(500f, 82f), Hex("#45eaff"));
            resume.onClick.AddListener(ResumeFlight);
            CreateText(card, "COMFORT", new Vector2(0f, -2f), new Vector2(500f, 28f), 15, new Color(.85f, .91f, 1f, .58f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var reduceMotion = CreateNeonButton(card, "", new Vector2(-158f, -68f), new Vector2(292f, 64f), Hex("#61f5b3"));
            reduceMotionText = reduceMotion.GetComponentInChildren<Text>();
            reduceMotion.onClick.AddListener(ToggleReduceMotion);
            var haptics = CreateNeonButton(card, "", new Vector2(158f, -68f), new Vector2(292f, 64f), Hex("#b17cff"));
            hapticsText = haptics.GetComponentInChildren<Text>();
            haptics.onClick.AddListener(ToggleHaptics);
            var menu = CreateNeonButton(card, "MENU", new Vector2(0f, -176f), new Vector2(500f, 72f), Hex("#8f64ff"));
            menu.onClick.AddListener(ResetToMenu);
            return root;
        }

        private GameObject CreateGameOverScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Game over screen");
            CreateFullPanel(root.transform, "Game over dim", new Color(.012f, .006f, .05f, .78f));
            var card = CreatePanel(root.transform, "Game over card", new Vector2(0f, 16f), new Vector2(820f, 960f), Hex("#11132a"));
            AddOutline(card.gameObject, Hex("#8f64ff"), 3.5f);
            CreateText(card, "RUN COMPLETE", new Vector2(0f, 390f), new Vector2(720f, 78f), 48, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultNewBestText = CreateText(card, "NEW BEST", new Vector2(0f, 330f), new Vector2(500f, 42f), 25, Hex("#ffc34d"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultReasonText = CreateText(card, "GATE IMPACT", new Vector2(0f, 286f), new Vector2(600f, 30f), 16, Hex("#f05bc6"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultModeText = CreateText(card, "ENDLESS CYBER ROUTE", new Vector2(0f, 248f), new Vector2(600f, 30f), 16, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultScoreText = CreateText(card, "SCORE  0", new Vector2(0f, 190f), new Vector2(600f, 54f), 31, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultBestText = CreateText(card, "BEST  0", new Vector2(0f, 143f), new Vector2(600f, 42f), 22, new Color(.93f, .95f, 1f, .78f), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultCrystalsText = CreateText(card, "CRYSTALS PICKED UP  ·  0", new Vector2(0f, 88f), new Vector2(660f, 36f), 19, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultBonusText = CreateText(card, "SALVAGE CODEC BONUS  ·  +0", new Vector2(0f, 48f), new Vector2(660f, 36f), 18, Hex("#ffc34d"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultBalanceText = CreateText(card, "TOTAL BALANCE  ·  0 ✦", new Vector2(0f, 8f), new Vector2(660f, 36f), 19, new Color(.93f, .95f, 1f, .78f), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultWorldText = CreateText(card, "ROUTE REACHED  ·  NEON CITY", new Vector2(0f, -34f), new Vector2(660f, 36f), 18, Hex("#b17cff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var flyAgain = CreateNeonButton(card, "RETRY", new Vector2(0f, -136f), new Vector2(570f, 88f), Hex("#f05bc6"));
            flyAgain.onClick.AddListener(RestartFlight);
            var hangar = CreateNeonButton(card, "HANGAR", new Vector2(-193f, -246f), new Vector2(260f, 70f), Hex("#45eaff"));
            hangar.onClick.AddListener(OpenHangar);
            var upgrades = CreateNeonButton(card, "UPGRADES", new Vector2(96f, -246f), new Vector2(288f, 70f), Hex("#ffc34d"));
            upgrades.onClick.AddListener(OpenUpgrades);
            var share = CreateNeonButton(card, "SHARE", new Vector2(0f, -336f), new Vector2(570f, 64f), Hex("#8f64ff"));
            resultShareText = share.GetComponentInChildren<Text>();
            share.onClick.AddListener(CopyRunSummaryToClipboard);
            return root;
        }

        private GameObject CreateCustomizeScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Customize screen");
            CreateFullPanel(root.transform, "Customize veil", new Color(.01f, .006f, .05f, .48f));
            var back = CreateNeonButton(root.transform, "‹  MENU", new Vector2(-390f, 802f), new Vector2(220f, 68f), Hex("#8f64ff"));
            back.onClick.AddListener(ResetToMenu);
            customizeCrystalText = CreateChip(root.transform, new Vector2(365f, 802f), "✦  0", Hex("#45eaff"));
            customizeTitle = CreateText(root.transform, "BIRD HANGAR", new Vector2(0f, 690f), new Vector2(720f, 80f), 48, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            CreateText(root.transform, "CHOOSE YOUR CYBER-BIRD OR CRYSTAL TECH", new Vector2(0f, 638f), new Vector2(800f, 38f), 18, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);

            var labels = new[] { "HANGAR", "UPGRADES" };
            var categories = new[] { CosmeticCategory.Birds, CosmeticCategory.Upgrades };
            for (var index = 0; index < labels.Length; index += 1)
            {
                var tab = CreateNeonButton(root.transform, labels[index], new Vector2(-150 + index * 300f, 560f), new Vector2(276f, 60f), index == 0 ? Hex("#45eaff") : Hex("#ffc34d"));
                var category = categories[index];
                tab.onClick.AddListener(() => SetCosmeticCategory(category));
            }

            var viewport = CreatePanel(root.transform, "Collection viewport", new Vector2(0f, -172f), new Vector2(970f, 1380f), Hex("#070a18"));
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 26f;
            scroll.viewport = viewport;

            var contentObject = new GameObject("Collection content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            customizeContent = contentObject.GetComponent<RectTransform>();
            customizeContent.anchorMin = new Vector2(0f, 1f);
            customizeContent.anchorMax = new Vector2(1f, 1f);
            customizeContent.pivot = new Vector2(.5f, 1f);
            customizeContent.anchoredPosition = Vector2.zero;
            scroll.content = customizeContent;
            return root;
        }

        private GameObject CreatePurchaseModal(Transform parent)
        {
            var root = CreateScreen(parent, "Bird skin purchase confirmation");
            CreateFullPanel(root.transform, "Purchase dim", new Color(.008f, .004f, .04f, .86f));
            var card = CreatePanel(root.transform, "Purchase card", new Vector2(0f, 18f), new Vector2(850f, 720f), Hex("#11132a"));
            AddOutline(card.gameObject, Hex("#45eaff"), 4f);
            purchaseHalo = CreateImage(card, "Purchase focus ring", new Vector2(0f, 128f), new Vector2(340f, 340f), new Color(.27f, .92f, 1f, .20f));
            purchaseHalo.sprite = ringSprite;
            purchaseHalo.raycastTarget = false;
            purchasePreviewImage = CreateImage(card, "Bird skin preview", new Vector2(0f, 128f), new Vector2(355f, 220f), Color.white);
            purchasePreviewImage.preserveAspect = true;
            purchasePreviewImage.raycastTarget = false;
            purchaseTitleText = CreateText(card, "UNLOCK BIRD?", new Vector2(0f, 278f), new Vector2(710f, 52f), 35, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            purchaseDetailText = CreateText(card, "", new Vector2(0f, -38f), new Vector2(700f, 52f), 24, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            purchaseBalanceText = CreateText(card, "", new Vector2(0f, -84f), new Vector2(700f, 36f), 19, new Color(.9f, .94f, 1f, .72f), TextAnchor.MiddleCenter, FontStyle.Bold);
            CreateText(card, "CONFIRM YOUR PURCHASE", new Vector2(0f, -142f), new Vector2(720f, 32f), 15, new Color(.9f, .94f, 1f, .57f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var cancel = CreateNeonButton(card, "CANCEL", new Vector2(-190f, -250f), new Vector2(330f, 78f), Hex("#8f64ff"));
            cancel.onClick.AddListener(ClosePurchaseModal);
            purchaseConfirmButton = CreateNeonButton(card, "UNLOCK", new Vector2(190f, -250f), new Vector2(330f, 78f), Hex("#45eaff"));
            purchaseConfirmText = purchaseConfirmButton.GetComponentInChildren<Text>();
            purchaseConfirmButton.onClick.AddListener(ConfirmPurchase);
            return root;
        }

        private GameObject CreateUnlockReveal(Transform parent)
        {
            var root = CreateScreen(parent, "Bird unlock reveal");
            CreateFullPanel(root.transform, "Unlock reveal dim", new Color(.005f, .004f, .026f, .92f));
            unlockRevealCard = CreatePanel(root.transform, "Unlock reveal card", new Vector2(0f, 22f), new Vector2(900f, 920f), Hex("#10142b"));
            AddOutline(unlockRevealCard.gameObject, Hex("#45eaff"), 4f);

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

            unlockRevealTitle = CreateText(unlockRevealCard, "BIRD UNLOCKED", new Vector2(0f, 392f), new Vector2(760f, 64f), 45, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
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
            // fallback. A missing Nova unlock pose must never silently become an
            // Aetherwing unlock pose (or vice versa).
            var unlockPose = LoadOptionalSprite(skin.UnlockPath);
            if (unlockPose == null)
            {
                Debug.LogWarning($"SkyPulse: {skin.Name} is missing its bespoke unlock frame at '{skin.UnlockPath}'. Showing its own current bird art until that frame is supplied.");
                unlockPose = LoadSprite(skin.ArtPath);
            }

            unlockRevealBirdImage.sprite = unlockPose;
            activeUnlockSkin = skin;
            var usesPremiumTint = IsAetherwingSkin(skin);
            unlockRevealBirdImage.color = usesPremiumTint ? Color.Lerp(Color.white, skin.Accent, .14f) : Color.white;
            unlockRevealTitle.text = $"{skin.Name} UNLOCKED";
            unlockRevealDetail.text = "EQUIPPED  ·  NEW FLIGHT FORM ACQUIRED";
            unlockRevealTitle.color = Hex("#f4fbff");
            unlockRevealDetail.color = skin.Accent;
            unlockRevealHalo.color = new Color(skin.Accent.r, skin.Accent.g, skin.Accent.b, 0f);
            unlockRevealFlash.color = new Color(skin.Accent.r, skin.Accent.g, skin.Accent.b, 0f);
            unlockRevealTimer = 0f;
            unlockRevealCard.localScale = Vector3.one * .88f;
            unlockRevealBirdTransform.anchoredPosition = new Vector2(0f, -72f);
            unlockRevealBirdTransform.localScale = Vector3.one * .62f;
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
            var duration = reduceMotionEnabled ? .34f : .92f;
            unlockRevealTimer = Mathf.Min(duration, unlockRevealTimer + deltaTime);
            var progress = Mathf.Clamp01(unlockRevealTimer / duration);
            var arrival = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / .58f));
            var settle = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((progress - .28f) / .72f));
            var bounce = reduceMotionEnabled ? 0f : Mathf.Sin(settle * Mathf.PI) * (1f - settle) * .11f;
            var pulse = reduceMotionEnabled ? 1f : 1f + Mathf.Sin(progress * Mathf.PI * 3.2f) * .07f * (1f - progress);
            var motion = UnlockMotionFor(activeUnlockSkin);

            unlockRevealCard.localScale = Vector3.one * (Mathf.Lerp(.88f, 1f, arrival) + bounce);
            unlockRevealBirdTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-72f, 150f + motion.y, arrival));
            unlockRevealBirdTransform.localScale = Vector3.one * (Mathf.Lerp(.62f, 1f, arrival) + bounce * .45f);
            unlockRevealBirdTransform.localRotation = Quaternion.Euler(0f, 0f, reduceMotionEnabled ? 0f : Mathf.Lerp(motion.x, 0f, arrival) + Mathf.Sin(progress * Mathf.PI * 2f) * (1f - progress) * 2.5f);

            var haloColor = unlockRevealHalo.color;
            haloColor.a = .16f + (1f - progress) * .32f;
            unlockRevealHalo.color = haloColor;
            unlockRevealHalo.rectTransform.localScale = Vector3.one * (Mathf.Lerp(.48f, 1.16f, arrival) * pulse);
            unlockRevealHalo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, reduceMotionEnabled ? 0f : -progress * 92f * motion.z);

            var flashColor = unlockRevealFlash.color;
            flashColor.a = Mathf.Sin(Mathf.Clamp01(progress / .42f) * Mathf.PI) * .32f;
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
            var frameDelta = Mathf.Min(Time.unscaledDeltaTime, MaximumSimulationCatchup);
            ambientTime += frameDelta;
            UpdateAmbientVisuals();
            UpdateRearThrust();
            UpdateMenuBird(frameDelta);
            UpdateUnlockReveal(frameDelta);
            UpdateScoreBurst(frameDelta);
            UpdateFlightFeedback(frameDelta);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UpdateDevelopmentQualityControls();
#endif

#if UNITY_EDITOR
            UpdateEditorQualityHarness(frameDelta);
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
                if (WasTapped() && !PointerOverUi()) StartFlight();
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
            UpdateFlightCoach();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UpdateCollisionDebug();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void UpdateDevelopmentQualityControls()
        {
            if (Input.GetKeyDown(KeyCode.F1)) SetDevelopmentFrameRateCap(30);
            if (Input.GetKeyDown(KeyCode.F2)) SetDevelopmentFrameRateCap(60);
            if (Input.GetKeyDown(KeyCode.F3)) SetDevelopmentFrameRateCap(120);
            if (Input.GetKeyDown(KeyCode.F4)) collisionDebugEnabled = !collisionDebugEnabled;
        }

        private void SetDevelopmentFrameRateCap(int frameRate)
        {
            developmentFrameRateCap = frameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = frameRate;
        }
#endif

#if UNITY_EDITOR
        private void UpdateEditorQualityHarness(float deltaTime)
        {
            if (editorQualityText == null) return;
            editorFrameSampleTime += Mathf.Max(0f, deltaTime);
            editorFrameSampleCount += 1;
            if (editorFrameSampleTime >= .25f)
            {
                editorDisplayedFps = editorFrameSampleCount / editorFrameSampleTime;
                editorFrameSampleTime = 0f;
                editorFrameSampleCount = 0;
            }

            var hitboxState = collisionDebugEnabled ? "HITBOX ON" : "F4 HITBOX";
            editorQualityText.text = $"EDITOR QA  ·  {developmentFrameRateCap} FPS CAP  ·  {Mathf.RoundToInt(editorDisplayedFps)} FPS  ·  F1 30  F2 60  F3 120  ·  {hitboxState}";
        }
#endif

        private void SimulateFlight(float deltaTime)
        {
            ConsumeBufferedFlap();
            UpdatePowerUpEffects(deltaTime);
            if (shieldHitStopTimer > 0f)
            {
                shieldHitStopTimer = Mathf.Max(0f, shieldHitStopTimer - deltaTime);
                UpdateTrail(0f);
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
                UpdateTrail(simulationDelta);
                return;
            }
            UpdatePipes(simulationDelta);
            if (state != FlightState.Playing) return;
            UpdateCrystalPickups(simulationDelta);
            UpdatePowerUps(simulationDelta);
            UpdateTrail(simulationDelta);
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
            var ambientMotion = reduceMotionEnabled ? .28f : 1f;
            if (backgroundRenderer != null)
            {
                backgroundRenderer.transform.position = new Vector3(Mathf.Sin(ambientTime * .08f) * .012f * ambientMotion, .12f + Mathf.Sin(ambientTime * .11f) * .008f * ambientMotion, 0f);
            }
            foreach (var star in ambientStars)
            {
                var y = star.Y + Mathf.Sin(ambientTime * star.Speed + star.Phase) * .025f * ambientMotion;
                star.Transform.position = new Vector3(star.X, y, 0f);
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
            if (menuWingTimer > 1.18f) menuWingTimer = 0f;

            var wingPhase = menuWingTimer / 1.18f;
            GetWingWeights(wingPhase, out var riseStrength, out var flapStrength);
            var premiumRig = UsesAetherwing();
            var usesFlapFrameSequence = UsesFlapFrameSequence();
            if (usesFlapFrameSequence)
            {
                var pose = SelectFlapFrame(wingPhase);
                if (pose != null && menuBirdImage.sprite != pose) menuBirdImage.sprite = pose;
                if (pose != null) menuBirdImage.enabled = true;
                if (pose != null && menuBirdShadowImage != null && menuBirdShadowImage.sprite != pose) menuBirdShadowImage.sprite = pose;
                if (pose != null && menuBirdShadowImage != null) menuBirdShadowImage.enabled = true;
                menuBirdImage.color = Color.white;
                if (menuBirdRiseImage != null) menuBirdRiseImage.color = Color.clear;
                if (menuBirdFlapImage != null) menuBirdFlapImage.color = Color.clear;
            }
            else if (premiumRig)
            {
                // Rendering only one complete pose prevents the double-body ghosting
                // that made the previous flight loop look like a blurred sticker.
                var pose = SelectAetherwingPose(riseStrength, flapStrength);
                if (pose != null && menuBirdImage.sprite != pose) menuBirdImage.sprite = pose;
                if (pose != null) menuBirdImage.enabled = true;
                if (pose != null && menuBirdShadowImage != null && menuBirdShadowImage.sprite != pose) menuBirdShadowImage.sprite = pose;
                if (pose != null && menuBirdShadowImage != null) menuBirdShadowImage.enabled = true;
                menuBirdImage.color = PremiumBirdTint();
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
            var authoredFlight = premiumRig || usesFlapFrameSequence;
            var flightTilt = authoredFlight ? hover * .75f - flapStrength * .85f : hover * 2.2f - flapStrength * 1.6f;
            var glideLean = (authoredFlight ? Mathf.Sin(ambientTime * 1.16f) * .24f : Mathf.Sin(ambientTime * 1.16f) * .65f) * menuMotion;
            var intro = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(menuPresentationTime / .48f));
            if (menuHeroTransform != null)
            {
                menuHeroTransform.anchoredPosition = new Vector2(Mathf.Sin(ambientTime * .82f) * (authoredFlight ? 3f : 8f) * menuMotion, 148f + hover * (authoredFlight ? 5f : 13f));
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
                // selected bird. Showing it over valid art made every cosmetic look
                // like the same emergency Aetherwing silhouette.
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
                // Aetherwing has a small, authored visor light. The old floating UI
                // glint belongs only to the legacy round-eyed birds.
                menuBirdEyeGlintImage.gameObject.SetActive(!UsesAetherwing() && !usesFlapFrameSequence);
                if (!UsesAetherwing() && !usesFlapFrameSequence)
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
                menuTitleText.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(ambientTime * 2.1f) * .006f);
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
            612f + t * riseDistance
        );

    var color =
        scoreBurstIsCrystal
            ? Hex("#ffc34d")
            : equippedSkin != null
                ? equippedSkin.Accent
                : Color.white;

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
            UpdateBirdWingMotion();
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
                    if (perfect) perfectPasses += 1;
                    // One physical gate is always exactly one score. Crystals,
                    // birds and permanent economy never leak into this number.
                    score += 1;
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
            if (routeScore < 5) return .32f;
            if (routeScore < 15) return .36f;
            if (routeScore < 30) return .40f;
            if (routeScore < 45) return .44f;
            var remixStep = 1 + Mathf.FloorToInt((routeScore - 45) / 15f);
            return Mathf.Min(.48f, .44f + remixStep * .01f);
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

        private bool AllowsGameplayUpgrades()
        {
            return false;
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
        // separate from Adventure power-ups so Classic and Daily players can still
        // build their collection, while power-ups remain an Adventure reward.
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
                    GetWorldWidth() * CrystalResonatorRadiusFraction());
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

                var bob = Mathf.Sin(ambientTime * 3.8f + pickup.Phase) * .09f;
                pickup.Transform.localPosition = new Vector3(pickup.X, pickup.Y + bob, 0f);
                var pulse = 1f + Mathf.Sin(ambientTime * 4.8f + pickup.Phase) * .10f;
                var spin = ambientTime * 2.8f + pickup.Phase;
                pickup.Glow.transform.localScale = Vector3.one * (.72f * pulse);
                pickup.Artwork.transform.localScale = pickup.ArtworkBaseScale * (1f + Mathf.Sin(spin) * .04f);
                pickup.Artwork.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(spin) * 5.5f);
                pickup.Depth.transform.localScale = pickup.ArtworkBaseScale * (1.12f + Mathf.Sin(spin) * .035f);
                pickup.Spark.transform.localPosition = new Vector3(Mathf.Cos(ambientTime * 5.1f + pickup.Phase) * .26f, Mathf.Sin(ambientTime * 5.1f + pickup.Phase) * .26f, 0f);

                if (Vector2.Distance(new Vector2(BirdX, birdY), new Vector2(pickup.X, pickup.Y + bob)) <= ActiveTuning().CollisionRadius + CrystalPickupRadius)
                {
                    CollectCrystalPickup(pickup);
                }
            }
        }

        private PipePair FindAvailableCrystalGate(PowerUpPickup ignoredPickup)
        {
            PipePair best = null;
            foreach (var candidate in pipePool)
            {
                if (candidate == null || !candidate.Root.activeSelf || candidate.Passed || candidate.X <= BirdX + 1.05f) continue;
                var claimed = false;
                foreach (var pickup in powerUpPool)
                {
                    if (pickup.Active && pickup.Gate == candidate)
                    {
                        claimed = true;
                        break;
                    }
                }
                if (claimed) continue;
                foreach (var pickup in crystalPickupPool)
                {
                    if (pickup != ignoredPickup && pickup.Active && pickup.Gate == candidate)
                    {
                        claimed = true;
                        break;
                    }
                }
                if (claimed) continue;
                if (best == null || candidate.X > best.X) best = candidate;
            }
            return best;
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
            if (gate == null || gate.IsStatic && gate.RouteScore < 0) return;
            // Sixty percent of gates carry a concise, safe three-dimensional-looking
            // arc. A power-up owns its gate instead, keeping the flight corridor
            // legible at a phone scale.
            if (GateHasPowerUp(gate) || RouteRange(0f, 1f) > .60f) return;
            var count = RouteRange(1, 4);
            var safeOffset = Mathf.Max(.30f, gate.GapHeight * .5f - 1.05f);
            var baseOffset = RouteRange(-safeOffset * .58f, safeOffset * .58f);
            for (var index = 0; index < count; index += 1)
            {
                var pickup = FindInactiveCrystalPickup();
                if (pickup == null) return;
                ConfigureCrystalPickup(pickup, gate, index, count, baseOffset, safeOffset);
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
            var crystal = LoadSprite("SkyPulse/art/powerups/generated/crystal-pellet-v3");
            var gold = Hex("#ffc34d");
            var cyan = Hex("#45eaff");
            pickup.Glow.color = new Color(gold.r, gold.g, gold.b, .24f);
            pickup.Artwork.sprite = crystal ?? softCircleSprite;
            pickup.Artwork.color = crystal == null ? gold : Color.white;
            pickup.ArtworkBaseScale = ArtworkScale(pickup.Artwork.sprite, .62f);
            pickup.Artwork.transform.localScale = pickup.ArtworkBaseScale;
            pickup.Artwork.transform.localRotation = Quaternion.identity;
            pickup.Artwork.transform.localPosition = Vector3.zero;
            pickup.Depth.sprite = crystal ?? softCircleSprite;
            pickup.Depth.transform.localPosition = Vector3.zero;
            pickup.Depth.transform.localScale = pickup.ArtworkBaseScale * 1.12f;
            pickup.Depth.color = new Color(gold.r, gold.g, gold.b, .14f);
            pickup.Spark.color = new Color(cyan.r, cyan.g, cyan.b, .94f);
            pickup.Transform.localPosition = new Vector3(pickup.X, pickup.Y, 0f);
        }

        private void CollectCrystalPickup(PowerUpPickup pickup)
        {
            DeferCrystalPickup(pickup, 0f);
            // Currency is banked on contact—even a failed run keeps the find.
            BankCollectedCrystals(1);
            ShowCrystalBurst(1);
            TriggerFlightFeedback(Hex("#ffc34d"), .26f);
            PulseHaptic(.08f);
            Play(crystalSound);
        }

        private PipePair FindAvailablePowerUpGate(PowerUpPickup ignoredPickup)
        {
            if (!AllowsPowerUps()) return null;
            PipePair best = null;
            foreach (var candidate in pipePool)
            {
                if (candidate == null || !candidate.Root.activeSelf || candidate.Passed || candidate.X <= BirdX + 1.05f) continue;
                var claimed = false;
                foreach (var pickup in powerUpPool)
                {
                    if (pickup != ignoredPickup && pickup.Active && pickup.Gate == candidate)
                    {
                        claimed = true;
                        break;
                    }
                }
                if (claimed) continue;
                foreach (var pickup in crystalPickupPool)
                {
                    if (pickup.Active && pickup.Gate == candidate)
                    {
                        claimed = true;
                        break;
                    }
                }
                if (claimed) continue;
                if (best == null || candidate.X > best.X) best = candidate;
            }
            return best;
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
            TriggerFlightFeedback(pickup.Glow.color, .22f);
            PulseHaptic(.08f);
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

        private void ConfigureRearThrust()
        {
            if (equippedSkin == null) return;
            birdThrustGlowColour = equippedSkin.Trail;
            birdThrustGlowColour.a = 1f;
            birdThrustCoreColour = Color.Lerp(equippedSkin.Trail, Color.white, .72f);
            birdThrustCoreColour.a = 1f;
        }

        private void UpdateRearThrust()
        {
            if (birdThrust == null ||
                birdThrustGlowRenderer == null ||
                birdThrustCoreRenderer == null)
            {
                return;
            }

            var alive =
                state == FlightState.Playing &&
                bird != null &&
                bird.gameObject.activeInHierarchy;

            var impactFade =
                state == FlightState.Impact
                    ? Mathf.Clamp01(
                        impactTumbleTimer /
                        Mathf.Max(.01f, ImpactTumbleSeconds)
                      ) * .18f
                    : 0f;

            var visibility = alive ? 1f : impactFade;

            if (visibility <= .001f)
            {
                birdThrustGlowRenderer.enabled = false;
                birdThrustCoreRenderer.enabled = false;
                return;
            }

            var motion = reduceMotionEnabled ? .30f : 1f;

            // Strongest immediately after a flap.
            var flapStrength =
                alive
                    ? 1f - Mathf.Clamp01(
                        wingTimer / WingCycleSeconds
                      )
                    : 0f;

            // Climbing gives the engine more energy.
            var riseBoost =
                alive
                    ? Mathf.Clamp01(
                        Mathf.Max(0f, birdVelocity) / 6.5f
                      )
                    : 0f;

            // Falling softens the engine slightly.
            var fallAmount =
                alive
                    ? Mathf.Clamp01(
                        Mathf.Max(0f, -birdVelocity) / 7f
                      )
                    : 0f;

            var fallDamp =
                Mathf.Lerp(1f, .76f, fallAmount);

            // Two frequencies stop the flame looking like
            // a perfectly repeating sine-wave animation.
            var fastFlicker =
                .5f +
                .5f * Mathf.Sin(
                    ambientTime * 23f
                );

            var fineFlicker =
                .5f +
                .5f * Mathf.Sin(
                    ambientTime * 37f + 1.4f
                );

            var energy =
                (
                    .50f +
                    flapStrength * .38f +
                    riseBoost * .25f +
                    fastFlicker * .08f +
                    fineFlicker * .05f
                ) * fallDamp;

            var coreLength =
                BirdThrustCoreLength +
                BirdThrustPulseLength *
                energy;

            var glowLength =
                BirdThrustGlowLength +
                BirdThrustPulseLength *
                1.65f *
                energy;

            // Slight irregular movement keeps the flame alive.
            var flutterY =
                (
                    Mathf.Sin(ambientTime * 17f) * .012f +
                    Mathf.Sin(ambientTime * 31f) * .005f
                ) * motion;

            birdThrust.localPosition =
                new Vector3(
                    BirdThrustAnchorX,
                    BirdThrustAnchorY + flutterY,
                    0f
                );

            birdThrust.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Sin(ambientTime * 15f) *
                    1.5f *
                    motion
                );

            // Large soft plasma envelope.
            birdThrustGlowRenderer.transform.localPosition =
                new Vector3(
                    -glowLength * .46f,
                    0f,
                    0f
                );

            birdThrustGlowRenderer.transform.localScale =
                new Vector3(
                    glowLength,
                    BirdThrustGlowHeight *
                    (
                        1f +
                        fastFlicker *
                        .20f *
                        motion
                    ),
                    1f
                );

            // Smaller, brighter hot core.
            birdThrustCoreRenderer.transform.localPosition =
                new Vector3(
                    -coreLength * .43f,
                    0f,
                    0f
                );

            birdThrustCoreRenderer.transform.localScale =
                new Vector3(
                    coreLength,
                    BirdThrustCoreHeight *
                    (
                        1f +
                        fineFlicker *
                        .14f *
                        motion
                    ),
                    1f
                );

            var glow = birdThrustGlowColour;

            glow.a =
                visibility *
                (
                    .18f +
                    energy * .19f
                );

            birdThrustGlowRenderer.color = glow;

            var core = Color.Lerp(
                birdThrustCoreColour,
                Color.white,
                .38f
            );

            core.a =
                visibility *
                (
                    .62f +
                    energy * .28f
                );

            birdThrustCoreRenderer.color = core;

            birdThrustGlowRenderer.enabled = true;
            birdThrustCoreRenderer.enabled = true;
        }
        private void UpdateTrail(float deltaTime)
        {
            var trailScale = 1f;
            if (slowFieldTimer > 0f) trailScale *= .88f;
            if (magnetHaloTimer > 0f) trailScale *= 1.12f;
            if (trailSafety != null) trailSafety.startWidth = .048f * trailScale;
            trailGlow.startWidth = .19f * trailScale;
            trailCore.startWidth = .082f * trailScale;
            trailPoints[0] = bird.TransformPoint(new Vector3(BirdThrustAnchorX, BirdThrustAnchorY, .1f));
            for (var index = 1; index < trailPoints.Length; index += 1)
            {
                var follow = 1f - Mathf.Exp(-deltaTime * Mathf.Lerp(19f, 8f, index / (float)(trailPoints.Length - 1)));
                trailPoints[index] = Vector3.Lerp(trailPoints[index], trailPoints[index - 1], follow);
            }
            if (trailSafety != null) trailSafety.positionCount = trailPoints.Length;
            trailGlow.positionCount = trailPoints.Length;
            trailCore.positionCount = trailPoints.Length;
            if (trailSafety != null) trailSafety.SetPositions(trailPoints);
            trailGlow.SetPositions(trailPoints);
            trailCore.SetPositions(trailPoints);
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
        new Vector2(0f, 612f);

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
        new Vector2(0f, 612f);

    scoreBurstText.rectTransform.localScale =
        Vector3.one * 1.06f;

    scoreBurstText.gameObject.SetActive(true);
}
        private void TriggerFlightFeedback(Color colour, float duration)
        {
            if (flightFeedbackRenderer == null || bird == null) return;
            flightFeedbackColour = colour;
            flightFeedbackTimer = Mathf.Max(flightFeedbackTimer, duration);
            flightFeedbackRenderer.transform.position = bird.position + new Vector3(0f, 0f, .2f);
            flightFeedbackRenderer.enabled = true;
        }

        private void UpdateFlightFeedback(float deltaTime)
        {
            if (flightFeedbackRenderer == null || flightFeedbackTimer <= 0f) return;
            flightFeedbackTimer = Mathf.Max(0f, flightFeedbackTimer - deltaTime);
            if (flightFeedbackTimer <= 0f)
            {
                flightFeedbackRenderer.enabled = false;
                return;
            }

            var progress = 1f - Mathf.Clamp01(flightFeedbackTimer / .36f);
            var colour = flightFeedbackColour;
            colour.a = Mathf.Lerp(.42f, 0f, progress);
            flightFeedbackRenderer.color = colour;
            flightFeedbackRenderer.transform.localScale = Vector3.one * Mathf.Lerp(.48f, 2.15f, progress);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
            BeginFlight(FlightMode.Classic);
        }

        private void RestartFlight()
        {
            BeginFlight(FlightMode.Classic);
        }

        private void StartDailyFlight()
        {
            BeginFlight(FlightMode.Classic);
        }

        private void BeginFlight(FlightMode mode)
        {
            ClosePurchaseModal();
            // The launch experience is one fair route. Retain the legacy enum only
            // so old local saves remain readable; it no longer changes the run.
            flightMode = FlightMode.Classic;
            selectedFlightMode = FlightMode.Classic;
            activeDailyRouteKey = string.Empty;
            dailyRouteRandom = null;
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
            lastCrashReason = "GATE IMPACT";
            birdY = 0f;
            birdVelocity = 0f;
            birdTilt = 0f;
            birdTiltVelocity = 0f;
            wingTimer = 1f;
            impactFrameTimer = 0f;
            impactTumbleTimer = 0f;
            slowFieldTimer = 0f;
            shieldFlashTimer = 0f;
            magnetHaloTimer = 0f;
            shieldImmunityTimer = 0f;
            shieldHitStopTimer = 0f;
            shieldCharges = 0;
            rescueCharges = 0;
            if (trailSafety != null) trailSafety.positionCount = 0;
            trailGlow.positionCount = 0;
            trailCore.positionCount = 0;
            var launchTrailPoint = new Vector3(BirdX, birdY, .1f);
            for (var index = 0; index < trailPoints.Length; index += 1) trailPoints[index] = launchTrailPoint;
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
            // The launch input is itself a flap, so start the 70 ms duplicate-touch
            // lockout here before any synthetic mouse/touch echo can arrive.
            lastFlapInputTime = Time.unscaledTime;
            Flap();
        }

        private void ResetToMenu()
        {
            ClosePurchaseModal();
            state = FlightState.Menu;
            menuPresentationTime = 0f;
            menuWingTimer = 0f;
            simulationAccumulator = 0f;
            bufferedFlapUntil = -1f;
            flightFeedbackTimer = 0f;
            if (flightFeedbackRenderer != null) flightFeedbackRenderer.enabled = false;
            birdY = .15f;
            birdVelocity = 0f;
            birdTilt = 0f;
            birdTiltVelocity = 0f;
            bird.position = new Vector3(BirdX, birdY, 0f);
            bird.gameObject.SetActive(false);
            if (trailSafety != null) trailSafety.positionCount = 0;
            trailGlow.positionCount = 0;
            trailCore.positionCount = 0;
            if (birdBodyCollider != null) birdBodyCollider.enabled = false;
            foreach (var pair in pipePool) pair.Root.SetActive(false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
            skySurgeTimer = 0f;
            scorePrismTimer = 0f;
            magnetHaloTimer = 0f;
            phaseShiftTimer = 0f;
            shieldCharges = 0;
            rescueCharges = 0;
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
            pair.IsStatic = pair.RouteScore < 3 || firstGateAfterTransition;
            if (firstGateAfterTransition) firstGateAfterTransition = false;
            pair.GapHeight = ActiveGap();
            var halfGap = pair.GapHeight * .5f;
            // Keep enough visible body above and below every opening. This makes a
            // low gate read as a deliberate lower route, not a clipped top pipe.
            var centreMinimum = Mathf.Max(GapCenterMinimum, GroundY + PipeMinimumVisibleHeight + halfGap);
            var centreMaximum = Mathf.Min(GapCenterMaximum, CameraHeight * .5f - PipeMinimumVisibleHeight - halfGap);
            var nextCentre = RouteRange(centreMinimum, centreMaximum);
            var precedingPair = FindPrecedingPipe(pair, x);
            if (precedingPair != null)
            {
                var maximumStep = CameraHeight * .20f;
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
            pair.DriftAmplitude = (pair.RouteWorldIndex == 1 || usesRemixPatterns) && !pair.IsStatic ? CameraHeight * .04f : 0f;
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
            if (nextWorldIndex == routeWorldIndex) return;

            routeWorldIndex = nextWorldIndex;
            routeWorld = Worlds[routeWorldIndex];
            RecordFarthestWorld(routeWorldIndex);
            worldTransitionTimer = WorldTransitionSeconds;
            worldRecoveryTimer = 0f;
            firstGateAfterTransition = true;
            foreach (var pair in pipePool) if (pair != null) pair.Root.SetActive(false);
            // Queued gates are intentionally discarded for the tunnel. Rebase their
            // route labels to the score the player actually reached so power-up
            // placement and post-45 remix rules never start a few gates early.
            nextGateRouteScore = score;
            foreach (var pickup in powerUpPool) if (pickup != null) DeferPowerUp(pickup, 0f);
            foreach (var pickup in crystalPickupPool) if (pickup != null) DeferCrystalPickup(pickup, 0f);
            ApplyRouteWorldVisuals();
            if (scoreBurstText != null)
            {
                scoreBurstTimer = .80f;
                scoreBurstText.text = routeWorld.Name;
                scoreBurstText.color = routeWorld.Accent;
                scoreBurstText.rectTransform.anchoredPosition = new Vector2(0f, 612f);
                scoreBurstText.gameObject.SetActive(true);
            }
        }

        private void UpdateWorldTransition(float deltaTime)
        {
            if (worldTransitionTimer > 0f)
            {
                worldTransitionTimer = Mathf.Max(0f, worldTransitionTimer - deltaTime);
                if (backgroundVeil != null)
                {
                    var flare = Mathf.Sin(Mathf.Clamp01(1f - worldTransitionTimer / WorldTransitionSeconds) * Mathf.PI);
                    backgroundVeil.color = new Color(routeWorld.Accent.r, routeWorld.Accent.g, routeWorld.Accent.b, .12f + flare * .36f);
                }
                if (worldTransitionTimer > 0f) return;

                // Gates reappear only after the tunnel finishes, and the first is
                // static. The following recovery beat keeps the re-entry readable.
                spawnX = GetWorldWidth() * .5f + 2.4f;
                for (var index = 0; index < pipePool.Length; index += 1) ConfigurePipe(pipePool[index], spawnX + index * RoutePipeSpacing());
                worldRecoveryTimer = WorldRecoverySeconds;
            }

            if (worldRecoveryTimer > 0f)
            {
                worldRecoveryTimer = Mathf.Max(0f, worldRecoveryTimer - deltaTime);
                if (backgroundVeil != null) backgroundVeil.color = new Color(routeWorld.Accent.r, routeWorld.Accent.g, routeWorld.Accent.b, .11f);
            }
        }

        private void ApplyRouteWorldVisuals()
        {
            if (routeWorld == null) routeWorld = Worlds[Mathf.Clamp(routeWorldIndex, 0, Worlds.Length - 1)];
            equippedWorld = routeWorld;
            equippedPipe = FindById(PipeStyles, routeWorld.PresetPipeId) ?? PipeStyles[0];
            if (backgroundRenderer != null)
            {
                backgroundRenderer.sprite = WorldBackdrop(routeWorld);
                FitBackgroundToCamera(backgroundRenderer, .5f);
            }
            if (backgroundVeil != null) backgroundVeil.color = new Color(routeWorld.Accent.r, routeWorld.Accent.g, routeWorld.Accent.b, .11f);
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
            // Movement stays within the non-colliding light layers. The gateway feels
            // alive, while the bright visual opening always remains the safe opening.
            var gateMotion = reduceMotionEnabled ? 0f : 1f;
            var pulse = reduceMotionEnabled ? .56f : .5f + .5f * Mathf.Sin(ambientTime * 6.4f + pipeX * 1.7f);
            var direction = topPipe ? 1f : -1f;
            var capBodyInset = .23f;
            // Theme identity belongs to the narrow energy parts. The cylindrical
            // body stays graphite, with only a restrained metal reflection.
            var metal = Color.Lerp(Hex("#0a1222"), equippedPipe.Panel, .15f);
            var reflectionColour = Color.Lerp(metal, Color.white, .20f + pulse * .06f);
            reflectionColour.a = Mathf.Lerp(.22f, .38f, pulse);
            if (!hasAuthoredPipeBody) surface.Artwork.color = reflectionColour;

            var coreColour = equippedPipe.Energy;
            coreColour.a = Mathf.Lerp(.08f, .18f, pulse);
            if (!hasAuthoredPipeBody) surface.Core.color = coreColour;
            var corePulseColour = Color.Lerp(equippedPipe.Energy, Color.white, .42f);
            corePulseColour.a = Mathf.Lerp(.12f, .42f, pulse);
            surface.CorePulse.color = corePulseColour;
            var corePhase = reduceMotionEnabled ? .48f : Mathf.Repeat(ambientTime * .68f + pipeX * .17f, 1f);
            var bodyHeight = Mathf.Max(.12f, surface.Panel.transform.localScale.y);
            var corePulseHeight = .44f + pulse * .10f;
            var corePulseStart = .18f + corePulseHeight * .5f;
            var corePulseTravel = Mathf.Max(0f, bodyHeight - .18f - corePulseHeight);
            surface.CorePulse.transform.localPosition = new Vector3(
                Mathf.Sin((ambientTime * 2.8f + pipeX) * gateMotion) * .018f,
                capY + direction * (corePulseStart + corePhase * corePulseTravel), 0f);
            surface.CorePulse.transform.localScale = new Vector3(.34f + pulse * .08f, corePulseHeight, 1f);

            var seamColour = surface.Energy.color;
            seamColour.a = Mathf.Lerp(.48f, .92f, pulse);
            surface.Energy.color = seamColour;
            surface.Energy.transform.localPosition = new Vector3(0f, capY + direction * (.055f + Mathf.Sin(ambientTime * 8.8f + pipeX) * .012f * gateMotion), 0f);
            surface.Energy.transform.localScale = new Vector3(PipeWidth * Mathf.Lerp(.58f, .69f, pulse), .016f + pulse * .012f, 1f);

            var highlightColour = surface.Highlight.color;
            highlightColour.a = Mathf.Lerp(.04f, .20f, pulse);
            surface.Highlight.color = highlightColour;
            surface.Highlight.transform.localPosition = new Vector3(0f, capY + direction * (.035f + Mathf.Cos(ambientTime * 7.2f + pipeX) * .010f * gateMotion), 0f);
            surface.Highlight.transform.localScale = new Vector3(PipeWidth * Mathf.Lerp(.52f, .62f, pulse), .006f + pulse * .007f, 1f);

            var scanPhase = reduceMotionEnabled ? .48f : Mathf.Repeat(ambientTime * .82f + pipeX * .11f, 1f);
            var scanColour = surface.Scan.color;
            scanColour.a = Mathf.Lerp(.05f, .20f, pulse);
            surface.Scan.color = scanColour;
            surface.Scan.transform.localPosition = new Vector3(0f, capY + direction * (capBodyInset + scanPhase * Mathf.Max(.08f, bodyHeight - .30f)), 0f);
            surface.Scan.transform.localScale = new Vector3(PipeWidth * .70f, .008f, 1f);

            var beaconColour = surface.Beacon.color;
            beaconColour.a = Mathf.Lerp(.13f, .43f, pulse);
            surface.Beacon.color = beaconColour;
            surface.Beacon.transform.localPosition = new Vector3(0f, capY + direction * .115f, 0f);
            surface.Beacon.transform.localScale = Vector3.one * Mathf.Lerp(.23f, .34f, pulse);

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
            capEnergyColour.a = Mathf.Lerp(.62f, .98f, pulse);
            surface.CapEnergy.color = capEnergyColour;
            surface.CapEnergy.transform.localPosition = new Vector3(0f, capY + direction * (.030f + Mathf.Sin(ambientTime * 8.8f + pipeX) * .008f * gateMotion), 0f);
            surface.CapEnergy.transform.localScale = new Vector3(PipeWidth * Mathf.Lerp(.60f, .69f, pulse), .016f + pulse * .008f, 1f);
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

        private void LayoutPipeSurface(PipeSurface surface, float centreY, float height, float capY, bool topPipe)
        {
            var style = equippedPipe;
            LayoutPlumbingGate(surface, centreY, height, capY, topPipe, style);

            var direction = topPipe ? 1f : -1f;
            var capCentre = capY + direction * (PipeCapHeight * .5f);
            SetPipeCollider(surface.BodyCollider, new Vector2(0f, centreY), new Vector2(PipeWidth, height));
            SetPipeCollider(surface.CapCollider, new Vector2(0f, capCentre), new Vector2(PipeCollisionWidth, PipeCapHeight));
            var insideOffset = direction * .048f;
            var useProceduralBodyDetails = !hasAuthoredPipeBody;
            surface.Core.enabled = useProceduralBodyDetails;
            surface.CorePulse.enabled = useProceduralBodyDetails;
            surface.Core.sortingOrder = 6;
            surface.CorePulse.sortingOrder = 7;
            surface.CapGlow.sortingOrder = 8;
            var coreColor = style.Energy;
            coreColor.a = .12f;
            surface.Core.color = coreColor;
            SetBlock(surface.Core, new Vector2(0f, centreY), new Vector2(PipeWidth * .29f, Mathf.Max(.16f, height - .44f)));
            surface.CorePulse.color = new Color(coreColor.r, coreColor.g, coreColor.b, 0f);
            surface.CorePulse.transform.localPosition = new Vector3(0f, capY + direction * .68f, 0f);
            surface.CorePulse.transform.localScale = new Vector3(.38f, .82f, 1f);

            surface.Energy.enabled = false;
            surface.Energy.sortingOrder = 9;
            var seamColor = style.Energy;
            seamColor.a = .72f;
            surface.Energy.color = seamColor;
            surface.Energy.transform.localPosition = new Vector3(0f, capY + insideOffset, 0f);
            surface.Energy.transform.localScale = new Vector3(PipeWidth * .64f, .022f, 1f);

            surface.Highlight.enabled = false;
            surface.Highlight.sortingOrder = 10;
            surface.Highlight.color = new Color(1f, 1f, 1f, .13f);
            surface.Highlight.transform.localPosition = new Vector3(0f, capY + insideOffset * .45f, 0f);
            surface.Highlight.transform.localScale = new Vector3(PipeWidth * .57f, .008f, 1f);

            surface.Scan.enabled = false;
            surface.Scan.sortingOrder = 10;
            var scanColor = style.Energy;
            scanColor.a = .18f;
            surface.Scan.color = scanColor;
            surface.Scan.transform.localPosition = new Vector3(0f, capY + direction * .36f, 0f);
            surface.Scan.transform.localScale = new Vector3(PipeWidth * .70f, .008f, 1f);

            surface.Beacon.enabled = false;
            surface.Beacon.sortingOrder = 12;
            var beaconColor = style.Energy;
            beaconColor.a = .28f;
            surface.Beacon.color = beaconColor;
            surface.Beacon.transform.localPosition = new Vector3(0f, capY + direction * .10f, 0f);
            surface.Beacon.transform.localScale = Vector3.one * .28f;

            surface.CapGlow.enabled = hasAuthoredPipeCap && hasAuthoredPipeGlow;
            if (hasAuthoredPipeCap && hasAuthoredPipeGlow)
            {
                var glowColor = style.Energy;
                glowColor.a = .20f;
                surface.CapGlow.sprite = pipeGlowSprite;
                surface.CapGlow.color = glowColor;
                SetSpriteBlock(surface.CapGlow, Vector2.up * capCentre, new Vector2(PipeCapWidth * .82f, PipeCapHeight * .28f));
                surface.CapGlow.transform.localRotation = Quaternion.identity;
            }
        }

        private void LayoutPlumbingGate(PipeSurface surface, float centreY, float height, float capY, bool topPipe, PipeStyle style)
        {
            var direction = topPipe ? 1f : -1f;
            var bodyHeight = Mathf.Max(.12f, height - .08f);
            var metal = Color.Lerp(Hex("#0a1222"), style.Panel, .08f);
            var metalDark = Darken(metal, .72f);
            var collarMetal = Color.Lerp(metal, style.Accent, .24f);

            // Prefer the authored mechanical pipe supplied for SkyPulse. The
            // procedural layers remain a safe fallback if an asset is omitted from
            // a build, while collision continues to use PipeCollisionWidth.
            surface.Artwork.enabled = hasAuthoredPipeBody;
            surface.Outer.enabled = !hasAuthoredPipeBody;
            surface.Panel.enabled = !hasAuthoredPipeBody;
            surface.Shade.enabled = !hasAuthoredPipeBody;
            surface.RailLeft.enabled = !hasAuthoredPipeBody;
            surface.RailRight.enabled = !hasAuthoredPipeBody;
            surface.Core.enabled = !hasAuthoredPipeBody;
            surface.CorePulse.enabled = !hasAuthoredPipeBody;

            if (hasAuthoredPipeBody)
            {
                surface.Artwork.sprite = pipeBodySprite;
                surface.Artwork.color = Color.white;
                surface.Artwork.sortingOrder = 5;
                SetSpriteBlock(surface.Artwork, new Vector2(0f, centreY), new Vector2(PipeWidth, height));
                surface.Artwork.transform.localRotation = topPipe
                    ? Quaternion.Euler(0f, 0f, 180f)
                    : Quaternion.identity;
            }
            else
            {
                SetBlock(surface.Outer, Vector2.up * centreY, new Vector2(PipeWidth, height));
                SetBlock(surface.Panel, Vector2.up * centreY, new Vector2(PipeWidth - .06f, bodyHeight));
                SetBlock(surface.Shade, new Vector2(-PipeWidth * .32f, centreY), new Vector2(PipeWidth * .18f, Mathf.Max(.12f, height - .14f)));
                SetBlock(surface.Artwork, new Vector2(PipeWidth * .07f, centreY), new Vector2(PipeWidth * .19f, Mathf.Max(.12f, height - .18f)));
                SetBlock(surface.RailLeft, new Vector2(-PipeWidth * .36f, centreY), new Vector2(.028f, Mathf.Max(.12f, height - .18f)));
                SetBlock(surface.RailRight, new Vector2(PipeWidth * .36f, centreY), new Vector2(.020f, Mathf.Max(.12f, height - .22f)));
                surface.Panel.color = new Color(metal.r, metal.g, metal.b, 1f);
                surface.Outer.color = new Color(metalDark.r, metalDark.g, metalDark.b, 1f);
                surface.Shade.color = new Color(0f, 0f, 0f, .18f);
                var reflection = Color.Lerp(metal, Color.white, .23f);
                reflection.a = .30f;
                surface.Artwork.color = reflection;
                var leftRail = style.Energy;
                leftRail.a = .28f;
                surface.RailLeft.color = leftRail;
                var rightRail = Color.Lerp(style.Energy, Color.white, .28f);
                rightRail.a = .15f;
                surface.RailRight.color = rightRail;
            }

            // The separate authored cap sits precisely at the playable gap edge.
            var capCentre = capY + direction * (PipeCapHeight * .5f);
            surface.CapOuter.enabled = !hasAuthoredPipeCap;
            surface.CapAccent.enabled = false;
            surface.CapPanel.enabled = true;
            surface.CapEnergy.enabled = !hasAuthoredPipeCap;

            if (hasAuthoredPipeCap)
            {
                surface.CapPanel.sprite = pipeCapSprite;
                surface.CapPanel.color = Color.white;
                surface.CapPanel.sortingOrder = 11;
                SetSpriteBlock(surface.CapPanel, Vector2.up * capCentre, new Vector2(PipeCollisionWidth, PipeCapHeight));
                surface.CapPanel.transform.localRotation = topPipe
                    ? Quaternion.Euler(0f, 0f, 180f)
                    : Quaternion.identity;

                surface.CapGlow.enabled = hasAuthoredPipeGlow;
                if (hasAuthoredPipeGlow)
                {
                    var authoredGlow = style.Energy;
                    authoredGlow.a = .20f;
                    surface.CapGlow.color = authoredGlow;
                    surface.CapGlow.sprite = pipeGlowSprite;
                    SetSpriteBlock(surface.CapGlow, Vector2.up * capCentre, new Vector2(PipeCapWidth * .82f, PipeCapHeight * .28f));
                    surface.CapGlow.transform.localRotation = Quaternion.identity;
                }
            }
            else
            {
                surface.CapGlow.enabled = false;
                SetBlock(surface.CapOuter, Vector2.up * capCentre, new Vector2(PipeWidth + .18f, .42f));
                SetBlock(surface.CapAccent, Vector2.up * capCentre, new Vector2(PipeWidth + .10f, .34f));
                SetBlock(surface.CapPanel, Vector2.up * capCentre, new Vector2(PipeWidth - .08f, .28f));
                SetBlock(surface.CapEnergy, Vector2.up * (capY + direction * .030f), new Vector2(PipeWidth * .64f, .018f));
                surface.CapOuter.color = Darken(metalDark, .18f);
                surface.CapAccent.color = collarMetal;
                surface.CapPanel.color = Darken(metal, .40f);
                var capEnergy = style.Energy;
                capEnergy.a = .82f;
                surface.CapEnergy.color = capEnergy;
            }
        }

        private float ActiveGap()
        {
            if (score < 5) return CameraHeight * .34f;
            if (score < 15) return CameraHeight * .31f;
            if (score < 30) return CameraHeight * .29f;
            if (score < 45) return CameraHeight * .27f;
            return CameraHeight * .25f;
        }

        private float RouteRange(float minimum, float maximum)
        {
            if (dailyRouteRandom == null) return UnityEngine.Random.Range(minimum, maximum);
            return minimum + (float)dailyRouteRandom.NextDouble() * (maximum - minimum);
        }

        private static float RandomCrystalRange(float minimum, float maximum)
        {
            return UnityEngine.Random.Range(minimum, maximum);
        }

        private int RouteRange(int minimumInclusive, int maximumExclusive)
        {
            if (dailyRouteRandom == null) return UnityEngine.Random.Range(minimumInclusive, maximumExclusive);
            return dailyRouteRandom.Next(minimumInclusive, maximumExclusive);
        }

        private static string DailyRouteKey()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        private static int DailyRouteSeed(string routeKey)
        {
            unchecked
            {
                var hash = 17;
                foreach (var character in routeKey) hash = hash * 31 + character;
                return hash & int.MaxValue;
            }
        }

        private void Flap()
        {
            birdVelocity = Mathf.Max(ActiveFlapVelocity(), birdVelocity * .18f);
            wingTimer = 0f;
            Play(flapSound);
        }

        private void EndFlight()
        {
            if (state != FlightState.Playing) return;
            state = FlightState.Impact;
            if (birdThrustGlowRenderer != null)
                birdThrustGlowRenderer.enabled = false;
            if (birdThrustCoreRenderer != null)
                birdThrustCoreRenderer.enabled = false;
            if (birdBodyCollider != null) birdBodyCollider.enabled = false;
            impactFrameTimer = ImpactFreezeSeconds;
            impactTumbleTimer = ImpactTumbleSeconds;
            newBest = score > best && score > 0;
            best = Mathf.Max(best, score);
            ShowHitFrame();
            // Give every collision a clear downward reaction before the tumble.
            birdVelocity = Mathf.Min(birdVelocity, -2.4f);
            birdTiltVelocity = 0f;

           // Kill the long flight ribbon immediately so it does not freeze
          // awkwardly in mid-air during the crash animation.
          if (trailSafety != null) trailSafety.positionCount = 0;
          if (trailGlow != null) trailGlow.positionCount = 0;
          if (trailCore != null) trailCore.positionCount = 0;
            TriggerFlightFeedback(Hex("#f05bc6"), .36f);
            PulseHaptic(.28f);
            Play(crashSound);
            resultScoreText.text = $"SCORE  {score}";
            resultBestText.text = $"BEST  {best}";
            if (resultReasonText != null) resultReasonText.text = lastCrashReason;
            if (resultModeText != null)
            {
                resultModeText.text = "ENDLESS CYBER ROUTE";
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

           var tumbleSpeed = Mathf.Lerp(220f,360f,fallStrength);

            birdTilt += tumbleSpeed * deltaTime;
            bird.rotation = Quaternion.Euler(0f, 0f, birdTilt);
        }

        private void ShowHitFrame()
        {
            // The hit cell is visible for a short beat before the result card. It
            // makes a collision legible without interrupting the immediate retry flow.
            var hitPose = hitBirdSprite ?? LoadOptionalSprite(equippedSkin?.HitPath);
            if (hitPose == null || birdRenderer == null) return;

            SetAetherwingRigVisible(false);
            birdRenderer.sprite = hitPose;
            birdRenderer.color = Color.white;
            birdRenderer.enabled = true;
            birdArt.localScale = ArtworkScale(hitPose, BirdDisplayWidth);
            birdArt.localPosition = new Vector3(-.025f, .012f, 0f);
            birdArt.localRotation = Quaternion.Euler(0f, 0f, -8f);
            if (birdFlapRenderer != null) birdFlapRenderer.enabled = false;
            if (birdRiseRenderer != null) birdRiseRenderer.enabled = false;
            if (birdParallaxRenderer != null) birdParallaxRenderer.enabled = false;
            if (birdDepthRenderer != null) birdDepthRenderer.enabled = false;
            if (birdEyeGlintRenderer != null) birdEyeGlintRenderer.enabled = false;
            if (birdSafetyRenderer != null) birdSafetyRenderer.enabled = false;
        }

        private void CycleFlightMode()
        {
            selectedFlightMode = selectedFlightMode == FlightMode.Classic ? FlightMode.Adventure : FlightMode.Classic;
            UpdateModeCopy();
            RefreshScreens();
        }

        private int BestFor(FlightMode mode)
        {
            if (mode == FlightMode.Adventure) return adventureBest;
            return mode == FlightMode.Daily ? dailyBest : best;
        }

        private void SetBestFor(FlightMode mode, int value)
        {
            if (mode == FlightMode.Adventure) adventureBest = value;
            else if (mode == FlightMode.Daily) dailyBest = value;
            else best = value;
        }

        private static string ModeLabel(FlightMode mode)
        {
            return mode == FlightMode.Adventure ? "ADVENTURE" : mode == FlightMode.Daily ? "DAILY" : "CLASSIC";
        }

        private static Color ModeAccent(FlightMode mode)
        {
            return mode == FlightMode.Adventure ? Hex("#f05bc6") : mode == FlightMode.Daily ? Hex("#ffc34d") : Hex("#45eaff");
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

        private void OpenWorldCollection()
        {
            state = FlightState.Customize;
            bird.gameObject.SetActive(false);
            cosmeticCategory = CosmeticCategory.Worlds;
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
            for (var index = customizeContent.childCount - 1; index >= 0; index -= 1) Destroy(customizeContent.GetChild(index).gameObject);

            switch (cosmeticCategory)
            {
                case CosmeticCategory.Birds:
                    customizeTitle.text = "BIRD HANGAR";
                    for (var index = 0; index < Skins.Length; index += 1)
                    {
                        var skin = Skins[index];
                        var status = equippedSkin.Id == skin.Id
                            ? "EQUIPPED"
                            : IsSkinOwned(skin) ? "TAP TO EQUIP" : $"UNLOCK · {skin.Price} ✦";
                        CreateCosmeticCard(index, skin.Name, status, skin.Accent, LoadSprite(skin.ArtPath), () => SelectSkin(skin));
                    }
                    SetContentRows(Skins.Length);
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
                    SetContentRows(Worlds.Length);
                    break;
                case CosmeticCategory.Pipes:
                    customizeTitle.text = "PIPE COLLECTION";
                    for (var index = 0; index < PipeStyles.Length; index += 1)
                    {
                        var style = PipeStyles[index];
                        CreateCosmeticCard(index, style.Name, equippedPipe.Id == style.Id ? "EQUIPPED" : "TAP TO EQUIP", style.Accent, null, () => EquipPipe(style), style.Panel, style.Energy, true);
                    }
                    SetContentRows(PipeStyles.Length);
                    break;
                default:
                    customizeTitle.text = "CRYSTAL UPGRADES";
                    for (var index = 0; index < Upgrades.Length; index += 1)
                    {
                        var upgrade = Upgrades[index];
                        CreateUpgradeCard(index, upgrade);
                    }
                    SetContentRows(Upgrades.Length);
                    break;
            }
        }

        private void SetContentRows(int count)
        {
            var rows = Mathf.CeilToInt(count / 2f);
            customizeContent.sizeDelta = new Vector2(0f, Mathf.Max(1360f, rows * 255f + 22f));
            customizeContent.anchoredPosition = Vector2.zero;
        }

        private void CreateCosmeticCard(int index, string title, string status, Color accent, Sprite preview, Action select, Color secondary = default, Color tertiary = default, bool pipePreview = false)
        {
            var column = index % 2;
            var row = index / 2;
            var card = CreatePanel(customizeContent, $"{title} card", new Vector2(column == 0 ? -235f : 235f, -12f - row * 250f), new Vector2(440f, 222f), Hex("#0b1022"));
            card.anchorMin = new Vector2(.5f, 1f);
            card.anchorMax = new Vector2(.5f, 1f);
            card.pivot = new Vector2(.5f, 1f);
            AddOutline(card.gameObject, accent, status == "EQUIPPED" ? 3f : 1.5f);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() => select());

            if (preview != null)
            {
                var image = CreateImage(card, "Preview", new Vector2(0f, 33f), new Vector2(384f, 134f), Color.white);
                image.sprite = preview;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            else if (pipePreview)
            {
                var outer = CreateImage(card, "Pipe preview shell", new Vector2(0f, 37f), new Vector2(206f, 80f), Hex("#030613"));
                var panel = CreateImage(card, "Pipe preview panel", new Vector2(0f, 37f), new Vector2(182f, 62f), secondary);
                var core = CreateImage(card, "Pipe preview core", new Vector2(0f, 37f), new Vector2(8f, 62f), tertiary);
                outer.raycastTarget = panel.raycastTarget = core.raycastTarget = false;
            }
            else
            {
                var glow = CreateImage(card, "Trail glow", new Vector2(0f, 38f), new Vector2(236f, 25f), secondary);
                var core = CreateImage(card, "Trail core", new Vector2(0f, 38f), new Vector2(204f, 8f), accent);
                glow.raycastTarget = core.raycastTarget = false;
            }

            CreateText(card, title, new Vector2(-185f, -68f), new Vector2(340f, 34f), 20, Hex("#f4fbff"), TextAnchor.MiddleLeft, FontStyle.Bold).raycastTarget = false;
            CreateText(card, status, new Vector2(-185f, -99f), new Vector2(330f, 28f), 15, status == "EQUIPPED" ? accent : new Color(.85f, .9f, 1f, .68f), TextAnchor.MiddleLeft, FontStyle.Bold).raycastTarget = false;
        }

        private void CreateUpgradeCard(int index, Upgrade upgrade)
        {
            var column = index % 2;
            var row = index / 2;
            var level = GetUpgradeLevel(upgrade.Id);
            var maxed = level >= upgrade.MaxLevel;
            var card = CreatePanel(customizeContent, $"{upgrade.Name} upgrade", new Vector2(column == 0 ? -235f : 235f, -12f - row * 250f), new Vector2(440f, 222f), Hex("#0b1022"));
            card.anchorMin = new Vector2(.5f, 1f);
            card.anchorMax = new Vector2(.5f, 1f);
            card.pivot = new Vector2(.5f, 1f);
            AddOutline(card.gameObject, upgrade.Accent, maxed ? 3f : 1.5f);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() => SelectUpgrade(upgrade));

            var halo = CreateImage(card, "Upgrade focus ring", new Vector2(-146f, 34f), new Vector2(102f, 102f), new Color(upgrade.Accent.r, upgrade.Accent.g, upgrade.Accent.b, .42f));
            halo.sprite = ringSprite;
            halo.raycastTarget = false;
            var previewSprite = GetUpgradeArtwork(upgrade);
            var artwork = CreateImage(card, "Upgrade artwork", new Vector2(-146f, 34f), new Vector2(94f, 94f), previewSprite == null ? upgrade.Accent : Color.white);
            artwork.sprite = previewSprite ?? softCircleSprite;
            artwork.preserveAspect = true;
            artwork.raycastTarget = false;
            CreateText(card, upgrade.Name, new Vector2(-70f, 53f), new Vector2(250f, 34f), 18, Hex("#f4fbff"), TextAnchor.MiddleLeft, FontStyle.Bold).raycastTarget = false;
            var nextEffect = maxed ? "MAXED · ALL CRYSTAL BENEFITS ACTIVE" : upgrade.EffectAtLevel(level);
            CreateText(card, nextEffect, new Vector2(-70f, 12f), new Vector2(278f, 68f), 14, new Color(.86f, .91f, 1f, .73f), TextAnchor.MiddleLeft, FontStyle.Normal).raycastTarget = false;
            var status = maxed ? "LEVEL 3 / 3 · MAXED" : $"LEVEL {level} / {upgrade.MaxLevel} · BUY {upgrade.PriceAtLevel(level)} ✦";
            CreateText(card, status, new Vector2(-185f, -85f), new Vector2(380f, 28f), 15, maxed ? upgrade.Accent : new Color(.85f, .9f, 1f, .68f), TextAnchor.MiddleLeft, FontStyle.Bold).raycastTarget = false;
        }

        private Sprite GetUpgradeArtwork(Upgrade upgrade)
        {
            var kind = upgrade.Id == "crystal_resonator" ? PowerUpKind.CrystalMagnet : PowerUpKind.Aegis;
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

        private bool HasUpgrade(string id)
        {
            var requiredLevel = UpgradeAliasLevel(id, "crystal_resonator_");
            if (requiredLevel > 0) return GetUpgradeLevel("crystal_resonator") >= requiredLevel;
            requiredLevel = UpgradeAliasLevel(id, "salvage_codec_");
            if (requiredLevel > 0) return GetUpgradeLevel("salvage_codec") >= requiredLevel;
            return GetUpgradeLevel(id) > 0;
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

        // These two helpers are intentionally the only permanent modifiers exposed
        // to the route. They affect crystal collection, never the bird, score, gates,
        // or power-up timing.
        private float CrystalResonatorRadiusFraction()
        {
            switch (GetUpgradeLevel("crystal_resonator"))
            {
                case 3: return .14f;
                case 2: return .10f;
                case 1: return .06f;
                default: return 0f;
            }
        }

        private float SalvageCodecBonusFraction()
        {
            switch (GetUpgradeLevel("salvage_codec"))
            {
                case 3: return .30f;
                case 2: return .20f;
                case 1: return .10f;
                default: return 0f;
            }
        }

        private void BeginProgressionRun()
        {
            runCrystalsCollected = 0;
            runCrystalBonus = 0;
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

        private int ApplySalvageCodecResultBonus()
        {
            if (resultCrystalBonusApplied) return runCrystalBonus;
            resultCrystalBonusApplied = true;
            runCrystalBonus = Mathf.FloorToInt(runCrystalsCollected * SalvageCodecBonusFraction() + .0001f);
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
            switch (Mathf.Clamp(worldIndex, 0, 2))
            {
                case 2: return "ORBITAL BAZAAR";
                case 1: return "ACID FOUNDRY";
                default: return "NEON CITY";
            }
        }

        private void RefreshProgressionResultLabels()
        {
            var bonus = ApplySalvageCodecResultBonus();
            if (resultCrystalsText != null) resultCrystalsText.text = $"CRYSTALS PICKED UP  ·  {runCrystalsCollected}";
            if (resultBonusText != null) resultBonusText.text = $"SALVAGE CODEC BONUS  ·  +{bonus}";
            if (resultBalanceText != null) resultBalanceText.text = $"TOTAL BALANCE  ·  {crystals} ✦";
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
            var level = GetUpgradeLevel(upgrade.Id);
            if (level >= upgrade.MaxLevel) return;
            pendingUpgrade = upgrade;
            pendingSkin = null;
            pendingPurchase = PendingPurchase.Upgrade;
            OpenPurchaseModal($"{upgrade.Name}  ·  L{level + 1}", upgrade.EffectAtLevel(level), upgrade.PriceAtLevel(level), upgrade.Accent, softCircleSprite);
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

        private void ClosePurchaseModal()
        {
            if (purchaseModal != null) purchaseModal.SetActive(false);
            pendingSkin = null;
            pendingUpgrade = null;
            pendingPurchase = PendingPurchase.None;
        }

        private void ConfirmPurchase()
        {
            if (pendingPurchase == PendingPurchase.Upgrade && (pendingUpgrade == null || GetUpgradeLevel(pendingUpgrade.Id) >= pendingUpgrade.MaxLevel))
            {
                ClosePurchaseModal();
                return;
            }
            var price = pendingPurchase == PendingPurchase.Skin && pendingSkin != null ? pendingSkin.Price
                : pendingPurchase == PendingPurchase.Upgrade && pendingUpgrade != null
                    ? pendingUpgrade.PriceAtLevel(GetUpgradeLevel(pendingUpgrade.Id)) : -1;
            if (price < 0 || crystals < price) return;
            var unlockedSkin = pendingPurchase == PendingPurchase.Skin ? pendingSkin : null;
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
                if (level >= pendingUpgrade.MaxLevel) return;
                upgradeLevels[pendingUpgrade.Id] = level + 1;
                ownedUpgradeIds.Add(pendingUpgrade.Id);
            }
            ClosePurchaseModal();
            ApplyEquippedVisuals();
            SaveProgress();
            Play(unlockSound);
            RebuildCustomizeGrid();
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
            if (equippedSkin == null) equippedSkin = Skins[0];
            if (equippedTrail == null) equippedTrail = GetTrailForSkin(equippedSkin);
            if (equippedWorld == null) equippedWorld = Worlds[0];
            if (equippedPipe == null) equippedPipe = PipeStyles[0];

            backgroundRenderer.sprite = WorldBackdrop(equippedWorld);
            FitBackgroundToCamera(backgroundRenderer, .5f);
            backgroundVeil.color = new Color(equippedWorld.Accent.r, equippedWorld.Accent.g, equippedWorld.Accent.b, .11f);
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
            if (menuBirdEyeGlintImage != null) menuBirdEyeGlintImage.gameObject.SetActive(!UsesAetherwing());
            UpdateCrystalLabels();
            if (menuEquippedText != null) menuEquippedText.text = $"EQUIPPED  ·  {equippedSkin.Name}";
            UpdateModeCopy();
            ApplyTrailColors();
            foreach (var pair in pipePool)
            {
                if (pair != null && pair.Root.activeSelf) ConfigurePipe(pair, pair.X);
            }
        }

        private void UpdateCrystalLabels()
        {
            if (menuCrystalText != null) menuCrystalText.text = $"✦  {crystals}";
            if (hudCrystalText != null) hudCrystalText.text = $"✦  {crystals}";
            if (customizeCrystalText != null) customizeCrystalText.text = $"✦  {crystals}";
        }

        private void ApplyTrailColors()
        {
            var safety = Color.Lerp(Hex("#0d286a"), equippedTrail.Core, .42f);
            safety.a = .92f;
            if (trailSafety != null)
            {
                trailSafety.startColor = safety;
                var safetyEnd = safety;
                safetyEnd.a = 0f;
                trailSafety.endColor = safetyEnd;
            }
            var glowStart = equippedTrail.Glow;
            glowStart.a = .26f;
            var glowEnd = equippedTrail.Core;
            glowEnd.a = 0f;
            trailGlow.startColor = glowStart;
            trailGlow.endColor = glowEnd;
            var coreStart = equippedTrail.Core;
            coreStart.a = .94f;
            var coreEnd = equippedTrail.Glow;
            coreEnd.a = 0f;
            trailCore.startColor = coreStart;
            trailCore.endColor = coreEnd;
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
            var aetherwing = UsesAetherwing();
            var hasFlapFrameSequence = LoadFlapFrameSequence(equippedSkin);
            hitBirdSprite = LoadOptionalSprite(equippedSkin?.HitPath);
            activeFlapFrameIndex = 0;
            if (hasFlapFrameSequence)
            {
                // The last supplied pose is the settled glide. The two named legacy
                // fields still populate menus/fallbacks; live flight uses all six.
                idleBirdSprite = flapFrameBirdSprites[flapFrameBirdSprites.Length - 1];
                flapBirdSprite = flapFrameBirdSprites[Mathf.Min(3, flapFrameBirdSprites.Length - 1)];
                riseBirdSprite = flapFrameBirdSprites[0];
            }
            else
            {
                idleBirdSprite = LoadSprite(equippedSkin.ArtPath)
                    ?? (aetherwing ? LoadSprite(AetherwingGlidePath) : null);
                flapBirdSprite = LoadSprite(equippedSkin.FlapPath)
                    ?? (aetherwing ? LoadSprite(AetherwingFlapPath) : idleBirdSprite);
                riseBirdSprite = string.IsNullOrEmpty(equippedSkin.RisePath)
                    ? flapBirdSprite
                    : LoadSprite(equippedSkin.RisePath) ?? flapBirdSprite;
            }
            if (idleBirdSprite != null)
            {
                birdRenderer.sprite = idleBirdSprite;
                idleBirdBaseScale = ArtworkScale(idleBirdSprite, BirdDisplayWidth);
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
            aetherwingRigReady = aetherwing && ConfigureAetherwingRig();
            SetAetherwingRigVisible(aetherwingRigReady);
            birdRenderer.enabled = idleBirdSprite != null && !aetherwingRigReady;
            birdFlapRenderer.enabled = !aetherwing && !hasFlapFrameSequence && flapBirdSprite != null;
            birdRiseRenderer.enabled = !aetherwing && !hasFlapFrameSequence && riseBirdSprite != null;
            if (birdParallaxRenderer != null) birdParallaxRenderer.enabled = idleBirdSprite != null && !aetherwingRigReady && !hasFlapFrameSequence;
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
            if (birdDepthRenderer != null) birdDepthRenderer.enabled = !aetherwing && !hasFlapFrameSequence;
            if (birdEyeGlintRenderer != null) birdEyeGlintRenderer.enabled = !UsesAetherwing() && !hasFlapFrameSequence;
            ConfigureRearThrust();
        }

        private bool LoadFlapFrameSequence(Skin skin)
        {
            flapFrameBirdSprites = null;
            if (skin == null || skin.FlapFramePaths == null || skin.FlapFramePaths.Length != 6) return false;
            var frames = new Sprite[skin.FlapFramePaths.Length];
            for (var index = 0; index < frames.Length; index += 1)
            {
                frames[index] = LoadOptionalSprite(skin.FlapFramePaths[index]);
                if (frames[index] == null) return false;
            }

            flapFrameBirdSprites = frames;
            // All test frames are rendered on a fixed canvas. One shared scale stops
            // a different source crop from making the bird pulse in size mid-flap.
            flapFrameBaseScale = ArtworkScale(frames[frames.Length - 1], BirdDisplayWidth);
            return true;
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

    var progress = Mathf.Clamp01(normalizedProgress);
    var frameCount = flapFrameBirdSprites.Length;

    // Custom timing for the six authored flight poses.
    // Lift happens quickly, while the later downstroke/recovery
    // frames remain visible slightly longer for smoother follow-through.
    if (frameCount == 6)
    {
        if (progress < .12f) return 0;
        if (progress < .25f) return 1;
        if (progress < .40f) return 2;
        if (progress < .57f) return 3;
        if (progress < .77f) return 4;
        return 5;
    }

    // Safe fallback for any future bird using a different frame count.
    return Mathf.Min(
        frameCount - 1,
        Mathf.FloorToInt(progress * frameCount)
    );
}
        private bool ConfigureAetherwingRig()
        {
            var rigResourcePrefix = equippedSkin?.RigResourcePrefix;
            if (aetherwingRig == null || string.IsNullOrEmpty(rigResourcePrefix)) return false;

            // Every skin supplies its own coloured pieces. The transparent pixels
            // around those pieces merely let them move over the background; they do
            // not mean a colourless, shared bird.
            var body = LoadOptionalSprite(rigResourcePrefix + "-body-v1");
            var farWing = LoadOptionalSprite(rigResourcePrefix + "-far-wing-v1");
            var upperWing = LoadOptionalSprite(rigResourcePrefix + "-upper-wing-v1");
            var lowerWing = LoadOptionalSprite(rigResourcePrefix + "-lower-wing-v1");
            var featherFan = LoadOptionalSprite(rigResourcePrefix + "-feather-fan-v1");
            var tail = LoadOptionalSprite(rigResourcePrefix + "-tail-v1");
            var complete = body != null && farWing != null && upperWing != null && lowerWing != null && featherFan != null && tail != null;
            if (!complete) return false;

            aetherwingBodyRenderer.sprite = body;
            aetherwingFarWingRenderer.sprite = farWing;
            aetherwingUpperWingRenderer.sprite = upperWing;
            aetherwingLowerWingRenderer.sprite = lowerWing;
            aetherwingFeatherFanRenderer.sprite = featherFan;
            aetherwingTailRenderer.sprite = tail;
            aetherwingRigBaseScale = ArtworkScale(body, BirdDisplayWidth);
            ResetAetherwingRigPose();
            return true;
        }

        private void SetAetherwingRigVisible(bool visible)
        {
            if (aetherwingRig == null) return;
            aetherwingRig.gameObject.SetActive(visible);
            if (aetherwingBodyRenderer != null) aetherwingBodyRenderer.enabled = visible;
            if (aetherwingFarWingRenderer != null) aetherwingFarWingRenderer.enabled = visible;
            if (aetherwingUpperWingRenderer != null) aetherwingUpperWingRenderer.enabled = visible;
            if (aetherwingLowerWingRenderer != null) aetherwingLowerWingRenderer.enabled = visible;
            if (aetherwingFeatherFanRenderer != null) aetherwingFeatherFanRenderer.enabled = visible;
            if (aetherwingTailRenderer != null) aetherwingTailRenderer.enabled = visible;
        }

        private void ResetAetherwingRigPose()
        {
            if (aetherwingFarWingJoint != null) aetherwingFarWingJoint.localRotation = Quaternion.identity;
            if (aetherwingUpperWingJoint != null) aetherwingUpperWingJoint.localRotation = Quaternion.identity;
            if (aetherwingLowerWingJoint != null) aetherwingLowerWingJoint.localRotation = Quaternion.identity;
            if (aetherwingFeatherFanJoint != null) aetherwingFeatherFanJoint.localRotation = Quaternion.identity;
            if (aetherwingTailJoint != null) aetherwingTailJoint.localRotation = Quaternion.identity;
        }

        private bool UsesAetherwing()
        {
            return IsAetherwingSkin(equippedSkin);
        }

        private static bool IsAetherwingSkin(Skin skin)
        {
            // A skin opts into continuous rig animation only when it has named its
            // own authored layers. No bird ever borrows a different bird's colour,
            // feathers, crystal parts, or silhouette.
            return skin != null && !string.IsNullOrEmpty(skin.RigResourcePrefix);
        }

        private Color PremiumBirdTint()
        {
            if (equippedSkin == null) return Color.white;
            var tint = Color.Lerp(Color.white, equippedSkin.Accent, .14f);
            tint.a = 1f;
            return tint;
        }

        private Sprite SelectAetherwingPose(float riseWeight, float flapWeight)
        {
            // The thresholds intentionally overlap. Lift owns the start of the
            // cycle, then downstroke owns the hand-off, so rapid taps never reveal a
            // momentary idle pose between two wing strokes.
            if (riseWeight > WingLiftPoseThreshold && riseBirdSprite != null) return riseBirdSprite;
            if (flapWeight > WingDownstrokePoseThreshold && flapBirdSprite != null) return flapBirdSprite;
            return idleBirdSprite;
        }

        private static void GetWingWeights(float normalizedPhase, out float riseWeight, out float downstrokeWeight)
        {
            normalizedPhase = Mathf.Clamp01(normalizedPhase);
            riseWeight = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedPhase / WingLiftPhase));
            downstrokeWeight = Mathf.Sin(Mathf.PI * Mathf.Clamp01((normalizedPhase - WingDownstrokeDelay) / WingDownstrokeSpan));
        }

        private Vector3 BaseScaleForBirdPose(Sprite pose)
        {
            if (pose == riseBirdSprite) return riseBirdBaseScale;
            if (pose == flapBirdSprite) return flapBirdBaseScale;
            return idleBirdBaseScale;
        }

        private void UpdateAetherwingRigMotion(float riseWeight, float downstrokeWeight)
        {
            if (!aetherwingRigReady) return;
            var motion = equippedSkin?.RigMotion ?? AetherwingRigMotion;

            // The small stagger between pieces is what makes the feathered armour
            // feel connected rather than like one stiff card rotating as a whole.
            // Values are intentionally modest; the six supplied templates remain
            // the visual target, and these hinges can be tuned after the first
            // in-game preview without redrawing any artwork.
            if (aetherwingFarWingJoint != null)
                aetherwingFarWingJoint.localRotation = Quaternion.Euler(0f, 0f, motion.FarWingLift * riseWeight + motion.FarWingDownstroke * downstrokeWeight);
            if (aetherwingUpperWingJoint != null)
                aetherwingUpperWingJoint.localRotation = Quaternion.Euler(0f, 0f, motion.UpperWingLift * riseWeight + motion.UpperWingDownstroke * downstrokeWeight);
            if (aetherwingLowerWingJoint != null)
                aetherwingLowerWingJoint.localRotation = Quaternion.Euler(0f, 0f, motion.LowerWingLift * riseWeight + motion.LowerWingDownstroke * downstrokeWeight);
            if (aetherwingFeatherFanJoint != null)
                aetherwingFeatherFanJoint.localRotation = Quaternion.Euler(0f, 0f, motion.FeatherFanLift * riseWeight + motion.FeatherFanDownstroke * downstrokeWeight);
            if (aetherwingTailJoint != null)
                aetherwingTailJoint.localRotation = Quaternion.Euler(0f, 0f, motion.TailLift * riseWeight + motion.TailDownstroke * downstrokeWeight);

            if (aetherwingBodyRenderer != null) aetherwingBodyRenderer.color = Color.white;
            if (aetherwingFarWingRenderer != null) aetherwingFarWingRenderer.color = Color.white;
            if (aetherwingUpperWingRenderer != null) aetherwingUpperWingRenderer.color = Color.white;
            if (aetherwingLowerWingRenderer != null) aetherwingLowerWingRenderer.color = Color.white;
            if (aetherwingFeatherFanRenderer != null) aetherwingFeatherFanRenderer.color = Color.white;
            if (aetherwingTailRenderer != null) aetherwingTailRenderer.color = Color.white;
        }

        private static Vector3 ArtworkScale(Sprite sprite, float targetWidth)
        {
            var sourceWidth = Mathf.Max(.01f, sprite.bounds.size.x);
            return Vector3.one * (targetWidth / sourceWidth);
        }

        private void UpdateBirdWingMotion()
        {
            if (birdRenderer == null || birdFlapRenderer == null) return;
            var flapProgress = Mathf.Clamp01(wingTimer / WingCycleSeconds);
            var flapKick = 1f - flapProgress * flapProgress * (3f - 2f * flapProgress);
            GetWingWeights(flapProgress, out var riseWeight, out var wingWave);
            var premiumRig = UsesAetherwing();
            var usesFlapFrameSequence = UsesFlapFrameSequence();
            var usesLayeredAetherwingRig = premiumRig && aetherwingRigReady;
            if (usesFlapFrameSequence && !usesLayeredAetherwingRig)
            {
                // One authored drawing at a time: no crossfade means no ghosted
                // double-body. The six supplied poses play in their intended order
                // on every tap, while the transform below retains 60 fps flight feel.
                activeFlapFrameIndex = SelectFlapFrameIndex(flapProgress);
                var pose = flapFrameBirdSprites[activeFlapFrameIndex];
                if (pose != null && birdRenderer.sprite != pose) birdRenderer.sprite = pose;
                birdRenderer.enabled = pose != null;
                birdRenderer.color = Color.white;
                birdFlapRenderer.enabled = false;
                if (birdRiseRenderer != null) birdRiseRenderer.enabled = false;
                if (birdParallaxRenderer != null) birdParallaxRenderer.enabled = false;
            }
            else if (premiumRig)
            {
                if (usesLayeredAetherwingRig)
                {
                    birdRenderer.enabled = false;
                    if (birdParallaxRenderer != null) birdParallaxRenderer.enabled = false;
                    UpdateAetherwingRigMotion(riseWeight, wingWave);
                }
                else
                {
                    // Until the complete rig art arrives, exactly one sharp drawing
                    // is displayed. This is the safe, current fallback.
                    var pose = SelectAetherwingPose(riseWeight, wingWave);
                    if (pose != null && birdRenderer.sprite != pose) birdRenderer.sprite = pose;
                    birdRenderer.color = PremiumBirdTint();
                    if (birdParallaxRenderer != null)
                    {
                        birdParallaxRenderer.enabled = pose != null;
                        if (pose != null && birdParallaxRenderer.sprite != pose) birdParallaxRenderer.sprite = pose;
                    }
                }
                birdFlapRenderer.enabled = false;
                if (birdRiseRenderer != null) birdRiseRenderer.enabled = false;
            }
            else
            {
                var flapColour = Color.white;
                flapColour.a = (.10f + wingWave * .82f) * flapKick * (1f - riseWeight * .84f);
                birdFlapRenderer.enabled = flapBirdSprite != null;
                birdFlapRenderer.color = flapColour;
                birdRenderer.color = new Color(1f, 1f, 1f, 1f - Mathf.Max(riseWeight * .78f, wingWave * .50f));
                if (birdRiseRenderer != null)
                {
                    var showRise = birdRiseRenderer.enabled && birdRiseRenderer.sprite != null;
                    birdRiseRenderer.color = new Color(1f, 1f, 1f, showRise ? riseWeight * .94f : 0f);
                    birdRiseArt.localScale = Vector3.Scale(riseBirdBaseScale, new Vector3(1f + riseWeight * .065f, 1f - riseWeight * .025f, 1f));
                    birdRiseArt.localPosition = new Vector3(-riseWeight * .060f, riseWeight * .065f, 0f);
                    birdRiseArt.localRotation = Quaternion.Euler(0f, 0f, riseWeight * 4.7f);
                }
            }
            var lifeMotion = reduceMotionEnabled ? .38f : 1f;
            var authoredFlight = premiumRig || usesFlapFrameSequence;
            var breathing = 1f + Mathf.Sin(ambientTime * 5.2f) * .010f * lifeMotion;
            var glide = Mathf.Clamp(birdVelocity / Mathf.Abs(ActiveMaxFallVelocity()), -1f, 1f);
            var liftSquash =(flapKick - riseWeight * .34f) *(authoredFlight ? .035f : .065f);

            var diveStretch = Mathf.Clamp01(-glide) *(authoredFlight ? .016f : .024f);

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
            var activeArtworkTransform = usesLayeredAetherwingRig ? aetherwingRig : birdArt;
            var activeArtworkBaseScale = usesLayeredAetherwingRig
                ? aetherwingRigBaseScale
                : usesFlapFrameSequence ? ArtworkScale(birdRenderer.sprite, BirdDisplayWidth)
                : premiumRig ? BaseScaleForBirdPose(birdRenderer.sprite) : idleBirdBaseScale;
            activeArtworkTransform.localScale = Vector3.Scale(activeArtworkBaseScale, new Vector3(breathing + liftSquash + diveStretch, breathing - liftSquash - diveStretch * .55f, 1f));
            activeArtworkTransform.localPosition = authoredFlight
    ? new Vector3(
        -flapKick * .035f
        - wingWave * .012f
        - glide * .012f,

        Mathf.Sin(ambientTime * 7f) * .006f * lifeMotion
        + flapKick * .018f
        + riseWeight * .025f
        + wingWave * .008f,

        0f
    )
    : new Vector3(
        -flapKick * .052f - glide * .020f,
        Mathf.Sin(ambientTime * 7f) * .014f + riseWeight * .018f,
        0f
    );
            activeArtworkTransform.localRotation = Quaternion.Euler(0f, 0f, bodyRoll);
            if (!premiumRig && !usesFlapFrameSequence)
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
                if (UsesFlapFrameSequence() || aetherwingRigReady)
                {
                    birdParallaxRenderer.enabled = false;
                }
                else if (UsesAetherwing())
                {
                    var pose = birdRenderer != null ? birdRenderer.sprite : idleBirdSprite;
                    if (pose != null && birdParallaxRenderer.sprite != pose) birdParallaxRenderer.sprite = pose;
                    birdParallaxRenderer.enabled = pose != null;
                    birdParallaxRenderer.color = new Color(.004f, .010f, .040f, .52f);
                    birdParallaxRenderer.transform.localPosition = new Vector3(-.080f - glide * .012f, -.078f - riseWeight * .010f, 0f);
                    birdParallaxRenderer.transform.localRotation = birdArt != null ? birdArt.localRotation : Quaternion.identity;
                    birdParallaxRenderer.transform.localScale = BaseScaleForBirdPose(pose) * 1.018f;
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
            if (birdEyeGlintRenderer != null && !UsesAetherwing())
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
            var effectMotion = reduceMotionEnabled ? .28f : 1f;
            if (slowAuraRenderer != null)
            {
                var slowPulse = 1f + Mathf.Sin(ambientTime * 6.5f) * .12f * effectMotion;
                slowAuraRenderer.enabled = slowFieldTimer > 0f;
                slowAuraRenderer.color = new Color(.55f, .35f, 1f, .48f + Mathf.Sin(ambientTime * 5f) * .12f * effectMotion);
                slowAuraRenderer.transform.localScale = Vector3.one * (1.42f * slowPulse);
                slowAuraRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -ambientTime * 110f * effectMotion);
            }
            if (effectAuraRenderer != null)
            {
                var active = magnetHaloTimer > 0f;
                var colour = Hex("#45eaff");
                colour.a = active ? .22f + Mathf.Sin(ambientTime * 9f) * .08f * effectMotion : 0f;
                effectAuraRenderer.enabled = active;
                effectAuraRenderer.color = colour;
                effectAuraRenderer.transform.localScale = Vector3.one * (1.12f + Mathf.Sin(ambientTime * 7.5f) * .10f * effectMotion);
            }
            if (shieldAuraRenderer != null)
            {
                var visible = shieldCharges > 0 || shieldFlashTimer > 0f;
                var flash = shieldFlashTimer > 0f ? 1f : .5f;
                shieldAuraRenderer.enabled = visible;
                shieldAuraRenderer.color = new Color(.38f, 1f, .70f, visible ? flash : 0f);
                shieldAuraRenderer.transform.localScale = Vector3.one * (1.2f + Mathf.Sin(ambientTime * 8f) * .08f * effectMotion + shieldFlashTimer * .25f);
                shieldAuraRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, ambientTime * 95f * effectMotion);
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
                menuModeDetailText.text = "ONE FAIR ROUTE · COLLECT CRYSTALS · MASTER THE FLOW";
                menuModeDetailText.color = Hex("#45eaff");
            }
            if (menuDailyText != null)
            {
                menuDailyText.text = "NEON CITY  →  ACID FOUNDRY  →  ORBITAL BAZAAR";
            }
            if (hudModeText != null)
            {
                hudModeText.text = RouteWorldName(routeWorldIndex);
                hudModeText.color = routeWorld == null ? Hex("#45eaff") : routeWorld.Accent;
            }
            if (menuBestText != null) menuBestText.text = $"BEST · {best}";
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
            if (reduceMotionText != null) reduceMotionText.text = reduceMotionEnabled ? "MOTION  ·  REDUCED" : "MOTION  ·  FULL";
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
            // The previous single best score is intentionally carried forward as the
            // Classic best, so existing pilots never lose progress when fair modes land.
            best = PlayerPrefs.GetInt("skypulse.native.best", 0);
            adventureBest = PlayerPrefs.GetInt("skypulse.native.adventure-best", 0);
            var today = DailyRouteKey();
            dailyBest = PlayerPrefs.GetString("skypulse.native.daily-key", string.Empty) == today
                ? PlayerPrefs.GetInt("skypulse.native.daily-best", 0)
                : 0;
            selectedFlightMode = PlayerPrefs.GetString("skypulse.native.selected-mode", "classic") == "adventure"
                ? FlightMode.Adventure
                : FlightMode.Classic;
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
                // pilots start with Neon Finch only; the four unlocks stay earned.
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
            PlayerPrefs.SetInt("skypulse.native.adventure-best", adventureBest);
            PlayerPrefs.SetString("skypulse.native.daily-key", DailyRouteKey());
            PlayerPrefs.SetInt("skypulse.native.daily-best", dailyBest);
            PlayerPrefs.SetString("skypulse.native.selected-mode", selectedFlightMode == FlightMode.Adventure ? "adventure" : "classic");
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
                        alpha = 1f - Mathf.SmoothStep(0f, outerRadius, distance);
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
                // the player; usual play uses the high-detail Aetherwing artwork.
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
                name = "Aetherwing emergency silhouette",
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
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = objectRoot.GetComponent<Image>();
            image.color = color;
            return rect;
        }

        private RectTransform CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var image = CreateImage(parent, name, position, size, color);
            if (roundedPanelSprite != null)
            {
                image.sprite = roundedPanelSprite;
                image.type = Image.Type.Sliced;
            }
            return image.rectTransform;
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
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private Text CreateChip(Transform parent, Vector2 position, string value, Color accent)
        {
            var shell = CreatePanel(parent, "Crystal chip", position, new Vector2(200f, 68f), Hex("#0a0f20"));
            AddOutline(shell.gameObject, new Color(accent.r, accent.g, accent.b, .50f), 1f);
            return CreateText(shell, value, Vector2.zero, new Vector2(180f, 48f), 23, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private Button CreateNeonButton(Transform parent, string label, Vector2 position, Vector2 size, Color accent)
        {
            var shell = CreatePanel(parent, "Button · " + label, position, size, Hex("#090e1e"));
            var shellImage = shell.GetComponent<Image>();
            AddOutline(shell.gameObject, new Color(accent.r, accent.g, accent.b, .58f), 1f);
            var primaryAction = label == "PLAY" || label == "RETRY" || label == "FLY";
            var fill = Color.Lerp(Hex("#11172c"), accent, primaryAction ? .13f : .065f);
            fill.a = 1f;
            var inner = CreatePanel(shell, "Button inner", Vector2.zero, size - new Vector2(8f, 8f), fill);
            inner.GetComponent<Image>().raycastTarget = false;
            var energy = CreatePanel(shell, "Button energy line", new Vector2(0f, -size.y * .25f), new Vector2(primaryAction ? 128f : 88f, 1.5f), new Color(accent.r, accent.g, accent.b, .60f));
            energy.GetComponent<Image>().raycastTarget = false;
            var text = CreateText(shell, label, Vector2.zero, size - new Vector2(22f, 14f), primaryAction ? 34 : 22, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            text.raycastTarget = false;
            var button = shell.gameObject.AddComponent<Button>();
            button.targetGraphic = shellImage;
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
