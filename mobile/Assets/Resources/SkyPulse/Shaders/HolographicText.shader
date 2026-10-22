Shader "SkyPulse/Holographic Lettering"
{
    Properties
    {
        [PerRendererData] _MainTex("Font atlas",2D)="white" {}
        _Clock("Light clock",Float)=0
        _Motion("Motion enabled",Float)=1
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
            struct app { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 optical:TEXCOORD1; };
            struct vary { float4 pos:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 optical:TEXCOORD1; float4 local:TEXCOORD2; };
            sampler2D _MainTex;fixed4 _TextureSampleAdd;float4 _ClipRect;float _Clock,_Motion;
            vary vert(app v) { vary o;o.local=v.vertex;o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;o.optical=v.optical;return o; }
            fixed4 frag(vary i):SV_Target
            {
                fixed4 c=(tex2D(_MainTex,i.uv)+_TextureSampleAdd)*i.color;
                // Crisp, fully readable letters throughout the sweep. Spectral
                // light changes their finish, never their opacity or position.
                float y=i.optical.y,e=i.optical.z;
                float bevel=pow(saturate(1-abs(y-.78)*9),3);
                float sweep=pow(saturate(1-abs(i.optical.x+y*.22-(frac(_Clock*.14)*1.7-.35))*9),3)*_Motion;
                float3 spectral=lerp(float3(.74,.89,1),float3(1,.92,1),y);
                c.rgb=c.rgb*spectral*(.88+y*.14)+float3(.54,.85,1)*(bevel*.10+sweep*.25)*e;
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
