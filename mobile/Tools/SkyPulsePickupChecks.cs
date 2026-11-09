// Editor-only QA. Copy into Assets/Editor of a disposable product; never run on an owner's save.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulsePickupChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Pending="SkyPulse.PickupChecks.Pending";
    static readonly List<string> checks=new List<string>();
    [Serializable] sealed class Stamp {public string path,sha256;}
    [Serializable] sealed class Report
    {public string result,scope;public bool deviceTested;public string[] checks;public Stamp[] sources;}
    static string Output=>Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT")??Application.temporaryCachePath,"pickups");
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static MethodInfo Method(object o,string n,int argc)=>o.GetType().GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==argc);
    static object Call(object o,string n,params object[] args)=>Method(o,n,args.Length).Invoke(o,args);
    static object EnumValue(object o,string field,string value)=>Enum.Parse(o.GetType().GetField(field,F).FieldType,value);
    static void Check(bool ok,string text)
    {if(!ok)throw new Exception(text);checks.Add(text);Debug.Log("PICKUP_CHECK: "+text);}
    static float Timer(SkyPulseNativeGame g,string n)=>(float)Get(g,n);
    static int Count(SkyPulseNativeGame g,string n)=>(int)Get(g,n);
    static object Item(SkyPulseNativeGame g,string pool)=>((Array)Get(g,pool)).GetValue(0);
    static object Gate(SkyPulseNativeGame g)=>Item(g,"pipePool");
    static void RequireIsolated()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006"||!Application.productName.Contains("QA"))
            throw new Exception("Refusing pickup/save tests outside a disposable batch QA product");
    }
    static SkyPulsePickupChecks(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        RequireIsolated();checks.Clear();Directory.CreateDirectory(Output);
        foreach(var file in new[]{"PICKUP-VALIDATION.json","FAILURE.txt"})if(File.Exists(Path.Combine(Output,file)))File.Delete(Path.Combine(Output,file));
        PlayerPrefs.DeleteAll();PlayerPrefs.Save();EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool(Pending,false);
        new GameObject("Isolated pickup QA").AddComponent<SkyPulsePickupCheckRunner>().Begin(Execute());
    }
    static void Fresh(SkyPulseNativeGame game)
    {
        Call(game,"ResetToMenu");Call(game,"StartFlight");
        Set(game,"flightBirdGenome",null);Set(game,"birdCollectionCarry",0f);Set(game,"prismConduitCarry",0f);
    }
    static object Power(SkyPulseNativeGame g,string kind)
    {
        var p=Item(g,"powerUpPool");Call(g,"ConfigurePowerUp",p,Gate(g));Set(p,"Kind",EnumValue(p,"Kind",kind));return p;
    }
    static object CollectPower(SkyPulseNativeGame g,string kind)
    {var p=Power(g,kind);Call(g,"CollectPowerUp",p);return p;}
    static object Crystal(SkyPulseNativeGame g)
    {
        var p=Item(g,"crystalPickupPool");Call(g,"ConfigureCrystalPickup",p,Gate(g),0,1,0f,0f);return p;
    }
    static IEnumerator Execute()
    {
        RequireIsolated();var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();
        if(game==null)game=new GameObject("QA game").AddComponent<SkyPulseNativeGame>();
        game.enabled=false;Set(game,"hapticsEnabled",false);((AudioSource)Get(game,"audioSource")).mute=true;
        ((IDictionary)Get(game,"upgradeLevels")).Clear();((ICollection<string>)Get(game,"ownedUpgradeIds")).Clear();
        Fresh(game);yield return null;
        Check(Convert.ToInt32(EnumValue(Item(game,"powerUpPool"),"Kind","Aegis"))==0&&Convert.ToInt32(EnumValue(Item(game,"powerUpPool"),"Kind","TimePulse"))==1&&Convert.ToInt32(EnumValue(Item(game,"powerUpPool"),"Kind","CrystalMagnet"))==2,"Existing pickup enum identities remain stable");
        Check(Timer(game,"crystalEchoTimer")==0&&Timer(game,"routeLensTimer")==0&&Count(game,"crystalEchoBudget")==0,"A fresh run starts with neither new effect or latent charge");
        var randomState=UnityEngine.Random.state;UnityEngine.Random.InitState(1909);var spawned=new HashSet<string>();
        var scheduled=Item(game,"powerUpPool");
        var iconIds=new Dictionary<string,Sprite>();bool matchingIcons=true;
        for(int sample=0;sample<200;sample++)
        {
            Call(game,"ConfigurePowerUp",scheduled,Gate(game));string kind=Get(scheduled,"Kind").ToString();spawned.Add(kind);
            var icon=((SpriteRenderer)Get(scheduled,"Artwork")).sprite;
            var expected=Call(game,"PowerUpArtwork",Get(scheduled,"Kind")) as Sprite;
            matchingIcons&=icon!=null&&icon==expected&&icon.bounds.size.x>0&&icon.bounds.size.y>0;
            if(icon!=null)iconIds[kind]=icon;
        }
        UnityEngine.Random.state=randomState;
        Check(spawned.SetEquals(new[]{"Aegis","TimePulse","CrystalMagnet","CrystalEcho","RouteLens"}),"The fixed-seed normal pooled random selection includes both new pickups and all three existing powers");
        Check(matchingIcons&&iconIds.Values.Distinct().Count()==5,"All five normal spawn kinds use their own nonempty cached icon, including two distinct new emblems");
        InspectIcon(game,"CrystalEcho");InspectIcon(game,"RouteLens");
        PhysicalCollection(game);Fresh(game);
        var echo=CollectPower(game,"CrystalEcho");float duration=Timer(game,"crystalEchoTimer");int budget=Count(game,"crystalEchoBudget");
        Check(duration==6&&budget==6&&(bool)Call(game,"HasActivePowerEffect"),"Crystal Echo starts a six-second, six-crystal bounded effect");
        Call(game,"TickEndlessPickupBoosts",.5f);Call(game,"CollectPowerUp",echo);
        Check(Mathf.Abs(Timer(game,"crystalEchoTimer")-5.5f)<.0001f&&Count(game,"crystalEchoBudget")==6,"A duplicate power-up callback cannot refresh its consumed pickup");
        int wallet=Count(game,"crystals");int dailyBefore=((SkyPulseDailyMissions)Get(game,"dailyMissions")).Data.progress[(int)DailyGameplayEvent.CrystalPickup];
        for(int i=0;i<8;i++)
        {
            var crystal=Crystal(game);Call(game,"CollectCrystalPickup",crystal);int after=Count(game,"crystals"),remaining=Count(game,"crystalEchoBudget");
            Call(game,"CollectCrystalPickup",crystal);
            Check(Count(game,"crystals")==after&&Count(game,"crystalEchoBudget")==remaining,"Consumed crystal callback is idempotent at collection "+(i+1));
        }
        Check(Count(game,"crystals")==wallet+14&&Count(game,"runCrystalsCollected")==14&&Count(game,"runEchoPickupBonus")==6,"Eight real pickups bank exactly eight base plus six Echo crystals once");
        Check(((SkyPulseDailyMissions)Get(game,"dailyMissions")).Data.progress[(int)DailyGameplayEvent.CrystalPickup]==Math.Min(10,dailyBefore+8),"Echo bonus crystals do not inflate daily collection objectives");
        Check(Timer(game,"crystalEchoTimer")==0&&Count(game,"crystalEchoBudget")==0,"Echo ends immediately when its sixth bonus is spent");
        CollectPower(game,"CrystalEcho");Call(game,"CollectCrystalPickup",Crystal(game));Call(game,"TickEndlessPickupBoosts",1f);
        CollectPower(game,"CrystalEcho");Check(Timer(game,"crystalEchoTimer")==6&&Count(game,"crystalEchoBudget")==6,"A separately configured Echo refreshes to six; duration and budget never stack");
        var resumeCrystal=Crystal(game);Call(game,"PauseFlight");wallet=Count(game,"crystals");Call(game,"CollectCrystalPickup",resumeCrystal);
        var pausedPower=Power(game,"RouteLens");Call(game,"CollectPowerUp",pausedPower);
        Check(Count(game,"crystals")==wallet&&(bool)Get(resumeCrystal,"Active")&&(bool)Get(pausedPower,"Active")&&Timer(game,"routeLensTimer")==0,"Paused collision callbacks neither grant nor consume either pickup");
        Call(game,"ResumeFlight");Call(game,"CollectCrystalPickup",resumeCrystal);Call(game,"CollectPowerUp",pausedPower);
        Check(Count(game,"crystals")==wallet+2&&Timer(game,"routeLensTimer")==8,"Resume accepts one still-live crystal and Lens pickup");

        // Bonus crystals are a bounded pickup award, never an input to a second percentage reward.
        Fresh(game);var levels=(IDictionary)Get(game,"upgradeLevels");levels["salvage_codec"]=3;levels["apex_matrix"]=3;
        var genome=new BirdGenome{id="qa",appearance="neon_finch",primary=(int)BirdTrait.Salvager,secondary=(int)BirdTrait.Collector,primaryGrade=3,level=5};
        Set(game,"flightBirdGenome",genome);Set(game,"score",10);Set(game,"runCrystalsCollected",106);Set(game,"runEchoPickupBonus",6);Set(game,"perfectPasses",0);Set(game,"newBest",false);Set(game,"resultCrystalBonusApplied",false);
        int expectedTech=Mathf.FloorToInt(100*(float)Call(game,"SalvageCodecBonusFraction")+.0001f)+Mathf.FloorToInt(100*(float)Call(game,"ApexMatrixBonusFraction")+.0001f);
        int expectedBird=SkyPulseBirdProgression.ResultBonus(genome,100,10,0);wallet=Count(game,"crystals");
        Call(game,"ApplyResultTechBonuses");
        Check(Count(game,"runTechResultBonus")==expectedTech&&Count(game,"runBirdResultBonus")==expectedBird&&Count(game,"crystals")==wallet+expectedTech+expectedBird,"Tech and bird percentage rewards exclude all Echo awards from their base");
        Call(game,"ApplyResultTechBonuses");Call(game,"RefreshProgressionResultLabels");
        Check(Count(game,"crystals")==wallet+expectedTech+expectedBird,"Repeated result refresh cannot pay Echo or other rewards twice");levels.Clear();

        foreach(int fps in new[]{30,60,120})
        {
            Fresh(game);CollectPower(game,"CrystalEcho");CollectPower(game,"RouteLens");
            foreach(var pipe in (Array)Get(game,"pipePool")){Set(pipe,"X",50f+(float)Get(pipe,"X"));((GameObject)Get(pipe,"Root")).transform.localPosition=new Vector3((float)Get(pipe,"X"),0,0);}
            for(int frame=0;frame<fps*2;frame++)
            {
                for(int substep=0;substep<120/fps;substep++)
                {
                    if(Timer(game,"birdY")<-.8f&&Timer(game,"birdVelocity")<0)Call(game,"Flap");
                    Call(game,"SimulateFlight",1f/120);
                }
                Call(game,"LateUpdate");
            }
            Check(Get(game,"state").ToString()=="Playing","Normal flap-driven production simulation remains in flight at "+fps+" rendered Hz");
            Check(Mathf.Abs(Timer(game,"crystalEchoTimer")-4f)<.0003f&&Mathf.Abs(Timer(game,"routeLensTimer")-6f)<.0003f,"Two seconds through production simulation and late presentation agree at "+fps+" rendered Hz (correctness, not measured device FPS)");
            Call(game,"PauseFlight");float e=Timer(game,"crystalEchoTimer"),l=Timer(game,"routeLensTimer");
            Call(game,"TickEndlessPickupBoosts",2f);Call(game,"Update");
            Check(Timer(game,"crystalEchoTimer")==e&&Timer(game,"routeLensTimer")==l,"Pause freezes both real timers at "+fps+" Hz");
            Call(game,"ResumeFlight");Call(game,"TickEndlessPickupBoosts",20f);
            Check(Timer(game,"crystalEchoTimer")==0&&Timer(game,"routeLensTimer")==0&&Count(game,"crystalEchoBudget")==0,"Both effects expire without negative time or retained budget at "+fps+" Hz");
        }
        Fresh(game);CollectPower(game,"CrystalEcho");CollectPower(game,"RouteLens");
        Call(game,"OnApplicationFocus",false);float focusEcho=Timer(game,"crystalEchoTimer"),focusLens=Timer(game,"routeLensTimer");Call(game,"Update");Call(game,"OnApplicationFocus",true);
        Check(Get(game,"state").ToString()=="Paused"&&Timer(game,"crystalEchoTimer")==focusEcho&&Timer(game,"routeLensTimer")==focusLens,"Focus loss preserves both effects and requires explicit resume");
        Call(game,"ResumeFlight");Call(game,"RestartFlight");
        Check(Timer(game,"crystalEchoTimer")==0&&Timer(game,"routeLensTimer")==0&&Count(game,"crystalEchoBudget")==0&&Count(game,"runEchoPickupBonus")==0,"Retry resets timers, charge budget and per-run Echo accounting");
        Lens(game);ModeIsolation(game);yield return Guide(game);
        Fresh(game);CollectPower(game,"CrystalEcho");CollectPower(game,"RouteLens");Call(game,"ResetToMenu");
        Check(Timer(game,"crystalEchoTimer")==0&&Timer(game,"routeLensTimer")==0&&Count(game,"runEchoPickupBonus")==0,"Returning Home leaves no hidden effect active");
        foreach(string kind in new[]{"Aegis","TimePulse","CrystalMagnet"})
        {
            Fresh(game);CollectPower(game,kind);
            Check(kind=="Aegis"?Count(game,"shieldCharges")==1:Timer(game,kind=="TimePulse"?"slowFieldTimer":"magnetHaloTimer")== (kind=="TimePulse"?4:6),"Existing "+kind+" effect retains its original behaviour");
        }
        Call(game,"ResetToMenu");
        var stamps=Directory.GetFiles("Assets/Scripts","*.cs").Where(p=>Path.GetFileName(p).StartsWith("SkyPulseNativeGame",StringComparison.Ordinal)).OrderBy(p=>p,StringComparer.Ordinal).Select(p=>new Stamp{path=p,sha256=Hash(p)}).ToArray();
        File.WriteAllText(Path.Combine(Output,"PICKUP-VALIDATION.json"),JsonUtility.ToJson(new Report{result="PASS",scope="Actual runtime handlers exercised in isolated Unity Play mode. Bounded rewards, real physical pickup contact, callback idempotence, production simulation timers, pause/retry/mode isolation, pooled visual correctness, warmed allocations and actual guide UI raycast/textfit at three portrait sizes. 30/60/120 Hz are simulated correctness samples; visual appearance and device performance remain unverified.",deviceTested=false,checks=checks.ToArray(),sources=stamps},true));
        Debug.Log("SKYPULSE_PICKUP_PASS: "+checks.Count+" assertions; "+Output);EditorApplication.Exit(0);
    }
    static void InspectIcon(SkyPulseNativeGame game,string kind)
    {
        var pickup=Item(game,"powerUpPool");var sprite=(Sprite)Call(game,"PowerUpArtwork",EnumValue(pickup,"Kind",kind));
        var target=RenderTexture.GetTemporary(sprite.texture.width,sprite.texture.height,0,RenderTextureFormat.ARGB32);
        var previous=RenderTexture.active;Graphics.Blit(sprite.texture,target);RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
        var pixels=image.GetPixels32();int visible=pixels.Count(c=>c.a>=128);int lit=pixels.Count(c=>c.a>=128&&Math.Max(c.r,Math.Max(c.g,c.b))>=128);
        Check(visible>pixels.Length*.05f&&lit>pixels.Length*.02f,"Rendered "+kind+" emblem has readable opaque material coverage, not a nearly transparent placeholder");
        Check(pixels[0].a<8&&pixels[target.width-1].a<8&&pixels[pixels.Length-1].a<8,"Rendered "+kind+" emblem retains transparent outer corners");
        File.WriteAllBytes(Path.Combine(Output,"icon-"+kind.ToLowerInvariant()+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
    }
    static void PhysicalCollection(SkyPulseNativeGame game)
    {
        var previous=UnityEngine.Random.state;UnityEngine.Random.InitState(4119);
        foreach(string kind in new[]{"CrystalEcho","RouteLens"})
        {
            Fresh(game);Set(game,"reduceMotionEnabled",true);var pipe=Gate(game);var pickup=Item(game,"powerUpPool");
            Set(pipe,"X",-2.45f);Set(pipe,"GapCenter",Timer(game,"birdY"));Set(pipe,"Passed",false);
            ((GameObject)Get(pipe,"Root")).transform.localPosition=new Vector3(-2.45f,0,0);
            int attempts=0;do{Call(game,"ConfigurePowerUp",pickup,pipe);attempts++;}while(Get(pickup,"Kind").ToString()!=kind&&attempts<200);
            Check(Get(pickup,"Kind").ToString()==kind,"Normal random spawn supplies "+kind+" for contact-path test");
            Set(pickup,"GapOffset",0f);Call(game,"UpdatePowerUps",1f/120);
            string timer=kind=="CrystalEcho"?"crystalEchoTimer":"routeLensTimer";
            Check(!(bool)Get(pickup,"Active")&&Timer(game,timer)==(kind=="CrystalEcho"?6:8),"Actual playable-area distance collection activates "+kind+" without a button");
            Call(game,"TickEndlessPickupBoosts",.25f);float remaining=Timer(game,timer);Call(game,"UpdatePowerUps",1f/120);
            Check(Timer(game,timer)==remaining,"Next physical update cannot recollect "+kind);
            if(kind=="CrystalEcho")
            {
                var crystal=Crystal(game);Set(crystal,"GapOffset",0f);int wallet=Count(game,"crystals");Call(game,"UpdateCrystalPickups",1f/120);
                Check(!(bool)Get(crystal,"Active")&&Count(game,"crystals")==wallet+2&&Count(game,"runEchoPickupBonus")==1,"Actual crystal contact banks one base plus one Echo crystal");
                Call(game,"UpdateCrystalPickups",1f/120);Check(Count(game,"crystals")==wallet+2,"Consecutive contact updates cannot double-pay the collected crystal");
            }
        }
        UnityEngine.Random.state=previous;Set(game,"reduceMotionEnabled",false);
    }
    static IEnumerator Guide(SkyPulseNativeGame game)
    {
        Call(game,"ResetToMenu");Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);int wallet=Count(game,"crystals");
        var camera=(Camera)Get(game,"flightCamera");var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();
        var priorMode=canvas.renderMode;var priorCamera=canvas.worldCamera;var priorTarget=camera.targetTexture;var priorRect=camera.rect;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f;
        var root=(GameObject)Get(game,"flightGuideScreen");
        foreach(var size in new[]{new Vector2Int(640,1136),new Vector2Int(1320,2868),new Vector2Int(1640,2360)})
        {
            var target=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);
            Call(game,"RefreshViewportDecor");Call(game,"OpenWorldGuide");
            foreach(string group in new[]{"SURVIVAL","DISCOVERY"})
            {
                var button=root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text==group);
                var other=root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text==(group=="SURVIVAL"?"DISCOVERY":"SURVIVAL"));
                other.onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();camera.Render();
                Vector2 at=RectTransformUtility.WorldToScreenPoint(camera,((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center));
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=at},hits);
                Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject,"Actual guide raycast reaches "+group+" tab at "+size);
                Check((bool)Call(game,"InteractiveUiAt",at),"Guide tab is interactive UI and cannot become a gameplay tap at "+size);
                ExecuteEvents.Execute<IPointerClickHandler>(button.gameObject,new PointerEventData(EventSystem.current){position=at,button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
                yield return null;Canvas.ForceUpdateCanvases();
                string[] expected=group=="SURVIVAL"?new[]{"AEGIS","TIME PULSE","CRYSTAL MAGNET"}:new[]{"CRYSTAL ECHO","ROUTE LENS"};
                var labels=root.GetComponentsInChildren<Text>();
                var rects=new List<Rect>();
                foreach(var control in root.GetComponentsInChildren<Button>())
                {
                    var rect=(RectTransform)control.transform;var graphic=control.GetComponent<Image>();var pad=graphic.raycastPadding;
                    Vector2 lo=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(new Vector3(rect.rect.xMin+pad.x,rect.rect.yMin+pad.y,0)));
                    Vector2 hi=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(new Vector3(rect.rect.xMax-pad.z,rect.rect.yMax-pad.w,0)));
                    float pixelScale=size.x==1320?3f:2f;
                    Check(Mathf.Abs(hi.x-lo.x)/pixelScale>=43.9f&&Mathf.Abs(hi.y-lo.y)/pixelScale>=43.9f,"Guide effective touch target is at least 44 logical points: "+control.GetComponentInChildren<Text>().text+" / "+size+" = "+(Mathf.Abs(hi.x-lo.x)/pixelScale)+" × "+(Mathf.Abs(hi.y-lo.y)/pixelScale));
                    var area=Rect.MinMaxRect(Mathf.Min(lo.x,hi.x),Mathf.Min(lo.y,hi.y),Mathf.Max(lo.x,hi.x),Mathf.Max(lo.y,hi.y));
                    var probe=new Vector2(area.center.x,area.yMax-2);var probeHits=new List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=probe},probeHits);
                    Check(probeHits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(probeHits[0].gameObject)==control.gameObject,"Padded guide touch edge reaches the correct control: "+control.GetComponentInChildren<Text>().text+" / "+size);
                    Check(rects.All(otherArea=>!otherArea.Overlaps(area)),"Guide touch areas do not overlap: "+control.GetComponentInChildren<Text>().text+" / "+size);rects.Add(area);
                }
                Check(expected.All(text=>labels.Any(t=>t.text==text)),"Guide group lists every "+group+" pickup at "+size);
                var oversized=labels.Where(t=>!t.resizeTextForBestFit&&t.preferredHeight>t.rectTransform.rect.height+2).ToArray();
                Check(oversized.Length==0,"All visible world/pickup guide lettering fits its rectangle at "+size+" / "+group+"; overflow="+string.Join(" | ",oversized.Select(t=>t.text+" preferred="+t.preferredHeight+" height="+t.rectTransform.rect.height)));
                var icons=root.GetComponentsInChildren<Image>().Where(i=>i.name=="Power-up emblem").ToArray();
                Check(icons.Length==expected.Length&&icons.All(i=>i.sprite!=null&&!i.raycastTarget),"Guide shows distinct cached non-blocking emblems at "+size+" / "+group);
                foreach(var stage in UnityEngine.Object.FindObjectsByType<SkyPulseDepthStage>())stage.Render();
                Save(camera,target,Path.Combine(Output,"guide-"+group.ToLowerInvariant()+"-"+size.x+".png"));
            }
            root.SetActive(false);camera.targetTexture=priorTarget;RenderTexture.ReleaseTemporary(target);
        }
        canvas.renderMode=priorMode;canvas.worldCamera=priorCamera;camera.targetTexture=priorTarget;camera.rect=priorRect;
        Check(Count(game,"crystals")==wallet,"Browsing both pickup guide groups never spends or rewards crystals");
    }
    static void Save(Camera camera,RenderTexture target,string path)
    {
        camera.Render();var prior=RenderTexture.active;RenderTexture.active=target;
        var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();
        File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);RenderTexture.active=prior;
    }
    static void ModeIsolation(SkyPulseNativeGame game)
    {
        Fresh(game);CollectPower(game,"CrystalEcho");CollectPower(game,"RouteLens");int budget=Count(game,"crystalEchoBudget"),wallet=Count(game,"crystals");
        var crystal=Crystal(game);var pickup=Power(game,"CrystalEcho");Set(game,"activeMode",SkyPulseMode.PulseRelay);
        Call(game,"TickEndlessPickupBoosts",2f);Check((int)Call(game,"TakeCrystalEchoBonus")==0&&Count(game,"crystalEchoBudget")==budget,"Relay cannot consume a stale Endless Echo charge");
        Call(game,"CollectCrystalPickup",crystal);Call(game,"CollectPowerUp",pickup);Check(Count(game,"crystals")==wallet&&(bool)Get(crystal,"Active")&&(bool)Get(pickup,"Active"),"Relay rejects stale Endless crystal and power-up callbacks");
        Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);var relay=(RelaySession)Get(game,"relaySession");relay.Collect(0);relay.Collect(1);
        Check(relay.IsOpen(0)&&relay.Cells==2&&relay.Charges==0&&Timer(game,"crystalEchoTimer")==0&&Timer(game,"routeLensTimer")==0,"Relay still opens exactly its assigned gate from its own two crystals with no Endless effects");
        Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);var blackout=(BlackoutSession)Get(game,"blackoutSession");int trace=blackout.Trace;
        Call(game,"TickEndlessPickupBoosts",8f);Check((int)Call(game,"TakeCrystalEchoBonus")==0&&blackout.Trace==trace,"Blackout puzzle resources are unaffected by either flight pickup");
    }
    static void Lens(SkyPulseNativeGame game)
    {
        Fresh(game);CollectPower(game,"RouteLens");var pipes=(Array)Get(game,"pipePool");int count=pipes.Length;
        var roots=new Transform[count];var bodySizes=new Vector2[count*2];
        for(int i=0;i<count;i++)
        {
            var p=pipes.GetValue(i);Set(p,"X",1.5f+i*5f);Set(p,"GapCenter",-.9f+i*.3f);Set(p,"GapHeight",4.5f);Set(p,"Passed",false);
            var root=(GameObject)Get(p,"Root");root.SetActive(true);root.transform.localPosition=new Vector3((float)Get(p,"X"),0,0);
            roots[i]=(Transform)Get(p,"LensGuide");Check(roots[i]!=null&&((Array)Get(p,"LensEdges")).Length==8,"Each reusable pipe has eight pooled Lens edges: "+i);
            Check(roots[i].GetComponentsInChildren<Collider>(true).Length==0&&roots[i].GetComponentsInChildren<Collider2D>(true).Length==0,"Lens marker adds no 3D or 2D colliders: "+i);
            bodySizes[i*2]=((BoxCollider2D)Get(Get(p,"Top"),"BodyCollider")).size;bodySizes[i*2+1]=((BoxCollider2D)Get(Get(p,"Bottom"),"BodyCollider")).size;
        }
        Call(game,"UpdateRouteLensVisuals");
        Check(roots.Count(t=>t.gameObject.activeSelf)==Math.Min(2,count),"Lens highlights only the two nearest eligible upcoming gaps");
        for(int i=0;i<Math.Min(2,count);i++)
        {
            var p=pipes.GetValue(i);Check(Mathf.Abs(roots[i].position.x-((GameObject)Get(p,"Root")).transform.position.x)<.001f&&Mathf.Abs(roots[i].position.y-(float)Get(p,"GapCenter"))<.001f,"Lens marker follows its actual gap without changing the path: "+i);
        }
        var first=pipes.GetValue(0);Set(first,"GapCenter",1.15f);Set(first,"X",2.35f);((GameObject)Get(first,"Root")).transform.localPosition=new Vector3(2.35f,0,0);Call(game,"LateUpdate");
        Check(Mathf.Abs(roots[0].position.x-2.35f)<.001f&&Mathf.Abs(roots[0].position.y-1.15f)<.001f,"Production LateUpdate tracks final pipe drift and horizontal movement without a stale gap marker");
        Set(first,"Passed",true);Call(game,"UpdateRouteLensVisuals");Check(!roots[0].gameObject.activeSelf&&roots.Count(t=>t.gameObject.activeSelf)==Math.Min(2,count-1),"A passed gate cannot keep a stale marker; the next gap is selected");
        Set(game,"worldTransitionTimer",.5f);Call(game,"UpdateRouteLensVisuals");Check(roots.All(t=>!t.gameObject.activeSelf),"Lens hides during a world transition rather than showing obsolete gates");Set(game,"worldTransitionTimer",0f);
        Call(game,"UpdateRouteLensVisuals");Set(game,"reduceMotionEnabled",true);Call(game,"UpdateRouteLensVisuals");
        var edges=roots.SelectMany(t=>t.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();var positions=edges.Select(t=>t.transform.localPosition).ToArray();var scales=edges.Select(t=>t.transform.localScale).ToArray();var colours=edges.Select(t=>t.color).ToArray();
        Set(game,"ambientTime",200f);Call(game,"UpdateRouteLensVisuals");
        Check(edges.Select((t,i)=>t.transform.localPosition==positions[i]&&t.transform.localScale==scales[i]&&t.color==colours[i]).All(b=>b),"Reduced Motion keeps Lens visual geometry and brightness steady");Set(game,"reduceMotionEnabled",false);
        Call(game,"PauseFlight");Call(game,"UpdateRouteLensVisuals");Check(roots.Count(t=>t.gameObject.activeSelf)==Math.Min(2,count-1),"Pause preserves readable markers while freezing effect time");Call(game,"ResumeFlight");
        for(int i=0;i<count;i++)
        {var p=pipes.GetValue(i);Check(((BoxCollider2D)Get(Get(p,"Top"),"BodyCollider")).size==bodySizes[i*2]&&((BoxCollider2D)Get(Get(p,"Bottom"),"BodyCollider")).size==bodySizes[i*2+1],"Lens never changes existing pipe collider dimensions: "+i);}
        var animate=(Action)Delegate.CreateDelegate(typeof(Action),game,Method(game,"UpdateRouteLensVisuals",0));
        for(int i=0;i<10;i++)animate();long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<300;i++)animate();long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Check(allocated==0,"Warmed pooled Lens visual update allocates zero managed bytes across 300 ticks");
        Call(game,"RestartFlight");for(int i=0;i<count;i++)Check(ReferenceEquals(roots[i],Get(pipes.GetValue(i),"LensGuide"))&&!roots[i].gameObject.activeSelf,"Retry reuses the existing marker objects without duplicates: "+i);
    }
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    public static void Fail(Exception e)
    {Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"FAILURE.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
}
public sealed class SkyPulsePickupCheckRunner:MonoBehaviour
{
    public void Begin(IEnumerator routine){StartCoroutine(Guard(routine));}
    IEnumerator Guard(IEnumerator routine)
    {
        while(true)
        {
            bool more;object next;
            try{more=routine.MoveNext();next=more?routine.Current:null;}
            catch(Exception e){SkyPulsePickupChecks.Fail(e);yield break;}
            if(!more)yield break;
            if(next is IEnumerator nested)yield return Guard(nested);else yield return next;
        }
    }
}
