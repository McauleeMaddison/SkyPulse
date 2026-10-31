using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    public sealed partial class SkyPulseNativeGame
    {
        Text homeIdentity;
        Text homeHangarTitle,homeTechTitle;
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
            // The real mode exhibit supplies the hero identity. Retire the
            // generic illustrated radar rings behind the dimensional equipment.
            if(homeProtocolGraphic!=null)homeProtocolGraphic.gameObject.SetActive(false);
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
                face.ProjectedGlass=true;face.SoftGlass=true;face.Hologram=false;face.Ambient=false;face.material=HomeGlassFinish;face.canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1;
                var finish=Color.Lerp(accent,relay?Hex("#e7b76d"):Hex("#72c9ff"),face.Primary?.05f:.22f);
                finish.a=face.Primary?1f:.72f;face.color=finish;face.SetVerticesDirty();
            }
            foreach(var button in homeScreen.GetComponentsInChildren<Button>(true))
            {
                SetComfortHitTarget(button);
                var face=button.targetGraphic as SkyPulseCircuitGraphic;
                if(face!=null){face.rectTransform.sizeDelta=((RectTransform)button.transform).sizeDelta;face.SetVerticesDirty();}
                var text=button.GetComponentInChildren<Text>();
                if(text!=null)
                {
                    text.color=button==homeLaunchButton?Hex("#f2fdff"):Hex("#bed5e2");
                    var optics=text.GetComponent<SkyPulseHolographicText>();
                    if(optics!=null)optics.Emphasis=button==homeLaunchButton?.62f:.22f;
                }
                foreach(var glyph in button.GetComponentsInChildren<SkyPulseUiGlyph>())glyph.color=new Color(accent.r,accent.g,accent.b,.7f);
            }
            menuEquippedText.color=Hex("#b2d0db");
            homeBestCaption.color=Color.Lerp(accent,Hex("#b8d2dc"),.5f);
            menuBestText.color=Hex("#edfaff");
            difficultyText.color=accent;
            ApplyHomeActionProgress();
        }
        // Reuse the existing labels and action roots so callbacks, ownership and
        // navigation stay unchanged. This can run again after every mode switch.
        void SetupHomeActionProgress()
        {
            if(homeHangar==null||homeTech==null||menuHangarProgressText==null||menuTechProgressText==null)return;
            if(homeHangarTitle==null)homeHangarTitle=homeHangar.GetComponentInChildren<Text>(true);
            if(homeTechTitle==null)homeTechTitle=homeTech.GetComponentInChildren<Text>(true);
            if(menuHangarProgressText.transform.parent!=homeHangar)menuHangarProgressText.transform.SetParent(homeHangar,false);
            if(menuTechProgressText.transform.parent!=homeTech.transform)menuTechProgressText.transform.SetParent(homeTech.transform,false);
            menuHangarProgressText.name="Bird collection progress";
            menuTechProgressText.name="Installed tech progress";
        }
        void ApplyHomeActionProgress()
        {
            SetupHomeActionProgress();
            if(homeHangarTitle==null||homeTechTitle==null)return;
            bool endless=homeProtocol==SkyPulseMode.EndlessRoute,relay=homeProtocol==SkyPulseMode.PulseRelay;
            var tech=(RectTransform)homeTech.transform;
            // 448 x 196 gives two clearly separate cards and a 54 pt authored
            // title (15 pt on the smallest supported 320 x 568 pt phone).
            if(endless)
            {
                homeHangar.anchoredPosition=new Vector2(-240,-390);homeHangar.sizeDelta=new Vector2(448,196);
                tech.anchoredPosition=new Vector2(240,-390);tech.sizeDelta=new Vector2(448,196);
            }
            else if(relay){homeHangar.anchoredPosition=new Vector2(0,-440);homeHangar.sizeDelta=new Vector2(228,144);}
            homeHangarTitle.text=relay?"YOUR BIRD":"BIRD HANGAR";
            homeTechTitle.text="TECH TREE";
            HomeActionLabel(homeHangarTitle,endless?new Vector2(0,34):Vector2.zero,
                endless?new Vector2(416,78):new Vector2(204,116),endless?54:40,Hex("#eefbff"),FontStyle.Bold);
            HomeActionLabel(homeTechTitle,new Vector2(0,34),new Vector2(416,78),54,Hex("#eefbff"),FontStyle.Bold);
            HomeActionLabel(menuHangarProgressText,new Vector2(0,-40),new Vector2(416,66),46,Hex("#b8ceda"),FontStyle.Normal);
            HomeActionLabel(menuTechProgressText,new Vector2(0,-40),new Vector2(416,66),46,Hex("#b8ceda"),FontStyle.Normal);
            menuHangarProgressText.gameObject.SetActive(endless);menuTechProgressText.gameObject.SetActive(endless);
            HomeActionSurface(homeHangar.GetComponent<Button>(),Hex("#7cb5c9"));
            HomeActionSurface(homeTech.GetComponent<Button>(),Hex("#9aadc8"));
        }
        void HomeActionLabel(Text text,Vector2 position,Vector2 size,int fontSize,Color colour,FontStyle style)
        {
            var rect=text.rectTransform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.localScale=Vector3.one;rect.anchoredPosition=position;rect.sizeDelta=size;
            text.fontSize=fontSize;text.resizeTextForBestFit=false;text.fontStyle=style;text.alignment=TextAnchor.MiddleCenter;
            text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Truncate;
            text.color=colour;PolishScoreLabel(text,style==FontStyle.Bold?.18f:.08f);
        }
        void HomeActionSurface(Button button,Color tint)
        {
            SetComfortHitTarget(button);
            var face=button.targetGraphic as SkyPulseCircuitGraphic;
            if(face==null)return;
            // A denser individual satin face protects the lettering from busy
            // city geometry without putting a slab behind the whole action row.
            tint.a=1;face.color=tint;face.ProjectedGlass=true;face.SoftGlass=true;face.Hologram=false;
            face.material=HomeGlassFinish;face.rectTransform.sizeDelta=((RectTransform)button.transform).sizeDelta;face.SetVerticesDirty();
        }
    }
}
