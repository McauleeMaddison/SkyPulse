// Disposable Editor QA only. Actual input handlers, production updates and camera callbacks;
// no owner save, phone build, forced preview time, or Studio-only animation pass.
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
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulseFlapChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    const string Pending="SkyPulse.FlapQA.Pending",Baseline="SkyPulse.FlapQA.Baseline";
    static readonly List<string> checks=new List<string>();
    static readonly List<MotionCase> cases=new List<MotionCase>();
    [Serializable] sealed class Stamp{public string path,sha256;}
    [Serializable] sealed class MotionCase
    {public string mode,bird;public bool reducedMotion;public int acceptedTaps,nativePoses,samples;public float minWing,maxWing,wingRange,activeSeconds,minTipPixels,maxTipPixels,projectedWingRange;public string[] captures;}
    [Serializable] sealed class Report
    {public string result,scope;public bool deviceTested;public string[] checks;public MotionCase[] motion;public Stamp[] sources;}
    static string Output=>Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT")??Application.temporaryCachePath,SessionState.GetBool(Baseline,false)?"flap-baseline":"flap-fixed");
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static MethodInfo Method(object o,string n,int argc)=>o.GetType().GetMethods(F).Single(m=>m.Name==n&&m.GetParameters().Length==argc);
    static object Call(object o,string n,params object[] args)=>Method(o,n,args.Length).Invoke(o,args);
    static object Static(string n,params object[] args)=>typeof(SkyPulseBirdRosterPreview).GetMethods(S).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(null,args);
    static float Clock=>(float)typeof(SkyPulseBirdRosterPreview).GetProperty("CurrentFlightTime",S).GetValue(null);
    static void Check(bool value,string text){if(!value)throw new Exception(text);checks.Add(text);Debug.Log("FLAP_CHECK: "+text);}
    static float Float(SkyPulseNativeGame g,string n)=>(float)Get(g,n);
    static void Guard()
    {if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006"||Application.productName!="SkyPulseFlapQA20261110")throw new Exception("Refusing flap/save QA outside its disposable batch identity");}
    static SkyPulseFlapChecks(){EditorApplication.update+=Tick;}
    public static void RunBaseline(){Begin(true);}
    public static void Run(){Begin(false);}
    static void Begin(bool baseline)
    {
        Guard();checks.Clear();cases.Clear();SessionState.SetBool(Baseline,baseline);Directory.CreateDirectory(Output);
        foreach(string file in new[]{"VALIDATION.json","FAILURE.txt"})if(File.Exists(Path.Combine(Output,file)))File.Delete(Path.Combine(Output,file));
        PlayerPrefs.DeleteAll();PlayerPrefs.Save();EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool(Pending,false);new GameObject("Flap regression QA").AddComponent<SkyPulseFlapCheckRunner>().Begin(Execute());
    }
    static void Start(SkyPulseNativeGame g,SkyPulseMode mode,string bird,bool reduced)
    {
        g.enabled=false;SkyPulseBirdRosterPreview.Select("",false);
        Call(g,"ResetToMenu");Set(g,"equippedSkin",Call(g,"FindBird",bird));Set(g,"reduceMotionEnabled",reduced);
        if(mode==SkyPulseMode.EndlessRoute)Call(g,"StartFlight");else Call(g,"LaunchMission",mode,0);
        Set(g,"hapticsEnabled",false);((AudioSource)Get(g,"audioSource")).mute=true;
        // Keep the collision-checked simulation alive while observing animation.
        // This is a QA-only fixture, not a tuning change or gameplay demonstration.
        Set(g,"shieldImmunityTimer",100f);
        if(mode==SkyPulseMode.EndlessRoute)
        {
            foreach(var pair in (Array)Get(g,"pipePool"))
            {Set(pair,"X",100f+(float)Get(pair,"X"));((GameObject)Get(pair,"Root")).transform.localPosition=new Vector3((float)Get(pair,"X"),0,0);}
        }
        else
        {
            var relay=(RelaySession)Get(g,"relaySession");for(int i=0;i<relay.Mission.RelayCount*2;i++)relay.Collect(i);
        }
    }
    static bool Press(SkyPulseNativeGame g,SkyPulseMode mode,int frame,float now,Camera camera)
    {
        if(mode==SkyPulseMode.EndlessRoute)
        {Call(g,"BufferFlapInput");return Float(g,"bufferedFlapUntil")>=Time.unscaledTime;}
        Call(g,"ReleaseGameplayPointer",61);
        return (bool)Call(g,"SubmitRelayPress",61,frame,now,true,camera.pixelRect.center);
    }
    static IEnumerator Execute()
    {
        Guard();var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();
        if(game==null)game=new GameObject("QA game").AddComponent<SkyPulseNativeGame>();
        game.enabled=false;var camera=(Camera)Get(game,"flightCamera");
        var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f;
        var target=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);
        Call(game,"RefreshViewportDecor");Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();
        bool baseline=SessionState.GetBool(Baseline,false);
        if(baseline)
        {
            yield return RealMotion(game,camera,target,SkyPulseMode.EndlessRoute,"chrome_raven",false,true);
            Check(cases[0].acceptedTaps>=8&&cases[0].nativePoses>=4,"Baseline reaches native lift/wing animation through at least eight accepted taps");
            Check(cases[0].wingRange<35,"Old camera hook reproduces an incomplete 3D stroke under repeated taps: range="+cases[0].wingRange);
            Write("REPRODUCED","Historical source baseline: real buffered taps, automatic production Update/LateUpdate and Camera.onPreCull. Only QA safety fixtures alter the course; no forced pose or preview timestamp. This demonstrates the pre-fix incomplete stroke, not a visual approval or device result.");
            Debug.Log("SKYPULSE_FLAP_BASELINE_REPRODUCED");EditorApplication.Exit(0);yield break;
        }
        ClockContract();NativeFrames(game,camera);
        foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay})
        {
            yield return RealMotion(game,camera,target,mode,"chrome_raven",false,false);
            yield return RealMotion(game,camera,target,mode,"neon_finch",true,false);
        }
        yield return InputSeparation(game,camera);
        SkyPulseBirdRosterPreview.Select("",false);game.enabled=false;Call(game,"ResetToMenu");camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
        Write("PASS","Actual native tap handlers, deterministic 30/60/120 Hz controller/simulation cases and real Unity Play-mode automatic updates + camera callbacks. Normal and Reduced Motion flight, repeated taps, pause/resume, retry and UI fencing are exercised without forced preview timestamps. Native controller sampling frequencies are correctness tests, not measured device FPS; appearance approval and mobile performance remain pending.");
        Debug.Log("SKYPULSE_FLAP_FIXED_PASS: "+checks.Count+" assertions; "+Output);EditorApplication.Exit(0);
    }
    static void ClockContract()
    {
        foreach(int hz in new[]{30,60,120})
        {
            Static("ResetFlightClock");float value=(float)Static("AdvanceFlightClock",0,1f/hz,true);
            Check(value==0,"Clock establishes a zero epoch before the first rendered frame at "+hz+" Hz");
            for(int frame=1;frame<=hz*2;frame++)
            {
                value=(float)Static("AdvanceFlightClock",frame,1f/hz,true);
                Check((float)Static("AdvanceFlightClock",frame,1f/hz,true)==value,"Repeated camera pass cannot advance the same frame twice: "+hz+" / "+frame);
            }
            Check(Mathf.Abs(value-2f)<.0001f,"Two active seconds agree at "+hz+" Hz");
            Check((float)Static("AdvanceFlightClock",hz*2+1,10f,false)==value,"Pause holds animation without spending wall-clock time at "+hz+" Hz");
            Check((float)Static("AdvanceFlightClock",hz*2+2,10f,true)==value,"First resumed frame preserves the held pose at "+hz+" Hz");
            float next=(float)Static("AdvanceFlightClock",hz*2+3,1f/hz,true);
            Check(Mathf.Abs(next-value-1f/hz)<.00001f,"Resume advances only the new active delta at "+hz+" Hz");
        }
        Static("ResetFlightClock");Static("AdvanceFlightClock",0,0f,true);
        Check((float)Static("AdvanceFlightClock",1,float.NaN,true)==0&&(float)Static("AdvanceFlightClock",2,-1f,true)==0,"Invalid and negative elapsed values cannot poison the clock");
        Check((float)Static("AdvanceFlightClock",3,10f,true)<=.10001f,"A render hitch is bounded instead of skipping a full wing stroke");
        Static("ResetFlightClock");
    }
    static void NativeFrames(SkyPulseNativeGame g,Camera camera)
    {
        int pressFrame=Time.frameCount+20;
        foreach(int hz in new[]{30,60,120})foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay})
        {
            Start(g,mode,"neon_finch",false);var renderer=(SpriteRenderer)Get(g,"birdRenderer");var indices=new List<int>{(int)Get(g,"activeFlapFrameIndex")};var sprites=new HashSet<Sprite>{renderer.sprite};
            Check(Press(g,mode,pressFrame++,Time.unscaledTime,camera),"A deliberate native input is accepted in "+mode+" / "+hz+" Hz");
            for(int frame=0;frame<hz*.7f;frame++)
            {
                for(int step=0;step<120/hz;step++)Call(g,mode==SkyPulseMode.EndlessRoute?"SimulateFlight":"SimulateRelay",1f/120);
                Call(g,"UpdateGameplayWingState",1f/hz);Call(g,"UpdateBirdWingMotion");
                int index=(int)Get(g,"activeFlapFrameIndex");if(indices[indices.Count-1]!=index)indices.Add(index);sprites.Add(renderer.sprite);
            }
            Check(indices.SequenceEqual(new[]{5,4,3,2,1,0,1,2,3,4,5})&&sprites.Count==6,"Accepted tap traverses all six original authored poses and returns smoothly: "+mode+" / "+hz+" Hz; actual="+string.Join(",",indices));
            Check(Float(g,"birdY")!=0&&Get(g,"state").ToString()=="Playing","The same tap drives real flight physics while the original wing frames advance: "+mode+" / "+hz+" Hz");
        }
    }
    static IEnumerator RealMotion(SkyPulseNativeGame g,Camera camera,RenderTexture target,SkyPulseMode mode,string bird,bool reduced,bool baseline)
    {
        Start(g,mode,bird,reduced);SkyPulseBirdRosterPreview.Select(bird,true);SkyPulseBirdRosterPreview.TimeOverride=-1;
        Set(g,"birdY",-3.8f);Set(g,"birdVelocity",0f);Call(g,"UpdateBird",0f);
        g.enabled=true;yield return null;
        var report=new MotionCase{mode=mode.ToString(),bird=bird,reducedMotion=reduced,minWing=float.MaxValue,maxWing=float.MinValue,minTipPixels=float.MaxValue,maxTipPixels=float.MinValue};
        var sprites=new HashSet<Sprite>();var captures=new List<string>();var sprite=(SpriteRenderer)Get(g,"birdRenderer");Quaternion bind=Quaternion.identity;bool bound=false;VertexLandmark landmark=default;
        float started=Time.unscaledTime,nextTap=started+.05f,nextCapture=started+.20f;int capture=0;
        while(Time.unscaledTime-started<2.15f)
        {
            if(Time.unscaledTime>=nextTap)
            {
                if(Press(g,mode,Time.frameCount,Time.unscaledTime,camera))report.acceptedTaps++;
                nextTap=Time.unscaledTime+.13f;
            }
            camera.Render();var rig=SkyPulseBirdRosterPreview.CurrentRig;CheckOnceRig(rig,bird);
            if(!bound){bind=rig.leftWing.localRotation;landmark=NearWingTip(rig,camera);bound=true;}
            float tipPixels=camera.WorldToScreenPoint(landmark.owner.TransformPoint(landmark.point)).y-camera.WorldToScreenPoint(rig.body.position).y;
            report.minTipPixels=Mathf.Min(report.minTipPixels,tipPixels);report.maxTipPixels=Mathf.Max(report.maxTipPixels,tipPixels);
            float x=Mathf.DeltaAngle(0,(Quaternion.Inverse(bind)*rig.leftWing.localRotation).eulerAngles.x);
            report.minWing=Mathf.Min(report.minWing,x);report.maxWing=Mathf.Max(report.maxWing,x);report.samples++;sprites.Add(sprite.sprite);
            if(Time.unscaledTime>=nextCapture&&capture<5)
            {
                string file=(baseline?"baseline":"fixed")+"-"+mode+"-"+bird+(reduced?"-reduced":"")+"-"+capture.ToString("00")+".png";
                Save(target,Path.Combine(Output,file));captures.Add(file);
                string crop=file.Replace(".png","-bird.png");SaveBirdCrop(camera,target,(Transform)Get(g,"bird"),Path.Combine(Output,crop));captures.Add(crop);capture++;nextCapture=Time.unscaledTime+.36f;
            }
            Check(Get(g,"state").ToString()=="Playing","Observed real Play-mode frame remains in active flight: "+mode+" / "+bird);
            yield return null;
        }
        g.enabled=false;report.wingRange=report.maxWing-report.minWing;report.projectedWingRange=report.maxTipPixels-report.minTipPixels;report.nativePoses=sprites.Count;report.captures=captures.ToArray();report.activeSeconds=baseline?-1:Clock;cases.Add(report);
        Check(report.acceptedTaps>=8,"Real-time rapid press loop accepts at least eight deliberate taps: "+mode+" / "+bird);
        Check(report.nativePoses>=4,"Automatic native Update still changes the underlying authored wing art: "+mode+" / "+bird);
        if(baseline)yield break;
        Check(SkyPulseBirdRosterPreview.TimeOverride<0,"Real camera preview runs without any forced sampling timestamp");
        Check(report.wingRange>(reduced?28f:48f),"Real rendered 3D wings complete both half-strokes despite rapid input: "+mode+" / "+bird+"; span="+report.wingRange);
        Check(report.projectedWingRange>(reduced?6f:10f),"A real near-wing mesh vertex moves visibly relative to the bird in the rendered view: "+mode+" / "+bird+"; pixels="+report.projectedWingRange);
        Check(report.activeSeconds>1.6f,"Actual camera callback advances over a whole live flight cycle: "+mode+" / "+bird);
        var current=SkyPulseBirdRosterPreview.CurrentRig;float before=Clock;var held=Joints(current).Select(t=>t.localRotation).ToArray();
        Call(g,"PauseFlight");g.enabled=true;float pauseAt=Time.unscaledTime;
        while(Time.unscaledTime-pauseAt<.30f){camera.Render();yield return null;}
        Check(Clock==before&&Joints(current).Select((t,i)=>Quaternion.Angle(t.localRotation,held[i])<.01f).All(same=>same),"Pause holds every real 3D joint through repeated camera callbacks: "+mode+" / "+bird);
        Call(g,"ResumeFlight");camera.Render();Check(Clock==before,"Resume does not include time spent paused: "+mode+" / "+bird);
        yield return null;camera.Render();Check(Clock-before<=.1001f,"The first new rendered frame resumes smoothly without a phase jump: "+mode+" / "+bird);
        g.enabled=false;
        if(reduced)
        {
            var body=current.body.localRotation;var head=current.head.localRotation;var tail=current.tail.localRotation;
            g.enabled=true;float observe=Time.unscaledTime;while(Time.unscaledTime-observe<.20f){camera.Render();yield return null;}g.enabled=false;
            Check(Quaternion.Angle(body,current.body.localRotation)<.01f&&Quaternion.Angle(head,current.head.localRotation)<.01f&&Quaternion.Angle(tail,current.tail.localRotation)<.01f,"Reduced Motion retains the flight stroke while ambient body/head/tail remain still: "+mode);
        }
        if(mode==SkyPulseMode.EndlessRoute)Call(g,"RestartFlight");else Call(g,"LaunchMission",mode,0);
        camera.Render();Check(Clock==0,"Retry starts a new zero-phase flight independently of taps: "+mode+" / "+bird);
        SkyPulseBirdRosterPreview.Select("",false);
    }
    struct VertexLandmark { public Transform owner;public Vector3 point; }
    static VertexLandmark NearWingTip(SkyPulseNeonFinchPrototype rig,Camera camera)
    {
        var left=rig.leftWing;var right=rig.rightWing;
        var near=camera.WorldToScreenPoint(left.position).z<camera.WorldToScreenPoint(right.position).z?left:right;
        VertexLandmark result=default;float distance=-1;
        foreach(var filter in near.GetComponentsInChildren<MeshFilter>())
        {
            if(filter.sharedMesh==null)continue;
            foreach(var vertex in filter.sharedMesh.vertices)
            {
                float candidate=(filter.transform.TransformPoint(vertex)-rig.body.position).sqrMagnitude;
                if(candidate>distance){distance=candidate;result=new VertexLandmark{owner=filter.transform,point=vertex};}
            }
        }
        if(result.owner==null)throw new Exception("A near-wing mesh landmark is unavailable");return result;
    }
    static void SaveBirdCrop(Camera camera,RenderTexture target,Transform bird,string path)
    {
        const int size=280;Vector3 at=camera.WorldToScreenPoint(bird.position);int x=Mathf.Clamp(Mathf.RoundToInt(at.x)-size/2,0,target.width-size),y=Mathf.Clamp(Mathf.RoundToInt(at.y)-size/2,0,target.height-size);
        var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(size,size,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(x,y,size,size),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;
    }
    static void CheckOnceRig(SkyPulseNeonFinchPrototype rig,string bird)
    {if(rig==null||rig.leftWing==null||rig.rightWing==null)throw new Exception("Actual camera did not load an articulated rig: "+bird);}
    static Transform[] Joints(SkyPulseNeonFinchPrototype r)=>new[]{r.body,r.head,r.leftWing,r.rightWing,r.leftWingTip,r.rightWingTip,r.tail,r.tailTip};
    static IEnumerator InputSeparation(SkyPulseNativeGame g,Camera camera)
    {
        Start(g,SkyPulseMode.PulseRelay,"neon_finch",false);yield return null;Canvas.ForceUpdateCanvases();camera.Render();
        var hud=(GameObject)Get(g,"relayHudScreen");var pause=hud.GetComponentsInChildren<Button>().Single();
        Vector2 pauseAt=RectTransformUtility.WorldToScreenPoint(camera,pause.transform.position);float velocity=Float(g,"birdVelocity");
        Check((bool)Call(g,"InteractiveUiAt",pauseAt),"Pause is still recognized as interactive UI");
        Check(!(bool)Call(g,"SubmitRelayPress",81,Time.frameCount+1,Time.unscaledTime,true,pauseAt)&&Float(g,"birdVelocity")==velocity,"Pause-button touch does not trigger flight or wing animation");
        int frame=Time.frameCount+2;Call(g,"ReleaseGameplayPointer",81);
        Check((bool)Call(g,"SubmitRelayPress",81,frame,Time.unscaledTime+.1f,true,camera.pixelRect.center),"Playable-area touch still applies lift immediately");
        float lifted=Float(g,"birdVelocity");Check(lifted>velocity&&Float(g,"wingTimer")==0,"Accepted touch still starts the flight impulse and native wing state");
        Check(!(bool)Call(g,"SubmitRelayPress",-1,frame+1,Time.unscaledTime+.12f,false,camera.pixelRect.center),"Simulated mouse cannot duplicate the same deliberate touch");
        Call(g,"PauseFlight");Check(!(bool)Call(g,"SubmitRelayPress",82,frame+2,Time.unscaledTime+.2f,true,camera.pixelRect.center),"Paused gameplay ignores a fresh touch");
        Call(g,"ResumeFlight");
    }
    static void Save(RenderTexture target,string path)
    {
        var prior=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=prior;
    }
    static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    static void Write(string result,string scope)
    {
        var sources=new[]{"Assets/Scripts/SkyPulseNativeGame.cs","Assets/Scripts/SkyPulseNativeGame.Missions.cs","Assets/Scripts/SkyPulseNativeGame.Controls.cs","Assets/Scripts/SkyPulseNeonFinchPrototype.cs","Assets/Editor/SkyPulseBirdRosterPreview.cs"};
        File.WriteAllText(Path.Combine(Output,"VALIDATION.json"),JsonUtility.ToJson(new Report{result=result,scope=scope,deviceTested=false,checks=checks.ToArray(),motion=cases.ToArray(),sources=sources.Select(p=>new Stamp{path=p,sha256=Hash(p)}).ToArray()},true));
    }
    public static void Fail(Exception e)
    {Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"FAILURE.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
}
public sealed class SkyPulseFlapCheckRunner:MonoBehaviour
{
    public void Begin(IEnumerator routine){StartCoroutine(Guard(routine));}
    IEnumerator Guard(IEnumerator routine)
    {
        while(true)
        {
            bool more;object next;
            try{more=routine.MoveNext();next=more?routine.Current:null;}catch(Exception e){SkyPulseFlapChecks.Fail(e);yield break;}
            if(!more)yield break;if(next is IEnumerator nested)yield return Guard(nested);else yield return next;
        }
    }
}
