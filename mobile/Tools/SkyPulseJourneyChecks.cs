using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;
public static class SkyPulseJourneyChecks
{
    const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    static int count;static void Check(bool ok,string note){if(!ok)throw new Exception(note);count++;Debug.Log("JOURNEY_CHECK: "+note);}
    sealed class Store:IMissionStore
    {
        public readonly Dictionary<string,string> Text=new Dictionary<string,string>();public readonly Dictionary<string,int> Ints=new Dictionary<string,int>();public int Writes;
        public string Get(string k,string d)=>Text.TryGetValue(k,out var v)?v:d;public int GetInt(string k,int d)=>Ints.TryGetValue(k,out var v)?v:d;
        public void Set(string k,string v){Text[k]=v;Writes++;}public void SetInt(string k,int v){Ints[k]=v;}public void Flush(){}
    }
    public static void Pure()
    {
        DateTime time=new DateTime(2026,11,13,12,0,0,DateTimeKind.Utc);var store=new Store();store.SetInt(SkyPulseDailyMissions.WalletKey,321);var journey=new SkyPulsePlayerJourney(store,()=>time);
        Check(journey.BirdRank("neon_finch")==0,"New mastery starts without granting a cosmetic");
        foreach(int score in new[]{5,15,30}){journey.RecordFlight("neon_finch",score);Check(journey.BirdRank("neon_finch")== (score==5?1:score==15?2:3),"Endless threshold unlocks exactly its earned cradle: "+score);}
        int writes=store.Writes;journey.RecordFlight("neon_finch",5);journey.RecordFlight("neon_finch",30);Check(store.Writes==writes,"Repeated/lesser flight results do not write or multiply mastery");
        foreach(int gates in new[]{2,4,5}){journey.RecordRelay("voltcrest",gates);Check(journey.BirdRank("voltcrest")== (gates==2?1:gates==4?2:3),"Relay success offers the same cosmetic tiers: "+gates);}
        Check(journey.BirdRank("newbird01")==0,"Mastery belongs to the actual bird, not a shared appearance");
        Check(new SkyPulsePlayerJourney(store,()=>time).FlightBest("neon_finch")==30,"Bird mastery survives reload");
        for(int i=0;i<4;i++)
        {
            var p=SkyPulseBlackoutCasebook.All[i];var solve=SkyPulseMissionChecks.Solve(p);Check(solve!=null,"Exhaustive solver finds the authored daily case route: "+p.Name);
            var s=new BlackoutSession(p);Check(s.State==MissionSessionState.Playing,"Daily case starts unsolved: "+p.Name);foreach(int tile in solve)Check(s.Rotate(tile),"Authored solution move accepted: "+p.Name);
            Check(s.State==MissionSessionState.Succeeded&&s.Medal==3&&s.Trace==p.Par,"Daily case has a reachable exact Gold target: "+p.Name);
        }
        int expected=0;
        for(int day=0;day<4;day++)
        {
            time=new DateTime(2026,11,13,12,0,0,DateTimeKind.Utc).AddDays(day);journey.EnsureDay();long stamp=journey.Data.day;int index=SkyPulseBlackoutCasebook.Index(stamp);var s=journey.StartCase(true);var solve=SkyPulseMissionChecks.Solve(s.Puzzle);s.Rotate(solve[0]);journey.SaveCase(stamp,s);
            var reload=new SkyPulsePlayerJourney(store,()=>time);var resumed=reload.StartCase();Check(resumed.Trace==s.Trace&&resumed.Rotations.SequenceEqual(s.Rotations),"Daily case attempt resumes with exact trace and rotations: "+index);
            s=journey.StartCase(true);foreach(int tile in solve)s.Rotate(tile);journey.CompleteCase(stamp,s);expected|=1<<index;Check(journey.Data.caseGoldMask==expected,"Each distinct Gold case grants one persistent archive stamp: "+index);writes=store.Writes;journey.CompleteCase(stamp,s);Check(store.Writes==writes,"Duplicate completion does not grant or save a second stamp");
        }
        Check(journey.Data.caseGoldMask==15&&journey.DisplayRank("untrained_bird")==4,"Four distinct Gold case designs unlock the shared Archivist cradle");
        long held=journey.Data.day;time=time.AddDays(-5);journey.EnsureDay();Check(journey.Data.day==held,"Clock rollback cannot reopen an old daily period");
        Check(store.GetInt(SkyPulseDailyMissions.WalletKey,0)==321,"Cosmetic mastery and optional cases never mutate the crystal wallet");
        for(int i=0;i<10;i++)journey.DisplayRank("neon_finch");long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)journey.DisplayRank("neon_finch");Check(GC.GetAllocatedBytesForCurrentThread()==bytes,"Warmed per-frame Hangar mastery lookups allocate no managed bytes");
    }
    public static IEnumerator Live(SkyPulseNativeGame g,Camera camera,RenderTexture target)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        Call(g,"InitializeMissionServices");SkyPulseNativeGame.ReviewOriginalBirdArtwork=false;SkyPulseBirdRosterPreview.Select("",false);var previewDaily=(SkyPulseDailyMissions)Get(g,"dailyMissions");for(int i=0;i<4;i++){previewDaily.Data.progress[i]=0;previewDaily.Data.claimed[i]=false;}var journey=(SkyPulsePlayerJourney)Get(g,"playerJourney");
        foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay,SkyPulseMode.BlackoutProtocol})
        {
            Call(g,"SetHomeProtocol",mode);Call(g,"ResetToMenu");Canvas.ForceUpdateCanvases();var button=(Button)Get(g,"journeyButton");Check(button!=null&&button.gameObject.activeInHierarchy,"Shared next-goal card exists in "+mode);
            foreach(string field in new[]{"journeyTitle","journeyDetail"}){var text=(Text)Get(g,field);Check(text.preferredWidth<=text.rectTransform.rect.width+1&&text.preferredHeight<=text.rectTransform.rect.height+1,"Goal copy fits its actual label: "+mode+" / "+field);}
            Call(g,"UpdateProtocolPresentation",.1f);Call(g,"UpdateCinematicPresentation",.1f);Call(g,"UpdateSolidBirds",.1f);Check(g.RuntimeSolidBirdsActive,"Home review uses native 3D birds, not the retired artwork review override");foreach(var stage in UnityEngine.Object.FindObjectsByType<SkyPulseDepthStage>())stage.Render();Canvas.ForceUpdateCanvases();camera.Render();Save(target,"home-goal-"+mode+".png");
        }
        var campaign=(BlackoutCampaignProgress)Get(g,"blackoutCampaign");string story=JsonUtility.ToJson(campaign.Data);Call(g,"LaunchDailyBlackout");var session=(BlackoutSession)Get(g,"blackoutSession");Check((bool)Get(g,"blackoutDailyActive")&&session.Puzzle==SkyPulseBlackoutCasebook.All[SkyPulseBlackoutCasebook.Index(journey.Data.day)],"Daily launch uses the day's authored case and real optical renderer");
        ((SkyPulseHeistBoard)Get(g,"blackoutBoard")).Render();((SkyPulseRestorationDisplay)Get(g,"blackoutRestoration")).Render();camera.Render();Save(target,"daily-case.png");
        foreach(int move in SkyPulseMissionChecks.Solve(session.Puzzle))Call(g,"TouchBlackoutNode",move);
        Check((bool)Get(g,"missionFinalized")&&((GameObject)Get(g,"missionResultScreen")).activeSelf,"Daily final solve opens the proper result without advancing the story");
        Check(JsonUtility.ToJson(campaign.Data)==story,"Optional case cannot mark a story restoration complete or overwrite its saved attempt");
        int mask=journey.Data.caseGoldMask;Call(g,"FinalizeMission");Check(journey.Data.caseGoldMask==mask,"Repeated daily finalization is idempotent");camera.Render();Save(target,"daily-result.png");
        Call(g,"ResetToMenu");Call(g,"SetHomeProtocol",SkyPulseMode.EndlessRoute);
        var daily=(SkyPulseDailyMissions)Get(g,"dailyMissions");daily.Data.progress[0]=SkyPulseDailyMissions.Targets[0];Call(g,"ApplyJourneyHome");int before=PlayerPrefs.GetInt(SkyPulseDailyMissions.WalletKey,0);Call(g,"ClaimJourneyDaily",0);Call(g,"ClaimJourneyDaily",0);Check(PlayerPrefs.GetInt(SkyPulseDailyMissions.WalletKey,0)==before+5,"Next-goal card claims the existing daily reward exactly once");
        DateTime dailyTime=new DateTime(2026,11,13,12,0,0,DateTimeKind.Utc);var rollover=new SkyPulsePlayerJourney(new Store(),()=>dailyTime);Set(g,"playerJourney",rollover);Call(g,"LaunchDailyBlackout");dailyTime=dailyTime.AddDays(1);Call(g,"RestartBlackoutVault");
        Check((long)Get(g,"blackoutDailyDay")==rollover.Data.day&&((BlackoutSession)Get(g,"blackoutSession")).Puzzle==SkyPulseBlackoutCasebook.All[SkyPulseBlackoutCasebook.Index(rollover.Data.day)],"Retry across UTC midnight binds and saves the correct new daily case");
        var nextDay=(BlackoutSession)Get(g,"blackoutSession");foreach(int move in SkyPulseMissionChecks.Solve(nextDay.Puzzle))Call(g,"TouchBlackoutNode",move);Check(rollover.Data.caseGoldMask==(1<<SkyPulseBlackoutCasebook.Index(rollover.Data.day)),"A post-midnight retry earns the correct authored-case stamp");Set(g,"playerJourney",journey);Call(g,"ResetToMenu");
        // Validate genuine display material changes without changing the model or rig.
        var root=new GameObject("Mastery cradle QA",typeof(RectTransform)).GetComponent<RectTransform>();
        using(var studio=new SkyPulseSolidBirdStudio(root,"SkyPulse/Birds3D/neon_finch",new Vector2(560,360),Vector2.zero,Color.cyan,true,true))
        {
            float stroke=studio.Rig.flapDegrees;var guide=(Material)Get(studio,"guide");var hues=new HashSet<Color>();for(int tier=0;tier<=4;tier++){studio.SetMastery(tier);Check(hues.Add(guide.GetColor("_GlowColor")),"Each cradle tier has a distinct actual material: "+tier);Check(studio.Rig.flapDegrees==stroke,"Mastery never changes the accepted wing stroke");studio.Render(1,false);Save(studio.StageCamera.targetTexture,"cradle-tier-"+tier+".png");}
        }
        UnityEngine.Object.Destroy(root.gameObject);Call(g,"ResetToMenu");File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"JOURNEY_VALIDATION.json"),"{\"result\":\"PASS\",\"checks\":"+count+",\"deviceTested\":false}");Debug.Log("SKYPULSE_JOURNEY_PASS");yield return null;
    }
    static void Save(RenderTexture target,string name){var previous=RenderTexture.active;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),name),pixels.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixels);RenderTexture.active=previous;}
}
