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
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}
