Shader "Custom/URP/OccludedSilhouette"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _Alpha("Alpha", Range(0, 1)) = 1
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
            Name "OccludedSilhouette"
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Alpha;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

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
                float2 uv : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = positionInputs.positionCS;
                output.positionVS = positionInputs.positionVS;
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

                if (sceneEyeDepth >= fragmentEyeDepth)
                {
                    discard;
                }

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = tex.a * _Alpha;
                clip(alpha - 0.001h);

                return half4(tex.rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
