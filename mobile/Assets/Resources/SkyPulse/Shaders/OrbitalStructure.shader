Shader "SkyPulse/Orbital Station Hardware"
{
    Properties { _Tint("Station light",Color)=(.23,.66,.82,1) _Haze("Distance haze",Color)=(.02,.07,.11,1) _Environment("Station reflections",2D)="gray"{} _Clock("Presentation clock",Float)=0 _Apron("Polished arrival floor",Float)=0 _SurfaceTex("Machined alloy surface",2D)="gray"{} _SurfaceAmount("Surface detail",Range(0,1))=0 _SurfaceScale("Surface scale",Float)=.3 }
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
            sampler2D _Environment,_SurfaceTex;float4 _Tint,_Haze;float _Clock,_Apron,_SurfaceAmount,_SurfaceScale;
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
                float3 metal=i.colour.rgb*(.28+ndl*.72)+reflected*(.18+fresnel*.15);
                metal+=float3(.76,.84,.92)*(broad*.38+spec*.72)+_Tint.rgb*fresnel*.10;
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
                float glazing=smoothstep(.28,.32,i.colour.a)*(1-smoothstep(.50,.54,i.colour.a));
                float occupied=smoothstep(.445,.465,i.colour.a)*(1-smoothstep(.495,.515,i.colour.a));
                metal+=glazing*reflected*(.16+fresnel*.22);
                metal+=occupied*i.colour.rgb*(.67+.025*sin(_Clock*.18+floor(i.local.y)*.6));
                if(_SurfaceAmount>.001)
                {
                    float3 axis=pow(abs(mul((float3x3)unity_WorldToObject,n)),4);axis/=max(dot(axis,1),.001);
                    float3 p=i.local*_SurfaceScale;
                    float3 detail=tex2D(_SurfaceTex,p.yz).rgb*axis.x+tex2D(_SurfaceTex,p.xz).rgb*axis.y+tex2D(_SurfaceTex,p.xy).rgb*axis.z;
                    metal=lerp(metal,detail*(.38+ndl*.82)+reflected*.10+float3(.68,.80,.88)*(broad*.28+spec*.52),_SurfaceAmount*(1-glazing));
                }
                if(_Apron>.5&&i.colour.a>.55&&i.colour.a<.70)
                {
                    float3 floorReflection=tex2D(_Environment,float2(saturate(.5+i.local.x*.018),.08+saturate(i.local.z/30)*.17)).rgb;
                    float3 coatedDeck=float3(.12,.19,.24)*(.45+ndl*.65)+floorReflection*.12;
                    metal=lerp(metal,coatedDeck,.60)+float3(.56,.71,.78)*(broad*.12+spec*.22);
                    float panelLine=1-smoothstep(.01,.035,min(abs(frac(i.local.x/3.75)-.5),abs(frac(i.local.z/3.75)-.5)));
                    metal*=1-panelLine*.22;
                }
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
