Shader "SkyPulse/Atmospheric LED Stars"
{
    Properties { _Tint("Primary LED",Color)=(.1,.8,1,1) _Secondary("Secondary LED",Color)=(1,.1,.7,1) _Visibility("World fade",Float)=1 _Clock("Presentation clock",Float)=0 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float4 _Tint,_Secondary;float _Visibility,_Clock;
            struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 colour:COLOR;};
            struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 colour:COLOR;};
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.colour=v.colour;return o;}
            float4 frag(vary i):SV_Target
            {
                float2 p=(i.uv-.5)*3.4;float radius=length(p),angle=atan2(p.y,p.x)-1.5707963;
                // Five-point LED lens with a bright inset centre and a soft,
                // transparent aura. No opaque square or texture backplate.
                float sector=floor(angle/.62831853),phase=angle-sector*.62831853;
                float outer=lerp(.80,.34,fmod(abs(sector),2)),inner=1.14-outer;
                float outline=outer*inner*.58778525/max(.001,inner*sin(.62831853-phase)+outer*sin(phase));
                float edge=radius-outline,aa=max(fwidth(edge),.018);
                float lens=1-smoothstep(-aa,aa,edge);float bezel=exp2(-abs(edge)*30);
                float halo=exp2(-max(edge,0)*max(edge,0)*12)*(1-smoothstep(1.35,1.65,radius));
                float core=exp2(-radius*radius*26),pulse=.93+.05*sin(_Clock*.65+i.colour.g*6.28);
                float3 led=lerp(_Tint.rgb,_Secondary.rgb,i.colour.r);
                float3 colour=led*(lens*.72+halo*.18)+float3(.7,.85,1)*(bezel*.13+core*.6);
                return float4(colour,pulse*_Visibility*saturate(lens*.8+halo*.28));
            }
            ENDCG
        }
    }
}
