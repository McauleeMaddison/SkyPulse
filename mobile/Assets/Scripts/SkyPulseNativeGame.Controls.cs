using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        readonly SkyPulseInputFence gameplayInput=new SkyPulseInputFence();
        readonly List<RaycastResult> gameplayUiHits=new List<RaycastResult>(32);
        PointerEventData gameplayPointer;
        EventSystem gameplayEventSystem;
        int blackoutGeneration;
        // Authored at 1080 x 2040: 176 units remains about 49 pt on a
        // 320 x 568 phone. Expand the hit area, not the visible HUD artwork.
        // Use only on isolated controls; densely packed controls need layout space.
        private static void SetComfortHitTarget(Button button)
        {
            var rect = (RectTransform)button.transform;
            var hit = button.GetComponent<Image>();
            var x = Mathf.Max(0f, (176f - rect.rect.width) * .5f);
            var y = Mathf.Max(0f, (176f - rect.rect.height) * .5f);
            hit.raycastPadding = new Vector4(-x, -y, -x, -y);
        }
        void ReleaseGameplayPointer(int pointer){gameplayInput.Release(pointer);}
        void FenceGameplayBoundary(){gameplayInput.Boundary(Time.frameCount);}
        void UpdateGameplayPointerReleases()
        {
            if(Input.touchCount>0)
            {
                gameplayInput.ObserveTouch(Time.unscaledTime);
                for(int i=0;i<Input.touchCount;i++)
                {
                    var touch=Input.GetTouch(i);
                    if(touch.phase==TouchPhase.Ended || touch.phase==TouchPhase.Canceled)gameplayInput.Release(touch.fingerId);
                }
            }
            else if(!Input.GetMouseButton(0))gameplayInput.ReleaseAll();
            if(Input.GetMouseButtonUp(0))gameplayInput.Release(-1);
        }
        bool InteractiveUiAt(Vector2 screenPosition)
        {
            if(EventSystem.current==null)return false;
            if(gameplayEventSystem!=EventSystem.current)
            {gameplayEventSystem=EventSystem.current;gameplayPointer=new PointerEventData(gameplayEventSystem);}
            gameplayPointer.position=screenPosition;gameplayUiHits.Clear();gameplayEventSystem.RaycastAll(gameplayPointer,gameplayUiHits);
            for(int i=0;i<gameplayUiHits.Count;i++)
            {
                var hit=gameplayUiHits[i].gameObject;
                if(hit.GetComponentInParent<Selectable>()!=null || hit.GetComponentInParent<ScrollRect>()!=null)return true;
            }
            return false;
        }
        bool SubmitRelayPress(int pointer,int frame,float now,bool touch,Vector2 position)
        {
            if(activeMode!=SkyPulseMode.PulseRelay || state!=FlightState.Playing || AuxiliaryScreenOpen ||
                privacyScreen.activeSelf || flightGuideScreen.activeSelf || InteractiveUiAt(position) || !flightCamera.pixelRect.Contains(position))return false;
            if(!gameplayInput.Press(pointer,frame,now,touch))return false;
            // Reuse the original impulse and wing controller immediately. The
            // Relay poll never also feeds buffered touch/mouse or UI buttons.
            Flap();return true;
        }
        void PollRelayGameplayPress()
        {
            if(Input.touchCount>0)
            {
                for(int i=0;i<Input.touchCount;i++)
                {
                    var touch=Input.GetTouch(i);
                    if(touch.phase==TouchPhase.Began && SubmitRelayPress(touch.fingerId,Time.frameCount,Time.unscaledTime,true,touch.position))break;
                }
                return;
            }
            if(Input.GetMouseButtonDown(0))SubmitRelayPress(-1,Time.frameCount,Time.unscaledTime,false,Input.mousePosition);
            if(!AuxiliaryScreenOpen && !privacyScreen.activeSelf && !flightGuideScreen.activeSelf && (Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.UpArrow)))
            {if(state==FlightState.Playing && gameplayInput.Press(-99,Time.frameCount,Time.unscaledTime,false))Flap();gameplayInput.Release(-99);}
        }
        void HandleBlackoutPress(int tile,int generation,int pointer)
        {
            if(generation!=blackoutGeneration || activeMode!=SkyPulseMode.BlackoutProtocol || state!=FlightState.Playing)return;
            if(!gameplayInput.Press(pointer,Time.frameCount,Time.unscaledTime,pointer>=0))return;
            if(blackoutPlanning)PreviewBlackoutNode(tile);else TouchBlackoutNode(tile);
        }
        Button modeHomeMap,modeHomeGuide;
        RectTransform homeHangar;
        GameObject homeTech,homeWorldGuide,modeGuideScreen;
        Text homeActivityCopy;
        RectTransform homePrivacy,homeTasks;
        Text homeArcade;
        void SetupModeHomeControls()
        {
            homeHangar=homeScreen.transform.Find("Button · BIRD HANGAR") as RectTransform;
            homeTech=homeScreen.transform.Find("Button · TECH TREE").gameObject;
            homeWorldGuide=homeScreen.transform.Find("Button · WORLDS & POWER-UPS").gameObject;
            homePrivacy=homeScreen.transform.Find("Button · PRIVACY") as RectTransform;
            homeTasks=homeScreen.transform.Find("Button · MISSION CONTROL") as RectTransform;
            foreach(var label in homeScreen.GetComponentsInChildren<Text>())if(label.text=="A  R  C  A  D  E")homeArcade=label;
            foreach(var label in homeScreen.GetComponentsInChildren<Text>())if(label.text=="FLIGHT · CIRCUITS · PUZZLES")homeActivityCopy=label;
            modeHomeMap=MissionButton(homeScreen.transform,"CIRCUIT MAP",0,-355,720,()=>OpenMissionSelection(homeProtocol),Hex("#45eaff"),112);
            modeHomeGuide=MissionButton(homeScreen.transform,"HOW TO PLAY",0,-694,660,OpenModeGuide,Hex("#8fa7c4"),96);
            modeGuideScreen=MissionScreen("Mode field guide");
            modeHomeMap.gameObject.SetActive(false);modeHomeGuide.gameObject.SetActive(false);
            SetupHomeActionProgress();
        }
        int HomeNextMission()
        {
            if(homeProtocol==SkyPulseMode.BlackoutProtocol)
            {
                int active=blackoutCampaign.Data.active;if(blackoutCampaign.CanPlay(active))return active;
                for(int i=0;i<8;i++)if(blackoutCampaign.Data.medals[i]==0)return i;return 0;
            }
            for(int i=0;i<RelayMission.All.Length;i++)if(dailyRelay.Data.medals[i]==0)return i;
            return 0;
        }
        void ApplyModeHomeLayout()
        {
            if(modeHomeMap==null)return;
            bool endless=homeProtocol==SkyPulseMode.EndlessRoute,relay=homeProtocol==SkyPulseMode.PulseRelay;
            homeHangar.gameObject.SetActive(endless||relay);homeTech.SetActive(endless);homeWorldGuide.SetActive(false);
            menuCrystalText.transform.parent.gameObject.SetActive(endless||relay);
            modeHomeMap.gameObject.SetActive(!endless);modeHomeGuide.gameObject.SetActive(true);
            // Instructions live in the guide. Home keeps identity, progress and
            // actions, with one objective only where the campaign needs it.
            homeActivityCopy.gameObject.SetActive(false);
            if(homeProtocolCaption!=null)homeProtocolCaption.gameObject.SetActive(false);
            menuRouteText.gameObject.SetActive(!endless&&!relay);menuModeDetailText.gameObject.SetActive(false);
            foreach(var glyph in homeLaunchButton.GetComponentsInChildren<SkyPulseUiGlyph>(true))glyph.gameObject.SetActive(endless||relay);
            foreach(var button in new[]{modeHomeMap,modeHomeGuide}){var surface=button.targetGraphic as SkyPulseCircuitGraphic;surface.Variant=(int)homeProtocol;surface.color=ProtocolAccent(homeProtocol);surface.SetVerticesDirty();}
            ApplyRelayHomeRefinement(relay);
            if(endless)return;
            int next=HomeNextMission(),completed=0,total=relay?4:8;
            foreach(int medal in relay?dailyRelay.Data.medals:blackoutCampaign.Data.medals)if(medal>0)completed++;
            homeBestCaption.text=relay?"TODAY CLEARED":"SYSTEMS RESTORED";menuBestText.text=completed+" / "+total;
            homeLaunchButton.GetComponentInChildren<Text>().text=relay?"FLY CIRCUIT "+(next+1).ToString("00"):"ENTER VAULT "+(next+1).ToString("00");
            modeHomeMap.GetComponentInChildren<Text>().text=relay?"CIRCUIT MAP":"VAULT MAP";
            homeActivityCopy.text=relay?"TAP TO FLY · CRYSTALS OPEN GATES":"ROTATE PRISMS · ROUTE THE LIGHT";
            menuModeDetailText.rectTransform.anchoredPosition=relay?new Vector2(0,-640):new Vector2(0,-478);
            menuRouteText.rectTransform.anchoredPosition=relay?new Vector2(0,-745):new Vector2(0,-540);
            if(relay){menuModeDetailText.text="SPECTRAL CRYSTALS · AUTOMATIC GATES";menuRouteText.text="NEXT · "+dailyRelay.Missions[next].Name;}
            else{menuEquippedText.text="SIGNAL HEIST";menuRouteText.text=completed==8?"NETWORK RESTORED · REPLAY ANY VAULT":SkyPulseBlackoutCampaign.Objective(next).ToUpperInvariant();}
        }
        // One launch action, one secondary row, quiet footer. Changes are restored
        // explicitly when switching modes; collection cards own their own layout.
        void ApplyRelayHomeRefinement(bool relay)
        {
            var launch=(RectTransform)homeLaunchButton.transform;
            launch.anchoredPosition=new Vector2(0,relay?-220:-165);launch.sizeDelta=new Vector2(620,relay?136:104);
            homeLaunchButton.GetComponentInChildren<Text>().fontSize=relay?32:40;
            menuCrystalText.transform.parent.gameObject.SetActive(homeProtocol==SkyPulseMode.EndlessRoute);
            SetHomeRect(modeHomeMap,relay?new Vector2(-250,-440):new Vector2(0,-355),relay?new Vector2(228,144):new Vector2(720,112));
            SetHomeRect(modeHomeGuide,relay?new Vector2(250,-440):new Vector2(0,-694),relay?new Vector2(228,144):new Vector2(660,96));
            if(homePrivacy!=null){homePrivacy.anchoredPosition=relay?new Vector2(180,-730):new Vector2(0,-822);homePrivacy.sizeDelta=relay?new Vector2(280,116):new Vector2(280,96);}
            if(homeTasks!=null){homeTasks.anchoredPosition=relay?new Vector2(-180,-730):new Vector2(0,-950);homeTasks.sizeDelta=relay?new Vector2(320,116):new Vector2(720,100);}
            menuTitleText.fontSize=relay?72:100;menuTitleText.rectTransform.anchoredPosition=new Vector2(0,relay?800:835);
            if(homeArcade!=null){homeArcade.fontSize=relay?24:37;homeArcade.rectTransform.anchoredPosition=new Vector2(0,relay?734:753);}
            menuEquippedText.fontSize=relay?23:30;menuEquippedText.gameObject.SetActive(!relay);
            var best=(RectTransform)homeBestCaption.transform.parent;best.anchoredPosition=new Vector2(0,relay?-68:-35);
            foreach(string name in new[]{"Brand wing port","Brand wing starboard","Title energy rule","Flight deck"}){var decor=homeScreen.transform.Find(name);if(decor!=null)decor.gameObject.SetActive(!relay);}
            // The 3D hoop supplies depth; remove the competing 2D radar rings.
            if(homeProtocolGraphic!=null)homeProtocolGraphic.gameObject.SetActive(!relay);
            foreach(var face in homeScreen.GetComponentsInChildren<SkyPulseCircuitGraphic>(true))
                if(face.Surface==SkyPulseSurface.Panel){face.Hologram=relay;face.SetVerticesDirty();}
            foreach(var button in homeScreen.GetComponentsInChildren<Button>(true))
            {
                var face=button.targetGraphic as SkyPulseCircuitGraphic;
                if(face!=null){face.rectTransform.sizeDelta=((RectTransform)button.transform).sizeDelta;face.SetVerticesDirty();}
            }
            ApplyHomeActionProgress();
        }
        static void SetHomeRect(Button button,Vector2 at,Vector2 size){var rect=(RectTransform)button.transform;rect.anchoredPosition=at;rect.sizeDelta=size;}
    }
}
