using System;
using UnityEngine;

namespace SkyPulse.Mobile
{
    // Authored local campaign. Build 9's four puzzles and medal key remain intact.
    public static class SkyPulseBlackoutCampaign
    {
        public static readonly BlackoutPuzzle[] Stages = {
            new BlackoutPuzzle("blackout.cipher.sector.01", "PACKET RECOVERY", "Recover two cores and reach the uplink.", new[]{"S....",".###.",".P.P.","....E"},13,0,8,true),
            new BlackoutPuzzle("blackout.cipher.sector.02", "GHOST CHANNEL", "Recover the ice cipher key before opening the uplink locks.", new[]{"S....",".#.#.","P#P#.",".#K#L","...LE"},18,0,13,true),
            new BlackoutPuzzle("blackout.cipher.sector.03", "FIREWALL FORK", "A keyed vault and breach shortcuts divide three cores.", new[]{"S.F.LP",".###.L","..P#..","KF.#..","P....E"},26,1,22,true),
            new BlackoutPuzzle("blackout.cipher.sector.04", "SPLIT HORIZON", "Switches exchange the mint and amber circuits. Route around inactive phase gates.", new[]{"S.CB.P",".##.#A","P.A.#.",".#B.C.",".#P##T","...TTE"},27,0,23,true),
            new BlackoutPuzzle("blackout.cipher.sector.05", "WATCHTOWER", "A cipher vault crosses both power phases. Recover its key and plan the return route.", new[]{"S.CB.LP",".##F##L","P.A#K..",".F.#B#.","..P#C.T","TTT.TTE"},28,2,25,true),
            new BlackoutPuzzle("blackout.cipher.sector.06", "CIPHER VAULT", "Four cores on two circuits. Switch phase again to recover the guarded lower core.", new[]{"S.CB.LP","T##A##L","P.K#B..","TF.#P#.","..BCAT.","T#P###T","TTTTTTE"},34,2,32,true),
            new BlackoutPuzzle("blackout.cipher.sector.07", "DARK WEAVE", "Two cipher keys, five cores and repeated phase changes. Keep an extraction route open.", new[]{"S.CB.LP",".##A##L","PTK#k.T",".FT#P#B","TACBTTT","T#P#l#l","TTP.TlE"},35,2,33,true),
            new BlackoutPuzzle("blackout.cipher.sector.08", "CORE EXTRACTION", "The deepest vault combines both keys, phase routing and costly security nodes.", new[]{"STCB.LP","T##A##L","PTK#P.B","TFT#k#T","TABCPAT","T#P#l#l","TTTTTlE"},37,3,36,true)
        };
        // Independently solved by a shortest-path search, also rechecked against the live session model.
        public static readonly int[] MinimumTrace = {7,12,21,22,24,31,32,35};
    }

    [Serializable] public sealed class BlackoutCampaignSave
    {
        public int version=2;
        public int[] medals=new int[8];
        public int[] bestTrace=new int[8];
    }
    public sealed class BlackoutCampaignProgress
    {
        public const string Key="skypulse.blackout-campaign.v2";
        readonly IMissionStore store;
        public BlackoutCampaignSave Data { get; private set; }
        public int Unlocked
        {
            get { int i=0;while(i<Data.medals.Length-1 && Data.medals[i]>0)i++;return i; }
        }
        public bool CanPlay(int index)=>index>=0 && index<SkyPulseBlackoutCampaign.Stages.Length && index<=Unlocked;
        public BlackoutCampaignProgress(IMissionStore store)
        {
            this.store=store;
            try { Data=JsonUtility.FromJson<BlackoutCampaignSave>(store.Get(Key,"")); }catch { Data=null; }
            if(Data==null || Data.version!=2 || Data.medals==null || Data.bestTrace==null || Data.medals.Length!=8 || Data.bestTrace.Length!=8)Data=new BlackoutCampaignSave();
            for(int i=0;i<8;i++){Data.medals[i]=Math.Max(0,Math.Min(3,Data.medals[i]));Data.bestTrace[i]=Math.Max(0,Data.bestTrace[i]);}
        }
        public void Complete(int index,int medal,int trace)
        {
            if(!CanPlay(index) || medal<1 || trace<0)return;
            // Publish in-memory unlocks only after the store has flushed successfully.
            var next=new BlackoutCampaignSave {medals=(int[])Data.medals.Clone(),bestTrace=(int[])Data.bestTrace.Clone()};
            next.medals[index]=Math.Max(next.medals[index],Math.Min(3,medal));
            next.bestTrace[index]=next.bestTrace[index]==0?trace:Math.Min(next.bestTrace[index],trace);
            store.Set(Key,JsonUtility.ToJson(next));store.Flush();Data=next;
        }
    }
}
