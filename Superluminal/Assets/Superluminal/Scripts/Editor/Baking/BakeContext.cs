using System;
using System.Collections.Generic;

using UnityEngine;

namespace Superluminal
{
	public class BakeContext
	{
		private struct TriangleData
		{
			public Submesh submesh;
		}
		
		private KDTree tree;

		private Dictionary<Triangle, TriangleData> triangleMap;

		private Light[] lights;

		private long castedRayCount;

		public BakeContext()
		{
			tree = new KDTree();
			triangleMap = new Dictionary<Triangle, TriangleData>();
		}

		public void Setup(List<Submesh> submeshes, List<Light> lights)
		{
			castedRayCount = 0;

			ProcessMeshes(submeshes);

			this.lights = lights.ToArray();
		}

		private void ProcessMeshes(List<Submesh> submeshes)
		{ 
			tree.Clear();
			triangleMap.Clear();
			
			List<Vector3> vertices = new List<Vector3>();
			List<int> indices = new List<int>();

			List<Triangle> triangles = new List<Triangle>();

			// Iterate through all passed submeshes
			foreach (Submesh submesh in submeshes)
			{
				vertices.Clear();
				indices.Clear();

				// Retrieve vrertex and index data for this submesh
				submesh.mesh.GetVertices(vertices);
				submesh.mesh.GetIndices(indices, submesh.submeshIdx);

				// Create a triangle object for each triangle in the submesh
				for (int i = 0; i < indices.Count; i += 3)
				{
					Vector3 v0 = vertices[indices[i + 0]];
					Vector3 v1 = vertices[indices[i + 1]];
					Vector3 v2 = vertices[indices[i + 2]];

					v0 = submesh.transform.TransformPoint(v0);
					v1 = submesh.transform.TransformPoint(v1);
					v2 = submesh.transform.TransformPoint(v2);

					Triangle triangle = new Triangle(v0, v1, v2);
					triangles.Add(triangle);

					// Store which submesh this triangle belongs to
					triangleMap.Add(triangle, new TriangleData()
					{
						submesh = submesh
					});
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
			++castedRayCount;

			hitInfo = new RaycastHit();
			return tree.IntersectRay(ref ray, ref hitInfo);
		}

		/// <summary>
		/// Finds the submesh to which the given triangle belongs
		/// </summary>
		/// <param name="triangle"></param>
		/// <returns></returns>
		public bool RetrieveTriangleData(Triangle triangle, out Submesh submesh)
		{
			TriangleData triangleData;
			if (triangleMap.TryGetValue(triangle, out triangleData))
			{
				submesh = triangleData.submesh;

				return true;
			}
			else
			{
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

		public long CastedRayCount
		{
			get { return castedRayCount; }
		}
	}
}
