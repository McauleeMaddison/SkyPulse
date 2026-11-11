// Disposable Editor QA. Uses actual callbacks; never run against owner saves.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseFinalUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    static void Check(bool ok,string copy){if(!ok)throw new Exception(copy);Debug.Log("FINAL_UI_CHECK: "+copy);}
    static Button Action(GameObject root,string copy)=>root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>()?.text==copy);
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable UI QA required");
        game.enabled=false;
        var oldSkin=Get(game,"equippedSkin");var oldTrail=Get(game,"equippedTrail");var oldMode=(SkyPulseMode)Get(game,"homeProtocol");
        bool oldReduced=(bool)Get(game,"reduceMotionEnabled");int wallet=(int)Get(game,"crystals");
        var owned=(HashSet<string>)Get(game,"ownedSkinIds");var ownedBefore=new HashSet<string>(owned);
        var progression=(SkyPulseBirdProgression)Get(game,"birdProgression");int oldCore=progression.Data.core,oldRevision=progression.Data.revision;var oldGenomes=progression.Data.birds.ToArray();
        try
        {
            Call(game,"ClosePurchaseModal");Call(game,"CloseBirdProgression");
            progression.Data.core=1;progression.Data.birds.Clear();
            foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay,SkyPulseMode.BlackoutProtocol})
            {
                Call(game,"SetHomeProtocol",mode);Call(game,"ResetToMenu");Canvas.ForceUpdateCanvases();
                var launch=(Button)Get(game,"homeLaunchButton");var label=launch.GetComponentInChildren<Text>();
                Check(label.fontSize==56&&((RectTransform)launch.transform).sizeDelta.y==152,"Primary launch retains readable type and height in "+mode);
                Check(label.preferredWidth<=label.rectTransform.rect.width+1&&label.preferredHeight<=label.rectTransform.rect.height+1,"Primary launch copy fits in "+mode);
            }
            Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);owned.Add("neon_finch");owned.Add("chrome_raven");
            Set(game,"equippedSkin",Call(game,"FindBird","neon_finch"));Call(game,"ApplyEquippedVisuals");Call(game,"OpenHangar");
            Set(game,"reduceMotionEnabled",false);Set(game,"hangarInformationFadeTime",0f);Set(game,"hangarPageIndex",1);Call(game,"RebuildCustomizeGrid");
            yield return null;Canvas.ForceUpdateCanvases();
            var hangar=(GameObject)Get(game,"customizeScreen");var equip=Action(hangar,"EQUIP BIRD");
            Check(((CanvasGroup)Get(game,"hangarInformationCanvas")).alpha>=.72f,"Arriving Hangar identity and price stay readable before the first reveal tick");
            var hit=equip.GetComponent<Image>();var rect=(RectTransform)equip.transform;
            Check(rect.rect.height-hit.raycastPadding.y-hit.raycastPadding.w>=176,"Equip action receives the existing comfortable touch envelope");
            foreach(string flag in new[]{"hangarDragging","hangarSwipeAnimating"})
            {
                Set(game,flag,true);equip.onClick.Invoke();Action(hangar,"TRAIN BIRD").onClick.Invoke();Action(hangar,"BREED").onClick.Invoke();
                Check((string)Get(Get(game,"equippedSkin"),"Id")=="neon_finch"&&!(Get(game,"birdProgressionScreen") is GameObject panel&&panel.activeInHierarchy),"Carousel "+flag+" rejects equip, training and breeding actions");Set(game,flag,false);
            }
            Set(game,"hangarPageIndex",0);Call(game,"RebuildCustomizeGrid");equip.onClick.Invoke();
            Check((string)Get(Get(game,"equippedSkin"),"Id")=="neon_finch","Retired outgoing-page callback cannot equip its bird after a rebuild");
            Set(game,"hangarPageIndex",1);Call(game,"RebuildCustomizeGrid");yield return null;Action(hangar,"EQUIP BIRD").onClick.Invoke();
            Check((string)Get(Get(game,"equippedSkin"),"Id")=="chrome_raven"&&(int)Get(game,"crystals")==wallet,"Settled owned bird equips normally without a debit");
            progression.Data.core=1;progression.Data.birds.Clear();progression.Data.birds.Add(new BirdGenome{id="neon_finch",appearance="neon_finch",level=2,primary=0,secondary=1});
            Set(game,"hangarPageIndex",0);Call(game,"RebuildCustomizeGrid");yield return null;
            Action(hangar,"EVOLVE CORE").onClick.Invoke();
            var body=(RectTransform)Get(game,"birdProgressionBody");
            Check(body.GetComponentsInChildren<Text>().Any(t=>t.text=="SKYPULSE CORE"),"Core-limited training action opens the actual Core evolution panel directly");
            Check((int)Get(game,"crystals")==wallet&&progression.Data.revision==oldRevision,"Reading Core benefits never spends or commits progression");
        }
        finally
        {
            Set(game,"hangarDragging",false);Set(game,"hangarSwipeAnimating",false);Set(game,"reduceMotionEnabled",oldReduced);
            progression.Data.core=oldCore;progression.Data.birds.Clear();progression.Data.birds.AddRange(oldGenomes);
            owned.Clear();foreach(var id in ownedBefore)owned.Add(id);Set(game,"equippedSkin",oldSkin);Set(game,"equippedTrail",oldTrail);Call(game,"CloseBirdProgression");Call(game,"ClosePurchaseModal");Call(game,"ApplyEquippedVisuals");Call(game,"SetHomeProtocol",oldMode);Call(game,"ResetToMenu");
        }
        Debug.Log("SKYPULSE_FINAL_UI_PASS");
    }
}
