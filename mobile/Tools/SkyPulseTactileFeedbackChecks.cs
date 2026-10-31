using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SkyPulse.Mobile;

public static class SkyPulseTactileFeedbackChecks
{
    static void Check(bool good,string text){if(!good)throw new Exception(text);Debug.Log("TACTILE_CHECK: "+text);}
    public static void Run()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable batch QA only");
        try{Pure();Debug.Log("SKYPULSE_TACTILE_PASS");EditorApplication.Exit(0);}
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    public static void Pure()
    {
        var importer=AssetImporter.GetAtPath(SkyPulseTactilePluginSetup.Path) as PluginImporter;
        Check(importer!=null&&!importer.GetCompatibleWithAnyPlatform()&&!importer.GetCompatibleWithEditor()&&importer.GetCompatibleWithPlatform(BuildTarget.iOS),"UIKit bridge is included only in iOS players, never loaded by Editor");
        Check(importer.GetPlatformData(BuildTarget.iOS,"CompileFlags")=="-fobjc-arc"&&importer.GetPlatformData(BuildTarget.iOS,"FrameworkDependencies")=="UIKit","Native source retains ARC and UIKit linking settings");
        var type=typeof(SkyPulseNativeGame).Assembly.GetType("SkyPulse.Mobile.SkyPulseTactileFeedback");
        var pulse=type.GetMethod("Pulse",BindingFlags.Static|BindingFlags.NonPublic);
        pulse.Invoke(null,new object[]{false});pulse.Invoke(null,new object[]{true});
        Check(type.GetMethod("NativePulse",BindingFlags.Static|BindingFlags.NonPublic)==null,"Editor compiles managed no-op feedback with no native P/Invoke entry point");
    }
}
