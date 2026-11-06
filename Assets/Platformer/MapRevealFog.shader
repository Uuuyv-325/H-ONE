Shader "HOne/Map Reveal Fog"
{
    Properties
    {
        _RevealMask ("Reveal Mask", 2D) = "black" {}
        _FlashMask ("Flash Mask", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 positionWS : TEXCOORD0;
            };

            TEXTURE2D(_RevealMask);
            SAMPLER(sampler_RevealMask);
            TEXTURE2D(_FlashMask);
            SAMPLER(sampler_FlashMask);
            float4 _MapMinimum;
            float4 _MapSize;
            int _LightCount;
            float4 _LightData[16];

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(world);
                output.positionWS = world.xy;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = (input.positionWS - _MapMinimum.xy) / _MapSize.xy;
                float reveal = saturate(SAMPLE_TEXTURE2D(_RevealMask, sampler_RevealMask, uv).r);
                float flash = saturate(SAMPLE_TEXTURE2D(_FlashMask, sampler_FlashMask, uv).r);

                float pointLight = 0.0;
                for (int i = 0; i < _LightCount; i++)
                {
                    float distanceToPoint = distance(input.positionWS, _LightData[i].xy);
                    pointLight = max(pointLight,
                        (1.0 - smoothstep(0.06, 0.35, distanceToPoint)) * _LightData[i].z);
                }

                float brightness = max(flash, pointLight);
                float fogAlpha = 1.0 - 0.42 * reveal;
                float whiteAlpha = lerp(0.72, 1.0, pointLight);
                return half4(brightness.xxx, lerp(fogAlpha, whiteAlpha, brightness));
            }
            ENDHLSL
        }
    }
}
