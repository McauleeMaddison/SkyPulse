Shader "SkyPulse/Original Endless Neon Metal"
{
    Properties
    {
        _MainTex("Original pipe artwork",2D)="white" {}
        _ArtRect("Artwork UV rectangle",Vector)=(0,0,1,1)
        _ArtFlip("Invert upper pipe",Float)=0
        _Cutout("Respect collar silhouette",Float)=0
        _LightTint("World key colour",Color)=(.8,.9,1,1)
        _Clock("Energy clock",Float)=0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Off ZWrite On AlphaToMask On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct app { float4 vertex:POSITION;float3 normal:NORMAL; };
            struct vary { float4 pos:SV_POSITION;float3 local:TEXCOORD0;float3 normal:TEXCOORD1;float3 world:TEXCOORD2; };
            sampler2D _MainTex;float4 _ArtRect;float _ArtFlip,_Clock,_Cutout;fixed4 _LightTint;
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            fixed4 frag(vary i):SV_Target
            {
                float2 uv=float2(i.local.x+.5,lerp(i.local.y+.5,.5-i.local.y,_ArtFlip));
                fixed4 art=tex2D(_MainTex,_ArtRect.xy+saturate(uv)*_ArtRect.zw);
                // The collar is an elliptical cutout. Transparent source pixels
                // must write neither dark colour nor depth at its outer corners.
                // MSAA coverage softens the contour without a black fringe.
                clip(lerp(1,art.a-.02,_Cutout));
                float3 n=normalize(i.normal),v=normalize(_WorldSpaceCameraPos.xyz-i.world),l=normalize(float3(-.5,.7,-1));
                float diffuse=.84+.22*saturate(dot(n,l));
                float spec=pow(saturate(dot(n,normalize(l+v))),100);
                float3 base=lerp(lerp(float3(.035,.05,.085),art.rgb,art.a),art.rgb,_Cutout);
                // Only the original coloured inlays emit: cyan edge rails and
                // pink central/collar strips. A bright packet travels through
                // each diffuser while the underlying rail stays continuously lit.
                float cyan=saturate((min(base.g,base.b)-base.r-.16)*5);
                float pink=saturate((min(base.r,base.b)-base.g-.16)*5);
                float led=max(cyan,pink);
                float packet=pow(saturate(1-abs(frac(uv.y*1.4-_Clock*.48)-.5)*6),3);
                float3 ledColour=lerp(float3(.01,.85,1),float3(1,.03,.85),pink/(cyan+pink+.001));
                float3 tint=base*diffuse*lerp(float3(1,1,1),_LightTint.rgb,.15)+_LightTint.rgb*spec*.14;
                tint+=ledColour*led*(.28+packet*.85);
                return fixed4(tint,lerp(1,saturate((art.a-.02)/.6),_Cutout));
            }
            ENDCG
        }
    }
}
