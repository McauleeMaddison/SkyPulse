using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SkyPulse.Mobile;

/// <summary>Unsaved Editor review of the canonical solids in the real Home and Hangar.
/// One surface replaces each artwork layer; touch targets, carousel and player data stay native.</summary>
[InitializeOnLoad]
public static class SkyPulseBirdMenuPreview
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static readonly Dictionary<RectTransform,View> views=new Dictionary<RectTransform,View>();
    static readonly Dictionary<Behaviour,bool> originals=new Dictionary<Behaviour,bool>();
    static readonly HashSet<Behaviour> hidden=new HashSet<Behaviour>();
    static readonly List<Behaviour> released=new List<Behaviour>();
    static readonly HashSet<RectTransform> wanted=new HashSet<RectTransform>();
    static readonly List<RectTransform> expired=new List<RectTransform>();
    static readonly string[] homeLayers={"menuBirdImage","menuBirdSafetyImage","menuBirdFlapImage","menuBirdRiseImage","menuBirdShadowImage","menuBirdEyeGlintImage"};
    static SkyPulseBirdRosterWorkshop.Design[] designs;
    static int frame=-1;static float clock;static bool rendering;
    public static int ActiveViews=>views.Count;
    static object Get(object o,string name)=>o?.GetType().GetField(name,F)?.GetValue(o);
    static object Skin(SkyPulseNativeGame g,string id)=>g.GetType().GetMethod("FindBird",F).Invoke(g,new object[]{id});
    static SkyPulseBirdMenuPreview()
    {
        Canvas.willRenderCanvases+=Present;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode||s==PlayModeStateChange.EnteredEditMode)Clear();};
        AssemblyReloadEvents.beforeAssemblyReload+=Clear;
    }
    public static void Present()
    {
        if(rendering)return;
        if(!EditorApplication.isPlaying||!SkyPulseBirdRosterPreview.Enabled){Clear();return;}
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();if(game==null){Clear();return;}
        rendering=true;
        hidden.Clear();
        try
        {
            bool reduced=(bool)Get(game,"reduceMotionEnabled");
            bool paused=Get(game,"state").ToString()=="Paused"||((GameObject)Get(game,"birdProgressionScreen"))?.activeInHierarchy==true;
            bool fresh=frame!=Time.frameCount;
            if(fresh){if(!paused&&!reduced)clock+=Mathf.Clamp(Time.unscaledDeltaTime,0,.1f);frame=Time.frameCount;}
            wanted.Clear();
            var home=Get(game,"homeScreen") as GameObject;
            if(home!=null&&home.activeInHierarchy&&(SkyPulseMode)Get(game,"homeProtocol")!=SkyPulseMode.BlackoutProtocol)
            {
                var root=Get(game,"menuHeroTransform") as RectTransform;
                string selected=SkyPulseBirdRosterPreview.SelectedId;
                var skin=string.IsNullOrEmpty(selected)?Get(game,"equippedSkin"):Skin(game,selected);
                if(root!=null&&skin!=null)
                {
                    Show(root,skin,new Vector2(850,440),Vector2.zero,reduced,fresh,false);
                    foreach(string name in homeLayers)Hide(Get(game,name) as Image);
                }
            }
            var screen=Get(game,"customizeScreen") as GameObject;
            if(screen!=null&&screen.activeInHierarchy&&Get(game,"cosmeticCategory").ToString()=="Birds")
            {
                var roster=(Array)game.GetType().GetProperty("BirdRoster",F).GetValue(game);int index=(int)Get(game,"hangarPageIndex");
                string[] roots={"hangarPreviousBirdRoot","hangarCurrentBirdRoot","hangarNextBirdRoot"};
                for(int i=0;i<3;i++)
                {
                    var root=Get(game,roots[i]) as RectTransform;if(root==null)continue;
                    var image=root.Find(i==1?"Selected bird":"Neighbour bird")?.GetComponent<Image>();
                    if(image==null)continue;
                    Show(root,roster.GetValue((index+i-1+roster.Length)%roster.Length),i==1?new Vector2(480,315):new Vector2(230,180),new Vector2(0,8),reduced,fresh,i==1);
                    Hide(image);
                    if(i==1)
                    {
                        var legacy=Get(game,"hangarPreviewStage") as SkyPulseProgressionStage;
                        if(legacy!=null){Hide(legacy);Hide(legacy.GetComponent<RawImage>());}
                    }
                }
            }
            expired.Clear();foreach(var pair in views)if(pair.Key==null||!wanted.Contains(pair.Key))expired.Add(pair.Key);
            foreach(var key in expired){views[key].Dispose();views.Remove(key);}
            released.Clear();foreach(var pair in originals)if(pair.Key==null||!hidden.Contains(pair.Key))released.Add(pair.Key);
            foreach(var item in released){if(item!=null)item.enabled=originals[item];originals.Remove(item);}
        }
        finally{rendering=false;}
    }
    static void Show(RectTransform parent,object skin,Vector2 size,Vector2 at,bool reduced,bool fresh,bool dock)
    {
        string id=Get(skin,"Id") as string,art=Get(skin,"ArtPath") as string;
        designs=designs??SkyPulseBirdRosterWorkshop.LoadDesigns();
        var design=designs.FirstOrDefault(d=>d.id==id)??designs.FirstOrDefault(d=>art!=null&&art.EndsWith(d.reference,StringComparison.Ordinal));
        string path=design==null?SkyPulseBirdPrototypeWorkshop.PrefabPath:SkyPulseBirdRosterWorkshop.PrefabPath(design);
        wanted.Add(parent);
        if(views.TryGetValue(parent,out var view)&&view.Path!=path){view.Dispose();views.Remove(parent);view=null;}
        if(view==null){view=new View(parent,path,size,at,Get(skin,"Accent") is Color accent?accent:Color.cyan,dock);views.Add(parent,view);fresh=true;}
        if(fresh)view.Render(clock,reduced);
    }
    static void Hide(Behaviour image){if(image==null)return;hidden.Add(image);if(!originals.ContainsKey(image))originals.Add(image,image.enabled);image.enabled=false;}
    static void Restore(){foreach(var pair in originals)if(pair.Key!=null)pair.Key.enabled=pair.Value;originals.Clear();}
    public static void Clear()
    {foreach(var view in views.Values)view.Dispose();views.Clear();Restore();wanted.Clear();expired.Clear();frame=-1;clock=0;}
    sealed class View:IDisposable
    {
        public readonly string Path;
        readonly PreviewRenderUtility preview;readonly RenderTexture texture;readonly RawImage surface;
        readonly SkyPulseNeonFinchPrototype rig;readonly Transform turntable;
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();static readonly int ClockId=Shader.PropertyToID("_Clock");
        Material guide;Transform dockStatusMarkers;
        public View(RectTransform parent,string path,Vector2 size,Vector2 at,Color accent,bool dock)
        {
            Path=path;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab==null)throw new InvalidOperationException("Saved bird missing: "+path);
            preview=new PreviewRenderUtility();preview.ambientColor=new Color(.12f,.14f,.18f);
            preview.lights[0].transform.rotation=Quaternion.Euler(35,35,0);preview.lights[0].intensity=1.1f;
            preview.lights[1].transform.rotation=Quaternion.Euler(335,215,0);preview.lights[1].color=new Color(.88f,.94f,1);preview.lights[1].intensity=.7f;
            foreach(var light in preview.lights)light.enabled=true;
            var model=preview.InstantiatePrefabInScene(prefab);rig=model.GetComponent<SkyPulseNeonFinchPrototype>();
            rig.animate=false;rig.turntable=false;rig.flapPeriod=1.65f;rig.turntableDegreesPerSecond=12;rig.ResetPose();
            var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
            foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            // Keep the saved rig root untouched: its sampler restores that bind pose on every frame.
            // An outer pivot rotates the whole bird around its visual centre, rather than its beak/root.
            var pivotObject=new GameObject("Centred bird turntable");preview.AddSingleGO(pivotObject);turntable=pivotObject.transform;
            var offset=new GameObject("Saved rig centring offset").transform;offset.SetParent(turntable,false);
            model.transform.SetParent(offset,false);offset.localPosition=-bounds.center;
            bounds=new Bounds(Vector3.zero,bounds.size);
            var reflection=AssetDatabase.LoadAssetAtPath<Cubemap>(SkyPulseBirdStudioLighting.ReflectionPath);
            if(reflection!=null)
            {
                var probeObject=new GameObject("Isolated bird studio reflection",typeof(ReflectionProbe));preview.AddSingleGO(probeObject);
                var probe=probeObject.GetComponent<ReflectionProbe>();probe.mode=UnityEngine.Rendering.ReflectionProbeMode.Custom;probe.customBakedTexture=reflection;
                probe.intensity=.85f;probe.size=Vector3.one*(bounds.extents.magnitude*20);probe.center=bounds.center;
            }
            Renderer[] dockRenderers=Array.Empty<Renderer>();
            if(dock)
            {
                var platform=new GameObject("Open titanium display cradle");preview.AddSingleGO(platform);
                float scale=Mathf.Max(.5f,bounds.size.x*.48f);platform.transform.position=new Vector3(bounds.center.x,bounds.min.y-.20f,bounds.center.z);platform.transform.localScale=Vector3.one*scale;
                var metal=new Material(Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics"));owned.Add(metal);
                metal.SetColor("_BaseColor",new Color(.14f,.20f,.25f));metal.SetFloat("_OpticFinish",1);metal.SetFloat("_Roughness",.46f);
                guide=new Material(metal);owned.Add(guide);guide.SetColor("_GlowColor",accent);guide.SetFloat("_Energy",.42f);
                AddRing(platform.transform,metal,new[]{new Vector2(.70f,-.035f),new Vector2(.96f,-.035f),new Vector2(1,-.015f),new Vector2(1,.025f),new Vector2(.97f,.05f),new Vector2(.72f,.05f),new Vector2(.70f,.025f),new Vector2(.70f,-.035f)});
                AddRing(platform.transform,guide,new[]{new Vector2(.81f,.051f),new Vector2(.825f,.051f),new Vector2(.825f,.055f),new Vector2(.81f,.055f),new Vector2(.81f,.051f)});
                // Real inset machining gives the dock a manufactured scale. The
                // short status arcs occupy a separate lane, so they neither
                // cross the pilot nor depth-fight with the continuous light rail.
                AddDockSegments(platform.transform,metal,24,15f,1.8f,.892f,.944f,.0502f,.053f,"Machined radial index inlays");
                AddDockSegments(platform.transform,metal,4,90f,2.4f,.869f,.948f,.0502f,.053f,"Quarter-turn precision index inlays");
                AddRing(platform.transform,metal,new[]{new Vector2(.837f,.049f),new Vector2(.861f,.049f),new Vector2(.861f,.052f),new Vector2(.837f,.052f),new Vector2(.837f,.049f)});
                var markerRoot=new GameObject("Paired travelling cradle status lights").transform;markerRoot.SetParent(platform.transform,false);dockStatusMarkers=markerRoot;
                AddDockSegments(markerRoot,guide,2,180f,9f,.842f,.856f,.0525f,.055f,"Inset travelling optical markers");
                dockRenderers=platform.GetComponentsInChildren<Renderer>();
            }
            // RawImage fills its native rectangle. Matching its aspect avoids stretching the same
            // bird differently in the hero, centre bay and neighbouring carousel bays.
            int width=size.x<300?384:768,height=Mathf.Max(128,Mathf.RoundToInt(width*size.y/size.x));
            texture=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){name="Editor bird review surface",hideFlags=HideFlags.HideAndDontSave};texture.Create();
            var obj=new GameObject("Editor solid bird · "+model.name,typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage)){hideFlags=HideFlags.HideAndDontSave};
            obj.transform.SetParent(parent,false);surface=obj.GetComponent<RawImage>();surface.texture=texture;surface.raycastTarget=false;
            var rect=surface.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=at;
            var camera=preview.camera;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            camera.orthographic=false;camera.fieldOfView=26;camera.aspect=width/(float)height;camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.targetTexture=texture;
            FitCamera(renderers,dockRenderers);
        }
        public void Render(float time,bool reduced)
        {
            rig.reducedMotion=reduced;rig.SamplePose(time);
            turntable.localRotation=Quaternion.Euler(0,reduced?0:time*rig.turntableDegreesPerSecond,0);
            if(guide!=null)guide.SetFloat(ClockId,reduced?0:time);
            // The shared showcase clock pauses with settings/overlays. A
            // reduced-motion render holds the current marker orientation.
            if(dockStatusMarkers!=null&&!reduced)dockStatusMarkers.localRotation=Quaternion.Euler(0,time*9f,0);
            preview.camera.Render();
        }
        void FitCamera(Renderer[] birdRenderers,Renderer[] dockRenderers)
        {
            var camera=preview.camera;var rotation=Quaternion.Euler(10,-18,0);var inverse=Quaternion.Inverse(rotation);
            // A constant camera fits the actual projected mesh envelopes across a full turn and
            // wing stroke. Unlike a bounding sphere, unused depth never shrinks the showcase.
            float tanY=Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad)*.92f,tanX=tanY*camera.aspect;
            float right=float.NegativeInfinity,left=right,top=right,bottom=right;
            Vector3 min=Vector3.one*float.PositiveInfinity,max=Vector3.one*float.NegativeInfinity;
            for(int yaw=0;yaw<12;yaw++)
            {
                turntable.localRotation=Quaternion.Euler(0,yaw*30,0);
                for(int pose=0;pose<8;pose++)
                {
                    rig.reducedMotion=false;rig.SamplePose((yaw+pose/8f)*rig.flapPeriod);
                    foreach(var renderer in birdRenderers)Include(renderer);
                }
            }
            // The cradle stays fixed while the bird turns; include it once in the same fit.
            foreach(var renderer in dockRenderers)Include(renderer);
            var centre=(min+max)*.5f;
            float distance=Mathf.Max(right-centre.x/tanX,left+centre.x/tanX,top-centre.y/tanY,bottom+centre.y/tanY)+centre.z;
            var focus=rotation*centre;
            camera.transform.SetPositionAndRotation(focus+rotation*Vector3.back*Mathf.Max(.1f,distance),rotation);
            rig.ResetPose();turntable.localRotation=Quaternion.identity;

            void Include(Renderer renderer)
            {
                if(!renderer.enabled)return;
                var local=renderer.localBounds;var matrix=renderer.localToWorldMatrix;
                for(int corner=0;corner<8;corner++)
                {
                    var p=inverse*matrix.MultiplyPoint3x4(local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                    min=Vector3.Min(min,p);max=Vector3.Max(max,p);
                    right=Mathf.Max(right,p.x/tanX-p.z);left=Mathf.Max(left,-p.x/tanX-p.z);
                    top=Mathf.Max(top,p.y/tanY-p.z);bottom=Mathf.Max(bottom,-p.y/tanY-p.z);
                }
            }
        }
        void AddDockSegments(Transform parent,Material material,int count,float separation,float sweep,float inner,float outer,float bottom,float top,string name)
        {
            const int steps=6;
            var profile=new[]{new Vector2(inner,bottom),new Vector2(outer,bottom),new Vector2(outer,top-.0006f),new Vector2(outer-.0015f,top),new Vector2(inner+.0015f,top),new Vector2(inner,top-.0006f),new Vector2(inner,bottom)};
            int stride=profile.Length,span=(steps+1)*stride;
            var vertices=new Vector3[count*span];var triangles=new int[count*(steps*(stride-1)*6+(stride-3)*6)];int next=0;
            for(int segment=0;segment<count;segment++)
            {
                for(int i=0;i<=steps;i++)
                {
                    float angle=(segment*separation-sweep*.5f+sweep*i/steps)*Mathf.Deg2Rad;
                    for(int j=0;j<stride;j++)vertices[segment*span+i*stride+j]=new Vector3(Mathf.Cos(angle)*profile[j].x,profile[j].y,Mathf.Sin(angle)*profile[j].x);
                    if(i==steps)continue;
                    for(int j=0;j<stride-1;j++)
                    {int a=segment*span+i*stride+j,b=a+stride;triangles[next++]=a;triangles[next++]=b;triangles[next++]=a+1;triangles[next++]=a+1;triangles[next++]=b;triangles[next++]=b+1;}
                }
                // Both swept ends are capped, retaining real physical depth
                // rather than leaving an open illuminated ribbon.
                int first=segment*span,last=first+steps*stride;
                for(int j=1;j<stride-2;j++)
                {triangles[next++]=first;triangles[next++]=first+j+1;triangles[next++]=first+j;triangles[next++]=last;triangles[next++]=last+j;triangles[next++]=last+j+1;}
            }
            var mesh=new Mesh{name=name,vertices=vertices,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
            var part=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(parent,false);
            part.GetComponent<MeshFilter>().sharedMesh=mesh;part.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        void AddRing(Transform parent,Material material,Vector2[] profile)
        {
            const int segments=64;var vertices=new Vector3[segments*profile.Length];var triangles=new int[segments*(profile.Length-1)*6];int next=0;
            for(int i=0;i<segments;i++)
            {
                float angle=i*Mathf.PI*2/segments;
                for(int j=0;j<profile.Length;j++)vertices[i*profile.Length+j]=new Vector3(Mathf.Cos(angle)*profile[j].x,profile[j].y,Mathf.Sin(angle)*profile[j].x);
                for(int j=0;j<profile.Length-1;j++)
                {int a=i*profile.Length+j,b=((i+1)%segments)*profile.Length+j;triangles[next++]=a;triangles[next++]=b;triangles[next++]=a+1;triangles[next++]=a+1;triangles[next++]=b;triangles[next++]=b+1;}
            }
            var mesh=new Mesh{name="Display cradle · continuous bevelled annulus",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
            var part=new GameObject("Recessed display housing",typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(parent,false);
            part.GetComponent<MeshFilter>().sharedMesh=mesh;part.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        public void Dispose()
        {if(surface!=null)UnityEngine.Object.DestroyImmediate(surface.gameObject);preview.camera.targetTexture=null;preview.Cleanup();foreach(var item in owned)UnityEngine.Object.DestroyImmediate(item);owned.Clear();texture.Release();UnityEngine.Object.DestroyImmediate(texture);}
    }
}
