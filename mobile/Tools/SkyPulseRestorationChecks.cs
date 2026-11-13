// Isolated Editor regression and real-render review. Never runs on the owner's save.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;
[InitializeOnLoad]
public static class SkyPulseRestorationChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static readonly List<string> checks=new List<string>();
    static string Output=>Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    static void Check(bool yes,string s){if(!yes)throw new Exception(s);checks.Add(s);Debug.Log("RESTORATION_CHECK: "+s);}
    static SkyPulseRestorationChecks(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006"||Application.productName!="SkyPulseFlapQA20261110")throw new Exception("Disposable QA required");
        Directory.CreateDirectory(Output);PlayerPrefs.DeleteAll();PlayerPrefs.Save();SkyPulseNativeGame.ReviewOriginalBirdArtwork=false;SkyPulseBirdRosterPreview.Select("",false);
        EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");SessionState.SetBool("SkyPulse.RestorationQA",true);EditorApplication.EnterPlaymode();
    }
    static void Tick(){if(!EditorApplication.isPlaying||!SessionState.GetBool("SkyPulse.RestorationQA",false))return;SessionState.SetBool("SkyPulse.RestorationQA",false);new GameObject("Restoration QA").AddComponent<SkyPulseRestorationRunner>().Begin(Execute());}
    sealed class Store:IMissionStore
    {
        public Dictionary<string,string> Values=new Dictionary<string,string>();
        public string Get(string k,string d)=>Values.TryGetValue(k,out var v)?v:d;public int GetInt(string k,int d)=>d;
        public void Set(string k,string v)=>Values[k]=v;public void SetInt(string k,int v){}public void Flush(){}
    }
    static IEnumerator Execute()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();g.enabled=false;Set(g,"hapticsEnabled",false);Set(g,"flightCoachStage",2);((AudioSource)Get(g,"audioSource")).mute=true;
        var camera=(Camera)Get(g,"flightCamera");var target=new RenderTexture(750,1334,24);target.Create();camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);
        var canvas=((GameObject)Get(g,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Fit(g);
        SkyPulseBlackoutChecks.Pure();yield return SkyPulseBlackoutChecks.Live(g);yield return SkyPulseHeistDepthChecks.Live(g);yield return SkyPulsePurposeChecks.Live(g);
        var store=new Store();var campaign=new BlackoutCampaignProgress(store);Set(g,"blackoutCampaign",campaign);
        Check(campaign.RestoredMask==0,"No restoration is granted to a new save");
        var block=new MaterialPropertyBlock();SkyPulseWorldRestoration.Apply(block,0);Check(block.GetFloat("_RestoredWindows")==0&&block.GetFloat("_RestoredBeacons")==0&&block.GetFloat("_RestoredTransit")==0,"Original world illumination is preserved before any Blackout progress");
        for(int i=0;i<8;i++)
        {
            var s=campaign.Start(i,true);foreach(int move in SkyPulseMissionChecks.Solve(s.Puzzle))s.Rotate(move);
            Check(s.State==MissionSessionState.Succeeded,"Authored restoration puzzle solves: "+i);campaign.Complete(i,s.Medal,s.Trace);int expected=(1<<(i+1))-1;
            Check(campaign.RestoredMask==expected,"Only a solved system grants its light: "+i);campaign.Complete(i,s.Medal,s.Trace);Check(campaign.RestoredMask==expected,"Replay cannot multiply the restoration reward: "+i);
            Check(new BlackoutCampaignProgress(store).RestoredMask==expected,"Restoration reloads from the existing v3 save: "+i);
        }
        Check(store.Values.Count==1&&store.Values.ContainsKey(BlackoutCampaignProgress.Key),"No new save key or second economy was created");
        Call(g,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(g,"ResetToMenu");Call(g,"StartFlight");Present(g,.1f);
        var environment=(SkyPulseFlightEnvironment)Get(g,"flightEnvironment");
        var renderers=(MeshRenderer[,])Get(environment,"districtRenderers");
        for(int world=0;world<12;world++)
        {
            environment.RestoredMask=0;environment.Present(world,0,5,1,new Color(.3f,.8f,1),new Color(.02f,.04f,.09f),false,1);environment.Render();if(world==0)Save(environment.WorldImage,"endless-before-restoration.png");
            environment.RestoredMask=255;environment.Present(world,0,5,1,new Color(.3f,.8f,1),new Color(.02f,.04f,.09f),false,1);renderers[1,4].GetPropertyBlock(block);
            Check(block.GetFloat("_RestoredWindows")>.6f&&block.GetFloat("_RestoredBeacons")>.5f&&block.GetFloat("_RestoredTransit")>.4f,"All three light families follow actual progress in world "+world);
            environment.Render();if(world==0)Save(environment.WorldImage,"endless-after-restoration.png");
        }
        Call(g,"LaunchMission",SkyPulseMode.PulseRelay,0);var relay=(RelaySession)Get(g,"relaySession");Set(g,"birdY",relay.Mission.RelayY[0]);relay.Move(relay.Mission.RelayX(0)-4.8f);Call(g,"UpdateRelayPresentation");Present(g,.18f);yield return null;Present(g,0);camera.Render();Save(target,"relay-titanium-skyport.png");
        var relayWorld=(SkyPulseRelayEnvironment)Get(g,"relayEnvironment");var scene=(Transform)Get(relayWorld,"scene");
        foreach(string quiet in new[]{"Inner orbital megastructure","Outer orbital megastructure","Overhead optical gantry","Suspended launch hub"})Check(!scene.GetComponentsInChildren<Transform>(true).First(t=>t.name==quiet).gameObject.activeSelf,"Relay A removes competing scenery: "+quiet);
        foreach(string visible in new[]{"East transit tower","West transfer tower","Middle transit bridge","Lower transit bridge","Arrival transit bridge","Angled arrival apron"})Check(scene.GetComponentsInChildren<Transform>(true).First(t=>t.name==visible).gameObject.activeInHierarchy,"Relay A retains connected structure: "+visible);
        for(int stage=0;stage<8;stage++)
        {
            Call(g,"LaunchMission",SkyPulseMode.BlackoutProtocol,stage);Call(g,"RestartBlackoutVault");Present(g,.2f);yield return null;Present(g,0);
            var board=(SkyPulseHeistBoard)Get(g,"blackoutBoard");var island=(SkyPulseRestorationDisplay)Get(g,"blackoutRestoration");var live=(BlackoutSession)Get(g,"blackoutSession");
            Check(island.RestoredMask==255,"Diorama receives saved campaign state: "+stage);Check(board.Circuit==live,"New visual renderer observes the real live puzzle: "+stage);
            var buttons=(List<Button>)Get(g,"blackoutNodeButtons");var points=(Vector2[])Get(g,"blackoutPositions");var wrapper=(RectTransform)Get(g,"blackoutOpticalArea");
            Check(wrapper.localScale==Vector3.one,"Board and thumb targets remain full size: "+stage);
            foreach(int tile in Enumerable.Range(0,live.Puzzle.Count).Where(live.Puzzle.Rotatable))Check(Vector2.Distance(((RectTransform)buttons[tile].transform).anchoredPosition,points[tile])<.01f,"Rendered optic and input target share projected centre "+stage+" / "+tile);
            if(stage==0||stage==2||stage==7){camera.Render();Save(target,"blackout-vault-"+stage.ToString("00")+".png");}
        }
        // Exercise the densest real board at the same small phone, notched
        // phone and tablet sizes used by the existing touch-comfort suite.
        foreach(var size in new[]{new Vector2Int(640,1136),new Vector2Int(1320,2868),new Vector2Int(1640,2360)})
        {
            var phoneTarget=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=phoneTarget;
            typeof(SkyPulseTouchComfortChecks).GetMethod("Fit",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{g,size});Call(g,"FitBlackoutToSafeArea");Canvas.ForceUpdateCanvases();
            var targets=(List<Button>)Get(g,"blackoutNodeButtons");var fixture=(BlackoutSession)Get(g,"blackoutSession");
            var safe=size.x==1320?new Rect(0,102,1320,2590):new Rect(0,0,size.x,size.y);float ratio=size.x==1320?3:2;
            foreach(int tile in Enumerable.Range(0,fixture.Puzzle.Count).Where(fixture.Puzzle.Rotatable))typeof(SkyPulseTouchComfortChecks).GetMethod("Target",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{targets[tile],camera,ratio,safe});
            Check(true,"Densest board targets are at least 44 points, safe and raycastable at "+size);camera.Render();Save(phoneTarget,"blackout-layout-"+size.x+"x"+size.y+".png");
            camera.targetTexture=target;RenderTexture.ReleaseTemporary(phoneTarget);
        }
        Fit(g);
        // Show a genuinely new campaign, free planning and actual power. Never
        // fake campaign progress or a success animation into a marketing image.
        campaign=new BlackoutCampaignProgress(new Store());Set(g,"blackoutCampaign",campaign);Call(g,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);Call(g,"RestartBlackoutVault");Present(g,.2f);camera.Render();Save(target,"blackout-new-city.png");
        var display=(SkyPulseRestorationDisplay)Get(g,"blackoutRestoration");Call(g,"ToggleBlackoutPreview");Call(g,"PreviewBlackoutNode",6);Check(display.LivePower==0&&display.RestoredMask==0,"Free planning never lights or saves the actual city");Call(g,"CancelBlackoutPreview");
        Call(g,"TouchBlackoutNode",6);Present(g,.18f);Check(display.LivePower>.49f&&display.RestoredMask==0,"Real power previews the active city system without awarding a completion");camera.Render();Save(target,"blackout-live-power.png");
        Call(g,"RestartBlackoutVault");Check(display.LivePower==0&&display.RestoredMask==0,"Restart clears the partial restoration immediately");
        var current=(BlackoutSession)Get(g,"blackoutSession");foreach(int move in SkyPulseMissionChecks.Solve(current.Puzzle))Call(g,"TouchBlackoutNode",move);Check(campaign.RestoredMask==1&&(int)Get(g,"selectedMission")==1,"Completion persists one system and activates the next puzzle immediately");
        Present(g,.2f);camera.Render();Save(target,"blackout-restored-next-puzzle.png");
        float phase=display.Phase;display.MotionPaused=true;display.Tick(.5f);Check(display.Phase==phase,"Pause freezes the live island animation");display.MotionPaused=false;display.ReducedMotion=true;display.Tick(.5f);Check(display.Phase==phase,"Reduced Motion preserves the completed lighting without ambient movement");display.ReducedMotion=false;
        for(int i=0;i<10;i++)display.Tick(1f/60);long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)display.Tick(1f/60);Check(GC.GetAllocatedBytesForCurrentThread()==bytes,"Warmed restoration presentation allocates no managed bytes");
        var motionBoard=(SkyPulseHeistBoard)Get(g,"blackoutBoard");var newPuzzle=(BlackoutSession)Get(g,"blackoutSession");var solution=SkyPulseMissionChecks.Solve(newPuzzle.Puzzle);string frames=Path.Combine(Output,"motion");Directory.CreateDirectory(frames);
        for(int frame=0;frame<50;frame++)
        {
            if(frame==5||frame==19||frame==33){int step=frame==5?0:frame==19?1:2;if(step<solution.Count)Call(g,"TouchBlackoutNode",solution[step]);}
            motionBoard.Tick(.075f);display.Tick(.075f);Present(g,.075f);camera.Render();Save(target,"motion/frame-"+frame.ToString("000")+".png");
        }
        var boardClose=(SkyPulseHeistBoard)Get(g,"blackoutBoard");var boardCamera=(Camera)Get(boardClose,"opticalCamera");float oldSize=boardCamera.orthographicSize;boardCamera.orthographicSize=2.9f;boardClose.Render();Save(boardClose.Texture,"blackout-optics-detail.png");boardCamera.orthographicSize=oldSize;boardClose.Render();
        foreach(string name in new[]{"FlightArchitecture","OrbitalStructure","HeistOptics","HeistBeam"})
        {var shader=Resources.Load<Shader>("SkyPulse/Shaders/"+name);Check(shader!=null&&shader.isSupported,"Shader supported: "+name);foreach(var message in ShaderUtil.GetShaderMessages(shader))Check(message.severity.ToString()!="Error","Shader compiles: "+name+" / "+message.message);}
        File.WriteAllText(Path.Combine(Output,"VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",checks=checks.ToArray(),deviceTested=false},true));Debug.Log("SKYPULSE_RESTORATION_PASS");EditorApplication.Exit(0);
    }
    static void Fit(SkyPulseNativeGame g){Canvas.ForceUpdateCanvases();Call(g,"RefreshViewportDecor");Call(g,"ApplySafeArea");Call(g,"FitInterfaceToSafeArea");Call(g,"FitBlackoutToSafeArea");Canvas.ForceUpdateCanvases();}
    static void Present(SkyPulseNativeGame g,float dt){Set(g,"ambientTime",(float)Get(g,"ambientTime")+dt);Call(g,"UpdateProtocolPresentation",dt);Call(g,"UpdateCinematicPresentation",dt);Call(g,"UpdateSolidBirds",dt);Fit(g);foreach(var board in UnityEngine.Object.FindObjectsByType<SkyPulseHeistBoard>())board.Render();foreach(var display in UnityEngine.Object.FindObjectsByType<SkyPulseRestorationDisplay>())display.Render();}
    static void Save(RenderTexture target,string name){var previous=RenderTexture.active;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Output,name),pixels.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixels);RenderTexture.active=previous;}
    [Serializable]public sealed class Report{public string result;public string[] checks;public bool deviceTested;}
    public static void Fail(Exception e){Debug.LogException(e);File.WriteAllText(Path.Combine(Output,"FAILURE.txt"),e.ToString());EditorApplication.Exit(1);}
}
public sealed class SkyPulseRestorationRunner:MonoBehaviour
{
    public void Begin(IEnumerator routine){StartCoroutine(Guard(routine));}
    IEnumerator Guard(IEnumerator routine){while(true){bool more;object next;try{more=routine.MoveNext();next=more?routine.Current:null;}catch(Exception e){SkyPulseRestorationChecks.Fail(e);yield break;}if(!more)yield break;if(next is IEnumerator nested)yield return Guard(nested);else yield return next;}}
}
