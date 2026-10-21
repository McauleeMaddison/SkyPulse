Shader "SkyPulse/Holographic Tree"
{
    Properties { _Clock("Clock", Float) = 0 _Reveal("Reveal", Float) = 1 _Press("Press", Vector) = (0,0,10,0) }
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
            struct vary { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; float2 uv:TEXCOORD2; float3 local:TEXCOORD3; fixed4 color:COLOR; };
            float _Clock, _Reveal; float4 _Press;
            vary vert(app v)
            {
                vary o; o.pos=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.local=v.vertex.xyz; o.uv=v.uv; o.color=v.color; return o;
            }
            fixed4 frag(vary i):SV_Target
            {
                float3 n=normalize(i.normal), v=normalize(_WorldSpaceCameraPos.xyz-i.world);
                float edge=pow(1-abs(dot(n,v)),.65);
                float moving=pow(saturate(1-abs(frac(i.uv.x-_Clock*.19)-.5)*13),4);
                moving+=pow(saturate(1-abs(frac(i.uv.x+_Clock*.11+.23)-.5)*23),4)*.23;
                float radius=distance(i.local.xy,_Press.xy);
                float touch=pow(saturate(1-abs(radius-_Press.z*5)*4),3)*saturate(1-_Press.z*1.7)*_Press.w;
                float scan=.96+.04*sin(i.world.y*55+_Clock*.3);
                float facet=.8+.3*saturate(dot(n,normalize(float3(-.4,.6,-1))));
                float3 spectral=lerp(i.color.rgb, float3(.78,.96,1), saturate(edge*.25+moving*.2+touch*.5));
                return fixed4(spectral*(.6+edge*.65+moving*.6+touch*1.8)*facet, i.color.a*scan*(.75+edge*.25)*_Reveal);
            }
            ENDCG
        }
    }
}
