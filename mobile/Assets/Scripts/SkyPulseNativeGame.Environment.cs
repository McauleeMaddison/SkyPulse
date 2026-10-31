using UnityEngine;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        SkyPulseFlightEnvironment flightEnvironment;
        SkyPulseRelayEnvironment relayHomeEnvironment;
        float sceneryTravel,sceneryAnchorX,sceneryEntry,sceneryHomeTravel;
        int scenerySequence=int.MinValue;
        void ResetFlightEnvironment()
        {
            // Home already shows this city. Keep it solid when entering or
            // retrying flight instead of flashing a dithered construction reveal.
            sceneryTravel=0;sceneryEntry=1;scenerySequence=int.MinValue;
            if(flightEnvironment!=null)flightEnvironment.ResetView();
        }
        void UpdateFlightEnvironment(float dt)
        {
            bool relayHome=homeScreen!=null&&homeScreen.activeInHierarchy&&homeProtocol==SkyPulseMode.PulseRelay;
            // The Canvas underlay must not cover the live perspective station.
            if(homeProtocolBackdrop!=null)homeProtocolBackdrop.enabled=!relayHome;
            if(relayHome&&relayHomeEnvironment==null)
            {
                var station=new GameObject("Home orbital station depth");station.transform.SetParent(transform,false);relayHomeEnvironment=station.AddComponent<SkyPulseRelayEnvironment>();relayHomeEnvironment.Build(GetViewportWidth(),LoadSprite(RelayStationPath).texture);
            }
            if(relayHomeEnvironment!=null)
            {
                relayHomeEnvironment.gameObject.SetActive(relayHome);
                if(relayHome){relayHomeEnvironment.SetViewport(GetViewportWidth());relayHomeEnvironment.SetVisual(0,reduceMotionEnabled?0:cinematicClock);}
            }
            bool home=homeScreen!=null&&homeScreen.activeInHierarchy&&homeProtocol==SkyPulseMode.EndlessRoute;
            bool show=home||activeMode==SkyPulseMode.EndlessRoute&&(state==FlightState.Playing||state==FlightState.Paused||state==FlightState.Impact||state==FlightState.GameOver);
            // The original illustrations remain available to the world guide
            // and material reflections, but never flatten the constructed view.
            bool modeHome=homeScreen!=null&&homeScreen.activeInHierarchy&&homeProtocol!=SkyPulseMode.EndlessRoute;
            bool legacyWorld=!show&&activeMode==SkyPulseMode.EndlessRoute&&!modeHome;
            backgroundRenderer.enabled=legacyWorld;incomingBackground.enabled=legacyWorld&&worldTransitionTimer>0;backgroundVeil.enabled=legacyWorld;
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
