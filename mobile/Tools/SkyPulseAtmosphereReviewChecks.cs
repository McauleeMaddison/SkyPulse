// Disposable, batch-only main-game atmosphere review. Never included in a player.
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
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulseAtmosphereReviewChecks
{
    const string Key="SkyPulseAtmosphereReview20261104";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static readonly List<string> checks=new List<string>();
    static readonly List<Frame> frames=new List<Frame>();
    static readonly List<string> motionFiles=new List<string>(),motionHashes=new List<string>();
    static readonly List<string> inspectionFiles=new List<string>();
    static string Output=>Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT")??"/private/tmp","atmosphere-review");
    [Serializable] sealed class Frame
    {
        public string file,mode,state,world,sha256;
        public int worldIndex,width,height;
        public Vector3 lowerSkyMean,lowerSkyRange;
    }
    [Serializable] sealed class Report
    {
        public string result,date,scope,graphicsDevice,proof,limits,comparisonReference,failure;
        public string[] assertions,neonMotionFiles,neonMotionSha256,inspectionCaptures;
        public string neonMotionFolder,motionProof,inspectionProof;
        public int neonMotionFrames,neonMotionFps;
        public bool neonMotionCollisionChecked;
        public Frame[] captures;
        public bool deviceTested,shippingProgressChanged,routeWorldSelectedForInspection;
    }
    static SkyPulseAtmosphereReviewChecks(){EditorApplication.update+=Tick;}
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object value)=>o.GetType().GetField(n,F).SetValue(o,value);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);checks.Add(message);Debug.Log("ATMOSPHERE_REVIEW_CHECK: "+message);}
    static void GuardProduct(){if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new InvalidOperationException("Disposable SkyPulseQA20261006 batch product required");}
    public static void Run()
    {
        GuardProduct();Directory.CreateDirectory(Output);checks.Clear();frames.Clear();motionFiles.Clear();motionHashes.Clear();inspectionFiles.Clear();
        EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");
        PlayerPrefs.DeleteAll();PlayerPrefs.Save();SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool(Key,false);new GameObject("Main-game atmosphere inspection runner").AddComponent<SkyPulseAtmosphereReviewRunner>().Begin(Execute());
    }
    static string Digest(byte[] bytes)
    {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    static string PixelsHash(Color32[] pixels)
    {
        var raw=new byte[pixels.Length*3];for(int i=0;i<pixels.Length;i++){raw[i*3]=pixels[i].r;raw[i*3+1]=pixels[i].g;raw[i*3+2]=pixels[i].b;}return Digest(raw);
    }
    static Color32[] Read(RenderTexture target)
    {
        var prior=RenderTexture.active;Texture2D image=null;
        try{RenderTexture.active=target;image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();return image.GetPixels32();}
        finally{RenderTexture.active=prior;if(image!=null)UnityEngine.Object.Destroy(image);}
    }
    static void Fit(SkyPulseNativeGame g)
    {Canvas.ForceUpdateCanvases();Call(g,"ApplySafeArea");Call(g,"FitInterfaceToSafeArea");Call(g,"FitBlackoutToSafeArea");Canvas.ForceUpdateCanvases();}
    static void Present(SkyPulseNativeGame g,float dt)
    {
        Call(g,"UpdateMenuBird",dt);Call(g,"UpdateProtocolPresentation",dt);Call(g,"UpdateCinematicPresentation",dt);
        SkyPulseBirdRig.FlushPending();
    }
    static string Capture(SkyPulseNativeGame g,Camera camera,RenderTexture target,string file,string mode,string state,int index,string world,bool includeInReport=true)
    {
        Fit(g);
        foreach(var stage in UnityEngine.Object.FindObjectsByType<SkyPulseDepthStage>())stage.Render();
        foreach(var stage in UnityEngine.Object.FindObjectsByType<SkyPulseProgressionStage>())stage.Render();
        camera.Render();var prior=RenderTexture.active;Texture2D image=null;
        try
        {
            RenderTexture.active=target;image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();byte[] png=image.EncodeToPNG();File.WriteAllBytes(Path.Combine(Output,file),png);
            var pixels=image.GetPixels32();long r=0,green=0,b=0;int count=0;Vector3 min=Vector3.one*255,max=Vector3.zero;
            // Recorded descriptive samples, not an aesthetic acceptance test.
            // Central lower-air region avoids the left bird and edge pipe.
            for(int y=target.height/6;y<target.height*2/5;y+=3)for(int x=target.width*2/5;x<target.width*3/5;x+=3)
            {var c=pixels[y*target.width+x];r+=c.r;green+=c.g;b+=c.b;count++;var rgb=new Vector3(c.r,c.g,c.b);min=Vector3.Min(min,rgb);max=Vector3.Max(max,rgb);}
            if(state=="Playing")Check(SkyPulseBirdRosterPreview.CurrentRig!=null&&SkyPulseBirdRosterPreview.CurrentRig.gameObject.activeInHierarchy&&SkyPulseBirdRosterPreview.SelectedId=="neon_finch","Approved Finch 3D visual is active in actual flight capture: "+file);
            else Check(SkyPulseBirdRosterPreview.CurrentRig==null||!SkyPulseBirdRosterPreview.CurrentRig.gameObject.activeInHierarchy,"Flight-only Finch preview stays hidden on actual Home: "+file);
            if(includeInReport)frames.Add(new Frame{file=file,mode=mode,state=state,world=world,worldIndex=index,width=target.width,height=target.height,sha256=Digest(png),lowerSkyMean=new Vector3(r/(float)count,green/(float)count,b/(float)count)/255,lowerSkyRange=(max-min)/255});
            Check(pixels.Any(p=>p.r>32||p.g>32||p.b>32),"Actual main camera renders visible colour: "+file);return Digest(png);
        }
        finally{RenderTexture.active=prior;if(image!=null)UnityEngine.Object.Destroy(image);}
    }
    static void SaveWorldTarget(SkyPulseFlightEnvironment environment,string name)
    {
        environment.Render();var target=environment.WorldImage;var prior=RenderTexture.active;Texture2D image=null;
        try
        {
            RenderTexture.active=target;image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name),image.EncodeToPNG());inspectionFiles.Add(name);
            Check(image.GetPixels32().Any(p=>p.r>32||p.g>32||p.b>32),"Actual perspective-world feed renders visible architecture: "+name);
        }
        finally{RenderTexture.active=prior;if(image!=null)UnityEngine.Object.Destroy(image);}
    }
    static void InspectNeonArchitecture(SkyPulseFlightEnvironment environment)
    {
        SaveWorldTarget(environment,"world-00-unobstructed.png");
        var camera=environment.WorldCamera;Vector3 priorPosition=camera.transform.position;Quaternion priorRotation=camera.transform.rotation;
        var districts=(Transform[,])Get(environment,"scenery");var renderer=districts[1,4].GetComponent<MeshRenderer>();
        Check(renderer!=null&&renderer.gameObject.activeInHierarchy,"Neon civic landmark exists in the actual cached city scene");
        Bounds bounds=renderer.bounds;Vector3 eye=new Vector3(.34f,.14f,-1).normalized,right=Vector3.Cross(Vector3.up,-eye).normalized,up=Vector3.Cross(-eye,right).normalized;
        float tanVertical=Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f),tanHorizontal=tanVertical*camera.aspect,distance=1;
        for(int corner=0;corner<8;corner++)
        {
            Vector3 offset=new Vector3((corner&1)==0?-bounds.extents.x:bounds.extents.x,(corner&2)==0?-bounds.extents.y:bounds.extents.y,(corner&4)==0?-bounds.extents.z:bounds.extents.z);
            float depth=Vector3.Dot(offset,eye);
            distance=Mathf.Max(distance,depth+Mathf.Abs(Vector3.Dot(offset,right))/tanHorizontal,depth+Mathf.Abs(Vector3.Dot(offset,up))/tanVertical);
        }
        try
        {
            camera.transform.SetPositionAndRotation(bounds.center+eye*distance*1.10f,Quaternion.LookRotation(-eye,Vector3.up));
            SaveWorldTarget(environment,"world-00-architecture-inspection.png");
        }
        finally{camera.transform.SetPositionAndRotation(priorPosition,priorRotation);environment.Render();}
        Check(camera.transform.position==priorPosition&&camera.transform.rotation==priorRotation,"Inspection restores exact perspective-camera position/rotation before actual gameplay capture");
    }
    static IEnumerator CaptureNeonMotion(SkyPulseNativeGame game,Camera camera,RenderTexture target)
    {
        Directory.CreateDirectory(Path.Combine(Output,"neon-city-motion"));
        for(int frame=0;frame<90;frame++)
        {
            // Existing 120Hz simulation and buffered player action, just as in
            // the established route-style QA. No invulnerability, moved gates,
            // bypassed collision or altered flight forces are introduced here.
            for(int step=0;step<4;step++)
            {
                float nearest=float.MaxValue,gap=0;
                foreach(var pipe in (Array)Get(game,"pipePool")){float x=(float)Get(pipe,"X");if(!(bool)Get(pipe,"Passed")&&x<nearest){nearest=x;gap=(float)Get(pipe,"GapCenter");}}
                if((float)Get(game,"birdY")<gap-.55f&&(float)Get(game,"birdVelocity")<0)Call(game,"BufferFlapInput");
                Call(game,"SimulateFlight",1f/120);
            }
            Check(Get(game,"state").ToString()=="Playing","Neon motion remains in real collision-checked flight: "+frame+" / "+Get(game,"lastCrashReason"));
            const float dt=1f/30;
            Set(game,"ambientTime",(float)Get(game,"ambientTime")+dt);Call(game,"UpdateAmbientVisuals");Call(game,"UpdateScoreBurst",dt);Call(game,"UpdateFlightFeedback",dt);Call(game,"UpdateCrystalPickupBursts",dt);
            Call(game,"UpdateGameplayWingState",dt);Call(game,"UpdateBirdWingMotion");Call(game,"UpdateScoreFinish",dt);Present(game,dt);
            SkyPulseBirdRosterPreview.TimeOverride=frame/30f;
            string file="neon-city-motion/"+frame.ToString("000")+".png";
            string hash=Capture(game,camera,target,file,"EndlessRoute","Playing",0,"NEON CITY",false);motionFiles.Add(file);motionHashes.Add(hash);yield return null;
        }
        Check(motionFiles.Count==90&&motionHashes.Distinct().Count()>60,"Ninety distinct actual-game motion frames captured at30Hz");
        SkyPulseBirdRosterPreview.TimeOverride=.25f;
    }
    static void Shaders()
    {
        foreach(string name in new[]{"WorldAtmosphere","AtmosphereVeil","WorldComposite","FlightArchitecture"})
        {
            var shader=Resources.Load<Shader>("SkyPulse/Shaders/"+name);
            Check(shader!=null&&shader.isSupported,"Actual graphics device supports "+name);
            foreach(var error in ShaderUtil.GetShaderMessages(shader))Check(error.severity.ToString()!="Error","Shader import/render message: "+name+" / "+error.message);
        }
    }
    static void SelectWorld(SkyPulseNativeGame g,Array worlds,int index)
    {
        Set(g,"routeWorldIndex",index);Set(g,"routeWorld",worlds.GetValue(index));Set(g,"equippedWorld",worlds.GetValue(index));
        Set(g,"score",index*15);Set(g,"worldTransitionTimer",0f);Call(g,"ApplyRouteWorldVisuals");
        Set(g,"sceneryHomeTravel",0f);Set(g,"sceneryHasView",false);Set(g,"cinematicClock",2f);Call(g,"ResetFlightEnvironment");
    }
    static IEnumerator Execute()
    {
        GuardProduct();Directory.CreateDirectory(Output);Shaders();
        var g=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();if(g==null)g=new GameObject("QA main SkyPulse game").AddComponent<SkyPulseNativeGame>();
        g.enabled=false;Set(g,"hapticsEnabled",false);Set(g,"soundEnabled",false);Set(g,"reduceMotionEnabled",false);
        var camera=(Camera)Get(g,"flightCamera");var oldTarget=camera.targetTexture;
        var canvas=((GameObject)Get(g,"uiRoot")).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var target=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=target;
        bool priorPreview=SkyPulseBirdRosterPreview.Enabled;string priorBird=SkyPulseBirdRosterPreview.SelectedId;float priorTime=SkyPulseBirdRosterPreview.TimeOverride;
        try
        {
            Call(g,"RefreshViewportDecor");
            var skins=(Array)typeof(SkyPulseNativeGame).GetField("Skins",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            Set(g,"equippedSkin",skins.GetValue(0));Set(g,"equippedTrail",Call(g,"GetTrailForSkin",skins.GetValue(0)));Call(g,"ApplyEquippedVisuals");
            SkyPulseBirdRosterPreview.Select("neon_finch",true);SkyPulseBirdRosterPreview.TimeOverride=-1;
            var worlds=(Array)typeof(SkyPulseNativeGame).GetField("Worlds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            Check(worlds.Length==12,"Review uses all twelve existing route definitions");
            for(int world=0;world<worlds.Length;world++)
            {
                string name=(string)Get(worlds.GetValue(world),"Name");
                SkyPulseBirdRosterPreview.TimeOverride=-1;Call(g,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(g,"ResetToMenu");SelectWorld(g,worlds,world);Present(g,0f);yield return null;Fit(g);Present(g,0f);
                Check(((GameObject)Get(g,"homeScreen")).activeInHierarchy,"Actual Home screen remains active for "+name);
                var env=(SkyPulseFlightEnvironment)Get(g,"flightEnvironment");Check(env!=null&&env.CurrentWorld==world,"Home perspective world matches actual route definition: "+name);
                Capture(g,camera,target,"world-"+world.ToString("00")+"-home.png","EndlessRoute","Home",world,name);
                Call(g,"StartFlight");SkyPulseBirdRosterPreview.TimeOverride=.25f;SelectWorld(g,worlds,world);
                int slot=0;float spacing=(float)Call(g,"RoutePipeSpacing");foreach(var pipe in (Array)Get(g,"pipePool"))Call(g,"ConfigurePipe",pipe,3.6f+slot++*spacing);
                ((Text)Get(g,"hudScoreText")).text=(world*15).ToString();
                for(int frame=0;frame<24;frame++)
                {
                    float nearest=float.MaxValue,gap=0;
                    foreach(var pipe in (Array)Get(g,"pipePool")){float x=(float)Get(pipe,"X");if(!(bool)Get(pipe,"Passed")&&x<nearest){nearest=x;gap=(float)Get(pipe,"GapCenter");}}
                    if((float)Get(g,"birdY")<gap-.55f&&(float)Get(g,"birdVelocity")<0)Call(g,"BufferFlapInput");
                    Call(g,"SimulateFlight",1f/60);Call(g,"UpdateGameplayWingState",1f/60);Call(g,"UpdateBirdWingMotion");Call(g,"UpdateScoreFinish",1f/60);Present(g,1f/60);
                }
                Check(Get(g,"state").ToString()=="Playing","Flight inspection follows collision-checked simulation: "+name);
                Call(g,"UpdateScoreBurst",10f);Fit(g);yield return null;Present(g,0f);
                Capture(g,camera,target,"world-"+world.ToString("00")+"-flight.png","EndlessRoute","Playing",world,name);
                if(world==0)
                {
                    InspectNeonArchitecture(env);
                    yield return CaptureNeonMotion(g,camera,target);
                }
            }
            var environment=(SkyPulseFlightEnvironment)Get(g,"flightEnvironment");environment.Render();string before=PixelsHash(Read(environment.WorldImage));
            float heldClock=environment.PresentationClock,heldTravel=environment.Travel;Set(g,"reduceMotionEnabled",true);Set(g,"cinematicClock",heldClock+4);Present(g,1f);
            environment.Render();string held=PixelsHash(Read(environment.WorldImage));
            Check(environment.PresentationClock==heldClock&&environment.Travel==heldTravel,"Reduced Motion holds actual sky/light clock and scenery travel");
            Check(before==held,"Actual rendered perspective-world pixels stay identical while Reduced Motion is held");Set(g,"reduceMotionEnabled",false);
            SkyPulseBirdRosterPreview.TimeOverride=-1;Call(g,"SetHomeProtocol",SkyPulseMode.PulseRelay);Call(g,"ResetToMenu");Present(g,.5f);yield return null;Present(g,0f);
            Check(Get(g,"relayHomeEnvironment")!=null,"Relay Home uses the live orbital environment");
            Capture(g,camera,target,"relay-home.png","PulseRelay","Home",-1,"Orbital station");
            Call(g,"LaunchMission",SkyPulseMode.PulseRelay,0);SkyPulseBirdRosterPreview.TimeOverride=.25f;Present(g,.25f);yield return null;Present(g,0f);
            Check(Get(g,"relayEnvironment")!=null&&Get(g,"state").ToString()=="Playing","Relay flight uses its actual live station and active mission");
            Capture(g,camera,target,"relay-flight.png","PulseRelay","Playing",-1,"Orbital station");
            Shaders();Check(frames.Count==26&&frames.Select(f=>f.sha256).Distinct().Count()==26,"Twenty-six distinct main-camera Home/flight frames captured");
            Write("PASS",null);Debug.Log("SKYPULSE_ATMOSPHERE_REVIEW_PASS: 12 actual Home/flight world pairs and Relay Home/flight; Reduced Motion GPU hold verified");
        }
        finally{SkyPulseBirdRosterPreview.Select(priorBird,priorPreview);SkyPulseBirdRosterPreview.TimeOverride=priorTime;camera.targetTexture=oldTarget;RenderTexture.ReleaseTemporary(target);}
        EditorApplication.Exit(0);
    }
    static void Write(string result,Exception error)
    {
        Directory.CreateDirectory(Output);
        var report=new Report{result=result,date="2026-11-04",scope="Neon City structural/composition/shader candidate in the actual main SkyPulse scene and UI at750x1334. Approved Neon Finch3D workshop model uses the Editor-only in-game visual preview; static flight pose t=.25, motion pose frame/30. Home uses the actual current Home renderer. Remaining eleven worlds are comparison fixtures still under individual model review.",graphicsDevice=SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType,proof="Camera.Render GPU readbacks; normal collision-checked flight simulation; actual palette/world bindings and reduced-motion pixel identity.",limits="Editor rendering is not physical-device visual acceptance, thermal profiling or device FPS. Fixtures select worlds directly; these are not a single uninterrupted scored run. Earlier comparison used the Chrome Raven sprite and a different pose, so this is not a controlled shader-only A/B comparison. The preview does not establish physical-device behavior or approval of the other fourteen roster studies.",comparisonReference="/private/tmp/skypulse-roster-20261104/captures/roster/gameplay/EndlessRoute-chrome_raven.png",assertions=checks.ToArray(),captures=frames.ToArray(),neonMotionFolder="neon-city-motion",neonMotionFrames=motionFiles.Count,neonMotionFps=30,neonMotionFiles=motionFiles.ToArray(),neonMotionSha256=motionHashes.ToArray(),neonMotionCollisionChecked=true,motionProof="90 actual main-camera frames using existing buffered flap input and 120Hz collision-checked simulation, presented at30Hz. Encoding rate does not measure deviceFPS.",inspectionCaptures=inspectionFiles.ToArray(),inspectionProof="world-00-unobstructed.png is the normal live3DWorldImage without gameplay camera/UI/pipes. world-00-architecture-inspection.png reframes the actual existing civic landmark only; perspective camera pose is restored exactly before motion. The close-up is an inspection angle, not the normal gameplay camera.",deviceTested=false,shippingProgressChanged=false,routeWorldSelectedForInspection=true,failure=error==null?null:error.ToString()};
        File.WriteAllText(Path.Combine(Output,"VALIDATION.json"),JsonUtility.ToJson(report,true));
    }
    public static void Failed(Exception error){Write("FAIL",error);Debug.LogException(error);EditorApplication.Exit(1);}
}
public sealed class SkyPulseAtmosphereReviewRunner:MonoBehaviour
{
    public void Begin(IEnumerator routine){StartCoroutine(Guard(routine));}
    IEnumerator Guard(IEnumerator routine)
    {
        while(true)
        {
            bool more;object next;
            try{more=routine.MoveNext();next=more?routine.Current:null;}
            catch(Exception error){SkyPulseAtmosphereReviewChecks.Failed(error);yield break;}
            if(!more)yield break;
            if(next is IEnumerator nested)yield return Guard(nested);else yield return next;
        }
    }
}
