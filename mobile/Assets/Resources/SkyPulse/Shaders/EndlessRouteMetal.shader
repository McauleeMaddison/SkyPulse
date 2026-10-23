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
        _Theme("World structure",Float)=0
        _ThemeAmount("World finish",Float)=0
        _ThemeAccent("Primary inlay",Color)=(.02,.85,1,1)
        _ThemeEnergy("Secondary inlay",Color)=(1,.03,.85,1)
        _MetalTone("Anodised metal",Color)=(.31,.43,.62,1)
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
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct app { float4 vertex:POSITION;float3 normal:NORMAL;fixed4 color:COLOR; };
            struct vary { float4 pos:SV_POSITION;float3 local:TEXCOORD0;float3 normal:TEXCOORD1;float3 world:TEXCOORD2;fixed4 color:COLOR; };
            sampler2D _MainTex;float4 _ArtRect;float _ArtFlip,_Clock,_Cutout,_Visibility,_Theme,_ThemeAmount;fixed4 _LightTint,_ThemeAccent,_ThemeEnergy,_MetalTone;
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.color=v.color;return o;}
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
                float decoration=_ThemeAmount*step(.1,i.color.a);
                clip(lerp(1,art.a-.02,_Cutout*(1-decoration)));
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
                // The original Neon City finish remains exact. Other worlds
                // share its authored metal detail but have matching anodisation,
                // two-colour inlays and their own continuous lighting rhythm.
                if(_ThemeAmount>.5)
                {
                    float luminance=dot(base,float3(.2126,.7152,.0722));
                    base=lerp(base,luminance*_MetalTone.rgb*2.15,.84);
                    ledColour=lerp(_ThemeAccent.rgb,_ThemeEnergy.rgb,pink/(cyan+pink+.001));
                    if(_Theme<1.5)packet=.5+.45*sin(path*3.1-_Clock*.9);
                    else if(_Theme<2.5)packet=pow(saturate(cos(path*5-_Clock*1.8)),5);
                    else if(_Theme<3.5)packet=.5+.42*sin(path*4-_Clock*1.1);
                    else if(_Theme<4.5)packet=.45+.35*sin(_Clock*.75+path*2);
                    else if(_Theme<5.5)packet=pow(saturate(cos(path*8-_Clock*1.3)),6);
                    else if(_Theme<6.5)packet=.45+.4*sin(path*3.7-_Clock*.8);
                    else if(_Theme<7.5)packet=pow(saturate(1-frac(path*2+_Clock*.8)),3);
                    else if(_Theme<8.5)packet=pow(saturate(cos(path*2-_Clock*.55)),10);
                    else if(_Theme<9.5)packet=.25+.65*pow(saturate(sin(path*5-_Clock*1.6)),4);
                    else if(_Theme<10.5)packet=.45+.4*sin(path*2.4-_Clock*.5);
                    else packet=.5+.38*sin(path*2.8-_Clock*.65);
                }
                float3 tint=base*diffuse*lerp(float3(1,1,1),_LightTint.rgb,.15)+_LightTint.rgb*spec*.14;
                // Modulate the diffuser rather than adding light to already-white
                // source pixels: additive saturation hid motion on the iPhone.
                float breathing=.04*sin(_Clock*2.1+path*6.2831853);
                float3 diffuser=base*(.25+packet*.12)+ledColour*(.28+breathing+packet*.42);
                tint=lerp(tint,diffuser,saturate(led*.94));
                float luminous=step(.75,i.color.a);
                float3 relief=_MetalTone.rgb*(.20+diffuse*.39)+_LightTint.rgb*spec*.55;
                relief=lerp(relief,ledColour*(.45+packet*.42)+_LightTint.rgb*spec*.12,luminous);
                tint=lerp(tint,relief,decoration);
                return fixed4(tint,lerp(1,saturate((art.a-.02)/.6),_Cutout*(1-decoration)));
            }
            ENDCG
        }
    }
}
