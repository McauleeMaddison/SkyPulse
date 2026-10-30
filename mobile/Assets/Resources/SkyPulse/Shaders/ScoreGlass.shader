Shader "SkyPulse/Holographic Score Glass"
{
    Properties
    {
        [PerRendererData] _MainTex("UI texture",2D)="white" {}
        _Clock("Light clock",Float)=0
        _Motion("Motion enabled",Float)=1
        _HomeFinish("Open Home glass",Float)=0
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Alpha clip",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct app { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 extent:TEXCOORD1; };
            struct vary { float4 pos:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float2 extent:TEXCOORD1; float4 local:TEXCOORD2; };
            float4 _ClipRect;float _Clock,_Motion,_HomeFinish;
            vary vert(app v) { vary o;o.local=v.vertex;o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;o.extent=v.extent.xy;return o; }
            fixed4 frag(vary i):SV_Target
            {
                float2 size=max(i.extent,1),p=(i.uv-.5)*size;
                float radius=min(lerp(28,44,_HomeFinish),min(size.x,size.y)*lerp(.15,.23,_HomeFinish));
                float2 q=abs(p)-(size*.5-radius-1.5);
                float sd=length(max(q,0))+min(max(q.x,q.y),0)-radius;
                float aa=max(fwidth(sd),.65),inside=1-smoothstep(-aa,aa,sd);
                float rim=1-smoothstep(1.15,2.7,abs(sd+1.5));
                float inset=(1-smoothstep(.8,1.8,abs(sd+7)))*.12;
                float edgeLight=.48+.52*saturate(i.uv.y);
                float top=pow(saturate(i.uv.y),7);
                // Broad reflected light moves slowly, never across text opacity.
                float sweep=pow(saturate(1-abs(i.uv.x+i.uv.y*.24-(frac(_Clock*.075)*1.8-.35))*5),4)*_Motion;
                float3 accent=i.color.rgb;
                float3 base=lerp(float3(.016,.034,.065),float3(.052,.092,.135),i.uv.y)+accent*(.065+top*.10);
                float3 spectrum=lerp(accent,float3(.72,.83,1),i.uv.x*.65);
                base+=spectrum*(rim*edgeLight*.82+inset)+float3(.68,.89,1)*(top*.035+sweep*.022);
                base+=spectrum*sweep*rim*.25;
                fixed4 c=fixed4(base,inside*(lerp(.91,.72,_HomeFinish)+rim*.07)*i.color.a);
                if(_HomeFinish>.5)
                {
                    // Home actions have a single fine optical edge. The launch
                    // face is denser and softly bevelled; secondary controls stay
                    // quiet instead of repeating the same bright double outline.
                    float prominence=smoothstep(.80,1.0,i.color.a);
                    float homeRim=1-smoothstep(.55,1.5,abs(sd+1.0));
                    float crest=pow(saturate(i.uv.y),9);
                    float foot=pow(saturate(1-i.uv.y),11);
                    float3 face=lerp(float3(.012,.025,.038),float3(.044,.085,.105),i.uv.y)
                        +accent*(.018+prominence*.10);
                    face-=foot*.010;
                    face+=spectrum*homeRim*(.16+prominence*.46)*(.32+.68*i.uv.y);
                    face+=float3(.72,.91,1)*(crest*(.018+prominence*.055)+sweep*.012);
                    c=fixed4(face,inside*(.66+prominence*.25)*i.color.a);
                }
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
