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
    sealed class Store : IMissionStore
    {
        readonly Dictionary<string,string> values=new Dictionary<string,string>();
        readonly Dictionary<string,int> numbers=new Dictionary<string,int>();
        public string Get(string key,string fallback)=>values.TryGetValue(key,out var value)?value:fallback;
        public int GetInt(string key,int fallback)=>numbers.TryGetValue(key,out var value)?value:fallback;
        public void Set(string key,string value)=>values[key]=value;
        public void SetInt(string key,int value)=>numbers[key]=value;
        public void Flush(){}
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        game.enabled=false;Call(game,"ClosePurchaseModal");Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");
        var levels=(Dictionary<string,int>)Get(game,"upgradeLevels");var saved=new Dictionary<string,int>(levels);levels.Clear();
        var ownedUpgrades=(HashSet<string>)Get(game,"ownedUpgradeIds");var savedOwned=new HashSet<string>(ownedUpgrades);ownedUpgrades.Clear();
        var previousStore=Get(game,"missionStore");var previousDaily=Get(game,"dailyMissions");var previousProgress=Get(game,"birdProgression");var previousSkin=Get(game,"equippedSkin");
        int balance=(int)Get(game,"crystals");
        // Each live suite owns its transaction state. Earlier suites may leave
        // a discounted Core or pending wallet recovery; neither is this fixture.
        var store=new Store();store.SetInt(SkyPulseDailyMissions.WalletKey,5000);
        Set(game,"missionStore",store);Set(game,"dailyMissions",new SkyPulseDailyMissions(store,()=>new DateTime(2026,10,30,0,0,0,DateTimeKind.Utc)));
        Call(game,"InitializeBirdProgression");
        var skins=(Array)typeof(SkyPulseNativeGame).GetField("Skins",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
        Set(game,"equippedSkin",skins.GetValue(0));Set(game,"crystals",5000);
        Call(game,"OpenUpgrades");Canvas.ForceUpdateCanvases();yield return null;
        var content=(RectTransform)Get(game,"customizeContent");var scroll=(ScrollRect)Get(game,"customizeScroll");var tree=(SkyPulseTechTreeView)Get(game,"holographicTree");
        Call(game,"StartRoundFromCustomize");Check(((GameObject)Get(game,"customizeScreen")).activeInHierarchy,"Blank tree taps do not launch an accidental flight");
        Check(scroll.horizontal&&scroll.vertical&&content.rect.width>scroll.viewport.rect.width&&content.rect.height>scroll.viewport.rect.height,"The tree supports one-finger/mouse panning on both axes");
        Check(scroll.verticalScrollbar==null&&!scroll.viewport.Find("Collection scroll track").gameObject.activeSelf&&scroll.scrollSensitivity==0,"No visible scrollbar or mouse-wheel scrolling remains in the free-pan tree");
        var beforeWheel=content.anchoredPosition;scroll.OnScroll(new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,10)});Check(content.anchoredPosition==beforeWheel,"Mouse wheel cannot move the drag-only tree");
        Check(tree.SculptureCount==10,"The core and all nine upgrade roots have animated 3D optical sculptures");
        var projector=tree.TreeCamera.transform.parent.Find("Living SkyPulse core projector");
        var rotating=projector.Find("Levitating upgrade prism");var fixedPoint=projector.localPosition;var oldRotation=rotating.localRotation;tree.Tick(.2f);
        Check(Quaternion.Angle(oldRotation,rotating.localRotation)>.1f&&projector.localPosition==fixedPoint,"The 3D projector animates while its connected branch root remains fixed");
        Quaternion referencePose=Quaternion.identity;
        foreach(int fps in new[]{30,60,120})
        {
            Set(tree,"clock",0f);for(int i=0;i<fps;i++)tree.Tick(1f/fps);
            if(fps==30)referencePose=rotating.localRotation;
            Check(Quaternion.Angle(referencePose,rotating.localRotation)<.05f,"Projector animation is frame-rate independent at "+fps+" FPS");
        }
        tree.Press(Vector2.zero);tree.ReducedMotion=true;tree.Tick(1);tree.ReducedMotion=false;tree.Tick(0);
        Check((float)Get(tree,"pressAge")>=1f,"Reduced Motion does not queue a stale press animation for later");
        var coreControl=(RectTransform)Get(game,"techCoreControl");
        Check(coreControl.rect.width>=168&&coreControl.rect.height>=168&&coreControl.GetComponent<Button>()!=null,"Core locator has a generous fixed touch target");
        Check(coreControl.parent==((GameObject)Get(game,"customizeScreen")).transform,"Core locator lives in the fixed header and cannot cover the tree's level labels");
        var nextControl=(RectTransform)Get(game,"techNextControl");
        Check(nextControl!=null&&nextControl.rect.width>=168&&nextControl.parent==coreControl.parent,"Available-level locator has a generous fixed header target");
        int beforeFocus=(int)Get(game,"crystals");
        nextControl.GetComponent<Button>().onClick.Invoke();tree.Tick(1);
        Check(!tree.IsFocusing&&content.anchoredPosition!=Vector2.zero&&(int)Get(game,"crystals")==beforeFocus&&!((GameObject)Get(game,"purchaseModal")).activeSelf,"Next locator centres an available upgrade without opening or spending");
        var identities=content.GetComponentsInChildren<Text>().Where(t=>t.name.EndsWith(" identity")).ToArray();
        Check(identities.Length==27&&identities.All(t=>t.fontSize==26&&t.preferredWidth<=t.rectTransform.rect.width+1&&t.preferredHeight<=t.rectTransform.rect.height+1),"All 27 concise upgrade identities fit at a readable single-line size");
        foreach(int fps in new[]{30,60,120})
        {
            tree.FocusCore();tree.Tick(1);tree.FocusPoint(new Vector2(-690,-1070));
            for(int frame=0;frame<fps;frame++)tree.Tick(1f/fps);
            Check(Vector2.Distance(content.anchoredPosition,new Vector2(650,345))<.01f&&!tree.IsFocusing,"Next-level focus lands within both pan limits at "+fps+" FPS");
            content.anchoredPosition=new Vector2(300,900);scroll.velocity=new Vector2(20,30);tree.FocusCore();
            Check(scroll.velocity==Vector2.zero,"Return-to-core clears inertia at "+fps+" FPS");
            for(int frame=0;frame<fps;frame++)tree.Tick(1f/fps);
            Check(content.anchoredPosition==Vector2.zero&&!tree.IsFocusing,"Smooth core return settles exactly at "+fps+" FPS");
        }
        content.anchoredPosition=new Vector2(300,900);tree.FocusCore();tree.Tick(.1f);
        var pan=scroll.viewport.GetComponent<SkyPulseTreePanInput>();
        pan.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});
        var interrupted=content.anchoredPosition;tree.Tick(.1f);
        Check(!tree.IsFocusing&&content.anchoredPosition==interrupted,"Fresh finger/mouse input interrupts core return immediately");
        tree.ReducedMotion=true;coreControl.GetComponent<Button>().onClick.Invoke();
        Check(content.anchoredPosition==Vector2.zero&&!tree.IsFocusing,"Reduced Motion returns to core instantly");tree.ReducedMotion=false;
        var touch=content.GetComponentsInChildren<SkyPulseTreeTouchFeedback>()[0];int originalBalance=(int)Get(game,"crystals");
        touch.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});
        Check((int)Get(game,"crystals")==originalBalance&&!((GameObject)Get(game,"purchaseModal")).activeSelf,"Immediate visual press feedback cannot purchase an upgrade");
        pan.OnBeginDrag(new PointerEventData(EventSystem.current));
        Check(tree.BranchCount==39&&tree.SocketCount==37,"One core feeds three trunks, nine families and all 27 upgrade levels");
        Check(content.GetComponentsInChildren<Text>().Count(t=>t.text.StartsWith("LEVEL "))==27&&content.GetComponentsInChildren<Text>().Any(t=>t.text.StartsWith("NEXT · "))&&content.GetComponentsInChildren<Text>().Any(t=>t.text=="LOCKED"),"Every upgrade point has a prominent level label and an explicit next/locked state");
        var lettering=content.GetComponentsInChildren<SkyPulseHolographicText>();
        Check(lettering.Length==content.GetComponentsInChildren<Text>().Length&&lettering.All(t=>t.GetComponent<Text>().material==tree.TypographyMaterial),"All tree lettering shares one optical material instead of duplicated glow text");
        Check(Resources.Load<Shader>("SkyPulse/Shaders/HolographicText").isSupported&&tree.TypographyMaterial.shader.name=="SkyPulse/Holographic Lettering","Holographic font shader compiles with the font atlas and UI clipping");
        Check(content.GetComponentsInChildren<Text>().Count(t=>t.text=="VIEW UPGRADE ›")==3&&content.GetComponentsInChildren<SkyPulseUiGlyph>().Count(g=>g.Shape==SkyPulseUiGlyph.Kind.UpgradePoint)==3,"Each initially available family has exactly one purchase-inspection cue");
        foreach(var label in lettering.Where(l=>l.GetComponent<Text>().text.StartsWith("LEVEL ")||l.GetComponent<Text>().text=="VIEW UPGRADE ›"))
        {
            var text=label.GetComponent<Text>();Check(text.preferredWidth<=text.rectTransform.rect.width+1,"Minimal level/action lettering fits without horizontal overflow");
        }
        foreach(int fps in new[]{30,60,120}){Set(tree,"clock",0f);for(int i=0;i<fps;i++)tree.Tick(1f/fps);Check(Mathf.Abs(tree.TypographyMaterial.GetFloat("_Clock")-1)<.001f,"Lettering light clock is frame independent at "+fps+" FPS");}
        tree.ReducedMotion=true;tree.Tick(1);Check(tree.TypographyMaterial.GetFloat("_Motion")==0,"Reduced Motion retains the static glossy finish and stops the light sweep");tree.ReducedMotion=false;
        var reachable=new HashSet<Vector2>{new Vector2(0,-250)};
        for(int pass=0;pass<40;pass++)foreach(var b in tree.Branches)if(reachable.Contains(b.From))reachable.Add(b.To);
        Check(tree.Roots.All(r=>reachable.Contains(r.At)),"Every family and level remains connected to the single core");
        Check(content.GetComponentsInChildren<Button>().Length==37&&!content.GetComponentsInChildren<Image>().Any(i=>i.color.a>.001f),"No separate opaque upgrade cards; fixed generous touch areas overlay the continuous tree");
        Check(content.GetComponentsInChildren<Button>().All(b=>((RectTransform)b.transform).rect.width>=200&&((RectTransform)b.transform).rect.height>=180),"Every root/level has a generous stationary touch target");
        var targets=content.GetComponentsInChildren<Button>();
        for(int i=0;i<targets.Length;i++)for(int j=i+1;j<targets.Length;j++)
        {
            var a=(RectTransform)targets[i].transform;var b=(RectTransform)targets[j].transform;
            if(new Rect(a.anchoredPosition-a.sizeDelta*.5f,a.sizeDelta).Overlaps(new Rect(b.anchoredPosition-b.sizeDelta*.5f,b.sizeDelta)))
                throw new Exception("Tree touch targets overlap: "+a.name+" and "+b.name);
        }
        Check(true,"All 37 core/family/level touch targets are disjoint, including neighbouring limbs");
        var veil=((GameObject)Get(game,"customizeScreen")).transform.Find("Customize veil").GetComponent<Image>();
        Check(veil.material.GetFloat("_Backdrop")==1&&veil.color==Color.white&&scroll.viewport.rect.height==1450,"Tree uses a full-bleed quiet atmosphere and expanded exploration field");
        Check(Resources.Load<Shader>("SkyPulse/Shaders/TechTreeField").isSupported,"Tree atmosphere and edge-compositing shader compiles on Metal");
        Check(Resources.Load<Shader>("SkyPulse/Shaders/HolographicTree").isSupported,"Volumetric hologram shader compiles on Metal");
        var uiCanvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();var eventCamera=uiCanvas.renderMode==RenderMode.ScreenSpaceOverlay?null:uiCanvas.worldCamera;
        var start=RectTransformUtility.WorldToScreenPoint(eventCamera,scroll.viewport.position);
        var drag=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=start,pressPosition=start};
        scroll.OnInitializePotentialDrag(drag);scroll.OnBeginDrag(drag);drag.position=start+new Vector2(60,90);scroll.OnDrag(drag);scroll.OnEndDrag(drag);scroll.StopMovement();
        Check(content.anchoredPosition.x>0&&content.anchoredPosition.y>0,"A real ScrollRect drag moves both axes with mouse/touch pointer events");
        content.anchoredPosition=new Vector2(300,900);tree.Tick(0);
        Check(Mathf.Abs(tree.TreeCamera.transform.localPosition.x+3)<.001f&&Mathf.Abs(tree.TreeCamera.transform.localPosition.y+(scroll.viewport.rect.height*.5f+900)*.01f)<.001f,"3D fibres remain aligned with labels when panning both axes");
        var upgrades=(Array)typeof(SkyPulseNativeGame).GetField("Upgrades",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);var first=upgrades.GetValue(0);
        Call(game,"InspectTechLevel",first,2);
        var inspector=((GameObject)Get(game,"purchaseModal")).transform.Find("Purchase card");
        Check(!inspector.Find("Acquisition dock").gameObject.activeSelf&&!inspector.GetComponent<Outline>().enabled,"Upgrade inspector removes the Hangar dock decorations without replacing purchase logic");
        Call(game,"ConfirmPurchase");Check((int)Get(game,"crystals")==5000&&levels.Count==0,"A locked future level cannot charge the wallet");Call(game,"ClosePurchaseModal");
        Call(game,"InspectTechLevel",first,0);SkyPulsePlayerExperienceChecks.CaptureUi(game,"tech-upgrade-inspector");Call(game,"ConfirmPurchase");yield return null;
        Check(levels["crystal_resonator"]==1&&(int)Get(game,"crystals")==4850,"Next-level leaf uses the existing exact price and transaction");
        Check(Vector2.Distance(content.anchoredPosition,new Vector2(300,900))<.1f,"Purchasing retains both pan coordinates");
        Call(game,"InspectTechLevel",first,0);Call(game,"ConfirmPurchase");Check(levels["crystal_resonator"]==1&&(int)Get(game,"crystals")==4850,"Installed leaf cannot repurchase or grant a level");Call(game,"ClosePurchaseModal");
        Call(game,"InspectTechLevel",upgrades.GetValue(1),0);Call(game,"ConfirmPurchase");Check(!levels.ContainsKey("prism_conduit")&&(int)Get(game,"crystals")==4850,"The existing level-two family prerequisite remains enforced");Call(game,"ClosePurchaseModal");
        tree=(SkyPulseTechTreeView)Get(game,"holographicTree");tree.ReducedMotion=true;float clock=tree.AnimationClock;tree.Tick(1);Check(tree.AnimationClock==clock,"Reduced Motion freezes holographic flow without blocking input");tree.ReducedMotion=false;tree.MotionPaused=true;tree.Tick(1);Check(tree.AnimationClock==clock,"Paused hologram does not advance");tree.MotionPaused=false;
        for(int i=0;i<20;i++)tree.Tick(1f/60);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<120;i++)tree.Tick(1f/60);Check(GC.GetAllocatedBytesForCurrentThread()-before==0,"Warmed holographic animation and camera tracking allocate zero managed bytes");
        Call(game,"OpenHangar");yield return null;Check(Get(game,"holographicTree")==null&&!scroll.horizontal&&!scroll.vertical,"Hangar restores its original paging and releases the tree");
        Check(veil.material.shader.name!="SkyPulse/Tech Tree Field"&&scroll.viewport.sizeDelta==new Vector2(950,1280),"Hangar restores its own backdrop and viewport dimensions");
        Check(!UnityEngine.Object.FindObjectsByType<Camera>().Any(c=>c.name=="Holographic viewport camera"&&c.isActiveAndEnabled),"No orphan tree camera renders in the Hangar");
        Call(game,"OpenUpgrades");yield return null;
        tree=(SkyPulseTechTreeView)Get(game,"holographicTree");content=(RectTransform)Get(game,"customizeContent");
        coreControl=(RectTransform)Get(game,"techCoreControl");
        Check(content.anchoredPosition==Vector2.zero,"Reopening the tree starts at the common core");
        var camera=(Camera)Get(game,"flightCamera");var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        string folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"holographic-tree");Directory.CreateDirectory(folder);
        var rt=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=rt;
        // Keep the isolated stage active for rendering. Pause automatic ticking
        // between captured frames instead of disabling the renderer's component.
        tree.MotionPaused=true;
        for(int frame=0;frame<240;frame++)
        {
            // Actual 30 FPS Editor rendering: follow a connected limb, hold its
            // readable levels, then use the same interruptible Core control.
            if(frame<45)content.anchoredPosition=Vector2.zero;
            if(frame==8)tree.Press(new Vector2(0,-250));
            if(frame>=45&&frame<120){float t=Mathf.SmoothStep(0,1,(frame-45)/74f);content.anchoredPosition=new Vector2(520,560)*t;}
            if(frame==210)coreControl.GetComponent<Button>().onClick.Invoke();
            tree.MotionPaused=false;Set(tree,"clock",frame/30f);tree.Tick(1f/30);tree.MotionPaused=true;Canvas.ForceUpdateCanvases();tree.Render();camera.Render();
            RenderTexture.active=rt;var image=new Texture2D(750,1334,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,750,1334),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,frame.ToString("000")+".png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);RenderTexture.active=null;
            yield return null;
        }
        tree.MotionPaused=false;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);
        Call(game,"ClosePurchaseModal");levels.Clear();foreach(var pair in saved)levels[pair.Key]=pair.Value;
        ownedUpgrades.Clear();foreach(var id in savedOwned)ownedUpgrades.Add(id);
        Set(game,"missionStore",previousStore);Set(game,"dailyMissions",previousDaily);Set(game,"birdProgression",previousProgress);Set(game,"equippedSkin",previousSkin);Call(game,"RebuildBirdRoster");
        Set(game,"crystals",balance);Call(game,"SaveProgress");Call(game,"ResetToMenu");Debug.Log("SKYPULSE_TECH_TREE_PASS");
    }
}
