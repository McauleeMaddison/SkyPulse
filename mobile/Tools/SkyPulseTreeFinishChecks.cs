using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using SkyPulse.Mobile;

// Visual motion regression only; exact costs and purchase routing remain in the
// existing TechTreeChecks suite. This runs against the disposable QA product.
public static class SkyPulseTreeFinishChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string name)=>o.GetType().GetField(name,F).GetValue(o);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,F).Invoke(o,args);
    static void Check(bool ok,string note){if(!ok)throw new Exception(note);Debug.Log("TREE_FINISH_CHECK: "+note);}
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA product required");
        Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");Call(game,"OpenUpgrades");yield return null;
        var tree=(SkyPulseTechTreeView)Get(game,"holographicTree");var content=(RectTransform)Get(game,"customizeContent");
        tree.ReducedMotion=false;tree.MotionPaused=false;tree.Tick(.37f);
        var core=tree.TreeCamera.transform.parent.Find("Living SkyPulse core projector");
        var crystal=core.Find("Levitating upgrade prism");var halo=core.Find("Core level halo");
        var rotation=crystal.localRotation;var position=crystal.localPosition;var haloRotation=halo.localRotation;
        float clock=tree.AnimationClock;
        tree.ReducedMotion=true;tree.Tick(.8f);
        Check(tree.AnimationClock==clock&&Quaternion.Angle(rotation,crystal.localRotation)<.001f&&(position-crystal.localPosition).sqrMagnitude<.0000001f,"Enabling Reduced Motion freezes the existing prism pose rather than resetting its phase");
        Check(Quaternion.Angle(haloRotation,halo.localRotation)<.001f,"Core halo retains its current physical angle under Reduced Motion");
        tree.ReducedMotion=false;tree.Tick(0);
        Check(Quaternion.Angle(rotation,crystal.localRotation)<.001f&&(position-crystal.localPosition).sqrMagnitude<.0000001f,"Disabling Reduced Motion resumes from the same optical pose");
        content.anchoredPosition=new Vector2(300,900);var before=content.anchoredPosition;
        tree.FocusCore();tree.Tick(.001f);
        Check(Vector2.Distance(before,content.anchoredPosition)<.01f,"Focus begins with zero velocity instead of an abrupt ease-out kick");
        tree.Tick(.42f);Check(content.anchoredPosition==Vector2.zero&&!tree.IsFocusing,"Smooth focus still settles exactly at the core");
        content.anchoredPosition=new Vector2(300,900);tree.FocusCore();tree.Tick(.08f);tree.CancelFocus();var interrupted=content.anchoredPosition;tree.Tick(.1f);
        Check(content.anchoredPosition==interrupted&&!tree.IsFocusing,"The new easing remains immediately interruptible");
        Call(game,"ResetToMenu");Debug.Log("SKYPULSE_TREE_FINISH_PASS");
    }
}
