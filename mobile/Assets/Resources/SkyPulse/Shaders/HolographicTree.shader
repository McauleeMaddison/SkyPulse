Shader "SkyPulse/Holographic Tree"
{
    Properties { _Clock("Clock", Float) = 0 _Reveal("Reveal", Float) = 1 _Press("Press", Vector) = (0,0,10,0) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Back ZWrite Off Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
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
                if (dot(n,v)<0) n=-n;
                float edge=pow(1-saturate(dot(n,v)),2);
                float moving=pow(saturate(1-abs(frac(i.uv.x-_Clock*.10)-.5)*18),4);
                float radius=distance(i.local.xy,_Press.xy);
                float touch=pow(saturate(1-abs(radius-_Press.z*5)*4),3)*saturate(1-_Press.z*1.7)*_Press.w;
                float3 light=normalize(float3(-.5,.75,-1));
                float diffuse=.16+.52*saturate(dot(n,light));
                float spec=pow(saturate(dot(n,normalize(light+v))),62);
                float body=step(1.5,i.uv.y)*(1-step(3.5,i.uv.y));
                float3 glass=i.color.rgb*(diffuse+edge*.30)+float3(.67,.88,1)*spec*.72;
                float3 energy=lerp(i.color.rgb,float3(.82,.97,1),.2+moving*.5)*( .78+moving*.7+touch*.55 );
                float3 tint=lerp(energy,glass,body)+i.color.rgb*touch*.35;
                return fixed4(tint, i.color.a*_Reveal);
            }
            ENDCG
        }
    }
}
