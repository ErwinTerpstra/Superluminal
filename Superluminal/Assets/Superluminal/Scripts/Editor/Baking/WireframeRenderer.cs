using UnityEngine;
using System.Collections;

namespace Superluminal
{
	public class WireframeRenderer
	{
		private Material material;

		public WireframeRenderer()
		{
			material = new Material(Shader.Find("Superluminal/Wireframe"));
			material.hideFlags = HideFlags.HideAndDontSave;
		}

		public void Dispose()
		{
			Object.DestroyImmediate(material);
		}

		public void DrawWireframe(Mesh mesh, int submeshIdx, Matrix4x4 mtx, Color color)
		{
			Vector3[] vertices = mesh.vertices;
			int[] indices = mesh.GetIndices(submeshIdx);

			GL.PushMatrix();
			GL.MultMatrix(mtx);

			GL.Begin(GL.LINES);
			GL.Color(color);

			for (int indexOffset = 0; indexOffset < indices.Length; indexOffset += 3)
			{
				Vector3 v0 = vertices[indices[indexOffset + 0]];
				Vector3 v1 = vertices[indices[indexOffset + 1]];
				Vector3 v2 = vertices[indices[indexOffset + 2]];

				GL.Vertex(v0);
				GL.Vertex(v1);

				GL.Vertex(v1);
				GL.Vertex(v2);

				GL.Vertex(v2);
				GL.Vertex(v0);
			}

			GL.End();
			GL.PopMatrix();

		}
	}
}
