using UnityEngine;

namespace SkyPulse.Mobile
{
    // One persisted medal per system. Eight spatially distinct light groups,
    // derived from that save, are shared by flight worlds and the city overlook.
    public static class SkyPulseWorldRestoration
    {
        static readonly int GroupsA=Shader.PropertyToID("_RestoredA"),GroupsB=Shader.PropertyToID("_RestoredB");
        static readonly string[] Benefits={"Dock approach lights","Cargo district windows","Transit direction markers","Relay tower beacons","Power-grid window banks","Skybridge edge lights","Cliff shield markers","Rooftop uplink LEDs"};
        public static string Benefit(int system)=>Benefits[Mathf.Clamp(system,0,7)];
        public static void Apply(MaterialPropertyBlock finish,int mask,float activePower=0,int activeSystem=-1)
        {
            // PLAN is never supplied here. Only actual core/uplink state can
            // preview an unfinished system, and only a saved medal persists it.
            var a=Vector4.zero;var b=Vector4.zero;
            for(int i=0;i<8;i++)
            {
                float power=(mask&(1<<i))!=0?1:i==activeSystem?Mathf.Clamp01(activePower):0;
                if(i<4)a[i]=power;else b[i-4]=power;
            }
            finish.SetVector(GroupsA,a);finish.SetVector(GroupsB,b);
        }
    }
}
