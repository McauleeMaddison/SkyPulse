// Editor-only: isolated saves and real UI handlers. Never ship these fixtures.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SkyPulse.Mobile;

[InitializeOnLoad]
public static class SkyPulseScoreChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);Debug.Log("SCORE_CHECK: "+text);}
    static SkyPulseScoreChecks(){EditorApplication.update+=Tick;}
    public static void Run(){if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Isolated QA required");PlayerPrefs.DeleteAll();PlayerPrefs.Save();SessionState.SetBool("ScoreQa",true);EditorApplication.EnterPlaymode();}
    static void Tick()
    {
        if(!SessionState.GetBool("ScoreQa",false)||!EditorApplication.isPlaying)return;
        SessionState.SetBool("ScoreQa",false);new GameObject("Result QA runner").AddComponent<SkyPulseWorldExpansionRunner>().Begin(Standalone());
    }
    static IEnumerator Standalone()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<SkyPulseNativeGame>();if(game==null)game=new GameObject("QA game").AddComponent<SkyPulseNativeGame>();
        yield return Live(game);Debug.Log("SKYPULSE_SCORE_CAPTURE_PASS");EditorApplication.Exit(0);
    }
    public static IEnumerator Live(SkyPulseNativeGame game)
    {
        if(Application.companyName!="SkyPulseQA20261006")throw new Exception("Isolated QA required");
        game.enabled=false;Set(game,"hapticsEnabled",false);Set(game,"reduceMotionEnabled",false);
        ((GameObject)Get(game,"flightGuideScreen")).SetActive(false);
        var camera=(Camera)Get(game,"flightCamera");var canvas=((GameObject)Get(game,"uiRoot")).GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        string folder=Path.Combine(Environment.GetEnvironmentVariable("SKYPULSE_QA_OUTPUT"),"results");Directory.CreateDirectory(folder);
        foreach(var size in new[]{new Vector2Int(640,1136),new Vector2Int(1320,2868),new Vector2Int(1640,2360)})
        {
            var rt=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);Call(game,"RefreshViewportDecor");
            foreach(string scene in new[]{"endless","relay","blackout"})
            {
                Call(game,"ResetToMenu");
                if(scene=="endless")
                {
                    Call(game,"SetHomeProtocol",SkyPulseMode.EndlessRoute);Call(game,"StartFlight");Set(game,"score",27);Set(game,"runCrystalsCollected",36);Call(game,"EndFlight");
                    Set(game,"state",Enum.Parse(Get(game,"state").GetType(),"GameOver"));Call(game,"RefreshScreens");
                }
                else if(scene=="relay"){Call(game,"LaunchMission",SkyPulseMode.PulseRelay,0);Call(game,"FinishRelayFailure","GROUND CONTACT");}
                else
                {
                    Call(game,"LaunchMission",SkyPulseMode.BlackoutProtocol,0);var s=(BlackoutSession)Get(game,"blackoutSession");
                    while(s.State==MissionSessionState.Playing)Call(game,"TouchBlackoutNode",6);
                }
                var root=(GameObject)Get(game,scene=="endless"?"gameOverScreen":"missionResultScreen");Check(root.activeSelf,"Real result flow visible: "+scene);
                Canvas.ForceUpdateCanvases();Call(game,"ApplySafeArea");Call(game,"FitInterfaceToSafeArea");Canvas.ForceUpdateCanvases();yield return null;
                var glasses=root.GetComponentsInChildren<SkyPulseScoreGlass>();Check(glasses.Length>0,"Holographic glass exists: "+scene);
                foreach(var glass in glasses)Check(!glass.raycastTarget&&glass.material.shader.isSupported,"Supported static glass cannot intercept input: "+scene);
                foreach(var text in root.GetComponentsInChildren<Text>())
                {
                    Check(text.GetComponent<SkyPulseHolographicText>()!=null&&!text.raycastTarget,"One readable text mesh: "+text.text);
                    foreach(var shadow in text.GetComponents<Shadow>())Check(!shadow.enabled,"No doubled lettering: "+text.text);
                }
                foreach(var button in root.GetComponentsInChildren<Button>())
                {
                    Check(button.GetComponentInChildren<Text>().color.maxColorComponent>=.7f,"Action lettering stays light over dark glass");
                    var rect=(RectTransform)button.transform;
                    var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
                    var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                    Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject,"Actual centre raycast reaches result action: "+button.GetComponentInChildren<Text>().text);
                }
                int score=(int)Get(game,"score"),wallet=(int)Get(game,"crystals");Call(game,"UpdateScoreFinish",.5f);
                Check((int)Get(game,"score")==score&&(int)Get(game,"crystals")==wallet,"Finish cannot change scores or rewards");
                foreach(var stage in UnityEngine.Object.FindObjectsByType<SkyPulseDepthStage>(FindObjectsSortMode.None))stage.Render();
                Save(camera,rt,Path.Combine(folder,scene+"-"+size.x+".png"));
                if(size.x==640)
                {
                    string motion=Path.Combine(folder,scene+"-motion");Directory.CreateDirectory(motion);
                    for(int frame=0;frame<90;frame++){Call(game,"UpdateScoreFinish",1f/30);Save(camera,rt,Path.Combine(motion,frame.ToString("000")+".png"));}
                }
            }
            camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);
        }
        foreach(int fps in new[]{30,60,120})
        {
            Set(game,"scoreFinishClock",0f);for(int i=0;i<fps;i++)Call(game,"UpdateScoreFinish",1f/fps);
            Check(Mathf.Abs((float)Get(game,"scoreFinishClock")-1)<.00001f,"Frame-independent finish at "+fps+" FPS");
        }
        Set(game,"reduceMotionEnabled",true);float clock=(float)Get(game,"scoreFinishClock");Call(game,"UpdateScoreFinish",2f);
        Check((float)Get(game,"scoreFinishClock")==clock&&((Material)Get(game,"scoreGlassMaterial")).GetFloat("_Motion")==0&&((Material)Get(game,"scoreTypographyMaterial")).GetFloat("_Motion")==0,"Reduced Motion freezes both finishes");
        Set(game,"reduceMotionEnabled",false);Set(game,"state",Enum.Parse(Get(game,"state").GetType(),"Paused"));Call(game,"UpdateScoreFinish",2f);
        Check((float)Get(game,"scoreFinishClock")==clock,"Pause freezes finish clock");
        Set(game,"state",Enum.Parse(Get(game,"state").GetType(),"GameOver"));
        var update=(Action<float>)Delegate.CreateDelegate(typeof(Action<float>),game,typeof(SkyPulseNativeGame).GetMethod("UpdateScoreFinish",F));for(int i=0;i<50;i++)update(1f/60);
        long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)update(1f/60);long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
        Check(bytes==0,"Warmed finish update allocates zero managed bytes");Call(game,"ResetToMenu");Debug.Log("SKYPULSE_SCORE_FINISH_PASS");
    }
    static void Save(Camera camera,RenderTexture rt,string file)
    {
        camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;
    }
}
