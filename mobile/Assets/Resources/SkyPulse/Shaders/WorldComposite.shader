Shader "SkyPulse/Live World Composite"
{
    Properties { _MainTex("Live perspective camera",2D)="black"{} }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Opaque"}
        Pass
        {
            Cull Off ZWrite Off ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            sampler2D _MainTex;
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(vary i):SV_Target{return tex2D(_MainTex,i.uv);}
            ENDCG
        }
    }
}
