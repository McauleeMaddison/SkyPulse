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
            float3 hardware(vary i,float2 uv)
            {
                float3 n=normalize(i.normal),view=unity_OrthoParams.w>.5?float3(0,0,-1):normalize(_WorldSpaceCameraPos.xyz-i.world);
                if(dot(n,view)<0)n=-n;
                float3 key=normalize(float3(-.65,.78,-.83)),r=reflect(-view,n);
                float ndl=saturate(dot(n,key)),fresnel=pow(1-saturate(dot(n,view)),5);
                float broad=pow(saturate(1-abs(r.x*.83+r.y*.22-.20)*1.50),9);
                float strip=pow(saturate(1-abs(r.x+.39)*1.8),38);
                float spec=pow(saturate(dot(n,normalize(key+view))),96);
                float seal=step(.43,i.color.a)*(1-step(.73,i.color.a)),trim=step(.12,i.color.a)*(1-step(.43,i.color.a)),led=step(.80,i.color.a);
                float3 colour=_MetalTone.rgb*(.29+ndl*.69)+float3(.11,.16,.22)*(broad*.74+fresnel*.42);
                colour+=float3(.79,.88,1)*(strip*.34+spec*.51);
                colour=lerp(colour,colour*1.12+float3(.11,.13,.16)*(broad*.75+spec),trim);
                colour=lerp(colour,float3(.018,.028,.041)*(1+ndl*.3)+spec*.05,seal);
                float grain=sin(i.world.y*228)*sin(i.local.x*316);float aa=saturate(1-fwidth(i.world.y*228));colour*=1+grain*.008*aa;
                float path=lerp(i.world.y,-i.world.y,_ArtFlip)*.55;
                float speed=.48+frac(_Theme*.137)*.35;
                float packet=pow(saturate(1-abs(frac(path-_Clock*speed)-.5)*3.3),3);
                if(_Theme<1.5)packet=.5+.40*sin(path*2.8-_Clock*.80);
                else if(_Theme<2.5)packet=smoothstep(.25,.85,cos(path*5.1-_Clock*1.5));
                else if(_Theme<3.5)packet=.5+.38*sin(path*3.4-_Clock*.94);
                else if(_Theme<4.5)packet=.40+.24*sin(_Clock*.72+path*1.6);
                else if(_Theme<5.5)packet=pow(saturate(cos(path*7.8-_Clock*1.2)),6);
                else if(_Theme<6.5)packet=.42+.35*sin(path*3.7-_Clock*.78);
                else if(_Theme<7.5)packet=pow(1-frac(path*1.8+_Clock*.72),3);
                else if(_Theme<8.5)packet=pow(saturate(cos(path*2-_Clock*.45)),8);
                else if(_Theme<9.5)packet=.18+.70*pow(saturate(sin(path*4.8-_Clock*1.5)),4);
                else if(_Theme<10.5)packet=.44+.32*sin(path*2.4-_Clock*.45);
                else packet=.5+.34*sin(path*2.8-_Clock*.56)*cos(path*.6+_Clock*.23);
                float3 energy=lerp(_ThemeAccent.rgb,_ThemeEnergy.rgb,saturate((i.local.x+.4)*.8));
                float rails=(_Theme>1.5&&_Theme<2.5)||(_Theme>8.5&&_Theme<9.5)?6:(_Theme>4.5&&_Theme<5.5)||_Theme>10.5?3:4;
                float angle=atan2(i.local.z,i.local.x);
                float angularDistance=abs(frac(angle*rails/6.2831853)-.5)*6.2831853/rails;
                float channelAA=max(fwidth(angularDistance),.002);
                float lens=(1-smoothstep(.043,.063+channelAA,angularDistance))*(1-_Cutout);
                float channelGlow=(1-smoothstep(.055,.14+channelAA,angularDistance))*(1-_Cutout);
                colour+=energy*channelGlow*.065;
                colour=lerp(colour,energy*(.43+packet*.52)+float3(.6,.8,1)*spec*.14,max(led,lens));
                // The cutout follows the existing tested lip contour. Its shaded
                // well, rolled rim and side housing use independent materials.
                if(_Cutout>.5)
                {
                    float2 ellipse=(uv-float2(.5,.745))/float2(.46,.225);
                    float d=length(ellipse);float inside=1-smoothstep(.87,1.03,d);
                    float rolled=exp(-pow((d-1.03)*15,2));
                    float rimLight=.42+.43*pow(saturate(1-abs(ellipse.x+.35)*.75),6);
                    colour=lerp(colour,float3(.015,.028,.046)+_ThemeAccent.rgb*.025,inside*.94);
                    colour+=rolled*(float3(.26,.34,.44)*rimLight+_ThemeAccent.rgb*(.31+packet*.22));
                }
                return colour*lerp(float3(1,1,1),_LightTint.rgb,.05);
            }
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
                float capCoverage=_ThemeAmount>.5?_Cutout:_Cutout*(1-decoration);
                clip(lerp(1,art.a-.02,capCoverage));
                if(_ThemeAmount>.5)return fixed4(hardware(i,uv),lerp(1,saturate((art.a-.02)/.6),capCoverage));
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
                float3 tint=base*diffuse*lerp(float3(1,1,1),_LightTint.rgb,.15)+_LightTint.rgb*spec*.14;
                float3 reflected=reflect(-v,n);
                tint+=float3(.60,.73,.90)*pow(saturate(1-abs(reflected.x+.26)*1.7),22)*.065;
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
