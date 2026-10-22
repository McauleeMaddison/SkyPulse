// Disposable Editor checks for visual geometry, not a claim of phone performance.
using System;
using System.Collections;
using System.IO;
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
        var shaft=(Material)Get(game,"routeMetal");var collar=(Material)Get(game,"routeCapMetal");
        Check(shaft.shader.isSupported&&shaft.shader.name=="SkyPulse/Original Endless Neon Metal","Original neon art is rendered on the real collision-matched 3D geometry");
        Check(shaft.mainTexture==((Sprite)Get(game,"pipeBodySprite")).texture&&collar.mainTexture==((Sprite)Get(game,"pipeCapSprite")).texture,"Endless uses the original shaft and collar textures, not replacement pipe artwork");
        foreach(int fps in new[]{30,60,120})
        {
            typeof(SkyPulseNativeGame).GetField("cinematicClock",F).SetValue(game,0f);
            for(int i=0;i<fps;i++)Call(game,"UpdateCinematicPresentation",1f/fps);Call(game,"UpdateDepthPresentation",0f);
            Check(Mathf.Abs(shaft.GetFloat("_Clock")-1)<.001f,"LED strip packets use the same elapsed-time clock at "+fps+" FPS");
        }
        typeof(SkyPulseNativeGame).GetField("reduceMotionEnabled",F).SetValue(game,true);Call(game,"UpdateDepthPresentation",0f);Check(shaft.GetFloat("_Clock")==0&&collar.GetFloat("_Clock")==0,"Reduced Motion keeps cyan/pink rails lit with static packets");typeof(SkyPulseNativeGame).GetField("reduceMotionEnabled",F).SetValue(game,false);
        foreach(var pair in (Array)Get(game,"pipePool"))foreach(string side in new[]{"Top","Bottom"})
        {
            var s=Get(pair,side);var body=(BoxCollider2D)Get(s,"BodyCollider");var cap=(BoxCollider2D)Get(s,"CapCollider");
            var root=(GameObject)Get(pair,"Root");var matching=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="3D extruded gate column"||r.name=="3D bevelled gate collar").ToArray();
            var column=matching.Where(r=>r.name=="3D extruded gate column").OrderBy(r=>Mathf.Abs(r.bounds.center.y-body.bounds.center.y)).First();
            float direction=body.bounds.center.y>cap.bounds.center.y?1:-1;
            float far=column.bounds.center.y+direction*column.bounds.extents.y;
            float near=column.bounds.center.y-direction*column.bounds.extents.y;
            Check(Mathf.Abs(far-(body.bounds.center.y+direction*body.bounds.extents.y))<.001f,"Shaft retains its original far end");
            Check(Mathf.Abs(near-(cap.bounds.center.y+direction*(cap.bounds.extents.y-.04f)))<.001f,"Visible shaft ends inside the collar shoulder, away from the open lip");
            Check(matching.Any(r=>Mathf.Abs(r.bounds.center.y-cap.bounds.center.y)<.001f&&Mathf.Abs(r.bounds.size.y-cap.bounds.size.y)<.001f),"3D collar matches the physical gap edge");
        }
        CapSilhouette(game,shaft,collar);
        foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay,SkyPulseMode.BlackoutProtocol})
        {
            Call(game,"SetHomeProtocol",mode);Call(game,"ResetToMenu");Call(game,"UpdateDepthPresentation",0f);yield return null;
            var home=(GameObject)Get(game,"homeScreen");var stage=(SkyPulseDepthStage)Get(game,"homeDepth");var all=UnityEngine.Object.FindObjectsByType<SkyPulseDepthStage>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            Check(stage.Mode==mode&&stage.StageCamera.isActiveAndEnabled&&all.Count(s=>s.StageCamera!=null&&s.StageCamera.isActiveAndEnabled)==1,"Only the selected Home's 3D scene renders");
            Check(home.transform.Find("Flight deck").GetComponent<Image>().color.a==0&&!home.transform.Find("Flight deck").GetComponentsInChildren<SkyPulseCircuitGraphic>().Any(g=>g.Surface==SkyPulseSurface.Panel&&g.transform.parent.name=="Flight deck"),"No enclosing black Home backplate");
            Check(!stage.StageCamera.transform.parent.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Hovering machined deck"||t.name=="Dock shoulder")&&!home.transform.Find("Home flight dock").gameObject.activeSelf,"Home has open geometry instead of dark support slabs and duplicate docks");
            Check(stage.StageCamera.backgroundColor.a==0&&home.GetComponentsInChildren<RawImage>().Any(r=>r.texture==stage.StageCamera.targetTexture&&r.material.shader.name=="SkyPulse/Tech Tree Field"),"Offscreen Home has transparent clear and premultiplied UI compositing");
            var badge=((Text)Get(game,"difficultyText")).rectTransform.parent as RectTransform;Check(badge.rect.width>=340,"Full mode name fits the mode badge");
            foreach(var label in home.GetComponentsInChildren<Text>())Check(!System.Text.RegularExpressions.Regex.IsMatch(label.text,@"\b(placeholder|lorem ipsum|coming soon|todo)\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase),"Home contains finished player-facing copy");
            Check(!((Text)Get(game,"homeProtocolCaption")).gameObject.activeInHierarchy&&!((Text)Get(game,"homeActivityCopy")).gameObject.activeInHierarchy&&!((Text)Get(game,"menuModeDetailText")).gameObject.activeInHierarchy,"Home removes repeated marketing and instruction strips: "+mode);
        }
        Check(Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics").isSupported,"3D optical materials compile on Metal");Call(game,"ResetToMenu");Debug.Log("SKYPULSE_DEPTH_PASS");
    }
    static Vector3 Rgb(Color c)=>new Vector3(c.r,c.g,c.b);
    static void CapSilhouette(SkyPulseNativeGame game,Material shaft,Material collar)
    {
        Check(shaft.GetFloat("_Cutout")==0&&collar.GetFloat("_Cutout")==1,"Only the collar respects its elliptical artwork cutout; the solid shaft is preserved");
        var stage=new GameObject("Isolated collar contour QA");stage.transform.position=new Vector3(16000,16000,0);
        var cap=new GameObject("Original 3D collar",typeof(MeshFilter),typeof(MeshRenderer));cap.transform.SetParent(stage.transform,false);cap.layer=29;
        cap.GetComponent<MeshFilter>().sharedMesh=(Mesh)Get(game,"routeCapMesh");var renderer=cap.GetComponent<MeshRenderer>();renderer.sharedMaterial=collar;cap.transform.localScale=new Vector3(2.06f,.62f,2.06f);
        var probe=GameObject.CreatePrimitive(PrimitiveType.Quad);probe.name="Late-drawn scenery behind collar";probe.transform.SetParent(stage.transform,false);probe.layer=29;probe.transform.localPosition=new Vector3(0,0,1.5f);probe.transform.localScale=new Vector3(4,2,1);
        var probeMaterial=new Material(Shader.Find("Unlit/Color")){color=new Color(.21f,.58f,.72f),renderQueue=2010};probe.GetComponent<MeshRenderer>().sharedMaterial=probeMaterial;
        var cameraObject=new GameObject("Collar contour camera",typeof(Camera));cameraObject.transform.SetParent(stage.transform,false);cameraObject.transform.localPosition=new Vector3(0,0,-5);
        var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1;camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.005f,.008f,.02f);camera.allowMSAA=true;
        var target=new RenderTexture(512,256,24,RenderTextureFormat.ARGB32){antiAliasing=4};target.Create();camera.targetTexture=target;
        var pixels=new Texture2D(512,256,TextureFormat.RGB24,false);var block=new MaterialPropertyBlock();float clock=collar.GetFloat("_Clock");
        var previous=RenderTexture.active;
        Action read=()=>{camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,512,256),0,0);pixels.Apply();};
        var points=new[]{new Vector3(-.45f,-.45f,0),new Vector3(.45f,-.45f,0),new Vector3(-.45f,.45f,0),new Vector3(.45f,.45f,0)};
        var colours=new Color[4];var coords=new Vector2Int[4];
        try
        {
            renderer.enabled=false;read();var centre=pixels.GetPixel(256,128);var reference=pixels.GetPixels();
            for(int i=0;i<4;i++){var v=camera.WorldToViewportPoint(cap.transform.TransformPoint(points[i]));coords[i]=new Vector2Int(Mathf.RoundToInt(v.x*512),Mathf.RoundToInt(v.y*256));colours[i]=pixels.GetPixel(coords[i].x,coords[i].y);}
            renderer.enabled=true;collar.SetFloat("_Cutout",0);read();
            Check(Enumerable.Range(0,4).Any(i=>Vector3.Distance(Rgb(colours[i]),Rgb(pixels.GetPixel(coords[i].x,coords[i].y)))>.1f),"Negative control reproduces the original opaque black corners");
            collar.SetFloat("_Cutout",1);
            foreach(int flip in new[]{0,1})foreach(float phase in new[]{0f,.7f,1.8f})
            {
                block.SetFloat("_ArtFlip",flip);renderer.SetPropertyBlock(block);collar.SetFloat("_Clock",phase);read();
                for(int i=0;i<4;i++)for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)Check(Vector3.Distance(Rgb(colours[i]),Rgb(pixels.GetPixel(coords[i].x+x,coords[i].y+y)))<.025f,"Transparent collar corner preserves later-drawn scenery and depth · flip "+flip+" / phase "+phase);
                Check(Vector3.Distance(Rgb(centre),Rgb(pixels.GetPixel(256,128)))>.1f,"Collar centre remains solid rather than hiding the whole mesh");
                if(phase==0){var folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"cap-contour");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"collar-"+flip+".png"),pixels.EncodeToPNG());}
            }
            Debug.Log("SKYPULSE_CAP_CONTOUR_PASS");
            int last=300;
            foreach(float visibility in new[]{1f,.75f,.5f,.25f,0f})
            {
                collar.SetFloat("_Visibility",visibility);read();int covered=0;
                for(int y=120;y<136;y++)for(int x=248;x<264;x++)
                    if(Vector3.Distance(Rgb(reference[y*512+x]),Rgb(pixels.GetPixel(x,y)))>.05f)covered++;
                Check(covered<=last,"Opaque 3D collar coverage decreases with world departure");last=covered;
                if(visibility==.5f)Check(covered>80&&covered<180,"Half-faded real collar keeps partial coverage rather than popping off");
                if(visibility==0)Check(covered==0,"Fully faded collar writes no colour or depth over later scenery");
            }
            Debug.Log("SKYPULSE_ROUTE_FADE_PASS");
        }
        finally
        {
            collar.SetFloat("_Cutout",1);collar.SetFloat("_Clock",clock);collar.SetFloat("_Visibility",1);RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(probeMaterial);UnityEngine.Object.Destroy(stage);
        }
    }
}
