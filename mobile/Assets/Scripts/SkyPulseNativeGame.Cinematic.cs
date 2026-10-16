using UnityEngine;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        SkyPulseBirdRig cinematicBird;
        SkyPulseFlightEffects cinematicFlight;
        SkyPulseRelayEnvironment relayEnvironment;
        AudioSource relayPassAudio;
        RelaySession cinematicSession;
        Sprite[] cinematicPreparedFrames;
        float cinematicClock,cinematicLastWing=100;
        int cinematicPassed;
        bool cinematicFlying;

        // Late presentation observes the final physics/pose of the rendered frame.
        // No protected Endless simulation or input methods need modification.
        void LateUpdate(){UpdateCinematicPresentation(Mathf.Min(Time.unscaledDeltaTime,MaximumSimulationCatchup));}
        void UpdateCinematicPresentation(float dt)
        {
            if(birdRenderer==null||equippedSkin==null)return;
            if(cinematicBird==null){cinematicBird=birdRenderer.gameObject.AddComponent<SkyPulseBirdRig>();cinematicBird.Bind(birdRenderer);}
            if(cinematicPreparedFrames!=flapFrameBirdSprites){cinematicPreparedFrames=flapFrameBirdSprites;cinematicBird.PrepareFrames(cinematicPreparedFrames);}
            if(cinematicFlight==null){cinematicFlight=new GameObject("Pilot engine and passage effects").AddComponent<SkyPulseFlightEffects>();cinematicFlight.transform.SetParent(transform,false);cinematicFlight.Build();}
            bool playing=state==FlightState.Playing && activeMode!=SkyPulseMode.BlackoutProtocol;
            bool paused=state==FlightState.Paused && cinematicFlying;
            cinematicFlight.gameObject.SetActive(playing||paused);
            if(paused)return;
            if(!reduceMotionEnabled&&(playing||state==FlightState.Menu))cinematicClock+=dt;
            var light=activeMode==SkyPulseMode.PulseRelay?new Color(.30f,.85f,1):equippedSkin.Accent;
            cinematicBird.Present(reduceMotionEnabled?0:cinematicClock,Mathf.Exp(-wingTimer*12),birdVelocity/15,reduceMotionEnabled,light);
            if(!playing){if(cinematicFlying)cinematicFlight.ResetPresentation();cinematicFlying=false;cinematicLastWing=100;return;}
            if(!cinematicFlying||cinematicSession!=relaySession){cinematicFlight.ResetPresentation();cinematicSession=relaySession;cinematicPassed=0;cinematicLastWing=100;}
            cinematicFlying=true;
            if(wingTimer<cinematicLastWing)cinematicFlight.Flap(bird.position,equippedSkin.Trail,reduceMotionEnabled);
            cinematicLastWing=wingTimer;
            if(relaySession!=null)
            {
                for(int i=cinematicPassed;i<relaySession.Passed;i++)
                {
                    var at=new Vector3(BirdX+relaySession.Mission.RelayX(i)-relaySession.Distance,relaySession.Mission.RelayY[i],0);
                    cinematicFlight.Passage(at,new Color(.4f,1,.85f),reduceMotionEnabled);PlayRelayPass(at);
                }
                cinematicPassed=relaySession.Passed;
            }
            cinematicFlight.Tick(dt,bird.position,birdVelocity/15,equippedSkin.Trail,reduceMotionEnabled);
        }
        void PlayRelayPass(Vector3 at)
        {
            if(scoreSound==null||audioSource==null)return;
            if(relayPassAudio==null){relayPassAudio=gameObject.AddComponent<AudioSource>();relayPassAudio.playOnAwake=false;relayPassAudio.spatialBlend=0;relayPassAudio.pitch=1.14f;}
            relayPassAudio.volume=audioSource.volume*.55f;relayPassAudio.mute=audioSource.mute;
            // Controlled stereo positioning is stable with the orthographic camera;
            // distance attenuation must not make high/low gates inaudible.
            relayPassAudio.panStereo=Mathf.Clamp(at.x/(GetViewportWidth()*.5f),-.5f,.5f);relayPassAudio.PlayOneShot(scoreSound);
        }
        void UpdateCinematicHome()
        {
            if(state!=FlightState.Menu||menuHeroTransform==null)return;
            if(homeProtocol==SkyPulseMode.PulseRelay)
            {
                float t=reduceMotionEnabled?0:ambientTime;
                menuHeroTransform.localScale=Vector3.one*.54f;
                menuHeroTransform.anchoredPosition=new Vector2(-105+Mathf.Sin(t*.8f)*3,355+Mathf.Sin(t*1.4f)*5);
                homeRelayPortal.rectTransform.localRotation=Quaternion.Euler(0,Mathf.Sin(t*.35f)*3,0);
                homeRelayCrystal.rectTransform.localRotation=Quaternion.Euler(0,Mathf.Sin(t*.7f)*12,Mathf.Sin(t*.8f)*4);
            }
        }
    }
}
