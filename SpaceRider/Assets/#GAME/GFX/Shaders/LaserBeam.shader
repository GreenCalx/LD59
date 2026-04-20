Shader "MAUVE/LaserBeam"
{
    // ── Intended use ──────────────────────────────────────────────────────────
    // Attach to a LineRenderer (or a thin quad/tube mesh).
    // LineRenderer UV mode: Stretch (U = 0→1 along beam, V = 0→1 across width).
    // Blend is additive — place on the Transparent queue and let it bloom.
    // ─────────────────────────────────────────────────────────────────────────

    Properties
    {
        [HDR] _CoreColor    ("Core Color",     Color)           = (0.9, 1.0, 1.0, 1)
        [HDR] _GlowColor    ("Glow Color",     Color)           = (0.2, 0.5, 1.0, 1)
        _CoreWidth          ("Core Width",     Range(0, 0.5))   = 0.06
        _GlowFalloff        ("Glow Falloff",   Range(0.01, 0.5))= 0.28
        _ScrollSpeed        ("Scroll Speed",   Range(0, 10))    = 3.0
        _Intensity          ("Intensity",      Range(0, 8))     = 3.0
        _FlickerSpeed       ("Flicker Speed",  Range(0, 20))    = 7.0
        _FlickerAmp         ("Flicker Amp",    Range(0, 0.5))   = 0.12
        _RippleCount        ("Ripple Count",   Range(1, 16))    = 4.0
        _TipFade            ("Tip Fade Width", Range(0, 0.2))   = 0.05
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
            Name "LaserBeam"
            Blend  One One   // additive — naturally blooms on bright backgrounds
            ZWrite Off
            Cull   Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _CoreColor;
                half4  _GlowColor;
                float  _CoreWidth;
                float  _GlowFalloff;
                float  _ScrollSpeed;
                float  _Intensity;
                float  _FlickerSpeed;
                float  _FlickerAmp;
                float  _RippleCount;
                float  _TipFade;
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
                // ── cross-section profile (V axis) ────────────────────────
                // vCentered: 0 at beam center, 1 at edge
                float vCentered = abs(IN.uv.y - 0.5) * 2.0;

                // Thin bright core
                float core = 1.0 - smoothstep(0.0, _CoreWidth, vCentered);

                // Wider soft glow (squared for sharper roll-off)
                float glow = 1.0 - smoothstep(_CoreWidth, _CoreWidth + _GlowFalloff, vCentered);
                glow *= glow;

                // ── energy flow (U axis) ──────────────────────────────────
                // Scrolling ripple pattern along the beam length
                float scrollU = frac(IN.uv.x - _Time.y * _ScrollSpeed);
                float ripple  = sin(scrollU * TWO_PI * _RippleCount) * 0.25 + 0.75;

                // ── flicker ───────────────────────────────────────────────
                float flicker = sin(_Time.y * _FlickerSpeed) * _FlickerAmp
                              + (1.0 - _FlickerAmp);

                // ── tip fade: soft alpha at both ends ─────────────────────
                float tipFade = smoothstep(0.0, _TipFade, IN.uv.x)
                              * smoothstep(1.0, 1.0 - _TipFade, IN.uv.x);

                // ── composite ─────────────────────────────────────────────
                half3 col  = _CoreColor.rgb * core  * _Intensity;
                col       += _GlowColor.rgb * glow  * ripple * _Intensity * 0.4;
                col       *= flicker * tipFade;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
