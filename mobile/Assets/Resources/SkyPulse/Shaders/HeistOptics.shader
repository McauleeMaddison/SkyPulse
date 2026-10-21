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
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Off ZWrite [_ZWrite] Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct app {float4 vertex:POSITION;float3 normal:NORMAL;fixed4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct vary {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;fixed4 color:COLOR;};
            fixed4 _BaseColor,_GlowColor;float _Energy,_Glass,_Roughness;
            vary vert(app v){UNITY_SETUP_INSTANCE_ID(v);vary o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.color=v.color;return o;}
            fixed4 frag(vary i):SV_Target
            {
                float3 n=normalize(i.normal),v=normalize(_WorldSpaceCameraPos.xyz-i.world);if(dot(n,v)<0)n=-n;
                float3 light=normalize(float3(-.55,.7,-1)),fill=normalize(float3(.8,-.1,-.7));
                float diffuse=.29+.57*saturate(dot(n,light))+.17*saturate(dot(n,fill));
                float3 reflection=reflect(-v,n);float strip=pow(saturate(1-abs(reflection.x*.7+reflection.y*.5-.22)*3.1),14);
                float rim=pow(1-saturate(dot(n,v)),3),spec=pow(saturate(dot(n,normalize(light+v))),lerp(150,24,_Roughness));
                float brushed=sin(i.world.x*630+i.world.y*91)*sin(i.world.y*812)*.012;
                float3 metal=_BaseColor.rgb*i.color.rgb*(diffuse+brushed)+float3(.60,.79,.88)*(spec*.9+strip*(1-_Roughness)*.22+rim*.12);
                float3 glass=_BaseColor.rgb*(.48+.4*diffuse)+float3(.67,.93,1)*(spec*.9+strip*.45+rim*.36);
                float led=i.color.a;float3 tint=lerp(metal,glass,_Glass)+_GlowColor.rgb*_Energy*((.22+led*.68)+_Glass*.28);
                return fixed4(tint,lerp(_BaseColor.a,saturate(_BaseColor.a+rim*.3+spec*.18),_Glass));
            }
            ENDCG
        }
    }
}
