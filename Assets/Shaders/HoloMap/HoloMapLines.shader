// The hologram map's lines. They draw after the map's faces and are hidden wherever a face is in front,
// and wherever a map label's letters are, so labels stay readable without a background.
Shader "Entropy/HoloMap Lines"
{
    Properties
    {
        _Color ("Color", Color) = (0.35, 0.85, 1, 1)
        _Lift ("Lift Toward Camera", Range(0, 0.05)) = 0.005

        // Lines are one pixel wide, so from far away they crowd together. They fade toward Far Opacity
        // between these distances from the camera, so a far-off map reads lighter instead of solid
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

                // Lines lie on the faces they outline, and merging can move them slightly behind.
                // Lifting them toward the camera by a share of their distance keeps them on top at any map scale
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float depth = -TransformWorldToView(positionWS).z;
                positionWS += GetWorldSpaceNormalizeViewDir(positionWS) * depth * _Lift;

                output.positionCS = TransformWorldToHClip(positionWS);
                output.depth = depth;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = PowerLook(_Color);
                color.a *= _Fade * DepthDim(input.depth) * lerp(1, _FarOpacity, saturate((input.depth - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001)));
                return color;
            }
            ENDHLSL
        }
    }
}
