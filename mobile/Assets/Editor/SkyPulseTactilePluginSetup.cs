using UnityEditor;

// Keep the native source iOS-only and ARC-enabled through Unity's supported
// importer API. No signing, export or public submission happens here.
[InitializeOnLoad]
public static class SkyPulseTactilePluginSetup
{
    public const string Path="Assets/Plugins/iOS/SkyPulseTactileFeedback.mm";
    static SkyPulseTactilePluginSetup(){EditorApplication.delayCall+=Configure;}
    static void Configure()
    {
        var importer=AssetImporter.GetAtPath(Path) as PluginImporter;if(importer==null)return;
        bool changed=importer.GetCompatibleWithAnyPlatform() || importer.GetCompatibleWithEditor() ||
            !importer.GetCompatibleWithPlatform(BuildTarget.iOS) ||
            importer.GetPlatformData(BuildTarget.iOS,"CompileFlags")!="-fobjc-arc" ||
            importer.GetPlatformData(BuildTarget.iOS,"FrameworkDependencies")!="UIKit";
        if(!changed)return;
        importer.SetCompatibleWithAnyPlatform(false);importer.SetCompatibleWithEditor(false);
        importer.SetCompatibleWithPlatform(BuildTarget.iOS,true);
        importer.SetPlatformData(BuildTarget.iOS,"CompileFlags","-fobjc-arc");
        importer.SetPlatformData(BuildTarget.iOS,"FrameworkDependencies","UIKit");
        importer.SaveAndReimport();
    }
}
