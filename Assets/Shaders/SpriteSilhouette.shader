// 스프라이트의 모양만 한 가지 색으로 칠한다. 캐릭터 외곽선(SpriteOutline)이 쓴다.
// 색은 머티리얼의 _FillColor에서 오고, 그림에서는 알파만 가져온다. 조명에 반응하지 않는다.
// (Unity 6의 SpriteRenderer는 색을 정점 색으로 넘기지 않는다 — 정점 색은 흰색이 온다.)
Shader "Underwrite/SpriteSilhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _FillColor ("Fill Color", Color) = (0.17, 0.11, 0.07, 0.9)
        // 0: 모양 전체를 칠한다(몸 뒤에 밀어 까는 외곽선). 1: 반투명한 가장자리만 칠한다(몸 위에 덮는 테두리).
        _EdgeOnly ("Edge Only", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _FillColor;
                half _EdgeOnly;
            CBUFFER_END

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
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;

                // 그림의 가장자리 반투명 픽셀은 밝은 바탕에서 뽑혀 색이 옅다. 짙은 선 위에 그대로 얹히면
                // 몸과 선 사이에 옅은 띠가 생겼다(2026-10-18 사용자 보고). 몸 위에서 그 띠만 덮는다.
                if (_EdgeOnly > 0.5h)
                {
                    half edge = smoothstep(0.02h, 0.2h, alpha) * (1.0h - smoothstep(0.6h, 0.95h, alpha));
                    return half4(_FillColor.rgb, edge * _FillColor.a);
                }

                // 밀어 까는 쪽은 또렷하게 — 반투명으로 번지면 그림자처럼 보인다.
                return half4(_FillColor.rgb, step(0.4h, alpha) * _FillColor.a);
            }
            ENDHLSL
        }
    }
}
