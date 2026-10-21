// Disposable QA only. Exercises real upgrade transactions without touching the owner save.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SkyPulse.Mobile;
public static class SkyPulseTechTreeChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("TREE_CHECK: "+message);}
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        game.enabled=false;Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");
        var levels=(Dictionary<string,int>)Get(game,"upgradeLevels");var saved=new Dictionary<string,int>(levels);levels.Clear();
        int balance=(int)Get(game,"crystals");Set(game,"crystals",5000);
        Call(game,"OpenUpgrades");Canvas.ForceUpdateCanvases();yield return null;
        var content=(RectTransform)Get(game,"customizeContent");var scroll=(ScrollRect)Get(game,"customizeScroll");var tree=(SkyPulseTechTreeView)Get(game,"holographicTree");
        Call(game,"StartRoundFromCustomize");Check(((GameObject)Get(game,"customizeScreen")).activeInHierarchy,"Blank tree taps do not launch an accidental flight");
        Check(scroll.horizontal&&scroll.vertical&&content.rect.width>scroll.viewport.rect.width&&content.rect.height>scroll.viewport.rect.height,"The tree supports one-finger/mouse panning on both axes");
        Check(scroll.verticalScrollbar==null&&!scroll.viewport.Find("Collection scroll track").gameObject.activeSelf&&scroll.scrollSensitivity==0,"No visible scrollbar or mouse-wheel scrolling remains in the free-pan tree");
        var beforeWheel=content.anchoredPosition;scroll.OnScroll(new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,10)});Check(content.anchoredPosition==beforeWheel,"Mouse wheel cannot move the drag-only tree");
        Check(tree.BranchCount==39&&tree.SocketCount==37,"One core feeds three trunks, nine families and all 27 upgrade levels");
        var reachable=new HashSet<Vector2>{new Vector2(0,-160)};
        for(int pass=0;pass<40;pass++)foreach(var b in tree.Branches)if(reachable.Contains(b.From))reachable.Add(b.To);
        Check(tree.Roots.All(r=>reachable.Contains(r.At)),"Every family and level remains connected to the single core");
        Check(content.GetComponentsInChildren<Button>().Length==36&&!content.GetComponentsInChildren<Image>().Any(i=>i.color.a>.001f),"No separate opaque upgrade cards; fixed generous touch areas overlay the continuous tree");
        Check(content.GetComponentsInChildren<Button>().All(b=>((RectTransform)b.transform).rect.width>=200&&((RectTransform)b.transform).rect.height>=180),"Every root/level has a generous stationary touch target");
        Check(Resources.Load<Shader>("SkyPulse/Shaders/HolographicTree").isSupported,"Volumetric hologram shader compiles on Metal");
        var uiCanvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();var eventCamera=uiCanvas.renderMode==RenderMode.ScreenSpaceOverlay?null:uiCanvas.worldCamera;
        var start=RectTransformUtility.WorldToScreenPoint(eventCamera,scroll.viewport.position);
        var drag=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=start,pressPosition=start};
        scroll.OnInitializePotentialDrag(drag);scroll.OnBeginDrag(drag);drag.position=start+new Vector2(60,90);scroll.OnDrag(drag);scroll.OnEndDrag(drag);scroll.StopMovement();
        Check(content.anchoredPosition.x>0&&content.anchoredPosition.y>0,"A real ScrollRect drag moves both axes with mouse/touch pointer events");
        content.anchoredPosition=new Vector2(300,900);tree.Tick(0);
        Check(Mathf.Abs(tree.TreeCamera.transform.localPosition.x+3)<.001f&&Mathf.Abs(tree.TreeCamera.transform.localPosition.y+(scroll.viewport.rect.height*.5f+900)*.01f)<.001f,"3D fibres remain aligned with labels when panning both axes");
        var upgrades=(Array)typeof(SkyPulseNativeGame).GetField("Upgrades",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);var first=upgrades.GetValue(0);
        Call(game,"InspectTechLevel",first,2);Call(game,"ConfirmPurchase");Check((int)Get(game,"crystals")==5000&&levels.Count==0,"A locked future level cannot charge the wallet");Call(game,"ClosePurchaseModal");
        Call(game,"InspectTechLevel",first,0);Call(game,"ConfirmPurchase");yield return null;
        Check(levels["crystal_resonator"]==1&&(int)Get(game,"crystals")==4850,"Next-level leaf uses the existing exact price and transaction");
        Check(Vector2.Distance(content.anchoredPosition,new Vector2(300,900))<.1f,"Purchasing retains both pan coordinates");
        Call(game,"InspectTechLevel",first,0);Call(game,"ConfirmPurchase");Check(levels["crystal_resonator"]==1&&(int)Get(game,"crystals")==4850,"Installed leaf cannot repurchase or grant a level");Call(game,"ClosePurchaseModal");
        Call(game,"InspectTechLevel",upgrades.GetValue(1),0);Call(game,"ConfirmPurchase");Check(!levels.ContainsKey("prism_conduit")&&(int)Get(game,"crystals")==4850,"The existing level-two family prerequisite remains enforced");Call(game,"ClosePurchaseModal");
        tree=(SkyPulseTechTreeView)Get(game,"holographicTree");tree.ReducedMotion=true;float clock=tree.AnimationClock;tree.Tick(1);Check(tree.AnimationClock==clock,"Reduced Motion freezes holographic flow without blocking input");tree.ReducedMotion=false;tree.MotionPaused=true;tree.Tick(1);Check(tree.AnimationClock==clock,"Paused hologram does not advance");tree.MotionPaused=false;
        for(int i=0;i<20;i++)tree.Tick(1f/60);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<120;i++)tree.Tick(1f/60);Check(GC.GetAllocatedBytesForCurrentThread()-before==0,"Warmed holographic animation and camera tracking allocate zero managed bytes");
        Call(game,"OpenHangar");yield return null;Check(Get(game,"holographicTree")==null&&!scroll.horizontal&&!scroll.vertical,"Hangar restores its original paging and releases the tree");
        Check(!UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Any(c=>c.name=="Holographic viewport camera"&&c.isActiveAndEnabled),"No orphan tree camera renders in the Hangar");
        Call(game,"OpenUpgrades");yield return null;
        tree=(SkyPulseTechTreeView)Get(game,"holographicTree");content=(RectTransform)Get(game,"customizeContent");
        Check(content.anchoredPosition==Vector2.zero,"Reopening the tree starts at the common core");
        var camera=(Camera)Get(game,"flightCamera");var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        string folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"holographic-tree");Directory.CreateDirectory(folder);
        var rt=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=rt;
        for(int frame=0;frame<90;frame++)
        {
            // A continuous two-axis pan, beginning at the core and visiting all lanes.
            float t=frame/89f;content.anchoredPosition=new Vector2(Mathf.Sin(t*Mathf.PI*2)*640,Mathf.SmoothStep(0,2150,t));
            tree.Tick(1f/10);Canvas.ForceUpdateCanvases();tree.Render();camera.Render();
            RenderTexture.active=rt;var image=new Texture2D(750,1334,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,750,1334),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,frame.ToString("000")+".png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);RenderTexture.active=null;
            yield return null;
        }
        camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);levels.Clear();foreach(var pair in saved)levels[pair.Key]=pair.Value;Set(game,"crystals",balance);Call(game,"SaveProgress");Call(game,"ResetToMenu");Debug.Log("SKYPULSE_TECH_TREE_PASS");
    }
}
