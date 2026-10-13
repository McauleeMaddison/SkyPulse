using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SkyPulse.Mobile
{
    /// <summary>Background clicks start a run; child controls retain their own clicks.</summary>
    public sealed class SkyPulseRoundStartSurface : MonoBehaviour, IPointerClickHandler
    {
        public Action StartRound;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || eventData.dragging) return;
            var threshold = EventSystem.current == null ? 10f : EventSystem.current.pixelDragThreshold;
            if ((eventData.position - eventData.pressPosition).sqrMagnitude > threshold * threshold) return;
            StartRound?.Invoke();
        }
    }
}
