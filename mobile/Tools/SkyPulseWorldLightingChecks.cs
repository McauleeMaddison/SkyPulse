// Focused local QA; does not author, save or regenerate world geometry.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SkyPulse.Mobile;

public static class SkyPulseWorldLightingChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static int assertions;
    static void Check(bool good,string note){assertions++;if(!good)throw new Exception("WORLD_LIGHTING: "+note);Debug.Log("WORLD_LIGHTING_CHECK: "+note);}
    static bool Same(Color a,Color b)=>Mathf.Abs(a.r-b.r)<.00001f&&Mathf.Abs(a.g-b.g)<.00001f&&Mathf.Abs(a.b-b.b)<.00001f;
    static bool Finite(Color c)=>float.IsFinite(c.r)&&float.IsFinite(c.g)&&float.IsFinite(c.b)&&c.r>=0&&c.g>=0&&c.b>=0&&c.maxColorComponent<=1.001f;
    public static Color Accent(int world)=>(Color)typeof(SkyPulseFlightEnvironment).Assembly.GetType("SkyPulse.Mobile.SkyPulseRouteArchitecture").GetMethod("Accent",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{world});
    public static void Pure()
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA product required");
        assertions=0;var keys=new HashSet<Color>();
        Check(Same(SkyPulseFlightEnvironment.KeyLightForWorld(0),new Color(1,.87f,.73f)),"Neon retains the original architectural sun colour");
        Check(Same(SkyPulseFlightEnvironment.FillLightForWorld(0),new Color(.18f,.29f,.43f)),"Neon retains the original architectural fill colour");
        for(int world=0;world<12;world++)
        {
            Color key=SkyPulseFlightEnvironment.KeyLightForWorld(world),fill=SkyPulseFlightEnvironment.FillLightForWorld(world);
            Check(Finite(key)&&Finite(fill),"World key/fill remain finite bounded light tones: "+world);
            keys.Add(key);
        }
        Check(keys.Count==12,"All twelve skies retain distinct architectural key tones");
        Check(Same(SkyPulseFlightEnvironment.KeyLightForWorld(-1),SkyPulseFlightEnvironment.KeyLightForWorld(0))&&Same(SkyPulseFlightEnvironment.FillLightForWorld(-1),SkyPulseFlightEnvironment.FillLightForWorld(0)),"Negative world fixtures clamp to Neon");
        Check(Same(SkyPulseFlightEnvironment.KeyLightForWorld(12),SkyPulseFlightEnvironment.KeyLightForWorld(11))&&Same(SkyPulseFlightEnvironment.FillLightForWorld(12),SkyPulseFlightEnvironment.FillLightForWorld(11)),"High world fixtures clamp to Polar");
        var shader=Resources.Load<Shader>("SkyPulse/Shaders/FlightArchitecture");
        Check(shader!=null&&shader.isSupported,"Architecture shader remains supported by the active graphics backend");
        Debug.Log("SKYPULSE_WORLD_LIGHTING_PURE_PASS: "+assertions+" assertions");
    }
    // Invoke on the isolated game's existing environment. Only the presentation
    // fixture changes; no mesh construction or asset import occurs here.
    public static void Live(SkyPulseFlightEnvironment environment)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA product required");
        var type=typeof(SkyPulseFlightEnvironment);
        var surfaces=(Material[])type.GetField("surfaces",F).GetValue(environment);
        var light=(Light)type.GetField("cityKey",F).GetValue(environment);
        var filters=environment.WorldScene.GetComponentsInChildren<MeshFilter>(true);
        var meshPool=new HashSet<Mesh>(Resources.FindObjectsOfTypeAll<Mesh>());
        var materials=environment.WorldScene.GetComponentsInChildren<Renderer>(true).Select(r=>r.sharedMaterial).ToArray();
        for(int world=0;world<12;world++)
        {
            environment.Present(world,0,0,1,Accent(world),new Color(.02f,.04f,.08f),false);
            Check(light!=null&&Same(light.color,SkyPulseFlightEnvironment.KeyLightForWorld(world)),"Actual camera sun follows the selected world without changing intensity: "+world);
            Check(Mathf.Abs(light.intensity-1)<.00001f&&light.cullingMask==(1<<30),"World lighting remains isolated from bird, UI and pipes: "+world);
            Color expected=world==0?new Color(1.05f,.91f,.75f):SkyPulseFlightEnvironment.KeyLightForWorld(world)*1.045f;
            Check(Same(surfaces[1].GetColor("_KeyTone"),expected)&&Same(surfaces[1].GetColor("_FillTone"),SkyPulseFlightEnvironment.FillLightForWorld(world)),"Actual bank material receives its own key/fill pair: "+world);
            var currentFilters=environment.WorldScene.GetComponentsInChildren<MeshFilter>(true);
            Check(currentFilters.SequenceEqual(filters)&&currentFilters.All(f=>meshPool.Contains(f.sharedMesh)),"Changing palette reuses its renderer pool and cached world meshes: "+world);
            Check(environment.WorldScene.GetComponentsInChildren<Renderer>(true).Select(r=>r.sharedMaterial).SequenceEqual(materials),"Changing palette reuses the existing material pool: "+world);
            Check(environment.WorldScene.GetComponentsInChildren<Collider>(true).Length==0&&environment.WorldScene.GetComponentsInChildren<Collider2D>(true).Length==0,"World lighting adds no collision geometry: "+world);
        }
        environment.Present(0,0,0,.5f,Accent(0),new Color(.02f,.04f,.08f),false);
        var blended=Color.Lerp(SkyPulseFlightEnvironment.KeyLightForWorld(11),SkyPulseFlightEnvironment.KeyLightForWorld(0),.5f);
        Check(Same(light.color,blended),"The physical sun blends continuously during a world crossfade");
        Debug.Log("SKYPULSE_WORLD_LIGHTING_LIVE_PASS: "+assertions+" assertions");
    }
}
