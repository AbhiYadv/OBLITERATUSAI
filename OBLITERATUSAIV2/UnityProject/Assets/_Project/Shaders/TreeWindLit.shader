Shader "OBLITERATUS AI/Tree Wind Lit"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.29, 0.48, 0.20, 1)
        _WindHeight("Wind Height", Float) = 7.5
        _SwayStrength("Sway Strength", Range(0, 1)) = 0.34
        _FlutterStrength("Flutter Strength", Range(0, 0.3)) = 0.07
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex WindVertex
            #pragma fragment WindFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _WindHeight;
                float _SwayStrength;
                float _FlutterStrength;
            CBUFFER_END

            float4 _ObliteratusTreeWind;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                half colorVariation : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            Varyings WindVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 rootWS = TransformObjectToWorld(float3(0, 0, 0));
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float height = saturate(input.positionOS.y / max(0.1, _WindHeight));
                float bendWeight = height * height;
                float2 windDirection = normalize(_ObliteratusTreeWind.xy + float2(0.0001, 0));
                float phase = dot(rootWS.xz, float2(0.071, 0.113));
                float sway = sin(_Time.y * 1.15 + phase)
                    + sin(_Time.y * 0.43 + phase * 0.61) * 0.45;
                float flutter = sin(
                    _Time.y * (4.2 + _ObliteratusTreeWind.w * 2.5)
                    + phase * 2.3
                    + dot(input.positionOS.xz, float2(1.7, 2.1)));
                float2 crossWind = float2(-windDirection.y, windDirection.x);

                positionWS.xz += windDirection
                    * sway
                    * _ObliteratusTreeWind.z
                    * _SwayStrength
                    * bendWeight;
                positionWS.xz += crossWind
                    * flutter
                    * (0.45 + _ObliteratusTreeWind.w)
                    * _FlutterStrength
                    * height;

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.shadowCoord = TransformWorldToShadowCoord(positionWS);
                output.colorVariation = lerp(0.84, 1.08, Hash21(rootWS.xz));
                return output;
            }

            half4 WindFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(input.shadowCoord);
                half diffuse = saturate(dot(normalWS, mainLight.direction));
                half3 indirect = SampleSH(normalWS);
                half3 direct = mainLight.color
                    * diffuse
                    * mainLight.distanceAttenuation
                    * mainLight.shadowAttenuation;
                half3 color = _BaseColor.rgb
                    * input.colorVariation
                    * (indirect + direct);
                color = MixFog(color, input.fogFactor);
                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
