Shader "SkyPulse/Constructed World Atmosphere"
{
    Properties { _Tint("World light",Color)=(.2,.6,1,1) _Haze("Horizon",Color)=(.03,.08,.16,1) _Clock("Clock",Float)=0 _World("World",Float)=0 _SkyA("Outgoing sky art",2D)="gray"{} _SkyB("Incoming sky art",2D)="gray"{} _Blend("Sky transition",Float)=1 }
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
            float4 _Tint,_Haze;float _Clock,_World,_Blend;sampler2D _SkyA,_SkyB;
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
                // Only the original sky is mapped onto the enclosing volume.
                // Its illustrated buildings are cropped out: inhabited islands
                // in front are real meshes with independent depth and travel.
                float2 uv=float2(.5+d.x*2.7,.44+d.y*1.24);
                uv+=float2(sin(_Clock*.017)*.006,cos(_Clock*.013)*.004);
                uv.y=clamp(uv.y,.30,.985);uv.x=saturate(uv.x);
                float3 sky=lerp(tex2D(_SkyA,uv).rgb,tex2D(_SkyB,uv).rgb,_Blend)*.90;
                float3 p=d*5+float3(_World*.17,_Clock*.0015,0);
                float cloud=noise(p)*.6+noise(p*2.1)*.28+noise(p*4.2)*.12;
                float band=exp(-pow((d.x+d.y*.65+.08)*4,2));
                sky+=lerp(float3(.28,.055,.43),_Tint.rgb,.15)*smoothstep(.38,.86,cloud)*(.06+band*.15);
                float sunset=exp(-pow((d.y+.11)*9,2));
                sky+=lerp(float3(.95,.25,.18),_Tint.rgb,.18)*sunset*.27;
                float sea=1-smoothstep(-.18,.045,d.y);
                float billow=noise(float3(d.x*29,d.y*36,_Clock*.009))* .6+noise(float3(d.x*57,d.y*70,4))*.4;
                sky=lerp(sky,lerp(float3(.15,.075,.32),float3(.63,.25,.52),billow)+float3(.45,.22,.12)*sunset,sea);
                float3 starCell=floor(d*720);float seed=hash(starCell);
                float star=step(.9988,seed)*pow(saturate(1-length(frac(d*720)-.5)*1.8),8);
                sky+=float3(.70,.86,1)*star*.75;
                return fixed4(sky,1);
            }
            ENDCG
        }
    }
}
