using UnityEngine;

namespace SkyPulse.Mobile
{
    public sealed class SkyPulseNativeGame : MonoBehaviour
    {
        private enum FlightState { Menu, Playing, GameOver }

        private sealed class PipePair
        {
            public GameObject Root;
            public SpriteRenderer Top;
            public SpriteRenderer Bottom;
            public SpriteRenderer TopCap;
            public SpriteRenderer BottomCap;
            public float X;
            public float GapCenter;
            public bool Passed;
        }

        private const float CameraHeight = 18f;
        private const float GroundY = -7.35f;
        private const float BirdX = -2.45f;
        private const float BirdRadius = .34f;
        private const float PipeWidth = 1.3f;
        private const float PipeSpacing = 6.7f;
        private const float ClassicGap = 4.75f;
        private const float Gravity = -18.2f;
        private const float FlapVelocity = 6.25f;
        private const float MaxFallVelocity = -11.2f;

        private readonly PipePair[] pipePool = new PipePair[4];
        private readonly Vector3[] trailPoints = new Vector3[8];
        private FlightState state;
        private Camera flightCamera;
        private SpriteRenderer birdRenderer;
        private Transform bird;
        private LineRenderer trail;
        private AudioSource audioSource;
        private AudioClip flapSound;
        private AudioClip scoreSound;
        private AudioClip crashSound;
        private Sprite novaSprite;
        private Sprite novaFlapSprite;
        private Sprite whiteSprite;
        private float birdY;
        private float birdVelocity;
        private float wingTimer;
        private float spawnX;
        private int score;
        private int best;
        private GUIStyle scoreStyle;
        private GUIStyle messageStyle;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Time.maximumDeltaTime = 1f / 30f;

            CreateCamera();
            CreateVisuals();
            ResetToMenu();
        }

        private void CreateCamera()
        {
            var cameraObject = new GameObject("SkyPulse Camera");
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            flightCamera = cameraObject.AddComponent<Camera>();
            flightCamera.orthographic = true;
            flightCamera.orthographicSize = CameraHeight * .5f;
            flightCamera.clearFlags = CameraClearFlags.SolidColor;
            flightCamera.backgroundColor = new Color(.018f, .008f, .07f);
            cameraObject.AddComponent<AudioListener>();
        }

        private void CreateVisuals()
        {
            whiteSprite = CreateSprite(Texture2D.whiteTexture, 1f);
            novaSprite = LoadSprite("SkyPulse/characters/nova") ?? whiteSprite;
            novaFlapSprite = LoadSprite("SkyPulse/characters/nova-flap") ?? novaSprite;

            var background = CreateRenderer("Neon City", LoadSprite("SkyPulse/backgrounds/neon-city") ?? whiteSprite, new Color(1f, 1f, 1f, .96f), -30);
            background.transform.position = new Vector3(0f, .05f, 0f);
            FitToCameraHeight(background, CameraHeight + .5f);

            var floor = CreateRenderer("Solid Floor", whiteSprite, new Color(.025f, .012f, .10f), -4);
            floor.transform.position = new Vector3(0f, GroundY - .55f, 0f);
            floor.transform.localScale = new Vector3(GetWorldWidth() + 2f, 1.1f, 1f);
            var floorEdge = CreateRenderer("Floor Edge", whiteSprite, new Color(.27f, .92f, 1f, .9f), -3);
            floorEdge.transform.position = new Vector3(0f, GroundY, 0f);
            floorEdge.transform.localScale = new Vector3(GetWorldWidth() + 2f, .12f, 1f);

            bird = new GameObject("Nova").transform;
            birdRenderer = bird.gameObject.AddComponent<SpriteRenderer>();
            birdRenderer.sprite = novaSprite;
            birdRenderer.sortingOrder = 10;
            bird.localScale = Vector3.one * .45f;

            trail = bird.gameObject.AddComponent<LineRenderer>();
            trail.useWorldSpace = true;
            trail.positionCount = 0;
            trail.sortingOrder = 8;
            trail.startWidth = .11f;
            trail.endWidth = .025f;
            trail.startColor = new Color(.96f, .35f, .82f, .78f);
            trail.endColor = new Color(.27f, .92f, 1f, 0f);
            trail.numCapVertices = 2;
            var trailShader = Shader.Find("Sprites/Default");
            if (trailShader != null) trail.material = new Material(trailShader);

            for (var index = 0; index < pipePool.Length; index += 1) pipePool[index] = CreatePipePair(index);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            flapSound = Resources.Load<AudioClip>("SkyPulse/audio/flap");
            scoreSound = Resources.Load<AudioClip>("SkyPulse/audio/score");
            crashSound = Resources.Load<AudioClip>("SkyPulse/audio/crash");
        }

        private PipePair CreatePipePair(int index)
        {
            var root = new GameObject($"Pipe Pair {index}");
            root.transform.SetParent(transform, false);
            return new PipePair
            {
                Root = root,
                Top = CreateRenderer("Top Pipe", whiteSprite, new Color(.025f, .12f, .34f), 3, root.transform),
                Bottom = CreateRenderer("Bottom Pipe", whiteSprite, new Color(.025f, .12f, .34f), 3, root.transform),
                TopCap = CreateRenderer("Top Pipe Edge", whiteSprite, new Color(.28f, .92f, 1f), 4, root.transform),
                BottomCap = CreateRenderer("Bottom Pipe Edge", whiteSprite, new Color(.28f, .92f, 1f), 4, root.transform),
            };
        }

        private void Update()
        {
            if (WasTapped())
            {
                if (state == FlightState.Playing) Flap();
                else StartFlight();
            }

            if (state != FlightState.Playing) return;

            var deltaTime = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            UpdateBird(deltaTime);
            if (state != FlightState.Playing) return;
            UpdatePipes(deltaTime);
            UpdateTrail();
        }

        private void UpdateBird(float deltaTime)
        {
            birdVelocity = Mathf.Max(MaxFallVelocity, birdVelocity + Gravity * deltaTime);
            birdY += birdVelocity * deltaTime;
            wingTimer += deltaTime;
            bird.position = new Vector3(BirdX, birdY, 0f);
            bird.eulerAngles = new Vector3(0f, 0f, Mathf.Clamp(birdVelocity * 3.2f, -30f, 24f));
            birdRenderer.sprite = wingTimer < .15f ? novaFlapSprite : novaSprite;
            bird.localScale = Vector3.one * (.45f + Mathf.Max(0f, .15f - wingTimer) * .14f);

            if (birdY + BirdRadius >= CameraHeight * .5f || birdY - BirdRadius <= GroundY) EndFlight();
        }

        private void UpdatePipes(float deltaTime)
        {
            var speed = 4.25f + Mathf.Min(score, 25) * .045f;
            var furthestX = float.MinValue;
            foreach (var pair in pipePool) if (pair.X > furthestX) furthestX = pair.X;

            foreach (var pair in pipePool)
            {
                pair.X -= speed * deltaTime;
                pair.Root.transform.localPosition = new Vector3(pair.X, 0f, 0f);
                if (pair.X < -GetWorldWidth() * .5f - PipeWidth) ConfigurePipe(pair, furthestX + PipeSpacing);
                furthestX = Mathf.Max(furthestX, pair.X);

                var overlapsPipe = BirdX + BirdRadius > pair.X - PipeWidth * .5f && BirdX - BirdRadius < pair.X + PipeWidth * .5f;
                var hitsPipe = birdY + BirdRadius > pair.GapCenter + ClassicGap * .5f || birdY - BirdRadius < pair.GapCenter - ClassicGap * .5f;
                if (overlapsPipe && hitsPipe) EndFlight();

                if (!pair.Passed && pair.X + PipeWidth * .5f < BirdX - BirdRadius)
                {
                    pair.Passed = true;
                    score += 1;
                    Play(scoreSound);
                }
            }
        }

        private void UpdateTrail()
        {
            for (var index = trailPoints.Length - 1; index > 0; index -= 1) trailPoints[index] = trailPoints[index - 1];
            trailPoints[0] = bird.position + new Vector3(-.46f, 0f, .1f);
            trail.positionCount = trailPoints.Length;
            trail.SetPositions(trailPoints);
        }

        private void StartFlight()
        {
            state = FlightState.Playing;
            score = 0;
            birdY = 0f;
            birdVelocity = 0f;
            wingTimer = 1f;
            trail.positionCount = 0;
            spawnX = GetWorldWidth() * .5f + 3f;
            for (var index = 0; index < pipePool.Length; index += 1) ConfigurePipe(pipePool[index], spawnX + index * PipeSpacing);
            Flap();
        }

        private void ResetToMenu()
        {
            state = FlightState.Menu;
            birdY = .15f;
            birdVelocity = 0f;
            bird.position = new Vector3(BirdX, birdY, 0f);
            foreach (var pair in pipePool) pair.Root.SetActive(false);
        }

        private void ConfigurePipe(PipePair pair, float x)
        {
            pair.Root.SetActive(true);
            pair.X = x;
            pair.Passed = false;
            pair.GapCenter = Random.Range(-1.75f, 1.95f);
            pair.Root.transform.localPosition = new Vector3(x, 0f, 0f);

            var topLowerEdge = pair.GapCenter + ClassicGap * .5f;
            var topHeight = CameraHeight * .5f - topLowerEdge;
            SetBlock(pair.Top, new Vector2(0f, topLowerEdge + topHeight * .5f), new Vector2(PipeWidth, topHeight));
            SetBlock(pair.TopCap, new Vector2(0f, topLowerEdge), new Vector2(PipeWidth + .16f, .17f));

            var bottomUpperEdge = pair.GapCenter - ClassicGap * .5f;
            var bottomHeight = bottomUpperEdge - GroundY;
            SetBlock(pair.Bottom, new Vector2(0f, GroundY + bottomHeight * .5f), new Vector2(PipeWidth, bottomHeight));
            SetBlock(pair.BottomCap, new Vector2(0f, bottomUpperEdge), new Vector2(PipeWidth + .16f, .17f));
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
            best = Mathf.Max(best, score);
            Play(crashSound);
        }

        private bool WasTapped()
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
            return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow);
        }

        private void Play(AudioClip clip)
        {
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, Color color, int sortingOrder, Transform parent = null)
        {
            var visual = new GameObject(name);
            if (parent != null) visual.transform.SetParent(parent, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static Sprite CreateSprite(Texture2D texture, float pixelsPerUnit = 100f)
        {
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(.5f, .5f), pixelsPerUnit);
        }

        private static Sprite LoadSprite(string path)
        {
            var texture = Resources.Load<Texture2D>(path);
            return texture == null ? null : CreateSprite(texture);
        }

        private void FitToCameraHeight(SpriteRenderer renderer, float targetHeight)
        {
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

        private void OnGUI()
        {
            EnsureStyles();
            var width = Screen.width;
            if (state == FlightState.Playing)
            {
                GUI.Label(new Rect(0f, 44f, width, 74f), score.ToString(), scoreStyle);
                return;
            }

            var title = state == FlightState.Menu ? "SKYPULSE" : "GAME OVER";
            var message = state == FlightState.Menu ? "TAP ANYWHERE TO FLY" : $"SCORE {score}   ·   BEST {best}\nTAP ANYWHERE TO FLY AGAIN";
            GUI.Label(new Rect(0f, Screen.height * .15f, width, 86f), title, scoreStyle);
            GUI.Label(new Rect(0f, Screen.height * .15f + 88f, width, 96f), message, messageStyle);
        }

        private void EnsureStyles()
        {
            if (scoreStyle != null) return;
            scoreStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(.94f, .97f, 1f) },
            };
            messageStyle = new GUIStyle(scoreStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(.29f, .92f, 1f) },
            };
        }
    }
}
