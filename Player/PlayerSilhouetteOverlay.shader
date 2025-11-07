Shader "Player/PlayerSilhouetteOverlay"
{
    Properties
    {
        _Color("Silhouette Color", Color) = (0.9098039, 0.4666667, 0.2705882, 0.5)
        _MainTex("Mask Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Geometry+10" "RenderType"="Transparent" }
        LOD 100
        Cull Back
        ZTest Greater  // ★変更：遮蔽されている部分のみ描画
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }

    FallBack Off
}
