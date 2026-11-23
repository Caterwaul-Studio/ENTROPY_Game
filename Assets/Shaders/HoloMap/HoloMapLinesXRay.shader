// Lines that show through the map near the front and are hidden further back. Used by working doors, so the
// doors right in front of the viewer stay readable without every door in the map showing at once.
// One pass draws them where nothing is in front, the other where something is, fading out with depth.
// MapDoors sets where the map starts and ends for each camera; without it only the first pass shows.
Shader "Entropy/HoloMap Lines XRay"
{
    Properties
    {
        _Color ("Color", Color) = (0.35, 0.85, 1, 0.6)
        _Lift ("Lift Toward Camera", Range(0, 0.05)) = 0.005
        _XRayShare ("See-Through Depth", Range(0, 1)) = 0.35
        _XRayStrength ("See-Through Strength", Range(0, 1)) = 0.6

        // The see-through only happens when the camera is near the map: full within the first distance, none past the second
        _SeeThroughNear ("See-Through Within (m)", Float) = 1.5
        _SeeThroughFar ("See-Through Until (m)", Float) = 3

        // Lines fade toward Far Opacity between these distances, so a far-off map reads lighter
        _FadeStart ("Fade Start (m)", Float) = 2
        _FadeEnd ("Fade End (m)", Float) = 8
        _FarOpacity ("Far Opacity", Range(0, 1)) = 0.35

        // Set by the holo table while the map fades in and out, through each part's property block
        [HideInInspector] _Fade ("Fade", Range(0, 1)) = 1

        // Off for terminals and the player, which stay full brightness wherever they are in the map
        [HideInInspector] _DepthDimmed ("Dims With Depth", Float) = 1

        // Set by the map's sectors through each part's property block: 1 powered, 0 unpowered, and how unpowered looks
        [HideInInspector] _Power ("Power", Range(0, 1)) = 1
        [HideInInspector] _UnpoweredBrightness ("Unpowered Brightness", Range(0, 1)) = 0.25
        [HideInInspector] _UnpoweredSaturation ("Unpowered Saturation", Range(0, 1)) = 0.2
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        half4 _Color;
        float _Lift;
        float _XRayShare;
        float _XRayStrength;
        float _SeeThroughNear;
        float _SeeThroughFar;
        float _FadeStart;
        float _FadeEnd;
        float _FarOpacity;
        float _Fade;
        float _DepthDimmed;
        float _Power;
        float _UnpoweredBrightness;
        float _UnpoweredSaturation;
    CBUFFER_END

    // An unpowered sector is drawn dim and greyed out
    half4 PowerLook(half4 color)
    {
        half grey = dot(color.rgb, half3(0.299, 0.587, 0.114));
        color.rgb = lerp(grey.xxx, color.rgb, lerp(_UnpoweredSaturation, 1, _Power));
        color.a *= lerp(_UnpoweredBrightness, 1, _Power);
        return color;
    }

    half DistanceFade(float depth)
    {
        return _Fade * lerp(1, _FarOpacity, saturate((depth - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001)));
    }

    // The nearest and farthest the map reaches from the camera, and how much its far side dims. Set by MapDoors
    float _HoloMapNear;
    float _HoloMapFar;
    float _HoloMapDepthDim;

    // Full brightness at the map's nearest point, dimming toward its farthest, so what's in front reads first
    half DepthDim(float depth)
    {
        float range = _HoloMapFar - _HoloMapNear;
        if (range <= 0.0001) return 1;
        // Fully dimmed three quarters of the way back, since the farthest corner alone is too little to see.
        // Eased, so the front stays bright a little longer before it fades
        half t = saturate((depth - _HoloMapNear) / (range * 0.75));
        return 1 - _HoloMapDepthDim * _DepthDimmed * t * t * (3 - 2 * t);
    }

    struct Attributes
    {
        float3 positionOS : POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float depth : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        // Lifted toward the camera like the other map lines, so they sit on top of their own faces
        float3 positionWS = TransformObjectToWorld(input.positionOS);
        float depth = -TransformWorldToView(positionWS).z;
        positionWS += GetWorldSpaceNormalizeViewDir(positionWS) * depth * _Lift;

        output.positionCS = TransformWorldToHClip(positionWS);
        output.depth = depth;
        return output;
    }

    half4 FragShown(Varyings input) : SV_Target
    {
        half4 color = PowerLook(_Color);
        color.a *= DistanceFade(input.depth) * DepthDim(input.depth);
        return color;
    }

    half4 FragSeeThrough(Varyings input) : SV_Target
    {
        float range = (_HoloMapFar - _HoloMapNear) * _XRayShare;
        if (range <= 0) discard;
        float fade = 1 - saturate((input.depth - _HoloMapNear) / range);

        // Only when the camera is close to the map
        float closeness = 1 - saturate((_HoloMapNear - _SeeThroughNear) / max(_SeeThroughFar - _SeeThroughNear, 0.001));
        half4 color = PowerLook(_Color);
        color.a *= fade * closeness * _XRayStrength * DistanceFade(input.depth) * DepthDim(input.depth);
        if (color.a <= 0.001) discard;
        return color;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+101" "IgnoreProjector" = "True" }

        // Where nothing is in front
        Pass
        {
            Name "Shown"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Stencil { Ref 1 ReadMask 1 Comp NotEqual }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragShown
            #pragma multi_compile_instancing
            ENDHLSL
        }

        // Where something is in front: only near the front of the map
        Pass
        {
            Name "SeeThrough"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Greater
            Cull Off
            Stencil { Ref 1 ReadMask 1 Comp NotEqual }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragSeeThrough
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
