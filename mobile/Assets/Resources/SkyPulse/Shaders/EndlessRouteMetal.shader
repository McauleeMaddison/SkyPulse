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
        _Visibility("World departure coverage",Range(0,1))=1
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
            sampler2D _MainTex;float4 _ArtRect;float _ArtFlip,_Clock,_Cutout,_Visibility;fixed4 _LightTint;
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            fixed4 frag(vary i):SV_Target
            {
                // Stable screen-door coverage fades opaque depth-writing meshes
                // with the old scenery, including on devices without MSAA.
                float2 cell=fmod(floor(i.pos.xy),4);
                float2 low=fmod(cell,2),high=floor(cell/2);
                float threshold=(4*(2*low.x+3*low.y-4*low.x*low.y)+(2*high.x+3*high.y-4*high.x*high.y)+.5)/16;
                clip(_Visibility-threshold);
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
                // World-distance packets keep long and short columns consistent;
                // the collar instead carries a highlight around its elliptical rim.
                float shaftPath=lerp(i.world.y,-i.world.y,_ArtFlip)*.48;
                float rimPath=atan2((uv.y-.75)*3.4,uv.x-.5)*.15915494+.5;
                float path=lerp(shaftPath,rimPath,_Cutout);
                float packet=pow(saturate(1-abs(frac(path-_Clock*.55)-.5)*5),2);
                float3 ledColour=lerp(float3(.01,.85,1),float3(1,.03,.85),pink/(cyan+pink+.001));
                float3 tint=base*diffuse*lerp(float3(1,1,1),_LightTint.rgb,.15)+_LightTint.rgb*spec*.14;
                // Modulate the diffuser rather than adding light to already-white
                // source pixels: additive saturation hid motion on the iPhone.
                float breathing=.04*sin(_Clock*2.1+path*6.2831853);
                float3 diffuser=base*(.25+packet*.12)+ledColour*(.28+breathing+packet*.42);
                tint=lerp(tint,diffuser,saturate(led*.94));
                return fixed4(tint,lerp(1,saturate((art.a-.02)/.6),_Cutout));
            }
            ENDCG
        }
    }
}
