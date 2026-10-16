// Isolated Editor QA only; never copied into the shipping player.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseRelayGateChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string note){if(!ok)throw new Exception(note);Debug.Log("RELAY_MESH_CHECK: "+note);}
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        game.enabled=false;Set(game,"reduceMotionEnabled",false);Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);yield return null;
        var session=(RelaySession)Get(game,"relaySession");session.Collect(0);session.Collect(1);
        var hardware=(List<SkyPulseRelayGate>)Get(game,"relayPortalHardware");var gate=hardware[0];
        Check(gate.GetComponentsInChildren<SpriteRenderer>().Length==0,"Hardware contains genuine mesh surfaces, no flat gate sprite");
        Check(gate.GetComponentsInChildren<Collider>().Length==0 && gate.GetComponentsInChildren<Collider2D>().Length==0,"3D art adds no competing colliders");
        Check(gate.RearSurface.sortingOrder<13 && gate.FrontSurface.sortingOrder>16,"All pilot layers sit between far shell and near lip");
        foreach(var surface in new[]{gate.RearSurface,gate.FrontSurface})
        {
            var mesh=surface.GetComponent<MeshFilter>().sharedMesh;
            Check(mesh.vertexCount>1000 && mesh.bounds.size.z>2,"Hardware has bevelled surface geometry and real camera depth");
            Check(surface.sharedMaterial.shader.isSupported,"Included titanium shader is supported by the active graphics device");
        }
        Check(Mathf.Abs(SkyPulseRelayGate.InnerRadius-RelaySession.ApertureHalfHeight)<.0001f,"Visible bore retains the tested collision aperture");
        var camera=(Camera)Get(game,"flightCamera");var previousTarget=camera.targetTexture;var previousRect=camera.rect;
        var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var target=RenderTexture.GetTemporary(750,1334,24);camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);
        Call(game,"RefreshViewportDecor");Set(game,"birdY",session.Mission.RelayY[0]);Set(game,"birdVelocity",0f);Call(game,"UpdateBird",0f);
        string folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"gate-volume");Directory.CreateDirectory(folder);
        // A controlled passage shows the same live meshes and pilot at each
        // stage. These are labelled Editor evidence, never store/device media.
        for(int frame=0;frame<60;frame++)
        {
            float desired=session.Mission.RelayX(0)-3.5f+frame*7f/59;
            session.Move(desired-session.Distance);Set(game,"protocolVisualTime",frame/30f+1);
            Call(game,"UpdateGameplayWingState",1f/30);Call(game,"UpdateBirdWingMotion");Call(game,"UpdateRelayPresentation");
            Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();camera.Render();
            Save(target,Path.Combine(folder,frame.ToString("000")+".png"));
        }
        // Render the actual overlap twice. Suppressing only the near surface
        // must uncover pilot-coloured pixels; the centre must remain a hole.
        Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);session=(RelaySession)Get(game,"relaySession");session.Collect(0);session.Collect(1);
        hardware=(List<SkyPulseRelayGate>)Get(game,"relayPortalHardware");gate=hardware[0];
        session.Move(session.Mission.RelayX(0)+1.05f);Set(game,"birdY",session.Mission.RelayY[0]);Set(game,"birdVelocity",0f);Call(game,"UpdateBird",0f);Call(game,"UpdateRelayPresentation");
        yield return null;Canvas.ForceUpdateCanvases();camera.Render();var withLip=Pixels(target);
        gate.FrontSurface.enabled=false;camera.Render();var withoutLip=Pixels(target);gate.FrontSurface.enabled=true;
        var bird=(Transform)Get(game,"bird");Vector3 pilot=camera.WorldToScreenPoint(bird.position);
        int differences=0;
        for(int y=Mathf.Max(0,(int)pilot.y-30);y<Mathf.Min(target.height,(int)pilot.y+30);y++)
            for(int x=Mathf.Max(0,(int)pilot.x-40);x<Mathf.Min(target.width,(int)pilot.x+40);x++)
            {int i=y*target.width+x;if(Mathf.Abs(withLip[i].r-withoutLip[i].r)+Mathf.Abs(withLip[i].g-withoutLip[i].g)+Mathf.Abs(withLip[i].b-withoutLip[i].b)>30)differences++;}
        Check(differences>20,"Rendered near rim actually occludes the crossing pilot (not just a sorting declaration)");
        var setVisual=(Action<bool,float,float>)gate.SetVisual;
        for(int i=0;i<3;i++)setVisual(true,i*.1f,0);
        long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<120;i++)setVisual(true,i/60f,0);
        Check(GC.GetAllocatedBytesForCurrentThread()==before,"Warmed 3D gate animation has zero managed allocations");
        Call(game,"ResetToMenu");yield return null;
        Check(gate==null,"Retry/menu destroys old mesh assemblies and their owned resources");
        camera.targetTexture=previousTarget;camera.rect=previousRect;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;RenderTexture.ReleaseTemporary(target);
        Debug.Log("SKYPULSE_RELAY_MESH_PASS");
    }
    static Color32[] Pixels(RenderTexture target)
    {
        var old=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();var pixels=texture.GetPixels32();UnityEngine.Object.Destroy(texture);RenderTexture.active=old;return pixels;
    }
    static void Save(RenderTexture target,string path)
    {
        var old=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);RenderTexture.active=old;
    }
}
