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
        readonly List<Vector2> blackoutTrail=new List<Vector2>();
        readonly List<int> blackoutEdgeA=new List<int>(),blackoutEdgeB=new List<int>();
        Vector2[] blackoutPositions;
        SkyPulseBlackoutGraphic blackoutNetwork,blackoutTraceGauge,blackoutHomeCore,blackoutHomeBackdrop;
        Coroutine blackoutEffectRoutine;
        bool blackoutBusy;
        int blackoutSelectedFirewall=-1;
        Text blackoutSectorText,blackoutActionText,blackoutPulseText;
        Text blackoutPhaseText,blackoutIceKeyText,blackoutVioletKeyText;
        Button blackoutBreachButton;
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
            else menuBestText.text=best.ToString();
        }
        void UpdateBlackoutPresentation()
        {
            for(int i=blackoutGraphics.Count-1;i>=0;i--)
            {
                var g=blackoutGraphics[i];if(g==null){blackoutGraphics.RemoveAt(i);continue;}
                g.ReducedMotion=reduceMotionEnabled;g.MotionPaused=state==FlightState.Paused;
            }
        }
        void ResetBlackoutEffects()
        {
            if(blackoutEffectRoutine!=null){StopCoroutine(blackoutEffectRoutine);blackoutEffectRoutine=null;}
            blackoutBusy=false;blackoutSelectedFirewall=-1;blackoutTrail.Clear();blackoutNodes.Clear();blackoutNodeButtons.Clear();blackoutNodeLabels.Clear();
            blackoutNetwork=null;blackoutTraceGauge=null;blackoutPositions=null;blackoutEdgeA.Clear();blackoutEdgeB.Clear();
            blackoutPhaseText=null;blackoutIceKeyText=null;blackoutVioletKeyText=null;
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
            ClearScreen(blackoutScreen);blackoutScreen.SetActive(true);BlackoutBackdrop(blackoutScreen.transform);
            blackoutNodes.Clear();blackoutNodeButtons.Clear();blackoutNodeLabels.Clear();blackoutSelectedFirewall=-1;
            var p=blackoutSession.Puzzle;
            var header=BlackoutPanel(blackoutScreen.transform,"Blackout command deck",new Vector2(0,849),new Vector2(970,225));
            MissionText(header,"BLACKOUT",54,51,BlackoutIce);
            MissionText(header,"P R O T O C O L",-5,21,BlackoutMint);
            blackoutSectorText=MissionText(header,"SECTOR "+(selectedMission+1).ToString("00")+" / 08  ·  "+p.Name,-65,24,Hex("#94b9c9"),910,40);
            blackoutDataText=ProtocolStat(blackoutScreen.transform,"CORES", "0",-320,665,300,2,BlackoutMint);
            blackoutTraceText=ProtocolStat(blackoutScreen.transform,"TRACE", "0",0,665,300,2,BlackoutIce);
            blackoutHackText=ProtocolStat(blackoutScreen.transform,"BREACHES", "0",320,665,300,2,Hex("#ffc478"));
            blackoutTraceGauge=BlackoutGraphic(blackoutScreen.transform,"Living trace rail",new Vector2(0,574),new Vector2(918,10),BlackoutSurface.Trace);
            if(p.HasPhaseRouting || p.CipherKeyMask>0)
            {
                var systems=BlackoutPanel(blackoutScreen.transform,"Cipher and phase telemetry",new Vector2(0,526),new Vector2(930,54));
                blackoutPhaseText=MissionText(systems,"",0,19,BlackoutMint,306,42);blackoutPhaseText.rectTransform.anchoredPosition=new Vector2(-308,0);
                blackoutIceKeyText=MissionText(systems,"",0,19,BlackoutIce,306,42);
                blackoutVioletKeyText=MissionText(systems,"",0,19,Hex("#bb89ff"),306,42);blackoutVioletKeyText.rectTransform.anchoredPosition=new Vector2(308,0);
            }
            float stride=Mathf.Min(148,952f/p.Width),vertical=128;float centre=20,top=centre+(p.Height-1)*vertical*.5f;
            var deck=BlackoutPanel(blackoutScreen.transform,"Floating lattice deck",new Vector2(0,centre),new Vector2(p.Width*stride+28,p.Height*vertical+50));
            blackoutNetwork=BlackoutGraphic(blackoutScreen.transform,"Live data connections",Vector2.zero,new Vector2(1080,1600),BlackoutSurface.Network);
            var from=new List<Vector2>();var to=new List<Vector2>();blackoutPositions=new Vector2[p.Width*p.Height];
            blackoutEdgeA.Clear();blackoutEdgeB.Clear();
            for(int i=0;i<blackoutPositions.Length;i++)blackoutPositions[i]=new Vector2((i%p.Width-(p.Width-1)*.5f)*stride,top-(i/p.Width)*vertical);
            for(int tile=0;tile<blackoutPositions.Length;tile++)
            {
                if(p.Tile(tile)=='#')continue;
                foreach(int next in new[]{tile+1,tile+p.Width})if(next<blackoutPositions.Length && p.Tile(next)!='#' && (next==tile+p.Width || next/p.Width==tile/p.Width)){from.Add(blackoutPositions[tile]);to.Add(blackoutPositions[next]);blackoutEdgeA.Add(tile);blackoutEdgeB.Add(next);}
            }
            blackoutNetwork.EdgeFrom=from.ToArray();blackoutNetwork.EdgeTo=to.ToArray();
            blackoutNetwork.EdgePowered=new bool[from.Count];blackoutNetwork.HasPhaseRouting=p.HasPhaseRouting;
            for(int i=0;i<blackoutPositions.Length;i++)
            {
                int tile=i;
                var hit=CreatePanel(blackoutScreen.transform,"Terminal node "+i,blackoutPositions[i],new Vector2(stride,vertical),Color.clear);
                var g=BlackoutGraphic(hit,"Reactive lattice chip",Vector2.zero,new Vector2(stride-10,vertical-10),BlackoutSurface.Node);
                var label=CreateText(hit,"",new Vector2(0,-40),new Vector2(stride-12,25),16,BlackoutMint,TextAnchor.MiddleCenter,FontStyle.Bold);label.raycastTarget=false;
                var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=hit.GetComponent<Image>();button.transition=Selectable.Transition.None;
                hit.gameObject.AddComponent<SkyPulseButtonFeedback>();button.onClick.AddListener(()=>TouchBlackoutNode(tile));
                blackoutNodes.Add(g);blackoutNodeButtons.Add(button);blackoutNodeLabels.Add(label);
            }
            blackoutStatusText=MissionText(blackoutScreen.transform,"",-470,28,BlackoutIce,920,54);
            blackoutPulseText=MissionText(blackoutScreen.transform,"",-525,20,BlackoutMint,940,45);blackoutPulseText.raycastTarget=false;
            blackoutActionText=MissionText(blackoutScreen.transform,"",-525,21,Hex("#8eacba"),920,45);
            blackoutBreachButton=BlackoutButton(blackoutScreen.transform,"BREACH",-235,-635,430,BreachBlackoutFirewall);
            hackButton=blackoutBreachButton;
            BlackoutButton(blackoutScreen.transform,"PAUSE",235,-635,430,PauseFlight);
            MissionText(blackoutScreen.transform,"GOLD TARGET  ≤ "+p.Par+" TRACE",-750,22,Hex("#ffca84"));
            if(p.PunishMistakes)MissionText(blackoutScreen.transform,"BLOCKED MOVE  +1 TRACE   ·   FAILED BREACH  1 CHARGE +3 TRACE",-805,17,Hex("#a0bac9"),920,32);
            BlackoutButton(blackoutScreen.transform,"RESTART SECTOR",0,-885,910,()=>LaunchMission(SkyPulseMode.BlackoutProtocol,selectedMission),84);
            blackoutTrail.Clear();blackoutTrail.Add(blackoutPositions[blackoutSession.Cursor]);RefreshBlackoutLab();
        }
        void RefreshBlackoutLab()
        {
            if(blackoutSession==null || blackoutNetwork==null)return;
            var s=blackoutSession;var p=s.Puzzle;bool playing=state==FlightState.Playing && s.State==MissionSessionState.Playing;
            for(int i=0;i<blackoutNodes.Count;i++)
            {
                char kind=p.Tile(i);bool recovered=s.Recovered.Contains(i),opened=s.Opened.Contains(i),cursor=s.Cursor==i;
                var g=blackoutNodes[i];g.NodeKind=cursor?5:kind=='#'?1:kind=='P'?(recovered?6:2):kind=='F'?(opened?7:3):kind=='E'?4:kind=='T'?8:0;
                if(!cursor && "CABKkLl".IndexOf(kind)>=0)g.NodeKind=kind=='C'?9:kind=='A'?10:kind=='B'?11:kind=='K'?(recovered?6:12):kind=='k'?(recovered?6:13):kind=='L'?14:15;
                g.Powered=s.Powered(i);g.CircuitPhase=s.CircuitPhase;
                g.Adjacent=s.Adjacent(i) && (kind!='#'||p.PunishMistakes);g.Selected=i==blackoutSelectedFirewall;g.Animate=cursor || kind=='P'&&!recovered || kind=='E' || kind=='T' || kind=='C' || "ABKkLl".IndexOf(kind)>=0;g.SetVerticesDirty();
                blackoutNodeLabels[i].text=cursor?"YOU":kind=='P'?(recovered?"SECURED":"CORE"):kind=='F'?(opened?"OPEN":"BREACH"):kind=='E'?"UPLINK":kind=='T'?"+2 TRACE":kind=='C'?"SWITCH":kind=='A'?(g.Powered?"MINT · ON":"MINT · OFF"):kind=='B'?(g.Powered?"AMBER · ON":"AMBER · OFF"):kind=='K'?(recovered?"KEY SAVED":"ICE KEY"):kind=='k'?(recovered?"KEY SAVED":"VIOLET KEY"):kind=='L'||kind=='l'?(g.Powered?"UNLOCKED":"LOCKED"):"";
                blackoutNodeLabels[i].color=!g.Powered?Hex("#8f9aab"):kind=='T'?Hex("#ff8595"):kind=='k'||kind=='l'?Hex("#c6a1ff"):kind=='K'||kind=='L'?BlackoutIce:kind=='B'||kind=='F'&&!opened?Hex("#ffd08f"):BlackoutMint;
                blackoutNodeButtons[i].interactable=playing && !blackoutBusy && g.Adjacent;
            }
            blackoutDataText.text=s.Packets+" / "+s.RequiredPackets;blackoutTraceText.text=s.Trace+" / "+p.TraceLimit;blackoutHackText.text=s.Charges.ToString();
            blackoutTraceGauge.TraceFraction=s.Trace/(float)p.TraceLimit;blackoutTraceGauge.SetVerticesDirty();
            blackoutNetwork.Trail=blackoutTrail.ToArray();blackoutNetwork.SetVerticesDirty();
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
            bool firewall=blackoutSelectedFirewall>=0 && p.Tile(blackoutSelectedFirewall)=='F'&&!s.Opened.Contains(blackoutSelectedFirewall);
            blackoutActionText.text=blackoutSelectedFirewall>=0?(firewall?"FIREWALL SELECTED · 1 BREACH / 3 TRACE":"LOCK SELECTED · ONLY FIREWALLS CAN BE BREACHED"):spare<=2?"TRACE CRITICAL · "+spare+" REMAINING":"TRACE RESERVE  "+spare+"   /   NETWORK LIVE";
            blackoutActionText.color=spare<=2?Hex("#ff8595"):Hex("#8eacba");
            var actionColour=blackoutActionText.color;actionColour.a=blackoutBusy&&!string.IsNullOrEmpty(blackoutPulseText.text)?0:1;blackoutActionText.color=actionColour;
            blackoutBreachButton.interactable=playing && !blackoutBusy && blackoutSelectedFirewall>=0 && s.Charges>0;
            blackoutBreachButton.GetComponentInChildren<Text>().text=blackoutSelectedFirewall>=0&&!firewall?"ATTEMPT BREACH":"BREACH";
        }
        void TouchBlackoutNode(int tile)
        {
            if(activeMode!=SkyPulseMode.BlackoutProtocol || state!=FlightState.Playing || blackoutBusy || blackoutSession==null || blackoutSession.State!=MissionSessionState.Playing || !blackoutSession.Adjacent(tile))return;
            if(blackoutSession.Puzzle.Tile(tile)=='F' && !blackoutSession.Opened.Contains(tile))
            {blackoutSelectedFirewall=tile;RefreshBlackoutLab();Play(scoreSound);return;}
            AnimateBlackoutAction(tile,false);
        }
        void BreachBlackoutFirewall()
        {
            if(blackoutSelectedFirewall>=0)AnimateBlackoutAction(blackoutSelectedFirewall,true);
        }
        void AnimateBlackoutAction(int tile,bool breach)
        {
            if(blackoutBusy || state!=FlightState.Playing || activeMode!=SkyPulseMode.BlackoutProtocol || blackoutSession==null)return;
            int previous=blackoutSession.Cursor,cores=blackoutSession.Packets,keys=blackoutSession.CipherKeys,phase=blackoutSession.CircuitPhase,charges=blackoutSession.Charges;
            if(!blackoutSession.Act(tile,breach))return;
            bool rejected=blackoutSession.LastActionFailed;
            blackoutBusy=true;blackoutSelectedFirewall=rejected?tile:-1;
            if(!breach&&!rejected){blackoutTrail.Add(blackoutPositions[tile]);if(blackoutTrail.Count>18)blackoutTrail.RemoveAt(0);}
            blackoutNetwork.Origin=blackoutPositions[previous];blackoutNetwork.Target=blackoutPositions[tile];blackoutNetwork.Travel=breach||rejected?1:0;blackoutNetwork.Impact=1;blackoutNetwork.Selected=breach;
            bool switched=phase!=blackoutSession.CircuitPhase,keyRecovered=keys!=blackoutSession.CipherKeys;
            blackoutNetwork.ImpactKind=rejected?2:breach?1:blackoutSession.Puzzle.Tile(tile)=='T'?2:blackoutSession.Packets>cores?3:switched?4:keyRecovered?(blackoutSession.Puzzle.Tile(tile)=='k'?6:5):0;
            blackoutPulseText.text=rejected?blackoutSession.LastFault+" · "+(charges>blackoutSession.Charges?"-1 BREACH / ":"")+"+"+blackoutSession.LastActionCost+" TRACE":breach?"FIREWALL BREACHED":blackoutSession.Packets>cores?"CORE SECURED":switched?(blackoutSession.CircuitPhase==0?"MINT":"AMBER")+" CIRCUIT ONLINE":keyRecovered?(blackoutSession.Puzzle.Tile(tile)=='k'?"VIOLET":"ICE")+" CIPHER KEY SECURED":blackoutSession.Puzzle.Tile(tile)=='T'?"SECURITY PULSE · +2 TRACE":"";
            blackoutPulseText.color=blackoutNetwork.ImpactKind==2?Hex("#ff8595"):breach||switched&&blackoutSession.CircuitPhase==1?Hex("#ffd08f"):blackoutNetwork.ImpactKind==6?Hex("#c6a1ff"):BlackoutIce;
            Play(rejected?crashSound:blackoutSession.Packets>cores||keyRecovered?unlockSound:scoreSound);RefreshBlackoutLab();
            blackoutEffectRoutine=StartCoroutine(BlackoutActionEffect(blackoutSession,breach||rejected));
        }
        IEnumerator BlackoutActionEffect(BlackoutSession session,bool breach)
        {
            float elapsed=0;float duration=session.State==MissionSessionState.Succeeded?.8f:breach||blackoutNetwork.ImpactKind==4?.44f:.24f;
            while(elapsed<duration && session==blackoutSession)
            {
                if(state!=FlightState.Paused)
                {
                    elapsed=reduceMotionEnabled?duration:elapsed+Time.unscaledDeltaTime;
                    blackoutNetwork.Travel=breach?1:Mathf.Clamp01(elapsed/.24f);blackoutNetwork.Impact=1-Mathf.Clamp01(elapsed/duration);blackoutNetwork.SetVerticesDirty();
                    var pulse=blackoutPulseText.color;pulse.a=blackoutNetwork.Impact;blackoutPulseText.color=pulse;
                }
                yield return null;
            }
            if(session!=blackoutSession)yield break;
            blackoutEffectRoutine=null;blackoutBusy=false;blackoutNetwork.Travel=1;blackoutNetwork.Impact=0;blackoutPulseText.text="";
            if(session.State==MissionSessionState.Succeeded || session.State==MissionSessionState.Failed)FinalizeMission();else RefreshBlackoutLab();
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
            bool next=won && selectedMission<7 && blackoutCampaign.CanPlay(selectedMission+1);
            int current=selectedMission;
            BlackoutButton(missionResultScreen.transform,next?"NEXT SECTOR  "+(current+2).ToString("00"):"RETRY SECTOR",0,-390,900,()=>LaunchMission(SkyPulseMode.BlackoutProtocol,next?current+1:current),110);
            BlackoutButton(missionResultScreen.transform,"SECTOR MAP",0,-545,900,OpenBlackoutSectors);
            BlackoutButton(missionResultScreen.transform,"HOME",0,-690,900,ResetToMenu);
            MissionText(missionResultScreen.transform,won && current==7?"BLACKOUT CAMPAIGN COMPLETE":"A DEEPER NETWORK AWAITS",-840,22,BlackoutMint);
        }
    }
}
