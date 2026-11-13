using System;
namespace SkyPulse.Mobile
{
    // Authored optional cases, not random unsolved boards. Each has an audited
    // trace budget and a different route. UTC chooses the case; no timer or fee.
    public static class SkyPulseBlackoutCasebook
    {
        static BlackoutPuzzle Case(string id,string name,string briefing,string[] rows,int budget,int par,params int[] changed)
        {var initial=new int[rows.Length*rows[0].Length];for(int i=0;i<changed.Length;i+=2)initial[changed[i]]=changed[i+1];return new BlackoutPuzzle(id,name,briefing,rows,initial,budget,par);}
        public static readonly BlackoutPuzzle[] All={
            Case("case.cargo","Cargo Switch","Bring both cargo cores online through the upper transfer route.",new[]{"##MCM","#!.#.","S.M#C","E...M"},9,4,2,1,12,1,19,1),
            Case("case.return","Return Current","Fold the signal back through both cores before reaching the uplink.",new[]{"S...M","#MCE.","!.##C","#M..M"},9,4,6,1,16,0,19,1),
            Case("case.split","Split Horizon","Aim the splitter along the incoming beam. Keep both branches powered.",new[]{"##MCM","#!.#C","S.YCE","#####"},10,4,2,1,12,2),
            Case("case.skyway","Skyway Exchange","Power the relay before the shutter. Keep three cores live across both routes.",new[]{"##M.CM","#M.C.E","S.YRGM","#.#!#.","#M.C.M"},15,7,2,1,7,1,14,2,25,0,29,1)
        };
        public static int Index(long utcDay)=>(int)(Math.Max(0,utcDay)%All.Length);
    }
}
