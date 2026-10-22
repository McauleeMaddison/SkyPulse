Shader "SkyPulse/Flight Light"
{
    Properties { _Soft("Feathered flight ribbon",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Off ZWrite Off Blend SrcAlpha One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Soft;
            struct vary { float4 pos:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0; };
            vary vert(float4 vertex:POSITION,fixed4 color:COLOR,float2 uv:TEXCOORD0){vary o;o.pos=UnityObjectToClipPos(vertex);o.color=color;o.uv=uv;return o;}
            fixed4 frag(vary i):SV_Target
            {
                // Analytic light falloff, with no texture or opaque backplate.
                // Default remains flat for the existing Heist optical fibres.
                float across=saturate(1-abs(i.uv.y*2-1));
                float ends=smoothstep(0,.065,i.uv.x)*(1-smoothstep(.72,1,i.uv.x));
                i.color.a*=lerp(1,across*across*ends,_Soft);
                return i.color;
            }
            ENDCG
        }
    }
}
