Shader "SkyPulse/Optical Prism"
{
    Properties { _Clock ("Clock", Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct app { float4 vertex:POSITION; float3 normal:NORMAL; fixed4 color:COLOR; };
            struct vary { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; fixed4 color:COLOR; };
            float _Clock;
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.color=v.color;return o;}
            fixed4 frag(vary i):SV_Target
            {
                float3 n=normalize(i.normal),view=normalize(_WorldSpaceCameraPos.xyz-i.world);
                float edge=pow(1-abs(dot(n,view)),3);
                float spec=pow(saturate(dot(n,normalize(float3(-.55,.8,-.9)+view))),65);
                float reflectBand=pow(saturate(.5+.5*sin(reflect(-view,n).y*10+reflect(-view,n).x*3)),18);
                float core=step(.99,i.color.a);
                float3 tint=i.color.rgb*(.5+.45*abs(dot(n,normalize(float3(-.55,.8,-.9)))))+float3(.55,.88,1)*(spec*1.1+reflectBand*.4+edge*.35);
                tint=lerp(tint,float3(.48,.95,1)*(1.1+.1*sin(_Clock*2)),core);
                return fixed4(tint,lerp(saturate(i.color.a+edge*.12),1,core));
            }
            ENDCG
        }
    }
}
