using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Callbacks;

namespace Superluminal
{
	public static class ScenePostProcessor
	{

		[PostProcessSceneAttribute(2)]
		public static void OnPostprocessScene()
		{
			BakeData bakeData = Object.FindObjectOfType<BakeData>();

			if (bakeData == null)
				return;

			HashSet<GameObject> rootObjects = new HashSet<GameObject>();

			foreach (BakeTarget binding in bakeData.targets)
			{
				if (binding.bakedMesh == null || binding.renderer == null)
					continue;

				MeshFilter meshFilter = binding.renderer.GetComponent<MeshFilter>();
				meshFilter.sharedMesh = binding.bakedMesh;

				binding.renderer.enabled = true;

				rootObjects.Add(binding.renderer.transform.root.gameObject);
			}

			foreach (GameObject root in rootObjects)
				StaticBatchingUtility.Combine(root);
			
		}
	}

}
