using System;
using UnityEngine;

namespace SkyPulse.Mobile
{
    public interface IMissionStore { string Get(string key,string fallback); void Set(string key,string value); int GetInt(string key,int fallback); void SetInt(string key,int value); void Flush(); }
    public sealed class PlayerPrefsMissionStore : IMissionStore
    {
        public string Get(string key,string fallback)=>PlayerPrefs.GetString(key,fallback);
        public void Set(string key,string value)=>PlayerPrefs.SetString(key,value);
        public int GetInt(string key,int fallback)=>PlayerPrefs.GetInt(key,fallback);
        public void SetInt(string key,int value)=>PlayerPrefs.SetInt(key,value);
        public void Flush()=>PlayerPrefs.Save();
    }
    public enum DailyGameplayEvent { EndlessGate, RelayActivated, IntroBlackoutCompleted, CrystalPickup }
    [Serializable] public sealed class DailyMissionSave
    {
        public int version=1;
        public long day;
        public int[] progress=new int[4];
        public bool[] claimed=new bool[4];
        public int pending=-1, walletAfter;
    }
    // Journal before wallet write, replay absolute target, then clear journal.
    // Offline monotonic UTC periods prevent reopening old claimed days after a clock rollback.
    public sealed class SkyPulseDailyMissions
    {
        public const string SaveKey="skypulse.daily.v1", BackupKey="skypulse.daily.v1.backup", WalletKey="skypulse.native.crystals";
        public static readonly string[] Ids={"daily.endless.gates","daily.relay.activate","daily.blackout.intro","daily.flight.crystals"};
        public static readonly int[] Targets={10,3,1,10};
        public static readonly string[] Objectives={"Pass 10 Endless gates","Open 3 Relay gates","Clear Blackout sector 01","Collect 10 flight pickup crystals"};
        public static readonly string[] Modes={"ENDLESS ROUTE","PULSE RELAY","BLACKOUT PROTOCOL","ENDLESS ROUTE"};
        readonly IMissionStore store;
        readonly Func<DateTime> clock;
        public DailyMissionSave Data { get; private set; }
        public int ClaimedTotal { get { int total=0;for(int i=0;i<4;i++)if(Data.claimed[i])total+=5;return total; } }
        public bool ClockBehind => Day(clock())<Data.day;
        public DateTime ResetUtc => new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddDays(Data.day+1);
        public static long Day(DateTime now)=>(long)(now.ToUniversalTime()-new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalDays;
        public SkyPulseDailyMissions(IMissionStore store,Func<DateTime> clock)
        {
            this.store=store;this.clock=clock;
            Data=Read(store.Get(SaveKey,"")) ?? Read(store.Get(BackupKey,"")) ?? new DailyMissionSave{day=Day(clock())};
            EnsurePeriod();
        }
        static DailyMissionSave Read(string text)
        {
            if(string.IsNullOrEmpty(text))return null;
            try {
                var d=JsonUtility.FromJson<DailyMissionSave>(text);
                if(d==null || d.version!=1 || d.day<0 || d.progress==null || d.progress.Length!=4 || d.claimed==null || d.claimed.Length!=4 || d.pending < -1 || d.pending >3 || d.walletAfter<0)return null;
                for(int i=0;i<4;i++)d.progress[i]=Math.Max(0,Math.Min(Targets[i],d.progress[i]));
                return d;
            } catch { return null; }
        }
        void Persist()
        {
            string current=store.Get(SaveKey,"");
            // Keep only a completed backup; replaying an obsolete pending target could undo later gameplay credits.
            var previous=Read(current);
            if(previous!=null && previous.pending<0)store.Set(BackupKey,current);
            store.Set(SaveKey,JsonUtility.ToJson(Data));store.Flush();
        }
        public void RecoverPending()
        {
            if(Data.pending<0)return;
            // Persist intent again before applying when the previous Flush threw.
            Persist();
            store.SetInt(WalletKey,Data.walletAfter);store.Flush();
            int pending=Data.pending;Data.pending=-1;
            try { Persist();store.Set(BackupKey,JsonUtility.ToJson(Data));store.Flush(); } catch { Data.pending=pending;throw; }
        }
        public void EnsurePeriod()
        {
            RecoverPending();
            long day=Day(clock());
            if(day>Data.day) { Data=new DailyMissionSave{day=day};Persist(); }
        }
        public void Progress(DailyGameplayEvent kind,int amount=1)
        {
            EnsurePeriod();int index=(int)kind;
            if(amount<=0 || Data.claimed[index] || Data.progress[index]>=Targets[index])return;
            Data.progress[index]=Math.Min(Targets[index],Data.progress[index]+amount);Persist();
        }
        public bool Claim(int index)
        {
            EnsurePeriod();
            if(index<0 || index>=4 || Data.claimed[index] || Data.progress[index]<Targets[index])return false;
            int wallet=store.GetInt(WalletKey,0);
            if(wallet>int.MaxValue-5)throw new InvalidOperationException("Wallet cannot accept reward");
            Data.claimed[index]=true;Data.pending=index;Data.walletAfter=wallet+5;
            Persist();RecoverPending();return true;
        }
    }
    [Serializable] public sealed class ModeCompletionSave { public int version=1; public int[] medals=new int[4]; }
    public sealed class SkyPulseModeProgress
    {
        readonly IMissionStore store;
        public const string RelayKey="skypulse.relay.v1", BlackoutKey="skypulse.blackout.v1";
        public ModeCompletionSave Relay { get; private set; }
        public ModeCompletionSave Blackout { get; private set; }
        public SkyPulseModeProgress(IMissionStore store) { this.store=store;Relay=Load(RelayKey);Blackout=Load(BlackoutKey); }
        ModeCompletionSave Load(string key)
        {
            try { var s=JsonUtility.FromJson<ModeCompletionSave>(store.Get(key,""));if(s!=null && s.version==1 && s.medals!=null && (s.medals.Length==3 || s.medals.Length==4)){if(s.medals.Length==3)Array.Resize(ref s.medals,4);for(int i=0;i<4;i++)s.medals[i]=Math.Max(0,Math.Min(3,s.medals[i]));return s;} }catch{}
            return new ModeCompletionSave();
        }
        public void Complete(SkyPulseMode mode,int mission,int medal)
        {
            if(mode==SkyPulseMode.EndlessRoute || mission<0 || mission>=4 || medal<1)return;
            var save=mode==SkyPulseMode.PulseRelay?Relay:Blackout;
            save.medals[mission]=Math.Max(save.medals[mission],Math.Min(3,medal));
            store.Set(mode==SkyPulseMode.PulseRelay?RelayKey:BlackoutKey,JsonUtility.ToJson(save));store.Flush();
        }
    }
}
