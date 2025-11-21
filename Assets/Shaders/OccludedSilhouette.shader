Shader "Custom/URP/OccludedSilhouette"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0, 0, 0, 0)
        _OutlineColor("Outline Color", Color) = (0.07, 0.93, 1, 0.8)
        _OutlineWidth("Outline Width", Range(0.01, 5)) = 1
        _DepthBias("Depth Bias", Range(0.0001, 0.05)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
        }

        LOD 100

        Pass
        {
            Name "OccludedOutline"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _OutlineColor;
                half _OutlineWidth;
                half _DepthBias;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionVS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionVS = positionInputs.positionVS;
                output.normalWS = normalInputs.normalWS;
                float3 positionWS = positionInputs.positionWS;
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.positionCS.xy / input.positionCS.w;
                #if UNITY_UV_STARTS_AT_TOP
                    screenUV.y = 1.0 - screenUV.y;
                #endif

                float sceneDepth01 = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(sceneDepth01, _ZBufferParams);
                float fragmentEyeDepth = -input.positionVS.z;

                bool isOccluded = sceneEyeDepth + _DepthBias < fragmentEyeDepth;
                if (!isOccluded)
                {
                    discard;
                }

                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                float3 viewDirWS = SafeNormalize(-input.viewDirWS);
                half fresnel = pow(saturate(1.0h - dot(normalWS, viewDirWS)), saturate(_OutlineWidth) * 2.5h + 1.0h);

                half alpha = _OutlineColor.a * fresnel;
                clip(alpha - 0.001h);

                half3 color = lerp(_BaseColor.rgb, _OutlineColor.rgb, fresnel);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
