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
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct app {float4 vertex:POSITION;float3 normal:NORMAL;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct vary {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;float3 local:TEXCOORD2;fixed4 color:COLOR;};
            fixed4 _BaseColor,_GlowColor,_LightTint;float _Energy,_Glass,_Roughness,_PanelMode,_ContactShadow,_Hologram,_Clock;
            vary vert(app v){UNITY_SETUP_INSTANCE_ID(v);vary o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.local=v.vertex.xyz;o.color=v.color;return o;}
            fixed4 frag(vary i):SV_Target
            {
                if(_ContactShadow>.5)return fixed4(.003,.01,.014,exp(-dot(i.local.xy,i.local.xy)*8)*_BaseColor.a);
                float3 n=normalize(i.normal),v=normalize(_WorldSpaceCameraPos.xyz-i.world);if(dot(n,v)<0)n=-n;
                float3 light=normalize(float3(-.55,.7,-1)),fill=normalize(float3(.8,-.1,-.7));
                float diffuse=.43+.62*saturate(dot(n,light))+.20*saturate(dot(n,fill));
                float3 reflection=reflect(-v,n);float strip=pow(saturate(1-abs(reflection.x*.7+reflection.y*.5-.22)*3.1),14);
                float rim=pow(1-saturate(dot(n,v)),3),spec=pow(saturate(dot(n,normalize(light+v))),lerp(150,24,_Roughness));
                float brushed=sin(i.local.y*72)*.006;
                float3 metal=_BaseColor.rgb*i.color.rgb*(diffuse+brushed)*lerp(float3(1,1,1),_LightTint.rgb,.36)+_LightTint.rgb*(spec*.65+strip*(1-_Roughness)*.30+rim*.12);
                // A recessed longitudinal rail and fine collar seams give the
                // original collision-matched columns a readable manufactured form.
                float rail=pow(saturate(1-abs(i.local.x+.19)*32),3)*step(.45,abs(n.z));
                float seam=pow(saturate(1-abs(frac((i.local.y+.5)*5)-.5)*60),3);
                metal=metal*(1-_PanelMode*seam*.12)+_GlowColor.rgb*rail*_PanelMode*.26;
                float3 glass=_BaseColor.rgb*i.color.rgb*(.52+.4*diffuse)+_LightTint.rgb*(spec*.65+strip*.35+rim*.30);
                float led=i.color.a;float3 tint=lerp(metal,glass,_Glass)+_GlowColor.rgb*_Energy*((.22+led*.68)+_Glass*.28);
                float alpha=lerp(_BaseColor.a,saturate(_BaseColor.a+rim*.3+spec*.18),_Glass);
                // A translucent projection, with a lit silhouette and moving
                // interference bands. No opaque image or black backing plane.
                float scan=pow(saturate(cos(i.local.y*38-_Clock*1.6)),18);
                float sweep=pow(saturate(1-abs(frac(i.local.y*.18-_Clock*.045)-.5)*12),4);
                float3 hologram=_BaseColor.rgb*.28+_GlowColor.rgb*(.16+rim*.85+scan*.09+sweep*.16+_Energy*.6)+_LightTint.rgb*spec*.45;
                tint=lerp(tint,hologram,_Hologram);
                alpha=lerp(alpha,saturate(_BaseColor.a+rim*.4+scan*.05+sweep*.10),_Hologram);
                return fixed4(tint,alpha);
            }
            ENDCG
        }
    }
}
