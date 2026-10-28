#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct ChrisVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    half4 tangentWS : TEXCOORD3;
    float3 positionOS : TEXCOORD4;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
    half4 fogAndVertexLight : TEXCOORD6;
    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
        half3 backVertexLight : TEXCOORD7;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

ChrisVaryings ChrisVertex(ChrisAttributes input)
{
    ChrisVaryings o = (ChrisVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs n = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    o.positionCS = p.positionCS;
    o.positionWS = p.positionWS;
    o.positionOS = input.positionOS.xyz;
    o.normalWS = n.normalWS;
    o.tangentWS = half4(n.tangentWS, input.tangentOS.w * GetOddNegativeScale());
    o.uv = input.uv;
    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, o.lightmapUV);
    OUTPUT_SH(o.normalWS, o.vertexSH);
    o.fogAndVertexLight = half4(ComputeFogFactor(p.positionCS.z), VertexLighting(p.positionWS, n.normalWS));
    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
        o.backVertexLight = _Cull < 0.5 ? VertexLighting(p.positionWS, -n.normalWS) : o.fogAndVertexLight.yzw;
    #endif
    return o;
}

half3 ChrisLight(Light light, half3 n, half3 v, half3 tangent, half3 albedo, half highlightMask)
{
    // Half Lambert gives an art-directable, stable three-tone ramp.
    half ndl = dot(n, light.direction) * 0.5 + 0.5;
    half shadow = lerp(1.0, light.shadowAttenuation, _ShadowStrength);
    half ramp = min(ndl, shadow);
    half feather = max(_ShadeSoftness, fwidth(ramp));
    half litBand = smoothstep(_ShadeThreshold - feather, _ShadeThreshold + feather, ramp);
    half deepBorder = min(_DeepThreshold, _ShadeThreshold - 0.001);
    half shadeBand = smoothstep(deepBorder - feather, deepBorder + feather, ramp);
    half3 shade = lerp(_DeepShadeColor.rgb, _ShadeColor.rgb, shadeBand);
    shade = lerp(shade, half3(1,1,1), litBand);
    half3 color = albedo * shade;
    #if defined(_SPECULAR_ON)
        half3 h = SafeNormalize(light.direction + v);
        half regular = saturate(dot(n, h));
        half3 strand = SafeNormalize(tangent + n * _HairShift);
        half th = dot(strand, h);
        half hair = sqrt(saturate(1.0 - th * th));
        half spec = lerp(regular, hair, _Anisotropy);
        half border = 1.0 - _SpecularSize * 0.5;
        half aa = max(_SpecularSoftness, fwidth(spec));
        half highlight = smoothstep(border - aa, border + aa, spec);
        color += highlight * _SpecColor.rgb * _SpecularStrength * highlightMask * saturate(dot(n, light.direction)) * shadow;
    #endif
    return color * light.color.rgb * light.distanceAttenuation;
}

half4 ChrisFragment(ChrisVaryings i, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    half4 base = ChrisBase(i.uv);
    half edge = ChrisClip(i.uv, i.positionOS, base.a);
    half faceSign = IS_FRONT_VFACE(frontFace, 1.0, -1.0);
    base.rgb *= IS_FRONT_VFACE(frontFace, half3(1,1,1), _BackfaceColor.rgb);
    half3 n = ChrisNormal(i.uv, i.normalWS, i.tangentWS, faceSign);
    half3 v = SafeNormalize(GetWorldSpaceViewDir(i.positionWS));
    half4 masks = SAMPLE_TEXTURE2D(_EffectMask, sampler_EffectMask, i.uv);
    half ao = lerp(1.0, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, i.uv).g, _OcclusionStrength);
    half3 gi;
    #if defined(LIGHTMAP_ON)
        gi = SAMPLE_GI(i.lightmapUV, i.vertexSH, n);
    #else
        // Fragment SH is necessary for correctly lit backfaces.
        gi = SampleSH(n);
    #endif
    half4 shadowMask = SAMPLE_SHADOWMASK(i.lightmapUV);
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS), i.positionWS, shadowMask);
    half3 indirect = max(gi * _AmbientStrength, _LightFloor.xxx) * ao;
    half3 color = base.rgb * indirect;
    color += ChrisLight(mainLight, n, v, i.tangentWS.xyz, base.rgb, masks.r);
    #if defined(_ADDITIONAL_LIGHTS)
        uint count = GetAdditionalLightsCount();
        LIGHT_LOOP_BEGIN(count)
            Light light = GetAdditionalLight(lightIndex, i.positionWS, shadowMask);
            color += ChrisLight(light, n, v, i.tangentWS.xyz, base.rgb, masks.r);
        LIGHT_LOOP_END
    #elif defined(_ADDITIONAL_LIGHTS_VERTEX)
        color += base.rgb * IS_FRONT_VFACE(frontFace, i.fogAndVertexLight.yzw, i.backVertexLight);
    #endif
    // Limit runaway map lights before applying HDR effects.
    half peak = max(max(color.r, color.g), color.b);
    color *= min(1.0, _LightLimit / max(peak, 0.0001));
    #if defined(_MATCAP_ON)
        half3 viewNormal = normalize(mul((half3x3)GetWorldToViewMatrix(), n));
        float2 matcapUV = viewNormal.xy * 0.5 + 0.5;
        half3 matcap = SAMPLE_TEXTURE2D(_MatcapMap, sampler_MatcapMap, matcapUV).rgb * _MatcapColor.rgb;
        half weight = masks.b * _MatcapStrength;
        color = _MatcapBlend < 0.5 ? color + matcap * weight : color * lerp(half3(1,1,1), matcap, saturate(weight));
    #endif
    #if defined(_RIM_ON)
        half fresnel = pow(1.0 - saturate(dot(n, v)), _RimPower);
        half aa = max(_RimSoftness, fwidth(fresnel));
        half rim = smoothstep(_RimThreshold - aa, _RimThreshold + aa, fresnel);
        color += rim * _RimColor.rgb * _RimStrength * masks.g;
    #endif
    color += ChrisEmission(i.uv, i.positionOS, masks.a) + edge * _DissolveEdgeColor.rgb;
    // SLZ uses sky-colored fog and a camera-to-surface vector.
    color = MixFog(color, i.positionWS - GetCameraPositionWS(), i.fogAndVertexLight.x);
    return Volumetrics(half4(color, 1), i.positionWS);
}
