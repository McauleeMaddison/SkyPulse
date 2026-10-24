Shader "SkyPulse/Atmospheric Depth Veil"
{
    Properties { _Tint("World light",Color)=(.2,.7,1,1) _Haze("World haze",Color)=(.05,.10,.18,1) _Visibility("World fade",Float)=1 _Clock("Presentation clock",Float)=0 _Origin("Scene origin",Vector)=(0,0,0,0) }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float4 _Tint,_Haze,_Origin;float _Visibility,_Clock;
            struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
            vary vert(float4 vertex:POSITION,float2 uv:TEXCOORD0){vary o;o.pos=UnityObjectToClipPos(vertex);o.uv=uv;o.world=mul(unity_ObjectToWorld,vertex).xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
            float4 frag(vary i):SV_Target
            {
                float feather=smoothstep(0,.12,i.uv.y)*(1-smoothstep(.58,1,i.uv.y))*smoothstep(0,.10,i.uv.x)*(1-smoothstep(.90,1,i.uv.x));
                clip(feather-.001);
                float2 p=(i.world.xy-_Origin.xy)*float2(.13,.32)+float2(_Clock*.004,0);
                float billow=noise(p),detail=noise(p*2.03+billow*.5);
                float body=smoothstep(.24,.80,billow*.70+detail*.30);
                float3 shadow=max(_Haze.rgb*.9,float3(.045,.065,.11));
                float3 light=lerp(float3(.28,.38,.49),_Tint.rgb,.33);
                return float4(lerp(shadow,light,body),feather*(.035+body*.14)*_Visibility);
            }
            ENDCG
        }
    }
}
