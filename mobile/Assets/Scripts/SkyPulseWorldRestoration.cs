using UnityEngine;

namespace SkyPulse.Mobile
{
    // The eight existing campaign medals are the only source of truth. These
    // additive lighting rewards never remove the flight worlds' original light.
    public static class SkyPulseWorldRestoration
    {
        static readonly int Windows=Shader.PropertyToID("_RestoredWindows"),Beacons=Shader.PropertyToID("_RestoredBeacons"),Transit=Shader.PropertyToID("_RestoredTransit");
        public static float WindowPower(int mask)=>((mask&2)!=0?.18f:0)+((mask&16)!=0?.26f:0)+((mask&128)!=0?.20f:0);
        public static float BeaconPower(int mask)=>((mask&1)!=0?.20f:0)+((mask&8)!=0?.18f:0)+((mask&64)!=0?.20f:0);
        public static float TransitPower(int mask)=>((mask&4)!=0?.22f:0)+((mask&32)!=0?.28f:0);
        public static void Apply(MaterialPropertyBlock finish,int mask,float activePower=0,int activeSystem=-1)
        {
            // A live circuit previews the currently powered system on the
            // diorama only. Failed/unfinished circuits never persist a reward.
            int active=activeSystem>=0&&activeSystem<8?1<<activeSystem:0;
            if((mask&active)!=0)activePower=0;
            finish.SetFloat(Windows,WindowPower(mask)+WindowPower(active)*Mathf.Clamp01(activePower));
            finish.SetFloat(Beacons,BeaconPower(mask)+BeaconPower(active)*Mathf.Clamp01(activePower));
            finish.SetFloat(Transit,TransitPower(mask)+TransitPower(active)*Mathf.Clamp01(activePower));
        }
    }
}
