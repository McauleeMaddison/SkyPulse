using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

// Copy into a disposable QA project's Assets/Editor only. Never run on a player save.
public static class SkyPulseReleaseEdgeChecks
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static object Get(object o, string n) => o.GetType().GetField(n, F).GetValue(o);
    static void Set(object o, string n, object v) => o.GetType().GetField(n, F).SetValue(o, v);
    static object Call(object o, string n, params object[] args) => o.GetType().GetMethod(n, F).Invoke(o, args);
    static Array Catalog(string n) => (Array)typeof(SkyPulseNativeGame).GetField(n, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Debug.Log("EDGE_CHECK: " + message); }
    public static void Run(SkyPulseNativeGame game)
    {
        if (Application.companyName != "SkyPulseQA20261006" || Application.productName != "SkyPulseQA20261006")
            throw new Exception("Refusing save tests outside the isolated QA identity");
        game.enabled = false;
        PlayerPrefs.DeleteAll(); PlayerPrefs.Save();
        var levels = (IDictionary)Get(game, "upgradeLevels");
        var ownedTech = (HashSet<string>)Get(game, "ownedUpgradeIds");
        var ownedBirds = (HashSet<string>)Get(game, "ownedSkinIds");
        levels.Clear(); ownedTech.Clear(); ownedBirds.Clear();
        Call(game, "LoadProgress"); Call(game, "ResetToMenu"); Call(game, "OpenUpgrades");
        Set(game, "crystals", 1000000);
        // Catalogue order follows prerequisites within each branch.
        foreach (var upgrade in Catalog("Upgrades"))
        {
            string id = (string)Get(upgrade, "Id");
            for (int level = 0; level < 3; level++)
            {
                int before = (int)Get(game, "crystals");
                int price = (int)Call(upgrade, "PriceAtLevel", level);
                Call(game, "SelectUpgrade", upgrade); Call(game, "ConfirmPurchase"); Call(game, "ConfirmPurchase");
                Check((int)Get(game, "crystals") == before - price && (int)Call(game, "GetUpgradeLevel", id) == level + 1,
                    id + " level " + (level + 1) + " charges once at the displayed tier price");
            }
            int balance = (int)Get(game, "crystals");
            Call(game, "SelectUpgrade", upgrade); Call(game, "ConfirmPurchase");
            Check((int)Get(game, "crystals") == balance, id + " cannot purchase beyond max level");
        }
        Call(game, "OpenHangar");
        foreach (var bird in Catalog("Skins"))
        {
            string id = (string)Get(bird, "Id");
            int price = ownedBirds.Contains(id) ? 0 : (int)Get(bird, "Price");
            int before = (int)Get(game, "crystals");
            Call(game, "SelectSkin", bird); Call(game, "ConfirmPurchase"); Call(game, "ConfirmPurchase");
            Check(ownedBirds.Contains(id) && (int)Get(game, "crystals") == before - price, id + " unlock charges once");
            if (price > 0)
            {
                Call(game, "UpdateUnlockReveal", 1f);
                var proceed = (Button)Get(game, "unlockRevealContinueButton");
                Check(proceed.interactable, id + " unlock Continue becomes available");
                proceed.onClick.Invoke();
                Check(!((GameObject)Get(game, "unlockRevealModal")).activeSelf, id + " Continue dismisses the unlock overlay");
            }
            Call(game, "SelectSkin", bird);
            Check((int)Get(game, "crystals") == before - price, id + " re-equip is free");
        }
        int savedBalance = (int)Get(game, "crystals");
        string savedBird = (string)Get(Get(game, "equippedSkin"), "Id");
        Call(game, "SaveProgress");
        levels.Clear(); ownedTech.Clear(); ownedBirds.Clear(); Set(game, "crystals", 0);
        Call(game, "LoadProgress");
        Check(ownedBirds.Count == 15 && ownedTech.Count == 9 && (int)Get(game, "crystals") == savedBalance,
            "Full collection and balance survive clearing memory and reloading persisted progress");
        foreach (var upgrade in Catalog("Upgrades")) Check((int)Call(game, "GetUpgradeLevel", Get(upgrade, "Id")) == 3, "Max tech level survives reload");
        Check((string)Get(Get(game, "equippedSkin"), "Id") == savedBird, "Equipped bird survives reload");
        Call(game, "StartFlight"); Set(game, "slowFieldTimer", 4f); Set(game, "magnetHaloTimer", 6f);
        Call(game, "OnApplicationFocus", false);
        float y = (float)Get(game, "birdY"); int score = (int)Get(game, "score");
        for (int i = 0; i < 60; i++) Call(game, "Update");
        Check(Get(game, "state").ToString() == "Paused" && (float)Get(game, "birdY") == y && (int)Get(game, "score") == score,
            "Focus loss freezes gameplay position and score");
        Check((float)Get(game, "slowFieldTimer") == 4f && (float)Get(game, "magnetHaloTimer") == 6f, "Pause preserves remaining power-up time");
        Call(game, "OnApplicationFocus", true);
        Check(Get(game, "state").ToString() == "Paused", "Focus restoration requires explicit resume");
        Call(game, "ResumeFlight"); Call(game, "UpdatePowerUpEffects", 10f);
        Check((float)Get(game, "slowFieldTimer") == 0f && (float)Get(game, "magnetHaloTimer") == 0f, "Power-up timers expire without negative values");
        Call(game, "ResetToMenu"); Call(game, "StartFlight");
        Check((int)Get(game, "score") == 0 && (int)Get(game, "runCrystalsCollected") == 0 && !(bool)Get(game, "resultCrystalBonusApplied"),
            "New run resets score and reward bookkeeping");
        Debug.Log("SKYPULSE_RELEASE_EDGE_PASS");
    }
}
