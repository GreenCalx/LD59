Shader "MAUVE/SaucerExplosion"
{
    // ── Intended use ──────────────────────────────────────────────────────────
    // Assign to a Material on a Particle System (Render Mode: Billboard or Mesh).
    // The Particle System drives lifetime via vertex COLOR.a (enable "Color over
    // Lifetime" fading to alpha 0).  Two passes layer on top of each other:
    //   Pass 1 — shockwave ring (expanding hollow circle, additive)
    //   Pass 2 — hot core flash (central bloom, additive)
    // Both are URP-compatible additive blends — let bloom do the rest.
    // ─────────────────────────────────────────────────────────────────────────

    Properties
    {
        [HDR] _RingColor    ("Ring Color",          Color)          = (1.0, 0.5, 0.1, 1)
        [HDR] _CoreColor    ("Core Color",          Color)          = (1.0, 0.9, 0.4, 1)
        _RingWidth          ("Ring Width",          Range(0.01, 0.5)) = 0.08
        _RingRadius         ("Ring Radius",         Range(0.0, 1.0))  = 0.55
        _CoreRadius         ("Core Radius",         Range(0.0, 1.0))  = 0.35
        _CoreFalloff        ("Core Falloff",        Range(0.01, 2.0)) = 1.5
        _NoiseScale         ("Noise Scale",         Range(1, 32))     = 8.0
        _NoiseStrength      ("Noise Strength",      Range(0, 1))      = 0.35
        _Intensity          ("Intensity",           Range(0, 8))      = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual

        // ── Pass 1: shockwave ring ────────────────────────────────────────────
        Pass
        {
            Name "SaucerExplosion_Ring"
            Blend One One   // additive

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag_ring
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _RingColor;
                float4 _CoreColor;
                float  _RingWidth;
                float  _RingRadius;
                float  _CoreRadius;
                float  _CoreFalloff;
                float  _NoiseScale;
                float  _NoiseStrength;
                float  _Intensity;
            CBUFFER_END

            // ── cheap hash-based value noise ──────────────────────────────────
            float hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(i),               hash21(i + float2(1,0)), u.x),
                            lerp(hash21(i + float2(0,1)), hash21(i + float2(1,1)), u.x), u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag_ring(Varyings IN) : SV_Target
            {
                // Remap UV to -1..1
                float2 p    = IN.uv * 2.0 - 1.0;
                float  dist = length(p);

                // Ring mask: smooth band around _RingRadius
                float inner = smoothstep(_RingRadius - _RingWidth,        _RingRadius,              dist);
                float outer = smoothstep(_RingRadius + _RingWidth * 0.5,  _RingRadius - _RingWidth * 0.2, dist);
                float ring  = inner * outer;

                // Polar noise to break up the ring edge
                float angle      = atan2(p.y, p.x);
                float2 noiseUV   = float2(angle * _NoiseScale * 0.15915, dist * _NoiseScale);
                float  noise     = valueNoise(noiseUV) * 2.0 - 1.0;
                ring             = saturate(ring + noise * _NoiseStrength * ring);

                // Particle lifetime alpha from vertex color
                float alpha = IN.color.a;

                half3 col = _RingColor.rgb * ring * _Intensity * alpha;
                return half4(col, 0);   // alpha 0: additive doesn't need dst alpha
            }
            ENDHLSL
        }

        // ── Pass 2: core flash ────────────────────────────────────────────────
        Pass
        {
            Name "SaucerExplosion_Core"
            Blend One One   // additive

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag_core
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _RingColor;
                float4 _CoreColor;
                float  _RingWidth;
                float  _RingRadius;
                float  _CoreRadius;
                float  _CoreFalloff;
                float  _NoiseScale;
                float  _NoiseStrength;
                float  _Intensity;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(i),               hash21(i + float2(1,0)), u.x),
                            lerp(hash21(i + float2(0,1)), hash21(i + float2(1,1)), u.x), u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag_core(Varyings IN) : SV_Target
            {
                float2 p    = IN.uv * 2.0 - 1.0;
                float  dist = length(p);

                // Soft radial falloff from centre
                float core = pow(saturate(1.0 - dist / _CoreRadius), _CoreFalloff);

                // Slight noise texture to break up the smooth blob
                float2 noiseUV = p * _NoiseScale * 0.5 + 0.5;
                float  noise   = valueNoise(noiseUV);
                core           = saturate(core * (0.7 + noise * 0.6));

                float alpha = IN.color.a;
                // Core brightens early and fades fast — invert alpha curve
                float coreFade = alpha * alpha;

                half3 col = _CoreColor.rgb * core * _Intensity * coreFade;
                return half4(col, 0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
