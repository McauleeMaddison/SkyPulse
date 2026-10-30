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
        // The featured bird shares the existing 3D portrait studio used by training
        // and breeding. Its cradle/rig are meshes, with one transparent render target.
        private SkyPulseProgressionStage hangarPreviewStage;

        private void BuildBirdHangarPage()
        {
            if (BirdRoster == null || BirdRoster.Length == 0) return;
            hangarPageIndex = Mathf.Clamp(hangarPageIndex, 0, BirdRoster.Length - 1);
            var skin = BirdRoster[hangarPageIndex];
            bool owned = IsSkinOwned(skin);
            int unlockPrice = BirdUnlockPrice(skin);
            bool equipped = equippedSkin != null && equippedSkin.Id == skin.Id;
            var profile = GetBirdHangarProfile(skin);
            var accent = skin.Accent;
            int previousIndex = (hangarPageIndex + BirdRoster.Length - 1) % BirdRoster.Length;
            int nextIndex = (hangarPageIndex + 1) % BirdRoster.Length;

            customizeContent.sizeDelta = new Vector2(0, 1200);
            customizeContent.anchoredPosition = Vector2.zero;
            var page = new GameObject("Bird carousel page", typeof(RectTransform));
            page.transform.SetParent(customizeContent, false);
            hangarPageRoot = page.GetComponent<RectTransform>();
            hangarPageRoot.anchorMin = hangarPageRoot.anchorMax = new Vector2(.5f, 1);
            hangarPageRoot.pivot = new Vector2(.5f, 1);
            hangarPageRoot.anchoredPosition = new Vector2(0, -12);
            hangarPageRoot.sizeDelta = new Vector2(920, 1080);
            hangarPageIndicatorText = HangarLabel(hangarPageRoot,
                $"BAY {hangarPageIndex + 1:00}  /  {BirdRoster.Length:00}",
                new Vector2(0, 492), new Vector2(500, 38), 24, Hex("#b6d6e3"));

            var track = new GameObject("Bird carousel track", typeof(RectTransform));
            track.transform.SetParent(hangarPageRoot, false);
            hangarCarouselTrack = track.GetComponent<RectTransform>();
            hangarCarouselTrack.sizeDelta = new Vector2(1250, 420);
            hangarCarouselTrack.anchoredPosition = new Vector2(0, 255);
            hangarPreviousBirdRoot = CreateHangarCarouselBird(hangarCarouselTrack, BirdRoster[previousIndex], -HangarCarouselSpacing, false, ShowPreviousHangarBird);
            hangarNextBirdRoot = CreateHangarCarouselBird(hangarCarouselTrack, BirdRoster[nextIndex], HangarCarouselSpacing, false, ShowNextHangarBird);
            hangarCurrentBirdRoot = CreateHangarCarouselBird(hangarCarouselTrack, skin, 0, true, null);
            hangarPreviousBirdCanvas = hangarPreviousBirdRoot.GetComponent<CanvasGroup>();
            hangarCurrentBirdCanvas = hangarCurrentBirdRoot.GetComponent<CanvasGroup>();
            hangarNextBirdCanvas = hangarNextBirdRoot.GetComponent<CanvasGroup>();

            var swipe = CreateImage(hangarPageRoot, "Hangar swipe surface", new Vector2(0, 255),
                new Vector2(900, 420), new Color(1, 1, 1, .001f));
            swipe.raycastTarget = true;
            var trigger = swipe.gameObject.AddComponent<EventTrigger>();
            AddHangarSwipeEvent(trigger, EventTriggerType.BeginDrag, OnHangarBeginDrag);
            AddHangarSwipeEvent(trigger, EventTriggerType.Drag, OnHangarDrag);
            AddHangarSwipeEvent(trigger, EventTriggerType.EndDrag, OnHangarEndDrag);
            // Explicit controls remain above the swipe surface: neighbour artwork
            // can never hide the only way to browse the collection.
            HangarBrowseButton("‹", -390, ShowPreviousHangarBird);
            HangarBrowseButton("›", 390, ShowNextHangarBird);
            HangarLabel(hangarPageRoot, "SWIPE TO EXPLORE", new Vector2(0, 37),
                new Vector2(560, 36), 21, Hex("#91b9ca"));

            var glass = ProgressionSurface(hangarPageRoot, "Flight profile glass",
                new Vector2(0, -225), new Vector2(800, 470), SkyPulseProgressionSurface.Finish.Quiet);
            glass.Accent = accent;
            hangarInformationCanvas = glass.gameObject.AddComponent<CanvasGroup>();
            hangarInformationCanvas.alpha = hangarInformationFadeTime >= 0 ? 0 : 1;
            HangarLabel(glass.transform, skin.Name, new Vector2(0, 175), new Vector2(720, 62), 44, Hex("#effaff"));
            HangarLabel(glass.transform, profile.Rarity + "  /  LEVEL " + BirdLevel(skin),
                new Vector2(0, 121), new Vector2(700, 40), 24, profile.RarityColour);
            HangarLabel(glass.transform, BredBird(skin) ? "INHERITED LINEAGE" : "FOUNDER BIRD",
                new Vector2(0, 83), new Vector2(700, 32), 20, Hex("#9ebac9"));
            var genome = birdProgression.Genome(skin.Id);
            HangarTrait(glass.transform, genome, (BirdTrait)genome.primary, -190, accent);
            HangarTrait(glass.transform, genome, (BirdTrait)genome.secondary, 190, accent);
            string saving = owned
                ? "TRAINING SAVES " + birdProgression.BirdUnlockDiscountPercent + "% ON NEW BIRDS"
                : crystals >= unlockPrice ? BirdUnlockSaving(skin)
                : $"NEED {unlockPrice - crystals:N0} MORE ✦  /  BALANCE {crystals:N0}";
            HangarLabel(glass.transform, saving, new Vector2(0, -108), new Vector2(740, 42), 23, Hex("#accbd9"));
            string action = equipped ? "EQUIPPED" : owned ? "EQUIP BIRD" : $"UNLOCK · {unlockPrice:N0} ✦";
            var select = CreateNeonButton(glass.transform, action, new Vector2(0, -182), new Vector2(620, 88), accent);
            select.GetComponentInChildren<Text>().fontSize = 29;
            select.interactable = !equipped;
            if (!equipped) select.onClick.AddListener(() => SelectSkin(skin));
            var train = ProgressionButton(hangarPageRoot, "TRAIN BIRD", new Vector2(-205, -550),
                new Vector2(380, 152), () => OpenBirdTraining(skin));
            var breed = ProgressionButton(hangarPageRoot, "BREED", new Vector2(205, -550),
                new Vector2(380, 152), () => OpenBreeding(skin));
            train.interactable = breed.interactable = owned;
            HangarLabel(hangarPageRoot, owned ? "TRAIN FOR STRONGER TRAITS  /  BREED A NEW BIRD" : "UNLOCK TO TRAIN AND BREED",
                new Vector2(0, -658), new Vector2(850, 42), 21, Hex("#a3c0d1"));
        }

        Text HangarLabel(Transform parent, string copy, Vector2 at, Vector2 size, int font, Color colour)
        {
            var text = ProgressionLabel(parent, copy, at, size, font, colour, TextAnchor.MiddleCenter);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(19, font - 6);
            text.resizeTextMaxSize = font;
            text.fontStyle = FontStyle.Normal;
            return text;
        }

        void HangarTrait(Transform parent, BirdGenome genome, BirdTrait trait, float x, Color accent)
        {
            var tile = ProgressionSurface(parent, "Bird trait · " + trait, new Vector2(x, -16),
                new Vector2(350, 126), SkyPulseProgressionSurface.Finish.Quiet);
            tile.Accent = accent;
            HangarLabel(tile.transform, trait.ToString().ToUpperInvariant(), new Vector2(0, 38),
                new Vector2(330, 30), 20, Hex("#a3bdcd"));
            HangarLabel(tile.transform, TraitValue(trait, SkyPulseBirdProgression.Points(genome, trait)), Vector2.zero,
                new Vector2(330, 46), 34, accent);
            HangarLabel(tile.transform, TraitContext(trait), new Vector2(0, -42),
                new Vector2(330, 35), 21, Hex("#c5dce5"));
        }

        void HangarBrowseButton(string label, float x, Action browse)
        {
            var control = ProgressionButton(hangarPageRoot, label, new Vector2(x, 255),
                new Vector2(152, 168), () => { if (!hangarDragging && !hangarSwipeAnimating) browse(); });
            ((SkyPulseProgressionSurface)control.targetGraphic).Style = SkyPulseProgressionSurface.Finish.Quiet;
            control.GetComponentInChildren<Text>().fontSize = 48;
            control.GetComponentInChildren<Text>().resizeTextMinSize = 48;
            control.GetComponentInChildren<Text>().resizeTextMaxSize = 48;
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
            if (state != FlightState.Customize || cosmeticCategory != CosmeticCategory.Birds) return;
            if (hangarPreviewStage != null)
            {
                hangarPreviewStage.ReducedMotion = reduceMotionEnabled;
                hangarPreviewStage.MotionPaused = birdProgressionScreen != null && birdProgressionScreen.activeInHierarchy;
            }
            if (!reduceMotionEnabled) hangarHeroAnimationTime += Time.unscaledDeltaTime;
            if (progressionTypographyMaterial != null)
            {
                progressionTypographyMaterial.SetFloat("_Clock", hangarHeroAnimationTime);
                progressionTypographyMaterial.SetFloat("_Motion", reduceMotionEnabled ? 0 : 1);
            }
            // Information responds even when a bird has a single authored pose.
            if (hangarInformationFadeTime >= 0 && hangarInformationCanvas != null)
            {
                hangarInformationFadeTime += Time.unscaledDeltaTime;
                float progress = reduceMotionEnabled ? 1 : Mathf.Clamp01(hangarInformationFadeTime / HangarInformationFadeDuration);
                hangarInformationCanvas.alpha = Mathf.SmoothStep(0, 1, progress);
                if (progress >= 1) hangarInformationFadeTime = -1;
            }
            if (hangarHeroAura != null)
            {
                float t = reduceMotionEnabled ? 0 : hangarHeroAnimationTime;
                hangarHeroAura.rectTransform.localScale = Vector3.one * (.98f + Mathf.Sin(t * 1.2f) * .025f);
                var colour = hangarHeroAura.color;
                colour.a = .08f + Mathf.Sin(t * .9f) * .015f;
                hangarHeroAura.color = colour;
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

            while (!reduceMotionEnabled && elapsed < HangarSwipeDuration)
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

            if (selected)
            {
                // The original pose remains the layout/registration reference;
                // the visible hero is rendered with physical cradle and rig meshes.
                preview.enabled = false;
                root.Find("Hero scanner").gameObject.SetActive(false);
                root.Find("Hero docking platform").gameObject.SetActive(false);
                root.Find("Hero horizon").gameObject.SetActive(false);
                var studioObject = new GameObject("Hangar three-dimensional studio", typeof(RectTransform), typeof(RawImage));
                studioObject.transform.SetParent(root, false);
                var view = studioObject.GetComponent<RawImage>();
                view.rectTransform.sizeDelta = new Vector2(530, 420);
                hangarPreviewStage = studioObject.AddComponent<SkyPulseProgressionStage>();
                hangarPreviewStage.ReducedMotion = reduceMotionEnabled;
                hangarPreviewStage.Build(view, new[] {
                    ProgressionPortrait(skin, new Vector2(0, 28), new Vector2(440, 265))
                }, false);
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

            hangarInformationFadeTime = reduceMotionEnabled ? -1 : 0;
            hangarPageIndex--;

            if (hangarPageIndex < 0)
                hangarPageIndex = BirdRoster.Length - 1;

            RebuildCustomizeGrid();
        }

        private void ShowNextHangarBird()
        {
            if (BirdRoster == null || BirdRoster.Length == 0) return;

            hangarInformationFadeTime = reduceMotionEnabled ? -1 : 0;
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
