Shader "Custom/EightSegmentColor"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _SlotCount ("Slot Count", Float) = 8
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define SLOT_ARRAY_SIZE 16

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _SlotCount;
            float4 _SlotColors[SLOT_ARRAY_SIZE];

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_BaseMap, i.uv) * _BaseColor;

                int slotCount = clamp((int)_SlotCount, 1, SLOT_ARRAY_SIZE);
                float scaled = saturate(i.uv.x) * slotCount;
                int index = (int)floor(scaled);
                index = clamp(index, 0, slotCount - 1);

                col *= _SlotColors[index];
                return col;
            }
            ENDHLSL
        }
    }
}
