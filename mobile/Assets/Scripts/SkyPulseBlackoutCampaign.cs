using System;
using UnityEngine;

namespace SkyPulse.Mobile
{
    public sealed class BlackoutPuzzle
    {
        public readonly string Id,Name,Briefing;
        public readonly string[] Rows;
        public readonly int[] Initial;
        public readonly int TraceLimit,Par,SourceDirection;
        public int Width=>Rows[0].Length;
        public int Height=>Rows.Length;
        public int Count=>Width*Height;
        public char Tile(int index)=>Rows[index/Width][index%Width];
        public int Start { get; private set; }
        public int CoreCount { get; private set; }
        public bool Rotatable(int tile)=>tile>=0&&tile<Count&&(Tile(tile)=='M'||Tile(tile)=='Y');
        public int Orientations(int tile)=>Tile(tile)=='Y'?4:2;
        public BlackoutPuzzle(string id,string name,string briefing,string[] rows,int[] initial,int budget,int par,int sourceDirection=0)
        {
            Id=id;Name=name;Briefing=briefing;Rows=rows;Initial=initial;TraceLimit=budget;Par=par;SourceDirection=sourceDirection;
            if(Count>49||initial.Length!=Count)throw new ArgumentException("Invalid signal board");
            Start=-1;for(int i=0;i<Count;i++){if(Tile(i)=='S')Start=i;if(Tile(i)=='C')CoreCount++;}
            if(Start<0||CoreCount==0)throw new ArgumentException("Missing source/core");
        }
    }
    public struct BlackoutBeam { public int From,To,Direction;public bool Blocked; }

    // The only shipping Blackout simulation. Rotation, live power and trace are
    // discrete; render rate, celebrations and animation callbacks never own rules.
    public sealed class BlackoutSession
    {
        public readonly BlackoutPuzzle Puzzle;
        public MissionSessionState State { get; private set; }=MissionSessionState.Playing;
        public readonly int[] Rotations;
        public readonly bool[] Powered;
        public readonly BlackoutBeam[] Beams=new BlackoutBeam[200];
        readonly bool[] visited=new bool[196];readonly int[] queueTile=new int[200],queueDirection=new int[200];
        public int BeamCount { get; private set; }
        public int Trace { get; private set; }
        public int Turns { get; private set; }
        public int CoresPowered { get; private set; }
        public int RelayMask { get; private set; }
        public int Alarms { get; private set; }
        public bool UplinkPowered { get; private set; }
        public int LastCost { get; private set; }
        public string Result { get; private set; }="";
        public int Remaining=>Math.Max(0,Puzzle.TraceLimit-Trace);
        public int Medal=>State!=MissionSessionState.Succeeded?0:Trace<=Puzzle.Par?3:Trace<=Puzzle.TraceLimit-2?2:1;
        public BlackoutSession(BlackoutPuzzle puzzle,int[] rotations=null,int trace=0,int turns=0)
        {
            Puzzle=puzzle??throw new ArgumentNullException(nameof(puzzle));Rotations=(int[])puzzle.Initial.Clone();Powered=new bool[puzzle.Count];
            if(rotations!=null&&rotations.Length==Rotations.Length)for(int i=0;i<rotations.Length;i++)if(puzzle.Rotatable(i))Rotations[i]=Math.Max(0,rotations[i])%puzzle.Orientations(i);
            Trace=Math.Max(0,trace);Turns=Math.Max(0,turns);Recalculate();Resolve();
        }
        public void Pause(bool pause){if(State==MissionSessionState.Playing&&pause)State=MissionSessionState.Paused;else if(State==MissionSessionState.Paused&&!pause)State=MissionSessionState.Playing;}
        public bool Rotate(int tile)
        {
            if(State!=MissionSessionState.Playing||!Puzzle.Rotatable(tile))return false;
            Rotations[tile]=(Rotations[tile]+1)%Puzzle.Orientations(tile);Turns++;Recalculate();LastCost=1+(Alarms>0?2:0);Trace+=LastCost;Resolve();return true;
        }
        // The same rules evaluate a speculative move in a reusable, separate
        // session. Preview cannot spend live trace, complete a vault or save.
        public bool PreviewRotation(int tile,BlackoutSession destination)
        {
            if(State!=MissionSessionState.Playing||!Puzzle.Rotatable(tile)||destination==null||ReferenceEquals(this,destination)||destination.Puzzle!=Puzzle)return false;
            Array.Copy(Rotations,destination.Rotations,Rotations.Length);
            destination.Trace=Trace;destination.Turns=Turns;destination.State=MissionSessionState.Playing;destination.Result="";destination.LastCost=0;
            return destination.Rotate(tile);
        }
        void Resolve()
        {
            if(Trace<=Puzzle.TraceLimit&&CoresPowered==Puzzle.CoreCount&&UplinkPowered){State=MissionSessionState.Succeeded;Result="VAULT EXTRACTED";}
            else if(Trace>=Puzzle.TraceLimit){State=MissionSessionState.Failed;Result="TRACE EXHAUSTED";}
        }
        public void Recalculate()
        {
            int mask=0;
            // Two relay groups can only add passable shutters. Resolve from a
            // dark board every turn so disconnected relays cannot latch open.
            for(int pass=0;pass<3;pass++)
            {
                Array.Clear(Powered,0,Powered.Length);Array.Clear(visited,0,visited.Length);BeamCount=CoresPowered=Alarms=0;UplinkPowered=false;
                int read=0,write=1,nextMask=mask;queueTile[0]=Puzzle.Start;queueDirection[0]=Puzzle.SourceDirection;visited[Puzzle.Start*4+Puzzle.SourceDirection]=true;Powered[Puzzle.Start]=true;
                while(read<write)
                {
                    int from=queueTile[read],d=queueDirection[read++],x=from%Puzzle.Width+(d==0?1:d==2?-1:0),y=from/Puzzle.Width+(d==3?1:d==1?-1:0);
                    if(x<0||x>=Puzzle.Width||y<0||y>=Puzzle.Height){AddBeam(from,-1,d,true);continue;}
                    int tile=y*Puzzle.Width+x;char c=Puzzle.Tile(tile);
                    bool blocked=c=='#'||c=='S'||c=='G'&&(mask&1)==0||c=='H'&&(mask&2)==0||c=='Y'&&Rotations[tile]!=d||c=='!';
                    AddBeam(from,tile,d,blocked);
                    if(blocked){if(c=='!'&&!Powered[tile]){Powered[tile]=true;Alarms++;}continue;}
                    Powered[tile]=true;
                    if(c=='E'){UplinkPowered=true;continue;}
                    if(c=='R')nextMask|=1;if(c=='Q')nextMask|=2;
                    if(c=='M')Enqueue(tile,d^(Rotations[tile]==0?1:3),ref write);
                    else if(c=='Y'){Enqueue(tile,d,ref write);Enqueue(tile,(d+1)%4,ref write);}
                    else Enqueue(tile,d,ref write);
                }
                for(int i=0;i<Puzzle.Count;i++)if(Puzzle.Tile(i)=='C'&&Powered[i])CoresPowered++;
                if(nextMask==mask){RelayMask=mask;return;}mask=nextMask;
            }
            RelayMask=mask;
        }
        void Enqueue(int tile,int direction,ref int count)
        {
            int key=tile*4+direction;if(visited[key])return;visited[key]=true;queueTile[count]=tile;queueDirection[count++]=direction;
        }
        void AddBeam(int from,int to,int direction,bool blocked){if(BeamCount<Beams.Length)Beams[BeamCount++]=new BlackoutBeam{From=from,To=to,Direction=direction,Blocked=blocked};}
    }
    public static class SkyPulseBlackoutCampaign
    {
        // Each optical vault restores one real part of the city's signal grid.
        // Existing stage identifiers, solutions, budgets and saves stay intact.
        public static readonly string[] Systems={"Dock beacons","Cargo link","Transit grid","Relay station","Power exchange","Skyway network","City shield","SkyPulse uplink"};
        public static string Objective(int stage)=>"Restore "+Systems[Math.Max(0,Math.Min(7,stage))];
        public static readonly BlackoutPuzzle[] Stages={
            new BlackoutPuzzle("heist.v1.01", "First Light", "Rotate the two prisms. Power the core and the uplink together.", new[]{"####","S.M#","##C#","##ME"}, new[]{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}, 8, 2, 0),
            new BlackoutPuzzle("heist.v1.02", "Silent Angles", "Keep both cores powered while guiding the beam to the uplink.", new[]{"#####","S..M#","#M.CE","#MCM!","#####"}, new[]{0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0}, 10, 4, 0),
            new BlackoutPuzzle("heist.v1.03", "Split Signal", "A splitter sends light forward and left. Match its input arrow to the arriving beam.", new[]{"#MM##","CMC##","S.YCE","#####","#####"}, new[]{0,1,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0}, 12, 6, 0),
            new BlackoutPuzzle("heist.v1.04", "Relay Vault", "The relay powers the cyan shutter. Feed it from a separate branch.", new[]{"M.M#R#","M...M!","##C###","S.YGCE","######"}, new[]{1,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0}, 13, 7, 0),
            new BlackoutPuzzle("heist.v1.05", "Ghost Engine", "Plan around red sensors. A lit sensor adds two trace to each rotation.", new[]{"M.M###","M...ME","#.C#.C","S.YGCM","#R##.#","#M..M!"}, new[]{1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,1,0,0,0,0,0,0,0,0,0,0,1,0}, 16, 10, 0),
            new BlackoutPuzzle("heist.v1.06", "Crossfeed", "Use two splitters to keep four cores and the uplink powered together.", new[]{"M.M####","M...ME#","#.C#.C#","S.YGCYC","#R##.##","#M..M!#"}, new[]{1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0}, 18, 12, 0),
            new BlackoutPuzzle("heist.v1.07", "Service Shaft", "A folded service shaft feeds the vault. Trace both branches before rotating.", new[]{"#M.Y###","#M...ME","##.C#.C","M..YGCM",".#R##.#",".#M..M!","MS#####"}, new[]{0,1,0,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,1,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0}, 20, 14, 2),
            new BlackoutPuzzle("heist.v1.08", "Double Lock", "Two independent relays control the shutters. Keep all three cores powered.", new[]{"#M.Y###","CM...ME","#Q.C#.H","MY.YGCM",".#R##.#",".#M..M!","MS#####"}, new[]{0,1,0,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,1,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0}, 22, 16, 2)
        };
        public static readonly int[] MinimumTrace={2,4,6,7,10,12,14,16};
    }
    [Serializable] public sealed class BlackoutCampaignSave
    {
        public int version=3;
        public int[] medals=new int[8],bestTrace=new int[8];
        public int active=-1,trace,turns;
        public int[] rotations;
    }
    // Data-only migration of the retired walker's medals. Its gameplay code is
    // removed. Old achievements unlock access but never mark new puzzles solved.
    [Serializable] sealed class PreviousBlackoutProgress { public int version;public int[] medals; }
    public sealed class BlackoutCampaignProgress
    {
        public const string Key="skypulse.blackout-signal-heist.v1";
        const string PreviousKey="skypulse.blackout-campaign.v2";
        readonly IMissionStore store;readonly int inheritedAccess;
        public BlackoutCampaignSave Data { get; private set; }
        public int Unlocked { get {int n=0;while(n<7&&Data.medals[n]>0)n++;return Math.Max(inheritedAccess,n);} }
        public bool CanPlay(int i)=>i>=0&&i<8&&i<=Unlocked;
        public BlackoutCampaignProgress(IMissionStore store)
        {
            this.store=store;int inherited=0;
            try {var old=JsonUtility.FromJson<PreviousBlackoutProgress>(store.Get(PreviousKey,""));if(old!=null&&old.version==2&&old.medals!=null&&old.medals.Length==8){while(inherited<7&&old.medals[inherited]>0)inherited++;}}catch{}
            inheritedAccess=inherited;
            try{Data=JsonUtility.FromJson<BlackoutCampaignSave>(store.Get(Key,""));}catch{}
            if(Data==null||Data.version!=3||Data.medals==null||Data.bestTrace==null||Data.medals.Length!=8||Data.bestTrace.Length!=8)Data=new BlackoutCampaignSave();
            for(int i=0;i<8;i++){Data.medals[i]=Math.Max(0,Math.Min(3,Data.medals[i]));Data.bestTrace[i]=Math.Max(0,Data.bestTrace[i]);}
        }
        public BlackoutSession Start(int i,bool restart=false)
        {
            if(!CanPlay(i))throw new InvalidOperationException("Vault is locked");var p=SkyPulseBlackoutCampaign.Stages[i];
            bool resume=!restart&&Data.active==i&&Data.rotations!=null&&Data.rotations.Length==p.Count&&Data.trace>=0&&Data.trace<=p.TraceLimit&&Data.turns>=0;
            var session=new BlackoutSession(p,resume?Data.rotations:null,resume?Data.trace:0,resume?Data.turns:0);
            return session.State==MissionSessionState.Failed?new BlackoutSession(p):session;
        }
        BlackoutCampaignSave Copy()=>new BlackoutCampaignSave{medals=(int[])Data.medals.Clone(),bestTrace=(int[])Data.bestTrace.Clone(),active=Data.active,trace=Data.trace,turns=Data.turns,rotations=Data.rotations};
        public void SaveAttempt(int index,BlackoutSession s)
        {
            if(!CanPlay(index)||s.Puzzle!=SkyPulseBlackoutCampaign.Stages[index])throw new InvalidOperationException("Wrong vault");
            var next=Copy();next.active=index;next.trace=s.Trace;next.turns=s.Turns;next.rotations=(int[])s.Rotations.Clone();Publish(next);
        }
        public void Complete(int index,int medal,int trace)
        {
            if(!CanPlay(index)||medal<1||medal>3||trace<0)return;
            var next=Copy();next.medals[index]=Math.Max(next.medals[index],medal);next.bestTrace[index]=next.bestTrace[index]==0?trace:Math.Min(next.bestTrace[index],trace);next.active=-1;next.rotations=null;Publish(next);
        }
        void Publish(BlackoutCampaignSave next){store.Set(Key,JsonUtility.ToJson(next));store.Flush();Data=next;}
    }
}
