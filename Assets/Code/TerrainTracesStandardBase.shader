Shader "Hidden/TerrainTracesStandardBase" 
{
	Properties 
	{
		_MainTex ("Base (RGB) Smoothness (A)", 2D) = "white" {}
		_MetallicTex ("Metallic (R)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	SubShader 
	{
		Tags 
		{
			"RenderType" = "Opaque"
			"Queue" = "Geometry-100"
			"TerrainCompatible" = "True"
		}
		LOD 200

		CGPROGRAM
		#pragma surface SurfaceShader Standard fullforwardshadows
		#pragma target 3.0
		#pragma exclude_renderers gles
		#include "UnityPBSLighting.cginc"

		sampler2D _MainTex;
		sampler2D _MetallicTex;

		struct Input 
		{
			float2 uv_MainTex;
		};

		void SurfaceShader (Input IN, inout SurfaceOutputStandard o) 
		{
			half4 color = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = color.rgb;
			o.Alpha = 1;
			o.Smoothness = color.a;
			o.Metallic = tex2D (_MetallicTex, IN.uv_MainTex).r;
		}
		ENDCG
	}
	FallBack "Diffuse"
}