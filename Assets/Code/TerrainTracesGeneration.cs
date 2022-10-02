using UnityEngine;
using System.Runtime.InteropServices;

public class TerrainTracesGeneration : MonoBehaviour
{
	[Tooltip("Array of source game objects.")]
	public Transform[] Emitters;
	[Tooltip("Array of destination terrains.")]
	public Terrain[] Terrains;
	[Tooltip("Shader: Hidden/TerrainTracesGeneration")]
	public Shader TerrainTracesShader;
	[Tooltip("Render textures resolution. Higher value = better quality and lower performance.")]
	public int Resolution = 2048;
	[Tooltip("Radius value for emitters without renderer [meters].")]
	public float Radius = 1.0f;
	[Tooltip("Register only moving emitters, with velocity above this value [m/s].")]
	public float MinimumSpeed = 0.05f;
	[Tooltip("Number of frames to skip rendering of traces. Higher value = lower accuracy and better performance. Must be higher than zero.")]
	public int Delay = 1;
	[Tooltip("How quickly traces are faded.")]
	[Range(0.0f, 1.0f)] public float Fade = 0.1f;
	[Tooltip("Brush intensity.")]
	[Range(0.0f, 1.0f)] public float Intensity = 0.5f;
	[Tooltip("Base color for traces.")]
	public Color BaseColor = Color.blue;

	private ComputeBuffer _ComputeBuffer;
	private Material[] _Materials;
	private RenderTexture[] _Inputs, _Outputs;
	private bool _Swap = true;
	private Renderer[] _Renderers;
	private float[] _CurrentSpeedsArray;
	private Vector3[] _CurrentPositionsArray;
	private Brush _BrushClear = new Brush { Center = new Vector2(1e8f, 1e8f), Height = 0.0f, Index = -1, Radius = 0.0f};
	private Brush[] _Brushes;

	struct Brush 
	{
		public Vector2 Center;
		public float Height;
		public int Index;
		public float Radius;
	}

	// Get current terrain at the given position defined in world space (vec3).
	Terrain GetCurrentTerrain (Terrain[] terrains, Vector3 vec3, out int index)
	{
		for (int i = 0; i < terrains.Length; i++)
		{
			Terrain terrain = terrains[i];
			Vector3 p = terrain.transform.position;
			Vector3 s = terrain.terrainData.size;
			if (vec3.x > p.x && vec3.x < (p.x + s.x) && vec3.z > p.z && vec3.z < (p.z + s.z)) 
			{
				index = i;
				return terrain;
			}
		}
		index = -1;
		return null;
	}

	// Converts world space (vec3) to terrain normalized uv space.
	Vector2 WorldSpaceToTerrainUV (Terrain terrain, Vector3 vec3)
	{
		Vector3 p = terrain.transform.position;
		Vector3 s = terrain.terrainData.size;
		float x = (vec3.x - p.x) / s.x;
		float y = (vec3.z - p.z) / s.z;
		return new Vector2(x, y);
	}

	void ComputeSpeedForObjects(Transform[] transforms)
	{
		if (transforms.Length == _CurrentSpeedsArray.Length && transforms.Length == _CurrentPositionsArray.Length)
		{
			for (int i = 0; i < transforms.Length; i++)
			{
				_CurrentSpeedsArray[i] = ((transforms[i].position - _CurrentPositionsArray[i]).magnitude) / Time.deltaTime;
				_CurrentPositionsArray[i] = transforms[i].position;
			}
		}
	}

	void RenderToTexture (RenderTexture source, RenderTexture destination, Material material)
	{
		RenderTexture.active = destination;
		GL.PushMatrix();
		GL.LoadOrtho();
		GL.invertCulling = true;
		material.SetPass(0);
		GL.Begin(GL.QUADS);
		GL.MultiTexCoord2(0, 0.0f, 0.0f);
		GL.Vertex3(0.0f, 0.0f, 0.0f);
		GL.MultiTexCoord2(0, 1.0f, 0.0f);
		GL.Vertex3(1.0f, 0.0f, 0.0f); 
		GL.MultiTexCoord2(0, 1.0f, 1.0f);
		GL.Vertex3(1.0f, 1.0f, 0.0f); 
		GL.MultiTexCoord2(0, 0.0f, 1.0f);
		GL.Vertex3(0.0f, 1.0f, 0.0f);
		GL.End();
		GL.invertCulling = false;
		GL.PopMatrix();
	}

	void CreateMaps()
	{
		if (Terrains.Length == 0) return;
		_Materials = new Material[Terrains.Length];
		for (int i = 0; i < _Materials.Length; i++) _Materials[i] = new Material(TerrainTracesShader);
		_Inputs = new RenderTexture[Terrains.Length];
		_Outputs = new RenderTexture[Terrains.Length];
		for (int i = 0; i < _Inputs.Length; i++) _Inputs[i] = new RenderTexture(Resolution, Resolution, 0, RenderTextureFormat.RFloat);
		for (int i = 0; i < _Outputs.Length; i++) _Outputs[i] = new RenderTexture(Resolution, Resolution, 0, RenderTextureFormat.RFloat);
	}

	void CreateEmitters()
	{
		if (Emitters.Length == 0) return;
		if (_ComputeBuffer != null) _ComputeBuffer.Release();
		_ComputeBuffer = new ComputeBuffer(Emitters.Length, Marshal.SizeOf(typeof(Brush)), ComputeBufferType.Default);
		_CurrentSpeedsArray = new float[Emitters.Length];
		_CurrentPositionsArray = new Vector3[Emitters.Length];
		_Brushes = new Brush[Emitters.Length];
		_Renderers = new Renderer[Emitters.Length];
		for (int i = 0; i < Emitters.Length; i++)
		{
			_Renderers[i] = Emitters[i].gameObject.GetComponent<Renderer>();
		}
	}

	void DeleteMaps()
	{
		if (_Materials != null) for (int i = 0; i < _Materials.Length; i++) if (_Materials[i] != null) Destroy(_Materials[i]);
		if (_Inputs != null) for (int i = 0; i < _Inputs.Length; i++) if (_Inputs[i] != null) _Inputs[i].Release();
		if (_Outputs != null) for (int i = 0; i < _Outputs.Length; i++) if (_Outputs[i] != null) _Outputs[i].Release();
	}

	void DeleteEmitters()
	{
		if (_ComputeBuffer != null) _ComputeBuffer.Release();
	}

	void Start()
	{
		CreateMaps();
		CreateEmitters();
	}

	void Update()
	{
		if (Emitters.Length == 0 || Terrains.Length == 0) return;
		ComputeSpeedForObjects(Emitters); // don't draw trackmarks for static objects
		for (int i = 0; i < Emitters.Length; i++) // iterate over source gameobjects
		{
			Terrain terrain = GetCurrentTerrain (Terrains, Emitters[i].position, out int index);
			if (terrain && _CurrentSpeedsArray[i] > MinimumSpeed)
			{
				Vector3 size = (_Renderers[i] != null) ? _Renderers[i].bounds.size : new Vector3(Radius, Radius, Radius);
				float radius = Mathf.Max(size.x, Mathf.Max(size.y, size.z)) / terrain.terrainData.size.x * 0.5f;
				Vector2 center = WorldSpaceToTerrainUV(terrain, Emitters[i].position);
				Brush brush;
				brush.Center = new Vector2(center.x, center.y);
				brush.Height = Emitters[i].position.y;
				brush.Index = index;
				brush.Radius = radius;
				_Brushes[i] = brush;
			}
			else
			{
				_Brushes[i] = _BrushClear; // don't draw invisible trackmarks
			}
		}
		_ComputeBuffer.SetData (_Brushes); // send brush parameters to GPU memory
		if (Time.frameCount % Delay == 0)
		{
			for (int i = 0; i < _Materials.Length; i++) // iterate over off-screen materials
			{
				_Materials[i].SetBuffer("_ComputeBuffer", _ComputeBuffer);
				_Materials[i].SetInt("_TerrainIndex", i);
				_Materials[i].SetFloat("_Fade", 1.0f - Mathf.Lerp(0.0f, 2.0f * Time.deltaTime, Fade));
				_Materials[i].SetInt("_Count", Emitters.Length);
				_Materials[i].SetTexture("_HeightMap", Terrains[i].terrainData.heightmapTexture);
				_Materials[i].SetVector("_TerrainMaxSize", Terrains[i].terrainData.size);
				_Materials[i].SetFloat("_TerrainPositionY", Terrains[i].transform.position.y);
				if (_Swap)
				{
					_Materials[i].SetTexture("_RenderTexture", _Inputs[i]);
					RenderToTexture(_Inputs[i], _Outputs[i], _Materials[i]); // execute TerrainTracesShader and write result to _Outputs[i]
				}
				else
				{
					_Materials[i].SetTexture("_RenderTexture", _Outputs[i]);
					RenderToTexture(_Outputs[i], _Inputs[i], _Materials[i]); // execute TerrainTracesShader and write result to _Inputs[i]
				}
				Terrains[i].materialTemplate.SetColor("_TerrainTracesBaseColor", BaseColor);
				Terrains[i].materialTemplate.SetFloat("_TerrainTracesIntensity", Intensity);
				Terrains[i].materialTemplate.SetTexture("_TerrainTracesRenderTexture", _Outputs[i]);
			}
			_Swap = !_Swap;
		}
	}

	void OnDestroy()
	{
		DeleteEmitters();
		DeleteMaps();
	}
}