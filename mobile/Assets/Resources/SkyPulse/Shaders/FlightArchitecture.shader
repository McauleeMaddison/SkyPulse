Shader "SkyPulse/Flight Architecture"
{
    Properties
    {
        _Visibility ("World blend", Float)=1
        _Clock ("Motion", Float)=0
        _Tint ("World light", Color)=(.3,.8,1,1)
        _Haze ("Atmosphere", Color)=(.02,.04,.09,1)
        _Environment ("Authored world reflections",2D)="gray"{}
        _Architecture("Curtain wall detail",Float)=0
        _Geology("Mineral strata",Float)=0
        _Design("District identity",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Opaque"}
        Pass
        {
            Cull Off ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct app {float4 vertex:POSITION;float3 normal:NORMAL;float4 color:COLOR;};
            struct vary {float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float4 color:COLOR;float3 local:TEXCOORD2;};
            float _Visibility,_Clock,_Architecture,_Geology,_Design;float4 _Tint,_Haze;
            sampler2D _Environment;
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color;o.local=v.vertex.xyz;return o;}
            float hash21(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float facadeLine(float value,float width){float aa=max(fwidth(value),.001);return 1-smoothstep(width,width+aa,abs(frac(value)-.5));}
            float3 district(vary i)
            {
                float3 n=normalize(i.normal),view=normalize(_WorldSpaceCameraPos.xyz-i.world);
                if(dot(n,view)<0)n=-n;
                float3 key=normalize(float3(-.64,.87,-.75)),halfway=normalize(key+view);
                float facing=saturate(dot(n,view)),ndl=saturate(dot(n,key));
                float glass=smoothstep(.27,.32,i.color.a)*(1-smoothstep(.38,.43,i.color.a));
                float light=smoothstep(.76,.96,i.color.a),stone=step(.06,i.color.a)*(1-step(.23,i.color.a));
                float3 reflection=reflect(-view,n);
                float sky=saturate(reflection.y*.6+.4);
                float3 ambient=lerp(float3(.035,.050,.081),float3(.34,.43,.55),sky);
                ambient=lerp(ambient,ambient*lerp(float3(1,1,1),max(_Tint.rgb,.24),.24),.65);
                float3 environment=tex2D(_Environment,saturate(reflection.xy*.24+float2(.5,.38))).rgb;
                ambient=lerp(ambient,ambient*.8+environment*.19,.55);
                float fresnel=.055+.945*pow(1-facing,5);
                // A broad cool skybox and narrow warm key show curved surfaces;
                // the frame and glass have separate roughness and reflectance.
                float softbox=pow(saturate(1-abs(reflection.x*.72+reflection.y*.32-.19)*1.75),22);
                float spec=pow(saturate(dot(n,halfway)),lerp(52,126,glass));
                float3 colour=i.color.rgb*(.24+ndl*.80)*lerp(float3(.92,.98,1.05),_Tint.rgb,.06);
                colour+=ambient*(.19+glass*.56+fresnel*.46)+float3(.87,.91,1)*(spec*.46+softbox*(.12+glass*.34));
                // Facade bays are glazing with dark reveals and a few occupied
                // warm interiors. They are not continuous glowing floor discs.
                float2 grid=float2((i.local.x+i.local.z*.61)*9.5,i.local.y*4.1);
                float mullion=max(facadeLine(grid.x,.015),facadeLine(grid.y,.018));
                colour*=1-glass*mullion*.25;
                float2 cell=frac(grid);float interior=smoothstep(.15,.23,cell.x)*(1-smoothstep(.72,.84,cell.x))*smoothstep(.18,.28,cell.y)*(1-smoothstep(.67,.78,cell.y));
                float occupied=step(.76,hash21(floor(grid)+_Design*7.1));
                colour+=glass*interior*occupied*float3(.90,.60,.30)*(.15+.05*sin(_Clock*.10+i.local.y));
                float surface=.94+.06*hash21(floor(i.local.xz*28));
                colour*=lerp(1,surface,stone);
                colour+=stone*i.color.rgb*(.17+.14*saturate(n.y))+.035*stone*_Tint.rgb;
                float floorShade=lerp(.72,1,smoothstep(-.10,.50,i.local.y));colour*=floorShade;
                float pulse=.88+.08*sin(_Clock*.46-i.local.y*1.7+_Design);
                colour=lerp(colour,i.color.rgb*pulse*.88+ambient*.05,light);
                float distanceToCamera=length(_WorldSpaceCameraPos.xyz-i.world);
                float mist=saturate((distanceToCamera-25)/35)*.51;
                float3 atmosphere=lerp(float3(.13,.15,.24),max(_Haze.rgb,_Tint.rgb*.23),.46);
                return lerp(colour,atmosphere,mist);
            }
            fixed4 frag(vary i):SV_Target
            {
                // Stable coverage fade writes neither colour nor depth at zero.
                float2 p=floor(i.pos.xy);float threshold=frac(dot(p,float2(.75487766,.56984029)));
                clip(_Visibility-threshold-.00001);
                if(_Architecture>.5)return fixed4(district(i),1);
                float3 n=normalize(i.normal),v=unity_OrthoParams.w>.5?float3(0,0,-1):normalize(_WorldSpaceCameraPos.xyz-i.world),key=normalize(float3(-.55,.8,-.9));
                if(dot(n,v)<0)n=-n;
                float diffuse=.89+.55*saturate(dot(n,key))+.18*saturate(dot(n,normalize(float3(.8,.2,-.6))));
                float gloss=smoothstep(.12,.38,i.color.a)*(1-smoothstep(.65,.8,i.color.a));
                float spec=pow(saturate(dot(n,normalize(key+v))),lerp(52,110,gloss))*.28;
                float3 reflection=reflect(-v,n);
                float softbox=pow(saturate(1-abs(reflection.x*.7+reflection.y*.4-.17)*2.4),12);
                float rim=pow(1-abs(dot(n,v)),4)*.055;
                float3 metal=i.color.rgb*diffuse*lerp(float3(.75,.84,.94),_Tint.rgb,.18)+(spec+softbox*gloss*.14)*float3(.68,.83,1)+_Tint.rgb*rim;
                float2 reflectionUv=saturate(reflection.xy*.34+float2(.5,.36));
                float3 worldReflection=tex2D(_Environment,reflectionUv).rgb;
                metal=lerp(metal,worldReflection*.64+metal*.86,.12+gloss*.28);
                // Fine mullions and reflected softbox strips make the glazing
                // continuous curtain wall, rather than painted blue blocks.
                float floors=1-smoothstep(.015,.045,abs(frac(i.local.y*4.5)-.5));
                float mullions=1-smoothstep(.015,.035,abs(frac(i.local.x*13)-.5));
                metal*=1-_Architecture*gloss*(floors*.20+mullions*.12);
                float reflectedStrip=pow(saturate(1-abs(n.x-.32)*2.1),18);
                float fresnel=pow(1-saturate(dot(n,v)),3);
                metal+=_Architecture*gloss*(softbox*.13+spec*.12+reflectedStrip*.24+fresnel*.21)*float3(.65,.80,.95);
                float pulse=.78+.07*sin(_Clock*.40+i.world.y*.35);
                float emission=smoothstep(.65,.95,i.color.a);
                float3 lit=lerp(metal,lerp(i.color.rgb,_Tint.rgb,.20)*pulse*lerp(1,.72,_Architecture),emission);
                // Distance haze ties the skyline into the authored atmosphere.
                float geology=_Geology*(1-step(.03,i.local.y));
                float strata=.90+.10*sin(i.local.y*39+sin(i.local.x*7)*2);
                lit=lerp(lit,lit*strata+float3(.16,.045,.11)*pow(saturate(-n.y),3),geology);
                float mist=saturate((i.world.z-5)*.009);
                float3 atmosphere=max(_Haze.rgb,lerp(float3(.04,.075,.14),_Tint.rgb,.06));
                return fixed4(lerp(lit,atmosphere,mist),1);
            }
            ENDCG
        }
    }
    Fallback Off
}
