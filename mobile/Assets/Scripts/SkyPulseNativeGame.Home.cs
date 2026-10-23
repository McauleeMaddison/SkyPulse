using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        Text homeIdentity;
        Material homeGlassMaterial;
        Material HomeGlassFinish
        {
            get
            {
                if(homeGlassMaterial==null){homeGlassMaterial=new Material(ScoreGlassFinish){name="Open holographic Home glass"};homeGlassMaterial.SetFloat("_HomeFinish",1);}
                return homeGlassMaterial;
            }
        }
        void ApplyHomeIdentity()
        {
            bool endless=homeProtocol==SkyPulseMode.EndlessRoute,relay=homeProtocol==SkyPulseMode.PulseRelay;
            if(homeProtocolBackdrop!=null)homeProtocolBackdrop.enabled=!relay;
            Color accent=endless?Hex("#ab91ff"):relay?Hex("#50e9ff"):Hex("#4dffd3");
            if(homeIdentity==null)
            {
                homeIdentity=CreateText(homeScreen.transform,"ENDLESS ROUTE",new Vector2(0,645),new Vector2(930,90),55,accent,TextAnchor.MiddleCenter,FontStyle.Bold);
                foreach(var text in homeScreen.GetComponentsInChildren<Text>(true))PolishScoreLabel(text,.40f);
            }
            homeIdentity.text=endless?"ENDLESS ROUTE":relay?"PULSE RELAY":"BLACKOUT PROTOCOL";homeIdentity.fontSize=endless||relay?57:46;homeIdentity.color=accent;
            menuTitleText.fontSize=58;menuTitleText.rectTransform.anchoredPosition=new Vector2(0,817);menuTitleText.color=Hex("#e5f7ff");
            if(homeArcade!=null){homeArcade.fontSize=20;homeArcade.rectTransform.anchoredPosition=new Vector2(0,755);homeArcade.color=Hex("#a1c2d2");}
            if(homeAtmosphere!=null)homeAtmosphere.gameObject.SetActive(!endless);
            foreach(string name in new[]{"Brand wing port","Brand wing starboard","Title energy rule","Flight deck"}){var decor=homeScreen.transform.Find(name);if(decor!=null)decor.gameObject.SetActive(false);}
            if(!relay)
            {
                SetHomeRect(modeHomeGuide,new Vector2(0,-665),new Vector2(660,144));
                homePrivacy.anchoredPosition=new Vector2(180,-870);homePrivacy.sizeDelta=new Vector2(280,144);
                homeTasks.anchoredPosition=new Vector2(-180,-870);homeTasks.sizeDelta=new Vector2(320,144);
            }
            // Preserve navigation and use disjoint, generous touch targets. The mode's
            // identity comes from its world, hero and material, not extra menus.
            foreach(var face in homeScreen.GetComponentsInChildren<SkyPulseCircuitGraphic>(true))
            {
                if(face.Surface!=SkyPulseSurface.Panel)continue;
                face.ProjectedGlass=true;face.SoftGlass=true;face.Ambient=false;face.material=HomeGlassFinish;face.canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1;
                face.color=Color.Lerp(accent,relay?Hex("#e7b76d"):Hex("#72c9ff"),face.Primary?.05f:.22f);face.SetVerticesDirty();
            }
            foreach(var button in homeScreen.GetComponentsInChildren<Button>(true))
            {
                SetComfortHitTarget(button);
                var face=button.targetGraphic as SkyPulseCircuitGraphic;
                if(face!=null){face.rectTransform.sizeDelta=((RectTransform)button.transform).sizeDelta;face.SetVerticesDirty();}
                var text=button.GetComponentInChildren<Text>();if(text!=null)text.color=Hex("#e6f5ff");
                foreach(var glyph in button.GetComponentsInChildren<SkyPulseUiGlyph>())glyph.color=new Color(accent.r,accent.g,accent.b,.7f);
            }
            difficultyText.color=accent;
        }
    }
}
