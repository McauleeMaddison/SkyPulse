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
        readonly float[] crystalX,crystalY;
        public float CrystalX(int id)=>crystalX[id];
        public float CrystalY(int id)=>crystalY[id];
        public readonly float Spacing, ExitY;
        public readonly long Day;
        public readonly bool HasShield;
        public int RelayCount => RelayY.Length;
        public float RelayX(int index) => 12f + index * Spacing;
        public float ExitX => RelayX(RelayCount - 1) + 13f;
        public RelayMission(string id, string name, string briefing, float[] heights, float spacing, float exitY, bool shield, long day=-1,float[] crystalsX=null,float[] crystalsY=null)
        {
            Id=id;Name=name;Briefing=briefing;RelayY=heights;Spacing=spacing;ExitY=exitY;HasShield=shield;Day=day;
            crystalX=crystalsX??new float[heights.Length*2];crystalY=crystalsY??new float[heights.Length*2];
            if(crystalsX==null||crystalsY==null)
            {
                int seed=17;foreach(char c in id)seed=unchecked(seed*31+c);var random=new Random(seed);
                for(int gate=0;gate<heights.Length;gate++)for(int cell=0;cell<2;cell++)
                {int n=gate*2+cell;crystalX[n]=RelayX(gate)-(cell==0?6.2f+(float)random.NextDouble()*1.3f:2.9f+(float)random.NextDouble()*.8f);crystalY[n]=heights[gate]+((float)random.NextDouble()*2-1)*.85f;}
            }
        }
        public static readonly RelayMission[] All = {
            new RelayMission("relay.tutorial", "01 · FIRST CIRCUIT", "Tap the flight area to flap. Collect both crystals to open their numbered gate automatically. Fly through both gates and the green exit.", new[]{0f, .6f}, 18f, 0f, true),
            new RelayMission("relay.switchback", "02 · SWITCHBACK", "Collect both crystals for each of three locked gates. Crystal positions vary along each approach.", new[]{-1.1f, 1.1f, -.4f}, 20f, .4f, false),
            new RelayMission("relay.highwire", "03 · HIGHWIRE", "Collect all eight crystals to open four gates. Each gate requires its two matching crystals before you can fly through.", new[]{-1.5f, -.4f, .7f, 1.5f}, 21f, 0f, true),
            new RelayMission("relay.crossfeed", "04 · CROSSFEED", "Five alternating gates test your flight rhythm. Crystals open their matching gates automatically; follow the illuminated route to the exit.", new[]{1.5f, -1.5f, 1.5f, -.8f, 0f}, 22f, 0f, false)
        };
    }

    public sealed class RelaySession
    {
        public readonly RelayMission Mission;
        public MissionSessionState State { get; private set; } = MissionSessionState.Playing;
        public int Charges { get; private set; }
        public int NextRelay { get; private set; }
        public int Passed { get; private set; }
        public int UnlockedGates { get; private set; }
        public int LastUnlockedGate { get; private set; } = -1;
        public int Cells { get; private set; }
        public int HazardHits { get; private set; }
        public float Distance { get; private set; }
        public string Result { get; private set; } = "";
        readonly bool[] collected;
        readonly bool[] open;
        public const float ApertureHalfHeight=1.85f;
        public RelaySession(RelayMission mission) { Mission=mission ?? throw new ArgumentNullException(nameof(mission));open=new bool[mission.RelayCount];collected=new bool[mission.RelayCount*2]; }
        public void Pause(bool paused) { if(State==MissionSessionState.Playing && paused)State=MissionSessionState.Paused; else if(State==MissionSessionState.Paused && !paused)State=MissionSessionState.Playing; }
        public const int RequiredGateCrystals=2;
        public int GateCrystals(int gate)=>gate<0||gate>=open.Length?0:(collected[gate*2]?1:0)+(collected[gate*2+1]?1:0);
        public bool Collect(int cellId)
        {
            if(State!=MissionSessionState.Playing || cellId<0 || cellId>=Mission.RelayCount*2 || collected[cellId])return false;
            collected[cellId]=true;
            Charges++;Cells++;LastUnlockedGate=-1;
            int gate=cellId/2;
            if(!open[gate] && GateCrystals(gate)>=RequiredGateCrystals)
            {
                // Both assigned crystals are required. Charge cannot open another
                // gate, and logical passage never waits for the artwork.
                open[gate]=true;Charges-=RequiredGateCrystals;UnlockedGates++;LastUnlockedGate=gate;
                while(NextRelay<open.Length && open[NextRelay])NextRelay++;
            }
            return true;
        }
        public void Move(float distance)
        {
            if(State!=MissionSessionState.Playing)return;
            Distance+=Math.Max(0f,distance);
        }
        public void Advance(float distance,float birdY){Move(distance);ResolveCourse(birdY);}
        public void ResolveCourse(float birdY,bool checkAlignment=true)
        {
            if(State!=MissionSessionState.Playing)return;
            while(Passed<Mission.RelayCount && Distance>Mission.RelayX(Passed)+.85f)
            {
                if(!IsOpen(Passed)) { Fail("RELAY "+(Passed+1)+" NEEDS ITS CRYSTAL"); return; }
                if(checkAlignment && Math.Abs(birdY-Mission.RelayY[Passed])>ApertureHalfHeight) { Fail("MISSED RELAY "+(Passed+1)+" WINDOW"); return; }
                Passed++;
            }
            if(Distance>Mission.ExitX+.85f)
            {
                if(Passed!=Mission.RelayCount || checkAlignment && Math.Abs(birdY-Mission.ExitY)>ApertureHalfHeight) { Fail("MISSED EXIT"); return; }
                State=MissionSessionState.Succeeded; Result="CIRCUIT RESTORED";
            }
        }
        public bool IsOpen(int index) => index>=0 && index<open.Length && open[index];
        public void Hazard(bool protectedHit) { if(State!=MissionSessionState.Playing)return; if(protectedHit)HazardHits++; else Fail("UNPROTECTED ENERGY SHUTTER"); }
        public void Fail(string reason) { if(State!=MissionSessionState.Playing)return; State=MissionSessionState.Failed; Result=reason; }
        public int Medal => State!=MissionSessionState.Succeeded ? 0 : HazardHits==0 && Cells>=Mission.RelayCount+2 ? 3 : HazardHits==0 ? 2 : 1;
    }

}
