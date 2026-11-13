Shader "SkyPulse/Flight Architecture"
{
    Properties
    {
        _Visibility ("World blend", Float)=1
        _Clock ("Motion", Float)=0
        _Tint ("World light", Color)=(.3,.8,1,1)
        _Haze ("Atmosphere", Color)=(.02,.04,.09,1)
        _KeyTone ("World architectural key",Color)=(1.05,.91,.75,1)
        _FillTone ("World architectural fill",Color)=(.18,.29,.43,1)
        _Environment ("Authored world reflections",2D)="gray"{}
        _Architecture("Curtain wall detail",Float)=0
        _Geology("Mineral strata",Float)=0
        _Design("District identity",Float)=0
        _WindowGlow("Occupied window illumination",Range(0,1))=1
        _RestoredA("Dock / cargo / transit / relay",Vector)=(0,0,0,0)
        _RestoredB("Power / skyway / shield / uplink",Vector)=(0,0,0,0)
        _RestorationHeight("Authored district roof height",Float)=6
        _RestorePulse("Completed restoration scan",Float)=0
        _TitaniumFinish("Relay titanium finish",Float)=0
        _Backdrop("Distant skyline haze",Range(0,1))=0
        _DistrictDepth("Composed aerial perspective",Range(0,1))=0
        _BeaconGlow("Star LED light spill",Range(0,1))=1
        _SurfaceTex("Machined alloy surface",2D)="gray"{}
        _SurfaceAmount("Surface detail",Range(0,1))=0
        _SurfaceScale("Surface scale",Float)=.3
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Opaque"}
        Pass
        {
            Tags {"LightMode"="ForwardBase"}
            Cull Off ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            struct app {float4 vertex:POSITION;float3 normal:NORMAL;float4 color:COLOR;};
            struct vary {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float4 color:COLOR;float3 local:TEXCOORD2;SHADOW_COORDS(3)};
            float4 _RestoredA,_RestoredB;float _RestorePulse,_RestorationHeight,_TitaniumFinish;
            float _Visibility,_Clock,_Architecture,_Geology,_Design,_WindowGlow,_BeaconGlow,_Backdrop,_DistrictDepth;float4 _Tint,_Haze,_KeyTone,_FillTone;
            float4 _BeaconPoints[5],_BeaconColours[5];
            sampler2D _Environment,_SurfaceTex;float _SurfaceAmount,_SurfaceScale;
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color;o.local=v.vertex.xyz;TRANSFER_SHADOW(o);return o;}
            float hash21(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float facadeLine(float value,float width){float aa=max(fwidth(value),.001);return 1-smoothstep(width,width+aa,abs(frac(value)-.5));}
            float3 district(vary i)
            {
                float3 n=normalize(i.normal),view=normalize(_WorldSpaceCameraPos.xyz-i.world);
                if(dot(n,view)<0)n=-n;
                float3 key=normalize(float3(-.64,.87,-.75)),halfway=normalize(key+view);
                float fill=saturate(dot(n,normalize(float3(.72,.32,-.55))));
                float skyFill=saturate(n.y*.5+.5);
                float facing=saturate(dot(n,view));
                // Real facade, balcony and cliff shadows modulate the direct key.
                // Ambient sky and occupied rooms stay readable in the shade.
                float contact=SHADOW_ATTENUATION(i);
                float ndl=saturate(dot(n,key))*lerp(.18,1,contact);
                float neon=smoothstep(.445,.465,i.color.a)*(1-smoothstep(.495,.515,i.color.a));
                float glass=max(neon,smoothstep(.27,.32,i.color.a)*(1-smoothstep(.38,.43,i.color.a)));
                float light=smoothstep(.76,.96,i.color.a),stone=step(.06,i.color.a)*(1-step(.23,i.color.a));
                float3 reflection=reflect(-view,n);
                float sky=saturate(reflection.y*.6+.4);
                float3 ambient=lerp(float3(.035,.050,.081),float3(.34,.43,.55),sky);
                ambient=lerp(ambient,ambient*lerp(float3(1,1,1),max(_Tint.rgb,.24),.24),.65);
                float3 environment=tex2D(_Environment,saturate(reflection.xy*.24+float2(.5,.38))).rgb;
                ambient=lerp(ambient,ambient*.8+environment*.19,.55);
                float fresnel=.055+.945*pow(1-facing,5);
                // A broad cool skybox and narrow warm key show curved surfaces;
                // the frame and glass have separate roughness and reflectance.
                float softbox=pow(saturate(1-abs(reflection.x*.72+reflection.y*.32-.19)*1.75),22);
                float spec=pow(saturate(dot(n,halfway)),lerp(52,126,glass));
                float alloyMask=(1-step(.25,i.color.a))*_TitaniumFinish;
                i.color.rgb=lerp(i.color.rgb,float3(.32,.43,.53)*clamp(.68+dot(i.color.rgb,float3(.299,.587,.114))*.60,.70,1.1),alloyMask);
                float3 surfaceColour=lerp(i.color.rgb,float3(.035,.055,.085)+i.color.rgb*.04,neon);
                // A warm key and cool sky fill expose the actual wall, sill and
                // crown normals. Keep alloy solid rather than glowing glass.
                float3 lighting=float3(.22,.25,.31)+_KeyTone.rgb*ndl*.84+lerp(_FillTone.rgb,_Tint.rgb*.42,.34)*fill*.34;
                lighting+=float3(.075,.12,.17)*skyFill;
                float3 colour=surfaceColour*lighting*lerp(float3(.96,.99,1.02),max(_Tint.rgb,.28),.08);
                // Near facades retain solid readable values. Reflections belong
                // mainly to inset glazing; broad grey gloss flattened the masonry.
                // A restrained solid-wall reflection leaves the actual
                // ceramic/stone colour visible. Clear inset glass retains the
                // stronger sky reflection, without silver-washing the city.
                colour+=ambient*(.075+glass*.38+fresnel*.14)+float3(.90,.94,1)*(spec*contact*(.18+glass*.15)+softbox*(.030+glass*.24))*lerp(1,.12,stone);
                if(_SurfaceAmount>.001)
                {
                    float3 axis=pow(abs(mul((float3x3)unity_WorldToObject,n)),4);axis/=max(dot(axis,1),.001);float3 p=i.local*_SurfaceScale;
                    float3 detail=tex2D(_SurfaceTex,p.yz).rgb*axis.x+tex2D(_SurfaceTex,p.xz).rgb*axis.y+tex2D(_SurfaceTex,p.xy).rgb*axis.z;
                    // Surface grain modulates the existing alloy colour instead of
                    // painting unrelated image panels across each building.
                    float grain=dot(detail,float3(.2126,.7152,.0722));
                    colour*=lerp(1,.87+grain*.24,_SurfaceAmount*(1-glass)*(1-light));
                    float3 detailedFinish=detail*(.38+ndl*.82)+ambient*.19+float3(.87,.91,1)*(spec*.46+softbox*.20);
                    colour=lerp(colour,detailedFinish,_SurfaceAmount*.42*(1-glass)*(1-light));
                }
                // Facade bays are glazing with dark reveals and a few occupied
                // warm interiors. They are not continuous glowing floor discs.
                float2 grid=float2((i.local.x+i.local.z*.61)*9.5,i.local.y*4.1);
                float mullion=max(facadeLine(grid.x,.015),facadeLine(grid.y,.018));
                colour*=1-glass*mullion*.25;
                // Filter the occupied-room mask at its projected size. Distant
                // windows should merge into quiet warm interiors rather than
                // sparkle as subpixel rooms cross a pixel during parallax.
                float2 cell=frac(grid),roomAA=max(fwidth(grid),float2(.001,.001));
                float roomDetail=1-smoothstep(.45,1.25,max(roomAA.x,roomAA.y));
                float interior=smoothstep(.15-roomAA.x,.23+roomAA.x,cell.x)*(1-smoothstep(.72-roomAA.x,.84+roomAA.x,cell.x))*smoothstep(.18-roomAA.y,.28+roomAA.y,cell.y)*(1-smoothstep(.67-roomAA.y,.78+roomAA.y,cell.y));
                interior=lerp(.30,interior,roomDetail);
                float occupied=lerp(.24,step(.76,hash21(floor(grid)+_Design*7.1)),roomDetail);
                colour+=glass*(1-neon)*interior*occupied*float3(.90,.60,.30)*(.15+.05*sin(_Clock*.10+i.local.y));
                // Only the explicitly occupied recessed panes emit. Their
                // colour is baked from the same palette as the pipe LEDs.
                // Slow, restrained drift keeps interiors alive without flashes.
                float windowPulse=.66+.025*sin(_Clock*.24+floor(i.local.y*2.6)*.73+_Design);
                // Occupied rooms sit inside every world's recessed glazing.
                // Fine dark mullions remain visible between individual lights.
                float rooms=lerp(.8176,lerp(.24,1,step(.24,hash21(floor(grid)+17.3+_Design*2.1))),roomDetail);
                float paneFalloff=.055+.945*interior*rooms;
                float windowExposure=_Design<.5?1.85:1.5;
                colour+=neon*_WindowGlow*i.color.rgb*(windowPulse*.78+.06*pow(facing,3))*paneFalloff*windowExposure;
                float surface=.94+.06*hash21(floor(i.local.xz*28));
                colour*=lerp(1,surface,stone);
                colour+=stone*i.color.rgb*(.17+.14*saturate(n.y))+.035*stone*_Tint.rgb;
                float masonry=step(.10,i.color.a)*(1-step(.145,i.color.a));
                float2 courses=float2((i.local.x+i.local.z*.73)*3.2,i.local.y*10.5);
                courses.x+=fmod(floor(courses.y),2)*.5;
                float mortar=max(facadeLine(courses.y,.012),facadeLine(courses.x,.017));
                colour*=1-masonry*mortar*.18;
                colour*=lerp(1,.95+hash21(floor(courses))*.10,masonry);
                float floorShade=lerp(.72,1,smoothstep(-.10,.50,i.local.y));colour*=floorShade;
                // Each completed network adds light to existing physical geometry.
                // The zero-progress shader remains identical to the original.
                float altitude=saturate(i.local.y/max(_RestorationHeight,.25));
                // Distinct infrastructure zones, not eight copies of one boost.
                float dock=light*(1-smoothstep(.18,.65,abs(i.local.y-.12)));
                float cargo=neon*(1-smoothstep(.28,.40,altitude));
                float transit=neon*(1-smoothstep(.06,.16,abs(frac(altitude*4)-.5)));
                float relay=neon*(1-smoothstep(.10,.24,abs(frac((i.local.x+i.local.z*.61)*.82)-.5)))*smoothstep(.30,.52,altitude);
                float powerBank=neon*smoothstep(.30,.44,altitude)*(1-smoothstep(.80,.94,altitude));
                float skyway=light*smoothstep(-.05,.08,i.local.y)*(1-smoothstep(.65,1.3,i.local.y));
                float shield=light*(1-smoothstep(-.10,.02,i.local.y));
                float uplink=max(light,neon*.48)*smoothstep(.76,.91,altitude);
                float restoreBand=pow(saturate(1-abs(i.local.y-(1-_RestorePulse)*11)*1.8),3);
                colour+=float3(.16,.65,.95)*restoreBand*_RestorePulse*.18*saturate(neon+light);
                float pulse=.88+.08*sin(_Clock*.46-i.local.y*1.7+_Design);
                colour=lerp(colour,i.color.rgb*pulse*(.88)+ambient*.05,light);
                colour+=lerp(i.color.rgb,float3(1,.63,.24),.22)*dock*_RestoredA.x*.70;
                colour+=i.color.rgb*cargo*_RestoredA.y*.85;
                colour+=lerp(i.color.rgb,float3(.16,.82,1),.20)*transit*_RestoredA.z*.85;
                colour+=lerp(i.color.rgb,float3(.22,.65,1),.22)*relay*_RestoredA.w*.85;
                colour+=i.color.rgb*powerBank*_RestoredB.x*.85;
                colour+=lerp(i.color.rgb,float3(.12,1,.72),.18)*skyway*_RestoredB.y*.75;
                colour+=lerp(i.color.rgb,float3(.68,.44,1),.22)*shield*_RestoredB.z*.65;
                colour+=lerp(i.color.rgb,float3(.65,.90,1),.20)*uplink*_RestoredB.w*.85;
                // Local LED spill is evaluated on the real cliff/foundation,
                // not on a transparent square. Shared mesh-space positions keep
                // it attached during parallax, rotation and nonuniform scaling.
                // The fixed five-source loop runs only on the island underside.
                if(_BeaconGlow>.001&&i.local.y<-.06)
                {
                    float3 spill=0;
                    [unroll] for(int beacon=0;beacon<5;beacon++)
                    {
                        float radius=_BeaconPoints[beacon].w;
                        float distanceToLed=length(i.local-_BeaconPoints[beacon].xyz)/max(radius,.001);
                        float ledPulse=.90+.07*sin(_Clock*.80+beacon*.63+_Design);
                        spill+=_BeaconColours[beacon].rgb*exp2(-distanceToLed*distanceToLed*1.35)*ledPulse*step(.01,radius);
                    }
                    colour+=spill*(_BeaconGlow+_RestoredB.z*.58)*.58;
                }
                if(_Design<.5)
                {
                    // Opposed city light catches real facade/reveal normals.
                    // Recesses remain dark while exposed roof and wall edges
                    // pick up the cyan network and magenta district lighting.
                    float cyanSide=pow(saturate(dot(n,normalize(float3(-.78,.28,-.56)))),3);
                    float pinkSide=pow(saturate(dot(n,normalize(float3(.80,.42,.26)))),3);
                    float edgeReflect=.30+.70*pow(1-facing,3);
                    colour+=(float3(.025,.16,.22)*cyanSide+float3(.13,.025,.115)*pinkSide)
                        *edgeReflect*(1-light)*(1-neon)*lerp(1,.35,stone);
                }
                float distanceToCamera=length(_WorldSpaceCameraPos.xyz-i.world);
                float mist=saturate((distanceToCamera-30)/48)*.34;
                float3 atmosphere=max(_Haze.rgb*.80+_Tint.rgb*.065,float3(.025,.040,.080));
                float3 aerialLight=atmosphere+_Tint.rgb*.105;
                // Per-instance depth is copied beside the owner's beacon data.
                // Far silhouettes become quieter coloured air; the close crown,
                // facade reveals and island edge remain crisp and dimensional.
                float distanceVeil=saturate(mist+_DistrictDepth*.28+_Backdrop*.20);
                float luminance=dot(colour,float3(.2126,.7152,.0722));
                float3 distantColour=luminance*.46+_Tint.rgb*.065;
                if(_Design>.5)
                {
                    // Keep the existing aerial-depth amount. Only its colour
                    // follows the place: rose/copper and polar scenes should
                    // not converge on the same neutral grey distant masonry.
                    float3 skyColour=lerp(_FillTone.rgb,_Tint.rgb,.20);
                    float skyLuminance=max(dot(skyColour,float3(.2126,.7152,.0722)),.001);
                    distantColour=luminance*.46*lerp(float3(1,1,1),skyColour/skyLuminance,.28)+_Tint.rgb*.065;
                }
                colour=lerp(colour,distantColour,_DistrictDepth*.20);
                return lerp(colour,aerialLight,distanceVeil);
            }
            fixed4 frag(vary i):SV_Target
            {
                // Stable coverage fade writes neither colour nor depth at zero.
                float threshold=frac(dot(floor(i.world*37),float3(.75487766,.56984029,.438579)));
                clip(_Visibility-threshold-.00001);
                if(_Architecture>.5)return fixed4(district(i),1);
                float3 n=normalize(i.normal),v=unity_OrthoParams.w>.5?float3(0,0,-1):normalize(_WorldSpaceCameraPos.xyz-i.world),key=normalize(float3(-.55,.8,-.9));
                if(dot(n,v)<0)n=-n;
                float diffuse=.89+.55*saturate(dot(n,key))+.18*saturate(dot(n,normalize(float3(.8,.2,-.6))));
                float gloss=smoothstep(.12,.38,i.color.a)*(1-smoothstep(.65,.8,i.color.a));
                float spec=pow(saturate(dot(n,normalize(key+v))),lerp(52,110,gloss))*.28;
                float3 reflection=reflect(-v,n);
                float softbox=pow(saturate(1-abs(reflection.x*.7+reflection.y*.4-.17)*2.4),12);
                float rim=pow(1-abs(dot(n,v)),4)*.055;
                float3 metal=i.color.rgb*diffuse*lerp(float3(.75,.84,.94),_Tint.rgb,.18)+(spec+softbox*gloss*.14)*float3(.68,.83,1)+_Tint.rgb*rim;
                float2 reflectionUv=saturate(reflection.xy*.34+float2(.5,.36));
                float3 worldReflection=tex2D(_Environment,reflectionUv).rgb;
                metal=lerp(metal,worldReflection*.64+metal*.86,.12+gloss*.28);
                // Fine mullions and reflected softbox strips make the glazing
                // continuous curtain wall, rather than painted blue blocks.
                float floors=1-smoothstep(.015,.045,abs(frac(i.local.y*4.5)-.5));
                float mullions=1-smoothstep(.015,.035,abs(frac(i.local.x*13)-.5));
                metal*=1-_Architecture*gloss*(floors*.20+mullions*.12);
                float reflectedStrip=pow(saturate(1-abs(n.x-.32)*2.1),18);
                float fresnel=pow(1-saturate(dot(n,v)),3);
                metal+=_Architecture*gloss*(softbox*.13+spec*.12+reflectedStrip*.24+fresnel*.21)*float3(.65,.80,.95);
                float pulse=.78+.07*sin(_Clock*.40+i.world.y*.35);
                float emission=smoothstep(.65,.95,i.color.a);
                float3 lit=lerp(metal,lerp(i.color.rgb,_Tint.rgb,.20)*pulse*lerp(1,.72,_Architecture),emission);
                // Distance haze ties the skyline into the authored atmosphere.
                float geology=_Geology*(1-step(.03,i.local.y));
                float strata=.90+.10*sin(i.local.y*39+sin(i.local.x*7)*2);
                lit=lerp(lit,lit*strata+float3(.16,.045,.11)*pow(saturate(-n.y),3),geology);
                float mist=saturate((i.world.z-5)*.009);
                float3 atmosphere=max(_Haze.rgb,lerp(float3(.04,.075,.14),_Tint.rgb,.06));
                return fixed4(lerp(lit,atmosphere,mist),1);
            }
            ENDCG
        }
        // The same real district mesh casts its own balcony / crown shadows.
        // A stable world-space coverage test respects crossfaded district banks.
        Pass
        {
            Name "District shadows"
            Tags {"LightMode"="ShadowCaster"}
            Cull Off ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_shadowcaster
            #pragma target 3.0
            #include "UnityCG.cginc"
            float _Visibility;
            struct shadowVary {V2F_SHADOW_CASTER;float3 world:TEXCOORD1;};
            shadowVary shadowVert(appdata_base v)
            {
                shadowVary o;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            float4 shadowFrag(shadowVary i):SV_Target
            {
                float threshold=frac(dot(floor(i.world*37),float3(.75487766,.56984029,.438579)));
                clip(_Visibility-threshold-.00001);SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    Fallback Off
}
