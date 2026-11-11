// Actual Unity meshes and animation, rendered offscreen for owner visual review.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using SkyPulse.Mobile;
public static class SkyPulseMotionPreviewChecks
{
    static string Output=>Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"motion-review");
    public static void Run()
    {
        if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        Directory.CreateDirectory(Output);
        try{Bird();Pickups();Debug.Log("SKYPULSE_MOTION_PREVIEW_PASS");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static PreviewRenderUtility Studio()
    {
        var p=new PreviewRenderUtility();p.ambientColor=new Color(.12f,.14f,.18f);
        p.lights[0].transform.rotation=Quaternion.Euler(35,35,0);p.lights[0].color=new Color(1,.985f,.96f);p.lights[0].intensity=1.1f;
        p.lights[1].transform.rotation=Quaternion.Euler(335,215,0);p.lights[1].color=new Color(.88f,.94f,1);p.lights[1].intensity=.7f;
        p.camera.clearFlags=CameraClearFlags.SolidColor;p.camera.backgroundColor=new Color(.025f,.032f,.043f);p.cameraFieldOfView=30;
        var reflection=AssetDatabase.LoadAssetAtPath<Cubemap>(SkyPulseBirdStudioLighting.ReflectionPath);
        if(reflection!=null){var o=new GameObject("Review reflection",typeof(ReflectionProbe));p.AddSingleGO(o);var probe=o.GetComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=reflection;probe.intensity=.85f;probe.size=Vector3.one*100;}
        return p;
    }
    static void Bird()
    {
        var p=Studio();try
        {
            var path="Assets/Editor/SkyPulseBirdWorkshop/Roster/Chromaflux/Chromaflux Rig.prefab";
            var model=p.InstantiatePrefabInScene(AssetDatabase.LoadAssetAtPath<GameObject>(path));var rig=model.GetComponent<SkyPulseNeonFinchPrototype>();rig.animate=false;rig.turntable=false;rig.reducedMotion=false;
            var all=model.GetComponentsInChildren<Renderer>();Bounds bounds=default;bool first=true;
            for(int i=0;i<50;i++){rig.SamplePose(i*rig.flapPeriod/50);foreach(var r in all){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}}
            Fit(p.camera,bounds,Quaternion.Euler(8,-90,0));var folder=Path.Combine(Output,"chromaflux-front");Directory.CreateDirectory(folder);
            for(int i=0;i<50;i++){rig.SamplePose(i*rig.flapPeriod/50);Render(p,Path.Combine(folder,i.ToString("000")+".png"));}
        }finally{p.Cleanup();}
    }
    static void Pickups()
    {
        var p=Studio();try
        {
            var root=new GameObject("Pooled pickup review",typeof(SkyPulsePickupDisplay));p.AddSingleGO(root);var display=root.GetComponent<SkyPulsePickupDisplay>();display.Build();
            var names=new[]{"aegis","magnet","time-pulse","crystal-echo","route-lens"};var primary=new[]{new Color(.38f,.96f,.70f),Color.cyan,new Color(.69f,.49f,1),new Color(.73f,.61f,1),new Color(.40f,.91f,1)};
            p.camera.transform.SetPositionAndRotation(new Vector3(0,0,-2.7f),Quaternion.identity);p.camera.nearClipPlane=.01f;p.camera.farClipPlane=10;
            for(int i=0;i<5;i++){display.Present(i,2.25f,false,primary[i],Color.white);Render(p,Path.Combine(Output,"pickup-"+names[i]+".png"));}
        }finally{p.Cleanup();}
    }
    static void Fit(Camera camera,Bounds bounds,Quaternion rotation)
    {
        var inv=Quaternion.Inverse(rotation);float tan=Mathf.Tan(15*Mathf.Deg2Rad),distance=.1f;
        for(int i=0;i<8;i++){var point=inv*Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));distance=Mathf.Max(distance,-point.z+Mathf.Abs(point.x)/tan,-point.z+Mathf.Abs(point.y)/tan);}
        camera.transform.SetPositionAndRotation(bounds.center+rotation*Vector3.back*distance*1.08f,rotation);camera.nearClipPlane=.01f;camera.farClipPlane=distance+bounds.extents.magnitude*4+10;
    }
    static void Render(PreviewRenderUtility p,string path)
    {
        p.BeginPreview(new Rect(0,0,400,400),GUIStyle.none);try{p.Render();}finally
        {
            var target=(RenderTexture)p.EndPreview();var before=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=before;
        }
    }
}
