// Editor-only review capture, guarded to the disposable company/product identity.
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
public static class SkyPulseVisualAcceptanceChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static string Output=>Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");
    static readonly List<string> notes=new List<string>();
    static readonly List<string> overflow=new List<string>();
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==a.Length).Invoke(o,a);
    static void Check(bool ok,string note){if(!ok)throw new Exception(note);notes.Add(note);Debug.Log("VISUAL_ACCEPTANCE_CHECK: "+note);}
    static SkyPulseVisualAcceptanceChecks(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006"||Application.productName!="SkyPulseFlapQA20261110")throw new Exception("Disposable QA only");
        Directory.CreateDirectory(Output);PlayerPrefs.DeleteAll();PlayerPrefs.Save();SkyPulseNativeGame.ReviewOriginalBirdArtwork=false;SkyPulseBirdRosterPreview.Select("",false);
        EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");SessionState.SetBool("SkyPulse.VisualAcceptance",true);EditorApplication.EnterPlaymode();
    }
    static void Tick(){if(!EditorApplication.isPlaying||!SessionState.GetBool("SkyPulse.VisualAcceptance",false))return;SessionState.SetBool("SkyPulse.VisualAcceptance",false);new GameObject("Visual acceptance capture").AddComponent<SkyPulseVisualAcceptanceRunner>().Begin(Execute());}
    static void Present(SkyPulseNativeGame g,float dt=.1f)
    {
        Set(g,"ambientTime",(float)Get(g,"ambientTime")+dt);Call(g,"UpdateMenuBird",dt);Call(g,"UpdateProtocolPresentation",dt);Call(g,"UpdateCinematicPresentation",dt);Call(g,"UpdateSolidBirds",dt);Call(g,"UpdateHangarHeroPresentation");
        Canvas.ForceUpdateCanvases();Call(g,"ApplySafeArea");Call(g,"FitInterfaceToSafeArea");Call(g,"FitBlackoutToSafeArea");Canvas.ForceUpdateCanvases();
        foreach(var b in UnityEngine.Object.FindObjectsByType<SkyPulseHeistBoard>())b.Render();foreach(var d in UnityEngine.Object.FindObjectsByType<SkyPulseRestorationDisplay>())d.Render();foreach(var s in UnityEngine.Object.FindObjectsByType<SkyPulseDepthStage>())s.Render();
    }
    static void Save(Camera camera,RenderTexture target,string path)
    {
        camera.Render();var old=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();var file=Path.Combine(Output,path+".png");Directory.CreateDirectory(Path.GetDirectoryName(file));File.WriteAllBytes(file,pixels.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixels);RenderTexture.active=old;
    }
    static void TextFit(GameObject root,string page)
    {
        foreach(var t in root.GetComponentsInChildren<Text>())
        {
            if(!t.enabled||t.text.Length==0)continue;
            // Dynamic free-pan tree labels are deliberately clipped at its viewport.
            if(t.preferredHeight>t.rectTransform.rect.height+2 || t.horizontalOverflow==HorizontalWrapMode.Overflow&&t.preferredWidth>t.rectTransform.rect.width+2)
                overflow.Add(page+" / "+t.name+" / "+t.text.Replace('\n',' ') +" / "+t.preferredHeight+">"+t.rectTransform.rect.height);
        }
    }
    static IEnumerator Execute()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();g.enabled=false;Set(g,"hapticsEnabled",false);((AudioSource)Get(g,"audioSource")).mute=true;
        var camera=(Camera)Get(g,"flightCamera");var rt=new RenderTexture(750,1334,24);rt.Create();camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);var canvas=((GameObject)Get(g,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        Check(g.RuntimeSolidBirdsActive,"Review uses the real native 3D presenter without a legacy-art override");
        foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay,SkyPulseMode.BlackoutProtocol})
        {
            Call(g,"SetHomeProtocol",mode);Call(g,"ResetToMenu");Present(g,1);yield return null;Present(g,0);TextFit((GameObject)Get(g,"homeScreen"),"Home "+mode);
            if(mode==SkyPulseMode.PulseRelay)foreach(var control in new[]{(Button)Get(g,"modeHomeMap"),((RectTransform)Get(g,"homeHangar")).GetComponent<Button>()})
            {var face=(SkyPulseCircuitGraphic)control.targetGraphic;Check(face.rectTransform.sizeDelta==((RectTransform)control.transform).sizeDelta,"Relay visible card matches its real thumb target: "+control.name);Check(control.GetComponentInChildren<Text>().preferredWidth<=control.GetComponentInChildren<Text>().rectTransform.rect.width,"Relay action label fits its actual frame: "+control.name);}
            Save(camera,rt,"home-"+mode);
            Call(g,"OpenModeGuide");int count=mode==SkyPulseMode.EndlessRoute?5:2;
            for(int i=0;i<count;i++){Call(g,"ShowModeGuidePage",i);yield return null;Present(g,0);TextFit((GameObject)Get(g,"modeGuideScreen"),"Guide "+mode+" "+i);Save(camera,rt,"guides/"+mode+"-"+i);}
            Call(g,"ResetToMenu");
        }
        var worlds=(Array)typeof(SkyPulseNativeGame).GetField("Worlds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        for(int world=0;world<12;world++)
        {
            Call(g,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(g,"StartFlight");Set(g,"score",world*15);((Text)Get(g,"hudScoreText")).text=(world*15).ToString();Set(g,"routeWorldIndex",world);Set(g,"routeWorld",worlds.GetValue(world));Call(g,"ApplyRouteWorldVisuals");Call(g,"ResetFlightEnvironment");
            int k=0;float spacing=(float)Call(g,"RoutePipeSpacing");foreach(var pipe in (Array)Get(g,"pipePool"))Call(g,"ConfigurePipe",pipe,1.8f+(k++)*spacing);
            Call(g,"Flap");Call(g,"UpdateGameplayWingState",.08f);Present(g,.08f);Present(g,.6f);yield return null;Present(g,0);
            Check(((GameObject)Get(g,"solidPilot")).activeSelf,"Native solid pilot is present in world "+world);Save(camera,rt,"worlds/"+world.ToString("00"));
        }
        DensityChecks(g);
        var skins=(Array)typeof(SkyPulseNativeGame).GetField("Skins",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        foreach(var skin in skins)
        {
            string id=(string)Get(skin,"Id");Set(g,"equippedSkin",skin);Call(g,"ApplyEquippedVisuals");Call(g,"StartFlight");Present(g,0);
            Call(g,"Flap");float previous=0;
            foreach(float time in new[]{0f,.08f,.18f,.32f})
            {
                float elapsed=time-previous;while(elapsed>0.00001f){float step=Mathf.Min(elapsed,1f/120);Call(g,"UpdateGameplayWingState",step);Present(g,step);elapsed-=step;}previous=time;Save(camera,rt,"birds/"+id+"-"+Mathf.RoundToInt(time*100).ToString("00"));
            }
            Check((string)Get(g,"solidPilotId")==id,"Equipped solid identity is correct in gameplay: "+id);
        }
        Set(g,"equippedSkin",skins.GetValue(0));Call(g,"ApplyEquippedVisuals");Call(g,"ResetToMenu");Call(g,"OpenHangar");Set(g,"hangarInformationFadeTime",1f);Present(g,1);yield return null;Present(g,0);TextFit((GameObject)Get(g,"customizeScreen"),"Hangar");Save(camera,rt,"hangar");
        Call(g,"OpenUpgrades");Present(g,1);yield return null;Present(g,0);Save(camera,rt,"tech-tree");
        Call(g,"OpenCoreProgression");Present(g,1);yield return null;Present(g,0);TextFit((GameObject)Get(g,"birdProgressionScreen"),"Core");Save(camera,rt,"core");
        Call(g,"OpenBirdTraining",skins.GetValue(0));Present(g,1);yield return null;Present(g,0);TextFit((GameObject)Get(g,"birdProgressionScreen"),"Training");Save(camera,rt,"training");
        // Only this guarded QA save receives extra ownership, for a real breeding quote.
        ((HashSet<string>)Get(g,"ownedSkinIds")).Add((string)Get(skins.GetValue(1),"Id"));((SkyPulseBirdProgression)Get(g,"birdProgression")).Data.core=2;Call(g,"OpenBreeding",skins.GetValue(0));Present(g,1);yield return null;Present(g,0);TextFit((GameObject)Get(g,"birdProgressionScreen"),"Breeding");Save(camera,rt,"breeding");
        Call(g,"LaunchMission",SkyPulseMode.PulseRelay,0);var relay=(RelaySession)Get(g,"relaySession");Set(g,"birdY",relay.Mission.RelayY[0]);var flyer=(Transform)Get(g,"bird");flyer.position=new Vector3(flyer.position.x,relay.Mission.RelayY[0],flyer.position.z);relay.Move(relay.Mission.RelayX(0)-2.2f);Call(g,"UpdateRelayPresentation");Present(g,.1f);yield return null;Present(g,0);Save(camera,rt,"relay-locked");
        // Use real collection/collision code, not data-only Collect calls, so
        // collected crystal visuals and gate unlock effects match gameplay.
        for(int state=1;state<=2;state++)
        {
            Call(g,"LaunchMission",SkyPulseMode.PulseRelay,0);relay=(RelaySession)Get(g,"relaySession");var cells=(List<Vector2>)Get(g,"relayCellPositions");
            for(int i=0;i<state;i++){relay.Move(cells[i].x-relay.Distance);Set(g,"birdY",cells[i].y);Set(g,"birdVelocity",0f);Call(g,"SimulateRelay",0f);}
            Check(relay.Cells==state,"Actual flight collection records exactly "+state+" gate crystals");
            for(int i=0;i<state;i++)Check(!((List<Transform>)Get(g,"relayCells"))[i].gameObject.activeSelf,"A collected crystal disappears through the real flight hook: "+i);
            relay.Move(relay.Mission.RelayX(0)-2.2f-relay.Distance);Set(g,"birdY",relay.Mission.RelayY[0]);Set(g,"birdVelocity",0f);Call(g,"UpdateRelayPresentation");Present(g,.1f);Save(camera,rt,state==1?"relay-half-charge":"relay-open");
        }
        Check(relay.IsOpen(0),"Both matching crystals open the gate before physical passage");
        relay.Move(relay.Mission.RelayX(0)+1f-relay.Distance);Call(g,"SimulateRelay",0f);Call(g,"UpdateRelayPresentation");Present(g,.1f);Check(relay.Passed==1&&relay.State==MissionSessionState.Playing,"The actual unlocked gate collision/pass hook accepts the bird through its centre");Save(camera,rt,"relay-passage");
        Call(g,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);Call(g,"RestartBlackoutVault");Present(g,.1f);Save(camera,rt,"blackout-start");Call(g,"ToggleBlackoutPreview");Call(g,"PreviewBlackoutNode",6);Present(g,.1f);Save(camera,rt,"blackout-plan");Call(g,"CancelBlackoutPreview");var board=(BlackoutSession)Get(g,"blackoutSession");foreach(var tile in SkyPulseMissionChecks.Solve(board.Puzzle))Call(g,"TouchBlackoutNode",tile);Present(g,.1f);Save(camera,rt,"blackout-next");
        yield return SkyPulseHomeProgressChecks.Live(g);
        yield return SkyPulseTouchComfortChecks.Live(g);
        yield return SkyPulseGuideWorldChecks.Live(g);
        Call(g,"ResetToMenu");File.WriteAllText(Path.Combine(Output,"VISUAL_CAPTURE.json"),JsonUtility.ToJson(new Report{result="CAPTURED",checks=notes.ToArray(),textFitFindings=overflow.ToArray(),ownerApproved=false,deviceTested=false},true));Debug.Log("SKYPULSE_VISUAL_ACCEPTANCE_CAPTURED");EditorApplication.Exit(0);
    }
    static void DensityChecks(SkyPulseNativeGame g)
    {
        var env=(SkyPulseFlightEnvironment)Get(g,"flightEnvironment");var rows=(MeshRenderer[,])Get(env,"districtRenderers");var links=(Mesh[,])Get(env,"cityLinks");
        var meshes=new HashSet<Mesh>(Resources.FindObjectsOfTypeAll<Mesh>());var filters=env.WorldScene.GetComponentsInChildren<MeshFilter>(true);
        var landing=typeof(SkyPulseFlightEnvironment).GetMethod("CityLanding",BindingFlags.Static|BindingFlags.NonPublic);
        int[] left={5,4,7},right={4,6,5};
        for(int world=0;world<12;world++)
        {
            env.ResetView();
            foreach(float distance in new[]{0f,75f,150f})
            {
                env.Present(world,distance,3,1,Color.cyan,new Color(.02f,.04f,.08f),false,1);
                Check(rows.Cast<MeshRenderer>().Count(x=>x.gameObject.activeInHierarchy)==9,"Six inhabited districts plus three skyways use the existing bounded bank: "+world+" / "+distance);
                var planes=GeometryUtility.CalculateFrustumPlanes(env.WorldCamera);int visible=0;
                foreach(int i in new[]{0,2,4,5,6,7})if(GeometryUtility.TestPlanesAABB(planes,rows[1,i].bounds))visible++;
                Check(visible>=5,"At least five detailed districts remain in the actual portrait frustum: "+world+" / "+distance+" / "+visible);
                for(int link=0;link<3;link++)
                {
                    var mesh=links[world,link];var t=rows[1,9+link].transform;
                    foreach(var pair in new[]{new Vector2(left[link],1.8f),new Vector2(right[link],-1.8f)})
                    {
                        var expected=rows[1,(int)pair.x].transform.TransformPoint(new Vector3(pair.y,.29f,0));
                        var planned=(Vector3)landing.Invoke(null,new object[]{world,(int)pair.x,pair.y});
                        var bounds=mesh.bounds;bounds.Expand(.05f);
                        Check(Vector3.Distance(t.TransformPoint(planned),expected)<.005f&&bounds.Contains(planned),"Cached skyway anchor stays attached to its real landing and inside bridge bounds: "+world+" / "+distance+" / "+link+" / "+pair.x);
                    }
                }
                Check(env.WorldScene.GetComponentsInChildren<MeshFilter>(true).SequenceEqual(filters)&&filters.All(f=>meshes.Contains(f.sharedMesh)),"Travel/theme changes only reuse cached meshes and transforms: "+world+" / "+distance);
            }
        }
        Check(env.WorldScene.GetComponentsInChildren<Collider>(true).Length==0&&env.WorldScene.GetComponentsInChildren<Collider2D>(true).Length==0,"Denser city remains entirely behind gameplay with no colliders");
        Check(links.Length==36&&links.Cast<Mesh>().Distinct().Count()==36,"Three world-specific skyways per theme are cached once, not generated during flight");
        for(int i=0;i<10;i++)env.Present(11,150,3,1,Color.cyan,Color.black,false);long allocated=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<60;i++)env.Present(11,150+i*.01f,3,1,Color.cyan,Color.black,false);
        Check(GC.GetAllocatedBytesForCurrentThread()==allocated,"Warmed connected-city parallax introduces no managed allocation per Present");
    }
    [Serializable]sealed class Report{public string result;public string[] checks,textFitFindings;public bool ownerApproved,deviceTested;}
    public static void Fail(Exception e){File.WriteAllText(Path.Combine(Output,"FAILURE.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
}
public sealed class SkyPulseVisualAcceptanceRunner:MonoBehaviour
{
    public void Begin(IEnumerator routine)=>StartCoroutine(Guard(routine));
    IEnumerator Guard(IEnumerator routine){while(true){bool more;object next;try{more=routine.MoveNext();next=more?routine.Current:null;}catch(Exception e){SkyPulseVisualAcceptanceChecks.Fail(e);yield break;}if(!more)yield break;if(next is IEnumerator nested)yield return Guard(nested);else yield return next;}}
}
