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

			foreach (BakeTarget binding in bakeData.targets)
			{
				if (binding.bakedMesh == null)
					continue;

				MeshFilter meshFilter = binding.renderer.GetComponent<MeshFilter>();
				meshFilter.sharedMesh = binding.bakedMesh;

				binding.renderer.enabled = true;
			}

			
		}
	}

}
