// Disposable Editor QA: actual Canvas raycasts, not simulated hardware taps.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SkyPulse.Mobile;

public static class SkyPulseTouchComfortChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);Debug.Log("TOUCH_COMFORT_CHECK: "+s);}
    static Rect Bounds(Button b,Camera camera)
    {
        var rect=(RectTransform)b.transform;var padding=b.GetComponent<Image>().raycastPadding;
        Vector2 min=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(new Vector3(rect.rect.xMin+padding.x,rect.rect.yMin+padding.y,0)));
        Vector2 max=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(new Vector3(rect.rect.xMax-padding.z,rect.rect.yMax-padding.w,0)));
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    static GameObject Hit(Vector2 point)
    {
        var results=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},results);
        return results.Count==0?null:ExecuteEvents.GetEventHandler<IPointerClickHandler>(results[0].gameObject);
    }
    static void Fit(SkyPulseNativeGame game,Vector2Int size)
    {
        Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");
        // Exercise a notched phone as well as full-area small phone/tablet.
        var safe=(RectTransform)Get(game,"safeAreaRoot");
        safe.anchorMin=size.x==1320?new Vector2(0,102f/2868):Vector2.zero;
        safe.anchorMax=size.x==1320?new Vector2(1,2692f/2868):Vector2.one;
        safe.offsetMin=safe.offsetMax=Vector2.zero;
        Canvas.ForceUpdateCanvases();Call(game,"FitInterfaceToSafeArea");Call(game,"FitRelayHudToSafeArea");Canvas.ForceUpdateCanvases();
    }
    static void Target(Button button,Camera camera,float pixelRatio,Rect safe)
    {
        var box=Bounds(button,camera);
        Check(box.width/pixelRatio>=44f&&box.height/pixelRatio>=44f,"44 pt minimum hit area: "+button.name+" = "+box.size/pixelRatio);
        Check(box.xMin>=safe.xMin-1&&box.xMax<=safe.xMax+1&&box.yMin>=safe.yMin-1&&box.yMax<=safe.yMax+1,"Hit area stays inside safe bounds: "+button.name);
        Check(Hit(box.center)==button.gameObject,"Centre raycast reaches its actual Button: "+button.name);
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Disposable QA required");
        var camera=(Camera)Get(game,"flightCamera");var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();
        var oldTarget=camera.targetTexture;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        foreach(var size in new[]{new Vector2Int(640,1136),new Vector2Int(1320,2868),new Vector2Int(1640,2360)})
        {
            var rt=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=rt;
            float ratio=size.x==1320?3:2;
            var safe=size.x==1320?new Rect(0,102,1320,2590):new Rect(0,0,size.x,size.y);
            foreach(var mode in new[]{SkyPulseMode.EndlessRoute,SkyPulseMode.PulseRelay,SkyPulseMode.BlackoutProtocol})
            {
                Call(game,"SetHomeProtocol",mode);Call(game,"ResetToMenu");Fit(game,size);yield return null;
                var badge=((Text)Get(game,"difficultyText")).GetComponentInParent<Button>();Target(badge,camera,ratio,safe);
                Call(game,"ShowProtocolDropdown");Call(game,"UpdateProtocolPresentation",1f);Fit(game,size);yield return null;
                var rows=((RectTransform)Get(game,"modeDropdownClip")).GetComponentsInChildren<Button>();
                Check(rows.Length==3,"Mode list has exactly three choices");
                var bounds=rows.Select(b=>Bounds(b,camera)).ToArray();
                for(int i=0;i<rows.Length;i++)
                {
                    Target(rows[i],camera,ratio,safe);
                    if(i>0)Check(bounds[i-1].yMin>bounds[i].yMax,"Mode choice targets have separate touch space");
                    foreach(var label in rows[i].GetComponentsInChildren<Text>().Where(t=>t.enabled))
                        Check(label.preferredHeight<=label.rectTransform.rect.height+2,"Mode copy fits without truncation: "+label.text);
                }
                Call(game,"CloseProtocolDropdown");Call(game,"UpdateProtocolPresentation",1f);Call(game,"LaunchHomeProtocol");Fit(game,size);yield return null;
                if(mode!=SkyPulseMode.BlackoutProtocol)
                {
                    var root=(GameObject)Get(game,mode==SkyPulseMode.EndlessRoute?"hudScreen":"relayHudScreen");
                    var pause=root.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>().text=="Ⅱ");Target(pause,camera,ratio,safe);
                    Vector2 edge=RectTransformUtility.WorldToScreenPoint(camera,pause.transform.TransformPoint(new Vector3(0,-75,0)));
                    Check(Hit(edge)==pause.gameObject,"Pause responds beyond its visible face");
                    if(mode==SkyPulseMode.PulseRelay)
                    {
                        float velocity=(float)Get(game,"birdVelocity");
                        Check(!(bool)Call(game,"SubmitRelayPress",7,Time.frameCount+10,Time.unscaledTime,true,edge)&&(float)Get(game,"birdVelocity")==velocity,"Expanded pause area cannot also flap");
                        Check(!(bool)Call(game,"InteractiveUiAt",new Vector2(size.x*.5f,size.y*.4f)),"Open flight area remains available to flap");
                    }
                }
                Call(game,"PauseFlight");Fit(game,size);yield return null;
                var controls=((GameObject)Get(game,"pauseScreen")).GetComponentsInChildren<Button>();
                Check(controls.Length==5,"Pause exposes resume, three preferences and menu");
                var boxes=controls.Select(b=>Bounds(b,camera)).ToArray();
                for(int i=0;i<controls.Length;i++)
                {
                    Target(controls[i],camera,ratio,safe);
                    if(i>0)Check(!boxes[i].Overlaps(boxes[i-1]),"Pause preferences never share a hit area");
                }
                var resume=controls.First(b=>b.GetComponentInChildren<Text>().text=="RESUME");resume.onClick.Invoke();
                Check(!((GameObject)Get(game,"pauseScreen")).activeSelf,"Actual Resume restores "+mode);
                Call(game,"ResetToMenu");
            }
            camera.targetTexture=oldTarget;RenderTexture.ReleaseTemporary(rt);
        }
        Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"ResetToMenu");Call(game,"ApplySafeArea");
        Call(game,"ShowProtocolDropdown");Call(game,"UpdateProtocolPresentation",1f);yield return null;
        SkyPulsePlayerExperienceChecks.CaptureUi(game,"mode-list-touch-comfort");
        Call(game,"CloseProtocolDropdown");Call(game,"UpdateProtocolPresentation",1f);Call(game,"StartFlight");Call(game,"PauseFlight");yield return null;
        SkyPulsePlayerExperienceChecks.CaptureUi(game,"pause-touch-comfort");Call(game,"ResetToMenu");
        camera.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;Call(game,"ApplySafeArea");
        Debug.Log("SKYPULSE_TOUCH_COMFORT_PASS");
    }
}
