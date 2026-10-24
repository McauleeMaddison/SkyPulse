Shader "SkyPulse/Orbital Planet"
{
    Properties { _Clock("Presentation clock",Float)=0 _Deep("Deep cloud bands",Color)=(.07,.27,.38,1) _Light("Light cloud bands",Color)=(.39,.58,.62,1) _Rim("Atmosphere rim",Color)=(.19,.51,.64,1) }
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
            float _Clock;float4 _Deep,_Light,_Rim;
            struct app{float4 vertex:POSITION;float3 normal:NORMAL;};
            struct vary{float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;float3 local:TEXCOORD2;};
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.local=v.vertex.xyz;return o;}
            float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
            float noise(float3 p)
            {
                float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);
            }
            float4 frag(vary i):SV_Target
            {
                float3 n=normalize(i.normal),view=normalize(_WorldSpaceCameraPos.xyz-i.world);float diffuse=saturate(dot(n,normalize(float3(-.6,.6,-1))));
                float3 p=normalize(i.local)*5.5;float continent=noise(p*.48)*.87+noise(p*1.7+3.4)*.13;
                float land=smoothstep(.53,.64,continent),coast=1-smoothstep(.008,.035,abs(continent-.56));
                float3 ocean=lerp(float3(.018,.10,.20),_Deep.rgb*.60,continent);
                float3 surface=lerp(ocean,_Light.rgb*float3(.25,.43,.43),land);
                surface+=coast*float3(.012,.035,.04);
                float3 wind=p+float3(_Clock*.0005,3.7,1.9);
                float cloud=noise(wind*1.8+continent*1.7)*.68+noise(wind*4.3)*.32;
                float cloudCover=smoothstep(.49,.81,cloud);
                surface=lerp(surface,float3(.59,.72,.77),cloudCover*.62);
                float facing=saturate(dot(n,view)),rim=pow(1-facing,3.6);
                float twilight=pow(1-diffuse,3)*pow(saturate(diffuse*5),2);
                float city=pow(saturate(noise(p*35)-.62)*2.7,4)*land*(1-cloudCover)*(1-smoothstep(.02,.28,diffuse));
                float3 colour=surface*(.10+diffuse*.84)+_Rim.rgb*rim*.58;
                colour+=float3(.10,.28,.39)*twilight*.18+float3(.85,.48,.16)*city*.18;
                return float4(lerp(colour,float3(.025,.09,.15),.08),1);
            }
            ENDCG
        }
    }
}
