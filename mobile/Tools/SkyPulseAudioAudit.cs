// Copy into Assets/Editor in an isolated validation project only.
using System;
using UnityEditor;
using UnityEngine;
public static class SkyPulseAudioAudit
{
    public static void Run()
    {
        SkyPulseAuditValidation.Run();
        string[] names = { "flap", "score", "crystal", "crash", "unlock" };
        int[] frames = { 2205, 3969, 2866, 7056, 6394 };
        for (int i = 0; i < names.Length; ++i)
        {
            var clip = Resources.Load<AudioClip>("SkyPulse/audio/" + names[i]);
            if (clip == null || clip.channels != 1 || Mathf.Abs(clip.length - frames[i] / 22050f) > .002f)
                throw new Exception("Audio resource/format/duration mismatch: " + names[i]);
            if (!clip.LoadAudioData()) throw new Exception("Audio load failed: " + names[i]);
            var samples = new float[clip.samples * clip.channels];
            if (!clip.GetData(samples, 0)) throw new Exception("Cannot decode: " + names[i]);
            float peak = 0f;
            foreach (float value in samples)
            {
                if (float.IsNaN(value) || float.IsInfinity(value)) throw new Exception("Invalid audio samples");
                peak = Mathf.Max(peak, Mathf.Abs(value));
            }
            if (peak < .01f || peak > 1.001f) throw new Exception("Silent/invalid decoded audio: " + names[i]);
            Debug.Log("SKYPULSE_AUDIO_PASS: " + names[i] + " duration=" + clip.length + " peak=" + peak);
        }
        Debug.Log("SKYPULSE_AUTHORED_AUDIO_IMPORT_PASS");
    }
}
