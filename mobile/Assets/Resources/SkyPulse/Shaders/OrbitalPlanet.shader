Shader "SkyPulse/Orbital Planet"
{
    Properties
    {
        _Clock("Presentation clock",Float)=0
        _Kind("Ocean world / crater moon / ice moon",Float)=0
        _Seed("Geological identity",Float)=0
        _Deep("Ocean or basalt",Color)=(.012,.105,.33,1)
        _Light("Continental highlands",Color)=(.38,.55,.32,1)
        _Rim("Atmosphere scattering",Color)=(.13,.44,1,1)
        _Atmosphere("Atmosphere depth",Range(0,.06))=.024
        _LightDirection("Shared station sun",Vector)=(-.66,.58,-.42,0)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Opaque"}
        Pass
        {
            Cull Back ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float _Clock,_Kind,_Seed,_Atmosphere;float4 _Deep,_Light,_Rim,_LightDirection;
            struct app{float4 vertex:POSITION;};
            struct vary{float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;float3 local:TEXCOORD2;};
            vary vert(app v)
            {
                vary o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(normalize(v.vertex.xyz));
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.local=normalize(v.vertex.xyz);return o;
            }
            float hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
            float noise(float3 p)
            {
                float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);
            }
            float terrain(float3 p){return noise(p)*.56+noise(p*2.13+7.1)*.29+noise(p*4.71-3.4)*.15;}
            float craters(float3 p)
            {
                // Circular impact basins with raised lips, not thresholded
                // noise contours. Longitude wraps so no mesh seam appears.
                float2 uv=float2(atan2(p.z,p.x)*.15915494+.5,asin(clamp(p.y,-1,1))*.31830989+.5)*float2(19,10);
                float2 baseCell=floor(uv),local=frac(uv);float relief=0;
                [unroll] for(int y=-1;y<=1;y++)[unroll] for(int x=-1;x<=1;x++)
                {
                    float2 offset=float2(x,y),cell=baseCell+offset;cell.x=fmod(cell.x+19,19);
                    float seed=hash(float3(cell,_Seed));
                    float2 centre=float2(seed,hash(float3(cell+7.31,_Seed)))*.54+.23;
                    float radius=.16+hash(float3(cell-3.71,_Seed))*.24;
                    float distance=length(local-offset-centre)/radius;
                    float basin=1-smoothstep(.42,.94,distance);
                    float lip=exp2(-pow((distance-.98)*9,2));
                    relief+=-basin*.12+lip*.045;
                }
                return relief;
            }
            float4 frag(vary i):SV_Target
            {
                float3 p=normalize(i.local),n=normalize(i.normal),view=normalize(_WorldSpaceCameraPos.xyz-i.world);
                float3 sun=normalize(_LightDirection.xyz);float ndl=dot(n,sun),day=smoothstep(-.065,.075,ndl);
                float facing=saturate(dot(n,view));float3 id=float3(_Seed*1.13,_Seed*.47,_Seed*.83);
                float geology=terrain(p*3.8+id);
                float3 albedo;float cloud=0,land=0,roughness=.72,relief=0;
                if(_Kind<.5)
                {
                    // Deliberate continental masses have one quiet colour
                    // hierarchy. Fine erosion only shapes the coastline; it
                    // never creates an even camouflage pattern over the globe.
                    float plateA=.61-length((p-normalize(float3(.42,.40,-.82)))*float3(1.10,1.48,.88));
                    float plateB=.40-length((p-normalize(float3(-.69,-.32,-.65)))*float3(1.26,1.60,.88));
                    float plateC=.57-length((p-normalize(float3(-.15,.19,.97)))*float3(1.20,1.08,.92));
                    float coastField=max(max(plateA,plateB),plateC)+(terrain(p*9.3+id)-.5)*.19;
                    // A broad inlet cuts the dominant landmass; small coastal
                    // erosion sits below it rather than becoming camouflage.
                    float gulf=length((p-normalize(float3(.04,.48,-.87)))*float3(1.7,1.0,1.2));
                    coastField-=exp2(-pow(gulf*4.2,2))*.21;
                    land=smoothstep(-.012,.016,coastField);
                    float coast=1-smoothstep(.008,.040,abs(coastField));
                    float3 ocean=lerp(_Deep.rgb*.86,float3(.024,.30,.42),smoothstep(-.16,.025,coastField)*.70);
                    float upland=smoothstep(.48,.70,geology);
                    float3 ground=_Light.rgb*lerp(float3(.52,.71,.55),float3(.91,.82,.56),upland);
                    albedo=lerp(ocean,ground,land)+float3(.015,.043,.045)*coast*.32;
                    float snow=smoothstep(.87,.975,abs(p.y)+(geology-.5)*.04);
                    albedo=lerp(albedo,float3(.47,.58,.66),snow*.84);
                    // High, stretched frequencies produce thin atmospheric
                    // wisps. Broad opaque white patches hid the surface design.
                    float3 wind=p*float3(9,26,9)+id+float3(_Clock*.00065,1.8,-2.3);
                    float curl=noise(p*7.1+id);
                    float weather=noise(wind+curl*.85)*.68+noise(wind*2.41+4.7)*.32;
                    float circulation=.66+.34*sin(p.y*18+noise(p*5.2+id)*3.1);
                    cloud=smoothstep(.56,.78,weather)*circulation*.70;
                    float shadow=smoothstep(.56,.78,noise(wind+sun*.14+curl*.85));
                    albedo*=1-shadow*.065*(1-cloud);
                    albedo=lerp(albedo,float3(.65,.76,.86),cloud);
                    relief=(geology-.5)*land*.045;
                    roughness=lerp(.42,.91,saturate(land+cloud));
                }
                else if(_Kind<1.5)
                {
                    relief=craters(p);
                    float maria=smoothstep(.40,.57,geology);
                    albedo=lerp(_Deep.rgb*.88,_Light.rgb,.40+geology*.24);
                    albedo*=lerp(.82,1.02,maria)+relief*.60;
                    roughness=.95;
                }
                else
                {
                    // Glacial shelves and narrow mineral fissures make the
                    // smaller icy moon clearly distinct from the ocean planet.
                    float ridge=noise(p*13.7+id+geology*2.3);
                    float fissure=1-smoothstep(.012,.034+fwidth(ridge),abs(ridge-.53));
                    float shelf=smoothstep(.39,.67,geology);
                    albedo=lerp(_Deep.rgb,_Light.rgb,shelf*.75+.20);
                    albedo=lerp(albedo,_Deep.rgb*.62,fissure*.48);
                    relief=(shelf-.5)*.05-fissure*.025;
                    roughness=.86;
                }
                // Screen derivatives give surface relief real light response,
                // while the analytic sphere normal keeps the limb round.
                float3 dpdx=ddx(i.world),dpdy=ddy(i.world);
                float3 tangent=cross(dpdy,n),bitangent=cross(n,dpdx);
                float determinant=max(abs(dot(dpdx,tangent)),.000001);
                float3 reliefNormal=normalize(n-(tangent*ddx(relief)+bitangent*ddy(relief))*(.10/determinant));
                float diffuse=saturate(dot(reliefNormal,sun));
                float3 halfVector=normalize(sun+view);
                float spec=pow(saturate(dot(reliefNormal,halfVector)),lerp(110,22,roughness))*(1-roughness)*.34;
                float3 colour=albedo*(float3(.065,.10,.18)+diffuse*float3(1,.94,.86))*day;
                // Nights remain dark enough to show a curved terminator. Only
                // the inhabited world has a restrained city-light network.
                float night=1-smoothstep(-.10,.16,ndl);
                float cityCell=noise(p*148+id);
                float city=pow(saturate((cityCell-.61)*3.6),3)*land*(1-cloud)*night;
                if(_Kind<.5)colour+=float3(1,.57,.22)*city*.16;
                colour+=albedo*float3(.012,.025,.045)*(1-day);
                colour+=float3(.79,.91,1)*spec*day;
                float rim=pow(1-facing,4.3)*smoothstep(-.28,.35,ndl);
                float dusk=exp2(-pow(ndl*8,2))*(1-facing*.55);
                colour+=_Rim.rgb*(rim*.25+dusk*.055)*saturate(_Atmosphere*45);
                return float4(colour,1);
            }
            ENDCG
        }
        Pass
        {
            // Thin, genuinely expanded atmospheric shell. It is depth-tested
            // against the solid globe and has no painted rectangular halo.
            Cull Front ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float _Atmosphere;float4 _Rim,_LightDirection;
            struct vary{float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;};
            vary vert(float4 vertex:POSITION)
            {
                vary o;float3 direction=normalize(vertex.xyz);vertex.xyz*=1+_Atmosphere;
                o.pos=UnityObjectToClipPos(vertex);o.normal=UnityObjectToWorldNormal(direction);o.world=mul(unity_ObjectToWorld,vertex).xyz;return o;
            }
            float4 frag(vary i):SV_Target
            {
                clip(_Atmosphere-.001);
                float3 n=normalize(i.normal),view=normalize(_WorldSpaceCameraPos.xyz-i.world);
                float limb=pow(1-saturate(abs(dot(n,view))),4);
                float sun=smoothstep(-.34,.28,dot(n,normalize(_LightDirection.xyz)));
                return float4(_Rim.rgb*(.60+sun*.35),limb*(.02+sun*.17));
            }
            ENDCG
        }
    }
}
