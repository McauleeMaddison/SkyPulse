using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

// Run only in the isolated QA product. No real player save or owner session.
public static class SkyPulseGuideWorldChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);Debug.Log("GUIDE_WORLD_CHECK: "+s);}
    static string Labels(GameObject root)=>string.Join(" | ",root.GetComponentsInChildren<Text>().Select(t=>t.text));
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        game.enabled=false;Set(game,"hapticsEnabled",false);Set(game,"reduceMotionEnabled",false);
        int wallet=(int)Get(game,"crystals");
        var camera=(Camera)Get(game,"flightCamera");var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        foreach(var size in new[]{new Vector2Int(640,1136),new Vector2Int(1320,2868),new Vector2Int(1640,2360)})
        {
            var target=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=target;
            Call(game,"RefreshViewportDecor");Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();
            foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay,SkyPulseMode.BlackoutProtocol})
            {
                Call(game,"SetHomeProtocol",mode);Call(game,"ResetToMenu");
                var button=(Button)Get(game,"modeHomeGuide");Check(button.gameObject.activeInHierarchy,"Home opens How to Play in "+mode);button.onClick.Invoke();
                var root=(GameObject)Get(game,"modeGuideScreen");Check(root.activeSelf,"Guide opens from its actual Home button");
                int pages=mode==SkyPulseMode.EndlessRoute?5:2;
                for(int page=0;page<pages;page++)
                {
                    Call(game,"ShowModeGuidePage",page);yield return null;Canvas.ForceUpdateCanvases();
                    foreach(var text in root.GetComponentsInChildren<Text>())
                        Check(text.preferredHeight<=text.rectTransform.rect.height+2,"Guide copy fits without truncation: "+mode+" / "+page+" / "+text.text.Substring(0,Math.Min(24,text.text.Length))+" @ "+size);
                    if(mode==SkyPulseMode.EndlessRoute&&page==2)Check(Labels(root).Contains("20%")&&Labels(root).Contains("breeding"),"Core guide explains real savings and lineage capacity");
                    if(mode==SkyPulseMode.EndlessRoute&&page==3)Check(Labels(root).Contains("2%")&&Labels(root).Contains("flight handling stays the same"),"Training guide describes its actual benefit without inventing handling changes");
                    if(mode==SkyPulseMode.EndlessRoute&&page==4)Check(Labels(root).Contains("Both parents stay")||Labels(root).Contains("keep both parents"),"Breeding guide explains parent retention");
                    if(mode==SkyPulseMode.PulseRelay&&page==0)Check(Labels(root).Contains("opens immediately")&&Labels(root).Contains("UTC"),"Relay guide covers automatic gates and daily refresh");
                    if(mode==SkyPulseMode.BlackoutProtocol&&page==0)Check(Labels(root).Contains("PLAN")&&Labels(root).Contains("Closing Plan leaves"),"Routing guide explains non-spending planning and unchanged live state");
                    if(mode==SkyPulseMode.BlackoutProtocol&&page==1)Check(Labels(root).Contains("immediately")&&Labels(root).Contains("Gold")&&Labels(root).Contains("city system"),"Mission guide covers city restoration, trace, Gold and instant progression");
                    if(size.x==640)SkyPulsePlayerExperienceChecks.CaptureUi(game,"guide-"+mode+"-"+page);
                }
                if(mode==SkyPulseMode.EndlessRoute)
                {
                    var worldButton=root.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>().text=="WORLDS &\nPOWER-UPS");worldButton.onClick.Invoke();
                    var worlds=(GameObject)Get(game,"flightGuideScreen");Check(worlds.activeSelf&&!root.activeSelf,"Endless guide reaches the complete world/power-up map");
                    var next=worlds.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>().text=="NEXT");
                    for(int page=0;page<3;page++)next.onClick.Invoke();
                    Call(game,"OpenWorldGuide");Check(Labels(worlds).Contains("ROUTE MAP · 1 / 4"),"Reopening the world map starts at page one");worlds.SetActive(false);
                }
                root.SetActive(false);
            }
            camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
        }
        Check((int)Get(game,"crystals")==wallet,"Guide browsing never spends crystals");
        Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");Call(game,"StartFlight");
        var worldsCatalog=(Array)typeof(SkyPulseNativeGame).GetField("Worlds",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
        var lookup=typeof(SkyPulseNativeGame).GetMethod("WorldIndexForScore",BindingFlags.NonPublic|BindingFlags.Static);
        var textureIds=new HashSet<Texture2D>();
        for(int i=0;i<12;i++)
        {
            var world=worldsCatalog.GetValue(i);var texture=Resources.Load<Texture2D>((string)Get(world,"BackgroundPath"));
            Check(texture!=null&&textureIds.Add(texture),"Distinct authored backdrop loads for "+Get(world,"Name"));
            Check((int)lookup.Invoke(null,new object[]{i*15})==i&&(int)lookup.Invoke(null,new object[]{i*15+14})==i,"World milestone and fifteen-gate duration agree");
        }
        Check((int)lookup.Invoke(null,new object[]{180})==0&&(int)lookup.Invoke(null,new object[]{360})==0,"Full twelve-world route wraps at 180 and 360");
        Call(game,"UpdateDepthPresentation",0f);var material=(Material)Get(game,"routeMetal");
        foreach(int fps in new[]{30,60,120})
        {
            Call(game,"StartFlight");
            for(int i=1;i<=12;i++)
            {
                int next=i%12;Set(game,"score",i*15);Call(game,"BeginWorldTransition",next);
                var incoming=(SpriteRenderer)Get(game,"incomingBackground");var background=(SpriteRenderer)Get(game,"backgroundRenderer");var expected=incoming.sprite;
                float prior=0;bool departed=false;
                for(int f=0;f<Mathf.CeilToInt(1.8f*fps)+1;f++)
                {
                    Call(game,"UpdateWorldTransition",1f/fps);Call(game,"UpdateDepthPresentation",0f);
                    if(incoming.enabled)
                    {
                        Check(incoming.color.a+.0001f>=prior&&background.color.a==1,"Crossfade is monotonic over a fully opaque old background");prior=incoming.color.a;
                        float timer=(float)Get(game,"worldTransitionTimer");
                        if(timer<1.35f){departed=true;Check(material.GetFloat("_Visibility")<.001f,"3D pipes finish fading before the departing pool is hidden");}
                    }
                }
                Check(departed&&!incoming.enabled&&background.sprite==expected&&(int)Get(game,"routeWorldIndex")==next,"Transition commits its exact final image: "+Get(worldsCatalog.GetValue(next),"Name")+" @ "+fps);
                Check(material.GetFloat("_Visibility")==1,"Next world's pipe material is restored");
                for(int f=0;f<=fps;f++)Call(game,"UpdateWorldTransition",1f/fps);
                Check((float)Get(game,"worldRecoveryTimer")==0,"Recovery ends cleanly before normal obstacle flow");
                if(fps==60)SkyPulsePlayerExperienceChecks.CaptureUi(game,"world-"+next.ToString("00"));
            }
        }
        Set(game,"score",15);Call(game,"BeginWorldTransition",1);Call(game,"UpdateWorldTransition",.3f);Call(game,"PauseFlight");float paused=(float)Get(game,"worldTransitionTimer");Call(game,"Update");
        Check((float)Get(game,"worldTransitionTimer")==paused,"Pause freezes an active world crossfade");Call(game,"ResumeFlight");Call(game,"ResetToMenu");Call(game,"StartFlight");Call(game,"UpdateDepthPresentation",0f);
        Check((int)Get(game,"routeWorldIndex")==0&&(float)Get(game,"worldTransitionTimer")==0&&material.GetFloat("_Visibility")==1,"Retry during a transition restores clean Neon City presentation");
        Call(game,"SaveProgress");Call(game,"LoadProgress");Check((int)Get(game,"farthestWorldIndex")==11,"World discovery persists across reload");
        Call(game,"ResetToMenu");Debug.Log("SKYPULSE_GUIDE_WORLD_PASS");
    }
}
