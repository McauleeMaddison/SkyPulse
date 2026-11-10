// Disposable Editor QA. Presentation/navigation only; the owner wallet and saves are never used.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseProgressionClarityChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("PROGRESSION_CLARITY_CHECK: "+message);}
    static bool Has(GameObject root,string value)=>root.GetComponentsInChildren<Text>().Any(t=>t.text==value);
    static void Fits(Text label)=>Check(label.preferredHeight<=label.rectTransform.rect.height+2,"New progression copy fits: "+label.text.Replace("\n"," / "));
    sealed class Store:IMissionStore
    {
        readonly Dictionary<string,string> text=new Dictionary<string,string>();readonly Dictionary<string,int> numbers=new Dictionary<string,int>();
        public string Get(string k,string d)=>text.TryGetValue(k,out var v)?v:d;
        public int GetInt(string k,int d)=>numbers.TryGetValue(k,out int v)?v:d;
        public void Set(string k,string v){text[k]=v;}public void SetInt(string k,int v){numbers[k]=v;}public void Flush(){}
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable progression QA required");
        game.enabled=false;Call(game,"ClosePurchaseModal");Call(game,"ResetToMenu");
        var oldStore=Get(game,"missionStore");var oldDaily=Get(game,"dailyMissions");var oldProgress=Get(game,"birdProgression");var oldSkin=Get(game,"equippedSkin");int oldWallet=(int)Get(game,"crystals");
        var owned=(HashSet<string>)Get(game,"ownedSkinIds");var savedOwned=new HashSet<string>(owned);
        var levels=(Dictionary<string,int>)Get(game,"upgradeLevels");var savedLevels=new Dictionary<string,int>(levels);levels.Clear();
        var installed=(HashSet<string>)Get(game,"ownedUpgradeIds");var savedInstalled=new HashSet<string>(installed);installed.Clear();
        var skins=(Array)typeof(SkyPulseNativeGame).GetField("Skins",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        var upgrades=(Array)typeof(SkyPulseNativeGame).GetField("Upgrades",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        var ids=skins.Cast<object>().Select(b=>(string)Get(b,"Id")).ToArray();var store=new Store();store.SetInt(SkyPulseDailyMissions.WalletKey,0);
        var progression=new SkyPulseBirdProgression(store,ids);
        Set(game,"missionStore",store);Set(game,"dailyMissions",new SkyPulseDailyMissions(store,()=>new DateTime(2026,11,10,0,0,0,DateTimeKind.Utc)));Set(game,"birdProgression",progression);Set(game,"equippedSkin",skins.GetValue(0));Set(game,"crystals",0);
        owned.Clear();owned.Add(ids[0]);owned.Add(ids[1]);Call(game,"RebuildBirdRoster");
        try
        {
            Call(game,"OpenUpgrades");yield return null;Canvas.ForceUpdateCanvases();
            var content=(RectTransform)Get(game,"customizeContent");
            Check((int)Get(game,"techReadyLevelCount")==0,"Empty wallet does not label unaffordable Tech as ready");
            Check(Has(((RectTransform)Get(game,"techNextControl")).gameObject,"NEXT"),"Saving-goal locator keeps its clear NEXT state");
            var needed=content.GetComponentsInChildren<Text>().Where(t=>t.text.StartsWith("NEED ",StringComparison.Ordinal)).ToArray();
            Check(needed.Length==3,"Each initially unlocked saving goal has an exact crystal shortfall");foreach(var text in needed)Fits(text);
            int minimum=int.MaxValue;foreach(var upgrade in upgrades)if((bool)Call(game,"IsUpgradePrerequisiteMet",upgrade))minimum=Math.Min(minimum,(int)Call(game,"UpgradeCrystalPrice",upgrade,0));
            store.SetInt(SkyPulseDailyMissions.WalletKey,minimum);Set(game,"crystals",minimum);Call(game,"OpenUpgrades");yield return null;
            int affordable=0;foreach(var upgrade in upgrades)if((bool)Call(game,"IsUpgradePrerequisiteMet",upgrade)&&(int)Call(game,"UpgradeCrystalPrice",upgrade,0)<=minimum)affordable++;
            Check((int)Get(game,"techReadyLevelCount")==affordable&&affordable>0,"READY count uses real Core-adjusted prices and current balance");
            var next=(RectTransform)Get(game,"techNextControl");Check(Has(next.gameObject,"READY"),"The locator identifies a purchase the player can afford");
            int wallet=(int)Get(game,"crystals");next.GetComponent<Button>().onClick.Invoke();
            Check((int)Get(game,"crystals")==wallet&&!((GameObject)Get(game,"purchaseModal")).activeSelf,"Ready locator only focuses the tree and cannot spend");
            object selected=null;foreach(var upgrade in upgrades)if((bool)Call(game,"IsUpgradePrerequisiteMet",upgrade)&&(int)Call(game,"UpgradeCrystalPrice",upgrade,0)<=minimum){selected=upgrade;break;}
            var tree=(SkyPulseTechTreeView)Get(game,"holographicTree");tree.Tick(1f);
            var scroll=(ScrollRect)Get(game,"customizeScroll");content=(RectTransform)Get(game,"customizeContent");
            var focused=content.Find((string)Get(selected,"Id")+" level 1");
            Check(focused!=null&&scroll.viewport.rect.Contains((Vector2)scroll.viewport.InverseTransformPoint(focused.position)),"READY locator brings the actual affordable leaf into the viewport");
            Call(game,"InspectTechLevel",selected,0);
            Check(((Text)Get(game,"purchaseDetailText")).text.StartsWith("ACTIVE · Not installed\nNEXT · ",StringComparison.Ordinal),"Direct leaf inspection presents the current and next effect together");
            Check(((Button)Get(game,"purchaseConfirmButton")).interactable&&(int)Get(game,"quotedUpgradePrice")==minimum,"Presentation preserves the exact affordable quote and confirm guard");Fits((Text)Get(game,"purchaseDetailText"));Call(game,"ClosePurchaseModal");
            for(int core=1;core<=4;core++)
            {
                progression.Data.core=core;Call(game,"OpenCoreProgression");yield return null;
                var screen=(GameObject)Get(game,"birdProgressionScreen");var example=screen.GetComponentsInChildren<Text>().Single(t=>t.text.Contains("TRAINING LEVEL 2 ·"));
                string expected="TRAINING LEVEL 2 · "+SkyPulseBirdProgression.PriceAfterCoreDiscount(120,core)+" → "+SkyPulseBirdProgression.PriceAfterCoreDiscount(120,core+1)+" ✦";
                Check(example.text.EndsWith(expected,StringComparison.Ordinal),"Core example shows an eligible owned bird's actual training costs at rank "+core);Fits(example);Call(game,"CloseBirdProgression");
            }
            progression.Data.core=1;Set(game,"hangarPageIndex",0);Call(game,"OpenHangar");yield return null;
            var hangar=(GameObject)Get(game,"customizeScreen");Check(Has(hangar,"NEXT TRAINING · LEVEL 2 · 120 ✦"),"Owned starter exposes the exact next training action and price");
            foreach(var text in hangar.GetComponentsInChildren<Text>().Where(t=>t.text.StartsWith("NEXT TRAINING",StringComparison.Ordinal)||t.text.StartsWith("NEW BIRD SAVINGS",StringComparison.Ordinal)))Fits(text);
            progression.Data.birds.Add(new BirdGenome{id=ids[0],appearance=ids[0],primary=0,secondary=1,level=2});Call(game,"OpenHangar");yield return null;
            Check(Has(hangar,"CORE 2 OPENS LEVEL 3 TRAINING"),"Core-limited bird states the exact next Core prerequisite");
            progression.Data.core=5;progression.Data.birds[0].level=5;Call(game,"OpenHangar");yield return null;
            Check(Has(hangar,"FULLY TRAINED · BOTH REWARD TRAITS ACTIVE")&&Has(hangar,"VIEW TRAITS"),"Maximum training becomes an honest trait-inspection action");
            progression.Data.core=2;Call(game,"OpenBreeding",skins.GetValue(0));yield return null;
            var body=(RectTransform)Get(game,"birdProgressionBody");var inheritance=body.GetComponentsInChildren<Text>().Single(t=>t.text.StartsWith("PARENT A APPEARANCE",StringComparison.Ordinal));Fits(inheritance);
            Check(inheritance.text.Contains("A + B REWARD TRAITS"),"Breeding explains appearance inheritance separately from its useful reward traits");
            Check((int)Get(game,"crystals")==wallet&&progression.Data.revision==0,"Browsing all progression explanations never debits or commits a purchase");
        }
        finally
        {
            Call(game,"CloseBirdProgression");Call(game,"ClosePurchaseModal");Set(game,"missionStore",oldStore);Set(game,"dailyMissions",oldDaily);Set(game,"birdProgression",oldProgress);Set(game,"equippedSkin",oldSkin);Set(game,"crystals",oldWallet);
            owned.Clear();foreach(var id in savedOwned)owned.Add(id);levels.Clear();foreach(var pair in savedLevels)levels[pair.Key]=pair.Value;installed.Clear();foreach(var id in savedInstalled)installed.Add(id);Call(game,"RebuildBirdRoster");Call(game,"ResetToMenu");
        }
        Debug.Log("SKYPULSE_PROGRESSION_CLARITY_PASS");
    }
}
