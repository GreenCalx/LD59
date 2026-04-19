Shader "MAUVE/UI/WaveForm"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0, 1, 0.75, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent+1"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
        }

        Stencil
        {
            Ref  1
            Comp Equal
            Pass Keep
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    Always
        Blend    SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _Color;

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv          = v.uv;
                o.color       = v.color * _Color;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
            }
            ENDHLSL
        }
    }
}
