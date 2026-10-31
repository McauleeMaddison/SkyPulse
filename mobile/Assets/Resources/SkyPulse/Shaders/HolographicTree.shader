Shader "SkyPulse/Holographic Tree"
{
    Properties { _Clock("Clock", Float) = 0 _Reveal("Reveal", Float) = 1 _Press("Press", Vector) = (0,0,10,0) _Tint("Tint",Color)=(1,1,1,1) }
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
            float _Clock, _Reveal; float4 _Press; fixed4 _Tint;
            vary vert(app v)
            {
                vary o; o.pos=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.local=v.vertex.xyz; o.uv=v.uv; o.color=v.color; return o;
            }
            fixed4 frag(vary i):SV_Target
            {
                float3 n=normalize(i.normal),v=unity_OrthoParams.w>.5?float3(0,0,-1):normalize(_WorldSpaceCameraPos.xyz-i.world);
                if(dot(n,v)<0)n=-n;
                float3 key=normalize(float3(-.55,.78,-.85));
                float facing=saturate(dot(n,v)),edge=pow(1-facing,3),ndl=saturate(dot(n,key));
                float3 r=reflect(-v,n);
                float reflection=pow(saturate(1-abs(r.x*.62+r.y*.25+.17)*1.62),19);
                float strip=pow(saturate(1-abs(r.x-.44)*2.8),42);
                float spec=pow(saturate(dot(n,normalize(key+v))),88);
                float moving=pow(saturate(1-abs(frac(i.uv.x-_Clock*.10)-.5)*20),4);
                float radius=distance(i.local.xy,_Press.xy);
                float touch=pow(saturate(1-abs(radius-_Press.z*5)*4),3)*saturate(1-_Press.z*1.7)*_Press.w;
                float body=step(1.5,i.uv.y)*(1-step(3.5,i.uv.y));
                // A crisp continuous optical jacket has a curved dark centre,
                // narrow silver reflection and a restrained coloured rim. The
                // live fibre carries pulses; no expanded haze layer is needed.
                float3 jacket=float3(.018,.040,.062)+i.color.rgb*(.09+ndl*.18+edge*.23);
                jacket+=float3(.60,.79,.92)*(reflection*.28+strip*.17+spec*.26);
                float powered=smoothstep(.30,.88,i.color.a);
                jacket*=lerp(.48,1,powered);
                float3 fibre=lerp(i.color.rgb,float3(.72,.94,1),.18+moving*.30)*(.61+moving*.75+touch*.6);
                float3 tint=lerp(fibre,jacket,body)+i.color.rgb*touch*.26;
                return fixed4(tint*_Tint.rgb,i.color.a*_Reveal*_Tint.a);
            }
            ENDCG
        }
    }
}
