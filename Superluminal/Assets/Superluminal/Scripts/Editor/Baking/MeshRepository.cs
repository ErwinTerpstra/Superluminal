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
	public class MeshRepository : ScriptableObject
	{
		private const string ASSET_NAME = "MeshRepository.asset";
				
		public static MeshRepository Create(Scene scene)
		{
			string sceneDirectory = Path.GetDirectoryName(scene.path);
			string assetDirectory = sceneDirectory + Path.DirectorySeparatorChar + scene.name;

			if (!Directory.Exists(Application.dataPath + assetDirectory))
			{
				Directory.CreateDirectory(assetDirectory);
				AssetDatabase.Refresh();
			}

			string assetPath = assetDirectory + Path.DirectorySeparatorChar + ASSET_NAME;

			//MeshRepository repository = AssetDatabase.LoadAssetAtPath<MeshRepository>(assetPath);

			MeshRepository repository = CreateInstance<MeshRepository>();

			AssetDatabase.CreateAsset(repository, assetPath);
			AssetDatabase.SaveAssets();

			return repository;
		}
		
		public void StoreMesh(Mesh mesh, string guid)
		{
			//AssetDatabase.CreateAsset(mesh, assetDirectory + Path.DirectorySeparatorChar + guid + ".asset");

			mesh.name = guid;

			AssetDatabase.AddObjectToAsset(mesh, this);
		}

		public void Flush()
		{
			AssetDatabase.SaveAssets();
		}
	}
}
