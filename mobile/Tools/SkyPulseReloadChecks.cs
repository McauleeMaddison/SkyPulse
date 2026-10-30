// Disposable Editor QA only. Forces a real compile during a live session.
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;
using SkyPulse.Mobile;
using SkyPulse.Mobile.Editor;

[InitializeOnLoad]
public static class SkyPulseReloadChecks
{
    const string PhaseKey="SkyPulse.ReloadQa.Phase", StartKey="SkyPulse.ReloadQa.Start",
        ErrorKey="SkyPulse.ReloadQa.Error", ReloadKey="SkyPulse.ReloadQa.Reloaded", BaselineKey="SkyPulse.ReloadQa.Baseline",
        FreshKey="SkyPulse.ReloadQa.FreshStart", FrameKey="SkyPulse.ReloadQa.Frame";
    const BindingFlags RuntimeFields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static SkyPulseReloadChecks()
    {
        EditorApplication.update+=Tick;
        CompilationPipeline.compilationStarted+=OnCompile;
        Application.logMessageReceived+=OnLog;
    }
    public static void Run()
    {
        if(Application.companyName!="SkyPulseQA20261006" || Environment.GetEnvironmentVariable("SKYPULSE_VALIDATE_RELOAD_GUARD")!="1")
            throw new Exception("Disposable QA with explicit reload-guard flag required");
        SessionState.SetInt(PhaseKey,1);SessionState.SetFloat(StartKey,(float)EditorApplication.timeSinceStartup);
        SessionState.SetBool(ErrorKey,false);SessionState.SetBool(ReloadKey,false);
        SessionState.SetInt(BaselineKey,SessionState.GetInt(SkyPulsePlaySessionGuard.StopCountKey,0));
        EditorApplication.EnterPlaymode();
    }
    static void OnCompile(object context)
    {
        if(SessionState.GetInt(PhaseKey,0)==2)SessionState.SetInt(PhaseKey,3);
    }
    static void OnLog(string message,string stack,LogType type)
    {
        if(SessionState.GetInt(PhaseKey,0)<2)return;
        // Unity's optional editor indexing can log startup exceptions. Restrict
        // this assertion to the runtime game, including every mode/partial.
        if((type==LogType.Exception || type==LogType.Error) &&
            ((!string.IsNullOrEmpty(stack) && (stack.Contains("SkyPulse.Mobile.") || stack.Contains("SkyPulseNativeGame"))) ||
             (!string.IsNullOrEmpty(message) && message.StartsWith("SkyPulse:"))))
            SessionState.SetBool(ErrorKey,true);
    }
    [DidReloadScripts]
    static void Reloaded()
    {
        if(SessionState.GetInt(PhaseKey,0)==3)SessionState.SetBool(ReloadKey,true);
    }
    static void Tick()
    {
        int phase=SessionState.GetInt(PhaseKey,0);if(phase==0)return;
        if(EditorApplication.timeSinceStartup-SessionState.GetFloat(StartKey,0)>180)
        {Fail("Timed out while waiting for a real in-Play compile/reload");return;}
        if(phase==1 && EditorApplication.isPlaying && !EditorApplication.isCompiling &&
            UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>()!=null)
        {
            SessionState.SetInt(PhaseKey,2);
            CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
        }
        if(phase==3 && SessionState.GetBool(ReloadKey,false) && !EditorApplication.isPlaying &&
            !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            if(SessionState.GetBool(ErrorKey,false)){Fail("Runtime null reference occurred during compilation/reload");return;}
            if(SessionState.GetInt(SkyPulsePlaySessionGuard.StopCountKey,0)!=SessionState.GetInt(BaselineKey,0)+1)
            {Fail("Expected exactly one scoped stop request");return;}
            // Exercise an ordinary second Play, with the real Update/LateUpdate
            // running. Never call Awake manually or clear the player save.
            SessionState.SetInt(PhaseKey,4);
            EditorApplication.EnterPlaymode();
        }
        var game=phase>=4?UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>():null;
        if(phase==4 && EditorApplication.isPlaying && !EditorApplication.isCompiling && game!=null)
        {
            if(!ValidFreshSession(game))return;
            SessionState.SetFloat(FreshKey,(float)EditorApplication.timeSinceStartup);
            SessionState.SetInt(FrameKey,Time.frameCount);
            SessionState.SetInt(PhaseKey,5);
        }
        if(phase>=5 && phase<=7)
        {
            if(SessionState.GetBool(ErrorKey,false)){Fail("SkyPulse runtime exception during fresh Play/navigation");return;}
            if(!EditorApplication.isPlaying || game==null){Fail("Fresh Play ended before validation finished");return;}
            if(!ValidFreshSession(game))return;
        }
        if(phase==5 && EditorApplication.timeSinceStartup-SessionState.GetFloat(FreshKey,0)>=2 &&
            Time.frameCount-SessionState.GetInt(FrameKey,Time.frameCount)>=120)
        {
            Call(game,"OpenHangar");SessionState.SetInt(FrameKey,Time.frameCount);SessionState.SetInt(PhaseKey,6);
        }
        if(phase==6 && Time.frameCount-SessionState.GetInt(FrameKey,Time.frameCount)>=2)
        {
            var hangar=(GameObject)typeof(SkyPulseNativeGame).GetField("customizeScreen",RuntimeFields).GetValue(game);
            if(hangar==null || !hangar.activeInHierarchy){Fail("Fresh Hangar did not open");return;}
            Call(game,"OpenUpgrades");SessionState.SetInt(FrameKey,Time.frameCount);SessionState.SetInt(PhaseKey,7);
        }
        if(phase==7 && Time.frameCount-SessionState.GetInt(FrameKey,Time.frameCount)>=2)
        {
            if(typeof(SkyPulseNativeGame).GetField("holographicTree",RuntimeFields).GetValue(game)==null)
            {Fail("Fresh Tech Tree did not rebuild");return;}
            SessionState.SetInt(PhaseKey,8);EditorApplication.ExitPlaymode();
        }
        if(phase==8 && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode &&
            !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            if(SessionState.GetBool(ErrorKey,false)){Fail("SkyPulse runtime exception while stopping fresh Play");return;}
            SessionState.EraseInt(PhaseKey);
            Debug.Log("SKYPULSE_EDITOR_RELOAD_GUARD_PASS: real in-Play compilation stopped safely, fresh Play ran Update/LateUpdate for at least two seconds and 120 frames, Hangar/Tech Tree reopened without runtime exceptions");
            EditorApplication.Exit(0);
        }
    }
    static bool ValidFreshSession(SkyPulseNativeGame game)
    {
        if(!game.isActiveAndEnabled){Fail("Fresh game is not running its normal update loop");return false;}
        // routeWorld is intentionally absent on Home; the renderer uses equippedWorld.
        foreach(string field in new[]{"flightCamera","birdRenderer","equippedSkin","equippedWorld","birdProgression"})
            if(typeof(SkyPulseNativeGame).GetField(field,RuntimeFields).GetValue(game)==null)
            {Fail("Fresh session failed to restore "+field);return false;}
        return true;
    }
    static void Call(SkyPulseNativeGame game,string method)
    {typeof(SkyPulseNativeGame).GetMethod(method,RuntimeFields).Invoke(game,null);}
    static void Fail(string reason)
    {
        SessionState.EraseInt(PhaseKey);Debug.LogError("SKYPULSE_EDITOR_RELOAD_GUARD_FAIL: "+reason);EditorApplication.Exit(1);
    }
}
