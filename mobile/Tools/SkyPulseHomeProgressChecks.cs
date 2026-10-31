// Disposable Editor QA only. Exercises rendered Home UI without changing saves.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

[UnityEditor.InitializeOnLoad]
public static class SkyPulseHomeProgressChecks
{
    static SkyPulseHomeProgressChecks(){EditorApplication.update+=TickRunner;}
    public static void Run()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable batch QA only");
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");PlayerPrefs.DeleteAll();PlayerPrefs.Save();
        SessionState.SetBool("SkyPulse.HomeProgressQA",true);EditorApplication.EnterPlaymode();
    }
    static void TickRunner()
    {
        if(!SessionState.GetBool("SkyPulse.HomeProgressQA",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("SkyPulse.HomeProgressQA",false);new GameObject("Home progress QA runner").AddComponent<SkyPulseWorldExpansionRunner>().Begin(Execute());
    }
    static IEnumerator Execute()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();game.enabled=false;
        typeof(SkyPulseNativeGame).GetField("hapticsEnabled",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,false);
        yield return Live(game);Debug.Log("SKYPULSE_HOME_PROGRESS_RUN_PASS");EditorApplication.Exit(0);
    }

    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    [Serializable] sealed class LabelSample
    {
        public string viewport,mode,text;
        public int authoredFontSize;
        public float renderedFontPoints;
        public Rect screenBounds;
    }
    [Serializable] sealed class Report {public LabelSample[] labels;public int captures;}
    static object Get(object o,string n)
    {
        var field=o.GetType().GetField(n,F);return field!=null?field.GetValue(o):o.GetType().GetProperty(n,F).GetValue(o);
    }
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool good,string message){if(!good)throw new Exception(message);Debug.Log("HOME_PROGRESS_CHECK: "+message);}
    static Array Roster(SkyPulseNativeGame game)=>(Array)game.GetType().GetProperty("BirdRoster",F).GetValue(game);
    static Array Upgrades()=>(Array)typeof(SkyPulseNativeGame).GetField("Upgrades",S).GetValue(null);
    static Rect Bounds(RectTransform rect,Camera camera)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        var points=corners.Select(p=>RectTransformUtility.WorldToScreenPoint(camera,p)).ToArray();
        return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
    }
    static Rect HitBounds(Button button,Camera camera)
    {
        var rect=(RectTransform)button.transform;var padding=button.GetComponent<Image>().raycastPadding;
        var min=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(new Vector3(rect.rect.xMin+padding.x,rect.rect.yMin+padding.y,0)));
        var max=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(new Vector3(rect.rect.xMax-padding.z,rect.rect.yMax-padding.w,0)));
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    static bool Contains(Rect outer,Rect inner)=>inner.xMin>=outer.xMin-1&&inner.xMax<=outer.xMax+1&&inner.yMin>=outer.yMin-1&&inner.yMax<=outer.yMax+1;
    static void Counts(SkyPulseNativeGame game)
    {
        int owned=0,installed=0,capacity=0;var roster=Roster(game);
        foreach(var bird in roster)if((bool)Call(game,"IsSkinOwned",bird))owned++;
        foreach(var upgrade in Upgrades()){installed+=(int)Call(game,"GetUpgradeLevel",(string)Get(upgrade,"Id"));capacity+=(int)Get(upgrade,"MaxLevel");}
        Call(game,"UpdateModeCopy");
        Check(((Text)Get(game,"menuHangarProgressText")).text==$"{owned} / {roster.Length} OWNED","Bird count matches the current roster and ownership");
        Check(((Text)Get(game,"menuTechProgressText")).text==$"{installed} / {capacity} LEVELS","Tech count matches all installed levels and capacity");
    }
    static void CurrentData(SkyPulseNativeGame game)
    {
        var owned=(HashSet<string>)Get(game,"ownedSkinIds");var beforeOwned=owned.ToArray();
        var levels=(Dictionary<string,int>)Get(game,"upgradeLevels");var beforeLevels=new Dictionary<string,int>(levels);
        try
        {
            Counts(game);
            var bird=Roster(game).Cast<object>().FirstOrDefault(b=>(int)Get(b,"Price")>0&&!(bool)Call(game,"IsSkinOwned",b));
            if(bird!=null)
            {
                var previous=((Text)Get(game,"menuHangarProgressText")).text;owned.Add((string)Get(bird,"Id"));Counts(game);
                Check(previous!=((Text)Get(game,"menuHangarProgressText")).text,"An ownership change refreshes the visible bird count immediately");
            }
            var upgrade=Upgrades().Cast<object>().FirstOrDefault(u=>(int)Call(game,"GetUpgradeLevel",(string)Get(u,"Id"))<(int)Get(u,"MaxLevel"));
            if(upgrade!=null)
            {
                var previous=((Text)Get(game,"menuTechProgressText")).text;var id=(string)Get(upgrade,"Id");levels[id]=(int)Call(game,"GetUpgradeLevel",id)+1;Counts(game);
                Check(previous!=((Text)Get(game,"menuTechProgressText")).text,"An installed-level change refreshes the visible tech count immediately");
            }
        }
        finally
        {
            owned.Clear();foreach(var id in beforeOwned)owned.Add(id);levels.Clear();foreach(var entry in beforeLevels)levels.Add(entry.Key,entry.Value);Counts(game);
        }
    }
    static void Fit(SkyPulseNativeGame game,Vector2Int size)
    {
        Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");
        var safe=(RectTransform)Get(game,"safeAreaRoot");
        safe.anchorMin=size.x==1320?new Vector2(0,102f/2868):Vector2.zero;
        safe.anchorMax=size.x==1320?new Vector2(1,2692f/2868):Vector2.one;
        safe.offsetMin=safe.offsetMax=Vector2.zero;
        Canvas.ForceUpdateCanvases();Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();
    }
    static void Label(Text label,RectTransform action,Camera camera,float ratio,bool title,string viewport,string mode,List<LabelSample> report)
    {
        var box=Bounds(label.rectTransform,camera);var origin=RectTransformUtility.WorldToScreenPoint(camera,label.rectTransform.TransformPoint(Vector3.zero));
        var up=RectTransformUtility.WorldToScreenPoint(camera,label.rectTransform.TransformPoint(Vector3.up));
        float points=label.fontSize*Vector2.Distance(origin,up)/ratio;
        Check(!label.resizeTextForBestFit,"Progress titles and counts never shrink silently: "+label.text);
        Check(label.preferredWidth<=label.rectTransform.rect.width+1&&label.preferredHeight<=label.rectTransform.rect.height+1,"One-line Home copy fits its assigned bounds: "+label.text);
        Check(Contains(Bounds(action,camera),box),"Home label stays inside its own action face: "+label.text);
        Check(points>=(title?14.9f:12.5f),"Rendered Home "+(title?"title":"count")+" meets readable point size: "+label.text+" = "+points.ToString("F2")+" pt");
        report.Add(new LabelSample{viewport=viewport,mode=mode,text=label.text,authoredFontSize=label.fontSize,renderedFontPoints=points,screenBounds=box});
    }
    static void Save(Camera camera,RenderTexture target,string path)
    {
        foreach(var stage in UnityEngine.Object.FindObjectsByType<SkyPulseDepthStage>())stage.Render();
        camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.Destroy(image);RenderTexture.active=previous;
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        var camera=(Camera)Get(game,"flightCamera");var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();
        var oldTarget=camera.targetTexture;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldDistance=canvas.planeDistance;
        var oldProtocol=(SkyPulseMode)Get(game,"homeProtocol");bool oldEnabled=game.enabled;game.enabled=false;
        var folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"home-progress");Directory.CreateDirectory(folder);
        var samples=new List<LabelSample>();int captures=0;
        var hangar=(RectTransform)Get(game,"homeHangar");var tech=(RectTransform)((GameObject)Get(game,"homeTech")).transform;
        var bird=(Text)Get(game,"menuHangarProgressText");var levels=(Text)Get(game,"menuTechProgressText");
        var birdTitle=hangar.GetComponentsInChildren<Text>(true).Single(t=>t!=bird);var techTitle=tech.GetComponentsInChildren<Text>(true).Single(t=>t!=levels);
        var parentBird=bird;var parentTech=levels;
        CurrentData(game);
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        try
        {
            foreach(var size in new[]{new Vector2Int(640,1136),new Vector2Int(1320,2868),new Vector2Int(1640,2360),new Vector2Int(1536,2048)})
            {
                var target=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=target;float ratio=size.x==1320?3:2;
                var safe=size.x==1320?new Rect(0,102,1320,2590):new Rect(0,0,size.x,size.y);
                try
                {
                    foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay,SkyPulseMode.BlackoutProtocol})
                    {
                        Call(game,"SetHomeProtocol",mode);Call(game,"ResetToMenu");Call(game,"UpdateCinematicPresentation",0f);Call(game,"UpdateProtocolPresentation",0f);Fit(game,size);yield return null;Fit(game,size);
                        // Reapply every writer to catch the former absolute-position reset.
                        Call(game,"ApplyModeHomeLayout");Call(game,"ApplyHomeIdentity");Call(game,"ApplyHomeIdentity");Canvas.ForceUpdateCanvases();Counts(game);
                        Check(bird.transform.parent==hangar&&levels.transform.parent==tech,"Existing progress labels remain children of their own actions after layout and mode changes");
                        Check(ReferenceEquals(bird,parentBird)&&ReferenceEquals(levels,parentTech),"Mode changes reuse the same progress Text instances");
                        var home=(GameObject)Get(game,"homeScreen");var controls=home.GetComponentsInChildren<Button>();
                        for(int i=0;i<controls.Length;i++)
                        {
                            var box=HitBounds(controls[i],camera);Check(box.width/ratio>=44&&box.height/ratio>=44,"Home action retains a 44 pt touch target: "+controls[i].name);
                            Check(Contains(safe,box),"Home action stays within the current safe area: "+controls[i].name);
                            for(int j=i+1;j<controls.Length;j++)Check(!box.Overlaps(HitBounds(controls[j],camera)),"Home actions keep separate touch bounds: "+controls[i].name+" / "+controls[j].name);
                        }
                        Check(bird.gameObject.activeInHierarchy==(mode==SkyPulseMode.EndlessRoute)&&levels.gameObject.activeInHierarchy==(mode==SkyPulseMode.EndlessRoute),"Only Endless shows collection progress; Relay and Blackout have no loose counters");
                        Check(hangar.gameObject.activeSelf==(mode!=SkyPulseMode.BlackoutProtocol)&&tech.gameObject.activeSelf==(mode==SkyPulseMode.EndlessRoute),"Each Home shows only its relevant collection actions");
                        if(mode==SkyPulseMode.EndlessRoute)
                        {
                            foreach(var pair in new[]{new[]{birdTitle,bird},new[]{techTitle,levels}})
                            {
                                var action=(RectTransform)pair[0].transform.parent;
                                Label(pair[0],action,camera,ratio,true,size.ToString(),mode.ToString(),samples);Label(pair[1],action,camera,ratio,false,size.ToString(),mode.ToString(),samples);
                                Check(!Bounds(pair[0].rectTransform,camera).Overlaps(Bounds(pair[1].rectTransform,camera)),"Title and quiet count occupy separate rows: "+pair[0].text);
                                var face=action.GetComponent<Button>().targetGraphic as SkyPulseCircuitGraphic;
                                Check(face!=null&&face.SoftGlass&&face.ProjectedGlass&&face.color.a>=.99f,"Collection action has a dense satin glass face for scene contrast");
                                Check(pair[1].color.b>=pair[1].color.r,"Collection progress uses quiet cool lettering");
                            }
                            Check(home.GetComponentsInChildren<Text>().Where(t=>t.text.Contains("OWNED")||t.text.Contains("LEVELS")).All(t=>t==bird||t==levels),"No duplicate or freestanding Home collection counters remain");
                        }
                        if(mode==SkyPulseMode.PulseRelay)Check(birdTitle.text=="YOUR BIRD"&&hangar.GetComponentsInChildren<Text>().Length==1,"Relay keeps its single YOUR BIRD action without an outside progress line");
                        Save(camera,target,Path.Combine(folder,"home-"+mode+"-"+size.x+"x"+size.y+".png"));captures++;
                    }
                }
                finally{camera.targetTexture=oldTarget;RenderTexture.ReleaseTemporary(target);}
            }
            File.WriteAllText(Path.Combine(folder,"home-progress-report.json"),JsonUtility.ToJson(new Report{labels=samples.ToArray(),captures=captures},true));
        }
        finally
        {
            camera.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;
            Call(game,"SetHomeProtocol",oldProtocol);Call(game,"ResetToMenu");Call(game,"ApplySafeArea");game.enabled=oldEnabled;
        }
        Debug.Log("SKYPULSE_HOME_PROGRESS_PASS: "+captures+" captures / "+samples.Count+" rendered title and count measurements");
    }
}
