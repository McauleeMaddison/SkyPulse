using UnityEngine;
using UnityEngine.EventSystems;

namespace SkyPulse.Mobile
{
    // Visual response starts on press while the existing Button remains solely
    // responsible for click/drag cancellation and authoritative purchases.
    public sealed class SkyPulseTreeTouchFeedback : MonoBehaviour, IPointerDownHandler
    {
        public SkyPulseTechTreeView View;
        public Vector2 TreePoint;
        public void OnPointerDown(PointerEventData data)
        { if (View != null && data.button == PointerEventData.InputButton.Left) View.Press(TreePoint); }
    }
}
