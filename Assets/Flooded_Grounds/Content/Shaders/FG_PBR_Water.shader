Shader "Flooded_Grounds/PBR_Water" {
Properties {
	_Color ("Main Color", Color) = (0.786, 0.791, 0.626, 1.0)
	_Emis("Self-Ilumination", Range(0,1)) = 0.05
	_Smth("Smoothness", Range(0,1)) = 0.95
	_Parallax ("Height", Range (0.005, 0.08)) = 0.02
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_BumpMap ("Normalmap", 2D) = "bump" {}
	_BumpMap2 ("Normalmap2", 2D) = "bump" {}
	_BumpLerp("Normalmap2 Blend", Range(0,1)) = 0.5
	_ParallaxMap ("Heightmap", 2D) = "black" {}
	_ScrollSpeed("Scroll Speed", float) = 0.2
	_WaveFreq("Wave Frequency", float) = 20
	_WaveHeight("Wave Height", float) = 0.05
}

// URP SubShader
SubShader { 
	Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "RenderPipeline"="UniversalPipeline" }
	LOD 200
	
	Pass {
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

		TEXTURE2D(_MainTex);     SAMPLER(sampler_MainTex);
		TEXTURE2D(_BumpMap);     SAMPLER(sampler_BumpMap);
		TEXTURE2D(_BumpMap2);    SAMPLER(sampler_BumpMap2);
		TEXTURE2D(_ParallaxMap); SAMPLER(sampler_ParallaxMap);

		CBUFFER_START(UnityPerMaterial)
			float4 _Color;
			float _Emis;
			float _Smth;
			float _Parallax;
			float _BumpLerp;
			float _ScrollSpeed;
			float _WaveFreq;
			float _WaveHeight;
		CBUFFER_END

		Varyings vert(Attributes input) {
			Varyings output;
			float phase = _Time.y * _WaveFreq * 0.1;
			float offset = (input.positionOS.x + input.positionOS.z * 2.0) * 0.5;
			float waveY = sin(phase + offset) * _WaveHeight;
			float3 modifiedPosOS = input.positionOS.xyz + float3(0, waveY, 0);

			VertexPositionInputs posInputs = GetVertexPositionInputs(modifiedPosOS);
			VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

			output.positionCS   = posInputs.positionCS;
			output.positionWS   = posInputs.positionWS;
			output.normalWS     = normInputs.normalWS;
			output.tangentWS    = normInputs.tangentWS;
			output.bitangentWS  = normInputs.bitangentWS;
			output.uv           = input.uv;
			return output;
		}

		half4 frag(Varyings input) : SV_Target {
			float timeVal = _Time.y * _ScrollSpeed * 0.05;
			float2 uv1 = input.uv * 12.0 + float2(timeVal, timeVal * 0.5);
			float2 uv2 = input.uv * 16.0 + float2(-timeVal * 0.7, timeVal * 0.4);

			half3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv1));
			half3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv2));
			half3 nCombined = normalize(half3(n1.xy + n2.xy, n1.z));

			half3x3 tbn = half3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalize(input.normalWS));
			half3 normalWS = normalize(mul(nCombined, tbn));

			half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv * 10.0 + float2(timeVal * 0.2, timeVal * 0.1));
			half3 waterAlbedo = texCol.rgb * _Color.rgb;

			InputData inputData = (InputData)0;
			inputData.positionWS = input.positionWS;
			inputData.normalWS = normalWS;
			inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
			inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

			SurfaceData surfaceData = (SurfaceData)0;
			surfaceData.albedo = waterAlbedo;
			surfaceData.alpha = 1.0;
			surfaceData.metallic = 0.05;
			surfaceData.smoothness = _Smth;
			surfaceData.emission = waterAlbedo * _Emis;
			surfaceData.occlusion = 1.0;

			return UniversalFragmentPBR(inputData, surfaceData);
		}
		ENDHLSL
	}
}

// Built-in SubShader Fallback
SubShader { 
	Tags { "RenderType"="Opaque" "Queue"="Geometry" }
	LOD 200
	
	CGPROGRAM
	#include "UnityPBSLighting.cginc"
	#pragma surface surf Standard vertex:vert
	#pragma target 3.0

	sampler2D _MainTex, _BumpMap, _BumpMap2, _ParallaxMap;
	fixed4 _Color;
	half _ScrollSpeed, _WaveFreq, _WaveHeight;
	float _Parallax;
	half _Smth, _Emis, _BumpLerp;

	struct Input {
		half2 uv_MainTex;
		half2 uv_BumpMap;
		half2 uv_BumpMap2;
		half2 uv_ParallaxMap;
		half3 viewDir;
		INTERNAL_DATA	
	};

	void vert (inout appdata_full v) {
		float phase = _Time * _WaveFreq;
		float offset = (v.vertex.x + (v.vertex.z * 2)) * 8;
		v.vertex.y = sin(phase + offset) * _WaveHeight; 
	}

	void surf (Input IN, inout SurfaceOutputStandard o) {
		half scrollX = _ScrollSpeed * _Time;
		half scrollY = (_ScrollSpeed * _Time) * 0.5;
		half scrollX2 = (1 - _ScrollSpeed) * _Time;
		half scrollY2 = (1 - _ScrollSpeed * _Time) * 0.5;

		half2 uv1 = IN.uv_BumpMap + half2(scrollX, scrollY);
		half2 uv2 = IN.uv_BumpMap + half2(scrollX2, scrollY2);

		half3 nrml = UnpackNormal(tex2D(_BumpMap, uv1));
		half3 nrml2 = UnpackNormal(tex2D(_BumpMap, uv2));
		half3 nrml3 = UnpackNormal(tex2D(_BumpMap2, IN.uv_BumpMap2));
		half3 finalnormal = lerp(nrml.rgb + (nrml2.rgb * half3(1,1,0)), nrml3, _BumpLerp);

		half4 tex = tex2D(_MainTex, IN.uv_MainTex);

		o.Albedo = tex * _Color;
		o.Smoothness = _Smth;
		o.Metallic = 0;
		o.Emission = tex * _Color * _Emis;
		o.Normal = normalize(finalnormal);
	}
	ENDCG
}
FallBack "Universal Render Pipeline/Lit"
}
