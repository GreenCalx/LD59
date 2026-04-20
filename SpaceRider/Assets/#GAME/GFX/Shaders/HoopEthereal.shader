Shader "MAUVE/HoopEthereal"
{
    Properties
    {
        _BaseRimColor        ("Base Rim Color",        Color)            = (0.1, 0.4, 1.0, 1)
        _HighlightRimColor   ("Highlight Rim Color",   Color)            = (0.6, 0.9, 1.0, 1)
        _RimPower            ("Rim Power",             Range(1, 8))      = 3.0
        _RimIntensity        ("Rim Intensity",         Range(0, 4))      = 1.5
        _PulseSpeed          ("Pulse Speed",           Range(0, 5))      = 1.0
        _HighlightPulseSpeed ("Highlight Pulse Speed", Range(0, 5))      = 3.0
        _HighlightT          ("Highlight T",           Range(0, 1))      = 0
        _DissolveT           ("Dissolve T",            Range(0, 1))      = 0
        _FringeWidth         ("Fringe Width",          Range(0.01, 0.5)) = 0.1
        _NoiseScale          ("Noise Scale",           Range(0.1, 10))   = 2.0
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
            Name "HoopEthereal"
            Blend  One One
            ZWrite Off
            Cull   Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseRimColor;
                half4  _HighlightRimColor;
                float  _RimPower;
                float  _RimIntensity;
                float  _PulseSpeed;
                float  _HighlightPulseSpeed;
                float  _HighlightT;
                float  _DissolveT;
                float  _FringeWidth;
                float  _NoiseScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 positionOS  : TEXCOORD2;
            };

            // Deterministic hash noise from object-space position.
            // Returns 0-1 pseudo-random value stable per vertex/fragment position.
            float Hash(float3 p)
            {
                p = frac(p * float3(443.897, 441.423, 437.195));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionOS  = IN.positionOS.xyz;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // --- Dissolve clip ---
                float noiseVal  = Hash(IN.positionWS * _NoiseScale);
                float threshold = _DissolveT;
                clip(noiseVal - threshold);

                // --- Fresnel rim ---
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float  NdotV     = saturate(dot(normalWS, viewDirWS));
                float  fresnel   = pow(1.0 - NdotV, _RimPower);

                // --- Breathing pulse (lerps speed between normal/highlight) ---
                float pulseSpeed = lerp(_PulseSpeed, _HighlightPulseSpeed, _HighlightT);
                float pulse      = sin(_Time.y * pulseSpeed) * 0.3 + 0.7; // range 0.4–1.0

                // --- Color lerp between normal and highlight ---
                half3 rimColor = lerp(_BaseRimColor.rgb, _HighlightRimColor.rgb, _HighlightT);

                // --- Dissolve fringe: bright edge at clip boundary ---
                float fringe = saturate(1.0 - (noiseVal - threshold) / max(_FringeWidth, 0.001));
                fringe *= step(0.001, _DissolveT); // suppress fringe when not dissolving

                // --- Composite ---
                half3 col = rimColor * fresnel * _RimIntensity * pulse;
                col      += rimColor * fringe * 2.0;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
