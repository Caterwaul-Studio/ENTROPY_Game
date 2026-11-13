// The hologram map's lines. They draw after the map's faces and are hidden wherever a face is in front,
// and wherever a map label's letters are, so labels stay readable without a background.
Shader "Entropy/HoloMap Lines"
{
    Properties
    {
        _Color ("Color", Color) = (0.35, 0.85, 1, 1)
        _Lift ("Lift Toward Camera", Range(0, 0.05)) = 0.005
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+101" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Lines"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            // Label letters mark this stencil value as they draw (the label material sets it up in the Map Builder)
            Stencil
            {
                Ref 1
                ReadMask 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Lift;
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

                // Lines lie on the faces they outline, and merging can move them slightly behind.
                // Lifting them toward the camera by a share of their distance keeps them on top at any map scale
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float depth = -TransformWorldToView(positionWS).z;
                positionWS += GetWorldSpaceNormalizeViewDir(positionWS) * depth * _Lift;

                output.positionCS = TransformWorldToHClip(positionWS);
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
