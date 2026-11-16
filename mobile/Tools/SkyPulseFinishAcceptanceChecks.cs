// Run in a disposable QA product only. Simulated scenarios; never store footage.
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
public static class SkyPulseFinishAcceptanceChecks
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static readonly List<string> checks = new List<string>();
    static string Output => Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");
    static object Get(object o,string n) => o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v) => o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a) => o.GetType().GetMethods(F).Single(m => m.Name==n && m.GetParameters().Length==a.Length).Invoke(o,a);
    static void Check(bool ok,string note) { if(!ok)throw new Exception(note); checks.Add(note); Debug.Log("FINISH_ACCEPTANCE_CHECK: "+note); }
    static SkyPulseFinishAcceptanceChecks() { EditorApplication.update += Tick; }
    public static void Run()
    {
        if(!Application.isBatchMode || Application.companyName!="SkyPulseQA20261006" || Application.productName!="SkyPulseFlapQA20261110")throw new Exception("Disposable QA required");
        Directory.CreateDirectory(Output);PlayerPrefs.DeleteAll();PlayerPrefs.Save();
        SkyPulseNativeGame.ReviewOriginalBirdArtwork=false;SkyPulseBirdRosterPreview.Select("",false);
        EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");SessionState.SetBool("SkyPulse.FinishAcceptance",true);EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying || !SessionState.GetBool("SkyPulse.FinishAcceptance",false))return;
        SessionState.SetBool("SkyPulse.FinishAcceptance",false);
        new GameObject("Final finish checks").AddComponent<SkyPulseFinishAcceptanceRunner>().Begin(Execute());
    }
    sealed class Store : IMissionStore
    {
        readonly Dictionary<string,string> data=new Dictionary<string,string>(),pending=new Dictionary<string,string>();
        public bool FailSet,FailFlush;public int Flushes;
        public string Get(string k,string d) => data.TryGetValue(k,out var v)?v:d;
        public int GetInt(string k,int d) => d;
        public void SetInt(string k,int v) { }
        public void Set(string k,string v) { if(FailSet)throw new IOException("Injected QA write failure");pending[k]=v; }
        public void Flush() { if(FailFlush)throw new IOException("Injected QA flush failure");foreach(var p in pending)data[p.Key]=p.Value;pending.Clear();Flushes++; }
    }
    static Button Action(GameObject root,string title) => root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text==title);
    static bool Has(GameObject root,string part) => root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains(part));
    static void Fit(SkyPulseNativeGame g,Vector2Int size)
    {
        typeof(SkyPulseFailureClarityChecks).GetMethod("Fit",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{g,size});
    }
    static void Show(SkyPulseNativeGame g,Camera camera,RenderTexture rt,string name)
    {
        Call(g,"UpdateProtocolPresentation",.15f);Call(g,"UpdateCinematicPresentation",.15f);Call(g,"UpdateSolidBirds",.15f);Canvas.ForceUpdateCanvases();
        foreach(var stage in UnityEngine.Object.FindObjectsByType<SkyPulseDepthStage>())stage.Render();camera.Render();
        var before=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=before;
    }
    static IEnumerator Execute()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();g.enabled=false;Set(g,"hapticsEnabled",false);((AudioSource)Get(g,"audioSource")).mute=true;
        var camera=(Camera)Get(g,"flightCamera");var rt=new RenderTexture(750,1334,24);rt.Create();camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);
        var canvas=((GameObject)Get(g,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Fit(g,new Vector2Int(rt.width,rt.height));
        SkyPulseCatalogChecks.Pure();SkyPulseJourneyChecks.Pure();
        var oldJourney=(SkyPulsePlayerJourney)Get(g,"playerJourney");var oldRelay=(SkyPulseDailyRelay)Get(g,"dailyRelay");
        DateTime now=new DateTime(2026,11,16,12,0,0,DateTimeKind.Utc);var store=new Store();var journey=new SkyPulsePlayerJourney(store,()=>now);Set(g,"playerJourney",journey);
        Call(g,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(g,"StartFlight");Set(g,"score",5);Set(g,"lastCrashReason","PIPE CONTACT");Call(g,"EndFlight");
        var label=(Text)Get(g,"resultJourneyText");Check(label.text=="ION CRADLE UNLOCKED","A newly earned tier is visible in the actual Endless report");
        Call(g,"RefreshProgressionResultLabels");Check(label.text=="ION CRADLE UNLOCKED","Refreshing the report cannot erase its earned-tier notice");
        Set(g,"state",Enum.Parse(Get(g,"state").GetType(),"GameOver"));Call(g,"RefreshScreens");Show(g,camera,rt,"endless-earned-cradle");
        foreach(var size in new[]{new Vector2Int(640,1136),new Vector2Int(1320,2868),new Vector2Int(1640,2360)})
        {
            var large=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=large;Fit(g,size);Canvas.ForceUpdateCanvases();
            Check(label.preferredWidth<=label.rectTransform.rect.width && label.preferredHeight<=label.rectTransform.rect.height+1,"Cradle notice fits on phone/iPad: "+size);
            var card=label.transform.parent;var corners=new Vector3[4];label.rectTransform.GetWorldCorners(corners);
            foreach(var button in card.GetComponentsInChildren<Button>())
            {
                var bc=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(bc);
                Check(corners[0].y>bc[1].y,"Cradle notice stays separate from every result action: "+size+" / "+button.name);
            }
            camera.Render();Show(g,camera,large,"endless-result-"+size.x+"x"+size.y);camera.targetTexture=rt;RenderTexture.ReleaseTemporary(large);
        }
        Fit(g,new Vector2Int(rt.width,rt.height));Call(g,"StartFlight");Set(g,"score",1);Call(g,"EndFlight");Check(label.text=="PRISM CRADLE · BEST 5 / 15 GATES","A replay retains mastery and shows the next real target, not another unlock");
        // A completed Relay run uses its own gate best and the same cosmetic tiers.
        store=new Store();journey=new SkyPulsePlayerJourney(store,()=>now);Set(g,"playerJourney",journey);Set(g,"dailyRelay",new SkyPulseDailyRelay(new Store(),()=>now));Call(g,"LaunchMission",SkyPulseMode.PulseRelay,0);
        var relay=(RelaySession)Get(g,"relaySession");for(int i=0;i<relay.Mission.RelayCount*2;i++)relay.Collect(i);relay.Move(relay.Mission.ExitX+1);relay.ResolveCourse(0,false);Call(g,"FinalizeMission");
        var result=(GameObject)Get(g,"missionResultScreen");Check(Has(result,"ION CRADLE UNLOCKED"),"Successful Relay exposes its earned bird tier without changing gate rules");Show(g,camera,rt,"relay-earned-cradle");
        Call(g,"LaunchNextRelayMission",SkyPulseDailyMissions.Day(now),0);Check((int)Get(g,"selectedMission")==1,"Next Mission continues in course order on the same UTC day");
        now=now.AddDays(1);Call(g,"LaunchNextRelayMission",SkyPulseDailyMissions.Day(now.AddDays(-1)),0);Check((int)Get(g,"selectedMission")==0 && ((RelaySession)Get(g,"relaySession")).Mission.Day==SkyPulseDailyMissions.Day(now),"Next Mission across midnight starts today's first unfinished circuit");
        // Both failure sites must retain the solved route and expose a retry-save action.
        foreach(bool failFlush in new[]{false,true})
        {
            store=new Store();journey=new SkyPulsePlayerJourney(store,()=>now);Set(g,"playerJourney",journey);Call(g,"LaunchDailyBlackout");var solved=(BlackoutSession)Get(g,"blackoutSession");var route=SkyPulseMissionChecks.Solve(solved.Puzzle);
            for(int i=0;i<route.Count-1;i++)Call(g,"TouchBlackoutNode",route[i]);
            store.FailSet=!failFlush;store.FailFlush=failFlush;Call(g,"TouchBlackoutNode",route.Last());
            Check(solved.State==MissionSessionState.Succeeded && Has(result,"EXTRACTION UNSAVED") && Has(result,"RETRY SAVE"),"Failed "+(failFlush?"flush":"write")+" reports unsaved victory and preserves solve");
            Check(journey.Data.caseGoldMask==0,"No unpersisted archive stamp is displayed as earned");
            if(!failFlush)Show(g,camera,rt,"daily-save-interrupted");
            var retry=Action(result,"RETRY SAVE");Call(g,"PauseFlight");yield return null;retry.onClick.Invoke();Check(Has(result,"EXTRACTION UNSAVED"),"Repeated failing save remains recoverable, without restarting puzzle");
            yield return null;store.FailSet=false;store.FailFlush=false;now=now.AddDays(1);int trace=solved.Trace;long original=(long)Get(g,"blackoutDailyDay");retry=Action(result,"RETRY SAVE");retry.onClick.Invoke();
            Check(ReferenceEquals(solved,Get(g,"blackoutSession")) && solved.Trace==trace && Has(result,"DAILY HEIST COMPLETE"),"Retry Save after midnight persists the original extraction without replaying");
            Check(journey.Data.caseGoldMask==(1<<SkyPulseBlackoutCasebook.Index(original)),"Recovered save stamps the original authored case");int writes=store.Flushes;retry.onClick.Invoke();Call(g,"FinalizeMission");Check(store.Flushes==writes,"Retired retry/completion callbacks cannot write or award twice");
            Show(g,camera,rt,"daily-save-recovered");yield return null;Action(result,"REPLAY DAILY CASE").onClick.Invoke();Check(((BlackoutSession)Get(g,"blackoutSession")).Puzzle==SkyPulseBlackoutCasebook.All[SkyPulseBlackoutCasebook.Index(SkyPulseDailyMissions.Day(now))],"Explicit replay starts the current daily design after recovery");
        }
        Set(g,"playerJourney",oldJourney);Set(g,"dailyRelay",oldRelay);Call(g,"ResetToMenu");
        yield return SkyPulseEverydayControlsChecks.Live(g);yield return SkyPulseFailureClarityChecks.Live(g);yield return SkyPulseTouchComfortChecks.Live(g);yield return SkyPulseJourneyChecks.Live(g,camera,rt);
        File.WriteAllText(Path.Combine(Output,"VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",checks=checks.ToArray(),deviceTested=false},true));Debug.Log("SKYPULSE_FINISH_ACCEPTANCE_PASS");EditorApplication.Exit(0);
    }
    [Serializable] sealed class Report {public string result;public string[] checks;public bool deviceTested;}
    public static void Fail(Exception e) {Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"FAILURE.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
}
public sealed class SkyPulseFinishAcceptanceRunner:MonoBehaviour
{
    public void Begin(IEnumerator routine)=>StartCoroutine(Guard(routine));
    IEnumerator Guard(IEnumerator routine) {while(true){bool more;object next;try{more=routine.MoveNext();next=more?routine.Current:null;}catch(Exception e){SkyPulseFinishAcceptanceChecks.Fail(e);yield break;}if(!more)yield break;if(next is IEnumerator nested)yield return Guard(nested);else yield return next;}}
}
