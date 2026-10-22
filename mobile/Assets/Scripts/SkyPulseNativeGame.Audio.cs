using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        // Six prewarmed voices: no per-tap objects or unbounded PlayOneShot stack.
        // Voice zero is reserved for impact/unlock, so taps cannot mask a result.
        const int EffectVoiceCount = 6;
        const float EffectGain = .45f;
        readonly AudioSource[] effectVoices = new AudioSource[EffectVoiceCount];
        readonly AudioClip[] effectClips = new AudioClip[7];
        readonly double[] nextEffectAt = new double[7];
        AudioClip prismSound, sensorSound;
        Text soundText;
        bool soundEnabled = true;
        uint effectSequence;

        void CreateGameAudio()
        {
            // Retain this control source for existing mute/volume conventions.
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0;
            audioSource.mute = !soundEnabled;
            flapSound = Resources.Load<AudioClip>("SkyPulse/audio/flap");
            scoreSound = Resources.Load<AudioClip>("SkyPulse/audio/score");
            crystalSound = Resources.Load<AudioClip>("SkyPulse/audio/crystal");
            crashSound = Resources.Load<AudioClip>("SkyPulse/audio/crash");
            unlockSound = Resources.Load<AudioClip>("SkyPulse/audio/unlock");
            prismSound = LoadAudioCue("prism",scoreSound);
            sensorSound = LoadAudioCue("sensor",crashSound);
            effectClips[0]=flapSound; effectClips[1]=scoreSound; effectClips[2]=crystalSound;
            effectClips[3]=crashSound; effectClips[4]=unlockSound;
            effectClips[5]=prismSound; effectClips[6]=sensorSound;
            relayPassAudio=gameObject.AddComponent<AudioSource>();
            relayPassAudio.playOnAwake=false;relayPassAudio.spatialBlend=0;relayPassAudio.dopplerLevel=0;relayPassAudio.pitch=1.14f;
            foreach (var clip in effectClips) if (clip != null) clip.LoadAudioData();
            for (int i=0; i<effectVoices.Length; i++)
            {
                var voice = gameObject.AddComponent<AudioSource>();
                voice.playOnAwake=false; voice.spatialBlend=0; voice.dopplerLevel=0;
                voice.priority=i==0?64:128;
                effectVoices[i]=voice;
            }
        }

        static AudioClip LoadAudioCue(string id,AudioClip fallback)=>Resources.Load<AudioClip>("SkyPulse/audio/"+id)??fallback;

        private void Play(AudioClip clip)
        {
            if (clip==null || audioSource==null || audioSource.mute || !soundEnabled) return;
            int cue=-1;
            for (int i=0; i<effectClips.Length; i++) if (effectClips[i]==clip) { cue=i; break; }
            double now=AudioSettings.dspTime;
            if (cue>=0 && now<nextEffectAt[cue]) return;
            bool result=clip==crashSound || clip==unlockSound;
            for (int i=result?0:1; i<effectVoices.Length; i++)
            {
                var voice=effectVoices[i];
                if (voice==null || voice.isPlaying) continue;
                // Small deterministic timbre movement never consumes gameplay RNG.
                float variation=(effectSequence++ % 3 - 1f)*.012f;
                voice.pitch=clip==flapSound || clip==prismSound?1f+variation:1f;
                voice.volume=EffectGain*audioSource.volume;
                voice.mute=false; voice.panStereo=0; voice.clip=clip; voice.Play();
                if (cue>=0) nextEffectAt[cue]=now+.035;
                return;
            }
            // Saturated low-priority taps stay silent rather than hard-cutting a
            // ringing voice or raising the mix above its fixed headroom budget.
        }

        void StopGameSounds()
        {
            foreach (var voice in effectVoices) if (voice!=null) voice.Stop();
            if (relayPassAudio!=null) relayPassAudio.Stop();
            for (int i=0; i<nextEffectAt.Length; i++) nextEffectAt[i]=0;
        }

        void ToggleSound()
        {
            soundEnabled=!soundEnabled;
            audioSource.mute=!soundEnabled;
            StopGameSounds();
            UpdateComfortCopy();
            SaveProgress();
        }
    }
}
