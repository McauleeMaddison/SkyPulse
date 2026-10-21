Shader "SkyPulse/Mechanical Bird"
{
    Properties
    {
        [PerRendererData] _MainTex ("Bird",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        _Rig ("Clock, impulse, glide, motion",Vector)=(0,0,0,1)
        _RigBounds ("Local sprite bounds",Vector)=(0,0,1,1)
        _LightTint ("Environment light",Color)=(.3,.8,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Pass
        {
            Cull Off ZWrite Off Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _MainTex_TexelSize,_Color,_Rig,_RigBounds,_LightTint;
            struct app { float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
            struct vary { float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 body:TEXCOORD1;fixed4 color:COLOR; };
            vary vert(app v)
            {
                vary o;float2 p=(v.vertex.xy-_RigBounds.xy)*_RigBounds.zw;
                float tail=(1-smoothstep(.03,.34,p.x))*(1-smoothstep(.53,.82,p.y));
                float wing=(1-smoothstep(.43,.66,p.x))*smoothstep(.49,.91,p.y);
                float flex=sin(_Rig.x*7-p.x*5)*.012+_Rig.y*.018;
                v.vertex.y+=(tail*(sin(_Rig.x*8-p.x*6)*.018-_Rig.z*.016)+wing*flex)*_Rig.w/_RigBounds.w;
                v.vertex.x+=tail*wing*sin(_Rig.x*6)*.007*_Rig.w/_RigBounds.z;
                float bodyDepth=sqrt(saturate(1-dot((p-float2(.57,.46))*float2(1.8,2.4),(p-float2(.57,.46))*float2(1.8,2.4))));
                v.vertex.z-=(bodyDepth*.035+wing*flex*1.5*_Rig.w)/_RigBounds.z;
                o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.body=p;o.color=v.color*_Color;return o;
            }
            fixed4 frag(vary i):SV_Target
            {
                fixed4 art=tex2D(_MainTex,i.uv)*i.color;
                float3 lum=float3(.2126,.7152,.0722);float2 pixel=_MainTex_TexelSize.xy*1.6;
                float dx=dot(tex2D(_MainTex,i.uv+float2(pixel.x,0)).rgb-tex2D(_MainTex,i.uv-float2(pixel.x,0)).rgb,lum);
                float dy=dot(tex2D(_MainTex,i.uv+float2(0,pixel.y)).rgb-tex2D(_MainTex,i.uv-float2(0,pixel.y)).rgb,lum);
                float3 n=normalize(float3(-dx*2,-dy*2,-1));float3 key=normalize(float3(-.55,.8,-.9));
                float spec=pow(saturate(dot(n,normalize(key+float3(0,0,-1)))),45);
                float metallic=smoothstep(.16,.72,dot(art.rgb,lum));
                float sweep=pow(saturate(1-abs(i.body.x+i.body.y*.35-(.6+sin(_Rig.x*.65)*.45))),18);
                art.rgb=art.rgb*(.88+.12*saturate(dot(n,key)))+_LightTint.rgb*metallic*(spec*.10+sweep*.055);
                art.rgb*=art.a;return art;
            }
            ENDCG
        }
    }
}
