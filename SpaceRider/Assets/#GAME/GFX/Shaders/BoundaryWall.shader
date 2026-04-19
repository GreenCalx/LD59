Shader "MAUVE/BoundaryWall"
{
    Properties
    {
        _GlowColor          ("Glow Color",          Color)        = (0, 0.8, 1, 1)
        _ProximityT         ("Proximity T",          Range(0,1))   = 0
        _GridTiling         ("Grid Tiling",          Vector)       = (8, 20, 0, 0)
        _GridLineWidth      ("Grid Line Width",       Range(0,0.5)) = 0.05
        _BaseAlpha          ("Base Alpha",            Range(0,1))   = 0.05
        _ProximityMaxAlpha  ("Proximity Max Alpha",   Range(0,1))   = 0.6
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
            Name "BoundaryWall"
            Blend  One One
            ZWrite Off
            Cull   Front

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _GlowColor;
                float  _ProximityT;
                float4 _GridTiling;
                float  _GridLineWidth;
                float  _BaseAlpha;
                float  _ProximityMaxAlpha;
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
                float2 tiledUV = IN.uv * _GridTiling.xy;

                // Grid: 1 on line, 0 inside cell
                float2 cell     = abs(frac(tiledUV) - 0.5) * 2.0;
                float  lineMask = step(1.0 - _GridLineWidth * 2.0, max(cell.x, cell.y));

                // Alpha ramps from baseAlpha → proximityMaxAlpha as ProximityT → 1
                float alpha = lerp(_BaseAlpha, _ProximityMaxAlpha, _ProximityT) * lineMask;

                // Extra pulse glow at high proximity
                alpha += _ProximityT * _ProximityT * 0.15 * lineMask;

                half3 col = _GlowColor.rgb * alpha;
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
