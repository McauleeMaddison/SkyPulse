Shader "SkyPulse/Relay Runway"
{
    Properties { _Distance("Course distance",Float)=0 _Clock("Clock",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Opaque" }
        Pass
        {
            Cull Off ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct input {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct varying {float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
            float _Distance,_Clock;
            varying vert(input i){varying o;o.position=UnityObjectToClipPos(i.vertex);o.uv=i.uv;return o;}
            fixed4 frag(varying i):SV_Target
            {
                float x=i.uv.x+_Distance, depth=i.uv.y;
                float seam=1-smoothstep(.018,.045,min(frac(x*.6),1-frac(x*.6)));
                float edge=1-smoothstep(.015,.055,abs(depth));
                float chevron=step(.53,frac(x*1.1+abs(depth)*1.4));
                float3 base=lerp(float3(.075,.12,.15),float3(.18,.25,.28),saturate(depth));
                base*=1-seam*.24;
                base+=float3(.065,.10,.12)*pow(saturate(1-abs(depth-.58)*2),8);
                float guide=(1-smoothstep(.012,.038,abs(depth-.34)))*.45;
                float3 colour=base+float3(.14,.67,.79)*guide+float3(1,.56,.18)*edge*(.35+chevron*.65);
                return fixed4(colour,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
