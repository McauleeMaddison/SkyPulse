// Copy into Assets/Editor only in a disposable QA project. Never touches a wallet or save.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SkyPulse.Mobile;

public static class SkyPulsePickupDisplayChecks
{
    static void Check(bool ok,string note){if(!ok)throw new Exception(note);Debug.Log("PICKUP_DISPLAY_CHECK: "+note);}
    static void Guard(){if(!Application.isBatchMode||Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable pickup display QA required");}
    public static void Pure()
    {
        Guard();var root=new GameObject("Isolated pickup housing QA");var display=root.AddComponent<SkyPulsePickupDisplay>();
        try
        {
            display.Build();int children=root.transform.childCount;var surfaces=root.GetComponentsInChildren<MeshRenderer>(true);var meshes=root.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();
            Check(display.BuildCount==1&&children==5&&surfaces.Length==10&&meshes.Length==10,"All five housings and their two surfaces are built once");
            Check(display.MaterialCount==2&&surfaces.Select(r=>r.sharedMaterial).Distinct().Count()==2,"Ten surfaces share only two supported finish materials");
            Check(surfaces.All(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.isSupported),"All pickup finish shaders are supported");
            Check(root.GetComponentsInChildren<Collider>(true).Length==0&&root.GetComponentsInChildren<Collider2D>(true).Length==0,"Presentation contributes no pickup or flight colliders");
            var signatures=new HashSet<string>();
            for(int kind=0;kind<SkyPulsePickupDisplay.KindCount;kind++)
            {
                var shell=display.ShellMesh(kind);var light=display.LightMesh(kind);
                Check(shell!=null&&light!=null&&shell!=light&&shell.vertexCount>24&&light.vertexCount>24,"Power has its own structural and light geometry: "+kind);
                Bounds bounds=shell.bounds;bounds.Encapsulate(light.bounds);
                Check(bounds.size.z>.13f&&Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))<=1.3f,"True-depth geometry fits the 1.3-unit display envelope on every axis: "+kind+" / "+bounds.size);
                Check(shell.vertexCount+light.vertexCount<16000,"Pickup construction stays inside its bounded mesh budget: "+kind);
                signatures.Add(shell.vertexCount+"/"+light.vertexCount+"/"+bounds.size);
                display.Present(kind,2.25f,false,Color.cyan,Color.white);
                Check(display.CurrentKind==kind&&Enumerable.Range(0,5).Count(i=>display.Model(i).gameObject.activeSelf)==1,"Exactly the requested power is visible: "+kind);
                var rotation=display.Model(kind).localRotation;
                foreach(int hz in new[]{30,60,120})
                {
                    for(int frame=0;frame<=hz*3;frame++)display.Present(kind,frame/(float)hz,false,Color.cyan,Color.white);
                    var expected=Quaternion.Euler(12+Mathf.Sin(3*.90f)*5,-18+Mathf.Sin(3*.60f)*26,Mathf.Sin(3*.70f)*4);
                    Check(Quaternion.Angle(display.Model(kind).localRotation,expected)<.001f,"Absolute presentation time matches at "+hz+" Hz: "+kind);
                }
                display.Present(kind,3,true,Color.cyan,Color.white);var frozen=display.Model(kind).localRotation;
                display.Present(kind,30,true,Color.cyan,Color.white);
                Check(Quaternion.Angle(frozen,display.Model(kind).localRotation)<.001f,"Reduced Motion holds a stable readable pose: "+kind);
                display.Present(kind,2.25f,false,Color.cyan,Color.white);
                Check(Quaternion.Angle(rotation,display.Model(kind).localRotation)<.001f,"Repeated time has no frame-history drift: "+kind);
            }
            Check(signatures.Count==5,"All five pickup silhouettes have distinct saved construction signatures");
            display.Build();Check(display.BuildCount==1&&root.transform.childCount==children,"Repeated Build cannot duplicate model children");
            for(int i=0;i<30;i++)display.Present(i%5,4.5f,false,Color.cyan,Color.white);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<180;i++)display.Present(i%5,4.5f,false,Color.cyan,Color.white);
            Check(GC.GetAllocatedBytesForCurrentThread()==before,"Warmed model switches and presentation allocate no managed memory");
            Check(root.transform.childCount==children&&root.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).SequenceEqual(meshes),"Animations retain the original pooled geometry");
            Check(surfaces.Select(r=>r.sharedMaterial).Distinct().Count()==2,"Presentation creates no material instances");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        Debug.Log("SKYPULSE_PICKUP_DISPLAY_PURE_PASS");
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        Guard();const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        object Get(object o,string n)=>o.GetType().GetField(n,flags).GetValue(o);
        void Set(object o,string n,object v)=>o.GetType().GetField(n,flags).SetValue(o,v);
        object Call(object o,string n,params object[] args)=>o.GetType().GetMethods(flags).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(o,args);
        game.enabled=false;Call(game,"ResetToMenu");Call(game,"StartFlight");
        var pool=(Array)Get(game,"powerUpPool");var gate=((Array)Get(game,"pipePool")).GetValue(0);var pickup=pool.GetValue(0);var root=(GameObject)Get(pickup,"Root");
        var display=root.GetComponent<SkyPulsePickupDisplay>()??root.GetComponentInChildren<SkyPulsePickupDisplay>(true);
        Check(display!=null,"Actual power pool owns the true-depth display component");
        var enumType=pickup.GetType().GetField("Kind",flags).FieldType;
        for(int kind=0;kind<5;kind++)
        {
            Call(game,"ConfigurePowerUp",pickup,gate);Set(pickup,"Kind",Enum.ToObject(enumType,kind));Set(pickup,"Active",true);
            Call(game,"UpdatePowerUps",0f);yield return null;
            Check(display.CurrentKind==kind&&display.Model(kind).gameObject.activeInHierarchy,"Actual production presentation selects power identity: "+kind);
            Check(!((SpriteRenderer)Get(pickup,"Artwork")).enabled&&!((SpriteRenderer)Get(pickup,"Depth")).enabled,"Power model replaces both flat artwork layers: "+kind);
            Call(game,"DeferPowerUp",pickup,0f);
            Check(!root.activeSelf,"The native pool hides the entire display when the pickup is deferred");
        }
        Call(game,"ResetToMenu");Debug.Log("SKYPULSE_PICKUP_DISPLAY_LIVE_PASS");
    }
}
