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
        Text birdProgressionNotice;
        bool progressionCommitting;
        int progressionPanelEpoch;
        string breedingParentA,breedingParentB;
        BirdGenome flightBirdGenome;
        float birdCollectionCarry;
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
            size.y=Mathf.Max(152,size.y); // At least 45 points on a 320-point-wide phone.
            var b=CreateNeonButton(parent,text,at,size,Hex("#74e9ef"));
            b.GetComponent<Image>().sprite=roundedPanelSprite;b.GetComponent<Outline>().enabled=false;
            ((Image)b.targetGraphic).sprite=roundedPanelSprite;b.transform.Find("Flight control keyline").gameObject.SetActive(false);
            var label=b.GetComponentInChildren<Text>();label.fontSize=27;label.GetComponent<Shadow>().enabled=false;
            b.GetComponent<SkyPulseButtonFeedback>().ReducedMotion=reduceMotionEnabled;
            bool modal=birdProgressionBody!=null&&(parent==birdProgressionBody||parent.IsChildOf(birdProgressionBody));
            int epoch=progressionPanelEpoch;
            b.navigation=new Navigation{mode=Navigation.Mode.None};
            b.onClick.AddListener(()=>{if(modal&&epoch!=progressionPanelEpoch)return;action();});return b;
        }
        void ProgressionPanel(string title,string subtitle,UnityEngine.Events.UnityAction back=null)
        {
            progressionPanelEpoch++;
            if(birdProgressionScreen==null)
            {
                birdProgressionScreen=CreateScreen(interfaceContentRoot,"Core and bird progression");
                var veil=CreateFullPanel(birdProgressionScreen.transform,"Progression dim",new Color(.004f,.012f,.026f,.94f));veil.GetComponent<Image>().raycastTarget=true;
                var body=CreatePanel(birdProgressionScreen.transform,"Progression glass",new Vector2(0,-20),new Vector2(900,1560),Hex("#0a1c2b"));
                body.GetComponent<Image>().sprite=roundedPanelSprite;birdProgressionBody=body;
            }
            if(holographicTree!=null){holographicTree.CancelPress();holographicTree.CancelFocus();holographicTree.MotionPaused=true;}
            foreach(Transform child in birdProgressionBody){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            coreProgressionPreview=null;birdProgressionScreen.SetActive(true);birdProgressionScreen.transform.SetAsLastSibling();progressionCommitting=false;
            ProgressionText(birdProgressionBody,title,640,39,90,Hex("#f0fbff"));ProgressionText(birdProgressionBody,subtitle,560,27,70,Hex("#8fbdcc"));
            birdProgressionNotice=ProgressionText(birdProgressionBody,"",-565,24,60,Hex("#a4c3d2"));
            if(back==null)back=CloseBirdProgression;
            ProgressionButton(birdProgressionBody,"BACK",new Vector2(0,-690),new Vector2(720,100),back);
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
            ProgressionPanel("SKYPULSE CORE",SkyPulseBirdProgression.CoreNames[rank-1]+" · LEVEL "+rank+" / 5");
            var raw=CreateImage(birdProgressionBody,"Core preview footprint",new Vector2(0,250),new Vector2(650,400),Color.clear).rectTransform;
            var visual=new GameObject("Next Core preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage),typeof(SkyPulseTechTreeView));visual.transform.SetParent(raw,false);
            var image=visual.GetComponent<RawImage>();image.rectTransform.anchorMin=Vector2.zero;image.rectTransform.anchorMax=Vector2.one;image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero;
            var content=new GameObject("Preview origin",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(raw,false);content.anchoredPosition=Vector2.zero;
            coreProgressionPreview=visual.GetComponent<SkyPulseTechTreeView>();coreProgressionPreview.CoreRank=next;coreProgressionPreview.ReducedMotion=reduceMotionEnabled;
            coreProgressionPreview.Build(image,content,raw,new List<HolographicBranch>(),new List<HolographicRoot>{new HolographicRoot{At=new Vector2(0,-200),Colour=SkyPulseBirdProgression.CoreColours[next-1],Powered=true,Size=57}});
            ProgressionText(birdProgressionBody,rank<5?"NEXT · "+SkyPulseBirdProgression.CoreNames[next-1]:"NOVA CORE · FULLY EVOLVED",-10,34,60,SkyPulseBirdProgression.CoreColours[next-1]);
            ProgressionText(birdProgressionBody,"Training cap · level "+Math.Min(5,next+1)+"\nLineage bays · "+next*4+"\n"+(next>=5?"Inherit up to grade 3 traits":next>=3?"Inherit up to grade 2 traits":"Breeding unlocks at Core level 2"),-150,31,210);
            ProgressionText(birdProgressionBody,"Each Core has its own colour and halo motion.\nYour existing Tech Tree upgrades remain available.",-300,26,90,Hex("#91afc0"));
            var button=ProgressionButton(birdProgressionBody,rank<5?"UPGRADE · "+birdProgression.CorePrice+" ✦":"MAXIMUM CORE",new Vector2(0,-455),new Vector2(720,110),()=>CommitBirdProgression(()=>birdProgression.UpgradeCore(revision)));
            button.interactable=rank<5&&crystals>=birdProgression.CorePrice;birdProgressionNotice.text="BALANCE · "+crystals+" ✦";
        }
        void OpenBirdTraining(Skin skin)
        {
            RecoverWalletTransaction();var bird=birdProgression.Genome(skin.Id);int revision=birdProgression.Data.revision;
            ProgressionPanel(skin.Name,"BIRD LEVEL "+bird.level+" / "+birdProgression.TrainingCap+" · CORE "+birdProgression.Data.core);
            var portrait=CreateImage(birdProgressionBody,"Training portrait",new Vector2(0,365),new Vector2(430,230),Color.white);portrait.sprite=LoadPresentationPose(skin.UnlockPath)??LoadSprite(skin.ArtPath);portrait.preserveAspect=true;portrait.raycastTarget=false;
            ProgressionText(birdProgressionBody,"CURRENT\n"+SkyPulseBirdProgression.TraitText(bird),165,29,130,Hex("#a5c4d1"));
            bool can=IsSkinOwned(skin)&&bird.level<birdProgression.TrainingCap;
            var trained=JsonUtility.FromJson<BirdGenome>(JsonUtility.ToJson(bird));if(can)trained.level++;
            ProgressionText(birdProgressionBody,(can?"NEXT LEVEL\n":"ACTIVE TRAITS\n")+SkyPulseBirdProgression.TraitText(trained),-20,31,150,skin.Accent);
            ProgressionText(birdProgressionBody,"Traits reward Endless Route play. Flight lift, speed, gate score and collision remain the same for every bird.",-210,28,150);
            var button=ProgressionButton(birdProgressionBody,can?"TRAIN · "+SkyPulseBirdProgression.TrainingPrices[bird.level-1]+" ✦":bird.level>=5?"FULLY TRAINED":"CORE UPGRADE REQUIRED",new Vector2(0,-370),new Vector2(740,110),()=>CommitBirdProgression(()=>birdProgression.Train(skin.Id,revision,OwnBirdId)));
            button.interactable=can&&crystals>=SkyPulseBirdProgression.TrainingPrices[bird.level-1];
            if(!can&&bird.level<5)ProgressionButton(birdProgressionBody,"VIEW CORE",new Vector2(0,-530),new Vector2(720,90),OpenCoreProgression);
            else birdProgressionNotice.text="BALANCE · "+crystals+" ✦";
        }
        List<Skin> OwnedBirdRoster(){var list=new List<Skin>();foreach(var b in BirdRoster)if(IsSkinOwned(b))list.Add(b);return list;}
        void OpenBreeding(Skin first)
        {
            RecoverWalletTransaction();var owned=OwnedBirdRoster();
            if(first!=null)breedingParentA=first.Id;
            if(!OwnBirdId(breedingParentA))breedingParentA=owned[0].Id;
            if(!OwnBirdId(breedingParentB)||breedingParentB==breedingParentA){breedingParentB=null;foreach(var b in owned)if(b.Id!=breedingParentA){breedingParentB=b.Id;break;}}
            ProgressionPanel("LINEAGE LAB","Two owned parents · preview the exact inheritance");
            ParentPortrait(true,breedingParentA);ParentPortrait(false,breedingParentB);
            var preview=birdProgression.Preview(breedingParentA,breedingParentB);int revision=birdProgression.Data.revision;
            string firstId=breedingParentA,secondId=breedingParentB;
            string block=birdProgression.BreedingBlock(breedingParentA,breedingParentB,OwnBirdId);
            if(preview!=null)
            {
                var skin=BredSkin(preview);var image=CreateImage(birdProgressionBody,"Offspring appearance",new Vector2(0,70),new Vector2(310,180),Color.Lerp(Color.white,skin.Accent,.15f));image.sprite=LoadPresentationPose(skin.UnlockPath)??LoadSprite(skin.ArtPath);image.preserveAspect=true;image.raycastTarget=false;
                ProgressionText(birdProgressionBody,"OFFSPRING · GENERATION "+preview.generation,-65,28,60,skin.Accent);
                ProgressionText(birdProgressionBody,SkyPulseBirdProgression.TraitText(preview),-150,30,110);
                ProgressionText(birdProgressionBody,"Inherits the first parent's silhouette and primary trait, plus a complementary trait from the second parent. Both parents stay in your Hangar.",-290,26,140);
            }
            else ProgressionText(birdProgressionBody,"Unlock a second bird to create your first lineage.",-90,32,160);
            var create=ProgressionButton(birdProgressionBody,preview!=null?"BREED · "+SkyPulseBirdProgression.BreedingPrice(preview)+" ✦":"CHOOSE TWO PARENTS",new Vector2(0,-445),new Vector2(740,110),()=>CommitBirdProgression(()=>birdProgression.Breed(firstId,secondId,revision,OwnBirdId),preview?.id));
            create.interactable=block==""&&crystals>=SkyPulseBirdProgression.BreedingPrice(preview);
            birdProgressionNotice.text=block!=""?block:"BALANCE · "+crystals+" ✦ · NO RANDOM OUTCOME";
        }
        void ParentPortrait(bool first,string id)
        {
            float x=first?-210:210;var skin=FindBird(id);
            ProgressionButton(birdProgressionBody,(first?"PARENT A":"PARENT B")+" · CHANGE",new Vector2(x,445),new Vector2(390,100),()=>OpenParentPicker(first));
            var image=CreateImage(birdProgressionBody,"Parent portrait",new Vector2(x,280),new Vector2(275,170),Color.white);image.sprite=skin==null?null:LoadPresentationPose(skin.UnlockPath)??LoadSprite(skin.ArtPath);image.preserveAspect=true;image.enabled=skin!=null;image.raycastTarget=false;
            var label=ProgressionText(birdProgressionBody,skin?.Name??"NOT SELECTED",185,24,70);label.rectTransform.anchoredPosition=new Vector2(x,185);label.rectTransform.sizeDelta=new Vector2(380,70);
        }
        void OpenParentPicker(bool first)
        {
            ProgressionPanel(first?"CHOOSE PARENT A":"CHOOSE PARENT B","Owned birds · their upgrades contribute to inherited grades",()=>OpenBreeding(null));
            var viewport=CreateImage(birdProgressionBody,"Parent list",new Vector2(0,-5),new Vector2(820,1000),new Color(.01f,.02f,.04f,.4f)).rectTransform;
            viewport.gameObject.AddComponent<RectMask2D>();var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.decelerationRate=.08f;
            var content=new GameObject("Owned parent choices",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport,false);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);scroll.content=content;
            var birds=OwnedBirdRoster();content.sizeDelta=new Vector2(0,Mathf.Max(1000,birds.Count*165));
            for(int i=0;i<birds.Count;i++)
            {
                var selected=birds[i];var button=ProgressionButton(content,selected.Name+" · LV "+BirdLevel(selected),new Vector2(0,-82-i*165),new Vector2(770,125),()=>{if(first)breedingParentA=selected.Id;else breedingParentB=selected.Id;OpenBreeding(null);});
                button.GetComponent<RectTransform>().anchorMin=button.GetComponent<RectTransform>().anchorMax=new Vector2(.5f,1);
                button.interactable=selected.Id!=(first?breedingParentB:breedingParentA);
            }
        }
    }
}
