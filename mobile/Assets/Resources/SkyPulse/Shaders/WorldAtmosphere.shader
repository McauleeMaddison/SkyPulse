Shader "SkyPulse/Constructed World Atmosphere"
{
    Properties { _Tint("World light",Color)=(.2,.6,1,1) _Haze("Horizon",Color)=(.03,.08,.16,1) _Clock("Clock",Float)=0 _World("World",Float)=0 }
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
            struct app {float4 vertex:POSITION;};
            struct vary {float4 pos:SV_POSITION;float3 direction:TEXCOORD0;};
            float4 _Tint,_Haze;float _Clock,_World;
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o;}
            float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
            float noise(float3 p)
            {
                float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);
            }
            fixed4 frag(vary i):SV_Target
            {
                float3 d=normalize(i.direction);float y=saturate(d.y*2+.50);
                float3 upper=lerp(float3(.008,.013,.048),_Tint.rgb,.015);
                float3 horizon=lerp(max(_Haze.rgb,float3(.035,.045,.10)),_Tint.rgb,.065);
                float3 sky=lerp(horizon,upper,smoothstep(.03,.94,y));
                float3 p=d*5+float3(_World*.17,_Clock*.0015,0);
                float cloud=noise(p)*.6+noise(p*2.1)*.28+noise(p*4.2)*.12;
                float band=exp(-pow((d.x+d.y*.65+.08)*4,2));
                sky+=lerp(float3(.17,.09,.36),_Tint.rgb,.2)*smoothstep(.32,.78,cloud)*(.16+band*.38);
                float3 starCell=floor(d*720);float seed=hash(starCell);
                float star=step(.9988,seed)*pow(saturate(1-length(frac(d*720)-.5)*1.8),8);
                sky+=float3(.70,.86,1)*star*.75;
                return fixed4(sky,1);
            }
            ENDCG
        }
    }
}
