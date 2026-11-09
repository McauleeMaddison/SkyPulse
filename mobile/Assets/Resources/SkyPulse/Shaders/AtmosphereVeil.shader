Shader "SkyPulse/Atmospheric Depth Veil"
{
    Properties { _Tint("World light",Color)=(.2,.7,1,1) _Haze("World haze",Color)=(.05,.10,.18,1) _Visibility("World fade",Float)=1 _Clock("Presentation clock",Float)=0 _Origin("Scene origin",Vector)=(0,0,0,0) }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float4 _Tint,_Haze,_Origin;float _Visibility,_Clock;
            struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
            vary vert(float4 vertex:POSITION,float2 uv:TEXCOORD0){vary o;o.pos=UnityObjectToClipPos(vertex);o.uv=uv;o.world=mul(unity_ObjectToWorld,vertex).xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
            float4 frag(vary i):SV_Target
            {
                float feather=smoothstep(0,.12,i.uv.y)*(1-smoothstep(.58,1,i.uv.y))*smoothstep(0,.10,i.uv.x)*(1-smoothstep(.90,1,i.uv.x));
                clip(feather-.001);
                float2 p=(i.world.xy-_Origin.xy)*float2(.095,.27)+float2(_Clock*.0035,0);
                float curl=noise(p*.61+float2(0,_Clock*.0008));
                float2 warped=p+float2(curl*.56,noise(p*.81+3.7)*.21);
                float body=noise(warped),detail=noise(warped*3.19+float2(.7,-1.3));
                // Narrow torn filaments drift between physical depths. They
                // have illuminated edges and fully transparent openings rather
                // than multiplying a flat, broad fog sheet over the scenery.
                float pocket=smoothstep(.43,.75,body*.69+detail*.31);
                float thread=pow(saturate(1-abs(detail-.52)*3.8),2);
                float density=pocket*lerp(.28,1,thread);
                float crown=saturate((body-noise(warped+float2(-.08,.19)))*4);
                float3 shadow=float3(.021,.041,.075)+_Haze.rgb*.12;
                float3 light=float3(.27,.39,.53)+_Tint.rgb*.07;
                float3 colour=lerp(shadow,light,crown*.78+thread*.10);
                return float4(colour,feather*density*.13*_Visibility);
            }
            ENDCG
        }
    }
}
