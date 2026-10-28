Shader "ChrisToon/ChrisToon"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Texture (RGBA)", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Render Faces (Off = Both)", Float) = 0
        _BackfaceColor("Backface Tint", Color) = (1,1,1,1)
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Cutout", Float) = 0
        _Cutoff("Cutout Threshold", Range(0,1)) = 0.5
        [Toggle(_NORMALMAP)] _NormalEnabled("Normal Mapping", Float) = 0
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0,2)) = 1
        _ShadeColor("Shadow Color", Color) = (0.68,0.57,0.78,1)
        _DeepShadeColor("Deep Shadow Color", Color) = (0.38,0.32,0.52,1)
        _ShadeThreshold("Light / Shadow Border", Range(0,1)) = 0.5
        _DeepThreshold("Deep Shadow Border", Range(0,1)) = 0.18
        _ShadeSoftness("Border Softness", Range(0.001,0.3)) = 0.025
        _ShadowStrength("Received Shadow Strength", Range(0,1)) = 0.85
        _AmbientStrength("Environment Lighting", Range(0,2)) = 1
        _LightFloor("Minimum Brightness", Range(0,1)) = 0.12
        _LightLimit("Maximum Light Intensity", Range(1,10)) = 3
        [NoScaleOffset] _OcclusionMap("Occlusion (G)", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1
        [NoScaleOffset] _EffectMask("Effect Mask (R Spec / G Rim / B Matcap / A Glow)", 2D) = "white" {}
        [Toggle(_SPECULAR_ON)] _SpecularEnabled("Toon Highlights", Float) = 1
        [HDR] _SpecColor("Highlight Color", Color) = (1,0.94,0.87,1)
        _SpecularStrength("Highlight Strength", Range(0,3)) = 0.3
        _SpecularSize("Highlight Size", Range(0.01,1)) = 0.25
        _SpecularSoftness("Highlight Softness", Range(0.001,0.3)) = 0.035
        _Anisotropy("Hair Anisotropy", Range(0,1)) = 0
        _HairShift("Hair Highlight Shift", Range(-1,1)) = 0
        [Toggle(_RIM_ON)] _RimEnabled("Rim Light", Float) = 1
        [HDR] _RimColor("Rim Color", Color) = (0.45,0.7,1,1)
        _RimStrength("Rim Strength", Range(0,3)) = 0.25
        _RimPower("Rim Power", Range(0.5,10)) = 3
        _RimThreshold("Rim Border", Range(0,1)) = 0.45
        _RimSoftness("Rim Softness", Range(0.001,0.5)) = 0.1
        [Toggle(_MATCAP_ON)] _MatcapEnabled("Matcap", Float) = 0
        [NoScaleOffset] _MatcapMap("Matcap Texture", 2D) = "black" {}
        [HDR] _MatcapColor("Matcap Tint", Color) = (1,1,1,1)
        _MatcapStrength("Matcap Strength", Range(0,2)) = 0.5
        [Enum(Add,0,Multiply,1)] _MatcapBlend("Matcap Blend", Float) = 0
        [Toggle(_EMISSION_ON)] _EmissionEnabled("Animated Emission", Float) = 0
        _EmissionMap("Emission Texture", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0,1.5,2,1)
        _EmissionScroll("Emission UV Speed (XY)", Vector) = (0,0,0,0)
        _EmissionPulse("Pulse Amount", Range(0,1)) = 0
        _EmissionPulseSpeed("Pulse Cycles Per Second", Range(0,10)) = 1
        _EmissionScanStrength("Scanline Amount", Range(0,1)) = 0
        _EmissionScanScale("Scanline Density (Object Space)", Float) = 16
        _EmissionScanSpeed("Scanline Speed", Float) = 1
        [Toggle(_DISSOLVE_ON)] _DissolveEnabled("Dissolve", Float) = 0
        _DissolveMap("Dissolve Noise (R)", 2D) = "white" {}
        _DissolveAmount("Dissolve Amount", Range(0,1)) = 0
        _DissolveScale("Procedural Noise Scale", Float) = 12
        _DissolveNoiseMix("Texture / Procedural Noise", Range(0,1)) = 1
        _DissolveEdgeWidth("Dissolve Edge Width", Range(0.001,0.2)) = 0.035
        [HDR] _DissolveEdgeColor("Dissolve Edge Glow", Color) = (0,3,5,1)
        [Toggle] _OutlineEnabled("Outline", Float) = 1
        [HDR] _OutlineColor("Outline Color", Color) = (0.018,0.012,0.032,1)
        _OutlineWidth("Outline Width (Pixels)", Range(0,8)) = 1.5
        _OutlineMaxWidth("Outline World Width Limit (Meters)", Range(0.001,0.05)) = 0.012
        [NoScaleOffset] _OutlineMask("Outline Width Mask (R)", 2D) = "white" {}
        _OutlineBaseMix("Outline Base Color Mix", Range(0,1)) = 0.15
        [HideInInspector] _QueueOffset("Queue Offset", Float) = 0
    }
    SubShader
    {
        // PC BONELAB forces Equal depth testing on opaque draws. A hull cannot
        // match the unexpanded depth prepass. Draw immediately after opaques,
        // retaining opaque blending and depth writes, to preserve the silhouette.
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest+51" }
        LOD 300
        HLSLINCLUDE
        #pragma target 3.5
        #include "ChrisToonInput.hlsl"
        ENDHLSL

        // An unlit hull followed by UniversalForward works with the stock game renderer.
        // It needs no RendererFeature or runtime MonoBehaviour in the pallet.
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma vertex ChrisOutlineVertex
            #pragma fragment ChrisOutlineFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _VOLUMETRICS_ENABLED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _DISSOLVE_ON
            #include "ChrisToonOutline.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On
            HLSLPROGRAM
            #pragma vertex ChrisVertex
            #pragma fragment ChrisFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _VOLUMETRICS_ENABLED
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _NORMALMAP
            #pragma shader_feature_local_fragment _SPECULAR_ON
            #pragma shader_feature_local_fragment _RIM_ON
            #pragma shader_feature_local_fragment _MATCAP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _DISSOLVE_ON
            #include "ChrisToonForward.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ChrisShadowVertex
            #pragma fragment ChrisDepthFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _DISSOLVE_ON
            #include "ChrisToonDepth.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull [_Cull]
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ChrisDepthVertex
            #pragma fragment ChrisDepthFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _DISSOLVE_ON
            #include "ChrisToonDepth.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            Cull [_Cull]
            ZWrite On
            HLSLPROGRAM
            #pragma vertex ChrisDepthVertex
            #pragma fragment ChrisNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _NORMALMAP
            #pragma shader_feature_local_fragment _DISSOLVE_ON
            #include "ChrisToonDepth.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags { "LightMode"="Meta" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex ChrisMetaVertex
            #pragma fragment ChrisMetaFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _DISSOLVE_ON
            #include "ChrisToonMeta.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "ChrisToon.Editor.ChrisToonShaderGUI"
    Fallback Off
}
