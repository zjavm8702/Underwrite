// 스프라이트 모양 안쪽을 색으로 곱한다. 용병단 단원의 천(마스크)에 단 색을 입힐 때 쓴다(DirectionalSprites).
// 마스크의 알파만큼 색이 곱해지고, 바깥은 그대로(×1). 아래 그림의 명암이 남는다. 색은 SpriteRenderer.color가
// 정점 색으로 오지 않는 경우가 있어 _Tint로도 받는다 — 둘을 곱한다.
Shader "Underwrite/SpriteMultiply"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask", 2D) = "white" {}
        [PerRendererData] _Tint ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        Blend DstColor Zero

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _Tint;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                return half4(lerp(half3(1, 1, 1), input.color.rgb * _Tint.rgb, mask), 1);
            }
            ENDHLSL
        }
    }
}
