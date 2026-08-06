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
        private enum FlightState { Menu, Playing, Paused, GameOver, Customize }
        private enum CosmeticCategory { Birds, Worlds, Trails, Pipes }
        private enum PowerUpKind { SlowField, PulseShield, CrystalCache }

        private sealed class Skin
        {
            public string Id;
            public string Name;
            public string ArtPath;
            public string FlapPath;
            public Color Accent;
            public Color Trail;
            public int Price;

            public Skin(string id, string name, string artPath, string flapPath, string accent, string trail, int price)
            {
                Id = id;
                Name = name;
                ArtPath = artPath;
                FlapPath = flapPath;
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

            public WorldTheme(string id, string name, string backgroundPath, string accent, string floor)
            {
                Id = id;
                Name = name;
                BackgroundPath = backgroundPath;
                Accent = Hex(accent);
                Floor = Hex(floor);
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
            public SpriteRenderer Ring;
            public SpriteRenderer Core;
            public SpriteRenderer Icon;
            public SpriteRenderer Orbit;
            public PowerUpKind Kind;
            public float X;
            public float Y;
            public float Phase;
            public float RespawnTimer;
            public bool Active;
        }

        private const float CameraHeight = 18f;
        private const float GroundY = -6.82f;
        private const float BirdX = -2.45f;
        private const float BirdCollisionRadius = .27f;
        private const float BirdDisplayWidth = 2.26f;
        private const float PipeWidth = .92f;
        private const float PipeSpacing = 6.65f;
        private const float Gravity = -18.2f;
        private const float FlapVelocity = 6.25f;
        private const float MaxFallVelocity = -11.2f;
        private const int PipeCount = 4;
        private const int PowerUpCount = 3;
        private const float PickupRadius = .43f;

        private static readonly Skin[] Skins =
        {
            new Skin("nova", "NOVA", "SkyPulse/characters/nova", "SkyPulse/characters/nova-flap", "#8f64ff", "#45eaff", 0),
            new Skin("lumen", "LUMEN", "SkyPulse/characters/lumen", "SkyPulse/characters/lumen-flap", "#45eaff", "#8f64ff", 24),
            new Skin("ember", "EMBER", "SkyPulse/characters/ember", "SkyPulse/characters/ember-flap", "#f05bc6", "#ffc34d", 32),
            new Skin("sol", "SOL", "SkyPulse/characters/sol", "SkyPulse/characters/sol-flap", "#ffc34d", "#45eaff", 40),
            new Skin("aurora", "AURORA", "SkyPulse/characters/lumen", "SkyPulse/characters/lumen-flap", "#61f5b3", "#45eaff", 48),
            new Skin("orchid", "ORCHID", "SkyPulse/characters/nova", "SkyPulse/characters/nova-flap", "#b17cff", "#f05bc6", 52),
            new Skin("coral", "CORAL", "SkyPulse/characters/ember", "SkyPulse/characters/ember-flap", "#f082af", "#ffc34d", 56),
            new Skin("glacier", "GLACIER", "SkyPulse/characters/sol", "SkyPulse/characters/sol-flap", "#edf7ff", "#45eaff", 60),
            new Skin("prism", "PRISM", "SkyPulse/characters/generated/prism", "SkyPulse/characters/generated/prism-flap", "#45eaff", "#edf7ff", 68),
            new Skin("verdant", "VERDANT", "SkyPulse/characters/generated/verdant", "SkyPulse/characters/generated/verdant-flap", "#61f5b3", "#45eaff", 72),
            new Skin("cinder", "CINDER", "SkyPulse/characters/generated/cinder", "SkyPulse/characters/generated/cinder-flap", "#f05bc6", "#ffc34d", 76),
            new Skin("tide", "TIDE", "SkyPulse/characters/generated/tide", "SkyPulse/characters/generated/tide-flap", "#45eaff", "#8f64ff", 80),
            new Skin("wisp", "WISP", "SkyPulse/characters/generated/wisp", "SkyPulse/characters/generated/wisp-flap", "#edf7ff", "#45eaff", 88),
            new Skin("bloom", "BLOOM", "SkyPulse/characters/generated/bloom", "SkyPulse/characters/generated/bloom-flap", "#f05bc6", "#b17cff", 92),
            new Skin("emberwing", "EMBERWING", "SkyPulse/characters/generated/emberwing", "SkyPulse/characters/generated/emberwing-flap", "#ffc34d", "#f05bc6", 100),
            new Skin("steel", "STEEL", "SkyPulse/characters/generated/steel", "SkyPulse/characters/generated/steel-flap", "#edf7ff", "#45eaff", 108),
        };

        private static readonly WorldTheme[] Worlds =
        {
            new WorldTheme("neon_city", "NEON CITY", "SkyPulse/backgrounds/neon-city", "#45eaff", "#0a0522"),
            new WorldTheme("aurora_rise", "AURORA RISE", "SkyPulse/backgrounds/themes/polar-glow", "#61f5b3", "#05251e"),
            new WorldTheme("solar_drift", "SOLAR DRIFT", "SkyPulse/backgrounds/themes/amber-skies", "#ffc34d", "#2b0d10"),
            new WorldTheme("midnight_tide", "MIDNIGHT TIDE", "SkyPulse/backgrounds/themes/cobalt-storm", "#45eaff", "#07113d"),
            new WorldTheme("velvet_dawn", "VELVET DAWN", "SkyPulse/backgrounds/themes/rose-orbit", "#f05bc6", "#26051f"),
            new WorldTheme("crystal_night", "CRYSTAL NIGHT", "SkyPulse/backgrounds/themes/crystal-night", "#edf7ff", "#071239"),
            new WorldTheme("jade_horizon", "JADE HORIZON", "SkyPulse/backgrounds/themes/jade-horizon", "#61f5b3", "#063523"),
            new WorldTheme("violet_rain", "VIOLET RAIN", "SkyPulse/backgrounds/themes/violet-rain", "#b17cff", "#210842"),
            new WorldTheme("eclipse", "ECLIPSE", "SkyPulse/backgrounds/themes/eclipse", "#b17cff", "#10051f"),
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

        private FlightState state;
        private CosmeticCategory cosmeticCategory;
        private Camera flightCamera;
        private Sprite whiteSprite;
        private Sprite softCircleSprite;
        private Sprite ringSprite;
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer backgroundVeil;
        private LineRenderer cometTrail;
        private SpriteRenderer floorBase;
        private SpriteRenderer floorSurface;
        private SpriteRenderer floorLip;
        private SpriteRenderer floorGlow;
        private Transform bird;
        private Transform birdArt;
        private Transform birdFlapArt;
        private SpriteRenderer birdRenderer;
        private SpriteRenderer birdFlapRenderer;
        private SpriteRenderer shieldAuraRenderer;
        private SpriteRenderer slowAuraRenderer;
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
        private Text hudScoreText;
        private Text hudCrystalText;
        private Text hudPowerUpText;
        private Text scoreBurstText;
        private Text resultScoreText;
        private Text resultBestText;
        private Text resultNewBestText;
        private Image menuBirdImage;
        private RectTransform menuBirdTransform;
        private RectTransform customizeContent;
        private Text customizeTitle;
        private Text purchaseTitleText;
        private Text purchaseDetailText;
        private Text purchaseBalanceText;
        private Text purchaseConfirmText;
        private Image purchasePreviewImage;
        private Button purchaseConfirmButton;

        private Skin equippedSkin;
        private WorldTheme equippedWorld;
        private TrailStyle equippedTrail;
        private PipeStyle equippedPipe;
        private Skin pendingSkin;
        private int difficultyIndex;
        private int score;
        private int best;
        private int crystals;
        private float birdY;
        private float birdVelocity;
        private float birdTilt;
        private float wingTimer;
        private float menuWingTimer;
        private float spawnX;
        private float scoreBurstTimer;
        private float ambientTime;
        private float slowFieldTimer;
        private float shieldFlashTimer;
        private int shieldCharges;
        private int displayedSlowTenths = -1;
        private bool newBest;
        private Vector3 idleBirdBaseScale = Vector3.one;
        private Vector3 flapBirdBaseScale = Vector3.one;

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
            Time.maximumDeltaTime = 1f / 30f;

            LoadProgress();
            CreateCamera();
            CreateVisuals();
            CreateInterface();
            ApplyEquippedVisuals();
            ResetToMenu();
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
            softCircleSprite = CreateRadialSprite("Soft neon orb", 96, 0f, .5f);
            ringSprite = CreateRadialSprite("Neon ring", 96, .31f, .5f);

            backgroundRenderer = CreateRenderer("Cinematic world", LoadSprite("SkyPulse/backgrounds/neon-city"), Color.white, -40);
            backgroundRenderer.transform.position = new Vector3(0f, .12f, 0f);
            FitToCameraHeight(backgroundRenderer, CameraHeight + .5f);

            backgroundVeil = CreateRenderer("World colour veil", whiteSprite, new Color(.015f, .01f, .08f, .20f), -39);
            backgroundVeil.transform.position = new Vector3(0f, .1f, 0f);
            backgroundVeil.transform.localScale = new Vector3(GetWorldWidth() + 1f, CameraHeight + .5f, 1f);

            CreateAmbientStars();
            CreateFloor();
            CreateBird();
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
            for (var index = 0; index < 22; index += 1)
            {
                var star = CreateRenderer($"Ambient light {index + 1}", whiteSprite, new Color(.35f, .95f, 1f, .16f), -33);
                var x = Mathf.Lerp(-GetWorldWidth() * .48f, GetWorldWidth() * .48f, (float)random.NextDouble());
                var y = Mathf.Lerp(-5.8f, 8.3f, (float)random.NextDouble());
                var size = Mathf.Lerp(.018f, .055f, (float)random.NextDouble());
                star.transform.position = new Vector3(x, y, 0f);
                star.transform.localScale = Vector3.one * size;
                ambientStars.Add(new AmbientStar
                {
                    Transform = star.transform,
                    X = x,
                    Y = y,
                    Phase = (float)random.NextDouble() * Mathf.PI * 2f,
                    Speed = Mathf.Lerp(.45f, 1.15f, (float)random.NextDouble()),
                    BaseSize = size,
                });
            }

            var comet = new GameObject("Distant comet");
            comet.transform.SetParent(transform, false);
            cometTrail = comet.AddComponent<LineRenderer>();
            cometTrail.useWorldSpace = true;
            cometTrail.positionCount = 2;
            cometTrail.startWidth = .028f;
            cometTrail.endWidth = .007f;
            cometTrail.startColor = new Color(.83f, .95f, 1f, .72f);
            cometTrail.endColor = new Color(.45f, .30f, 1f, 0f);
            cometTrail.sortingOrder = -32;
            ConfigureLineMaterial(cometTrail);
        }

        private void CreateFloor()
        {
            var width = GetWorldWidth() + 1f;
            floorBase = CreateRenderer("Solid floor base", whiteSprite, Hex("#02030d"), -9);
            floorBase.transform.position = new Vector3(0f, GroundY - 1.1f, 0f);
            floorBase.transform.localScale = new Vector3(width, 2.24f, 1f);

            floorSurface = CreateRenderer("Floor material", whiteSprite, Hex("#130a32"), -8);
            floorSurface.transform.position = new Vector3(0f, GroundY - 1.04f, 0f);
            floorSurface.transform.localScale = new Vector3(width, 1.90f, 1f);

            floorLip = CreateRenderer("Floor solid edge", whiteSprite, Hex("#271252"), -7);
            floorLip.transform.position = new Vector3(0f, GroundY - .08f, 0f);
            floorLip.transform.localScale = new Vector3(width, .28f, 1f);

            floorGlow = CreateRenderer("Floor energy rail", whiteSprite, Hex("#45eaff"), -6);
            floorGlow.transform.position = new Vector3(0f, GroundY + .035f, 0f);
            floorGlow.transform.localScale = new Vector3(width, .072f, 1f);

            var floorHighlight = CreateRenderer("Floor edge highlight", whiteSprite, new Color(1f, 1f, 1f, .42f), -5);
            floorHighlight.transform.position = new Vector3(0f, GroundY + .105f, 0f);
            floorHighlight.transform.localScale = new Vector3(width, .018f, 1f);

            CreateFloorLane("Left flight lane", new Vector3(-.72f, GroundY - .12f), new Vector3(-4.6f, -9f));
            CreateFloorLane("Centre flight lane", new Vector3(0f, GroundY - .12f), new Vector3(0f, -9f));
            CreateFloorLane("Right flight lane", new Vector3(.72f, GroundY - .12f), new Vector3(4.6f, -9f));
            for (var index = 0; index < 4; index += 1)
            {
                var rail = CreateRenderer($"Floor depth rail {index + 1}", whiteSprite, new Color(.28f, .9f, 1f, .16f), -7);
                rail.transform.position = new Vector3(0f, GroundY - .38f - index * .43f, 0f);
                rail.transform.localScale = new Vector3(width, .028f, 1f);
            }
        }

        private void CreateFloorLane(string name, Vector3 from, Vector3 to)
        {
            var lane = new GameObject(name);
            lane.transform.SetParent(transform, false);
            var renderer = lane.AddComponent<LineRenderer>();
            renderer.useWorldSpace = true;
            renderer.positionCount = 2;
            renderer.SetPosition(0, from);
            renderer.SetPosition(1, to);
            renderer.startWidth = .018f;
            renderer.endWidth = .038f;
            renderer.startColor = new Color(.28f, .92f, 1f, .16f);
            renderer.endColor = new Color(.28f, .92f, 1f, .42f);
            renderer.sortingOrder = -7;
            ConfigureLineMaterial(renderer);
        }

        private void CreateBird()
        {
            bird = new GameObject("Flight bird").transform;
            bird.SetParent(transform, false);
            var slowAura = CreateRenderer("Slow field aura", ringSprite, new Color(.45f, .3f, 1f, 0f), 12, bird);
            slowAura.transform.localScale = Vector3.one * 1.42f;
            slowAuraRenderer = slowAura;
            var shieldAura = CreateRenderer("Pulse shield aura", ringSprite, new Color(.38f, 1f, .70f, 0f), 13, bird);
            shieldAura.transform.localScale = Vector3.one * 1.22f;
            shieldAuraRenderer = shieldAura;
            birdArt = new GameObject("Bird idle artwork").transform;
            birdArt.SetParent(bird, false);
            birdRenderer = birdArt.gameObject.AddComponent<SpriteRenderer>();
            birdRenderer.sortingOrder = 14;
            birdFlapArt = new GameObject("Bird wing motion artwork").transform;
            birdFlapArt.SetParent(bird, false);
            birdFlapRenderer = birdFlapArt.gameObject.AddComponent<SpriteRenderer>();
            birdFlapRenderer.sortingOrder = 15;
            birdFlapRenderer.color = new Color(1f, 1f, 1f, 0f);
        }

        private PowerUpPickup CreatePowerUp(int index)
        {
            var root = new GameObject($"Power-up pickup {index + 1}");
            root.transform.SetParent(transform, false);
            var glow = CreateRenderer("Outer glow", softCircleSprite, Color.white, 10, root.transform);
            glow.transform.localScale = Vector3.one * .88f;
            var ring = CreateRenderer("Orbit ring", ringSprite, Color.white, 11, root.transform);
            ring.transform.localScale = Vector3.one * .64f;
            var core = CreateRenderer("Energy core", softCircleSprite, Color.white, 12, root.transform);
            core.transform.localScale = Vector3.one * .34f;
            var icon = CreateRenderer("Power icon", whiteSprite, Color.white, 13, root.transform);
            icon.transform.localScale = new Vector3(.16f, .16f, 1f);
            var orbit = CreateRenderer("Orbit spark", softCircleSprite, Color.white, 13, root.transform);
            orbit.transform.localScale = Vector3.one * .12f;
            return new PowerUpPickup
            {
                Root = root,
                Transform = root.transform,
                Glow = glow,
                Ring = ring,
                Core = core,
                Icon = icon,
                Orbit = orbit,
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
            return new PipePair
            {
                Root = root,
                Top = CreatePipeSurface(root.transform, "Top pipe"),
                Bottom = CreatePipeSurface(root.transform, "Bottom pipe"),
            };
        }

        private PipeSurface CreatePipeSurface(Transform parent, string label)
        {
            var top = label.StartsWith("Top", StringComparison.Ordinal);
            return new PipeSurface
            {
                Artwork = CreateRenderer($"{label} artwork", LoadSprite(top ? "SkyPulse/art/pipe-top" : "SkyPulse/art/pipe-bottom"), Color.white, 6, parent),
                Outer = CreateRenderer($"{label} outer", whiteSprite, Hex("#030613"), 2, parent),
                Panel = CreateRenderer($"{label} panel", whiteSprite, Hex("#0b3076"), 3, parent),
                Shade = CreateRenderer($"{label} shadow", whiteSprite, new Color(0f, 0f, 0f, .28f), 4, parent),
                Highlight = CreateRenderer($"{label} edge highlight", whiteSprite, new Color(.8f, .95f, 1f, .18f), 4, parent),
                Energy = CreateRenderer($"{label} energy core", whiteSprite, Hex("#45eaff"), 5, parent),
                CapOuter = CreateRenderer($"{label} cap shell", whiteSprite, Hex("#030613"), 6, parent),
                CapAccent = CreateRenderer($"{label} cap accent", whiteSprite, Hex("#45eaff"), 7, parent),
                CapPanel = CreateRenderer($"{label} cap panel", whiteSprite, Hex("#0b3076"), 8, parent),
                CapEnergy = CreateRenderer($"{label} cap energy", whiteSprite, Hex("#45eaff"), 9, parent),
            };
        }

        private void CreateInterface()
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Arial", 16);

            uiRoot = new GameObject("SkyPulse interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(uiRoot);
            var canvas = uiRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = uiRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .65f;

            if (EventSystem.current == null)
            {
                var eventSystem = new GameObject("SkyPulse input", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(eventSystem);
            }

            homeScreen = CreateHomeScreen(uiRoot.transform);
            hudScreen = CreateHud(uiRoot.transform);
            pauseScreen = CreatePauseScreen(uiRoot.transform);
            gameOverScreen = CreateGameOverScreen(uiRoot.transform);
            customizeScreen = CreateCustomizeScreen(uiRoot.transform);
            purchaseModal = CreatePurchaseModal(uiRoot.transform);
            purchaseModal.SetActive(false);
        }

        private GameObject CreateHomeScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Home screen");

            var difficulty = CreateNeonButton(root.transform, "CLASSIC", new Vector2(-402f, 799f), new Vector2(184f, 70f), Hex("#8f64ff"));
            difficultyText = difficulty.GetComponentInChildren<Text>();
            difficulty.onClick.AddListener(CycleDifficulty);
            menuCrystalText = CreateChip(root.transform, new Vector2(365f, 799f), "✦  0", Hex("#45eaff"));

            CreateText(root.transform, "SKYPULSE", new Vector2(0f, 442f), new Vector2(880f, 130f), 82, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            CreateText(root.transform, "FLY THROUGH THE GLOW", new Vector2(0f, 348f), new Vector2(700f, 44f), 24, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);

            menuBirdImage = CreateImage(root.transform, "Menu bird", new Vector2(0f, 47f), new Vector2(510f, 290f), Color.white);
            menuBirdImage.preserveAspect = true;
            menuBirdImage.raycastTarget = false;
            menuBirdTransform = menuBirdImage.rectTransform;

            menuBestText = CreateText(root.transform, "BEST · 0", new Vector2(0f, -214f), new Vector2(480f, 44f), 25, new Color(.93f, .95f, 1f, .86f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var fly = CreateNeonButton(root.transform, "FLY", new Vector2(0f, -335f), new Vector2(576f, 104f), Hex("#f05bc6"));
            fly.onClick.AddListener(StartFlight);
            CreateText(root.transform, "TAP ANYWHERE TO START", new Vector2(0f, -414f), new Vector2(650f, 34f), 17, new Color(.91f, .92f, 1f, .64f), TextAnchor.MiddleCenter, FontStyle.Bold);

            var customize = CreateNeonButton(root.transform, "CUSTOMIZE", new Vector2(0f, -524f), new Vector2(576f, 88f), Hex("#45eaff"));
            customize.onClick.AddListener(OpenCustomize);
            var daily = CreateText(root.transform, "DAILY FLIGHT", new Vector2(0f, -625f), new Vector2(520f, 40f), 21, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            daily.raycastTarget = true;
            var dailyButton = daily.gameObject.AddComponent<Button>();
            dailyButton.targetGraphic = daily;
            dailyButton.onClick.AddListener(StartDailyFlight);
            menuEquippedText = CreateText(root.transform, "NOVA EQUIPPED", new Vector2(0f, -700f), new Vector2(650f, 36f), 17, Hex("#8f64ff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            return root;
        }

        private GameObject CreateHud(Transform parent)
        {
            var root = CreateScreen(parent, "Flight HUD");
            var pause = CreateNeonButton(root.transform, "Ⅱ", new Vector2(-425f, 804f), new Vector2(82f, 70f), Hex("#8f64ff"));
            pause.onClick.AddListener(PauseFlight);
            hudCrystalText = CreateChip(root.transform, new Vector2(365f, 804f), "✦  0", Hex("#45eaff"));
            hudScoreText = CreateText(root.transform, "0", new Vector2(0f, 708f), new Vector2(260f, 120f), 76, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            scoreBurstText = CreateText(root.transform, "+1", new Vector2(0f, 612f), new Vector2(220f, 70f), 34, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            scoreBurstText.gameObject.SetActive(false);
            hudPowerUpText = CreateText(root.transform, "", new Vector2(0f, 576f), new Vector2(600f, 38f), 19, Hex("#61f5b3"), TextAnchor.MiddleCenter, FontStyle.Bold);
            hudPowerUpText.gameObject.SetActive(false);
            return root;
        }

        private GameObject CreatePauseScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Pause screen");
            CreateFullPanel(root.transform, "Pause dim", new Color(.015f, .008f, .06f, .72f));
            var card = CreatePanel(root.transform, "Pause card", new Vector2(0f, 20f), new Vector2(760f, 430f), new Color(.055f, .025f, .16f, .96f));
            AddOutline(card.gameObject, Hex("#8f64ff"), 3f);
            CreateText(card, "PAUSED", new Vector2(0f, 116f), new Vector2(650f, 80f), 52, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            var resume = CreateNeonButton(card, "RESUME", new Vector2(0f, 5f), new Vector2(500f, 82f), Hex("#45eaff"));
            resume.onClick.AddListener(ResumeFlight);
            var menu = CreateNeonButton(card, "MENU", new Vector2(0f, -108f), new Vector2(500f, 72f), Hex("#8f64ff"));
            menu.onClick.AddListener(ResetToMenu);
            return root;
        }

        private GameObject CreateGameOverScreen(Transform parent)
        {
            var root = CreateScreen(parent, "Game over screen");
            CreateFullPanel(root.transform, "Frosted game over dim", new Color(.012f, .006f, .05f, .78f));
            var card = CreatePanel(root.transform, "Game over card", new Vector2(0f, 25f), new Vector2(820f, 700f), new Color(.055f, .022f, .17f, .96f));
            AddOutline(card.gameObject, Hex("#8f64ff"), 3.5f);
            CreateText(card, "GAME OVER", new Vector2(0f, 235f), new Vector2(720f, 78f), 54, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultNewBestText = CreateText(card, "NEW BEST", new Vector2(0f, 164f), new Vector2(500f, 42f), 25, Hex("#ffc34d"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultScoreText = CreateText(card, "SCORE  0", new Vector2(0f, 82f), new Vector2(600f, 54f), 31, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            resultBestText = CreateText(card, "BEST  0", new Vector2(0f, 25f), new Vector2(600f, 45f), 24, new Color(.93f, .95f, 1f, .78f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var flyAgain = CreateNeonButton(card, "FLY AGAIN", new Vector2(0f, -105f), new Vector2(570f, 88f), Hex("#f05bc6"));
            flyAgain.onClick.AddListener(StartFlight);
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

            var labels = new[] { "BIRDS", "WORLDS", "TRAILS", "PIPES" };
            var categories = new[] { CosmeticCategory.Birds, CosmeticCategory.Worlds, CosmeticCategory.Trails, CosmeticCategory.Pipes };
            for (var index = 0; index < labels.Length; index += 1)
            {
                var tab = CreateNeonButton(root.transform, labels[index], new Vector2(-342f + index * 228f, 560f), new Vector2(210f, 60f), index == 0 ? Hex("#45eaff") : Hex("#8f64ff"));
                var category = categories[index];
                tab.onClick.AddListener(() => SetCosmeticCategory(category));
            }

            var viewport = CreatePanel(root.transform, "Collection viewport", new Vector2(0f, -172f), new Vector2(970f, 1380f), new Color(.015f, .01f, .08f, .34f));
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
            var card = CreatePanel(root.transform, "Purchase card", new Vector2(0f, 18f), new Vector2(850f, 720f), new Color(.035f, .018f, .13f, .99f));
            AddOutline(card.gameObject, Hex("#45eaff"), 4f);
            var halo = CreateImage(card, "Purchase halo", new Vector2(0f, 128f), new Vector2(370f, 370f), new Color(.27f, .92f, 1f, .07f));
            halo.sprite = softCircleSprite;
            halo.raycastTarget = false;
            purchasePreviewImage = CreateImage(card, "Bird skin preview", new Vector2(0f, 128f), new Vector2(355f, 220f), Color.white);
            purchasePreviewImage.preserveAspect = true;
            purchasePreviewImage.raycastTarget = false;
            purchaseTitleText = CreateText(card, "UNLOCK BIRD?", new Vector2(0f, 278f), new Vector2(710f, 52f), 35, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            purchaseDetailText = CreateText(card, "", new Vector2(0f, -38f), new Vector2(700f, 52f), 24, Hex("#45eaff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            purchaseBalanceText = CreateText(card, "", new Vector2(0f, -84f), new Vector2(700f, 36f), 19, new Color(.9f, .94f, 1f, .72f), TextAnchor.MiddleCenter, FontStyle.Bold);
            CreateText(card, "THIS BIRD WILL BE EQUIPPED AFTER UNLOCKING", new Vector2(0f, -142f), new Vector2(720f, 32f), 15, new Color(.9f, .94f, 1f, .57f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var cancel = CreateNeonButton(card, "CANCEL", new Vector2(-190f, -250f), new Vector2(330f, 78f), Hex("#8f64ff"));
            cancel.onClick.AddListener(ClosePurchaseModal);
            purchaseConfirmButton = CreateNeonButton(card, "UNLOCK", new Vector2(190f, -250f), new Vector2(330f, 78f), Hex("#45eaff"));
            purchaseConfirmText = purchaseConfirmButton.GetComponentInChildren<Text>();
            purchaseConfirmButton.onClick.AddListener(ConfirmSkinPurchase);
            return root;
        }

        private void Update()
        {
            var deltaTime = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            ambientTime += deltaTime;
            UpdateAmbientVisuals();
            UpdateMenuBird(deltaTime);
            UpdateScoreBurst(deltaTime);

            if (state == FlightState.Menu)
            {
                if (WasTapped() && !PointerOverUi()) StartFlight();
                return;
            }

            if (state == FlightState.GameOver)
            {
                if (WasTapped() && !PointerOverUi()) StartFlight();
                return;
            }

            if (state != FlightState.Playing) return;

            if (WasTapped() && !PointerOverUi()) Flap();
            UpdatePowerUpEffects(deltaTime);
            UpdateBird(deltaTime);
            if (state != FlightState.Playing) return;
            UpdatePipes(deltaTime);
            UpdatePowerUps(deltaTime);
            UpdateTrail();
        }

        private void UpdateAmbientVisuals()
        {
            if (backgroundRenderer != null)
            {
                backgroundRenderer.transform.position = new Vector3(Mathf.Sin(ambientTime * .16f) * .045f, .12f + Mathf.Sin(ambientTime * .23f) * .035f, 0f);
            }
            foreach (var star in ambientStars)
            {
                var y = star.Y + Mathf.Sin(ambientTime * star.Speed + star.Phase) * .07f;
                star.Transform.position = new Vector3(star.X, y, 0f);
                var scale = .86f + Mathf.Sin(ambientTime * star.Speed * 1.6f + star.Phase) * .22f;
                star.Transform.localScale = Vector3.one * Mathf.Max(.012f, star.BaseSize * scale);
            }
            if (cometTrail != null)
            {
                var range = GetWorldWidth() + 3.4f;
                var x = Mathf.Repeat(ambientTime * 1.12f, range) - range * .5f;
                var y = 4.7f + Mathf.Sin(ambientTime * .42f) * .5f;
                cometTrail.SetPosition(0, new Vector3(x, y, 0f));
                cometTrail.SetPosition(1, new Vector3(x - .85f, y - .27f, 0f));
            }
        }

        private void UpdateMenuBird(float deltaTime)
        {
            if (state != FlightState.Menu || menuBirdImage == null) return;
            menuWingTimer += deltaTime;
            if (menuWingTimer > .72f) menuWingTimer = 0f;
            // Keeping one clean silhouette here avoids the pop caused by swapping
            // differently-sized artwork frames in the menu.
            menuBirdImage.sprite = LoadSprite(equippedSkin.ArtPath);
            var floatY = 47f + Mathf.Sin(ambientTime * 1.7f) * 12f;
            menuBirdTransform.anchoredPosition = new Vector2(Mathf.Sin(ambientTime * .85f) * 5f, floatY);
            menuBirdTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(ambientTime * 1.7f) * 2f);
            menuBirdTransform.localScale = Vector3.one * (1f + Mathf.Sin(ambientTime * 3.4f) * .018f);
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
            birdVelocity = Mathf.Max(MaxFallVelocity, birdVelocity + Gravity * deltaTime);
            birdY += birdVelocity * deltaTime;
            wingTimer += deltaTime;
            bird.position = new Vector3(BirdX, birdY, 0f);
            var targetTilt = Mathf.Clamp(birdVelocity * 3.05f, -31f, 25f);
            birdTilt = Mathf.Lerp(birdTilt, targetTilt, 1f - Mathf.Exp(-deltaTime * 11f));
            bird.rotation = Quaternion.Euler(0f, 0f, birdTilt);
            UpdateBirdWingMotion();

            if (birdY + BirdCollisionRadius >= CameraHeight * .5f || birdY - BirdCollisionRadius <= GroundY)
            {
                if (!UseShield())
                {
                    EndFlight();
                    return;
                }

                birdY = Mathf.Clamp(birdY, GroundY + BirdCollisionRadius + .14f, CameraHeight * .5f - BirdCollisionRadius - .14f);
                birdVelocity = FlapVelocity * .52f;
                bird.position = new Vector3(BirdX, birdY, 0f);
            }
        }

        private void UpdatePipes(float deltaTime)
        {
            var speed = ActiveScrollSpeed();
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

                // The cap is part of the obstacle too: if the art is visible, it is dangerous.
                var physicalWidth = PipeWidth + .25f;
                var overlapsPipe = BirdX + BirdCollisionRadius > pair.X - physicalWidth * .5f && BirdX - BirdCollisionRadius < pair.X + physicalWidth * .5f;
                var halfGap = ActiveGap() * .5f;
                var hitsPipe = birdY + BirdCollisionRadius > pair.GapCenter + halfGap || birdY - BirdCollisionRadius < pair.GapCenter - halfGap;
                if (overlapsPipe && hitsPipe)
                {
                    if (!UseShield())
                    {
                        EndFlight();
                        return;
                    }

                    pair.X = BirdX - physicalWidth - .22f;
                    pair.Root.transform.localPosition = new Vector3(pair.X, 0f, 0f);
                    pair.Passed = true;
                    continue;
                }

                if (!pair.Passed && pair.X + PipeWidth * .5f < BirdX - BirdCollisionRadius)
                {
                    pair.Passed = true;
                    score += 1;
                    crystals += 1;
                    hudScoreText.text = score.ToString();
                    ShowScoreBurst();
                    Play(scoreSound);
                    UpdateCrystalLabels();
                }
            }
        }

        private float ActiveScrollSpeed()
        {
            var speed = 4.3f + Mathf.Min(score, 24) * .045f;
            return slowFieldTimer > 0f ? speed * .52f : speed;
        }

        private void UpdatePowerUps(float deltaTime)
        {
            var furthestX = GetWorldWidth() * .5f + 1.5f;
            foreach (var pickup in powerUpPool) if (pickup.Active && pickup.X > furthestX) furthestX = pickup.X;

            foreach (var pickup in powerUpPool)
            {
                if (!pickup.Active)
                {
                    pickup.RespawnTimer -= deltaTime;
                    if (pickup.RespawnTimer <= 0f)
                    {
                        ConfigurePowerUp(pickup, furthestX + UnityEngine.Random.Range(7.5f, 10.2f));
                        furthestX = pickup.X;
                    }
                    continue;
                }
                pickup.X -= ActiveScrollSpeed() * deltaTime;
                var bob = Mathf.Sin(ambientTime * 3.2f + pickup.Phase) * .12f;
                pickup.Transform.localPosition = new Vector3(pickup.X, pickup.Y + bob, 0f);
                var pulse = 1f + Mathf.Sin(ambientTime * 4.2f + pickup.Phase) * .10f;
                pickup.Glow.transform.localScale = Vector3.one * (.88f * pulse);
                pickup.Ring.transform.localRotation = Quaternion.Euler(0f, 0f, ambientTime * 88f + pickup.Phase * 40f);
                pickup.Orbit.transform.localPosition = new Vector3(Mathf.Cos(ambientTime * 4.3f + pickup.Phase) * .35f, Mathf.Sin(ambientTime * 4.3f + pickup.Phase) * .35f, 0f);

                if (Vector2.Distance(new Vector2(BirdX, birdY), new Vector2(pickup.X, pickup.Y + bob)) <= BirdCollisionRadius + PickupRadius)
                {
                    CollectPowerUp(pickup);
                    continue;
                }

                if (pickup.X < -GetWorldWidth() * .5f - 1.2f)
                {
                    ConfigurePowerUp(pickup, furthestX + UnityEngine.Random.Range(7.5f, 10.2f));
                }
                furthestX = Mathf.Max(furthestX, pickup.X);
            }
        }

        private void ConfigurePowerUp(PowerUpPickup pickup, float x)
        {
            pickup.Root.SetActive(true);
            pickup.Active = true;
            pickup.RespawnTimer = 0f;
            pickup.X = x;
            pickup.Y = UnityEngine.Random.Range(-3.65f, 4.45f);
            pickup.Phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            pickup.Kind = (PowerUpKind)UnityEngine.Random.Range(0, 3);
            var colour = Hex("#8f64ff");
            var secondary = Hex("#45eaff");
            switch (pickup.Kind)
            {
                case PowerUpKind.PulseShield:
                    colour = Hex("#61f5b3");
                    secondary = Hex("#edf7ff");
                    pickup.Icon.transform.localScale = new Vector3(.14f, .20f, 1f);
                    pickup.Icon.transform.localRotation = Quaternion.identity;
                    break;
                case PowerUpKind.CrystalCache:
                    colour = Hex("#ffc34d");
                    secondary = Hex("#f05bc6");
                    pickup.Icon.transform.localScale = new Vector3(.17f, .17f, 1f);
                    pickup.Icon.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    break;
                default:
                    pickup.Icon.transform.localScale = new Vector3(.13f, .13f, 1f);
                    pickup.Icon.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    break;
            }

            pickup.Glow.color = new Color(colour.r, colour.g, colour.b, .20f);
            pickup.Ring.color = new Color(secondary.r, secondary.g, secondary.b, .95f);
            pickup.Core.color = colour;
            pickup.Icon.color = Color.white;
            pickup.Orbit.color = secondary;
            pickup.Transform.localPosition = new Vector3(pickup.X, pickup.Y, 0f);
        }

        private void CollectPowerUp(PowerUpPickup pickup)
        {
            pickup.Active = false;
            pickup.Root.SetActive(false);
            pickup.RespawnTimer = UnityEngine.Random.Range(5.5f, 8.5f);
            switch (pickup.Kind)
            {
                case PowerUpKind.SlowField:
                    slowFieldTimer = Mathf.Min(9f, Mathf.Max(slowFieldTimer, 0f) + 5.5f);
                    Play(crystalSound);
                    break;
                case PowerUpKind.PulseShield:
                    shieldCharges = 1;
                    shieldFlashTimer = .6f;
                    Play(unlockSound);
                    break;
                default:
                    crystals += 12;
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
            UpdatePowerUpHud();
        }

        private void UpdatePowerUpHud()
        {
            if (hudPowerUpText == null) return;
            if (slowFieldTimer > 0f)
            {
                var remainingTenths = Mathf.CeilToInt(slowFieldTimer * 10f);
                if (remainingTenths != displayedSlowTenths || !hudPowerUpText.gameObject.activeSelf)
                {
                    hudPowerUpText.text = $"◌  SLOW FIELD  {remainingTenths / 10f:0.0}s";
                    displayedSlowTenths = remainingTenths;
                }
                hudPowerUpText.color = Hex("#b17cff");
            }
            else if (shieldCharges > 0)
            {
                hudPowerUpText.text = "◈  PULSE SHIELD READY";
                hudPowerUpText.color = Hex("#61f5b3");
                displayedSlowTenths = -1;
            }
            else
            {
                hudPowerUpText.gameObject.SetActive(false);
                displayedSlowTenths = -1;
                return;
            }
            hudPowerUpText.gameObject.SetActive(true);
        }

        private bool UseShield()
        {
            if (shieldCharges <= 0) return false;
            shieldCharges = 0;
            shieldFlashTimer = .85f;
            Play(unlockSound);
            UpdatePowerUpHud();
            return true;
        }

        private void UpdateTrail()
        {
            for (var index = trailPoints.Length - 1; index > 0; index -= 1) trailPoints[index] = trailPoints[index - 1];
            trailPoints[0] = bird.position + new Vector3(-.66f, .02f, .1f);
            trailGlow.positionCount = trailPoints.Length;
            trailCore.positionCount = trailPoints.Length;
            trailGlow.SetPositions(trailPoints);
            trailCore.SetPositions(trailPoints);
        }

        private void ShowScoreBurst()
        {
            scoreBurstTimer = .36f;
            scoreBurstText.text = "+1";
            scoreBurstText.rectTransform.anchoredPosition = new Vector2(0f, 612f);
            scoreBurstText.gameObject.SetActive(true);
        }

        private void StartFlight()
        {
            ClosePurchaseModal();
            state = FlightState.Playing;
            score = 0;
            newBest = false;
            birdY = 0f;
            birdVelocity = 0f;
            birdTilt = 0f;
            wingTimer = 1f;
            slowFieldTimer = 0f;
            shieldFlashTimer = 0f;
            shieldCharges = 0;
            trailGlow.positionCount = 0;
            trailCore.positionCount = 0;
            spawnX = GetWorldWidth() * .5f + 3.2f;
            for (var index = 0; index < pipePool.Length; index += 1) ConfigurePipe(pipePool[index], spawnX + index * PipeSpacing);
            for (var index = 0; index < powerUpPool.Length; index += 1) ConfigurePowerUp(powerUpPool[index], spawnX + 7.8f + index * 8.9f);
            RefreshScreens();
            hudScoreText.text = "0";
            UpdatePowerUpHud();
            bird.gameObject.SetActive(true);
            Flap();
        }

        private void StartDailyFlight()
        {
            StartFlight();
        }

        private void ResetToMenu()
        {
            ClosePurchaseModal();
            state = FlightState.Menu;
            birdY = .15f;
            birdVelocity = 0f;
            birdTilt = 0f;
            bird.position = new Vector3(BirdX, birdY, 0f);
            bird.gameObject.SetActive(false);
            trailGlow.positionCount = 0;
            trailCore.positionCount = 0;
            foreach (var pair in pipePool) pair.Root.SetActive(false);
            foreach (var pickup in powerUpPool)
            {
                pickup.Active = false;
                pickup.Root.SetActive(false);
            }
            slowFieldTimer = 0f;
            shieldFlashTimer = 0f;
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
            pair.Root.SetActive(true);
            pair.X = x;
            pair.Passed = false;
            pair.GapCenter = UnityEngine.Random.Range(-1.72f, 1.92f);
            pair.Root.transform.localPosition = new Vector3(x, 0f, 0f);

            var halfGap = ActiveGap() * .5f;
            var topLowerEdge = pair.GapCenter + halfGap;
            var topHeight = CameraHeight * .5f - topLowerEdge;
            LayoutPipeSurface(pair.Top, topLowerEdge + topHeight * .5f, topHeight, topLowerEdge, true);

            var bottomUpperEdge = pair.GapCenter - halfGap;
            var bottomHeight = bottomUpperEdge - GroundY;
            LayoutPipeSurface(pair.Bottom, GroundY + bottomHeight * .5f, bottomHeight, bottomUpperEdge, false);
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
                art.color = Color.Lerp(Color.white, style.Accent, style.Id == "ion" ? 0f : .14f);
            }

            surface.Outer.enabled = false;
            surface.Panel.enabled = false;
            surface.Shade.enabled = false;
            surface.Highlight.enabled = false;
            surface.Energy.enabled = false;
            surface.CapOuter.enabled = false;
            surface.CapAccent.enabled = false;
            surface.CapPanel.enabled = false;
            surface.CapEnergy.enabled = false;
        }

        private float ActiveGap()
        {
            switch (difficultyIndex)
            {
                case 0: return 5.00f;
                case 2: return 3.78f;
                default: return 4.36f;
            }
        }

        private void Flap()
        {
            birdVelocity = FlapVelocity;
            wingTimer = 0f;
            Play(flapSound);
        }

        private void EndFlight()
        {
            if (state != FlightState.Playing) return;
            state = FlightState.GameOver;
            newBest = score > best && score > 0;
            best = Mathf.Max(best, score);
            SaveProgress();
            Play(crashSound);
            resultScoreText.text = $"SCORE  {score}";
            resultBestText.text = $"BEST  {best}";
            resultNewBestText.gameObject.SetActive(newBest);
            RefreshScreens();
        }

        private void OpenCustomize()
        {
            state = FlightState.Customize;
            bird.gameObject.SetActive(false);
            cosmeticCategory = CosmeticCategory.Birds;
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
                        CreateCosmeticCard(index, world.Name, equippedWorld.Id == world.Id ? "EQUIPPED" : "TAP TO EQUIP", world.Accent, LoadSprite(world.BackgroundPath), () => EquipWorld(world));
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
                default:
                    customizeTitle.text = "PIPE COLLECTION";
                    for (var index = 0; index < PipeStyles.Length; index += 1)
                    {
                        var style = PipeStyles[index];
                        CreateCosmeticCard(index, style.Name, equippedPipe.Id == style.Id ? "EQUIPPED" : "TAP TO EQUIP", style.Accent, null, () => EquipPipe(style), style.Panel, style.Energy, true);
                    }
                    SetContentRows(PipeStyles.Length);
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
            var card = CreatePanel(customizeContent, $"{title} card", new Vector2(column == 0 ? -235f : 235f, -12f - row * 250f), new Vector2(440f, 222f), new Color(.02f, .025f, .11f, .97f));
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

        private void SelectSkin(Skin skin)
        {
            if (IsSkinOwned(skin))
            {
                EquipSkin(skin);
                return;
            }
            OpenPurchaseModal(skin);
        }

        private void OpenPurchaseModal(Skin skin)
        {
            pendingSkin = skin;
            purchasePreviewImage.sprite = LoadSprite(skin.ArtPath);
            purchaseTitleText.text = $"UNLOCK {skin.Name}?";
            purchaseDetailText.text = $"SPEND  {skin.Price}  ✦  TO ADD THIS BIRD";
            var remainder = Mathf.Max(0, skin.Price - crystals);
            purchaseBalanceText.text = remainder == 0
                ? $"YOUR BALANCE · {crystals} ✦"
                : $"YOUR BALANCE · {crystals} ✦   ·   NEED {remainder} MORE";
            purchaseConfirmButton.interactable = crystals >= skin.Price;
            purchaseConfirmText.text = crystals >= skin.Price ? $"UNLOCK · {skin.Price} ✦" : "NOT ENOUGH ✦";
            purchaseModal.SetActive(true);
        }

        private void ClosePurchaseModal()
        {
            if (purchaseModal != null) purchaseModal.SetActive(false);
            pendingSkin = null;
        }

        private void ConfirmSkinPurchase()
        {
            if (pendingSkin == null || crystals < pendingSkin.Price) return;
            crystals -= pendingSkin.Price;
            ownedSkinIds.Add(pendingSkin.Id);
            equippedSkin = pendingSkin;
            ClosePurchaseModal();
            ApplyEquippedVisuals();
            SaveProgress();
            Play(unlockSound);
            RebuildCustomizeGrid();
        }

        private void EquipWorld(WorldTheme world)
        {
            equippedWorld = world;
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

            backgroundRenderer.sprite = LoadSprite(equippedWorld.BackgroundPath) ?? whiteSprite;
            FitToCameraHeight(backgroundRenderer, CameraHeight + .5f);
            backgroundVeil.color = new Color(equippedWorld.Accent.r, equippedWorld.Accent.g, equippedWorld.Accent.b, .11f);
            floorSurface.color = equippedWorld.Floor;
            floorGlow.color = equippedWorld.Accent;
            floorLip.color = Darken(equippedWorld.Floor, .65f);
            SetBirdArtwork();
            if (menuBirdImage != null) menuBirdImage.sprite = LoadSprite(equippedSkin.ArtPath);
            UpdateCrystalLabels();
            if (menuBestText != null) menuBestText.text = $"BEST · {best}";
            if (menuEquippedText != null) menuEquippedText.text = $"{equippedSkin.Name} EQUIPPED";
            UpdateDifficultyCopy();
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
            if (birdRenderer == null || birdFlapRenderer == null) return;
            var idle = LoadSprite(equippedSkin.ArtPath);
            var flap = LoadSprite(equippedSkin.FlapPath);
            if (idle != null)
            {
                birdRenderer.sprite = idle;
                idleBirdBaseScale = ArtworkScale(idle, BirdDisplayWidth);
                birdArt.localScale = idleBirdBaseScale;
            }
            if (flap != null)
            {
                birdFlapRenderer.sprite = flap;
                flapBirdBaseScale = ArtworkScale(flap, BirdDisplayWidth);
                birdFlapArt.localScale = flapBirdBaseScale;
            }
        }

        private static Vector3 ArtworkScale(Sprite sprite, float targetWidth)
        {
            var sourceWidth = Mathf.Max(.01f, sprite.bounds.size.x);
            return Vector3.one * (targetWidth / sourceWidth);
        }

        private void UpdateBirdWingMotion()
        {
            if (birdRenderer == null || birdFlapRenderer == null) return;
            // Cross-fading the two original SkyPulse wing poses is deliberately smoother
            // than snapping sprites on a timer. The idle body stays readable throughout.
            var flapStrength = Mathf.Clamp01(1f - wingTimer / .23f);
            flapStrength = flapStrength * flapStrength * (3f - 2f * flapStrength);
            var flapColour = Color.white;
            flapColour.a = flapStrength * .84f;
            birdFlapRenderer.color = flapColour;
            birdRenderer.color = new Color(1f, 1f, 1f, 1f - flapStrength * .16f);
            var breathing = 1f + Mathf.Sin(ambientTime * 5.2f) * .012f + flapStrength * .025f;
            var glideStretch = Mathf.Clamp(birdVelocity / FlapVelocity, -1f, 1f) * .028f;
            birdArt.localScale = Vector3.Scale(idleBirdBaseScale, new Vector3(breathing + glideStretch, breathing - glideStretch * .7f, 1f));
            birdFlapArt.localScale = Vector3.Scale(flapBirdBaseScale, new Vector3(1f + flapStrength * .038f, 1f - flapStrength * .022f, 1f));
            birdArt.localPosition = new Vector3(-flapStrength * .035f, Mathf.Sin(ambientTime * 7f) * .012f, 0f);
            birdFlapArt.localPosition = new Vector3(flapStrength * .02f, flapStrength * .045f, 0f);
            birdFlapArt.localRotation = Quaternion.Euler(0f, 0f, -flapStrength * 4f);
            UpdateBirdPowerUpVisuals();
        }

        private void UpdateBirdPowerUpVisuals()
        {
            if (slowAuraRenderer != null)
            {
                var slowPulse = 1f + Mathf.Sin(ambientTime * 6.5f) * .12f;
                slowAuraRenderer.enabled = slowFieldTimer > 0f;
                slowAuraRenderer.color = new Color(.55f, .35f, 1f, .48f + Mathf.Sin(ambientTime * 5f) * .12f);
                slowAuraRenderer.transform.localScale = Vector3.one * (1.42f * slowPulse);
                slowAuraRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -ambientTime * 110f);
            }
            if (shieldAuraRenderer != null)
            {
                var visible = shieldCharges > 0 || shieldFlashTimer > 0f;
                var flash = shieldFlashTimer > 0f ? 1f : .5f;
                shieldAuraRenderer.enabled = visible;
                shieldAuraRenderer.color = new Color(.38f, 1f, .70f, visible ? flash : 0f);
                shieldAuraRenderer.transform.localScale = Vector3.one * (1.2f + Mathf.Sin(ambientTime * 8f) * .08f + shieldFlashTimer * .25f);
                shieldAuraRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, ambientTime * 95f);
            }
        }

        private void UpdateDifficultyCopy()
        {
            if (difficultyText == null) return;
            switch (difficultyIndex)
            {
                case 0:
                    difficultyText.text = "EASY";
                    difficultyText.color = Hex("#61f5b3");
                    break;
                case 2:
                    difficultyText.text = "EXPERT";
                    difficultyText.color = Hex("#ffc34d");
                    break;
                default:
                    difficultyText.text = "CLASSIC";
                    difficultyText.color = Hex("#45eaff");
                    break;
            }
        }

        private void CycleDifficulty()
        {
            difficultyIndex = (difficultyIndex + 1) % 3;
            UpdateDifficultyCopy();
            PlayerPrefs.SetInt("skypulse.native.difficulty", difficultyIndex);
            PlayerPrefs.Save();
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
                menuBestText.text = $"BEST · {best}";
                menuEquippedText.text = $"{equippedSkin.Name} EQUIPPED";
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

        private void LoadProgress()
        {
            best = PlayerPrefs.GetInt("skypulse.native.best", 0);
            crystals = PlayerPrefs.GetInt("skypulse.native.crystals", 0);
            difficultyIndex = Mathf.Clamp(PlayerPrefs.GetInt("skypulse.native.difficulty", 1), 0, 2);
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
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt("skypulse.native.best", best);
            PlayerPrefs.SetInt("skypulse.native.crystals", crystals);
            PlayerPrefs.SetInt("skypulse.native.difficulty", difficultyIndex);
            PlayerPrefs.SetString("skypulse.native.skin", equippedSkin.Id);
            PlayerPrefs.SetString("skypulse.native.world", equippedWorld.Id);
            PlayerPrefs.SetString("skypulse.native.trail", equippedTrail.Id);
            PlayerPrefs.SetString("skypulse.native.pipe", equippedPipe.Id);
            PlayerPrefs.SetString("skypulse.native.owned-skins", string.Join(",", ownedSkinIds));
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
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            sprite = CreateSprite(texture);
            spriteCache[path] = sprite;
            return sprite;
        }

        private void FitToCameraHeight(SpriteRenderer renderer, float targetHeight)
        {
            if (renderer == null || renderer.sprite == null) return;
            var sourceHeight = Mathf.Max(.01f, renderer.sprite.bounds.size.y);
            renderer.transform.localScale = Vector3.one * (targetHeight / sourceHeight);
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
            return text;
        }

        private Text CreateChip(Transform parent, Vector2 position, string value, Color accent)
        {
            var shell = CreatePanel(parent, "Crystal chip", position, new Vector2(200f, 68f), new Color(.015f, .02f, .10f, .80f));
            AddOutline(shell.gameObject, accent, 2.5f);
            return CreateText(shell, value, Vector2.zero, new Vector2(180f, 48f), 23, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private Button CreateNeonButton(Transform parent, string label, Vector2 position, Vector2 size, Color accent)
        {
            var shell = CreatePanel(parent, "Button · " + label, position, size, accent);
            var shellImage = shell.GetComponent<Image>();
            var inner = CreatePanel(shell, "Button inner", Vector2.zero, size - new Vector2(8f, 8f), new Color(.05f, .02f, .15f, .94f));
            inner.GetComponent<Image>().raycastTarget = false;
            var sheen = CreatePanel(shell, "Button sheen", new Vector2(0f, size.y * .30f), new Vector2(size.x - 46f, 2f), new Color(1f, 1f, 1f, .20f));
            sheen.GetComponent<Image>().raycastTarget = false;
            var text = CreateText(shell, label, Vector2.zero, size - new Vector2(22f, 14f), label == "FLY" ? 34 : 22, Hex("#f4fbff"), TextAnchor.MiddleCenter, FontStyle.Bold);
            text.raycastTarget = false;
            var button = shell.gameObject.AddComponent<Button>();
            button.targetGraphic = shellImage;
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
