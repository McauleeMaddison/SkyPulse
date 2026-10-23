Shader "SkyPulse/Physical Mineral Moon"
{
    Properties { _Tint("Atmosphere",Color)=(.2,.6,1,1) }
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
            struct app {float4 vertex:POSITION;};
            struct vary {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 local:TEXCOORD1;};
            float4 _Tint;
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.local=normalize(v.vertex.xyz);o.normal=UnityObjectToWorldNormal(o.local);return o;}
            float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
            fixed4 frag(vary i):SV_Target
            {
                float3 n=normalize(i.normal);float light=saturate(dot(n,normalize(float3(-.6,.45,-.35))));
                float3 p=i.local*22,cell=floor(p);float grain=hash(floor(i.local*270));
                float pits=pow(saturate(1-length(frac(p)-.5)*2),3)*step(.65,hash(cell));
                float terrain=.82+sin(i.local.y*20+i.local.x*13)*.04+grain*.10-pits*.25;
                float3 stone=lerp(float3(.35,.40,.48),float3(.73,.70,.66),light)*terrain;
                return fixed4(stone*(.035+light*.72)+_Tint.rgb*.012,1);
            }
            ENDCG
        }
    }
}
