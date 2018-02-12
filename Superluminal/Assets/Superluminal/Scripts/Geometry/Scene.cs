using System;
using System.Collections.Generic;

using UnityEngine;

namespace Superluminal
{
	public class Scene
	{
		private struct TriangleData
		{
			public MeshBinding binding;

			public Submesh submesh;
		}

		private KDTree tree;

		private Dictionary<Triangle, TriangleData> triangleMap;

		private Light[] lights;

		public Scene()
		{
			tree = new KDTree();
			triangleMap = new Dictionary<Triangle, TriangleData>();
		}

		public void Setup(List<MeshBinding> submeshes, List<Light> lights)
		{
			ProcessMeshes(submeshes);

			this.lights = lights.ToArray();
		}

		private void ProcessMeshes(List<MeshBinding> bindings)
		{ 
			tree.Clear();
			triangleMap.Clear();
			
			List<Vector3> vertices = new List<Vector3>();
			List<int> indices = new List<int>();

			List<Triangle> triangles = new List<Triangle>();

			// Iterate through all passed submeshes
			foreach (MeshBinding binding in bindings)
			{
				// Retrieve vertex data for this mesh
				vertices.Clear();
				binding.originalMesh.GetVertices(vertices);

				foreach (Submesh submesh in binding.submeshes)
				{
					indices.Clear();

					// Retrieve index data for this submesh
					binding.originalMesh.GetIndices(indices, submesh.idx);

					// Create a triangle object for each triangle in the submesh
					for (int i = 0; i < indices.Count; i += 3)
					{
						Vector3 v0 = vertices[indices[i + 0]];
						Vector3 v1 = vertices[indices[i + 1]];
						Vector3 v2 = vertices[indices[i + 2]];

						v0 = binding.renderer.transform.TransformPoint(v0);
						v1 = binding.renderer.transform.TransformPoint(v1);
						v2 = binding.renderer.transform.TransformPoint(v2);

						Triangle triangle = new Triangle(v0, v1, v2);
						triangles.Add(triangle);

						// Store which submesh this triangle belongs to
						triangleMap.Add(triangle, new TriangleData()
						{
							binding = binding,
							submesh = submesh
						});
					}
				}
			}
			
			tree.Generate(triangles);			
		}

		/// <summary>
		/// Performs a raycast against all triangles in the scene
		/// </summary>
		/// <param name="ray"></param>
		/// <param name="hitInfo"></param>
		/// <returns></returns>
		public bool Raycast(ref Ray ray, out RaycastHit hitInfo)
		{
			hitInfo = new RaycastHit();
			return tree.IntersectRay(ref ray, ref hitInfo);
		}

		/// <summary>
		/// Finds the mesh binding and submesh to which the given triangle belongs
		/// </summary>
		/// <param name="triangle"></param>
		/// <returns></returns>
		public bool RetrieveTriangleData(Triangle triangle, out MeshBinding binding, out Submesh submesh)
		{
			TriangleData triangleData;
			if (triangleMap.TryGetValue(triangle, out triangleData))
			{
				binding = triangleData.binding;
				submesh = triangleData.submesh;

				return true;
			}
			else
			{
				binding = null;
				submesh = null;

				return false;
			}
		}

		public KDTree Tree
		{
			get { return tree; }
		}

		public Light[] Lights
		{
			get { return lights; }
		}

	}
}
