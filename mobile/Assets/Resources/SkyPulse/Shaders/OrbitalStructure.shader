Shader "SkyPulse/Orbital Station Hardware"
{
    Properties { _Tint("Station light",Color)=(.23,.66,.82,1) _Haze("Distance haze",Color)=(.02,.07,.11,1) _Environment("Station reflections",2D)="gray"{} _Clock("Presentation clock",Float)=0 }
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
            sampler2D _Environment;float4 _Tint,_Haze;float _Clock;
            struct app {float4 vertex:POSITION;float3 normal:NORMAL;float4 colour:COLOR;};
            struct vary {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;float3 local:TEXCOORD2;float4 colour:COLOR;};
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.local=v.vertex.xyz;o.colour=v.colour;return o;}
            float4 frag(vary i):SV_Target
            {
                float3 n=normalize(i.normal),view=normalize(_WorldSpaceCameraPos.xyz-i.world);if(dot(n,view)<0)n=-n;
                float3 key=normalize(float3(-.6,.8,-.8)),reflection=reflect(-view,n);
                float ndl=saturate(dot(n,key)),fresnel=pow(1-saturate(dot(n,view)),4);
                float broad=pow(saturate(1-abs(reflection.x*.75+reflection.y*.3-.18)*1.8),12);
                float spec=pow(saturate(dot(n,normalize(key+view))),96);
                float3 reflected=tex2D(_Environment,saturate(reflection.xy*.28+float2(.5,.42))).rgb;
                float3 metal=i.colour.rgb*(.25+ndl*.64)+reflected*(.10+fresnel*.09);
                metal+=float3(.68,.78,.88)*(broad*.28+spec*.58)+_Tint.rgb*fresnel*.085;
                float seam=1-smoothstep(.012,.024+fwidth(i.local.z*2),abs(frac(i.local.z*2)-.5));
                metal*=1-seam*.08;
                // The wide concourse is solid machined metal. Flush service
                // panels and recessed optical lanes break up its large face;
                // they follow the actual curved deck rather than a backplate.
                float radius=length(i.local.xy),angle=atan2(i.local.y,i.local.x);
                float deck=smoothstep(6.10,6.15,radius)*(1-smoothstep(6.66,6.71,radius))*(1-smoothstep(.01,.025,abs(i.local.z+.12)));
                float panel=abs(frac(angle*12.732395)-.5),panelAA=max(fwidth(panel),.004);
                float joint=1-smoothstep(.008,.008+panelAA,panel);
                float laneAA=max(fwidth(radius),.004),inner=1-smoothstep(.008,.008+laneAA,abs(radius-6.19)),outer=1-smoothstep(.006,.006+laneAA,abs(radius-6.61));
                float flow=.91+.08*sin(angle*4-_Clock*.55);
                metal=lerp(metal,metal*(.82+step(.5,frac(angle*6.366197))*.09),deck);
                metal*=1-deck*joint*.42;
                metal+=deck*(float3(.09,.65,.80)*inner+float3(.75,.43,.15)*outer)*flow;
                float light=step(.8,i.colour.a),phase=.84+.12*sin(_Clock*.72-i.local.z*.45+atan2(i.local.y,i.local.x)*1.8);
                float3 diffuser=i.colour.rgb*phase+float3(.40,.58,.70)*pow(saturate(phase),8)*.19;
                float3 colour=lerp(metal,diffuser,light);
                float distanceFog=saturate((length(_WorldSpaceCameraPos.xyz-i.world)-28)/50)*.36;
                return float4(lerp(colour,_Haze.rgb+_Tint.rgb*.10,distanceFog),1);
            }
            ENDCG
        }
    }
}
