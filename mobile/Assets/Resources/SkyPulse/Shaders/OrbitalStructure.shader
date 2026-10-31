Shader "SkyPulse/Orbital Station Hardware"
{
    Properties { _Tint("Station light",Color)=(.23,.66,.82,1) _Haze("Distance haze",Color)=(.02,.07,.11,1) _Environment("Station reflections",2D)="gray"{} _Clock("Presentation clock",Float)=0 _Apron("Polished arrival floor",Float)=0 _SurfaceTex("Machined alloy surface",2D)="gray"{} _SurfaceAmount("Surface detail",Range(0,1))=0 _SurfaceScale("Surface scale",Float)=.3 _LightDirection("Shared station sun",Vector)=(-.66,.58,-.42,0) }
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
            sampler2D _Environment,_SurfaceTex;float4 _Tint,_Haze,_LightDirection;float _Clock,_Apron,_SurfaceAmount,_SurfaceScale;
            struct app {float4 vertex:POSITION;float3 normal:NORMAL;float4 colour:COLOR;};
            struct vary {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;float3 local:TEXCOORD2;float4 colour:COLOR;};
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.local=v.vertex.xyz;o.colour=v.colour;return o;}
            float4 frag(vary i):SV_Target
            {
                float3 n=normalize(i.normal),view=normalize(_WorldSpaceCameraPos.xyz-i.world);if(dot(n,view)<0)n=-n;
                float3 key=normalize(_LightDirection.xyz),reflection=reflect(-view,n);
                float ndl=saturate(dot(n,key)),fresnel=pow(1-saturate(dot(n,view)),4);
                float broad=pow(saturate(1-abs(reflection.x*.75+reflection.y*.3-.18)*1.8),12);
                float spec=pow(saturate(dot(n,normalize(key+view))),54);
                float3 reflected=tex2D(_Environment,saturate(reflection.xy*.28+float2(.5,.42))).rgb;
                float fill=saturate(dot(n,normalize(float3(.70,.25,-.65))));
                float skyFill=saturate(n.y*.5+.5);
                // Warm solid alloy faces, a teal side fill and blue-violet
                // distance keep the station from becoming one grey material.
                float3 lighting=float3(.25,.28,.35)+float3(1,.86,.66)*ndl*.92+float3(.10,.40,.47)*fill*.40;
                lighting+=float3(.045,.070,.14)*skyFill;
                float3 metal=i.colour.rgb*lighting+reflected*(.055+fresnel*.075);
                metal+=float3(.60,.73,.84)*(broad*.14+spec*.24)+_Tint.rgb*fresnel*.08;
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
                metal+=deck*(float3(.06,.75,.84)*inner+float3(.84,.45,.15)*outer)*flow;
                float glazing=smoothstep(.28,.32,i.colour.a)*(1-smoothstep(.50,.54,i.colour.a));
                float occupied=smoothstep(.445,.465,i.colour.a)*(1-smoothstep(.495,.515,i.colour.a));
                // Framed windows reflect the station; opaque structural alloy
                // keeps its own colour and roughness rather than reading as glass.
                metal+=glazing*reflected*(.19+fresnel*.24);
                float grazing=pow(1-saturate(dot(n,view)),5);
                metal+=glazing*float3(.09,.20,.29)*grazing;
                float edgeFinish=smoothstep(.06,.10,i.colour.a)*(1-smoothstep(.22,.28,i.colour.a));
                metal+=edgeFinish*float3(.27,.35,.43)*(spec*.16+broad*.08);
                metal+=occupied*i.colour.rgb*(.74+.018*sin(_Clock*.18+floor(i.local.y)*.6));
                if(_SurfaceAmount>.001)
                {
                    float3 axis=pow(abs(mul((float3x3)unity_WorldToObject,n)),4);axis/=max(dot(axis,1),.001);
                    float3 p=i.local*_SurfaceScale;
                    float3 detail=tex2D(_SurfaceTex,p.yz).rgb*axis.x+tex2D(_SurfaceTex,p.xz).rgb*axis.y+tex2D(_SurfaceTex,p.xy).rgb*axis.z;
                    // Preserve the modelled frame colour and broad lighting. Fine
                    // alloy variation must not turn every deck into busy tiles.
                    float grain=dot(detail,float3(.2126,.7152,.0722));
                    metal*=lerp(1,.86+grain*.25,_SurfaceAmount*(1-glazing));
                    float3 detailedFinish=detail*(.38+ndl*.82)+reflected*.10+float3(.54,.69,.80)*(broad*.12+spec*.22);
                    metal=lerp(metal,detailedFinish,_SurfaceAmount*.38*(1-glazing));
                }
                if(_Apron>.5&&i.colour.a>.55&&i.colour.a<.70)
                {
                    float3 floorReflection=tex2D(_Environment,float2(saturate(.5+i.local.x*.018),.08+saturate(i.local.z/30)*.17)).rgb;
                    float apronDepth=saturate((i.local.z+2)/30);
                    float3 floorColour=lerp(float3(.22,.25,.30),float3(.10,.19,.29),apronDepth);
                    float3 coatedDeck=floorColour*(.66+ndl*.57)+floorReflection*.065;
                    metal=lerp(metal,coatedDeck,.60)+float3(.46,.62,.73)*(broad*.06+spec*.12);
                    // Align seams to the real tile boundaries, not a second
                    // grid through each panel's centre. Inlaid runway lanes
                    // and quiet cross-strip markers give the floor a purpose.
                    float2 tile=frac(float2(i.local.x,i.local.z+.625)/3.75);
                    float2 edge=min(tile,1-tile);
                    float panelLine=1-smoothstep(.002,.010+max(fwidth(tile.x),fwidth(tile.y)),min(edge.x,edge.y));
                    metal*=1-panelLine*.26;
                    float laneDistance=abs(abs(i.local.x)-4.60);
                    float lane=1-smoothstep(.012,.030+fwidth(i.local.x),laneDistance);
                    float zone=smoothstep(-2.4,-1.9,i.local.z)*(1-smoothstep(27,27.5,i.local.z));
                    float pace=.88+.10*sin(_Clock*.60-i.local.z*.27);
                    metal+=lane*zone*float3(.035,.54,.66)*pace;
                    float markerZ=abs(frac((i.local.z+1.1)/3.75)-.5);
                    float marker=1-smoothstep(.025,.045+fwidth(markerZ),markerZ);
                    float sideStrip=smoothstep(5.2,5.35,abs(i.local.x))*(1-smoothstep(5.9,6.05,abs(i.local.x)));
                    metal+=marker*sideStrip*zone*float3(.62,.37,.16)*.34;
                }
                float light=step(.8,i.colour.a),phase=.84+.12*sin(_Clock*.72-i.local.z*.45+atan2(i.local.y,i.local.x)*1.8);
                float3 diffuser=i.colour.rgb*phase+float3(.24,.42,.58)*pow(saturate(phase),8)*.10;
                float3 colour=lerp(metal,diffuser,light);
                // Depth separation is shared by the whole station, including
                // its physical bridges. Near hardware retains crisp edges.
                float distanceFog=saturate((length(_WorldSpaceCameraPos.xyz-i.world)-27)/62)*.49;
                float3 distanceLight=_Haze.rgb+_Tint.rgb*.105;
                colour=lerp(colour,distanceLight,distanceFog);
                // Roll off bright optical cores without washing broad metal
                // planes into flat white shapes on the non-HDR mobile target.
                colour/=1+max(colour-.60,0)*.78;
                return float4(colour,1);
            }
            ENDCG
        }
    }
}
