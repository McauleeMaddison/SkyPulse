using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        SkyPulseBirdProgression birdProgression;
        Skin[] birdRoster;
        Skin[] BirdRoster => birdRoster ?? Skins;
        GameObject birdProgressionScreen;
        RectTransform birdProgressionBody;
        SkyPulseTechTreeView coreProgressionPreview;
        SkyPulseProgressionStage birdProgressionStage;
        CanvasGroup progressionCanvas;
        float progressionReveal;
        Color progressionAccent=new Color(.45f,.9f,1);
        Text birdProgressionNotice;
        bool progressionCommitting;
        int progressionPanelEpoch;
        string breedingParentA,breedingParentB;
        BirdGenome flightBirdGenome;
        float birdCollectionCarry;
        int runBirdPickupBonus,runTechPickupBonus,runBirdResultBonus,runTechResultBonus;
        int quotedUpgradePrice=-1,quotedUpgradeLevel=-1,quotedUpgradeCore=-1;
        int UpgradeCrystalPrice(Upgrade upgrade,int level)=>birdProgression.DiscountedPrice(upgrade.PriceAtLevel(level));
        void QuoteUpgrade(Upgrade upgrade,int level)
        {
            quotedUpgradePrice=UpgradeCrystalPrice(upgrade,level);quotedUpgradeLevel=level;quotedUpgradeCore=birdProgression.Data.core;
        }
        bool UpgradeQuoteCurrent(Upgrade upgrade)=>quotedUpgradeLevel==GetUpgradeLevel(upgrade.Id)&&quotedUpgradeCore==birdProgression.Data.core&&quotedUpgradePrice==UpgradeCrystalPrice(upgrade,quotedUpgradeLevel);
        string CorePriceDescription(int basePrice)
        {
            int discount=birdProgression.UpgradeDiscountPercent;
            return discount==0?"ION CORE · BASE PRICE "+basePrice+" ✦":"BASE "+basePrice+" ✦ · CORE −"+discount+"% · SAVE "+(basePrice-birdProgression.DiscountedPrice(basePrice))+" ✦";
        }
        void InitializeBirdProgression()
        {
            var ids=new string[Skins.Length];for(int i=0;i<ids.Length;i++)ids[i]=Skins[i].Id;
            birdProgression=new SkyPulseBirdProgression(missionStore,ids);
            RecoverWalletTransaction();RebuildBirdRoster();
        }
        void RebuildBirdRoster()
        {
            var roster=new List<Skin>(Skins);
            foreach(var b in birdProgression.Data.birds)if(b.generation>0)roster.Add(BredSkin(b));
            birdRoster=roster.ToArray();
        }
        Skin FindBird(string id)=>FindById(BirdRoster,id);
        Skin BredSkin(BirdGenome b)
        {
            var original=FindById(Skins,b.appearance)??Skins[0];
            var skin=new Skin(b.id,original.Name+" · "+b.id.Substring(8),original.ArtPath,original.FlapPath,"#ffffff","#ffffff",0,
                original.RisePath,original.HitPath,original.UnlockPath,original.FlapFramePaths,original.FlapFramePivots);
            skin.Accent=Color.Lerp(SkyPulseBirdProgression.CoreColours[b.primary],SkyPulseBirdProgression.CoreColours[b.secondary],.4f);
            skin.Trail=SkyPulseBirdProgression.CoreColours[b.secondary];return skin;
        }
        string BirdTraitSummary(Skin skin)=>SkyPulseBirdProgression.TraitText(birdProgression.Genome(skin.Id));
        int BirdLevel(Skin skin)=>birdProgression.Genome(skin.Id)?.level??1;
        bool BredBird(Skin skin)=>skin!=null&&skin.Id.StartsWith("lineage.",StringComparison.Ordinal);
        bool OwnBirdId(string id) {var bird=FindBird(id);return bird!=null&&IsSkinOwned(bird);}
        void BeginBirdProgressionRun()
        {
            // Snapshot the equipped build once; no DNA lookup/allocation in the pickup loop.
            flightBirdGenome=birdProgression.Genome(equippedSkin.Id);birdCollectionCarry=0;
            runBirdPickupBonus=runTechPickupBonus=runBirdResultBonus=runTechResultBonus=0;
        }
        int BirdPickupBonus()
        {
            birdCollectionCarry+=SkyPulseBirdProgression.Points(flightBirdGenome,BirdTrait.Collector)*.02f;
            int earned=Mathf.FloorToInt(birdCollectionCarry+.0001f);birdCollectionCarry-=earned;return earned;
        }
        int BirdRunBonus()=>SkyPulseBirdProgression.ResultBonus(flightBirdGenome??birdProgression.Genome(equippedSkin.Id),runCrystalsCollected,score,perfectPasses);
        Text ProgressionText(Transform parent,string text,float y,int size=30,float height=80,Color? colour=null)
        {
            var label=CreateText(parent,text,new Vector2(0,y),new Vector2(790,height),size,colour??Hex("#dceef5"),TextAnchor.MiddleCenter,FontStyle.Normal);
            label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;
            label.GetComponent<Shadow>().enabled=false;return label;
        }
        Button ProgressionButton(Transform parent,string text,Vector2 at,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            size.y=Mathf.Max(152,size.y);
            var hit=CreateImage(parent,"Progression control · "+text,at,size,new Color(1,1,1,.001f));hit.raycastTarget=true;
            bool primary=text.StartsWith("UPGRADE ·",StringComparison.Ordinal)||text.StartsWith("TRAIN ·",StringComparison.Ordinal)||text.StartsWith("BREED ·",StringComparison.Ordinal)||text=="VIEW CORE";
            var face=ProgressionSurface(hit.transform,"Control face",Vector2.zero,size-new Vector2(2,12),primary?SkyPulseProgressionSurface.Finish.Action:text=="BACK"?SkyPulseProgressionSurface.Finish.Quiet:SkyPulseProgressionSurface.Finish.Inset);
            var label=ProgressionText(hit.transform,text,0,32,110,primary?Hex("#061b25"):Hex("#d5e8f0"));label.rectTransform.sizeDelta=new Vector2(size.x-48,110);label.fontStyle=FontStyle.Bold;
            label.resizeTextForBestFit=true;label.resizeTextMinSize=26;label.resizeTextMaxSize=32;
            var b=hit.gameObject.AddComponent<Button>();b.targetGraphic=face;
            var colours=b.colors;colours.pressedColor=new Color(.72f,.84f,.88f);colours.disabledColor=new Color(.45f,.51f,.57f,.6f);colours.fadeDuration=.07f;b.colors=colours;
            b.gameObject.AddComponent<SkyPulseButtonFeedback>().ReducedMotion=reduceMotionEnabled;
            bool modal=birdProgressionBody!=null&&(parent==birdProgressionBody||parent.IsChildOf(birdProgressionBody));
            int epoch=progressionPanelEpoch;
            b.navigation=new Navigation{mode=Navigation.Mode.None};
            b.onClick.AddListener(()=>{if(modal&&epoch!=progressionPanelEpoch)return;action();});return b;
        }
        SkyPulseProgressionSurface ProgressionSurface(Transform parent,string name,Vector2 at,Vector2 size,SkyPulseProgressionSurface.Finish style)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(SkyPulseProgressionSurface));go.transform.SetParent(parent,false);
            var face=go.GetComponent<SkyPulseProgressionSurface>();face.rectTransform.anchoredPosition=at;face.rectTransform.sizeDelta=size;
            face.Style=style;face.Accent=progressionAccent;face.raycastTarget=false;return face;
        }
        Text ProgressionLabel(Transform parent,string value,Vector2 at,Vector2 size,int font,Color colour,TextAnchor anchor=TextAnchor.MiddleLeft)
        {
            var label=ProgressionText(parent,value,at.y,font,size.y,colour);label.rectTransform.anchoredPosition=at;label.rectTransform.sizeDelta=size;label.alignment=anchor;return label;
        }
        void UpdateProgressionPresentation(float dt)
        {
            if(birdProgressionScreen==null||!birdProgressionScreen.activeInHierarchy)return;
            progressionReveal=Mathf.Min(1,progressionReveal+Mathf.Max(0,dt)/.18f);
            progressionCanvas.alpha=reduceMotionEnabled?1:.8f+.2f*Mathf.SmoothStep(0,1,progressionReveal);
            if(coreProgressionPreview!=null)coreProgressionPreview.ReducedMotion=reduceMotionEnabled;
            if(birdProgressionStage!=null)birdProgressionStage.ReducedMotion=reduceMotionEnabled;
        }
        void ProgressionPanel(string title,string subtitle,UnityEngine.Events.UnityAction back=null,Color? accent=null)
        {
            progressionPanelEpoch++;progressionAccent=accent??Hex("#72e5f1");progressionReveal=0;
            if(birdProgressionScreen==null)
            {
                birdProgressionScreen=CreateScreen(interfaceContentRoot,"Core and bird progression");
                var veil=CreateFullPanel(birdProgressionScreen.transform,"Progression dim",new Color(.003f,.007f,.015f,.97f));veil.GetComponent<Image>().raycastTarget=true;
                birdProgressionBody=new GameObject("Progression workspace",typeof(RectTransform),typeof(CanvasGroup)).GetComponent<RectTransform>();birdProgressionBody.SetParent(birdProgressionScreen.transform,false);
                birdProgressionBody.anchoredPosition=new Vector2(0,-10);birdProgressionBody.sizeDelta=new Vector2(940,1640);progressionCanvas=birdProgressionBody.GetComponent<CanvasGroup>();
            }
            if(holographicTree!=null){holographicTree.CancelPress();holographicTree.CancelFocus();holographicTree.MotionPaused=true;}
            foreach(Transform child in birdProgressionBody){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            coreProgressionPreview=null;birdProgressionStage=null;birdProgressionScreen.SetActive(true);birdProgressionScreen.transform.SetAsLastSibling();progressionCommitting=false;
            progressionCanvas.alpha=reduceMotionEnabled?1:.8f;
            ProgressionSurface(birdProgressionBody,"Satin glass workspace",Vector2.zero,birdProgressionBody.sizeDelta,SkyPulseProgressionSurface.Finish.Sheet);
            var titleText=ProgressionLabel(birdProgressionBody,title,new Vector2(-25,684),new Vector2(770,90),39,Hex("#eff9fc"));titleText.fontStyle=FontStyle.Bold;
            titleText.resizeTextForBestFit=true;titleText.resizeTextMinSize=30;titleText.resizeTextMaxSize=39;
            ProgressionLabel(birdProgressionBody,subtitle,new Vector2(-25,613),new Vector2(770,65),28,Hex("#a9c3d0"));
            CreateUiGlyph(birdProgressionBody,"Progression identity",new Vector2(385,687),new Vector2(42,36),progressionAccent,SkyPulseUiGlyph.Kind.WingMark);
            birdProgressionNotice=ProgressionText(birdProgressionBody,"",-565,26,85,Hex("#b0cbd7"));
            if(back==null)back=CloseBirdProgression;
            ProgressionButton(birdProgressionBody,"BACK",new Vector2(0,-690),new Vector2(820,152),back);
        }
        void CloseBirdProgression()
        {
            progressionPanelEpoch++;
            if(birdProgressionScreen!=null)birdProgressionScreen.SetActive(false);
            if(holographicTree!=null)holographicTree.MotionPaused=false;
        }
        void CommitBirdProgression(Func<bool> change,string equip=null)
        {
            if(progressionCommitting)return;progressionCommitting=true;
            try
            {
                if(!RecoverWalletTransaction()||!change()) {birdProgressionNotice.text="This choice has changed. Reopen the panel to refresh.";progressionCommitting=false;return;}
                var at=customizeContent.anchoredPosition;
                crystals=missionStore.GetInt(SkyPulseDailyMissions.WalletKey,crystals);RebuildBirdRoster();
                equippedSkin=FindBird(equip??equippedSkin.Id)??Skins[0];
                if(equip!=null){ownedSkinIds.Add(equip);hangarPageIndex=Array.FindIndex(BirdRoster,b=>b.Id==equip);}
                equippedTrail=GetTrailForSkin(equippedSkin);CloseBirdProgression();ApplyEquippedVisuals();SaveProgress();RebuildCustomizeGrid();
                if(cosmeticCategory==CosmeticCategory.Upgrades)customizeContent.anchoredPosition=at;
                Play(unlockSound);PulseHaptic(.3f);
            }
            catch(Exception e){Debug.LogWarning("SkyPulse progression save will retry: "+e.GetType().Name);birdProgressionNotice.text="Couldn't finish saving. Reopen this panel to retry safely.";}
        }
        void OpenCoreProgression()
        {
            RecoverWalletTransaction();int rank=birdProgression.Data.core,next=Math.Min(5,rank+1),revision=birdProgression.Data.revision;
            var accent=SkyPulseBirdProgression.CoreColours[next-1];
            ProgressionPanel("SKYPULSE CORE","EVOLUTION   /   "+SkyPulseBirdProgression.CoreNames[rank-1]+" · LEVEL "+rank,null,accent);
            var aura=CreateImage(birdProgressionBody,"Core ambient light",new Vector2(0,330),new Vector2(730,420),new Color(accent.r,accent.g,accent.b,.09f));aura.sprite=softCircleSprite;aura.raycastTarget=false;
            var raw=CreateImage(birdProgressionBody,"Core preview footprint",new Vector2(0,327),new Vector2(656,320),Color.clear).rectTransform;raw.localScale=Vector3.one*1.25f;
            var visual=new GameObject("Next Core preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage),typeof(SkyPulseTechTreeView));visual.transform.SetParent(raw,false);
            var image=visual.GetComponent<RawImage>();image.rectTransform.anchorMin=Vector2.zero;image.rectTransform.anchorMax=Vector2.one;image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero;
            var content=new GameObject("Preview origin",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(raw,false);content.anchoredPosition=Vector2.zero;
            coreProgressionPreview=visual.GetComponent<SkyPulseTechTreeView>();coreProgressionPreview.CoreRank=next;coreProgressionPreview.ReducedMotion=reduceMotionEnabled;
            coreProgressionPreview.Build(image,content,raw,new List<HolographicBranch>(),new List<HolographicRoot>{new HolographicRoot{At=new Vector2(0,-160),Colour=SkyPulseBirdProgression.CoreColours[next-1],Powered=true,Size=57}});
            ProgressionText(birdProgressionBody,rank<5?SkyPulseBirdProgression.CoreNames[next-1]+"  /  LEVEL "+next:"NOVA  /  FULLY EVOLVED",110,33,65,accent).fontStyle=FontStyle.Bold;
            for(int i=0;i<5;i++)
            {
                var colour=SkyPulseBirdProgression.CoreColours[i];float x=(i-2)*166;
                var rail=CreateImage(birdProgressionBody,"Core evolution step "+(i+1),new Vector2(x,21),new Vector2(145,i+1==next?5:2),i+1<=next?colour:Hex("#314352"));rail.raycastTarget=false;
                ProgressionLabel(birdProgressionBody,SkyPulseBirdProgression.CoreNames[i],new Vector2(x,-17),new Vector2(150,48),21,i+1==next?colour:Hex("#92acbb"),TextAnchor.MiddleCenter);
            }
            CoreBenefit(-280,"TRAINING",Math.Min(5,rank+1)+" → "+Math.Min(5,next+1),"Bird level cap");
            CoreBenefit(0,"LINEAGE",(rank<2?0:rank*4)+" → "+next*4,"Offspring bays");
            int grade=next>=5?3:next>=3?2:1;
            CoreBenefit(280,"INHERITANCE","G"+grade,rank<2?"Breeding unlocks":"Maximum trait grade");
            int currentDiscount=birdProgression.UpgradeDiscountPercent,nextDiscount=SkyPulseBirdProgression.DiscountPercentForCore(next);
            ProgressionText(birdProgressionBody,(rank<5?"UPGRADE SAVINGS · "+currentDiscount+"% → "+nextDiscount+"%":"UPGRADE SAVINGS · "+currentDiscount+"%")+"\nTech Tree · Bird training · Breeding",-308,26,95,Hex("#d2e5ed"));
            var button=ProgressionButton(birdProgressionBody,rank<5?"UPGRADE · "+birdProgression.CorePrice+" ✦":"MAXIMUM CORE",new Vector2(0,-445),new Vector2(820,152),()=>CommitBirdProgression(()=>birdProgression.UpgradeCore(revision)));
            button.interactable=rank<5&&crystals>=birdProgression.CorePrice;birdProgressionNotice.text="BALANCE · "+crystals+" ✦";
        }
        void CoreBenefit(float x,string heading,string value,string detail)
        {
            var tile=ProgressionSurface(birdProgressionBody,"Core benefit · "+heading,new Vector2(x,-157),new Vector2(260,188),SkyPulseProgressionSurface.Finish.Inset).transform;
            ProgressionLabel(tile,heading,new Vector2(0,52),new Vector2(232,40),24,Hex("#b1cbd9"),TextAnchor.MiddleCenter);
            ProgressionLabel(tile,value,new Vector2(0,0),new Vector2(232,62),38,progressionAccent,TextAnchor.MiddleCenter).fontStyle=FontStyle.Bold;
            ProgressionLabel(tile,detail,new Vector2(0,-57),new Vector2(232,48),24,Hex("#c6dae3"),TextAnchor.MiddleCenter);
        }
        SkyPulseProgressionStage.Portrait ProgressionPortrait(Skin skin,Vector2 at,Vector2 size)
        {
            Sprite[] frames=null;
            if(skin.FlapFramePaths!=null&&skin.FlapFramePaths.Length==6)
            {
                frames=new Sprite[6];for(int i=0;i<6;i++)frames[i]=LoadRegisteredFlapFrame(skin.FlapFramePaths[i],skin.FlapFramePivots==null?new Vector2(.5f,.5f):skin.FlapFramePivots[i]);
                foreach(var f in frames)if(f==null){frames=null;break;}
            }
            return new SkyPulseProgressionStage.Portrait{At=at,Size=size,Accent=skin.Accent,Frames=frames,Still=frames!=null?frames[0]:LoadPresentationPose(skin.UnlockPath)??LoadSprite(skin.ArtPath)};
        }
        void ProgressionStudio(Vector2 at,Vector2 size,SkyPulseProgressionStage.Portrait[] portraits,bool lineage=false)
        {
            var root=new GameObject("Bird progression studio",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage),typeof(SkyPulseProgressionStage));root.transform.SetParent(birdProgressionBody,false);
            var raw=root.GetComponent<RawImage>();raw.rectTransform.anchoredPosition=at;raw.rectTransform.sizeDelta=size;
            birdProgressionStage=root.GetComponent<SkyPulseProgressionStage>();birdProgressionStage.ReducedMotion=reduceMotionEnabled;birdProgressionStage.Build(raw,portraits,lineage);
        }
        void TraitComparison(BirdGenome bird,BirdGenome next,int trait,float y)
        {
            var t=(BirdTrait)trait;int before=SkyPulseBirdProgression.Points(bird,t),after=SkyPulseBirdProgression.Points(next,t);
            var row=ProgressionSurface(birdProgressionBody,"Trait comparison · "+t,new Vector2(0,y),new Vector2(820,128),SkyPulseProgressionSurface.Finish.Inset).transform;
            var colour=SkyPulseBirdProgression.CoreColours[trait];
            CreateUiGlyph(row,"Trait optical mark",new Vector2(-355,0),new Vector2(36,44),colour,SkyPulseUiGlyph.Kind.CircuitHex);
            ProgressionLabel(row,t.ToString().ToUpperInvariant(),new Vector2(-128,25),new Vector2(370,42),25,Hex("#b5cedb"));
            ProgressionLabel(row,TraitContext(t),new Vector2(-128,-23),new Vector2(370,48),27,Hex("#d7e9f1"));
            string value=TraitValue(t,before)+(before!=after?" → "+TraitValue(t,after):"");
            ProgressionLabel(row,value,new Vector2(244,0),new Vector2(285,80),33,colour,TextAnchor.MiddleRight).fontStyle=FontStyle.Bold;
        }
        static string TraitValue(BirdTrait t,int points)=>t==BirdTrait.Collector||t==BirdTrait.Salvager?"+"+(points*2)+"%":"+"+(t==BirdTrait.Recovery?points*2:points)+" ✦";
        static string TraitContext(BirdTrait t)
        {
            switch(t){case BirdTrait.Collector:return "Crystals collected";case BirdTrait.Salvager:return "Run crystal bonus";case BirdTrait.Precision:return "Every 4 perfect gates";case BirdTrait.Voyager:return "Every 10 gates";default:return "After 5 gates";}
        }
        void OpenBirdTraining(Skin skin)
        {
            RecoverWalletTransaction();var bird=birdProgression.Genome(skin.Id);int revision=birdProgression.Data.revision;
            ProgressionPanel(skin.Name,"FLIGHT TRAINING   /   LEVEL "+bird.level+" · CORE "+birdProgression.Data.core,null,skin.Accent);
            ProgressionStudio(new Vector2(0,340),new Vector2(820,420),new[]{ProgressionPortrait(skin,new Vector2(0,35),new Vector2(495,275))});
            for(int i=0;i<5;i++)
            {
                var bar=CreateImage(birdProgressionBody,"Bird training level "+(i+1),new Vector2((i-2)*166,116),new Vector2(145,4),i<bird.level?skin.Accent:Hex("#2d414f"));bar.raycastTarget=false;
            }
            ProgressionText(birdProgressionBody,"LEVEL "+bird.level+"  /  "+birdProgression.TrainingCap+" AVAILABLE",74,22,48,Hex("#aec9d7"));
            bool can=IsSkinOwned(skin)&&bird.level<birdProgression.TrainingCap;
            var trained=JsonUtility.FromJson<BirdGenome>(JsonUtility.ToJson(bird));if(can)trained.level++;
            TraitComparison(bird,trained,bird.primary,-33);TraitComparison(bird,trained,bird.secondary,-179);
            ProgressionText(birdProgressionBody,can?"Next level · stronger rewards in Endless Route":bird.level==5?"Both traits are fully trained":"Evolve your Core to unlock further training",-307,26,68,Hex("#b2cbd7"));
            int price=birdProgression.TrainingCost(bird);
            var button=ProgressionButton(birdProgressionBody,can?"TRAIN · "+price+" ✦":bird.level>=5?"FULLY TRAINED":"VIEW CORE",new Vector2(0,-445),new Vector2(820,152),()=>{if(can)CommitBirdProgression(()=>birdProgression.Train(skin.Id,revision,OwnBirdId));else OpenCoreProgression();});
            button.interactable=can?crystals>=price:IsSkinOwned(skin)&&bird.level<5;
            birdProgressionNotice.text=!can&&bird.level<5?"CORE LEVEL "+(birdProgression.Data.core+1)+" UNLOCKS THE NEXT LEVEL":"BALANCE · "+crystals+" ✦";
            if(can)birdProgressionNotice.text+="\n"+CorePriceDescription(SkyPulseBirdProgression.TrainingPrices[bird.level-1]);
        }
        List<Skin> OwnedBirdRoster(){var list=new List<Skin>();foreach(var b in BirdRoster)if(IsSkinOwned(b))list.Add(b);return list;}
        void OpenBreeding(Skin first)
        {
            RecoverWalletTransaction();var owned=OwnedBirdRoster();
            if(first!=null)breedingParentA=first.Id;
            if(!OwnBirdId(breedingParentA))breedingParentA=owned[0].Id;
            if(!OwnBirdId(breedingParentB)||breedingParentB==breedingParentA){breedingParentB=null;foreach(var b in owned)if(b.Id!=breedingParentA){breedingParentB=b.Id;break;}}
            ProgressionPanel("LINEAGE LAB","INHERITANCE   /   BOTH PARENTS ARE KEPT",null,Hex("#9ab7ff"));
            var parentAControl=ParentPortrait(true,breedingParentA);var parentBControl=ParentPortrait(false,breedingParentB);
            var preview=birdProgression.Preview(breedingParentA,breedingParentB);int revision=birdProgression.Data.revision;
            string firstId=breedingParentA,secondId=breedingParentB;
            string block=birdProgression.BreedingBlock(breedingParentA,breedingParentB,OwnBirdId);
            if(preview!=null)
            {
                var skin=BredSkin(preview);
                ProgressionStudio(new Vector2(0,270),new Vector2(820,660),new[]{ProgressionPortrait(FindBird(firstId),new Vector2(-210,175),new Vector2(250,140)),ProgressionPortrait(FindBird(secondId),new Vector2(210,175),new Vector2(250,140)),ProgressionPortrait(skin,new Vector2(0,-110),new Vector2(375,200))},true);
                ProgressionText(birdProgressionBody,"OFFSPRING   /   GENERATION "+preview.generation,-59,23,50,Hex("#c6d6ff"));
                InheritedTrait(preview,true,-210);InheritedTrait(preview,false,210);
                ProgressionText(birdProgressionBody,"Combine reward traits for your style of play.\n"+(birdProgression.Data.core<3?"Core 3 unlocks stronger inherited grades.":"Level 3+ parents can pass stronger grades."),-297,26,88,Hex("#b8cbd7"));
            }
            else
            {
                ProgressionStudio(new Vector2(0,270),new Vector2(820,660),new[]{ProgressionPortrait(FindBird(firstId),new Vector2(-210,175),new Vector2(250,140))});
                ProgressionText(birdProgressionBody,"Unlock a second bird to create your first lineage.",-90,32,160);
            }
            // Labels and fixed touch targets stay above the shared transparent
            // render target; the recessed card backgrounds remain behind it.
            parentAControl.transform.SetAsLastSibling();parentBControl.transform.SetAsLastSibling();
            var create=ProgressionButton(birdProgressionBody,preview!=null?"BREED · "+birdProgression.BreedingCost(preview)+" ✦":"CHOOSE TWO PARENTS",new Vector2(0,-445),new Vector2(820,152),()=>CommitBirdProgression(()=>birdProgression.Breed(firstId,secondId,revision,OwnBirdId),preview?.id));
            create.interactable=block==""&&crystals>=birdProgression.BreedingCost(preview);
            birdProgressionNotice.text=block!=""?block:"BALANCE · "+crystals+" ✦   /   "+birdProgression.OffspringCount+" OF "+birdProgression.BreedingCapacity+" BAYS\n"+CorePriceDescription(SkyPulseBirdProgression.BreedingPrice(preview));
        }
        void InheritedTrait(BirdGenome bird,bool first,float x)
        {
            var trait=(BirdTrait)(first?bird.primary:bird.secondary);int grade=first?bird.primaryGrade:bird.secondaryGrade;
            var row=ProgressionSurface(birdProgressionBody,"Inherited trait · "+trait,new Vector2(x,-168),new Vector2(400,156),SkyPulseProgressionSurface.Finish.Inset).transform;
            var colour=SkyPulseBirdProgression.CoreColours[(int)trait];
            ProgressionLabel(row,trait.ToString().ToUpperInvariant()+"  /  G"+grade,new Vector2(0,46),new Vector2(370,38),24,Hex("#b6cbd7"),TextAnchor.MiddleCenter);
            ProgressionLabel(row,TraitValue(trait,SkyPulseBirdProgression.Points(bird,trait)),new Vector2(0,-3),new Vector2(370,58),34,colour,TextAnchor.MiddleCenter).fontStyle=FontStyle.Bold;
            ProgressionLabel(row,TraitContext(trait),new Vector2(0,-54),new Vector2(370,38),23,Hex("#c2d5e0"),TextAnchor.MiddleCenter);
        }
        Button ParentPortrait(bool first,string id)
        {
            float x=first?-210:210;var skin=FindBird(id);
            ProgressionSurface(birdProgressionBody,"Recessed parent studio",new Vector2(x,416),new Vector2(400,320),SkyPulseProgressionSurface.Finish.Inset);
            var button=ProgressionButton(birdProgressionBody,(first?"PARENT A":"PARENT B")+" · CHANGE",new Vector2(x,416),new Vector2(400,320),()=>OpenParentPicker(first));
            ((SkyPulseProgressionSurface)button.targetGraphic).Style=SkyPulseProgressionSurface.Finish.Overlay;
            var caption=button.GetComponentInChildren<Text>();caption.rectTransform.anchoredPosition=new Vector2(0,124);caption.fontSize=23;caption.resizeTextMaxSize=23;caption.resizeTextMinSize=23;caption.rectTransform.sizeDelta=new Vector2(370,46);caption.fontStyle=FontStyle.Normal;caption.color=Hex("#c7d4ef");
            var label=ProgressionLabel(button.transform,skin?.Name??"NOT SELECTED",new Vector2(0,-120),new Vector2(370,44),25,Hex("#e5f0f8"),TextAnchor.MiddleCenter);
            label.resizeTextForBestFit=true;label.resizeTextMinSize=19;label.resizeTextMaxSize=23;
            return button;
        }
        void OpenParentPicker(bool first)
        {
            ProgressionPanel(first?"CHOOSE PARENT A":"CHOOSE PARENT B","YOUR HANGAR   /   TRAINING IMPROVES INHERITANCE",()=>OpenBreeding(null),Hex("#9ab7ff"));
            var viewport=CreateImage(birdProgressionBody,"Parent list",new Vector2(0,20),new Vector2(840,1010),Color.clear).rectTransform;
            viewport.GetComponent<Image>().raycastTarget=true;
            viewport.gameObject.AddComponent<RectMask2D>();var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.decelerationRate=.08f;
            var content=new GameObject("Owned parent choices",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport,false);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);scroll.content=content;
            var birds=OwnedBirdRoster();content.sizeDelta=new Vector2(0,Mathf.Max(1010,birds.Count*190));
            for(int i=0;i<birds.Count;i++)
            {
                var selected=birds[i];var button=ProgressionButton(content,selected.Name+" · LV "+BirdLevel(selected),new Vector2(0,-90-i*190),new Vector2(820,172),()=>{if(first)breedingParentA=selected.Id;else breedingParentB=selected.Id;OpenBreeding(null);});
                button.GetComponent<RectTransform>().anchorMin=button.GetComponent<RectTransform>().anchorMax=new Vector2(.5f,1);
                var caption=button.GetComponentInChildren<Text>();caption.rectTransform.anchoredPosition=new Vector2(60,29);caption.rectTransform.sizeDelta=new Vector2(610,62);caption.alignment=TextAnchor.MiddleLeft;caption.resizeTextMaxSize=26;
                var portrait=CreateImage(button.transform,"Parent identity",new Vector2(-325,0),new Vector2(142,112),Color.white);portrait.sprite=LoadPresentationPose(selected.UnlockPath)??LoadSprite(selected.ArtPath);portrait.preserveAspect=true;portrait.raycastTarget=false;
                ProgressionLabel(button.transform,BirdTraitSummary(selected),new Vector2(65,-34),new Vector2(615,58),22,Hex("#a9c9da"));
                button.interactable=selected.Id!=(first?breedingParentB:breedingParentA);
            }
            birdProgressionNotice.text="Tap a bird to see the updated inheritance";
        }
    }
}
