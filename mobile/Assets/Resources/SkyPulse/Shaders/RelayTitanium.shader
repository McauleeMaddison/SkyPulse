Shader "SkyPulse/Relay Titanium"
{
    Properties
    {
        _Open ("Open", Float) = 0
        _Clock ("Presentation clock", Float) = 0
        _Pulse ("Unlock impulse", Float) = 0
        _Accent ("Open emitter colour", Color) = (.2,.86,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Opaque" }
        // Sorted with the existing flight sprites, with real depth inside each
        // surface. No extra camera, post-processing, transparency or realtime
        // shadow maps are required on a phone.
        Pass
        {
            Cull Off ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct input { float4 vertex:POSITION; float3 normal:NORMAL; float4 colour:COLOR; float2 uv:TEXCOORD0; };
            struct varying { float4 position:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; float4 colour:COLOR; float2 uv:TEXCOORD2; };
            float _Open, _Clock, _Pulse; float4 _Accent;
            varying vert(input v)
            {
                varying o; o.position=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.colour=v.colour;o.uv=v.uv;return o;
            }
            fixed4 frag(varying i):SV_Target
            {
                float3 n=normalize(i.normal), view=normalize(_WorldSpaceCameraPos.xyz-i.world);
                // Soft station key, cool rim and a narrow polished reflection.
                float3 key=normalize(float3(-.55,.8,-.9)),fill=normalize(float3(.75,-.2,.4));
                float diffuse=.40+.65*saturate(dot(n,key))+.24*saturate(dot(n,fill));
                float spec=pow(saturate(dot(n,normalize(key+view))),74)*1.15;
                float rim=pow(1-saturate(abs(dot(n,view))),3);
                float groove=1-.10*pow(abs(sin(i.uv.x*48)),24);
                float3 reflected=reflect(-view,n);
                float skyReflection=pow(saturate(.5+.5*sin(reflected.y*6+reflected.x*2)),12);
                float3 metal=i.colour.rgb*(diffuse+skyReflection*.38)*groove+float3(.74,.88,1)*spec+float3(.11,.27,.36)*rim;
                float sweep=pow(saturate(cos(i.uv.x*2-_Clock*1.5)),14);
                float3 emitter=lerp(float3(1,.44,.10),_Accent.rgb,_Open);
                float3 light=emitter*(.8+sweep*.45+_Pulse*.85);
                return fixed4(lerp(metal,light,i.colour.a),1);
            }
            ENDCG
        }
    }
    Fallback Off
}
