using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SkyPulse.Mobile;

// Invoke only from the disposable QA project, never from the owner's live save.
public static class SkyPulseBirdFinishChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string name)=>o.GetType().GetField(name,F).GetValue(o);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,F).SetValue(o,value);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethods(F).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(o,args);
    static void Check(bool value,string note){if(!value)throw new Exception(note);Debug.Log("BIRD_REVIEW_FINISH_CHECK: "+note);}
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        // Instantiate the EditorWindow without showing or saving any owner UI.
        // Its actual Studio constructor must enclose interstitial animation poses,
        // not merely the bind frame used by the previous viewer.
        var window=ScriptableObject.CreateInstance<SkyPulseBirdRosterPreviewWindow>();
        try
        {
            for(int index=0;index<15;index++)
            {
                Set(window,"studioIndex",index);Call(window,"CreatePreview");
                var studioRig=(SkyPulseNeonFinchPrototype)Get(window,"rig");
                Check(studioRig!=null,"Studio loads the saved articulated roster entry: "+index);
                var envelope=(Bounds)Get(window,"bounds");envelope.Expand(envelope.size*.08f);
                var geometry=studioRig.GetComponentsInChildren<Renderer>();bool fits=true;
                for(int pose=0;pose<32;pose++)
                {
                    studioRig.SamplePose((pose+.37f)*studioRig.flapPeriod/32f);
                    foreach(var part in geometry)fits&=envelope.Contains(part.bounds.min)&&envelope.Contains(part.bounds.max);
                }
                Check(fits,"Studio's constant framing covers the full articulated stroke, including unsampled rest-between poses: "+index);
                Call(window,"DisposePreview");
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(window);}
        var sky=RenderSettings.ambientSkyColor;var equator=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
        var reflection=RenderSettings.customReflectionTexture;float intensity=RenderSettings.reflectionIntensity;
        var studio=new PreviewRenderUtility();
        try
        {
            SkyPulseBirdRosterPreview.ConfigureStudioLighting(studio);
            Check(studio.lights[0].intensity>studio.lights[1].intensity&&studio.lights[1].intensity>.2f,"A restrained fill leaves real sculpted plate shading visible");
        }
        finally{studio.Cleanup();}
        Check(RenderSettings.ambientSkyColor==sky&&RenderSettings.ambientEquatorColor==equator&&RenderSettings.ambientGroundColor==ground&&RenderSettings.customReflectionTexture==reflection&&RenderSettings.reflectionIntensity==intensity,"Isolated three-light studio never changes active-world ambient or reflections");
        Call(game,"ResetToMenu");Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"StartFlight");
        SkyPulseBirdRosterPreview.Select("chrome_raven",true);var camera=(Camera)Get(game,"flightCamera");camera.Render();
        var rig=SkyPulseBirdRosterPreview.CurrentRig;Check(rig!=null,"Actual gameplay camera creates the solid reference rig");
        var holder=rig.transform.parent;var lights=holder.GetComponentsInChildren<Light>();
        Check(lights.Length==2&&lights.All(l=>l.type==LightType.Directional&&l.cullingMask==(1<<26)&&l.shadows==LightShadows.None),"Gameplay review owns only its two bird-layer lights, without another reflection probe");
        Check(holder.GetComponentsInChildren<ReflectionProbe>().Length==0,"Bird review cannot change nearby pipe/world reflection assignments");
        var rotations=lights.Select(l=>l.transform.rotation).ToArray();var pilot=(Transform)Get(game,"bird");var original=pilot.rotation;int originalWorld=(int)Get(game,"routeWorldIndex");
        try
        {
            foreach(float bank in new[]{-32f,-16f,0f,16f,32f})
            {
                pilot.rotation=Quaternion.Euler(0,0,bank);SkyPulseBirdRosterPreview.UpdateNow(game);
                Check(lights.Select((l,i)=>Quaternion.Angle(l.transform.rotation,rotations[i])<.001f).All(v=>v),"Real surface highlights keep a fixed world direction while the bird banks: "+bank);
            }
            for(int world=0;world<12;world++)
            {
                Set(game,"routeWorldIndex",world);SkyPulseBirdRosterPreview.UpdateNow(game);
                Check(lights.All(l=>l.color.r>.5f&&l.color.g>.5f&&l.color.b>.5f),"World tint retains neutral metal readability: "+world);
            }
        }
        finally{pilot.rotation=original;Set(game,"routeWorldIndex",originalWorld);SkyPulseBirdRosterPreview.Select("",false);Call(game,"ResetToMenu");}
        yield return null;
        Check(lights.All(l=>l==null),"Stopping review disposes the private bird lights");
        Debug.Log("SKYPULSE_BIRD_REVIEW_FINISH_PASS");
    }
}
