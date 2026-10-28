#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
struct ChrisMetaVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionOS : TEXCOORD1;
};
ChrisMetaVaryings ChrisMetaVertex(ChrisAttributes input)
{
    ChrisMetaVaryings o;
    o.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.lightmapUV, input.dynamicLightmapUV, unity_LightmapST, unity_DynamicLightmapST);
    o.uv = input.uv;
    o.positionOS = input.positionOS.xyz;
    return o;
}
half4 ChrisMetaFragment(ChrisMetaVaryings i) : SV_Target
{
    half4 base = ChrisBase(i.uv);
    ChrisClip(i.uv, i.positionOS, base.a);
    MetaInput meta = (MetaInput)0;
    meta.Albedo = base.rgb;
    // Bake the static emission texture; animated pulses must not flicker the bake.
    #if defined(_EMISSION_ON)
        meta.Emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv * _EmissionMap_ST.xy + _EmissionMap_ST.zw).rgb
            * _EmissionColor.rgb * SAMPLE_TEXTURE2D(_EffectMask, sampler_EffectMask, i.uv).a;
    #endif
    return UnityMetaFragment(meta);
}
