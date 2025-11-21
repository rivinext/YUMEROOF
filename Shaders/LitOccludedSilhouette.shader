Shader "Custom/LitOccludedSilhouette"
{
    Properties
    {
        // --- URP Lit 基本プロパティ（Inspector を共用するために定義） ---
        _WorkflowMode("Workflow Mode", Float) = 1.0
        _Surface("Surface Type", Float) = 0.0
        _Blend("Blend Mode", Float) = 0.0
        _Cull("Cull Mode", Float) = 2.0
        _AlphaClip("Alpha Clipping", Float) = 0.0
        _AlphaToMask("Alpha To Mask", Float) = 0.0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _ReceiveShadows("Receive Shadows", Float) = 1.0
        _QueueOffset("Queue Offset", Float) = 0.0
        _ZWriteControl("ZWrite Control", Float) = 0.0
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _SpecColor("Specular Color", Color) = (0.2,0.2,0.2,1)
        _SpecGlossMap("Specular Gloss Map", 2D) = "white" {}
        _MetallicGlossMap("Metallic Gloss Map", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0.0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _BumpScale("Normal Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}
        _Parallax("Height Scale", Range(0.005, 0.08)) = 0.02
        _ParallaxMap("Height Map", 2D) = "black" {}
        _OcclusionStrength("Occlusion", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (0,0,0)
        _EmissionMap("Emission Map", 2D) = "white" {}
        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        _DetailNormalMapScale("Detail Normal Scale", Float) = 1.0
        _DetailNormalMap("Detail Normal Map", 2D) = "bump" {}
        _ClearCoatMask("Coat Mask", Range(0.0, 1.0)) = 0.0
        _ClearCoatSmoothness("Coat Smoothness", Range(0.0, 1.0)) = 0.5
        _EnvironmentReflections("Environment Reflections", Float) = 1.0
        _SpecularHighlights("Specular Highlights", Float) = 1.0
        // --- シルエット用プロパティ ---
        _SilhouetteColor("Silhouette Color", Color) = (0,1,1,1)
        _OutlineWidth("Outline Width", Range(0,0.05)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 300

        // URP Lit の標準パスを再利用
        UsePass "Universal Render Pipeline/Lit/ForwardLit"
        UsePass "Universal Render Pipeline/Lit/GBuffer"
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"

        // --- 隠れたときだけ描画されるシルエットパス ---
        Pass
        {
            Name "OccludedSilhouette"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _SilhouetteColor;
                float _OutlineWidth;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                posWS += normalWS * _OutlineWidth;
                o.positionHCS = TransformWorldToHClip(posWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return _SilhouetteColor;
            }
            ENDHLSL
        }
    }
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
