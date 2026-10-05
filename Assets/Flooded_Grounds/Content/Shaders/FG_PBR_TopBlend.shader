Shader "Flooded_Grounds/PBR_TopBlend" {
    Properties {
        _MainTex ("Base Albedo (RGB)", 2D) = "white" {}
        _Spc("Base Metalness(R) Smoothness(A)", 2D) = "black" {}
        _BumpMap ("Base Normal", 2D) = "bump" {} 
        _AO("Base AO", 2D)= "white" {}
        _layer1Tex ("Layer1 Albedo (RGB) Smoothness (A)", 2D) = "white" {}
        _layer1Metal ("Layer1 Metalness", Range(0,1)) = 0
        _layer1Norm("Layer 1 Normal", 2D) = "bump" {}
        _layer1Breakup ("Layer1 Breakup (R)", 2D) = "white" {}
        _layer1BreakupAmnt ("Layer1 Breakup Amount", Range(0,1)) = 0.5
        _layer1Tiling("Layer1 Tiling", float) = 10
        _Power ("Layer1 Blend Amount", float ) = 1
        _Shift("Layer1 Blend Height", float) = 1           
        _DetailBump ("Detail Normal", 2D) = "bump" {}  
        _DetailInt ("DetailNormal Intensity", Range(0,1)) = 0.4
        _DetailTiling("DetailNormal Tiling", float) = 2  
    }

    // URP SubShader
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 500

        Pass {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 tangentWS    : TEXCOORD2;
                float3 bitangentWS  : TEXCOORD3;
                float2 uv           : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_Spc);            SAMPLER(sampler_Spc);
            TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
            TEXTURE2D(_AO);             SAMPLER(sampler_AO);
            TEXTURE2D(_layer1Tex);      SAMPLER(sampler_layer1Tex);
            TEXTURE2D(_layer1Norm);     SAMPLER(sampler_layer1Norm);
            TEXTURE2D(_layer1Breakup);  SAMPLER(sampler_layer1Breakup);
            TEXTURE2D(_DetailBump);     SAMPLER(sampler_DetailBump);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _layer1Metal;
                float _layer1BreakupAmnt;
                float _layer1Tiling;
                float _Power;
                float _Shift;
                float _DetailInt;
                float _DetailTiling;
            CBUFFER_END

            Varyings vert(Attributes input) {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS   = posInputs.positionCS;
                output.positionWS   = posInputs.positionWS;
                output.normalWS     = normInputs.normalWS;
                output.tangentWS    = normInputs.tangentWS;
                output.bitangentWS  = normInputs.bitangentWS;
                output.uv           = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                float2 uvMain = input.uv;
                float2 uvLayer = input.uv * _layer1Tiling;
                float2 uvDetail = input.uv * _DetailTiling;

                half3 mainCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvMain).rgb;
                half4 specVal = SAMPLE_TEXTURE2D(_Spc, sampler_Spc, uvMain);
                half4 bumpRaw = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMain);
                half3 norm    = UnpackNormal(bumpRaw);
                half aoVal    = SAMPLE_TEXTURE2D(_AO, sampler_AO, uvMain).r;

                half4 layer1Col = SAMPLE_TEXTURE2D(_layer1Tex, sampler_layer1Tex, uvLayer);
                half3 layer1Norm = UnpackNormal(SAMPLE_TEXTURE2D(_layer1Norm, sampler_layer1Norm, uvLayer));
                half layer1Breakup = SAMPLE_TEXTURE2D(_layer1Breakup, sampler_layer1Breakup, uvLayer).r;
                half3 detNorm = UnpackNormal(SAMPLE_TEXTURE2D(_DetailBump, sampler_DetailBump, uvDetail));

                // World space normal & tangent frame
                half3x3 tbn = half3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalize(input.normalWS));

                half3 modNormalTS = norm + half3(layer1Norm.r * 0.6, layer1Norm.g * 0.6, 0);
                half3 modNormalWS = normalize(mul(modNormalTS, tbn));

                half blend = dot(modNormalWS, half3(0, 1, 0));
                half blend2 = (blend * _Power + _Shift) * lerp(1.0, layer1Breakup, _layer1BreakupAmnt);
                blend2 = saturate(pow(max(0.0, blend2), 3.0));

                half3 blendedNormalTS = lerp(norm, layer1Norm, blend2);
                blendedNormalTS += detNorm * half3(_DetailInt, _DetailInt, 0);
                half3 finalNormalWS = normalize(mul(blendedNormalTS, tbn));

                half3 blendedColor = lerp(mainCol, layer1Col.rgb, blend2);
                half finalSmoothness = lerp(specVal.a, layer1Col.a, blend2);
                half finalMetallic = lerp(specVal.r, _layer1Metal, blend2);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = finalNormalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = blendedColor;
                surfaceData.alpha = 1.0;
                surfaceData.metallic = saturate(finalMetallic);
                surfaceData.smoothness = saturate(finalSmoothness);
                surfaceData.normalTS = blendedNormalTS;
                surfaceData.occlusion = aoVal;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }

        Pass {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input) {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target {
                return 0;
            }
            ENDHLSL
        }

        Pass {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target {
                return 0;
            }
            ENDHLSL
        }
    }

    // Built-in SubShader Fallback
    SubShader {
        Tags { "RenderType"="Opaque" "Queue" = "Geometry" }
        LOD 500
 
        CGPROGRAM
        #include "UnityPBSLighting.cginc"
        #pragma surface surf Standard 
        #pragma target 3.0

        sampler2D _MainTex, _Spc, _BumpMap, _AO, _DetailBump, _layer1Tex, _layer1Norm, _layer1Breakup;
        half _Power, _DetailInt, _DetailTiling, _layer1Tiling, _layer1Metal, _layer1BreakupAmnt, _Shift;

        struct Input {
            half2 uv_MainTex; 
            half2 uv_BumpMap;          
            half3 worldNormal; INTERNAL_DATA         
        };

        void surf (Input IN, inout SurfaceOutputStandard o) {       
            half3 layer1direction = half3(0,1,0);

            half3 main = tex2D (_MainTex, IN.uv_MainTex).rgb;            
            half3 norm = UnpackNormal(tex2D (_BumpMap, IN.uv_BumpMap));
            half4 spec = tex2D(_Spc, IN.uv_MainTex);
            half3 ao = tex2D(_AO, IN.uv_BumpMap).rgb;
            half4 layer1 = tex2D (_layer1Tex, IN.uv_MainTex * _layer1Tiling);
            half3 layer1norm = UnpackNormal(tex2D(_layer1Norm, IN.uv_MainTex * _layer1Tiling));
            half layer1Breakup = tex2D (_layer1Breakup, IN.uv_MainTex * _layer1Tiling).r;
            half3 detnorm = UnpackNormal(tex2D (_DetailBump, IN.uv_MainTex * _DetailTiling));

            half3 modNormal = norm + half3(layer1norm.r * 0.6, layer1norm.g * 0.6, 0);
            half blend = dot(WorldNormalVector(IN, modNormal), layer1direction);
            half blend2 = (blend * _Power + _Shift) * lerp(1, layer1Breakup, _layer1BreakupAmnt);
            blend2 = saturate(pow(max(0, blend2), 3));

            half3 blendedNormal = lerp(norm, layer1norm, blend2);
            blendedNormal = blendedNormal + (detnorm * half3(_DetailInt,_DetailInt,0));
            half3 blendedColor = lerp(main, layer1.rgb, blend2);

            o.Albedo = blendedColor;
            o.Occlusion = ao.r;
            o.Smoothness = lerp(spec.a, layer1.a, blend2);
            o.Metallic = lerp(spec.r, _layer1Metal, blend2);
            o.Normal = blendedNormal.rgb;
        }
        ENDCG
    } 
    FallBack "Universal Render Pipeline/Lit"
}