using System;
using UnityEngine;

namespace SkyPulse.Mobile
{
    [Serializable] public sealed class DailyRelaySave
    {
        public int version=1;
        public long day;
        public int[] medals=new int[4];
    }
    // UTC day determines a bounded flight course, not Unity's global random state.
    // Legacy static-circuit medals and every wallet key remain untouched.
    public sealed class SkyPulseDailyRelay
    {
        public const string SaveKey="skypulse.relay-daily.v1",BackupKey="skypulse.relay-daily.v1.backup",LastRunKey="skypulse.relay-daily.last-run.v1";
        readonly IMissionStore store;
        readonly Func<DateTime> clock;
        public DailyRelaySave Data { get; private set; }
        public RelayMission[] Missions { get; private set; }
        public DateTime ResetUtc=>new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddDays(Data.day+1);
        public SkyPulseDailyRelay(IMissionStore store,Func<DateTime> clock)
        {
            this.store=store;this.clock=clock;
            Data=Read(store.Get(SaveKey,""))??Read(store.Get(BackupKey,""))??new DailyRelaySave{day=SkyPulseDailyMissions.Day(clock())};
            Missions=Generate(Data.day);EnsurePeriod();
        }
        static DailyRelaySave Read(string json)
        {
            try{var s=JsonUtility.FromJson<DailyRelaySave>(json);if(s==null||s.version!=1||s.day<0||s.medals==null||s.medals.Length!=4)return null;for(int i=0;i<4;i++)s.medals[i]=Math.Max(0,Math.Min(3,s.medals[i]));return s;}catch{return null;}
        }
        void Persist(DailyRelaySave next)
        {
            var old=Read(store.Get(SaveKey,""));if(old!=null)store.Set(BackupKey,JsonUtility.ToJson(old));
            store.Set(SaveKey,JsonUtility.ToJson(next));store.Flush();Data=next;
        }
        public bool EnsurePeriod()
        {
            long day=SkyPulseDailyMissions.Day(clock());if(day<=Data.day)return false;
            var next=new DailyRelaySave{day=day};Persist(next);Missions=Generate(day);return true;
        }
        public void Complete(long day,int index,int medal)
        {
            if(index<0||index>=4||medal<1)return;
            // A run crossing midnight keeps its original course. Its last-run
            // medal is recorded without marking a different day's course clear.
            if(day!=Data.day)
            {
                var last=new DailyRelaySave{day=day};last.medals[index]=Math.Min(3,medal);
                string json=JsonUtility.ToJson(last);if(store.Get(LastRunKey,"")==json)return;
                store.Set(LastRunKey,json);store.Flush();return;
            }
            if(Data.medals[index]>=medal)return;
            var next=new DailyRelaySave{day=Data.day,medals=(int[])Data.medals.Clone()};next.medals[index]=Math.Min(3,medal);Persist(next);
        }
        static readonly string[] Names={"ION RUN","LUMEN TRAIL","AURORA LINK","PRISM DRIFT","STARLINE","ECHO ARC","NOVA CROSSING","SOLAR THREAD","ORBIT WEAVE","VECTOR LIGHT","HALO ROUTE","DAWN SIGNAL"};
        struct CourseRandom
        {
            uint seed;
            public CourseRandom(long day,int index){seed=unchecked((uint)day*2654435761u+(uint)(index+1)*2246822519u);if(seed==0)seed=1;}
            public float Next(){seed^=seed<<13;seed^=seed>>17;seed^=seed<<5;return (seed&0x00ffffffu)/16777216f;}
        }
        public static RelayMission[] Generate(long day)
        {
            var result=new RelayMission[4];var usedNames=new bool[Names.Length];
            for(int i=0;i<4;i++)
            {
                var random=new CourseRandom(day,i);var template=RelayMission.All[i];var heights=new float[template.RelayCount];float y=0;
                for(int gate=0;gate<heights.Length;gate++)
                {float target=random.Next()*3.3f-1.65f;y=Math.Max(y-1.9f,Math.Min(y+1.9f,target));heights[gate]=y;}
                float spacing=template.Spacing+random.Next()*1.8f,exit=random.Next()*1.4f-.7f;
                int nameIndex=(int)(random.Next()*Names.Length);while(usedNames[nameIndex])nameIndex=(nameIndex+1)%Names.Length;usedNames[nameIndex]=true;
                string name=(i+1).ToString("00")+" · "+Names[nameIndex];
                var placement=new CourseRandom(day,i+12);var crystalX=new float[heights.Length*2];var crystalY=new float[heights.Length*2];
                for(int gate=0;gate<heights.Length;gate++)for(int cell=0;cell<2;cell++)
                {
                    int id=gate*2+cell;float gateX=12f+gate*spacing;
                    crystalX[id]=gateX-(cell==0?6.2f+placement.Next()*1.3f:2.9f+placement.Next()*.8f);
                    crystalY[id]=heights[gate]+(placement.Next()*2-1)*.85f;
                }
                result[i]=new RelayMission("relay.daily."+day+"."+i,name,template.RelayCount+" GATES · "+(template.RelayCount*2)+" CRYSTALS\nCollect both randomly placed crystals for each numbered gate, then reach the green exit.",heights,spacing,exit,template.HasShield,day,crystalX,crystalY);
            }
            return result;
        }
    }
}
