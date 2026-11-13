using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        // The shipping presenter uses generated combined meshes, never Editor
        // scene objects. All physics, equipment, costs and crash poses stay native.
#if UNITY_EDITOR
        public static bool ReviewOriginalBirdArtwork
        {
            get=>UnityEditor.SessionState.GetBool("SkyPulse.ReviewOriginalBirdArtwork",false);
            set=>UnityEditor.SessionState.SetBool("SkyPulse.ReviewOriginalBirdArtwork",value);
        }
#endif
        bool solidAvailabilityChecked,solidAvailable;
        GameObject solidPilot;
        SkyPulseNeonFinchPrototype solidRig;
        Light solidKey,solidFill;
        string solidPilotId;
        float solidWingStroke,solidFlightTime,solidShowcaseTime,solidStudioTick;
        bool solidWasFlying;
        readonly Dictionary<RectTransform,SolidMenuView> solidMenuViews=new Dictionary<RectTransform,SolidMenuView>();
        readonly HashSet<RectTransform> solidWanted=new HashSet<RectTransform>();
        readonly List<RectTransform> solidExpired=new List<RectTransform>();
        readonly Dictionary<Behaviour,bool> solidHidden=new Dictionary<Behaviour,bool>();
        sealed class SolidMenuView{public string Id;public SkyPulseSolidBirdStudio View;}
        public bool RuntimeSolidBirdsActive
        {
            get
            {
#if UNITY_EDITOR
                if(ReviewOriginalBirdArtwork)return false;
#endif
                if(!solidAvailabilityChecked){solidAvailabilityChecked=true;solidAvailable=Resources.Load<TextAsset>("SkyPulse/Birds3D/BakeManifest")!=null;}
                return solidAvailable;
            }
        }
        string SolidBirdId(Skin skin)
        {
            if(skin==null)return "neon_finch";
            // Offspring inherit the actual parent's appearance while retaining
            // their own catalogue ID, genome, equipment and progression record.
            foreach(var founder in Skins)if(founder.Id==skin.Id||founder.ArtPath==skin.ArtPath)return founder.Id;
            return "neon_finch";
        }
        float SolidTargetStroke()
        {
            if(!UsesFlapFrameSequence()||gameplayWingState==GameplayWingState.Settled)return 0;
            float lift=1-Mathf.Clamp01(gameplayWingFrameIndex/(float)(flapFrameBirdSprites.Length-1));
            return SkyPulseNeonFinchPrototype.TapStroke(lift,gameplayWingState==GameplayWingState.Downstroke);
        }
        void ResetSolidFlightPose()
        {solidWingStroke=solidFlightTime=0;solidWasFlying=false;if(solidRig!=null)solidRig.ResetPose();}
        void PresentSolidTap()
        {
            if(solidRig==null||!RuntimeSolidBirdsActive)return;
            // First input has a visible opening movement in the accepted frame.
            // Mid-stroke taps reverse smoothly from the existing physical pose.
            if(Mathf.Abs(solidWingStroke)<.001f)solidWingStroke=.20f;
            solidRig.SampleFlightStroke(solidFlightTime,solidWingStroke);
        }
        void UpdateSolidBirds(float dt)
        {
            if(!RuntimeSolidBirdsActive){DisposeSolidBirds();return;}
            dt=Mathf.Clamp(dt,0,.1f);
            bool flight=activeMode!=SkyPulseMode.BlackoutProtocol&&(state==FlightState.Playing||state==FlightState.Paused);
            if(flight)
            {
                EnsureSolidPilot();
                if(solidPilot!=null)
                {
                    solidPilot.SetActive(true);solidRig.reducedMotion=reduceMotionEnabled;
                    if(state==FlightState.Playing)
                    {solidFlightTime+=dt;solidWingStroke=Mathf.MoveTowards(solidWingStroke,SolidTargetStroke(),dt*20f);}
                    solidRig.SampleFlightStroke(solidFlightTime,solidWingStroke);solidWasFlying=true;
                    solidKey.transform.rotation=Quaternion.Euler(35,35,0);solidFill.transform.rotation=Quaternion.Euler(335,215,0);
                    solidKey.color=Color.Lerp(new Color(1,.985f,.96f),SceneLightTint(),.18f);
                    birdRenderer.enabled=birdFlapRenderer.enabled=birdRiseRenderer.enabled=false;
                    birdParallaxRenderer.enabled=birdSafetyRenderer.enabled=birdDepthRenderer.enabled=birdEyeGlintRenderer.enabled=false;
                }
            }
            else if(solidPilot!=null){solidPilot.SetActive(false);if(solidWasFlying){ResetSolidFlightPose();solidWasFlying=false;}}
            bool home=state==FlightState.Menu&&homeScreen.activeInHierarchy&&homeProtocol!=SkyPulseMode.BlackoutProtocol;
            bool hangar=state==FlightState.Customize&&customizeScreen.activeInHierarchy&&cosmeticCategory==CosmeticCategory.Birds;
            solidWanted.Clear();
            if(home&&menuHeroTransform!=null)solidWanted.Add(menuHeroTransform);
            if(hangar){solidWanted.Add(hangarPreviousBirdRoot);solidWanted.Add(hangarCurrentBirdRoot);solidWanted.Add(hangarNextBirdRoot);}
            // Retired pages release their layer/target before the next three
            // bays are created, so a swipe cannot exhaust or duplicate the pool.
            solidExpired.Clear();foreach(var pair in solidMenuViews)if(pair.Key==null||!solidWanted.Contains(pair.Key))solidExpired.Add(pair.Key);
            foreach(var key in solidExpired){solidMenuViews[key].View.Dispose();solidMenuViews.Remove(key);}
            if(!home&&!hangar){RestoreSolidUi();return;}
            bool paused=state==FlightState.Paused||(birdProgressionScreen!=null&&birdProgressionScreen.activeInHierarchy);
            if(!paused&&!reduceMotionEnabled)solidShowcaseTime+=dt;
            solidStudioTick+=dt;bool render=solidStudioTick>=1f/30;if(render)solidStudioTick=0;
            if(home)
            {
                ShowSolidMenu(menuHeroTransform,equippedSkin,new Vector2(850,440),Vector2.zero,false,render);
                HideSolidUi(menuBirdImage);HideSolidUi(menuBirdSafetyImage);HideSolidUi(menuBirdFlapImage);HideSolidUi(menuBirdRiseImage);HideSolidUi(menuBirdShadowImage);HideSolidUi(menuBirdEyeGlintImage);
            }
            if(hangar)
            {
                var roster=BirdRoster;int n=roster.Length,index=Mathf.Clamp(hangarPageIndex,0,n-1);
                ShowSolidMenu(hangarPreviousBirdRoot,roster[(index+n-1)%n],new Vector2(230,180),new Vector2(0,8),false,render);
                ShowSolidMenu(hangarCurrentBirdRoot,roster[index],new Vector2(560,360),new Vector2(0,8),true,render);
                ShowSolidMenu(hangarNextBirdRoot,roster[(index+1)%n],new Vector2(230,180),new Vector2(0,8),false,render);
                HideSolidUi(hangarPreviousBirdRoot.Find("Neighbour bird")?.GetComponent<Image>());
                HideSolidUi(hangarCurrentBirdRoot.Find("Selected bird")?.GetComponent<Image>());
                HideSolidUi(hangarNextBirdRoot.Find("Neighbour bird")?.GetComponent<Image>());
                if(hangarPreviewStage!=null){HideSolidUi(hangarPreviewStage);HideSolidUi(hangarPreviewStage.GetComponent<RawImage>());}
            }
        }
        void EnsureSolidPilot()
        {
            string id=SolidBirdId(equippedSkin);if(solidPilot!=null&&solidPilotId==id)return;
            if(solidPilot!=null){solidPilot.SetActive(false);Destroy(solidPilot);}
            var prefab=Resources.Load<GameObject>("SkyPulse/Birds3D/"+id);if(prefab==null){Debug.LogError("SkyPulse solid bird is missing: "+id);return;}
            solidPilot=new GameObject("Live solid pilot");solidPilot.transform.SetParent(bird,false);
            var model=Instantiate(prefab,solidPilot.transform);solidRig=model.GetComponent<SkyPulseNeonFinchPrototype>();solidRig.animate=solidRig.turntable=false;solidRig.ResetPose();
            var renderers=model.GetComponentsInChildren<Renderer>();Bounds bounds=default;bool first=true;
            foreach(var renderer in renderers)
            {
                var local=renderer.localBounds;var matrix=solidPilot.transform.worldToLocalMatrix*renderer.localToWorldMatrix;
                for(int i=0;i<8;i++){var point=matrix.MultiplyPoint3x4(local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
            }
            solidPilot.transform.localScale=Vector3.one*(2.30f/Mathf.Max(.01f,bounds.size.x));solidPilot.transform.localRotation=Quaternion.Euler(0,-9,0);
            foreach(var t in solidPilot.GetComponentsInChildren<Transform>(true))t.gameObject.layer=26;
            solidKey=SolidFlightLight("Pilot key light",1.05f);solidFill=SolidFlightLight("Pilot fill light",.42f);solidFill.color=new Color(.88f,.94f,1);
            solidPilotId=id;ResetSolidFlightPose();
        }
        Light SolidFlightLight(string label,float intensity)
        {var o=new GameObject(label,typeof(Light));o.transform.SetParent(solidPilot.transform,false);var l=o.GetComponent<Light>();l.type=LightType.Directional;l.cullingMask=1<<26;l.intensity=intensity;l.shadows=LightShadows.None;return l;}
        void ShowSolidMenu(RectTransform root,Skin skin,Vector2 size,Vector2 at,bool dock,bool render)
        {
            if(root==null||skin==null)return;string id=SolidBirdId(skin);
            if(solidMenuViews.TryGetValue(root,out var entry)&&entry.Id!=id){entry.View.Dispose();solidMenuViews.Remove(root);entry=null;}
            if(entry==null){entry=new SolidMenuView{Id=id,View=new SkyPulseSolidBirdStudio(root,"SkyPulse/Birds3D/"+id,size,at,skin.Accent,dock,state==FlightState.Customize)};solidMenuViews.Add(root,entry);render=true;}
            if(playerJourney!=null)entry.View.SetMastery(playerJourney.DisplayRank(skin.Id));
            if(render)entry.View.Render(solidShowcaseTime,reduceMotionEnabled);
        }
        void HideSolidUi(Behaviour component)
        {if(component==null)return;if(!solidHidden.ContainsKey(component))solidHidden.Add(component,component.enabled);component.enabled=false;}
        void RestoreSolidUi(){foreach(var pair in solidHidden)if(pair.Key!=null)pair.Key.enabled=pair.Value;solidHidden.Clear();}
        void DisposeSolidBirds()
        {
            foreach(var entry in solidMenuViews.Values)entry.View.Dispose();solidMenuViews.Clear();RestoreSolidUi();
            if(solidPilot!=null){solidPilot.SetActive(false);Destroy(solidPilot);}solidPilot=null;solidRig=null;solidPilotId=null;
        }
    }
}
