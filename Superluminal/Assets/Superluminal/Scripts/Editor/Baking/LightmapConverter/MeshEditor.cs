using UnityEngine;

using System.Collections.Generic;

namespace Superluminal
{
	/// <summary>
	/// Manages a local copy of a mesh's vertex attributes for easy modification and storing
	/// </summary>
	public class MeshEditor
	{
		public const int MAX_UV_CHANNELS = 4;

		public readonly Mesh mesh;

		public readonly List<List<int>> indices;

		public readonly List<Vector3> vertices;

		public readonly List<Vector3> normals;

		public readonly List<Vector4> tangents;

		public readonly List<Color> colors;

		public readonly List<List<Vector2>> uvs;

		public MeshEditor(Mesh mesh)
		{
			this.mesh = mesh;

			indices = new List<List<int>>();

			vertices = new List<Vector3>();
			normals = new List<Vector3>();
			tangents = new List<Vector4>();
			colors = new List<Color>();

			uvs = new List<List<Vector2>>();

			for (int uvChannel = 0; uvChannel < MAX_UV_CHANNELS; ++uvChannel)
				uvs.Add(new List<Vector2>());

			Load();
		}

		public void Load()
		{
			for (int submeshIdx = 0; submeshIdx < mesh.subMeshCount; ++submeshIdx)
			{
				List<int> indexList;

				if (submeshIdx >= indices.Count)
				{
					indexList = new List<int>();
					indices.Add(indexList);
				}
				else
					indexList = indices[submeshIdx];

				mesh.GetIndices(indexList, submeshIdx);
			}

			mesh.GetVertices(vertices);
			mesh.GetNormals(normals);
			mesh.GetTangents(tangents);
			mesh.GetColors(colors);

			for (int uvChannel = 0; uvChannel < MAX_UV_CHANNELS; ++uvChannel)
				mesh.GetUVs(uvChannel, uvs[uvChannel]);
		}

		public void Store()
		{
			mesh.Clear();

			mesh.SetVertices(vertices);

			if (normals.Count > 0)
				mesh.SetNormals(normals);

			if (tangents.Count > 0)
				mesh.SetTangents(tangents);

			if (colors.Count > 0)
				mesh.SetColors(colors);

			for (int uvChannel = 0; uvChannel < MAX_UV_CHANNELS; ++uvChannel)
			{
				if (uvs[uvChannel].Count > 0)
					mesh.SetUVs(uvChannel, uvs[uvChannel]);
			}

			mesh.subMeshCount = indices.Count;

			for (int submeshIdx = 0; submeshIdx < indices.Count; ++submeshIdx)
				mesh.SetTriangles(indices[submeshIdx], submeshIdx);
		}

		public List<Vector2> LightmapUV
		{
			get { return uvs[1].Count > 0 ? uvs[1] : uvs[0]; }
		}
		
	}
}
