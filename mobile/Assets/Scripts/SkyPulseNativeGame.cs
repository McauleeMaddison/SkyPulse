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
        private enum FlightState { Menu, Playing, Paused, GameOver, Customize }
        // Classic is the score-first, leaderboard-ready route. Adventure deliberately
        // keeps the expressive upgrades and power-ups that make collection rewarding.
        // Daily shares Classic's fixed rules, plus a seeded obstacle sequence.
        private enum FlightMode { Classic, Adventure, Daily }
        private enum CosmeticCategory { Birds, Worlds, Trails, Pipes, Upgrades }
        private enum PowerUpKind { SlowField, PulseShield, CrystalCache, SkySurge, ScorePrism, MagnetHalo, PhaseShift }
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

        private sealed class Skin
        {
            public string Id;
            public string Name;
            public string ArtPath;
            public string FlapPath;
            public string RisePath;
            public Color Accent;
            public Color Trail;
            public int Price;

            public Skin(string id, string name, string artPath, string flapPath, string accent, string trail, int price, string risePath = null)
            {
                Id = id;
                Name = name;
                ArtPath = artPath;
                FlapPath = flapPath;
                RisePath = risePath;
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
            public string Description;
            public int Price;
            public Color Accent;

            public Upgrade(string id, string name, string description, int price, string accent)
            {
                Id = id;
                Name = name;
                Description = description;
                Price = price;
                Accent = Hex(accent);
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
            public SpriteRenderer Artwork;
            public SpriteRenderer Outer;
            public SpriteRenderer Panel;
            public SpriteRenderer Shade;
            public SpriteRenderer Highlight;
            public SpriteRenderer Energy;
            public SpriteRenderer Scan;
            public SpriteRenderer Beacon;
            public SpriteRenderer CapOuter;
            public SpriteRenderer CapAccent;
            public SpriteRenderer CapPanel;
            public SpriteRenderer CapEnergy;
        }

        private sealed class PipePair
        {
            public GameObject Root;
            public PipeSurface Top;
            public PipeSurface Bottom;
            public float X;
            public float GapCenter;
            public bool Passed;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            public SpriteRenderer DebugTop;
            public SpriteRenderer DebugBottom;
#endif
        }

        private sealed class AmbientStar
        {
            public Transform Transform;
            public float X;
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
            public float Phase;
            public float RespawnTimer;
            public Vector3 ArtworkBaseScale;
            public bool Active;
        }

        private const float CameraHeight = 18f;
        private const float GroundY = -6.82f;
        private const float BirdX = -2.45f;
        private const float BirdCollisionRadius = .27f;
        private const float BirdDisplayWidth = 2.26f;
        private const float PipeWidth = .92f;
        private const float PipeSpacing = 6.65f;
        private const int PipeCount = 4;
        private const int PowerUpCount = 3;
        private const float PickupRadius = .43f;
        private const float SimulationStep = 1f / 120f;
        private const float MaximumSimulationCatchup = 1f / 12f;
        // The Aetherwing poses are deliberately stepped rather than cross-faded: a
        // full-body fade reads as a blurry double bird on a phone. Smooth transform
        // motion joins the three crisp poses instead.
        private const float WingCycleSeconds = .42f;
        private const float WingLiftPhase = .31f;
        private const float WingDownstrokeDelay = .075f;
        private const float WingDownstrokeSpan = .90f;
        private const float WingLiftPoseThreshold = .32f;
        private const float WingDownstrokePoseThreshold = .14f;
        // Every theme uses the same authored three-pose Aetherwing silhouette. Themes
        // change the material tint, trail, gates and world—not the bird's anatomy or
        // visual quality—so a player never unlocks a lower-fidelity mascot.
        private const string AetherwingGlidePath = "SkyPulse/characters/aetherwing_v2/aetherwing-glide-v3";
        private const string AetherwingFlapPath = "SkyPulse/characters/aetherwing_v2/aetherwing-downstroke-v3";
        private const string AetherwingRisePath = "SkyPulse/characters/aetherwing_v2/aetherwing-lift-v3";

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
            collisionRadius: BirdCollisionRadius, perfectPassWindow: .32f, inputBufferSeconds: .095f, maximumGapCenterStep: .90f,
            powerUpSlots: PowerUpCount, powerUpRespawnMinimum: 5.5f, powerUpRespawnMaximum: 8.5f,
            allowsUpgrades: true, allowsPowerUps: true);

        private static readonly Skin[] Skins =
        {
            new Skin("nova", "NOVA", AetherwingGlidePath, AetherwingFlapPath, "#8f64ff", "#45eaff", 0, AetherwingRisePath),
            new Skin("lumen", "LUMEN", AetherwingGlidePath, AetherwingFlapPath, "#45eaff", "#8f64ff", 24, AetherwingRisePath),
            new Skin("ember", "EMBER", AetherwingGlidePath, AetherwingFlapPath, "#f05bc6", "#ffc34d", 32, AetherwingRisePath),
            new Skin("sol", "SOL", AetherwingGlidePath, AetherwingFlapPath, "#ffc34d", "#45eaff", 40, AetherwingRisePath),
            new Skin("aurora", "AURORA", AetherwingGlidePath, AetherwingFlapPath, "#61f5b3", "#45eaff", 48, AetherwingRisePath),
            new Skin("orchid", "ORCHID", AetherwingGlidePath, AetherwingFlapPath, "#b17cff", "#f05bc6", 52, AetherwingRisePath),
            new Skin("coral", "CORAL", AetherwingGlidePath, AetherwingFlapPath, "#f082af", "#ffc34d", 56, AetherwingRisePath),
            new Skin("glacier", "GLACIER", AetherwingGlidePath, AetherwingFlapPath, "#edf7ff", "#45eaff", 60, AetherwingRisePath),
            new Skin("prism", "PRISM", AetherwingGlidePath, AetherwingFlapPath, "#45eaff", "#edf7ff", 68, AetherwingRisePath),
            new Skin("verdant", "VERDANT", AetherwingGlidePath, AetherwingFlapPath, "#61f5b3", "#45eaff", 72, AetherwingRisePath),
            new Skin("cinder", "CINDER", AetherwingGlidePath, AetherwingFlapPath, "#f05bc6", "#ffc34d", 76, AetherwingRisePath),
            new Skin("tide", "TIDE", AetherwingGlidePath, AetherwingFlapPath, "#45eaff", "#8f64ff", 80, AetherwingRisePath),
            new Skin("wisp", "WISP", AetherwingGlidePath, AetherwingFlapPath, "#edf7ff", "#45eaff", 88, AetherwingRisePath),
            new Skin("bloom", "BLOOM", AetherwingGlidePath, AetherwingFlapPath, "#f05bc6", "#b17cff", 92, AetherwingRisePath),
            new Skin("emberwing", "EMBERWING", AetherwingGlidePath, AetherwingFlapPath, "#ffc34d", "#f05bc6", 100, AetherwingRisePath),
            new Skin("steel", "STEEL", AetherwingGlidePath, AetherwingFlapPath, "#edf7ff", "#45eaff", 108, AetherwingRisePath),
        };

        private static readonly WorldTheme[] Worlds =
        {
            new WorldTheme("neon_city", "NEON CITY", "SkyPulse/backgrounds/neon-flightdeck-v1", "#45eaff", "#0a0522", "EASY", .88f, 5.10f, "ion", "pulse"),
            new WorldTheme("aurora_rise", "AURORA RISE", "SkyPulse/backgrounds/themes/aurora-rise-v2", "#61f5b3", "#05251e", "EASY", .94f, 4.92f, "frost", "aurora"),
            new WorldTheme("solar_drift", "SOLAR DRIFT", "SkyPulse/backgrounds/themes/solar-drift-v2", "#ffc34d", "#2b0d10", "CLASSIC", 1f, 4.46f, "solar", "solar"),
            new WorldTheme("midnight_tide", "MIDNIGHT TIDE", "SkyPulse/backgrounds/themes/midnight-tide-v2", "#45eaff", "#07113d", "CLASSIC", 1.04f, 4.30f, "cobalt", "seaglass"),
            new WorldTheme("velvet_dawn", "VELVET DAWN", "SkyPulse/backgrounds/themes/velvet-dawn-v3", "#f05bc6", "#26051f", "ADVANCED", 1.08f, 4.14f, "rose", "sakura"),
            new WorldTheme("crystal_night", "CRYSTAL NIGHT", "SkyPulse/backgrounds/themes/crystal-night-v2", "#edf7ff", "#071239", "ADVANCED", 1.12f, 4.00f, "prism", "glacial"),
            new WorldTheme("jade_horizon", "JADE HORIZON", "SkyPulse/backgrounds/themes/jade-horizon-v2", "#61f5b3", "#063523", "EXPERT", 1.18f, 3.86f, "jade", "mintwave"),
            new WorldTheme("violet_rain", "VIOLET RAIN", "SkyPulse/backgrounds/themes/violet-rain-v2", "#b17cff", "#210842", "EXPERT", 1.24f, 3.72f, "amethyst", "nebula"),
            new WorldTheme("eclipse", "ECLIPSE", "SkyPulse/backgrounds/themes/eclipse-v2", "#b17cff", "#10051f", "APEX", 1.32f, 3.56f, "obsidian", "starlight"),
            new WorldTheme("night_circuit", "NIGHT CIRCUIT", "SkyPulse/backgrounds/themes/night-circuit-v3", "#f05bc6", "#12092b", "APEX", 1.38f, 3.42f, "emberline", "voltage"),
        };

        private static readonly Upgrade[] Upgrades =
        {
            new Upgrade("thrust_plumes", "THRUST PLUMES", "+10% flap lift", 38, "#45eaff"),
            new Upgrade("featherweight", "FEATHERWEIGHT", "-8% gravity", 44, "#61f5b3"),
            new Upgrade("air_brakes", "AIR BRAKES", "Softer maximum fall speed", 48, "#edf7ff"),
            new Upgrade("rescue_feather", "RESCUE FEATHER", "One extra life each flight", 74, "#f05bc6"),
            new Upgrade("time_weaver", "TIME WEAVER", "Slow Field lasts +2 seconds", 58, "#b17cff"),
            new Upgrade("shield_cell", "SHIELD CELL", "Begin flights shielded", 82, "#61f5b3"),
            new Upgrade("cache_cores", "CACHE CORES", "Crystal Cache grants +8", 66, "#ffc34d"),
            new Upgrade("magnet_array", "MAGNET ARRAY", "A stronger, wider pickup pull", 62, "#45eaff"),
            new Upgrade("phase_stabilizer", "PHASE STABILIZER", "Phase Shift lasts +1.5 seconds", 76, "#b17cff"),
            new Upgrade("prism_resonator", "PRISM RESONATOR", "Score Prism grants extra crystals", 70, "#f05bc6"),
            new Upgrade("comet_trail", "COMET TRAIL", "A broader living light trail", 46, "#edf7ff"),
            new Upgrade("starheart", "STARHEART", "Bonus crystals every four gates", 86, "#ffc34d"),
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
        private readonly PowerUpPickup[] powerUpPool = new PowerUpPickup[PowerUpCount];
        private readonly Vector3[] trailPoints = new Vector3[9];
        private readonly List<AmbientStar> ambientStars = new List<AmbientStar>();
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private readonly HashSet<string> ownedSkinIds = new HashSet<string>();
        private readonly HashSet<string> ownedUpgradeIds = new HashSet<string>();

        private FlightState state;
        private CosmeticCategory cosmeticCategory;
        private Camera flightCamera;
        private Sprite whiteSprite;
        private Sprite midnightSprite;
        private Sprite softCircleSprite;
        private Sprite ringSprite;
        private Sprite roundedPanelSprite;
        private Sprite emergencyBirdSprite;
        private Sprite idleBirdSprite;
        private Sprite flapBirdSprite;
        private Sprite riseBirdSprite;
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer backgroundVeil;
        private SpriteRenderer floorBase;
        private SpriteRenderer floorSurface;
        private SpriteRenderer floorLip;
        private SpriteRenderer floorGlow;
        private Transform bird;
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
        private SpriteRenderer shieldAuraRenderer;
        private SpriteRenderer slowAuraRenderer;
        private SpriteRenderer effectAuraRenderer;
        private LineRenderer trailGlow;
        private LineRenderer trailCore;
        private AudioSource audioSource;
        private AudioClip flapSound;
        private AudioClip scoreSound;
        private AudioClip crashSound;
        private AudioClip crystalSound;
        private AudioClip unlockSound;
        private Font uiFont;

        private GameObject uiRoot;
        private RectTransform safeAreaRoot;
        private Rect appliedSafeArea;
        private Vector2Int appliedScreenSize;
        private GameObject homeScreen;
        private GameObject hudScreen;
        private GameObject pauseScreen;
        private GameObject gameOverScreen;
        private GameObject customizeScreen;
        private GameObject purchaseModal;
        private Text menuCrystalText;
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

        private Skin equippedSkin;
        private WorldTheme equippedWorld;
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
        private float menuWingTimer;
        private float menuPresentationTime;
        private float spawnX;
        private float scoreBurstTimer;
        private float ambientTime;
        private float slowFieldTimer;
        private float shieldFlashTimer;
        private float skySurgeTimer;
        private float scorePrismTimer;
        private float magnetHaloTimer;
        private float phaseShiftTimer;
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
            CreateCamera();
            CreateVisuals();
            CreateInterface();
            ApplyEquippedVisuals();
            UpdateComfortCopy();
            ResetToMenu();
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

            backgroundRenderer = CreateRenderer("Cinematic world", LoadSprite("SkyPulse/backgrounds/neon-flightdeck-v1") ?? midnightSprite, Color.white, -40);
            backgroundRenderer.transform.position = new Vector3(0f, .12f, 0f);
            FitBackgroundToCamera(backgroundRenderer, .5f);

            backgroundVeil = CreateRenderer("World colour veil", whiteSprite, new Color(.015f, .01f, .08f, .20f), -39);
            backgroundVeil.transform.position = new Vector3(0f, .1f, 0f);
            backgroundVeil.transform.localScale = new Vector3(GetWorldWidth() + 1f, CameraHeight + .5f, 1f);

            CreateAmbientStars();
            CreateFloor();
            CreateBird();
            CreateFlightFeedback();
            CreateTrail();
            for (var index = 0; index < pipePool.Length; index += 1) pipePool[index] = CreatePipePair(index);
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
                var x = Mathf.Lerp(-GetWorldWidth() * .48f, GetWorldWidth() * .48f, (float)random.NextDouble());
                var y = Mathf.Lerp(-5.8f, 8.3f, (float)random.NextDouble());
                var size = Mathf.Lerp(.016f, .034f, (float)random.NextDouble());
                star.transform.position = new Vector3(x, y, 0f);
                star.transform.localScale = Vector3.one * size;
                ambientStars.Add(new AmbientStar
                {
                    Transform = star.transform,
                    X = x,
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
            floorBase = CreateRenderer("Solid floor base", whiteSprite, Hex("#02030d"), -9);
            floorBase.transform.position = new Vector3(0f, GroundY - 1.1f, 0f);
            floorBase.transform.localScale = new Vector3(width, 2.24f, 1f);

            floorSurface = CreateRenderer("Floor material", whiteSprite, new Color(.03f, .05f, .15f, .62f), -8);
            floorSurface.transform.position = new Vector3(0f, GroundY - 1.04f, 0f);
            floorSurface.transform.localScale = new Vector3(width, 1.90f, 1f);

            floorLip = CreateRenderer("Floor solid edge", whiteSprite, new Color(.05f, .10f, .25f, .85f), -7);
            floorLip.transform.position = new Vector3(0f, GroundY - .08f, 0f);
            floorLip.transform.localScale = new Vector3(width, .12f, 1f);

            floorGlow = CreateRenderer("Floor energy rail", whiteSprite, new Color(.27f, .86f, 1f, .38f), -6);
            floorGlow.transform.position = new Vector3(0f, GroundY + .035f, 0f);
            floorGlow.transform.localScale = new Vector3(width, .026f, 1f);

            var floorHighlight = CreateRenderer("Floor edge highlight", whiteSprite, new Color(.78f, .94f, 1f, .18f), -5);
            floorHighlight.transform.position = new Vector3(0f, GroundY + .105f, 0f);
            floorHighlight.transform.localScale = new Vector3(width, .010f, 1f);
        }

        private void CreateBird()
        {
            bird = new GameObject("Flight bird").transform;
            bird.SetParent(transform, false);
            // This compact, opaque inner silhouette is deliberately independent of
            // imported artwork. It gives every bird a readable body against bright
            // worlds and makes a missing/unsupported texture impossible to turn the
            // player avatar invisible on a phone.
            if (emergencyBirdSprite == null) emergencyBirdSprite = CreateEmergencyBirdSprite();
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
            birdSafetyRenderer.sortingOrder = 13;
            birdSafetyRenderer.color = Color.clear;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            collisionBirdDebug = CreateRenderer("Bird collision guide", ringSprite, new Color(.35f, 1f, .72f, .78f), 31, bird);
            collisionBirdDebug.enabled = false;
#endif
        }

        private void CreateFlightFeedback()
        {
            flightFeedbackRenderer = CreateRenderer("Flight feedback bloom", softCircleSprite, new Color(1f, 1f, 1f, 0f), 30);
            flightFeedbackRenderer.enabled = false;
        }

        private PowerUpPickup CreatePowerUp(int index)
        {
            var root = new GameObject($"Power-up pickup {index + 1}");
            root.transform.SetParent(transform, false);
            var glow = CreateRenderer("Power-up halo", softCircleSprite, Color.white, 10, root.transform);
            glow.transform.localScale = Vector3.one * 1.12f;
            var depth = CreateRenderer("Power-up dimensional bloom", softCircleSprite, Color.white, 12, root.transform);
            depth.transform.localScale = Vector3.one * 1.22f;
            var artwork = CreateRenderer("Premium power-up artwork", whiteSprite, Color.white, 13, root.transform);
            var spark = CreateRenderer("Power-up glint", softCircleSprite, Color.white, 14, root.transform);
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
            pair.DebugTop = CreateRenderer("Top collision guide", whiteSprite, new Color(1f, .26f, .55f, .13f), 29, root.transform);
            pair.DebugBottom = CreateRenderer("Bottom collision guide", whiteSprite, new Color(1f, .26f, .55f, .13f), 29, root.transform);
            pair.DebugTop.enabled = false;
            pair.DebugBottom.enabled = false;
#endif
            return pair;
        }

        private PipeSurface CreatePipeSurface(Transform parent, string label)
        {
            var top = label.StartsWith("Top", StringComparison.Ordinal);
            return new PipeSurface
            {
                Artwork = CreateRenderer($"{label} artwork", LoadSprite(top ? "SkyPulse/art/pipe-top-v2" : "SkyPulse/art/pipe-bottom-v2"), Color.white, 6, parent),
                Outer = CreateRenderer($"{label} outer", whiteSprite, Hex("#030613"), 2, parent),
                Panel = CreateRenderer($"{label} panel", whiteSprite, Hex("#0b3076"), 3, parent),
                Shade = CreateRenderer($"{label} shadow", whiteSprite, new Color(0f, 0f, 0f, .28f), 4, parent),
                Highlight = CreateRenderer($"{label} edge highlight", whiteSprite, new Color(.8f, .95f, 1f, .18f), 4, parent),
                Energy = CreateRenderer($"{label} energy core", whiteSprite, Hex("#45eaff"), 5, parent),
                Scan = CreateRenderer($"{label} scan line", whiteSprite, new Color(.27f, .92f, 1f, 0f), 6, parent),
                Beacon = CreateRenderer($"{label} gateway beacon", ringSprite, new Color(.27f, .92f, 1f, 0f), 10, parent),
                CapOuter = CreateRenderer($"{label} cap shell", whiteSprite, Hex("#030613"), 6, parent),
                CapAccent = CreateRenderer($"{label} cap accent", whiteSprite, Hex("#45eaff"), 7, parent),
                CapPanel = CreateRenderer($"{label} cap panel", whiteSprite, Hex("#0b3076"), 8, parent),
                CapEnergy = CreateRenderer($"{label} cap energy", whiteSprite, Hex("#45eaff"), 9, parent),
            };
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

        private GameObject CreateHomeScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Home screen");
            // The home screen is a clear flight deck, not a frosted layer over the
            // world. Keep the world visible, then give controls a solid place to sit.
            CreateFullPanel(root.transform, "Home contrast veil", new Color(.005f, .012f, .05f, .10f));

            var difficulty = CreateNeonButton(root.transform, "CLASSIC", new Vector2(-365f, 790f), new Vector2(202f, 64f), Hex("#8f64ff"));
            difficultyText = difficulty.GetComponentInChildren<Text>();
            difficultyText.resizeTextForBestFit = true;
            difficultyText.resizeTextMinSize = 13;
            difficultyText.resizeTextMaxSize = 20;
            difficulty.onClick.AddListener(CycleFlightMode);
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
            menuBirdSafetyImage = CreateImage(menuHeroTransform, "Menu bird visibility silhouette", Vector2.zero, new Vector2(630f, 304f), Color.clear);
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

            menuBestText = CreateChip(root.transform, new Vector2(0f, -160f), "BEST · 0", Hex("#8fa7c4"));
            menuModeDetailText = CreateText(root.transform, "FAIR FLIGHT · NO POWER UPS", new Vector2(0f, -211f), new Vector2(700f, 32f), 16, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var fly = CreateNeonButton(root.transform, "FLY", new Vector2(0f, -292f), new Vector2(592f, 108f), Hex("#f05bc6"));
            fly.onClick.AddListener(StartFlight);
            CreateText(root.transform, "TAP ANYWHERE TO TAKE FLIGHT", new Vector2(0f, -370f), new Vector2(650f, 34f), 15, new Color(.91f, .92f, 1f, .68f), TextAnchor.MiddleCenter, FontStyle.Bold);

            var customize = CreateNeonButton(root.transform, "CUSTOMIZE", new Vector2(0f, -456f), new Vector2(592f, 78f), Hex("#45eaff"));
            customize.onClick.AddListener(OpenCustomize);
            menuDailyText = CreateText(root.transform, "DAILY ROUTE · SHARED & FAIR", new Vector2(0f, -538f), new Vector2(650f, 40f), 19, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            menuDailyText.raycastTarget = true;
            var dailyButton = menuDailyText.gameObject.AddComponent<Button>();
            dailyButton.targetGraphic = menuDailyText;
            dailyButton.onClick.AddListener(StartDailyFlight);
            menuEquippedText = CreateText(root.transform, "EQUIPPED  ·  NOVA", new Vector2(0f, -600f), new Vector2(650f, 36f), 16, Hex("#8f64ff"), TextAnchor.MiddleCenter, FontStyle.Bold);
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
            hudModeText = CreateText(root.transform, "CLASSIC · FAIR FLIGHT", new Vector2(0f, 652f), new Vector2(600f, 30f), 16, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            scoreBurstText = CreateText(root.transform, "+1", new Vector2(0f, 612f), new Vector2(220f, 70f), 34, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            scoreBurstText.gameObject.SetActive(false);
            hudPowerUpText = CreateText(root.transform, "", new Vector2(0f, 576f), new Vector2(600f, 38f), 19, Hex("#61f5b3"), TextAnchor.MiddleCenter, FontStyle.Bold);
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
            var card = CreatePanel(root.transform, "Game over card", new Vector2(0f, 25f), new Vector2(820f, 700f), Hex("#11132a"));
            AddOutline(card.gameObject, Hex("#8f64ff"), 3.5f);
            CreateText(card, "GAME OVER", new Vector2(0f, 235f), new Vector2(720f, 78f), 54, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultNewBestText = CreateText(card, "NEW BEST", new Vector2(0f, 170f), new Vector2(500f, 42f), 25, Hex("#ffc34d"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultReasonText = CreateText(card, "GATE IMPACT", new Vector2(0f, 130f), new Vector2(600f, 30f), 16, Hex("#f05bc6"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultModeText = CreateText(card, "CLASSIC · FAIR FLIGHT", new Vector2(0f, 94f), new Vector2(600f, 30f), 16, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultScoreText = CreateText(card, "SCORE  0", new Vector2(0f, 50f), new Vector2(600f, 54f), 31, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultBestText = CreateText(card, "BEST  0", new Vector2(0f, -7f), new Vector2(600f, 45f), 24, new Color(.93f, .95f, 1f, .78f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var flyAgain = CreateNeonButton(card, "FLY AGAIN", new Vector2(0f, -105f), new Vector2(570f, 88f), Hex("#f05bc6"));
            flyAgain.onClick.AddListener(RestartFlight);
            var menu = CreateNeonButton(card, "MENU", new Vector2(0f, -215f), new Vector2(570f, 70f), Hex("#45eaff"));
            menu.onClick.AddListener(ResetToMenu);
            return root;
        }

        private GameObject CreateCustomizeScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Customize screen");
            CreateFullPanel(root.transform, "Customize veil", new Color(.01f, .006f, .05f, .48f));
            var back = CreateNeonButton(root.transform, "‹  MENU", new Vector2(-390f, 802f), new Vector2(220f, 68f), Hex("#8f64ff"));
            back.onClick.AddListener(ResetToMenu);
            CreateChip(root.transform, new Vector2(365f, 802f), "✦  0", Hex("#45eaff"));
            customizeTitle = CreateText(root.transform, "CUSTOMIZE", new Vector2(0f, 690f), new Vector2(720f, 80f), 48, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            CreateText(root.transform, "CHOOSE YOUR FLIGHT STYLE", new Vector2(0f, 638f), new Vector2(720f, 38f), 18, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);

            var labels = new[] { "BIRDS", "WORLDS", "TRAILS", "PIPES", "TECH" };
            var categories = new[] { CosmeticCategory.Birds, CosmeticCategory.Worlds, CosmeticCategory.Trails, CosmeticCategory.Pipes, CosmeticCategory.Upgrades };
            for (var index = 0; index < labels.Length; index += 1)
            {
                var tab = CreateNeonButton(root.transform, labels[index], new Vector2(-360f + index * 180f, 560f), new Vector2(166f, 60f), index == 0 ? Hex("#45eaff") : Hex("#8f64ff"));
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

        private void Update()
        {
            ApplySafeArea();
            var frameDelta = Mathf.Min(Time.unscaledDeltaTime, MaximumSimulationCatchup);
            ambientTime += frameDelta;
            UpdateAmbientVisuals();
            UpdateMenuBird(frameDelta);
            UpdateScoreBurst(frameDelta);
            UpdateFlightFeedback(frameDelta);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UpdateDevelopmentQualityControls();
#endif

#if UNITY_EDITOR
            UpdateEditorQualityHarness(frameDelta);
#endif

            if (state == FlightState.Menu)
            {
                if (WasTapped() && !PointerOverUi()) StartFlight();
                return;
            }

            if (state == FlightState.GameOver)
            {
                // A tap outside the result card has the same promise as the explicit
                // FLY AGAIN button: retain the current route, including Daily.
                if (WasTapped() && !PointerOverUi()) RestartFlight();
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
            UpdateBird(deltaTime);
            if (state != FlightState.Playing) return;
            UpdatePipes(deltaTime);
            if (state != FlightState.Playing) return;
            UpdatePowerUps(deltaTime);
            UpdateTrail(deltaTime);
        }

        private void BufferFlapInput()
        {
            bufferedFlapUntil = Time.unscaledTime + ActiveTuning().InputBufferSeconds;
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
            if (premiumRig)
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
            var flightTilt = premiumRig ? hover * .75f - flapStrength * .85f : hover * 2.2f - flapStrength * 1.6f;
            var glideLean = (premiumRig ? Mathf.Sin(ambientTime * 1.16f) * .24f : Mathf.Sin(ambientTime * 1.16f) * .65f) * menuMotion;
            var intro = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(menuPresentationTime / .48f));
            if (menuHeroTransform != null)
            {
                menuHeroTransform.anchoredPosition = new Vector2(Mathf.Sin(ambientTime * .82f) * (premiumRig ? 3f : 8f) * menuMotion, 148f + hover * (premiumRig ? 5f : 13f));
                menuHeroTransform.localScale = Vector3.one * Mathf.Lerp(.94f, 1f + Mathf.Sin(ambientTime * 3.4f) * (premiumRig ? .006f : .018f) * menuMotion, intro);
            }
            menuBirdTransform.localRotation = Quaternion.Euler(0f, 0f, flightTilt + glideLean);
            menuBirdTransform.localScale = premiumRig
                ? new Vector3(1f + riseStrength * .012f, 1f - riseStrength * .008f, 1f)
                : new Vector3(1f + Mathf.Sin(ambientTime * 3.4f) * .018f + riseStrength * .035f, 1f - riseStrength * .018f, 1f);
            if (menuBirdShadowImage != null)
            {
                // A crisp, offset silhouette gives the hero real spatial separation
                // from the deck without a bloom, blur, or translucent glass effect.
                var showPremiumDepth = premiumRig && menuBirdShadowImage.sprite != null;
                menuBirdShadowImage.enabled = showPremiumDepth;
                menuBirdShadowImage.color = showPremiumDepth ? new Color(.004f, .010f, .040f, .48f) : Color.clear;
                menuBirdShadowImage.rectTransform.anchoredPosition = premiumRig
                    ? new Vector2(-17f - riseStrength * 3f, -16f - flapStrength * 2f)
                    : Vector2.zero;
                menuBirdShadowImage.rectTransform.localRotation = menuBirdTransform.localRotation;
                menuBirdShadowImage.rectTransform.localScale = premiumRig
                    ? menuBirdTransform.localScale * 1.012f
                    : Vector3.one;
            }
            if (menuBirdSafetyImage != null)
            {
                // The readable core sits inside the authored art rather than around
                // it, so it behaves as depth when the texture is present and as a
                // complete, polished bird if an import ever fails.
                menuBirdSafetyImage.enabled = true;
                menuBirdSafetyImage.color = BirdSafetyColour();
                menuBirdSafetyImage.rectTransform.localRotation = menuBirdTransform.localRotation;
                menuBirdSafetyImage.rectTransform.localScale = menuBirdTransform.localScale * (1f + riseStrength * .018f);
            }
            if (menuBirdEyeGlintImage != null)
            {
                // Aetherwing has a small, authored visor light. The old floating UI
                // glint belongs only to the legacy round-eyed birds.
                menuBirdEyeGlintImage.gameObject.SetActive(!UsesAetherwing());
                if (!UsesAetherwing())
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
            if (scoreBurstTimer <= 0f || scoreBurstText == null) return;
            scoreBurstTimer -= deltaTime;
            if (scoreBurstTimer <= 0f)
            {
                scoreBurstText.gameObject.SetActive(false);
                return;
            }

            var t = 1f - scoreBurstTimer / .36f;
            scoreBurstText.rectTransform.anchoredPosition = new Vector2(0f, 612f + t * 44f);
            var color = equippedSkin.Accent;
            color.a = Mathf.Clamp01(scoreBurstTimer / .13f);
            scoreBurstText.color = color;
        }

        private void UpdateBird(float deltaTime)
        {
            var collisionRadius = ActiveTuning().CollisionRadius;
            birdVelocity = Mathf.Max(ActiveMaxFallVelocity(), birdVelocity + ActiveGravity() * deltaTime);
            birdY += birdVelocity * deltaTime;
            wingTimer = Mathf.Min(WingCycleSeconds, wingTimer + deltaTime);
            bird.position = new Vector3(BirdX, birdY, 0f);
            var flapKick = Mathf.Exp(-wingTimer * 12f);
            var targetTilt = Mathf.Clamp(birdVelocity * 3.55f + flapKick * 5.5f, -37f, 28f);
            birdTilt = Mathf.SmoothDamp(birdTilt, targetTilt, ref birdTiltVelocity, .075f, 360f, deltaTime);
            bird.rotation = Quaternion.Euler(0f, 0f, birdTilt);
            UpdateBirdWingMotion();

            if (birdY + collisionRadius >= CameraHeight * .5f || birdY - collisionRadius <= GroundY)
            {
                if (!UseShield())
                {
                    lastCrashReason = birdY + collisionRadius >= CameraHeight * .5f ? "CEILING CONTACT" : "GROUND CONTACT";
                    EndFlight();
                    return;
                }

                birdY = Mathf.Clamp(birdY, GroundY + collisionRadius + .14f, CameraHeight * .5f - collisionRadius - .14f);
                birdVelocity = ActiveFlapVelocity() * .52f;
                bird.position = new Vector3(BirdX, birdY, 0f);
            }
        }

        private void UpdatePipes(float deltaTime)
        {
            var speed = ActiveScrollSpeed();
            var collisionRadius = ActiveTuning().CollisionRadius;
            var furthestX = float.MinValue;
            foreach (var pair in pipePool) if (pair.X > furthestX) furthestX = pair.X;

            foreach (var pair in pipePool)
            {
                pair.X -= speed * deltaTime;
                pair.Root.transform.localPosition = new Vector3(pair.X, 0f, 0f);
                if (pair.X < -GetWorldWidth() * .5f - PipeWidth)
                {
                    ConfigurePipe(pair, furthestX + PipeSpacing);
                }
                furthestX = Mathf.Max(furthestX, pair.X);
                AnimatePipePair(pair);

                // The cap is part of the obstacle too: if the art is visible, it is dangerous.
                var physicalWidth = PipeWidth + .25f;
                var overlapsPipe = BirdX + collisionRadius > pair.X - physicalWidth * .5f && BirdX - collisionRadius < pair.X + physicalWidth * .5f;
                var halfGap = ActiveGap() * .5f;
                var hitsPipe = birdY + collisionRadius > pair.GapCenter + halfGap || birdY - collisionRadius < pair.GapCenter - halfGap;
                if (phaseShiftTimer <= 0f && overlapsPipe && hitsPipe)
                {
                    if (!UseShield())
                    {
                        lastCrashReason = "GATE IMPACT";
                        EndFlight();
                        return;
                    }

                    pair.X = BirdX - physicalWidth - .22f;
                    pair.Root.transform.localPosition = new Vector3(pair.X, 0f, 0f);
                    pair.Passed = true;
                    continue;
                }

                if (!pair.Passed && pair.X + PipeWidth * .5f < BirdX - collisionRadius)
                {
                    pair.Passed = true;
                    score += 1;
                    var crystalReward = 1;
                    var perfect = Mathf.Abs(birdY - pair.GapCenter) <= ActiveTuning().PerfectPassWindow;
                    if (perfect)
                    {
                        perfectPasses += 1;
                        crystalReward += 1;
                    }
                    if (scorePrismTimer > 0f) crystalReward += AllowsGameplayUpgrades() && HasUpgrade("prism_resonator") ? 2 : 1;
                    if (AllowsGameplayUpgrades() && HasUpgrade("starheart"))
                    {
                        gatesSinceStarheart += 1;
                        if (gatesSinceStarheart >= 4)
                        {
                            crystalReward += 4;
                            gatesSinceStarheart = 0;
                        }
                    }
                    crystals += crystalReward;
                    hudScoreText.text = score.ToString();
                    AdvanceFlightCoach();
                    ShowScoreBurst(crystalReward, perfect);
                    TriggerFlightFeedback(perfect ? equippedSkin.Accent : Hex("#45eaff"), perfect ? .26f : .13f);
                    if (perfect) PulseHaptic(.10f);
                    Play(scoreSound);
                    UpdateCrystalLabels();
                }
            }
        }

        private float ActiveScrollSpeed()
        {
            var tuning = ActiveTuning();
            var speed = tuning.StartingScrollSpeed + Mathf.Min(score, 24) * tuning.ScrollRampPerGate;
            // Adventure worlds retain their authored intensity. In Classic and Daily a
            // world is visual expression only, so every pilot flies the same route.
            if (flightMode == FlightMode.Adventure && equippedWorld != null) speed *= equippedWorld.ScrollMultiplier;
            return slowFieldTimer > 0f ? speed * .52f : speed;
        }

        private float ActiveGravity()
        {
            var gravity = ActiveTuning().Gravity;
            if (AllowsGameplayUpgrades() && HasUpgrade("featherweight")) gravity *= .92f;
            if (skySurgeTimer > 0f) gravity *= .64f;
            return gravity;
        }

        private float ActiveMaxFallVelocity()
        {
            var maximum = ActiveTuning().MaxFallVelocity;
            return AllowsGameplayUpgrades() && HasUpgrade("air_brakes") ? maximum * .88f : maximum;
        }

        private float ActiveFlapVelocity()
        {
            var lift = ActiveTuning().FlapVelocity;
            if (AllowsGameplayUpgrades() && HasUpgrade("thrust_plumes")) lift *= 1.10f;
            if (skySurgeTimer > 0f) lift *= 1.18f;
            return lift;
        }

        private FlightTuning ActiveTuning()
        {
            return flightMode == FlightMode.Adventure ? AdventureTuning : ClassicTuning;
        }

        private bool AllowsGameplayUpgrades()
        {
            return ActiveTuning().AllowsUpgrades;
        }

        private bool AllowsPowerUps()
        {
            return ActiveTuning().AllowsPowerUps;
        }

        private void UpdatePowerUps(float deltaTime)
        {
            if (!AllowsPowerUps())
            {
                foreach (var pickup in powerUpPool)
                {
                    if (pickup.Active || pickup.Root.activeSelf) DeferPowerUp(pickup, 0f);
                }
                return;
            }

            foreach (var pickup in powerUpPool)
            {
                if (!pickup.Active)
                {
                    pickup.RespawnTimer -= deltaTime;
                    if (pickup.RespawnTimer <= 0f)
                    {
                        var gate = FindAvailablePowerUpGate(pickup);
                        if (gate != null) ConfigurePowerUp(pickup, gate);
                    }
                    continue;
                }

                // A pickup belongs to a live gate, so it is always presented in open air
                // rather than floating into a pipe body or spawning at a random height.
                if (pickup.Gate == null || !pickup.Gate.Root.activeSelf || pickup.Gate.Passed)
                {
                    DeferPowerUp(pickup, RouteRange(1.15f, 2.1f));
                    continue;
                }

                var targetX = pickup.Gate.X;
                var targetY = pickup.Gate.GapCenter + pickup.GapOffset;
                if (magnetHaloTimer > 0f)
                {
                    var pullSpeed = HasUpgrade("magnet_array") ? 8.8f : 6.2f;
                    pickup.X = Mathf.MoveTowards(pickup.X, BirdX, pullSpeed * deltaTime);
                    pickup.Y = Mathf.MoveTowards(pickup.Y, birdY, pullSpeed * .72f * deltaTime);
                }
                else
                {
                    pickup.X = targetX;
                    pickup.Y = targetY;
                }
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
                DeferPowerUp(pickup, 1f);
                return;
            }
            pickup.Root.SetActive(true);
            pickup.Active = true;
            pickup.RespawnTimer = 0f;
            pickup.Gate = gate;
            pickup.X = gate.X;
            var safeGapOffset = Mathf.Max(.28f, ActiveGap() * .5f - .92f);
            pickup.GapOffset = RouteRange(-safeGapOffset, safeGapOffset);
            pickup.Y = gate.GapCenter + pickup.GapOffset;
            pickup.Phase = RouteRange(0f, Mathf.PI * 2f);
            pickup.Kind = (PowerUpKind)RouteRange(0, 7);
            var colour = Hex("#8f64ff");
            var secondary = Hex("#45eaff");
            switch (pickup.Kind)
            {
                case PowerUpKind.PulseShield:
                    colour = Hex("#61f5b3");
                    secondary = Hex("#edf7ff");
                    break;
                case PowerUpKind.CrystalCache:
                    colour = Hex("#ffc34d");
                    secondary = Hex("#f05bc6");
                    break;
                case PowerUpKind.SkySurge:
                    colour = Hex("#ffc34d");
                    secondary = Hex("#edf7ff");
                    break;
                case PowerUpKind.ScorePrism:
                    colour = Hex("#f05bc6");
                    secondary = Hex("#edf7ff");
                    break;
                case PowerUpKind.MagnetHalo:
                    colour = Hex("#45eaff");
                    secondary = Hex("#61f5b3");
                    break;
                case PowerUpKind.PhaseShift:
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
                case PowerUpKind.PulseShield: return "SkyPulse/art/powerups/generated/pulse-shield-v2";
                case PowerUpKind.CrystalCache: return "SkyPulse/art/powerups/generated/crystal-cache-v2";
                case PowerUpKind.SkySurge: return "SkyPulse/art/powerups/generated/sky-surge-v2";
                case PowerUpKind.ScorePrism: return "SkyPulse/art/powerups/generated/score-prism-v2";
                case PowerUpKind.MagnetHalo: return "SkyPulse/art/powerups/generated/magnet-halo-v2";
                case PowerUpKind.PhaseShift: return "SkyPulse/art/powerups/generated/phase-shift-v2";
                default: return "SkyPulse/art/powerups/generated/slow-field-v2";
            }
        }

        private void CollectPowerUp(PowerUpPickup pickup)
        {
            pickup.Active = false;
            pickup.Root.SetActive(false);
            var tuning = ActiveTuning();
            pickup.RespawnTimer = RouteRange(tuning.PowerUpRespawnMinimum, tuning.PowerUpRespawnMaximum);
            TriggerFlightFeedback(pickup.Glow.color, .22f);
            PulseHaptic(.08f);
            switch (pickup.Kind)
            {
                case PowerUpKind.SlowField:
                    slowFieldTimer = Mathf.Min(11f, Mathf.Max(slowFieldTimer, 0f) + (AllowsGameplayUpgrades() && HasUpgrade("time_weaver") ? 7.5f : 5.5f));
                    Play(crystalSound);
                    break;
                case PowerUpKind.PulseShield:
                    shieldCharges = 1;
                    shieldFlashTimer = .6f;
                    Play(unlockSound);
                    break;
                case PowerUpKind.SkySurge:
                    skySurgeTimer = Mathf.Min(9f, Mathf.Max(skySurgeTimer, 0f) + 5f);
                    Play(unlockSound);
                    break;
                case PowerUpKind.ScorePrism:
                    scorePrismTimer = Mathf.Min(10f, Mathf.Max(scorePrismTimer, 0f) + 6f);
                    Play(crystalSound);
                    break;
                case PowerUpKind.MagnetHalo:
                    magnetHaloTimer = Mathf.Min(10f, Mathf.Max(magnetHaloTimer, 0f) + 6.5f);
                    Play(unlockSound);
                    break;
                case PowerUpKind.PhaseShift:
                    phaseShiftTimer = Mathf.Min(8f, Mathf.Max(phaseShiftTimer, 0f) + (AllowsGameplayUpgrades() && HasUpgrade("phase_stabilizer") ? 4.7f : 3.2f));
                    Play(unlockSound);
                    break;
                default:
                    crystals += AllowsGameplayUpgrades() && HasUpgrade("cache_cores") ? 20 : 12;
                    UpdateCrystalLabels();
                    Play(crystalSound);
                    break;
            }
            UpdatePowerUpHud();
        }

        private void UpdatePowerUpEffects(float deltaTime)
        {
            if (slowFieldTimer > 0f) slowFieldTimer = Mathf.Max(0f, slowFieldTimer - deltaTime);
            if (shieldFlashTimer > 0f) shieldFlashTimer = Mathf.Max(0f, shieldFlashTimer - deltaTime);
            if (skySurgeTimer > 0f) skySurgeTimer = Mathf.Max(0f, skySurgeTimer - deltaTime);
            if (scorePrismTimer > 0f) scorePrismTimer = Mathf.Max(0f, scorePrismTimer - deltaTime);
            if (magnetHaloTimer > 0f) magnetHaloTimer = Mathf.Max(0f, magnetHaloTimer - deltaTime);
            if (phaseShiftTimer > 0f) phaseShiftTimer = Mathf.Max(0f, phaseShiftTimer - deltaTime);
            UpdatePowerUpHud();
        }

        private void UpdatePowerUpHud()
        {
            if (hudPowerUpText == null) return;
            var code = -1;
            var timer = 0f;
            var label = string.Empty;
            var colour = Hex("#f4fbff");
            if (phaseShiftTimer > 0f) { code = 0; timer = phaseShiftTimer; label = "◇  PHASE SHIFT"; colour = Hex("#b17cff"); }
            else if (slowFieldTimer > 0f) { code = 1; timer = slowFieldTimer; label = "◌  SLOW FIELD"; colour = Hex("#b17cff"); }
            else if (skySurgeTimer > 0f) { code = 2; timer = skySurgeTimer; label = "↟  SKY SURGE"; colour = Hex("#ffc34d"); }
            else if (scorePrismTimer > 0f) { code = 3; timer = scorePrismTimer; label = "✦  SCORE PRISM"; colour = Hex("#f05bc6"); }
            else if (magnetHaloTimer > 0f) { code = 4; timer = magnetHaloTimer; label = "◌  MAGNET HALO"; colour = Hex("#45eaff"); }
            else if (shieldCharges > 0) { code = 5; label = "◈  PULSE SHIELD READY"; colour = Hex("#61f5b3"); }
            else if (rescueCharges > 0) { code = 6; label = "♥  RESCUE FEATHER READY"; colour = Hex("#f05bc6"); }
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
            if (!AllowsPowerUps()) return false;
            if (shieldCharges > 0)
            {
                shieldCharges = 0;
                shieldFlashTimer = .85f;
                TriggerFlightFeedback(Hex("#61f5b3"), .34f);
                PulseHaptic(.16f);
                Play(unlockSound);
                UpdatePowerUpHud();
                return true;
            }
            if (rescueCharges <= 0) return false;
            rescueCharges = 0;
            shieldFlashTimer = .5f;
            TriggerFlightFeedback(Hex("#f05bc6"), .30f);
            PulseHaptic(.16f);
            Play(unlockSound);
            UpdatePowerUpHud();
            return true;
        }

        private void UpdateTrail(float deltaTime)
        {
            var trailScale = AllowsGameplayUpgrades() && HasUpgrade("comet_trail") ? 1.32f : 1f;
            if (skySurgeTimer > 0f) trailScale *= 1.18f;
            if (phaseShiftTimer > 0f) trailScale *= 1.10f;
            trailGlow.startWidth = .19f * trailScale;
            trailCore.startWidth = .082f * trailScale;
            trailPoints[0] = bird.position + new Vector3(-.66f, .02f, .1f);
            for (var index = 1; index < trailPoints.Length; index += 1)
            {
                var follow = 1f - Mathf.Exp(-deltaTime * Mathf.Lerp(19f, 8f, index / (float)(trailPoints.Length - 1)));
                trailPoints[index] = Vector3.Lerp(trailPoints[index], trailPoints[index - 1], follow);
            }
            trailGlow.positionCount = trailPoints.Length;
            trailCore.positionCount = trailPoints.Length;
            trailGlow.SetPositions(trailPoints);
            trailCore.SetPositions(trailPoints);
        }

        private void ShowScoreBurst(int crystalReward, bool perfect)
        {
            scoreBurstTimer = .36f;
            scoreBurstText.text = perfect
                ? $"PERFECT  ·  +{crystalReward} ✦"
                : crystalReward > 1 ? $"+1  ·  +{crystalReward} ✦" : "+1  ·  +1 ✦";
            scoreBurstText.rectTransform.anchoredPosition = new Vector2(0f, 612f);
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
                collisionBirdDebug.transform.localScale = Vector3.one * (ActiveTuning().CollisionRadius * 2f);
            }

            var halfGap = ActiveGap() * .5f;
            var physicalWidth = PipeWidth + .25f;
            foreach (var pair in pipePool)
            {
                if (pair == null || pair.DebugTop == null || pair.DebugBottom == null) continue;
                var showPair = visible && pair.Root.activeSelf;
                pair.DebugTop.enabled = showPair;
                pair.DebugBottom.enabled = showPair;
                if (!showPair) continue;

                var topEdge = pair.GapCenter + halfGap;
                var topHeight = Mathf.Max(0f, CameraHeight * .5f - topEdge);
                pair.DebugTop.transform.localPosition = new Vector3(0f, topEdge + topHeight * .5f, 0f);
                pair.DebugTop.transform.localScale = new Vector3(physicalWidth, topHeight, 1f);

                var bottomEdge = pair.GapCenter - halfGap;
                var bottomHeight = Mathf.Max(0f, bottomEdge - GroundY);
                pair.DebugBottom.transform.localPosition = new Vector3(0f, GroundY + bottomHeight * .5f, 0f);
                pair.DebugBottom.transform.localScale = new Vector3(physicalWidth, bottomHeight, 1f);
            }
        }
#endif

        private void StartFlight()
        {
            BeginFlight(selectedFlightMode);
        }

        private void RestartFlight()
        {
            BeginFlight(flightMode);
        }

        private void StartDailyFlight()
        {
            BeginFlight(FlightMode.Daily);
        }

        private void BeginFlight(FlightMode mode)
        {
            ClosePurchaseModal();
            flightMode = mode;
            if (mode != FlightMode.Daily) selectedFlightMode = mode;
            activeDailyRouteKey = mode == FlightMode.Daily ? DailyRouteKey() : string.Empty;
            dailyRouteRandom = mode == FlightMode.Daily ? new System.Random(DailyRouteSeed(activeDailyRouteKey)) : null;
            state = FlightState.Playing;
            score = 0;
            perfectPasses = 0;
            newBest = false;
            simulationAccumulator = 0f;
            bufferedFlapUntil = -1f;
            flightFeedbackTimer = 0f;
            lastCrashReason = "GATE IMPACT";
            birdY = 0f;
            birdVelocity = 0f;
            birdTilt = 0f;
            birdTiltVelocity = 0f;
            wingTimer = 1f;
            slowFieldTimer = 0f;
            shieldFlashTimer = 0f;
            skySurgeTimer = 0f;
            scorePrismTimer = 0f;
            magnetHaloTimer = 0f;
            phaseShiftTimer = 0f;
            shieldCharges = AllowsGameplayUpgrades() && HasUpgrade("shield_cell") ? 1 : 0;
            rescueCharges = AllowsGameplayUpgrades() && HasUpgrade("rescue_feather") ? 1 : 0;
            gatesSinceStarheart = 0;
            trailGlow.positionCount = 0;
            trailCore.positionCount = 0;
            var launchTrailPoint = new Vector3(BirdX, birdY, .1f);
            for (var index = 0; index < trailPoints.Length; index += 1) trailPoints[index] = launchTrailPoint;
            spawnX = GetWorldWidth() * .5f + 3.2f;
            foreach (var pickup in powerUpPool)
            {
                pickup.Active = false;
                pickup.Gate = null;
                pickup.Root.SetActive(false);
            }
            for (var index = 0; index < pipePool.Length; index += 1) ConfigurePipe(pipePool[index], spawnX + index * PipeSpacing);
            for (var index = 0; index < ActiveTuning().PowerUpSlots && index < powerUpPool.Length; index += 1)
            {
                var pickup = powerUpPool[index];
                var gate = FindAvailablePowerUpGate(pickup);
                if (gate != null) ConfigurePowerUp(pickup, gate);
            }
            RefreshScreens();
            hudScoreText.text = "0";
            UpdateModeCopy();
            UpdatePowerUpHud();
            bird.gameObject.SetActive(true);
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
            trailGlow.positionCount = 0;
            trailCore.positionCount = 0;
            foreach (var pair in pipePool) pair.Root.SetActive(false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (collisionBirdDebug != null) collisionBirdDebug.enabled = false;
            foreach (var pair in pipePool)
            {
                if (pair.DebugTop != null) pair.DebugTop.enabled = false;
                if (pair.DebugBottom != null) pair.DebugBottom.enabled = false;
            }
#endif
            foreach (var pickup in powerUpPool)
            {
                pickup.Active = false;
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
            pair.Root.SetActive(true);
            pair.X = x;
            pair.Passed = false;
            var nextCentre = RouteRange(-1.72f, 1.92f);
            var precedingPair = FindPrecedingPipe(pair, x);
            if (precedingPair != null)
            {
                var maximumStep = ActiveTuning().MaximumGapCenterStep;
                nextCentre = Mathf.Clamp(nextCentre, precedingPair.GapCenter - maximumStep, precedingPair.GapCenter + maximumStep);
            }
            pair.GapCenter = nextCentre;
            pair.Root.transform.localPosition = new Vector3(x, 0f, 0f);

            var halfGap = ActiveGap() * .5f;
            var topLowerEdge = pair.GapCenter + halfGap;
            var topHeight = CameraHeight * .5f - topLowerEdge;
            LayoutPipeSurface(pair.Top, topLowerEdge + topHeight * .5f, topHeight, topLowerEdge, true);

            var bottomUpperEdge = pair.GapCenter - halfGap;
            var bottomHeight = bottomUpperEdge - GroundY;
            LayoutPipeSurface(pair.Bottom, GroundY + bottomHeight * .5f, bottomHeight, bottomUpperEdge, false);
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

        private void AnimatePipePair(PipePair pair)
        {
            var halfGap = ActiveGap() * .5f;
            AnimatePipeSurface(pair.Top, pair.GapCenter + halfGap, true, pair.X);
            AnimatePipeSurface(pair.Bottom, pair.GapCenter - halfGap, false, pair.X);
        }

        private void AnimatePipeSurface(PipeSurface surface, float capY, bool topPipe, float pipeX)
        {
            // The movement stays in the pipe's own non-colliding light layers. This
            // makes the gateway feel alive without ever changing the visible safe gap.
            var gateMotion = reduceMotionEnabled ? 0f : 1f;
            var pulse = reduceMotionEnabled ? .56f : .5f + .5f * Mathf.Sin(ambientTime * 6.4f + pipeX * 1.7f);
            var direction = topPipe ? 1f : -1f;
            // Keep the shared deep-metal shell legible, then let the selected gate
            // material breathe through a tiny controlled light pulse.
            var shellColour = Color.Lerp(Color.white, equippedPipe.Accent, equippedPipe.Id == "ion" ? .04f : .34f);
            surface.Artwork.color = Color.Lerp(shellColour, Color.white, pulse * .075f);
            var seamColour = surface.Energy.color;
            seamColour.a = Mathf.Lerp(.38f, .88f, pulse);
            surface.Energy.color = seamColour;
            surface.Energy.transform.localPosition = new Vector3(0f, capY + direction * (.055f + Mathf.Sin(ambientTime * 8.8f + pipeX) * .012f * gateMotion), 0f);
            surface.Energy.transform.localScale = new Vector3(PipeWidth * Mathf.Lerp(.62f, .82f, pulse), .026f + pulse * .020f, 1f);

            var highlightColour = surface.Highlight.color;
            highlightColour.a = Mathf.Lerp(.05f, .28f, pulse);
            surface.Highlight.color = highlightColour;
            surface.Highlight.transform.localPosition = new Vector3(0f, capY + direction * (.035f + Mathf.Cos(ambientTime * 7.2f + pipeX) * .010f * gateMotion), 0f);
            surface.Highlight.transform.localScale = new Vector3(PipeWidth * Mathf.Lerp(.53f, .72f, pulse), .008f + pulse * .011f, 1f);

            var scanPhase = reduceMotionEnabled ? .48f : Mathf.Repeat(ambientTime * .82f + pipeX * .11f, 1f);
            var scanColour = surface.Scan.color;
            scanColour.a = Mathf.Lerp(.05f, .24f, pulse);
            surface.Scan.color = scanColour;
            surface.Scan.transform.localPosition = new Vector3(0f, capY + direction * (.17f + scanPhase * .72f), 0f);
            surface.Scan.transform.localScale = new Vector3(PipeWidth * .68f, .010f, 1f);

            var beaconColour = surface.Beacon.color;
            beaconColour.a = Mathf.Lerp(.08f, .34f, pulse);
            surface.Beacon.color = beaconColour;
            surface.Beacon.transform.localPosition = new Vector3(0f, capY + direction * .115f, 0f);
            surface.Beacon.transform.localScale = Vector3.one * Mathf.Lerp(.21f, .31f, pulse);
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

        private void LayoutPipeSurface(PipeSurface surface, float centreY, float height, float capY, bool topPipe)
        {
            var style = equippedPipe;
            // Real pipe art replaces the placeholder collection of stretched blocks.
            // It is scaled independently in each direction so its engineered rim lands
            // precisely on the playable edge of the gap.
            var art = surface.Artwork;
            art.enabled = art.sprite != null;
            art.transform.localPosition = new Vector3(0f, centreY, 0f);
            if (art.sprite != null)
            {
                var artWidth = Mathf.Max(.01f, art.sprite.bounds.size.x);
                var artHeight = Mathf.Max(.01f, art.sprite.bounds.size.y);
                art.transform.localScale = new Vector3(PipeWidth / artWidth, (height + .12f) / artHeight, 1f);
                // The other cosmetic pipe sets tint the shared premium base very gently,
                // keeping metal readable rather than turning it into a coloured block.
                art.color = Color.Lerp(Color.white, style.Accent, style.Id == "ion" ? 0f : .34f);
            }

            // A restrained illuminated seam gives every pipe theme a readable edge at
            // speed. It sits just inside the obstacle, so the opening remains clean.
            var insideOffset = topPipe ? .055f : -.055f;
            surface.Energy.enabled = true;
            surface.Energy.sortingOrder = 7;
            var seamColor = style.Energy;
            seamColor.a = .64f;
            surface.Energy.color = seamColor;
            surface.Energy.transform.localPosition = new Vector3(0f, capY + insideOffset, 0f);
            surface.Energy.transform.localScale = new Vector3(PipeWidth * .70f, .035f, 1f);

            surface.Highlight.enabled = true;
            surface.Highlight.sortingOrder = 8;
            surface.Highlight.color = new Color(1f, 1f, 1f, .16f);
            surface.Highlight.transform.localPosition = new Vector3(0f, capY + insideOffset * .45f, 0f);
            surface.Highlight.transform.localScale = new Vector3(PipeWidth * .62f, .011f, 1f);

            surface.Scan.enabled = true;
            surface.Scan.sortingOrder = 8;
            var scanColor = style.Energy;
            scanColor.a = .14f;
            surface.Scan.color = scanColor;
            surface.Scan.transform.localPosition = new Vector3(0f, capY + insideOffset * 2f, 0f);
            surface.Scan.transform.localScale = new Vector3(PipeWidth * .68f, .010f, 1f);

            surface.Beacon.enabled = true;
            surface.Beacon.sortingOrder = 10;
            var beaconColor = style.Energy;
            beaconColor.a = .20f;
            surface.Beacon.color = beaconColor;
            surface.Beacon.transform.localPosition = new Vector3(0f, capY + insideOffset * 2f, 0f);
            surface.Beacon.transform.localScale = Vector3.one * .25f;

            surface.Outer.enabled = false;
            surface.Panel.enabled = false;
            surface.Shade.enabled = false;
            surface.CapOuter.enabled = false;
            surface.CapAccent.enabled = false;
            surface.CapPanel.enabled = false;
            surface.CapEnergy.enabled = false;
        }

        private float ActiveGap()
        {
            var tuning = ActiveTuning();
            var startingGap = tuning.StartingGap;
            // Adventure worlds preserve their character; they do not leak mechanical
            // advantage or disadvantage into a fair Classic/Daily score.
            if (flightMode == FlightMode.Adventure && equippedWorld != null)
            {
                startingGap += equippedWorld.GapSize - AdventureTuning.StartingGap;
            }

            var minimumGap = Mathf.Min(startingGap, tuning.MinimumGap);
            return Mathf.Max(minimumGap, startingGap - Mathf.Min(score, 24) * tuning.GapShrinkPerGate);
        }

        private float RouteRange(float minimum, float maximum)
        {
            if (dailyRouteRandom == null) return UnityEngine.Random.Range(minimum, maximum);
            return minimum + (float)dailyRouteRandom.NextDouble() * (maximum - minimum);
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
            state = FlightState.GameOver;
            var previousBest = BestFor(flightMode);
            newBest = score > previousBest && score > 0;
            SetBestFor(flightMode, Mathf.Max(previousBest, score));
            SaveProgress();
            TriggerFlightFeedback(Hex("#f05bc6"), .36f);
            PulseHaptic(.28f);
            Play(crashSound);
            resultScoreText.text = flightMode == FlightMode.Daily ? $"DAILY SCORE  {score}" : $"SCORE  {score}";
            resultBestText.text = flightMode == FlightMode.Daily
                ? $"TODAY'S BEST  {dailyBest}"
                : $"{ModeLabel(flightMode)} BEST  {BestFor(flightMode)}";
            if (resultReasonText != null) resultReasonText.text = lastCrashReason;
            if (resultModeText != null)
            {
                resultModeText.text = flightMode == FlightMode.Adventure
                    ? "ADVENTURE · POWER UPS ACTIVE"
                    : flightMode == FlightMode.Daily
                        ? $"DAILY ROUTE · {activeDailyRouteKey} · FAIR FLIGHT"
                        : "CLASSIC · FAIR FLIGHT";
                resultModeText.color = ModeAccent(flightMode);
            }
            resultNewBestText.gameObject.SetActive(newBest);
            RefreshScreens();
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
                    customizeTitle.text = "BIRD COLLECTION";
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
                case CosmeticCategory.Trails:
                    customizeTitle.text = "TRAIL COLLECTION";
                    for (var index = 0; index < Trails.Length; index += 1)
                    {
                        var trail = Trails[index];
                        CreateCosmeticCard(index, trail.Name, equippedTrail.Id == trail.Id ? "EQUIPPED" : "TAP TO EQUIP", trail.Core, null, () => EquipTrail(trail), trail.Core, trail.Glow);
                    }
                    SetContentRows(Trails.Length);
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
                    customizeTitle.text = "FLIGHT TECH";
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
            var owned = HasUpgrade(upgrade.Id);
            var card = CreatePanel(customizeContent, $"{upgrade.Name} upgrade", new Vector2(column == 0 ? -235f : 235f, -12f - row * 250f), new Vector2(440f, 222f), Hex("#0b1022"));
            card.anchorMin = new Vector2(.5f, 1f);
            card.anchorMax = new Vector2(.5f, 1f);
            card.pivot = new Vector2(.5f, 1f);
            AddOutline(card.gameObject, upgrade.Accent, owned ? 3f : 1.5f);
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
            CreateText(card, upgrade.Description, new Vector2(-70f, 17f), new Vector2(270f, 44f), 15, new Color(.86f, .91f, 1f, .73f), TextAnchor.MiddleLeft, FontStyle.Normal).raycastTarget = false;
            var status = owned ? "OWNED" : $"BUY · {upgrade.Price} ✦";
            CreateText(card, status, new Vector2(-185f, -85f), new Vector2(350f, 28f), 16, owned ? upgrade.Accent : new Color(.85f, .9f, 1f, .68f), TextAnchor.MiddleLeft, FontStyle.Bold).raycastTarget = false;
        }

        private Sprite GetUpgradeArtwork(Upgrade upgrade)
        {
            PowerUpKind kind;
            switch (upgrade.Id)
            {
                case "thrust_plumes":
                case "comet_trail":
                    kind = PowerUpKind.SkySurge;
                    break;
                case "featherweight":
                case "time_weaver":
                    kind = PowerUpKind.SlowField;
                    break;
                case "air_brakes":
                case "phase_stabilizer":
                    kind = PowerUpKind.PhaseShift;
                    break;
                case "rescue_feather":
                case "shield_cell":
                    kind = PowerUpKind.PulseShield;
                    break;
                case "cache_cores":
                    kind = PowerUpKind.CrystalCache;
                    break;
                case "magnet_array":
                    kind = PowerUpKind.MagnetHalo;
                    break;
                default:
                    kind = PowerUpKind.ScorePrism;
                    break;
            }
            return LoadSprite(PowerUpArtworkPath(kind));
        }

        private void EquipSkin(Skin skin)
        {
            equippedSkin = skin;
            ApplyEquippedVisuals();
            SaveProgress();
            RebuildCustomizeGrid();
        }

        private bool IsSkinOwned(Skin skin)
        {
            return skin.Price <= 0 || ownedSkinIds.Contains(skin.Id);
        }

        private bool HasUpgrade(string id)
        {
            return ownedUpgradeIds.Contains(id);
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
            if (HasUpgrade(upgrade.Id)) return;
            pendingUpgrade = upgrade;
            pendingSkin = null;
            pendingPurchase = PendingPurchase.Upgrade;
            OpenPurchaseModal(upgrade.Name, upgrade.Description, upgrade.Price, upgrade.Accent, softCircleSprite);
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
            purchaseTitleText.text = $"UNLOCK {itemName}?";
            purchaseDetailText.text = $"SPEND  {price}  ✦  ·  {detail}";
            var remainder = Mathf.Max(0, price - crystals);
            purchaseBalanceText.text = remainder == 0
                ? $"YOUR BALANCE · {crystals} ✦"
                : $"YOUR BALANCE · {crystals} ✦   ·   NEED {remainder} MORE";
            purchaseConfirmButton.interactable = crystals >= price;
            purchaseConfirmText.text = crystals >= price ? $"UNLOCK · {price} ✦" : "NOT ENOUGH ✦";
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
            var price = pendingPurchase == PendingPurchase.Skin && pendingSkin != null ? pendingSkin.Price
                : pendingPurchase == PendingPurchase.Upgrade && pendingUpgrade != null ? pendingUpgrade.Price : -1;
            if (price < 0 || crystals < price) return;
            crystals -= price;
            if (pendingPurchase == PendingPurchase.Skin)
            {
                ownedSkinIds.Add(pendingSkin.Id);
                equippedSkin = pendingSkin;
            }
            else
            {
                ownedUpgradeIds.Add(pendingUpgrade.Id);
            }
            ClosePurchaseModal();
            ApplyEquippedVisuals();
            SaveProgress();
            Play(unlockSound);
            RebuildCustomizeGrid();
        }

        private void EquipWorld(WorldTheme world)
        {
            equippedWorld = world;
            // Worlds are the replacement for a difficulty selector: selecting one
            // applies its authored backdrop, obstacle treatment and matching trail.
            // Players can still override either cosmetic afterwards in its collection.
            equippedPipe = FindById(PipeStyles, world.PresetPipeId) ?? equippedPipe ?? PipeStyles[0];
            equippedTrail = FindById(Trails, world.PresetTrailId) ?? equippedTrail ?? Trails[0];
            ApplyEquippedVisuals();
            SaveProgress();
            RebuildCustomizeGrid();
        }

        private void EquipTrail(TrailStyle trail)
        {
            equippedTrail = trail;
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
            if (equippedWorld == null) equippedWorld = Worlds[0];
            if (equippedTrail == null) equippedTrail = Trails[0];
            if (equippedPipe == null) equippedPipe = PipeStyles[0];

            backgroundRenderer.sprite = LoadSprite(equippedWorld.BackgroundPath) ?? midnightSprite;
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
        }

        private void ApplyTrailColors()
        {
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

        private void SetBirdArtwork()
        {
            if (birdRenderer == null || birdFlapRenderer == null || birdRiseRenderer == null) return;
            var aetherwing = UsesAetherwing();
            idleBirdSprite = LoadSprite(equippedSkin.ArtPath)
                ?? (aetherwing ? LoadSprite("SkyPulse/characters/premium/aetherwing-glide-v2") : null);
            flapBirdSprite = LoadSprite(equippedSkin.FlapPath)
                ?? (aetherwing ? LoadSprite("SkyPulse/characters/premium/aetherwing-flap-v1") : idleBirdSprite);
            riseBirdSprite = string.IsNullOrEmpty(equippedSkin.RisePath)
                ? flapBirdSprite
                : LoadSprite(equippedSkin.RisePath) ?? flapBirdSprite;
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
            var premiumRig = UsesAetherwing();
            birdRenderer.enabled = idleBirdSprite != null;
            birdFlapRenderer.enabled = !premiumRig && flapBirdSprite != null;
            birdRiseRenderer.enabled = !premiumRig && riseBirdSprite != null;
            if (birdParallaxRenderer != null) birdParallaxRenderer.enabled = idleBirdSprite != null;
            if (birdSafetyRenderer != null)
            {
                birdSafetyRenderer.sprite = emergencyBirdSprite;
                safetyBirdBaseScale = ArtworkScale(emergencyBirdSprite, BirdDisplayWidth * .72f);
                birdSafetyRenderer.transform.localScale = safetyBirdBaseScale;
                birdSafetyRenderer.transform.localPosition = new Vector3(-.005f, -.012f, 0f);
                birdSafetyRenderer.color = BirdSafetyColour();
                birdSafetyRenderer.enabled = true;
            }
            if (birdDepthRenderer != null) birdDepthRenderer.enabled = !premiumRig;
            if (birdEyeGlintRenderer != null) birdEyeGlintRenderer.enabled = !UsesAetherwing();
        }

        private bool UsesAetherwing()
        {
            return equippedSkin != null && equippedSkin.ArtPath.StartsWith("SkyPulse/characters/aetherwing", StringComparison.Ordinal);
        }

        private Color PremiumBirdTint()
        {
            if (equippedSkin == null) return Color.white;
            var tint = Color.Lerp(Color.white, equippedSkin.Accent, .14f);
            tint.a = 1f;
            return tint;
        }

        private Color BirdSafetyColour()
        {
            // A rich cobalt inner body remains legible on every world, including
            // bright fire and glacier themes. A small share of the selected accent
            // keeps the safety layer feeling like the equipped bird, not a UI icon.
            var cobalt = Hex("#102b70");
            var accent = equippedSkin != null ? equippedSkin.Accent : Hex("#45eaff");
            var colour = Color.Lerp(cobalt, accent, .24f);
            colour.a = .86f;
            return colour;
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
            if (premiumRig)
            {
                // A proper full-body pose rig: exactly one sharp drawing is displayed
                // while gentle transform interpolation supplies the connective motion.
                // This deliberately avoids translucent full-body crossfades.
                var pose = SelectAetherwingPose(riseWeight, wingWave);
                if (pose != null && birdRenderer.sprite != pose) birdRenderer.sprite = pose;
                birdRenderer.color = PremiumBirdTint();
                birdFlapRenderer.enabled = false;
                if (birdRiseRenderer != null) birdRiseRenderer.enabled = false;
                if (birdParallaxRenderer != null)
                {
                    birdParallaxRenderer.enabled = pose != null;
                    if (pose != null && birdParallaxRenderer.sprite != pose) birdParallaxRenderer.sprite = pose;
                }
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
            var breathing = 1f + Mathf.Sin(ambientTime * 5.2f) * .010f * lifeMotion;
            var glide = Mathf.Clamp(birdVelocity / Mathf.Abs(ActiveMaxFallVelocity()), -1f, 1f);
            var liftSquash = (flapKick - riseWeight * .34f) * (premiumRig ? .022f : .065f);
            var diveStretch = Mathf.Clamp01(-glide) * (premiumRig ? .010f : .024f);
            var bodyRoll = premiumRig
                ? riseWeight * 1.25f - wingWave * .85f + glide * .90f
                : riseWeight * 3.2f - wingWave * 2.4f + glide * 1.8f;
            var depthPulse = premiumRig ? riseWeight * .018f + wingWave * .012f : riseWeight * .075f + wingWave * .050f;
            bird.localScale = new Vector3(1f + depthPulse + diveStretch * .30f, 1f - depthPulse * .62f + diveStretch * .12f, 1f);
            birdArt.localScale = Vector3.Scale(premiumRig ? BaseScaleForBirdPose(birdRenderer.sprite) : idleBirdBaseScale, new Vector3(breathing + liftSquash + diveStretch, breathing - liftSquash - diveStretch * .55f, 1f));
            birdArt.localPosition = premiumRig
                ? new Vector3(-flapKick * .017f - wingWave * .006f - glide * .010f, Mathf.Sin(ambientTime * 7f) * .005f * lifeMotion + riseWeight * .010f + wingWave * .004f, 0f)
                : new Vector3(-flapKick * .052f - glide * .020f, Mathf.Sin(ambientTime * 7f) * .014f + riseWeight * .018f, 0f);
            birdArt.localRotation = Quaternion.Euler(0f, 0f, bodyRoll);
            if (!premiumRig)
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
                if (UsesAetherwing())
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
                var active = skySurgeTimer > 0f || scorePrismTimer > 0f || magnetHaloTimer > 0f || phaseShiftTimer > 0f;
                var colour = Hex("#ffc34d");
                if (phaseShiftTimer > 0f) colour = Hex("#b17cff");
                else if (scorePrismTimer > 0f) colour = Hex("#f05bc6");
                else if (magnetHaloTimer > 0f) colour = Hex("#45eaff");
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
                difficultyText.text = ModeLabel(selectedFlightMode);
                difficultyText.color = ModeAccent(selectedFlightMode);
            }
            if (menuModeDetailText != null)
            {
                menuModeDetailText.text = selectedFlightMode == FlightMode.Adventure
                    ? "POWER UPS · UPGRADES · EXPRESSIVE FLIGHT"
                    : "FAIR FLIGHT · NO POWER UPS · COMPARABLE SCORES";
                menuModeDetailText.color = ModeAccent(selectedFlightMode);
            }
            if (menuDailyText != null)
            {
                menuDailyText.text = $"DAILY ROUTE · {DateTime.UtcNow:MMM dd} · BEST {dailyBest}".ToUpperInvariant();
            }
            if (hudModeText != null)
            {
                hudModeText.text = flightMode == FlightMode.Adventure
                    ? "ADVENTURE · POWER UPS ACTIVE"
                    : flightMode == FlightMode.Daily
                        ? $"DAILY ROUTE · {activeDailyRouteKey} · FAIR FLIGHT"
                        : "CLASSIC · FAIR FLIGHT";
                hudModeText.color = ModeAccent(flightMode);
            }
            if (menuBestText != null) menuBestText.text = $"{ModeLabel(selectedFlightMode)} BEST · {BestFor(selectedFlightMode)}";
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
                : "CRYSTALS UNLOCK BIRDS  ·  WORLDS  ·  TRAILS";
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
            hudScreen.SetActive(state == FlightState.Playing);
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
            equippedSkin = FindById(Skins, PlayerPrefs.GetString("skypulse.native.skin", "nova")) ?? Skins[0];
            equippedWorld = FindById(Worlds, PlayerPrefs.GetString("skypulse.native.world", "neon_city")) ?? Worlds[0];
            equippedTrail = FindById(Trails, PlayerPrefs.GetString("skypulse.native.trail", "pulse")) ?? Trails[0];
            equippedPipe = FindById(PipeStyles, PlayerPrefs.GetString("skypulse.native.pipe", "ion")) ?? PipeStyles[0];
            var savedOwnedSkins = PlayerPrefs.GetString("skypulse.native.owned-skins", string.Empty);
            if (!string.IsNullOrEmpty(savedOwnedSkins))
            {
                foreach (var id in savedOwnedSkins.Split(','))
                {
                    if (!string.IsNullOrEmpty(id)) ownedSkinIds.Add(id);
                }
            }
            else
            {
                // Existing native players keep the bird they were already using when the
                // collection gains unlock states; new players simply begin with Nova.
                ownedSkinIds.Add(Skins[0].Id);
                if (equippedSkin != null) ownedSkinIds.Add(equippedSkin.Id);
            }
            var savedOwnedUpgrades = PlayerPrefs.GetString("skypulse.native.owned-upgrades", string.Empty);
            if (!string.IsNullOrEmpty(savedOwnedUpgrades))
            {
                foreach (var id in savedOwnedUpgrades.Split(','))
                {
                    if (!string.IsNullOrEmpty(id)) ownedUpgradeIds.Add(id);
                }
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
            PlayerPrefs.SetInt("skypulse.native.flight-coach-stage", flightCoachStage);
            PlayerPrefs.SetInt("skypulse.native.reduce-motion", reduceMotionEnabled ? 1 : 0);
            PlayerPrefs.SetInt("skypulse.native.haptics", hapticsEnabled ? 1 : 0);
            PlayerPrefs.SetString("skypulse.native.skin", equippedSkin.Id);
            PlayerPrefs.SetString("skypulse.native.world", equippedWorld.Id);
            PlayerPrefs.SetString("skypulse.native.trail", equippedTrail.Id);
            PlayerPrefs.SetString("skypulse.native.pipe", equippedPipe.Id);
            PlayerPrefs.SetString("skypulse.native.owned-skins", string.Join(",", ownedSkinIds));
            PlayerPrefs.SetString("skypulse.native.owned-upgrades", string.Join(",", ownedUpgradeIds));
            PlayerPrefs.Save();
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
            if (texture == null && path.StartsWith("SkyPulse/characters/", StringComparison.Ordinal))
            {
                // A public build must never turn a missing cosmetic into an invisible
                // player. This is only reached if an authored asset was omitted from
                // the player; usual play uses the high-detail Aetherwing artwork.
                if (emergencyBirdSprite == null) emergencyBirdSprite = CreateEmergencyBirdSprite();
                spriteCache[path] = emergencyBirdSprite;
                return emergencyBirdSprite;
            }
            if (texture == null) return null;
            sprite = CreateSprite(texture);
            spriteCache[path] = sprite;
            return sprite;
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
            var widthScale = (GetWorldWidth() + padding) / sourceWidth;
            renderer.transform.localScale = Vector3.one * Mathf.Max(heightScale, widthScale);
        }

        private static void SetBlock(SpriteRenderer renderer, Vector2 position, Vector2 size)
        {
            renderer.transform.localPosition = new Vector3(position.x, position.y, 0f);
            renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        private float GetWorldWidth()
        {
            return CameraHeight * flightCamera.aspect;
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
            var fill = Color.Lerp(Hex("#11172c"), accent, label == "FLY" ? .13f : .065f);
            fill.a = 1f;
            var inner = CreatePanel(shell, "Button inner", Vector2.zero, size - new Vector2(8f, 8f), fill);
            inner.GetComponent<Image>().raycastTarget = false;
            var energy = CreatePanel(shell, "Button energy line", new Vector2(0f, -size.y * .25f), new Vector2(label == "FLY" ? 128f : 88f, 1.5f), new Color(accent.r, accent.g, accent.b, .60f));
            energy.GetComponent<Image>().raycastTarget = false;
            var text = CreateText(shell, label, Vector2.zero, size - new Vector2(22f, 14f), label == "FLY" ? 34 : 22, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
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
