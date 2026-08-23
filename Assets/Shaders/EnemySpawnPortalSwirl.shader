Shader "TowerDefense/EnemySpawnPortalSwirl"
{
    Properties
    {
        [HDR]_OuterColor ("Outer Color", Color) = (0.45, 6.0, 0.02, 1)
        [HDR]_MidColor ("Mid Color", Color) = (0.05, 2.4, 0.01, 1)
        [HDR]_DarkColor ("Dark Center", Color) = (0.005, 0.18, 0.01, 1)
        [HDR]_HighlightColor ("Highlight", Color) = (3.5, 8.0, 1.0, 1)
        _Speed ("Swirl Speed", Float) = 1.2
        _SwirlStrength ("Swirl Strength", Float) = 5.5
        _EdgeWobble ("Edge Wobble", Range(0,0.2)) = 0.075
        _EmissionStrength ("Emission Strength", Range(0,8)) = 2.2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _OuterColor;
            float4 _MidColor;
            float4 _DarkColor;
            float4 _HighlightColor;
            float _Speed;
            float _SwirlStrength;
            float _EdgeWobble;
            float _EmissionStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0;
                float r = length(p);
                float a = atan2(p.y, p.x);
                float time = _Time.y * _Speed;

                // Irregular liquid rim. Several frequencies keep the silhouette organic.
                float rimNoise = sin(a * 7.0 + time * 0.85) * 0.50;
                rimNoise += sin(a * 13.0 - time * 1.15 + 1.7) * 0.28;
                rimNoise += sin(a * 19.0 + time * 0.55 + 4.1) * 0.16;
                float edge = 0.965 + rimNoise * _EdgeWobble;
                float alpha = smoothstep(edge, edge - 0.055, r);
                clip(alpha - 0.01);

                // Deep rotating vortex. Radius is folded into the angle so bands spiral inward.
                float spiralCoord = a * _SwirlStrength - r * 23.0 + time * 3.0;
                float s1 = sin(spiralCoord);
                float s2 = sin(a * (_SwirlStrength + 3.0) - r * 37.0 - time * 2.1 + 1.8);
                float s3 = sin(a * 3.0 - r * 14.0 + time * 1.35 + sin(a * 5.0) * 0.8);
                float swirl = saturate(0.5 + s1 * 0.28 + s2 * 0.15 + s3 * 0.09);

                // Outer slime is bright, center falls into a dark tunnel.
                float radial = saturate(r);
                float centerDepth = smoothstep(0.08, 0.78, r);
                float3 baseCol = lerp(_DarkColor.rgb, _MidColor.rgb, centerDepth);
                baseCol = lerp(baseCol, _OuterColor.rgb, saturate((radial - 0.55) * 1.9));
                baseCol *= lerp(0.58, 1.28, swirl);

                // Long glossy streaks running along the spiral.
                float streak = smoothstep(0.72, 0.985, swirl);
                streak *= smoothstep(0.10, 0.35, r);
                baseCol += _HighlightColor.rgb * streak * 0.34;

                // Wet luminous rim.
                float rim = smoothstep(0.70, 0.98, r) * smoothstep(edge, edge - 0.16, r);
                baseCol += _OuterColor.rgb * rim * 0.42;

                // Procedural white/lime glints concentrated around the rim like the reference.
                float2 cell = floor((p + 1.0) * 16.0);
                float rnd = hash21(cell);
                float2 f = frac((p + 1.0) * 16.0) - 0.5;
                float dotMask = 1.0 - smoothstep(0.08, 0.28, length(f));
                float speckleBand = smoothstep(0.62, 0.78, r) * (1.0 - smoothstep(0.96, 1.04, r));
                float speck = step(0.83, rnd) * dotMask * speckleBand;
                baseCol += _HighlightColor.rgb * speck * 0.95;

                // Small breathing pulse prevents the portal from looking like a static decal.
                float pulse = 0.94 + 0.06 * sin(time * 2.4 + r * 8.0);
                baseCol *= pulse * _EmissionStrength;

                return float4(baseCol, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
