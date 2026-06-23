Shader "Custom/URP_AbsoluteFadeSphere"
{
    Properties
    {
        // URP 預設的主要顏色屬性名稱通常為 _BaseColor
        [MainColor] _BaseColor("Base Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        // 關鍵 Tags：宣告這是在 URP 執行，並且 Queue 設為 4100 (Overlay+100)
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Overlay+100" 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "Unlit"
            
            // --- 最核心的魔法設定 ---
            ZWrite Off                  // 關閉深度寫入
            ZTest Always                // 永遠通過深度測試 (無視霧氣與場景模型)
            Cull Front                  // 剔除正面，只渲染球體內側
            Blend SrcAlpha OneMinusSrcAlpha // 開啟透明度混合
            // ------------------------

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 引入 URP 核心函式庫
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            // 為了支援 URP 的 SRP Batcher (效能優化)，變數必須包在 CBUFFER 中
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                // 將物件空間座標轉換為裁剪空間座標 (螢幕顯示位置)
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 單純輸出顏色與 Alpha，不受任何光照影響
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}