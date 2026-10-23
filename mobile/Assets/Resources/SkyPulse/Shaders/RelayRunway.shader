Shader "SkyPulse/Relay Runway"
{
    Properties
    {
        _Distance("Course distance",Float)=0 _Clock("Clock",Float)=0
        _PanelSpan("Pooled panel span",Float)=20
        _Environment("Station reflection",2D)="gray"{}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Opaque" }
        Pass
        {
            Cull Off ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct input {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float2 tile:TEXCOORD1;};
            struct varying {float4 position:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;};
            float _Distance,_Clock,_PanelSpan;sampler2D _Environment;
            varying vert(input i)
            {
                varying o;float centre=frac((i.tile.x-_Distance+_PanelSpan*.5)/_PanelSpan)*_PanelSpan-_PanelSpan*.5;
                i.vertex.x+=(centre-i.tile.x)*i.tile.y;
                o.position=UnityObjectToClipPos(i.vertex);o.world=mul(unity_ObjectToWorld,i.vertex).xyz;o.normal=UnityObjectToWorldNormal(i.normal);o.uv=i.uv;return o;
            }
            fixed4 frag(varying i):SV_Target
            {
                float3 n=normalize(i.normal),v=normalize(_WorldSpaceCameraPos.xyz-i.world);if(dot(n,v)<0)n=-n;
                float3 key=normalize(float3(-.5,.8,-1)),reflection=reflect(-v,n);
                float diffuse=.55+.6*saturate(dot(n,key)),spec=pow(saturate(dot(n,normalize(key+v))),94);
                float softbox=pow(saturate(1-abs(reflection.x*.7+reflection.y*.5-.17)*2.5),16);
                float3 reflected=tex2D(_Environment,saturate(reflection.xy*.31+float2(.5,.43))).rgb;
                float3 metal=lerp(float3(.105,.17,.205)*diffuse,reflected*.48,.28)+float3(.7,.86,1)*(spec*.42+softbox*.10);
                float depth=i.uv.y,x=i.world.x+_Distance;
                if(depth>0)
                {
                    float3 floorReflection=tex2D(_Environment,float2(saturate(.5+i.world.x*.018),.18+saturate(depth)*.20)).rgb;
                    metal=lerp(metal,floorReflection*.52,.34)+float3(.08,.16,.21)*pow(saturate(1-abs(depth-.72)*2.3),8);
                }
                float seam=1-smoothstep(.008,.018,min(frac(x/.1),1-frac(x/.1)));
                // Fine brushed finish and a restrained recessed inset, not a
                // flat construction stripe. Normal-based light reveals bevels.
                metal*=1-seam*.025;
                float inset=(1-smoothstep(.012,.035,abs(depth-.58)))*step(0,depth);
                metal*=1-inset*.28;
                float guide=(1-smoothstep(.008,.020,abs(depth-.18)))*step(0,depth);
                float packet=.55+.25*pow(saturate(cos(i.world.x*1.6+_Clock*1.8)),14);
                metal+=float3(.10,.64,.78)*guide*packet;
                float lip=1-smoothstep(.004,.014,abs(depth));
                float docking=pow(saturate(cos(x*2.2)),32);
                metal=lerp(metal,lerp(float3(.18,.76,.94),float3(.85,.91,.96),docking)*(.75+packet*.15),lip);
                if(depth<-.02)metal=metal*.76+float3(.02,.05,.07)*pow(saturate(1-abs(depth+.42)*12),4);
                return fixed4(metal,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
