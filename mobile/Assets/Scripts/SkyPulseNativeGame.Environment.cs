using UnityEngine;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        SkyPulseFlightEnvironment flightEnvironment;
        float sceneryTravel,sceneryAnchorX,sceneryEntry,sceneryHomeTravel;
        int scenerySequence=int.MinValue;
        void ResetFlightEnvironment()
        {
            sceneryTravel=sceneryEntry=0;scenerySequence=int.MinValue;
            if(flightEnvironment!=null)flightEnvironment.ResetView();
        }
        void UpdateFlightEnvironment(float dt)
        {
            bool home=homeScreen!=null&&homeScreen.activeInHierarchy&&homeProtocol==SkyPulseMode.EndlessRoute;
            bool show=home||activeMode==SkyPulseMode.EndlessRoute&&(state==FlightState.Playing||state==FlightState.Paused||state==FlightState.Impact||state==FlightState.GameOver);
            // The original illustrations remain available to the world guide
            // and material reflections, but never flatten the constructed view.
            backgroundRenderer.enabled=!show;incomingBackground.enabled=!show;backgroundVeil.enabled=!show;
            if(flightEnvironment==null)
            {
                var root=new GameObject("Pooled in-flight world architecture");root.transform.SetParent(transform,false);
                flightEnvironment=root.AddComponent<SkyPulseFlightEnvironment>();flightEnvironment.Build(GetViewportWidth());
            }
            if(flightEnvironment.gameObject.activeSelf!=show)flightEnvironment.gameObject.SetActive(show);
            if(!show||state==FlightState.Paused)return;
            if(home&&!reduceMotionEnabled)sceneryHomeTravel+=Mathf.Max(0,dt)*.45f;
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
            if(flightEnvironment.ReflectionWorld!=routeWorldIndex)flightEnvironment.SetReflection(routeWorldIndex,LoadSprite(world.BackgroundPath).texture);
            flightEnvironment.Present(routeWorldIndex,home?sceneryHomeTravel:sceneryTravel,cinematicClock,blend,world.Accent,world.Floor,reduceMotionEnabled,home||reduceMotionEnabled?1:Mathf.SmoothStep(0,1,sceneryEntry));
            flightEnvironment.SetViewport(GetViewportWidth());flightEnvironment.Render();
        }
    }
}
