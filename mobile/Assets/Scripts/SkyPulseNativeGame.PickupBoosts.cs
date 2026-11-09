using UnityEngine;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        // EDIT HERE: these two automatic pickups belong only to Endless Route.
        // No save keys, flight forces, gate sizes or upgrade prices are changed.
        const float CrystalEchoSeconds = 6f;
        const int CrystalEchoMaxBonus = 6;
        const float RouteLensSeconds = 8f;
        float crystalEchoTimer, routeLensTimer;
        int crystalEchoBudget, runEchoPickupBonus;
        Sprite crystalEchoSprite, routeLensSprite;

        void ShowEndlessPickupBanner(string label, Color colour)
        {
            if (scoreBurstText == null) return;
            scoreBurstText.text = label;
            scoreBurstText.color = colour;
            scoreBurstText.rectTransform.anchoredPosition = new Vector2(0f, HudFeedbackY);
            scoreBurstText.rectTransform.localScale = Vector3.one;
            scoreBurstDuration = scoreBurstTimer = .95f;
            scoreBurstIsCrystal = false;
            scoreBurstText.gameObject.SetActive(true);
        }

        void ResetEndlessPickupBoosts()
        {
            crystalEchoTimer = routeLensTimer = 0f;
            crystalEchoBudget = runEchoPickupBonus = 0;
            UpdateRouteLensVisuals();
        }
        void TickEndlessPickupBoosts(float dt)
        {
            if (activeMode != SkyPulseMode.EndlessRoute || state != FlightState.Playing) return;
            dt = Mathf.Max(0f, dt);
            crystalEchoTimer = Mathf.Max(0f, crystalEchoTimer - dt);
            routeLensTimer = Mathf.Max(0f, routeLensTimer - dt);
            if (crystalEchoTimer <= 0f) crystalEchoBudget = 0;
        }
        int TakeCrystalEchoBonus()
        {
            if (activeMode != SkyPulseMode.EndlessRoute || state != FlightState.Playing || crystalEchoTimer <= 0f || crystalEchoBudget <= 0) return 0;
            crystalEchoBudget--;
            if (crystalEchoBudget == 0) crystalEchoTimer = 0f;
            return 1;
        }

        void CreateRouteLensGuide(PipePair pair)
        {
            // Eight small pooled brackets, not another gate or a solid corridor.
            pair.LensGuide = new GameObject("Route Lens · opening guide").transform;
            pair.LensGuide.SetParent(pair.Root.transform, false);
            pair.LensEdges = new SpriteRenderer[8];
            for (int i = 0; i < pair.LensEdges.Length; i++)
                pair.LensEdges[i] = CreateRenderer("Aperture corner " + (i + 1), whiteSprite, Color.clear, 9, pair.LensGuide);
            pair.LensGuide.gameObject.SetActive(false);
        }
        bool RouteLensCandidate(PipePair pair)
        {
            return pair != null && pair.Root.activeSelf && !pair.Passed && pair.X >= BirdX - PipeCapWidth * .5f;
        }
        void UpdateRouteLensVisuals()
        {
            bool active = activeMode == SkyPulseMode.EndlessRoute && (state == FlightState.Playing || state == FlightState.Paused) &&
                routeLensTimer > 0f && worldTransitionTimer <= 0f && worldRecoveryTimer <= 0f;
            for (int i = 0; i < pipePool.Length; i++)
            {
                var pair = pipePool[i];
                if (pair == null || pair.LensGuide == null) continue;
                bool show = active && RouteLensCandidate(pair);
                if (show)
                {
                    int nearer = 0;
                    for (int j = 0; j < pipePool.Length; j++)
                        if (j != i && RouteLensCandidate(pipePool[j]) && pipePool[j].X < pair.X) nearer++;
                    show = nearer < 2;
                }
                if (pair.LensGuide.gameObject.activeSelf != show) pair.LensGuide.gameObject.SetActive(show);
                if (!show) continue;
                pair.LensGuide.localPosition = new Vector3(0f, pair.GapCenter, -.02f);
                float halfWidth = PipeCapWidth * .5f + .10f;
                float halfHeight = Mathf.Max(.25f, pair.GapHeight * .5f - BirdHitboxHeight * .5f - .10f);
                float opacity = Mathf.Clamp01(routeLensTimer / .45f) * .82f;
                float shimmer = reduceMotionEnabled ? 1f : .92f + .08f * Mathf.Sin((RouteLensSeconds - routeLensTimer) * 2.5f);
                Color colour = new Color(.38f, .91f, 1f, opacity * shimmer);
                for (int corner = 0; corner < 4; corner++)
                {
                    float x = (corner % 2 == 0 ? -1f : 1f), y = (corner < 2 ? -1f : 1f);
                    SetSpriteBlock(pair.LensEdges[corner * 2], new Vector2(x * (halfWidth - .16f), y * halfHeight), new Vector2(.32f, .035f));
                    SetSpriteBlock(pair.LensEdges[corner * 2 + 1], new Vector2(x * halfWidth, y * (halfHeight - .12f)), new Vector2(.035f, .24f));
                    pair.LensEdges[corner * 2].color = pair.LensEdges[corner * 2 + 1].color = colour;
                }
            }
        }

        Sprite PowerUpArtwork(PowerUpKind kind)
        {
            if (kind == PowerUpKind.CrystalEcho) return crystalEchoSprite;
            if (kind == PowerUpKind.RouteLens) return routeLensSprite;
            return LoadSprite(PowerUpArtworkPath(kind));
        }
        void CreateEndlessPickupSprites()
        {
            // Rasterised once at startup, cached and shared by the pool and guide.
            // Faceted cores and a machined aperture distinguish these from currency.
            crystalEchoSprite = CreateEndlessPickupSprite(true);
            routeLensSprite = CreateEndlessPickupSprite(false);
        }
        void DisposeEndlessPickupSprites()
        {
            if (crystalEchoSprite != null) { Destroy(crystalEchoSprite.texture); Destroy(crystalEchoSprite); }
            if (routeLensSprite != null) { Destroy(routeLensSprite.texture); Destroy(routeLensSprite); }
        }
        static Sprite CreateEndlessPickupSprite(bool echo)
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = echo ? "Crystal Echo · twin cut quartz" : "Route Lens · machined aperture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x + .5f) / size - .5f, (y + .5f) / size - .5f);
                    Color pixel;
                    if (echo)
                    {
                        Color back = EchoFacet(p - new Vector2(.14f, .065f), .165f, .275f, true);
                        Color front = EchoFacet(p - new Vector2(-.10f, -.035f), .19f, .34f, false);
                        pixel = Color.Lerp(back, front, front.a);
                        pixel.a = Mathf.Max(back.a, front.a);
                    }
                    else
                    {
                        // A chamfered optical bezel with a dark, visibly open centre.
                        float radius = Mathf.Max(Mathf.Abs(p.x) * .88f + Mathf.Abs(p.y) * .35f, Mathf.Abs(p.y));
                        float outer = 1f - PickupEdge(.354f, .366f, radius);
                        float inner = PickupEdge(.245f, .255f, radius);
                        float metal = Mathf.Clamp01(.42f + p.y * .8f - p.x * .4f);
                        pixel = Color.Lerp(new Color(.08f, .16f, .25f), new Color(.70f, .84f, .93f), metal);
                        float rail = SoftFieldLine(Mathf.Abs(radius - .316f), .007f);
                        pixel = Color.Lerp(pixel, new Color(.30f, .94f, 1f), rail);
                        pixel.a = outer * inner;
                        float cross = Mathf.Max(SoftFieldLine(Mathf.Abs(p.y), .004f) * (1f - PickupEdge(.07f, .12f, Mathf.Abs(p.x))),
                            SoftFieldLine(Mathf.Abs(p.x), .004f) * (1f - PickupEdge(.07f, .12f, Mathf.Abs(p.y))));
                        if (cross > pixel.a) pixel = new Color(.88f, .98f, 1f, cross * .9f);
                        float anchor = Mathf.Min(Vector2.Distance(p, new Vector2(-.31f, .22f)), Vector2.Distance(p, new Vector2(.31f, -.22f)));
                        float glint = SoftFieldLine(anchor, .014f);
                        if (glint > pixel.a) pixel = new Color(1f, .87f, .57f, glint);
                    }
                    pixels[y * size + x] = pixel;
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return CreateSprite(texture, size);
        }
        // Input thresholds must be normalised: Unity SmoothStep interpolates its
        // first two arguments rather than treating them as distance boundaries.
        static float PickupEdge(float start, float end, float value)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));
        }
        static Color EchoFacet(Vector2 p, float width, float height, bool rear)
        {
            float shape = Mathf.Abs(p.x) / width + Mathf.Abs(p.y) / height;
            float alpha = 1f - PickupEdge(.97f, 1.02f, shape);
            float face = p.x < 0 ? .72f : .30f;
            if (p.y > 0) face += .22f;
            Color colour = Color.Lerp(new Color(.18f, .12f, .47f), new Color(.80f, .89f, 1f), face);
            if (rear) colour = Color.Lerp(colour, new Color(.24f, .62f, .96f), .38f);
            float edge = SoftFieldLine(Mathf.Abs(shape - .91f) * width, .004f);
            float seam = SoftFieldLine(Mathf.Abs(p.x + p.y * .14f), .004f) * (1f - PickupEdge(.85f, 1f, shape));
            colour = Color.Lerp(colour, new Color(.87f, .95f, 1f), Mathf.Max(edge * .85f, seam * .55f));
            colour.a = alpha;
            return colour;
        }
    }
}
