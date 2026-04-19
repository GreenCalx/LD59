Shader "Custom/UIGaugeFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Fill       ("Fill Amount",              Range(0,1)) = 0.5
        _ArcStart   ("Arc Start (deg CW/top)",   Float)      = -135
        _ArcSpan    ("Arc Span  (deg)",          Float)      = 270
        _ColorA     ("Color at 0 (min)",         Color)      = (0,1,0,1)
        _ColorB     ("Color at 1 (max)",         Color)      = (1,0,0,1)
        _Center     ("Center UV",                Vector)     = (0.5,0.5,0,0)

        _StencilComp      ("Stencil Comparison",  Float) = 8
        _Stencil          ("Stencil ID",          Float) = 0
        _StencilOp        ("Stencil Operation",   Float) = 0
        _StencilWriteMask ("Stencil Write Mask",  Float) = 255
        _StencilReadMask  ("Stencil Read Mask",   Float) = 255
        _ColorMask        ("Color Mask",          Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        Blend    SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float2 rawUV    : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _TextureSampleAdd;
            float4    _ClipRect;

            float  _Fill;
            float  _ArcStart;
            float  _ArcSpan;
            fixed4 _ColorA;
            fixed4 _ColorB;
            float4 _Center;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPos  = v.vertex;
                o.vertex    = UnityObjectToClipPos(v.vertex);
                o.texcoord  = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.rawUV     = v.texcoord;
                o.color     = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample sprite for shape alpha only
                half4 tex = tex2D(_MainTex, i.texcoord) + _TextureSampleAdd;

                // Angle clockwise from top (12 o'clock = 0 deg)
                // atan2(x, y) with UV dir: up→0, right→90, down→180, left→-90
                float2 dir = i.rawUV - _Center.xy;
                float angleDeg = degrees(atan2(dir.x, dir.y));

                // Shift relative to arc start, wrap to [0, 360)
                float rel = angleDeg - _ArcStart;
                rel = rel - 360.0 * floor(rel / 360.0);

                // Mirror rel so ArcEnd = 0 (fill starts from ArcEnd side)
                float invRel = _ArcSpan - rel;

                // Visible when inside the filled portion, starting from ArcEnd
                float fillDeg = _Fill * _ArcSpan;
                float visible = step(invRel, fillDeg) * step(invRel, _ArcSpan);

                // Gradient: ArcEnd (invRel=0, first visible) = ColorA (green)
                //           ArcStart (invRel=ArcSpan, last visible) = ColorB (red)
                float t = saturate(invRel / max(_ArcSpan, 0.001));
                fixed4 gaugeColor = lerp(_ColorA, _ColorB, t);

                fixed4 color;
                color.rgb = gaugeColor.rgb;
                color.a   = tex.a * visible * i.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
