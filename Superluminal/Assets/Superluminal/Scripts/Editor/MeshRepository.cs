using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEditor;

namespace Superluminal
{
	/// <summary>
	/// Manages baked meshes for a single scene.
	/// </summary>
	public class MeshRepository
	{
		private const string MESH_DIRECTORY = "BakedMeshes";

		private Scene scene;

		private Dictionary<string, Mesh> meshes;

		private string meshDirectory;

		public MeshRepository(Scene scene)
		{
			this.scene = scene;

			string sceneDirectory = Path.GetDirectoryName(scene.path);
			meshDirectory = sceneDirectory + Path.DirectorySeparatorChar + scene.name + Path.DirectorySeparatorChar + MESH_DIRECTORY;

			if (!Directory.Exists(Application.dataPath + meshDirectory))
			{
				Directory.CreateDirectory(meshDirectory);
				AssetDatabase.Refresh();
			}

			meshes = new Dictionary<string, Mesh>();

			LoadAll();
		}

		private void LoadAll()
		{
			string[] guids = AssetDatabase.FindAssets("t:Mesh", new string[] { meshDirectory });
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

				meshes.Add(guid, mesh);
			}
		}

		public void StoreMesh(Mesh mesh, string guid)
		{
			AssetDatabase.CreateAsset(mesh, meshDirectory + Path.DirectorySeparatorChar + guid + ".asset");

			meshes[guid] = mesh;
		}

		public void Flush()
		{
			AssetDatabase.SaveAssets();
		}
	}
}
