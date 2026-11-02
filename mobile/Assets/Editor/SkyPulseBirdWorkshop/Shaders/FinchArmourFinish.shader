// Isolated art-review finish. This shader and its consumers live in Editor.
// Standard owns direct light, shadows and reflections; geometry carries plate edges.
Shader "SkyPulse/Workshop/Finch Armour Finish"
{
    Properties
    {
        _Color ("Armour colour", Color) = (0.045, 0.115, 0.26, 1)
        _BladeColor ("Feather face colour", Color) = (0.025, 0.16, 0.60, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0.66
        _Glossiness ("Surface smoothness", Range(0, 1)) = 0.74
        [HDR] _EmissionColor ("Optical emission", Color) = (0, 0, 0, 1)
        [Enum(Coated armour,0,Brushed alloy,1,Recessed chassis,2,Optical glass,3,Emissive inlay,4)]
        _FinishKind ("Finish", Float) = 0
        _DetailScale ("Microfinish cycles per local unit", Range(40, 400)) = 240
        _BrushStrength ("Fine directional microfinish", Range(0, 2)) = 0.35
        _SatinStrength ("Restrained plate satin", Range(0, 2)) = 1
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

        fixed4 _Color, _BladeColor;
        half _Metallic, _Glossiness, _FinishKind, _SeamStrength, _EmissionStrength;
        float _DetailScale;
        half _BrushStrength, _SatinStrength;
        half4 _EmissionColor;

        struct Input
        {
            float3 localPosition;
            float2 panelUV;
            half bladeMask;
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.localPosition = v.vertex.xyz;
            o.panelUV = v.texcoord.xy;
            // Meshes without vertex colours use white alpha, preserving _Color.
            o.bladeMask = saturate(1.0h - v.color.a);

            // Preserve valid mesh tangents and repair absent or parallel ones.
            float3 n = normalize(v.normal);
            float3 tangent = v.tangent.xyz - n * dot(v.tangent.xyz, n);
            if (dot(tangent, tangent) < 0.0001)
            {
                float3 axis = abs(n.x) < 0.92 ? float3(1, 0, 0) : float3(0, 1, 0);
                tangent = axis - n * dot(axis, n);
            }
            v.tangent = float4(normalize(tangent), v.tangent.w < 0 ? -1 : 1);
        }

        float FilteredScribe(float distanceToLine, float halfWidth, float footprint)
        {
            float aa = max(footprint * 0.65, 0.00005);
            return (1.0 - smoothstep(halfWidth - aa, halfWidth + aa, distanceToLine))
                * saturate(halfWidth / aa);
        }

        float3 HeightNormal(float3 position, float3 normal, float height)
        {
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

            float3 basisNormal = WorldNormalVector(IN, float3(0, 0, 1));
            float3 n = normalize(basisNormal);
            float3 localNormal = normalize(mul(n, (float3x3)unity_ObjectToWorld));
            float2 projection = abs(localNormal.zy);
            projection /= max(projection.x + projection.y, 0.0001);

            // One extremely shallow, filtered microfinish. No broad tool-pass
            // waves: plate curvature and the lighting shape every reflection.
            float scale = max(_DetailScale, 1.0);
            float2 cycles = IN.localPosition.yz * scale;
            float2 resolved = 1.0 - smoothstep(0.14, 0.42, fwidth(cycles));
            float2 grain = sin(cycles * tau) * resolved;
            float machining = (coating * 0.2 + alloy + chassis * 0.05) * _BrushStrength;
            float microHeight = dot(grain, projection) * machining * (0.006 / (tau * scale));

            // Scribes belong to authored UV strips, and vanish cleanly on
            // caps or when subpixel. Their dark recess never becomes a glow.
            float2 uv = IN.panelUV;
            float validUV = step(0.0001, abs(ddx(uv.y)) + abs(ddy(uv.y)));
            float extent = smoothstep(0.06, 0.14, uv.x)
                * (1.0 - smoothstep(0.86, 0.96, uv.x));
            float edgeDistance = min(abs(uv.y - 0.075), abs(uv.y - 0.925));
            float groove = FilteredScribe(edgeDistance, 0.0020, fwidth(uv.y));
            float seamWeight = _SeamStrength * (coating + alloy * 0.65) * extent * validUV;
            float etched = groove * seamWeight;

            float panelBorder = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
            float borderFootprint = max(fwidth(panelBorder), 0.0001);
            float edge = (1.0 - smoothstep(0.004, 0.004 + borderFootprint, panelBorder))
                * validUV * (coating + alloy * 0.7);
            float panelField = smoothstep(0.015, 0.12, panelBorder) * validUV;
            float satinRoughness = (coating * 0.009 + alloy * 0.006)
                * panelField * _SatinStrength;
            float unresolvedRoughness = dot(1.0 - resolved, projection)
                * machining * 0.003;

            float3 finishedNormal = HeightNormal(IN.worldPos, n, microHeight - etched * 0.0001);
            // Invert the interpolated frame so zero detail reproduces the
            // authored normal even on curved meshes and nonuniform transforms.
            float3 tangent = WorldNormalVector(IN, float3(1, 0, 0));
            float3 bitangent = WorldNormalVector(IN, float3(0, 1, 0));
            float3 cofactor = cross(bitangent, basisNormal);
            float frameDeterminant = dot(tangent, cofactor);
            float3 mappedNormal = float3(dot(finishedNormal, cofactor),
                dot(finishedNormal, cross(basisNormal, tangent)),
                dot(finishedNormal, cross(tangent, bitangent)));
            o.Normal = abs(frameDeterminant) > 1e-6
                ? normalize(mappedNormal * sign(frameDeterminant)) : float3(0, 0, 1);
            half3 baseColour = lerp(_Color.rgb, _BladeColor.rgb, saturate(IN.bladeMask));
            o.Albedo = baseColour * (1.0h - etched * 0.58h - edge * 0.2h);
            o.Metallic = _Metallic;
            o.Smoothness = saturate(_Glossiness - satinRoughness
                - unresolvedRoughness - etched * 0.08 - edge * 0.025);
            o.Occlusion = 1.0h - etched * 0.18h - edge * 0.08h;
            o.Emission = _EmissionColor.rgb * _EmissionStrength;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
