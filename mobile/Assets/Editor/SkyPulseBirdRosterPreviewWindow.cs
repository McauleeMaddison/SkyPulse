using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using SkyPulse.Mobile;

public sealed class SkyPulseBirdRosterPreviewWindow:EditorWindow
{
    static readonly string[] Tabs={"Studio","In game"};
    [SerializeField] int tab,studioIndex=1;
    [SerializeField] bool animate=true,turntable;
    [SerializeField] Vector2 orbit=new Vector2(-22,14);
    [SerializeField] float zoom=1;
    string[] names,ids,paths,gameNames,gameIds;
    PreviewRenderUtility preview;
    GameObject model,reflectionObject;
    SkyPulseNeonFinchPrototype rig;
    Renderer[] renderers;
    Bounds bounds;
    float radius=1,clock;
    double lastUpdate,nextRepaint;
    string previewError;

    [MenuItem("Tools/SkyPulse/Bird Workshop/3D roster in actual gameplay")]
    public static void Open()
    {
        // Opening the gameplay preview must actually enable its solid bird.
        // Keep the chosen model; gameplay stats and player preferences are untouched.
        SkyPulseBirdRosterPreview.Select(SkyPulseBirdRosterPreview.SelectedId, true);
        ShowTab(1);
    }
    [MenuItem("Tools/SkyPulse/Bird Workshop/3D roster in actual gameplay", true)]
    static bool ValidateGameplayPreview()
    {
        Menu.SetChecked("Tools/SkyPulse/Bird Workshop/3D roster in actual gameplay", SkyPulseBirdRosterPreview.Enabled);
        return true;
    }
    [MenuItem("Tools/SkyPulse/Bird Workshop/3D bird studio")]
    public static void OpenStudio(){ShowTab(0);}
    static void ShowTab(int value)
    {var window=GetWindow<SkyPulseBirdRosterPreviewWindow>("3D Bird Review");window.tab=value;if(value==1)window.DisposePreview();window.minSize=new Vector2(420,360);window.Show();window.Repaint();}
    void OnEnable()
    {
        LoadRoster();lastUpdate=EditorApplication.timeSinceStartup;
        EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=DisposePreview;
    }
    void OnDisable()
    {EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=DisposePreview;DisposePreview();}
    void OnDestroy(){DisposePreview();}
    void OnProjectChange(){DisposePreview();previewError=null;Repaint();}
    void LoadRoster()
    {
        var designs=SkyPulseBirdRosterWorkshop.LoadDesigns();int count=designs.Length+1;
        names=new string[count];ids=new string[count];paths=new string[count];
        gameNames=new string[count+1];gameIds=new string[count+1];
        names[0]="Neon Finch";ids[0]="neon_finch";paths[0]=SkyPulseBirdPrototypeWorkshop.PrefabPath;
        gameNames[0]="Use equipped bird";gameIds[0]="";
        for(int i=0;i<designs.Length;i++){names[i+1]=designs[i].name;ids[i+1]=designs[i].id;paths[i+1]=SkyPulseBirdRosterWorkshop.PrefabPath(designs[i]);}
        for(int i=0;i<count;i++){gameNames[i+1]=names[i];gameIds[i+1]=ids[i];}
        studioIndex=Mathf.Clamp(studioIndex,0,count-1);
    }
    void Tick()
    {
        double now=EditorApplication.timeSinceStartup;float dt=(float)Math.Min(.1,Math.Max(0,now-lastUpdate));lastUpdate=now;
        if(tab!=0||preview==null||model==null)return;
        if(animate)clock+=dt;
        if(now>=nextRepaint){nextRepaint=now+1.0/30;Repaint();}
    }
    void OnGUI()
    {
        if(names==null)LoadRoster();
        int selectedTab=GUILayout.Toolbar(tab,Tabs);
        if(selectedTab!=tab){tab=selectedTab;lastUpdate=EditorApplication.timeSinceStartup;if(tab==1)DisposePreview();}
        if(tab==0)DrawStudio();else DrawGame();
    }
    void DrawGame()
    {
        GUILayout.Label("Live 3D bird review",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Play the normal SkyPulse scene. This changes only the Editor bird visual; the equipped bird's stats, controls and player save stay in use.",MessageType.Info);
        int old=Array.IndexOf(gameIds,SkyPulseBirdRosterPreview.SelectedId),selected=EditorGUILayout.Popup("Model",Mathf.Max(0,old),gameNames);
        bool enabled=EditorGUILayout.Toggle("Show 3D model",SkyPulseBirdRosterPreview.Enabled);
        if(old!=selected||enabled!=SkyPulseBirdRosterPreview.Enabled)SkyPulseBirdRosterPreview.Select(gameIds[selected],enabled);
        if(GUILayout.Button("Open SkyPulse game scene"))
        {if(!EditorApplication.isPlaying&&EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene("Assets/Scenes/SkyPulse.unity");}
        GUILayout.Label("Home, Hangar and both flight modes show the saved 3D birds.\nUse the dropdown to inspect a model without changing its stats.",EditorStyles.wordWrappedLabel);
        if(GUILayout.Button("Stop 3D visual preview"))SkyPulseBirdRosterPreview.Select("",false);
    }
    void DrawStudio()
    {
        int selected=EditorGUILayout.Popup("Bird",studioIndex,names);
        if(selected!=studioIndex){studioIndex=selected;DisposePreview();previewError=null;Frame();}
        using(new EditorGUILayout.HorizontalScope())
        {
            animate=GUILayout.Toggle(animate,"Animate",EditorStyles.miniButton);
            turntable=GUILayout.Toggle(turntable,"Turntable",EditorStyles.miniButton);
            if(GUILayout.Button("Frame / reset",EditorStyles.miniButton))Frame();
        }
        GUILayout.Label("Drag to orbit · Scroll to zoom",EditorStyles.centeredGreyMiniLabel);
        Rect area=GUILayoutUtility.GetRect(100,100,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));
        HandleOrbit(area);
        if(preview==null&&previewError==null)CreatePreview();
        if(previewError!=null){EditorGUI.HelpBox(area,previewError,MessageType.Warning);return;}
        if(Event.current.type!=EventType.Repaint||preview==null||rig==null||area.width<1||area.height<1)return;
        rig.turntable=turntable;rig.SamplePose(clock);
        var camera=preview.camera;float aspect=area.width/area.height;
        float halfFov=15*Mathf.Deg2Rad;float limitingFov=Mathf.Min(halfFov,Mathf.Atan(Mathf.Tan(halfFov)*aspect));
        float distance=radius/Mathf.Sin(limitingFov)*zoom*1.08f;
        Quaternion rotation=Quaternion.Euler(orbit.y,orbit.x,0);
        camera.transform.SetPositionAndRotation(bounds.center+rotation*Vector3.back*distance,rotation);
        camera.nearClipPlane=Mathf.Max(.01f,distance-radius*1.8f);camera.farClipPlane=distance+radius*4+10;
        preview.BeginPreview(area,GUIStyle.none);
        try{preview.Render();}
        finally{preview.EndAndDrawPreview(area);}
    }
    void Frame(){orbit=new Vector2(-22,14);zoom=1;clock=0;Repaint();}
    void HandleOrbit(Rect area)
    {
        var e=Event.current;int control=GUIUtility.GetControlID(FocusType.Passive,area);
        if(e.type==EventType.MouseDown&&e.button==0&&area.Contains(e.mousePosition))
        {GUIUtility.hotControl=control;e.Use();}
        else if(e.type==EventType.MouseDrag&&GUIUtility.hotControl==control)
        {orbit.x-=e.delta.x*.4f;orbit.y=Mathf.Clamp(orbit.y-e.delta.y*.4f,-75,75);e.Use();Repaint();}
        else if(e.type==EventType.MouseUp&&GUIUtility.hotControl==control)
        {GUIUtility.hotControl=0;e.Use();}
        else if(e.type==EventType.ScrollWheel&&area.Contains(e.mousePosition))
        {zoom=Mathf.Clamp(zoom*Mathf.Exp(e.delta.y*.065f),.4f,2.5f);e.Use();Repaint();}
        EditorGUIUtility.AddCursorRect(area,MouseCursor.Orbit);
    }
    void CreatePreview()
    {
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(paths[studioIndex]);
            if(prefab==null)throw new InvalidOperationException("The saved bird prefab is unavailable.");
            preview=new PreviewRenderUtility();preview.cameraFieldOfView=30;
            preview.camera.orthographic=false;preview.camera.clearFlags=CameraClearFlags.SolidColor;
            preview.camera.backgroundColor=new Color(.025f,.032f,.043f);
            preview.ambientColor=new Color(.12f,.14f,.18f);
            preview.lights[0].transform.rotation=Quaternion.Euler(35,35,0);preview.lights[0].color=new Color(1,.985f,.96f);preview.lights[0].intensity=1.1f;
            preview.lights[1].transform.rotation=Quaternion.Euler(335,215,0);preview.lights[1].color=new Color(.88f,.94f,1);preview.lights[1].intensity=.7f;
            model=preview.InstantiatePrefabInScene(prefab);model.name=names[studioIndex]+" · unsaved studio preview";
            var transforms=model.GetComponentsInChildren<Transform>(true);
            for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.hideFlags=HideFlags.HideAndDontSave;
            rig=model.GetComponent<SkyPulseNeonFinchPrototype>();
            if(rig==null)throw new InvalidOperationException("The saved model has no articulated bird rig.");
            rig.animate=false;rig.turntable=false;rig.reducedMotion=false;
            rig.flapPeriod=1.65f;rig.flapDegrees=28;rig.bankDegrees=5;rig.turntableDegreesPerSecond=12;rig.ResetPose();
            renderers=model.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0)throw new InvalidOperationException("The saved model contains no rendered geometry.");
            bounds=renderers[0].bounds;
            for(int i=0;i<renderers.Length;i++){bounds.Encapsulate(renderers[i].bounds);renderers[i].reflectionProbeUsage=ReflectionProbeUsage.Simple;}
            radius=Mathf.Max(.1f,bounds.extents.magnitude);
            // An instance-only local probe supplies the saved studio cubemap.
            // Active-scene RenderSettings and shared source materials are untouched.
            var reflection=AssetDatabase.LoadAssetAtPath<Cubemap>(SkyPulseBirdStudioLighting.ReflectionPath);
            if(reflection!=null)
            {
                reflectionObject=new GameObject("Unsaved studio reflection",typeof(ReflectionProbe)){hideFlags=HideFlags.HideAndDontSave};
                var probe=reflectionObject.GetComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=reflection;
                probe.intensity=.85f;probe.size=Vector3.one*(radius*20);probe.center=bounds.center;probe.boxProjection=false;
                preview.AddSingleGO(reflectionObject);
            }
            clock=0;lastUpdate=EditorApplication.timeSinceStartup;
        }
        catch(Exception error){previewError=error.Message;DisposePreview();}
    }
    void DisposePreview()
    {
        if(preview!=null){preview.Cleanup();preview=null;}
        if(model!=null)DestroyImmediate(model);if(reflectionObject!=null)DestroyImmediate(reflectionObject);
        model=null;reflectionObject=null;rig=null;renderers=null;
    }
}
