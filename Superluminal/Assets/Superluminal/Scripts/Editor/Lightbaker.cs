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

		private Dictionary<string, BakeTarget> targets;

		private Dictionary<MeshRenderer, BakeTarget> rendererMap;

		private MeshRepository meshRespository;

		private BakeData bakeData;
		
		public Lightbaker()
		{
			targets = new Dictionary<string, BakeTarget>();
			rendererMap = new Dictionary<MeshRenderer, BakeTarget>();

			context = new BakeContext();
			raytracer = new Raytracer(context);
		}

		public void Setup()
		{
			targets.Clear();
			rendererMap.Clear();

			SetupBakeData();

			foreach (BakeTarget target in bakeData.targets)
				StoreTarget(target);

			List<Light> lights = new List<Light>();
			List<Submesh> submeshes = new List<Submesh>();
			
			CollectMeshes(submeshes);
			CollectLights(lights);

			context.Setup(submeshes, lights);
		}

		public void Bake()
		{
			Scene scene = EditorSceneManager.GetActiveScene();
			meshRespository = new MeshRepository(scene);

			// Iterate through all bake targets
			foreach (KeyValuePair<string, BakeTarget> pair in targets)
			{
				// Bake the new mesh
				Bake(pair.Value);

				// Store the new baked mesh
				meshRespository.StoreMesh(pair.Value.bakedMesh, pair.Value.guid);
			}

			StoreBakeData();
		}

		private void Bake(BakeTarget target)
		{
			Vector3[] vertices = target.originalMesh.vertices;
			Vector3[] normals = target.originalMesh.normals;
			Vector2[] texcoords = target.originalMesh.uv;
			
			// Create a new mesh with the same vertex attributes
			Mesh bakedMesh = new Mesh();
			bakedMesh.vertices = vertices;
			bakedMesh.normals = normals;
			bakedMesh.uv = texcoords;

			// Copy submesh indices to the new mesh
			List<int> indices = new List<int>();
			for (int submeshIdx = 0; submeshIdx < target.originalMesh.subMeshCount; ++submeshIdx)
			{
				indices.Clear();
				target.originalMesh.GetIndices(indices, submeshIdx);
				bakedMesh.SetTriangles(indices, submeshIdx);
			}

			// Bake vertex colors
			Color[] colors = new Color[target.originalMesh.vertexCount];
			

			// Store the generated colors
			bakedMesh.colors = colors;

			target.bakedMesh = bakedMesh;
		}

		private void SetupBakeData()
		{
			// Find if there is already a BakeData object in the scene
			bakeData = Object.FindObjectOfType<BakeData>();

			if (bakeData == null)
			{
				// If there's not, create one
				GameObject bakeDataObject = new GameObject("BakeData");
				bakeData = bakeDataObject.AddComponent<BakeData>();

				// Make sure the new object is saved
				EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
			}

		}

		private void StoreBakeData()
		{
			// Store the list of baked meshes in the bake data
			bakeData.targets = targets.Values.ToArray();

			// Write the meshes to disk
			meshRespository.Flush();

			// Save the scene
			EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		}

		private void StoreTarget(BakeTarget target)
		{
			targets.Add(target.guid, target);
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

					// If the material has a supported shader, add it as a mesh that is part of the reflection.
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
