#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
struct ChrisOutlineVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionOS : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
    float fog : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};
ChrisOutlineVaryings ChrisOutlineVertex(ChrisAttributes input)
{
    ChrisOutlineVaryings o = (ChrisOutlineVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.uv = input.uv;
    o.positionOS = input.positionOS.xyz;
    o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float4 clipPosition = TransformWorldToHClip(o.positionWS);
    float4 clipNormal = mul(GetWorldToHClipMatrix(), float4(normalWS, 0));
    // Derivative of projected position: works for perspective and orthographic cameras.
    float2 projected = clipNormal.xy * clipPosition.w - clipPosition.xy * clipNormal.w;
    float2 pixelDirection = projected * _ScaledScreenParams.xy;
    pixelDirection *= rsqrt(max(dot(pixelDirection, pixelDirection), 1e-8));
    float widthMask = SAMPLE_TEXTURE2D_LOD(_OutlineMask, sampler_OutlineMask, input.uv, 0).r;
    float2 offset = pixelDirection * (2.0 * _OutlineWidth * widthMask * _OutlineEnabled) / _ScaledScreenParams.xy * clipPosition.w;
    float4 limitPosition = TransformWorldToHClip(o.positionWS + normalWS * _OutlineMaxWidth);
    float2 limitOffset = (limitPosition.xy / max(abs(limitPosition.w), 1e-5) - clipPosition.xy / max(abs(clipPosition.w), 1e-5)) * clipPosition.w;
    offset *= min(1.0, length(limitOffset) / max(length(offset), 1e-6));
    o.positionCS = clipPosition;
    o.positionCS.xy += offset;
    o.fog = ComputeFogFactor(clipPosition.z);
    return o;
}
half4 ChrisOutlineFragment(ChrisOutlineVaryings i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    clip(_OutlineEnabled - 0.5);
    clip(_OutlineWidth - 0.0001);
    half4 base = ChrisBase(i.uv);
    ChrisClip(i.uv, i.positionOS, base.a);
    half3 color = _OutlineColor.rgb * lerp(half3(1,1,1), base.rgb, _OutlineBaseMix);
    color = MixFog(color, i.positionWS - GetCameraPositionWS(), i.fog);
    return Volumetrics(half4(color,1), i.positionWS);
}
