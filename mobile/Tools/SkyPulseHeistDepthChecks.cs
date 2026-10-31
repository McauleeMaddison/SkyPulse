using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using SkyPulse.Mobile;
using UnityEditor;

// Disposable Editor QA: checks presentation without changing puzzle rules.
[InitializeOnLoad]
public static class SkyPulseHeistDepthChecks
{
    const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    const string FocusedKey="SkyPulseHeistDepthFocused";
    static SkyPulseHeistDepthChecks(){EditorApplication.update+=FocusedTick;}
    public static void Run()
    {
        // Preference resets are allowed only inside the disposable QA product.
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006"||Application.productName!="SkyPulseQA20261006")throw new Exception("Disposable batch QA product required");
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");
        PlayerPrefs.DeleteAll();PlayerPrefs.Save();SessionState.SetBool(FocusedKey,true);EditorApplication.EnterPlaymode();
    }
    static void FocusedTick()
    {
        if(!SessionState.GetBool(FocusedKey,false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool(FocusedKey,false);
        new GameObject("Focused optical repair QA").AddComponent<SkyPulseWorldExpansionRunner>().Begin(ExecuteFocused());
    }
    static IEnumerator ExecuteFocused()
    {
        if(Application.companyName!="SkyPulseQA20261006"||Application.productName!="SkyPulseQA20261006")throw new Exception("Disposable QA product required");
        SkyPulseBlackoutChecks.Pure();
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();Check(game!=null,"Shipping scene starts the real game for optical repair checks");
        game.enabled=false;game.GetType().GetField("hapticsEnabled",F).SetValue(game,false);
        yield return SkyPulseBlackoutChecks.Live(game);
        yield return Live(game);
        yield return SkyPulsePurposeChecks.Live(game);
        yield return SkyPulseBlackoutChecks.Captures(game);
        string previous=Environment.GetEnvironmentVariable("SKYPULSE_PREVIEW_SCENES");
        try
        {
            Environment.SetEnvironmentVariable("SKYPULSE_PREVIEW_SCENES","home-blackout");
            yield return SkyPulsePresentationChecks.Captures(game);
        }
        finally{Environment.SetEnvironmentVariable("SKYPULSE_PREVIEW_SCENES",previous);}
        Debug.Log("SKYPULSE_OPTICAL_REPAIR_PASS");EditorApplication.Exit(0);
    }
    static object Get(object source,string name)=>source.GetType().GetField(name,F).GetValue(source);
    static object Call(object source,string name,params object[] args)=>source.GetType().GetMethod(name,F).Invoke(source,args);
    static void Check(bool ok,string note){if(!ok)throw new Exception(note);Debug.Log("HEIST_DEPTH_CHECK: "+note);}
    static Color32[] OpticalPixels(SkyPulseHeistBoard board,string evidence=null)
    {
        var camera=(Camera)Get(board,"opticalCamera");camera.Render();var previous=RenderTexture.active;
        var pixels=new Texture2D(board.Texture.width,board.Texture.height,TextureFormat.RGBA32,false);
        try
        {
            RenderTexture.active=board.Texture;pixels.ReadPixels(new Rect(0,0,pixels.width,pixels.height),0,0);pixels.Apply();
            var folder=Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT");
            if(evidence!=null&&!string.IsNullOrEmpty(folder)){folder=Path.Combine(folder,"heist-readability");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,evidence+".png"),pixels.EncodeToPNG());}
            return pixels.GetPixels32();
        }
        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);}
    }
    static int ContrastPixels(Color32[] shown,Color32[] absent,float margin)
    {
        int readable=0;
        for(int i=0;i<shown.Length;i++)
        {
            float a=(shown[i].r*.2126f+shown[i].g*.7152f+shown[i].b*.0722f)/255;
            float b=(absent[i].r*.2126f+absent[i].g*.7152f+absent[i].b*.0722f)/255;
            if(a-b>margin&&shown[i].a>180)readable++;
        }
        return readable;
    }
    static void CheckInactiveOpticContrast(SkyPulseHeistBoard board,MeshRenderer[] parts,string name,int minimum)
    {
        var present=OpticalPixels(board,name+"-current");
        foreach(var part in parts)part.enabled=false;
        var absent=OpticalPixels(board);
        foreach(var part in parts)part.enabled=true;
        int actual=ContrastPixels(present,absent,.065f);
        // An intentionally black, unlit instrument is the negative control.
        // It must fail the same visibility threshold rather than passing merely
        // because an opaque object occupies the expected socket.
        var originals=parts.Select(p=>p.sharedMaterial).ToArray();var blocks=parts.Select(p=>{var block=new MaterialPropertyBlock();p.GetPropertyBlock(block);return block;}).ToArray();var black=new Material(originals[0]);
        black.SetColor("_BaseColor",Color.black);black.SetColor("_LightTint",Color.black);black.SetColor("_GlowColor",Color.black);black.SetFloat("_OpticFinish",0);black.SetFloat("_Glass",0);black.SetFloat("_Hologram",0);black.SetFloat("_BoardDeck",0);
        Color32[] negative;
        try{var off=new MaterialPropertyBlock();off.SetFloat("_Energy",0);off.SetColor("_GlowColor",Color.black);foreach(var part in parts){part.sharedMaterial=black;part.SetPropertyBlock(off);}negative=OpticalPixels(board,name+"-negative");}
        finally{for(int i=0;i<parts.Length;i++){parts[i].sharedMaterial=originals[i];parts[i].SetPropertyBlock(blocks[i]);}UnityEngine.Object.DestroyImmediate(black);}
        int missing=ContrastPixels(negative,absent,.065f);
        Check(missing<minimum,"Black/unlit negative control fails instrument contrast: "+name+" / "+missing);
        Check(actual>=minimum,"Inactive instrument remains visible against its physical deck: "+name+" / "+actual);
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");
        Call(game,"ResetToMenu");var campaign=typeof(SkyPulseBlackoutChecks).GetMethod("CaptureCampaign",F).Invoke(null,new object[]{7});game.GetType().GetField("blackoutCampaign",F).SetValue(game,campaign);
        for(int vault=0;vault<8;vault++)
        {
            Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,vault);Call(game,"RestartBlackoutVault");
            var board=(SkyPulseHeistBoard)Get(game,"blackoutBoard");var console=(Transform)Get(board,"console");var camera=(Camera)Get(board,"opticalCamera");
            Check(Mathf.Abs(Mathf.DeltaAngle(console.localEulerAngles.x,28))<.01f,"Fixed 28-degree view reveals actual optic depth: vault "+vault);
            var platen=(MeshRenderer)Get(board,"platenRenderer");Check(platen.GetComponent<MeshFilter>().sharedMesh.bounds.size.z>.47f,"Vault has a solid raised chassis: "+vault);
            var bounds=platen.GetComponent<MeshFilter>().sharedMesh.bounds;
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
            {
                var point=platen.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));var view=camera.WorldToViewportPoint(point);
                Check(view.x>=0&&view.x<=1&&view.y>=0&&view.y<=1,"Chassis depth stays inside the board camera: "+vault);
            }
            var session=(BlackoutSession)Get(game,"blackoutSession");var modules=(Array)Get(board,"modules");
            if(vault==2)Check(((RectInt)Get(board,"layout")).height==3,"Unused padded rows do not leave an empty half-board below Split Signal");
            Check(((Transform)Get(modules.GetValue(session.Puzzle.Start),"Emitter")).gameObject.activeSelf,"Source has a directional physical emitter: "+vault);
            foreach(int tile in Enumerable.Range(0,session.Puzzle.Count).Where(t=>session.Puzzle.Tile(t)=='E'))Check(((Transform)Get(modules.GetValue(tile),"Receiver")).gameObject.activeSelf,"Uplink has a distinct receiver crown: "+vault);
            var targetPositions=(Vector2[])Get(game,"blackoutPositions");
            foreach(int tile in Enumerable.Range(0,session.Puzzle.Count).Where(session.Puzzle.Rotatable))
            {
                var module=(Transform)Get(modules.GetValue(tile),"Root");var screen=camera.WorldToViewportPoint(module.position);var centre=new Vector2((screen.x-.5f)*1080,(screen.y-.5f)*1080);
                Check(Vector2.Distance(centre,targetPositions[tile])<12,"Touch stays centred on the raised optic: "+vault+" / "+tile);
            }
        }
        var liveBoard=(SkyPulseHeistBoard)Get(game,"blackoutBoard");
        var first=new BlackoutSession(SkyPulseBlackoutCampaign.Stages[0]);var screenPoints=new Vector2[49];
        liveBoard.ReducedMotion=false;liveBoard.MotionPaused=false;liveBoard.Bind(first,screenPoints);
        var optics=(Array)Get(liveBoard,"modules");var moving=optics.GetValue(6);
        var housing=(MeshRenderer)Get(moving,"Collar");var mirror=(MeshRenderer)Get(moving,"MirrorRenderer");
        Check(housing.GetComponent<MeshFilter>().sharedMesh.bounds.size.z>.10f,"Fine retaining clamps have genuine depth without a bulky annular sleeve");
        var smoked=mirror.sharedMaterial.GetColor("_BaseColor");Check(smoked.a<.6f&&smoked.r<.10f,"Interactive mirror is smoked optical glass rather than an opaque pearl bar");
        var frame=(MeshRenderer)Get(moving,"MirrorFrame");Check(frame.GetComponent<MeshFilter>().sharedMesh.bounds.size.z>.15f,"A separate narrow titanium frame defines the prism edges");
        CheckInactiveOpticContrast(liveBoard,new[]{frame,mirror,(MeshRenderer)Get(moving,"Collar"),(MeshRenderer)Get(moving,"Base")},"inactive-prism",80);
        int uplink=Enumerable.Range(0,first.Puzzle.Count).First(t=>first.Puzzle.Tile(t)=='E');
        CheckInactiveOpticContrast(liveBoard,new[]{(MeshRenderer)Get(optics.GetValue(uplink),"ReceiverRenderer"),(MeshRenderer)Get(optics.GetValue(uplink),"LensRenderer")},"inactive-uplink",30);
        var fixturePreview=new BlackoutSession(first.Puzzle);Check(first.PreviewRotation(6,fixturePreview),"Free route preview uses the existing optical rules");
        liveBoard.SetPreview(fixturePreview,6);liveBoard.Tick(1f/30);
        var props=new MaterialPropertyBlock();((MeshRenderer)Get(liveBoard,"platenRenderer")).GetPropertyBlock(props);
        Check(props.GetFloat("_Energy")==0&&first.CoresPowered==0,"Speculative beam cannot illuminate the actual city output contacts");
        liveBoard.SetPreview(null,-1);liveBoard.Bind(first,screenPoints);
        Check(first.Rotate(6),"First deliberate turn accepted");liveBoard.Tick(1f/120);float openingAngle=(float)Get(moving,"Angle");
        Check(first.Rotate(6),"Second deliberate turn accepted during motion");liveBoard.Tick(1f/120);
        Check((float)Get(moving,"Angle")>openingAngle&&(float)Get(moving,"TargetAngle")==180,"Fast consecutive thumb turns continue forward instead of reversing halfway");
        for(int i=0;i<120;i++)liveBoard.Tick(1f/120);
        Check(Mathf.Abs(liveBoard.VisualAngle(6))<.01f&&first.Trace==2,"A half-turn mirror settles at its equivalent logical orientation with exactly two trace spent");
        first=new BlackoutSession(SkyPulseBlackoutCampaign.Stages[0]);liveBoard.Bind(first,screenPoints);first.Rotate(6);liveBoard.Tick(1f/120);
        int core=Enumerable.Range(0,first.Puzzle.Count).First(i=>first.Puzzle.Tile(i)=='C');
        Check(((MeshRenderer)Get(optics.GetValue(core),"Ripple")).enabled,"A newly powered core emits a pooled charge ripple while the next input remains available");
        liveBoard.ReducedMotion=true;liveBoard.Tick(1f/60);
        Check(optics.Cast<object>().All(m=>!((MeshRenderer)Get(m,"Ripple")).enabled),"Reduced Motion suppresses all charge ripples without suppressing live power");
        foreach(string path in new[]{"HeistOptics","HeistBeam"})
        {
            var shader=Resources.Load<Shader>("SkyPulse/Shaders/"+path);Check(shader!=null&&shader.isSupported,"Optical finish shader resolves: "+path);
            foreach(var message in ShaderUtil.GetShaderMessages(shader))Check(message.severity.ToString()!="Error","Optical finish compiles: "+message.message);
        }
        float? result=null;
        foreach(int fps in new[]{30,60,120})
        {
            var fixture=new BlackoutSession(SkyPulseBlackoutCampaign.Stages[0]);var points=new Vector2[49];liveBoard.ReducedMotion=false;liveBoard.MotionPaused=false;liveBoard.Bind(fixture,points);int tile=SkyPulseMissionChecks.Solve(fixture.Puzzle)[0];fixture.Rotate(tile);
            for(int i=0;i<fps;i++)liveBoard.Tick(1f/fps);float angle=liveBoard.VisualAngle(tile);if(result.HasValue)Check(Mathf.Abs(Mathf.DeltaAngle(angle,result.Value))<.01f,"Rotor easing agrees at "+fps+" FPS");result=angle;
        }
        for(int i=0;i<10;i++)liveBoard.Tick(1f/60);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)liveBoard.Tick(1f/60);Check(GC.GetAllocatedBytesForCurrentThread()==before,"Warmed raised-board updates allocate no managed bytes");
        Call(game,"ResetToMenu");Call(game,"InitializeMissionServices");yield return null;Debug.Log("SKYPULSE_HEIST_DEPTH_PASS");
    }
}
