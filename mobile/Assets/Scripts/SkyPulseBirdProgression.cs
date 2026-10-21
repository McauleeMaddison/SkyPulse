using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyPulse.Mobile
{
    public enum BirdTrait { Collector, Salvager, Precision, Voyager, Recovery }
    [Serializable] public sealed class BirdGenome
    {
        public string id, appearance, parentA, parentB;
        public int primary, secondary, primaryGrade=1, secondaryGrade=1, level=1, generation;
    }
    [Serializable] public sealed class BirdProgressionSave
    {
        public int version=1, core=1, revision, sequence=1;
        public List<BirdGenome> birds=new List<BirdGenome>();
        public bool pending;
        public int walletAfter;
    }
    // A completed snapshot and an absolute wallet target form one replayable debit.
    // The daily-mission coordinator recovers this journal before other wallet writes.
    public sealed class SkyPulseBirdProgression
    {
        public const string SaveKey="skypulse.birds.v1", BackupKey="skypulse.birds.v1.backup";
        public const int MaxCore=5, MaxTraining=5;
        readonly IMissionStore store;
        readonly string[] founders;
        public BirdProgressionSave Data { get; private set; }
        public int TrainingCap => Math.Min(5,Data.core+1);
        public int BreedingCapacity => Data.core*4;
        public int OffspringCount { get { int n=0;foreach(var b in Data.birds)if(b.generation>0)n++;return n; } }
        public static readonly int[] CorePrices={400,900,1800,3200};
        public static readonly int[] TrainingPrices={120,280,600,1100};
        public static readonly string[] CoreNames={"ION","JADE","PRISM","SOLAR","NOVA"};
        public static readonly Color[] CoreColours={new Color(.36f,.88f,1),new Color(.35f,1,.70f),new Color(.70f,.48f,1),new Color(1,.72f,.30f),new Color(1,.54f,.80f)};
        public int CorePrice => Data.core<5?CorePrices[Data.core-1]:0;
        public SkyPulseBirdProgression(IMissionStore store,string[] founders)
        {
            this.store=store;this.founders=founders;
            Data=Read(store.Get(SaveKey,""))??Read(store.Get(BackupKey,""))??new BirdProgressionSave();
        }
        bool Founder(string id) { foreach(var f in founders)if(f==id)return true;return false; }
        public BirdGenome Genome(string id)
        {
            foreach(var b in Data.birds)if(b.id==id)return b;
            for(int i=0;i<founders.Length;i++)if(founders[i]==id)
                return new BirdGenome {id=id,appearance=id,primary=i%5,secondary=(i%5+i/5+1)%5};
            return null;
        }
        public static int Points(BirdGenome b,BirdTrait trait)
        {
            if(b==null)return 0;
            return (b.primary==(int)trait?b.primaryGrade+1+b.level:0)+(b.secondary==(int)trait?b.secondaryGrade+b.level-1:0);
        }
        public static string TraitText(BirdGenome b)
        {
            if(b==null)return "";
            return Effect((BirdTrait)b.primary,Points(b,(BirdTrait)b.primary))+"  ·  "+Effect((BirdTrait)b.secondary,Points(b,(BirdTrait)b.secondary));
        }
        public static string Effect(BirdTrait t,int points)
        {
            switch(t)
            {
                case BirdTrait.Collector:return "Collection +"+(points*2)+"%";
                case BirdTrait.Salvager:return "Salvage +"+(points*2)+"%";
                case BirdTrait.Precision:return "+"+points+" ✦ / 4 perfect gates";
                case BirdTrait.Voyager:return "+"+points+" ✦ / 10 gates";
                default:return "+"+(points*2)+" ✦ after 5 gates";
            }
        }
        public static int ResultBonus(BirdGenome b,int pickups,int gates,int perfect)
        {
            return Mathf.FloorToInt(Math.Max(0,pickups)*Points(b,BirdTrait.Salvager)*.02f+.0001f)
                +Math.Max(0,perfect)/4*Points(b,BirdTrait.Precision)
                +Math.Max(0,gates)/10*Points(b,BirdTrait.Voyager)
                +(gates>=5?Points(b,BirdTrait.Recovery)*2:0);
        }
        static BirdGenome Copy(BirdGenome b)=>JsonUtility.FromJson<BirdGenome>(JsonUtility.ToJson(b));
        BirdProgressionSave CopySave()=>JsonUtility.FromJson<BirdProgressionSave>(JsonUtility.ToJson(Data));
        BirdProgressionSave Read(string json)
        {
            if(string.IsNullOrEmpty(json))return null;
            try
            {
                var d=JsonUtility.FromJson<BirdProgressionSave>(json);
                if(d==null||d.version!=1||d.core<1||d.core>5||d.revision<0||d.sequence<1||d.birds==null||d.birds.Count>35||d.walletAfter<0)return null;
                var ids=new HashSet<string>();int offspring=0;
                foreach(var b in d.birds)
                {
                    if(b==null||string.IsNullOrEmpty(b.id)||!ids.Add(b.id)||!Founder(b.appearance)||b.level<1||b.level>5||b.primary<0||b.primary>4||b.secondary<0||b.secondary>4||b.primary==b.secondary||b.primaryGrade<1||b.primaryGrade>3||b.secondaryGrade<1||b.secondaryGrade>3||b.generation<0||b.generation>8)return null;
                    if(b.generation==0) {if(b.id!=b.appearance||!Founder(b.id))return null;}
                    else {if(!b.id.StartsWith("lineage.",StringComparison.Ordinal)||string.IsNullOrEmpty(b.parentA)||string.IsNullOrEmpty(b.parentB)||b.parentA==b.parentB)return null;offspring++;}
                }
                if(offspring>d.core*4)return null;
                foreach(var b in d.birds)if(b.generation>0)
                {
                    if(!int.TryParse(b.id.Substring(8),out int serial)||serial<=0||serial>=d.sequence)return null;
                    var a=d.birds.Find(p=>p.id==b.parentA);var other=d.birds.Find(p=>p.id==b.parentB);
                    if((a==null&&!Founder(b.parentA))||(other==null&&!Founder(b.parentB))||b.generation!=1+Math.Max(a?.generation??0,other?.generation??0))return null;
                }
                return d;
            }
            catch {return null;}
        }
        void Persist()
        {
            var current=store.Get(SaveKey,"");var before=Read(current);
            if(before!=null&&!before.pending)store.Set(BackupKey,current);
            store.Set(SaveKey,JsonUtility.ToJson(Data));store.Flush();
        }
        public void RecoverPending()
        {
            if(!Data.pending)return;
            Persist();store.SetInt(SkyPulseDailyMissions.WalletKey,Data.walletAfter);store.Flush();
            Data.pending=false;
            try {Persist();store.Set(BackupKey,JsonUtility.ToJson(Data));store.Flush();}
            catch {Data.pending=true;throw;}
        }
        bool Commit(BirdProgressionSave next,int cost,int expectedRevision)
        {
            RecoverPending();if(expectedRevision!=Data.revision||cost<=0||Data.revision==int.MaxValue)return false;
            int wallet=store.GetInt(SkyPulseDailyMissions.WalletKey,0);if(wallet<cost)return false;
            next.revision=Data.revision+1;next.pending=true;next.walletAfter=wallet-cost;Data=next;
            Persist();RecoverPending();return true;
        }
        public bool UpgradeCore(int revision)
        {
            RecoverPending();if(Data.core>=5||revision!=Data.revision)return false;
            var next=CopySave();next.core++;return Commit(next,CorePrice,revision);
        }
        public bool Train(string id,int revision,Func<string,bool> owned)
        {
            RecoverPending();var bird=Genome(id);
            if(revision!=Data.revision||bird==null||!owned(id)||bird.level>=TrainingCap)return false;
            var next=CopySave();var trained=next.birds.Find(b=>b.id==id);
            if(trained==null){trained=Copy(bird);next.birds.Add(trained);}trained.level++;
            return Commit(next,TrainingPrices[bird.level-1],revision);
        }
        public BirdGenome Preview(string first,string second)
        {
            var a=Genome(first);var b=Genome(second);if(a==null||b==null||first==second)return null;
            int secondary=b.secondary;if(secondary==a.primary)secondary=b.primary;
            if(secondary==a.primary)secondary=(a.primary+1)%5;
            int gradeCap=Data.core>=5?3:Data.core>=3?2:1;
            return new BirdGenome {id="lineage."+Data.sequence.ToString("0000"),appearance=a.appearance,parentA=first,parentB=second,
                primary=a.primary,secondary=secondary,primaryGrade=Math.Min(gradeCap,a.primaryGrade+(a.level>=3?1:0)),
                secondaryGrade=Math.Min(gradeCap,(secondary==b.primary?b.primaryGrade:b.secondaryGrade)+(b.level>=3?1:0)),
                generation=Math.Max(a.generation,b.generation)+1};
        }
        public static int BreedingPrice(BirdGenome b)=>b==null?0:300+(b.primaryGrade+b.secondaryGrade-2)*200;
        static bool SameGenome(BirdGenome a,BirdGenome b)=>a.appearance==b.appearance&&a.primary==b.primary&&a.secondary==b.secondary&&a.primaryGrade==b.primaryGrade&&a.secondaryGrade==b.secondaryGrade;
        public string BreedingBlock(string first,string second,Func<string,bool> owned)
        {
            var b=Preview(first,second);
            if(b==null)return "Choose two different parents";
            if(!owned(first)||!owned(second))return "Both parents must be owned";
            if(Data.core<2)return "Core level 2 unlocks breeding";
            if(OffspringCount>=BreedingCapacity)return Data.core<5?"Upgrade the Core for more lineage bays":"All 20 lineage bays are occupied";
            if(b.generation>8||Data.sequence==int.MaxValue)return "This lineage has reached its generation limit";
            foreach(var existing in Data.birds)if(existing.generation>0&&SameGenome(b,existing))return "This inherited combination is already in your Hangar";
            return "";
        }
        public bool Breed(string first,string second,int revision,Func<string,bool> owned)
        {
            RecoverPending();if(revision!=Data.revision||BreedingBlock(first,second,owned)!="")return false;
            var bird=Preview(first,second);var next=CopySave();next.sequence++;next.birds.Add(bird);
            return Commit(next,BreedingPrice(bird),revision);
        }
    }
}
