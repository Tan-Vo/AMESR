Shader "Cosmic Horizon/Dive Atmosphere"
{
    Properties
    {
        _WaterTint ("Water Tint", Color) = (0.1, 0.6, 0.85, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "DiveAtmosphere"
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _WaterTint;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 positionWS : TEXCOORD0;
            };

            Varyings Vert(float3 positionOS : POSITION)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(positionOS);
                output.positionWS = TransformObjectToWorld(positionOS).xy;
                return output;
            }

            float Hash(float2 cell)
            {
                float3 value = frac(float3(cell.xyx) * 0.1031);
                value += dot(value, value.yzx + 33.33);
                return frac((value.x + value.y) * value.z);
            }

            float Motes(float2 grid, float time, float density)
            {
                float2 cell = floor(grid);
                float seed = Hash(cell);
                float2 center = 0.2 + 0.6 * float2(seed, Hash(cell + float2(7, 23)));
                float radius = lerp(0.013, 0.037, seed);
                float edge = max(fwidth(grid.x), fwidth(grid.y));
                float dotAlpha = 1.0 - smoothstep(radius, radius + edge, length(frac(grid) - center));
                float pulse = 0.7 + 0.3 * sin(time * 1.4 + seed * 37.0);
                return dotAlpha * pulse * step(1.0 - density, seed);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 position = input.positionWS;
                float time = _Time.y;
                float wave = 0.07 * sin(position.x * 1.6 - time * 1.8)
                           + 0.035 * sin(position.x * 3.7 + time * 2.4);
                float depth = wave - position.y;
                float submerged = smoothstep(0.0, 0.22, depth);
                float surface = 0.60 * exp2(-abs(depth) * 18.0);
                float haze = submerged * (0.035 + 0.12 * exp2(-max(0.0, depth) * 0.65));
                float fine = Motes(position * 1.3 + float2(-0.07, -0.13) * time, time, 0.43);
                float bright = Motes(position * 0.65 + float2(8, -0.24 * time), time, 0.28);
                float drifting = submerged * (fine * 0.30 + bright * 0.52);
                half3 color = lerp(_WaterTint.rgb, half3(0.73, 0.94, 1.0), saturate(0.12 + surface + drifting));
                return half4(color, saturate(haze + drifting + surface));
            }
            ENDHLSL
        }
    }
}
