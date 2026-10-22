using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        SkyPulseTechTreeView holographicTree;
        RectTransform techCoreControl;
        Material techBackdropMaterial;
        Material techTypographyMaterial;
        // Authored space, not a row/column grid: the three uninterrupted limbs
        // sweep through a shared free-pan field with room for their level fans.
        static readonly Vector2[] TechFamilyPositions = {
            new Vector2(-520,-780), new Vector2(-680,-1630), new Vector2(-470,-2460),
            new Vector2(80,-700), new Vector2(-90,-1570), new Vector2(70,-2480),
            new Vector2(650,-830), new Vector2(490,-1660), new Vector2(680,-2500)
        };

        void ClearHolographicTree()
        {
            var insignia = customizeScreen != null ? customizeScreen.transform.Find("Hangar flight insignia") : null;
            if (insignia != null) insignia.gameObject.SetActive(true);
            if (techCoreControl != null) { techCoreControl.gameObject.SetActive(false); Destroy(techCoreControl.gameObject); techCoreControl = null; }
            if (holographicTree == null) return;
            // Disable the isolated scene now; Destroy completes at frame end.
            holographicTree.gameObject.SetActive(false);
            Destroy(holographicTree.gameObject);
            holographicTree = null;
        }

        private void BuildTechTree()
        {
            var branches = new List<HolographicBranch>(39);
            var roots = new List<HolographicRoot>(40);
            var families = new[] { "COLLECTION", "RECOVERY", "MASTERY" };
            var colours = new[] { Hex("#45eaff"), Hex("#7ee4dc"), Hex("#bbabff") };
            var core = new Vector2(0, -250f);
            var installed = 0;
            var capacity = 0;
            foreach (var upgrade in Upgrades) { installed += GetUpgradeLevel(upgrade.Id); capacity += upgrade.MaxLevel; }
            roots.Add(new HolographicRoot { At = core, Colour = SkyPulseBirdProgression.CoreColours[birdProgression.Data.core-1], Powered = true, Size = 57f });
            CreateTechTouch("SkyPulse Core upgrade",core,new Vector2(260,260),OpenCoreProgression);
            TechLabel("SKYPULSE CORE", new Vector2(0, -48), new Vector2(580, 54), 34, Hex("#effcff"));
            TechLabel($"{installed} / {capacity} INSTALLED", new Vector2(0, -94), new Vector2(580, 36), 25, Hex("#91a9be"));
            TechLabel(SkyPulseBirdProgression.CoreNames[birdProgression.Data.core-1]+" · CORE "+birdProgression.Data.core+" / 5",new Vector2(0,-141),new Vector2(580,34),25,SkyPulseBirdProgression.CoreColours[birdProgression.Data.core-1]);
            TechLabel("CORE SAVINGS · "+birdProgression.UpgradeDiscountPercent+"% · "+(birdProgression.Data.core<5?"TAP TO EVOLVE":"DETAILS"),new Vector2(0,-370),new Vector2(580,32),23,Hex("#a9c8d8"));

            for (int family = 0; family < families.Length; family++)
            {
                float x = (family - 1) * 560f;
                var colour = colours[family];
                var fork = new Vector2(x, family == 1 ? -490 : -450);
                branches.Add(new HolographicBranch { From = core, To = fork, Colour = colour, Powered = true, Trunk = true });
                TechLabel(families[family], new Vector2(x + (family==0?-38:family==2?38:0), family == 1 ? -560 : -520), new Vector2(280, 40), 27, colour);
                var previous = fork;
                foreach (var upgrade in Upgrades)
                {
                    if (upgrade.Branch != families[family]) continue;
                    var at = TechFamilyPositions[family * 3 + upgrade.Tier - 1];
                    int level = GetUpgradeLevel(upgrade.Id);
                    bool unlocked = IsUpgradePrerequisiteMet(upgrade);
                    branches.Add(new HolographicBranch { From = previous, To = at, Colour = colour, Powered = unlocked, Trunk = true });
                    roots.Add(new HolographicRoot { At = at, Colour = colour, Powered = unlocked, Size = 32f });
                    CreateTechTouch(upgrade.Name + " family root", at + new Vector2(100, 0), new Vector2(420, 180), () => InspectTechNode(upgrade), at);
                    TechLabel(System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(upgrade.Name.ToLowerInvariant()).Replace(" ", "\n"), at + new Vector2(180, -8), new Vector2(250, 86), 33, unlocked ? Hex("#effaff") : Hex("#8193a8"));
                    TechLabel($"{level} / {upgrade.MaxLevel}", at + new Vector2(180, -65), new Vector2(250, 30), 23, unlocked ? colour : Hex("#63748c"));
                    for (int index = 0; index < upgrade.MaxLevel; index++)
                    {
                        int selected = index;
                        // Stagger the fans without overlapping another family's hit
                        // area. The whole asymmetric, connected map stays free-pan.
                        var leaf = at + new Vector2(index == 0 ? -130 : index == 1 ? 120 : -160, -290f - index * 165f);
                        // Every level grows from its family's root. The prerequisite
                        // chain continues along the same uninterrupted main trunk.
                        branches.Add(new HolographicBranch { From = at, To = leaf, Colour = colour, Powered = unlocked && index <= level });
                        roots.Add(new HolographicRoot { At = leaf, Colour = colour, Powered = index <= level && unlocked, Size = index == level && unlocked ? 26f : 18f });
                        float side = index == 1 ? 1 : -1;
                        CreateTechTouch(upgrade.Id + " level " + (index + 1), leaf + new Vector2(side * 40, 0), new Vector2(200, 180), () => InspectTechLevel(upgrade, selected), leaf);
                        bool next=index==level&&unlocked,done=index<level;
                        var labelAt=leaf+new Vector2(side*40,-3);
                        if(next)
                        {
                            var plaque=ProgressionSurface(customizeContent,"Next-level glass · "+upgrade.Id,labelAt+Vector2.down*24,new Vector2(198,130),SkyPulseProgressionSurface.Finish.Quiet);
                            plaque.Capsule=true;plaque.Accent=colour;plaque.rectTransform.anchorMin=plaque.rectTransform.anchorMax=new Vector2(.5f,1);
                        }
                        var rank=TechLabel("LEVEL "+(index+1),labelAt,new Vector2(188,42),31,next?Hex("#f4fcff"):done?colour:Hex("#94a7bc"));
                        rank.GetComponent<SkyPulseHolographicText>().Emphasis=next?1:done?.5f:0;
                        rank.gameObject.name=upgrade.Name+" · level "+(index+1)+" label";
                        TechLabel(done?"INSTALLED":next?"NEXT · "+UpgradeCrystalPrice(upgrade,index)+" ✦":"LOCKED",labelAt+new Vector2(0,-38),new Vector2(194,32),22,next?colour:done?Hex("#aac6d3"):Hex("#8295aa"));
                        if(next)
                        {
                            TechLabel("VIEW UPGRADE ›",labelAt+new Vector2(0,-67),new Vector2(194,26),19,Hex("#c8e5ef"));
                            var marker=CreateUiGlyph(customizeContent,"Next available · "+upgrade.Id,leaf+new Vector2(side*40,61),new Vector2(36,36),colour,SkyPulseUiGlyph.Kind.UpgradePoint);
                            marker.rectTransform.anchorMin=marker.rectTransform.anchorMax=new Vector2(.5f,1);marker.raycastTarget=false;
                        }
                    }
                    previous = at;
                }
            }
            customizeContent.anchorMin = customizeContent.anchorMax = new Vector2(.5f, 1f);
            customizeContent.sizeDelta = new Vector2(2300, 3600);
            customizeScroll.horizontal = customizeScroll.vertical = true;
            customizeScroll.movementType = ScrollRect.MovementType.Clamped;
            customizeScroll.inertia = true;
            customizeScroll.decelerationRate = .055f;
            customizeScroll.viewport.sizeDelta = new Vector2(1000, 1450);
            customizeScroll.viewport.anchoredPosition = new Vector2(0, -235);
            customizeContent.anchoredPosition = Vector2.zero;
            var scene = new GameObject("Holographic upgrade tree", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(SkyPulseTechTreeView));
            scene.transform.SetParent(customizeScroll.viewport, false);
            // The mesh follows scrolling behind the content's stationary hit areas.
            scene.transform.SetSiblingIndex(customizeContent.GetSiblingIndex());
            var raw = scene.GetComponent<RawImage>();
            raw.rectTransform.anchorMin = Vector2.zero; raw.rectTransform.anchorMax = Vector2.one;
            raw.rectTransform.offsetMin = raw.rectTransform.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            holographicTree = scene.GetComponent<SkyPulseTechTreeView>();
            holographicTree.TypographyMaterial=techTypographyMaterial;
            holographicTree.CoreRank = birdProgression.Data.core;
            holographicTree.ReducedMotion = reduceMotionEnabled;
            holographicTree.Build(raw, customizeContent, customizeScroll.viewport, branches, roots);
            var pan = customizeScroll.viewport.GetComponent<SkyPulseTreePanInput>();
            if (pan == null) pan = customizeScroll.viewport.gameObject.AddComponent<SkyPulseTreePanInput>();
            pan.View = holographicTree;
            foreach (var touch in customizeContent.GetComponentsInChildren<SkyPulseTreeTouchFeedback>()) touch.View = holographicTree;
            CreateTechCoreControl();
        }

        Text TechLabel(string copy, Vector2 position, Vector2 size, int fontSize, Color colour)
        {
            var label = CreateText(customizeContent, copy, position, size, fontSize, colour, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, 1f);
            label.raycastTarget = false;
            var shadow = label.GetComponent<Shadow>();
            if (shadow != null) shadow.enabled = false;
            if(techTypographyMaterial==null)techTypographyMaterial=new Material(Resources.Load<Shader>("SkyPulse/Shaders/HolographicText")){name="SkyPulse spectral lettering"};
            var canvas=label.canvas;if(canvas!=null)canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1;
            label.material=techTypographyMaterial;
            label.gameObject.AddComponent<SkyPulseHolographicText>();
            return label;
        }

        void RefreshTechTreeChrome(bool tech)
        {
            var veil = customizeScreen.transform.Find("Customize veil").GetComponent<Image>();
            if (tech && techBackdropMaterial == null)
            {
                techBackdropMaterial = new Material(Resources.Load<Shader>("SkyPulse/Shaders/TechTreeField")) { name = "SkyPulse tree atmosphere" };
                techBackdropMaterial.SetFloat("_Backdrop", 1);
            }
            veil.material = tech ? techBackdropMaterial : null;
            veil.color = tech ? Color.white : new Color(.006f, .012f, .036f, .74f);
            customizeScroll.viewport.GetComponent<RectMask2D>().softness = tech ? new Vector2Int(24,24) : Vector2Int.zero;
            customizeTitle.fontSize = tech ? 46 : 52;
            customizeTitle.fontStyle = tech ? FontStyle.Normal : FontStyle.Bold;
            customizeTitle.GetComponent<Shadow>().enabled = !tech;
            customizeSubtitle.fontSize = tech ? 24 : 27;
            customizeSubtitle.GetComponent<Shadow>().enabled = !tech;
            if (!tech)
            {
                customizeScroll.viewport.sizeDelta = new Vector2(950, 1280);
                customizeScroll.viewport.anchoredPosition = new Vector2(0, -152);
            }
            foreach (var tab in collectionTabs)
            {
                // Keep the established controls and targets; replace their
                // chamfered neon trim only while viewing the tree.
                var shell = tab.GetComponent<Image>(); shell.color = tech ? Color.clear : Hex("#090e1e");
                var outline = tab.GetComponent<Outline>(); if (outline != null) outline.enabled = !tech;
                var face = (Image)tab.targetGraphic;
                face.sprite = tech ? roundedPanelSprite : interfacePanelSprite;
                tab.GetComponent<SkyPulseButtonFeedback>().ReducedMotion = reduceMotionEnabled;
                var keyline = tab.transform.Find("Flight control keyline");
                if (keyline != null) keyline.gameObject.SetActive(!tech);
                var text = tab.GetComponentInChildren<Text>();
                text.fontSize = tech ? 27 : 22;
                var shadow = text.GetComponent<Shadow>(); if (shadow != null) shadow.enabled = !tech;
            }
            var menu = customizeScreen.transform.Find("Button · ‹  MENU");
            var menuShell = menu.GetComponent<Image>(); menuShell.color = tech ? Color.clear : Hex("#090e1e");
            menu.GetComponent<Outline>().enabled = !tech;
            ((Image)menu.GetComponent<Button>().targetGraphic).sprite = tech ? roundedPanelSprite : interfacePanelSprite;
            menu.Find("Flight control keyline").gameObject.SetActive(!tech);
            menu.GetComponent<SkyPulseButtonFeedback>().ReducedMotion = reduceMotionEnabled;
            var menuLabel = menu.GetComponentInChildren<Text>(); menuLabel.fontSize = tech ? 27 : 22; menuLabel.GetComponent<Shadow>().enabled = !tech;
            var chip = customizeCrystalText.transform.parent;
            chip.GetComponent<Image>().sprite = tech ? roundedPanelSprite : interfacePanelSprite;
            chip.GetComponent<Outline>().enabled = !tech;
            chip.Find("Flight telemetry keyline").gameObject.SetActive(!tech);
        }

        void StyleTechInspector(bool tech)
        {
            var card = purchaseModal.transform.Find("Purchase card");
            purchaseModal.transform.Find("Purchase dim").GetComponent<Image>().color = tech ? new Color(.008f,.018f,.034f,.84f) : new Color(.008f,.004f,.04f,.86f);
            var image = card.GetComponent<Image>(); image.sprite = tech ? roundedPanelSprite : interfacePanelSprite;
            image.color = tech ? Hex("#0b1c2b") : Hex("#08132a");
            var outline = card.GetComponent<Outline>(); if (outline != null) outline.enabled = !tech;
            card.Find("Acquisition wing").gameObject.SetActive(!tech);
            card.Find("Acquisition dock").gameObject.SetActive(!tech);
            card.Find("Acquisition runway").gameObject.SetActive(!tech);
            purchaseHalo.gameObject.SetActive(!tech);
            // A quiet, clearly ordered inspector, still using the original
            // guarded purchase button and wallet transaction.
            purchaseTitleText.rectTransform.anchoredPosition = new Vector2(0, tech ? 294 : 278);
            purchaseTitleText.resizeTextMaxSize = tech ? 38 : 35;
            purchaseDetailText.rectTransform.anchoredPosition = new Vector2(0, tech ? -30 : -38);
            purchaseDetailText.rectTransform.sizeDelta = new Vector2(740, tech ? 144 : 100);
            purchaseDetailText.resizeTextMaxSize = tech ? 29 : 24;
            purchaseDetailText.resizeTextMinSize = tech ? 25 : 21;
            purchaseBalanceText.fontSize = tech ? 25 : 19;
            purchaseBalanceText.fontStyle = tech ? FontStyle.Normal : FontStyle.Bold;
            purchaseBalanceText.rectTransform.anchoredPosition = new Vector2(0, tech ? -156 : -120);
            purchaseBalanceText.rectTransform.sizeDelta = new Vector2(760, tech ? 88 : 52);
            foreach (var button in card.GetComponentsInChildren<Button>())
            {
                button.GetComponent<Image>().sprite = tech ? roundedPanelSprite : interfacePanelSprite;
                var edge = button.GetComponent<Outline>(); if (edge != null) edge.enabled = !tech;
                ((Image)button.targetGraphic).sprite = tech ? roundedPanelSprite : interfacePanelSprite;
                button.transform.Find("Flight control keyline").gameObject.SetActive(!tech);
                button.GetComponent<SkyPulseButtonFeedback>().ReducedMotion = reduceMotionEnabled;
                var label = button.GetComponentInChildren<Text>(); label.fontSize = tech ? 27 : 22;
                var shadow = label.GetComponent<Shadow>(); if (shadow != null) shadow.enabled = !tech;
            }
            foreach (var label in new[] { purchaseTitleText, purchaseDetailText, purchaseBalanceText })
            { var shadow = label.GetComponent<Shadow>(); if (shadow != null) shadow.enabled = !tech; }
            purchaseConfirmText.resizeTextMaxSize = tech ? 27 : 22;
            card.Find("Button · CANCEL").GetComponentInChildren<Text>().text = tech ? "BACK" : "CANCEL";
        }

        void CreateTechTouch(string name, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, Vector2? highlight = null)
        {
            var area = CreateImage(customizeContent, name, position, size, Color.clear);
            area.rectTransform.anchorMin = area.rectTransform.anchorMax = new Vector2(.5f, 1f);
            area.raycastTarget = true;
            var button = area.gameObject.AddComponent<Button>();
            button.targetGraphic = area;
            // Fixed transparent hit areas allow normal ScrollRect drag gestures;
            // the luminous mesh supplies depth without separate card backplates.
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(action);
            var feedback = area.gameObject.AddComponent<SkyPulseTreeTouchFeedback>();
            feedback.TreePoint = highlight ?? position;
        }

        void CreateTechCoreControl()
        {
            var insignia = customizeScreen.transform.Find("Hangar flight insignia");
            if (insignia != null) insignia.gameObject.SetActive(false);
            var control = CreateImage(customizeScreen.transform, "Return to tree core", Vector2.zero, new Vector2(190, 168), Color.clear);
            techCoreControl = control.rectTransform;
            techCoreControl.anchorMin = techCoreControl.anchorMax = new Vector2(.5f, .5f);
            techCoreControl.anchoredPosition = new Vector2(0, 756);
            var button = control.gameObject.AddComponent<Button>(); button.targetGraphic = control;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => { if (holographicTree != null) holographicTree.FocusCore(); });
            CreateUiGlyph(techCoreControl, "Core locator", new Vector2(0, 46), new Vector2(52, 52), Hex("#b3f8ff"), SkyPulseUiGlyph.Kind.CircuitHex);
            CreateText(techCoreControl, "CORE", new Vector2(0,10), new Vector2(190, 28), 24, Hex("#d9faff"), TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        void InspectTechLevel(Upgrade upgrade, int index)
        {
            int level = GetUpgradeLevel(upgrade.Id);
            if (index == level && IsUpgradePrerequisiteMet(upgrade)) { SelectUpgrade(upgrade); return; }
            // Installed/future levels are informational. Only the next real level
            // is purchasable, through the existing wallet transaction path.
            pendingPurchase = PendingPurchase.None; pendingUpgrade = null; pendingSkin = null;
            OpenPurchaseModal(upgrade.Name, "", 0, upgrade.Accent, GetUpgradeArtwork(upgrade) ?? softCircleSprite);
            purchaseTitleText.text = upgrade.Name + "  ·  LEVEL " + (index + 1);
            purchaseDetailText.text = upgrade.EffectAtLevel(index);
            purchaseConfirmButton.interactable = false;
            purchaseConfirmText.text = index < level ? "INSTALLED" : "LOCKED";
            var prerequisite = FindById(Upgrades, upgrade.PrerequisiteId);
            purchaseBalanceText.text = index < level ? "THIS LEVEL IS ACTIVE"
                : !IsUpgradePrerequisiteMet(upgrade) ? $"Requires {prerequisite?.Name} · level {upgrade.PrerequisiteLevel}"
                : $"Install level {index} first";
            SetPurchaseUpgradeArtwork(upgrade);
        }
    }
}
