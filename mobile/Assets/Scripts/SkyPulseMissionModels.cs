using System;
using System.Collections.Generic;

namespace SkyPulse.Mobile
{
    public enum SkyPulseMode { EndlessRoute, PulseRelay, BlackoutProtocol }
    public enum MissionSessionState { Selection, Playing, Paused, Succeeded, Failed }
    public enum WalletRewardSource { FlightPickup, FlightTechBonus, DailyMission }

    public sealed class RelayMission
    {
        public readonly string Id, Name, Briefing;
        public readonly float[] RelayY;
        public readonly float Spacing, ExitY;
        public readonly bool HasShield;
        public int RelayCount => RelayY.Length;
        public float RelayX(int index) => 12f + index * Spacing;
        public float ExitX => RelayX(RelayCount - 1) + 13f;
        public RelayMission(string id, string name, string briefing, float[] heights, float spacing, float exitY, bool shield)
        { Id=id; Name=name; Briefing=briefing; RelayY=heights; Spacing=spacing; ExitY=exitY; HasShield=shield; }
        public static readonly RelayMission[] All = {
            new RelayMission("relay.tutorial", "01 · FIRST CIRCUIT", "Tap to fly. Gold cells supply Pulse charges. Align with the cyan window, then press PULSE while it is in range. Open both numbered shutters and fly through the exit.", new[]{0f, .8f}, 18f, 0f, true),
            new RelayMission("relay.switchback", "02 · SWITCHBACK", "Route between three alternating relay windows. The lower cell is a safe refill; the high gold cell is optional. Activate each relay in order.", new[]{-1.5f, 1.5f, -.5f}, 19f, .5f, false),
            new RelayMission("relay.highwire", "03 · HIGHWIRE", "Four relays form an ascending circuit. Collect optional cells for a higher medal, then descend to the final exit. One Aegis pickup is available.", new[]{-2f, -.5f, 1f, 2f}, 20f, 0f, true),
            new RelayMission("relay.crossfeed", "04 · CROSSFEED", "Five alternating channels need a complete pulse circuit. Climb and descend between windows, choose optional high charge cells, then centre on the exit.", new[]{2f, -2f, 2f, -1f, 0f}, 22f, 0f, false)
        };
    }

    public sealed class RelaySession
    {
        public readonly RelayMission Mission;
        public MissionSessionState State { get; private set; } = MissionSessionState.Playing;
        public int Charges { get; private set; }
        public int NextRelay { get; private set; }
        public int Passed { get; private set; }
        public int Cells { get; private set; }
        public int HazardHits { get; private set; }
        public float Distance { get; private set; }
        public string Result { get; private set; } = "";
        readonly HashSet<int> collected = new HashSet<int>();
        public RelaySession(RelayMission mission) { Mission=mission ?? throw new ArgumentNullException(nameof(mission)); }
        public void Pause(bool paused) { if(State==MissionSessionState.Playing && paused)State=MissionSessionState.Paused; else if(State==MissionSessionState.Paused && !paused)State=MissionSessionState.Playing; }
        public bool Collect(int cellId) { if(State!=MissionSessionState.Playing || !collected.Add(cellId))return false; Charges++; Cells++; return true; }
        public bool Pulse(float birdY)
        {
            if(State!=MissionSessionState.Playing || NextRelay>=Mission.RelayCount || Charges<=0)return false;
            float ahead=Mission.RelayX(NextRelay)-Distance;
            if(ahead<.6f || ahead>6f || Math.Abs(birdY-Mission.RelayY[NextRelay])>1.55f)return false;
            Charges--; NextRelay++; return true;
        }
        public void Advance(float distance, float birdY)
        {
            if(State!=MissionSessionState.Playing)return;
            Distance+=Math.Max(0f,distance);
            while(Passed<Mission.RelayCount && Distance>Mission.RelayX(Passed)+.85f)
            {
                if(NextRelay<=Passed) { Fail("RELAY "+(Passed+1)+" NOT ACTIVATED"); return; }
                if(Math.Abs(birdY-Mission.RelayY[Passed])>1.65f) { Fail("MISSED RELAY "+(Passed+1)+" WINDOW"); return; }
                Passed++;
            }
            if(Distance>Mission.ExitX+.85f)
            {
                if(Passed!=Mission.RelayCount || Math.Abs(birdY-Mission.ExitY)>1.65f) { Fail("MISSED EXIT"); return; }
                State=MissionSessionState.Succeeded; Result="CIRCUIT RESTORED";
            }
        }
        public bool IsOpen(int index) => index<NextRelay;
        public void Hazard(bool protectedHit) { if(State!=MissionSessionState.Playing)return; if(protectedHit)HazardHits++; else Fail("UNPROTECTED ENERGY SHUTTER"); }
        public void Fail(string reason) { if(State!=MissionSessionState.Playing)return; State=MissionSessionState.Failed; Result=reason; }
        public int Medal => State!=MissionSessionState.Succeeded ? 0 : HazardHits==0 && Cells>=Mission.RelayCount+2 ? 3 : HazardHits==0 ? 2 : 1;
    }

    public sealed class BlackoutPuzzle
    {
        public readonly string Id, Name, Briefing;
        public readonly string[] Rows;
        public readonly int TraceLimit, HackCharges, Par;
        public int Width => Rows[0].Length;
        public int Height => Rows.Length;
        public char Tile(int index) => Rows[index/Width][index%Width];
        public int Start => string.Join("",Rows).IndexOf('S');
        public BlackoutPuzzle(string id,string name,string briefing,string[] rows,int budget,int hacks,int par)
        { Id=id;Name=name;Briefing=briefing;Rows=rows;TraceLimit=budget;HackCharges=hacks;Par=par; }
        public static readonly BlackoutPuzzle[] All = {
            new BlackoutPuzzle("blackout.tutorial","01 · PACKET RECOVERY","Tap an adjacent connected tile. Recover both DATA packets, then enter EXIT. Every move adds 1 TRACE. Walls cannot be crossed. Stay within 18 TRACE.",new[]{"S..#..",".#P#..","...#P.",".#....",".....E"},18,0,12),
            new BlackoutPuzzle("blackout.firewall","02 · FIREWALL FORK","Every move adds 1 TRACE. Arm HACK, then tap an adjacent firewall: opening it costs 1 charge and 3 TRACE. Tap the opened tile again to move. Recover both packets. A longer safe route also exists.",new[]{"S.F.P.",".#.##.","...F..","P#.#..",".....E"},23,2,18),
            new BlackoutPuzzle("blackout.bypass","03 · QUIET BYPASS","Recover three packets, then exit within 27 TRACE. Two charges can open shortcuts, but each hack adds 3 TRACE without moving. Plan before you spend.",new[]{"S..#P..",".#F#.#.","P..F...",".#.#F#.","...P..E"},27,2,22),
            new BlackoutPuzzle("blackout.darkweave","04 · DARK WEAVE","Three scattered packets share a six-row network. A single hack can shorten the route, but each move still costs trace. Recover all data within 31 TRACE.",new[]{"S..F..P",".##.#..","P...#..",".#F...#","..P.#..","......E"},31,1,25)
        };
    }

    public sealed class BlackoutSession
    {
        public readonly BlackoutPuzzle Puzzle;
        public MissionSessionState State { get; private set; } = MissionSessionState.Playing;
        public int Cursor { get; private set; }
        public int Trace { get; private set; }
        public int Charges { get; private set; }
        public int Packets { get; private set; }
        public int RequiredPackets { get; private set; }
        public string Result { get; private set; } = "";
        public readonly HashSet<int> Recovered = new HashSet<int>();
        public readonly HashSet<int> Opened = new HashSet<int>();
        public BlackoutSession(BlackoutPuzzle puzzle)
        {
            Puzzle=puzzle ?? throw new ArgumentNullException(nameof(puzzle)); Cursor=puzzle.Start; Charges=puzzle.HackCharges;
            for(int i=0;i<puzzle.Width*puzzle.Height;i++)if(puzzle.Tile(i)=='P')RequiredPackets++;
        }
        public void Pause(bool paused) { if(State==MissionSessionState.Playing && paused)State=MissionSessionState.Paused; else if(State==MissionSessionState.Paused && !paused)State=MissionSessionState.Playing; }
        public bool Adjacent(int tile) => tile>=0 && tile<Puzzle.Width*Puzzle.Height && Math.Abs(tile%Puzzle.Width-Cursor%Puzzle.Width)+Math.Abs(tile/Puzzle.Width-Cursor/Puzzle.Width)==1;
        public bool Act(int tile,bool hack)
        {
            if(State!=MissionSessionState.Playing || !Adjacent(tile))return false;
            char kind=Puzzle.Tile(tile);
            if(kind=='#')return false;
            if(kind=='F' && !Opened.Contains(tile))
            {
                if(!hack || Charges<=0)return false;
                Charges--; Trace+=3; Opened.Add(tile);
            }
            else
            {
                if(hack)return false;
                Cursor=tile; Trace++;
                if(kind=='P' && Recovered.Add(tile))Packets++;
            }
            if(Trace>Puzzle.TraceLimit) { State=MissionSessionState.Failed;Result="TRACE LIMIT EXCEEDED"; }
            else if(Cursor==tile && kind=='E' && Packets==RequiredPackets) { State=MissionSessionState.Succeeded;Result="DATA SECURED"; }
            return true;
        }
        public int Medal => State!=MissionSessionState.Succeeded ? 0 : Trace<=Puzzle.Par ? 3 : Trace<=Puzzle.TraceLimit-2 ? 2 : 1;
    }
}
