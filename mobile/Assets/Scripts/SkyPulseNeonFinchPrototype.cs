using UnityEngine;

namespace SkyPulse.Mobile
{
    /// <summary>Standalone art-review rig. Never installed on a gameplay bird.</summary>
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
        [SerializeField, HideInInspector] Transform[] joints;
        [SerializeField, HideInInspector] Quaternion[] bindRotations;
        [SerializeField, HideInInspector] Vector3[] bindPositions;
        [SerializeField, HideInInspector] Quaternion bindRootRotation = Quaternion.identity;
        [SerializeField, HideInInspector] Vector3 bindRootPosition;
        bool ready;

        public void CaptureBindPose()
        {
            joints = new[] { body, head, leftWing, rightWing, leftWingTip, rightWingTip, tail, tailTip };
            bindRotations = new Quaternion[joints.Length]; bindPositions = new Vector3[joints.Length];
            for (int i = 0; i < joints.Length; i++) if (joints[i] != null)
            { bindRotations[i] = joints[i].localRotation; bindPositions[i] = joints[i].localPosition; }
            bindRootRotation = transform.localRotation; bindRootPosition = transform.localPosition; ready = true;
        }
        void OnEnable() { ready = joints != null && joints.Length == 8 && bindRotations != null && bindRotations.Length == 8; }
        void Update() { if (animate) SamplePose(Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartupAsDouble); }
        public void ResetPose()
        {
            if (!ready) CaptureBindPose();
            transform.localRotation = bindRootRotation; transform.localPosition = bindRootPosition;
            for (int i = 0; i < joints.Length; i++) if (joints[i] != null)
            { joints[i].localRotation = bindRotations[i]; joints[i].localPosition = bindPositions[i]; }
        }
        public void SamplePose(float time)
        {
            if (!ready) CaptureBindPose();
            // Absolute time makes the same pose reproducible at any frame rate.
            float phase = time * (Mathf.PI * 2f / Mathf.Max(.1f, flapPeriod));
            float stroke = Mathf.Sin(phase), follow = Mathf.Sin(phase - .53f);
            float amount = reducedMotion ? 0f : 1f;
            transform.localRotation = bindRootRotation * Quaternion.Euler(0, turntable && !reducedMotion ? time * turntableDegreesPerSecond : 0, 0);
            transform.localPosition = bindRootPosition;
            Set(0, 0, 0, Mathf.Sin(phase * .31f) * bankDegrees * amount);
            Set(1, 0, Mathf.Sin(phase * .23f) * 2f * amount, -Mathf.Sin(phase * .5f) * 1.5f * amount);
            Set(2, stroke * flapDegrees * amount, Mathf.Sin(phase) * 3f * amount, 0);
            Set(3, -stroke * flapDegrees * amount, -Mathf.Sin(phase) * 3f * amount, 0);
            Set(4, follow * 13f * amount, 0, follow * 4f * amount);
            Set(5, -follow * 13f * amount, 0, follow * 4f * amount);
            Set(6, Mathf.Sin(phase * .5f) * 3f * amount, Mathf.Sin(phase * .41f) * 4f * amount, Mathf.Sin(phase - .6f) * 4f * amount);
            Set(7, 0, Mathf.Sin(phase * .41f - .4f) * 5f * amount, Mathf.Sin(phase - .9f) * 5f * amount);
        }
        void Set(int i, float x, float y, float z)
        { if (joints[i] != null) { joints[i].localRotation = bindRotations[i] * Quaternion.Euler(x, y, z); joints[i].localPosition = bindPositions[i]; } }
    }
}
