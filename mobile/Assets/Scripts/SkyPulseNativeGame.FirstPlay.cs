using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        // These preferences only control optional teaching. They never migrate
        // scores, medals, purchases or the existing flight-coach save key.
        const string FirstPlayEnabledKey="skypulse.first-play.tips.v1";
        static readonly string[] FirstPlayLearnedKeys={"skypulse.first-play.endless.v1","skypulse.first-play.relay.v1","skypulse.first-play.heist.v1"};
        readonly bool[] firstPlayLearned=new bool[3];
        IMissionStore firstPlayStore;
        bool firstPlayTips=true,firstPlayActive,firstPlayFlapped,firstPlayTurned;
        int firstPlayReplayMode=-1,firstPlayMode,firstPlayStep=-1;
        float firstPlaySeconds;
        RelaySession firstPlayRelay;
        BlackoutSession firstPlayHeist;
        Text firstPlayLabel;
        string firstPlayOriginalText;
        int firstPlayOriginalFont;
        Vector2 firstPlayOriginalSize,firstPlayOriginalPosition;
        bool firstPlayStyled;
        const float FirstPlayVisibleSeconds=24f;

        void ReadFirstPlayPreferences()
        {
            if(missionStore==null||ReferenceEquals(firstPlayStore,missionStore))return;
            firstPlayStore=missionStore;
            firstPlayTips=firstPlayStore.GetInt(FirstPlayEnabledKey,1)!=0;
            for(int i=0;i<3;i++)firstPlayLearned[i]=firstPlayStore.GetInt(FirstPlayLearnedKeys[i],0)==1;
        }
        // Called only when an actual run/attempt starts. A retry may show
        // unlearned tips again, but returning Home never records completion.
        void BeginFirstPlayGuidance()
        {
            RestoreFirstPlayLabel();ReadFirstPlayPreferences();
            firstPlayMode=(int)activeMode;firstPlaySeconds=0;firstPlayStep=-1;
            firstPlayFlapped=firstPlayTurned=false;
            firstPlayRelay=relaySession;firstPlayHeist=blackoutSession;
            bool replay=firstPlayReplayMode==firstPlayMode;firstPlayReplayMode=-1;
            firstPlayActive=firstPlayTips&&(!firstPlayLearned[firstPlayMode]||replay);
        }
        void NotifyFirstPlayFlap()
        {
            if(firstPlayActive&&state==FlightState.Playing&&activeMode!=SkyPulseMode.BlackoutProtocol&&(int)activeMode==firstPlayMode)
                firstPlayFlapped=true;
        }
        // Observe the authoritative post-rotation session BEFORE FinalizeMission
        // can replace it with the next puzzle. PLAN/preview never calls this.
        void NotifyFirstPlayBlackoutMove(BlackoutSession session)
        {
            if(!firstPlayActive||state!=FlightState.Playing||firstPlayMode!=(int)SkyPulseMode.BlackoutProtocol||activeMode!=SkyPulseMode.BlackoutProtocol||
                !ReferenceEquals(firstPlayHeist,session)||session==null||session.Turns==0)return;
            firstPlayTurned=true;
            if(session.State==MissionSessionState.Succeeded)CompleteFirstPlayGuidance();
        }
        void CompleteFirstPlayGuidance()
        {
            if(!firstPlayActive)return;
            ReadFirstPlayPreferences();
            if(!firstPlayLearned[firstPlayMode]&&firstPlayStore!=null)
            {
                try
                {
                    firstPlayStore.SetInt(FirstPlayLearnedKeys[firstPlayMode],1);firstPlayStore.Flush();
                    firstPlayLearned[firstPlayMode]=true;
                }
                catch(Exception e)
                {
                    // A failed preference write cannot interrupt a run or its
                    // reward transaction. Retry teaching next launch instead.
                    Debug.LogWarning("SkyPulse play-tip preference save deferred: "+e.GetType().Name);
                }
            }
            firstPlayActive=false;RestoreFirstPlayLabel();
        }
        void UpdateFirstPlayGuidance(float dt)
        {
            ReadFirstPlayPreferences();
            bool playing=state==FlightState.Playing&&(int)activeMode==firstPlayMode;
            if(!firstPlayActive||!firstPlayTips||!playing||AuxiliaryScreenOpen||
                privacyScreen!=null&&privacyScreen.activeSelf||flightGuideScreen!=null&&flightGuideScreen.activeSelf)
            {RestoreFirstPlayLabel();return;}
            // Pause and auxiliary UI do not spend reading time. No animation or
            // countdown blocks the board, flight input or automatic progression.
            if(float.IsFinite(dt)&&dt>0)firstPlaySeconds+=Mathf.Min(dt,.1f);
            int step;Text target;string copy;
            if(activeMode==SkyPulseMode.EndlessRoute)
            {
                if(firstPlayFlapped&&score>0){CompleteFirstPlayGuidance();return;}
                step=firstPlayFlapped?1:0;target=hudCoachText;
                copy=step==0?"TAP TO FLAP\nKEEP ABOVE THE GROUND":"AIM FOR THE OPENING\nFLY BETWEEN THE PIPE CAPS";
            }
            else if(activeMode==SkyPulseMode.PulseRelay)
            {
                if(!ReferenceEquals(firstPlayRelay,relaySession)||relaySession==null){RestoreFirstPlayLabel();return;}
                if(firstPlayFlapped&&relaySession.Passed>0){CompleteFirstPlayGuidance();return;}
                int cells=relaySession.GateCrystals(0);
                step=!firstPlayFlapped?0:cells==0?1:cells==1?2:3;target=relayHintText;
                copy=step==0?"TAP THE FLIGHT AREA TO FLAP":step==1?"COLLECT BOTH CRYSTALS MARKED 01":
                    step==2?"ONE MORE 01 CRYSTAL OPENS THE GATE":"GATE 01 IS OPEN · FLY THROUGH THE HOOP";
                // The gate's actual unlock confirmation takes precedence.
                if(relayUnlockFeedback>0){RestoreFirstPlayLabel();return;}
            }
            else
            {
                if(!ReferenceEquals(firstPlayHeist,blackoutSession)||blackoutSession==null){RestoreFirstPlayLabel();return;}
                step=firstPlayTurned?1:0;target=blackoutBriefing;
                copy=step==0?"Tap a silver prism to turn it and redirect the live beam.":"Keep every numbered core and the amber uplink lit together.";
            }
            if(firstPlaySeconds>=FirstPlayVisibleSeconds){RestoreFirstPlayLabel();return;}
            if(target==null)return;
            if(firstPlayLabel!=target)
            {
                RestoreFirstPlayLabel();firstPlayLabel=target;firstPlayOriginalText=target.text;
                firstPlayOriginalFont=target.fontSize;firstPlayOriginalSize=target.rectTransform.sizeDelta;firstPlayOriginalPosition=target.rectTransform.anchoredPosition;
                firstPlayStyled=true;
                if(activeMode==SkyPulseMode.EndlessRoute)
                {target.fontSize=36;target.rectTransform.sizeDelta=new Vector2(920,100);target.rectTransform.anchoredPosition=new Vector2(0,570);}
                else if(activeMode==SkyPulseMode.PulseRelay)target.fontSize=27;
            }
            if(firstPlayStep!=step||target.text!=copy){firstPlayStep=step;target.text=copy;}
            target.raycastTarget=false;target.gameObject.SetActive(true);
        }
        void RestoreFirstPlayLabel()
        {
            if(firstPlayLabel!=null&&firstPlayStyled)
            {
                firstPlayLabel.fontSize=firstPlayOriginalFont;firstPlayLabel.rectTransform.sizeDelta=firstPlayOriginalSize;firstPlayLabel.rectTransform.anchoredPosition=firstPlayOriginalPosition;
                if(firstPlayLabel==hudCoachText)firstPlayLabel.gameObject.SetActive(false);
                else if(firstPlayLabel==relayHintText)
                    firstPlayLabel.text=relayUnlockFeedback>0?relayUnlockCaption:relayFlightCaption;
                else if(firstPlayLabel==blackoutBriefing&&blackoutSession!=null)firstPlayLabel.text=blackoutSession.Puzzle.Briefing;
                else firstPlayLabel.text=firstPlayOriginalText;
            }
            firstPlayStyled=false;firstPlayLabel=null;firstPlayStep=-1;
        }
        void SetFirstPlayTipsEnabled(bool enabled)
        {
            ReadFirstPlayPreferences();firstPlayTips=enabled;
            if(!enabled){firstPlayActive=false;RestoreFirstPlayLabel();}
            try{firstPlayStore?.SetInt(FirstPlayEnabledKey,enabled?1:0);firstPlayStore?.Flush();}
            catch(Exception e){Debug.LogWarning("SkyPulse play-tip preference save deferred: "+e.GetType().Name);}
        }
        void ReplayFirstPlayGuidance(SkyPulseMode mode)
        {
            // A queued click from the previous guide cannot restart a run.
            if(modeGuideScreen==null||!modeGuideScreen.activeInHierarchy)return;
            SetFirstPlayTipsEnabled(true);firstPlayReplayMode=(int)mode;
            // Replaying tips uses a normal run with normal rules. Previously
            // earned completion keys, scores and medals remain unchanged.
            if(modeGuideScreen!=null)modeGuideScreen.SetActive(false);
            SetHomeProtocol(mode);FenceGameplayBoundary();LaunchHomeProtocol();
        }
        // Add after the normal guide has been built. Both actions are outside
        // gameplay; the reading regions themselves never intercept a touch.
        void AddFirstPlayGuideControls()
        {
            ReadFirstPlayPreferences();var mode=homeProtocol;var accent=ProtocolAccent(mode);
            foreach(var text in modeGuideScreen.GetComponentsInChildren<Text>())
                if(text.text=="Crystals are earned in play. No real-money purchases.")
                {text.rectTransform.anchoredPosition=new Vector2(0,-566);text.rectTransform.sizeDelta=new Vector2(870,54);text.fontSize=23;}
            foreach(var button in modeGuideScreen.GetComponentsInChildren<Button>())
                if(button.GetComponentInChildren<Text>()?.text=="BACK")
                    ((RectTransform)button.transform).anchoredPosition=new Vector2(0,-885);
            var replay=MissionButton(modeGuideScreen.transform,"PLAY WITH TIPS",-230,-693,430,()=>ReplayFirstPlayGuidance(mode),accent,176);
            replay.gameObject.name="Guide · play with tips";
            Button toggle=null;
            toggle=MissionButton(modeGuideScreen.transform,firstPlayTips?"TIPS: ON":"TIPS: OFF",230,-693,430,()=>
            {
                if(modeGuideScreen==null||!modeGuideScreen.activeInHierarchy||toggle==null||!toggle.IsActive())return;
                SetFirstPlayTipsEnabled(!firstPlayTips);toggle.GetComponentInChildren<Text>().text=firstPlayTips?"TIPS: ON":"TIPS: OFF";
            },Hex("#9cb6ca"),176);
            toggle.gameObject.name="Guide · optional play tips";
        }
    }
}
