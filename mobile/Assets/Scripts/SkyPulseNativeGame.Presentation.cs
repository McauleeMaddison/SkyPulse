using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkyPulse.Mobile
{
    // Presentation only. Flight forces, objective coordinates, puzzle rules and wallet data are separate.
    public sealed partial class SkyPulseNativeGame
    {
        private readonly List<SkyPulseCircuitGraphic> protocolGraphics=new List<SkyPulseCircuitGraphic>();
        private readonly List<SkyPulseCircuitGraphic> homeFrames=new List<SkyPulseCircuitGraphic>();
        private readonly List<SkyPulseCircuitGraphic> modeRowFrames=new List<SkyPulseCircuitGraphic>();
        private RectTransform modeDropdownClip;
        private const float ModeDropdownHeight=688f;
        private CanvasGroup modeDropdownGroup;
        private bool modeDropdownOpen;
        private float modeDropdownReveal, protocolVisualTime;
        private Text modeBadgeArrow, homeBestCaption;
        private readonly List<SkyPulseButtonFeedback> protocolButtonFeedback=new List<SkyPulseButtonFeedback>();
        private SkyPulseCircuitGraphic homeAtmosphere;
        private Transform homeFlightDock;
        private const string RelayStationPath="SkyPulse/backgrounds/pulse-relay-station-v1";
        private const string BlackoutCorePath="SkyPulse/backgrounds/blackout-core-v1";
        private Button homeLaunchButton;
        private Sprite relayGlowSprite, relayPlateSprite, relayFieldSprite;
        private readonly List<SpriteRenderer> relayApertureGlow=new List<SpriteRenderer>();
        private readonly List<SkyPulseRelayGate> relayPortalHardware=new List<SkyPulseRelayGate>();
        private SkyPulseRelayGate relayExitHardware;
        private SkyPulseRelayDeck relayDeck;
        private readonly List<Transform> relayPortalOrbits=new List<Transform>();
        private readonly List<Transform> relayChargeArt=new List<Transform>();
        private readonly List<SkyPulsePrism> relayPrisms=new List<SkyPulsePrism>(10);
        private readonly List<SpriteRenderer> relayLasers=new List<SpriteRenderer>(15);
        private readonly List<SpriteRenderer> relayLaserHalos=new List<SpriteRenderer>(15);
        private readonly List<SpriteRenderer> relayCrystalHighlights=new List<SpriteRenderer>(10),relayCrystalFlares=new List<SpriteRenderer>(10);
        private readonly List<float> relayOpeningTimes=new List<float>();
        private readonly List<SpriteRenderer> relayShutterArt=new List<SpriteRenderer>(),relayMembranes=new List<SpriteRenderer>(),relayCrystalGlints=new List<SpriteRenderer>();
        private readonly List<TextMesh> relayGateLabels=new List<TextMesh>();
        private readonly List<int> relayGateLabelCounts=new List<int>();
        private SpriteRenderer relayScenery;
        private Text relayChargeText,relayProgressText,blackoutDataText,blackoutTraceText,blackoutUplinkText;

        private SkyPulseCircuitGraphic ProtocolGraphic(Transform parent,string name,Vector2 position,Vector2 size,int variant,SkyPulseSurface surface,Color accent,bool animated=false)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(SkyPulseCircuitGraphic));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.sizeDelta=size;rect.anchoredPosition=position;
            var graphic=obj.GetComponent<SkyPulseCircuitGraphic>();graphic.Surface=surface;graphic.Variant=variant;graphic.color=accent;graphic.Ambient=animated;graphic.ReducedMotion=reduceMotionEnabled;graphic.raycastTarget=false;
            protocolGraphics.Add(graphic);return graphic;
        }
        private SkyPulseCircuitGraphic ProtocolBackdrop(RectTransform backing,int variant)
        {
            var graphic=ProtocolGraphic(backing,"Protocol architecture",Vector2.zero,Vector2.zero,variant,SkyPulseSurface.Backdrop,Color.white,true);
            var rect=graphic.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            if(variant==2){backing.GetComponent<Image>().sprite=LoadSprite(BlackoutCorePath);backing.GetComponent<Image>().color=Color.white;graphic.CinematicUnderlay=true;}return graphic;
        }
        private RectTransform PremiumPanel(Transform parent,string name,Vector2 at,Vector2 size,int variant,Color accent,bool primary=false)
        {
            if(IsResultContent(parent))return HolographicScorePanel(parent,name,at,size,accent);
            var panel=CreatePanel(parent,name,at,size,Color.clear);panel.GetComponent<Image>().raycastTarget=false;
            var graphic=ProtocolGraphic(panel,"Machined surface",Vector2.zero,size,variant,SkyPulseSurface.Panel,accent,primary);graphic.Primary=primary;return panel;
        }
        private SkyPulseCircuitGraphic PolishButton(Button button,int variant,Color accent,bool primary=false)
        {
            var root=button.GetComponent<RectTransform>();var image=root.GetComponent<Image>();image.color=Color.clear;
            foreach(var child in root.GetComponentsInChildren<Image>())if(child!=image)child.enabled=false;
            foreach(var outline in root.GetComponents<Outline>())outline.enabled=false;
            var surface=ProtocolGraphic(root,"Illuminated action surface",Vector2.zero,root.sizeDelta,variant,SkyPulseSurface.Panel,accent,primary);surface.transform.SetAsFirstSibling();surface.Primary=primary;
            button.targetGraphic=surface;var colours=button.colors;colours.normalColor=Color.white;colours.pressedColor=new Color(.65f,.83f,.86f);colours.disabledColor=new Color(.35f,.45f,.45f,1);colours.fadeDuration=.12f;button.colors=colours;
            var label=button.GetComponentInChildren<Text>();if(label!=null)label.color=Hex("#effcff");
            foreach(var glyph in root.GetComponentsInChildren<SkyPulseUiGlyph>())glyph.color=new Color(.7f,.94f,1,.65f);
            return surface;
        }
        private void CreateProtocolDropdown()
        {
            modeSelectionScreen=MissionScreen("Protocol dropdown",false,false);
            var scrim=CreateFullPanel(modeSelectionScreen.transform,"Protocol dismiss layer",new Color(0,.01f,.02f,.48f));
            var dismiss=scrim.gameObject.AddComponent<Button>();dismiss.targetGraphic=scrim.GetComponent<Image>();dismiss.transition=Selectable.Transition.None;dismiss.onClick.AddListener(CloseProtocolDropdown);
            var obj=new GameObject("Badge anchored menu",typeof(RectTransform),typeof(RectMask2D),typeof(CanvasGroup));obj.transform.SetParent(modeSelectionScreen.transform,false);
            modeDropdownClip=obj.GetComponent<RectTransform>();modeDropdownClip.pivot=new Vector2(.5f,1);modeDropdownClip.anchoredPosition=new Vector2(-135,898);modeDropdownClip.sizeDelta=new Vector2(620,ModeDropdownHeight);
            modeDropdownGroup=obj.GetComponent<CanvasGroup>();
            var content=new GameObject("Fixed dropdown content",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(modeDropdownClip,false);content.anchorMin=content.anchorMax=new Vector2(.5f,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(620,ModeDropdownHeight);
            PremiumPanel(content,"Dropdown frame",Vector2.zero,new Vector2(620,ModeDropdownHeight),0,Hex("#8ed8ed"));
            // Content's own pivot is top-centred; its children are positioned around its centre.
            var heading=CreateText(content,"GAME MODES",new Vector2(-175,314),new Vector2(220,32),22,Hex("#b4d0df"),TextAnchor.MiddleLeft,FontStyle.Bold);heading.raycastTarget=false;
            CreateText(content,"ALL FREE",new Vector2(229,314),new Vector2(116,32),18,Hex("#65ffad"),TextAnchor.MiddleRight,FontStyle.Bold);
            var names=new[]{"ENDLESS ROUTE","PULSE RELAY","BLACKOUT PROTOCOL"};var captions=new[]{"Classic flight through 12 worlds","Tap to fly. Crystals open gates.","Rotate prisms. Route live energy."};
            for(int i=0;i<3;i++)
            {
                int index=i;var colour=ProtocolAccent((SkyPulseMode)i);
                var row=MissionButton(content,"",0,200-i*196,580,()=>SelectHomeProtocol((SkyPulseMode)index),colour,176);
                var frame=row.targetGraphic as SkyPulseCircuitGraphic;frame.Variant=i;modeRowFrames.Add(frame);
                var defaultLabel=row.GetComponentInChildren<Text>();defaultLabel.text="";defaultLabel.enabled=false;
                ProtocolGraphic(row.transform,"Mode insignia",new Vector2(-235,0),new Vector2(72,82),i,SkyPulseSurface.Emblem,Color.white,true);
                var label=CreateText(row.transform,names[i],new Vector2(45,27),new Vector2(435,46),34,colour,TextAnchor.MiddleLeft,FontStyle.Bold);label.horizontalOverflow=HorizontalWrapMode.Wrap;
                CreateText(row.transform,captions[i],new Vector2(45,-27),new Vector2(435,44),26,Hex("#b7cbd3"),TextAnchor.MiddleLeft,FontStyle.Normal);
            }
            var badge=(RectTransform)difficultyText.transform.parent;var toggle=badge.gameObject.AddComponent<Button>();toggle.onClick.AddListener(()=>{if(modeDropdownOpen)CloseProtocolDropdown();else OpenModeSelection();});
            toggle.targetGraphic=badge.GetComponent<Image>();toggle.transition=Selectable.Transition.None;
            SetComfortHitTarget(toggle);
            modeBadgeArrow=CreateText(badge,"▾",new Vector2(140,0),new Vector2(34,48),28,Hex("#b4e8f4"),TextAnchor.MiddleCenter,FontStyle.Bold);
            modeSelectionScreen.SetActive(false);
        }
        private static Color ProtocolAccent(SkyPulseMode mode)=>mode==SkyPulseMode.BlackoutProtocol?Hex("#65ffad"):mode==SkyPulseMode.PulseRelay?Hex("#ffd078"):Hex("#b392ff");
        private void SelectHomeProtocol(SkyPulseMode mode){SetHomeProtocol(mode);ResetToMenu();}
        private void LaunchHomeProtocol(){if(homeProtocol==SkyPulseMode.EndlessRoute)StartFlight();else LaunchMission(homeProtocol,HomeNextMission());}
        private void CloseProtocolDropdown(){modeDropdownOpen=false;if(reduceMotionEnabled)modeSelectionScreen.SetActive(false);}
        private void ShowProtocolDropdown()
        {
            modeDropdownOpen=true;modeDropdownReveal=reduceMotionEnabled?1:0;modeSelectionScreen.SetActive(true);modeSelectionScreen.transform.SetAsLastSibling();
            foreach(var frame in modeRowFrames){frame.Primary=frame.Variant==(int)homeProtocol;frame.SetVerticesDirty();}
            UpdateProtocolPresentation(0);
        }
        private void SetupHomePresentation()
        {
            homeAtmosphere=ProtocolGraphic(homeScreen.transform,"Mode atmosphere",Vector2.zero,new Vector2(1080,2160),0,SkyPulseSurface.Atmosphere,Color.white,true);
            homeAtmosphere.transform.SetSiblingIndex(1);
            foreach(string name in new[]{"Flight deck","Personal best"})
            {
                var root=homeScreen.transform.Find(name);if(root==null)continue;
                root.GetComponent<Image>().color=Color.clear;foreach(var outline in root.GetComponents<Outline>())outline.enabled=false;
                if(name=="Flight deck")continue;
                var rect=(RectTransform)root;var frame=ProtocolGraphic(root,"Home deck surface",Vector2.zero,rect.sizeDelta,0,SkyPulseSurface.Panel,Hex("#45eaff"));frame.transform.SetAsFirstSibling();homeFrames.Add(frame);
            }
            if(homeLaunchButton!=null)homeFrames.Add(PolishButton(homeLaunchButton,0,Hex("#45eaff"),true));
            foreach(var button in homeScreen.GetComponentsInChildren<Button>(true))
                if(button!=homeLaunchButton&&button.transform!=difficultyText.transform.parent&&!(button.targetGraphic is SkyPulseCircuitGraphic))homeFrames.Add(PolishButton(button,0,Hex("#8ed8ed")));
            homeFlightDock=homeScreen.transform.Find("Home flight dock");
            // The old illustrated dock and opaque 3D support bars competed with
            // the hero. The selected mode now gets one open, dimensional stage.
            foreach(var frame in homeFrames){frame.SoftGlass=true;frame.Ambient=false;frame.SetVerticesDirty();}
            foreach(var text in homeScreen.GetComponentsInChildren<Text>())if(text.text=="PERSONAL BEST")homeBestCaption=text;
        }
        private void ApplyPremiumHomeTheme()
        {
            var accent=ProtocolAccent(homeProtocol);
            foreach(var face in homeScreen.GetComponentsInChildren<SkyPulseCircuitGraphic>(true))
                if(face.Surface==SkyPulseSurface.Panel){face.SoftGlass=true;face.Ambient=false;face.SetVerticesDirty();}
            if(homeAtmosphere!=null){homeAtmosphere.Variant=(int)homeProtocol;homeAtmosphere.SetVerticesDirty();}
            foreach(var frame in homeFrames){frame.Variant=(int)homeProtocol;frame.color=homeProtocol==SkyPulseMode.EndlessRoute?Hex("#45eaff"):accent;frame.SetVerticesDirty();}
            difficultyText.color=accent;var badge=(RectTransform)difficultyText.transform.parent;badge.sizeDelta=new Vector2(340,68);badge.anchoredPosition=new Vector2(-285,932);
            SetComfortHitTarget(badge.GetComponent<Button>());
            difficultyText.rectTransform.sizeDelta=new Vector2(286,48);difficultyText.rectTransform.anchoredPosition=new Vector2(-17,0);difficultyText.horizontalOverflow=HorizontalWrapMode.Wrap;difficultyText.verticalOverflow=VerticalWrapMode.Truncate;
            if(modeBadgeArrow!=null)modeBadgeArrow.color=accent;
            if(homeLaunchButton!=null)homeLaunchButton.GetComponentInChildren<Text>().text="PLAY";
            if(homeBestCaption!=null)homeBestCaption.text=homeProtocol==SkyPulseMode.EndlessRoute?"PERSONAL BEST":"ENDLESS BEST";
            ApplyHomePlayerView();
            ApplyModeHomeLayout();
            ApplyHomeIdentity();
        }
        private void ApplyHomePlayerView()
        {
            if(menuHeroTransform==null)return;
            menuHeroTransform.gameObject.SetActive(homeProtocol!=SkyPulseMode.BlackoutProtocol);
            if(homeFlightDock!=null)homeFlightDock.gameObject.SetActive(false);
            if(homeProtocol==SkyPulseMode.PulseRelay){menuHeroTransform.localScale=Vector3.one*.54f;menuHeroTransform.anchoredPosition=new Vector2(-105,355);menuEquippedText.text="PILOT · "+equippedSkin.Name;}
            else if(homeProtocol==SkyPulseMode.BlackoutProtocol)menuEquippedText.text="Rotate prisms · route live energy";
            else {menuHeroTransform.localScale=Vector3.one;menuHeroTransform.anchoredPosition=new Vector2(0,355);menuEquippedText.text="EQUIPPED · "+equippedSkin.Name;}
            ApplyBlackoutHomeView();EnsureHomeDepth();
        }
        private void UpdateProtocolPresentation(float dt)
        {
            bool paused=state==FlightState.Paused;
            UpdateScoreFinish(dt);
            FitRelayHudToSafeArea();
            UpdateBlackoutPresentation();
            UpdateBlackoutActionAnimation(dt);
            UpdateCinematicHome();
            if(!paused)relayUnlockFeedback=Mathf.Max(0,relayUnlockFeedback-dt);
            for(int i=protocolButtonFeedback.Count-1;i>=0;i--)
            {
                var feedback=protocolButtonFeedback[i];
                if(feedback==null){protocolButtonFeedback.RemoveAt(i);continue;}
                feedback.ReducedMotion=reduceMotionEnabled;
            }

            for(int i=protocolGraphics.Count-1;i>=0;i--)
            {
                var graphic=protocolGraphics[i];if(graphic==null){protocolGraphics.RemoveAt(i);continue;}
                graphic.ReducedMotion=reduceMotionEnabled;graphic.MotionPaused=paused;
            }
            if(modeDropdownClip!=null && modeSelectionScreen.activeSelf)
            {
                float target=modeDropdownOpen?1:0;
                modeDropdownReveal=reduceMotionEnabled?target:Mathf.MoveTowards(modeDropdownReveal,target,dt*5f);
                float eased=1-Mathf.Pow(1-modeDropdownReveal,3);modeDropdownClip.sizeDelta=new Vector2(620,ModeDropdownHeight*eased);modeDropdownGroup.alpha=eased;
                modeDropdownGroup.interactable=modeDropdownOpen;modeDropdownGroup.blocksRaycasts=modeDropdownOpen;
                if(modeDropdownReveal<=0 && !modeDropdownOpen)modeSelectionScreen.SetActive(false);
            }
            if(activeMode==SkyPulseMode.PulseRelay && state==FlightState.Playing && !reduceMotionEnabled)protocolVisualTime+=dt;
        }
        private void ClearProtocolWorldArt(){relayApertureGlow.Clear();relayPortalHardware.Clear();relayPortalOrbits.Clear();relayChargeArt.Clear();relayPrisms.Clear();relayLasers.Clear();relayLaserHalos.Clear();relayOpeningTimes.Clear();relayShutterArt.Clear();relayMembranes.Clear();relayCrystalGlints.Clear();relayCrystalHighlights.Clear();relayCrystalFlares.Clear();relayGateLabels.Clear();relayGateLabelCounts.Clear();relayExitHardware=null;relayDeck=null;relayEnvironment=null;relayScenery=null;protocolVisualTime=0;modeDropdownOpen=false;}
        private void RemoveScreenPresentation(GameObject root)
        {
            protocolGraphics.RemoveAll(g=>g==null || (g.transform.IsChildOf(root.transform) && !g.transform.parent.name.EndsWith("backdrop")));
        }
        private Sprite ProceduralGlowSprite()
        {
            if(relayGlowSprite!=null)return relayGlowSprite;
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.name="Authored soft circuit light";texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
            var pixels=new Color[64*64];for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;pixels[y*64+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2));}
            texture.SetPixels(pixels);texture.Apply(false,true);relayGlowSprite=Sprite.Create(texture,new Rect(0,0,64,64),Vector2.one*.5f,32);return relayGlowSprite;
        }
        private Sprite ProceduralPlateSprite()
        {
            if(relayPlateSprite!=null)return relayPlateSprite;
            var texture=new Texture2D(32,256,TextureFormat.RGBA32,false);texture.name="Authored relay station gradient";texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
            var pixels=new Color[32*256];for(int y=0;y<256;y++)for(int x=0;x<32;x++)
            {
                float t=x/31f;float brightness=.26f+Mathf.Sin(t*Mathf.PI)*.42f;pixels[y*32+x]=new Color(brightness,brightness,brightness,1);
            }
            texture.SetPixels(pixels);texture.Apply(false,true);relayPlateSprite=Sprite.Create(texture,new Rect(0,0,32,256),Vector2.one*.5f,32);return relayPlateSprite;
        }
        private Sprite RelayFieldSprite()
        {
            if(relayFieldSprite!=null)return relayFieldSprite;
            var texture=new Texture2D(64,128,TextureFormat.RGBA32,false);texture.name="Authored elliptical relay membrane";texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
            var pixels=new Color[64*128];for(int y=0;y<128;y++)for(int x=0;x<64;x++)
            {
                float dx=(x-31.5f)/31.5f,dy=(y-63.5f)/63.5f;float edge=Mathf.Clamp01(1-dx*dx-dy*dy);
                float bands=.58f+.42f*Mathf.Pow(Mathf.Sin(y*.35f),8);pixels[y*64+x]=new Color(1,1,1,Mathf.Sqrt(edge)*bands);
            }
            texture.SetPixels(pixels);texture.Apply(false,true);relayFieldSprite=Sprite.Create(texture,new Rect(0,0,64,128),Vector2.one*.5f,32);return relayFieldSprite;
        }
        private SkyPulseRelayGate RelayPortalFrame(Transform parent,float y,bool exit=false)
        {
            var root=new GameObject("Volumetric relay assembly").transform;root.SetParent(parent,false);root.localPosition=new Vector3(0,y,0);
            var gate=root.gameObject.AddComponent<SkyPulseRelayGate>();gate.Build(exit);return gate;
        }
        private SpriteRenderer WorldGlow(Transform parent,string name,Vector2 at,Vector2 size,Color colour,int order)
        {
            var renderer=CreateRenderer(name,ProceduralGlowSprite(),colour,order,parent);SetSpriteBlock(renderer,at,size);return renderer;
        }
        private SpriteRenderer WorldPlate(Transform parent,string name,Vector2 at,Vector2 size,Color colour,int order)
        {
            var renderer=CreateRenderer(name,ProceduralPlateSprite(),colour,order,parent);SetSpriteBlock(renderer,at,size);return renderer;
        }
        private SpriteRenderer WorldLine(Transform parent,string name,Vector2 a,Vector2 b,float width,Color colour,int order)
        {
            var renderer=CircuitSprite(parent,name,(a+b)*.5f,new Vector2((b-a).magnitude,width),colour,order);renderer.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2((b-a).y,(b-a).x)*Mathf.Rad2Deg);return renderer;
        }
        private TextMesh WorldLabel(Transform parent,string text,Vector2 at,Color colour,float scale=.12f)
        {
            var obj=new GameObject("Circuit label "+text,typeof(TextMesh));obj.transform.SetParent(parent,false);obj.transform.localPosition=at;
            var label=obj.GetComponent<TextMesh>();label.text=text;label.font=uiFont;label.fontSize=40;label.characterSize=scale;label.anchor=TextAnchor.MiddleCenter;label.color=colour;
            var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=uiFont.material;renderer.sortingOrder=12;return label;
        }
        private void BuildPremiumRelayWorld()
        {
            missionWorld=new GameObject("Authored Pulse circuit").transform;HideOriginalWorld(true);
            var tint=selectedMission==1?new Color(.76f,.95f,1):selectedMission==2?new Color(1,.87f,.68f):selectedMission==3?new Color(.70f,.82f,1):Color.white;
            relayScenery=CreateRenderer("Relay orbital station",LoadSprite(RelayStationPath),tint,-40,missionWorld);FitBackgroundToCamera(relayScenery,1.1f);
            CircuitSprite(missionWorld,"Station atmosphere veil",Vector2.zero,new Vector2(GetViewportWidth()+1,22),new Color(.005f,.035f,.065f,.24f),-30);
            WorldGlow(missionWorld,"Station atmosphere",new Vector2(1,2),new Vector2(20,26),new Color(.1f,.68f,.83f,.31f),-28);
            WorldGlow(missionWorld,"Gold horizon",new Vector2(0,-7),new Vector2(18,7),new Color(1,.6f,.12f,.16f),-27);
            relayEnvironment=new GameObject("Living orbital station depth").AddComponent<SkyPulseRelayEnvironment>();relayEnvironment.transform.SetParent(missionWorld,false);relayEnvironment.Build(GetViewportWidth(),LoadSprite(RelayStationPath).texture);
            for(int i=-3;i<=3;i++)WorldLine(missionWorld,"Perspective bus",new Vector2(i*3,-9),new Vector2(i*.65f,3),.008f,new Color(.1f,.6f,.7f,.07f),-24);
            var deck=new GameObject("Relay runway · ground collision edge").AddComponent<SkyPulseRelayDeck>();deck.transform.SetParent(missionWorld,false);deck.transform.localPosition=new Vector3(0,GroundY,0);deck.Build(GetViewportWidth()+4,LoadSprite(RelayStationPath).texture);relayDeck=deck;
            for(int i=0;i<relaySession.Mission.RelayCount;i++)
            {
                float y=relaySession.Mission.RelayY[i];var gate=new GameObject("Relay "+(i+1)).transform;gate.SetParent(missionWorld,false);relayShutters.Add(gate);
                // Floating portal hardware, not Endless's ceiling/floor pipe silhouettes.
                WorldGlow(gate,"Suspended hardware contact light",new Vector2(.3f,y-2.55f),new Vector2(2.5f,.34f),new Color(.08f,.65f,.9f,.17f),2);
                relayPortalHardware.Add(RelayPortalFrame(gate,y));
                var orbit=new GameObject("Portal emitter orbit").transform;orbit.SetParent(gate,false);orbit.localPosition=new Vector2(0,y);relayPortalOrbits.Add(orbit);
                for(int emitter=0;emitter<2;emitter++)WorldGlow(orbit,"Emitter glint",Vector2.zero,new Vector2(.34f,.34f),new Color(.2f,.92f,1,.75f),7);
                var membrane=CreateRenderer("Activation window",RelayFieldSprite(),new Color(1,.08f,.09f,.10f),4,gate);SetSpriteBlock(membrane,new Vector2(0,y),new Vector2(1.35f,RelaySession.ApertureHalfHeight*2));relayMembranes.Add(membrane);
                relayShutterArt.Add(CircuitSprite(gate,"Energy shutter",new Vector2(0,y),new Vector2(.025f,RelaySession.ApertureHalfHeight*2),new Color(1,.12f,.14f,.5f),5));
                for(int beam=0;beam<3;beam++)
                {
                    float h=(beam-1)*.85f,half=Mathf.Sqrt(SkyPulseRelayGate.InnerRadius*SkyPulseRelayGate.InnerRadius-h*h)*.46f;
                    relayLasers.Add(WorldLine(gate,"Red security laser "+beam,new Vector2(-half,y+h),new Vector2(half,y+h),.024f,new Color(1,.16f,.18f,.85f),6));
                    relayLaserHalos.Add(WorldGlow(gate,"Laser scattering "+beam,new Vector2(0,y+h),new Vector2(half*2+.08f,.15f),new Color(1,.09f,.12f,.24f),5));
                }
                var glow=WorldGlow(gate,"Aperture illumination",new Vector2(0,y),new Vector2(2.15f,3.5f),new Color(1,.6f,.15f,.22f),3);relayApertureGlow.Add(glow);relayOpeningTimes.Add(-1f);
                relayGateLabels.Add(WorldLabel(gate,(i+1).ToString("00")+" · 0/2",new Vector2(0,y+2.85f),Hex("#ffd078"),.085f));relayGateLabelCounts.Add(0);
                // Random daily positions remain ahead of their assigned gate,
                // within a reachable vertical collection band.
                AddRelayCell(i*2,new Vector2(relaySession.Mission.CrystalX(i*2),relaySession.Mission.CrystalY(i*2)));AddRelayCell(i*2+1,new Vector2(relaySession.Mission.CrystalX(i*2+1),relaySession.Mission.CrystalY(i*2+1)));
            }
            if(relaySession.Mission.HasShield)
            {
                relayShield=new GameObject("Aegis circuit pickup").transform;relayShield.SetParent(missionWorld,false);var icon=CreateRenderer("Aegis",LoadSprite(PowerUpArtworkPath(PowerUpKind.Aegis)),Color.white,8,relayShield);icon.transform.localScale=Vector3.one*(.9f/icon.sprite.bounds.size.x);
            }
            relayExit=new GameObject("Circuit finish line").transform;relayExit.SetParent(missionWorld,false);
            WorldGlow(relayExit,"Exit atmosphere",new Vector2(0,relaySession.Mission.ExitY),new Vector2(3,5),new Color(.2f,1,.55f,.38f),4);
            relayExitHardware=RelayPortalFrame(relayExit,relaySession.Mission.ExitY,true);
            WorldLabel(relayExit,"EXIT",new Vector2(0,relaySession.Mission.ExitY+2.85f),Hex("#65ffad"),.13f);
        }
        private void BuildPremiumRelayCell(int id,Vector2 at)
        {
            var cell=new GameObject("Gate crystal "+id).transform;cell.SetParent(missionWorld,false);
            WorldGlow(cell,"Crystal spectral bloom",Vector2.zero,new Vector2(1.45f,2.05f),new Color(.22f,.78f,1,.24f),6);
            var art=new GameObject("Faceted gate prism").transform;art.SetParent(cell,false);
            var prism=art.gameObject.AddComponent<SkyPulsePrism>();prism.Build();relayPrisms.Add(prism);
            var glint=WorldGlow(art,"Moving facet reflection",new Vector2(-.08f,.28f),new Vector2(.16f,.22f),new Color(.8f,1,1,.8f),9);relayCrystalGlints.Add(glint);
            var highlight=WorldGlow(art,"Polished crystal facet",new Vector2(-.10f,.03f),new Vector2(.065f,.72f),new Color(.75f,.94f,1,.42f),9);
            highlight.transform.localRotation=Quaternion.Euler(0,0,-19);relayCrystalHighlights.Add(highlight);
            var flare=WorldGlow(art,"Prism crown caustic",new Vector2(0,.43f),new Vector2(.36f,.085f),new Color(.76f,.94f,1,.50f),10);relayCrystalFlares.Add(flare);
            WorldLabel(cell,(id/2+1).ToString("00"),new Vector2(0,-.76f),Hex("#aeefff"),.065f);
            relayChargeArt.Add(art);relayCells.Add(cell);relayCellPositions.Add(at);
        }
        private void AnimateRelayArtwork()
        {
            float motion=reduceMotionEnabled?0:protocolVisualTime;
            if(relayScenery!=null)relayScenery.transform.localPosition=new Vector2(reduceMotionEnabled?0:Mathf.Sin(relaySession.Distance*.018f)*-.22f+Mathf.Sin(motion*.22f)*.06f,.12f+Mathf.Sin(motion*.3f)*.06f);
            for(int i=0;i<relayShutters.Count;i++)
            {
                bool open=relaySession.IsOpen(i);var gate=relayShutters[i];
                if(open && relayOpeningTimes[i]<0)relayOpeningTimes[i]=protocolVisualTime;
                float opening=!open?0:reduceMotionEnabled?1:Mathf.Clamp01((protocolVisualTime-relayOpeningTimes[i])/.28f);
                var shutter=relayShutterArt[i];SetSpriteBlock(shutter,new Vector2(0,relaySession.Mission.RelayY[i]),new Vector2(.025f,RelaySession.ApertureHalfHeight*2*(1-opening)));shutter.color=opening>=1?Color.clear:new Color(1,.12f,.14f,.5f);
                relayMembranes[i].color=open?Color.clear:new Color(1,.08f,.09f,.10f);
                if(open)shutter.color=Color.clear;
                for(int beam=0;beam<3;beam++)
                {
                    var hue=open?new Color(.12f,.66f,1,reduceMotionEnabled?0:(1-opening)*.28f):new Color(1,.12f,.15f,.78f);
                    relayLasers[i*3+beam].color=hue;hue.a*=.32f;relayLaserHalos[i*3+beam].color=hue;
                }
                relayPortalHardware[i].SetCharge(relaySession.GateCrystals(i),open,motion,open&&!reduceMotionEnabled?1-opening:0);
                var orbit=relayPortalOrbits[i];for(int emitter=0;emitter<2;emitter++){float t=motion*.6f+emitter*Mathf.PI+i*.7f;var tip=orbit.GetChild(emitter);tip.localPosition=new Vector2(Mathf.Cos(t)*1.04f,Mathf.Sin(t)*2.03f);tip.GetComponent<SpriteRenderer>().color=open?new Color(.15f,.65f,1,.65f):new Color(1,.12f,.15f,.55f);}
                float unlockLight=open&&!reduceMotionEnabled?Mathf.Pow(1-opening,2)*.30f:0;
                relayApertureGlow[i].color=open?new Color(.14f,.6f,1,.13f+unlockLight):new Color(1,.12f,.13f,.13f+Mathf.Sin(motion*1.8f+i)*.015f);
                int count=relaySession.GateCrystals(i);
                if(relayGateLabelCounts[i]!=count){relayGateLabelCounts[i]=count;relayGateLabels[i].text=(i+1).ToString("00")+" · "+count+"/2";}
                relayGateLabels[i].color=open?Hex("#94ffe1"):Hex("#ffd078");

            }
            if(relayDeck!=null)relayDeck.SetVisual(reduceMotionEnabled?0:relaySession.Distance,motion);
            if(relayEnvironment!=null)relayEnvironment.SetVisual(reduceMotionEnabled?0:relaySession.Distance,motion);
            if(relayExitHardware!=null)relayExitHardware.SetVisual(true,motion,0);
            for(int i=0;i<relayChargeArt.Count;i++)
            {
                if(!relayCells[i].gameObject.activeSelf)continue;
                float phase=motion*1.65f+i*.8f;
                relayPrisms[i].SetVisual(motion,i*.8f);
                relayChargeArt[i].localPosition=new Vector2(0,Mathf.Sin(phase)*.045f);
                relayCrystalGlints[i].transform.localPosition=new Vector2(-.12f+Mathf.Sin(phase)*.1f,.18f+Mathf.Cos(phase*.8f)*.17f);
                relayCrystalGlints[i].color=new Color(.8f,1,1,.48f+Mathf.Sin(phase*.65f)*.18f);
                float reflection=.5f+.5f*Mathf.Sin(phase*.8f);
                relayCrystalHighlights[i].color=new Color(.75f,.94f,1,.22f+reflection*.28f);
                relayCrystalFlares[i].color=new Color(.76f,.94f,1,.24f+reflection*.30f);
            }
        }
        private Text ProtocolStat(Transform parent,string name,string value,float x,float y,float width,int variant,Color accent)
        {
            var panel=PremiumPanel(parent,name+" instrument",new Vector2(x,y),new Vector2(width,112),variant,accent);
            MissionText(panel,name,28,18,accent,width-20,28);return MissionText(panel,value,-17,32,Hex("#e4fff5"),width-20,44);
        }
    }
}
