Shader "SkyPulse/Live Heist Energy Fibre"
{
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Cull Off ZWrite Off Blend SrcAlpha One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct app {float4 vertex:POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
            struct vary {float4 pos:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
            fixed4 frag(vary i):SV_Target
            {
                // A narrow optical core with a continuous soft edge. The beam
                // network is authoritative immediately; flow packets are pooled
                // decoration and never delay a rotation or a solved sector.
                float across=saturate(1-abs(i.uv.y*2-1));
                float edge=across*across*(3-2*across);
                return fixed4(i.color.rgb,i.color.a*edge);
            }
            ENDCG
        }
    }
}
