// 위치: Assets/Shaders/LowCostCharacter.shader
// 캐릭터(움직이는 오브젝트)용.
// 움직이면 법선이 바뀌어 밝기를 미리 저장할 수 없으므로,
// N·L 계산을 픽셀 셰이더가 아닌 "버텍스 셰이더"에서 정점당 1번만 수행.
// 실시간 그림자는 쓰지 않음.

Shader "Custom/LowCostCharacter"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _AmbientColor("Ambient Color", Color) = (0.30, 0.32, 0.38, 1)
        _UseSceneAmbient("Use Scene Ambient (0 = Ambient Color)", Range(0, 1)) = 1
        [Toggle(_USE_VERTEX_COLOR)] _UseVertexColor("Use Vertex Color", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half4  _AmbientColor;
            half   _UseSceneAmbient;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "VertexLambert"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _USE_VERTEX_COLOR
            #pragma multi_compile_fog

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half3  light      : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
                half4  color      : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);

                // 씬의 방향광 정보(방향·색)만 가져옴. 그림자 계산은 하지 않음
                half3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(normalWS, mainLight.direction));

                // 배경 굽기 도구와 같은 공식: 주변광 + 빛색 × N·L
                // 주변광은 굽기 도구와 똑같이 씬 환경광(SH)을 법선 방향으로 읽음 (정점당 1회)
                half3 ambient = lerp(_AmbientColor.rgb, SampleSH(normalWS), _UseSceneAmbient);
                OUT.light = ambient + mainLight.color * ndotl;

                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                #if defined(_USE_VERTEX_COLOR)
                    albedo *= IN.color;
                #endif
                half3 col = MixFog(albedo.rgb * IN.light, IN.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

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
