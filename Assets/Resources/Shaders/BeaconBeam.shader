Shader "LandConquest/BeaconBeam"
{
    Properties
    {
        _Color ("Beam Color", Color) = (1, 0.3, 0.1, 0.8)
        _Intensity ("Intensity", Float) = 1.5
        _FadeStart ("Fade Start (0-1)", Float) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+500" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _FadeStart;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Vertical fade: opaque at bottom (uv.y=0), transparent at top (uv.y=1)
                float fade = 1.0 - saturate((input.uv.y - _FadeStart) / (1.0 - _FadeStart));

                // Horizontal fade: strongest at center (uv.x=0.5), fades to edges
                float centerDist = abs(input.uv.x - 0.5) * 2.0;
                float hFade = 1.0 - centerDist * centerDist;

                float alpha = fade * hFade * _Color.a * _Intensity;
                if (alpha < 0.01) discard;

                return half4(_Color.rgb * _Intensity, alpha);
            }
            ENDHLSL
        }
    }
}
