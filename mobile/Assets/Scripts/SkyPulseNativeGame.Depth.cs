using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        SkyPulseDepthStage homeDepth,resultDepth;
        sealed class RouteDepth
        {
            public PipeSurface Surface;public Transform Body,Cap;public MeshRenderer BodyRenderer,CapRenderer;public SpriteRenderer[] Retired;
        }
        readonly List<RouteDepth> routeDepth=new List<RouteDepth>(8);
        Mesh routeBodyMesh,routeCapMesh;Material routeMetal;
        SkyPulseDepthStage DepthStage(Transform parent,SkyPulseMode mode,Vector2 position,Vector2 size)
        {
            var g=new GameObject("SkyPulse 3D stage",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage),typeof(SkyPulseDepthStage));g.transform.SetParent(parent,false);
            var raw=g.GetComponent<RawImage>();raw.rectTransform.anchoredPosition=position;raw.rectTransform.sizeDelta=size;
            var stage=g.GetComponent<SkyPulseDepthStage>();stage.Mode=mode;stage.ReducedMotion=reduceMotionEnabled;stage.Build(raw);return stage;
        }
        void EnsureHomeDepth()
        {
            if(homeDepth==null){homeDepth=DepthStage(homeScreen.transform,homeProtocol,new Vector2(0,355),new Vector2(930,558));homeDepth.transform.SetSiblingIndex(menuHeroTransform.GetSiblingIndex());}
            homeDepth.Mode=homeProtocol;homeDepth.ReducedMotion=reduceMotionEnabled;homeDepth.MotionPaused=false;homeDepth.Tick(0);
        }
        void UpdateDepthPresentation(float dt)
        {
            UpdateProgressionPresentation(dt);
            if(homeScreen!=null&&homeScreen.activeInHierarchy){EnsureHomeDepth();homeDepth.ReducedMotion=reduceMotionEnabled;}
            if(gameOverScreen!=null&&gameOverScreen.activeInHierarchy&&activeMode==SkyPulseMode.EndlessRoute)
            {
                if(resultDepth==null){resultDepth=DepthStage(gameOverScreen.transform,SkyPulseMode.EndlessRoute,new Vector2(0,375),new Vector2(880,528));resultDepth.transform.SetSiblingIndex(2);}resultDepth.ReducedMotion=reduceMotionEnabled;
            }
            if(holographicTree!=null)holographicTree.ReducedMotion=reduceMotionEnabled;
            if(pipePool[0]==null)return;
            if(routeMetal==null)BuildEndlessDepth();
            bool show=activeMode==SkyPulseMode.EndlessRoute&&(state==FlightState.Playing||state==FlightState.Impact||state==FlightState.GameOver||state==FlightState.Paused);
            for(int i=0;i<routeDepth.Count;i++)
            {
                var visual=routeDepth[i];var surface=visual.Surface;visual.Body.gameObject.SetActive(show);visual.Cap.gameObject.SetActive(show);if(!show)continue;
                // Read only the collider dimensions already laid out by Endless.
                // The new meshes cannot change spacing, openings or collisions.
                var body=surface.BodyCollider;var cap=surface.CapCollider;
                visual.Body.localPosition=body.transform.localPosition+new Vector3(body.offset.x,body.offset.y,.10f);visual.Body.localScale=new Vector3(body.size.x,body.size.y,body.size.x);
                visual.Cap.localPosition=cap.transform.localPosition+new Vector3(cap.offset.x,cap.offset.y,.10f);visual.Cap.localScale=new Vector3(cap.size.x,cap.size.y,cap.size.x);
                for(int k=0;k<visual.Retired.Length;k++)visual.Retired[k].enabled=false;
            }
        }
        void BuildEndlessDepth()
        {
            routeMetal=new Material(Resources.Load<Shader>("SkyPulse/Shaders/HeistOptics")){name="Endless machined cobalt gates"};routeMetal.SetColor("_BaseColor",new Color(.065f,.16f,.25f));routeMetal.SetFloat("_Roughness",.25f);routeMetal.SetColor("_GlowColor",new Color(.15f,.65f,1));routeMetal.SetFloat("_Energy",.10f);
            routeBodyMesh=RouteCylinder(false);routeCapMesh=RouteCylinder(true);
            foreach(var pair in pipePool)foreach(var s in new[]{pair.Top,pair.Bottom})
            {
                var body=SkyPulseMeshBuilder.Surface(pair.Root.transform,"3D extruded gate column",routeBodyMesh,routeMetal,5);var cap=SkyPulseMeshBuilder.Surface(pair.Root.transform,"3D bevelled gate collar",routeCapMesh,routeMetal,11);
                routeDepth.Add(new RouteDepth{Surface=s,Body=body.transform,Cap=cap.transform,BodyRenderer=body,CapRenderer=cap,Retired=new[]{s.Artwork,s.Outer,s.Panel,s.Shade,s.RailLeft,s.RailRight,s.Core,s.CorePulse,s.Highlight,s.Energy,s.Scan,s.Beacon,s.CapOuter,s.CapAccent,s.CapPanel,s.CapEnergy}});
            }
        }
        Mesh RouteCylinder(bool cap)
        {
            var b=new SkyPulseMeshBuilder();int segments=48;var profile=cap?new[]{new Vector2(.46f,-.5f),new Vector2(.50f,-.39f),new Vector2(.50f,.39f),new Vector2(.46f,.5f)}:new[]{new Vector2(.47f,-.5f),new Vector2(.5f,-.48f),new Vector2(.5f,.48f),new Vector2(.47f,.5f)};
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,c=(i+1)*Mathf.PI*2/segments;
                for(int j=1;j<profile.Length;j++)
                {
                    var p=profile[j-1];var q=profile[j];Color colour=cap?new Color(.7f,.9f,1,j==2?0:1):new Color(.85f,.95f,1,i%12==0?1:0);
                    b.Quad(new Vector3(Mathf.Cos(a)*p.x,p.y,Mathf.Sin(a)*p.x),new Vector3(Mathf.Cos(c)*p.x,p.y,Mathf.Sin(c)*p.x),new Vector3(Mathf.Cos(c)*q.x,q.y,Mathf.Sin(c)*q.x),new Vector3(Mathf.Cos(a)*q.x,q.y,Mathf.Sin(a)*q.x),colour);
                }
                foreach(int side in new[]{-1,1})b.Face(new Vector3(0,side*.5f,0),new Vector3(Mathf.Cos(a)*.46f,side*.5f,Mathf.Sin(a)*.46f),new Vector3(Mathf.Cos(c)*.46f,side*.5f,Mathf.Sin(c)*.46f),new Color(.9f,.95f,1,.2f));
            }
            return b.Finish(cap?"Extruded precision gate collar":"Reflective cylindrical Endless column");
        }
        void OnDestroy()
        {
            if(techBackdropMaterial!=null)Destroy(techBackdropMaterial);
            if(routeBodyMesh!=null)Destroy(routeBodyMesh);if(routeCapMesh!=null)Destroy(routeCapMesh);if(routeMetal!=null)Destroy(routeMetal);
        }
    }
}
