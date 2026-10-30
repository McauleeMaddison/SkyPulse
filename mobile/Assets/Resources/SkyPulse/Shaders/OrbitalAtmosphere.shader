Shader "SkyPulse/Orbital Atmosphere"
{
    Properties { _Clock("Presentation clock",Float)=0 _Zenith("Deep space",Color)=(.025,.075,.12,1) _Horizon("Horizon haze",Color)=(.10,.25,.33,1) _Warm("Warm station glow",Color)=(.22,.13,.055,1) }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Opaque"}
        Pass
        {
            Cull Off ZWrite Off ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float _Clock;float4 _Zenith,_Horizon,_Warm;
            struct vary{float4 pos:SV_POSITION;float3 ray:TEXCOORD0;};
            vary vert(float4 vertex:POSITION){vary o;o.pos=UnityObjectToClipPos(vertex);o.ray=vertex.xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
            float4 frag(vary i):SV_Target
            {
                float3 d=normalize(i.ray);float horizon=pow(saturate(1-abs(d.y+.19)),4);
                float3 colour=lerp(_Zenith.rgb,_Horizon.rgb,horizon);
                // A quiet illuminated centre separates the active flight lane
                // from darker structures at the edges of the station.
                float corridor=exp2(-pow((d.x+d.y*.12)*3.8,2));
                colour+=corridor*float3(.015,.038,.048);
                float2 p=d.xy*5+float2(d.z*2,_Clock*.0007);float mist=noise(p+noise(p*.55)*1.2);
                colour+=pow(saturate(mist-.20),2)*float3(.07,.19,.24);
                float shaft=pow(saturate(.5+.5*sin(d.x*22+d.y*8+d.z*4)),18)*pow(saturate(-d.y+.6),2);
                colour+=shaft*float3(.012,.035,.045);
                float warm=pow(saturate(dot(d,normalize(float3(-.7,-.35,1)))),18);
                colour+=warm*_Warm.rgb;
                // Layered, slowly drifting dust wisps add scale behind the
                // modelled station; bright navigation lenses are separate.
                float filament=noise(d.xy*16+noise(d.xy*7+d.z)*2+float2(_Clock*.0004,0));
                float band=exp2(-pow((d.x+d.y*.42-.12)*4.5,2));
                colour+=smoothstep(.42,.81,filament)*band*float3(.030,.085,.12);
                float fineDust=pow(noise(d.xy*52+d.z*9),6);
                colour+=fineDust*band*float3(.08,.15,.18);
                float2 grid=d.xy*190;float2 cell=floor(grid);float star=step(.991,hash(cell));float distance=length(frac(grid)-.5);
                colour+=star*exp2(-distance*distance*180)*float3(.35,.59,.68)*(1-horizon*.45);
                return float4(colour,1);
            }
            ENDCG
        }
    }
}
