Shader "SkyPulse/Holographic Tree"
{
    Properties { _Clock("Clock", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Off ZWrite Off Blend SrcAlpha One, One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct app { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct vary { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; float2 uv:TEXCOORD2; fixed4 color:COLOR; };
            float _Clock;
            vary vert(app v)
            {
                vary o; o.pos=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.uv=v.uv; o.color=v.color; return o;
            }
            fixed4 frag(vary i):SV_Target
            {
                float3 n=normalize(i.normal), v=normalize(_WorldSpaceCameraPos.xyz-i.world);
                float edge=pow(1-abs(dot(n,v)),.65);
                float moving=pow(saturate(1-abs(frac(i.uv.x-_Clock*.19)-.5)*13),4);
                float scan=.96+.04*sin(i.world.y*55+_Clock*.3);
                float facet=.8+.3*saturate(dot(n,normalize(float3(-.4,.6,-1))));
                float3 spectral=lerp(i.color.rgb, float3(.78,.96,1), edge*.25+moving*.2);
                return fixed4(spectral*(.6+edge*.65+moving*.6)*facet, i.color.a*scan*(.75+edge*.25));
            }
            ENDCG
        }
    }
}
