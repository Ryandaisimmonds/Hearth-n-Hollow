// Hearth & Hollow — HDR-bright sprite for effects (slashes, sparks, embers, orbs) so bloom picks them up.
// _Flat = 1 draws the sprite as a solid silhouette in its vertex colour (hit flashes, dash after-images).
Shader "HearthHollow/Glow Sprite"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        _Glow ("Glow (HDR multiplier)", Float) = 2.2
        _Flat ("Flat silhouette", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend SrcAlpha OneMinusSrcAlpha   // normal blending keeps the colour on bright ground; the HDR value (> 1) is what blooms
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Glow;
                float _Flat;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings vert(Attributes i)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                SetUpSpriteInstanceProperties();
                float3 p = UnityFlipSprite(i.positionOS, unity_SpriteProps.xy);   // SpriteRenderer flipX / flipY
                o.positionCS = TransformObjectToHClip(p);
                o.uv = i.uv;
                o.color = i.color * unity_SpriteColor;                            // SpriteRenderer.color
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 rgb = lerp(t.rgb, half3(1, 1, 1), _Flat) * i.color.rgb;
                return half4(rgb * _Glow, t.a * i.color.a);
            }
            ENDHLSL
        }
    }
}
