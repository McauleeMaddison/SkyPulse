using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        // Result presentation only: scores, rewards, saves and replay callbacks
        // stay in their original mode files. Edit the glass shader for finish.
        Material scoreGlassMaterial,scoreTypographyMaterial;
        float scoreFinishClock;
        bool IsResultContent(Transform root)=>missionResultScreen!=null&&(root==missionResultScreen.transform||root.IsChildOf(missionResultScreen.transform));
        RectTransform HolographicScorePanel(Transform parent,string name,Vector2 at,Vector2 size,Color accent)
        {
            var panel=CreatePanel(parent,name,at,size,Color.clear);panel.GetComponent<Image>().raycastTarget=false;
            ApplyScoreGlass(panel,accent);return panel;
        }
        SkyPulseScoreGlass ApplyScoreGlass(RectTransform panel,Color accent)
        {
            var image=panel.GetComponent<Image>();if(image!=null){image.color=Color.clear;image.raycastTarget=false;}
            foreach(var outline in panel.GetComponents<Outline>())outline.enabled=false;
            if(scoreGlassMaterial==null)scoreGlassMaterial=new Material(Resources.Load<Shader>("SkyPulse/Shaders/ScoreGlass")){name="Shared SkyPulse result glass"};
            var obj=new GameObject("Holographic result finish",typeof(RectTransform),typeof(CanvasRenderer),typeof(SkyPulseScoreGlass));obj.transform.SetParent(panel,false);obj.transform.SetAsFirstSibling();
            var face=obj.GetComponent<SkyPulseScoreGlass>();var rect=face.rectTransform;
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            face.color=accent;face.material=scoreGlassMaterial;face.raycastTarget=false;
            face.canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1;
            return face;
        }
        void PolishScoreLabel(Text label,float emphasis=.65f)
        {
            foreach(var shadow in label.GetComponents<Shadow>())shadow.enabled=false;
            if(scoreTypographyMaterial==null)scoreTypographyMaterial=new Material(Resources.Load<Shader>("SkyPulse/Shaders/HolographicText")){name="Shared SkyPulse result lettering"};
            label.canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1;
            label.material=scoreTypographyMaterial;label.raycastTarget=false;
            var effect=label.GetComponent<SkyPulseHolographicText>();if(effect==null)effect=label.gameObject.AddComponent<SkyPulseHolographicText>();effect.Emphasis=emphasis;
        }
        void PolishScoreLabels(Transform root,float emphasis=.65f)
        {
            foreach(var label in root.GetComponentsInChildren<Text>(true))PolishScoreLabel(label,emphasis);
        }
        void PolishScoreActions(Transform root)
        {
            foreach(var button in root.GetComponentsInChildren<Button>(true))
            {
                var image=button.GetComponent<Image>();
                foreach(var layer in button.GetComponentsInChildren<Image>())if(layer!=image)layer.enabled=false;
                var accent=button.GetComponentInChildren<Text>().text=="RETRY"?Hex("#65e6ff"):Hex("#85b8d4");
                button.targetGraphic=ApplyScoreGlass((RectTransform)button.transform,accent);
                // The original transparent Image remains the button's hit area;
                // the finish and text are decorative and never intercept taps.
                image.color=new Color(1,1,1,.001f);image.raycastTarget=true;
                var colours=button.colors;colours.normalColor=Color.white;colours.pressedColor=new Color(.65f,.83f,.86f);colours.fadeDuration=.1f;button.colors=colours;
            }
        }
        void UpdateScoreFinish(float dt)
        {
            if(!reduceMotionEnabled&&state!=FlightState.Paused)scoreFinishClock+=Mathf.Max(0,dt);
            float motion=reduceMotionEnabled?0:1;
            if(scoreGlassMaterial!=null){scoreGlassMaterial.SetFloat("_Clock",scoreFinishClock);scoreGlassMaterial.SetFloat("_Motion",motion);}
            if(scoreTypographyMaterial!=null){scoreTypographyMaterial.SetFloat("_Clock",scoreFinishClock);scoreTypographyMaterial.SetFloat("_Motion",motion);}
        }
    }
}
