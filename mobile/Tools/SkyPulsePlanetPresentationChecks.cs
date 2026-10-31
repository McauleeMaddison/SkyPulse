// Real shader rendering in the disposable QA product. Not device evidence.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using SkyPulse.Mobile;

public static class SkyPulsePlanetPresentationChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string name)=>o.GetType().GetField(name,F).GetValue(o);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,F).Invoke(o,args);
    static void Check(bool good,string text){if(!good)throw new Exception(text);Debug.Log("PLANET_PRESENTATION_CHECK: "+text);}
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA only");
        Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);Call(game,"UpdateCinematicPresentation",0f);
        var env=(SkyPulseRelayEnvironment)Get(game,"relayEnvironment");var world=(SkyPulseOrbitalWorld)Get(env,"orbitalWorld");
        string[] names={"Distant blue planet","West distant moon","Lower distant moon"};
        var bodies=names.Select(n=>world.Find(n)).ToArray();var materials=bodies.Select(t=>t.GetComponent<MeshRenderer>().sharedMaterial).ToArray();
        Check(materials.Distinct().Count()==3,"Ocean world, crater moon and glacial moon use distinct materials");
        var surface=materials[0].GetTexture("_SurfaceMap") as Texture2D;
        Check(surface!=null&&surface==Resources.Load<Texture2D>("SkyPulse/materials/relay-ocean-albedo-v2")&&materials[0].GetFloat("_SurfaceMapAmount")==1,"Ocean sphere uses the shared original detailed geological albedo");
        Check(surface.width<=2048&&surface.height*2==surface.width&&surface.mipmapCount>1&&!surface.isReadable,"Original planet map is a bounded mipmapped 2:1 texture with CPU readback disabled");
        Check(surface.wrapModeU==TextureWrapMode.Repeat&&surface.wrapModeV==TextureWrapMode.Clamp,"Ocean map wraps longitude and clamps the polar rows");
        Check(materials[1].GetFloat("_SurfaceMapAmount")==0&&materials[2].GetFloat("_SurfaceMapAmount")==0,"Airless and glacial moons keep their separate physical geology");
        for(int i=0;i<3;i++)Check(materials[i].GetFloat("_Kind")==i,"Geological identity is explicit: "+names[i]);
        Check(materials[0].GetFloat("_Atmosphere")>.01f&&materials[1].GetFloat("_Atmosphere")==0&&materials[2].GetFloat("_Atmosphere")<materials[0].GetFloat("_Atmosphere"),"Airless crater moon and thin glacial atmosphere differ from the ocean world");
        foreach(string shaderName in new[]{"OrbitalPlanet","OrbitalAtmosphere","OrbitalStructure"})
        {
            var shader=Resources.Load<Shader>("SkyPulse/Shaders/"+shaderName);Check(shader!=null&&shader.isSupported,"Supported celestial/station shader: "+shaderName);
            foreach(var message in ShaderUtil.GetShaderMessages(shader))Check(message.severity.ToString()!="Error",shaderName+" compiles: "+message.message);
        }
        Check(world.StructureCount==29&&env.WorldCamera.transform.parent.GetComponentsInChildren<MeshRenderer>().Length==33,"Celestial detail reuses the existing pooled station objects");
        var holder=new GameObject("Isolated celestial shader QA");holder.transform.position=new Vector3(24000,24000,0);
        var cameraObject=new GameObject("Celestial inspection camera",typeof(Camera));cameraObject.transform.SetParent(holder.transform,false);cameraObject.transform.localPosition=new Vector3(0,0,-5);
        var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1.13f;camera.aspect=1;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.003f,.006f,.012f,0);
        var globe=new GameObject("Inspection sphere",typeof(MeshFilter),typeof(MeshRenderer));globe.transform.SetParent(holder.transform,false);globe.layer=29;globe.GetComponent<MeshFilter>().sharedMesh=bodies[0].GetComponent<MeshFilter>().sharedMesh;
        var renderer=globe.GetComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        var target=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32){antiAliasing=2};target.Create();camera.targetTexture=target;
        var pixels=new Texture2D(512,512,TextureFormat.RGBA32,false);var prior=RenderTexture.active;
        string folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"orbital-planets");Directory.CreateDirectory(folder);
        try
        {
            for(int i=0;i<3;i++)
            {
                renderer.sharedMaterial=materials[i];camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,512,512),0,0);pixels.Apply();
                var data=pixels.GetPixels();int bright=0,dark=0;float minimum=10,maximum=0;
                for(int y=90;y<422;y++)for(int x=90;x<422;x++)
                {
                    if(new Vector2(x-256,y-256).sqrMagnitude>155*155)continue;
                    var c=data[y*512+x];float luma=c.r*.2126f+c.g*.7152f+c.b*.0722f;
                    minimum=Mathf.Min(minimum,luma);maximum=Mathf.Max(maximum,luma);if(luma>.08f)bright++;if(luma<.025f)dark++;
                }
                Check(pixels.GetPixel(256,256).a>.95f,"Solid 3D globe writes a closed opaque centre: "+names[i]);
                Check(bright>300&&dark>300&&maximum-minimum>.065f,"Curved sunlit and night hemispheres have visible contrast: "+names[i]+" / bright "+bright+" / dark "+dark);
                File.WriteAllBytes(Path.Combine(folder,(i+1).ToString("00")+"-"+names[i].Replace(' ','-')+".png"),pixels.EncodeToPNG());
                if(i==0)
                {
                    try
                    {
                        materials[0].SetFloat("_SurfaceMapAmount",0);camera.Render();pixels.ReadPixels(new Rect(0,0,512,512),0,0);pixels.Apply();
                        var negative=pixels.GetPixels32();int changed=0;
                        for(int pixel=0;pixel<data.Length;pixel++)
                            if(Mathf.Abs(data[pixel].r-negative[pixel].r/255f)+Mathf.Abs(data[pixel].g-negative[pixel].g/255f)+Mathf.Abs(data[pixel].b-negative[pixel].b/255f)>.12f)changed++;
                        Check(changed>2000,"Disabling the original surface map is a real rendered negative control, not a decorative unused binding: "+changed+" pixels");
                    }
                    finally{materials[0].SetFloat("_SurfaceMapAmount",1);}
                }
            }
        }
        finally
        {
            RenderTexture.active=prior;camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(holder);
        }
        env.SetVisual(12,3.5f);Check(materials.All(m=>Mathf.Abs(m.GetFloat("_Clock")-3.5f)<.0001f),"All material animation follows the same absolute presentation clock");
        Call(game,"ResetToMenu");yield return null;Debug.Log("SKYPULSE_PLANET_PRESENTATION_PASS");
    }
}
