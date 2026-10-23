// Editor QA only. Never copy this navigation/save fixture into the player.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseCopyChecks
{
    [Serializable] sealed class Rules {public string pattern;}
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Regex blocked;
    static int labels;static readonly HashSet<string> screens=new HashSet<string>();
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    public static void AssertVisible(SkyPulseNativeGame game,string context)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Isolated QA required");
        if(blocked==null)
        {
            string path=Path.Combine(Application.dataPath,"Editor/player-copy-rules.json");
            if(!File.Exists(path))path=Path.Combine(Application.dataPath,"../Tools/player-copy-rules.json");
            blocked=new Regex(JsonUtility.FromJson<Rules>(File.ReadAllText(path)).pattern,RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
            if(!blocked.IsMatch("COMING SOON")||!blocked.IsMatch("New Text")||blocked.IsMatch("Preview your offspring"))throw new Exception("Copy guard controls failed");
        }
        var root=(GameObject)Get(game,"uiRoot");
        foreach(var text in root.GetComponentsInChildren<Text>())
        {
            labels++;if(blocked.IsMatch(text.text))throw new Exception("Unfinished copy in "+context+": "+text.text);
        }
        foreach(var text in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
        {
            labels++;if(blocked.IsMatch(text.text))throw new Exception("Unfinished world label in "+context+": "+text.text);
        }
        screens.Add(context);
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Isolated QA required");
        foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay,SkyPulseMode.BlackoutProtocol})
        {
            Call(game,"SetHomeProtocol",mode);Call(game,"ResetToMenu");AssertVisible(game,mode+" Home");
            Call(game,"OpenModeSelection");AssertVisible(game,mode+" mode list");Call(game,"ResetToMenu");
            Call(game,"OpenModeGuide");
            for(int page=0;page<(mode==SkyPulseMode.EndlessRoute?5:2);page++){Call(game,"ShowModeGuidePage",page);yield return null;AssertVisible(game,mode+" guide "+page);}
            Call(game,"ResetToMenu");
            if(mode!=SkyPulseMode.EndlessRoute){Call(game,"OpenMissionSelection",mode);AssertVisible(game,mode+" mission selection");}
        }
        Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");
        var privacy=(GameObject)Get(game,"privacyScreen");privacy.SetActive(true);AssertVisible(game,"Privacy");privacy.SetActive(false);
        Call(game,"OpenMissionControl");AssertVisible(game,"Mission Control");Call(game,"ResetToMenu");
        Call(game,"OpenWorldGuide");var worlds=(GameObject)Get(game,"flightGuideScreen");
        for(int page=0;page<4;page++)
        {
            AssertVisible(game,"World map "+page);
            foreach(var button in worlds.GetComponentsInChildren<Button>())if(button.GetComponentInChildren<Text>().text=="NEXT"){button.onClick.Invoke();break;}
        }
        // Match the real guide's Back action before navigating by fixture calls.
        // ResetToMenu does not own this independently opened overlay.
        worlds.SetActive(false);
        Call(game,"ResetToMenu");Call(game,"OpenHangar");
        for(int bird=0;bird<15;bird++){AssertVisible(game,"Founder bird "+bird);Call(game,"ShowNextHangarBird");}
        Call(game,"OpenUpgrades");AssertVisible(game,"Tech Tree");Call(game,"OpenCoreProgression");AssertVisible(game,"Core progression");Call(game,"CloseBirdProgression");
        var skin=Get(game,"equippedSkin");Call(game,"OpenBirdTraining",skin);AssertVisible(game,"Bird training");Call(game,"CloseBirdProgression");
        Call(game,"OpenBreeding",skin);AssertVisible(game,"Breeding");Call(game,"OpenParentPicker",true);AssertVisible(game,"Parent selection");Call(game,"CloseBirdProgression");Call(game,"ResetToMenu");
        Call(game,"StartFlight");AssertVisible(game,"Endless flight");Call(game,"PauseFlight");AssertVisible(game,"Pause and preferences");Call(game,"ResumeFlight");Call(game,"EndFlight");AssertVisible(game,"Endless result");Call(game,"ResetToMenu");
        Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);AssertVisible(game,"Relay flight");Call(game,"FinishRelayFailure","GROUND CONTACT");AssertVisible(game,"Relay result");Call(game,"ResetToMenu");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);AssertVisible(game,"Signal Heist");Call(game,"ToggleBlackoutPreview");Call(game,"PreviewBlackoutNode",6);AssertVisible(game,"Signal Heist planning");Call(game,"CancelBlackoutPreview");
        var session=(BlackoutSession)Get(game,"blackoutSession");while(session.State==MissionSessionState.Playing)Call(game,"TouchBlackoutNode",6);AssertVisible(game,"Signal Heist failure");Call(game,"ResetToMenu");
        string folder=Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,"copy-ui-report.json"),"{\"screenStatesChecked\":"+screens.Count+",\"textValuesChecked\":"+labels+",\"placeholderFindings\":0}");
        Debug.Log("SKYPULSE_COPY_UI_PASS: "+screens.Count+" states / "+labels+" text values");
    }
}
