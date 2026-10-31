Shader "SkyPulse/Orbital Atmosphere"
{
    Properties { _Clock("Presentation clock",Float)=0 _Zenith("Deep space",Color)=(.035,.020,.11,1) _Horizon("Horizon atmosphere",Color)=(.035,.15,.30,1) _Warm("Distant sun scatter",Color)=(.70,.30,.12,1) }
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
            float hash(float2 p){p=frac(p*float2(.1031,.1030));p+=dot(p,p.yx+33.33);return frac((p.x+p.y)*p.x);}
            float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
            float3 stars(float2 uv,float scale,float threshold,float strength)
            {
                float2 grid=uv*scale,cell=floor(grid),p=frac(grid)-.5;
                float seed=hash(cell),radius=.018+hash(cell+17.1)*.022;
                float aa=max(length(fwidth(grid))*.30,.008);
                float core=1-smoothstep(radius,radius+aa,length(p));
                float flare=exp2(-dot(p,p)*95)*.13;
                float3 tint=lerp(float3(.45,.70,1),float3(1,.79,.49),hash(cell-9));
                return tint*(core+flare)*step(threshold,seed)*strength;
            }
            float4 frag(vary i):SV_Target
            {
                float3 d=normalize(i.ray);float horizon=exp2(-pow((d.y+.27)*2.4,2));
                float3 colour=lerp(_Zenith.rgb,_Horizon.rgb,horizon*.78);
                float2 p=d.xy*7+float2(d.z*1.2,_Clock*.00013);
                float warp=noise(p*.75+3.8),cloud=noise(p*2.1+warp*2.3)*.68+noise(p*5.3)*.32;
                // A blue/copper dust lane provides scale behind the station.
                // Empty dark sky separates the actual planet silhouettes.
                float lane=exp2(-pow((d.x+d.y*.64+.10)*5,2));
                float filaments=smoothstep(.43,.78,cloud)*lane;
                float dust=1-smoothstep(.36,.56,noise(p*3.7+warp));
                // Blue-violet stellar depth is separate from the warm station
                // light. Narrow structured lanes leave clean sky silhouettes.
                colour+=lerp(float3(.060,.024,.13),float3(.018,.13,.19),saturate(d.y+.55))*filaments*(1-dust*.72);
                float farVeil=exp2(-pow((d.x-d.y*.38-.36)*7.5,2));
                colour+=float3(.029,.018,.075)*farVeil*smoothstep(.38,.70,noise(p*1.1-2.7));
                float warm=exp2(-pow(length(d.xy-float2(-.30,.22))*5.4,2));
                colour+=_Warm.rgb*warm*.20;
                float corridor=exp2(-pow((d.x+d.y*.12)*4.1,2));
                colour+=corridor*float3(.006,.020,.043);
                colour+=stars(d.xy,145,.982,.84)+stars(d.xy+float2(.39,.71),72,.993,.80);
                return float4(colour,1);
            }
            ENDCG
        }
    }
}
