using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Superluminal
{
	public class RaytracerBackend : BakeBackend
	{
		private Raytracer raytracer;

		private RaytracerContext context;

		public RaytracerBackend(BakeSettings settings) : base(settings)
		{
			context = new RaytracerContext();

			List<Light> lights = new List<Light>();
			List<Submesh> submeshes = new List<Submesh>();

			CollectMeshes(submeshes);
			CollectLights(lights);

			context.Setup(submeshes, lights);

			raytracer = new Raytracer(context);
		}

		private void CollectMeshes(List<Submesh> submeshes)
		{
			MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>();

			foreach (MeshRenderer renderer in renderers)
			{
				// Retrieve the linked mesh
				MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();

				if (meshFilter == null)
					continue;

				Mesh mesh = meshFilter.sharedMesh;

				if (mesh == null)
					continue;

				StaticEditorFlags staticFlags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
				Material[] materials = renderer.sharedMaterials;

				// Iterate through all submeshes in the meshss
				for (int submeshIdx = 0; submeshIdx < mesh.subMeshCount; ++submeshIdx)
				{
					Material material = materials[submeshIdx % materials.Length];

					// Check if this is a mesh that should be used as a occluder
					if ((staticFlags & settings.occluderFlags) != 0)
					{
						submeshes.Add(new Submesh()
						{
							transform = renderer.transform,
							material = material,
							mesh = mesh,
							submeshIdx = submeshIdx,
						});
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

		public override void Bake(BakeTarget target)
		{
			Vector3[] vertices = target.originalMesh.vertices;
			Vector3[] normals = target.originalMesh.normals;
			Vector2[] texcoords = target.originalMesh.uv;

			// Create a new mesh with the same vertex attributes
			Mesh bakedMesh = new Mesh();
			bakedMesh.vertices = vertices;
			bakedMesh.normals = normals;
			bakedMesh.uv = texcoords;

			CopyIndices(target.originalMesh, bakedMesh);

			target.bakedMesh = bakedMesh;

			// Bake vertex colors
			Color[] colors = new Color[target.originalMesh.vertexCount];

			for (int vertexIdx = 0; vertexIdx < vertices.Length; ++vertexIdx)
			{
				Vector3 position = vertices[vertexIdx];
				Vector3 normal = normals[vertexIdx];

				position = target.renderer.transform.TransformPoint(position);
				normal = target.renderer.transform.TransformDirection(normal);

				Color irradiance = raytracer.Integrate(position, normal, settings.bounces, settings.indirectSamples);
				colors[vertexIdx] = irradiance;
			}

			// Store the generated colors
			bakedMesh.colors = colors;
		}

		public RaytracerContext Context
		{
			get { return context; }
		}
	}
}
