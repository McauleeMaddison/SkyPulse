using UnityEngine;

namespace SkyPulse.Mobile
{
    // Original pipe art placement and fallback renderer layout. The current 3D
    // metal/LED appearance is in SkyPulseNativeGame.Depth.cs + EndlessRouteMetal.shader.
    // LayoutPipeSurface also positions the real colliders: preserve those dimensions
    // when changing artwork. Flight spacing/physics are owned by the main file.
    public sealed partial class SkyPulseNativeGame
    {
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
            var scanPhase = reduceMotionEnabled ? .48f : Mathf.Repeat(ambientTime * 1.05f + pipeX * .11f, 1f);
            var scanColour = Color.Lerp(equippedPipe.Energy, Color.white, .48f); scanColour.a = Mathf.Lerp(.16f, .48f, pulse);
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
            // PREMIUM NEON BASE
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
            // AUTHORED PREMIUM COLLAR
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
            // AUTHORED PIPE SHAFT
            // IMPORTANT:
            // The shaft deliberately overlaps BEHIND the collar.
            // ---------------------------------------------------------

            var farBodyY =
                centreY +
                direction *
                height * .5f;

            var collarBodyEdgeY =
                capY +
                direction *
                visualCapHeight;

            // Allow most of the collar depth to overlap the shaft.
            // This makes the pipe and collar physically read as one object.
            var shaftJoinY =
                capY +
                direction *
                (visualCapHeight * .28f);

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
            // STATIC ENERGY RAILS
            // ---------------------------------------------------------

            var railNearY =
                collarBodyEdgeY +
                direction * .12f;

            var railFarY =
                farBodyY -
                direction * .14f;

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

            // The authored shaft already supplies its main permanent detail.
            surface.Core.enabled = false;

            // Travelling energy packets.
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
            // PROTECTED ANIMATION REGION
            // ---------------------------------------------------------

            var visualCapHeight =
                PipeCapHeight * .82f;

            var collarBodyEdgeY =
                capY +
                direction *
                visualCapHeight;

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

            var shaftFarY =
                shaftCentre +
                direction *
                shaftHeight * .5f;

            // Animation starts well away from the collar.
            var packetNearY =
                collarBodyEdgeY +
                direction * .18f;

            var packetFarY =
                shaftFarY -
                direction * .22f;

            // ---------------------------------------------------------
            // PREMIUM COLOURS
            // ---------------------------------------------------------

            var cyan =
                Color.Lerp(
                    new Color(.07f, .95f, 1f),
                    equippedPipe.Accent,
                    .12f);

            var magenta =
                Color.Lerp(
                    new Color(1f, .015f, .72f),
                    equippedPipe.Energy,
                    .10f);

            // ---------------------------------------------------------
            // SUBTLE METAL BREATHING
            // ---------------------------------------------------------

            surface.Artwork.color =
                Color.Lerp(
                    Color.white,
                    equippedPipe.Accent,
                    .007f +
                    breathe * .012f);

            surface.CapPanel.color =
                Color.Lerp(
                    Color.white,
                    equippedPipe.Energy,
                    .003f +
                    breathe * .008f);

            // ---------------------------------------------------------
            // BASE POWER RAILS
            // ---------------------------------------------------------

            var leftRail =
                cyan;

            leftRail.a =
                .27f +
                breathe * .15f;

            surface.RailLeft.color =
                leftRail;

            var rightRail =
                Color.Lerp(
                    cyan,
                    Color.white,
                    .20f);

            rightRail.a =
                .25f +
                breathe * .16f;

            surface.RailRight.color =
                rightRail;

            // ---------------------------------------------------------
            // CENTRE MAGENTA ENERGY PACKET
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
                .03f +
                centreEnvelope * .91f;

            surface.CorePulse.color =
                centreColour;

            SetSpriteBlock(
                surface.CorePulse,
                new Vector2(
                    0f,
                    centreY),
                new Vector2(
                    PipeWidth *
                    (.090f +
                     centreEnvelope * .030f),
                    .46f +
                    centreEnvelope * .16f));

            // ---------------------------------------------------------
            // LEFT CYAN ENERGY PACKET
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
                    .048f +
                    cyanEnvelope * .020f,
                    .52f +
                    cyanEnvelope * .18f));

            // ---------------------------------------------------------
            // RIGHT MAGENTA / WHITE ENERGY PACKET
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
                    .050f +
                    secondaryEnvelope * .018f,
                    .45f +
                    secondaryEnvelope * .15f));

            // ---------------------------------------------------------
            // RAIL RESPONSE
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
                    surge * .09f);

            surface.RailLeft.color =
                surgedLeft;

            var surgedRight =
                surface.RailRight.color;

            surgedRight.a =
                Mathf.Clamp01(
                    surgedRight.a +
                    surge * .10f);

            surface.RailRight.color =
                surgedRight;

            // ---------------------------------------------------------
            // HARD CAP SAFETY
            // ---------------------------------------------------------

            surface.CapGlow.enabled = false;
            surface.CapOuter.enabled = false;
            surface.CapAccent.enabled = false;
            surface.CapEnergy.enabled = false;
        }

        private void LayoutPipeSurface(
            PipeSurface surface,
            float centreY,
            float height,
            float capY,
            bool topPipe)
        {
            var style = equippedPipe;

            var direction =
                topPipe ? 1f : -1f;

            var capCentre =
                capY +
                direction *
                (PipeCapHeight * .5f);

            // ---------------------------------------------------------
            // GAMEPLAY COLLISION
            // ---------------------------------------------------------

            SetPipeCollider(
                surface.BodyCollider,
                new Vector2(
                    0f,
                    centreY),
                new Vector2(
                    PipeWidth,
                    height));

            SetPipeCollider(
                surface.CapCollider,
                new Vector2(
                    0f,
                    capCentre),
                new Vector2(
                    PipeCollisionWidth,
                    PipeCapHeight));

            // ---------------------------------------------------------
            // PREMIUM NEON PIPE PATH
            // ---------------------------------------------------------

            if (hasNeonPipeArtwork)
            {
                LayoutNeonPipeSurface(
                    surface,
                    centreY,
                    height,
                    capY,
                    topPipe);

                return;
            }

            // ---------------------------------------------------------
            // FALLBACK / NON-NEON PIPE PATH
            // ---------------------------------------------------------

            LayoutPlumbingGate(
                surface,
                centreY,
                height,
                capY,
                topPipe,
                style);

            var insideOffset =
                direction * .048f;

            var useProceduralBodyDetails =
                !hasAuthoredPipeBody;

            // ---------------------------------------------------------
            // PROCEDURAL BODY ENERGY
            // ---------------------------------------------------------

            surface.Core.enabled =
                useProceduralBodyDetails;

            surface.CorePulse.enabled =
                useProceduralBodyDetails;

            surface.Core.sortingOrder = 6;
            surface.CorePulse.sortingOrder = 7;

            if (useProceduralBodyDetails)
            {
                var coreColor =
                    style.Energy;

                coreColor.a = .10f;

                surface.Core.color =
                    coreColor;

                SetBlock(
                    surface.Core,
                    new Vector2(
                        0f,
                        centreY),
                    new Vector2(
                        PipeWidth * .26f,
                        Mathf.Max(
                            .16f,
                            height - .46f)));

                surface.CorePulse.color =
                    new Color(
                        coreColor.r,
                        coreColor.g,
                        coreColor.b,
                        0f);

                surface.CorePulse.transform.localPosition =
                    new Vector3(
                        0f,
                        capY +
                        direction * .68f,
                        0f);

                surface.CorePulse.transform.localScale =
                    new Vector3(
                        .34f,
                        .09f,
                        1f);
            }

            // ---------------------------------------------------------
            // FALLBACK CAP DETAILS
            // ---------------------------------------------------------

            surface.Energy.enabled = false;
            surface.Energy.sortingOrder = 9;

            var seamColor =
                style.Energy;

            seamColor.a = .60f;

            surface.Energy.color =
                seamColor;

            surface.Energy.transform.localPosition =
                new Vector3(
                    0f,
                    capY + insideOffset,
                    0f);

            surface.Energy.transform.localScale =
                new Vector3(
                    PipeWidth * .58f,
                    .018f,
                    1f);

            surface.Highlight.enabled = false;
            surface.Highlight.sortingOrder = 10;

            surface.Highlight.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    .10f);

            surface.Highlight.transform.localPosition =
                new Vector3(
                    0f,
                    capY +
                    insideOffset * .45f,
                    0f);

            surface.Highlight.transform.localScale =
                new Vector3(
                    PipeWidth * .52f,
                    .006f,
                    1f);

            surface.Scan.enabled = false;
            surface.Scan.sortingOrder = 10;

            var scanColor =
                style.Energy;

            scanColor.a = .14f;

            surface.Scan.color =
                scanColor;

            surface.Scan.transform.localPosition =
                new Vector3(
                    0f,
                    capY +
                    direction * .36f,
                    0f);

            surface.Scan.transform.localScale =
                new Vector3(
                    PipeWidth * .60f,
                    .006f,
                    1f);

            surface.Beacon.enabled = false;
            surface.Beacon.sortingOrder = 12;

            var beaconColor =
                style.Energy;

            beaconColor.a = .20f;

            surface.Beacon.color =
                beaconColor;

            surface.Beacon.transform.localPosition =
                new Vector3(
                    0f,
                    capY +
                    direction * .10f,
                    0f);

            surface.Beacon.transform.localScale =
                Vector3.one * .24f;

            // Never create a broad floating collar halo.
            surface.CapGlow.enabled = false;
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
    }
}
