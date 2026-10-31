// Disposable QA capture; never loaded into the player.
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulsePolishCapture
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static SkyPulsePolishCapture(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");PlayerPrefs.DeleteAll();PlayerPrefs.Save();SessionState.SetBool("PolishCapture",true);EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool("PolishCapture",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("PolishCapture",false);
        new GameObject("Polish visual capture").AddComponent<SkyPulseWorldExpansionRunner>().Begin(Capture());
    }
    static IEnumerator Capture()
    {
        yield return Live(UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>());
        EditorApplication.Exit(0);
    }
    public static IEnumerator Live(SkyPulseNativeGame g)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        g.enabled=false;
        Set(g,"reduceMotionEnabled",false);Set(g,"hapticsEnabled",false);
        Call(g,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(g,"ResetToMenu");Call(g,"UpdateCinematicPresentation",0f);
        Call(g,"OpenHangar");Call(g,"UpdateCinematicPresentation",0f);
        var output=Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");
        Directory.CreateDirectory(Path.Combine(output,"player-experience/hangar-motion"));
        yield return null;yield return null;
        for(int frame=0;frame<90;frame++)
        {
            Call(g,"UpdateHangarHeroPresentation");
            // Advance the studio deterministically: PNG encoding time must not
            // speed the animated bird up in the resulting 30 FPS preview.
            var studio=(SkyPulseProgressionStage)Get(g,"hangarPreviewStage");
            studio.MotionPaused=false;studio.Tick(1f/30);studio.MotionPaused=true;
            yield return null;
            SkyPulsePlayerExperienceChecks.CaptureUi(g,"hangar-motion/"+frame.ToString("000"));
            if(frame==45)SkyPulsePlayerExperienceChecks.CaptureUi(g,"hangar-showcase");
        }
        ((SkyPulseProgressionStage)Get(g,"hangarPreviewStage")).MotionPaused=false;
        var page=(RectTransform)Get(g,"hangarPageRoot");
        foreach(var text in page.GetComponentsInChildren<Text>())
            if(text.text=="‹"||text.text=="›")
                Debug.Log("HANGAR_NAV "+text.text+" vertices="+text.cachedTextGenerator.vertexCount+" position="+text.rectTransform.position+" rect="+text.rectTransform.rect+" colour="+text.color);
        Call(g,"OpenUpgrades");yield return null;yield return null;
        SkyPulsePlayerExperienceChecks.CaptureUi(g,"tree-overview");
        ((RectTransform)Get(g,"techNextControl")).GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(.6f);
        SkyPulsePlayerExperienceChecks.CaptureUi(g,"tree-next-focus");
        // Final world shaders are rendered by the normal twelve-world checks.
        yield return SkyPulseDimensionalChecks.Environment(g);
        yield return SkyPulsePresentationChecks.Captures(g);
        Debug.Log("SKYPULSE_POLISH_CAPTURE_PASS");
    }
}

