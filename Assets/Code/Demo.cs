using UnityEngine;

public class Demo : MonoBehaviour
{
	public int Count = 64;
	public Geometry GeometryType;
	public Scenario ScenarioType;
	[Range(0, 1000)] public int FrameRate = 800;

	public enum Geometry {Sphere = 0, Cube = 3}
	public enum Scenario {Bouncing, Crazy}

	Transform[] _Transforms;
	Material _Material;

	void Awake()
	{
		TerrainTracesGeneration terrainTraces = GetComponent<TerrainTracesGeneration>();
		if (terrainTraces == null) return;
		PhysicMaterial physicMaterial = new PhysicMaterial();
		physicMaterial.bounciness = 0.8f;
		physicMaterial.bounceCombine = PhysicMaterialCombine.Maximum;
		_Material = new Material(Shader.Find("Legacy Shaders/Diffuse"));
		_Material.enableInstancing = true;
		_Transforms = new Transform[Count];
		for (int i = 0; i < _Transforms.Length; i++)
		{
			GameObject emitter = GameObject.CreatePrimitive((PrimitiveType)GeometryType);
			emitter.name = "Emitter" + i.ToString();
			_Transforms[i] = emitter.transform;
			_Transforms[i].position = new Vector3(Random.Range(64.0f, 196.0f), 50.0f, Random.Range(64.0f, 196.0f));
			float scale = UnityEngine.Random.Range(0.5f, 2.5f);
			_Transforms[i].localScale = new Vector3(1.0f, 1.0f, 1.0f) * scale;
			_Transforms[i].gameObject.AddComponent<Rigidbody>();
			_Transforms[i].gameObject.GetComponent<Renderer>().sharedMaterial = _Material;
			if (ScenarioType == Scenario.Bouncing)
				_Transforms[i].gameObject.GetComponent<Collider>().material = physicMaterial;
		}
		terrainTraces.Emitters = _Transforms;
		terrainTraces.Terrains = FindObjectsOfType<Terrain>();
	}

	void FixedUpdate()
	{
		Application.targetFrameRate = FrameRate;
		if (ScenarioType == Scenario.Crazy)
		{
			for (int i = 0; i < _Transforms.Length; i++)
			{
				float x = Mathf.Sign(Random.Range(-50.0f, 50.0f)) * Random.Range(-1.0f, 1.0f) * 0.5f;
				float y = Mathf.Sign(Random.Range(-50.0f, 50.0f)) * Random.Range(-1.0f, 1.0f) * 0.5f;
				_Transforms[i].Translate(new Vector3(x, 0.0f, y));
			}
		}
	}

	void OnDestroy()
	{
		if (_Material != null) Destroy(_Material);
	}
}