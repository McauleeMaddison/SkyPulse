using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    // The visual face presses into its frame. The original hierarchy and fixed
    // touch rectangle remain intact; no layout or gameplay input is animated.
    public sealed class SkyPulseButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool ReducedMotion;
        RectTransform face;float pressed,depth;
        void Prepare()
        {
            var button=GetComponent<Button>();if(button==null||button.targetGraphic==null)return;
            var candidate=button.targetGraphic.rectTransform;face=candidate!=transform?candidate:null;
        }
        void OnEnable(){pressed=depth=0;if(face!=null){face.localScale=Vector3.one;face.localRotation=Quaternion.identity;face.anchoredPosition=Vector2.zero;}}
        public void OnPointerDown(PointerEventData data)=>pressed=1;
        public void OnPointerUp(PointerEventData data)=>pressed=0;
        public void OnPointerExit(PointerEventData data)=>pressed=0;
        void Update()
        {
            Prepare();if(face==null)return;depth=ReducedMotion?0:Mathf.Lerp(depth,pressed,1-Mathf.Exp(-Time.unscaledDeltaTime*28));
            face.localScale=Vector3.one*(1-depth*.014f);face.localRotation=Quaternion.Euler(depth*3,0,0);face.anchoredPosition=new Vector2(0,-depth*3);
        }
    }
}
