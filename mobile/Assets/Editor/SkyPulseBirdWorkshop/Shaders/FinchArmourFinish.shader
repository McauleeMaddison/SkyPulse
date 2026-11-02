// Isolated art-review finish. This shader and all of its consumers live in Editor.
// Standard owns direct light, shadows, Fresnel and reflection-probe response.
Shader "SkyPulse/Workshop/Finch Armour Finish"
{
    Properties
    {
        _Color ("Armour colour", Color) = (0.045, 0.115, 0.26, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0.66
        _Glossiness ("Surface smoothness", Range(0, 1)) = 0.68
        [HDR] _EmissionColor ("Optical emission", Color) = (0, 0, 0, 1)
        [Enum(Coated armour,0,Brushed alloy,1,Recessed chassis,2,Optical glass,3,Emissive inlay,4)]
        _FinishKind ("Finish", Float) = 0
        _DetailScale ("Machining cycles per local unit", Range(40, 400)) = 160
        _SeamStrength ("Inset scribe strength", Range(0, 1)) = 0.1
        _EmissionStrength ("Optical emission strength", Range(0, 4)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 250
        Cull Back
        ZWrite On

        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma multi_compile_instancing
        #include "UnityCG.cginc"

        fixed4 _Color;
        half _Metallic, _Glossiness, _FinishKind, _SeamStrength, _EmissionStrength;
        float _DetailScale;
        half4 _EmissionColor;

        struct Input
        {
            float3 localPosition;
            float2 panelUV;
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.localPosition = v.vertex.xyz;
            o.panelUV = v.texcoord.xy;

            // The original procedural review meshes have no authored tangents.
            // Give Standard a valid orthogonal frame without altering the mesh.
            // Existing authored tangents remain useful when a mesh provides them.
            if (dot(v.tangent.xyz, v.tangent.xyz) < 0.1)
            {
                float3 n = normalize(v.normal);
                float3 axis = abs(n.x) < 0.92 ? float3(1, 0, 0) : float3(0, 1, 0);
                v.tangent = float4(normalize(axis - n * dot(axis, n)), 1);
            }
        }

        // A groove vanishes gradually before it falls below the pixel footprint.
        // This is material-space filtering; it draws no screen-space decoration.
        float2 ResolvedGrain(float2 cycles)
        {
            return 1.0 - smoothstep(0.16, 0.48, fwidth(cycles));
        }

        float FilteredScribe(float distanceToLine, float halfWidth, float footprint)
        {
            float aa = max(footprint * 0.65, 0.00005);
            float coverage = 1.0 - smoothstep(halfWidth - aa, halfWidth + aa, distanceToLine);
            return coverage * saturate(halfWidth / aa);
        }

        float3 HeightNormal(float3 position, float3 normal, float height)
        {
            // Surface gradients align the finish to the actual curved plate.
            // They also avoid UV-direction assumptions on rings and loft caps.
            float3 dx = ddx(position);
            float3 dy = ddy(position);
            float3 r1 = cross(dy, normal);
            float3 r2 = cross(normal, dx);
            float determinant = dot(dx, r1);
            float3 gradient = (r1 * ddx(height) + r2 * ddy(height))
                * (sign(determinant) / max(abs(determinant), 1e-10));
            return normalize(normal - gradient);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            const float tau = 6.28318530718;
            half alloy = 1.0h - saturate(abs(_FinishKind - 1.0h));
            half chassis = 1.0h - saturate(abs(_FinishKind - 2.0h));
            half glass = 1.0h - saturate(abs(_FinishKind - 3.0h));
            half inlay = 1.0h - saturate(abs(_FinishKind - 4.0h));
            half coating = saturate(1.0h - alloy - chassis - glass - inlay);
            half machining = coating * 0.28h + alloy + chassis * 0.14h;

            float3 n = normalize(WorldNormalVector(IN, float3(0, 0, 1)));
            float3 localNormal = normalize(mul(n, (float3x3)unity_ObjectToWorld));
            // Grain follows the local longitudinal axis. The two projections
            // blend across a compound surface instead of producing a tiled grid.
            float2 projection = abs(localNormal.zy);
            projection /= max(projection.x + projection.y, 0.0001);
            float scale = max(_DetailScale, 1.0);
            float2 cycles = IN.localPosition.yz * scale;
            float phase = sin(IN.localPosition.x * 3.1) * 0.24;
            float2 phaseA = cycles * tau + phase;
            float2 phaseB = cycles * (tau * 1.713) + IN.localPosition.x * 0.41;
            float2 resolvedA = ResolvedGrain(cycles);
            float2 resolvedB = ResolvedGrain(cycles * 1.713);
            float2 microHeight = sin(phaseA) * resolvedA * 0.68
                + sin(phaseB) * resolvedB * (0.32 / 1.713);
            float brushedHeight = dot(microHeight, projection)
                * (0.032 / (tau * scale)) * machining;

            // Broad, low-contrast roughness changes survive at normal framing.
            // No albedo noise or high-contrast random speckles are introduced.
            float2 satinCycles = cycles * 0.073;
            float2 satin = sin(satinCycles * tau + phase * 0.4)
                * ResolvedGrain(satinCycles);
            float roughnessVariation = dot(satin, projection)
                * (0.014 * coating + 0.027 * alloy + 0.012 * chassis);

            // One pair of fine longitudinal scribes per authored UV strip.
            // No repeating cells, crosshatching, fake outline or Fresnel colour.
            // Zero-UV caps and optical/chassis materials have no etched seams.
            float2 uv = IN.panelUV;
            float validUV = step(0.0001, abs(ddx(uv.y)) + abs(ddy(uv.y)));
            float extent = smoothstep(0.07, 0.16, uv.x)
                * (1.0 - smoothstep(0.84, 0.94, uv.x));
            float edgeDistance = min(abs(uv.y - 0.075), abs(uv.y - 0.925));
            float footprint = fwidth(uv.y);
            float groove = FilteredScribe(edgeDistance, 0.0017, footprint);
            float lip = FilteredScribe(abs(edgeDistance - 0.0038), 0.0014, footprint);
            float seamWeight = _SeamStrength * (coating + alloy * 0.65)
                * extent * validUV;
            float etched = groove * seamWeight;
            float height = brushedHeight - etched * 0.00024;

            float3 finishedNormal = HeightNormal(IN.worldPos, n, height);
            float3 tangent = normalize(WorldNormalVector(IN, float3(1, 0, 0)));
            float3 bitangent = normalize(WorldNormalVector(IN, float3(0, 1, 0)));
            o.Normal = normalize(float3(dot(finishedNormal, tangent),
                dot(finishedNormal, bitangent), dot(finishedNormal, n)));
            o.Albedo = _Color.rgb * (1.0h - etched * 0.12h);
            o.Metallic = _Metallic;
            // Satin enamel and polished alloy use the same energy-conserving
            // Standard BRDF. Real chamfers and the tiny scribe lip catch lights.
            o.Smoothness = saturate(_Glossiness + roughnessVariation
                + lip * seamWeight * 0.035 - etched * 0.08);
            o.Occlusion = 1.0h - etched * 0.12h;
            o.Emission = _EmissionColor.rgb * _EmissionStrength;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
