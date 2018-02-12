using System;
using System.Collections.Generic;

using UnityEngine;

namespace Superluminal
{
	public class Scene
	{
		private KDTree tree;

		public Scene()
		{
			tree = new KDTree();
		}

		public void Setup(List<Submesh> submeshes)
		{
			tree.Clear();

			Debug.Log("[Superluminal]: Converting meshes..");

			List<Vector3> vertices = new List<Vector3>();
			List<int> indices = new List<int>();

			List<Triangle> triangles = new List<Triangle>();

			// Iterate through all passed submeshes
			foreach (Submesh submesh in submeshes)
			{
				vertices.Clear();
				indices.Clear();

				// Retrieve vertex and index data for this submesh
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
				}
			}

			Debug.Log("[Superluminal]: Generating KD-tree..");

			tree.Generate(triangles);

			Debug.Log("[Superluminal]: Scene setup finished");
		}

		public KDTree Tree
		{
			get { return tree; }
		}

	}
}
