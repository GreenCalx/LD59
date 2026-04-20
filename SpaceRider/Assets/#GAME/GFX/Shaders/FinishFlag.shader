Shader "MAUVE/FinishFlag"
{
    Properties
    {
        _Alpha          ("Alpha",           Range(0,1)) = 0.4
        _Tiling         ("Tiling",          Float)      = 8.0
        _WaveSpeed      ("Wave Speed",      Float)      = 1.5
        _WaveAmplitude  ("Wave Amplitude",  Float)      = 0.04
        _WaveFrequency  ("Wave Frequency",  Float)      = 6.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "FinishFlag"
            Blend  SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull   Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Alpha;
                float _Tiling;
                float _WaveSpeed;
                float _WaveAmplitude;
                float _WaveFrequency;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 animUV  = IN.uv;
                animUV.x      += sin(IN.uv.y * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveAmplitude;
                float2 tiledUV = animUV * _Tiling;
                float  checker = fmod(floor(tiledUV.x) + floor(tiledUV.y), 2.0);
                half3  col     = checker > 0.5 ? half3(1,1,1) : half3(0,0,0);
                return half4(col, _Alpha);
            }
            ENDHLSL
        }
    }
}
