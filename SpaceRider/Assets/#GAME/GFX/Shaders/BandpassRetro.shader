Shader "MAUVE/PostProcess/BandpassRetro"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "BandpassRetro"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // ── Parameters ──────────────────────────────────────────────
            float _BandCount;           // colour posterisation levels
            float _ScanlineStrength;    // 0 = off, 1 = full
            float _ScanlineFrequency;   // lines per screen height
            float _VignetteStrength;    // 0 = off
            float _ChromaShift;         // lateral RGB offset (0 = off)

            // ── Helpers ─────────────────────────────────────────────────
            float3 Posterize(float3 col, float bands)
            {
                return floor(col * bands + 0.5) / bands;
            }

            float Vignette(float2 uv, float strength)
            {
                float2 d = uv - 0.5;
                return 1.0 - dot(d, d) * strength * 4.0;
            }

            // ── Main ─────────────────────────────────────────────────────
            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // Chromatic aberration — offset R and B slightly left/right
                float  shift = _ChromaShift * 0.005;
                float  r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( shift, 0)).r;
                float  g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv             ).g;
                float  b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-shift, 0)).b;
                float4 col = float4(r, g, b, 1.0);

                // Posterise (band quantisation)
                col.rgb = Posterize(col.rgb, max(_BandCount, 2.0));

                // Scanlines — dark horizontal bands
                float scan = abs(sin(uv.y * _ScanlineFrequency * PI));
                scan = pow(saturate(scan), 0.3);
                col.rgb *= lerp(1.0, scan, _ScanlineStrength);

                // Vignette
                col.rgb *= saturate(Vignette(uv, _VignetteStrength));

                return col;
            }
            ENDHLSL
        }
    }
}
