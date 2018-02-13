using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEditor;
using UnityEditor.SceneManagement;

namespace Superluminal
{
	/// <summary>
	/// The main lightbaking class. Will collect meshes from the scene and store newly generated meshes
	/// </summary>
	public class Lightbaker
	{
		private BakeContext context;

		private Raytracer raytracer;

		private Dictionary<string, BakeTarget> bindings;

		private Dictionary<MeshRenderer, BakeTarget> rendererMap;

		private MeshRepository meshRespository;

		private BakeData bakeData;
		
		public Lightbaker()
		{
			bindings = new Dictionary<string, BakeTarget>();
			rendererMap = new Dictionary<MeshRenderer, BakeTarget>();

			context = new BakeContext();
			raytracer = new Raytracer(context);
		}

		public void Setup()
		{
			bindings.Clear();
			rendererMap.Clear();

			SetupBakeData();

			foreach (BakeTarget target in bakeData.targets)
				StoreTarget(target);

			List<Light> lights = new List<Light>();
			List<Submesh> submeshes = new List<Submesh>();
			
			CollectMeshes(submeshes);

			lights.Clear();
			CollectLights(lights);

			context.Setup(submeshes, lights);
		}

		public void Bake()
		{
			Scene scene = EditorSceneManager.GetActiveScene();
			meshRespository = new MeshRepository(scene);

			foreach (KeyValuePair<string, BakeTarget> pair in bindings)
			{
				Bake(pair.Value);
				meshRespository.StoreMesh(pair.Value.bakedMesh, pair.Value.guid);
			}

			StoreBakeData();
		}

		private void Bake(BakeTarget target)
		{
			Mesh bakedMesh = new Mesh();
			bakedMesh.vertices = target.originalMesh.vertices;
			bakedMesh.uv = target.originalMesh.uv;
			bakedMesh.normals = target.originalMesh.normals;

			List<int> indices = new List<int>();
			for (int submeshIdx = 0; submeshIdx < target.originalMesh.subMeshCount; ++submeshIdx)
			{
				indices.Clear();
				target.originalMesh.GetIndices(indices, submeshIdx);
				bakedMesh.SetTriangles(indices, submeshIdx);
			}

			target.bakedMesh = bakedMesh;
		}

		private void SetupBakeData()
		{
			bakeData = Object.FindObjectOfType<BakeData>();

			if (bakeData == null)
			{
				GameObject bakeDataObject = new GameObject("BakeData");
				bakeData = bakeDataObject.AddComponent<BakeData>();

				EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
			}

		}

		private void StoreBakeData()
		{
			bakeData.targets = bindings.Values.ToArray();

			meshRespository.Flush();
			EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		}

		private void StoreTarget(BakeTarget target)
		{
			bindings.Add(target.guid, target);
			rendererMap.Add(target.renderer, target);
		}


		private void CollectMeshes(List<Submesh> submeshes)
		{
			List<BakeTargetSubmesh> targetSubmeshes = new List<BakeTargetSubmesh>();

			MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>();

			foreach (MeshRenderer renderer in renderers)
			{
				// Check if the GameObject is marked as LightmapStatic
				if ((GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.LightmapStatic) == 0)
					continue;

				// Retrieve the linked mesh
				MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
				Mesh mesh = meshFilter.sharedMesh;

				if (mesh == null)
					continue;

				Material[] materials = renderer.sharedMaterials;

				targetSubmeshes.Clear();

				// Iterate through all submeshes in the meshss
				for (int submeshIdx = 0; submeshIdx < mesh.subMeshCount; ++submeshIdx)
				{
					Material material = materials[submeshIdx % materials.Length];

					// If the material has the vertex baked shader, store it as a mesh to bake
					if (material.shader.name == "Superluminal/VertexBaked")
					{
						// Create a submesh for the binding
						targetSubmeshes.Add(new BakeTargetSubmesh()
						{
							idx = submeshIdx,
							material = material
						});
					}

					// Check if the materials' shader is supported
					if (material.shader.name == "Superluminal/VertexBaked" || material.shader.name == "Standard")
					{
						submeshes.Add(new Submesh()
						{
							transform = renderer.transform,
							material = material,
							mesh = mesh,
							submeshIdx = submeshIdx,
						});
					}
					else
					{ 
						Debug.LogWarning("[Superluminal]: Ignoring submesh because of unsupported shader", renderer);
						continue;
					}
					
				}

				if (targetSubmeshes.Count > 0)
				{
					BakeTarget target;
					if (!rendererMap.TryGetValue(renderer, out target))
					{
						// Create a new bae target with all eligable submeshes
						target = new BakeTarget()
						{
							guid = GUID.Generate().ToString(),
							renderer = renderer,
							originalMesh = mesh,
							bakedMesh = null,

							submeshes = targetSubmeshes,
						};

						StoreTarget(target);
					}
					else
					{
						foreach (BakeTargetSubmesh submesh in targetSubmeshes)
						{
							// Check if a submesh with this index was already present in the submesh list
							int existingSubmeshIndex = target.submeshes.FindIndex(s => s.idx == submesh.idx);

							if (existingSubmeshIndex >= 0)
							{
								// Remove the previous submesh and add the new one
								target.submeshes.RemoveAt(existingSubmeshIndex);
								target.submeshes.Add(submesh);
							}
						}
					}
				}
			}
		}

		private void CollectLights(List<Light> lights)
		{
			Light[] lightComponents = Object.FindObjectsOfType<Light>();

			foreach (Light light in lightComponents)
			{
				if (light.type != LightType.Directional)
					continue;

				lights.Add(light);
			}
		}

		public BakeContext Scene
		{
			get { return context; }
		}
	}
}
