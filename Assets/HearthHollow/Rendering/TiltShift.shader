// Hearth & Hollow — HD-2D tilt-shift: a soft blur towards the top and bottom of the screen, the middle stays sharp.
// Runs as a Full Screen Pass renderer feature on Renderer2D. Strength etc. are globals set by GameLook
// (_HHTiltStrength 0 = off, which is also the default outside play mode).
Shader "HearthHollow/Tilt Shift"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "TiltShift"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float _HHTiltStrength;   // 0..1
            float _HHTiltBand;       // half-height of the sharp band (0.5 = whole screen sharp)
            float _HHTiltFeather;    // how quickly it blurs past the band
            float _HHTiltRadius;     // max blur radius, as a fraction of screen height

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float k = saturate((abs(uv.y - 0.5) - _HHTiltBand) / max(0.001, _HHTiltFeather)) * _HHTiltStrength;
                if (k <= 0.001) return c;
                float2 r = k * _HHTiltRadius * float2(_ScreenParams.y / _ScreenParams.x, 1.0);
                half4 sum = c;
                [unroll] for (int s = 0; s < 16; s++)
                {
                    float a = s * 2.39996;
                    float d = sqrt((s + 0.5) / 16.0);
                    sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(cos(a), sin(a)) * d * r);
                }
                return sum / 17.0;
            }
            ENDHLSL
        }
    }
}
