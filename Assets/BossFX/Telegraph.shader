Shader "CrossGuard/Telegraph"
{
    // Windup telegraph: a faint danger zone that fills up toward the strike, with a
    // bright sweeping leading edge and a full-surface flash at impact. The fill
    // coordinate is baked into UV.x per mesh (0 = fill start, 1 = fill end):
    //   rect  -> 0 at boss, 1 at the far tip (forward wipe)
    //   arc   -> 0 at center, 1 at rim       (expand outward)
    //   circle-> 0 at rim, 1 at center       (contract inward)
    Properties
    {
        _BaseColor  ("Base (faint zone)", Color) = (1,0.1,0.1,0.18)
        _FillColor  ("Fill",              Color) = (1,0.15,0.1,0.55)
        _EdgeColor  ("Edge (HDR)",        Color) = (3,1.4,0.4,1)
        _FlashColor ("Flash (HDR)",       Color) = (4,4,4,1)
        _Fill       ("Fill amount", Range(0,1)) = 0
        _Flash      ("Flash amount", Range(0,1)) = 0
        _EdgeWidth  ("Edge width", Range(0.001,0.3)) = 0.06
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _FillColor;
                float4 _EdgeColor;
                float4 _FlashColor;
                float _Fill;
                float _Flash;
                float _EdgeWidth;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float d = IN.uv.x;                          // 0..1 fill coordinate
                half4 col = _BaseColor;                      // faint danger zone
                if (d <= _Fill) col = _FillColor;            // filled portion
                if (abs(d - _Fill) < _EdgeWidth) col = _EdgeColor;   // sweeping edge
                col = lerp(col, _FlashColor, saturate(_Flash));      // impact flash
                return col;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
