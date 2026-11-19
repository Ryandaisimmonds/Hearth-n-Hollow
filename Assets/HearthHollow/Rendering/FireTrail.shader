// Hearth & Hollow — fire along a swept ribbon (SlashRing with the fire material: the Effects Lab's Inferno Crescent).
// The same mesh as Glow Trail (u along, v across: 0 inside → 1 outside; TEXCOORD1.x = heat, TEXCOORD1.y = age 0→1),
// but instead of streaks it burns: two layers of noise in world space scroll upwards (the flames rise wherever the
// ribbon is), one warping the other so it churns, and they eat into the ribbon so its edges break into tongues —
// more and more as it ages, until only wisps are left. The colour comes from a temperature: white-yellow where it's
// hottest, through yellow and orange to a deep red at the ragged edges. Runs on _Time (scaled: the lab's slow motion
// slows the flames too).
Shader "HearthHollow/Fire Trail"
{
    Properties
    {
        _Glow ("Glow (HDR multiplier)", Float) = 1.15
        _Scale ("Noise scale (per world unit)", Float) = 3.0
        _Speed ("Rise speed", Float) = 2.6
        _Erode ("How much the noise eats the flame", Float) = 1.35
        [HDR] _White ("Hottest", Color) = (1, 0.97, 0.85, 1)
        [HDR] _Yellow ("Yellow", Color) = (1, 0.78, 0.25, 1)
        [HDR] _Orange ("Orange", Color) = (1, 0.42, 0.06, 1)
        [HDR] _Red ("Coolest (the ragged edges)", Color) = (0.6, 0.06, 0.02, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One OneMinusSrcAlpha   // premultiplied: the bright core adds light, the dim edges barely darken
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Glow, _Scale, _Speed, _Erode;
                float4 _White, _Yellow, _Orange, _Red;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float2 heat : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float2 heat : TEXCOORD1; float2 world : TEXCOORD2; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS);
                o.world = TransformObjectToWorld(i.positionOS).xy;
                o.uv = i.uv; o.color = i.color; o.heat = i.heat;
                return o;
            }

            float Hash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float Noise(float2 p)
            {
                float2 c = floor(p), f = frac(p);
                float a = Hash(c), b = Hash(c + float2(1, 0)), d = Hash(c + float2(0, 1)), e = Hash(c + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(d, e, u.x), u.y);
            }
            float Fbm(float2 p)
            {
                float s = 0.0, amp = 0.5;
                for (int k = 0; k < 4; k++) { s += amp * Noise(p); p = p * 2.03 + float2(17.1, 9.2); amp *= 0.5; }
                return s / 0.9375;   // 0..1
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                float v = i.uv.y, age = saturate(i.heat.y), heat = i.heat.x;
                // the noise: stretched upright (flames are tall), rising, the second layer warped by the first so it churns
                float2 p = i.world * _Scale * float2(1.0, 0.55);
                float n1 = Fbm(p + float2(0.0, -t * _Speed));
                float n2 = Fbm(p * 2.1 + float2(n1 * 1.7, -t * _Speed * 1.6 + n1));
                float n = n1 * 0.55 + n2 * 0.45;

                // the ribbon's body: thickest a little outside its middle, soft on both sides
                float across = smoothstep(0.0, 0.4, v) * (1.0 - smoothstep(0.72, 1.0, v));
                float life = 1.0 - age;
                // temperature: the body, hotter near the blade (heat), eaten by the noise more as it ages
                float temp = across * (0.32 + 0.55 * life) + saturate(heat - 1.0) * 0.08;
                temp -= (n - 0.34) * _Erode * (0.8 + 1.2 * age);
                temp *= i.color.a;   // SlashRing's end fades
                temp = saturate(temp);

                float alpha = smoothstep(0.04, 0.22, temp);
                if (alpha <= 0.001) discard;
                float3 c = lerp(_Red.rgb, _Orange.rgb, smoothstep(0.1, 0.38, temp));
                c = lerp(c, _Yellow.rgb, smoothstep(0.38, 0.62, temp));
                c = lerp(c, _White.rgb, smoothstep(0.66, 0.92, temp));
                float bright = _Glow * (0.3 + 1.5 * temp * temp) * (1.0 + 0.2 * saturate(heat - 1.0));   // only the very core blooms
                // premultiplied; the faint red edges stay mostly see-through
                return half4(c * bright * alpha, alpha * (0.25 + 0.6 * temp));
            }
            ENDHLSL
        }
    }
}
