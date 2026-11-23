// The hologram map's faces. The depth-only material draws faces invisibly first, so the map knows what's
// in front, and the room tint colors only the nearest face so they don't stack up. The terminal fill uses
// the stencil instead, so each pixel is filled once however many parts overlap there.
// The map draws after ordinary see-through things like smoke and glass, so the invisible faces can't cut holes in them.
Shader "Entropy/HoloMap Faces"
{
    Properties
    {
        _Color ("Color", Color) = (0.35, 0.85, 1, 0.08)
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _Fade ("Fade", Range(0, 1)) = 1

        // Off for terminals and the player, which stay full brightness wherever they are in the map
        [HideInInspector] _DepthDimmed ("Dims With Depth", Float) = 1

        // Set by the map's sectors through each part's property block: 1 powered, 0 unpowered, and how unpowered looks
        [HideInInspector] _Power ("Power", Range(0, 1)) = 1
        [HideInInspector] _UnpoweredBrightness ("Unpowered Brightness", Range(0, 1)) = 0.25
        [HideInInspector] _UnpoweredSaturation ("Unpowered Saturation", Range(0, 1)) = 0.2
        [HideInInspector] _ZWrite ("Depth Write", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0

        // Off by default (always pass, change nothing). The Map Builder sets these on the terminal fill
        [HideInInspector] _StencilRef ("Stencil Ref", Float) = 0
        [HideInInspector] _StencilComp ("Stencil Comp", Float) = 8
        [HideInInspector] _StencilPass ("Stencil Pass", Float) = 0
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+100" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Faces"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite [_ZWrite]
            ZTest LEqual
            ColorMask [_ColorMask]
            Cull [_Cull]

            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilPass]
            }

            // Pushed back slightly, so a line lying on a face still counts as in front of it
            Offset 1, 1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
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

            // Where the map starts and ends from the camera, and how much its far side dims. Set by MapDoors
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
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.depth = -TransformWorldToView(positionWS).z;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = PowerLook(_Color);
                color.a *= _Fade * DepthDim(input.depth);
                return color;
            }
            ENDHLSL
        }
    }
}
