Shader "SkyPulse/Flight Architecture"
{
    Properties
    {
        _Visibility ("World blend", Float)=1
        _Clock ("Motion", Float)=0
        _Tint ("World light", Color)=(.3,.8,1,1)
        _Haze ("Atmosphere", Color)=(.02,.04,.09,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Opaque"}
        Pass
        {
            Cull Off ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct app {float4 vertex:POSITION;float3 normal:NORMAL;float4 color:COLOR;};
            struct vary {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float4 color:COLOR;};
            float _Visibility,_Clock;float4 _Tint,_Haze;
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color;return o;}
            fixed4 frag(vary i):SV_Target
            {
                // Stable coverage fade writes neither colour nor depth at zero.
                float2 p=floor(i.pos.xy);float threshold=frac(dot(p,float2(.75487766,.56984029)));
                clip(_Visibility-threshold-.00001);
                float3 n=normalize(i.normal),v=normalize(_WorldSpaceCameraPos.xyz-i.world),key=normalize(float3(-.55,.8,-.9));
                float diffuse=.60+.66*saturate(dot(n,key));
                float spec=pow(saturate(dot(n,normalize(key+v))),36)*.32;
                float rim=pow(1-abs(dot(n,v)),3)*.10;
                float3 metal=i.color.rgb*diffuse*lerp(float3(.75,.84,.94),_Tint.rgb,.35)+spec+_Tint.rgb*rim;
                float pulse=.83+.10*sin(_Clock*.55+i.world.y*.8);
                float3 lit=lerp(metal,_Tint.rgb*pulse,i.color.a);
                float mist=saturate((i.world.z-3)*.038);
                return fixed4(lerp(lit,_Haze.rgb,mist),1);
            }
            ENDCG
        }
    }
    Fallback Off
}
