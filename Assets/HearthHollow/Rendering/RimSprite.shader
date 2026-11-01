// Hearth & Hollow — the coloured rim on a monster's silhouette (light plan step 2).
// Draws only the sprite's edge pixels (an inner outline), in _RimColor (HDR, so it blooms a little), faded by _Amount.
// Sits on top of the lit body: in the dark the body goes near-black and the rim is what you read; in light it fades away.
Shader "HearthHollow/Rim Sprite"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _RimColor ("Rim colour", Color) = (0.6, 0.75, 1, 1)
        _Amount ("Amount", Range(0, 1)) = 1
        _Fill ("Fill (faint inner tint)", Range(0, 1)) = 0.08
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _RimColor;
                float _Amount;
                float _Fill;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings vert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                SetUpSpriteInstanceProperties();
                o.positionCS = TransformObjectToHClip(UnityFlipSprite(i.positionOS, unity_SpriteProps.xy));
                o.uv = i.uv; o.color = i.color * unity_SpriteColor;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                if (a < 0.5) return half4(0, 0, 0, 0);
                float2 t = _MainTex_TexelSize.xy;
                float n = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(t.x, 0)).a;
                n = min(n, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - float2(t.x, 0)).a);
                n = min(n, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, t.y)).a);
                n = min(n, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - float2(0, t.y)).a);
                float edge = n < 0.5 ? 1.0 : _Fill;
                return half4(_RimColor.rgb, edge * _Amount * i.color.a);
            }
            ENDHLSL
        }
    }
}
