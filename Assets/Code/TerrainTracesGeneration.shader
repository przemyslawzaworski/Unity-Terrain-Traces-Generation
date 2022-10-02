Shader "Hidden/TerrainTracesGeneration"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex VSMain
			#pragma fragment PSMain
			#pragma target 5.0

			struct Brush 
			{
				float2 Center;
				float Height;
				int Index;
				float Radius;
			};

			sampler2D _RenderTexture, _HeightMap;
			StructuredBuffer<Brush> _ComputeBuffer;
			float _TerrainPositionY, _Fade;
			float3 _TerrainMaxSize;
			int _Count, _TerrainIndex;

			float Circle (float2 p, float2 c, float r)
			{
				return 1.0 - step(0.0, length(p - c) - r);
			}

			float4 VSMain (in float4 vertex:POSITION, inout float2 uv:TEXCOORD0) : SV_POSITION
			{
				return UnityObjectToClipPos(vertex);
			}

			float4 PSMain (float4 vertex : SV_POSITION, float2 uv : TEXCOORD0) : SV_TARGET
			{
				float height = tex2D(_HeightMap, uv).r * 2.0 * _TerrainMaxSize.y + _TerrainPositionY;
				float accumulation = 0.0;
				for (int i = 0; i < _Count; i++)
				{
					bool intersection = distance(_ComputeBuffer[i].Height, height) < (_ComputeBuffer[i].Radius * _TerrainMaxSize.x);
					if (_TerrainIndex == _ComputeBuffer[i].Index && intersection)
						accumulation += Circle(uv, _ComputeBuffer[i].Center, _ComputeBuffer[i].Radius);
				}
				accumulation += tex2D(_RenderTexture, uv).r * _Fade; // accumulate with previous frames to make fade effect
				return float4(accumulation, 0.0, 0.0, 1.0);
			}
			ENDCG
		}
	}
}