using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using SkyPulse.Mobile;

// Deterministic captures of the actual gameplay renderers, using real event handlers.
[InitializeOnLoad]
public static class SkyPulseEffectsCapture
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static object Get(object o, string n) => o.GetType().GetField(n, Flags).GetValue(o);
    static void Set(object o, string n, object v) => o.GetType().GetField(n, Flags).SetValue(o,v);
    static object Call(object o, string n, params object[] a) => o.GetType().GetMethod(n, Flags).Invoke(o,a);
    static SkyPulseEffectsCapture()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("SkyPulseEffectsCapture", false))
                EditorApplication.delayCall += Capture;
        };
    }
    [MenuItem("SkyPulse/Capture Effects QA")]
    public static void Run()
    {
        SessionState.SetBool("SkyPulseEffectsCapture", true);
        if (EditorApplication.isPlaying) Capture(); else EditorApplication.EnterPlaymode();
    }
    static void Capture()
    {
        SessionState.SetBool("SkyPulseEffectsCapture", false);
        var game = UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();
        if (game == null) { EditorApplication.delayCall += Capture; return; }
        var ints = new Dictionary<string, int>(); var strings = new Dictionary<string,string>();
        var missing = new HashSet<string>();
        var source = File.ReadAllText("Assets/Scripts/SkyPulseNativeGame.cs");
        foreach (Match m in Regex.Matches(source, "PlayerPrefs.Set(Int|String)\\(\"([^\"]+)\""))
        {
            var k=m.Groups[2].Value;
            if (!PlayerPrefs.HasKey(k)) missing.Add(k);
            else if(m.Groups[1].Value=="Int") ints[k]=PlayerPrefs.GetInt(k);
            else strings[k]=PlayerPrefs.GetString(k);
        }
        foreach(var upgrade in (IEnumerable)typeof(SkyPulseNativeGame).GetField("Upgrades", BindingFlags.Static|BindingFlags.NonPublic).GetValue(null))
        {
            var k="skypulse.native.upgrade."+Get(upgrade,"Id");
            if(PlayerPrefs.HasKey(k)) ints[k]=PlayerPrefs.GetInt(k); else missing.Add(k);
        }
        try
        {
            game.enabled=false;
            Set(game,"hapticsEnabled",false);
            var folder=Path.GetFullPath("../artifacts/effects-qa/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(folder);
            var worlds=(Array)typeof(SkyPulseNativeGame).GetField("Worlds",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            foreach(var reduced in new[]{false,true})
            for(int world=0; world<3; world++)
            foreach(var effect in new[]{"flight","crystal","perfect","aegis","time","magnet"})
            {
                Call(game,"StartFlight"); Set(game,"reduceMotionEnabled",reduced); Set(game,"routeWorldIndex",world); Set(game,"routeWorld",worlds.GetValue(world)); Call(game,"ApplyRouteWorldVisuals");
                if(((SpriteRenderer)Get(game,"effectAuraRenderer")).enabled || ((SpriteRenderer)Get(game,"shieldAuraRenderer")).enabled || ((SpriteRenderer)Get(game,"slowAuraRenderer")).enabled) throw new Exception("Power field survived restart");
                Set(game,"birdY",0f); Set(game,"birdVelocity",0f); Call(game,"UpdateBird",0f);
                if(effect=="crystal") Call(game,"CollectCrystalPickup",((IList)Get(game,"crystalPickupPool"))[0]);
                if(effect=="perfect")
                {
                    var pipes=(IList)Get(game,"pipePool"); var pipe=pipes[0];
                    Set(pipe,"X",-4.5f); Set(pipe,"GapCenter",0f); Set(pipe,"Passed",false); ((GameObject)Get(pipe,"Root")).SetActive(true);
                    Call(game,"UpdatePipes",0f);
                    if((int)Get(game,"perfectPasses")<1) throw new Exception("Perfect gate did not trigger");
                }
                if(effect=="aegis" || effect=="time" || effect=="magnet")
                {
                    var pickup=((IList)Get(game,"powerUpPool"))[0];
                    Set(pickup,"Kind",Enum.Parse(Get(pickup,"Kind").GetType(),effect=="aegis"?"Aegis":effect=="time"?"TimePulse":"CrystalMagnet"));
                    ((SpriteRenderer)Get(pickup,"Glow")).color=effect=="aegis"?new Color(.38f,1f,.70f):effect=="time"?new Color(.69f,.49f,1f):new Color(.27f,.92f,1f);
                    Call(game,"CollectPowerUp",pickup); Call(game,"UpdateBirdPowerUpVisuals");
                }
                foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    if(r.name.StartsWith("Trail ")||r.name.StartsWith("Rear thrust ")) throw new Exception("Trail/thrust remains: "+r.name);
                var camera=(Camera)Get(game,"flightCamera");
                var frame=0;
                foreach(var elapsed in new[]{0f,.06f,.12f,.12f,.2f})
                {
                    Set(game,"ambientTime",(float)Get(game,"ambientTime")+elapsed);
                    Call(game,"UpdatePowerUpEffects",elapsed);
                    Call(game,"UpdateBirdPowerUpVisuals");
                    Call(game,"UpdateFlightFeedback",elapsed);
                    Save(camera,Path.Combine(folder,$"{(reduced?"reduced":"normal")}-world-{world}-{effect}-{frame++}.png"));
                }
                if(((SpriteRenderer)Get(game,"flightFeedbackRenderer")).enabled || ((SpriteRenderer)Get(game,"flightFeedbackRingRenderer")).enabled) throw new Exception("Feedback failed to expire");
                if(effect=="aegis" && !(bool)Call(game,"UseShield")) throw new Exception("Shield did not absorb impact");
                Call(game,"UpdatePowerUpEffects",7f); Call(game,"UpdateBirdPowerUpVisuals"); Call(game,"UpdateFlightFeedback",7f);
                foreach(var name in new[]{"effectAuraRenderer","shieldAuraRenderer","slowAuraRenderer","flightFeedbackRenderer","flightFeedbackRingRenderer"})
                    if(((SpriteRenderer)Get(game,name)).enabled) throw new Exception("Effect survived expiration: "+name);
                Save(camera,Path.Combine(folder,$"{(reduced?"reduced":"normal")}-world-{world}-{effect}-5.png"));
            }
            Call(game,"ResetToMenu");
            if(((SpriteRenderer)Get(game,"flightFeedbackRenderer")).enabled || ((SpriteRenderer)Get(game,"flightFeedbackRingRenderer")).enabled) throw new Exception("Feedback survived menu reset");
            File.WriteAllText(Path.Combine(folder,"PASS.txt"), "216 actual Unity renders: three worlds, normal/reduced motion, flight/crystal/perfect/Aegis/Time Pulse/Crystal Magnet, six lifecycle frames. Real collection and gate handlers; no trail/thrust renderers; feedback expiration, timed power expiration, shield consumption, and restart/menu cleanup passed.");
            Debug.Log("SKYPULSE_EFFECTS_QA_PASS "+folder);
        }
        catch(Exception e) { Debug.LogException(e); }
        finally
        {
            foreach(var p in ints) PlayerPrefs.SetInt(p.Key,p.Value);
            foreach(var p in strings) PlayerPrefs.SetString(p.Key,p.Value);
            foreach(var k in missing) PlayerPrefs.DeleteKey(k);
            PlayerPrefs.Save();
            Call(game,"LoadProgress");
            Call(game,"ResetToMenu");
            game.enabled=true;
        }
    }
    static void Save(Camera camera,string path)
    {
        var rt=RenderTexture.GetTemporary(540,960,24); var prior=camera.targetTexture; var active=RenderTexture.active; var rect=camera.rect;
        try {
            camera.targetTexture=rt; camera.rect=new Rect(0,0,1,1); camera.Render(); RenderTexture.active=rt;
            var image=new Texture2D(540,960,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,540,960),0,0); image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
        } finally {camera.targetTexture=prior; camera.rect=rect; RenderTexture.active=active; RenderTexture.ReleaseTemporary(rt);}
    }
}
