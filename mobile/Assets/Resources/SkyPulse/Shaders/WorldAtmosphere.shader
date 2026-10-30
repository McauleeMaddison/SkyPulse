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
            struct vary {float4 pos:SV_POSITION;float3 direction:TEXCOORD0;float3 cloudRay:TEXCOORD1;};
            float4 _Tint,_Haze;float _Clock,_World,_WorldA,_WorldB,_Blend,_Aspect;sampler2D _SkyA,_SkyB;sampler3D _CloudNoise;
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;o.cloudRay=mul(unity_ObjectToWorld,v.vertex).xyz-_WorldSpaceCameraPos.xyz;return o;}
            float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
            float noise(float3 p)
            {
                float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);
            }
            float3 cloudVolume(float3 ray,float3 behind)
            {
                // A finite slab below the inhabited city, evaluated once after
                // the sky palettes blend. The upper cosmos has no volume work.
                if(ray.y>=-.075)return behind;
                const float top=-1,bottom=-12,originHeight=6.4,maxDistance=96;
                float start=max(0,(top-originHeight)/ray.y);
                float finish=min(maxDistance,(bottom-originHeight)/ray.y);
                if(finish<=start)return behind;
                float stepLength=(finish-start)*.125;
                float3 origin=float3(0,originHeight,0),wind=float3(_Clock*.00042,0,_Clock*.00013);
                float3 shadow=lerp(float3(.13,.19,.29),max(_Haze.rgb,float3(.08,.12,.21)),.18);
                float3 lit=lerp(float3(.76,.83,.93),max(_Tint.rgb,float3(.40,.48,.60)),.18);
                float transmittance=1;float3 scattering=0;
                [unroll] for(int stepIndex=0;stepIndex<8;stepIndex++)
                {
                    float3 sampleAt=origin+ray*(start+(stepIndex+.45)*stepLength);
                    float3 uv=sampleAt*.0105+float3(.17,.43,.29)+wind;
                    float coarse=tex3Dlod(_CloudNoise,float4(uv,0)).r;
                    float fine=tex3Dlod(_CloudNoise,float4(uv*4.27+float3(.37,.11,.61),0)).r;
                    float height=saturate((sampleAt.y-bottom)/(top-bottom));
                    float envelope=smoothstep(0,.23,height)*(1-smoothstep(.62,1,height));
                    float density=smoothstep(.49,.68,coarse*.68+fine*.32)*envelope;
                    // One upper-light probe supplies self-shadowing. This is a
                    // bounded 8-step volume, not a second shadow raymarch.
                    float upper=tex3Dlod(_CloudNoise,float4(uv+float3(-.022,.075,-.028),0)).r;
                    float sunlight=saturate(.55+(coarse-upper)*2.3);
                    float illumination=saturate(.29+sunlight*.48+height*.16);
                    float3 colour=lerp(shadow,lit,illumination);
                    float opacity=1-exp2(-density*min(stepLength,4)*.28);
                    scattering+=colour*(transmittance*opacity);
                    transmittance*=1-opacity;
                }
                return behind*transmittance+scattering;
            }
            float3 neonSky(float3 direction,sampler2D artwork)
            {
                float3 d=direction;
                // Only the original sky is mapped onto the enclosing volume.
                // Its illustrated buildings are cropped out: inhabited islands
                // in front are real meshes with independent depth and travel.
                // Keep the painted sky inside its horizontal extent on tablets
                // as well as narrow phones; clamped edge columns form stripes.
                float2 uv=float2(.5+d.x*(1.24/max(.3,_Aspect)),.44+d.y*1.24);
                uv+=float2(sin(_Clock*.017)*.006,cos(_Clock*.013)*.004);
                uv.y=clamp(uv.y,.30,.985);uv.x=saturate(uv.x);
                float3 sky=tex2D(artwork,uv).rgb*.90;
                float3 p=d*5+float3(0*.17,_Clock*.0015,0);
                float cloud=noise(p)*.65+noise(p*2.1)*.35;
                float band=exp(-pow((d.x+d.y*.65+.08)*4,2));
                sky+=lerp(float3(.28,.055,.43),_Tint.rgb,.15)*smoothstep(.38,.86,cloud)*(.06+band*.15);
                float sunset=exp(-pow((d.y+.11)*9,2));
                sky+=lerp(float3(.95,.25,.18),_Tint.rgb,.18)*sunset*.27;
                // Cloud holes reveal clean distant air, never buildings from
                // the cropped source illustration. The upper art stays intact.
                float lower=1-smoothstep(-.18,.035,d.y);
                float3 deepAir=lerp(float3(.035,.055,.13),_Haze.rgb*.45+_Tint.rgb*.025,.22);
                sky=lerp(sky,deepAir,lower);
                float3 starCell=floor(d*720);float seed=hash(starCell);
                float star=step(.9988,seed)*pow(saturate(1-length(frac(d*720)-.5)*1.8),8);
                sky+=float3(.70,.86,1)*star*.75;
                return sky;
            }

            // Deliberate art direction for worlds 2–12. Palettes blend by world identity,
            // rather than sliding through unrelated intermediate colour themes.
            void palette(float world,out float3 zenith,out float3 middle,out float3 shadow,out float3 light,out float3 glow)
            {
                if(world<1.5){zenith=float3(.012,.032,.12);middle=float3(.045,.18,.28);shadow=float3(.035,.105,.17);light=float3(.30,.67,.68);glow=float3(.12,.88,.65);}
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
                if(world<.5)return neonSky(d,artwork);
                float3 zenith,middle,shadow,light,glow;palette(world,zenith,middle,shadow,light,glow);
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
                float3 deepAir=lerp(float3(.035,.055,.13),shadow*.42+glow*.025,.26);
                sky=lerp(sky,deepAir,sea);
                float3 cell=floor(d*720);float seed=hash(cell+world*3);
                float star=step(.9985,seed)*pow(saturate(1-length(frac(d*720)-.5)*1.8),8);
                sky+=lerp(float3(.70,.84,1),glow,.20)*star*.64*(1-sea*.8);
                // Slow storm illumination, never a lightning flash or a strobe.
                float storm=world>8.5&&world<9.5?1:0;
                sky+=light*storm*pow(saturate(sin(_Clock*.19+d.x*3+1.7)),10)*.055;
                return sky;
            }
            fixed4 frag(vary i):SV_Target
            {
                float3 d=normalize(i.direction);
                float3 sky;
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
                return fixed4(cloudVolume(normalize(i.cloudRay),sky),1);
            }
            ENDCG
        }
    }
}
