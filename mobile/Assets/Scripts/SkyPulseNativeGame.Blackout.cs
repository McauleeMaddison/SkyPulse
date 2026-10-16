using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        BlackoutCampaignProgress blackoutCampaign;
        readonly List<SkyPulseBlackoutGraphic> blackoutGraphics=new List<SkyPulseBlackoutGraphic>();
        readonly List<SkyPulseBlackoutGraphic> blackoutNodes=new List<SkyPulseBlackoutGraphic>();
        readonly List<Button> blackoutNodeButtons=new List<Button>();
        readonly List<Text> blackoutNodeLabels=new List<Text>();
        readonly List<int> blackoutEdgeA=new List<int>(84),blackoutEdgeB=new List<int>(84);
        Vector2[] blackoutPositions;
        SkyPulseBlackoutGraphic blackoutNetwork,blackoutTraceGauge,blackoutHomeCore,blackoutHomeBackdrop;
        // Board controls are created once, then rebound with a generation token.
        bool blackoutLabBuilt;
        int blackoutTileCount,blackoutTrailCount;
        RectTransform blackoutDeck;
        readonly Vector2[] blackoutTrailBuffer=new Vector2[18];
        readonly List<SkyPulsePressTarget> blackoutPressTargets=new List<SkyPulsePressTarget>(49);
        readonly List<SkyPulseButtonFeedback> blackoutPressFeedback=new List<SkyPulseButtonFeedback>(49);
        float blackoutActionElapsed,blackoutActionDuration,blackoutToastRemaining;
        Text blackoutCompletionToast;
        int blackoutSelectedFirewall=-1;
        Text blackoutSectorText,blackoutActionText,blackoutPulseText;
        Text blackoutPhaseText,blackoutIceKeyText,blackoutVioletKeyText;
        static Color BlackoutMint=>Hex("#66ffc3");
        static Color BlackoutIce=>Hex("#c4eaff");

        SkyPulseBlackoutGraphic BlackoutGraphic(Transform parent,string name,Vector2 at,Vector2 size,BlackoutSurface surface,bool animate=true)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(SkyPulseBlackoutGraphic));obj.transform.SetParent(parent,false);
            var g=obj.GetComponent<SkyPulseBlackoutGraphic>();g.rectTransform.sizeDelta=size;g.rectTransform.anchoredPosition=at;g.Surface=surface;
            g.Animate=animate;g.ReducedMotion=reduceMotionEnabled;g.raycastTarget=false;blackoutGraphics.Add(g);return g;
        }
        void BlackoutBackdrop(Transform parent)
        {
            Transform backing=parent;
            foreach(Transform child in parent)if(child is RectTransform rect && interfaceBackdrops.Contains(rect)){backing=child;break;}
            var cached=backing.Find("Obsidian signal chamber");
            if(cached!=null){cached.gameObject.SetActive(true);return;}
            var g=BlackoutGraphic(backing,"Obsidian signal chamber",Vector2.zero,Vector2.zero,BlackoutSurface.Backdrop);
            var r=g.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        }
        void PrepareBlackoutScreenClear(GameObject root)
        {
            foreach(var g in root.GetComponentsInChildren<SkyPulseBlackoutGraphic>(true))
                if(g.Surface==BlackoutSurface.Backdrop)g.gameObject.SetActive(false);
        }
        RectTransform BlackoutPanel(Transform root,string name,Vector2 at,Vector2 size)
        {
            var panel=CreatePanel(root,name,at,size,Color.clear);panel.GetComponent<Image>().raycastTarget=false;
            BlackoutGraphic(panel,"Brushed titanium surface",Vector2.zero,size,BlackoutSurface.Deck,false);return panel;
        }
        Button BlackoutButton(Transform root,string name,float x,float y,float width,Action action,float height=90)
        {
            var b=MissionButton(root,name,x,y,width,action,BlackoutMint,height);
            if(b.targetGraphic!=null)b.targetGraphic.gameObject.SetActive(false);
            var surface=BlackoutGraphic(b.transform,"Obsidian action plate",Vector2.zero,new Vector2(width,height),BlackoutSurface.Deck,false);surface.transform.SetAsFirstSibling();b.targetGraphic=surface;
            return b;
        }
        void ApplyBlackoutHomeView()
        {
            if(blackoutHomeCore==null)
            {
                blackoutHomeCore=BlackoutGraphic(homeScreen.transform,"Blackout suspended core",new Vector2(0,355),new Vector2(850,510),BlackoutSurface.Core);
                blackoutHomeCore.transform.SetSiblingIndex(menuHeroTransform.GetSiblingIndex());
                blackoutHomeBackdrop=BlackoutGraphic(homeProtocolBackdrop.transform,"Blackout Home chamber",Vector2.zero,Vector2.zero,BlackoutSurface.Backdrop);
                var r=blackoutHomeBackdrop.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            }
            bool on=homeProtocol==SkyPulseMode.BlackoutProtocol;
            blackoutHomeCore.gameObject.SetActive(on);blackoutHomeBackdrop.gameObject.SetActive(on);
            if(on)
            {
                int count=0;foreach(int medal in blackoutCampaign.Data.medals)if(medal>0)count++;
                menuEquippedText.text="8 SECTORS · BREACH THE LATTICE";
                homeProtocolCaption.text="BLACKOUT PROTOCOL · EXTRACT THE CORE";
                if(homeBestCaption!=null)homeBestCaption.text="SECTORS CLEARED";
                menuBestText.text=count+" / 8";
            }

        }
        void UpdateBlackoutPresentation()
        {
            for(int i=blackoutGraphics.Count-1;i>=0;i--)
            {
                var g=blackoutGraphics[i];if(g==null){blackoutGraphics.RemoveAt(i);continue;}
                g.ReducedMotion=reduceMotionEnabled;g.MotionPaused=state==FlightState.Paused;
            }
            for(int i=0;i<blackoutPressFeedback.Count;i++)blackoutPressFeedback[i].ReducedMotion=reduceMotionEnabled;
        }
        void ResetBlackoutEffects()
        {
            blackoutGeneration++;FenceGameplayBoundary();blackoutSelectedFirewall=-1;
            blackoutActionElapsed=blackoutActionDuration=blackoutToastRemaining=0;blackoutTrailCount=0;
            if(blackoutNetwork!=null){blackoutNetwork.Travel=1;blackoutNetwork.Impact=0;}
            if(blackoutPulseText!=null)blackoutPulseText.text="";
        }
        void OpenBlackoutSectors()
        {
            SetHomeProtocol(SkyPulseMode.BlackoutProtocol);ResetToMenu();homeScreen.SetActive(false);activeMode=SkyPulseMode.BlackoutProtocol;
            ClearScreen(missionSelectionScreen);missionSelectionScreen.SetActive(true);BlackoutBackdrop(missionSelectionScreen.transform);
            MissionText(missionSelectionScreen.transform,"BLACKOUT",895,58,BlackoutIce);
            MissionText(missionSelectionScreen.transform,"P R O T O C O L   /   S E C T O R S",827,22,BlackoutMint);
            MissionText(missionSelectionScreen.transform,"EACH EXTRACTION OPENS A DEEPER NETWORK",756,22,Hex("#83a3b7"));
            for(int i=0;i<SkyPulseBlackoutCampaign.Stages.Length;i++)
            {
                int index=i;var p=SkyPulseBlackoutCampaign.Stages[i];float x=i%2==0?-240:240,y=555-(i/2)*332;
                var card=BlackoutPanel(missionSelectionScreen.transform,"Sector "+(i+1),new Vector2(x,y),new Vector2(458,300));
                bool unlocked=blackoutCampaign.CanPlay(i);int medal=blackoutCampaign.Data.medals[i];
                MissionText(card,(i+1).ToString("00")+"  "+p.Name,108,25,unlocked?BlackoutIce:Hex("#597081"),420,54);
                var map=BlackoutGraphic(card,"Sector topology",new Vector2(-112,-1),new Vector2(132,132),BlackoutSurface.MiniMap,false);map.Miniature=p;
                MissionText(card,CountCores(p)+" CORES",20,24,unlocked?BlackoutMint:Hex("#597081"),210,38).rectTransform.anchoredPosition=new Vector2(105,20);
                MissionText(card,p.TraceLimit+" TRACE",-16,22,Hex("#86aabd"),210,38).rectTransform.anchoredPosition=new Vector2(105,-16);
                string systems=p.CipherKeyMask==3?"DUAL CIPHER":p.HasPhaseRouting?"PHASE ROUTING":p.CipherKeyMask>0?"CIPHER VAULT":"CORE RECOVERY";
                MissionText(card,systems,-53,16,Hex("#a0bac9"),210,30).rectTransform.anchoredPosition=new Vector2(105,-53);
                var launch=BlackoutButton(card,medal>0?MedalName(medal)+" · REPLAY":unlocked?"ENTER SECTOR":"LOCKED",0,-105,396,()=>LaunchMission(SkyPulseMode.BlackoutProtocol,index),64);
                launch.interactable=unlocked;
            }
            MissionText(missionSelectionScreen.transform,"NEXT SECTOR · "+(blackoutCampaign.Unlocked+1).ToString("00")+" / 08",-750,24,BlackoutMint);
            BlackoutButton(missionSelectionScreen.transform,"HOME",-235,-880,430,ResetToMenu);
            BlackoutButton(missionSelectionScreen.transform,"MISSION CONTROL",235,-880,430,OpenMissionControl);
        }
        static int CountCores(BlackoutPuzzle p){int n=0;foreach(var row in p.Rows)foreach(char c in row)if(c=='P')n++;return n;}
        void BuildBlackoutLab()
        {
            blackoutGeneration++;FenceGameplayBoundary();blackoutScreen.SetActive(true);BlackoutBackdrop(blackoutScreen.transform);
            var p=blackoutSession.Puzzle;
            if(!blackoutLabBuilt)
            {
                blackoutLabBuilt=true;
                var header=BlackoutPanel(blackoutScreen.transform,"Blackout command deck",new Vector2(0,880),new Vector2(970,154));
                MissionText(header,"BLACKOUT PROTOCOL",34,45,BlackoutIce,920,56);
                blackoutSectorText=MissionText(header,"",-17,27,Hex("#94b9c9"),920,40);
                blackoutCompletionToast=MissionText(header,"",-59,22,BlackoutMint,920,30);blackoutCompletionToast.raycastTarget=false;
                blackoutDataText=ProtocolStat(blackoutScreen.transform,"CORES", "0",-320,725,300,2,BlackoutMint);
                blackoutTraceText=ProtocolStat(blackoutScreen.transform,"TRACE", "0",0,725,300,2,BlackoutIce);
                blackoutHackText=ProtocolStat(blackoutScreen.transform,"BREACHES", "0",320,725,300,2,Hex("#ffc478"));
                blackoutTraceGauge=BlackoutGraphic(blackoutScreen.transform,"Living trace rail",new Vector2(0,632),new Vector2(930,10),BlackoutSurface.Trace);
                var systems=BlackoutPanel(blackoutScreen.transform,"Cipher and phase telemetry",new Vector2(0,577),new Vector2(956,58));
                blackoutPhaseText=MissionText(systems,"",0,24,BlackoutMint,308,46);blackoutPhaseText.rectTransform.anchoredPosition=new Vector2(-312,0);
                blackoutIceKeyText=MissionText(systems,"",0,24,BlackoutIce,308,46);
                blackoutVioletKeyText=MissionText(systems,"",0,24,Hex("#bb89ff"),308,46);blackoutVioletKeyText.rectTransform.anchoredPosition=new Vector2(312,0);
                blackoutDeck=BlackoutPanel(blackoutScreen.transform,"Floating lattice deck",new Vector2(0,24),new Vector2(980,980));
                blackoutNetwork=BlackoutGraphic(blackoutScreen.transform,"Live data connections",Vector2.zero,new Vector2(1080,1600),BlackoutSurface.Network);
                blackoutPositions=new Vector2[49];blackoutNetwork.EdgeFrom=new Vector2[84];blackoutNetwork.EdgeTo=new Vector2[84];blackoutNetwork.EdgePowered=new bool[84];
                blackoutNetwork.Trail=blackoutTrailBuffer;blackoutNetwork.EdgeCount=blackoutNetwork.TrailCount=0;
                for(int i=0;i<49;i++)
                {
                    var hit=CreatePanel(blackoutScreen.transform,"Terminal node "+i,Vector2.zero,new Vector2(136,136),Color.clear);
                    var g=BlackoutGraphic(hit,"Reactive lattice chip",Vector2.zero,new Vector2(124,124),BlackoutSurface.Node);
                    var label=CreateText(hit,"",new Vector2(0,-44),new Vector2(124,36),28,BlackoutMint,TextAnchor.MiddleCenter,FontStyle.Bold);label.raycastTarget=false;
                    label.resizeTextForBestFit=true;label.resizeTextMinSize=24;label.resizeTextMaxSize=28;
                    var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=hit.GetComponent<Image>();button.transition=Selectable.Transition.None;
                    var press=hit.gameObject.AddComponent<SkyPulsePressTarget>();press.Tile=i;press.Press=HandleBlackoutPress;press.Release=ReleaseGameplayPointer;
                    var feedback=hit.gameObject.AddComponent<SkyPulseButtonFeedback>();feedback.ReducedMotion=reduceMotionEnabled;blackoutPressFeedback.Add(feedback);blackoutNodes.Add(g);blackoutNodeButtons.Add(button);blackoutNodeLabels.Add(label);blackoutPressTargets.Add(press);
                }
                blackoutStatusText=MissionText(blackoutScreen.transform,"",-508,32,BlackoutIce,950,52);
                blackoutPulseText=MissionText(blackoutScreen.transform,"",-562,28,BlackoutMint,950,48);blackoutPulseText.raycastTarget=false;
                blackoutActionText=MissionText(blackoutScreen.transform,"",-562,25,Hex("#8eacba"),950,48);


                BlackoutButton(blackoutScreen.transform,"RESTART",-235,-785,430,()=>LaunchMission(SkyPulseMode.BlackoutProtocol,selectedMission),112);
                BlackoutButton(blackoutScreen.transform,"PAUSE",235,-785,430,PauseFlight,112);

            }
            blackoutTileCount=p.Width*p.Height;
            float stride=Mathf.Min(164,960f/p.Width),vertical=136,top=24+(p.Height-1)*vertical*.5f;
            blackoutDeck.sizeDelta=new Vector2(p.Width*stride+28,p.Height*vertical+28);var deckGraphic=blackoutDeck.GetComponentInChildren<SkyPulseBlackoutGraphic>();deckGraphic.rectTransform.sizeDelta=blackoutDeck.sizeDelta;deckGraphic.SetVerticesDirty();
            blackoutSectorText.text="SECTOR "+(selectedMission+1).ToString("00")+" / 08 · "+p.Name;
            blackoutEdgeA.Clear();blackoutEdgeB.Clear();
            for(int i=0;i<49;i++)
            {
                bool used=i<blackoutTileCount;blackoutNodeButtons[i].gameObject.SetActive(used);if(!used)continue;
                var pos=new Vector2((i%p.Width-(p.Width-1)*.5f)*stride,top-(i/p.Width)*vertical);blackoutPositions[i]=pos;
                var hit=(RectTransform)blackoutNodeButtons[i].transform;hit.anchoredPosition=pos;hit.sizeDelta=new Vector2(stride,vertical);
                blackoutNodes[i].rectTransform.sizeDelta=new Vector2(stride-12,vertical-12);blackoutNodeLabels[i].rectTransform.sizeDelta=new Vector2(stride-12,36);
                blackoutPressTargets[i].Generation=blackoutGeneration;
                if(p.Tile(i)=='#')continue;
                if(i%p.Width<p.Width-1 && p.Tile(i+1)!='#'){blackoutEdgeA.Add(i);blackoutEdgeB.Add(i+1);}
                if(i+p.Width<blackoutTileCount && p.Tile(i+p.Width)!='#'){blackoutEdgeA.Add(i);blackoutEdgeB.Add(i+p.Width);}
            }
            for(int i=0;i<blackoutEdgeA.Count;i++){blackoutNetwork.EdgeFrom[i]=blackoutPositions[blackoutEdgeA[i]];blackoutNetwork.EdgeTo[i]=blackoutPositions[blackoutEdgeB[i]];}
            blackoutNetwork.EdgeCount=blackoutEdgeA.Count;blackoutNetwork.HasPhaseRouting=p.HasPhaseRouting;
            blackoutActionElapsed=blackoutActionDuration=0;blackoutNetwork.Travel=1;blackoutNetwork.Impact=0;blackoutPulseText.text="";blackoutCompletionToast.text="";
            blackoutTrailCount=1;blackoutTrailBuffer[0]=blackoutPositions[blackoutSession.Cursor];blackoutSelectedFirewall=-1;RefreshBlackoutLab();
        }
        void RefreshBlackoutLab()
        {
            if(blackoutSession==null || blackoutNetwork==null)return;
            var s=blackoutSession;var p=s.Puzzle;bool playing=state==FlightState.Playing && s.State==MissionSessionState.Playing;
            for(int i=0;i<blackoutTileCount;i++)
            {
                // Logical position is readable on the same press; transport is
                // decorative and must not hide the player's next valid moves.
                char kind=p.Tile(i);bool recovered=s.Recovered.Contains(i),opened=s.Opened.Contains(i),cursor=s.Cursor==i;
                var g=blackoutNodes[i];g.NodeKind=cursor?5:kind=='#'?1:kind=='P'?(recovered?6:2):kind=='F'?(opened?7:3):kind=='E'?4:kind=='T'?8:0;
                if(!cursor && "CABKkLl".IndexOf(kind)>=0)g.NodeKind=kind=='C'?9:kind=='A'?10:kind=='B'?11:kind=='K'?(recovered?6:12):kind=='k'?(recovered?6:13):kind=='L'?14:15;
                g.Powered=s.Powered(i);g.CircuitPhase=s.CircuitPhase;
                g.Adjacent=s.Adjacent(i) && (kind!='#'||p.PunishMistakes);g.Selected=i==blackoutSelectedFirewall;g.Animate=cursor || kind=='P'&&!recovered || kind=='E' || kind=='T' || kind=='C' || "ABKkLl".IndexOf(kind)>=0;g.SetVerticesDirty();
                blackoutNodeLabels[i].text=cursor?"YOU":kind=='P'?(recovered?"SAVED":"CORE"):kind=='F'?(opened?"OPEN":"BREACH"):kind=='E'?"UPLINK":kind=='T'?"+2":kind=='C'?"SWITCH":kind=='A'?(g.Powered?"MINT":"OFF"):kind=='B'?(g.Powered?"AMBER":"OFF"):kind=='K'?(recovered?"SAVED":"ICE"):kind=='k'?(recovered?"SAVED":"VIOLET"):kind=='L'||kind=='l'?(g.Powered?"OPEN":"LOCK"):"";
                blackoutNodeLabels[i].color=!g.Powered?Hex("#8f9aab"):kind=='T'?Hex("#ff8595"):kind=='k'||kind=='l'?Hex("#c6a1ff"):kind=='K'||kind=='L'?BlackoutIce:kind=='B'||kind=='F'&&!opened?Hex("#ffd08f"):BlackoutMint;
                blackoutNodeButtons[i].interactable=playing && g.Adjacent;
            }
            blackoutDataText.text=s.Packets+" / "+s.RequiredPackets;blackoutTraceText.text=s.Trace+" / "+p.TraceLimit;blackoutHackText.text=s.Charges.ToString();
            blackoutTraceGauge.TraceFraction=s.Trace/(float)p.TraceLimit;blackoutTraceGauge.SetVerticesDirty();
            blackoutNetwork.TrailCount=blackoutTrailCount;blackoutNetwork.SetVerticesDirty();
            blackoutNetwork.CircuitPhase=s.CircuitPhase;
            for(int i=0;i<blackoutEdgeA.Count;i++)
            {
                int a=blackoutEdgeA[i],b=blackoutEdgeB[i];
                blackoutNetwork.EdgePowered[i]=s.Powered(a)&&s.Powered(b)&&(p.Tile(a)!='F'||s.Opened.Contains(a))&&(p.Tile(b)!='F'||s.Opened.Contains(b));
            }
            if(blackoutPhaseText!=null)
            {
                blackoutPhaseText.text=p.HasPhaseRouting?"PHASE · "+(s.CircuitPhase==0?"MINT":"AMBER"):"CIRCUIT · STABLE";
                blackoutPhaseText.color=s.CircuitPhase==0?BlackoutMint:Hex("#ffd08f");
                blackoutIceKeyText.text=(p.CipherKeyMask&1)==0?"ICE · —":(s.CipherKeys&1)!=0?"ICE KEY · READY":"ICE KEY · MISSING";
                blackoutVioletKeyText.text=(p.CipherKeyMask&2)==0?"VIOLET · —":(s.CipherKeys&2)!=0?"VIOLET KEY · READY":"VIOLET KEY · MISSING";
            }
            int spare=p.TraceLimit-s.Trace;
            blackoutStatusText.text=s.Packets==s.RequiredPackets?"ALL CORES SECURED · REACH THE UPLINK":(s.RequiredPackets-s.Packets)+" CORE"+(s.RequiredPackets-s.Packets==1?"":"S")+" REMAINING";
            blackoutActionText.text=spare<=2?"TRACE CRITICAL · "+spare+" REMAINING":"";
            var actionColour=spare<=2?Hex("#ff8595"):Hex("#8eacba");actionColour.a=blackoutActionElapsed<blackoutActionDuration&&!string.IsNullOrEmpty(blackoutPulseText.text)?0:1;blackoutActionText.color=actionColour;
        }
        void TouchBlackoutNode(int tile)
        {
            if(activeMode!=SkyPulseMode.BlackoutProtocol || state!=FlightState.Playing || blackoutSession==null || blackoutSession.State!=MissionSessionState.Playing || !blackoutSession.Adjacent(tile))return;
            bool breach=blackoutSession.Puzzle.Tile(tile)=='F'&&!blackoutSession.Opened.Contains(tile);
            AnimateBlackoutAction(tile,breach);
        }
        void AnimateBlackoutAction(int tile,bool breach)
        {
            if(state!=FlightState.Playing || activeMode!=SkyPulseMode.BlackoutProtocol || blackoutSession==null)return;
            var session=blackoutSession;
            int cores=session.Packets,keys=session.CipherKeys,phase=session.CircuitPhase,charges=session.Charges;
            var visual=blackoutNetwork.Travel<1?Vector2.Lerp(blackoutNetwork.Origin,blackoutNetwork.Target,Mathf.SmoothStep(0,1,blackoutNetwork.Travel)):blackoutPositions[session.Cursor];
            if(!session.Act(tile,breach))return;
            bool rejected=session.LastActionFailed;blackoutSelectedFirewall=-1;
            if(!breach&&!rejected)
            {
                if(blackoutTrailCount==18){for(int i=1;i<18;i++)blackoutTrailBuffer[i-1]=blackoutTrailBuffer[i];blackoutTrailCount--;}
                blackoutTrailBuffer[blackoutTrailCount++]=blackoutPositions[tile];
            }
            blackoutNetwork.Origin=visual;blackoutNetwork.Target=blackoutPositions[tile];blackoutNetwork.Travel=breach||rejected||reduceMotionEnabled?1:0;blackoutNetwork.Impact=1;
            bool switched=phase!=session.CircuitPhase,keyRecovered=keys!=session.CipherKeys;
            blackoutNetwork.ImpactKind=rejected?2:breach?1:session.Puzzle.Tile(tile)=='T'?2:session.Packets>cores?3:switched?4:keyRecovered?(session.Puzzle.Tile(tile)=='k'?6:5):0;
            blackoutPulseText.text=rejected?(breach?"BREACH FAILED · "+(charges>session.Charges?"-1 CHARGE / ":"")+"+3 TRACE":"MOVE BLOCKED · +1 TRACE"):breach?"FIREWALL OPEN · TAP TO ENTER":session.Packets>cores?"CORE SECURED":switched?(session.CircuitPhase==0?"MINT":"AMBER")+" CIRCUIT ONLINE":keyRecovered?"CIPHER KEY SECURED":session.Puzzle.Tile(tile)=='T'?"SECURITY NODE · +2 TRACE":"";
            blackoutPulseText.color=rejected?Hex("#ff8595"):breach||switched&&session.CircuitPhase==1?Hex("#ffd08f"):BlackoutIce;
            blackoutActionElapsed=0;blackoutActionDuration=reduceMotionEnabled?.24f:.28f;
            Play(rejected?crashSound:session.Packets>cores||keyRecovered?unlockSound:scoreSound);
            if(rejected||breach||switched||keyRecovered||session.Packets>cores)PulseHaptic(.12f);
            RefreshBlackoutLab();
            // Completion and next-stage activation are synchronous; neither the
            // cursor transport nor celebration owns progression callbacks.
            if(session.State==MissionSessionState.Succeeded || session.State==MissionSessionState.Failed)FinalizeMission();
        }
        void UpdateBlackoutActionAnimation(float dt)
        {
            if(state!=FlightState.Playing || blackoutSession==null || blackoutNetwork==null)return;
            if(blackoutToastRemaining>0){blackoutToastRemaining=Mathf.Max(0,blackoutToastRemaining-dt);if(blackoutToastRemaining==0)blackoutCompletionToast.text="";}
            if(blackoutActionElapsed>=blackoutActionDuration)return;
            blackoutActionElapsed=Mathf.Min(blackoutActionDuration,blackoutActionElapsed+dt);
            bool moving=blackoutNetwork.Travel<1;
            blackoutNetwork.Travel=reduceMotionEnabled?1:Mathf.Clamp01(blackoutActionElapsed/.12f);blackoutNetwork.Impact=1-blackoutActionElapsed/blackoutActionDuration;blackoutNetwork.SetVerticesDirty();
            if(moving&&blackoutNetwork.Travel>=1){var node=blackoutNodes[blackoutSession.Cursor];node.NodeKind=5;node.Animate=true;node.SetVerticesDirty();blackoutNodeLabels[blackoutSession.Cursor].text="YOU";}
            var hue=blackoutPulseText.color;hue.a=reduceMotionEnabled?1:blackoutNetwork.Impact;blackoutPulseText.color=hue;
            if(blackoutActionElapsed>=blackoutActionDuration){blackoutPulseText.text="";var idle=blackoutActionText.color;idle.a=1;blackoutActionText.color=idle;}
        }
        void ActivateNextBlackoutPuzzle(int completed)
        {
            selectedMission=completed+1;blackoutSession=new BlackoutSession(SkyPulseBlackoutCampaign.Stages[selectedMission]);missionFinalized=false;
            BuildBlackoutLab();blackoutCompletionToast.text="SECTOR "+(completed+1).ToString("00")+" SECURED · NEXT SECTOR READY";blackoutToastRemaining=1.1f;
        }
        void BuildBlackoutResult(bool won,string reason,int medal)
        {
            BlackoutBackdrop(missionResultScreen.transform);
            MissionText(missionResultScreen.transform,won?"SECTOR SECURED":"SIGNAL LOST",835,52,won?BlackoutMint:Hex("#ff8595"));
            MissionText(missionResultScreen.transform,blackoutSession.Puzzle.Name,750,28,BlackoutIce);
            var core=BlackoutGraphic(missionResultScreen.transform,"Extracted data core",new Vector2(0,370),new Vector2(660,530),BlackoutSurface.Core);core.Celebrate=won;
            var report=BlackoutPanel(missionResultScreen.transform,"Extraction report",new Vector2(0,5),new Vector2(920,195));
            MissionText(report,won?MedalName(medal)+" EXTRACTION":reason,40,36,won?Hex("#ffce89"):Hex("#ff8595"),860,55);
            MissionText(report,won?"TRACE  "+blackoutSession.Trace+" / "+blackoutSession.Puzzle.TraceLimit: "RESTART TO RECOVER THE CORE",-29,24,BlackoutIce);
            if(reason.Contains("SAVE FAILED"))MissionText(missionResultScreen.transform,"SAVE FAILED · NEXT SECTOR REMAINS LOCKED",-125,22,Hex("#ff8595"));
            for(int i=0;i<8;i++)
            {
                var g=BlackoutGraphic(missionResultScreen.transform,"Sector progress "+i,new Vector2(-350+i*100,-225),new Vector2(88,92),BlackoutSurface.Node,false);
                g.NodeKind=blackoutCampaign.Data.medals[i]>0?6:i==selectedMission?5:1;
            }
            int current=selectedMission;
            BlackoutButton(missionResultScreen.transform,won&&current==7?"REPLAY CAMPAIGN":"RETRY SECTOR",0,-390,900,()=>LaunchMission(SkyPulseMode.BlackoutProtocol,won&&current==7?0:current),110);
            BlackoutButton(missionResultScreen.transform,"SECTOR MAP",0,-545,900,OpenBlackoutSectors);
            BlackoutButton(missionResultScreen.transform,"MAIN MENU",0,-690,900,ResetToMenu);
            MissionText(missionResultScreen.transform,won && current==7?"BLACKOUT CAMPAIGN COMPLETE":"A DEEPER NETWORK AWAITS",-840,22,BlackoutMint);
        }
    }
}
