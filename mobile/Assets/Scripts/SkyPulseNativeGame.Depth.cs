using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        SkyPulseDepthStage homeDepth;
        sealed class RouteDepth
        {
            public PipeSurface Surface;public Transform Body,Cap;public MeshRenderer BodyRenderer,CapRenderer;public SpriteRenderer[] Retired;
        }
        readonly List<RouteDepth> routeDepth=new List<RouteDepth>(8);
        Mesh routeBodyMesh,routeCapMesh;Material routeMetal,routeCapMetal;
        SkyPulseDepthStage DepthStage(Transform parent,SkyPulseMode mode,Vector2 position,Vector2 size)
        {
            var g=new GameObject("SkyPulse 3D stage",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage),typeof(SkyPulseDepthStage));g.transform.SetParent(parent,false);
            var raw=g.GetComponent<RawImage>();raw.rectTransform.anchoredPosition=position;raw.rectTransform.sizeDelta=size;
            var stage=g.GetComponent<SkyPulseDepthStage>();stage.Mode=mode;stage.ReducedMotion=reduceMotionEnabled;stage.Build(raw);return stage;
        }
        void EnsureHomeDepth()
        {
            if(homeDepth==null){homeDepth=DepthStage(homeScreen.transform,homeProtocol,new Vector2(0,355),new Vector2(930,558));homeDepth.transform.SetSiblingIndex(menuHeroTransform.GetSiblingIndex());}
            homeDepth.Mode=homeProtocol;homeDepth.ReducedMotion=reduceMotionEnabled;homeDepth.MotionPaused=false;homeDepth.RestoredSystems=0;
            if(blackoutCampaign!=null)foreach(int medal in blackoutCampaign.Data.medals)if(medal>0)homeDepth.RestoredSystems++;
            homeDepth.Tick(0);
        }
        void UpdateDepthPresentation(float dt)
        {
            UpdateProgressionPresentation(dt);
            if(homeScreen!=null&&homeScreen.activeInHierarchy){EnsureHomeDepth();homeDepth.ReducedMotion=reduceMotionEnabled;}
            if(holographicTree!=null)holographicTree.ReducedMotion=reduceMotionEnabled;
            if(pipePool[0]==null)return;
            if(routeMetal==null)BuildEndlessDepth();
            var sceneTint=SceneLightTint();routeMetal.SetColor("_LightTint",sceneTint);routeCapMetal.SetColor("_LightTint",sceneTint);
            float lightClock=reduceMotionEnabled?0:cinematicClock;routeMetal.SetFloat("_Clock",lightClock);routeCapMetal.SetFloat("_Clock",lightClock);
            float departure=worldTransitionTimer>0?1-Mathf.SmoothStep(0,1,(1-worldTransitionTimer/WorldTransitionSeconds)/.25f):1;
            routeMetal.SetFloat("_Visibility",departure);routeCapMetal.SetFloat("_Visibility",departure);
            bool show=activeMode==SkyPulseMode.EndlessRoute&&(state==FlightState.Playing||state==FlightState.Impact||state==FlightState.GameOver||state==FlightState.Paused);
            for(int i=0;i<routeDepth.Count;i++)
            {
                var visual=routeDepth[i];var surface=visual.Surface;visual.Body.gameObject.SetActive(show);visual.Cap.gameObject.SetActive(show);if(!show)continue;
                // Colliders intentionally overlap. The visible shaft must stop
                // inside the far shoulder, rather than entering the open lip.
                // Read collision shapes only: never change the tested opening.
                var body=surface.BodyCollider;var cap=surface.CapCollider;
                var bodyCentre=body.transform.localPosition+new Vector3(body.offset.x,body.offset.y,.10f);
                var capCentre=cap.transform.localPosition+new Vector3(cap.offset.x,cap.offset.y,.10f);
                float direction=bodyCentre.y>capCentre.y?1:-1;
                float farEnd=bodyCentre.y+direction*body.size.y*.5f;
                float shoulder=capCentre.y+direction*(cap.size.y*.5f-.04f);
                bodyCentre.y=(farEnd+shoulder)*.5f;
                visual.Body.localPosition=bodyCentre;visual.Body.localScale=new Vector3(body.size.x,Mathf.Max(.01f,Mathf.Abs(farEnd-shoulder)),body.size.x);
                visual.Cap.localPosition=capCentre;visual.Cap.localScale=new Vector3(cap.size.x,cap.size.y,cap.size.x);
                for(int k=0;k<visual.Retired.Length;k++)visual.Retired[k].enabled=false;
            }
        }
        void BuildEndlessDepth()
        {
            routeMetal=OriginalRouteMaterial(pipeBodySprite,"Original neon shaft on 3D metal");
            routeCapMetal=OriginalRouteMaterial(pipeCapSprite,"Original neon collar on 3D metal",true);
            routeBodyMesh=RouteCylinder(false);routeCapMesh=RouteCylinder(true);
            var properties=new MaterialPropertyBlock();
            foreach(var pair in pipePool)foreach(var s in new[]{pair.Top,pair.Bottom})
            {
                var body=SkyPulseMeshBuilder.Surface(pair.Root.transform,"3D extruded gate column",routeBodyMesh,routeMetal,5);var cap=SkyPulseMeshBuilder.Surface(pair.Root.transform,"3D bevelled gate collar",routeCapMesh,routeCapMetal,11);
                properties.SetFloat("_ArtFlip",s==pair.Top?1:0);body.SetPropertyBlock(properties);cap.SetPropertyBlock(properties);
                routeDepth.Add(new RouteDepth{Surface=s,Body=body.transform,Cap=cap.transform,BodyRenderer=body,CapRenderer=cap,Retired=new[]{s.Artwork,s.Outer,s.Panel,s.Shade,s.RailLeft,s.RailRight,s.Core,s.CorePulse,s.Highlight,s.Energy,s.Scan,s.Beacon,s.CapGlow,s.CapOuter,s.CapAccent,s.CapPanel,s.CapEnergy}});
            }
        }
        Material OriginalRouteMaterial(Sprite artwork,string label,bool cutout=false)
        {
            var material=new Material(Resources.Load<Shader>("SkyPulse/Shaders/EndlessRouteMetal")){name=label};
            material.SetFloat("_Cutout",cutout?1:0);
            var rect=artwork.rect;var texture=artwork.texture;
            material.mainTexture=texture;material.SetVector("_ArtRect",new Vector4(rect.x/texture.width,rect.y/texture.height,rect.width/texture.width,rect.height/texture.height));return material;
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
                    float dy=q.y-p.y,dr=q.x-p.x;
                    var na=new Vector3(Mathf.Cos(a)*dy,-dr,Mathf.Sin(a)*dy).normalized;var nb=new Vector3(Mathf.Cos(c)*dy,-dr,Mathf.Sin(c)*dy).normalized;
                    b.SmoothQuad(new Vector3(Mathf.Cos(a)*p.x,p.y,Mathf.Sin(a)*p.x),new Vector3(Mathf.Cos(c)*p.x,p.y,Mathf.Sin(c)*p.x),new Vector3(Mathf.Cos(c)*q.x,q.y,Mathf.Sin(c)*q.x),new Vector3(Mathf.Cos(a)*q.x,q.y,Mathf.Sin(a)*q.x),na,nb,colour);
                }
                foreach(int side in new[]{-1,1})b.Face(new Vector3(0,side*.5f,0),new Vector3(Mathf.Cos(a)*.46f,side*.5f,Mathf.Sin(a)*.46f),new Vector3(Mathf.Cos(c)*.46f,side*.5f,Mathf.Sin(c)*.46f),new Color(.9f,.95f,1,.2f));
            }
            return b.Finish(cap?"Extruded precision gate collar":"Reflective cylindrical Endless column");
        }
        void OnDestroy()
        {
            if(techBackdropMaterial!=null)Destroy(techBackdropMaterial);
            if(techTypographyMaterial!=null)Destroy(techTypographyMaterial);
            if(progressionTypographyMaterial!=null)Destroy(progressionTypographyMaterial);
            if(routeBodyMesh!=null)Destroy(routeBodyMesh);if(routeCapMesh!=null)Destroy(routeCapMesh);if(routeMetal!=null)Destroy(routeMetal);
            if(routeCapMetal!=null)Destroy(routeCapMetal);
        }
    }
}
