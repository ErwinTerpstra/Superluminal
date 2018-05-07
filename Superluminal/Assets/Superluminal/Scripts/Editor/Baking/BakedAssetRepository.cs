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
	public class BakedAssetRepository : ScriptableObject
	{
		private const string ASSET_NAME = "SuperluminalRepository.asset";
				
		public static BakedAssetRepository Create(Scene scene)
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

			BakedAssetRepository repository = CreateInstance<BakedAssetRepository>();

			AssetDatabase.CreateAsset(repository, assetPath);
			AssetDatabase.SaveAssets();

			return repository;
		}
		
		public void StoreMesh(Mesh mesh, string guid)
		{
			mesh.name = string.Format("mesh-{0}", guid);

			AssetDatabase.AddObjectToAsset(mesh, this);
		}

		public void StoreMaterial(Material material, string guid)
		{
			material.name = string.Format("material-{0}-{1}", guid, material.name);

			AssetDatabase.AddObjectToAsset(material, this);
		}

		public void Flush()
		{
			AssetDatabase.SaveAssets();
		}
	}
}
