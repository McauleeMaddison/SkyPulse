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
        void SetupModeHomeControls()
        {
            homeHangar=homeScreen.transform.Find("Button · BIRD HANGAR") as RectTransform;
            homeTech=homeScreen.transform.Find("Button · TECH TREE").gameObject;
            homeWorldGuide=homeScreen.transform.Find("Button · WORLDS & POWER-UPS").gameObject;
            foreach(var label in homeScreen.GetComponentsInChildren<Text>())if(label.text=="FLIGHT · CIRCUITS · PUZZLES")homeActivityCopy=label;
            modeHomeMap=MissionButton(homeScreen.transform,"CIRCUIT MAP",0,-355,720,()=>OpenMissionSelection(homeProtocol),Hex("#45eaff"),112);
            modeHomeGuide=MissionButton(homeScreen.transform,"HOW TO PLAY",0,-694,660,OpenModeGuide,Hex("#8fa7c4"),96);
            modeGuideScreen=MissionScreen("Mode field guide");
            modeHomeMap.gameObject.SetActive(false);modeHomeGuide.gameObject.SetActive(false);
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
            homeHangar.gameObject.SetActive(endless||relay);homeTech.SetActive(endless);homeWorldGuide.SetActive(endless);
            menuHangarProgressText.gameObject.SetActive(endless||relay);menuTechProgressText.gameObject.SetActive(endless);
            homeHangar.anchoredPosition=endless?new Vector2(-187,-355):new Vector2(0,-507);
            menuHangarProgressText.rectTransform.anchoredPosition=endless?new Vector2(-187,-436):new Vector2(0,-589);
            menuCrystalText.transform.parent.gameObject.SetActive(endless||relay);
            modeHomeMap.gameObject.SetActive(!endless);modeHomeGuide.gameObject.SetActive(!endless);
            menuRouteText.gameObject.SetActive(!relay);menuModeDetailText.gameObject.SetActive(!relay);
            foreach(var glyph in homeLaunchButton.GetComponentsInChildren<SkyPulseUiGlyph>(true))glyph.gameObject.SetActive(endless||relay);
            foreach(var button in new[]{modeHomeMap,modeHomeGuide}){var surface=button.targetGraphic as SkyPulseCircuitGraphic;surface.Variant=(int)homeProtocol;surface.color=ProtocolAccent(homeProtocol);surface.SetVerticesDirty();}
            if(endless){menuModeDetailText.rectTransform.anchoredPosition=new Vector2(0,-491);menuRouteText.rectTransform.anchoredPosition=new Vector2(0,-540);homeActivityCopy.text="FLIGHT · CIRCUITS · PUZZLES";return;}
            int next=HomeNextMission(),completed=0,total=relay?4:8;
            foreach(int medal in relay?dailyRelay.Data.medals:blackoutCampaign.Data.medals)if(medal>0)completed++;
            homeBestCaption.text=relay?"TODAY CLEARED":"SYSTEMS RESTORED";menuBestText.text=completed+" / "+total;
            homeLaunchButton.GetComponentInChildren<Text>().text=relay?"FLY CIRCUIT "+(next+1).ToString("00"):"ENTER VAULT "+(next+1).ToString("00");
            modeHomeMap.GetComponentInChildren<Text>().text=relay?"CIRCUIT MAP":"VAULT MAP";
            homeActivityCopy.text=relay?"TAP TO FLY · CRYSTALS OPEN GATES":"ROTATE PRISMS · ROUTE THE LIGHT";
            menuModeDetailText.rectTransform.anchoredPosition=relay?new Vector2(0,-640):new Vector2(0,-478);
            menuRouteText.rectTransform.anchoredPosition=relay?new Vector2(0,-745):new Vector2(0,-540);
            if(relay){menuModeDetailText.text="SPECTRAL CRYSTALS · AUTOMATIC GATES";menuRouteText.text="NEXT · "+dailyRelay.Missions[next].Name;}
            else{menuModeDetailText.text="BRING THE CITY BACK ONLINE";menuRouteText.text=completed==8?"NETWORK RESTORED · REPLAY ANY VAULT":SkyPulseBlackoutCampaign.Objective(next).ToUpperInvariant();}
        }
        void OpenModeGuide()
        {
            ClearScreen(modeGuideScreen);modeGuideScreen.SetActive(true);modeGuideScreen.transform.SetAsLastSibling();
            bool relay=homeProtocol==SkyPulseMode.PulseRelay;var accent=ProtocolAccent(homeProtocol);
            MissionText(modeGuideScreen.transform,relay?"PULSE RELAY":"BLACKOUT PROTOCOL",835,46,accent);
            var card=PremiumPanel(modeGuideScreen.transform,"Thumb controls",new Vector2(0,105),new Vector2(930,1190),relay?1:2,accent);
            MissionText(card,relay?"TAP THE FLIGHT AREA":"ROTATE THE LIGHT",460,32,accent,870,100);
            MissionText(card,relay?"Each fresh tap flaps your bird. Pause and menus never flap.":"Tap a silver prism to turn its mirror. The live beam changes immediately. Power every CORE and the UPLINK together.",300,29,null,820,150);
            MissionText(card,relay?"Collect both randomly placed crystals for each numbered gate. It opens automatically at 2/2; missing crystals leave it locked.":"Crystal splitters send light forward and left. Turn the amber input arrow to face the incoming beam.",70,29,null,820,200);
            MissionText(card,relay?"Follow the crystal trail through each aperture, then fly through the green exit.":"Power RELAY A or B to open its matching shutter. Relays must stay powered; disconnected shutters close again.",-180,29,null,820,190);
            MissionText(card,relay?"Four new circuits each UTC day. Today’s medals are saved until refresh. Your equipped bird is cosmetic; Tech Tree does not alter Relay.":"Each rotation spends 1 trace; a lit red sensor raises the cost to 3. Run out before extraction and restart. Cleared vaults advance immediately.",-415,27,accent,820,180);
            MissionButton(modeGuideScreen.transform,"BACK",0,-785,850,()=>modeGuideScreen.SetActive(false),accent,112);
        }
    }
}
