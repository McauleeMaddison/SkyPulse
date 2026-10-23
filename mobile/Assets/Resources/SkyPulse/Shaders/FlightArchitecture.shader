Shader "SkyPulse/Flight Architecture"
{
    Properties
    {
        _Visibility ("World blend", Float)=1
        _Clock ("Motion", Float)=0
        _Tint ("World light", Color)=(.3,.8,1,1)
        _Haze ("Atmosphere", Color)=(.02,.04,.09,1)
        _Environment ("Authored world reflections",2D)="gray"{}
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
            sampler2D _Environment;
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color;return o;}
            fixed4 frag(vary i):SV_Target
            {
                // Stable coverage fade writes neither colour nor depth at zero.
                float2 p=floor(i.pos.xy);float threshold=frac(dot(p,float2(.75487766,.56984029)));
                clip(_Visibility-threshold-.00001);
                float3 n=normalize(i.normal),v=normalize(_WorldSpaceCameraPos.xyz-i.world),key=normalize(float3(-.55,.8,-.9));
                if(dot(n,v)<0)n=-n;
                float diffuse=.55+.60*saturate(dot(n,key));
                float gloss=smoothstep(.12,.38,i.color.a)*(1-smoothstep(.65,.8,i.color.a));
                float spec=pow(saturate(dot(n,normalize(key+v))),lerp(52,110,gloss))*.28;
                float3 reflection=reflect(-v,n);
                float softbox=pow(saturate(1-abs(reflection.x*.7+reflection.y*.4-.17)*2.4),12);
                float rim=pow(1-abs(dot(n,v)),4)*.055;
                float3 metal=i.color.rgb*diffuse*lerp(float3(.75,.84,.94),_Tint.rgb,.18)+(spec+softbox*gloss*.14)*float3(.68,.83,1)+_Tint.rgb*rim;
                float2 reflectionUv=saturate(reflection.xy*.34+float2(.5,.36));
                float3 worldReflection=tex2D(_Environment,reflectionUv).rgb;
                metal=lerp(metal,worldReflection*.75+metal*.35,.18+gloss*.46);
                float pulse=.78+.07*sin(_Clock*.40+i.world.y*.35);
                float emission=smoothstep(.65,.95,i.color.a);
                float3 lit=lerp(metal,lerp(i.color.rgb,_Tint.rgb,.45)*pulse,emission);
                // Distance haze ties the skyline into the authored atmosphere.
                float mist=saturate((i.world.z-3)*.038);
                return fixed4(lerp(lit,_Haze.rgb,mist),1);
            }
            ENDCG
        }
    }
    Fallback Off
}
