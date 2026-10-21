using UnityEngine;
using UnityEngine.EventSystems;

namespace SkyPulse.Mobile
{
    // Observes the existing ScrollRect gestures; it does not create a second pan
    // implementation, consume clicks, or change the content's hit areas.
    public sealed class SkyPulseTreePanInput : MonoBehaviour, IPointerDownHandler, IBeginDragHandler
    {
        public SkyPulseTechTreeView View;
        public void OnPointerDown(PointerEventData data)
        { if (View != null && data.button == PointerEventData.InputButton.Left) View.CancelFocus(); }
        public void OnBeginDrag(PointerEventData data)
        { if (View != null && data.button == PointerEventData.InputButton.Left) { View.CancelFocus(); View.CancelPress(); } }
    }
}
