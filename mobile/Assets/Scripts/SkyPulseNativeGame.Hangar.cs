using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    // Hangar presentation only: carousel, swipe gestures, portraits and labels.
    // Edit BuildBirdHangarPage for layout/copy. Prices and training/breeding rules
    // live in BirdProgression.cs; bird identities/art paths stay in the main catalogue.
    public sealed partial class SkyPulseNativeGame
    {
        private void BuildBirdHangarPage()
        {
            if (BirdRoster == null || BirdRoster.Length == 0) return;

            hangarPageIndex = Mathf.Clamp(
                hangarPageIndex,
                0,
                BirdRoster.Length - 1);

            var skin = BirdRoster[hangarPageIndex];
            var owned = IsSkinOwned(skin);
            int unlockPrice=BirdUnlockPrice(skin);
            var equipped =
                equippedSkin != null &&
                equippedSkin.Id == skin.Id;

            var profile = GetBirdHangarProfile(skin);
            var accent = skin.Accent;

            var previousIndex = hangarPageIndex - 1;
            if (previousIndex < 0)
                previousIndex = BirdRoster.Length - 1;

            var nextIndex = hangarPageIndex + 1;
            if (nextIndex >= BirdRoster.Length)
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
                $"BAY {hangarPageIndex + 1:00}  /  {BirdRoster.Length:00}",
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
            BirdRoster[previousIndex],
            -HangarCarouselSpacing,
            false,
            ShowPreviousHangarBird);

            hangarNextBirdRoot = CreateHangarCarouselBird(
                hangarCarouselTrack,
                BirdRoster[nextIndex],
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
                "LEVEL "+BirdLevel(skin)+" · "+(BredBird(skin)?"BRED LINEAGE":"FOUNDER BIRD"),
                new Vector2(0f, 82f),
                new Vector2(660f, 44f),
                21,
                Hex("#b7cede"),
                TextAnchor.MiddleCenter,
                FontStyle.Normal);

            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 17;
            description.resizeTextMaxSize = 21;

            // Reward specialisations vary; tested flight handling stays shared.
            var traits=CreateText(informationGlass, BirdTraitSummary(skin), new Vector2(0f, 10f), new Vector2(690f, 42f), 25, Hex("#d2def0"), TextAnchor.MiddleCenter, FontStyle.Bold);
            traits.resizeTextForBestFit=true;traits.resizeTextMinSize=20;traits.resizeTextMaxSize=25;
            CreateText(informationGlass, $"YOUR CRYSTALS · {crystals:N0}", new Vector2(0f, -48f), new Vector2(690f, 42f), 27, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            var unlockProgress = owned ? "BIRD UNLOCK SAVINGS · "+birdProgression.BirdUnlockDiscountPercent+"%"
                : crystals >= unlockPrice ? BirdUnlockSaving(skin)
                : $"NEED {unlockPrice-crystals:N0} ✦ · TRAINING SAVINGS {birdProgression.BirdUnlockDiscountPercent}%";
            CreateText(informationGlass, unlockProgress, new Vector2(0f, -106f), new Vector2(710f, 42f), 23, Hex("#adc6dd"), TextAnchor.MiddleCenter, FontStyle.Normal);

            var actionLabel =
                equipped
                    ? "EQUIPPED"
                    : owned
                        ? "EQUIP BIRD"
                        : $"UNLOCK   ·   {unlockPrice:N0} ✦";

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
            var train=ProgressionButton(hangarPageRoot,"TRAIN BIRD",new Vector2(-205,-550),new Vector2(380,96),()=>OpenBirdTraining(skin));train.interactable=owned;
            var breed=ProgressionButton(hangarPageRoot,"BREED",new Vector2(205,-550),new Vector2(380,96),()=>OpenBreeding(skin));breed.interactable=owned;
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
                    hangarPageIndex = BirdRoster.Length - 1;

                if (hangarPageIndex >= BirdRoster.Length)
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

        private void ShowPreviousHangarBird()
        {
            if (BirdRoster == null || BirdRoster.Length == 0) return;

            hangarPageIndex--;

            if (hangarPageIndex < 0)
                hangarPageIndex = BirdRoster.Length - 1;

            RebuildCustomizeGrid();
        }

        private void ShowNextHangarBird()
        {
            if (BirdRoster == null || BirdRoster.Length == 0) return;

            hangarPageIndex++;

            if (hangarPageIndex >= BirdRoster.Length)
                hangarPageIndex = 0;

            RebuildCustomizeGrid();
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
    }
}
