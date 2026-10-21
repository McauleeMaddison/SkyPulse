using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        BlackoutCampaignProgress blackoutCampaign;
        readonly List<SkyPulseBlackoutGraphic> blackoutGraphics=new List<SkyPulseBlackoutGraphic>();
        readonly List<Button> blackoutNodeButtons=new List<Button>();
        readonly List<Text> blackoutNodeLabels=new List<Text>();
        readonly List<SkyPulsePressTarget> blackoutPressTargets=new List<SkyPulsePressTarget>();
        readonly Vector2[] blackoutPositions=new Vector2[49];
        SkyPulseBlackoutGraphic blackoutTraceGauge,blackoutHomeBackdrop;
        SkyPulseHeistBoard blackoutBoard;
        Text blackoutVaultText,blackoutBriefing,blackoutCompletionToast;
        bool blackoutLabBuilt;
        float blackoutToastRemaining;
        static Color BlackoutMint=>Hex("#66ffc3");
        static Color BlackoutIce=>Hex("#c4eaff");

        SkyPulseBlackoutGraphic BlackoutGraphic(Transform parent,string name,Vector2 at,Vector2 size,BlackoutSurface surface,bool animate=true)
        {
            var o=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(SkyPulseBlackoutGraphic));o.transform.SetParent(parent,false);
            var g=o.GetComponent<SkyPulseBlackoutGraphic>();g.rectTransform.anchoredPosition=at;g.rectTransform.sizeDelta=size;g.Surface=surface;g.Animate=animate;g.ReducedMotion=reduceMotionEnabled;g.raycastTarget=false;blackoutGraphics.Add(g);return g;
        }
        void BlackoutBackdrop(Transform parent)
        {
            Transform backing=parent;
            foreach(Transform child in parent)if(child is RectTransform rect&&interfaceBackdrops.Contains(rect)){backing=child;break;}
            var cached=backing.Find("Obsidian signal chamber");if(cached!=null){cached.gameObject.SetActive(true);return;}
            var g=BlackoutGraphic(backing,"Obsidian signal chamber",Vector2.zero,Vector2.zero,BlackoutSurface.Backdrop);var r=g.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        }
        void PrepareBlackoutScreenClear(GameObject root)
        {
            foreach(var g in root.GetComponentsInChildren<SkyPulseBlackoutGraphic>(true))if(g.Surface==BlackoutSurface.Backdrop)g.gameObject.SetActive(false);
        }
        RectTransform BlackoutPanel(Transform parent,string name,Vector2 at,Vector2 size)
        {
            var panel=CreatePanel(parent,name,at,size,Color.clear);panel.GetComponent<Image>().raycastTarget=false;BlackoutGraphic(panel,"Obsidian instrument",Vector2.zero,size,BlackoutSurface.Deck,false);return panel;
        }
        Button BlackoutButton(Transform parent,string label,float x,float y,float width,Action action,float height=110)
        {
            var b=MissionButton(parent,label,x,y,width,action,BlackoutMint,height);if(b.targetGraphic!=null)b.targetGraphic.gameObject.SetActive(false);
            var g=BlackoutGraphic(b.transform,"Obsidian action",Vector2.zero,new Vector2(width,height),BlackoutSurface.Deck,false);g.transform.SetAsFirstSibling();b.targetGraphic=g;return b;
        }
        Text BlackoutReadout(Transform parent,string caption,float x,Color hue)
        {
            var label=CreateText(parent,caption,new Vector2(x,29),new Vector2(310,32),25,hue,TextAnchor.MiddleCenter,FontStyle.Normal);label.raycastTarget=false;
            var value=CreateText(parent,"",new Vector2(x,-20),new Vector2(310,48),36,BlackoutIce,TextAnchor.MiddleCenter,FontStyle.Bold);value.raycastTarget=false;return value;
        }
        void ApplyBlackoutHomeView()
        {
            if(blackoutHomeBackdrop==null)
            {
                blackoutHomeBackdrop=BlackoutGraphic(homeProtocolBackdrop.transform,"Blackout Home chamber",Vector2.zero,Vector2.zero,BlackoutSurface.Backdrop);var r=blackoutHomeBackdrop.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            }
            bool on=homeProtocol==SkyPulseMode.BlackoutProtocol;blackoutHomeBackdrop.gameObject.SetActive(on);
            if(on){int n=0;foreach(int medal in blackoutCampaign.Data.medals)if(medal>0)n++;menuEquippedText.text="ROTATE PRISMS · ROUTE LIVE ENERGY";homeProtocolCaption.text="BLACKOUT PROTOCOL · SIGNAL HEIST";homeBestCaption.text="VAULTS EXTRACTED";menuBestText.text=n+" / 8";}
        }
        void UpdateBlackoutPresentation()
        {
            FitBlackoutToSafeArea();
            if(blackoutBoard!=null){blackoutBoard.ReducedMotion=reduceMotionEnabled;blackoutBoard.MotionPaused=state==FlightState.Paused;}
            for(int i=blackoutGraphics.Count-1;i>=0;i--){var g=blackoutGraphics[i];if(g==null){blackoutGraphics.RemoveAt(i);continue;}g.ReducedMotion=reduceMotionEnabled;g.MotionPaused=state==FlightState.Paused;}
        }
        void FitBlackoutToSafeArea()
        {
            if(blackoutScreen==null||safeAreaRoot==null)return;
            var r=(RectTransform)blackoutScreen.transform;
            // Compact portrait layout fits seven 150-unit targets at the safe
            // width even on 320-point phones; the board is not shrunk with menus.
            r.localScale=Vector3.one*Mathf.Min(safeAreaRoot.rect.width/1080,safeAreaRoot.rect.height/1680);
            foreach(Transform child in r)if(child is RectTransform backing&&interfaceBackdrops.Contains(backing)){backing.sizeDelta=((RectTransform)uiRoot.transform).rect.size/r.localScale.x;backing.position=uiRoot.transform.position;}
        }
        void ResetBlackoutEffects(){blackoutGeneration++;FenceGameplayBoundary();blackoutToastRemaining=0;if(blackoutBoard!=null){blackoutBoard.Clear();blackoutBoard.Reveal=0;}}
        void OpenBlackoutVaults()
        {
            SetHomeProtocol(SkyPulseMode.BlackoutProtocol);ResetToMenu();homeScreen.SetActive(false);activeMode=SkyPulseMode.BlackoutProtocol;
            ClearScreen(missionSelectionScreen);missionSelectionScreen.SetActive(true);BlackoutBackdrop(missionSelectionScreen.transform);
            MissionText(missionSelectionScreen.transform,"SIGNAL HEIST",885,52,BlackoutIce);MissionText(missionSelectionScreen.transform,"EIGHT VAULTS · ROTATE THE LIGHT",805,24,BlackoutMint);
            for(int i=0;i<8;i++)
            {
                int index=i;var p=SkyPulseBlackoutCampaign.Stages[i];bool unlocked=blackoutCampaign.CanPlay(i);int medal=blackoutCampaign.Data.medals[i];
                var card=BlackoutPanel(missionSelectionScreen.transform,"Vault "+i,new Vector2(i%2==0?-240:240,555-i/2*332),new Vector2(458,300));
                MissionText(card,(i+1).ToString("00")+" · "+p.Name,108,25,unlocked?BlackoutIce:Hex("#597081"),420,54);
                BlackoutGraphic(card,"Optical layout",new Vector2(-110,-3),new Vector2(140,135),BlackoutSurface.MiniMap,false).Miniature=p;
                var a=MissionText(card,p.CoreCount+" CORES",18,24,BlackoutMint,210,38);a.rectTransform.anchoredPosition=new Vector2(106,18);
                a=MissionText(card,p.TraceLimit+" TRACE",-20,22,BlackoutIce,210,38);a.rectTransform.anchoredPosition=new Vector2(106,-20);
                a=MissionText(card,i<2?"PRISM ROUTING":i<3?"SPLIT BEAMS":i<7?"RELAY SHUTTERS":"DUAL RELAYS",-58,17,Hex("#8eacba"),210,30);a.rectTransform.anchoredPosition=new Vector2(106,-58);
                var button=BlackoutButton(card,medal>0?MedalName(medal)+" · REPLAY":unlocked?"ENTER VAULT":"LOCKED",0,-109,396,()=>LaunchMission(SkyPulseMode.BlackoutProtocol,index),64);button.interactable=unlocked;
            }
            BlackoutButton(missionSelectionScreen.transform,"MAIN MENU",-235,-875,430,ResetToMenu);BlackoutButton(missionSelectionScreen.transform,"HOW TO PLAY",235,-875,430,OpenModeGuide);
        }
        void RestartBlackoutVault()
        {
            blackoutSession=blackoutCampaign.Start(selectedMission,true);missionFinalized=false;state=FlightState.Playing;BuildBlackoutLab();RefreshScreens();
            SaveBlackoutAttempt();
        }
        void SaveBlackoutAttempt()
        {
            try{blackoutCampaign.SaveAttempt(selectedMission,blackoutSession);}
            catch(Exception e){blackoutCompletionToast.text="PROGRESS SAVE INTERRUPTED";blackoutToastRemaining=2;Debug.LogWarning("SkyPulse heist attempt save failed: "+e.GetType().Name);}
        }
        void BuildBlackoutLab()
        {
            blackoutGeneration++;FenceGameplayBoundary();blackoutScreen.SetActive(true);BlackoutBackdrop(blackoutScreen.transform);var p=blackoutSession.Puzzle;
            if(!blackoutLabBuilt)
            {
                blackoutLabBuilt=true;var root=(RectTransform)blackoutScreen.transform;root.SetParent(safeAreaRoot,false);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.sizeDelta=new Vector2(1080,1680);
                var header=BlackoutPanel(root,"Signal heist command deck",new Vector2(0,747),new Vector2(1010,160));
                MissionText(header,"Signal Heist",47,44,BlackoutIce,950,50);blackoutVaultText=MissionText(header,"",-5,28,BlackoutMint,950,38);blackoutCompletionToast=MissionText(header,"",-49,23,BlackoutIce,950,35);
                var telemetry=BlackoutPanel(root,"Optical telemetry",new Vector2(0,610),new Vector2(1010,112));
                blackoutDataText=BlackoutReadout(telemetry,"Cores lit",-338,BlackoutMint);blackoutTraceText=BlackoutReadout(telemetry,"Trace left",0,BlackoutIce);blackoutUplinkText=BlackoutReadout(telemetry,"Uplink",338,Hex("#ffc478"));
                blackoutTraceGauge=BlackoutGraphic(root,"Trace budget rail",new Vector2(0,539),new Vector2(1010,8),BlackoutSurface.Trace,false);
                var viewObject=new GameObject("Rendered 3D optical console",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage),typeof(SkyPulseHeistBoard));viewObject.transform.SetParent(root,false);
                var view=viewObject.GetComponent<RawImage>();view.rectTransform.sizeDelta=new Vector2(1080,1080);blackoutBoard=viewObject.GetComponent<SkyPulseHeistBoard>();blackoutBoard.Build(view);
                for(int i=0;i<49;i++)
                {
                    var hit=CreatePanel(root,"Optical socket "+i,Vector2.zero,new Vector2(150,150),Color.clear);
                    var label=CreateText(hit,"",new Vector2(0,-59),new Vector2(142,40),29,BlackoutIce,TextAnchor.MiddleCenter,FontStyle.Bold);label.raycastTarget=false;blackoutNodeLabels.Add(label);
                    var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=hit.GetComponent<Image>();button.transition=Selectable.Transition.None;blackoutNodeButtons.Add(button);
                    var press=hit.gameObject.AddComponent<SkyPulsePressTarget>();press.Tile=i;press.Press=HandleBlackoutPress;press.Release=ReleaseGameplayPointer;blackoutPressTargets.Add(press);
                }
                blackoutStatusText=MissionText(root,"",-560,28,BlackoutIce,1010,40);
                blackoutBriefing=MissionText(root,"",-633,30,Hex("#9fc1ca"),1010,96);
                BlackoutButton(root,"Restart",-260,-760,490,RestartBlackoutVault,150);BlackoutButton(root,"Pause",260,-760,490,PauseFlight,150);
            }
            float stride=155,vertical=160,top=(p.Height-1)*vertical*.5f;
            blackoutVaultText.text="Vault "+(selectedMission+1).ToString("00")+" / 08 · "+p.Name;blackoutCompletionToast.text="Light every core. Reach the uplink.";blackoutBriefing.text=p.Briefing;
            for(int i=0;i<49;i++)
            {
                bool used=i<p.Count;blackoutNodeButtons[i].gameObject.SetActive(used);if(!used)continue;
                blackoutPositions[i]=new Vector2((i%p.Width-(p.Width-1)*.5f)*stride,top-i/p.Width*vertical);
                ((RectTransform)blackoutNodeButtons[i].transform).anchoredPosition=blackoutPositions[i];blackoutPressTargets[i].Generation=blackoutGeneration;
            }
            blackoutBoard.Bind(blackoutSession,blackoutPositions);
            for(int i=0;i<p.Count;i++)((RectTransform)blackoutNodeButtons[i].transform).anchoredPosition=blackoutPositions[i];
            FitBlackoutToSafeArea();UpdateBlackoutPresentation();RefreshBlackoutLab();
            if(blackoutSession.State==MissionSessionState.Succeeded||blackoutSession.State==MissionSessionState.Failed)FinalizeMission();
        }
        void RefreshBlackoutLab()
        {
            if(blackoutSession==null||blackoutBoard==null)return;var s=blackoutSession;var p=s.Puzzle;
            for(int i=0;i<p.Count;i++)
            {
                char c=p.Tile(i);
                blackoutNodeButtons[i].interactable=state==FlightState.Playing&&s.State==MissionSessionState.Playing&&p.Rotatable(i);
                blackoutNodeLabels[i].text=c=='S'?"LASER":c=='E'?"UPLINK":c=='C'?"CORE":c=='R'?"RELAY A":c=='Q'?"RELAY B":c=='G'?"A":c=='H'?"B":c=='!'?"+2":"";
                blackoutNodeLabels[i].color=c=='!'?Hex("#ff8992"):s.Powered[i]?BlackoutIce:Hex("#718d9d");
            }
            blackoutDataText.text=s.CoresPowered+" / "+p.CoreCount;blackoutTraceText.text=s.Remaining+" / "+p.TraceLimit;blackoutUplinkText.text=s.UplinkPowered?"LIVE":"OFFLINE";blackoutUplinkText.color=s.UplinkPowered?BlackoutMint:Hex("#9bb9c6");
            blackoutTraceGauge.TraceFraction=s.Trace/(float)p.TraceLimit;blackoutTraceGauge.SetVerticesDirty();blackoutBoard.Tick(0);
            blackoutStatusText.text=s.Alarms>0?"Sensor active · each turn costs 3 trace":s.CoresPowered==p.CoreCount?"All cores lit. Connect the uplink.":"Tap a mirror to redirect the light";
            blackoutStatusText.color=s.Alarms>0?Hex("#ff8992"):BlackoutIce;
        }
        void TouchBlackoutNode(int tile)
        {
            if(activeMode!=SkyPulseMode.BlackoutProtocol||state!=FlightState.Playing||blackoutSession==null)return;var s=blackoutSession;int cores=s.CoresPowered;
            if(!s.Rotate(tile))return;blackoutBoard.Hit(tile);RefreshBlackoutLab();Play(s.Alarms>0?crashSound:s.CoresPowered>cores?crystalSound:scoreSound);
            if(s.CoresPowered>cores||s.Alarms>0)PulseHaptic(.12f);
            SaveBlackoutAttempt();
            if(s.State==MissionSessionState.Succeeded||s.State==MissionSessionState.Failed)FinalizeMission();
        }
        void UpdateBlackoutActionAnimation(float dt)
        {
            if(state!=FlightState.Playing||blackoutSession==null)return;
            if(blackoutToastRemaining>0){blackoutToastRemaining=Mathf.Max(0,blackoutToastRemaining-dt);if(blackoutToastRemaining==0)blackoutCompletionToast.text="Light every core. Reach the uplink.";}
        }
        void ActivateNextBlackoutPuzzle(int completed)
        {
            selectedMission=completed+1;blackoutSession=blackoutCampaign.Start(selectedMission,true);missionFinalized=false;BuildBlackoutLab();blackoutBoard.Reveal=1;
            blackoutCompletionToast.text="VAULT "+(completed+1).ToString("00")+" EXTRACTED · NEXT VAULT LIVE";blackoutToastRemaining=1.3f;
        }
        void BuildBlackoutResult(bool won,string reason,int medal)
        {
            BlackoutBackdrop(missionResultScreen.transform);MissionText(missionResultScreen.transform,won?"VAULT EXTRACTED":"SIGNAL LOST",835,52,won?BlackoutMint:Hex("#ff8595"));MissionText(missionResultScreen.transform,blackoutSession.Puzzle.Name,750,28,BlackoutIce);
            DepthStage(missionResultScreen.transform,SkyPulseMode.BlackoutProtocol,new Vector2(0,370),new Vector2(850,510));
            var report=BlackoutPanel(missionResultScreen.transform,"Heist report",new Vector2(0,5),new Vector2(920,195));MissionText(report,won?MedalName(medal)+" EXTRACTION":reason,40,36,won?Hex("#ffce89"):Hex("#ff8595"),860,55);if(reason.Contains("SAVE FAILED"))MissionText(missionResultScreen.transform,"PROGRESS SAVE FAILED · REPLAY TO SAVE",-190,24,Hex("#ff8595"));MissionText(report,"TRACE "+blackoutSession.Trace+" / "+blackoutSession.Puzzle.TraceLimit,-29,24,BlackoutIce);
            bool saveFailed=reason.Contains("SAVE FAILED");int vault=selectedMission;
            BlackoutButton(missionResultScreen.transform,saveFailed?"RETRY SAVE":won?"REPLAY HEIST":"RETRY VAULT",0,-390,900,()=>{if(saveFailed)LaunchMission(SkyPulseMode.BlackoutProtocol,vault);else if(won)LaunchMission(SkyPulseMode.BlackoutProtocol,0);else RestartBlackoutVault();});
            BlackoutButton(missionResultScreen.transform,"VAULT MAP",0,-545,900,OpenBlackoutVaults);BlackoutButton(missionResultScreen.transform,"MAIN MENU",0,-690,900,ResetToMenu);
            MissionText(missionResultScreen.transform,saveFailed?"VAULT EXTRACTED · PROGRESS SAVE INTERRUPTED":won?"ALL EIGHT VAULTS EXTRACTED":"REWIRE THE LIGHT · TRY ANOTHER ROUTE",-840,22,BlackoutMint);
        }
    }
}
