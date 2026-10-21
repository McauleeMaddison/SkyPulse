// Disposable Editor checks for visual geometry, not a claim of phone performance.
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;
public static class SkyPulseDepthChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);Debug.Log("DEPTH_CHECK: "+s);}
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        Call(game,"StartFlight");Call(game,"UpdateDepthPresentation",0f);Physics2D.SyncTransforms();
        foreach(var pair in (Array)Get(game,"pipePool"))foreach(string side in new[]{"Top","Bottom"})
        {
            var s=Get(pair,side);var body=(BoxCollider2D)Get(s,"BodyCollider");var cap=(BoxCollider2D)Get(s,"CapCollider");
            var root=(GameObject)Get(pair,"Root");var matching=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="3D extruded gate column"||r.name=="3D bevelled gate collar").ToArray();
            Check(matching.Any(r=>Mathf.Abs(r.bounds.center.y-body.bounds.center.y)<.001f&&Mathf.Abs(r.bounds.size.y-body.bounds.size.y)<.001f),"Extruded body follows its own collider transform");
            Check(matching.Any(r=>Mathf.Abs(r.bounds.center.y-cap.bounds.center.y)<.001f&&Mathf.Abs(r.bounds.size.y-cap.bounds.size.y)<.001f),"3D collar matches the physical gap edge");
        }
        foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay,SkyPulseMode.BlackoutProtocol})
        {
            Call(game,"SetHomeProtocol",mode);Call(game,"ResetToMenu");Call(game,"UpdateDepthPresentation",0f);yield return null;
            var home=(GameObject)Get(game,"homeScreen");var stage=(SkyPulseDepthStage)Get(game,"homeDepth");var all=UnityEngine.Object.FindObjectsByType<SkyPulseDepthStage>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            Check(stage.Mode==mode&&stage.StageCamera.isActiveAndEnabled&&all.Count(s=>s.StageCamera!=null&&s.StageCamera.isActiveAndEnabled)==1,"Only the selected Home's 3D scene renders");
            Check(home.transform.Find("Flight deck").GetComponent<Image>().color.a==0&&!home.transform.Find("Flight deck").GetComponentsInChildren<SkyPulseCircuitGraphic>().Any(g=>g.Surface==SkyPulseSurface.Panel&&g.transform.parent.name=="Flight deck"),"No enclosing black Home backplate");
            var badge=((Text)Get(game,"difficultyText")).rectTransform.parent as RectTransform;Check(badge.rect.width>=340,"Full mode name fits the mode badge");
            foreach(var label in home.GetComponentsInChildren<Text>())Check(!System.Text.RegularExpressions.Regex.IsMatch(label.text,@"\b(placeholder|lorem ipsum|coming soon|todo)\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase),"Home contains finished player-facing copy");
        }
        Check(Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics").isSupported,"3D optical materials compile on Metal");Call(game,"ResetToMenu");Debug.Log("SKYPULSE_DEPTH_PASS");
    }
}
