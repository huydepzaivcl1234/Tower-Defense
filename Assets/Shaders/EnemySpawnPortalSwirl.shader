Shader "TowerDefense/EnemySpawnPortalSwirl"
{
    Properties
    {
        [HDR]_ColorA ("Color A", Color) = (0.03, 2.2, 0.01, 1)
        [HDR]_ColorB ("Color B", Color) = (0.35, 6.5, 0.02, 1)
        [HDR]_HighlightColor ("Highlight", Color) = (3.5, 8.0, 1.0, 1)
        [HDR]_DarkColor ("Dark", Color) = (0.002, 0.12, 0.006, 1)
        _Speed ("Speed", Float) = 1.0
        _SwirlStrength ("Swirl Strength", Float) = 5.5
        _Erosion ("Erosion", Range(0.2,10)) = 2.0
        _MaskErosion ("Mask Erosion", Range(0.2,8)) = 1.2
        _EdgeWobble ("Edge Wobble", Range(0,0.2)) = 0.08
        _EmissionStrength ("Emission", Range(0,10)) = 2.5
        _Alpha ("Alpha", Range(0,1)) = 1
        _LayerMode ("Layer Mode", Float) = 2
        _Scroll ("Scroll", Vector) = (0,-0.6,0,0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Portal"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float2 localPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA;
                float4 _ColorB;
                float4 _HighlightColor;
                float4 _DarkColor;
                float4 _Scroll;
                float _Speed;
                float _SwirlStrength;
                float _Erosion;
                float _MaskErosion;
                float _EdgeWobble;
                float _EmissionStrength;
                float _Alpha;
                float _LayerMode;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));
                return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float amp = 0.55;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    v += noise2(p) * amp;
                    p = p * 2.03 + 17.17;
                    amp *= 0.5;
                }
                return v;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                o.localPos = v.positionOS.xy;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // UV convention from the procedural spiral mesh:
                // U = around the portal, V = 0 outer edge -> 1 center.
                float2 uv = i.uv;
                float time = _Time.y * _Speed;
                float outerToCenter = saturate(uv.y);
                float radius01 = 1.0 - outerToCenter;

                float warpedU = uv.x + outerToCenter * _SwirlStrength * 0.115 + time * 0.075;
                float2 flowUV = float2(warpedU * 5.0, outerToCenter * 6.0) + _Scroll.xy * _Time.y;
                float n1 = fbm(flowUV);
                float n2 = fbm(flowUV * 1.73 + float2(time * 0.37, -time * 0.21));
                float bands = saturate(n1 * 0.72 + n2 * 0.45);

                float eroded = pow(saturate(bands), max(0.2, _Erosion));
                float centerMask = pow(saturate(radius01), max(0.2, _MaskErosion));

                // Irregular liquid outer edge.
                float angle = uv.x * 6.2831853;
                float wobble = sin(angle * 7.0 + time) * 0.5;
                wobble += sin(angle * 13.0 - time * 1.35 + 1.7) * 0.3;
                wobble += sin(angle * 19.0 + time * 0.55 + 4.1) * 0.2;
                float edgeLimit = 0.985 + wobble * _EdgeWobble;
                float edgeAlpha = smoothstep(edgeLimit, edgeLimit - 0.045, radius01);

                float3 color = lerp(_ColorA.rgb, _ColorB.rgb, eroded);
                float alpha = eroded * centerMask * edgeAlpha * _Alpha * i.color.a;

                // Dark background/core.
                if (_LayerMode < 0.5)
                {
                    float core = smoothstep(1.0, 0.15, radius01);
                    color = _DarkColor.rgb * (0.85 + 0.15 * bands);
                    alpha = saturate(core * 0.96) * edgeAlpha * _Alpha;
                }
                // Hollow outer ring.
                else if (_LayerMode < 1.5)
                {
                    float ring = smoothstep(0.68, 0.86, radius01) * (1.0 - smoothstep(0.94, 1.0, radius01));
                    float ripple = 0.72 + 0.28 * sin(angle * 9.0 + time * 1.8 + n1 * 4.0);
                    color = lerp(_ColorA.rgb, _ColorB.rgb, ripple);
                    alpha = ring * edgeAlpha * _Alpha;
                }
                // Main green spiral.
                else if (_LayerMode < 2.5)
                {
                    float tunnelFade = smoothstep(0.08, 0.34, radius01);
                    color = lerp(_DarkColor.rgb, color, tunnelFade);
                    alpha *= tunnelFade;
                }
                // Bright eroded streaks.
                else if (_LayerMode < 3.5)
                {
                    float streak = pow(saturate(bands), max(2.0, _Erosion + 2.0));
                    streak *= smoothstep(0.08, 0.30, radius01);
                    color = _HighlightColor.rgb * (0.55 + streak * 1.3);
                    alpha = streak * centerMask * edgeAlpha * _Alpha;
                }
                // Edge wave/highlight only.
                else
                {
                    float rim = smoothstep(0.72, 0.90, radius01) * (1.0 - smoothstep(0.94, 1.0, radius01));
                    float wave = pow(saturate(0.5 + 0.5 * sin(angle * 11.0 - time * 2.1 + n1 * 5.0)), 2.5);
                    color = lerp(_ColorB.rgb, _HighlightColor.rgb, wave);
                    alpha = rim * wave * edgeAlpha * _Alpha;
                }

                // Tiny glossy glints concentrated toward the rim.
                float2 cells = floor(float2(uv.x * 48.0, radius01 * 10.0));
                float rnd = hash21(cells);
                float glintBand = smoothstep(0.65, 0.78, radius01) * (1.0 - smoothstep(0.96, 1.0, radius01));
                float glint = step(0.94, rnd) * glintBand;
                color += _HighlightColor.rgb * glint * 0.65;

                color *= _EmissionStrength * i.color.rgb;
                clip(alpha - 0.004);
                return half4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
