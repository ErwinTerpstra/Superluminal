using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Superluminal
{
	public class Lightbaker
	{
		private Scene scene;

		private Raytracer raytracer;

		private List<MeshBinding> bindings;

		private List<Light> lights;

		public Lightbaker()
		{
			bindings = new List<MeshBinding>();
			lights = new List<Light>();

			scene = new Scene();
			raytracer = new Raytracer(scene);
		}

		public void SetupScene()
		{
			bindings.Clear();
			CollectMeshes(bindings);

			lights.Clear();
			CollectLights(lights);

			scene.Setup(bindings, lights);
		}

		public void Bake()
		{
			foreach (MeshBinding binding in bindings)
			{
				// Generate new meshes
			}

			StoreBakeData();
		}

		private void StoreBakeData()
		{
			BakeData bakeData = Object.FindObjectOfType<BakeData>();

			if (bakeData == null)
			{
				GameObject bakeDataObject = new GameObject("BakeData");
				bakeData = bakeDataObject.AddComponent<BakeData>();
			}

			bakeData.bindings = bindings.ToArray();
		}

		private void CollectMeshes(List<MeshBinding> bindings)
		{
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
				MeshBinding binding = new MeshBinding()
				{
					renderer = renderer,
					originalMesh = mesh,
					bakedMesh = null,

					submeshes = new List<Submesh>(),
				};

				// Iterate through all submeshes in the meshss
				for (int submeshIdx = 0; submeshIdx < mesh.subMeshCount; ++submeshIdx)
				{
					Material material = materials[submeshIdx % materials.Length];

					// Check if the materials' shader is supported
					if (material.shader.name != "Standard")
					{
						Debug.LogWarning("[Superluminal]: Ignoring submesh because of unsupported shader", renderer);
						continue;
					}

					// Add the submesh to our list
					binding.submeshes.Add(new Submesh()
					{
						idx = submeshIdx,
						material = material
					});
				}

				bindings.Add(binding);
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

		public Scene Scene
		{
			get { return scene; }
		}
	}
}
