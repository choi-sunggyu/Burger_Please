// 위치: Assets/Shaders/BakedVertexLit.shader
// 배경(정적) 오브젝트용. 실시간 빛 계산 없음.
// 최종색 = 텍스처색 × 기본색 × (VertexLightBaker 가 TEXCOORD3 에 구워둔 밝기)

Shader "Custom/BakedVertexLit"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        [Toggle(_USE_VERTEX_COLOR)] _UseVertexColor("Use Vertex Color", Float) = 0
        _BakeStrength("Bake Strength", Range(0, 1)) = 1
        [Toggle(_DEBUG_SHADOW)] _DebugShadow("Debug: Show Baked Light (black = shadow)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // 모든 패스가 같은 CBUFFER 를 써야 SRP Batcher 가 묶어서 처리함
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half   _BakeStrength;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "BakedVertexLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _USE_VERTEX_COLOR
            #pragma shader_feature_local _DEBUG_SHADOW
            #pragma multi_compile_fog

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 bakedLight : TEXCOORD3;   // 도구가 저장한 밝기 (굽지 않았으면 검게 보임)
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half3  light      : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
                half   shadow     : TEXCOORD3;   // 확인 모드용: 직접광 양 (흰색 = 빛 받음, 검정 = 그림자·뒷면)
                half4  color      : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                // 정점에서 읽은 값을 그대로 넘기면, 면 내부는 GPU 가 자동 보간
                OUT.light = lerp(half3(1, 1, 1), (half3)IN.bakedLight.rgb, _BakeStrength);
                OUT.color = IN.color;
                OUT.shadow = (half)IN.bakedLight.a;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                #if defined(_DEBUG_SHADOW)
                    return half4(IN.shadow.xxx, 1); // 흰색 = 빛 받음, 검정 = 그림자·뒷면
                #endif

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                #if defined(_USE_VERTEX_COLOR)
                    albedo *= IN.color;
                #endif

                // 픽셀 단위 작업은 곱셈 한 번뿐 (실시간 조명이면 여기서 N·L, 그림자맵 조회 등을 함)
                half3 col = albedo.rgb * IN.light;
                col = MixFog(col, IN.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        // 깊이 텍스처를 쓰는 효과가 있을 때 필요
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth

            struct AttributesD { float4 positionOS : POSITION; };
            struct VaryingsD   { float4 positionCS : SV_POSITION; };

            VaryingsD vertDepth(AttributesD IN)
            {
                VaryingsD OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half fragDepth(VaryingsD IN) : SV_Target { return IN.positionCS.z; }
            ENDHLSL
        }
    }
}
