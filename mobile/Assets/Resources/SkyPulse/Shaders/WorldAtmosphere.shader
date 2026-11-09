Shader "SkyPulse/Constructed World Atmosphere"
{
    Properties { _CloudNoise("Cached coherent RGBA cloud volume",3D)="white"{} _Tint("World light",Color)=(.2,.6,1,1) _Haze("Horizon",Color)=(.03,.08,.16,1) _Clock("Clock",Float)=0 _World("World",Float)=0 _SkyA("Outgoing sky art",2D)="gray"{} _SkyB("Incoming sky art",2D)="gray"{} _Blend("Sky transition",Float)=1 _Aspect("Sky camera aspect",Float)=.46 _WorldA("Outgoing world identity",Float)=0 _WorldB("Incoming world identity",Float)=0 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Opaque"}
        Pass
        {
            Cull Off ZWrite Off ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct app {float4 vertex:POSITION;};
            struct vary {float4 pos:SV_POSITION;float3 direction:TEXCOORD0;float3 cloudRay:TEXCOORD1;float3 cloudOrigin:TEXCOORD2;};
            float4 _Tint,_Haze;float _Clock,_World,_WorldA,_WorldB,_Blend,_Aspect;sampler2D _SkyA,_SkyB;sampler3D _CloudNoise;
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;o.cloudRay=mul(unity_ObjectToWorld,v.vertex).xyz-_WorldSpaceCameraPos.xyz;float scale=length(mul((float3x3)unity_ObjectToWorld,float3(1,0,0)));o.cloudOrigin=mul(unity_WorldToObject,float4(_WorldSpaceCameraPos.xyz,1)).xyz*scale;return o;}
            float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
            float noise(float3 p)
            {
                float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);
            }
            void palette(float world,out float3 zenith,out float3 middle,out float3 shadow,out float3 light,out float3 glow);
            float3 lowerAir(float3 d,float3 shadow,float3 glow)
            {
                float horizon=exp(-pow((d.y+.055)*5.8,2));
                float altitude=smoothstep(-.72,.015,d.y);
                float3 deep=float3(.008,.018,.042)+shadow*.085;
                float3 distant=float3(.023,.049,.095)+shadow*.16+glow*.009;
                return lerp(deep,distant,altitude)+glow*horizon*.014;
            }
            float3 cloudProfile(float world)
            {
                // Height, crown spread and shearing are authored per place.
                // The twelve profiles share one cached coherent density texture.
                if(world<.5)return float3(0,1,0);
                if(world<1.5)return float3(1.5,.78,.38);
                if(world<2.5)return float3(-1.8,.66,-.32);
                if(world<3.5)return float3(-.6,.60,.60);
                if(world<4.5)return float3(.8,.84,-.22);
                if(world<5.5)return float3(1.4,.76,.18);
                if(world<6.5)return float3(-.4,.82,-.46);
                if(world<7.5)return float3(.5,1.10,.44);
                if(world<8.5)return float3(-2,1.05,-.20);
                if(world<9.5)return float3(1.3,1.28,.66);
                if(world<10.5)return float3(-1.5,.70,-.54);
                return float3(.4,.62,.30);
            }
            float bankEnvelope(float3 at,float3 profile)
            {
                // This is only a finite weather bound. Coherent, eroded density
                // below is responsible for every visible billow and open channel.
                float3 p=at;p.x+=p.y*profile.z;p.y-=profile.x;
                // Weather belongs to small distant edge banks, not a blanket
                // across the flight lane or underneath every island.
                float3 left=(p-float3(-37,-17.5,66))/float3(18,6.0*profile.y,26);
                float3 right=(p-float3(40,-21.0,89))/float3(20,6.8*profile.y,27);
                return max(1-dot(left,left),1-dot(right,right));
            }
            float coherentDensity(float4 field,float envelope)
            {
                // Cached channel contract: R = coherent Perlin-Worley FBM;
                // G/B = inverse Worley at six/twelve cells; A = low-frequency
                // curl. Erosion removes the outside of a billow, not random
                // white-noise holes through an otherwise filled cloud sheet.
                float body=smoothstep(.51,.71,field.r);
                float boundary=smoothstep(.015,.38,envelope+(field.r-.5)*.24);
                float edge=1-body;
                float erosion=((1-field.g)*.18+(1-field.b)*.075)*(.22+edge*.78);
                float density=saturate(body-erosion);
                return density*density*(3-2*density)*boundary*.72;
            }
            float3 cloudVolume(float3 ray,float3 origin,float3 behind)
            {
                if(ray.y>.035)return behind;
                float blend=saturate(_Blend);
                float3 profile=lerp(cloudProfile(_WorldA),cloudProfile(_WorldB),blend);
                float3 za,ma,sa,la,ga,zb,mb,sb,lb,gb;
                palette(_WorldA,za,ma,sa,la,ga);palette(_WorldB,zb,mb,sb,lb,gb);
                float3 worldLight=lerp(la,lb,blend),worldGlow=lerp(ga,gb,blend);
                // Deep coloured interiors and a lit edge retain the world's
                // palette. Neutral grey scattering washed the whole scene out.
                float3 ambient=float3(.015,.030,.065)+worldGlow*.025+behind*.09;
                float3 sunlight=worldLight*.62+worldGlow*.15;
                float3 wind=float3(_Clock*.00046,0,_Clock*.00015);
                float3 densityScale=float3(.019,.025,.019);
                float3 sunDirection=normalize(float3(-.48,.81,-.34));
                // Twenty physically spaced primary samples resolve the volume
                // instead of projecting eight disconnected slabs. Light is
                // integrated from two further points inside each actual cloud.
                const float start=11,finish=114;
                const float stepLength=(finish-start)/20;
                float transmittance=1;float3 scattering=0;
                [loop] for(int sampleIndex=0;sampleIndex<20;sampleIndex++)
                {
                    float3 at=origin+ray*(start+(sampleIndex+.43)*stepLength);
                    if(bankEnvelope(at,profile)>-.33)
                    {
                        float4 curl=tex3Dlod(_CloudNoise,float4(at*float3(.0055,.0068,.0055)+float3(.37,.11,.29)+wind*.41,0));
                        float3 warp=(float3(curl.a,curl.r,curl.g)-.5)*float3(4.2,2.0,4.2);
                        float3 shaped=at+warp;
                        float3 uv=shaped*densityScale+float3(.13,.36,.51)+wind;
                        float4 field=tex3Dlod(_CloudNoise,float4(uv,0));
                        float density=coherentDensity(field,bankEnvelope(shaped,profile));
                        if(density>.001)
                        {
                            float3 nearAt=shaped+sunDirection*3.2;
                            float3 farAt=shaped+sunDirection*8.6;
                            float4 nearField=tex3Dlod(_CloudNoise,float4(uv+sunDirection*3.2*densityScale,0));
                            float4 farField=tex3Dlod(_CloudNoise,float4(uv+sunDirection*8.6*densityScale,0));
                            float nearDensity=coherentDensity(nearField,bankEnvelope(nearAt,profile));
                            float farDensity=coherentDensity(farField,bankEnvelope(farAt,profile));
                            float opticalDepth=nearDensity*2.0+farDensity*3.6;
                            float direct=exp2(-opticalDepth*.85);
                            float crown=saturate((density-nearDensity)*3.8);
                            float height=saturate((at.y+27-profile.x)*.052);
                            // Minimum multiple-scattered sky light keeps deep
                            // billows luminous; shadows cannot become ink holes.
                            float3 colour=ambient*(.72+height*.28)+sunlight*(direct*.43+crown*.24);
                            colour+=worldGlow*crown*.025;
                            float airFade=smoothstep(-.025,.16,-ray.y);
                            float opacity=(1-exp2(-density*stepLength*.105))*airFade;
                            // A cloud never replaces more than 42% of the clear sky.
                            // The space between separate islands stays open air.
                            opacity=min(opacity,max(0,(transmittance-.58)/max(transmittance,.001)));
                            scattering+=colour*transmittance*opacity;
                            transmittance*=1-opacity;
                        }
                    }
                }
                return behind*transmittance+scattering;
            }
            float3 neonSky(float3 direction,sampler2D artwork)
            {
                float3 d=direction;
                float2 uv=float2(.5+d.x*(1.24/max(.3,_Aspect)),.44+d.y*1.24);
                uv+=float2(sin(_Clock*.017)*.006,cos(_Clock*.013)*.004);
                // The source illustration supplies the forward nebula detail.
                // Off-axis inspection fades into constructed sky rather than
                // stretching its outermost pixel row across the dome.
                float artCoverage=smoothstep(.005,.075,uv.x)*(1-smoothstep(.925,.995,uv.x));
                uv.y=clamp(uv.y,.30,.985);uv.x=saturate(uv.x);
                float3 art=tex2D(artwork,uv).rgb;
                // Retain the original sharp nebula/star identity, with cooler
                // exposure so hot magenta edges support the engineered city.
                float magenta=saturate((art.r-art.g)*2.8);
                float3 cooled=art*float3(.72,.91,1.04);
                cooled=lerp(cooled,cooled*float3(.74,.95,1),magenta*.52);
                float3 edgeSky=lerp(float3(.006,.014,.047),float3(.014,.032,.105),saturate(d.y+.45));
                float3 sky=lerp(edgeSky,cooled*.86,artCoverage);
                float altitude=smoothstep(-.68,.05,d.y);
                float horizon=exp(-pow((d.y+.105)*8.2,2));
                float3 deepAir=lerp(float3(.013,.027,.067),float3(.025,.060,.135),altitude);
                deepAir+=float3(.022,.060,.115)*horizon;
                float leftGlow=exp(-pow((d.x+.23)*4.2,2));
                deepAir+=float3(.008,.026,.042)*horizon*leftGlow;
                deepAir+=float3(.024,.004,.025)*horizon*saturate(d.x+.22);
                // Fade to constructed air before the retained artwork reaches its
                // cropped lower edge; clamping that row otherwise creates columns.
                float lower=1-smoothstep(-.105,.060,d.y);
                sky=lerp(sky,deepAir,lower);
                float3 starCell=floor(d*1140);float seed=hash(starCell);
                float star=step(.9990,seed)*pow(saturate(1-length(frac(d*1140)-.5)*1.8),8);
                sky+=float3(.48,.72,1)*star*.38;
                return sky;
            }

            // Deliberate art direction for worlds 2–12. Palettes blend by world identity,
            // rather than sliding through unrelated intermediate colour themes.
            void palette(float world,out float3 zenith,out float3 middle,out float3 shadow,out float3 light,out float3 glow)
            {
                if(world<.5){zenith=float3(.012,.020,.10);middle=float3(.035,.070,.22);shadow=float3(.045,.035,.12);light=float3(.38,.28,.68);glow=float3(.34,.34,.96);}
                else if(world<1.5){zenith=float3(.012,.032,.12);middle=float3(.045,.18,.28);shadow=float3(.035,.105,.17);light=float3(.30,.67,.68);glow=float3(.12,.88,.65);}
                else if(world<2.5){zenith=float3(.025,.025,.10);middle=float3(.21,.09,.18);shadow=float3(.16,.07,.14);light=float3(.84,.40,.19);glow=float3(1,.53,.17);}
                else if(world<3.5){zenith=float3(.009,.027,.10);middle=float3(.02,.12,.24);shadow=float3(.022,.074,.14);light=float3(.17,.46,.59);glow=float3(.06,.58,.87);}
                else if(world<4.5){zenith=float3(.040,.020,.12);middle=float3(.25,.075,.23);shadow=float3(.15,.05,.16);light=float3(.75,.32,.47);glow=float3(1,.36,.60);}
                else if(world<5.5){zenith=float3(.015,.022,.11);middle=float3(.075,.11,.29);shadow=float3(.045,.075,.18);light=float3(.36,.48,.77);glow=float3(.38,.60,1);}
                else if(world<6.5){zenith=float3(.009,.036,.075);middle=float3(.025,.17,.17);shadow=float3(.025,.10,.11);light=float3(.25,.55,.39);glow=float3(.19,.81,.52);}
                else if(world<7.5){zenith=float3(.025,.013,.095);middle=float3(.14,.045,.29);shadow=float3(.085,.027,.16);light=float3(.46,.22,.61);glow=float3(.70,.24,1);}
                else if(world<8.5){zenith=float3(.012,.014,.045);middle=float3(.075,.045,.12);shadow=float3(.040,.031,.079);light=float3(.29,.19,.32);glow=float3(.84,.51,.24);}
                else if(world<9.5){zenith=float3(.009,.022,.09);middle=float3(.025,.095,.25);shadow=float3(.035,.075,.16);light=float3(.27,.46,.72);glow=float3(.16,.47,1);}
                else if(world<10.5){zenith=float3(.027,.024,.08);middle=float3(.19,.095,.14);shadow=float3(.14,.065,.095);light=float3(.78,.39,.23);glow=float3(1,.61,.27);}
                else{zenith=float3(.012,.030,.11);middle=float3(.045,.16,.24);shadow=float3(.035,.10,.17);light=float3(.35,.62,.70);glow=float3(.27,.88,.83);}
            }
            float3 themedSky(float3 d,float world,sampler2D artwork,float mist)
            {
                // One explicitly initialized return value avoids undefined
                // early-return temporaries on the Metal compiler's sampler path.
                float3 result=float3(0,0,0);
                if(world<.5)result=neonSky(d,artwork);
                else
                {
                float3 zenith=0,middle=0,shadow=0,light=0,glow=0;palette(world,zenith,middle,shadow,light,glow);
                float2 uv=float2(.5+d.x*(1.24/max(.3,_Aspect)),.44+d.y*1.24);
                uv+=float2(sin(_Clock*.017)*.006,cos(_Clock*.013)*.004);
                float artCoverage=smoothstep(.005,.075,uv.x)*(1-smoothstep(.925,.995,uv.x));
                float3 art=tex2D(artwork,clamp(uv,float2(0,.30),float2(1,.985))).rgb;
                float3 sky=lerp(zenith,middle,saturate(.50-d.y*1.5));
                // Authored nebula, ice and storm structures retain their local
                // colours and detail. Off-axis views fade before the crop edge.
                float3 artSky=art*.74+middle*.07+glow*mist*.022;
                sky=lerp(sky,artSky,artCoverage);
                float horizon=exp(-pow((d.y+.09)*7.1,2));sky+=glow*horizon*.035;
                float curtain=sin(d.x*8.5+d.y*2.3+sin(d.x*4-_Clock*.065)*.65);
                float aurora=pow(saturate(1-abs(d.y-.13-curtain*.065)*13),3)*smoothstep(-.2,.18,d.y);
                float polar=world<1.5||world>10.5?1:0;
                sky+=lerp(glow,float3(.52,.27,.95),saturate(d.x+.45))*aurora*polar*.19;
                float sea=1-smoothstep(-.105,.060,d.y);
                float3 deepAir=lowerAir(d,shadow,glow);
                sky=lerp(sky,deepAir,sea);
                float3 cell=floor(d*720);float seed=hash(cell+world*3);
                float star=step(.9985,seed)*pow(saturate(1-length(frac(d*720)-.5)*1.8),8);
                sky+=lerp(float3(.70,.84,1),glow,.20)*star*.64*(1-sea*.8);
                // Slow storm illumination, never a lightning flash or a strobe.
                float storm=world>8.5&&world<9.5?1:0;
                sky+=light*storm*pow(saturate(sin(_Clock*.19+d.x*3+1.7)),10)*.055;
                result=sky;
                }
                return result;
            }
            fixed4 frag(vary i):SV_Target
            {
                float3 d=normalize(i.direction);
                float3 sky=0;
                if(_WorldB<.5&&(_Blend>.9999||_WorldA<.5))sky=neonSky(d,_SkyB);
                else if(_WorldA<.5&&_Blend<.0001)sky=neonSky(d,_SkyA);
                else
                {
                    float3 p=d*5+float3(_World*.47,_Clock*.002,_World*.19);
                    float mist=noise(p)*.65+noise(p*2.07)*.35;
                    if(_Blend>.9999)sky=themedSky(d,_WorldB,_SkyB,mist);
                    else if(_Blend<.0001)sky=themedSky(d,_WorldA,_SkyA,mist);
                    else sky=lerp(themedSky(d,_WorldA,_SkyA,mist),themedSky(d,_WorldB,_SkyB,mist),_Blend);
                }
                float3 ray=normalize(i.cloudRay);
                float3 atmosphere=cloudVolume(ray,i.cloudOrigin,sky);
                return fixed4(atmosphere,1);
            }
            ENDCG
        }
    }
}
