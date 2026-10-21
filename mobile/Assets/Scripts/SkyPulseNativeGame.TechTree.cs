using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        SkyPulseTechTreeView holographicTree;
        // Authored space, not a row/column grid: the three uninterrupted limbs
        // sweep through a shared free-pan field with room for their level fans.
        static readonly Vector2[] TechFamilyPositions = {
            new Vector2(-520,-780), new Vector2(-680,-1630), new Vector2(-470,-2460),
            new Vector2(80,-700), new Vector2(-90,-1570), new Vector2(70,-2480),
            new Vector2(650,-830), new Vector2(490,-1660), new Vector2(680,-2500)
        };

        void ClearHolographicTree()
        {
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
            var core = new Vector2(0, -160f);
            var installed = 0;
            var capacity = 0;
            foreach (var upgrade in Upgrades) { installed += GetUpgradeLevel(upgrade.Id); capacity += upgrade.MaxLevel; }
            roots.Add(new HolographicRoot { At = core, Colour = Hex("#a9f6ff"), Powered = true, Size = 57f });
            TechLabel("SKYPULSE CORE", new Vector2(0, -42), new Vector2(580, 50), 33, Hex("#effcff"));
            TechLabel($"{installed} / {capacity} LEVELS INSTALLED", new Vector2(215, -225), new Vector2(390, 38), 25, Hex("#9cb8ce"));

            for (int family = 0; family < families.Length; family++)
            {
                float x = (family - 1) * 560f;
                var colour = colours[family];
                var fork = new Vector2(x, family == 1 ? -490 : -450);
                branches.Add(new HolographicBranch { From = core, To = fork, Colour = colour, Powered = true, Trunk = true });
                TechLabel(families[family], new Vector2(x + 100, family == 1 ? -520 : -480), new Vector2(280, 40), 27, colour);
                var previous = fork;
                foreach (var upgrade in Upgrades)
                {
                    if (upgrade.Branch != families[family]) continue;
                    var at = TechFamilyPositions[family * 3 + upgrade.Tier - 1];
                    int level = GetUpgradeLevel(upgrade.Id);
                    bool unlocked = IsUpgradePrerequisiteMet(upgrade);
                    branches.Add(new HolographicBranch { From = previous, To = at, Colour = colour, Powered = unlocked, Trunk = true });
                    roots.Add(new HolographicRoot { At = at, Colour = colour, Powered = unlocked, Size = 32f });
                    CreateTechTouch(upgrade.Name + " family root", at + new Vector2(100, 0), new Vector2(420, 180), () => InspectTechNode(upgrade));
                    TechLabel(upgrade.Name.Replace(" ", "\n"), at + new Vector2(180, -12), new Vector2(276, 76), 31, unlocked ? Hex("#effaff") : Hex("#94a8c2"));
                    for (int index = 0; index < upgrade.MaxLevel; index++)
                    {
                        int selected = index;
                        var leaf = at + new Vector2(index == 0 ? -225 : index == 1 ? 205 : -160, -240f - index * 185f);
                        // Every level grows from its family's root. The prerequisite
                        // chain continues along the same uninterrupted main trunk.
                        branches.Add(new HolographicBranch { From = at, To = leaf, Colour = colour, Powered = unlocked && index <= level });
                        roots.Add(new HolographicRoot { At = leaf, Colour = colour, Powered = index < level, Size = index == level && unlocked ? 22f : 14f });
                        CreateTechTouch(upgrade.Id + " level " + (index + 1), leaf, new Vector2(200, 190), () => InspectTechLevel(upgrade, selected));
                        string status = index < level ? "INSTALLED" : index == level && unlocked ? upgrade.PriceAtLevel(index) + " ✦" : "LOCKED";
                        TechLabel("LEVEL " + (index + 1), leaf + new Vector2(0, -42), new Vector2(200, 34), 25, index <= level && unlocked ? colour : Hex("#94a8c2"));
                        TechLabel(status, leaf + new Vector2(0, -76), new Vector2(200, 30), 23, index < level ? colour : Hex("#b2c4d8"));
                    }
                    previous = at;
                }
            }
            customizeContent.anchorMin = customizeContent.anchorMax = new Vector2(.5f, 1f);
            customizeContent.sizeDelta = new Vector2(2300, 3600);
            customizeScroll.horizontal = customizeScroll.vertical = true;
            customizeScroll.movementType = ScrollRect.MovementType.Clamped;
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
            holographicTree.ReducedMotion = reduceMotionEnabled;
            holographicTree.Build(raw, customizeContent, customizeScroll.viewport, branches, roots);
        }

        Text TechLabel(string copy, Vector2 position, Vector2 size, int fontSize, Color colour)
        {
            var label = CreateText(customizeContent, copy, position, size, fontSize, colour, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, 1f);
            label.raycastTarget = false;
            AddOutline(label.gameObject, new Color(.002f, .008f, .018f, .96f), 1.8f);
            return label;
        }

        void CreateTechTouch(string name, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
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
        }

        void InspectTechLevel(Upgrade upgrade, int index)
        {
            int level = GetUpgradeLevel(upgrade.Id);
            if (index == level && IsUpgradePrerequisiteMet(upgrade)) { SelectUpgrade(upgrade); return; }
            // Installed/future levels are informational. Only the next real level
            // is purchasable, through the existing wallet transaction path.
            pendingPurchase = PendingPurchase.None; pendingUpgrade = null; pendingSkin = null;
            OpenPurchaseModal(upgrade.Name, "", 0, upgrade.Accent, GetUpgradeArtwork(upgrade) ?? softCircleSprite);
            purchaseTitleText.text = upgrade.Name + "  ·  L" + (index + 1);
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
