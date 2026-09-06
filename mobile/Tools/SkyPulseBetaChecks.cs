using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

// Run only in the isolated QA project: these checks intentionally exercise saves.
public static class SkyPulseBetaChecks
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static object Get(object o,string n) => o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v) => o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a) => o.GetType().GetMethod(n,F).Invoke(o,a);
    static object Static(string n,params object[] a) => typeof(SkyPulseNativeGame).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,a);
    static Array Catalog(string n) => (Array)typeof(SkyPulseNativeGame).GetField(n,BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
    static void Check(bool c,string m) { if(!c) throw new Exception(m); }
    public static void Run(SkyPulseNativeGame game)
    {
        game.enabled=false;
        foreach(var clip in new[]{"flapSound","scoreSound","crashSound","crystalSound","unlockSound"})
            Check(Get(game,clip)!=null,"Missing audio asset: "+clip);
        float speed=0f, gap=1f;
        for(int score=0;score<=300;score++)
        {
            float s=(float)Static("RouteSpeedFraction",score), g=(float)Static("RouteGapFraction",score);
            Check(s>=speed && s<=.48001f && g<=gap && g>=.23999f,"Non-monotonic or uncapped route at "+score);
            if(score>2 && score<=15) Check(s>speed && g<gap,"Early route has a plateau at "+score);
            if(score==15 || score==30 || score==40 || score==45 || score==60) Check(s-speed<.011f && gap-g<.006f,"World transition difficulty cliff");
            speed=s;gap=g;
        }
        foreach(var milestone in new[]{40,60})
            Check((float)Static("RouteSpeedFraction",milestone)>(float)Static("RouteSpeedFraction",milestone-1),"Missing requested speed increase at "+milestone);
        Check((float)Static("RouteGapFraction",45)<.25f,"Late route openings were not narrowed");
        Check(Mathf.Abs((float)Call(game,"ActiveGravity")+34.8f)<.001f,"Gravity changed");
        Check(Mathf.Abs((float)Call(game,"ActiveFlapVelocity")-11.4f)<.001f,"Flap handling changed");
        var pipes=(IList)Get(game,"pipePool");
        for(int seed=0;seed<20;seed++)
        {
            UnityEngine.Random.InitState(seed); Call(game,"StartFlight");
            for(int index=0;index<65;index++)
            {
                var pair=pipes[index%pipes.Count];
                Set(game,"nextGateRouteScore",index); Set(game,"routeWorldIndex",(int)Static("WorldIndexForScore",index));
                Set(game,"score",0); // Deliberately far behind the gate being generated.
                Call(game,"ConfigurePipe",pair,12f+index*6f);
                Check(Mathf.Abs((float)Get(pair,"GapHeight")-18f*(float)Static("RouteGapFraction",index))<.001f,"Spawn-ahead gate uses current score");
                var y=(float)Get(pair,"GapCenter");
                Check(y>=-2.551f && y<=3.151f,"Gate exceeds flight corridor");
                if(index<3) Check(Mathf.Abs(y)<=(index==0?.651f:1.301f),"Opening gate is not welcoming");
                if(index>0) Check(Mathf.Abs(y-(float)Get(pipes[(index-1)%pipes.Count],"GapCenter"))<=(float)Static("RouteMaximumCenterStep",index)+.001f,"Unbounded gate step");
                for(int phase=0;phase<8;phase++)
                {
                    Set(game,"ambientTime",phase*.2f);Call(game,"UpdateRouteGateMotion",pair,0f);
                    var half=(float)Get(pair,"GapHeight")*.5f; var cy=(float)Get(pair,"GapCenter");
                    Check(cy-half>=-8.45f+1.56f-.001f && cy+half<=9f-1.56f+.001f,"Drifting gate exceeds visible bounds");
                }
                Set(pair,"GapCenter",Get(pair,"BaseGapCenter"));
            }
        }
        Call(game,"StartFlight"); Call(game,"OnApplicationPause",true);
        Check(Get(game,"state").ToString()=="Paused","Backgrounding does not pause");
        Call(game,"OnApplicationPause",false);
        Check(Get(game,"state").ToString()=="Paused","Resume starts flight without player input");
        Call(game,"ResumeFlight");
        Check(Get(game,"state").ToString()=="Playing","Explicit resume fails");
        Call(game,"BankCollectedCrystals",7);
        int bank=(int)Get(game,"crystals");Call(game,"SaveProgress");Set(game,"crystals",0);Call(game,"LoadProgress");
        Check((int)Get(game,"crystals")==bank,"Collected crystals do not persist");
        var upgrades=(IDictionary)Get(game,"upgradeLevels");upgrades.Clear();
        upgrades["salvage_codec"]=3;upgrades["apex_matrix"]=3;
        Set(game,"runCrystalsCollected",10);Set(game,"score",10);Set(game,"resultCrystalBonusApplied",false);Set(game,"newBest",false);
        var bonus=(int)Call(game,"ApplyResultTechBonuses");int after=(int)Get(game,"crystals");
        Check(bonus==10,"Result bonuses compound incorrectly");Call(game,"ApplyResultTechBonuses");
        Check((int)Get(game,"crystals")==after,"Result bonus can be claimed twice");
        upgrades.Clear(); ((HashSet<string>)Get(game,"ownedUpgradeIds")).Clear(); var first=Catalog("Upgrades").GetValue(0);
        Set(game,"crystals",0);Call(game,"SelectUpgrade",first);Call(game,"ConfirmPurchase");
        Check((int)Call(game,"GetUpgradeLevel",Get(first,"Id"))==0,"Insufficient balance buys upgrade");
        Set(game,"crystals",100000);Call(game,"SelectUpgrade",first);Call(game,"ConfirmPurchase");
        int paid=(int)Get(game,"crystals");Call(game,"ConfirmPurchase");
        Check((int)Get(game,"crystals")==paid && (int)Call(game,"GetUpgradeLevel",Get(first,"Id"))==1,"Duplicate purchase charged twice");
        foreach(var skin in Catalog("Skins"))
        {
            Set(game,"equippedSkin",skin);Call(game,"SetBirdArtwork");
            Check(((Array)Get(game,"flapFrameBirdSprites")).Length==6,"Bird animation is incomplete");
            Check(Get(game,"hitBirdSprite")!=null,"Bird impact pose missing");
        }
        Call(game,"ResetToMenu");
        var privacy=(GameObject)Get(game,"privacyScreen");
        foreach(var body in privacy.GetComponentsInChildren<Text>(true))
            if(body.text.StartsWith("SkyPulse plays offline"))
                Check(body.horizontalOverflow==HorizontalWrapMode.Wrap && body.preferredHeight<=body.rectTransform.rect.height,"Privacy notice is clipped");
        var content=(RectTransform)Get(game,"interfaceContentRoot");var safe=(RectTransform)Get(game,"safeAreaRoot");
        foreach(var dimensions in new[]{new Vector2(1080,1740),new Vector2(1080,2200),new Vector2(1920,1000)})
        {
            safe.anchorMin=safe.anchorMax=new Vector2(.5f,.5f);safe.sizeDelta=dimensions;Call(game,"FitInterfaceToSafeArea");
            Check(content.rect.width*content.localScale.x<=dimensions.x+.01f && content.rect.height*content.localScale.y<=dimensions.y+.01f,"UI exceeds safe area");
        }
        Call(game,"ShowScoreBurst",1,true);var text=(Text)Get(game,"scoreBurstText");
        Check(text.preferredWidth<text.rectTransform.rect.width,"Perfect score message wraps");
        Debug.Log("SKYPULSE_BETA_CHECKS_PASS: route bounds over 1300 gates, smooth milestones, handling, pause/resume, persistence, one-time rewards, purchase guards, 15 bird poses, safe-area fit, feedback width.");
        game.enabled=true;
    }
}
