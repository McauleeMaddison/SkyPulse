using UnityEngine;

namespace SkyPulse.Mobile
{
    /// <summary>Articulated mechanical bird with separate showcase and accepted-tap flight samplers.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class SkyPulseNeonFinchPrototype : MonoBehaviour
    {
        [Header("Editable mechanical joints")]
        public Transform body, head, leftWing, rightWing, leftWingTip, rightWingTip, tail, tailTip;
        [Header("Art review motion")]
        public bool animate = true, turntable = true, reducedMotion;
        [Range(.5f, 4f)] public float flapPeriod = 1.65f;
        [Range(0, 48)] public float flapDegrees = 28f;
        [Range(0, 12)] public float bankDegrees = 5f;
        [Range(0, 30)] public float turntableDegreesPerSecond = 12f;
        [Header("Volumetric blue exhaust")]
        public Renderer jetRenderer;
        [SerializeField, HideInInspector] Transform[] joints;
        [SerializeField, HideInInspector] Quaternion[] bindRotations;
        [SerializeField, HideInInspector] Vector3[] bindPositions;
        [SerializeField, HideInInspector] Quaternion bindRootRotation = Quaternion.identity;
        [SerializeField, HideInInspector] Vector3 bindRootPosition;
        bool ready;
        MaterialPropertyBlock jetProperties;
        static readonly int JetTimeId = Shader.PropertyToID("_JetTime");
        static readonly int JetStrengthId = Shader.PropertyToID("_JetStrength");
        static readonly int JetMotionId = Shader.PropertyToID("_JetMotion");

        public void CaptureBindPose()
        {
            joints = new[] { body, head, leftWing, rightWing, leftWingTip, rightWingTip, tail, tailTip };
            bindRotations = new Quaternion[joints.Length]; bindPositions = new Vector3[joints.Length];
            for (int i = 0; i < joints.Length; i++) if (joints[i] != null)
            { bindRotations[i] = joints[i].localRotation; bindPositions[i] = joints[i].localPosition; }
            bindRootRotation = transform.localRotation; bindRootPosition = transform.localPosition; ready = true;
        }
        void OnEnable()
        {
            ready = joints != null && joints.Length == 8 && bindRotations != null && bindRotations.Length == 8;
            SampleJet(0);
        }
        void Update() { if (animate) SamplePose(Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartupAsDouble); }
        public void ResetPose()
        {
            if (!ready) CaptureBindPose();
            transform.localRotation = bindRootRotation; transform.localPosition = bindRootPosition;
            for (int i = 0; i < joints.Length; i++) if (joints[i] != null)
            { joints[i].localRotation = bindRotations[i]; joints[i].localPosition = bindPositions[i]; }
            SampleJet(0);
        }
        public void SamplePose(float time)
        {
            if (!ready) CaptureBindPose();
            // Absolute time makes the same pose reproducible at any frame rate.
            float phase = time * (Mathf.PI * 2f / Mathf.Max(.1f, flapPeriod));
            float amount = reducedMotion ? 0f : 1f;
            transform.localRotation = bindRootRotation * Quaternion.Euler(0, turntable && !reducedMotion ? time * turntableDegreesPerSecond : 0, 0);
            transform.localPosition = bindRootPosition;
            Set(0, 0, 0, Mathf.Sin(phase * .31f) * bankDegrees * amount);
            Set(1, 0, Mathf.Sin(phase * .23f) * 2f * amount, -Mathf.Sin(phase * .5f) * 1.5f * amount);
            SampleWings(phase, amount);
            Set(6, Mathf.Sin(phase * .5f) * 3f * amount, Mathf.Sin(phase * .41f) * 4f * amount, Mathf.Sin(phase - .6f) * 4f * amount);
            Set(7, 0, Mathf.Sin(phase * .41f - .4f) * 5f * amount, Mathf.Sin(phase - .9f) * 5f * amount);
            SampleJet(time);
        }

        /// <summary>
        /// The one-argument sampler provides deterministic exhibition poses. Reduced Motion removes
        /// ambient banking, head/tail motion and jet shimmer, while retaining a
        /// restrained wing stroke as essential feedback that the bird is flying.
        /// The Studio sampler and saved bind pose retain their existing behaviour.
        /// </summary>
        public void SampleFlightPose(float activeSeconds)
        {
            bool studioTurntable = turntable;
            turntable = false;
            SamplePose(activeSeconds);
            turntable = studioTurntable;
            if (reducedMotion)
                SampleWings(activeSeconds * (Mathf.PI * 2f / Mathf.Max(.1f, flapPeriod)), .60f);
        }
        // Native input supplies a normalised lift, rather than restarting an
        // autonomous exhibition cycle. The bind pose is the settled glide.
        public void SampleFlightPose(float activeSeconds, float wingLift)
        {
            SampleFlightPose(activeSeconds);
            float lift = Mathf.Clamp01(wingLift);
            float amount = reducedMotion ? .60f : 1f;
            float stroke = Mathf.SmoothStep(0f, 1f, lift) * amount;
            Set(2, stroke * flapDegrees, stroke * 3f, -stroke * 14f);
            Set(3, -stroke * flapDegrees, -stroke * 3f, -stroke * 14f);
            Set(4, stroke * 13f, 0f, stroke * 4f);
            Set(5, -stroke * 13f, 0f, stroke * 4f);
        }
        // A committed flap travels above AND below glide before settling. This
        // sampler accepts a signed pose; controls still own every stroke event.
        public void SampleFlightStroke(float activeSeconds,float signedStroke)
        {
            SampleFlightPose(activeSeconds);
            float stroke=Mathf.Clamp(signedStroke,-1f,1f)*(reducedMotion?.60f:1f);
            Set(2,stroke*flapDegrees,stroke*3f,-stroke*14f);
            Set(3,-stroke*flapDegrees,-stroke*3f,-stroke*14f);
            Set(4,stroke*13f,0,stroke*4f);Set(5,-stroke*13f,0,stroke*4f);
        }
        public static float TapStroke(float lift,bool downstroke)
        {
            lift=Mathf.Clamp01(lift);
            if(!downstroke)return Mathf.SmoothStep(0,1,lift);
            float t=1-lift;
            return t<=.66f?Mathf.Lerp(1,-1,Mathf.SmoothStep(0,1,t/.66f)):
                Mathf.Lerp(-1,0,Mathf.SmoothStep(0,1,(t-.66f)/.34f));
        }
        void SampleWings(float phase, float amount)
        {
            float stroke = Mathf.Sin(phase), follow = Mathf.Sin(phase - .53f);
            Set(2, stroke * flapDegrees * amount, Mathf.Sin(phase) * 3f * amount, 0);
            Set(3, -stroke * flapDegrees * amount, -Mathf.Sin(phase) * 3f * amount, 0);
            Set(4, follow * 13f * amount, 0, follow * 4f * amount);
            Set(5, -follow * 13f * amount, 0, follow * 4f * amount);
        }

        /// <summary>Absolute shader time, smooth flap-linked thrust, and motion amount.</summary>
        public static Vector3 JetSample(float time, float period, bool reduced)
        {
            if (float.IsNaN(time) || float.IsInfinity(time)) time = 0;
            if (float.IsNaN(period) || float.IsInfinity(period)) period = 1.65f;
            float phase = time * (Mathf.PI * 2f / Mathf.Max(.1f, period));
            return reduced ? new Vector3(0, .92f, 0)
                : new Vector3(time, .94f + .06f * Mathf.Sin(phase - .35f), 1);
        }

        /// <summary>Updates only the jet; one cached block and no per-frame allocation.</summary>
        public void SampleJet(float time)
        {
            if (jetRenderer == null) return;
            if (jetProperties == null) jetProperties = new MaterialPropertyBlock();
            var sample = JetSample(time, flapPeriod, reducedMotion);
            jetProperties.Clear();
            jetProperties.SetFloat(JetTimeId, sample.x);
            jetProperties.SetFloat(JetStrengthId, sample.y);
            jetProperties.SetFloat(JetMotionId, sample.z);
            jetRenderer.SetPropertyBlock(jetProperties);
        }

        void Set(int i, float x, float y, float z)
        { if (joints[i] != null) { joints[i].localRotation = bindRotations[i] * Quaternion.Euler(x, y, z); joints[i].localPosition = bindPositions[i]; } }
    }
}
