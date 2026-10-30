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
                // The registered art remains the silhouette. A shallow torso
                // and head relief catches light without moving the flight body.
                float2 torso=(p-float2(.56,.46))*float2(2.1,2.6);
                float2 head=(p-float2(.76,.62))*float2(4.4,4.7);
                float bodyDepth=max(sqrt(saturate(1-dot(torso,torso)))*.034,sqrt(saturate(1-dot(head,head)))*.046);
                v.vertex.z-=(bodyDepth+wing*flex*1.5*_Rig.w)/_RigBounds.z;
                o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.body=p;o.color=v.color*_Color;return o;
            }
            fixed4 frag(vary i):SV_Target
            {
                fixed4 art=tex2D(_MainTex,i.uv)*i.color;
                float3 lum=float3(.2126,.7152,.0722);float2 pixel=_MainTex_TexelSize.xy*1.6;
                float4 right=tex2D(_MainTex,i.uv+float2(pixel.x,0)),left=tex2D(_MainTex,i.uv-float2(pixel.x,0));
                float4 up=tex2D(_MainTex,i.uv+float2(0,pixel.y)),down=tex2D(_MainTex,i.uv-float2(0,pixel.y));
                float2 engraving=float2(dot(right.rgb-left.rgb,lum),dot(up.rgb-down.rgb,lum));
                float2 contour=-float2(right.a-left.a,up.a-down.a);
                // Broad anatomical relief gives light a continuous curved form;
                // the art's local engraving and alpha edges supply finer facets.
                // Neither term changes the original bird's outline or collider.
                float2 torso=(i.body-float2(.56,.46))*float2(2.1,2.6);
                float2 head=(i.body-float2(.76,.62))*float2(4.4,4.7);
                float torsoMask=1-smoothstep(.45,1.08,length(torso));
                float headMask=1-smoothstep(.42,1.05,length(head));
                float2 rounded=lerp(torso*.38*torsoMask,head*.40,headMask);
                float3 n=normalize(float3(rounded-engraving*1.35+contour*.65,-1));
                float3 key=normalize(float3(-.55,.80,-.90)),view=float3(0,0,-1);
                float diffuse=saturate(dot(n,key));
                float spec=pow(saturate(dot(n,normalize(key+view))),38);
                float metallic=smoothstep(.14,.68,dot(art.rgb,lum));
                float fineEdge=saturate(length(engraving)*1.4+length(contour)*.40);
                float facing=saturate(-n.z),rim=pow(1-facing,2.8);
                // Moving reflections follow the existing saved motion setting.
                // They are restrained highlights over the authored feather metal.
                float phase=_Rig.x*_Rig.w;
                float reflection=pow(saturate(1-abs(i.body.x+i.body.y*.30-(.61+sin(phase*.55)*.26))*2),24);
                float3 light=lerp(float3(.72,.85,1),_LightTint.rgb,.38);
                art.rgb*=float3(.84,.87,.91)+float3(.17,.15,.12)*diffuse;
                art.rgb*=lerp(float3(1,1,1),_LightTint.rgb,.055);
                art.rgb+=metallic*(float3(.80,.89,1)*spec*.11+light*(reflection*.045+rim*.10+fineEdge*.035));
                art.rgb*=art.a;return art;
            }
            ENDCG
        }
    }
}
