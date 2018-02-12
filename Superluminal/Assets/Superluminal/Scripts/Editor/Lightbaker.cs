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

		public Lightbaker()
		{
			scene = new Scene();
			raytracer = new Raytracer(scene);
		}

		public void SetupScene()
		{
			List<Submesh> submeshes = new List<Submesh>();

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

				// Iterate through all submeshes in the meshss
				for (int submeshIdx = 0; submeshIdx < mesh.subMeshCount; ++submeshIdx)
				{
					Material material = materials[submeshIdx % materials.Length];

					// Check if the materials' shader is upported
					if (material.shader.name != "Standard")
					{
						Debug.LogWarning("[Superluminal]: Ignoring submesh because of unsupported shader", renderer);
						continue;
					}

					// Add the submesh to our list
					submeshes.Add(new Submesh()
					{
						mesh = mesh,
						submeshIdx = submeshIdx,
						transform = renderer.transform
					});
				}
			}

			scene.Setup(submeshes);
		}

		public Scene Scene
		{
			get { return scene; }
		}
	}
}
