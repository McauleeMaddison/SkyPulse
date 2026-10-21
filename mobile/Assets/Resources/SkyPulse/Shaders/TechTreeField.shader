Shader "SkyPulse/Tech Tree Field"
{
    Properties
    {
        [PerRendererData] _MainTex("Tree",2D)="white" {}
        _Backdrop("Atmosphere",Float)=0
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct app { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct vary { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; fixed4 color:COLOR; };
            sampler2D _MainTex; float _Backdrop; float4 _ClipRect;
            vary vert(app v) { vary o; o.local=v.vertex; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(vary i):SV_Target
            {
                // Full-bleed, quiet midnight glass; there is no inset backplate.
                float pool=exp(-dot((i.uv-float2(.5,.54))*float2(2.5,1.6),(i.uv-float2(.5,.54))*float2(2.5,1.6))*3);
                float3 atmosphere=lerp(float3(.03,.055,.09),float3(.065,.13,.18),pool*.62);
                fixed4 tree=tex2D(_MainTex,i.uv);
                float fade=smoothstep(0,.045,min(i.uv.x,1-i.uv.x))*smoothstep(0,.035,min(i.uv.y,1-i.uv.y));
                // Camera colour is already premultiplied; do not darken its
                // fine edges a second time when compositing onto the UI.
                fixed4 output=lerp(tree*fade,fixed4(atmosphere,1),_Backdrop);
                output.rgb*=i.color.rgb*i.color.a; output.a*=i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                output*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                return output;
            }
            ENDCG
        }
    }
}
