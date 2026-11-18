// Hearth & Hollow — HDR-bright ribbon for swept effects (SlashRing): a mesh, not a sprite. Each vertex carries its colour
// and, in TEXCOORD1.x, how bright it burns (so the head of a slash can be white-hot while its tail is plain green).
Shader "HearthHollow/Glow Trail"
{
    Properties
    {
        [MainTexture] _MainTex ("Streaks", 2D) = "white" {}
        _Glow ("Glow (HDR multiplier)", Float) = 1.6
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Glow;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float2 heat : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float heat : TEXCOORD1; };
            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS);
                o.uv = i.uv;
                o.color = i.color;
                o.heat = i.heat.x;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 rgb = t.rgb * i.color.rgb * (_Glow * max(i.heat, 0.0));
                return half4(rgb, t.a * i.color.a);
            }
            ENDHLSL
        }
    }
}
