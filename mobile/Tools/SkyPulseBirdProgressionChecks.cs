using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;
public static class SkyPulseBirdProgressionChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("BIRD_CHECK: "+message);}
    sealed class Store:IMissionStore
    {
        public Dictionary<string,string> text=new Dictionary<string,string>(),durableText=new Dictionary<string,string>();
        public Dictionary<string,int> ints=new Dictionary<string,int>(),durableInts=new Dictionary<string,int>();
        public int calls,failAt=-1;
        public string Get(string k,string d)=>text.TryGetValue(k,out var v)?v:d;
        public int GetInt(string k,int d)=>ints.TryGetValue(k,out var v)?v:d;
        public void Set(string k,string v)=>text[k]=v;
        public void SetInt(string k,int v)=>ints[k]=v;
        public void Flush(){if(++calls==failAt)throw new IOException("Injected bird save failure");durableText=new Dictionary<string,string>(text);durableInts=new Dictionary<string,int>(ints);}
        public void Crash(){text=new Dictionary<string,string>(durableText);ints=new Dictionary<string,int>(durableInts);failAt=-1;}
    }
    static string[] Founders()=>Enumerable.Range(0,15).Select(i=>"bird"+i).ToArray();
    static bool Owned(string id)=>true;
    static void PanelChecks(GameObject screen)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var label in screen.GetComponentsInChildren<Text>())
            Check(label.resizeTextForBestFit||label.preferredHeight<=label.rectTransform.rect.height+2,"Progression text fits: "+label.text.Replace('\n',' '));
        var buttons=screen.GetComponentsInChildren<Button>();
        Check(buttons.All(b=>((RectTransform)b.transform).rect.height>=152),"Progression controls retain at least 152 authored UI units; physical comfort remains a device check");
        for(int i=0;i<buttons.Length;i++)for(int j=i+1;j<buttons.Length;j++)
        {
            var a=(RectTransform)buttons[i].transform;var b=(RectTransform)buttons[j].transform;
            if(a.parent!=b.parent)continue;
            Check(!new Rect(a.anchoredPosition-a.sizeDelta*.5f,a.sizeDelta).Overlaps(new Rect(b.anchoredPosition-b.sizeDelta*.5f,b.sizeDelta)),"Progression buttons do not overlap");
        }
    }
    static void StudioChecks(SkyPulseNativeGame game,int pilots)
    {
        var studio=(SkyPulseProgressionStage)Get(game,"birdProgressionStage");
        Check(studio!=null&&studio.PilotCount==pilots,"One studio renders the expected animated bird portraits");
        var bird=studio.StageCamera.transform.parent.GetComponentsInChildren<SpriteRenderer>()[0];
        // Registered sprite canvases can contain transparent padding outside the
        // visible bird. Mesh bounds test the opaque pedestals/light guides;
        // authored wing registration has its own 90-frame preservation check.
        foreach(var renderer in studio.StageCamera.transform.parent.GetComponentsInChildren<MeshRenderer>())
        {
            var bounds=renderer.bounds;
            for(int corner=0;corner<8;corner++)
            {
                var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                var point=studio.StageCamera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,sign));
                Check(point.x>=0&&point.x<=1&&point.y>=0&&point.y<=1,"Studio mesh stays within its portrait render viewport: "+renderer.name);
            }
        }
        Vector3 position=Vector3.zero;Quaternion rotation=Quaternion.identity;Sprite pose=null;
        foreach(int fps in new[]{30,60,120})
        {
            Set(studio,"clock",0f);studio.ReducedMotion=false;
            for(int i=0;i<fps;i++)studio.Tick(1f/fps);
            if(fps==30){position=bird.transform.localPosition;rotation=bird.transform.localRotation;pose=bird.sprite;}
            Check(Vector3.Distance(position,bird.transform.localPosition)<.0001f&&Quaternion.Angle(rotation,bird.transform.localRotation)<.01f&&pose==bird.sprite,"Registered portrait motion matches at "+fps+" FPS");
        }
        studio.ReducedMotion=true;studio.Tick(0);position=bird.transform.localPosition;rotation=bird.transform.localRotation;pose=bird.sprite;
        studio.Tick(1);Check(position==bird.transform.localPosition&&rotation==bird.transform.localRotation&&pose==bird.sprite,"Reduced Motion freezes the bird pose, dock and bob without blocking input");
        studio.ReducedMotion=false;studio.MotionPaused=true;float clock=studio.AnimationClock;studio.Tick(2);
        Check(clock==studio.AnimationClock,"Paused progression studio does not advance");studio.MotionPaused=false;
        for(int i=0;i<30;i++)studio.Tick(1f/60);
        long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<240;i++)studio.Tick(1f/60);
        Check(GC.GetAllocatedBytesForCurrentThread()-before==0,"Warmed progression studio animation allocates no managed memory");
        var screen=(GameObject)Get(game,"birdProgressionScreen");
        var primary=screen.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text.StartsWith(pilots==1?"TRAIN ·":"BREED ·"));
        var hit=(RectTransform)primary.transform;var at=hit.anchoredPosition;studio.Tick(.5f);
        Check(hit.anchoredPosition==at,"Decorative animation never moves the purchase hit rectangle");
    }
    public static void Pure()
    {
        var ids=Founders();var store=new Store();store.SetInt(SkyPulseDailyMissions.WalletKey,20000);store.Flush();var p=new SkyPulseBirdProgression(store,ids);
        Check(p.Data.core==1&&p.TrainingCap==2&&p.OffspringCount==0,"Legacy saves start with a free level-one Core and retain their wallet");
        Check(ids.Select(id=>p.Genome(id).primary+":"+p.Genome(id).secondary).Distinct().Count()==15,"All 15 founder birds have distinct trait combinations");
        Check(p.Train(ids[0],0,Owned)&&p.Genome(ids[0]).level==2&&store.GetInt(SkyPulseDailyMissions.WalletKey,0)==19880,"Training debits the exact displayed price and raises both traits");
        Check(!p.Train(ids[0],0,Owned)&&!p.Train(ids[0],p.Data.revision,Owned),"Stale training clicks and the Core cap cannot charge again");
        Check(!p.Train(ids[1],p.Data.revision,id=>false),"Unowned birds cannot be trained");
        int before=store.GetInt(SkyPulseDailyMissions.WalletKey,0);int rev=p.Data.revision;var preview=p.Preview(ids[0],ids[1]);
        Check(preview!=null&&store.GetInt(SkyPulseDailyMissions.WalletKey,0)==before&&rev==p.Data.revision,"Breeding previews never debit, reroll or save");
        Check(!p.Breed(ids[0],ids[1],rev,Owned),"Level-one Core cannot bypass breeding unlock");
        Check(p.UpgradeCore(rev)&&p.Data.core==2&&!p.UpgradeCore(rev),"Core upgrade applies once and raises capacity/training cap");
        Check(p.Train(ids[0],p.Data.revision,Owned)&&p.Genome(ids[0]).level==3,"Core level two enables training level three");
        preview=p.Preview(ids[0],ids[1]);rev=p.Data.revision;before=store.GetInt(SkyPulseDailyMissions.WalletKey,0);
        Check(!p.Breed(ids[0],ids[0],rev,Owned)&&!p.Breed(ids[0],ids[1],rev,id=>id==ids[0]),"Breeding requires two different owned parents");
        Check(p.Breed(ids[0],ids[1],rev,Owned),"A valid fixed-price preview can be bred");var child=p.Genome(preview.id);
        Check(child.primary==preview.primary&&child.secondary==preview.secondary&&child.primaryGrade==preview.primaryGrade&&child.appearance==preview.appearance&&child.generation==1&&child.level==1,"Offspring exactly matches its preview, keeps parent identity and starts at training one");
        Check(p.Genome(ids[0]).level==3&&p.Genome(ids[1])!=null&&store.GetInt(SkyPulseDailyMissions.WalletKey,0)==before-p.BreedingCost(preview),"Both parents remain; discounted breeding debits once");
        Check(!p.Breed(ids[0],ids[1],rev,Owned)&&!p.Breed(ids[0],ids[1],p.Data.revision,Owned),"Queued confirmation and an identical owned genome cannot charge twice");
        var loaded=new SkyPulseBirdProgression(store,ids);Check(loaded.Data.core==2&&loaded.Genome(child.id).parentA==ids[0]&&loaded.Genome(ids[0]).level==3,"Core, founder training and offspring survive restart");
        while(p.Data.core<5)Check(p.UpgradeCore(p.Data.revision),"Core rank progression");
        Check(!p.UpgradeCore(p.Data.revision)&&p.TrainingCap==5&&p.BreedingCapacity==20,"Maximum Core does not overspend and bounds collection size");
        var dna=new BirdGenome{primary=(int)BirdTrait.Voyager,secondary=(int)BirdTrait.Recovery,level=3};
        Check(SkyPulseBirdProgression.ResultBonus(dna,0,4,0)==0&&SkyPulseBirdProgression.ResultBonus(dna,0,10,0)==11,"Bird rewards use actual Endless milestones and cannot farm immediate retries");
        foreach(int failure in new[]{1,2,3,4})
        {
            var disk=new Store();disk.SetInt(SkyPulseDailyMissions.WalletKey,1000);disk.Flush();disk.calls=0;disk.failAt=failure;
            var model=new SkyPulseBirdProgression(disk,ids);try{model.UpgradeCore(0);}catch(IOException){}
            disk.failAt=-1;model.RecoverPending();int wallet=disk.GetInt(SkyPulseDailyMissions.WalletKey,0);
            Check(wallet==600&&model.Data.core==2&&!model.UpgradeCore(0),"Interrupted debit recovers exactly once at write "+failure);
            disk.Crash();model=new SkyPulseBirdProgression(disk,ids);model.RecoverPending();Check(disk.GetInt(SkyPulseDailyMissions.WalletKey,0)==600&&model.Data.core==2,"Restart preserves completed debit at write "+failure);
        }
        var poor=new Store();poor.SetInt(SkyPulseDailyMissions.WalletKey,1);var locked=new SkyPulseBirdProgression(poor,ids);
        Check(!locked.UpgradeCore(0)&&!locked.Train(ids[0],0,Owned)&&locked.Data.revision==0,"Insufficient currency leaves progression untouched");
        store.Set(SkyPulseBirdProgression.SaveKey,"broken JSON");loaded=new SkyPulseBirdProgression(store,ids);Check(loaded.Data.core==5,"Corrupt primary save recovers a completed backup");
        int[] techCosts={150,143,135,128,120},trainCosts={120,114,108,102,96},breedCosts={300,285,270,255,240};
        for(int rank=1;rank<=5;rank++)
        {
            var prices=new SkyPulseBirdProgression(new Store(),ids);prices.Data.core=rank;
            Check(prices.UpgradeDiscountPercent==(rank-1)*5&&prices.DiscountedPrice(150)==techCosts[rank-1]&&prices.TrainingCost(prices.Genome(ids[0]))==trainCosts[rank-1]&&prices.BreedingCost(prices.Preview(ids[0],ids[1]))==breedCosts[rank-1],"Core "+rank+" reduces actual Tech Tree, training and breeding prices from their base costs");
            Check(prices.DiscountedPrice(1)==1&&prices.DiscountedPrice(0)==0&&prices.DiscountedPrice(int.MaxValue)>0,"Whole-crystal rounding, zero and integer-overflow boundaries are safe at Core "+rank);
        }
        Check(SkyPulseBirdProgression.PriceAfterCoreDiscount(101,2)==96&&SkyPulseBirdProgression.PriceAfterCoreDiscount(399,5)==320,"Non-divisible prices round the payable cost up once");
        var exact=new Store();exact.SetInt(SkyPulseDailyMissions.WalletKey,113);var efficient=new SkyPulseBirdProgression(exact,ids);efficient.Data.core=2;
        Check(!efficient.Train(ids[0],0,Owned),"One crystal below the discounted training quote cannot purchase");exact.SetInt(SkyPulseDailyMissions.WalletKey,114);
        Check(efficient.Train(ids[0],0,Owned)&&exact.GetInt(SkyPulseDailyMissions.WalletKey,-1)==0&&!efficient.Train(ids[0],0,Owned),"Exact discounted training balance succeeds once");
        efficient=new SkyPulseBirdProgression(exact,ids);Check(efficient.Data.core==2&&efficient.UpgradeDiscountPercent==5&&efficient.CorePrice==900,"Core savings survive restart; Core evolution itself keeps its stated investment price");
        var breedingWallet=new Store();breedingWallet.SetInt(SkyPulseDailyMissions.WalletKey,239);var breeding=new SkyPulseBirdProgression(breedingWallet,ids);breeding.Data.core=5;
        Check(!breeding.Breed(ids[0],ids[1],0,Owned),"One crystal below the discounted breeding quote cannot purchase");breedingWallet.SetInt(SkyPulseDailyMissions.WalletKey,240);
        Check(breeding.Breed(ids[0],ids[1],0,Owned)&&breedingWallet.GetInt(SkyPulseDailyMissions.WalletKey,-1)==0&&!breeding.Breed(ids[0],ids[1],0,Owned),"Exact discounted breeding balance succeeds once");
        foreach(int failure in new[]{1,2,3,4})
        {
            var disk=new Store();disk.SetInt(SkyPulseDailyMissions.WalletKey,1000);disk.Flush();disk.calls=0;disk.failAt=failure;
            var model=new SkyPulseBirdProgression(disk,ids);model.Data.core=2;try{model.Train(ids[0],0,Owned);}catch(IOException){}
            disk.failAt=-1;model.RecoverPending();disk.Crash();model=new SkyPulseBirdProgression(disk,ids);model.RecoverPending();
            Check(model.Data.core==2&&model.Genome(ids[0]).level==2&&disk.GetInt(SkyPulseDailyMissions.WalletKey,0)==886,"Interrupted discounted training recovers its original 114-crystal debit at write "+failure);
        }
        var genes=new Store();genes.SetInt(SkyPulseDailyMissions.WalletKey,20000);var lineage=new SkyPulseBirdProgression(genes,ids);lineage.Data.core=3;
        foreach(var parent in new[]{ids[0],ids[1]})for(int level=1;level<3;level++)Check(lineage.Train(parent,lineage.Data.revision,Owned),"Train a parent for a stronger inherited grade");
        var stronger=lineage.Preview(ids[0],ids[1]);Check(stronger.primaryGrade==2&&stronger.secondaryGrade==2,"Core 3 and trained parents produce stronger inherited traits");
        Check(lineage.Breed(ids[0],ids[1],lineage.Data.revision,Owned),"Stronger offspring can be bred at its exact discounted price");
        for(int level=1;level<3;level++)Check(lineage.Train(stronger.id,lineage.Data.revision,Owned),"Inherited bird supports real training");
        stronger=lineage.Genome(stronger.id);var founderGrade=new BirdGenome{primary=stronger.primary,secondary=stronger.secondary,level=stronger.level};
        Check(SkyPulseBirdProgression.Points(stronger,BirdTrait.Collector)>SkyPulseBirdProgression.Points(founderGrade,BirdTrait.Collector)&&SkyPulseBirdProgression.ResultBonus(stronger,100,10,4)>SkyPulseBirdProgression.ResultBonus(founderGrade,100,10,4),"Higher inherited grades improve both actual collection strength and milestone rewards at equal training level");
        var investments=new Store();investments.SetInt(SkyPulseDailyMissions.WalletKey,1000000);var fleet=new SkyPulseBirdProgression(investments,ids);fleet.Data.core=5;
        Check(fleet.BirdUnlockPrice(250)==250&&fleet.BirdUnlockDiscountPercent==0,"Untrained saves retain original catalogue prices");
        foreach(var id in ids.Take(3))for(int level=1;level<5;level++)Check(fleet.Train(id,fleet.Data.revision,Owned),"Training contributes permanent Hangar savings");
        Check(fleet.BirdUnlockDiscountPercent==20&&fleet.BirdUnlockPrice(250)==200,"Training discount is capped at twenty percent across the fleet");
        fleet=new SkyPulseBirdProgression(investments,ids);Check(fleet.BirdUnlockDiscountPercent==20&&fleet.BirdUnlockPrice(7100)==5680,"Existing genome persistence restores bird unlock savings without a new save schema");
        Check(SkyPulseBirdProgression.BirdPriceAfterTrainingDiscount(101,2)==99&&SkyPulseBirdProgression.BirdPriceAfterTrainingDiscount(1,20)==1&&SkyPulseBirdProgression.BirdPriceAfterTrainingDiscount(0,20)==0&&SkyPulseBirdProgression.BirdPriceAfterTrainingDiscount(int.MaxValue,20)>0,"Discount rounding, free starter and overflow boundaries are safe");
        Debug.Log("SKYPULSE_BIRD_PROGRESSION_PURE_PASS");
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        game.enabled=false;Call(game,"ResetToMenu");
        var oldStore=Get(game,"missionStore");var oldDaily=Get(game,"dailyMissions");var oldProgress=Get(game,"birdProgression");var oldSkin=Get(game,"equippedSkin");int oldWallet=(int)Get(game,"crystals");
        var skins=(Array)typeof(SkyPulseNativeGame).GetField("Skins",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        var ids=skins.Cast<object>().Select(s=>(string)Get(s,"Id")).ToArray();var store=new Store();store.SetInt(SkyPulseDailyMissions.WalletKey,20000);
        Set(game,"missionStore",store);Set(game,"dailyMissions",new SkyPulseDailyMissions(store,()=>new DateTime(2026,10,21,0,0,0,DateTimeKind.Utc)));Set(game,"birdProgression",new SkyPulseBirdProgression(store,ids));Set(game,"crystals",20000);
        var owned=(HashSet<string>)Get(game,"ownedSkinIds");var previousOwned=new HashSet<string>(owned);owned.Add(ids[0]);owned.Add(ids[1]);Call(game,"RebuildBirdRoster");
        Call(game,"OpenUpgrades");yield return null;var tree=(SkyPulseTechTreeView)Get(game,"holographicTree");Call(game,"OpenCoreProgression");yield return null;
        var preview=(SkyPulseTechTreeView)Get(game,"coreProgressionPreview");
        Check(preview.TreeCamera.transform.parent.position!=tree.TreeCamera.transform.parent.position,"Core inspector and main tree use separate isolated render stages");
        var screen=(GameObject)Get(game,"birdProgressionScreen");PanelChecks(screen);
        var stale=screen.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text.StartsWith("UPGRADE ·"));
        Call(game,"OpenBirdTraining",skins.GetValue(0));stale.onClick.Invoke();
        Check(((SkyPulseBirdProgression)Get(game,"birdProgression")).Data.core==1&&(int)Get(game,"crystals")==20000,"A callback from an earlier panel cannot spend currency on a new panel");
        Call(game,"OpenCoreProgression");yield return null;
        screen.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text.StartsWith("UPGRADE ·")).onClick.Invoke();yield return null;
        var p=(SkyPulseBirdProgression)Get(game,"birdProgression");Check(p.Data.core==2&&(int)Get(game,"crystals")==19600&&!screen.activeSelf,"Real Core button purchases once, closes and updates wallet/tree");
        tree=(SkyPulseTechTreeView)Get(game,"holographicTree");Check(tree.CoreRank==2&&tree.Roots[0].Colour==SkyPulseBirdProgression.CoreColours[1],"Core level updates the actual core material colour and halo style");
        Call(game,"OpenHangar");Call(game,"OpenBirdTraining",skins.GetValue(0));yield return null;
        PanelChecks(screen);StudioChecks(game,1);
        Call(game,"UpdateProgressionPresentation",.3f);yield return null;SkyPulsePlayerExperienceChecks.CaptureUi(game,"bird-training");
        Check(screen.GetComponentsInChildren<Text>().Any(t=>t.text=="LEVEL 1 → 2   /   CORE LIMIT 3"),"Training states the exact level being purchased and current Core limit");
        Check((string)Call(game,"ProgressionBalance",20000)=="NEED 400 MORE ✦   /   BALANCE 19600 ✦","Insufficient-crystal copy gives the exact shortfall");
        screen.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text.StartsWith("TRAIN ·")).onClick.Invoke();yield return null;
        Check(p.Genome(ids[0]).level==2&&(int)Get(game,"crystals")==19486,"Training charges the displayed 114 crystals after the Core 2 discount");
        Check(p.BirdUnlockDiscountPercent==2&&p.BirdUnlockPrice(250)==245,"The real trained bird reduces subsequent Hangar unlock prices by two percent");
        Call(game,"BeginBirdProgressionRun");int pickupBonus=0;for(int i=0;i<100;i++)pickupBonus+=(int)Call(game,"BirdPickupBonus");
        Check(pickupBonus==8,"Training produces the exact live eight-percent pickup bonus across 100 physical pickups");Call(game,"BeginBirdProgressionRun");Check((float)Get(game,"birdCollectionCarry")==0,"Retry resets bird fractional collection carry");
        var levels=(Dictionary<string,int>)Get(game,"upgradeLevels");var oldLevels=new Dictionary<string,int>(levels);levels.Clear();
        var techOwned=(HashSet<string>)Get(game,"ownedUpgradeIds");var oldTechOwned=new HashSet<string>(techOwned);techOwned.Clear();
        Set(game,"runCrystalsCollected",100);Set(game,"score",10);Set(game,"perfectPasses",0);Set(game,"newBest",false);Set(game,"resultCrystalBonusApplied",false);
        int reward=(int)Call(game,"ApplyResultTechBonuses"),afterReward=(int)Get(game,"crystals");Call(game,"ApplyResultTechBonuses");
        Check(reward==4&&afterReward==19490&&(int)Get(game,"crystals")==afterReward,"Trained bird traits credit the real result wallet exactly once");
        Call(game,"RefreshProgressionResultLabels");
        Check((int)Get(game,"runBirdResultBonus")==4&&(int)Get(game,"runTechResultBonus")==0&&((Text)Get(game,"resultBirdBonusText")).text.Contains("+4")&&((Text)Get(game,"resultBonusText")).text.Contains("+0"),"Result names the bird's actual reward separately from Tech, without crediting again");
        store.SetInt(SkyPulseDailyMissions.WalletKey,afterReward);levels.Clear();foreach(var pair in oldLevels)levels[pair.Key]=pair.Value;foreach(var id in oldTechOwned)techOwned.Add(id);
        yield return DiscountedTechChecks(game,p,store);
        Call(game,"OpenBreeding",skins.GetValue(0));yield return null;
        PanelChecks(screen);StudioChecks(game,3);var oldStudio=(SkyPulseProgressionStage)Get(game,"birdProgressionStage");
        Call(game,"UpdateProgressionPresentation",.3f);yield return null;SkyPulsePlayerExperienceChecks.CaptureUi(game,"bird-lineage");
        Check(screen.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("· A / G1"))&&screen.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("· B / G1")),"Inheritance preview identifies the parent source and grade for both reward traits");
        Call(game,"OpenParentPicker",true);Check(!oldStudio.StageCamera.gameObject.activeInHierarchy,"Changing panels immediately disables the old studio camera");yield return null;PanelChecks(screen);
        Check(oldStudio==null,"Retired progression studio and its native resources are destroyed on the next frame");
        screen.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="BACK").onClick.Invoke();yield return null;
        Check(screen.GetComponentsInChildren<Text>().Any(t=>t.text=="LINEAGE LAB"),"Parent picker Back restores the exact breeding preview");
        var breedButton=screen.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text.StartsWith("BREED ·"));
        Check(breedButton.GetComponentInChildren<Text>().text=="BREED · 285 ✦","Lineage preview shows the Core 2 discounted breeding cost");
        breedButton.onClick.Invoke();yield return null;
        var roster=(Array)Get(game,"birdRoster");Check(roster.Length==16&&p.OffspringCount==1&&((string)Get(Get(game,"equippedSkin"),"Id")).StartsWith("lineage."),"Bred offspring enters the real carousel and can be equipped");
        Check(((GameObject)Get(game,"unlockRevealModal")).activeSelf&&((Text)Get(game,"unlockRevealDetail")).text=="BRED & EQUIPPED · BOTH PARENTS KEPT","Breeding presents the permanent new bird through the existing unlock reveal");Call(game,"CloseUnlockReveal");
        Call(game,"InitializeBirdProgression");Call(game,"LoadProgress");Check(((string)Get(Get(game,"equippedSkin"),"Id")).StartsWith("lineage."),"Equipped offspring and its inherited artwork survive load");
        Call(game,"OpenCoreProgression");Call(game,"StartRoundFromCustomize");Check(Get(game,"state").ToString()=="Customize","Progression modal cannot leak a background tap into flight");
        Call(game,"CloseBirdProgression");yield return BirdUnlockChecks(game,skins,ids);
        // Exercise all five real halo styles at frame-rate-independent clocks and Reduced Motion.
        foreach(int rank in Enumerable.Range(1,5))
        {
            p=(SkyPulseBirdProgression)Get(game,"birdProgression");p.Data.core=rank;Call(game,"CloseBirdProgression");Call(game,"OpenUpgrades");yield return null;tree=(SkyPulseTechTreeView)Get(game,"holographicTree");
            Quaternion pose=Quaternion.identity;var halo=tree.TreeCamera.transform.parent.Find("Living SkyPulse core projector").Find("Core level halo");
            foreach(int fps in new[]{30,60,120}){Set(tree,"clock",0f);for(int i=0;i<fps;i++)tree.Tick(1f/fps);if(fps==30)pose=halo.localRotation;Check(Quaternion.Angle(pose,halo.localRotation)<.05f,"Core "+rank+" halo matches at "+fps+" FPS");}
            tree.ReducedMotion=true;tree.Tick(0);pose=halo.localRotation;tree.Tick(1);Check(Quaternion.Angle(pose,halo.localRotation)<.01f,"Reduced Motion freezes Core "+rank+" halo");
        }
        Call(game,"CloseBirdProgression");Set(game,"missionStore",oldStore);Set(game,"dailyMissions",oldDaily);Set(game,"birdProgression",oldProgress);owned.Clear();foreach(var id in previousOwned)owned.Add(id);Set(game,"equippedSkin",oldSkin);Set(game,"crystals",oldWallet);Call(game,"RebuildBirdRoster");Set(game,"flightBirdGenome",null);Call(game,"SaveProgress");Call(game,"ResetToMenu");
        Debug.Log("SKYPULSE_BIRD_PROGRESSION_LIVE_PASS");
    }
    static IEnumerator BirdUnlockChecks(SkyPulseNativeGame game,Array skins,string[] ids)
    {
        var oldStore=Get(game,"missionStore");var oldDaily=Get(game,"dailyMissions");var oldProgress=Get(game,"birdProgression");var oldSkin=Get(game,"equippedSkin");int oldWallet=(int)Get(game,"crystals");
        var owned=(HashSet<string>)Get(game,"ownedSkinIds");bool wasOwned=owned.Remove(ids[2]);
        var store=new Store();store.SetInt(SkyPulseDailyMissions.WalletKey,20000);var fleet=new SkyPulseBirdProgression(store,ids);fleet.Data.core=2;Check(fleet.Train(ids[0],0,Owned),"Prepare a genuinely trained bird for the UI quote");
        Set(game,"missionStore",store);Set(game,"dailyMissions",new SkyPulseDailyMissions(store,()=>new DateTime(2026,10,22,0,0,0,DateTimeKind.Utc)));Set(game,"birdProgression",fleet);Call(game,"RebuildBirdRoster");
        store.SetInt(SkyPulseDailyMissions.WalletKey,489);Set(game,"crystals",489);Call(game,"SelectSkin",skins.GetValue(2));Call(game,"ConfirmPurchase");
        Check(!owned.Contains(ids[2])&&(int)Get(game,"crystals")==489&&!((Button)Get(game,"purchaseConfirmButton")).interactable,"One crystal below the discounted bird price cannot unlock");
        store.SetInt(SkyPulseDailyMissions.WalletKey,20000);Set(game,"crystals",20000);Call(game,"SelectSkin",skins.GetValue(2));
        Check(((Text)Get(game,"purchaseConfirmText")).text=="CONFIRM · 490 ✦","Bird inspector quotes the same discounted price as the Hangar");
        Check(fleet.Train(ids[1],fleet.Data.revision,Owned),"A second completed training changes the fleet saving");Set(game,"crystals",store.GetInt(SkyPulseDailyMissions.WalletKey,0));int before=(int)Get(game,"crystals");Call(game,"ConfirmPurchase");
        Check((int)Get(game,"crystals")==before&&!owned.Contains(ids[2])&&((Text)Get(game,"purchaseConfirmText")).text=="CONFIRM · 480 ✦","Stale bird quote refreshes to the new training price without spending");
        store.SetInt(SkyPulseDailyMissions.WalletKey,480);Set(game,"crystals",480);Call(game,"ConfirmPurchase");Call(game,"ConfirmPurchase");
        Check(owned.Contains(ids[2])&&(int)Get(game,"crystals")==0,"Exact discounted balance unlocks the new bird once");Call(game,"CloseUnlockReveal");
        if(!wasOwned)owned.Remove(ids[2]);Set(game,"missionStore",oldStore);Set(game,"dailyMissions",oldDaily);Set(game,"birdProgression",oldProgress);Set(game,"equippedSkin",oldSkin);Set(game,"crystals",oldWallet);Call(game,"RebuildBirdRoster");yield return null;
    }
    static IEnumerator DiscountedTechChecks(SkyPulseNativeGame game,SkyPulseBirdProgression p,Store store)
    {
        int oldCore=p.Data.core,oldWallet=(int)Get(game,"crystals");
        var levels=(Dictionary<string,int>)Get(game,"upgradeLevels");var saved=new Dictionary<string,int>(levels);
        var owned=(HashSet<string>)Get(game,"ownedUpgradeIds");var savedOwned=new HashSet<string>(owned);
        var upgrades=(Array)typeof(SkyPulseNativeGame).GetField("Upgrades",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);var first=upgrades.GetValue(0);
        int[] costs={150,143,135,128,120};
        foreach(int rank in Enumerable.Range(1,5))
        {
            p.Data.core=rank;levels.Clear();owned.Clear();store.SetInt(SkyPulseDailyMissions.WalletKey,costs[rank-1]);Set(game,"crystals",costs[rank-1]);
            Call(game,"OpenUpgrades");yield return null;
            var content=(RectTransform)Get(game,"customizeContent");
            Check(content.GetComponentsInChildren<Text>().Any(t=>t.text=="NEXT · "+costs[rank-1]+" ✦"),"Core "+rank+" tree map shows its actual discounted purchase price");
            Call(game,"SelectUpgrade",first);
            Check(((Text)Get(game,"purchaseConfirmText")).text=="CONFIRM · "+costs[rank-1]+" ✦"&&((Button)Get(game,"purchaseConfirmButton")).interactable,"Core "+rank+" inspector matches the map and accepts the exact discounted balance");
            Call(game,"ConfirmPurchase");Call(game,"ConfirmPurchase");yield return null;
            Check((int)Get(game,"crystals")==0&&levels["crystal_resonator"]==1,"Core "+rank+" real Tech Tree purchase debits its displayed price once");
        }
        levels.Clear();owned.Clear();p.Data.core=2;store.SetInt(SkyPulseDailyMissions.WalletKey,10000);Set(game,"crystals",10000);Call(game,"SelectUpgrade",first);
        p.Data.core=3;Call(game,"ConfirmPurchase");
        Check((int)Get(game,"crystals")==10000&&levels.Count==0&&((Text)Get(game,"purchaseConfirmText")).text=="CONFIRM · 135 ✦","A changed Core refreshes a stale Tech Tree quote without spending");
        Call(game,"ConfirmPurchase");Call(game,"ConfirmPurchase");
        Check((int)Get(game,"crystals")==9865&&levels["crystal_resonator"]==1,"A fresh confirmation charges the updated quote once");
        p.Data.core=oldCore;levels.Clear();foreach(var pair in saved)levels[pair.Key]=pair.Value;owned.Clear();foreach(var id in savedOwned)owned.Add(id);
        store.SetInt(SkyPulseDailyMissions.WalletKey,oldWallet);Set(game,"crystals",oldWallet);Call(game,"OpenHangar");yield return null;
    }
}
