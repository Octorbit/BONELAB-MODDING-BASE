#ifndef CHRIS_TOON_INPUT
#define CHRIS_TOON_INPUT
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

// Identical layout in every pass for the SRP batcher.
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST, _BumpMap_ST, _EmissionMap_ST, _DissolveMap_ST;
half4 _BaseColor, _BackfaceColor, _ShadeColor, _DeepShadeColor;
half4 _SpecColor, _RimColor, _MatcapColor, _EmissionColor, _DissolveEdgeColor, _OutlineColor;
float4 _EmissionScroll;
float _Cull, _AlphaClip, _Cutoff, _NormalEnabled, _BumpScale;
float _ShadeThreshold, _DeepThreshold, _ShadeSoftness, _ShadowStrength;
float _AmbientStrength, _LightFloor, _LightLimit, _OcclusionStrength;
float _SpecularEnabled, _SpecularStrength, _SpecularSize, _SpecularSoftness, _Anisotropy, _HairShift;
float _RimEnabled, _RimStrength, _RimPower, _RimThreshold, _RimSoftness;
float _MatcapEnabled, _MatcapStrength, _MatcapBlend;
float _EmissionEnabled, _EmissionPulse, _EmissionPulseSpeed;
float _EmissionScanStrength, _EmissionScanScale, _EmissionScanSpeed;
float _DissolveEnabled, _DissolveAmount, _DissolveScale, _DissolveNoiseMix, _DissolveEdgeWidth;
float _OutlineEnabled, _OutlineWidth, _OutlineMaxWidth, _OutlineBaseMix, _QueueOffset;
CBUFFER_END
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_EffectMask); SAMPLER(sampler_EffectMask);
TEXTURE2D(_MatcapMap); SAMPLER(sampler_MatcapMap);
TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
TEXTURE2D(_DissolveMap); SAMPLER(sampler_DissolveMap);
TEXTURE2D(_OutlineMask); SAMPLER(sampler_OutlineMask);

struct ChrisAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

half4 ChrisBase(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv * _BaseMap_ST.xy + _BaseMap_ST.zw) * _BaseColor;
}

float ChrisHash(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

// Smooth value noise stays attached to the mesh, including moving avatars.
float ChrisNoise(float3 p)
{
    float3 cell = floor(p), f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(lerp(ChrisHash(cell), ChrisHash(cell + float3(1,0,0)), f.x),
                     lerp(ChrisHash(cell + float3(0,1,0)), ChrisHash(cell + float3(1,1,0)), f.x), f.y),
                lerp(lerp(ChrisHash(cell + float3(0,0,1)), ChrisHash(cell + float3(1,0,1)), f.x),
                     lerp(ChrisHash(cell + float3(0,1,1)), ChrisHash(cell + 1.0), f.x), f.y), f.z);
}

// Shared by color, hull, shadow, depth, normals and baking passes.
half ChrisClip(float2 uv, float3 positionOS, half alpha)
{
    #if defined(_ALPHATEST_ON)
        clip(alpha - _Cutoff);
    #endif
    half edge = 0;
    #if defined(_DISSOLVE_ON)
        if (_DissolveAmount > 0.0)
        {
            if (_DissolveAmount >= 1.0) clip(-1);
            float noise = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap,
                uv * _DissolveMap_ST.xy + _DissolveMap_ST.zw).r;
            if (_DissolveNoiseMix > 0.0)
                noise = lerp(noise, ChrisNoise(positionOS * _DissolveScale), _DissolveNoiseMix);
            float distanceToEdge = noise - _DissolveAmount;
            clip(distanceToEdge);
            edge = 1.0 - smoothstep(0, max(_DissolveEdgeWidth, 0.0001), distanceToEdge);
        }
    #endif
    return edge;
}

half3 ChrisNormal(float2 uv, half3 normalWS, half4 tangentWS, half faceSign)
{
    half3 n = normalize(normalWS);
    #if defined(_NORMALMAP)
        half3 t = SafeNormalize(tangentWS.xyz);
        half3 b = cross(n, t) * tangentWS.w;
        half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap,
            uv * _BumpMap_ST.xy + _BumpMap_ST.zw), _BumpScale);
        n = SafeNormalize(mul(normalTS, half3x3(t, b, n)));
    #endif
    return n * faceSign;
}

half3 ChrisEmission(float2 uv, float3 positionOS, half mask)
{
    half3 glow = 0;
    #if defined(_EMISSION_ON)
        float2 emissionUV = uv * _EmissionMap_ST.xy + _EmissionMap_ST.zw + _Time.y * _EmissionScroll.xy;
        half pulse = lerp(1.0, 0.5 + 0.5 * sin(_Time.y * _EmissionPulseSpeed * TWO_PI), _EmissionPulse);
        half scan = lerp(1.0, 0.5 + 0.5 * sin((positionOS.y * _EmissionScanScale - _Time.y * _EmissionScanSpeed) * TWO_PI), _EmissionScanStrength);
        glow = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, emissionUV).rgb * _EmissionColor.rgb * pulse * scan * mask;
    #endif
    return glow;
}
#endif
