using System;
using System.Collections.Generic;
using UnityEngine;
namespace SkyPulse.Mobile
{
    [Serializable] public sealed class SkyPulseBirdMastery {public string id;public int flight,relay;}
    [Serializable] public sealed class SkyPulseJourneySave
    {
        public int version=1,caseGoldMask,caseMedal;public long day,activeDay=-1;public int trace,turns;public int[] rotations;public List<SkyPulseBirdMastery> birds=new List<SkyPulseBirdMastery>();
    }
    // Cosmetic progression only: no currency, stat advantage or wallet mutation.
    // Best results and gold stamps are monotonic, so replays cannot farm rewards.
    public sealed class SkyPulsePlayerJourney
    {
        public const string Key="skypulse.player-journey.v1";
        readonly IMissionStore store;readonly Func<DateTime> clock;
        public SkyPulseJourneySave Data{get;private set;}
        public SkyPulsePlayerJourney(IMissionStore store,Func<DateTime> clock)
        {
            this.store=store;this.clock=clock;
            try{Data=JsonUtility.FromJson<SkyPulseJourneySave>(store.Get(Key,""));}catch{}
            if(Data==null||Data.version!=1||Data.birds==null||Data.birds.Count>256||Data.day<0)Data=new SkyPulseJourneySave{day=SkyPulseDailyMissions.Day(clock())};
            foreach(var bird in Data.birds)if(bird!=null){bird.flight=Math.Max(0,Math.Min(1000000,bird.flight));bird.relay=Math.Max(0,Math.Min(20,bird.relay));}
            Data.caseGoldMask&=15;Data.caseMedal=Math.Max(0,Math.Min(3,Data.caseMedal));Data.birds.RemoveAll(b=>b==null||string.IsNullOrEmpty(b.id)||b.id.Length>80);EnsureDay();
        }
        SkyPulseJourneySave Copy()=>JsonUtility.FromJson<SkyPulseJourneySave>(JsonUtility.ToJson(Data));
        void Publish(SkyPulseJourneySave next){store.Set(Key,JsonUtility.ToJson(next));store.Flush();Data=next;}
        public void EnsureDay(){long day=SkyPulseDailyMissions.Day(clock());if(day<=Data.day)return;var next=Copy();next.day=day;next.caseMedal=0;next.activeDay=-1;next.rotations=null;Publish(next);}
        SkyPulseBirdMastery Find(string id){for(int i=0;i<Data.birds.Count;i++)if(Data.birds[i].id==id)return Data.birds[i];return null;}
        public int FlightBest(string id){var bird=Find(id);return bird==null?0:bird.flight;}
        public int RelayBest(string id){var bird=Find(id);return bird==null?0:bird.relay;}
        public int BirdRank(string id)
        {int flight=FlightBest(id),relay=RelayBest(id);return flight>=30||relay>=5?3:flight>=15||relay>=4?2:flight>=5||relay>=2?1:0;}
        public int DisplayRank(string id)=>Data.caseGoldMask==15?4:BirdRank(id);
        public static string RankName(int rank)=>rank>=4?"ARCHIVIST":rank==3?"AURORA":rank==2?"PRISM":rank==1?"ION":"STANDARD";
        public void RecordFlight(string id,int score)=>Record(id,Math.Max(0,Math.Min(1000000,score)),0);
        public void RecordRelay(string id,int passed)=>Record(id,0,Math.Max(0,Math.Min(20,passed)));
        void Record(string id,int flight,int relay)
        {
            if(string.IsNullOrEmpty(id)||id.Length>80||flight<=FlightBest(id)&&relay<=RelayBest(id))return;
            var next=Copy();var bird=next.birds.Find(b=>b.id==id);if(bird==null){if(next.birds.Count>=256)return;bird=new SkyPulseBirdMastery{id=id};next.birds.Add(bird);}bird.flight=Math.Max(bird.flight,flight);bird.relay=Math.Max(bird.relay,relay);Publish(next);
        }
        public BlackoutSession StartCase(bool restart=false)
        {
            EnsureDay();var p=SkyPulseBlackoutCasebook.All[SkyPulseBlackoutCasebook.Index(Data.day)];
            bool resume=!restart&&Data.activeDay==Data.day&&Data.rotations!=null&&Data.rotations.Length==p.Count&&Data.trace>=0&&Data.trace<p.TraceLimit&&Data.turns>=0;
            return new BlackoutSession(p,resume?Data.rotations:null,resume?Data.trace:0,resume?Data.turns:0);
        }
        public void SaveCase(long day,BlackoutSession s)
        {
            if(day!=Data.day||s.Puzzle!=SkyPulseBlackoutCasebook.All[SkyPulseBlackoutCasebook.Index(day)])return;
            var next=Copy();next.activeDay=day;next.trace=s.Trace;next.turns=s.Turns;next.rotations=(int[])s.Rotations.Clone();Publish(next);
        }
        public void CompleteCase(long day,BlackoutSession s)
        {
            if(s.State!=MissionSessionState.Succeeded||s.Puzzle!=SkyPulseBlackoutCasebook.All[SkyPulseBlackoutCasebook.Index(day)])return;
            int bit=1<<SkyPulseBlackoutCasebook.Index(day);bool gold=s.Medal==3;int medal=day==Data.day?Math.Max(Data.caseMedal,s.Medal):Data.caseMedal;
            if((!gold||(Data.caseGoldMask&bit)!=0)&&medal==Data.caseMedal&&Data.activeDay<0)return;
            var next=Copy();if(gold)next.caseGoldMask|=bit;next.caseMedal=medal;if(next.activeDay==day){next.activeDay=-1;next.rotations=null;}Publish(next);
        }
    }
}
