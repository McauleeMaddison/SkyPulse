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
        _BrushStrength ("Directional machining", Range(0, 2)) = 1
        _SatinStrength ("Satin microfacet finish", Range(0, 2)) = 1
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
        half _BrushStrength, _SatinStrength;
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

            // Re-orthogonalize even supplied tangents: a missing mesh channel
            // can arrive as a nonzero default attribute on some graphics APIs.
            float3 n = normalize(v.normal);
            float3 tangent = v.tangent.xyz - n * dot(v.tangent.xyz, n);
            if (dot(tangent, tangent) < 0.0001)
            {
                float3 axis = abs(n.x) < 0.92 ? float3(1, 0, 0) : float3(0, 1, 0);
                tangent = axis - n * dot(axis, n);
            }
            v.tangent = float4(normalize(tangent), v.tangent.w < 0 ? -1 : 1);
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
            half machining = (coating * 0.48h + alloy + chassis * 0.12h)
                * _BrushStrength;

            float3 basisNormal = WorldNormalVector(IN, float3(0, 0, 1));
            float3 n = normalize(basisNormal);
            float3 localNormal = normalize(mul(n, (float3x3)unity_ObjectToWorld));
            // Longitudinal brushing follows the part rather than the camera.
            // Blend two cross-axis projections across compound curved armour.
            float2 projection = abs(localNormal.zy);
            projection /= max(projection.x + projection.y, 0.0001);
            float scale = max(_DetailScale, 1.0);
            float2 cycles = IN.localPosition.yz * scale;
            float phase = sin(IN.localPosition.x * 3.1) * 0.24;

            // Two machining scales: microscopic abrasion, plus the broader
            // directional tool passes that remain readable in the review view.
            // Neither scale changes albedo, so the metal never becomes paint noise.
            float2 finePhase = cycles * tau + phase;
            float2 fineResolved = ResolvedGrain(cycles);
            float2 fineHeight = sin(finePhase) * fineResolved;
            float fineHeightAmplitude = 0.025 / (tau * scale);
            float2 toolCycles = cycles * 0.60;
            float2 toolPhase = toolCycles * tau + phase * 0.65;
            float2 toolResolved = ResolvedGrain(toolCycles);
            float2 secondaryResolved = ResolvedGrain(toolCycles * 1.79);
            float2 toolHeight = sin(toolPhase) * toolResolved * 0.72
                + sin(toolPhase * 1.79 + IN.localPosition.x * 1.7)
                * secondaryResolved * (0.28 / 1.79);
            float toolHeightAmplitude = 0.066 / (tau * scale * 0.60);
            float brushedHeight = dot(fineHeight * fineHeightAmplitude
                + toolHeight * toolHeightAmplitude, projection) * machining;

            // Roughness carries the same grain when normal detail becomes
            // subpixel. Its low-frequency variation remains directional and
            // restrained; there are no isolated bright random microfacets.
            float2 satinCycles = cycles * 0.40;
            float2 satin = (sin(satinCycles * tau + phase) * 0.65
                + sin(satinCycles * (tau * 1.41) + 0.8) * 0.35)
                * ResolvedGrain(satinCycles);
            float directionalRoughness = dot(satin, projection)
                * (0.010 * coating + 0.018 * alloy + 0.005 * chassis)
                * _BrushStrength;
            float resolvedToolRoughness = dot(cos(toolPhase) * toolResolved, projection)
                * (0.021 * coating + 0.036 * alloy) * _BrushStrength;
            float unresolvedAbrasion = dot(1.0 - fineResolved, projection)
                * (0.010 * coating + 0.020 * alloy) * _BrushStrength;

            // One pair of fine longitudinal scribes per authored UV strip.
            // No repeating cells, crosshatching, fake outline or Fresnel colour.
            // Zero-UV caps and optical/chassis materials have no etched seams.
            float2 uv = IN.panelUV;
            float validUV = step(0.0001, abs(ddx(uv.y)) + abs(ddy(uv.y)));
            float extent = smoothstep(0.07, 0.16, uv.x)
                * (1.0 - smoothstep(0.84, 0.94, uv.x));
            float edgeDistance = min(abs(uv.y - 0.075), abs(uv.y - 0.925));
            float footprint = fwidth(uv.y);
            float groove = FilteredScribe(edgeDistance, 0.0023, footprint);
            float lip = FilteredScribe(abs(edgeDistance - 0.0048), 0.0016, footprint)
                * (1.0 - smoothstep(0.002, 0.009, footprint));
            float seamWeight = _SeamStrength * (coating + alloy * 0.65)
                * extent * validUV;
            float etched = groove * seamWeight;
            float height = brushedHeight - etched * 0.00032;
            // A satin plate field with a slightly tighter rolled perimeter.
            // This is one broad transition per authored part, never a tiled grid.
            float panelBorder = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
            float panelField = smoothstep(0.018, 0.15, panelBorder) * validUV;
            float satinRoughness = (coating * (0.022 + 0.030 * panelField)
                + alloy * (0.016 + 0.027 * panelField)) * _SatinStrength;

            float3 finishedNormal = HeightNormal(IN.worldPos, n, height);
            // Invert the interpolated frame rather than assuming three exactly
            // orthogonal axes. A flat finish therefore preserves the mesh normal.
            float3 tangent = WorldNormalVector(IN, float3(1, 0, 0));
            float3 bitangent = WorldNormalVector(IN, float3(0, 1, 0));
            float3 cofactor = cross(bitangent, basisNormal);
            float frameDeterminant = dot(tangent, cofactor);
            float3 mappedNormal = float3(dot(finishedNormal, cofactor),
                dot(finishedNormal, cross(basisNormal, tangent)),
                dot(finishedNormal, cross(tangent, bitangent)));
            o.Normal = abs(frameDeterminant) > 1e-6
                ? normalize(mappedNormal * sign(frameDeterminant)) : float3(0, 0, 1);
            o.Albedo = _Color.rgb * (1.0h - etched * 0.32h);
            o.Metallic = _Metallic;
            // Standard supplies energy-conserving real-light and probe response.
            // Finite satin roughness controls hotspots without invented highlights.
            o.Smoothness = saturate(_Glossiness - satinRoughness
                - unresolvedAbrasion + directionalRoughness + resolvedToolRoughness
                + lip * seamWeight * 0.025 - etched * 0.12);
            o.Occlusion = 1.0h - etched * 0.12h;
            o.Emission = _EmissionColor.rgb * _EmissionStrength;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
