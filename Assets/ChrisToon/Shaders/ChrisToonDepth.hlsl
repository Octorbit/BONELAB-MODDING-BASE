#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/EncodeNormalsTexture.hlsl"
float3 _LightDirection;
float3 _LightPosition;
struct ChrisDepthVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionOS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    half4 tangentWS : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};
ChrisDepthVaryings ChrisDepthVertex(ChrisAttributes input)
{
    ChrisDepthVaryings o = (ChrisDepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    o.uv = input.uv;
    o.positionOS = input.positionOS.xyz;
    o.normalWS = TransformObjectToWorldNormal(input.normalOS);
    o.tangentWS = half4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w * GetOddNegativeScale());
    return o;
}
ChrisDepthVaryings ChrisShadowVertex(ChrisAttributes input)
{
    ChrisDepthVaryings o = ChrisDepthVertex(input);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 direction = _LightDirection;
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
        direction = normalize(_LightPosition - positionWS);
    #endif
    // Bias must follow the light-facing side for a two-sided surface.
    half3 normal = o.normalWS * (dot(o.normalWS, direction) < 0 ? -1 : 1);
    o.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normal, direction));
    #if UNITY_REVERSED_Z
        o.positionCS.z = min(o.positionCS.z, o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
        o.positionCS.z = max(o.positionCS.z, o.positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif
    return o;
}
half4 ChrisDepthFragment(ChrisDepthVaryings i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    ChrisClip(i.uv, i.positionOS, ChrisBase(i.uv).a);
    return 0;
}
half4 ChrisNormalsFragment(ChrisDepthVaryings i, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    ChrisClip(i.uv, i.positionOS, ChrisBase(i.uv).a);
    half3 n = ChrisNormal(i.uv, i.normalWS, i.tangentWS, IS_FRONT_VFACE(frontFace, 1.0, -1.0));
    #if defined(_GBUFFER_NORMALS_OCT)
        float2 octNormal = PackNormalOctQuadEncode(n);
        return half4(PackFloat2To888(saturate(octNormal * 0.5 + 0.5)), 0);
    #else
        return half4(EncodeWSNormalForNormalsTex(n), 0);
    #endif
}
