using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SkyPulse.Mobile
{
    /// <summary>Small, allocation-free press response for touch-first controls.</summary>
    public sealed class SkyPulseButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private RectTransform target;
        private Vector3 restingScale;
        private float pressAmount;

        private void Awake()
        {
            target = transform as RectTransform;
            restingScale = target != null ? target.localScale : Vector3.one;
        }

        private void OnEnable()
        {
            pressAmount = 0f;
            if (target != null) target.localScale = restingScale;
        }

        public void OnPointerDown(PointerEventData eventData) => pressAmount = 1f;
        public void OnPointerUp(PointerEventData eventData) => pressAmount = 0f;
        public void OnPointerExit(PointerEventData eventData) => pressAmount = 0f;

        private void Update()
        {
            if (target == null) return;
            var scale = Vector3.one * (1f - pressAmount * .045f);
            target.localScale = Vector3.Lerp(target.localScale, Vector3.Scale(restingScale, scale), 1f - Mathf.Exp(-Time.unscaledDeltaTime * 24f));
        }
    }
}
