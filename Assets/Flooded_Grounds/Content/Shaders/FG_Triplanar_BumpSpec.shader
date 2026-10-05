Shader "Flooded_Grounds/Triplanar_BumpSpec"
{
    Properties
    {
        _TexScale ("Tex Scale", Range (0.1, 10.0))= 1.0
        _BlendPlateau ("BlendPlateau", Range (0.0, 1.0)) = 0.2       
        _MainTex ("Base 1 (RGB) Gloss(A)", 2D) = "white" {}
        _BumpMap1 ("NormalMap 1 (_Y_X)", 2D)  = "bump" {}   
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }

    // URP SubShader
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 400

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap1);   SAMPLER(sampler_BumpMap1);

            CBUFFER_START(UnityPerMaterial)
                float _TexScale;
                float _BlendPlateau;
                float _Cutoff;
            CBUFFER_END

            Varyings vert(Attributes input) {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS   = normInputs.normalWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                float3 normWS = normalize(input.normalWS);
                float3 absNorm = abs(normWS);
                float3 blend = pow(absNorm, 4.0);
                blend /= max(0.0001, blend.x + blend.y + blend.z);

                float2 uvX = input.positionWS.zy * _TexScale;
                float2 uvY = input.positionWS.xz * _TexScale;
                float2 uvZ = input.positionWS.xy * _TexScale;

                half4 colX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvX);
                half4 colY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvY);
                half4 colZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvZ);
                half4 blendedCol = colX * blend.x + colY * blend.y + colZ * blend.z;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = blendedCol.rgb;
                surfaceData.alpha = 1.0;
                surfaceData.metallic = 0.0;
                surfaceData.smoothness = 0.2;
                surfaceData.occlusion = 1.0;

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

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input) {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }
            half4 ShadowPassFragment(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    // Built-in SubShader Fallback
    SubShader
    {
        ZWrite On            
        Tags {"Queue"="Geometry" "IgnoreProjector"="True" "RenderType"="Opaque"}
        LOD 400

        CGPROGRAM
        #pragma target 3.0                               
        #pragma surface surf Lambert vertex:vertWorld alphatest:_Cutoff
                   
        sampler2D _MainTex, _BumpMap1;          
        half _TexScale, _BlendPlateau;

        struct Input
        {
            float3 thisPos;        
            float3 thisNormal;
            half4 color : COLOR; 
        };                    

        void vertWorld (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.thisPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.thisNormal = mul(unity_ObjectToWorld, float4(v.normal, 0.0)).xyz;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            half3 absNormal = abs(IN.thisNormal);
            half3 blend = saturate(absNormal - _BlendPlateau);
            blend /= (blend.x + blend.y + blend.z);

            half4 colX = tex2D(_MainTex, IN.thisPos.zy * _TexScale);
            half4 colY = tex2D(_MainTex, IN.thisPos.xz * _TexScale);
            half4 colZ = tex2D(_MainTex, IN.thisPos.xy * _TexScale);
            half4 finalCol = colX * blend.x + colY * blend.y + colZ * blend.z;

            o.Albedo = finalCol.rgb;
            o.Alpha = finalCol.a;
        }
        ENDCG
    }
    FallBack "Universal Render Pipeline/Lit"
}