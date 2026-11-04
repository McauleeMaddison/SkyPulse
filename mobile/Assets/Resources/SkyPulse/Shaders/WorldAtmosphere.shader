Shader "SkyPulse/Constructed World Atmosphere"
{
    Properties { _CloudNoise("Shared cloud density volume",3D)="white"{} _Tint("World light",Color)=(.2,.6,1,1) _Haze("Horizon",Color)=(.03,.08,.16,1) _Clock("Clock",Float)=0 _World("World",Float)=0 _SkyA("Outgoing sky art",2D)="gray"{} _SkyB("Incoming sky art",2D)="gray"{} _Blend("Sky transition",Float)=1 _Aspect("Sky camera aspect",Float)=.46 _WorldA("Outgoing world identity",Float)=0 _WorldB("Incoming world identity",Float)=0 }
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
            float cloudDensity(float3 at,float3 wind)
            {
                // Broken banks instead of one filled cloud floor. The density
                // volume is sampled in physical world space, so every ray sees
                // a different crown and a clear channel between the billows.
                float3 uv=at*float3(.0036,.0026,.0036)+float3(.17,.43,.29)+wind;
                float billow=tex3Dlod(_CloudNoise,float4(uv,0)).r;
                float crown=-9.5+(billow-.5)*11;
                float top=1-smoothstep(crown-1.2,crown+1.8,at.y);
                float bottom=smoothstep(-25,-16,at.y);
                float islands=smoothstep(.38,.73,billow);
                return top*bottom*islands;
            }
            float3 cloudVolume(float3 ray,float3 behind)
            {
                // The four existing samples remain bounded. Low opacity and
                // a soft horizon let clear air show through the cloud banks;
                // they never replace the lower screen with an opaque lilac slab.
                if(ray.y>=-.08)return behind;
                const float top=-3.5,bottom=-25,originHeight=6.4,maxDistance=110;
                float start=max(0,(top-originHeight)/ray.y);
                float finish=min(maxDistance,(bottom-originHeight)/ray.y);
                if(finish<=start)return behind;
                float3 za,ma,sa,la,ga,zb,mb,sb,lb,gb;
                palette(_WorldA,za,ma,sa,la,ga);palette(_WorldB,zb,mb,sb,lb,gb);
                float blend=saturate(_Blend);
                float3 shadow=lerp(sa,sb,blend)*.50+behind*.32;
                float3 lit=lerp(la,lb,blend)*.40+float3(.08,.13,.21)+lerp(ga,gb,blend)*.020;
                float stepLength=(finish-start)*.25;
                float horizonFade=smoothstep(.08,.24,-ray.y);
                float3 origin=float3(0,originHeight,0),wind=float3(_Clock*.00010,0,_Clock*.000035);
                float transmittance=1;float3 scattering=0;
                [unroll] for(int stepIndex=0;stepIndex<4;stepIndex++)
                {
                    float3 at=origin+ray*(start+(stepIndex+.42)*stepLength);
                    float density=cloudDensity(at,wind);
                    float upper=cloudDensity(at+float3(-2.8,4.6,-1.4),wind);
                    float silver=saturate((density-upper)*3.2);
                    float height=saturate((at.y+20)*.065);
                    float illumination=saturate(.14+height*.30+silver*.68);
                    float3 colour=lerp(shadow,lit,illumination);
                    float opacity=(1-exp2(-density*min(stepLength,6)*.11))*horizonFade;
                    scattering+=colour*(transmittance*opacity);
                    transmittance*=1-opacity;
                }
                return behind*transmittance+scattering;
            }
            float3 lowerAir(float3 d,float3 shadow,float3 glow)
            {
                // Clean indigo depth below the inhabited skyline. A broad
                // atmospheric falloff replaces the old constant purple fill.
                float horizon=exp(-pow((d.y+.08)*4.6,2));
                float altitude=smoothstep(-.65,-.035,d.y);
                float3 deep=float3(.012,.022,.055)+shadow*.12;
                float3 distant=float3(.026,.042,.095)+shadow*.24+glow*.014;
                return lerp(deep,distant,altitude)+glow*horizon*.018;
            }
            float neonBankEnvelope(float3 at)
            {
                // Three finite physical banks sit behind different city depths.
                // Their ellipsoidal volumes leave open air between the crowns,
                // rather than filling every point below a horizontal cloud floor.
                float3 left=(at-float3(-24,-10,48))/float3(33,9,29);
                float3 right=(at-float3(26,-9,76))/float3(31,8,27);
                float3 lower=(at-float3(-4,-19,30))/float3(34,11,30);
                return max(1-dot(left,left),max(1-dot(right,right),1-dot(lower,lower)));
            }
            float neonCloudPocket(float envelope,float billow,float detail)
            {
                // The envelope only bounds the field. It never fills the bank:
                // independently eroded pockets leave clear air through its body.
                float boundary=smoothstep(0,.45,envelope);
                float pocket=smoothstep(.40,.64,billow*.72+detail*.28);
                float erosion=smoothstep(.22,.58,detail+billow*.20);
                return boundary*pocket*erosion*.62;
            }
            float neonDensity(float3 at,float3 wind,out float detail)
            {
                float3 uv=at*float3(.009,.012,.009)+float3(.13,.36,.51)+wind;
                float billow=tex3Dlod(_CloudNoise,float4(uv,0)).r;
                detail=tex3Dlod(_CloudNoise,float4(uv*3.01+float3(.31,.07,.19),0)).r;
                return neonCloudPocket(neonBankEnvelope(at),billow,detail);
            }
            float3 neonCloudVolume(float3 ray,float3 origin,float3 behind)
            {
                // Eight low-extinction samples resolve wispy billow pockets,
                // not opaque ellipsoid walls or six broad integration bands.
                // All samples remain bounded to the existing cached 3D volume.
                if(ray.y>.08)return behind;
                float start=12,finish=110,stepLength=(finish-start)/8;
                float3 wind=float3(_Clock*.000065,0,_Clock*.000023);
                float transmittance=1;float3 scattering=0;
                [unroll] for(int sampleIndex=0;sampleIndex<8;sampleIndex++)
                {
                    float3 at=origin+ray*(start+(sampleIndex+.46)*stepLength);
                    float detail=0;float density=neonDensity(at,wind,detail);
                    float3 upperAt=at+float3(-2.5,3.6,-1.5);
                    float3 upperUv=upperAt*float3(.009,.012,.009)+float3(.13,.36,.51)+wind;
                    float upperNoise=tex3Dlod(_CloudNoise,float4(upperUv,0)).r;
                    float upper=neonCloudPocket(neonBankEnvelope(upperAt),upperNoise,detail);
                    float crown=saturate((density-upper)*8.0);
                    float height=saturate((at.y+23)*.055);
                    float3 shade=float3(.012,.026,.061)+behind*.10;
                    float3 light=float3(.22,.36,.53);
                    float3 colour=lerp(shade,light,saturate(.05+height*.12+crown*.68));
                    colour+=float3(.018,.060,.085)*crown;
                    colour+=float3(.045,.006,.042)*crown*smoothstep(-8,28,at.x)*.35;
                    float opacity=(1-exp2(-density*stepLength*.020))*smoothstep(-.08,.13,-ray.y);
                    scattering+=colour*transmittance*opacity;transmittance*=1-opacity;
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
                uv+=float2(sin(_Clock*.017)*.006,cos(_Clock*.013)*.004);uv=clamp(uv,float2(0,.30),float2(1,.985));
                float3 art=tex2D(artwork,uv).rgb;float luma=dot(art,float3(.2126,.7152,.0722));
                float structure=saturate(luma*.74+mist*.40);
                float3 sky=lerp(zenith,middle,saturate(.52-d.y*1.6));
                // Retain the authored cloud shapes; colour grade their light,
                // rather than stretching one brown/grey cloud across every sky.
                sky+=glow*smoothstep(.14,.78,structure)*.37;
                sky+=art*lerp(glow,float3(.65,.76,1),.28)*.29;
                float horizon=exp(-pow((d.y+.09)*6.8,2));sky+=glow*horizon*.14;
                float curtain=sin(d.x*8.5+d.y*2.3+sin(d.x*4-_Clock*.065)*.65);
                float aurora=pow(saturate(1-abs(d.y-.13-curtain*.065)*13),3)*smoothstep(-.2,.18,d.y);
                float polar=world<1.5||world>10.5?1:0;
                sky+=lerp(glow,float3(.52,.27,.95),saturate(d.x+.45))*aurora*polar*.19;
                float sea=1-smoothstep(-.18,.035,d.y);
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
                float neonWeight=lerp(1-step(.5,_WorldA),1-step(.5,_WorldB),saturate(_Blend));
                float3 atmosphere=sky;
                if(neonWeight>.9999)atmosphere=neonCloudVolume(ray,i.cloudOrigin,sky);
                else if(neonWeight<.0001)atmosphere=cloudVolume(ray,sky);
                else atmosphere=lerp(cloudVolume(ray,sky),neonCloudVolume(ray,i.cloudOrigin,sky),neonWeight);
                return fixed4(atmosphere,1);
            }
            ENDCG
        }
    }
}
