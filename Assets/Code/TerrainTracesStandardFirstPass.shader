Shader "Nature/Terrain/Terrain Traces Standard" 
{
	Properties 
	{
		[HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
		[HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
		[HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
		[HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
		[HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
		[HideInInspector] _Normal3 ("Normal 3 (A)", 2D) = "bump" {}
		[HideInInspector] _Normal2 ("Normal 2 (B)", 2D) = "bump" {}
		[HideInInspector] _Normal1 ("Normal 1 (G)", 2D) = "bump" {}
		[HideInInspector] _Normal0 ("Normal 0 (R)", 2D) = "bump" {}
		[HideInInspector] [Gamma] _Metallic0 ("Metallic 0", Range(0.0, 1.0)) = 0.0
		[HideInInspector] [Gamma] _Metallic1 ("Metallic 1", Range(0.0, 1.0)) = 0.0
		[HideInInspector] [Gamma] _Metallic2 ("Metallic 2", Range(0.0, 1.0)) = 0.0
		[HideInInspector] [Gamma] _Metallic3 ("Metallic 3", Range(0.0, 1.0)) = 0.0
		[HideInInspector] _Smoothness0 ("Smoothness 0", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _Smoothness1 ("Smoothness 1", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _Smoothness2 ("Smoothness 2", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _Smoothness3 ("Smoothness 3", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "white" {}
		[HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
		[HideInInspector] _TerrainTracesBaseColor ("Terrain Traces Base Color", Color) = (1,1,1,1)
		[HideInInspector] _TerrainTracesIntensity ("Terrain Traces Intensity", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _TerrainTracesRenderTexture ("Terrain Traces Render Texture", 2D) = "black" {}
	}

	SubShader 
	{
		Tags 
		{
			"Queue" = "Geometry-100"
			"RenderType" = "Opaque"
			"TerrainCompatible" = "True"
		}

		CGPROGRAM
		#pragma surface SurfaceShader Standard vertex:VSMain finalcolor:FinalColor finalgbuffer:FinalGBuffer addshadow fullforwardshadows noinstancing
		#pragma multi_compile_fog
		#pragma target 3.0
		#pragma exclude_renderers gles psp2
		#include "UnityPBSLighting.cginc"
		#pragma multi_compile __ _TERRAIN_NORMAL_MAP

		sampler2D _Control;
		float4 _Control_ST;
		sampler2D _Splat0,_Splat1,_Splat2,_Splat3;
		sampler2D _Normal0, _Normal1, _Normal2, _Normal3, _NormalMap;
		half _Metallic0, _Metallic1, _Metallic2, _Metallic3;
		half _Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3;

		float4 _TerrainTracesBaseColor;
		float _TerrainTracesIntensity;
		sampler2D _TerrainTracesRenderTexture;

		struct Input
		{
			float2 uv_Splat0 : TEXCOORD0;
			float2 uv_Splat1 : TEXCOORD1;
			float2 uv_Splat2 : TEXCOORD2;
			float2 uv_Splat3 : TEXCOORD3;
			float2 tc_Control : TEXCOORD4;
			UNITY_FOG_COORDS(5)
		};

		void VSMain (inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);
			data.tc_Control = TRANSFORM_TEX(v.texcoord, _Control); 
			float4 position = UnityObjectToClipPos(v.vertex);
			UNITY_TRANSFER_FOG(data, position);
			v.tangent.xyz = cross(v.normal, float3(0,0,1));
			v.tangent.w = -1;
		}

		void FinalColor (Input IN, SurfaceOutputStandard o, inout fixed4 color)
		{
			color *= o.Alpha;
			#ifdef TERRAIN_SPLAT_ADDPASS
				UNITY_APPLY_FOG_COLOR(IN.fogCoord, color, fixed4(0,0,0,0));
			#else
				UNITY_APPLY_FOG(IN.fogCoord, color);
			#endif
		}

		void FinalGBuffer (Input IN, SurfaceOutputStandard o, inout half4 outGBuffer0, inout half4 outGBuffer1, inout half4 outGBuffer2, inout half4 emission)
		{
			UnityStandardDataApplyWeightToGbuffer(outGBuffer0, outGBuffer1, outGBuffer2, o.Alpha);
			emission *= o.Alpha;
		}

		void SurfaceShader (Input IN, inout SurfaceOutputStandard o) 
		{
			half4 control = tex2D(_Control, IN.tc_Control);
			half weight = dot(control, half4(1,1,1,1));
			#if !defined(SHADER_API_MOBILE) && defined(TERRAIN_SPLAT_ADDPASS)
				clip(weight == 0.0f ? -1 : 1);
			#endif
			control /= (weight + 1e-3f);
			float4 a = tex2D(_Splat0, IN.uv_Splat0);
			float4 b = tex2D(_Splat1, IN.uv_Splat1);
			float4 c = tex2D(_Splat2, IN.uv_Splat2);
			float4 d = tex2D(_Splat3, IN.uv_Splat3);
			fixed4 diffuse = 0.0f;
			diffuse += control.r * a * half4(1.0, 1.0, 1.0, _Smoothness0);
			diffuse += control.g * b * half4(1.0, 1.0, 1.0, _Smoothness1);
			diffuse += control.b * c * half4(1.0, 1.0, 1.0, _Smoothness2);
			diffuse += control.a * d * half4(1.0, 1.0, 1.0, _Smoothness3);
			float source = tex2D(_TerrainTracesRenderTexture, IN.tc_Control).r;
			diffuse = lerp(diffuse, _TerrainTracesBaseColor, min(source, _TerrainTracesIntensity));
			fixed4 normal = 0.0f;
			normal += control.r * tex2D(_Normal0, IN.uv_Splat0);
			normal += control.g * tex2D(_Normal1, IN.uv_Splat1);
			normal += control.b * tex2D(_Normal2, IN.uv_Splat2);
			normal += control.a * tex2D(_Normal3, IN.uv_Splat3);
			o.Albedo = diffuse.rgb;
			o.Alpha = weight;
			o.Normal = UnpackNormal(normal);
			o.Smoothness = diffuse.a;
			o.Metallic = dot(control, half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3));
		}
		ENDCG
	}
	Dependency "AddPassShader" = "Hidden/TerrainTracesStandardAddPass"
	Dependency "BaseMapShader" = "Hidden/TerrainTracesStandardBase"
	Fallback "Nature/Terrain/Diffuse"
}