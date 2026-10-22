using UnityEngine;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        SkyPulseFlightEnvironment flightEnvironment;
        float sceneryTravel,sceneryAnchorX,sceneryEntry;
        int scenerySequence=int.MinValue;
        void ResetFlightEnvironment()
        {
            sceneryTravel=sceneryEntry=0;scenerySequence=int.MinValue;
            if(flightEnvironment!=null)flightEnvironment.ResetView();
        }
        void UpdateFlightEnvironment(float dt)
        {
            bool show=activeMode==SkyPulseMode.EndlessRoute&&(state==FlightState.Playing||state==FlightState.Paused||state==FlightState.Impact||state==FlightState.GameOver);
            if(flightEnvironment==null)
            {
                var root=new GameObject("Pooled in-flight world architecture");root.transform.SetParent(transform,false);
                flightEnvironment=root.AddComponent<SkyPulseFlightEnvironment>();flightEnvironment.Build(GetViewportWidth());
            }
            if(flightEnvironment.gameObject.activeSelf!=show)flightEnvironment.gameObject.SetActive(show);
            if(!show||state==FlightState.Paused)return;
            if(state==FlightState.Playing)
            {
                var anchor=pipePool[0];
                // Observe actual pipe movement: slow fields and hit-stop retain
                // their existing timing. A recycled anchor cannot jump scenery.
                if(anchor!=null)
                {
                    if(scenerySequence==anchor.Sequence)sceneryTravel+=Mathf.Max(0,sceneryAnchorX-anchor.X);
                    scenerySequence=anchor.Sequence;sceneryAnchorX=anchor.X;
                }
                sceneryEntry=Mathf.Min(1,sceneryEntry+dt*2);
            }
            float blend=worldTransitionTimer>0?Mathf.SmoothStep(0,1,1-worldTransitionTimer/WorldTransitionSeconds):1;
            var world=routeWorld??equippedWorld;
            flightEnvironment.Present(routeWorldIndex,sceneryTravel,cinematicClock,blend,world.Accent,world.Floor,reduceMotionEnabled,reduceMotionEnabled?1:Mathf.SmoothStep(0,1,sceneryEntry));
        }
    }
}
