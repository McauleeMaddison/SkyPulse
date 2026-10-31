// Real Editor Play transitions in the disposable QA product. No player save reset.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulseWorkshopIsolationChecks
{
    const string Key="SkyPulse.WorkshopIsolation.",Shipping="Assets/Scenes/SkyPulse.unity";
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    [Serializable] sealed class Record
    {
        public string result,workshopScene,gameScene;
        public int workshopGameCount,workshopInspectionCameras,shippingGameCount,shippingInspectionCameras,shippingNormalFrames;
        public string[] validation;
    }
    static SkyPulseWorkshopIsolationChecks()
    {
        EditorApplication.update+=Tick;Application.logMessageReceived+=Log;
    }
    static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
    static bool Policy(string scene,bool editor)
    {
        var method=typeof(SkyPulseBootstrap).GetMethod("ShouldStartInScene",BindingFlags.Public|BindingFlags.Static);
        Check(method!=null,"Bootstrap exposes the pure scene-start policy");return (bool)method.Invoke(null,new object[]{scene,editor});
    }
    public static void Run()
    {
        // This is checked before opening scenes or entering Play mode.
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Disposable batch QA product required");
        try
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Start the isolation check from Edit mode");
            Check(Policy(Shipping,true),"Shipping scene starts in the Editor");
            for(int world=0;world<12;world++)Check(!Policy(SkyPulsePipeWorkshop.ScenePath(world),true),"Pipe workshop never starts a game");
            Check(!Policy("Assets/Editor/SkyPulseBackgroundWorkshop/Scenes/01 Neon City.unity",true),"Background workshop never starts a game");
            Check(Policy(Shipping,false),"Shipping player bootstrap stays enabled");
            Check(Policy("Assets/Editor/Inspection.unity",false),"Player behavior is not coupled to Editor folder names");
            SessionState.SetBool(Key+"RuntimeError",false);SessionState.SetFloat(Key+"Started",(float)EditorApplication.timeSinceStartup);
            EditorSceneManager.OpenScene(SkyPulsePipeWorkshop.ScenePath(0),OpenSceneMode.Single);
            SessionState.SetInt(Key+"Phase",1);EditorApplication.EnterPlaymode();
        }
        catch(Exception e){Fail(e.ToString());}
    }
    static void Log(string message,string stack,LogType type)
    {
        if(SessionState.GetInt(Key+"Phase",0)==0)return;
        if((type==LogType.Error||type==LogType.Exception)&&
            ((!string.IsNullOrEmpty(stack)&&stack.Contains("SkyPulse.Mobile"))||(!string.IsNullOrEmpty(message)&&message.StartsWith("SkyPulse:"))))
            SessionState.SetBool(Key+"RuntimeError",true);
    }
    static T[] All<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
    static int InspectionCameras()=>All<Camera>().Count(c=>c.name=="Pipe inspection camera");
    static void FreshGame(SkyPulseNativeGame game)
    {
        Check(game.isActiveAndEnabled,"Shipping game runs its regular Update loop");
        foreach(string field in new[]{"flightCamera","birdRenderer","equippedSkin","equippedWorld","birdProgression"})
            Check(typeof(SkyPulseNativeGame).GetField(field,Fields).GetValue(game)!=null,"Shipping session restores "+field);
        var camera=(Camera)typeof(SkyPulseNativeGame).GetField("flightCamera",Fields).GetValue(game);
        Check(camera.enabled&&camera.gameObject.activeInHierarchy,"Shipping flight camera is active");
    }
    static void Tick()
    {
        int phase=SessionState.GetInt(Key+"Phase",0);if(phase==0)return;
        try
        {
            Check(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+"Started",0)<150,"Timed out waiting for real workshop/shipping Play transitions");
            Check(!SessionState.GetBool(Key+"RuntimeError",false),"Runtime exception during workshop/shipping isolation check");
            if(phase==1&&EditorApplication.isPlaying&&!EditorApplication.isCompiling)
            {
                SessionState.SetInt(Key+"Frame",Time.frameCount);SessionState.SetInt(Key+"Phase",2);return;
            }
            if(phase==2&&EditorApplication.isPlaying&&Time.frameCount-SessionState.GetInt(Key+"Frame",Time.frameCount)>=3)
            {
                Check(SceneManager.GetActiveScene().path==SkyPulsePipeWorkshop.ScenePath(0),"Actual Play starts in the workshop scene");
                Check(All<SkyPulseNativeGame>().Length==0,"Workshop Play creates no SkyPulseNativeGame");
                Check(All<Collider2D>().Length==0,"Workshop Play creates no gameplay collision shapes");
                Check(InspectionCameras()==1,"Workshop retains exactly one authoring inspection camera");
                SessionState.SetInt(Key+"Phase",3);EditorApplication.ExitPlaymode();return;
            }
            if(phase==3&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating)
            {
                EditorSceneManager.OpenScene(Shipping,OpenSceneMode.Single);SessionState.SetInt(Key+"Phase",4);EditorApplication.EnterPlaymode();return;
            }
            if(phase==4&&EditorApplication.isPlaying&&!EditorApplication.isCompiling)
            {
                Check(SceneManager.GetActiveScene().path==Shipping,"Actual Play restarts in the shipping scene");
                Check(All<SkyPulseNativeGame>().Length==1,"Shipping scene creates exactly one game");
                Check(InspectionCameras()==0,"Shipping scene contains no workshop inspection camera");
                FreshGame(All<SkyPulseNativeGame>()[0]);SessionState.SetInt(Key+"Frame",Time.frameCount);SessionState.SetFloat(Key+"NormalStart",(float)EditorApplication.timeSinceStartup);
                SessionState.SetInt(Key+"Phase",5);return;
            }
            if(phase==5)
            {
                Check(EditorApplication.isPlaying,"Shipping Play continues until normal-loop checks finish");
                var games=All<SkyPulseNativeGame>();Check(games.Length==1,"No duplicate game appears in the shipping scene");FreshGame(games[0]);
                Check(InspectionCameras()==0,"No workshop camera leaks into the shipping session");
                int frames=Time.frameCount-SessionState.GetInt(Key+"Frame",Time.frameCount);
                if(frames>=120&&EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+"NormalStart",0)>=2)
                {
                    SessionState.SetInt(Key+"NormalFrames",frames);SessionState.SetInt(Key+"Phase",6);EditorApplication.ExitPlaymode();
                }
                return;
            }
            if(phase==6&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating)
            {
                Check(!SessionState.GetBool(Key+"RuntimeError",false),"Both Play sessions stop without runtime exceptions");
                var folder=Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");if(!string.IsNullOrEmpty(folder))
                {
                    folder=Path.Combine(folder,"workshop-isolation");Directory.CreateDirectory(folder);
                    File.WriteAllText(Path.Combine(folder,"VALIDATION.json"),JsonUtility.ToJson(new Record{result="PASS",workshopScene=SkyPulsePipeWorkshop.ScenePath(0),gameScene=Shipping,
                        workshopGameCount=0,workshopInspectionCameras=1,shippingGameCount=1,shippingInspectionCameras=0,shippingNormalFrames=SessionState.GetInt(Key+"NormalFrames",0),
                        validation=new[]{"all twelve pipe scene paths excluded by Editor bootstrap policy","background workshop scene path excluded","actual workshop Play contains no game or gameplay colliders","fresh shipping Play creates one normal game and no authoring camera","regular shipping Update/LateUpdate for at least two seconds and 120 frames","disposable QA product only; no owner preferences reset"}},true));
                }
                SessionState.EraseInt(Key+"Phase");Debug.Log("SKYPULSE_WORKSHOP_ISOLATION_PASS: workshop has no game, shipping has one game and no inspection camera; normal shipping loop ran at least 120 frames and two seconds");EditorApplication.Exit(0);
            }
        }
        catch(Exception e){Fail(e.ToString());}
    }
    static void Fail(string reason)
    {
        SessionState.EraseInt(Key+"Phase");Debug.LogError("SKYPULSE_WORKSHOP_ISOLATION_FAIL: "+reason);EditorApplication.Exit(1);
    }
}
