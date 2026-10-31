Shader "SkyPulse/Heist Optical Materials"
{
    Properties
    {
        _BaseColor("Material",Color)=(.08,.13,.17,1)
        _GlowColor("Circuit",Color)=(.2,1,.8,1)
        _Energy("Power",Float)=0
        _Glass("Glass",Float)=0
        _Roughness("Roughness",Float)=.35
        _ZWrite("Depth",Float)=1
        _LightTint("World key colour",Color)=(.78,.9,1,1)
        _PanelMode("Machined column details",Float)=0
        _ContactShadow("Soft contact pool",Float)=0
        _Hologram("Projected optical volume",Float)=0
        _Clock("Projection clock",Float)=0
        _BoardDeck("Optical worktop finish",Float)=0
        _OpticFinish("Precision optical instrument",Float)=0
        _Visibility("Transient visibility",Float)=1
        _Reveal("Sector handoff",Float)=0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Off ZWrite [_ZWrite] Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct app {float4 vertex:POSITION;float3 normal:NORMAL;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct vary {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;float3 local:TEXCOORD2;fixed4 color:COLOR;};
            fixed4 _BaseColor,_GlowColor,_LightTint;float _Energy,_Glass,_Roughness,_PanelMode,_ContactShadow,_Hologram,_Clock,_BoardDeck,_OpticFinish,_Visibility,_Reveal;
            vary vert(app v){UNITY_SETUP_INSTANCE_ID(v);vary o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.local=v.vertex.xyz;o.color=v.color;return o;}
            fixed4 frag(vary i):SV_Target
            {
                if(_ContactShadow>.5)return fixed4(.003,.01,.014,exp(-dot(i.local.xy,i.local.xy)*8)*_BaseColor.a);
                float3 n=normalize(i.normal),v=normalize(_WorldSpaceCameraPos.xyz-i.world);if(_OpticFinish>.5&&unity_OrthoParams.w>.5)v=float3(0,0,-1);if(dot(n,v)<0)n=-n;
                float3 light=normalize(float3(-.55,.7,-1)),fill=normalize(float3(.8,-.1,-.7));
                float diffuse=.43+.62*saturate(dot(n,light))+.20*saturate(dot(n,fill));
                float3 reflection=reflect(-v,n);float strip=pow(saturate(1-abs(reflection.x*.7+reflection.y*.5-.22)*3.1),14);
                float rim=pow(1-saturate(dot(n,v)),3),spec=pow(saturate(dot(n,normalize(light+v))),lerp(150,24,_Roughness));
                float brushed=sin(i.local.y*72)*.006;
                float3 metal=_BaseColor.rgb*i.color.rgb*(diffuse+brushed)*lerp(float3(1,1,1),_LightTint.rgb,.36)+_LightTint.rgb*(spec*.65+strip*(1-_Roughness)*.30+rim*.12);
                // The deck stays darker and rougher than the interactive optics.
                // Narrow grazing reflections reveal its bevelled physical rim.
                float3 worktop=_BaseColor.rgb*i.color.rgb*(.56+diffuse*.40)+_LightTint.rgb*(spec*.14+strip*.035+rim*.045);
                metal=lerp(metal,worktop,_BoardDeck);
                // A recessed longitudinal rail and fine collar seams give the
                // original collision-matched columns a readable manufactured form.
                float rail=pow(saturate(1-abs(i.local.x+.19)*32),3)*step(.45,abs(n.z));
                float seam=pow(saturate(1-abs(frac((i.local.y+.5)*5)-.5)*60),3);
                metal=metal*(1-_PanelMode*seam*.12)+_GlowColor.rgb*rail*_PanelMode*.26;
                float3 glass=_BaseColor.rgb*i.color.rgb*(.52+.4*diffuse)+_LightTint.rgb*(spec*.65+strip*.35+rim*.30);
                float led=i.color.a;float3 tint=lerp(metal,glass,_Glass)+_GlowColor.rgb*_Energy*((.22+led*.68)+_Glass*.28);
                float alpha=lerp(_BaseColor.a,saturate(_BaseColor.a+rim*.3+spec*.18),_Glass);
                if(_OpticFinish>.5)
                {
                    // Broad studio reflections reveal curved housings; the
                    // palette comes from the material, not a uniform cyan wash.
                    float softbox=pow(saturate(1-abs(reflection.x*.78+reflection.y*.36+.18)*1.9),18);
                    float secondary=pow(saturate(1-abs(reflection.x-.48)*2.7),28);
                    float edge=pow(1-saturate(dot(n,v)),4);
                    // A neutral environmental fill keeps dark titanium readable on
                    // phones without adding a broad white/plastic reflection.
                    float diffuseOptic=.55+.50*saturate(dot(n,light))+.15*saturate(dot(n,fill));
                    float3 alloy=_BaseColor.rgb*i.color.rgb*diffuseOptic+float3(.67,.79,.90)*(softbox*.23+spec*.50+edge*.10)+float3(.15,.30,.37)*secondary*.20;
                    float brushAA=saturate(1-fwidth(i.local.y*94));
                    alloy*=1+sin(i.local.y*94)*sin(i.local.x*117)*brushAA*.004;
                    float glassEdge=pow(edge,.55);
                    float3 opticGlass=_BaseColor.rgb*i.color.rgb*(.40+diffuseOptic*.35)+float3(.70,.89,1)*(spec*.65+softbox*.35+glassEdge*.18);
                    float ledMask=smoothstep(.70,.95,i.color.a);
                    float3 emission=_GlowColor.rgb*_Energy*(ledMask*.90+_Glass*.24);
                    tint=lerp(alloy,opticGlass,_Glass)+emission;
                    float perimeter=1-smoothstep(.02,.08,i.color.a);
                    float3 deck=_BaseColor.rgb*i.color.rgb*(.72+diffuseOptic*.25)+float3(.28,.39,.48)*(softbox*.06+spec*.10+edge*.075)+float3(.025,.040,.052)*perimeter;
                    float handoff=pow(saturate(1-abs(i.local.x-lerp(5,-5,_Reveal))*.9),4)*step(.001,_Reveal);
                    tint=lerp(tint,deck+emission+_GlowColor.rgb*handoff*_Reveal*.065,_BoardDeck);
                    // The cut glass keeps a readable body while grazing edges
                    // and a restrained transmitted core establish its volume.
                    alpha=lerp(_BaseColor.a,saturate(_BaseColor.a*.80+glassEdge*.25+spec*.12),_Glass);
                }
                // A translucent projection, with a lit silhouette and moving
                // interference bands. No opaque image or black backing plane.
                float scan=pow(saturate(cos(i.local.y*38-_Clock*1.6)),18);
                float sweep=pow(saturate(1-abs(frac(i.local.y*.18-_Clock*.045)-.5)*12),4);
                float3 hologram=_BaseColor.rgb*.28+_GlowColor.rgb*(.16+rim*.85+scan*.09+sweep*.16+_Energy*.6)+_LightTint.rgb*spec*.45;
                tint=lerp(tint,hologram,_Hologram);
                alpha=lerp(alpha,saturate(_BaseColor.a+rim*.4+scan*.05+sweep*.10),_Hologram);
                return fixed4(tint,alpha*_Visibility);
            }
            ENDCG
        }
    }
}
