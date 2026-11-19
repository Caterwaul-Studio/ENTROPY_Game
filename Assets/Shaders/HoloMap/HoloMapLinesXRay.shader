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
    CBUFFER_END

    half DistanceFade(float depth)
    {
        return lerp(1, _FarOpacity, saturate((depth - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001)));
    }

    // The nearest and farthest the map reaches from the camera, set by MapDoors
    float _HoloMapNear;
    float _HoloMapFar;

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
        half4 color = _Color;
        color.a *= DistanceFade(input.depth);
        return color;
    }

    half4 FragSeeThrough(Varyings input) : SV_Target
    {
        float range = (_HoloMapFar - _HoloMapNear) * _XRayShare;
        if (range <= 0) discard;
        float fade = 1 - saturate((input.depth - _HoloMapNear) / range);

        // Only when the camera is close to the map
        float closeness = 1 - saturate((_HoloMapNear - _SeeThroughNear) / max(_SeeThroughFar - _SeeThroughNear, 0.001));
        half4 color = _Color;
        color.a *= fade * closeness * _XRayStrength * DistanceFade(input.depth);
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
