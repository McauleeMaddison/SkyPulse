Shader "SkyPulse/Original Endless Neon Metal"
{
    Properties
    {
        _MainTex("Original pipe artwork",2D)="white" {}
        _ArtRect("Artwork UV rectangle",Vector)=(0,0,1,1)
        _ArtFlip("Invert upper pipe",Float)=0
        _Cutout("Respect collar silhouette",Float)=0
        _StructuralDetails("Physical pipe hardware",Float)=0
        _LightTint("World key colour",Color)=(.8,.9,1,1)
        _Clock("Energy clock",Float)=0
        _Visibility("World departure coverage",Range(0,1))=1
        _Theme("World structure",Float)=0
        _ThemeAmount("World finish",Float)=0
        _ThemeAccent("Primary inlay",Color)=(.02,.85,1,1)
        _ThemeEnergy("Secondary inlay",Color)=(1,.03,.85,1)
        _Detail("Housing detail",Range(0,1.5))=1
        _LightRate("LED flow rate",Range(.4,1.6))=1
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
            sampler2D _MainTex;float4 _ArtRect;float _ArtFlip,_Clock,_Cutout,_Visibility,_Theme,_ThemeAmount,_Detail,_LightRate,_StructuralDetails;fixed4 _LightTint,_ThemeAccent,_ThemeEnergy,_MetalTone;
            vary vert(app v){vary o;
                // Only the appended physical cap components mirror for an upper
                // pipe. Original collision-fitted housing geometry stays intact.
                float capHardware=_StructuralDetails*step(.70,v.color.a)*(1-step(.80,v.color.a));
                float flip=lerp(1,1-2*_ArtFlip,capHardware);v.vertex.y*=flip;v.normal.y*=flip;
                o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.color=v.color;return o;}
            float3 hardware(vary i,float2 uv)
            {
                float time=_Clock*_LightRate;
                float3 n=normalize(i.normal),view=unity_OrthoParams.w>.5?float3(0,0,-1):normalize(_WorldSpaceCameraPos.xyz-i.world);
                if(dot(n,view)<0)n=-n;
                float3 key=normalize(float3(-.65,.78,-.83)),r=reflect(-view,n);
                float ndl=saturate(dot(n,key)),fresnel=pow(1-saturate(dot(n,view)),5);
                float broad=pow(saturate(1-abs(r.x*.83+r.y*.22-.20)*1.50),9);
                float strip=pow(saturate(1-abs(r.x+.39)*1.8),38);
                float spec=pow(saturate(dot(n,normalize(key+view))),96);
                float secondaryBox=pow(saturate(1-abs(r.x-.57)*2.3),21);
                float seal=step(.43,i.color.a)*(1-step(.73,i.color.a)),trim=step(.12,i.color.a)*(1-step(.43,i.color.a)),led=step(.80,i.color.a);
                float ceramic=(_Theme<1.5||(_Theme>3.5&&_Theme<4.5)||(_Theme>5.5&&_Theme<6.5)||_Theme>10.5)?1:0;
                float3 colour=_MetalTone.rgb*(.36+ndl*.68)+float3(.11,.16,.22)*(broad*.55+fresnel*.30);
                colour+=float3(.79,.88,1)*(strip*.36+spec*.46)+_ThemeEnergy.rgb*secondaryBox*.09;
                colour=lerp(colour,colour*1.08+float3(.10,.12,.15)*(broad*.75+spec),trim);
                // Authored machined detail from the first world is retained as
                // subtle surface relief, with each new housing's own materials.
                float3 art=tex2D(_MainTex,_ArtRect.xy+saturate(uv)*_ArtRect.zw).rgb;
                float authored=dot(art,float3(.2126,.7152,.0722));
                float machined=lerp(.43,1.36,smoothstep(.035,.66,authored));
                colour*=machined;
                float angle=atan2(i.local.z,i.local.x),around=angle*.15915494+.5;
                float panelBay=1-smoothstep(.36,.74,abs(cos(angle)));
                float3 enamel=lerp(_MetalTone.rgb*.38+_ThemeAccent.rgb*.055,_MetalTone.rgb*.77,ceramic);
                float3 coated=enamel*(.34+ndl*.62)+float3(.19,.25,.34)*broad+float3(.62,.78,.94)*spec*.36;
                colour=lerp(colour,coated*machined,panelBay*(1-_Cutout)*.72);
                float panel=abs(frac(around*8)-.5);
                float panelAA=max(fwidth(panel),.004);
                float engraving=1-smoothstep(.012,.026+panelAA,panel);
                colour*=1-engraving*(.055+ceramic*.035);
                // Small recessed service joints and machined fasteners belong
                // to each housing. Analytic antialiasing keeps them quiet at
                // phone size; no extra geometry enters the flight opening.
                float endJoint=min(abs(uv.y-.135),abs(uv.y-.865));
                float jointAA=max(fwidth(uv.y),.0015);
                float joint=1-smoothstep(.003,.007+jointAA,endJoint);
                colour*=1-joint*(1-_Cutout)*.16;
                float boltX=abs(i.local.x)-lerp(.205,.235,ceramic);
                float boltY=min(abs(uv.y-.115),abs(uv.y-.885));
                float boltDistance=length(float2(boltX*34,boltY*60));
                float boltAA=max(fwidth(boltDistance),.08);
                float bolt=1-smoothstep(.70,.87+boltAA,boltDistance);
                float boltRim=exp2(-pow((boltDistance-.65)*6,2));
                float slot=1-smoothstep(.06,.12+fwidth(boltX)*34,abs(boltX*34));
                float front=smoothstep(.35,.75,-n.z)*(1-_Cutout)*(1-led);
                colour=lerp(colour,float3(.13,.17,.22)+float3(.32,.39,.48)*(ndl*.38+broad*.25)+spec*.16,bolt*front*.65);
                colour+=boltRim*front*(float3(.16,.21,.27)+_ThemeAccent.rgb*.025);
                colour*=1-bolt*slot*front*.22;
                // Flush enamel skins and recessed channels vary by world.
                // Broad material transitions survive small portrait screens;
                // micromachining is antialiased and never changes the opening.
                float longitudinal=1-smoothstep(.31,.43+fwidth(uv.y),abs(uv.y-.5));
                float edge=pow(saturate(1-abs(abs(cos(angle))-.29)*11),3)*(1-_Cutout);
                colour+=edge*longitudinal*_Detail*(float3(.22,.29,.36)*(.25+broad*.45)+_ThemeAccent.rgb*.035);
                float ventCell=abs(frac(uv.y*18)-.5),ventAA=max(fwidth(uv.y*18),.035);
                float vent=1-smoothstep(.12,.17+ventAA,ventCell);
                float serviceSide=smoothstep(.32,.55,abs(cos(angle)))*(1-smoothstep(.68,.88,abs(cos(angle))));
                float thermal=(_Theme>1.5&&_Theme<2.5)||(_Theme>8.5&&_Theme<9.5)?1:0;
                colour*=1-vent*serviceSide*longitudinal*thermal*_Detail*.21*(1-_Cutout);
                float facet=(_Theme>4.5&&_Theme<5.5)||(_Theme>7.5&&_Theme<8.5)?1:0;
                colour+=facet*pow(saturate(1-abs(sin(angle*4))*.95),18)*longitudinal*_Detail*float3(.10,.13,.17)*(broad*.5+spec);
                float organic=(_Theme>2.5&&_Theme<3.5)||(_Theme>5.5&&_Theme<6.5)?1:0;
                float tide=1-smoothstep(.005,.012+fwidth(uv.y),abs(frac(uv.y*4+sin(angle*3)*.08)-.5));
                colour+=organic*tide*serviceSide*_Detail*_ThemeEnergy.rgb*.052*(1-_Cutout);
                float brush=sin(i.world.y*228)*sin(i.local.x*316);float aa=saturate(1-fwidth(i.world.y*228));colour*=1+brush*.008*aa;
                colour=lerp(colour,float3(.021,.031,.046)*(1+ndl*.3)+spec*.08,seal);
                float path=lerp(i.world.y,-i.world.y,_ArtFlip)*.55;
                float packet;
                if(_Theme<1.5)packet=.48+.44*sin(path*2.8-time*1.05);
                else if(_Theme<2.5)packet=smoothstep(.18,.86,cos(path*5.1-time*1.8));
                else if(_Theme<3.5)packet=.46+.43*sin(path*3.4-time*1.1);
                else if(_Theme<4.5)packet=.45+.39*sin(time*.92+path*2.2);
                else if(_Theme<5.5)packet=.12+.86*pow(saturate(cos(path*5.8-time*1.6)),4);
                else if(_Theme<6.5)packet=.44+.44*sin(path*3.7-time*.98);
                else if(_Theme<7.5)packet=.12+.84*pow(1-frac(path*1.25+time*.82),3);
                else if(_Theme<8.5)packet=.10+.88*pow(saturate(cos(path*2.5-time*.68)),6);
                else if(_Theme<9.5)packet=.13+.85*pow(saturate(sin(path*4.8-time*1.8)),3);
                else if(_Theme<10.5)packet=.45+.43*sin(path*2.4-time*.71);
                else packet=.46+.44*sin(path*2.8-time*.83)*cos(path*.6+time*.28);
                float secondary=smoothstep(.05,.55,i.local.x);
                float3 energy=lerp(_ThemeAccent.rgb,_ThemeEnergy.rgb,secondary);
                float rails=(_Theme>1.5&&_Theme<2.5)||(_Theme>8.5&&_Theme<9.5)?6:(_Theme>4.5&&_Theme<5.5)||_Theme>10.5?3:4;
                float angularDistance=abs(frac(angle*rails/6.2831853)-.5)*6.2831853/rails;
                float heroDistance=abs(atan2(sin(angle+1.5707963),cos(angle+1.5707963)));
                angularDistance=min(angularDistance,heroDistance);
                float channelAA=max(fwidth(angularDistance),.002);
                float lens=(1-smoothstep(.031,.052+channelAA,angularDistance))*(1-_Cutout);
                float halo=(1-smoothstep(.055,.23+channelAA,angularDistance))*(1-_Cutout);
                colour+=energy*halo*(.10+packet*.07);
                // A dark diffuser base and a moving white core keep motion
                // readable without clipping every LED into a solid white line.
                float3 diffuser=energy*(.31+packet*.67)+float3(.40,.56,.70)*pow(packet,6)*.20;
                colour=lerp(colour,diffuser+float3(.6,.8,1)*spec*.12,max(led,lens));
                if(_Cutout>.5)
                {
                    float2 ellipse=(uv-float2(.5,.745))/float2(.46,.225);
                    float d=length(ellipse),edgeAA=max(fwidth(d),.012);
                    float inside=1-smoothstep(.86,.94,d);
                    float lip=exp(-pow((d-1.035)*8.5,2));
                    float inner=exp(-pow((d-.935)*23,2));
                    float rimAngle=atan2(ellipse.y,ellipse.x);
                    float circulation=.5+.5*sin(rimAngle*2-time*(.78+frac(_Theme*.117)));
                    float3 rimEnergy=lerp(_ThemeAccent.rgb,_ThemeEnergy.rgb,.5+.5*sin(rimAngle+.5));
                    float rimKey=.22+.60*pow(saturate(1-abs(ellipse.x+.32)*.72),5);
                    float3 well=float3(.014,.025,.040)+rimEnergy*(.025+.035*saturate(ellipse.y));
                    colour=lerp(colour,well,inside*.97);
                    // Rolled metallic bezel, an inset luminous gasket, and a
                    // travelling reflection are separate layers of the lip.
                    colour+=lip*(float3(.30,.38,.48)*rimKey+_MetalTone.rgb*.13);
                    colour+=inner*rimEnergy*(.32+circulation*.47);
                    float rimScan=pow(saturate(cos(rimAngle-time*.72)),16);
                    colour+=lip*rimScan*float3(.52,.68,.82)*.20;
                    float faceLine=1-smoothstep(edgeAA,edgeAA+.035,abs(d-1.19));
                    colour+=faceLine*rimEnergy*.10;
                }
                return colour*lerp(float3(1,1,1),_LightTint.rgb,.055);
            }
            float3 structuralHardware(vary i)
            {
                float3 n=normalize(i.normal),view=unity_OrthoParams.w>.5?float3(0,0,-1):normalize(_WorldSpaceCameraPos.xyz-i.world);
                if(dot(n,view)<0)n=-n;
                float3 key=normalize(float3(-.55,.78,-.80)),r=reflect(-view,n);
                float diffuse=saturate(dot(n,key)),spec=pow(saturate(dot(n,normalize(key+view))),74);
                float softbox=pow(saturate(1-abs(r.x*.78+r.y*.32+.18)*1.65),16);
                float3 metal=_MetalTone.rgb*(.28+diffuse*.72)+float3(.62,.75,.90)*(softbox*.25+spec*.43);
                float3 energy=lerp(_ThemeAccent.rgb,_ThemeEnergy.rgb,smoothstep(-.18,.18,i.local.x));
                if(i.color.a>.735&&i.color.a<.755)metal=metal*.48+energy*.025;
                if(i.color.a>.755&&i.color.a<.775)metal=float3(.008,.015,.026)+energy*.012+spec*.025;
                float guide=(1-step(.003,abs(i.color.a-.67)))+step(.775,i.color.a);
                float path=lerp(i.world.y,-i.world.y,_ArtFlip)*.7;
                float packet=.5+.5*sin(path*4-_Clock*_LightRate*1.35);
                metal=lerp(metal,energy*(.36+packet*.47)+float3(.35,.52,.68)*pow(packet,5)*.10,saturate(guide));
                return metal*lerp(float3(1,1,1),_LightTint.rgb,.055);
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
                float capHardware=step(.70,i.color.a)*(1-step(.80,i.color.a));
                float shaftHardware=1-step(.003,min(abs(i.color.a-.68),abs(i.color.a-.67)));
                float physical=_StructuralDetails*saturate(capHardware+shaftHardware);
                if(_StructuralDetails>.5&&_Cutout>.5&&capHardware<.5&&i.local.z<0)
                {
                    // Remove the old front wall inside the real recess. The
                    // bore and rolled lip now provide actual depth and normals.
                    float2 opening=(uv-float2(.5,.745))/float2(.46,.195);
                    clip(length(opening)-1.10);
                }
                float decoration=_ThemeAmount*step(.1,i.color.a);
                float capCoverage=(_ThemeAmount>.5?_Cutout:_Cutout*(1-decoration))*(1-physical);
                clip(lerp(1,art.a-.02,capCoverage));
                if(physical>.5)return fixed4(structuralHardware(i),1);
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
                float packet=pow(saturate(1-abs(frac(path-_Clock*_LightRate*.55)-.5)*5),2);
                float3 ledColour=lerp(_ThemeAccent.rgb,_ThemeEnergy.rgb,pink/(cyan+pink+.001));
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
                // Keep the original first-world artwork and cyan/pink identity,
                // adding only a narrow optical core and quieter metal sheen.
                tint+=_Detail*(ledColour*led*pow(packet,4)*.045+float3(.40,.56,.73)*spec*(1-led)*.028);
                return fixed4(tint,lerp(1,saturate((art.a-.02)/.6),_Cutout*(1-decoration)));
            }
            ENDCG
        }
    }
}
