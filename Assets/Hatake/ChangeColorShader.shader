Shader "Custom/NewUnlitUniversalRenderPipelineShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white"{}

        [HDR] _DamageColor("Damage Color", Color) = (1, 0, 0, 1) // ダメージ時の色（赤）
        [HideInInspector] _DamageIntensity ("Damage Intensity", Range(0.0, 1.0)) = 0.0 
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            struct Attributes//Unityが毎フレーム入れていくれる情報
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half4 _DamageColor;
                float _DamageIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)//頂点シェーダー（形をいじれる）
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target //フラグメント（色とかいじれる）
            {
                //イメージ画像の色を取得（キャラクターのピクセルカラー ）
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                //Stepでしきい値処理（0.1以下は0.1以上は1）
                baseColor.a  = step(0.1f,baseColor.a);

                half4 normalColor = baseColor * _BaseColor;

                //アルファ値が低いフラグメントを破棄する
                clip(baseColor.a - 0.001f);

                half4 damageColor = baseColor * _DamageColor;

                half4 finalcolor = lerp(normalColor, damageColor, _DamageIntensity);
                return finalcolor;
            }
            ENDHLSL
        }
    }
}
