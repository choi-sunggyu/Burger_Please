// Inverted-hull outline that merges outlined objects of the same Stencil Group into one silhouette.
// Different groups (and anything without this outline, e.g. the floor) keep the outline drawn over them.
// Mobile friendly: stencil only, no depth texture. Neither pass uses a LightMode URP draws by default:
// the renderer must draw every "OutlineMask" pass first, then every "OutlineHull" pass
// (Render Objects features "OutlineMask" at AfterRenderingOpaques, "OutlineHull" at BeforeRenderingSkybox).
Shader "AP/OutlineStencil"
{
    Properties
    {
        _OutlineColor("OutlineColor", Color) = (0, 0, 0, 1)
        _OutlineWidth("OutlineWidth", Float) = 2
        [IntRange] _StencilGroup("Stencil Group", Range(1, 255)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        HLSLINCLUDE
        #pragma multi_compile_instancing
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _OutlineColor;
            float _OutlineWidth;
        CBUFFER_END

        struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings MaskVert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = TransformObjectToHClip(input.positionOS);
            return output;
        }

        // Same maths as OutlineExpand in SG_Outline: width in pixels at 1080p, sideways on screen only.
        Varyings HullVert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            float3 positionVS = TransformWorldToView(TransformObjectToWorld(input.positionOS));
            float3 normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(input.normalOS));
            normalVS.z = 0;
            float clipW = mul(UNITY_MATRIX_P, float4(positionVS, 1)).w;
            positionVS.xy += SafeNormalize(normalVS).xy * _OutlineWidth / 540.0 * clipW / abs(UNITY_MATRIX_P[1][1]);
            output.positionCS = TransformWViewToHClip(positionVS);
            return output;
        }

        half4 Frag(Varyings input) : SV_Target
        {
            return half4(_OutlineColor.rgb, 1);
        }
        ENDHLSL

        // Marks the visible pixels of every outlined body with its group.
        Pass
        {
            Name "OutlineMask"
            Tags { "LightMode" = "OutlineMask" }
            Cull Back
            ZWrite Off
            ZTest LEqual
            ColorMask 0
            Stencil { Ref [_StencilGroup] Comp Always Pass Replace }

            HLSLPROGRAM
            #pragma vertex MaskVert
            #pragma fragment Frag
            ENDHLSL
        }

        // Hull is rejected over bodies of its own group, so seams inside a group vanish.
        Pass
        {
            Name "OutlineHull"
            Tags { "LightMode" = "OutlineHull" }
            Cull Front
            ZWrite On
            ZTest LEqual
            Stencil { Ref [_StencilGroup] Comp NotEqual }

            HLSLPROGRAM
            #pragma vertex HullVert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
