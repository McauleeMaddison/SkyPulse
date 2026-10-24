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
            public PipeSurface Surface;public Transform Body,Cap;public MeshRenderer BodyRenderer,CapRenderer;public SpriteRenderer[] Retired;public MeshFilter BodyFilter,CapFilter;
        }
        readonly List<RouteDepth> routeDepth=new List<RouteDepth>(8);
        Mesh routeBodyMesh,routeCapMesh;Material routeMetal,routeCapMetal;
        readonly Mesh[] routeBodyThemes=new Mesh[12],routeCapThemes=new Mesh[12];
        readonly SkyPulsePipeTheme[] authoredPipeThemes=new SkyPulsePipeTheme[12];
        int routeDepthTheme=-1;
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
            int theme=Mathf.Clamp(routeWorldIndex,0,11);
            // Departing gates keep their previous structure until the original
            // transition has fully faded them; incoming gates use the new set.
            if(departingObjectsVisible&&routeDepthTheme>=0)theme=routeDepthTheme;
            if(routeDepthTheme!=theme)
            {
                routeDepthTheme=theme;
                foreach(var visual in routeDepth){visual.BodyFilter.sharedMesh=routeBodyThemes[theme];visual.CapFilter.sharedMesh=routeCapThemes[theme];}
                for(int i=0;i<2;i++)
                {
                    var material=i==0?routeMetal:routeCapMetal;
                    material.SetFloat("_Theme",theme);material.SetFloat("_ThemeAmount",theme==0?0:1);
                    var authored=authoredPipeThemes[theme];
                    material.SetColor("_ThemeAccent",authored!=null?authored.Accent:SkyPulseRouteArchitecture.Accent(theme));material.SetColor("_ThemeEnergy",authored!=null?authored.Energy:SkyPulseRouteArchitecture.Secondary(theme));material.SetColor("_MetalTone",authored!=null?authored.Metal:SkyPulseRouteArchitecture.Metal(theme));
                    material.SetFloat("_LightRate",authored!=null?authored.LightRate:1);material.SetFloat("_Detail",authored!=null?authored.Detail:1);
                }
            }
            var sceneTint=SceneLightTint();routeMetal.SetColor("_LightTint",sceneTint);routeCapMetal.SetColor("_LightTint",sceneTint);
            float lightClock=reduceMotionEnabled?0:cinematicClock;routeMetal.SetFloat("_Clock",lightClock);routeCapMetal.SetFloat("_Clock",lightClock);
            float departure=worldTransitionTimer>0?1-Mathf.SmoothStep(0,1,(1-worldTransitionTimer/WorldTransitionSeconds)/.25f):1;
            routeMetal.SetFloat("_Visibility",departure);routeCapMetal.SetFloat("_Visibility",departure);
            bool show=activeMode==SkyPulseMode.EndlessRoute&&(state==FlightState.Playing||state==FlightState.Impact||state==FlightState.GameOver||state==FlightState.Paused);
            for(int i=0;i<routeDepth.Count;i++)
            {
                var visual=routeDepth[i];var surface=visual.Surface;visual.Body.gameObject.SetActive(show);visual.Cap.gameObject.SetActive(show);if(!show)continue;
                // The collar's alpha silhouette curves upward at its shoulders.
                // A fixed .04 overlap left daylight beside the shaft. Seat the
                // shaft inside the solid lower wall, short of the open lip.
                // This reads collision shapes; it never changes them.
                var body=surface.BodyCollider;var cap=surface.CapCollider;
                var bodyCentre=body.transform.localPosition+new Vector3(body.offset.x,body.offset.y,.10f);
                var capCentre=cap.transform.localPosition+new Vector3(cap.offset.x,cap.offset.y,.10f);
                float direction=bodyCentre.y>capCentre.y?1:-1;
                float farEnd=bodyCentre.y+direction*body.size.y*.5f;
                float shoulder=capCentre.y+direction*cap.size.y*.22f;
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
            for(int i=0;i<12;i++)
            {
                var authored=Resources.Load<SkyPulsePipeTheme>("SkyPulse/PipeEdits/world-"+i.ToString("00"));authoredPipeThemes[i]=authored;
                routeBodyThemes[i]=authored!=null&&authored.Barrel!=null?Instantiate(authored.Barrel):SkyPulseRouteArchitecture.Build(i,false);
                routeCapThemes[i]=authored!=null&&authored.Collar!=null?Instantiate(authored.Collar):SkyPulseRouteArchitecture.Build(i,true);
            }
            routeBodyMesh=routeBodyThemes[0];routeCapMesh=routeCapThemes[0];
            var properties=new MaterialPropertyBlock();
            foreach(var pair in pipePool)foreach(var s in new[]{pair.Top,pair.Bottom})
            {
                var body=SkyPulseMeshBuilder.Surface(pair.Root.transform,"3D extruded gate column",routeBodyMesh,routeMetal,5);var cap=SkyPulseMeshBuilder.Surface(pair.Root.transform,"3D bevelled gate collar",routeCapMesh,routeCapMetal,11);
                properties.SetFloat("_ArtFlip",s==pair.Top?1:0);body.SetPropertyBlock(properties);cap.SetPropertyBlock(properties);
                routeDepth.Add(new RouteDepth{Surface=s,Body=body.transform,Cap=cap.transform,BodyRenderer=body,CapRenderer=cap,BodyFilter=body.GetComponent<MeshFilter>(),CapFilter=cap.GetComponent<MeshFilter>(),Retired=new[]{s.Artwork,s.Outer,s.Panel,s.Shade,s.RailLeft,s.RailRight,s.Core,s.CorePulse,s.Highlight,s.Energy,s.Scan,s.Beacon,s.CapGlow,s.CapOuter,s.CapAccent,s.CapPanel,s.CapEnergy}});
            }
        }
        Material OriginalRouteMaterial(Sprite artwork,string label,bool cutout=false)
        {
            var material=new Material(Resources.Load<Shader>("SkyPulse/Shaders/EndlessRouteMetal")){name=label};
            material.SetFloat("_Cutout",cutout?1:0);
            var rect=artwork.rect;var texture=artwork.texture;
            material.mainTexture=texture;material.SetVector("_ArtRect",new Vector4(rect.x/texture.width,rect.y/texture.height,rect.width/texture.width,rect.height/texture.height));return material;
        }
        Mesh RouteCylinder(bool cap)=>SkyPulseRouteArchitecture.Build(0,cap);
        void OnDestroy()
        {
            if(scoreGlassMaterial!=null)Destroy(scoreGlassMaterial);
            if(homeGlassMaterial!=null)Destroy(homeGlassMaterial);
            if(scoreTypographyMaterial!=null)Destroy(scoreTypographyMaterial);
            if(techBackdropMaterial!=null)Destroy(techBackdropMaterial);
            if(techTypographyMaterial!=null)Destroy(techTypographyMaterial);
            if(progressionTypographyMaterial!=null)Destroy(progressionTypographyMaterial);
            foreach(var mesh in routeBodyThemes)if(mesh!=null)Destroy(mesh);foreach(var mesh in routeCapThemes)if(mesh!=null)Destroy(mesh);if(routeMetal!=null)Destroy(routeMetal);
            if(routeCapMetal!=null)Destroy(routeCapMetal);
        }
    }
}
