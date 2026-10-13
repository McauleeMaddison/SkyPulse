using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    // Down-edge activation gives puzzle taps immediate feedback. There is no
    // click-on-release callback to spill into a newly configured puzzle.
    public sealed class SkyPulsePressTarget : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public int Tile,Generation;
        public Action<int,int,int> Press;
        public Action<int> Release;
        Button target;
        void Awake(){target=GetComponent<Button>();}
        public void OnPointerDown(PointerEventData data)
        {
            if(data.button!=PointerEventData.InputButton.Left || data.dragging || target==null || !target.interactable)return;
            int generation=Generation,tile=Tile;
            Press?.Invoke(tile,generation,data.pointerId);
        }
        public void OnPointerUp(PointerEventData data){Release?.Invoke(data.pointerId);}
    }
}
