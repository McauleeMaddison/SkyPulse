Shader "SkyPulse/Flight Light"
{
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
            struct vary { float4 pos:SV_POSITION;fixed4 color:COLOR; };
            vary vert(float4 vertex:POSITION,fixed4 color:COLOR){vary o;o.pos=UnityObjectToClipPos(vertex);o.color=color;return o;}
            fixed4 frag(vary i):SV_Target{return i.color;}
            ENDCG
        }
    }
}
